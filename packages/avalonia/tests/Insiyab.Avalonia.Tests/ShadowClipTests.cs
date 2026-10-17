using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Headless.XUnit;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.VisualTree;
using Insiyab.Avalonia.Controls;

using static Insiyab.Avalonia.Tests.Ui;

namespace Insiyab.Avalonia.Tests;

// Avalonia 12 clips a templated control to its bounds, which cuts a shadow or glow drawn past its edge.
public class ShadowClipTests
{
    private static Border? OuterShadow(Control control) =>
        control.GetSelfAndVisualDescendants().OfType<Border>().FirstOrDefault(b => Outer(b.BoxShadow));

    private static bool Outer(BoxShadows shadows)
    {
        foreach (var s in shadows)
            if (!s.IsInset && (s.Blur > 0 || s.Spread > 0)) return true;
        return false;
    }

    private static void AssertFree(Border face, Visual stop)
    {
        for (Visual? v = face.GetVisualParent(); v is not null && v != stop; v = v.GetVisualParent())
            Assert.False(v.ClipToBounds, $"{v.GetType().Name} {(v as Control)?.Name} clips the shadow of {face.Name}");
    }

    [AvaloniaTheory]
    [InlineData("Light")]
    [InlineData("Dark")]
    public void Island_shadows_fall_past_the_island(string theme)
    {
        var menu = new Button { Classes = { "island", "icon" }, Content = "≡" };
        var custom = new Button { Classes = { "island" }, Content = "المظهر" };
        var title = new Border { Classes = { "island", "title" }, Child = new TextBlock { Text = "العنوان" } };
        var version = new Border { Classes = { "island" }, Child = new TextBlock { Text = "0.1.0" } };
        var dayNight = new DayNight();
        var bar = new Border
        {
            Classes = { "topbar", "scrolled" },
            VerticalAlignment = VerticalAlignment.Top,
            Child = new StackPanel { Classes = { "zone", "start" }, Children = { menu, version, title, dayNight, custom } },
        };
        Show(new Panel { Children = { bar } }, FlowDirection.RightToLeft, theme == "Dark" ? ThemeVariant.Dark : ThemeVariant.Light, 900, 300);
        foreach (var island in new Control[] { menu, custom, title, version, dayNight })
        {
            var face = OuterShadow(island);
            Assert.NotNull(face);
            AssertFree(face!, bar);
        }
    }

    // A page in the shell's host casts its cards' shadows into the content's padding: neither the host nor the page clips them.
    [AvaloniaFact]
    public void A_shell_page_does_not_clip_its_cards()
    {
        var card = new Border { Classes = { "card" }, Height = 80, Child = new TextBlock { Text = "بطاقة" } };
        var page = new UserControl { Content = new StackPanel { Children = { card } } };
        var host = new TransitioningContentControl { Classes = { "shell-content" }, Content = page };
        var content = new Border { Classes = { "shell-content" }, Child = host };
        Show(content, FlowDirection.RightToLeft, null, 700, 400);
        Settle();
        Assert.Same(host, page.GetVisualAncestors().OfType<TransitioningContentControl>().First());
        for (Visual? v = card; v is not null && v != content; v = v.GetVisualParent())
            Assert.False(v.ClipToBounds, $"{v.GetType().Name} clips the card's shadow");
    }

    // Pagination and an avatar group hold raised faces and rings at their own edges.
    [AvaloniaFact]
    public void Containers_of_raised_faces_do_not_clip()
    {
        var pages = new Pagination { PageCount = 5, CurrentPage = 1 };
        var crumbs = new Breadcrumb { ItemsSource = new[] { "الرئيسية", "الإعدادات", "الفواتير" } };
        var segmented = new Segmented { Items = { new SegmentedItem { Content = "أ" }, new SegmentedItem { Content = "ب" } }, SelectedIndex = 1 };
        Show(new StackPanel { Spacing = 12, Margin = new Thickness(24), Children = { pages, crumbs, segmented } }, FlowDirection.RightToLeft, null, 700, 400);
        Settle();
        foreach (var root in new Control[] { pages, crumbs, segmented })
        {
            Assert.False(root.ClipToBounds, $"{root.GetType().Name} clips");
            foreach (var face in root.GetVisualDescendants().OfType<Border>().Where(b => Outer(b.BoxShadow)))
                AssertFree(face, root.GetVisualParent()!);
        }
    }

    // A slider at either end keeps the whole thumb, its ring and its shadow: nothing in the template clips them.
    [AvaloniaTheory]
    [InlineData("rtl")]
    [InlineData("ltr")]
    public void A_slider_at_either_end_keeps_its_thumb_whole(string dir)
    {
        var slider = new Slider { Width = 200, Minimum = 0, Maximum = 50, Value = 0 };
        var page = new StackPanel { Margin = new Thickness(40), Children = { slider } };
        var window = Show(page, dir == "rtl" ? FlowDirection.RightToLeft : FlowDirection.LeftToRight, null, 700, 300);
        var thumb = Part<Thumb>(slider, "thumb");
        foreach (var value in new[] { 0.0, 50.0 })
        {
            slider.Value = value;
            Settle();
            AssertFree(Part<Border>(thumb, "PART_Face"), page);
            var at = thumb.TranslatePoint(new Point(0, 0), window)!.Value;
            var box = slider.TranslatePoint(new Point(0, 0), window)!.Value;
            var left = Math.Min(at.X, thumb.TranslatePoint(new Point(thumb.Bounds.Width, 0), window)!.Value.X);
            var edge = Math.Min(box.X, slider.TranslatePoint(new Point(slider.Bounds.Width, 0), window)!.Value.X);
            Assert.True(left >= edge - 0.5, $"the thumb at {value} starts at {left}, the slider at {edge}");
        }
    }

    // The faces whose shadow or ring reaches past their own box: none of them clips it.
    [AvaloniaFact]
    public void Shadowed_faces_do_not_clip_themselves()
    {
        var segmented = new Segmented { Items = { new SegmentedItem { Content = "أ" }, new SegmentedItem { Content = "ب" } }, SelectedIndex = 0 };
        var chip = new Chip { Content = "وسم" };
        var slider = new Slider { Width = 200, Value = 40 };
        var tabs = new TabControl { Items = { new TabItem { Header = "الأول", Content = "١" }, new TabItem { Header = "الثاني", Content = "٢" } } };
        var crumbs = new Breadcrumb { ItemsSource = new[] { "الرئيسية", "الصفحة" } };
        var pages = new Pagination { PageCount = 5, CurrentPage = 2 };
        var list = new ListBox { ItemsSource = new[] { "أ", "ب" }, SelectedIndex = 0 };
        var repeat = new RepeatButton { Content = "+" };
        var page = new StackPanel { Spacing = 12, Margin = new Thickness(24), Children = { segmented, chip, slider, tabs, crumbs, pages, list, repeat } };
        Show(page, FlowDirection.RightToLeft, null, 700, 700);
        var faces = page.GetVisualDescendants().OfType<Control>().Where(c => c is Segmented or SegmentedItem or Chip or TabItem or BreadcrumbItem or ListBoxItem or RepeatButton || c is Thumb { Name: "thumb" } || c is Button && c.FindAncestorOfType<Pagination>() is not null).ToList();
        Assert.Contains(faces, c => c is Thumb);
        Assert.Contains(faces, c => c is Button && c.FindAncestorOfType<Pagination>() is not null);
        foreach (var c in faces)
            Assert.False(c.ClipToBounds, $"{c.GetType().Name} clips its own shadow");
    }
}
