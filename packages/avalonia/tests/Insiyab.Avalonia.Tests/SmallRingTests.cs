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

public class SmallRingTests
{
    [AvaloniaFact]
    public void A_small_ring_is_24px_with_no_label()
    {
        var ring = new ProgressRing { Classes = { "sm" }, Value = 45 };
        Show(new StackPanel { Margin = new Thickness(40), Children = { ring } }, FlowDirection.RightToLeft);
        Assert.Equal(24, ring.Bounds.Width, 0.5);
        Assert.Equal(24, ring.Bounds.Height, 0.5);
        Assert.Equal(3, ring.Thickness);
        Assert.False(Part<TextBlock>(ring, "PART_Label").IsVisible);
    }

    [AvaloniaFact]
    public void A_label_that_does_not_fit_the_ring_hides()
    {
        var tight = new ProgressRing { Width = 22, Height = 22, Thickness = 3, Value = 45 };
        var roomy = new ProgressRing { Value = 45 };
        Show(new StackPanel { Margin = new Thickness(40), Spacing = 12, Children = { tight, roomy } }, FlowDirection.RightToLeft);
        Assert.Contains(":crowded", tight.Classes);
        Assert.Equal(0, Part<TextBlock>(tight, "PART_Label").Opacity);
        Assert.DoesNotContain(":crowded", roomy.Classes);
        Assert.Equal(1, Part<TextBlock>(roomy, "PART_Label").Opacity);
    }
}
