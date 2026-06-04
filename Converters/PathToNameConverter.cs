// Converters/PathToNameConverter.cs
using System.Globalization;

namespace CopilotCostTracker.Converters;

public class PathToNameConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not string path || string.IsNullOrEmpty(path)) return string.Empty;
        return System.IO.Path.GetFileName(path.TrimEnd(System.IO.Path.DirectorySeparatorChar));
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
