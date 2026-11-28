using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.VisualTree;
using Insiyab.Avalonia.Controls;
using static Insiyab.Avalonia.Tests.Ui;

namespace Insiyab.Avalonia.Tests;

// A Linux window that draws its own frame (WindowFrame) holds its layers inside a see-through shadow margin that clips them. Avalonia gives a popup's anchor in the window's coordinates and sets the popup in the overlay layer's, so a pop near the window's edge was cut by the margin and stood off its trigger by the margin. A pop stays whole inside the frame, beside its trigger and at the pointer; with no frame (Windows) it is where it always was.
public class PopFrameTests
{
    private const double Margin = 13; // the shadow's room (12) and the hairline

    private static Rect Screen(Visual v)
    {
        var a = v.PointToScreen(default);
        var b = v.PointToScreen(new Point(v.Bounds.Width, v.Bounds.Height));
        return new Rect(new Point(Math.Min(a.X, b.X), Math.Min(a.Y, b.Y)), new Point(Math.Max(a.X, b.X), Math.Max(a.Y, b.Y)));
    }

    private static FlowDirection Dir(string dir) => dir == "rtl" ? FlowDirection.RightToLeft : FlowDirection.LeftToRight;

    // The window with its frame's margin as Linux draws it (the frame sets it only on Linux; headless it is set by hand).
    private static (Window Window, Rect Inside) Framed(Control content, string dir, double margin)
    {
        var w = Show(content, Dir(dir), width: 640, height: 420);
        var frame = Part<WindowFrame>(w, "PART_InsFrame");
        frame.Padding = new Thickness(margin);
        Settle(200);
        return (w, Screen(frame).Deflate(margin));
    }

    // The card the user sees: the box round the content, inside the presenter's shadow room.
    private static Rect Card(Control content) => Screen((Visual)content.GetVisualAncestors().OfType<ScrollViewer>().First().GetVisualParent()!);

    private static void Near(double expected, double actual, string what) => Assert.True(Math.Abs(actual - expected) <= 1.5, $"{what}: expected {expected}, got {actual}");

    [AvaloniaTheory]
    [InlineData("rtl", Margin)]
    [InlineData("ltr", Margin)]
    [InlineData("rtl", 0)]
    [InlineData("ltr", 0)]
    public void A_card_opened_at_the_window_edge_stays_whole_inside_the_frame_and_its_gap_from_the_trigger(string dir, double margin)
    {
        var body = new Border { Width = 300, Height = 120, Background = Brushes.Gray };
        // the trigger in the window's bottom right corner, whichever way the page reads
        var button = new Button
        {
            Content = "x", Width = 44, Height = 44, VerticalAlignment = VerticalAlignment.Bottom,
            HorizontalAlignment = dir == "rtl" ? HorizontalAlignment.Left : HorizontalAlignment.Right,
            Flyout = new PopCard { Content = body },
        };
        var (_, inside) = Framed(new Panel { Children = { button } }, dir, margin);
        button.Flyout!.ShowAt(button);
        Settle();
        var card = Card(body);
        var at = Screen(button);
        Assert.True(card.Right <= inside.Right - Pop.Edge + 1.5 && card.Left >= inside.Left + Pop.Edge - 1.5, $"card {card} inside {inside}");
        Assert.True(card.Bottom <= inside.Bottom && card.Top >= inside.Top, $"card {card} inside {inside}");
        Near(inside.Right - Pop.Edge, card.Right, "slid to the frame's edge");
        Near(at.Top - Pop.Gap, card.Bottom, "above the trigger by the gap");
        button.Flyout.Hide();
        Settle(100);
    }

    [AvaloniaTheory]
    [InlineData("rtl")]
    [InlineData("ltr")]
    public void A_menu_opened_at_the_pointer_opens_there_inside_the_frame(string dir)
    {
        var first = new MenuItem { Header = "نسخ" };
        var menu = new PopMenu { Items = { first, new MenuItem { Header = "لصق" } } };
        var target = new Border { Width = 300, Height = 200, Background = Brushes.Transparent };
        var (w, _) = Framed(new StackPanel { Margin = new Thickness(100), Children = { target } }, dir, Margin);
        var p = Center(target, w);
        w.MouseMove(p);
        Settle(100);
        menu.ShowAt(target, showAtPointer: true);
        Settle();
        Assert.True(menu.IsOpen);
        var card = Card(first);
        var pointer = w.PointToScreen(p);
        Near(pointer.Y + Pop.Gap, card.Top, "its top under the pointer");
        Near(pointer.X, dir == "rtl" ? card.Right : card.Left, "its start edge at the pointer");
        menu.Hide();
        Settle(100);
    }
}
