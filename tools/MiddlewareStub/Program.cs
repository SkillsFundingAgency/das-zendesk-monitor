// Middleware stub for testing the Zendesk Monitor locally.
// Stands in for the real middleware: records every call and returns 200 (or a forced failure).
//
// Run:  dotnet run            (listens on http://localhost:8080 - see Properties/launchSettings.json)
// Point the monitor's MiddlewareUrl at http://localhost:8080/
//
// The monitor calls:
//   PUT    /ticket?escalate=true   escalated
//   POST   /ticket                 handed off
//   DELETE /ticket                 solved
//
// For every call this prints a summary line, warns about duplicates and missing auth,
// and saves the full payload to ./payloads/<ticket>-<method>-<time>.json
//
// Control endpoints:
//   GET    /                              list of calls in a browser
//   GET    /__calls                       list of calls (JSON, newest first)
//   GET    /__calls/{n}/body              body of call n (0 = newest)
//   DELETE /__calls                       clear the call list and duplicate tracking
//   POST   /__fail-all?status=500         every call returns that status
//   POST   /__fail/{ticketId}?status=500&times=2
//                                         that ticket returns the status (times = fail N times then succeed; omit = always)
//   POST   /__delay?ms=5000               wait before responding (test timeouts)
//   POST   /__ok                          back to normal: all 200, no delay

using System.Collections.Concurrent;
using System.Net;
using System.Text;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);
builder.Logging.SetMinimumLevel(LogLevel.Warning);
var app = builder.Build();

var payloadDir = Path.Combine(Directory.GetCurrentDirectory(), "payloads");
Directory.CreateDirectory(payloadDir);

var state = new StubState();

app.MapMethods("/ticket", new[] { "POST", "PUT", "DELETE" }, async (HttpRequest req) =>
{
    using var reader = new StreamReader(req.Body);
    var body = await reader.ReadToEndAsync();
    var info = PayloadInfo.Parse(body);

    var reason = req.Method switch
    {
        "PUT" => "escalated",
        "POST" => "handedoff",
        "DELETE" => "solved",
        _ => "?"
    };

    var warnings = new List<string>();
    if (string.IsNullOrEmpty(req.Query["subscription-key"])) warnings.Add("no subscription-key");
    if (!req.Headers.Authorization.ToString().StartsWith("Basic ", StringComparison.OrdinalIgnoreCase)) warnings.Add("no Basic auth header");
    if (info.TicketId is null) warnings.Add("no ticket.id in body");

    var seenCount = state.Seen.AddOrUpdate($"{info.TicketId}:{req.Method}", 1, (_, n) => n + 1);
    if (seenCount > 1) warnings.Add($"DUPLICATE - received {seenCount} times");

    if (state.DelayMs > 0) await Task.Delay(state.DelayMs);

    var status = state.DecideStatus(info.TicketId);

    var file = Path.Combine(payloadDir, $"{info.TicketId?.ToString() ?? "unknown"}-{req.Method}-{DateTime.Now:yyyyMMdd-HHmmss-fff}.json");
    await File.WriteAllTextAsync(file, PayloadInfo.Pretty(body));

    var call = new Call(DateTime.Now, req.Method, reason, info.TicketId, info.Destination,
        info.CommentLengths.Count, info.DescriptionLength, status, string.Join("; ", warnings), body);
    state.Calls.Enqueue(call);

    var comments = info.CommentLengths.Count == 0
        ? "0 comments"
        : $"{info.CommentLengths.Count} comment(s) [{string.Join(", ", info.CommentLengths)} chars]";

    Console.ForegroundColor = status == 200 ? ConsoleColor.Green : ConsoleColor.Red;
    Console.Write($"{call.Time:HH:mm:ss}  {req.Method,-6} {reason,-10} ticket={info.TicketId?.ToString() ?? "?",-8} -> {status}");
    Console.ResetColor();
    Console.WriteLine($"  dest={info.Destination ?? "-"}  {comments}  description={info.DescriptionLength} chars");
    if (warnings.Count > 0)
    {
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine($"          ! {string.Join("; ", warnings)}");
        Console.ResetColor();
    }

    return status == 200
        ? Results.Ok(new { })
        : Results.Problem($"forced failure ({status})", statusCode: status);
});

app.MapGet("/__calls", () => state.Calls.Reverse().Select(c => new
{
    c.Time, c.Method, c.Reason, c.TicketId, c.Destination, c.Comments, c.DescriptionLength, c.Status, c.Warnings
}));

app.MapGet("/__calls/{n:int}/body", (int n) =>
{
    var c = state.Calls.Reverse().ElementAtOrDefault(n);
    return c is null ? Results.NotFound() : Results.Text(PayloadInfo.Pretty(c.Body), "application/json");
});

app.MapDelete("/__calls", () =>
{
    state.Calls.Clear();
    state.Seen.Clear();
    return Results.Ok("cleared");
});

app.MapPost("/__fail-all", (int? status) =>
{
    state.FailAllStatus = status ?? 500;
    return Results.Ok($"all calls will return {state.FailAllStatus}");
});

