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
            canvas.DrawArc(cx - arcR, cy - arcR, arcR * 2, arcR * 2, -90f, 269.9f, true, false);
            return;
        }

        float startAngle = -90f;
        for (int i = 0; i < pcts.Length; i++)
        {
            if (pcts[i] <= 0) continue;
            float sweep    = (pcts[i] / total) * 360f;
            float endAngle = startAngle + sweep - Gap;
            canvas.StrokeColor = _sliceColors[i];
            canvas.DrawArc(cx - arcR, cy - arcR, arcR * 2, arcR * 2, startAngle, endAngle, true, false);
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
