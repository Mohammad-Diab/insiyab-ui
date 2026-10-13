using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Layout;
using Avalonia.LogicalTree;
using Insiyab.Avalonia.Controls;

namespace Insiyab.Avalonia.Gallery.Pages;

// The playground stages draw their menu open, as the web's do: a still copy of the flyout's body under its trigger.
internal static class MenusGallery
{
    public static void DrawOpen(Control page)
    {
        foreach (var stage in page.GetLogicalDescendants().OfType<Border>().Where(b => b.Classes.Contains("stage") && b.Classes.Contains("open")).ToList())
        {
            if (stage.Child is not Control preview) continue;
            var trigger = preview as Button ?? preview.GetLogicalDescendants().OfType<Button>().FirstOrDefault(b => b.Flyout is not null);
            if (trigger?.Flyout is not PopupFlyoutBase flyout) continue;
            var body = Copy(flyout);
            if (body is null) continue;
            var end = flyout is PopMenu { IsEnd: true } or PopCard { IsEnd: true };
            stage.Child = null;
            var stack = new StackPanel { Spacing = 6, HorizontalAlignment = HorizontalAlignment.Center };
            preview.HorizontalAlignment = end ? HorizontalAlignment.Right : HorizontalAlignment.Left;
            body.HorizontalAlignment = preview.HorizontalAlignment;
            body.IsHitTestVisible = false;
            stack.Children.Add(preview);
            stack.Children.Add(body);
            stage.Child = stack;
            if (trigger != preview)
                stack.LayoutUpdated += (_, _) =>
                {
                    var at = trigger.TranslatePoint(default, stack);
                    if (at is { } p && Math.Abs(body.Margin.Left - p.X) > 0.5) body.Margin = new Thickness(p.X, 0, 0, 0);
                };
        }
    }

    private static Control? Copy(PopupFlyoutBase flyout)
    {
        if (flyout is MenuFlyout menu)
        {
            var presenter = new MenuFlyoutPresenter();
            foreach (var item in menu.Items)
                if (Copy(item) is { } c) presenter.Items.Add(c);
            return presenter;
        }
        if (flyout is Flyout { Content: StackPanel content } card)
        {
            var copy = new StackPanel();
            foreach (var t in content.Children.OfType<TextBlock>())
            {
                var tb = new TextBlock { Text = t.Text };
                tb.Classes.AddRange(t.Classes.Where(c => !c.StartsWith(":")));
                copy.Children.Add(tb);
            }
            var presenter = new FlyoutPresenter { Content = copy };
            presenter.Classes.AddRange(card.FlyoutPresenterClasses.Where(c => c != "pop" && !c.StartsWith(":")));
            return presenter;
        }
        return null;
    }

    private static Control? Copy(object? item)
    {
        if (item is Separator) return new Separator();
        if (item is not MenuItem m) return null;
        var copy = new MenuItem
        {
            Header = m.Header,
            InputGesture = m.InputGesture,
            IsEnabled = m.IsEnabled,
            ToggleType = m.ToggleType,
            IsChecked = m.IsChecked,
            GroupName = m.GroupName is null ? null : m.GroupName + "-still",
        };
        copy.Classes.AddRange(m.Classes.Where(c => !c.StartsWith(":")));
        if (m.Icon is Icon icon) copy.Icon = new Icon { Data = icon.Data };
        if (m.IsChecked) copy.AttachedToVisualTree += (_, _) => copy.IsChecked = true;
        return copy;
    }
}
