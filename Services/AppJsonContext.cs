// Services/AppJsonContext.cs
using System.Text.Json.Serialization;
using CopilotCostTracker.Models;

namespace CopilotCostTracker.Services;

[JsonSerializable(typeof(WatchedFolder[]))]
[JsonSerializable(typeof(CopilotSession))]
internal partial class AppJsonContext : JsonSerializerContext { }
