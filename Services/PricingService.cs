// Services/PricingService.cs
using System.Text.Json;
using CopilotCostTracker.Models;

namespace CopilotCostTracker.Services;

public class PricingService
{
    private static readonly string PricingFilePath =
        Path.Combine(FileSystem.AppDataDirectory, "model_pricing.json");

    /// <summary>
    /// Default pricing from https://docs.github.com/en/copilot/reference/copilot-billing/models-and-pricing
    /// All values are USD per 1 million tokens.
    /// Within a family, more specific keys (mini/nano/codex/higher version) come first
    /// so the Contains-based lookup matches the most precise entry first.
    /// </summary>
    public static IReadOnlyList<ModelPricing> DefaultPricing { get; } =
    [
        // Anthropic
        new() { Provider = "Anthropic", ModelKey = "Claude Fable 5",    InputPer1M = 10.00m, CachedInputPer1M = 1.00m,  CacheWritePer1M = 12.50m, OutputPer1M = 50.00m },
        new() { Provider = "Anthropic", ModelKey = "Claude Opus 5",     InputPer1M = 5.00m,  CachedInputPer1M = 0.50m,  CacheWritePer1M = 6.25m,  OutputPer1M = 25.00m },
        new() { Provider = "Anthropic", ModelKey = "Claude Opus 4.8",   InputPer1M = 5.00m,  CachedInputPer1M = 0.50m,  CacheWritePer1M = 6.25m,  OutputPer1M = 25.00m },
        new() { Provider = "Anthropic", ModelKey = "Claude Opus 4.7",   InputPer1M = 5.00m,  CachedInputPer1M = 0.50m,  CacheWritePer1M = 6.25m,  OutputPer1M = 25.00m },
        new() { Provider = "Anthropic", ModelKey = "Claude Opus 4.6",   InputPer1M = 5.00m,  CachedInputPer1M = 0.50m,  CacheWritePer1M = 6.25m,  OutputPer1M = 25.00m },
        new() { Provider = "Anthropic", ModelKey = "Claude Opus 4.5",   InputPer1M = 5.00m,  CachedInputPer1M = 0.50m,  CacheWritePer1M = 6.25m,  OutputPer1M = 25.00m },
        new() { Provider = "Anthropic", ModelKey = "Claude Sonnet 5",   InputPer1M = 2.00m,  CachedInputPer1M = 0.20m,  CacheWritePer1M = 2.50m,  OutputPer1M = 10.00m },
        new() { Provider = "Anthropic", ModelKey = "Claude Sonnet 4.6", InputPer1M = 3.00m,  CachedInputPer1M = 0.30m,  CacheWritePer1M = 3.75m,  OutputPer1M = 15.00m },
        new() { Provider = "Anthropic", ModelKey = "Claude Sonnet 4.5", InputPer1M = 3.00m,  CachedInputPer1M = 0.30m,  CacheWritePer1M = 3.75m,  OutputPer1M = 15.00m },
        new() { Provider = "Anthropic", ModelKey = "Claude Sonnet 4",   InputPer1M = 3.00m,  CachedInputPer1M = 0.30m,  CacheWritePer1M = 3.75m,  OutputPer1M = 15.00m },
        new() { Provider = "Anthropic", ModelKey = "Claude Haiku 4.5",  InputPer1M = 1.00m,  CachedInputPer1M = 0.10m,  CacheWritePer1M = 1.25m,  OutputPer1M = 5.00m  },
        // OpenAI – mini/nano/codex before base to avoid substring mis-matches
        new() { Provider = "OpenAI",    ModelKey = "GPT-5.6 Sol",       InputPer1M = 5.00m,  CachedInputPer1M = 0.50m,  CacheWritePer1M = 6.25m,  OutputPer1M = 30.00m },
        new() { Provider = "OpenAI",    ModelKey = "GPT-5.6 Terra",     InputPer1M = 2.00m,  CachedInputPer1M = 0.20m,  CacheWritePer1M = 2.50m,  OutputPer1M = 12.00m },
        new() { Provider = "OpenAI",    ModelKey = "GPT-5.6 Luna",      InputPer1M = 0.20m,  CachedInputPer1M = 0.02m,  CacheWritePer1M = 0.25m,  OutputPer1M = 1.20m  },
        new() { Provider = "OpenAI",    ModelKey = "GPT-5.5",           InputPer1M = 5.00m,  CachedInputPer1M = 0.50m,  CacheWritePer1M = 0m,     OutputPer1M = 30.00m },
        new() { Provider = "OpenAI",    ModelKey = "GPT-5.4 mini",      InputPer1M = 0.75m,  CachedInputPer1M = 0.075m, CacheWritePer1M = 0m,     OutputPer1M = 4.50m  },
        new() { Provider = "OpenAI",    ModelKey = "GPT-5.4 nano",      InputPer1M = 0.20m,  CachedInputPer1M = 0.02m,  CacheWritePer1M = 0m,     OutputPer1M = 1.25m  },
        new() { Provider = "OpenAI",    ModelKey = "GPT-5.4",           InputPer1M = 2.50m,  CachedInputPer1M = 0.25m,  CacheWritePer1M = 0m,     OutputPer1M = 15.00m },
        new() { Provider = "OpenAI",    ModelKey = "GPT-5.3-Codex",     InputPer1M = 1.75m,  CachedInputPer1M = 0.175m, CacheWritePer1M = 0m,     OutputPer1M = 14.00m },
        new() { Provider = "OpenAI",    ModelKey = "GPT-5 mini",        InputPer1M = 0.25m,  CachedInputPer1M = 0.025m, CacheWritePer1M = 0m,     OutputPer1M = 2.00m  },
        // Google
        new() { Provider = "Google",    ModelKey = "Gemini 3.6 Flash",  InputPer1M = 1.50m,  CachedInputPer1M = 0.15m,  CacheWritePer1M = 0m,     OutputPer1M = 7.50m  },
        new() { Provider = "Google",    ModelKey = "Gemini 3.5 Flash",  InputPer1M = 1.50m,  CachedInputPer1M = 0.15m,  CacheWritePer1M = 0m,     OutputPer1M = 9.00m  },
        new() { Provider = "Google",    ModelKey = "Gemini 3.1 Pro",    InputPer1M = 2.00m,  CachedInputPer1M = 0.20m,  CacheWritePer1M = 0m,     OutputPer1M = 12.00m },
        new() { Provider = "Google",    ModelKey = "Gemini 3 Flash",    InputPer1M = 0.50m,  CachedInputPer1M = 0.05m,  CacheWritePer1M = 0m,     OutputPer1M = 3.00m  },
        new() { Provider = "Google",    ModelKey = "Gemini 2.5 Pro",    InputPer1M = 1.25m,  CachedInputPer1M = 0.125m, CacheWritePer1M = 0m,     OutputPer1M = 10.00m },
        // GitHub fine-tuned
        new() { Provider = "GitHub",    ModelKey = "Raptor mini",       InputPer1M = 0.25m,  CachedInputPer1M = 0.025m, CacheWritePer1M = 0m,     OutputPer1M = 2.00m  },
        // Microsoft
        new() { Provider = "Microsoft", ModelKey = "MAI-Code-1-Flash",  InputPer1M = 0.75m,  CachedInputPer1M = 0.075m, CacheWritePer1M = 0m,     OutputPer1M = 4.50m  },
        // xAI
        new() { Provider = "xAI",       ModelKey = "Grok 4.5",          InputPer1M = 2.00m,  CachedInputPer1M = 0.50m,  CacheWritePer1M = 0m,     OutputPer1M = 6.00m  },
        // Moonshot AI
        new() { Provider = "Moonshot AI", ModelKey = "Kimi K2.7 Code",  InputPer1M = 0.95m,  CachedInputPer1M = 0.19m,  CacheWritePer1M = 0m,     OutputPer1M = 4.00m  },
        new() { Provider = "Moonshot AI", ModelKey = "Kimi K3",         InputPer1M = 3.00m,  CachedInputPer1M = 0.30m,  CacheWritePer1M = 0m,     OutputPer1M = 15.00m },
    ];

