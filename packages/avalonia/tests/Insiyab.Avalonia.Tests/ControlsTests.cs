using System.Diagnostics;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Styling;
using Avalonia.Threading;
using Avalonia.VisualTree;
using ShapePath = Avalonia.Controls.Shapes.Path;

using static Insiyab.Avalonia.Tests.Ui;

namespace Insiyab.Avalonia.Tests;

public class ControlsTests
{
    [AvaloniaFact]
    public void Button_gets_the_insiyab_template_at_field_height()
    {
        var button = new Button { Content = "حفظ" };
        Show(button);

        Part<Border>(button, "PART_Face");
        Part<Border>(button, "PART_Shadow");
        Part<Border>(button, "PART_FocusRing");
        Part<ContentPresenter>(button, "PART_ContentPresenter");
        Assert.Equal(46, button.Bounds.Height);
        Assert.Equal(FontWeight.Bold, button.FontWeight);
    }

    [AvaloniaFact]
    public void Button_classes_change_the_face()
    {
        var plain = new Button { Content = "عادي" };
        var primary = new Button { Content = "أساسي", Classes = { "primary" } };
        var secondary = new Button { Content = "ثانوي", Classes = { "secondary" } };
        var ghost = new Button { Content = "شفاف", Classes = { "ghost" } };
        var small = new Button { Content = "صغير", Classes = { "sm" } };
        Show(new StackPanel { Children = { plain, primary, secondary, ghost, small } });

        AssertBrush(plain, "InsCtlBg", plain.Background);
        AssertBrush(plain, "InsCtlBd", plain.BorderBrush);

        var fill = Assert.IsAssignableFrom<ILinearGradientBrush>(primary.Background);
        Assert.Equal(Res(primary, "InsPrimaryColor"), fill.GradientStops[0].Color);
        Assert.Equal(Res(primary, "InsPrimaryDarkColor"), fill.GradientStops[1].Color);
        AssertBrush(primary, "InsOnPrimary", primary.Foreground);
        Assert.Equal(Res(primary, "InsShBrandMd"), Part<Border>(primary, "PART_Shadow").BoxShadow);

        AssertBrush(secondary, "InsCtlBg", secondary.Background);
        AssertBrush(secondary, "InsCtlBd", secondary.BorderBrush);

        AssertBrush(ghost, "InsPrimaryDarker", ghost.Foreground);
        var ghostBorder = ColorOf(ghost.BorderBrush);
        Assert.Equal(Res(ghost, "InsPrimaryColor"), Color.FromRgb(ghostBorder.R, ghostBorder.G, ghostBorder.B));

        Assert.Equal(30, small.Bounds.Height);
    }

    [AvaloniaFact]
    public void Button_hover_deepens_the_secondary_face()
    {
        var secondary = new Button { Content = "ثانوي", Classes = { "secondary" } };
        var window = Show(new StackPanel { Margin = new Thickness(20), Children = { secondary } });

        window.MouseMove(Center(secondary, window));
        Settle();

        Assert.True(secondary.IsPointerOver);
        AssertBrush(secondary, "InsCtlBgHover", secondary.Background);
    }

    [AvaloniaFact]
    public void Button_focus_ring_shows_for_keyboard_focus_only()
    {
        var first = new Button { Content = "أول" };
        var second = new Button { Content = "ثانٍ", Classes = { "primary" } };
        var window = Show(new StackPanel { Margin = new Thickness(20), Spacing = 12, Children = { first, second } });
        var ring = Part<Border>(second, "PART_FocusRing");

        Click(second, window);
        Assert.True(second.IsFocused);
        Assert.Equal(0, ring.Opacity);

        first.Focus(NavigationMethod.Tab);
        window.KeyPress(Key.Tab, RawInputModifiers.None, PhysicalKey.Tab, null);
        window.KeyRelease(Key.Tab, RawInputModifiers.None, PhysicalKey.Tab, null);
        Settle();
        Assert.True(second.IsFocused);
        Assert.Equal(1, ring.Opacity);
        Assert.Equal(Res(second, "InsFocusRing"), ring.BoxShadow);
        Assert.Equal(Res(second, "InsShBrandLg"), Part<Border>(second, "PART_Shadow").BoxShadow);
    }

    [AvaloniaFact]
    public void Button_disabled_dims()
    {
        var button = new Button { Content = "معطّل", IsEnabled = false, Classes = { "primary" } };
        Show(button);
        Assert.Equal(0.5, button.Opacity);
    }

