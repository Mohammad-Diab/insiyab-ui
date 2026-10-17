using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Input.Platform;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Markup.Xaml.Styling;
using Avalonia.Media;
using Avalonia.Styling;
using Insiyab.Avalonia.Gallery;
using Avalonia.VisualTree;
using Insiyab.Avalonia.Gallery.Playground;
using static Insiyab.Avalonia.Tests.Ui;

namespace Insiyab.Avalonia.Tests;

// The gallery's code listings: a long one stops at the web's 26rem and scrolls inside itself, its copy button fixed in the corner.
public class ListingTests
{
    private static string Lines(int n) => string.Join("\n", Enumerable.Range(1, n).Select(i => $"<TextBlock Text=\"سطر {i}\" Margin=\"0,{i},0,0\" HorizontalAlignment=\"Stretch\" VerticalAlignment=\"Center\" />"));

    private static void LoadIcons()
    {
        var app = Application.Current!;
        if (app.TryGetResource("IconCheck", null, out _)) return;
        app.Resources.MergedDictionaries.Add((ResourceDictionary)AvaloniaXamlLoader.Load(new Uri("avares://Insiyab.Avalonia.Gallery/Icons.axaml")));
    }

    private static Rect BoundsIn(Visual v, Visual root)
    {
        var a = v.TranslatePoint(new Point(0, 0), root)!.Value;
        var b = v.TranslatePoint(new Point(v.Bounds.Width, v.Bounds.Height), root)!.Value;
        return new Rect(new Point(Math.Min(a.X, b.X), Math.Min(a.Y, b.Y)), new Point(Math.Max(a.X, b.X), Math.Max(a.Y, b.Y)));
    }

    [AvaloniaFact]
    public void A_long_listing_scrolls_inside_416px_with_the_copy_button_in_its_corner()
    {
        LoadIcons();
        var page = GalleryPages.Find("shell")!.Create();
        var w = Show(new ScrollViewer { Content = page }, FlowDirection.RightToLeft, width: 1100, height: 900);
        Settle(1000);
        var listings = page.GetVisualDescendants().OfType<PlayListing>().ToList();
        Assert.NotEmpty(listings);
        foreach (var l in listings) Assert.True(l.Bounds.Height <= PlayListing.MaxCodeHeight + 2, $"a listing {l.Bounds.Height}px tall");
        var listing = listings.First(l => l.GetVisualDescendants().OfType<ScrollViewer>().First().Extent.Height > PlayListing.MaxCodeHeight + 100);
        var scroll = listing.GetVisualDescendants().OfType<ScrollViewer>().First();
        Assert.Equal(PlayListing.MaxCodeHeight, scroll.Bounds.Height, 1);
        var copy = listing.GetVisualDescendants().OfType<Button>().First(b => b.Classes.Contains("play-copy") && !b.Classes.Contains("play-wrap"));
        var before = BoundsIn(copy, w);
        var box = BoundsIn(listing, w);
        Assert.InRange(box.Right - before.Right, 0, 12);
        Assert.InRange(before.Top - box.Top, 0, 12);
        scroll.Offset = new Vector(0, scroll.Extent.Height);
        Settle(200);
        Assert.True(scroll.Offset.Y > 0);
        Assert.Equal(before, BoundsIn(copy, w));

        Assert.NotNull(w.Clipboard);
        copy.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        Settle(300);
        Assert.Contains("done", copy.Classes);
        Assert.Equal(listing.Code, w.Clipboard!.TryGetTextAsync().GetAwaiter().GetResult());
    }

    [AvaloniaFact]
    public void A_short_listing_keeps_its_own_height()
    {
        var listing = new PlayListing { Code = Lines(3), Width = 520 };
        Show(new StackPanel { Margin = new Thickness(20), Children = { listing } }, FlowDirection.RightToLeft);
        var scroll = listing.GetVisualDescendants().OfType<ScrollViewer>().First();
        Assert.True(scroll.Bounds.Height < 140);
        Assert.Equal(scroll.Extent.Height, scroll.Viewport.Height, 1);
    }

