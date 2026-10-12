# Middleware stub

A small web app that stands in for the real middleware when you run the Zendesk Monitor locally. It records every call the monitor makes, saves the payloads, and can be told to fail.

It is not part of the solution and is never built or deployed with the monitor. See [docs/DEVELOPER-GUIDE.md](../../docs/DEVELOPER-GUIDE.md) – section 10 for why and how to set it up, and section 11 (Run locally against PP, and the worked examples) for testing with it.

## Run it

```powershell
cd tools/MiddlewareStub
dotnet run
```

It listens on `http://localhost:8080`. In your local config entry, set `MiddlewareUrl` to `http://localhost:8080/`. The subscription key and basic auth values can be anything non-empty.

## What you see

One line per call from the monitor:

```
10:01:14  PUT    escalated  ticket=16379    -> 200  dest=itsm  1 comment(s) [500 chars]  description=79 chars
          ! DUPLICATE - received 2 times
```

- Green means 200; red means a forced failure.
- Yellow warnings flag duplicates, a missing `subscription-key`, a missing `Authorization: Basic` header, or a body without `ticket.id`.
- Every payload is saved to `tools/MiddlewareStub/payloads/<ticket>-<method>-<time>.json` (git-ignored).
- `http://localhost:8080/` lists the calls in a browser and refreshes every 5 seconds.

## Controlling it

Send these from PowerShell (or any HTTP client):

| Request | Effect |
|---|---|
| `Invoke-RestMethod -Method Post "http://localhost:8080/__fail-all?status=500"` | Every call returns 500 (or any status you give) |
| `Invoke-RestMethod -Method Post "http://localhost:8080/__fail/16379?status=500"` | Only ticket 16379 fails, every time |
| `Invoke-RestMethod -Method Post "http://localhost:8080/__fail/16379?status=500&times=2"` | Ticket 16379 fails twice, then succeeds – tests the monitor's retries |
| `Invoke-RestMethod -Method Post "http://localhost:8080/__delay?ms=5000"` | Wait 5 seconds before every response |
| `Invoke-RestMethod -Method Post "http://localhost:8080/__ok"` | Back to normal: all 200, no delay |
| `Invoke-RestMethod "http://localhost:8080/__calls"` | List calls (newest first) |
| `Invoke-RestMethod "http://localhost:8080/__calls/0/body"` | Full body of the newest call |
| `Invoke-RestMethod -Method Delete "http://localhost:8080/__calls"` | Clear the list and duplicate tracking (saved files are kept) |