    private static readonly ModelPricing _fallback = new()
    {
        Provider = "", ModelKey = "default",
        InputPer1M = 3.00m, CachedInputPer1M = 0.30m, CacheWritePer1M = 3.75m, OutputPer1M = 15.00m
    };

    private List<ModelPricing> _models;

    public PricingService() => _models = LoadFromFileOrDefaults();

    // Persistence

    private static List<ModelPricing> LoadFromFileOrDefaults()
    {
        try
        {
            if (File.Exists(PricingFilePath))
            {
                var json   = File.ReadAllText(PricingFilePath);
                var loaded = JsonSerializer.Deserialize(json, AppJsonContext.Default.ListModelPricing);
                if (loaded is { Count: > 0 })
                    return loaded;
            }
        }
        catch { /* fall through to defaults */ }

        return [.. DefaultPricing];
    }

    public IReadOnlyList<ModelPricing> GetAll() => _models.AsReadOnly();

    public void UpdateAll(List<ModelPricing> models) => _models = models;

    public void ResetToDefaults() => _models = [.. DefaultPricing];

    public async Task SaveAsync()
    {
        try
        {
            var json = JsonSerializer.Serialize(_models, AppJsonContext.Default.ListModelPricing);
            await File.WriteAllTextAsync(PricingFilePath, json);
        }
        catch { /* ignore write errors */ }
    }

