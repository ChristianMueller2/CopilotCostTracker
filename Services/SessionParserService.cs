// Services/SessionParserService.cs
using System.Text.Json;
using System.Text.Json.Nodes;
using CopilotCostTracker.Models;

namespace CopilotCostTracker.Services;

public class SessionParserService : ISessionParserService
{
    private readonly PricingService _pricing;

    public SessionParserService(PricingService pricing) => _pricing = pricing;

    public async Task<IReadOnlyList<CopilotSession>> ParseFileAsync(string filePath, string sourceFolder)
    {
        var sessions = new List<CopilotSession>();
        // Each file belongs to exactly one session; track the most recent session.start.
        (string sessionId, DateTime startTime, string repo, string branch)? startInfo = null;

        try
        {
            var lines = await File.ReadAllLinesAsync(filePath);

            // Detect format from the first non-empty line
            var firstNonEmpty = lines.FirstOrDefault(l => !string.IsNullOrWhiteSpace(l));
            if (firstNonEmpty != null)
            {
                var probe = JsonNode.Parse(firstNonEmpty);
                var probeType = probe?["type"]?.GetValue<string>();
                var probeKind = probe?["kind"];

                if (probeType == "partition.created")
                    return await ParseEclipsePartitionAsync(lines, filePath, sourceFolder);

                if (probeKind != null && probe?["v"]?["sessionId"] != null)
                    return await ParseVsCodeChatSessionAsync(lines, filePath, sourceFolder);
            }

            foreach (var line in lines)
            {
                if (string.IsNullOrWhiteSpace(line)) continue;
                try
                {
                    var node = JsonNode.Parse(line);
                    if (node == null) continue;

                    var type = node["type"]?.GetValue<string>();
                    if (type == "session.start")
                    {
                        var id = node["id"]?.GetValue<string>() ?? string.Empty;
                        var data = node["data"];
                        var sessionId    = data?["sessionId"]?.GetValue<string>() ?? id;
                        var startTimeStr = data?["startTime"]?.GetValue<string>();
                        var startTime    = DateTime.TryParse(startTimeStr, out var dt) ? dt : DateTime.MinValue;
                        var repo         = data?["context"]?["repository"]?.GetValue<string>() ?? string.Empty;
                        var branch       = data?["context"]?["branch"]?.GetValue<string>() ?? string.Empty;
                        startInfo = (sessionId, startTime, repo, branch);
                    }
                    else if (type == "session.shutdown")
                    {
                        if (startInfo is not { } si) continue;

                        var data = node["data"];
                        var shutdownTime = data?["sessionStartTime"]?.GetValue<long>() ?? 0;
                        var durationMs   = data?["totalApiDurationMs"]?.GetValue<long>() ?? 0;
                        var modelMetrics = data?["modelMetrics"]?.AsObject();
                        if (modelMetrics == null) continue;

                        foreach (var kvp in modelMetrics)
                        {
                            var modelName   = kvp.Key;
                            var modelNode   = kvp.Value;
                            var reqCount    = modelNode?["requests"]?["count"]?.GetValue<int>() ?? 0;
                            var usageNode   = modelNode?["usage"];

                            var session = new CopilotSession
                            {
                                SessionId    = si.sessionId,
                                Model        = modelName,
                                Repository   = si.repo,
                                Branch       = si.branch,
                                StartTime    = si.startTime,
                                DurationMs   = durationMs,
                                RequestCount = reqCount,
                                SourceFile   = filePath,
                                SourceFolder = sourceFolder,
                                Usage = new TokenUsage
                                {
                                    InputTokens      = usageNode?["inputTokens"]?.GetValue<long>()      ?? 0,
                                    OutputTokens     = usageNode?["outputTokens"]?.GetValue<long>()     ?? 0,
                                    CacheReadTokens  = usageNode?["cacheReadTokens"]?.GetValue<long>()  ?? 0,
                                    CacheWriteTokens = usageNode?["cacheWriteTokens"]?.GetValue<long>() ?? 0,
                                }
                            };
                            _pricing.CalculateCosts(session);
                            sessions.Add(session);
                        }
                    }
                }
                catch { /* silently skip malformed lines */ }
            }
        }
        catch { /* silently skip unreadable files */ }

        return sessions;
    }

