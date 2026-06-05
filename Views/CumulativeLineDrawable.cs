// Views/CumulativeLineDrawable.cs
using CopilotCostTracker.ViewModels;
using Microsoft.Maui.Graphics;

namespace CopilotCostTracker.Views;

public class CumulativeLineDrawable : IDrawable
{
    public IReadOnlyList<DailyChartPoint> Points { get; set; } = [];

    private static readonly Color TokenColor = Color.FromArgb("#5AC87A"); // green
    private static readonly Color CostColor  = Color.FromArgb("#FF6B8A"); // red/rose
    private static readonly Color GridColor  = Color.FromArgb("#3A3A3C");
    private static readonly Color AxisColor  = Color.FromArgb("#636366");
    private static readonly Color LabelColor = Color.FromArgb("#8E8E93");

    private const float PadLeft   = 68f;
    private const float PadRight  = 68f;
    private const float PadTop    = 24f;
    private const float PadBottom = 44f;

    public void Draw(ICanvas canvas, RectF dirtyRect)
    {
        float chartLeft   = dirtyRect.Left + PadLeft;
        float chartRight  = dirtyRect.Right - PadRight;
        float chartTop    = dirtyRect.Top + PadTop;
        float chartBottom = dirtyRect.Bottom - PadBottom;
        float chartW      = chartRight - chartLeft;
        float chartH      = chartBottom - chartTop;

        // Horizontal grid lines
        canvas.StrokeColor = GridColor;
        canvas.StrokeSize  = 0.5f;
        for (int i = 0; i <= 4; i++)
        {
            float y = chartBottom - chartH / 4f * i;
            canvas.DrawLine(chartLeft, y, chartRight, y);
        }

        // Axes
        canvas.StrokeColor = AxisColor;
        canvas.StrokeSize  = 1f;
        canvas.DrawLine(chartLeft,  chartTop,    chartLeft,  chartBottom);
        canvas.DrawLine(chartRight, chartTop,    chartRight, chartBottom);
        canvas.DrawLine(chartLeft,  chartBottom, chartRight, chartBottom);

        if (Points.Count == 0)
        {
            canvas.FontColor = LabelColor;
            canvas.FontSize  = 13f;
            canvas.DrawString("No data yet",
                dirtyRect.Center.X - 50, dirtyRect.Center.Y - 10, 100, 20,
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

        long    maxTokens = displayPoints.Max(p => p.CumulativeTokens);
        decimal maxCost   = displayPoints.Max(p => p.CumulativeCostUsd);
        int     n         = displayPoints.Count;

        // Left Y-axis labels — Tokens (green)
        canvas.FontColor = TokenColor;
        canvas.FontSize  = 10f;
        for (int i = 0; i <= 4; i++)
        {
            long  val = (long)(maxTokens * i / 4L);
            float y   = chartBottom - chartH / 4f * i;
            string lbl = val >= 1_000_000 ? $"{val / 1_000_000.0:F1}M"
                       : val >= 1_000     ? $"{val / 1_000.0:F0}K"
                                          : val.ToString();
            canvas.DrawString(lbl, dirtyRect.Left, y - 8, PadLeft - 6, 16,
                HorizontalAlignment.Right, VerticalAlignment.Center);
        }

        // Right Y-axis labels — Cost (red)
        canvas.FontColor = CostColor;
        canvas.FontSize  = 10f;
        for (int i = 0; i <= 4; i++)
        {
            decimal val = maxCost * i / 4m;
            float   y   = chartBottom - chartH / 4f * i;
            canvas.DrawString($"${val:F2}", chartRight + 4, y - 8, PadRight - 4, 16,
                HorizontalAlignment.Left, VerticalAlignment.Center);
        }

        // X-axis date labels
        canvas.FontColor = LabelColor;
        canvas.FontSize  = 10f;
        int maxLabels = Math.Max(2, (int)(chartW / 60));
        int step      = Math.Max(1, (n - 1) / maxLabels);
        for (int i = 0; i < n; i += step)
        {
            float x = chartLeft + (float)i / (n - 1) * chartW;
            canvas.DrawString(displayPoints[i].Date.ToString("MM/dd"),
                x - 25, chartBottom + 6, 50, 16,
                HorizontalAlignment.Center, VerticalAlignment.Top);
        }
        // Always label the last point
        {
            float x = chartLeft + chartW;
            canvas.DrawString(displayPoints[n - 1].Date.ToString("MM/dd"),
                x - 25, chartBottom + 6, 50, 16,
                HorizontalAlignment.Center, VerticalAlignment.Top);
        }

        // Helpers
        float ToX(int idx) => chartLeft + (float)idx / (n - 1) * chartW;
        float ToYT(long v) => maxTokens > 0
            ? chartBottom - (float)((double)v / maxTokens) * chartH
            : chartBottom;
        float ToYC(decimal v) => maxCost > 0
            ? chartBottom - (float)(v / maxCost) * chartH
            : chartBottom;

        // Tokens line (green)
        canvas.StrokeColor    = TokenColor;
        canvas.StrokeSize     = 2f;
        canvas.StrokeLineCap  = LineCap.Round;
        canvas.StrokeLineJoin = LineJoin.Round;
        var tokenPath = new PathF();
        tokenPath.MoveTo(ToX(0), ToYT(displayPoints[0].CumulativeTokens));
        for (int i = 1; i < n; i++)
            tokenPath.LineTo(ToX(i), ToYT(displayPoints[i].CumulativeTokens));
        canvas.DrawPath(tokenPath);

        // Cost line (red)
        canvas.StrokeColor = CostColor;
        canvas.StrokeSize  = 2f;
        var costPath = new PathF();
        costPath.MoveTo(ToX(0), ToYC(displayPoints[0].CumulativeCostUsd));
        for (int i = 1; i < n; i++)
            costPath.LineTo(ToX(i), ToYC(displayPoints[i].CumulativeCostUsd));
        canvas.DrawPath(costPath);

        // Endpoint dots
        canvas.FillColor = TokenColor;
        canvas.FillCircle(ToX(n - 1), ToYT(displayPoints[n - 1].CumulativeTokens), 4f);
        canvas.FillColor = CostColor;
        canvas.FillCircle(ToX(n - 1), ToYC(displayPoints[n - 1].CumulativeCostUsd), 4f);

        // Legend (top-right inside chart area)
        float lx = chartRight - 100;
        float ly = chartTop + 8;
        canvas.FillColor = TokenColor;
        canvas.FillRectangle(lx, ly + 4, 14, 3);
        canvas.FontColor = TokenColor;
        canvas.FontSize  = 10f;
        canvas.DrawString("Tokens", lx + 18, ly, 70, 14,
            HorizontalAlignment.Left, VerticalAlignment.Top);

        canvas.FillColor = CostColor;
        canvas.FillRectangle(lx, ly + 18 + 4, 14, 3);
        canvas.FontColor = CostColor;
        canvas.DrawString("Cost (USD)", lx + 18, ly + 18, 70, 14,
            HorizontalAlignment.Left, VerticalAlignment.Top);
    }
}