    [AvaloniaFact]
    public void Brand_change_reaches_the_brand_fills()
    {
        var primary = new Button { Content = "أساسي", Classes = { "primary" } };
        var ghost = new Button { Content = "شفاف", Classes = { "ghost" } };
        var check = new CheckBox { IsChecked = true };
        var toggle = new ToggleSwitch { IsChecked = true };
        Show(new StackPanel { Children = { primary, ghost, check, toggle } });
        var teal = Color.Parse("#0F766E");
        try
        {
            InsiyabTheme.SetPrimary(teal);
            Settle();

            Assert.Equal(teal, Res(primary, "InsPrimaryColor"));
            Assert.Equal(teal, Assert.IsAssignableFrom<ILinearGradientBrush>(primary.Background).GradientStops[0].Color);
            var ghostBorder = ColorOf(ghost.BorderBrush);
            Assert.Equal(teal, Color.FromRgb(ghostBorder.R, ghostBorder.G, ghostBorder.B));
            Assert.Equal(teal, Assert.IsAssignableFrom<ILinearGradientBrush>(Part<Border>(check, "CheckFill").Background).GradientStops[0].Color);
            Assert.Equal(teal, Assert.IsAssignableFrom<ILinearGradientBrush>(Part<Border>(toggle, "SwitchKnobBounds").Background).GradientStops[0].Color);
        }
        finally
        {
            InsiyabTheme.SetPrimary(InsiyabTheme.DefaultPrimary);
        }
    }

    [AvaloniaFact]
    public void CheckBox_toggles_by_click_and_keyboard()
    {
        var check = new CheckBox { Content = "تذكّرني" };
        var window = Show(new StackPanel { Margin = new Thickness(20), Children = { check } });

        Part<Border>(check, "NormalRectangle");
        Part<Border>(check, "CheckFill");
        var glyph = Part<ShapePath>(check, "CheckGlyph");

        check.Focus(NavigationMethod.Tab);
        Settle(50);
        Assert.True(Part<Border>(check, "FocusRing").IsVisible);
        PressSpace(window);
        Assert.True(check.IsChecked);
        Assert.Equal(1, Part<Border>(check, "CheckFill").Opacity);
        Assert.Equal(1, glyph.RenderTransform!.Value.M11, 3);

        Click(check, window);
        Assert.False(check.IsChecked);
        Assert.Equal(0, Part<Border>(check, "CheckFill").Opacity);
    }

    [AvaloniaFact]
    public void CheckBox_indeterminate_shows_the_bar()
    {
        var check = new CheckBox { Content = "الكل", IsThreeState = true, IsChecked = null };
        Show(check);
        Assert.Equal(1, Part<Border>(check, "IndeterminateGlyph").Opacity);
        Assert.Equal(1, Part<Border>(check, "CheckFill").Opacity);
    }

    [AvaloniaFact]
    public void ToggleSwitch_toggles_by_click_and_keyboard()
    {
        var toggle = new ToggleSwitch { Content = "الإشعارات", OnContent = "مفعّلة", OffContent = "معطّلة" };
        var window = Show(new StackPanel { Margin = new Thickness(20), Children = { toggle } });

        Part<Panel>(toggle, "PART_MovingKnobs");
        Part<Canvas>(toggle, "PART_SwitchKnob");

        toggle.Focus(NavigationMethod.Tab);
        Settle(50);
        Assert.True(Part<Border>(toggle, "FocusRing").IsVisible);
        PressSpace(window);
        Assert.True(toggle.IsChecked);
        Assert.Equal(1, Part<Border>(toggle, "SwitchKnobBounds").Opacity);
        Assert.Equal(1, Part<ContentPresenter>(toggle, "PART_OnContentPresenter").Opacity);
        Assert.Equal(0, Part<ContentPresenter>(toggle, "PART_OffContentPresenter").Opacity);

        Click(toggle, window);
        Assert.False(toggle.IsChecked);
        Assert.Equal(0, Part<Border>(toggle, "SwitchKnobBounds").Opacity);
    }

    [AvaloniaTheory]
    [InlineData(FlowDirection.LeftToRight)]
    [InlineData(FlowDirection.RightToLeft)]
    public void ToggleSwitch_thumb_starts_on_the_start_side(FlowDirection direction)
    {
        var toggle = new ToggleSwitch { OnContent = null, OffContent = null };
        var window = Show(new StackPanel { Margin = new Thickness(20), Children = { toggle } }, direction);
        var track = Part<Panel>(toggle, "SwitchAreaGrid");
        var knob = Part<Panel>(toggle, "PART_MovingKnobs");

        var offStart = Center(knob, window).X < Center(track, window).X;
        toggle.IsChecked = true;
        Settle();
        var onStart = Center(knob, window).X < Center(track, window).X;

        var ltr = direction == FlowDirection.LeftToRight;
        Assert.Equal(ltr, offStart);
        Assert.Equal(!ltr, onStart);
    }