    private Task<IReadOnlyList<CopilotSession>> ParseEclipsePartitionAsync(
        string[] lines, string filePath, string sourceFolder)
    {
        var sessions = new List<CopilotSession>();
        try
        {
            string conversationId = string.Empty;
            DateTime startTime    = DateTime.MinValue;
            DateTime lastTime     = DateTime.MinValue;
            string source         = string.Empty;
            int turnCount         = 0;
            long estInputTokens   = 0;
            long estOutputTokens  = 0;

            foreach (var line in lines)
            {
                if (string.IsNullOrWhiteSpace(line)) continue;
                try
                {
                    var node = JsonNode.Parse(line);
                    if (node == null) continue;

                    var type  = node["type"]?.GetValue<string>();
                    var tsRaw = node["timestamp"]?.GetValue<string>();
                    if (DateTime.TryParse(tsRaw, null,
                            System.Globalization.DateTimeStyles.RoundtripKind, out var ts))
                    {
                        if (startTime == DateTime.MinValue) startTime = ts;
                        if (ts > lastTime) lastTime = ts;
                    }

                    var data = node["data"];
                    switch (type)
                    {
                        case "partition.created":
                            conversationId = data?["conversationId"]?.GetValue<string>() ?? string.Empty;
                            source         = data?["source"]?.GetValue<string>() ?? string.Empty;
                            var createdAt  = data?["createdAt"]?.GetValue<long>() ?? 0;
                            if (createdAt > 0)
                                startTime = DateTimeOffset.FromUnixTimeMilliseconds(createdAt).LocalDateTime;
                            break;

                        case "assistant.turn_end":
                            if (data?["status"]?.GetValue<string>() == "success") turnCount++;
                            break;

                        // Estimate input tokens from user messages (chars ÷ 4)
                        case "user.message":
                            estInputTokens += EstimateTokens(data?["content"]?.GetValue<string>());
                            break;

                        case "user.message_rendered":
                            estInputTokens += EstimateTokens(data?["renderedMessage"]?.GetValue<string>());
                            break;

                        // Estimate output tokens from assistant messages
                        case "assistant.message":
                            var msgText = data?["text"]?.GetValue<string>()
                                       ?? data?["content"]?.GetValue<string>();
                            estOutputTokens += EstimateTokens(msgText);
                            var thinkingText = data?["thinking"]?["text"]?.GetValue<string>();
                            estOutputTokens += EstimateTokens(thinkingText);
                            break;
                    }
                }
                catch { /* skip malformed line */ }
            }

            if (!string.IsNullOrEmpty(conversationId))
            {
                var durationMs = lastTime > startTime
                    ? (long)(lastTime - startTime).TotalMilliseconds
                    : 0;

                var session = new CopilotSession
                {
                    SessionId    = conversationId,
                    Model        = "eclipse" + (string.IsNullOrEmpty(source) ? "" : $"/{source}"),
                    StartTime    = startTime,
                    DurationMs   = durationMs,
                    RequestCount = turnCount,
                    SourceFile   = filePath,
                    SourceFolder = sourceFolder,
                    IsEstimated  = true,
                    Usage = new TokenUsage
                    {
                        InputTokens  = estInputTokens,
                        OutputTokens = estOutputTokens,
                    }
                };
                _pricing.CalculateCosts(session);
                sessions.Add(session);
            }
        }
        catch { /* unreadable */ }

        return Task.FromResult<IReadOnlyList<CopilotSession>>(sessions);
    }

    private static long EstimateTokens(string? text)
        => string.IsNullOrEmpty(text) ? 0 : (long)Math.Round(text.Length / 4.0);