app.MapPost("/__fail/{ticketId:long}", (long ticketId, int? status, int? times) =>
{
    state.TicketRules[ticketId] = new FailRule(status ?? 500, times);
    return Results.Ok(times is null
        ? $"ticket {ticketId} will always return {status ?? 500}"
        : $"ticket {ticketId} will return {status ?? 500} {times} time(s), then 200");
});

app.MapPost("/__delay", (int ms) =>
{
    state.DelayMs = ms;
    return Results.Ok($"responses delayed by {ms} ms");
});

app.MapPost("/__ok", () =>
{
    state.FailAllStatus = null;
    state.TicketRules.Clear();
    state.DelayMs = 0;
    return Results.Ok("all calls will return 200, no delay");
});

app.MapGet("/", () =>
{
    var rows = new StringBuilder();
    foreach (var c in state.Calls.Reverse())
    {
        var colour = c.Status == 200 ? "#e6ffe6" : "#ffe6e6";
        rows.Append($"<tr style='background:{colour}'><td>{c.Time:HH:mm:ss}</td><td>{c.Method}</td><td>{c.Reason}</td>" +
                    $"<td>{c.TicketId}</td><td>{WebUtility.HtmlEncode(c.Destination ?? "")}</td><td>{c.Comments}</td>" +
                    $"<td>{c.DescriptionLength}</td><td>{c.Status}</td><td>{WebUtility.HtmlEncode(c.Warnings)}</td></tr>");
    }

    var html = "<html><head><title>Middleware stub</title><meta http-equiv='refresh' content='5'>" +
               "<style>body{font-family:sans-serif}td,th{padding:4px 8px;border-bottom:1px solid #ccc;text-align:left}</style></head>" +
               $"<body><h2>Middleware stub - {state.Calls.Count} call(s)</h2>" +
               $"<p>Fail all: {state.FailAllStatus?.ToString() ?? "off"} | Ticket rules: {state.TicketRules.Count} | Delay: {state.DelayMs} ms | Payloads: {WebUtility.HtmlEncode(payloadDir)}</p>" +
               "<table><tr><th>Time</th><th>Method</th><th>Reason</th><th>Ticket</th><th>Dest</th><th>Comments</th><th>Description chars</th><th>Status</th><th>Warnings</th></tr>" +
               rows + "</table></body></html>";
    return Results.Content(html, "text/html");
});

Console.WriteLine("Middleware stub listening. Payloads saved to: " + payloadDir);
Console.WriteLine("Open http://localhost:8080/ to see calls. Ctrl+C to stop.");
app.Run();

record Call(DateTime Time, string Method, string Reason, long? TicketId, string? Destination,
    int Comments, int DescriptionLength, int Status, string Warnings, string Body);

record FailRule(int Status, int? Remaining)
{
    public int? Remaining { get; set; } = Remaining;
}

class StubState
{
    private readonly object _lock = new();

    public ConcurrentQueue<Call> Calls { get; } = new();
    public ConcurrentDictionary<string, int> Seen { get; } = new();
    public ConcurrentDictionary<long, FailRule> TicketRules { get; } = new();
    public int? FailAllStatus { get; set; }
    public int DelayMs { get; set; }

    public int DecideStatus(long? ticketId)
    {
        lock (_lock)
        {
            if (FailAllStatus is int all) return all;
            if (ticketId is long id && TicketRules.TryGetValue(id, out var rule))
            {
                if (rule.Remaining is null) return rule.Status;
                if (rule.Remaining > 0)
                {
                    rule.Remaining--;
                    return rule.Status;
                }
                TicketRules.TryRemove(id, out _);
            }
            return 200;
        }
    }
}

class PayloadInfo
{
    public long? TicketId { get; private set; }
    public string? Destination { get; private set; }
    public int DescriptionLength { get; private set; }
    public List<int> CommentLengths { get; } = new();

    public static PayloadInfo Parse(string body)
    {
        var info = new PayloadInfo();
        try
        {
            using var doc = JsonDocument.Parse(body);
            if (!doc.RootElement.TryGetProperty("ticket", out var t)) return info;

            if (t.TryGetProperty("id", out var id) && id.ValueKind == JsonValueKind.Number) info.TicketId = id.GetInt64();
            if (t.TryGetProperty("destination", out var dest) && dest.ValueKind == JsonValueKind.String) info.Destination = dest.GetString();
            if (t.TryGetProperty("description", out var desc) && desc.ValueKind == JsonValueKind.String) info.DescriptionLength = desc.GetString()?.Length ?? 0;
            if (t.TryGetProperty("comments", out var comments) && comments.ValueKind == JsonValueKind.Array)
            {
                foreach (var c in comments.EnumerateArray())
                {
                    var len = c.TryGetProperty("body", out var b) && b.ValueKind == JsonValueKind.String ? b.GetString()?.Length ?? 0 : 0;
                    info.CommentLengths.Add(len);
                }
            }
        }
        catch (JsonException)
        {
            // not JSON - leave defaults
        }
        return info;
    }

    public static string Pretty(string body)
    {
        try
        {
            using var doc = JsonDocument.Parse(body);
            return JsonSerializer.Serialize(doc.RootElement, new JsonSerializerOptions { WriteIndented = true });
        }
        catch (JsonException)
        {
            return body;
        }
    }
}
