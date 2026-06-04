// Converters/EventCategoryColorConverter.cs
using CopilotCostTracker.Models;
using Microsoft.Maui.Graphics;

namespace CopilotCostTracker.Converters;

public class EventCategoryColorConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, System.Globalization.CultureInfo culture)
    {
        return value switch
        {
            EventCategory.User      => Color.FromArgb("#4DA3FF"), // blue
            EventCategory.Assistant => Color.FromArgb("#5AC87A"), // green
            EventCategory.Tool      => Color.FromArgb("#FFB340"), // amber
            _                       => Color.FromArgb("#636366"), // grey (System)
        };
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, System.Globalization.CultureInfo culture)
        => throw new NotSupportedException();
}
