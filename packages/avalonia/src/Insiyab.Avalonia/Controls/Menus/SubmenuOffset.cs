using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace Insiyab.Avalonia.Controls;

// A submenu popup's horizontal offset from its item's direction: the card 4px past the item with the room for its shadow taken back out, on whichever side the submenu opens.
internal sealed class SubmenuOffset : IValueConverter
{
    public static readonly SubmenuOffset Instance = new();

    // The room left of the card (40) less the 4px gap; Avalonia mirrors the placement on a right-to-left page but not the offset.
    private const double Pull = 36;

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is FlowDirection.RightToLeft ? Pull : -Pull;

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => throw new NotSupportedException();
}
