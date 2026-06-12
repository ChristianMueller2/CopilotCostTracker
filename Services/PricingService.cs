// Services/PricingService.cs
using CopilotCostTracker.Models;

namespace CopilotCostTracker.Services;

public class PricingService
{
    private static readonly List<ModelPricing> _table =
    [
        new() { ModelKey = "claude-opus-4.8",   InputPer1M = 5.00m,  CachedInputPer1M = 0.50m,  CacheWritePer1M = 6.25m,  OutputPer1M = 25.00m },
        new() { ModelKey = "claude-opus-4.7",   InputPer1M = 5.00m,  CachedInputPer1M = 0.50m,  CacheWritePer1M = 6.25m,  OutputPer1M = 25.00m },
        new() { ModelKey = "claude-opus-4.6",   InputPer1M = 5.00m,  CachedInputPer1M = 0.50m,  CacheWritePer1M = 6.25m,  OutputPer1M = 25.00m },
        new() { ModelKey = "claude-opus-4.5",   InputPer1M = 5.00m,  CachedInputPer1M = 0.50m,  CacheWritePer1M = 6.25m,  OutputPer1M = 25.00m },
        new() { ModelKey = "claude-sonnet-4.6", InputPer1M = 3.00m,  CachedInputPer1M = 0.30m,  CacheWritePer1M = 3.75m,  OutputPer1M = 15.00m },
        new() { ModelKey = "claude-sonnet-4.5", InputPer1M = 3.00m,  CachedInputPer1M = 0.30m,  CacheWritePer1M = 3.75m,  OutputPer1M = 15.00m },
        new() { ModelKey = "claude-sonnet-4",   InputPer1M = 3.00m,  CachedInputPer1M = 0.30m,  CacheWritePer1M = 3.75m,  OutputPer1M = 15.00m },
        new() { ModelKey = "claude-haiku-4.5",  InputPer1M = 1.00m,  CachedInputPer1M = 0.10m,  CacheWritePer1M = 1.25m,  OutputPer1M = 5.00m  },
        new() { ModelKey = "claude-haiku-4",    InputPer1M = 1.00m,  CachedInputPer1M = 0.10m,  CacheWritePer1M = 1.25m,  OutputPer1M = 5.00m  },
        new() { ModelKey = "gpt-5.5",           InputPer1M = 5.00m,  CachedInputPer1M = 0.50m,  CacheWritePer1M = 0.00m,  OutputPer1M = 30.00m },
        new() { ModelKey = "gpt-5.4 mini",      InputPer1M = 0.75m,  CachedInputPer1M = 0.075m, CacheWritePer1M = 0.00m,  OutputPer1M = 4.50m  },
        new() { ModelKey = "gpt-5.4 nano",      InputPer1M = 0.20m,  CachedInputPer1M = 0.02m,  CacheWritePer1M = 0.00m,  OutputPer1M = 1.25m  },
        new() { ModelKey = "gpt-5.4",           InputPer1M = 2.50m,  CachedInputPer1M = 0.25m,  CacheWritePer1M = 0.00m,  OutputPer1M = 15.00m },
        new() { ModelKey = "gpt-5.3-codex",     InputPer1M = 1.75m,  CachedInputPer1M = 0.175m, CacheWritePer1M = 0.00m,  OutputPer1M = 14.00m },
        new() { ModelKey = "gpt-5.2-codex",     InputPer1M = 1.75m,  CachedInputPer1M = 0.175m, CacheWritePer1M = 0.00m,  OutputPer1M = 14.00m },
        new() { ModelKey = "gpt-5.2",           InputPer1M = 1.75m,  CachedInputPer1M = 0.175m, CacheWritePer1M = 0.00m,  OutputPer1M = 14.00m },
        new() { ModelKey = "gpt-5 mini",        InputPer1M = 0.25m,  CachedInputPer1M = 0.025m, CacheWritePer1M = 0.00m,  OutputPer1M = 2.00m  },
        new() { ModelKey = "gpt-4.1",           InputPer1M = 2.00m,  CachedInputPer1M = 0.50m,  CacheWritePer1M = 0.00m,  OutputPer1M = 8.00m  },
        new() { ModelKey = "gemini-3.5-flash",  InputPer1M = 1.50m,  CachedInputPer1M = 0.15m,  CacheWritePer1M = 0.00m,  OutputPer1M = 9.00m  },
        new() { ModelKey = "gemini-3.1-pro",    InputPer1M = 2.00m,  CachedInputPer1M = 0.20m,  CacheWritePer1M = 0.00m,  OutputPer1M = 12.00m },
        new() { ModelKey = "gemini-3-flash",    InputPer1M = 0.50m,  CachedInputPer1M = 0.05m,  CacheWritePer1M = 0.00m,  OutputPer1M = 3.00m  },
        new() { ModelKey = "gemini-2.5-pro",    InputPer1M = 1.25m,  CachedInputPer1M = 0.125m, CacheWritePer1M = 0.00m,  OutputPer1M = 10.00m },
        new() { ModelKey = "mai-code-1-flash",  InputPer1M = 0.75m,  CachedInputPer1M = 0.075m, CacheWritePer1M = 0.00m,  OutputPer1M = 4.50m  },
        new() { ModelKey = "raptor",            InputPer1M = 0.25m,  CachedInputPer1M = 0.025m, CacheWritePer1M = 0.00m,  OutputPer1M = 2.00m  },
    ];

    private static readonly ModelPricing _default = new()
    {
        ModelKey = "default",
        InputPer1M = 3.00m, CachedInputPer1M = 0.30m, CacheWritePer1M = 3.75m, OutputPer1M = 15.00m
    };

    // Normalise model name for comparison: lowercase + spaces→hyphens.
    // This ensures e.g. "gpt-5.4-mini" (log) matches "gpt-5.4 mini" (table).
    private static string Normalize(string s)
        => s.ToLowerInvariant().Replace(' ', '-');

    public ModelPricing GetPricing(string modelName)
    {
        var normalizedModel = Normalize(modelName);
        foreach (var p in _table)
            if (normalizedModel.Contains(Normalize(p.ModelKey), StringComparison.Ordinal))
                return p;
        return _default;
    }

    public void CalculateCosts(CopilotSession session)
    {
        var p = GetPricing(session.Model);
        // Prices in the table are USD per million tokens — compute USD cost directly.
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
