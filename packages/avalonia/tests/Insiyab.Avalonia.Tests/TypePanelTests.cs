using Avalonia;
using Avalonia.Animation;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.Media.Imaging;

namespace Insiyab.Avalonia.Tests;

public class TypePanelTests
{
    private static Window Show(Control content, ThemeVariant theme, FlowDirection flow = FlowDirection.RightToLeft)
    {
        var window = new Window
        {
            Width = 900,
            Height = 800,
            RequestedThemeVariant = theme,
            FlowDirection = flow,
            Content = content,
        };
        window.Show();
        return window;
    }

    private static T Token<T>(string key, ThemeVariant theme)
    {
        Assert.True(Application.Current!.TryGetResource(key, theme, out var value), $"missing resource {key}");
        return Assert.IsType<T>(value);
    }

    private static TextBlock Text(string cls) => new() { Text = "نص 123", Classes = { cls } };

    public static TheoryData<string> Themes => new() { "Light", "Dark" };

    private static ThemeVariant Theme(string name) => name == "Dark" ? ThemeVariant.Dark : ThemeVariant.Light;

    [AvaloniaTheory]
    [InlineData("display", 48, 57.6)]
    [InlineData("h1", 28, 36.4)]
    [InlineData("h2", 22, 28.6)]
    [InlineData("h3", 18, 23.4)]
    [InlineData("h4", 16, 20.8)]
    public void Headings_take_their_size_weight_and_line_height(string cls, double size, double lineHeight)
    {
        var block = Text(cls);
        Show(block, ThemeVariant.Light);
        Assert.Equal(size, block.FontSize);
        Assert.Equal(lineHeight, block.LineHeight, 3);
        Assert.Equal(FontWeight.Bold, block.FontWeight);
        Assert.Equal(TextWrapping.Wrap, block.TextWrapping);
    }

    [AvaloniaTheory]
    [MemberData(nameof(Themes))]
    public void Text_classes_take_their_colour_in_each_theme(string themeName)
    {
        var theme = Theme(themeName);
        var h1 = Text("h1");
        var lead = Text("lead");
        var muted = Text("muted");
        var plain = new TextBlock { Text = "نص" };
        Show(new StackPanel { Children = { h1, lead, muted, plain } }, theme);

        var text1 = Token<Color>("InsText1Color", theme);
        var label = Token<Color>("InsLabelColor", theme);
        Assert.Equal(text1, Assert.IsAssignableFrom<ISolidColorBrush>(h1.Foreground).Color);
        Assert.Equal(label, Assert.IsAssignableFrom<ISolidColorBrush>(lead.Foreground).Color);
        Assert.Equal(label, Assert.IsAssignableFrom<ISolidColorBrush>(muted.Foreground).Color);
        Assert.Equal(text1, Assert.IsAssignableFrom<ISolidColorBrush>(plain.Foreground).Color);
        Assert.Equal(theme == ThemeVariant.Dark ? Color.Parse("#F1F5F9") : Color.Parse("#0F172A"), text1);
    }

    [AvaloniaTheory]
    [MemberData(nameof(Themes))]
    public void Fluent_controls_follow_the_text_colour(string themeName)
    {
        var theme = Theme(themeName);
        var radio = new RadioButton { Content = "خيار" };
        var combo = new ComboBox();
        Show(new StackPanel { Children = { radio, combo } }, theme);

        var text1 = Token<Color>("InsText1Color", theme);
        Assert.Equal(text1, Assert.IsAssignableFrom<ISolidColorBrush>(radio.Foreground).Color);
        Assert.Equal(text1, Assert.IsAssignableFrom<ISolidColorBrush>(combo.Foreground).Color);
    }

    [AvaloniaFact]
    public void Lead_eyebrow_and_prose_match_the_web()
    {
        var lead = Text("lead");
        var eyebrow = Text("eyebrow");
        var prose = Text("prose");
        Show(new StackPanel { Children = { lead, eyebrow, prose } }, ThemeVariant.Light);

        Assert.Equal(18, lead.FontSize);
        Assert.Equal(32.4, lead.LineHeight, 3);
        Assert.Equal(12, eyebrow.FontSize);
        Assert.Equal(FontWeight.Bold, eyebrow.FontWeight);
        Assert.Equal(16, prose.FontSize);
        Assert.Equal(28.8, prose.LineHeight, 3);
    }