    private Task<IReadOnlyList<CopilotSession>> ParseVsCodeChatSessionAsync(
        string[] lines, string filePath, string sourceFolder)
    {
        var sessions = new List<CopilotSession>();
        try
        {
            string sessionId  = string.Empty;
            string modelId    = string.Empty;
            DateTime startTime = DateTime.MinValue;
            int requestCount  = 0;

            // completionTokens per request index (updated incrementally, keep last value)
            var completionTokensPerRequest = new Dictionary<int, long>();

            foreach (var line in lines)
            {
                if (string.IsNullOrWhiteSpace(line)) continue;
                try
                {
                    var node = JsonNode.Parse(line);
                    if (node == null) continue;

                    var kind = node["kind"]?.GetValue<int>() ?? -1;

                    if (kind == 0)
                    {
                        var v = node["v"];
                        sessionId = v?["sessionId"]?.GetValue<string>() ?? string.Empty;
                        modelId   = v?["inputState"]?["selectedModel"]?["identifier"]?.GetValue<string>() ?? string.Empty;
                        var createdAt = v?["creationDate"]?.GetValue<long>() ?? 0;
                        if (createdAt > 0)
                            startTime = DateTimeOffset.FromUnixTimeMilliseconds(createdAt).LocalDateTime;
                    }
                    else if (kind == 2)
                    {
                        var k = node["k"]?.AsArray();
                        // kind=2 with k=["requests"] means a request batch was appended
                        if (k?.Count == 1 && k[0]?.GetValue<string>() == "requests")
                        {
                            var reqArray = node["v"]?.AsArray();
                            if (reqArray != null)
                                requestCount = Math.Max(requestCount, reqArray.Count);
                        }
                    }
                    else if (kind == 1)
                    {
                        var k = node["k"]?.AsArray();
                        if (k?.Count == 3
                            && k[0]?.GetValue<string>() == "requests"
                            && k[2]?.GetValue<string>() == "completionTokens")
                        {
                            try
                            {
                                var idx    = k[1]!.GetValue<int>();
                                var tokens = node["v"]?.GetValue<long>() ?? 0;
                                completionTokensPerRequest[idx] = tokens;
                                requestCount = Math.Max(requestCount, idx + 1);
                            }
                            catch { /* index not an int */ }
                        }
                    }
                }
                catch { /* skip malformed line */ }
            }

            if (!string.IsNullOrEmpty(sessionId))
            {
                // Normalise model: strip "copilot/" vendor prefix
                var model = modelId.StartsWith("copilot/", StringComparison.OrdinalIgnoreCase)
                    ? modelId["copilot/".Length..]
                    : modelId;

                var totalOutputTokens = completionTokensPerRequest.Values.Sum();

                var session = new CopilotSession
                {
                    SessionId    = sessionId,
                    Model        = model,
                    StartTime    = startTime,
                    RequestCount = requestCount,
                    SourceFile   = filePath,
                    SourceFolder = sourceFolder,
                    Usage = new TokenUsage
                    {
                        OutputTokens = totalOutputTokens,
                    }
                };
                _pricing.CalculateCosts(session);
                sessions.Add(session);
            }
        }
        catch { /* unreadable */ }

        return Task.FromResult<IReadOnlyList<CopilotSession>>(sessions);
    }

    public async Task<IReadOnlyList<CopilotSession>> ParseFolderAsync(string folderPath, bool includeSubdirectories)
    {
        if (!Directory.Exists(folderPath)) return [];

        var option = includeSubdirectories ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
        var files  = Directory.GetFiles(folderPath, "*.jsonl", option);
        var tasks  = files.Select(f => ParseFileAsync(f, folderPath));
        var results = await Task.WhenAll(tasks);
        return results.SelectMany(r => r).ToList();
    }

