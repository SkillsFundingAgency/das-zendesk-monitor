# Zendesk Monitor – Developer Guide

This guide explains what the Zendesk Monitor does, how it works step by step, and how to run and test it locally.
It describes the code on `master` (commit `b45f0fe`). The original `README.md` explains why the monitor was designed this way; this guide explains what it actually does.

> **New here and just want to run it?** Follow [10. Running it locally](#10-running-it-locally), then work through the [worked examples](#5-worked-examples). Come back to sections 1–9 when you need to understand why something happened.

## Contents

**Understanding the monitor**
1. [What it does](#1-what-it-does)
2. [How the monitor gets started](#2-how-the-monitor-gets-started)
3. [The tags](#3-the-tags)
4. [What happens to each ticket, step by step](#4-what-happens-to-each-ticket-step-by-step)
5. [What is sent to the middleware](#5-what-is-sent-to-the-middleware)
6. [What Zendesk does](#6-what-zendesk-does-configured-in-zendesk-not-in-this-repo)
7. [Known issues](#7-known-issues)

**Working on it**

8. [Where things are in the code](#8-where-things-are-in-the-code)
9. [Configuration](#9-configuration)
10. [Running it locally](#10-running-it-locally)
11. [Testing](#11-testing)
    1. [Unit tests](#1-unit-tests)
    2. [Preparing a test ticket](#2-preparing-a-test-ticket)
    3. [Test in PP](#3-test-in-pp)
    4. [Run locally against PP](#4-run-locally-against-pp)
    5. [Worked examples](#5-worked-examples)
    6. [User acceptance tests](#6-user-acceptance-tests)
    
---

## 1. What it does

Some Zendesk tickets need to be transferred to other systems: **ServiceNow** (Tier 3 escalations) or **PCRM** (provider hand-offs). When a ticket needs to be transferred, a Zendesk trigger adds a tag to it and calls the monitor's webhook. 
The monitor then reads the ticket, sends the details to the middleware (which creates or updates the record in ServiceNow/PCRM), and removes the tag so the ticket isn't sent again.

Webhooks alone aren't reliable: a call can fail or be missed, Zendesk won't always retry it, and repeated failures can stop the webhook from sending, so a missed call could go unnoticed. 
So the monitor also runs on a timer as a safety net. Because the tag stays on the ticket until the send has succeeded, the timer can pick up tickets that still have a tag – whether the webhook call was missed or an earlier send failed – and try again. (There are limits – see Known issues: the timer reads only the first page of search results, and closed tickets can't be updated.) (See `README.md` for the original design reasoning.)

```
 Agent in Zendesk ──► Zendesk triggers add a tag ──► Zendesk Monitor ──► Middleware ──► ServiceNow / PCRM
                      (and call the monitor's        (this repo)         (not this repo)
                       webhook)
```

The monitor does not decide whether a ticket should be escalated – Zendesk does that with its own triggers. The monitor only delivers what Zendesk has tagged.

---

## 2. How the monitor gets started

There are two entry points, which perform the same action for one or more tickets.

| Way in | Function | When | What it processes |
|---|---|---|---|
| **Webhook** (normal path) | `NotifyTicket` – HTTP `POST /api/NotifyTicket` with body `{"id": 12345}` | Immediately, when a Zendesk trigger calls it | That one ticket |
| **Timer** (safety net) | `BackgroundTaskEntryPoint` | On the schedule in `MonitorCronSetting` (every 30 minutes in Azure) | The tagged tickets returned by the tag search (first page only – see Known issues) |

- The **webhook** is called by Zendesk triggers (see section 6). It starts the `ShareListedTickets` orchestration for the one ticket ID.
- The **timer** starts the `ShareAllTickets` orchestration, which first searches Zendesk for tagged tickets (one page of results – see Known issues). The timer is meant to allow only one run at a time: before starting, it checks whether the previous run is still in progress and, if so, skips this one. **This check doesn't work** – see Known issues – so overlapping runs are possible.

Both orchestrations are **Azure Durable Functions**. They call the activity `ShareTicket` once per ticket, **one ticket at a time**, with up to **5 attempts** per ticket (a 1-second retry interval with no backoff – in practice the gaps are a little longer because of request and storage overhead). If a ticket still fails, the error is logged and the run moves on to the next ticket.

---

## 3. The tags

The monitor only looks at these six tags:

| Reason | Waiting to be sent | Being sent |
|---|---|---|
| Escalated (→ ServiceNow, Tier 3) | `pending_middleware_escalated` | `sending_middleware_escalated` |
| Handed off (→ PCRM) | `pending_middleware_handedoff` | `sending_middleware_handedoff` |
| Solved | `pending_middleware_solved` | `sending_middleware_solved` |

- `pending_…` is added by **Zendesk triggers**. It means "this needs to be sent".
- `sending_…` is added by **the monitor** just before it calls the middleware. It means "send in progress".
- When the send succeeds, the monitor removes the tag completely.
- A ticket left on `sending_…` means a previous attempt didn't finish; the monitor treats it exactly like `pending_…` and tries again.

The monitor also **reads** (but never changes) one more tag, which tells the middleware where to send the ticket on to:

| Tag | Set by | Used for |
|---|---|---|
| `middleware_destination_itsm` / `middleware_destination_pcrm` | Zendesk triggers | Sets `destination` in the payload: `itsm` = ServiceNow, `pcrm` = PCRM. The middleware uses this to route the ticket. If neither tag is present, `itsm` is used. |

---

## 4. What happens to each ticket, step by step

This functionality is in `Watcher.ShareTicket` (`src/SFA.DAS.Zendesk.Monitor/Watcher.cs`).

**Step 1 – Read the ticket from Zendesk** (read-only)
- `GET /tickets/{id}.json?include=users,organizations,audits` – the ticket, requester and organisation
- `GET /tickets/{id}/comments.json` – the comments (first page only)
- `GET /tickets/{id}/audits.json` – the change history (first page only)

**Step 2 – Work out the reason for sharing**

The reason is `escalated`, `handedoff` or `solved` – the last part of the ticket's tag.

| Reason | Shared when… | Comments sent |
|---|---|---|
| Escalated | it has the tag **and** at least one **private** comment made in a Zendesk update containing an `escalated_tag` change. All retrieved updates are checked, not just the latest escalation. | Private comments from **all** those updates |
| Handed off | it has the tag | All comments |
| Solved | it has the tag | None |

If no reason matches (for example an escalated ticket with no qualifying note), **the ticket is skipped silently** – nothing is sent, the tags are not touched, and no error is logged.

**Step 3 – Mark as sending** (`MarkSharing`) – Zendesk write
- Add `sending_middleware_{reason}`, remove `pending_middleware_{reason}`.
- e.g. for an escalation: add `sending_middleware_escalated`, remove `pending_middleware_escalated`.

**Step 4 – Send to the middleware**

| Reason | Call |
|---|---|
| Escalated | `PUT  {MiddlewareUrl}/ticket?escalate=true` |
| Handed off | `POST {MiddlewareUrl}/ticket` |
| Solved | `DELETE {MiddlewareUrl}/ticket` |

**Step 5 – Mark as shared** (`MarkShared`) – Zendesk write, only if step 4 succeeded
- Remove `sending_middleware_{reason}`.

**If anything fails**, the activity is retried (up to 5 attempts in total), and if it still fails the tag stays on the ticket so it's picked up again on the next timer run. What gets logged depends on where it failed:
- **Step 3 (or building the payload) fails** – for example the 403 on a closed ticket. This happens before the send, so there's no `Error sharing … Payload: …` entry; you only see the exception and `Error processing ticket …`.
- **Step 4 or 5 fails** – the middleware call or the final tag update. The error is logged as `Error sharing … Payload: …`, with the full payload.

### How tag updates are written

Tag changes are made with `PUT /tickets/{id}.json` using Zendesk's *safe update* (`safe_update: true` plus the ticket's `updated_stamp`). The monitor first reads the ticket's current tags, adds/removes its own tag, and writes the whole list back. If someone else changed the ticket in between, Zendesk rejects the update instead of overwriting their change.

---

## 5. What is sent to the middleware

The body is JSON, built in `TicketProfile.cs`:

```json
{
  "ticket": {
    "id": 12345,
    "status": "hold",
    "subject": "…",
    "description": "…the first message on the ticket…",
    "created_at": "…",
    "via": "Web Form",
    "destination": "itsm",
    "custom_fields": [ { "id": 360004146580, "value": "…" } ],
    "comments": [ { "id": 1, "body": "…", "created_at": "…", "attachments": [ { "filename": "…", "url": "…" } ] } ],
    "requester":    { "…name, email, phone, address fields…" },
    "organization": { "…name, phone, address fields…" }
  }
}
```

- Only custom fields **with a value** are sent.
- Attachments are sent as **links**, not file contents.
- `via` is translated: email/api/twitter → `Mail`, web → `Web Form`, chat → `Chat`, voice → `Phone call (…)` or `Voicemail`.

---

## 6. What Zendesk does (configured in Zendesk, not in this repo)

The monitor depends on triggers configured in Zendesk. These are the ones seen in PP (Prod may differ):

**Solved tickets**
- *Solved Tickets notify Zendesk Monitor* – when status **is** Solved (and the update isn't by the monitor's account): add `pending_middleware_solved` and call the monitor webhook.

**Escalations** – the agent applies the escalation macro, which adds `escalated_tag`, then:
- *Escalation – Check Resolver group / Check Service Offering / Check escalation status / Check comment is private* – validation; if a check fails, `escalated_tag` is removed and the agent is told what's missing.
- *Escalation – ServiceNow / PCRM Destination* – add `middleware_destination_itsm` or `middleware_destination_pcrm`.
- *Escalation – Confirmed to escalate* – swap `escalated_tag` for `escalated` + `pending_middleware_escalated`.
- *Escalation – Notifies Zendesk Monitor* – call the monitor webhook.

**Other**
- *Remove tags from closed ticket follow-up* – removes monitor tags from follow-up tickets created from closed tickets.

The key triggers – *Solved Tickets notify Zendesk Monitor*, *Confirmed to escalate* and *Notifies Zendesk Monitor* – **ignore updates made by the monitor's own account** (role is not *ESFA Middleware*); the validation triggers don't. Keep that in mind when testing with the monitor's API token – your changes won't fire them.

---

## 7. Known issues

| Issue | What happens |
|---|---|
| **Closed tickets with a tag** | The monitor's account can't update closed tickets, so step 3 fails with **403 Forbidden** on every run. It appears the tags are added to closed tickets by the solved trigger when the satisfaction-survey automation runs, but this needs to be confirmed by the Zendesk developer. |
| **Escalations made when the ticket is created** | If a ticket is created and escalated in the same save, `escalated_tag` is recorded as a `Create` event, not a `Change` event. The escalation check doesn't find it, so the ticket is **silently never sent**. |
| **Escalation notes come from every qualifying update, not just the latest** | The monitor checks all the updates it retrieves and sends private notes from any update that contains an `escalated_tag` change. It doesn't restrict this to the latest escalation or check the tag was newly added. So an older note can be sent again, and a later escalation without a private note can still be sent using an earlier note. A note saved in a separate update from the escalation is not included. The ticket is skipped if none of the retrieved updates contains a qualifying private comment. |
| **Timer runs can overlap** | `RecoveryOrchestration.cs` (lines 20–24) checks for a running instance with the fixed ID `{8B2772F1-…}`, but then starts `ShareAllTickets` passing that ID as the orchestration's **input**, not its instance ID. Each run gets a generated ID, so the next check never finds it and a new run can start while the previous one is still going. Fix: pass `new StartOrchestrationOptions { InstanceId = WatcherInstance }`. This was introduced by the .NET 8 isolated-worker upgrade (commit `6cc592a`): the old in-process code called `StartNewAsync(name, WatcherInstance)`, where the second argument *was* the instance ID. Overlapping runs can process the same ticket at the same time, so this may contribute to duplicate sends (see below). |
| **Timer only reads the first page of search results** | `SharingTickets.GetTicketsForSharing` makes one search request and doesn't follow `next_page`. Zendesk returns at most 100 results per page, so if the first page is full of tickets that keep failing or being skipped (e.g. closed tickets), tickets on later pages may never be processed. |
| **Long tickets** | Only the first page of comments and audits is read. Very long tickets may lose notes. |
| **Duplicates are possible by design** | If the middleware call succeeds but `MarkShared` fails, the retry sends the ticket again. The middleware is expected to cope with duplicates ("at-least-once" delivery). Overlapping timer runs (above) make duplicates more likely. |
| **Middleware failures aren't visible in Zendesk** | A ticket stuck on `sending_…` is the only sign in Zendesk that a send failed. The details are in the function logs (`Error sharing … Payload: …`). Failures before the send don't log the payload: a step 3 failure (e.g. the closed-ticket 403) leaves the tag on `pending_…`; a failure building the payload, after step 3, leaves it on `sending_…`. |

---

## 8. Where things are in the code

| Folder / file | What it is |
|---|---|
| `src/SFA.DAS.Zendesk.Monitor.Function/` | The Azure Function app (entry points) |
| ├ `NotifyOrchestration.cs` | `NotifyTicket` HTTP webhook |
| ├ `RecoveryOrchestration.cs` | `BackgroundTaskEntryPoint` timer |
| ├ `WatcherOrchestration.cs` | `ShareAllTickets` / `ShareListedTickets` orchestrations, retry policy |
| ├ `DurableWatcher.cs` | `SearchTickets` / `ShareTicket` activities |
| `src/SFA.DAS.Zendesk.Monitor/` | The logic |
| ├ `Watcher.cs` | Steps 1–5 above |
| ├ `Zendesk/SharingTickets.cs` | Tag search, `MarkSharing`, `MarkShared`, tag names |
| ├ `Zendesk/*ResponseValidation.cs` | The rules for escalated / handed off / solved |
| ├ `Zendesk/IApi.cs`, `IApiExtensions.cs` | Zendesk API calls, safe tag update |
| ├ `Middleware/IApi.cs` | Middleware API calls |
| └ `TicketProfile.cs` | Mapping from Zendesk ticket to middleware payload |

---

## 9. Configuration

Settings come from two places.

**`local.settings.json` / Function App settings** – needed by the Functions host itself, so they can't come from table storage:

| Setting | Example (local) | Notes |
|---|---|---|
| `AzureWebJobsStorage` | `UseDevelopmentStorage=true` | Durable Functions state (Azurite locally) |
| `FUNCTIONS_WORKER_RUNTIME` | `dotnet-isolated` | |
| `MonitorCronSetting` | `0 */30 * * * *` | Timer schedule (6-field cron). Keep it set even when the timer is disabled. |
| `AzureWebJobs.BackgroundTaskEntryPoint.Disabled` | `true` | Local only – switches the timer off. Leave out (or `false`) to run it. |
| `ConfigNames` | `SFA.DAS.ZendeskMonitor` | Which table storage config entry to load |
| `EnvironmentName` | `LOCAL` | Partition key of the config entry |
| `ConfigurationStorageConnectionString` | `UseDevelopmentStorage=true` | Where the config table is |
| `APPLICATIONINSIGHTS_CONNECTION_STRING` | (leave out locally) | App Insights is skipped when empty |

**Table storage** – add a row to the `Configuration` table in Azurite using Azure Storage Explorer:

- PartitionKey = `LOCAL`
- RowKey = `SFA.DAS.ZendeskMonitor_1.0`
- Data = latest JSON from the employer-config repo

```json
{
  "ZendeskUrl": "https://<subdomain>.zendesk.com/api/v2/",
  "ApiUser": "<email>/token",
  "ApiKey": "<zendesk api token>",
  "MiddlewareUrl": "https://<middleware>/",
  "MiddlewareSubscriptionKey": "<key>",
  "MiddlewareApiBasicAuth": "<base64 user:password>",
  "MonitorCronSetting": "0 */30 * * * *"
}
```

`MonitorCronSetting` has to be in the JSON because the schema requires it, but the monitor doesn't use this copy – see below.

**MonitorCronSetting** (the timer schedule) is read by the Functions host when the app starts, so it must be an app setting – it can't come from the config table.

- **Running locally:** add it to `local.settings.json`. If it's missing, the function won't start.
- **Deploying via the pipeline:** make sure the variable group has a `MonitorCronSetting` variable. The pipeline passes it to the ARM template, which sets it as a Function App setting. If the variable is missing, the deployment fails.
- **Employer config:** `MonitorCronSetting` is also listed in the employer-config definition for this repo, but that copy isn't used. It's filled from the same pipeline variable, so it needs no extra value, and it **should be removed** from the definition.

---

## 10. Running it locally

### What you need
- .NET 8 SDK and Visual Studio (or VS Code) (will become .NET 10 in the near future)
- Azure Functions Core Tools v4 (`func --version`)
- Azurite (storage emulator) – no database is needed
- PP Zendesk API details (from the PP config entry)
- Something to stand in for the middleware: the **middleware stub** in `tools/MiddlewareStub`

### Why do we need a middleware stub?
When you run the monitor locally, you point it at the middleware stub instead of the real middleware. This:
- **keeps your local monitor's calls out of ServiceNow and PCRM** – everything your local monitor sends goes to the stub. But the deployed PP monitor is still running against the same tickets and may send them to the real PP middleware first or as well (see [Run locally against PP](#4-run-locally-against-pp)). This matters most during failure tests and while you're paused on a breakpoint.
- **shows exactly what your monitor sent** – every call is listed in the stub's window and saved as a payload file. Both the deployed PP monitor and your local one use the same Zendesk account, so the ticket history can't tell you which one sent it. The stub can.
- **lets you test failures** – you can make it return errors or respond slowly, to check the monitor's retries and error handling

It's a test tool only. It isn't part of the solution and is never deployed. See [`tools/MiddlewareStub/README.md`](../tools/MiddlewareStub/README.md) for how to control it.

### One-off setup
1. **Clone** `https://github.com/SkillsFundingAgency/das-zendesk-monitor`.
2. **If the Debug build fails with `NU1903`** (AutoMapper 14 security advisory): the library project treats warnings as errors in Debug. Locally, add `<WarningsNotAsErrors>NU1902;NU1903</WarningsNotAsErrors>` to the Debug `PropertyGroup` in `SFA.DAS.Zendesk.Monitor.csproj`, or build in Release. (A proper fix – upgrading or removing AutoMapper – is a separate piece of work.)
3. **Create `local.settings.json`** in `src/SFA.DAS.Zendesk.Monitor.Function/` (it's git-ignored):
   ```json
   {
     "IsEncrypted": false,
     "Values": {
       "AzureWebJobsStorage": "UseDevelopmentStorage=true",
       "FUNCTIONS_WORKER_RUNTIME": "dotnet-isolated",
       "EnvironmentName": "LOCAL",
       "ConfigNames": "SFA.DAS.ZendeskMonitor",
       "ConfigurationStorageConnectionString": "UseDevelopmentStorage=true",
       "MonitorCronSetting": "0 */30 * * * *",
       "AzureWebJobs.BackgroundTaskEntryPoint.Disabled": "true"
     }
   }
   ```
4. **Make sure `local.settings.json` is copied to the output folder.** The Function project needs:
   ```xml
   <ItemGroup>
     <None Update="local.settings.json">
       <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
       <CopyToPublishDirectory>Never</CopyToPublishDirectory>
     </None>
   </ItemGroup>
   ```
5. **Add the config entry** to Azurite's `Configuration` table (Azure Storage Explorer → Emulator → Tables) as described in section 9, with `MiddlewareUrl` set to `http://localhost:8080/` (the middleware stub).
6. **Optional – quieter console.** Azurite request logging floods the console. Locally, add `"Azure.Core": "Warning"`, `"DurableTask.AzureStorage": "Warning"` and `"DurableTask.Core": "Warning"` to `logLevel` in `host.json` (don't commit).

### Run
1. Start Azurite.
2. Start the middleware stub (`cd tools/MiddlewareStub` then `dotnet run`).
3. Press F5 on `SFA.DAS.Zendesk.Monitor.Function`. The console lists the functions, including `NotifyTicket: [POST] http://localhost:7071/api/NotifyTicket`.
4. Send a ticket to the monitor – this does exactly what the Zendesk webhook does:
   ```powershell
   Invoke-RestMethod -Method Post http://localhost:7071/api/NotifyTicket -Body '{"id":12345}' -ContentType application/json
   ```
   The ticket needs a `pending_middleware_*` tag – see [section 11.2, Preparing a test ticket](#2-preparing-a-test-ticket).
5. Check the result:

   | Where | What you see | Meaning |
   |---|---|---|
   | Function console | `Sharing ticket 12345` → `Sharing Escalated ticket 12345` → `Shared Escalated ticket 12345` | Sent successfully |
   | | `Error sharing … Payload: …` | The send failed – the error and payload are logged |
   | | only `Sharing ticket 12345` | Skipped – the ticket didn't qualify |
   | Middleware stub | A line for the ticket, and a payload file in `tools/MiddlewareStub/payloads/` | Your monitor sent it – and exactly what it sent |
   | Ticket history | `+sending_…` then `-sending_…` | Tags updated as expected |

### Running the timer
Normally leave the timer off and use `NotifyTicket`. To test the timer itself, set `AzureWebJobs.BackgroundTaskEntryPoint.Disabled` to `false` and `MonitorCronSetting` to `0 */2 * * * *` (every 2 minutes), and restart. Be aware it then processes the tagged tickets in PP (first page of search results), including old closed ones. Because of the overlapping-runs issue (see Known issues), a short interval can start a new run every 2 minutes while earlier ones are still retrying – stop the function as soon as you've finished.

Disabling the timer doesn't stop a durable orchestration that's already started: a timer run left over from an earlier session resumes when you start the function. See "Tickets you didn't send appear in the console" in the [worked examples](#5-worked-examples).

### Useful read-only Zendesk API calls
- `GET /tickets/{id}.json` – status and tags for a specific ticket
- `GET /tickets/{id}/audits.json` – full history for a specific ticket (who changed which tag, and when)
- `GET /search.json?query=tags:pending_middleware_escalated` – get a list of tickets the monitor will pick up

---

## 11. Testing

| Approach | Use it to test | Effort |
|---|---|---|
| [Unit tests](#1-unit-tests) | The monitor's logic – any code change | Low |
| [Test in PP](#3-test-in-pp) | The whole thing end to end, including the real Zendesk webhook and middleware | Low – no local setup |
| [Run locally against PP](#4-run-locally-against-pp) | Your code on real PP tickets, when you need to see the payload, debug with breakpoints, or force a middleware failure | Medium – needs section 10 |

Most changes only need unit tests and testing in PP. Both PP approaches need a test ticket – see [Preparing a test ticket](#2-preparing-a-test-ticket).

### 1. Unit tests

Run with `dotnet test src/SFA.DAS.Zendesk.Monitor.UnitTests` or Test Explorer. Write the test first and check it fails on the current code.

Most tests follow this pattern – copy one from `Escalating_tickets.cs`, `Handing_off_tickets.cs` or `Solving_tickets.cs`:

```csharp
[Theory, ZendeskAutoData]
public async Task Escalated_ticket_is_sent(
    [Frozen] FakeZendeskApi zendesk,          // in-memory Zendesk
    [Frozen] Middleware.IApi middleware,      // fake middleware - check what was sent
    Watcher sut,                              // the class under test
    [Pending.Escalated] Ticket ticket)        // a ticket the monitor will share
{
    zendesk.Tickets.Add(ticket);              // always add the ticket to the fake Zendesk

    await sut.ShareTicket(ticket.Id);

    await middleware.Received().EscalateTicket(Arg.Any<Middleware.EventWrapper>());
}
```

Setting up the data:

| To get… | Use |
|---|---|
| A ticket the monitor will share | `[Pending.Escalated]`, `[Pending.HandedOff]` or `[Pending.Solved] Ticket ticket` |
| A different status or tag | `ticket.Status = "closed";` / `ticket.Tags = new List<string> { "sending_middleware_solved" };` |
| Control over the escalation note | Add `[Frozen] AuditedComment comment` **before** the ticket parameter, then e.g. `comment.Public = true;` or `comment.AuditTagEvent.Value = "";` (note not in the escalation update) |
| A requester or organisation | `zendesk.Users.Add(user); ticket.RequesterId = user.Id;` (same for `Organizations` / `OrganizationId`) |
| A middleware failure | `middleware.When(x => x.EscalateTicket(Arg.Any<Middleware.EventWrapper>())).Do(_ => throw new Exception());` |

Checking the result:

| To check… | Use |
|---|---|
| It was sent | `await middleware.Received().EscalateTicket(...)` |
| It wasn't sent | `await middleware.DidNotReceive().EscalateTicket(Arg.Any<Middleware.EventWrapper>());` |
| The tags afterwards | `zendesk.Tickets.First(x => x.Id == ticket.Id).Tags.Should()...` |

### 2. Preparing a test ticket

You can create and tag test tickets in PP through the Zendesk API, using the PP API details from the PP config entry (`ZendeskUrl`, `ApiUser`, `ApiKey`). **PP only – never PROD.**

Paste this into PowerShell:

```powershell
# Connect (PP only)
$base = "https://<pp-subdomain>.zendesk.com/api/v2"
$auth = [Convert]::ToBase64String([Text.Encoding]::ASCII.GetBytes("<ApiUser>:<ApiKey>"))
$h    = @{ Authorization = "Basic $auth" }
$me   = (Invoke-RestMethod "$base/users/me.json" -Headers $h).user.id

# 1. Create a test ticket ($id is cleared first, so a failed create can't leave an old value behind)
$id = $null
$body = @{ ticket = @{ subject = "MONITOR TEST - please ignore"; assignee_id = $me; tags = @("monitor_test")
          comment = @{ body = "Monitor test ticket"; public = $false } } } | ConvertTo-Json -Depth 5
$id = (Invoke-RestMethod -Method Post "$base/tickets.json" -Headers $h -ContentType "application/json" -Body $body).ticket.id
$id

# 2. Put it in the state you want (pick one)

# Escalated - must be a separate update from the create
$body = @{ ticket = @{ status = "hold"; comment = @{ body = "Escalation note"; public = $false }
          tags = @("monitor_test", "escalated_tag", "pending_middleware_escalated") } } | ConvertTo-Json -Depth 5
Invoke-RestMethod -Method Put "$base/tickets/$id.json" -Headers $h -ContentType "application/json" -Body $body

# Solved
$body = @{ ticket = @{ status = "solved"; tags = @("monitor_test", "pending_middleware_solved") } } | ConvertTo-Json -Depth 5
Invoke-RestMethod -Method Put "$base/tickets/$id.json" -Headers $h -ContentType "application/json" -Body $body

# Handed off
$body = @{ ticket = @{ tags = @("monitor_test", "pending_middleware_handedoff") } } | ConvertTo-Json -Depth 5
Invoke-RestMethod -Method Put "$base/tickets/$id.json" -Headers $h -ContentType "application/json" -Body $body

# Re-run a ticket that's already been sent: put the pending tag back
$body = @{ ticket = @{ tags = @("monitor_test", "pending_middleware_escalated") } } | ConvertTo-Json -Depth 5
Invoke-RestMethod -Method Put "$base/tickets/$id.json" -Headers $h -ContentType "application/json" -Body $body
```

Good to know:
- **The webhook doesn't fire for these changes.** The API token belongs to the monitor's own account, and the Zendesk triggers ignore that account's changes. You have to get the ticket to the monitor yourself (see 3 and 4).
- **Escalations must be a separate update from the create.** If you create and escalate in one call, the monitor skips the ticket (see Known issues).
- Set tags by sending the full `tags` list – it replaces all existing tags, so include every tag you want to keep. (`additional_tags` was ignored in testing.)
- The monitor's account can't change closed tickets.
- Label test tickets clearly (as above) and close them when you've finished.
- Anything the PP monitor sends may create records in PP ServiceNow/PCRM. Let the owning team know if you're creating lots of tickets.

### 3. Test in PP

Deploy your branch to PP as normal, after checking no-one else is using it for another branch. Then:

1. **Put a ticket in the state you want to test**, in one of two ways:
   - **In the Zendesk UI, as a normal agent** (for example, escalate or solve it with the usual macro). This fires the real Zendesk triggers, and the webhook sends the ticket to the monitor within seconds. Use this to test the full end-to-end flow.
   - **Through the API** (see 2). No webhook is sent, so to get the monitor to pick up the ticket, either:
     - **wait for the timer** – the deployed monitor checks every 30 minutes. To make this quicker, you could temporarily change the PP Function App's `MonitorCronSetting` app setting (e.g. `0 */2 * * * *` for every 2 minutes) and set it back afterwards – but **avoid this until the overlapping-runs issue is fixed** (see Known issues): runs would pile up and could send the same tickets more than once. A shorter interval also processes all the tagged PP tickets (first page), not just yours.
     - **send it yourself** – call the PP monitor's `NotifyTicket` endpoint, just as the webhook would:
       ```powershell
       Invoke-RestMethod -Method Post "<NotifyTicket URL>" -Body '{"id":12345}' -ContentType application/json
       ```
       The URL is `https://<pp-function-app>.azurewebsites.net/api/NotifyTicket` (also shown in the Azure portal: PP Function App → Functions → NotifyTicket → **Get function URL**).
2. **Check the ticket history:**
   - `+sending_…` then `-sending_…` → sent successfully
   - stuck on `sending_…` → the send failed (see the PP Function App's logs)
   - no `sending_…` → skipped, or failed before sending
3. **To check what arrived in ServiceNow/PCRM**, ask the team that owns it, quoting the Zendesk ticket number.

### 4. Run locally against PP

Use this when you need to see exactly what's sent, force middleware failures, or debug with breakpoints on real PP tickets. Your local monitor reads PP Zendesk but sends to the middleware stub. Leave the PP Function App running – but be aware it can still pick up your test tickets and send them to the real PP middleware, before or alongside your local monitor. Sending your ticket "straight away" makes that less likely; it can't rule it out.

1. Set up and start everything as in section 10 (Azurite, the middleware stub, the function).
2. Prepare a test ticket (see 2).
3. **Straight away**, send it to your local monitor (section 10, Run, step 4).
   This runs exactly the code the Zendesk webhook would. What it can't test is Zendesk's side – whether the trigger fires and reaches the endpoint. Use approach 3 for that.
4. Check the result (section 10, Run, step 5): the function console, the middleware stub's output and payload files, and the ticket history.

**Why "straight away":** the deployed PP monitor is still running and picks up tagged PP tickets every 30 minutes. If it gets there first, the tag is gone and your local run skips the ticket. If the middleware stub shows nothing and the tag has gone, that's what happened: re-tag the ticket and try again.

**To test a middleware failure**, tell the stub to fail before step 3. For example, `Invoke-RestMethod -Method Post "http://localhost:8080/__fail/12345?status=500&times=2"` makes that ticket fail twice, then succeed. See [`tools/MiddlewareStub/README.md`](../tools/MiddlewareStub/README.md) for the other options.

For a step-by-step run-through of all of this, see the [worked examples](#5-worked-examples) below.

### 5. Worked examples

Step-by-step tests using your local monitor, the middleware stub and PP Zendesk (approach 4). Run them in order. Each one takes a couple of minutes. **PP only – never PROD.**

#### Before you start

1. Check `local.settings.json` has `"AzureWebJobs.BackgroundTaskEntryPoint.Disabled": "true"` (timer off), and that the quieter logging is in `host.json` (section 10, setup step 6) – without it you won't be able to follow the console.
2. Start Azurite, the middleware stub and the function (section 10, Run, steps 1–3). Watch the console for a minute: if it starts processing tickets you didn't send, see "Tickets you didn't send appear in the console" below.
3. Open a PowerShell window and paste the "Connect" lines from [Preparing a test ticket](#2-preparing-a-test-ticket).
4. Paste these two helpers into the same window. Use this window for all the tests – variables and helpers only exist in the window you set them in.

   ```powershell
   # Show a ticket's status and tags
   function Show($id) { (Invoke-RestMethod "$base/tickets/$id.json" -Headers $h).ticket | Select-Object id, status, tags }

   # Send a ticket to the LOCAL monitor (what the webhook does)
   # (waits 5 seconds so the monitor has finished before you check)
   function Notify($id) { Invoke-RestMethod -Method Post http://localhost:7071/api/NotifyTicket -Body "{`"id`":$id}" -ContentType application/json | Out-Null; Start-Sleep 5 }
   ```
5. Clear the stub: `Invoke-RestMethod -Method Delete "http://localhost:8080/__calls"`

**About `$id`.** Every command works on the ticket number held in `$id`:
- **Creating a ticket sets it for you.** The `# 1. Create a test ticket` lines from "Preparing a test ticket" store the new ticket number in `$id` and print it. Each test starts by creating a ticket, so you don't need to type a number.
- **To work on a ticket you already have**, set it yourself, e.g. `$id = 16420` (the number from the Zendesk URL, the ticket list, or an earlier `$id` output).
- **Check it before you carry on.** Type `$id` to see which ticket you're on. If a create fails, `$id` keeps its old value – so make sure it's printed a new number.
- `12345` in the examples elsewhere in this guide is a placeholder – use your own ticket number.

After the `Notify` step in each case, run it **straight away** – the deployed PP monitor runs every 30 minutes and may get there first (see [Run locally against PP](#4-run-locally-against-pp)).

---

#### Test 1 – Stub sanity check (no monitor, no Zendesk)

Checks the stub is running and saving payloads.

```powershell
Invoke-RestMethod -Method Put "http://localhost:8080/ticket?escalate=true&subscription-key=x" -Headers @{ Authorization = "Basic eDp4" } -ContentType application/json -Body '{"ticket":{"id":1,"destination":"itsm","comments":[{"body":"hello"}]}}'
```

**Expect**
- Stub window: a green line `PUT escalated ticket=1 -> 200 dest=itsm 1 comment(s) [5 chars]`
- A file `1-PUT-….json` in `tools/MiddlewareStub/payloads/`
- `http://localhost:8080/` shows the call

Clear the stub again before Test 2.

---

#### Test 2 – Escalated ticket is sent

1. Create a test ticket.
2. Run the `# Escalated` lines from "Preparing a test ticket".
3. `Notify $id`

**Expect**
- Function console: `Sharing ticket …` → `Sharing Escalated ticket …` → `Shared Escalated ticket …`
- Stub: green `PUT escalated ticket=… -> 200 dest=itsm 1 comment(s)` – the one private escalation note. A real run looked like this:
  ```
  11:59:55  PUT    escalated  ticket=16392    -> 200  dest=itsm  1 comment(s) [15 chars]  description=19 chars
  ```
  15 chars is "Escalation note". The description (19 chars, "Monitor test ticket") is the ticket's first message and is always sent.
- Payload file: `comments` contains only `"Escalation note"` – the first comment isn't included, because only private notes from updates containing an `escalated_tag` change are sent. (`"Monitor test ticket"` still appears in `description`, which is always the ticket's first message.)
- `Show $id`: no `pending_…` or `sending_…` tag left – e.g. `{escalated_tag, monitor_test}`

---

#### Test 3 – Solved ticket is sent

1. Create a test ticket.
2. Run the `# Solved` lines.
3. `Notify $id`

**Expect**
- Console: `Sharing Solved ticket …` → `Shared Solved ticket …`
- Stub: green `DELETE solved … 0 comments`, e.g.
  ```
  12:01:27  DELETE solved     ticket=16419    -> 200  dest=itsm  0 comments  description=19 chars
  ```
- `Show $id`: status `solved`, no monitor tags – e.g. `{ar_suggest_true, monitor_test}`. (`ar_suggest_true` is added by Zendesk itself – ignore it.)

---

#### Test 4 – Handed-off ticket is sent with all comments

1. Create a test ticket.
2. Add a second comment and the hand-off tag:
   ```powershell
   $body = @{ ticket = @{ comment = @{ body = "Second comment"; public = $false }
             tags = @("monitor_test", "pending_middleware_handedoff") } } | ConvertTo-Json -Depth 5
   Invoke-RestMethod -Method Put "$base/tickets/$id.json" -Headers $h -ContentType "application/json" -Body $body
   ```
3. `Notify $id`

**Expect**
- Console: `Sharing HandedOff ticket …` → `Shared HandedOff ticket …`
- Stub: green `POST handedoff … 2 comment(s)`, e.g.
  ```
  12:08:40  POST   handedoff  ticket=16420    -> 200  dest=itsm  2 comment(s) [19, 14 chars]  description=19 chars
  ```
- `Show $id`: no monitor tags. The status stays `new` – a hand-off doesn't change the status.

---

#### Test 5 – Escalation without a private note is skipped

1. Create a test ticket.
2. Escalate with a **public** comment:
   ```powershell
   $body = @{ ticket = @{ status = "hold"; comment = @{ body = "Public reply"; public = $true }
             tags = @("monitor_test", "escalated_tag", "pending_middleware_escalated") } } | ConvertTo-Json -Depth 5
   Invoke-RestMethod -Method Put "$base/tickets/$id.json" -Headers $h -ContentType "application/json" -Body $body
   ```
3. `Notify $id`

**Expect**
- Console: only `Sharing ticket …` – no error
- Stub: nothing
- `Show $id`: `pending_middleware_escalated` still there, but `escalated_tag` has gone – the *Check comment is private* validation trigger removed it, just as it would for an agent. (Validation triggers don't ignore the monitor's account.)

Remove the tag afterwards so the deployed PP monitor doesn't keep picking it up:
```powershell
$body = @{ ticket = @{ tags = @("monitor_test") } } | ConvertTo-Json -Depth 5
Invoke-RestMethod -Method Put "$base/tickets/$id.json" -Headers $h -ContentType "application/json" -Body $body
```

---

#### Test 6 – Middleware fails twice, then succeeds (retries)

1. Create a test ticket and run the `# Solved` lines.
2. Make the stub fail this ticket twice:
   `Invoke-RestMethod -Method Post "http://localhost:8080/__fail/$($id)?status=500&times=2"`
3. `Notify $id`

**Expect**
- Stub: two red `-> 500` lines, then a green `-> 200` (the later lines are flagged `DUPLICATE` – expected, they're retries), e.g.
  ```
  12:13:15  DELETE solved     ticket=16422    -> 500  dest=itsm  0 comments  description=19 chars
  12:13:16  DELETE solved     ticket=16422    -> 500  dest=itsm  0 comments  description=19 chars
            ! DUPLICATE - received 2 times
  12:13:18  DELETE solved     ticket=16422    -> 200  dest=itsm  0 comments  description=19 chars
            ! DUPLICATE - received 3 times
  ```
- Console: two `Error sharing … Payload: …` errors, then `Shared Solved ticket …`
- `Show $id`: no monitor tags. If it still shows `sending_…`, the retries haven't finished – wait 10 seconds and run `Show $id` again.

---

#### Test 7 – Middleware keeps failing, then recovers

1. Create a test ticket and run the `# Solved` lines.
2. `Invoke-RestMethod -Method Post "http://localhost:8080/__fail-all?status=500"`
3. `Notify $id`, then `Start-Sleep 20` (enough for all five attempts in our runs – if `Show` still shows `sending_…` with fewer than five stub lines, wait and check again), then `Show $id`

**Expect**
- Stub: five red `-> 500` lines (five attempts – roughly 2 seconds apart in our runs, though only 1 second is configured)
- Console: `Error sharing …` for each attempt, then `Error processing ticket …`
- `Show $id`: stuck on `sending_middleware_solved`

4. Put the stub back to normal and re-send **straight away** (otherwise the deployed PP monitor will pick it up and send it to the real PP middleware):
   ```powershell
   Invoke-RestMethod -Method Post "http://localhost:8080/__ok"
   Notify $id
   ```

**Expect**
- Stub: green `-> 200` (flagged `DUPLICATE - received 6 times` – fine)
- `Show $id`: no monitor tags – a ticket stuck on `sending_…` is retried just like `pending_…`

---

#### Test 8 – Closed ticket with a tag (known issue) – optional

Only run this if you need to see the 403 for yourself. Closed tickets can't be changed again, so the deployed PP monitor will hit a 403 on this ticket on every run from now on.

1. Create a test ticket.
2. Close it with the solved tag still on:
   ```powershell
   $body = @{ ticket = @{ status = "closed"; tags = @("monitor_test", "pending_middleware_solved") } } | ConvertTo-Json -Depth 5
   Invoke-RestMethod -Method Put "$base/tickets/$id.json" -Headers $h -ContentType "application/json" -Body $body
   ```
3. `Notify $id`

**Expect (current `master`)**
- Console: `Sharing Solved ticket …`, then a **403 Forbidden** error on each attempt
- Stub: nothing – it fails before the middleware call
- The ticket can't be changed any more, so the deployed PP monitor will hit the same 403 on every run. Note the ticket number for the team.

---

#### If something doesn't look right

| What you see | Why | What to do |
|---|---|---|
| **400 Bad Request** when creating or updating a ticket | Windows PowerShell 5.1 doesn't always send the `Content-Type` from `-Headers`, so Zendesk doesn't get JSON | Always pass `-ContentType "application/json"` (the snippets in this guide do) |
| A create fails but `$id` still prints a number or a GUID | PowerShell keeps the old value when the command fails | Set `$id = $null` before creating (the snippet does), and don't carry on until `$id` prints a new ticket number |
| `The term 'Notify' is not recognized` | Helpers and variables only exist in the window you created them in | Paste the Connect lines and helpers again, or use the same window throughout |
| `Notify` prints a block of URLs (`StatusQueryGetUri`, …) | That's Durable Functions confirming the run started – not an error | Use the `Notify` helper above, which hides it |
| `Show` straight after `Notify` still shows `pending_…` | The monitor runs in the background – `Notify` returns before it's finished | Wait a few seconds and run `Show` again (the helper waits 5 seconds) |
| **The tag has gone but the stub shows nothing** | The deployed PP monitor got there first and sent it to the **real PP middleware**. Your local monitor then found no tag and skipped the ticket (the console shows only `Sharing ticket …`). | Put the pending tag back and `Notify` straight away (see "Re-run a ticket" in [Preparing a test ticket](#2-preparing-a-test-ticket)) |
| The ticket is on `sending_…` but your console has nothing for it | The deployed PP monitor is part-way through sending it | Wait for PP to finish, then re-tag and `Notify` |
| **Tickets you didn't send appear in the console**, usually with 403 errors (e.g. `Error processing ticket 2529 … 403 (Forbidden)`) | A local timer run is processing the tagged PP tickets. Either the timer isn't disabled, or a timer run from an earlier session has resumed – Durable Functions keeps its state in Azurite. The 403s are old closed tickets (see Known issues). | Stop the function. Check `AzureWebJobs.BackgroundTaskEntryPoint.Disabled` is `true`. In Azure Storage Explorer → Emulator, delete everything named `testhubname…` (tables, queues, blob containers). Start again. **Don't leave it running** – it could send real PP tickets to your stub and remove their tags, so they'd never reach PP ServiceNow/PCRM. |
| **429 Too Many Requests** from Zendesk | Zendesk is rate-limiting the API account. Your tests, your local monitor and the deployed PP monitor all share it. | Wait a minute and try again. Don't carry on with a test until the call succeeds. |
| The console is flooded with storage request logging | The quieter logging in `host.json` is missing (e.g. after a `git stash`) | Re-add it (section 10, setup step 6) |

---

#### Afterwards

- `Invoke-RestMethod -Method Post "http://localhost:8080/__ok"` – make sure the stub is back to normal
- Solve or close the test tickets that are still open

### 6. User acceptance tests

`SFA.DAS.Zendesk.Monitor.UserAcceptanceTests` uses an in-process WireMock middleware and, by default, a fake Zendesk (`FakeZendesk`). Set `Zendesk:UseLiveInstance` to `true` in `appSettings.json` to use a live Zendesk instead. The feature file (`MiddlewareReceiveTicketUpdates.feature`) has two scenarios; *Share ticket with middleware* is marked `@ignore`.