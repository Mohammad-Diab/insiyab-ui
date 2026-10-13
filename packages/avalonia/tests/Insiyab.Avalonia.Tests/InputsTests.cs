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

public class InputsTests
{
    [AvaloniaFact]
    public void TextBox_gets_the_field_look()
    {
        var box = new TextBox { PlaceholderText = "الاسم" };
        Show(new StackPanel { Margin = new Thickness(20), Children = { box } });

        var border = Part<Border>(box, "PART_BorderElement");
        Part<TextPresenter>(box, "PART_TextPresenter");
        Assert.Equal(46, box.Bounds.Height);
        AssertBrush(box, "InsCtlBg", border.Background);
        AssertBrush(box, "InsCtlBd", border.BorderBrush);
        Assert.Equal(Res(box, "InsRMd"), border.CornerRadius);
        AssertBrush(box, "InsText3", Part<TextBlock>(box, "PART_Placeholder").Foreground);
    }

    [AvaloniaFact]
    public void TextBox_focus_glows()
    {
        var box = new TextBox();
        Show(new StackPanel { Margin = new Thickness(20), Children = { box } });

        box.Focus();
        Settle();

        AssertBrush(box, "InsGlassStrong", Part<Border>(box, "PART_BorderElement").Background);
        Assert.Equal(Res(box, "InsGlow"), Part<Border>(box, "PART_Shadow").BoxShadow);
    }

    [AvaloniaFact]
    public void TextBox_error_turns_red_and_shows_the_message()
    {
        var box = new TextBox { Text = "x" };
        Show(new StackPanel { Margin = new Thickness(20), Children = { box } });

        DataValidationErrors.SetError(box, new Exception("هذا الحقل مطلوب"));
        Settle();

        Assert.Contains(":error", box.Classes);
        var border = Part<Border>(box, "PART_BorderElement");
        AssertBrush(box, "InsToneBadBd", border.BorderBrush);
        AssertBrush(box, "InsToneBadBg", border.Background);
        var message = box.GetVisualDescendants().OfType<TextBlock>().Single(t => t.Text == "هذا الحقل مطلوب");
        Assert.True(message.IsEffectivelyVisible);
    }

    [AvaloniaFact]
    public void Reveal_button_toggles_RevealPassword()
    {
        var box = new TextBox { PasswordChar = '•', Text = "secret", Classes = { "revealPasswordButton" } };
        var window = Show(new StackPanel { Margin = new Thickness(20), Children = { box } });

        var eye = box.GetVisualDescendants().OfType<ToggleButton>().Single();
        Assert.False(box.RevealPassword);
        Click(eye, window);
        Assert.True(box.RevealPassword);
        Assert.True(eye.IsChecked);
        Click(eye, window);
        Assert.False(box.RevealPassword);
    }

    [AvaloniaTheory]
    [InlineData(FlowDirection.LeftToRight, FlowDirection.LeftToRight, true)]
    [InlineData(FlowDirection.RightToLeft, FlowDirection.LeftToRight, true)]
    [InlineData(FlowDirection.RightToLeft, FlowDirection.RightToLeft, false)]
    [InlineData(FlowDirection.LeftToRight, FlowDirection.RightToLeft, false)]
    public void Reveal_button_sits_on_the_end_side(FlowDirection windowDirection, FlowDirection boxDirection, bool onRight)
    {
        var box = new TextBox { PasswordChar = '•', Text = "secret", Width = 300, FlowDirection = boxDirection, Classes = { "revealPasswordButton" } };
        var window = Show(new StackPanel { Margin = new Thickness(20), Children = { box } }, windowDirection);

        var eye = box.GetVisualDescendants().OfType<ToggleButton>().Single();
        var eyeX = Center(eye, window).X;
        var boxX = Center(box, window).X;
        Assert.Equal(onRight, eyeX > boxX);
    }

    [AvaloniaTheory]
    [MemberData(nameof(Ui.Looks), MemberType = typeof(Ui))]
    public void Capture_fields(string theme, string dir)
    {
        var focused = new TextBox { PlaceholderText = "اكتب هنا" };
        var error = new TextBox { Text = "abc" };
        var page = new StackPanel
        {
            Margin = new Thickness(24),
            Spacing = 16,
            Width = 420,
            HorizontalAlignment = HorizontalAlignment.Left,
            Children =
            {
                new TextBox { PlaceholderText = "الاسم الكامل" },
                new TextBox { Text = "نص مكتوب في الحقل" },
                new TextBox { Text = "user@example.com", FlowDirection = FlowDirection.LeftToRight },
                new TextBox { Text = "secret123", PasswordChar = '•', FlowDirection = FlowDirection.LeftToRight, Classes = { "revealPasswordButton" } },
                new TextBox { Text = "كلمة سر", PasswordChar = '•', Classes = { "revealPasswordButton" } },
                focused,
                error,
                new TextBox { Text = "معطّل", IsEnabled = false },
            },
        };
        var window = Show(page, dir == "rtl" ? FlowDirection.RightToLeft : FlowDirection.LeftToRight, theme == "Dark" ? ThemeVariant.Dark : ThemeVariant.Light, 520, 640);
        DataValidationErrors.SetErrors(error, ["هذه القيمة غير صحيحة"]);
        focused.Focus();
        Capture(window, $"fields-{theme}-{dir}");
    }

    [AvaloniaFact]
    public void An_error_under_an_ltr_field_reads_in_the_page_direction()
    {
        var field = new TextBox { Text = "name@example", FlowDirection = FlowDirection.LeftToRight };
        DataValidationErrors.SetErrors(field, ["أدخل بريدًا صحيحًا."]);
        Show(new StackPanel { Children = { field } }, FlowDirection.RightToLeft);

        var message = field.GetVisualDescendants().OfType<TextBlock>().First(t => t.Text == "أدخل بريدًا صحيحًا.");
        Assert.Equal(FlowDirection.RightToLeft, message.FlowDirection);
    }
}
