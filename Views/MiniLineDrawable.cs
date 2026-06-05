// Views/MiniLineDrawable.cs
using CopilotCostTracker.ViewModels;
using Microsoft.Maui.Graphics;

namespace CopilotCostTracker.Views;

/// <summary>Compact chart with Y-axis labels for the Overview card.</summary>
public class MiniLineDrawable : IDrawable
{
    public IReadOnlyList<DailyChartPoint> Points { get; set; } = [];

    private static readonly Color TokenColor = Color.FromArgb("#5AC87A");
    private static readonly Color CostColor  = Color.FromArgb("#FF6B8A");
    private static readonly Color GridColor  = Color.FromArgb("#3A3A3C");
    private static readonly Color AxisColor  = Color.FromArgb("#4A4A4E");
    private static readonly Color LabelColor = Color.FromArgb("#636366");
    private static readonly Color FillToken  = Color.FromArgb("#205AC87A");
    private static readonly Color FillCost   = Color.FromArgb("#20FF6B8A");

    private const float PadLeft   = 50f;   // room for token Y labels
    private const float PadRight  = 50f;   // room for cost Y labels
    private const float PadTop    = 26f;   // room for legend
    private const float PadBottom = 22f;   // room for date labels

    public void Draw(ICanvas canvas, RectF dirtyRect)
    {
        float left   = dirtyRect.Left   + PadLeft;
        float right  = dirtyRect.Right  - PadRight;
        float top    = dirtyRect.Top    + PadTop;
        float bottom = dirtyRect.Bottom - PadBottom;
        float w      = right - left;
        float h      = bottom - top;

        // Grid lines (top, mid, bottom)
        canvas.StrokeColor = GridColor;
        canvas.StrokeSize  = 0.5f;
        for (int i = 0; i <= 2; i++)
        {
            float y = bottom - h / 2f * i;
            canvas.DrawLine(left, y, right, y);
        }

        // Axis lines
        canvas.StrokeColor = AxisColor;
        canvas.StrokeSize  = 1f;
        canvas.DrawLine(left,  top,    left,  bottom);
        canvas.DrawLine(right, top,    right, bottom);
        canvas.DrawLine(left,  bottom, right, bottom);

        if (Points.Count == 0)
        {
            canvas.FontColor = LabelColor;
            canvas.FontSize  = 11f;
            canvas.DrawString("No data yet",
                dirtyRect.Center.X - 50, dirtyRect.Center.Y - 8, 100, 16,
                HorizontalAlignment.Center, VerticalAlignment.Center);
            return;
        }

        // Need at least 2 points to draw a line — synthesise a zero-entry one day before
        var displayPoints = Points.Count == 1
            ? (IReadOnlyList<DailyChartPoint>)new[]
              {
                  new DailyChartPoint(Points[0].Date.AddDays(-1), 0, 0m),
                  Points[0]
              }
            : Points;

        int     n         = displayPoints.Count;
        long    maxTokens = displayPoints.Max(p => p.CumulativeTokens);
        decimal maxCost   = displayPoints.Max(p => p.CumulativeCostUsd);

        float ToX(int i)      => left + (float)i / (n - 1) * w;
        float ToYT(long v)    => maxTokens > 0
            ? bottom - (float)((double)v / maxTokens) * h : bottom;
        float ToYC(decimal v) => maxCost > 0
            ? bottom - (float)(v / maxCost) * h : bottom;

        // ── Left Y-axis labels — Tokens (green, 3 ticks) ─────────────────────
        canvas.FontSize  = 9f;
        canvas.FontColor = TokenColor;
        for (int i = 0; i <= 2; i++)
        {
            long  val = maxTokens * i / 2L;
            float y   = bottom - h / 2f * i;
            string lbl = val >= 1_000_000 ? $"{val / 1_000_000.0:F1}M"
                       : val >= 1_000     ? $"{val / 1_000.0:F0}K"
                                          : val.ToString();
            canvas.DrawString(lbl,
                dirtyRect.Left, y - 8f, PadLeft - 5f, 16f,
                HorizontalAlignment.Right, VerticalAlignment.Center);
        }

        // ── Right Y-axis labels — Cost (red, 3 ticks) ───────────────────────
        canvas.FontColor = CostColor;
        for (int i = 0; i <= 2; i++)
        {
            decimal val = maxCost * i / 2m;
            float   y   = bottom - h / 2f * i;
            canvas.DrawString($"${val:F2}",
                right + 4f, y - 8f, PadRight - 4f, 16f,
                HorizontalAlignment.Left, VerticalAlignment.Center);
        }

        // ── X-axis date labels (first, mid, last) ────────────────────────────
        canvas.FontColor = LabelColor;
        canvas.FontSize  = 9f;
        int[] xIndices = n >= 3
            ? [0, n / 2, n - 1]
            : [0, n - 1];
        foreach (int idx in xIndices)
        {
            float x = ToX(idx);
            canvas.DrawString(displayPoints[idx].Date.ToString("MM/dd"),
                x - 20f, bottom + 4f, 40f, 14f,
                HorizontalAlignment.Center, VerticalAlignment.Top);
        }

        // ── Filled areas ──────────────────────────────────────────────────────
        var tokenFill = new PathF();
        tokenFill.MoveTo(ToX(0), bottom);
        for (int i = 0; i < n; i++)
            tokenFill.LineTo(ToX(i), ToYT(displayPoints[i].CumulativeTokens));
        tokenFill.LineTo(ToX(n - 1), bottom);
        tokenFill.Close();
        canvas.FillColor = FillToken;
        canvas.FillPath(tokenFill);

        var costFill = new PathF();
        costFill.MoveTo(ToX(0), bottom);
        for (int i = 0; i < n; i++)
            costFill.LineTo(ToX(i), ToYC(displayPoints[i].CumulativeCostUsd));
        costFill.LineTo(ToX(n - 1), bottom);
        costFill.Close();
        canvas.FillColor = FillCost;
        canvas.FillPath(costFill);

        // ── Lines ─────────────────────────────────────────────────────────────
        canvas.StrokeLineCap  = LineCap.Round;
        canvas.StrokeLineJoin = LineJoin.Round;

        canvas.StrokeColor = TokenColor;
        canvas.StrokeSize  = 2f;
        var tLine = new PathF();
        tLine.MoveTo(ToX(0), ToYT(displayPoints[0].CumulativeTokens));
        for (int i = 1; i < n; i++)
            tLine.LineTo(ToX(i), ToYT(displayPoints[i].CumulativeTokens));
        canvas.DrawPath(tLine);

        canvas.StrokeColor = CostColor;
        canvas.StrokeSize  = 2f;
        var cLine = new PathF();
        cLine.MoveTo(ToX(0), ToYC(displayPoints[0].CumulativeCostUsd));
        for (int i = 1; i < n; i++)
            cLine.LineTo(ToX(i), ToYC(displayPoints[i].CumulativeCostUsd));
        canvas.DrawPath(cLine);

        // ── Endpoint dots ─────────────────────────────────────────────────────
        canvas.FillColor = TokenColor;
        canvas.FillCircle(ToX(n - 1), ToYT(displayPoints[n - 1].CumulativeTokens), 4f);
        canvas.FillColor = CostColor;
        canvas.FillCircle(ToX(n - 1), ToYC(displayPoints[n - 1].CumulativeCostUsd), 4f);

        // ── Legend (top-left inside chart area) ───────────────────────────────
        canvas.FontSize = 9f;
        canvas.FillColor = TokenColor;
        canvas.FillRectangle(left, dirtyRect.Top + 8f, 12f, 3f);
        canvas.FontColor = TokenColor;
        canvas.DrawString("Tokens", left + 16f, dirtyRect.Top + 2f, 55f, 14f,
            HorizontalAlignment.Left, VerticalAlignment.Top);

        canvas.FillColor = CostColor;
        canvas.FillRectangle(left + 72f, dirtyRect.Top + 8f, 12f, 3f);
        canvas.FontColor = CostColor;
        canvas.DrawString("Cost", left + 88f, dirtyRect.Top + 2f, 40f, 14f,
            HorizontalAlignment.Left, VerticalAlignment.Top);
    }
}