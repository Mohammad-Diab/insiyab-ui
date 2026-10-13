using Avalonia;
using Avalonia.Controls;
using Avalonia.VisualTree;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Styling;
using Insiyab.Avalonia.Controls;
using ShapePath = Avalonia.Controls.Shapes.Path;
using WindowDecorationProperties = Avalonia.Controls.Chrome.WindowDecorationProperties;

using static Insiyab.Avalonia.Tests.Ui;

namespace Insiyab.Avalonia.Tests;

public class TitleBarTests
{
    private static FlowDirection Dir(string dir) => dir == "rtl" ? FlowDirection.RightToLeft : FlowDirection.LeftToRight;

    private static ThemeVariant Variant(string theme) => theme == "Dark" ? ThemeVariant.Dark : ThemeVariant.Light;

    private static CaptionButtons Shown(out Window window, FlowDirection dir = FlowDirection.LeftToRight, ThemeVariant? theme = null)
    {
        var caps = new CaptionButtons { ForceShown = true };
        window = Show(new Panel { Children = { caps } }, dir, theme);
        return caps;
    }

    [AvaloniaFact]
    public void Extends_sets_the_window_up_on_windows()
    {
        Assert.SkipUnless(OperatingSystem.IsWindows(), "the client-drawn title bar is Windows only");
        var w = Track(new Window { Width = 640, Height = 420 });
        TitleBar.SetExtends(w, true);
        w.Show();
        Settle();
        Assert.True(w.ExtendClientAreaToDecorationsHint);
        Assert.Equal(40, w.ExtendClientAreaTitleBarHeightHint);
        Assert.NotNull(w.WindowDecorationsTheme);

        TitleBar.SetExtends(w, false);
        Assert.False(w.ExtendClientAreaToDecorationsHint);
        Assert.Equal(-1, w.ExtendClientAreaTitleBarHeightHint);
        Assert.Null(w.WindowDecorationsTheme);
    }

    [AvaloniaFact]
    public void Buttons_hide_while_the_window_is_not_extended()
    {
        var caps = new CaptionButtons();
        var w = Show(new Panel { Children = { caps } });
        TitleBar.SetExtends(w, true);
        Settle();
        Assert.False(TitleBar.IsActive(w));
        Assert.False(caps.IsVisible);
    }

    [AvaloniaTheory]
    [MemberData(nameof(Looks), MemberType = typeof(Ui))]
    public void Close_is_outermost_on_the_end_side(string theme, string dir)
    {
        var caps = Shown(out var w, Dir(dir), Variant(theme));
        var close = Center(Part<Button>(caps, "PART_Close"), w);
        var minimize = Center(Part<Button>(caps, "PART_Minimize"), w);
        if (dir == "rtl")
        {
            Assert.True(close.X < minimize.X, $"close {close.X} should be left of minimise {minimize.X}");
            Assert.True(close.X < 46 + 23, $"close {close.X} should sit at the far left");
        }
        else
        {
            Assert.True(close.X > minimize.X, $"close {close.X} should be right of minimise {minimize.X}");
            Assert.True(close.X > w.Bounds.Width - 69, $"close {close.X} should sit at the far right of {w.Bounds.Width}");
        }
    }

    [AvaloniaFact]
    public void Buttons_carry_the_hit_test_roles()
    {
        var caps = Shown(out _);
        Assert.Equal(WindowDecorationsElementRole.MinimizeButton, WindowDecorationProperties.GetElementRole(Part<Button>(caps, "PART_Minimize")));
        Assert.Equal(WindowDecorationsElementRole.MaximizeButton, WindowDecorationProperties.GetElementRole(Part<Button>(caps, "PART_Maximize")));
        Assert.Equal(WindowDecorationsElementRole.CloseButton, WindowDecorationProperties.GetElementRole(Part<Button>(caps, "PART_Close")));
    }

    [AvaloniaFact]
    public void Drag_area_marks_itself_and_frees_focusable_children()
    {
        var button = new Button { Content = "دخول" };
        var text = new TextBlock { Text = "العنوان" };
        var border = new Border { Background = Brushes.Transparent, Child = new StackPanel { Children = { button, text } } };
        TitleBar.SetIsDragArea(border, true);
        Show(border);
        Assert.Equal(WindowDecorationsElementRole.TitleBar, WindowDecorationProperties.GetElementRole(border));
        Assert.Equal(WindowDecorationsElementRole.User, WindowDecorationProperties.GetElementRole(button));
        Assert.Equal(WindowDecorationsElementRole.None, WindowDecorationProperties.GetElementRole(text));
        Assert.Equal(WindowDecorationsElementRole.TitleBar, TitleBar.RoleAt(text));

        TitleBar.SetIsDragArea(border, false);
        Assert.Equal(WindowDecorationsElementRole.None, WindowDecorationProperties.GetElementRole(border));
    }