    private static readonly string Indented = string.Join("\n",
        "<StackPanel>",
        "    <TextBlock Text=\"سطر طويل جدًا\" Margin=\"0,4,0,0\" HorizontalAlignment=\"Stretch\" VerticalAlignment=\"Center\" FontWeight=\"Bold\" Foreground=\"Gray\" />",
        "</StackPanel>");

    private static PlayListing Framed(string code, out Border host)
    {
        var listing = new PlayListing { Code = code, Width = 420 };
        host = new Border { Padding = new Thickness(20), Child = listing };
        host.Styles.Add(new StyleInclude(new Uri("avares://Insiyab.Avalonia.Gallery/")) { Source = new Uri("avares://Insiyab.Avalonia.Gallery/Playground/PlayFrames.axaml") });
        return listing;
    }

    private static Button Tool(PlayListing listing, string cls) => listing.GetVisualDescendants().OfType<Button>().First(b => b.Classes.Contains(cls));

    [AvaloniaTheory]
    [MemberData(nameof(Looks), MemberType = typeof(Ui))]
    public void The_wrap_toggle_wraps_long_lines_under_their_indentation(string theme, string dir)
    {
        var listing = Framed(Indented, out var host);
        var w = Show(host, dir == "rtl" ? FlowDirection.RightToLeft : FlowDirection.LeftToRight, theme == "Dark" ? ThemeVariant.Dark : ThemeVariant.Light, width: 520);
        var scroll = listing.GetVisualDescendants().OfType<ScrollViewer>().First();
        var wrap = Tool(listing, "play-wrap");
        var copy = listing.GetVisualDescendants().OfType<Button>().First(b => b.Classes.Contains("play-copy") && !b.Classes.Contains("play-wrap"));
        Assert.Equal("التفاف", wrap.Content);
        Assert.False(listing.IsWrapped);
        Assert.True(scroll.Extent.Width > scroll.Viewport.Width + 20);
        var a = BoundsIn(wrap, w);
        var b = BoundsIn(copy, w);
        Assert.True(a.Right <= b.Left && b.Left - a.Right < 8, $"wrap {a} copy {b}");
        Assert.Equal(a.Height, b.Height, 1);
        Assert.Equal(a.Top, b.Top, 1);
        Assert.Equal(ColorOf(copy.Foreground), ColorOf(wrap.Foreground));

        wrap.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        Settle(300);
        Assert.True(listing.IsWrapped);
        Assert.Contains("on", wrap.Classes);
        Assert.Equal(scroll.Viewport.Width, scroll.Extent.Width, 1);
        var rows = listing.GetVisualDescendants().OfType<Grid>().Where(g => g.Parent is StackPanel { } p && p.Classes.Contains("play-lines")).ToList();
        Assert.Equal(3, rows.Count);
        var indent = (TextBlock)rows[1].Children[0];
        var body = (TextBlock)rows[1].Children[1];
        Assert.Equal("    ", indent.Text);
        Assert.True(body.TextLayout.TextLines.Count > 1, "the long line did not wrap");
        Assert.True(indent.Bounds.Width > 20);
        Assert.Equal(BoundsIn(indent, w).Right, BoundsIn(body, w).Left, 1);
        Assert.Equal(BoundsIn((TextBlock)rows[0].Children[1], w).Left, BoundsIn(indent, w).Left, 1);

        wrap.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        Settle(300);
        Assert.False(listing.IsWrapped);
        Assert.True(scroll.Extent.Width > scroll.Viewport.Width + 20);

        wrap.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        copy.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        Settle(300);
        Assert.Equal(Indented, w.Clipboard!.TryGetTextAsync().GetAwaiter().GetResult());
    }
    // The gallery has one component for code samples, so no page draws its own plain `pre` box.
    [AvaloniaFact]
    public void Gallery_pages_show_code_only_through_the_listing()
    {
        var pages = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(ThisFile())!, "../../gallery/Insiyab.Avalonia.Gallery/Pages"));
        var offenders = Directory.EnumerateFiles(pages, "*.axaml").Where(f => File.ReadAllText(f).Contains("Classes=\"pre\"")).Select(Path.GetFileName).ToList();
        Assert.True(offenders.Count == 0, "pages with a plain pre box: " + string.Join(", ", offenders));
    }

    private static string ThisFile([System.Runtime.CompilerServices.CallerFilePath] string file = "") => file;
}
