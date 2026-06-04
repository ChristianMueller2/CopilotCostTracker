// Converters/TokensToShortStringConverter.cs
using System.Globalization;

namespace CopilotCostTracker.Converters;

public class TokensToShortStringConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not long n) return "0";
        return n >= 1_000_000
            ? $"{n / 1_000_000.0:F1}M"
            : n >= 1_000
                ? $"{n / 1_000}K"
                : n.ToString();
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