    [AvaloniaFact]
    public void App_icon_is_a_menu_target_not_a_drag_handle()
    {
        var iconText = new TextBlock { Text = "ان" };
        var icon = new Border { Width = 40, Height = 40, Background = Brushes.Transparent, Child = iconText };
        var bar = new Border { Background = Brushes.Transparent, HorizontalAlignment = HorizontalAlignment.Left, Child = icon };
        TitleBar.SetIsDragArea(bar, true);
        TitleBar.SetIsSystemMenuArea(icon, true);
        var w = Show(bar);
        Assert.Equal(WindowDecorationsElementRole.User, WindowDecorationProperties.GetElementRole(icon));
        Assert.Equal(WindowDecorationsElementRole.User, TitleBar.RoleAt(iconText));

        var released = 0;
        var handled = false;
        w.AddHandler(InputElement.PointerReleasedEvent, (_, e) => { released++; handled = e.Handled; }, RoutingStrategies.Bubble, handledEventsToo: true);
        var p = Center(icon, w);
        w.MouseDown(p, MouseButton.Right);
        w.MouseUp(p, MouseButton.Right);
        Settle();
        Assert.True(released > 0);
        Assert.False(handled);

        TitleBar.SetIsSystemMenuArea(icon, false);
        Assert.Equal(WindowDecorationsElementRole.None, WindowDecorationProperties.GetElementRole(icon));
    }

    [AvaloniaFact]
    public void Maximise_toggles_and_its_tip_follows()
    {
        var caps = Shown(out var w);
        var max = Part<Button>(caps, "PART_Maximize");
        Assert.Equal("تكبير", ToolTip.GetTip(max));
        Assert.Same(Res(max, "InsCaptionGlyphMaximize"), max.Content);

        Click(max, w);
        Assert.Equal(WindowState.Maximized, w.WindowState);
        Assert.Equal("استعادة", ToolTip.GetTip(max));
        Assert.Same(Res(max, "InsCaptionGlyphRestore"), Part<ShapePath>(max, "PART_Glyph").Data);

        Click(max, w);
        Assert.Equal(WindowState.Normal, w.WindowState);
        Assert.Equal("تكبير", ToolTip.GetTip(max));
        Assert.Same(Res(max, "InsCaptionGlyphMaximize"), Part<ShapePath>(max, "PART_Glyph").Data);
    }

    [AvaloniaFact]
    public void Close_turns_red_on_hover()
    {
        var caps = Shown(out var w);
        var close = Part<Button>(caps, "PART_Close");
        var minimize = Part<Button>(caps, "PART_Minimize");

        w.MouseMove(Center(close, w));
        Settle();
        AssertBrush(close, "InsCaptionClose", () => Part<Border>(close, "PART_Face").Background);

        w.MouseMove(Center(minimize, w));
        Settle();
        AssertBrush(minimize, "InsGlassHover", () => Part<Border>(minimize, "PART_Face").Background);
    }

    [AvaloniaTheory]
    [MemberData(nameof(Looks), MemberType = typeof(Ui))]
    public void Buttons_are_one_island_centred_with_the_others(string theme, string dir)
    {
        var caps = new CaptionButtons { ForceShown = true };
        var other = new Border { Classes = { "island" }, Child = new TextBlock { Text = "المظهر" } };
        var zone = new StackPanel { Classes = { "zone", "end" }, Children = { other } };
        Grid.SetColumn(caps, 3);
        var bar = new Border { Classes = { "topbar" }, VerticalAlignment = VerticalAlignment.Top, Child = new Grid { ColumnDefinitions = new ColumnDefinitions("*,Auto,*,Auto"), Children = { zone, caps } } };
        var w = Show(new Panel { Children = { bar } }, Dir(dir), Variant(theme), 900);

        Assert.Equal(default, caps.Margin);
        var island = Part<Border>(caps, "PART_Island");
        Assert.Equal(48, island.Bounds.Height);
        Assert.Equal(new Thickness(4), island.Padding);
        Assert.Equal(Res(caps, "InsRPill"), island.CornerRadius);
        AssertBrush(island, "InsGlass", () => island.Background);
        AssertBrush(island, "InsRim", () => island.BorderBrush);
        Assert.Equal(Center(other, w).Y, Center(island, w).Y, 0.5);
        var box = new Rect(island.Bounds.Size).TransformToAABB(island.TransformToVisual(w)!.Value);
        Assert.Equal(16, dir == "rtl" ? box.Left : w.Bounds.Width - box.Right, 0.5);
        foreach (var name in new[] { "PART_Minimize", "PART_Maximize", "PART_Close" })
        {
            var button = Part<Button>(caps, name);
            Assert.Equal(40, button.Bounds.Width);
            Assert.Equal(38, button.Bounds.Height);
            Assert.Equal(Res(caps, "InsRPill"), Part<Border>(button, "PART_Face").CornerRadius);
        }
    }