    [AvaloniaFact]
    public void Tracking_applies_in_LTR_only()
    {
        var rtlH1 = Text("h1");
        var rtlEyebrow = Text("eyebrow");
        Show(new StackPanel { Children = { rtlH1, rtlEyebrow } }, ThemeVariant.Light);
        Assert.Equal(0, rtlH1.LetterSpacing);
        Assert.Equal(0, rtlEyebrow.LetterSpacing);

        var ltrH1 = Text("h1");
        var ltrEyebrow = Text("eyebrow");
        var ltrProse = Text("prose");
        Show(new StackPanel { Children = { ltrH1, ltrEyebrow, ltrProse } }, ThemeVariant.Light, FlowDirection.LeftToRight);
        Assert.Equal(-0.56, ltrH1.LetterSpacing, 3);
        Assert.Equal(0.96, ltrEyebrow.LetterSpacing, 3);
        Assert.Equal(26.4, ltrProse.LineHeight, 3);
    }

    [AvaloniaFact]
    public void Num_is_an_LTR_island_with_tabular_figures_at_the_start_edge()
    {
        var num = new TextBlock { Text = "-2,375,000", Classes = { "num" } };
        var window = Show(new StackPanel { Width = 600, Children = { num } }, ThemeVariant.Light);

        Assert.Equal(FlowDirection.LeftToRight, num.FlowDirection);
        Assert.NotNull(num.FontFeatures);
        Assert.Contains(num.FontFeatures!, f => f.Tag == "tnum" && f.Value == 1);
        Assert.Contains(num.FontFeatures!, f => f.Tag == "lnum" && f.Value == 1);

        var centre = num.TranslatePoint(new Point(num.Bounds.Width / 2, 0), window)!.Value;
        Assert.True(centre.X > window.Width / 2, "in RTL the figure starts at the right edge");
    }

    [AvaloniaTheory]
    [MemberData(nameof(Themes))]
    public void Card_glass_and_panel_wear_the_four_layers(string themeName)
    {
        var theme = Theme(themeName);
        var card = new Border { Classes = { "card" }, Height = 80 };
        var glass = new Border { Classes = { "glass" }, Height = 80 };
        var panel = new Border { Classes = { "panel" }, Height = 80 };
        Show(new StackPanel { Spacing = 20, Margin = new Thickness(30), Children = { card, glass, panel } }, theme);

        Assert.Equal(Token<CornerRadius>("InsRMd", theme), card.CornerRadius);
        Assert.Equal(new CornerRadius(14), card.CornerRadius);
        Assert.Equal(new CornerRadius(14), glass.CornerRadius);
        Assert.Equal(new CornerRadius(22), panel.CornerRadius);
        Assert.False(panel.ClipToBounds);

        var glassShadow = Token<BoxShadows>("InsShGlass", theme)[0];
        var bevel = Token<BoxShadows>("InsBevel", theme)[0];
        foreach (var surface in new[] { card, glass, panel })
        {
            Assert.Equal(new Thickness(1), surface.BorderThickness);
            Assert.Equal(2, surface.BoxShadow.Count);
            Assert.Equal(glassShadow, surface.BoxShadow[0]);
            Assert.Equal(bevel, surface.BoxShadow[1]);
            Assert.False(surface.BoxShadow[0].IsInset);
            Assert.True(surface.BoxShadow[1].IsInset);
            Assert.True(surface.BoxShadow.HasInsetShadows);

            var rim = Assert.IsAssignableFrom<ILinearGradientBrush>(surface.BorderBrush);
            Assert.Equal(Token<Color>("InsRimTopColor", theme), rim.GradientStops[0].Color);
            Assert.Equal(Token<Color>("InsRimColor", theme), rim.GradientStops[^1].Color);
        }

        var endStop = theme == ThemeVariant.Dark ? Color.FromArgb(8, 255, 255, 255) : Color.FromArgb(97, 255, 255, 255);
        foreach (var surface in new[] { card, panel })
        {
            var fill = Assert.IsAssignableFrom<ILinearGradientBrush>(surface.Background);
            Assert.Equal(Token<Color>("InsGlassColor", theme), fill.GradientStops[0].Color);
            Assert.Equal(endStop, fill.GradientStops[^1].Color);
        }
        var sheen = Assert.IsAssignableFrom<ILinearGradientBrush>(glass.Background);
        Assert.Equal(4, sheen.GradientStops.Count);
        Assert.Equal(endStop, sheen.GradientStops[^1].Color);
    }

    [AvaloniaFact]
    public void Theme_switch_reaches_the_glass()
    {
        var card = new Border { Classes = { "card" }, Height = 40 };
        var window = Show(card, ThemeVariant.Light);
        var lightBevel = card.BoxShadow[1];

        window.RequestedThemeVariant = ThemeVariant.Dark;
        Assert.Equal(Token<BoxShadows>("InsBevel", ThemeVariant.Dark)[0], card.BoxShadow[1]);
        Assert.NotEqual(lightBevel, card.BoxShadow[1]);
        var fill = Assert.IsAssignableFrom<ILinearGradientBrush>(card.Background);
        Assert.Equal(Token<Color>("InsGlassColor", ThemeVariant.Dark), fill.GradientStops[0].Color);
    }

