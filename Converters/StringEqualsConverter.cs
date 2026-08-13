// Converters/StringEqualsConverter.cs
using System.Globalization;

namespace CopilotCostTracker.Converters;

/// <summary>Returns true when the bound string equals the converter parameter (used to toggle IsVisible).</summary>
public class StringEqualsConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => (value as string) == (parameter as string);

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