    public async Task<IReadOnlyList<CopilotSession>> ParseAllFoldersAsync(IEnumerable<WatchedFolder> folders)
    {
        var tasks = folders.Select(f => ParseFolderAsync(f.Path, f.IncludeSubdirectories));
        var results = await Task.WhenAll(tasks);

        // Deduplicate by SessionId + Model (overlapping folder configs)
        return results
            .SelectMany(r => r)
            .GroupBy(s => s.SessionId + "|" + s.Model)
            .Select(g => g.First())
            .OrderByDescending(s => s.StartTime)
            .ToList();
    }

    public async Task<IReadOnlyList<SessionEvent>> ParseRawEventsAsync(string filePath)
    {
        var events = new List<SessionEvent>();
        if (!File.Exists(filePath)) return events;

        try
        {
            var lines = await File.ReadAllLinesAsync(filePath);

            // Detect VS Code chat session format
            var firstNonEmpty = lines.FirstOrDefault(l => !string.IsNullOrWhiteSpace(l));
            if (firstNonEmpty != null)
            {
                var probe = JsonNode.Parse(firstNonEmpty);
                if (probe?["kind"] != null && probe?["v"]?["sessionId"] != null)
                    return ParseVsCodeChatSessionEvents(lines);
            }

            foreach (var line in lines)
            {
                if (string.IsNullOrWhiteSpace(line)) continue;
                try
                {
                    var node = JsonNode.Parse(line);
                    if (node == null) continue;

                    var type      = node["type"]?.GetValue<string>() ?? "unknown";
                    var tsRaw     = node["timestamp"]?.GetValue<string>();
                    var timestamp = DateTime.TryParse(tsRaw, out var dt) ? dt.ToLocalTime() : DateTime.MinValue;
                    var data      = node["data"];

                    var (category, summary, detail) = BuildEventInfo(type, data);

                    events.Add(new SessionEvent
                    {
                        Type      = type,
                        Timestamp = timestamp,
                        Category  = category,
                        Summary   = summary,
                        Detail    = detail,
                    });
                }
                catch { /* skip malformed line */ }
            }
        }
        catch { /* unreadable file */ }

        return events;
    }