    [AvaloniaFact]
    public void The_light_stays_at_the_top_left_in_both_directions()
    {
        var rtlCard = new Border { Classes = { "card" }, Height = 40 };
        var rtlGlass = new Border { Classes = { "glass" }, Height = 40 };
        Show(new StackPanel { Children = { rtlCard, rtlGlass } }, ThemeVariant.Light);
        var ltrCard = new Border { Classes = { "card" }, Height = 40 };
        var ltrGlass = new Border { Classes = { "glass" }, Height = 40 };
        Show(new StackPanel { Children = { ltrCard, ltrGlass } }, ThemeVariant.Light, FlowDirection.LeftToRight);

        static double StartX(IBrush? brush) => Assert.IsAssignableFrom<ILinearGradientBrush>(brush).StartPoint.Point.X;
        Assert.True(StartX(ltrCard.Background) < 0.5);
        Assert.True(StartX(rtlCard.Background) > 0.5, "RTL mirrors the drawing, so its brush starts at the far side");
        Assert.True(StartX(ltrGlass.Background) < 0.5);
        Assert.True(StartX(rtlGlass.Background) > 0.5);
    }

    [AvaloniaFact]
    public void Glass_inside_glass_strips_to_a_flat_fill()
    {
        var inner = new Border { Classes = { "glass" }, Height = 30 };
        var outer = new Border { Classes = { "glass" }, Padding = new Thickness(20), Child = inner };
        Show(outer, ThemeVariant.Light);

        Assert.Equal(0, inner.BoxShadow.Count);
        Assert.Equal(Token<Color>("InsGlassInnerColor", ThemeVariant.Light), Assert.IsAssignableFrom<ISolidColorBrush>(inner.Background).Color);
        Assert.Equal(2, outer.BoxShadow.Count);
    }

    private static (Border panel, Border head, TextBlock title, StackPanel actions, Border body, Border foot) BuildPanel()
    {
        var title = new TextBlock { Text = "الإعدادات العامة", Classes = { "panel-title" } };
        var actions = new StackPanel { Classes = { "panel-actions" }, Children = { new TextBlock { Text = "تعديل" } } };
        DockPanel.SetDock(actions, Dock.Right);
        var head = new Border { Classes = { "panel-head" }, Child = new DockPanel { Children = { actions, title } } };
        DockPanel.SetDock(head, Dock.Top);
        var foot = new Border { Classes = { "panel-foot" }, Child = new TextBlock { Text = "آخر تحديث قبل ساعة", Classes = { "muted" } } };
        DockPanel.SetDock(foot, Dock.Bottom);
        var body = new Border { Classes = { "panel-body" }, Child = new TextBlock { Text = "محتوى اللوحة" } };
        var panel = new Border { Classes = { "panel" }, Width = 500, Child = new DockPanel { Children = { head, foot, body } } };
        return (panel, head, title, actions, body, foot);
    }

    [AvaloniaTheory]
    [InlineData(FlowDirection.RightToLeft)]
    [InlineData(FlowDirection.LeftToRight)]
    public void Panel_head_mirrors_with_the_direction(FlowDirection flow)
    {
        var (panel, head, title, actions, body, foot) = BuildPanel();
        var window = Show(panel, ThemeVariant.Light, flow);

        Assert.Equal(new Thickness(20, 14, 20, 14), head.Padding);
        Assert.Equal(new Thickness(0, 0, 0, 1), head.BorderThickness);
        Assert.Equal(new Thickness(20), body.Padding);
        Assert.Equal(new Thickness(18, 12, 18, 12), foot.Padding);
        Assert.Equal(new Thickness(0, 1, 0, 0), foot.BorderThickness);
        Assert.Equal(17, title.FontSize);
        Assert.Equal(FontWeight.ExtraBold, title.FontWeight);

        var titleX = title.TranslatePoint(new Point(title.Bounds.Width / 2, 0), window)!.Value.X;
        var actionsX = actions.TranslatePoint(new Point(actions.Bounds.Width / 2, 0), window)!.Value.X;
        if (flow == FlowDirection.RightToLeft)
            Assert.True(titleX > actionsX, "RTL: title at the right, actions at the left");
        else
            Assert.True(titleX < actionsX, "LTR: title at the left, actions at the right");
    }

