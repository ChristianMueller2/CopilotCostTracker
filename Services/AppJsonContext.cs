// Services/AppJsonContext.cs
using System.Text.Json.Serialization;
using CopilotCostTracker.Models;

namespace CopilotCostTracker.Services;

[JsonSerializable(typeof(WatchedFolder[]))]
[JsonSerializable(typeof(CopilotSession))]
[JsonSerializable(typeof(List<ModelPricing>))]
internal partial class AppJsonContext : JsonSerializerContext { }
