// Models/SessionEvent.cs
namespace CopilotCostTracker.Models;

public enum EventCategory { User, Assistant, Tool, System }

public class SessionEvent
{
    public string Type { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public EventCategory Category { get; set; }

    /// <summary>Short one-liner shown in the list.</summary>
    public string Summary { get; set; } = string.Empty;

    /// <summary>Full text content (can be long).</summary>
    public string Detail { get; set; } = string.Empty;

    /// <summary>True when Detail contains more than Summary.</summary>
    public bool HasDetail => !string.IsNullOrWhiteSpace(Detail) && Detail != Summary;
}
