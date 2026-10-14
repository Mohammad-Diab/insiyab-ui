using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Data;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace Insiyab.Avalonia.Controls;

/// <summary>The web's <c>dir="auto"</c>: a text takes the direction of its first letter (digits are not letters), right to left for Arabic or Hebrew, left to right for any other, and the direction around it when it has none.</summary>
public sealed class TextDirection : IValueConverter
{
    /// <summary>The shared instance, for <c>{x:Static}</c>.</summary>
    public static readonly TextDirection Auto = new();

    /// <summary>The direction of <paramref name="text"/>'s first letter, or null when it has none.</summary>
    public static FlowDirection? Of(string? text)
    {
        if (string.IsNullOrEmpty(text)) return null;
        foreach (var c in text)
        {
            if (char.IsDigit(c) || c is '٫' or '٬') continue;
            if (IsRightToLeft(c)) return FlowDirection.RightToLeft;
            if (char.IsLetter(c)) return FlowDirection.LeftToRight;
        }
        return null;
    }

    // A date or time field: digits and separators read left to right, so a line with no letters is laid out left to right, held at the field's own start edge.
    internal static void FollowTyped(TextBox box, TextPresenter? presenter)
    {
        if (presenter is null) return;
        var own = box.FlowDirection;
        if ((Of(box.Text) ?? FlowDirection.LeftToRight) == own)
        {
            presenter.ClearValue(Visual.FlowDirectionProperty);
            presenter.ClearValue(TextPresenter.TextAlignmentProperty);
            return;
        }
        presenter.FlowDirection = own == FlowDirection.LeftToRight ? FlowDirection.RightToLeft : FlowDirection.LeftToRight;
        presenter.TextAlignment = box.TextAlignment switch
        {
            TextAlignment.Start or TextAlignment.Justify or TextAlignment.DetectFromContent => TextAlignment.End,
            TextAlignment.End => TextAlignment.Start,
            var a => a,
        };
    }

    /// <inheritdoc />
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var text = value switch
        {
            null => null,
            string s => s,
            Exception e => e.Message,
            _ => value.ToString(),
        };
        return Of(text) is { } direction ? direction : BindingOperations.DoNothing;
    }

    /// <inheritdoc />
    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => BindingOperations.DoNothing;

    private static bool IsRightToLeft(char c) => (int)c is (>= 0x0590 and <= 0x08FF) or (>= 0xFB1D and <= 0xFDFF) or (>= 0xFE70 and <= 0xFEFC);
}
