// Converters/TabFilterStyleConverter.cs
using System.Globalization;

namespace CopilotCostTracker.Converters;

/// <summary>Returns the active or inactive pill style for a filter tab button.</summary>
public class TabFilterStyleConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var active = value as string ?? string.Empty;
        var param  = parameter as string ?? string.Empty;
        var key    = active == param ? "PillButtonActiveStyle" : "PillButtonStyle";
        if (Application.Current?.Resources.TryGetValue(key, out var style) == true)
            return style;
        return null;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
