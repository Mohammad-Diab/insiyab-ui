using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.VisualTree;
using Insiyab.Avalonia.Controls;
using static Insiyab.Avalonia.Tests.Ui;

namespace Insiyab.Avalonia.Tests;

public class ChoiceDescriptionTests
{
    private static FlowDirection Dir(string dir) => dir == "rtl" ? FlowDirection.RightToLeft : FlowDirection.LeftToRight;

    // A control's box in window pixels, whichever way the window flows.
    private static Rect Box(Visual c, Visual to)
    {
        var a = c.TranslatePoint(default, to)!.Value;
        var b = c.TranslatePoint(new Point(c.Bounds.Width, c.Bounds.Height), to)!.Value;
        return new Rect(new Point(Math.Min(a.X, b.X), Math.Min(a.Y, b.Y)), new Point(Math.Max(a.X, b.X), Math.Max(a.Y, b.Y)));
    }

    private static void AssertUnderLabel(ToggleButton box, Window w, string dir)
    {
        var desc = Part<ContentPresenter>(box, "PART_Desc");
        var label = Part<ContentPresenter>(box, "PART_ContentPresenter");
        Assert.True(desc.IsVisible);
        var d = Box(desc, w);
        var l = Box(label, w);
        Assert.True(d.Top >= l.Bottom - 0.5, $"desc top {d.Top}, label bottom {l.Bottom}");
        if (dir == "rtl") Assert.InRange(d.Right - l.Right, -1, 1);
        else Assert.InRange(d.Left - l.Left, -1, 1);
        AssertBrush(desc, "InsText2", () => desc.Foreground);
        Assert.Equal((double)Res(desc, "InsFsXs"), desc.FontSize);
    }

    [AvaloniaTheory]
    [InlineData("rtl")]
    [InlineData("ltr")]
    public void A_check_box_shows_its_description_under_the_label(string dir)
    {
        var box = new CheckBox { Content = "تذكرني" };
        Choice.SetDescription(box, "يبقى الحساب مفتوحًا على هذا الجهاز.");
        var w = Show(new StackPanel { Margin = new Thickness(40), Children = { box } }, Dir(dir));
        AssertUnderLabel(box, w, dir);
        Capture(w, $"check-description-{dir}");
    }

    [AvaloniaTheory]
    [InlineData("rtl")]
    [InlineData("ltr")]
    public void A_radio_button_shows_its_description_under_the_label(string dir)
    {
        var box = new RadioButton { Content = "تذكرني" };
        Choice.SetDescription(box, "يبقى الحساب مفتوحًا على هذا الجهاز.");
        var w = Show(new StackPanel { Margin = new Thickness(40), Children = { box } }, Dir(dir));
        AssertUnderLabel(box, w, dir);
    }

    [AvaloniaFact]
    public void Without_a_description_a_check_box_keeps_its_height()
    {
        var plain = new CheckBox { Content = "أ" };
        var other = new CheckBox { Content = "أ" };
        Choice.SetDescription(other, "وصف.");
        Show(new StackPanel { Margin = new Thickness(40), Spacing = 12, Children = { plain, other } }, FlowDirection.RightToLeft);
        Assert.False(Part<ContentPresenter>(plain, "PART_Desc").IsVisible);
        var single = new CheckBox { Content = "أ" };
        Show(new StackPanel { Margin = new Thickness(40), Children = { single } }, FlowDirection.RightToLeft);
        Assert.Equal(single.Bounds.Height, plain.Bounds.Height, 0.5);
        Assert.True(other.Bounds.Height > plain.Bounds.Height);
    }
}
