using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;

namespace Insiyab.Avalonia.Controls;

/// <summary>A description value given as text: selectable, with its keyboard ring drawn inside its own box so neither its row nor a dialog's scroller cuts it.</summary>
internal sealed class DescriptionText : SelectableTextBlock
{
    protected override Type StyleKeyOverride => typeof(SelectableTextBlock);

    protected override void OnGotFocus(FocusChangedEventArgs e)
    {
        base.OnGotFocus(e);
        InvalidateVisual();
    }

    protected override void OnLostFocus(FocusChangedEventArgs e)
    {
        base.OnLostFocus(e);
        InvalidateVisual();
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == WindowActivity.IsInactiveProperty) InvalidateVisual();
    }

    protected override void RenderTextLayout(DrawingContext context, Point origin)
    {
        base.RenderTextLayout(context, origin);
        if (!IsFocused || !Classes.Contains(":focus-visible") || GetValue(WindowActivity.IsInactiveProperty)) return;
        if (!this.TryFindResource("InsToneBrandSoft", ActualThemeVariant, out var found) || found is not IBrush brush) return;
        context.DrawRectangle(null, new Pen(brush, 2), new RoundedRect(new Rect(Bounds.Size).Deflate(1), 6));
    }
}