    [AvaloniaFact]
    public void Cannot_maximize_hides_the_maximise_button()
    {
        var caps = Shown(out var w);
        var max = Part<Button>(caps, "PART_Maximize");
        var min = Part<Button>(caps, "PART_Minimize");
        Assert.True(max.IsVisible);
        Assert.True(min.IsVisible);

        w.CanMaximize = false;
        Settle();
        Assert.False(max.IsVisible);
        Assert.True(min.IsVisible);

        w.CanMinimize = false;
        Settle();
        Assert.False(min.IsVisible);
    }

    [AvaloniaFact]
    public void Full_screen_hides_the_buttons()
    {
        var caps = Shown(out var w);
        Assert.True(caps.IsVisible);
        w.WindowState = WindowState.FullScreen;
        Settle();
        Assert.False(caps.IsVisible);
    }

    [AvaloniaTheory]
    [MemberData(nameof(Looks), MemberType = typeof(Ui))]
    public void Glyphs_are_drawn_whatever_the_font(string theme, string dir)
    {
        var caps = Shown(out var w, Dir(dir), Variant(theme));
        Capture(w, $"titlebar-caption-{theme}-{dir}");
        foreach (var (part, key) in new[] { ("PART_Minimize", "InsCaptionGlyphMinimize"), ("PART_Maximize", "InsCaptionGlyphMaximize"), ("PART_Close", "InsCaptionGlyphClose") })
        {
            var button = Part<Button>(caps, part);
            var glyph = Part<ShapePath>(button, "PART_Glyph");
            Assert.Same(Res(button, key), glyph.Data);
            Assert.True(glyph.Bounds.Width >= 10 && glyph.Bounds.Height >= 10, $"{part} glyph is {glyph.Bounds}");
            AssertBrush(button, "InsText1", () => glyph.Stroke);
            Assert.Empty(button.GetVisualDescendants().OfType<TextBlock>());
        }
    }

    [AvaloniaFact]
    public void A_move_over_a_button_is_kept_from_windows()
    {
        var caps = Shown(out var w);
        var moves = new List<bool>();
        w.AddHandler(InputElement.PointerMovedEvent, (_, e) => moves.Add(e.Handled), RoutingStrategies.Bubble, handledEventsToo: true);
        w.MouseMove(Center(Part<Button>(caps, "PART_Close"), w));
        w.MouseMove(Center(Part<Button>(caps, "PART_Maximize"), w));
        Settle();
        Assert.NotEmpty(moves);
        Assert.All(moves, Assert.True);

        moves.Clear();
        w.MouseMove(new Point(w.Bounds.Width / 2, w.Bounds.Height - 20));
        Settle();
        Assert.All(moves, Assert.False);
    }

