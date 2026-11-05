using Avalonia;
using Avalonia.Controls;
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

public class ComboRoomTests
{
    private static FlowDirection Dir(string dir) => dir == "rtl" ? FlowDirection.RightToLeft : FlowDirection.LeftToRight;

    // A control's box in window pixels, whichever way the window flows.
    private static Rect Box(Visual c, Visual to)
    {
        var a = c.TranslatePoint(default, to)!.Value;
        var b = c.TranslatePoint(new Point(c.Bounds.Width, c.Bounds.Height), to)!.Value;
        return new Rect(new Point(Math.Min(a.X, b.X), Math.Min(a.Y, b.Y)), new Point(Math.Max(a.X, b.X), Math.Max(a.Y, b.Y)));
    }

    private static Border Card(ComboBox c) => (Border)((Border)Part<Popup>(c, "PART_Popup").Child!).Child!;

    private static ComboBox Combo() => new() { Items = { "الأول", "الثاني", "الثالث" }, SelectedIndex = 0, Width = 220 };

    [AvaloniaTheory]
    [InlineData("rtl")]
    [InlineData("ltr")]
    public void A_select_list_keeps_room_for_its_shadow_and_hangs_under_its_field(string dir)
    {
        var combo = Combo();
        var w = Show(new StackPanel { Margin = new Thickness(40), Children = { combo } }, Dir(dir));
        combo.IsDropDownOpen = true;
        Settle();
        var room = (Border)Part<Popup>(combo, "PART_Popup").Child!;
        Assert.Equal(new Thickness(40, 16, 40, 64), room.Padding);
        var card = Card(combo);
        Assert.Equal("PopupBorder", card.Name);
        var c = Box(card, w);
        var f = Box(combo, w);
        Assert.True(c.Width >= 219.5, $"width {c.Width}");
        Assert.InRange(c.Top - f.Bottom, 5, 7);
        if (dir == "rtl") Assert.InRange(c.Right - f.Right, -1, 1);
        else Assert.InRange(c.Left - f.Left, -1, 1);
        Capture(w, $"select-room-{dir}");
    }

    [AvaloniaFact]
    public void A_press_in_the_room_closes_the_list_without_choosing()
    {
        var combo = Combo();
        var w = Show(new StackPanel { Margin = new Thickness(40), Children = { combo } }, FlowDirection.RightToLeft);
        combo.IsDropDownOpen = true;
        Settle();
        var c = Box(Card(combo), w);
        var p = new Point(c.Center.X, c.Bottom + 30);
        w.MouseMove(p);
        w.MouseDown(p, MouseButton.Left);
        w.MouseUp(p, MouseButton.Left);
        Settle();
        Assert.False(combo.IsDropDownOpen);
        Assert.Equal(0, combo.SelectedIndex);
    }
}
