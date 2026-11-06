using Avalonia;
using Avalonia.Controls;

namespace Insiyab.Avalonia.Controls;

/// <summary>The items panel of a <c>ListBox.nav</c>: a column whose last items with the <c>foot</c> class stand apart at its foot, never above where the column put them; it stays one list, so the keys and the marker flight still work.</summary>
internal sealed class NavStack : StackPanel
{
    protected override Size ArrangeOverride(Size finalSize)
    {
        var size = base.ArrangeOverride(finalSize);
        var bottom = finalSize.Height;
        for (var i = Children.Count - 1; i >= 0; i--)
        {
            var item = Children[i];
            if (!item.IsVisible) continue;
            if (!item.Classes.Contains("foot")) break;
            var top = bottom - item.DesiredSize.Height;
            if (top <= item.Bounds.Y) break;
            item.Arrange(new Rect(0, top, finalSize.Width, item.DesiredSize.Height));
            bottom = top - Spacing;
        }
        return size;
    }
}

/// <summary>What a <c>ListBox.nav</c> hands its tabs.</summary>
internal static class NavLook
{
    /// <summary>Set by <c>ListBox.nav.compact</c>: its tabs show their icons only, larger, with their names as tips.</summary>
    public static readonly AttachedProperty<bool> CompactProperty =
        AvaloniaProperty.RegisterAttached<StyledElement, bool>("Compact", typeof(NavLook), inherits: true);

    public static bool GetCompact(StyledElement element) => element.GetValue(CompactProperty);
    public static void SetCompact(StyledElement element, bool value) => element.SetValue(CompactProperty, value);
}
