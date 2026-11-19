using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.VisualTree;
using Insiyab.Avalonia.Controls;
using static Insiyab.Avalonia.Tests.Ui;

namespace Insiyab.Avalonia.Tests;

public class TopbarFitTests
{
    private static FlowDirection Dir(string dir) => dir == "rtl" ? FlowDirection.RightToLeft : FlowDirection.LeftToRight;

    // A control's box in window pixels, whichever way the window flows.
    private static Rect Box(Visual c, Visual to)
    {
        var a = c.TranslatePoint(default, to)!.Value;
        var b = c.TranslatePoint(new Point(c.Bounds.Width, c.Bounds.Height), to)!.Value;
        return new Rect(new Point(Math.Min(a.X, b.X), Math.Min(a.Y, b.Y)), new Point(Math.Max(a.X, b.X), Math.Max(a.Y, b.Y)));
    }

    private static Window Build(string dir, string account, out Grid grid, out StackPanel center, out StackPanel end, out Border accountIsland, out CaptionButtons captions)
    {
        var accountBox = new Border { Classes = { "island" }, Child = new TextBlock { Text = account } };
        var centerZone = new StackPanel { Classes = { "zone", "center" }, Children = { new Border { Classes = { "island", "title" }, Child = new TextBlock { Classes = { "shell-title" }, Text = "لوحة المتابعة" } } } };
        var endZone = new StackPanel { Classes = { "zone", "end" }, Children = { new Border { Classes = { "island" }, Child = new TextBlock { Text = "الإشعارات" } }, accountBox } };
        var caps = new CaptionButtons { Preview = CaptionPreview.Normal };
        var g = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("*,Auto,*,Auto"),
            Children =
            {
                new StackPanel { Classes = { "zone", "start" }, Children = { new Border { Classes = { "island" }, Child = new TextBlock { Text = "القائمة" } } } },
                centerZone,
                endZone,
                caps,
            },
        };
        Grid.SetColumn(caps, 3);
        grid = g;
        center = centerZone;
        end = endZone;
        accountIsland = accountBox;
        captions = caps;
        return Show(new Border { Classes = { "topbar" }, Child = g }, Dir(dir), null, 1280, 800);
    }

    private const string Long = "مساحة العمل المشتركة لفريق التصميم والتطوير والتسويق والمبيعات";

    [AvaloniaTheory]
    [InlineData("rtl")]
    [InlineData("ltr")]
    public void A_long_end_zone_never_runs_under_the_caption_buttons(string dir)
    {
        var w = Build(dir, Long, out var grid, out var center, out var end, out var account, out var captions);
        var natural = end.Children.Where(c => c.IsVisible).Sum(c => c.DesiredSize.Width) + end.Spacing * (end.Children.Count - 1);
        var half = (grid.Bounds.Width - center.Bounds.Width - captions.Bounds.Width - 3 * grid.ColumnSpacing) / 2;
        Assert.True(natural > half + 20, $"the repro needs a longer account text: natural {natural}, half {half}");
        var cap = Box(captions, w);
        if (dir == "ltr")
        {
            var right = end.Children.Max(c => Box(c, w).Right);
            Assert.True(right <= cap.Left + 0.5, $"end zone right {right}, captions left {cap.Left} (natural {natural}, half {half})");
        }
        else
        {
            var left = end.Children.Min(c => Box(c, w).Left);
            Assert.True(left >= cap.Right - 0.5, $"end zone left {left}, captions right {cap.Right} (natural {natural}, half {half})");
        }
        var island = Box(account, w);
        foreach (var child in center.Children) Assert.False(island.Intersects(Box(child, w)), $"account island {island} meets the centre zone {Box(child, w)}");
        Capture(w, $"topbar-long-end-{dir}");
    }

    [AvaloniaTheory]
    [InlineData("rtl")]
    [InlineData("ltr")]
    public void With_room_the_side_columns_stay_equal(string dir)
    {
        Build(dir, "حسابي", out var grid, out _, out _, out _, out _);
        Assert.Equal(grid.ColumnDefinitions[0].ActualWidth, grid.ColumnDefinitions[2].ActualWidth, 0.5);
    }

    // A bar as the apps build it: ☰ and a back island at the start, the page's name (gliding) with its line under it in the middle, the account (a picture and a name) and the bell at the end.
    private static Window Folding(string dir, double width, out Grid grid, out TextBlock name, out TextBlock sub, out Button account, out Button back)
    {
        name = new TextBlock { Text = "سامي التجريبي" };
        account = new Button { Classes = { "island", "folds" }, Content = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8, Children = { new Border { Width = 28, Height = 28 }, name } } };
        var title = new TextBlock { Classes = { "shell-title" }, Text = "أمن نظم المعلومات والشبكات الحاسوبية", TextTrimming = TextTrimming.CharacterEllipsis };
        sub = new TextBlock { Classes = { "shell-sub" }, Text = "BIS601 · C1" };
        back = new Button { Classes = { "island", "back", "folds-last" }, Content = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8, Children = { new Border { Width = 18, Height = 18 }, new TextBlock { Text = "الفصل الحالي" } } } };
        var g = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("*,Auto,*"),
            Children =
            {
                new StackPanel { Classes = { "zone", "start" }, Children = { new Button { Classes = { "island", "icon" }, Content = "≡" }, back } },
                new StackPanel { Classes = { "zone", "center" }, Children =
                {
                    new Border { Classes = { "island", "title" }, Child = new StackPanel { Classes = { "shell-head" }, Children = { new Marquee { Child = title }, sub } } },
                } },
                new StackPanel { Classes = { "zone", "end" }, Children = { account, new Button { Classes = { "island", "icon" }, Content = "◔" } } },
            },
        };
        grid = g;
        var w = Show(new Border { Classes = { "topbar" }, VerticalAlignment = VerticalAlignment.Top, Child = g }, Dir(dir), null, width, 400);
        Settle(200);
        return w;
    }

    [AvaloniaTheory]
    [InlineData("rtl", 360)]
    [InlineData("ltr", 360)]
    [InlineData("rtl", 600)]
    [InlineData("ltr", 600)]
    public void A_bar_too_narrow_for_its_parts_folds_them_and_keeps_every_island_inside(string dir, double width)
    {
        var w = Folding(dir, width, out var grid, out var name, out var sub, out _, out _);
        Assert.True(ShellLook.GetFold(grid) >= 1, $"fold {ShellLook.GetFold(grid)} at {width}");
        Assert.False(name.IsEffectivelyVisible, "the account shows its picture alone");
        Assert.True(sub.Bounds.Width < 0.5 && sub.Bounds.Height < 0.5, "the title's second line goes");
        foreach (var island in grid.GetVisualDescendants().OfType<Control>().Where(c => c.Classes.Contains("island") && c.IsEffectivelyVisible))
        {
            var box = Box(island, w);
            Assert.True(box.Left >= -0.5 && box.Right <= width + 0.5, $"{island.GetType().Name} {box} in a {width} window (fold {ShellLook.GetFold(grid)})");
        }
        Capture(w, $"topbar-fold-{dir}-{width}");
    }

    [AvaloniaTheory]
    [InlineData("rtl")]
    [InlineData("ltr")]
    public void A_wide_bar_folds_nothing_and_unfolds_when_the_window_grows_again(string dir)
    {
        var w = Folding(dir, 1280, out var grid, out var name, out _, out _, out var back);
        Assert.Equal(0, ShellLook.GetFold(grid));
        Assert.True(name.IsEffectivelyVisible);
        w.Width = 360;
        Settle(200);
        Assert.True(ShellLook.GetFold(grid) >= 1);
        w.Width = 1280;
        Settle(200);
        Assert.Equal(0, ShellLook.GetFold(grid));
        Assert.True(name.IsEffectivelyVisible && back.IsEffectivelyVisible, "every label comes back");
    }

    // A views switch in the name's place (a calendar's month / week / day in a narrow desktop window) folds the bar as a name does: the head's cap no longer follows its own measure (a layout loop), the islands stay inside, and the switch comes back whole once the window is wide again
    [AvaloniaTheory]
    [InlineData("rtl")]
    [InlineData("ltr")]
    public void A_views_switch_in_the_names_place_folds_the_bar_without_a_layout_loop(string dir)
    {
        var views = new Segmented { ItemsSource = new[] { "الشهر", "الأسبوع", "اليوم", "جدول الأعمال" } };
        var name = new TextBlock { Text = "سامي التجريبي" };
        var account = new Button { Classes = { "island", "folds" }, Content = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8, Children = { new Border { Width = 28, Height = 28 }, name } } };
        var head = new StackPanel { Classes = { "shell-head" }, Children = { new ContentControl { Content = views } } };
        var grid = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("*,Auto,*"),
            Children =
            {
                new StackPanel { Classes = { "zone", "start" }, Children = { new Button { Classes = { "island", "icon" }, Content = "≡" } } },
                new StackPanel { Classes = { "zone", "center" }, Children = { new Border { Classes = { "island", "title" }, Child = head } } },
                new StackPanel { Classes = { "zone", "end" }, Children = { account, new Button { Classes = { "island", "icon" }, Content = "◔" } } },
            },
        };
        var w = Show(new Border { Classes = { "topbar" }, VerticalAlignment = VerticalAlignment.Top, Child = grid }, Dir(dir), null, 1280, 400);
        Settle(200);
        var whole = head.Bounds.Width;
        w.Width = 360;
        Settle(300); // a layout loop throws here
        Assert.Equal(TopbarColumns.MaxFold, ShellLook.GetFold(grid));
        Assert.True(head.Bounds.Width < whole - 1, $"the switch keeps {head.Bounds.Width} of its {whole}");
        foreach (var island in grid.GetVisualDescendants().OfType<Control>().Where(c => c.Classes.Contains("island") && c.IsEffectivelyVisible))
        {
            var box = Box(island, w);
            Assert.True(box.Left >= -0.5 && box.Right <= 360.5, $"{island.GetType().Name} {box} in a 360 window");
        }
        w.Width = 1280;
        Settle(300);
        Assert.Equal(0, ShellLook.GetFold(grid));
        Assert.Equal(whole, head.Bounds.Width, 0.5);
    }
    // The window's caption buttons are a part of the bar's fit at every width; at its last fold step their island tightens (inset, gaps and a narrower button) and gives the room back once the bar unfolds; every island, the caption island whole, stays inside the window
    [AvaloniaTheory]
    [InlineData("rtl")]
    [InlineData("ltr")]
    public void The_caption_buttons_tighten_at_the_last_fold_and_stay_whole_inside(string dir)
    {
        var views = new Segmented { ItemsSource = new[] { "شهر", "أسبوع", "يوم" } };
        var head = new StackPanel { Classes = { "shell-head" }, Children = { new ContentControl { Content = views } } };
        var caps = new CaptionButtons { Preview = CaptionPreview.Normal };
        var grid = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("*,Auto,*,Auto"),
            Children =
            {
                new StackPanel { Classes = { "zone", "start" }, Children = { new Button { Classes = { "island", "icon" }, Content = "≡" } } },
                new StackPanel { Classes = { "zone", "center" }, Children = { new Border { Classes = { "island", "title" }, Child = head }, new Button { Classes = { "island", "icon", "folds-away" }, Content = "⌕" } } },
                new StackPanel { Classes = { "zone", "end" } },
                caps,
            },
        };
        Grid.SetColumn(caps, 3);
        var w = Show(new Border { Classes = { "topbar" }, VerticalAlignment = VerticalAlignment.Top, Child = grid }, Dir(dir), null, 1280, 400);
        Settle(200);
        var full = caps.Bounds.Width;
        w.Width = 360;
        Settle(300);
        Assert.True(ShellLook.GetFold(grid) >= 3, $"fold {ShellLook.GetFold(grid)}: bar {grid.Bounds.Width}, " + string.Join(", ", grid.GetVisualDescendants().OfType<Control>().Where(c => (c.Classes.Contains("island") || c is CaptionButtons) && c.IsEffectivelyVisible).Select(c => $"{c.GetType().Name} {Box(c, w)}")));
        Assert.True(caps.Bounds.Width < full - 10, $"the caption island keeps {caps.Bounds.Width} of its {full}");
        foreach (var part in grid.GetVisualDescendants().OfType<Control>().Where(c => (c.Classes.Contains("island") || c is CaptionButtons) && c.IsEffectivelyVisible))
        {
            var box = Box(part, w);
            Assert.True(box.Left >= -0.5 && box.Right <= 360.5, $"{part.GetType().Name} {box} in a 360 window");
        }
        w.Width = 1280;
        Settle(300);
        Assert.Equal(0, ShellLook.GetFold(grid));
        Assert.Equal(full, caps.Bounds.Width, 0.5);
    }

    // An island marked folds-away-last goes only where the bar still lacks the room once the folds-away islands went and the bar tightened (step 4): a short name keeps it at 360, a long one takes its room
    [AvaloniaTheory]
    [InlineData("rtl")]
    [InlineData("ltr")]
    public void An_island_marked_to_fold_away_last_goes_only_where_the_tightened_bar_lacks_room(string dir)
    {
        var name = new TextBlock { Text = "الرئيسية" };
        var head = new StackPanel { Classes = { "shell-head" }, Children = { new Marquee { Child = name } } };
        var search = new Button { Classes = { "island", "icon", "folds-away-last" }, Content = "⌕" };
        var pill = new Button { Classes = { "island", "folds-last", "folds-away" }, Content = new StackPanel { Orientation = Orientation.Horizontal, Children = { new TextBlock { Text = "●" }, new TextBlock { Text = "الفصل F25" } } } };
        var caps = new CaptionButtons { Preview = CaptionPreview.Normal };
        var grid = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("*,Auto,*,Auto"),
            Children =
            {
                new StackPanel { Classes = { "zone", "start" }, Children = { new Button { Classes = { "island", "icon" }, Content = "≡" }, pill } },
                new StackPanel { Classes = { "zone", "center" }, Children = { new Border { Classes = { "island", "title" }, Child = head }, search } },
                new StackPanel { Classes = { "zone", "end" } },
                caps,
            },
        };
        Grid.SetColumn(caps, 3);
        var w = Show(new Border { Classes = { "topbar" }, VerticalAlignment = VerticalAlignment.Top, Child = grid }, Dir(dir), null, 360, 400);
        Settle(300);
        Assert.False(pill.IsEffectivelyVisible, $"the pill goes first (fold {ShellLook.GetFold(grid)})");
        Assert.True(search.IsEffectivelyVisible, $"a short name keeps the search (fold {ShellLook.GetFold(grid)})");
        name.Text = "ملفي الشخصي ومعه اسم أطول من مكانه";
        Settle(300);
        Assert.Equal(TopbarColumns.MaxFold, ShellLook.GetFold(grid));
        Assert.False(search.IsEffectivelyVisible, "a long name takes the search's room");
        name.Text = "الرئيسية";
        Settle(300);
        Assert.True(search.IsEffectivelyVisible, $"the room back, the search back (fold {ShellLook.GetFold(grid)})");
    }
}
