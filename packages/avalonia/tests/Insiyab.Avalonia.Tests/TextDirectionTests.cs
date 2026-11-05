using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Headless.XUnit;
using Avalonia.Layout;
using Avalonia.Media;
using Insiyab.Avalonia.Controls;
using static Insiyab.Avalonia.Tests.Ui;

namespace Insiyab.Avalonia.Tests;

public class TextDirectionTests
{
    [AvaloniaFact]
    public void The_converter_gives_a_text_its_first_letter_direction()
    {
        var c = TextDirection.Auto;
        Assert.Equal(FlowDirection.LeftToRight, c.Convert("report.pdf", typeof(FlowDirection), null, CultureInfo.InvariantCulture));
        Assert.Equal(FlowDirection.RightToLeft, c.Convert("2024 تقرير", typeof(FlowDirection), null, CultureInfo.InvariantCulture));
        Assert.Same(BindingOperations.DoNothing, c.Convert("2024", typeof(FlowDirection), null, CultureInfo.InvariantCulture));
        Assert.Equal(FlowDirection.LeftToRight, TextDirection.Of("12 apples"));
        Assert.Null(TextDirection.Of("١٢٣"));
    }

    [AvaloniaFact]
    public void A_text_block_follows_its_own_text()
    {
        var name = new TextBlock { Text = "quarterly-report.pdf", HorizontalAlignment = HorizontalAlignment.Left };
        TextDirection.SetIsAuto(name, true);
        Show(new StackPanel { Width = 300, Children = { name } }, FlowDirection.RightToLeft);
        Assert.Equal(FlowDirection.LeftToRight, name.FlowDirection);

        name.Text = "التقرير الفصلي";
        Assert.Equal(FlowDirection.RightToLeft, name.FlowDirection);

        name.Text = "2024";
        Assert.Equal(FlowDirection.RightToLeft, name.FlowDirection);

        name.Text = "notes";
        TextDirection.SetIsAuto(name, false);
        Assert.Equal(FlowDirection.RightToLeft, name.FlowDirection);
    }

    [AvaloniaFact]
    public void A_text_with_no_letters_takes_the_page_direction()
    {
        var name = new TextBlock { Text = "notes" };
        TextDirection.SetIsAuto(name, true);
        var page = new StackPanel { Children = { name } };
        Show(page, FlowDirection.RightToLeft);
        Assert.Equal(FlowDirection.LeftToRight, name.FlowDirection);
        name.Text = "";
        Assert.Equal(FlowDirection.RightToLeft, name.FlowDirection);
        page.FlowDirection = FlowDirection.LeftToRight;
        Assert.Equal(FlowDirection.LeftToRight, name.FlowDirection);
    }
}
