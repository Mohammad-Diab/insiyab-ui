using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace Insiyab.Avalonia.Controls;

/// <summary>Joins several <see cref="BoxShadows"/> into one, so a surface can wear a token shadow and the bevel together, as CSS lists them.</summary>
internal sealed class PanelShadows : IMultiValueConverter
{
    /// <summary>The shared instance, for <c>{x:Static}</c>.</summary>
    public static readonly PanelShadows Join = new();

    /// <inheritdoc />
    public object? Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
    {
        var count = 0;
        foreach (var value in values)
            if (value is BoxShadows shadows)
                count += shadows.Count;
        if (count == 0)
            return default(BoxShadows);

        var all = new BoxShadow[count];
        var i = 0;
        foreach (var value in values)
            if (value is BoxShadows shadows)
                foreach (var shadow in shadows)
                    all[i++] = shadow;
        return count == 1 ? new BoxShadows(all[0]) : new BoxShadows(all[0], all[1..]);
    }
}
