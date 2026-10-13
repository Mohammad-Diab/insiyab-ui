using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Headless.XUnit;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.VisualTree;
using Insiyab.Avalonia.Controls;
using static Insiyab.Avalonia.Tests.Ui;

namespace Insiyab.Avalonia.Tests;

// A field's message reads in the direction of its first letter, as the web's dir="auto": Arabic stays right to left on a left-to-right page.
public class ErrorDirectionTests
{
    private const string Arabic = "هذا الرقم غير مسجل.";
    private const string English = "Enter all 6 digits.";

    private static FlowDirection Dir(string dir) => dir == "rtl" ? FlowDirection.RightToLeft : FlowDirection.LeftToRight;

    private static TextBlock MessageIn(Visual root, string text) =>
        root.GetVisualDescendants().OfType<TextBlock>().First(t => t.Text == text && t.IsEffectivelyVisible);

    private static Viewbox MarkBeside(TextBlock message) =>
        message.GetVisualAncestors().OfType<DockPanel>().First().GetVisualDescendants().OfType<Viewbox>().First();

    private static double X(Visual v, Window w) => v.TranslatePoint(new Point(v.Bounds.Width / 2, 0), w)!.Value.X;

    [AvaloniaFact]
    public void TextDirection_follows_the_first_letter()
    {
        Assert.Equal(FlowDirection.RightToLeft, TextDirection.Of(Arabic));
        Assert.Equal(FlowDirection.RightToLeft, TextDirection.Of("6 أرقام"));
        Assert.Equal(FlowDirection.LeftToRight, TextDirection.Of(English));
        Assert.Equal(FlowDirection.LeftToRight, TextDirection.Of("12 digits, ٣ أرقام"));
        Assert.Null(TextDirection.Of("123."));
        Assert.Null(TextDirection.Of(""));
        Assert.Equal(FlowDirection.RightToLeft, TextDirection.Auto.Convert(new Exception(Arabic), typeof(FlowDirection), null, System.Globalization.CultureInfo.InvariantCulture));
        Assert.Same(BindingOperations.DoNothing, TextDirection.Auto.Convert("123", typeof(FlowDirection), null, System.Globalization.CultureInfo.InvariantCulture));
    }

    [AvaloniaTheory]
    [InlineData("ltr", Arabic, "rtl")]
    [InlineData("rtl", English, "ltr")]
    [InlineData("rtl", Arabic, "rtl")]
    [InlineData("ltr", "123", "ltr")]
    [InlineData("rtl", "123", "rtl")]
    public void Field_message_takes_its_own_direction(string page, string text, string reads)
    {
        var box = new TextBox { FlowDirection = FlowDirection.LeftToRight };
        var w = Show(new StackPanel { Width = 420, Children = { box } }, Dir(page));
        DataValidationErrors.SetErrors(box, [text]);
        Settle();
        var message = MessageIn(box, text);
        Assert.Equal(Dir(reads), message.FlowDirection);
        var mark = MarkBeside(message);
        if (reads == "rtl") Assert.True(X(mark, w) > X(message, w));
        else Assert.True(X(mark, w) < X(message, w));
        w.Close();
    }

    [AvaloniaTheory]
    [InlineData("ltr", Arabic, "rtl")]
    [InlineData("rtl", English, "ltr")]
    [InlineData("ltr", "123", "ltr")]
    [InlineData("rtl", "123", "rtl")]
    public void Error_line_takes_its_own_direction(string page, string text, string reads)
    {
        var line = new ContentControl { Content = text, HorizontalAlignment = HorizontalAlignment.Stretch };
        line.Classes.Add("error");
        var w = Show(new StackPanel { Width = 420, Children = { line } }, Dir(page));
        var message = MessageIn(line, text);
        Assert.Equal(Dir(reads), message.FlowDirection);
        var mark = MarkBeside(message);
        if (reads == "rtl") Assert.True(X(mark, w) > X(message, w));
        else Assert.True(X(mark, w) < X(message, w));
        w.Close();
    }

    [AvaloniaFact]
    public void Otp_message_takes_its_own_direction()
    {
        var otp = new OtpBox();
        var w = Show(new StackPanel { Width = 420, Children = { otp } }, FlowDirection.LeftToRight);
        DataValidationErrors.SetErrors(otp, ["الرمز غير صحيح."]);
        Settle();
        var message = MessageIn(otp, "الرمز غير صحيح.");
        Assert.Equal(FlowDirection.RightToLeft, message.FlowDirection);
        Assert.True(X(MarkBeside(message), w) > X(message, w));
        w.Close();
    }

    [AvaloniaTheory]
    [InlineData("rtl")]
    [InlineData("ltr")]
    public void Otp_message_spans_the_field_and_the_block_keeps_the_start(string page)
    {
        var otp = new OtpBox();
        var w = Show(new StackPanel { Width = 420, Children = { otp } }, Dir(page));
        DataValidationErrors.SetErrors(otp, ["الرمز غير صحيح."]);
        Settle();
        Assert.Equal(420, otp.Bounds.Width);
        Assert.Equal(Dir(page), otp.GetVisualDescendants().OfType<DataValidationErrors>().First().FlowDirection);
        var block = Part<Border>(otp, "PART_Cells").GetVisualAncestors().OfType<Panel>().First(p => p.Height == 52);
        Assert.Equal(FlowDirection.LeftToRight, block.FlowDirection);
        Assert.Equal(0, block.Bounds.X);
        Assert.True(block.Bounds.Width < 300);
        Assert.Equal(420, MessageIn(otp, "الرمز غير صحيح.").GetVisualAncestors().OfType<ItemsControl>().First().Bounds.Width);
        Capture(w, $"errors-otp-{page}");
        w.Close();
    }
}