    // Lookup

    // Models seen in logs that had no matching entry — consumers can read and
    // reset this set to show "unknown model" warnings after a refresh.
    private readonly HashSet<string> _unknownModels = new(StringComparer.OrdinalIgnoreCase);
    public IReadOnlySet<string> UnknownModels => _unknownModels;
    public void ClearUnknownModels() => _unknownModels.Clear();

    // Normalise for comparison: lowercase + spaces->hyphens so that e.g.
    // "gpt-5.4-mini" (log) correctly matches "GPT-5.4 mini" (table key).
    private static string Normalize(string s)
        => s.ToLowerInvariant().Replace(' ', '-');

    public ModelPricing GetPricing(string modelName)
    {
        // Skip synthetic model keys used internally by the parsers.
        if (string.IsNullOrWhiteSpace(modelName)
            || modelName.StartsWith("eclipse/", StringComparison.OrdinalIgnoreCase))
            return _fallback;

        var normalizedModel = Normalize(modelName);
        foreach (var p in _models)
            if (normalizedModel.Contains(Normalize(p.ModelKey), StringComparison.Ordinal))
                return p;

        _unknownModels.Add(modelName);
        return _fallback;
    }

    // Cost calculation

    public void CalculateCosts(CopilotSession session)
    {
        var p = GetPricing(session.Model);
        session.InputCostUsd      = (session.Usage.InputTokens      / 1_000_000m) * p.InputPer1M;
        session.OutputCostUsd     = (session.Usage.OutputTokens     / 1_000_000m) * p.OutputPer1M;
        session.CacheReadCostUsd  = (session.Usage.CacheReadTokens  / 1_000_000m) * p.CachedInputPer1M;
        session.CacheWriteCostUsd = (session.Usage.CacheWriteTokens / 1_000_000m) * p.CacheWritePer1M;
        // 1 AI credit = $0.01
        session.InputCredits      = session.InputCostUsd      / 0.01m;
        session.OutputCredits     = session.OutputCostUsd     / 0.01m;
        session.CacheReadCredits  = session.CacheReadCostUsd  / 0.01m;
        session.CacheWriteCredits = session.CacheWriteCostUsd / 0.01m;
    }
}
