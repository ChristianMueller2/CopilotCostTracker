// Converters/FolderIndexColorConverter.cs
using System.Globalization;

namespace CopilotCostTracker.Converters;

public class FolderIndexColorConverter : IValueConverter
{
    private static readonly Color[] _colors =
    [
        Color.FromArgb("#3B6D11"),
        Color.FromArgb("#185FA5"),
        Color.FromArgb("#BA7517"),
    ];

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not string path) return _colors[0];
        var idx = Math.Abs(path.GetHashCode()) % _colors.Length;
        return _colors[idx];
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
