using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.VisualTree;
using static Insiyab.Avalonia.Tests.Ui;

namespace Insiyab.Avalonia.Tests;

// Avalonia's DatePicker and TimePicker drums close every way a popover closes: the check, the cross, Escape, a click outside and a second click on the field.
[Collection("Pickers")]
public class PickerCloseTests
{
    private static readonly TimeSpan NineThirty = new(9, 30, 0);
    private static readonly DateTimeOffset Sept30 = new(new DateTime(2026, 9, 30));

    public static TheoryData<string> Dirs => new() { "rtl", "ltr" };

    private static FlowDirection Dir(string dir) => dir == "rtl" ? FlowDirection.RightToLeft : FlowDirection.LeftToRight;

    private static (TemplatedControl picker, Window w) Open(bool time, string dir)
    {
        TemplatedControl picker = time ? new TimePicker { SelectedTime = NineThirty } : new DatePicker { SelectedDate = Sept30 };
        var page = new StackPanel { Margin = new Thickness(60, 40), Width = 360, HorizontalAlignment = HorizontalAlignment.Left };
        page.Children.Add(picker);
        var w = Show(page, Dir(dir), width: 760, height: 760);
        Click(Field(picker), w);
        Assert.True(Pop(picker).IsOpen, "the field's click should open the drum");
        return (picker, w);
    }

    private static Button Field(Control picker) => Part<Button>(picker, "PART_FlyoutButton");

    private static Popup Pop(Control picker) => Part<Popup>(picker, "PART_Popup");

    private static Control Presenter(Control picker) => (Control)Pop(picker).Child!;

    // Turns one wheel away from the starting value, the way a user picks before choosing check or cross.
    private static void Turn(Control picker, bool time)
    {
        var wheel = Part<DateTimePickerPanel>(Presenter(picker), time ? "PART_HourSelector" : "PART_MonthSelector");
        wheel.SelectedValue = time ? 11 : 3;
        Settle(200);
    }

    private static bool Unchanged(Control picker, bool time) =>
        time ? ((TimePicker)picker).SelectedTime == NineThirty : ((DatePicker)picker).SelectedDate == Sept30;

    private static Rect Box(Control c, Visual to)
    {
        var a = c.TranslatePoint(default, to)!.Value;
        var b = c.TranslatePoint(new Point(c.Bounds.Width, c.Bounds.Height), to)!.Value;
        return new Rect(new Point(Math.Min(a.X, b.X), Math.Min(a.Y, b.Y)), new Point(Math.Max(a.X, b.X), Math.Max(a.Y, b.Y)));
    }

    private static void Press(Window w, Key key, PhysicalKey physical)
    {
        w.KeyPress(key, RawInputModifiers.None, physical, null);
        w.KeyRelease(key, RawInputModifiers.None, physical, null);
        Settle(200);
    }

    [AvaloniaTheory]
    [MemberData(nameof(Dirs))]
    public void TimePicker_footer_shows_the_cross_and_the_check(string dir) => FooterShows(true, dir);

    [AvaloniaTheory]
    [MemberData(nameof(Dirs))]
    public void DatePicker_footer_shows_the_cross_and_the_check(string dir) => FooterShows(false, dir);

    private static void FooterShows(bool time, string dir)
    {
        var (picker, w) = Open(time, dir);
        var presenter = Presenter(picker);
        var host = presenter.GetVisualAncestors().OfType<Control>().First(a => a is OverlayPopupHost or PopupRoot);
        var shown = Box(host, w).Intersect(new Rect(w.Bounds.Size));
        foreach (var name in new[] { "PART_DismissButton", "PART_AcceptButton" })
        {
            var b = Part<Button>(presenter, name);
            var box = Box(b, w);
            Assert.True(b.IsEffectivelyVisible, $"{name} hidden");
            Assert.True(b.Bounds.Height >= 40 && b.Bounds.Width > 40, $"{name} is {b.Bounds.Size}, presenter {presenter.Bounds}, host {Box(host, w)}");
            Assert.True(shown.Contains(box), $"{name} at {box} falls outside the popup's visible box {shown}, presenter {presenter.Bounds}");
        }
    }

