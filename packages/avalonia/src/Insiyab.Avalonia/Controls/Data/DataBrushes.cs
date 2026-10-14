using System.Globalization;
using Avalonia;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace Insiyab.Avalonia.Controls;

/// <summary>A top-to-bottom ramp of one colour between two alphas, as CSS writes <c>linear-gradient(to bottom, rgba(c, a), rgba(c, b))</c>; the parameter is "a b".</summary>
public sealed class DataBrushes : IMultiValueConverter
{
    /// <summary>The shared instance, for <c>{x:Static}</c>.</summary>
    public static readonly DataBrushes Vertical = new();

    /// <inheritdoc />
    public object? Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
    {
        if (values.Count == 0 || values[0] is not Color color) return null;
        var parts = (parameter as string ?? "1 1").Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var top = Alpha(parts.Length > 0 ? parts[0] : null);
        var bottom = parts.Length > 1 ? Alpha(parts[1]) : top;
        return new LinearGradientBrush
        {
            StartPoint = new RelativePoint(0, 0, RelativeUnit.Relative),
            EndPoint = new RelativePoint(0, 1, RelativeUnit.Relative),
            GradientStops =
            {
                new GradientStop(Color.FromArgb((byte)Math.Round(top * 255), color.R, color.G, color.B), 0),
                new GradientStop(Color.FromArgb((byte)Math.Round(bottom * 255), color.R, color.G, color.B), 1),
            },
        };
    }

    // An alpha that cannot be read is taken as fully opaque rather than thrown from a binding.
    private static double Alpha(string? s) =>
        double.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out var a) && double.IsFinite(a) ? Math.Clamp(a, 0, 1) : 1;
}