    [AvaloniaTheory]
    [MemberData(nameof(Ui.Looks), MemberType = typeof(Ui))]
    public void Capture_buttons(string theme, string dir)
    {
        var hover = new Button { Content = "تمرير", Classes = { "secondary" } };
        var focused = new Button { Content = "تركيز", Classes = { "primary" } };
        var page = new StackPanel
        {
            Margin = new Thickness(24),
            Spacing = 18,
            Children =
            {
                Row(new Button { Content = "حفظ", Classes = { "primary" } },
                    new Button { Content = "إلغاء", Classes = { "secondary" } },
                    new Button { Content = "تعديل", Classes = { "ghost" } },
                    new Button { Content = "بلا فئة" },
                    new Button { Content = "رجوع", Classes = { "bare" } }),
                Row(new Button { Content = "نجاح", Classes = { "success" } },
                    new Button { Content = "حذف", Classes = { "danger" } },
                    new Button { Content = "تنبيه", Classes = { "warning" } },
                    new Button { Content = "معلومة", Classes = { "info" } }),
                Row(new Button { Content = "حذف", Classes = { "ghost-danger" } },
                    new Button { Content = "تم", Classes = { "ghost-success" } },
                    new Button { Content = "انتباه", Classes = { "ghost-warning" } },
                    new Button { Content = "تفاصيل", Classes = { "ghost-info" } }),
                Row(new Button { Content = "صغير", Classes = { "primary", "sm" } },
                    new Button { Content = "عادي", Classes = { "primary" } },
                    new Button { Content = "كبير", Classes = { "primary", "lg" } },
                    new Button { Content = "+", Classes = { "secondary", "icon" } },
                    new Button { Content = "معطّل", Classes = { "primary" }, IsEnabled = false }),
                Row(hover, focused),
            },
        };
        var window = Show(page, dir == "rtl" ? FlowDirection.RightToLeft : FlowDirection.LeftToRight, theme == "Dark" ? ThemeVariant.Dark : ThemeVariant.Light, 720, 420);
        window.MouseMove(Center(hover, window));
        focused.Focus(NavigationMethod.Tab);
        Capture(window, $"buttons-{theme}-{dir}");
    }

    [AvaloniaTheory]
    [MemberData(nameof(Ui.Looks), MemberType = typeof(Ui))]
    public void Capture_checks(string theme, string dir)
    {
        var focusedCheck = new CheckBox { Content = "مع تركيز", IsChecked = true };
        var page = new StackPanel
        {
            Margin = new Thickness(24),
            Spacing = 14,
            Children =
            {
                new CheckBox { Content = "غير محدد" },
                new CheckBox { Content = "محدد", IsChecked = true },
                new CheckBox { Content = "جزئي", IsThreeState = true, IsChecked = null },
                new CheckBox { Content = "معطّل", IsChecked = true, IsEnabled = false },
                focusedCheck,
                new ToggleSwitch { OnContent = "مفعّل", OffContent = "معطّل" },
                new ToggleSwitch { OnContent = "مفعّل", OffContent = "معطّل", IsChecked = true },
                new ToggleSwitch { Content = "الإشعارات", OnContent = "تصلك التنبيهات", OffContent = "لن تصلك تنبيهات", IsChecked = true },
                new ToggleSwitch { OnContent = "معطّل", OffContent = "معطّل", IsEnabled = false },
            },
        };
        var window = Show(page, dir == "rtl" ? FlowDirection.RightToLeft : FlowDirection.LeftToRight, theme == "Dark" ? ThemeVariant.Dark : ThemeVariant.Light, 420, 560);
        focusedCheck.Focus(NavigationMethod.Tab);
        Capture(window, $"checks-{theme}-{dir}");
    }

    [AvaloniaFact]
    public void Button_gradient_stays_physical_while_its_content_follows_the_direction()
    {
        var button = new Button { Content = "حفظ", Classes = { "primary" } };
        Show(button, FlowDirection.RightToLeft);

        Assert.Equal(FlowDirection.LeftToRight, Part<Border>(button, "PART_Face").FlowDirection);
        Assert.Equal(FlowDirection.RightToLeft, Part<ContentPresenter>(button, "PART_ContentPresenter").FlowDirection);
    }
}
