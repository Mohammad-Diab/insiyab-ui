using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace Insiyab.Avalonia.Controls;

/// <summary>Turns the brand colour into the soft brand shadow a focused field wears under its ring: CSS's <c>0 4px 16px rgba(var(--ins-primary-rgb), .10)</c>.</summary>
public sealed class InputHalo : IValueConverter
{
    /// <summary>The shared instance, for <c>{x:Static}</c>.</summary>
    public static readonly InputHalo Instance = new();

    /// <inheritdoc />
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var color = value switch
        {
            Color c => c,
            ISolidColorBrush b => b.Color,
            _ => Colors.Transparent,
        };
        return new BoxShadows(new BoxShadow { OffsetY = 4, Blur = 16, Color = Color.FromArgb(0x1A, color.R, color.G, color.B) });
    }

    /// <inheritdoc />
    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => throw new NotSupportedException();
}
