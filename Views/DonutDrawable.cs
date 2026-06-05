// Views/DonutDrawable.cs
using Microsoft.Maui.Graphics;

namespace CopilotCostTracker.Views;

public class DonutDrawable : IDrawable
{
    public float PctInput      { get; set; }
    public float PctOutput     { get; set; }
    public float PctCacheRead  { get; set; }
    public float PctCacheWrite { get; set; }
    public string CenterText      { get; set; } = string.Empty;
    public Color  BackgroundColor { get; set; } = Color.FromArgb("#2C2C2E"); // kept for compat

    private static readonly Color[] _sliceColors =
    [
        Color.FromArgb("#4DA3FF"), // Input      — accent blue
        Color.FromArgb("#5AC87A"), // Output     — accent green
        Color.FromArgb("#FFB340"), // Cache read — amber
        Color.FromArgb("#FF6B8A"), // Cache write — rose
    ];

    private const float Thickness = 28f;
    private const float Gap       = 2f;  // degrees between slices

    // DrawArc with clockwise=true is unreliable on MAUI/Windows for large arcs.
    // We draw arcs as PathF polylines using sin/cos instead.
    private static void DrawRingArc(ICanvas canvas, float cx, float cy, float r,
                                    float startDeg, float endDeg)
    {
        int steps = Math.Max(4, (int)MathF.Ceiling(MathF.Abs(endDeg - startDeg)));
        var path  = new PathF();
        for (int s = 0; s <= steps; s++)
        {
            float deg = startDeg + (endDeg - startDeg) * s / steps;
            float rad = deg * MathF.PI / 180f;
            float x   = cx + r * MathF.Cos(rad);
            float y   = cy + r * MathF.Sin(rad);
            if (s == 0) path.MoveTo(x, y);
            else        path.LineTo(x, y);
        }
        canvas.DrawPath(path);
    }

    public void Draw(ICanvas canvas, RectF dirtyRect)
    {
        float cx   = dirtyRect.Center.X;
        float cy   = dirtyRect.Center.Y;
        float r    = Math.Min(dirtyRect.Width, dirtyRect.Height) / 2f - 6f;
        float arcR = r - Thickness / 2f; // radius to centre-line of the ring

        float[] pcts  = [PctInput, PctOutput, PctCacheRead, PctCacheWrite];
        float   total = pcts.Sum();

        canvas.StrokeLineCap = LineCap.Butt;
        canvas.StrokeSize    = Thickness;

        if (total <= 0)
        {
            // Gray placeholder ring
            canvas.StrokeColor = Color.FromArgb("#3A3A3C");
            DrawRingArc(canvas, cx, cy, arcR, -90f, 269.9f);
            return;
        }

        float startAngle = -90f;
        for (int i = 0; i < pcts.Length; i++)
        {
            if (pcts[i] <= 0) continue;
            float sweep    = (pcts[i] / total) * 360f;
            float endAngle = startAngle + sweep - Gap;
            canvas.StrokeColor = _sliceColors[i];
            DrawRingArc(canvas, cx, cy, arcR, startAngle, endAngle);
            startAngle += sweep;
        }

        // Center text
        if (!string.IsNullOrEmpty(CenterText))
        {
            canvas.FontColor = Color.FromArgb("#F2F2F7");
            canvas.FontSize  = 13f;
            canvas.DrawString(CenterText, cx - 40, cy - 10, 80, 20,
                HorizontalAlignment.Center, VerticalAlignment.Center);
        }
    }
}