    private static IReadOnlyList<SessionEvent> ParseVsCodeChatSessionEvents(string[] lines)
    {
        var events = new List<SessionEvent>();
        // Track request messages keyed by index to reconstruct user turns
        var requestMessages = new Dictionary<int, string>();

        foreach (var line in lines)
        {
            if (string.IsNullOrWhiteSpace(line)) continue;
            try
            {
                var node = JsonNode.Parse(line);
                if (node == null) continue;

                var kind = node["kind"]?.GetValue<int>() ?? -1;

                if (kind == 0)
                {
                    var v          = node["v"];
                    var sessionId  = v?["sessionId"]?.GetValue<string>() ?? string.Empty;
                    var model      = v?["inputState"]?["selectedModel"]?["identifier"]?.GetValue<string>() ?? string.Empty;
                    var createdAt  = v?["creationDate"]?.GetValue<long>() ?? 0;
                    var location   = v?["initialLocation"]?.GetValue<string>() ?? string.Empty;
                    var ts         = createdAt > 0
                        ? DateTimeOffset.FromUnixTimeMilliseconds(createdAt).LocalDateTime
                        : DateTime.MinValue;

                    if (model.StartsWith("copilot/", StringComparison.OrdinalIgnoreCase))
                        model = model["copilot/".Length..];

                    events.Add(new SessionEvent
                    {
                        Type      = "vscode.session",
                        Timestamp = ts,
                        Category  = EventCategory.System,
                        Summary   = $"VS Code chat session [{location}]",
                        Detail    = $"Session: {sessionId}\nModel: {model}",
                    });
                }
                else if (kind == 2)
                {
                    var k = node["k"]?.AsArray();
                    if (k?.Count == 1 && k[0]?.GetValue<string>() == "requests")
                    {
                        // New request(s) appended
                        var reqArray = node["v"]?.AsArray();
                        if (reqArray != null)
                        {
                            foreach (var req in reqArray)
                            {
                                var tsMs   = req?["timestamp"]?.GetValue<long>() ?? 0;
                                var msgText = req?["message"]?["text"]?.GetValue<string>() ?? string.Empty;
                                var model  = req?["modelId"]?.GetValue<string>() ?? string.Empty;
                                if (model.StartsWith("copilot/", StringComparison.OrdinalIgnoreCase))
                                    model = model["copilot/".Length..];
                                var ts = tsMs > 0
                                    ? DateTimeOffset.FromUnixTimeMilliseconds(tsMs).LocalDateTime
                                    : DateTime.MinValue;

                                events.Add(new SessionEvent
                                {
                                    Type      = "user.message",
                                    Timestamp = ts,
                                    Category  = EventCategory.User,
                                    Summary   = Truncate(msgText, 160),
                                    Detail    = string.IsNullOrEmpty(model) ? msgText : $"[{model}]\n{msgText}",
                                });
                            }
                        }
                    }
                    else if (k?.Count == 3
                        && k[0]?.GetValue<string>() == "requests"
                        && k[2]?.GetValue<string>() == "response")
                    {
                        // Assistant response chunk
                        var chunks = node["v"]?.AsArray();
                        if (chunks != null)
                        {
                            foreach (var chunk in chunks)
                            {
                                var chunkKind = chunk?["kind"]?.GetValue<string>() ?? string.Empty;
                                if (chunkKind == "markdownContent" || chunkKind == "treeData") continue;
                                var text = chunk?["value"]?.GetValue<string>() ?? string.Empty;
                                if (string.IsNullOrWhiteSpace(text)) continue;
                                events.Add(new SessionEvent
                                {
                                    Type      = "assistant.message",
                                    Timestamp = DateTime.MinValue,
                                    Category  = EventCategory.Assistant,
                                    Summary   = Truncate(text, 160),
                                    Detail    = text,
                                });
                                break; // Only first meaningful chunk per kind=2 response
                            }
                        }
                    }
                }
                else if (kind == 1)
                {
                    var k = node["k"]?.AsArray();
                    if (k?.Count == 3
                        && k[0]?.GetValue<string>() == "requests"
                        && k[2]?.GetValue<string>() == "completionTokens")
                    {
                        // Suppress intermediate token updates – only emit on final modelState
                    }
                    else if (k?.Count == 3
                        && k[0]?.GetValue<string>() == "requests"
                        && k[2]?.GetValue<string>() == "modelState")
                    {
                        try
                        {
                            var idx = k[1]!.GetValue<int>();
                            var completedAt = node["v"]?["completedAt"]?.GetValue<long>() ?? 0;
                            var ts = completedAt > 0
                                ? DateTimeOffset.FromUnixTimeMilliseconds(completedAt).LocalDateTime
                                : DateTime.MinValue;
                            events.Add(new SessionEvent
                            {
                                Type      = "assistant.turn_end",
                                Timestamp = ts,
                                Category  = EventCategory.Assistant,
                                Summary   = $"Turn {idx + 1} completed",
                                Detail    = string.Empty,
                            });
                        }
                        catch { /* index not an int */ }
                    }
                }
            }
            catch { /* skip malformed line */ }
        }

        return events;
    }

