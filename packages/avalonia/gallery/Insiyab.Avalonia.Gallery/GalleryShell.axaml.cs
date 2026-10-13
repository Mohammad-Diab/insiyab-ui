using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Automation;
using Avalonia.Media;
using Avalonia.Styling;
using Insiyab.Avalonia.Controls;

namespace Insiyab.Avalonia.Gallery;

/// <summary>The gallery on the package's app shell, as the web docs: brand and grouped links in the sidebar, the page's name in the top bar.</summary>
public partial class GalleryShell : UserControl
{
    // The docs site's brand swatches, with the names its tooltips give them.
    private static readonly (string Hex, string Name)[] Brands =
    [
        ("#0F766E", "أخضر مزرق"), ("#1D4ED8", "أزرق"), ("#9B2C5E", "عنابي"),
        ("#4C1D95", "بنفسجي"), ("#B45309", "كهرماني"), ("#C9A063", "ذهبي فاتح"),
    ];

    public GalleryShell()
    {
        InitializeComponent();
        VersionText.Text = typeof(InsiyabTheme).Assembly.GetName().Version?.ToString(3);
        DirectionSeg.SelectionChanged += (_, _) => DirectionFlip.Run(this, ApplyDirection);
        ResetButton.Click += (_, _) => Reset();
        EffectsButton.Click += (_, _) =>
        {
            if (TopLevel.GetTopLevel(this) is { } top) Motion.SetStill(top, !Motion.GetStill(top));
        };
        ActualThemeVariantChanged += (_, _) => PaintDot();
        foreach (var (hex, name) in Brands)
        {
            var swatch = new Button { Theme = (ControlTheme)Resources["SiteSwatch"]!, Background = new SolidColorBrush(Color.Parse(hex)), Tag = Color.Parse(hex) };
            ToolTip.SetTip(swatch, name);
            AutomationProperties.SetName(swatch, name);
            swatch.Click += (_, _) =>
            {
                InsiyabTheme.SetPrimary((Color)swatch.Tag!);
                SyncBrand();
            };
            Swatches.Children.Add(swatch);
        }
        SyncBrand();

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
        PaintDot();
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
        var dir = DirectionSeg.SelectedIndex == 1 ? FlowDirection.LeftToRight : FlowDirection.RightToLeft;
        if (TopLevel.GetTopLevel(this) is { } top) top.FlowDirection = dir;
        else FlowDirection = dir;
    }

    // The web's «إعادة الضبط»: the default brand, right to left, the system's theme and effects on.
    private void Reset()
    {
        InsiyabTheme.SetPrimary(InsiyabTheme.DefaultPrimary);
        SyncBrand();
        DirectionSeg.SelectedIndex = 0;
        if (TopLevel.GetTopLevel(this) is { } top) Motion.SetStill(top, false);
        ThemeToggle.Apply("system");
    }

    private void SyncBrand()
    {
        foreach (var swatch in Swatches.Children.OfType<Button>())
            swatch.Classes.Set("on", swatch.Tag is Color c && c == InsiyabTheme.Primary);
        PaintDot();
    }

    // The dot's soft shadow is the brand at 45%, which no token carries, so it is drawn here when the brand or the theme moves.
    private void PaintDot()
    {
        var c = this.TryFindResource("InsPrimaryRgbColor", ActualThemeVariant, out var v) && v is Color k ? k : InsiyabTheme.Primary;
        BrandDot.BoxShadow = new BoxShadows(new BoxShadow { OffsetY = 2, Blur = 6, Color = Color.FromArgb(115, c.R, c.G, c.B) });
    }
}