    // TITLEBARINFOEX as user32 lays it out: cbSize, rcTitleBar, rgstate[6], rgrect[6].
    [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)]
    private struct TitleBarInfo
    {
        public int Size, BarLeft, BarTop, BarRight, BarBottom;
        public int State0, State1, StateMin, StateMax, State4, StateClose;
        public TitleBarNative.RECT Rect0, Rect1, Min, Max, Rect4, Close;
    }

    [AvaloniaTheory]
    [InlineData("rtl")]
    [InlineData("ltr")]
    public void Title_bar_info_names_our_buttons_for_snap_layouts(string dir)
    {
        var caps = Shown(out var w, Dir(dir));
        var size = System.Runtime.InteropServices.Marshal.SizeOf<TitleBarInfo>();
        Assert.Equal(140, size);
        var info = System.Runtime.InteropServices.Marshal.AllocHGlobal(size);
        try
        {
            TitleBar.FillTitleBarInfo(info, caps, new TitleBarNative.RECT { Left = 0, Top = 0, Right = 640, Bottom = 420 });
            var read = System.Runtime.InteropServices.Marshal.PtrToStructure<TitleBarInfo>(info);
            Assert.Equal(TitleBar.ScreenRect(Part<Button>(caps, "PART_Maximize")), read.Max);
            Assert.Equal(TitleBar.ScreenRect(Part<Button>(caps, "PART_Minimize")), read.Min);
            Assert.Equal(TitleBar.ScreenRect(Part<Button>(caps, "PART_Close")), read.Close);
            Assert.Equal(40, read.Max.Right - read.Max.Left);
            Assert.Equal((0, 0, 0), (read.StateMin, read.StateMax, read.StateClose));
            Assert.Equal((0, 640, TitleBar.ScreenRect(caps).Bottom), (read.BarLeft, read.BarRight, read.BarBottom));
            var max = Center(Part<Button>(caps, "PART_Maximize"), w);
            Assert.InRange(max.X, read.Max.Left, read.Max.Right);

            w.CanMaximize = false;
            Settle(100);
            TitleBar.FillTitleBarInfo(info, caps, new TitleBarNative.RECT { Right = 640, Bottom = 420 });
            read = System.Runtime.InteropServices.Marshal.PtrToStructure<TitleBarInfo>(info);
            Assert.Equal(0x8000, read.StateMax);
            Assert.Equal(0, read.StateClose);
        }
        finally
        {
            System.Runtime.InteropServices.Marshal.FreeHGlobal(info);
        }
    }

    [AvaloniaFact]
    public void Inactive_window_dims_the_glyphs_but_not_a_hovered_close()
    {
        var caps = Shown(out var w);
        var close = Part<Button>(caps, "PART_Close");
        var minimize = Part<Button>(caps, "PART_Minimize");
        foreach (var b in new[] { close, minimize }) b.Classes.Set("inactive", true);
        Settle();
        Assert.True(Part<ShapePath>(minimize, "PART_Glyph").Opacity < 1);
        w.MouseMove(Center(close, w));
        Settle();
        Assert.Equal(1, Part<ShapePath>(close, "PART_Glyph").Opacity);
        AssertBrush(close, "InsCaptionClose", () => Part<Border>(close, "PART_Face").Background);
    }

    [AvaloniaFact]
    public void Preview_shows_its_state_and_leaves_the_window_alone()
    {
        var normal = new CaptionButtons { Preview = CaptionPreview.Normal };
        var maximized = new CaptionButtons { Preview = CaptionPreview.Maximized };
        var inactive = new CaptionButtons { Preview = CaptionPreview.Inactive };
        var w = Show(new StackPanel { Children = { normal, maximized, inactive } });
        Assert.False(TitleBar.IsActive(w));
        Assert.Null(w.GetValue(TitleBar.CaptionsProperty));
        foreach (var caps in new[] { normal, maximized, inactive })
        {
            Assert.True(caps.IsVisible);
            foreach (var name in new[] { "PART_Minimize", "PART_Maximize", "PART_Close" })
                Assert.Equal(WindowDecorationsElementRole.None, WindowDecorationProperties.GetElementRole(Part<Button>(caps, name)));
        }
        Assert.Same(Res(normal, "InsCaptionGlyphMaximize"), Part<ShapePath>(Part<Button>(normal, "PART_Maximize"), "PART_Glyph").Data);
        Assert.Same(Res(maximized, "InsCaptionGlyphRestore"), Part<ShapePath>(Part<Button>(maximized, "PART_Maximize"), "PART_Glyph").Data);
        Assert.Equal("استعادة", ToolTip.GetTip(Part<Button>(maximized, "PART_Maximize")));
        Assert.Equal(1, Part<ShapePath>(Part<Button>(normal, "PART_Minimize"), "PART_Glyph").Opacity);
        Assert.True(Part<ShapePath>(Part<Button>(inactive, "PART_Minimize"), "PART_Glyph").Opacity < 1);

        Click(Part<Button>(normal, "PART_Maximize"), w);
        Click(Part<Button>(normal, "PART_Minimize"), w);
        Click(Part<Button>(normal, "PART_Close"), w);
        Assert.Equal(WindowState.Normal, w.WindowState);
        Assert.True(w.IsVisible);

        normal.Preview = CaptionPreview.Off;
        Settle();
        Assert.Same(normal, w.GetValue(TitleBar.CaptionsProperty));
        Assert.Equal(WindowDecorationsElementRole.CloseButton, WindowDecorationProperties.GetElementRole(Part<Button>(normal, "PART_Close")));
        Assert.False(normal.IsVisible);
    }
}