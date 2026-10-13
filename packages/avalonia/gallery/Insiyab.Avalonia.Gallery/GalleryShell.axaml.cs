using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Styling;
using Insiyab.Avalonia.Controls;

namespace Insiyab.Avalonia.Gallery;

/// <summary>The gallery on the package's app shell, as the web docs: brand and grouped links in the sidebar, the page's name in the top bar.</summary>
public partial class GalleryShell : UserControl
{
    public GalleryShell()
    {
        InitializeComponent();
        DarkSwitch.IsCheckedChanged += (_, _) =>
            Application.Current!.RequestedThemeVariant = DarkSwitch.IsChecked == true ? ThemeVariant.Dark : ThemeVariant.Light;
        LtrSwitch.IsCheckedChanged += (_, _) => ApplyDirection();

        string? group = null;
        foreach (var page in GalleryPages.All)
        {
            if (page.Group != group)
            {
                group = page.Group;
                Nav.Items.Add(new ListBoxItem { Classes = { "shell-group" }, Content = group });
            }
            Nav.Items.Add(Link(page));
        }
        Nav.SelectionChanged += (_, _) =>
        {
            if (Nav.SelectedItem is ListBoxItem { Tag: GalleryPage page }) Open(page);
        };
        Nav.SelectedItem = Nav.Items.OfType<ListBoxItem>().FirstOrDefault(i => i.Tag is GalleryPage);
    }

    /// <summary>Opens the page with this slug, as a click on its link does. False when the gallery has no such page.</summary>
    public bool Select(string slug)
    {
        var link = Nav.Items.OfType<ListBoxItem>().FirstOrDefault(i => i.Tag is GalleryPage p && p.Slug == slug);
        if (link is null) return false;
        Nav.SelectedItem = link;
        return true;
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        ApplyDirection();
        if (Nav.SelectedItem is ListBoxItem { Tag: GalleryPage page }) ShowTitle(page);
    }

    private ListBoxItem Link(GalleryPage page)
    {
        var row = new DockPanel();
        if (Application.Current!.TryFindResource(page.Icon, out var data) && data is Geometry geometry)
            row.Children.Add(new Icon { Data = geometry });
        if (page.Fresh)
        {
            var badge = new Border { Classes = { "shell-link-badge" }, Child = new TextBlock { Text = "جديد" } };
            DockPanel.SetDock(badge, Dock.Right);
            row.Children.Add(badge);
        }
        row.Children.Add(new TextBlock { Text = page.Title, VerticalAlignment = VerticalAlignment.Center });
        return new ListBoxItem { Content = row, Tag = page };
    }

    private void Open(GalleryPage page)
    {
        Host.Content = page.Create();
        Scroller.Offset = default;
        ShowTitle(page);
        if (Shell.IsDrawer(ShellView) && ShellView.IsPaneOpen) Shell.Sidebar(ShellView, "closed");
    }

    private void ShowTitle(GalleryPage page)
    {
        PageTitle.Text = page.Title;
        if (TopLevel.GetTopLevel(this) is Window window) window.Title = $"{page.Title} · انسياب";
    }

    // The TopLevel takes the direction, so dialogs and toasts in its adorner layer follow it, in a window or a single view.
    private void ApplyDirection()
    {
        var dir = LtrSwitch.IsChecked == true ? FlowDirection.LeftToRight : FlowDirection.RightToLeft;
        if (TopLevel.GetTopLevel(this) is { } top) top.FlowDirection = dir;
        else FlowDirection = dir;
    }
}