    private static (EventCategory category, string summary, string detail) BuildEventInfo(string type, JsonNode? data)
    {
        switch (type)
        {
            case "session.start":
            {
                var repo = data?["context"]?["repository"]?.GetValue<string>()
                           ?? data?["context"]?["cwd"]?.GetValue<string>()
                           ?? string.Empty;
                var producer = data?["producer"]?.GetValue<string>() ?? string.Empty;
                return (EventCategory.System, $"Session started  {producer}".TrimEnd(), repo);
            }

            case "session.shutdown":
            {
                var modelMetrics = data?["modelMetrics"]?.AsObject();
                if (modelMetrics == null)
                    return (EventCategory.System, "Session ended", string.Empty);

                var parts = modelMetrics
                    .Select(kvp =>
                    {
                        var u = kvp.Value?["usage"];
                        var inp = u?["inputTokens"]?.GetValue<long>() ?? 0;
                        var out_ = u?["outputTokens"]?.GetValue<long>() ?? 0;
                        return $"{kvp.Key}: {inp:N0} in / {out_:N0} out";
                    })
                    .ToList();
                var summary = "Session ended · " + string.Join(", ", parts);
                return (EventCategory.System, Truncate(summary, 120), string.Join("\n", parts));
            }

            case "user.message":
            {
                var content = data?["content"]?.GetValue<string>() ?? string.Empty;
                return (EventCategory.User, Truncate(content, 160), content);
            }

            case "assistant.message":
            {
                var content = data?["content"]?.GetValue<string>() ?? string.Empty;
                var toolRequests = data?["toolRequests"]?.AsArray();
                string detail = content;
                if (toolRequests != null && toolRequests.Count > 0)
                {
                    var toolLines = toolRequests
                        .Select(t => "→ " + (t?["name"]?.GetValue<string>() ?? "tool")
                                   + ": " + Truncate(t?["intentionSummary"]?.GetValue<string>()
                                                     ?? t?["arguments"]?.ToString() ?? string.Empty, 80))
                        .ToList();
                    detail = (string.IsNullOrWhiteSpace(content) ? string.Empty : content + "\n\n")
                             + string.Join("\n", toolLines);
                }
                var summary = string.IsNullOrWhiteSpace(content)
                    ? (toolRequests != null && toolRequests.Count > 0
                        ? "→ " + (toolRequests[0]?["name"]?.GetValue<string>() ?? "tool call")
                        : "(empty)")
                    : Truncate(content, 160);
                return (EventCategory.Assistant, summary, detail);
            }

            case "assistant.turn_start":
                return (EventCategory.Assistant, "Turn started", string.Empty);

            case "assistant.turn_end":
            {
                var status = data?["status"]?.GetValue<string>() ?? data?["turnStatus"]?.GetValue<string>() ?? string.Empty;
                return (EventCategory.Assistant, $"Turn ended ({status})", string.Empty);
            }

            case "partition.created":
            {
                var source = data?["source"]?.GetValue<string>() ?? string.Empty;
                var convId = data?["conversationId"]?.GetValue<string>() ?? string.Empty;
                return (EventCategory.System, $"Conversation started [{source}]", convId);
            }

            case "user.message_rendered":
                return (EventCategory.User, "Message rendered", string.Empty);

            case "tool.execution_start":
            {
                var name = data?["toolName"]?.GetValue<string>() ?? "tool";
                var args = data?["arguments"]?.ToString() ?? string.Empty;
                return (EventCategory.Tool, $"⚙ {name}", Truncate(args, 500));
            }

            case "tool.execution_complete":
            {
                var success = data?["success"]?.GetValue<bool>() ?? true;
                var progress = data?["result"]?["progressMessage"]?.GetValue<string>() ?? string.Empty;
                var resultText = data?["result"]?["result"]?[0]?["value"]?.GetValue<string>()
                                 ?? data?["result"]?.ToString() ?? string.Empty;
                var icon = success ? "✓" : "✗";
                var summary = string.IsNullOrEmpty(progress)
                    ? $"{icon} tool complete"
                    : $"{icon} {Truncate(progress, 120)}";
                return (EventCategory.Tool, summary, Truncate(resultText, 500));
            }

            case "tool.result":
            {
                var name   = data?["toolName"]?.GetValue<string>() ?? "tool";
                var output = data?["content"]?.ToString()
                             ?? data?["output"]?.GetValue<string>()
                             ?? string.Empty;
                return (EventCategory.Tool, $"✓ {name}", Truncate(output, 500));
            }

            default:
            {
                var raw = data?.ToString() ?? string.Empty;
                return (EventCategory.System, type, Truncate(raw, 300));
            }
        }
    }

    private static string Truncate(string s, int max) =>
        s.Length <= max ? s : s[..max] + "…";
}