    [AvaloniaTheory]
    [MemberData(nameof(Dirs))]
    public void TimePicker_check_commits_and_closes(string dir) => CheckCommits(true, dir);

    [AvaloniaTheory]
    [MemberData(nameof(Dirs))]
    public void DatePicker_check_commits_and_closes(string dir) => CheckCommits(false, dir);

    private static void CheckCommits(bool time, string dir)
    {
        var (picker, w) = Open(time, dir);
        Turn(picker, time);
        Click(Part<Button>(Presenter(picker), "PART_AcceptButton"), w);
        Assert.False(Pop(picker).IsOpen);
        if (time) Assert.Equal(11, ((TimePicker)picker).SelectedTime!.Value.Hours);
        else Assert.Equal(3, ((DatePicker)picker).SelectedDate!.Value.Month);
    }

    [AvaloniaTheory]
    [MemberData(nameof(Dirs))]
    public void TimePicker_cross_restores_and_closes(string dir) => CrossRestores(true, dir);

    [AvaloniaTheory]
    [MemberData(nameof(Dirs))]
    public void DatePicker_cross_restores_and_closes(string dir) => CrossRestores(false, dir);

    private static void CrossRestores(bool time, string dir)
    {
        var (picker, w) = Open(time, dir);
        Turn(picker, time);
        Click(Part<Button>(Presenter(picker), "PART_DismissButton"), w);
        Assert.False(Pop(picker).IsOpen);
        Assert.True(Unchanged(picker, time));
    }

    [AvaloniaTheory]
    [MemberData(nameof(Dirs))]
    public void TimePicker_escape_closes_without_committing(string dir) => EscapeCloses(true, dir);

    [AvaloniaTheory]
    [MemberData(nameof(Dirs))]
    public void DatePicker_escape_closes_without_committing(string dir) => EscapeCloses(false, dir);

    private static void EscapeCloses(bool time, string dir)
    {
        var (picker, w) = Open(time, dir);
        Turn(picker, time);
        Press(w, Key.Escape, PhysicalKey.Escape);
        Assert.False(Pop(picker).IsOpen);
        Assert.True(Unchanged(picker, time));
    }

    [AvaloniaTheory]
    [MemberData(nameof(Dirs))]
    public void TimePicker_click_outside_closes_without_committing(string dir) => OutsideCloses(true, dir);

    [AvaloniaTheory]
    [MemberData(nameof(Dirs))]
    public void DatePicker_click_outside_closes_without_committing(string dir) => OutsideCloses(false, dir);

    private static void OutsideCloses(bool time, string dir)
    {
        var (picker, w) = Open(time, dir);
        Turn(picker, time);
        var p = new Point(w.Bounds.Width - 20, w.Bounds.Height - 20);
        w.MouseMove(p);
        w.MouseDown(p, MouseButton.Left);
        w.MouseUp(p, MouseButton.Left);
        Settle();
        Assert.False(Pop(picker).IsOpen);
        Assert.True(Unchanged(picker, time));
    }

    [AvaloniaTheory]
    [MemberData(nameof(Dirs))]
    public void TimePicker_second_click_on_the_field_closes(string dir) => SecondClickCloses(true, dir);

    [AvaloniaTheory]
    [MemberData(nameof(Dirs))]
    public void DatePicker_second_click_on_the_field_closes(string dir) => SecondClickCloses(false, dir);

    private static void SecondClickCloses(bool time, string dir)
    {
        var (picker, w) = Open(time, dir);
        var field = Box(Field(picker), w);
        var drum = Box(Presenter(picker), w);
        Assert.False(drum.Intersects(field), $"the drum {drum} covers the field {field}");
        Click(Field(picker), w);
        Assert.False(Pop(picker).IsOpen);
        Assert.True(Unchanged(picker, time));
    }
}