    [AvaloniaTheory]
    [MemberData(nameof(Themes))]
    public void Panel_title_and_alert_colours(string themeName)
    {
        var theme = Theme(themeName);
        var (panel, head, title, _, _, _) = BuildPanel();
        var (alert, alertHead, alertTitle, _, _, _) = BuildPanel();
        alert.Classes.Add("alert");
        Show(new StackPanel { Children = { panel, alert } }, theme);

        var titleColour = theme == ThemeVariant.Dark ? Token<Color>("InsText1Color", theme) : Token<Color>("InsPrimaryDarkerColor", theme);
        Assert.Equal(titleColour, Assert.IsAssignableFrom<ISolidColorBrush>(title.Foreground).Color);
        Assert.Equal(Token<Color>("InsToneBadColor", theme), Assert.IsAssignableFrom<ISolidColorBrush>(alertTitle.Foreground).Color);
        Assert.Equal(Token<Color>("InsToneBadBgColor", theme), Assert.IsAssignableFrom<ISolidColorBrush>(alertHead.Background).Color);
        Assert.Null(head.Background);
        Assert.Equal(new CornerRadius(21, 21, 0, 0), alertHead.CornerRadius);
    }

    [AvaloniaFact]
    public void Hoverable_lifts_under_the_pointer()
    {
        var card = new Border { Classes = { "card", "hoverable" }, Width = 300, Height = 120, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center };
        var plain = new Border { Classes = { "card" } };
        var window = Show(new Grid { Children = { plain, card } }, ThemeVariant.Light);
        Assert.NotNull(card.Transitions);
        Assert.Equal(3, card.Transitions!.Count);
        card.Transitions = new Transitions();

        window.MouseMove(new Point(450, 400));
        Assert.Equal(Token<BoxShadows>("InsShLg", ThemeVariant.Light)[0], card.BoxShadow[0]);
        Assert.True(card.BoxShadow[1].IsInset);
        Assert.Equal(Token<Color>("InsRimTopColor", ThemeVariant.Light), Assert.IsAssignableFrom<ISolidColorBrush>(card.BorderBrush).Color);
        Assert.Equal(-2, card.RenderTransform!.Value.M32);
        Assert.Null(plain.Transitions);
    }

    [AvaloniaTheory]
    [InlineData("Light", FlowDirection.RightToLeft)]
    [InlineData("Dark", FlowDirection.RightToLeft)]
    [InlineData("Light", FlowDirection.LeftToRight)]
    public void Renders_a_frame(string themeName, FlowDirection flow)
    {
        var (panel, _, _, _, _, _) = BuildPanel();
        panel.Width = double.NaN;
        var (alert, _, _, _, _, _) = BuildPanel();
        alert.Width = double.NaN;
        alert.Classes.Add("alert");
        var content = new StackPanel
        {
            Margin = new Thickness(32),
            Spacing = 20,
            Children =
            {
                Text("eyebrow"),
                new TextBlock { Text = flow == FlowDirection.LeftToRight ? "Heading one" : "عنوان رئيسي", Classes = { "h1" } },
                new TextBlock { Text = "فقرة تمهيدية تشرح ما تعرضه الصفحة في سطر أو سطرين.", Classes = { "lead" } },
                new TextBlock { Text = "-2,375,000", Classes = { "num" } },
                new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    Spacing = 20,
                    Children =
                    {
                        new Border { Classes = { "card" }, Width = 220, Height = 110, Child = new TextBlock { Text = "بطاقة", Margin = new Thickness(16) } },
                        new Border { Classes = { "glass" }, Width = 220, Height = 110, Child = new TextBlock { Text = "زجاج", Margin = new Thickness(16) } },
                        new Border { Classes = { "glass-inner" }, Width = 220, Height = 110, Child = new TextBlock { Text = "سطح داخلي", Margin = new Thickness(16) } },
                    },
                },
                panel,
                alert,
            },
        };
        var window = Show(content, Theme(themeName), flow);
        window.Background = new LinearGradientBrush
        {
            StartPoint = new RelativePoint(0, 0, RelativeUnit.Relative),
            EndPoint = new RelativePoint(1, 1, RelativeUnit.Relative),
            GradientStops = themeName == "Dark"
                ? new GradientStops { new GradientStop(Color.Parse("#141519"), 0), new GradientStop(Color.Parse("#2A1A24"), 1) }
                : new GradientStops { new GradientStop(Color.Parse("#F3E8EE"), 0), new GradientStop(Color.Parse("#E6ECF5"), 1) },
        };

        var frame = window.CaptureRenderedFrame();
        Assert.NotNull(frame);
        var dir = Environment.GetEnvironmentVariable("INS_SNAPSHOT_DIR");
        if (!string.IsNullOrEmpty(dir))
        {
            Directory.CreateDirectory(dir);
            frame!.Save(Path.Combine(dir, $"type-panel-{themeName}-{flow}.png"), PngBitmapEncoderOptions.Default);
        }
    }
}
