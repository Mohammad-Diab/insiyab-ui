using System.Diagnostics;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Styling;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Insiyab.Avalonia.Controls;

using static Insiyab.Avalonia.Tests.Ui;

namespace Insiyab.Avalonia.Tests;

public class InputMotionTests
{
    // The B1 switch that turns motion off; if B1 named its setter differently, this is the one line to change.
    private static void Still(Window w, bool on) => Motion.SetStill(w, on);

    private static List<(long Ms, T Value)> Sample<T>(Window window, int milliseconds, Func<T> read)
    {
        var samples = new List<(long, T)>();
        var clock = Stopwatch.StartNew();
        while (clock.ElapsedMilliseconds < milliseconds)
        {
            window.InvalidateVisual();
            Dispatcher.UIThread.RunJobs();
            AvaloniaHeadlessPlatform.ForceRenderTimerTick();
            samples.Add((clock.ElapsedMilliseconds, read()));
            Thread.Sleep(6);
        }
        return samples;
    }

    private static void Pump(Window window, int milliseconds) => Sample(window, milliseconds, () => 0);

    // A frame taken mid-motion, with no settling first, for the reviewer's eye.
    private static void Snap(Window window, string name)
    {
        window.InvalidateVisual();
        Dispatcher.UIThread.RunJobs();
        AvaloniaHeadlessPlatform.ForceRenderTimerTick();
        window.CaptureRenderedFrame()?.Save(Path.Combine(ShotsDir(), name + ".png"), PngBitmapEncoderOptions.Default);
    }

    private static (NumericUpDown Box, Window Window) Number(FlowDirection dir = FlowDirection.RightToLeft)
    {
        var box = new NumericUpDown { Value = 5, Minimum = 0, Maximum = 10, Width = 180, FormatString = "0" };
        var window = Show(new StackPanel { Margin = new Thickness(20), Children = { box } }, dir);
        return (box, window);
    }

    private static T Named<T>(Visual root, string name) where T : Control =>
        root.GetVisualDescendants().OfType<T>().First(c => c.Name == name);

    private static bool Rolling(NumericUpDown box) => box.Classes.Contains(":rolling");

    [AvaloniaFact]
    public void A_step_rolls_the_old_number_up_and_out_and_the_new_one_in()
    {
        var (box, window) = Number();
        var roll = Named<Panel>(box, "PART_Roll");
        var gone = Named<TextBlock>(box, "PART_RollOut");
        var next = Named<TextBlock>(box, "PART_RollIn");
        var field = Named<TextBox>(box, "PART_TextBox");
        var brand = ColorOf((IBrush)Res(box, "InsToneBrandSoft"));
        var up = Named<RepeatButton>(box, "PART_IncreaseButton");
        var p = Center(up, window);
        window.MouseMove(p);
        window.MouseDown(p, MouseButton.Left);
        window.MouseUp(p, MouseButton.Left);

        Assert.True(roll.IsVisible);
        Assert.True(Rolling(box));
        Assert.Equal("5", gone.Text);
        Assert.Equal("6", next.Text);
        var samples = Sample(window, 240, () => (OutY: gone.RenderTransform?.Value.M32 ?? 0, OutO: gone.Opacity, InY: next.RenderTransform?.Value.M32 ?? 0, InO: next.Opacity, Ink: (next.Foreground as ISolidColorBrush)?.Color, Hidden: field.Opacity == 0));
        Snap(window, "input-roll-mid");
        Assert.All(samples, s => Assert.True(s.Value.Hidden));
        Assert.Contains(samples, s => s.Value.OutY < -2 && s.Value.OutO < 0.9);
        Assert.Contains(samples, s => s.Value.InY > 5);
        Assert.All(samples.Where(s => s.Ms < 120), s => Assert.Equal(brand, s.Value.Ink));
        Assert.All(samples, s => Assert.InRange(s.Value.InO, 0, 1));

        Pump(window, 600);
        Snap(window, "input-roll-end");
        Assert.False(roll.IsVisible);
        Assert.False(Rolling(box));
        Assert.Equal(1, field.Opacity);
        Assert.Equal("6", box.Text);
        AssertBrush(next, "InsText1", next.Foreground);
    }

    [AvaloniaFact]
    public void Stepping_down_rolls_downward()
    {
        var (box, window) = Number(FlowDirection.LeftToRight);
        var gone = Named<TextBlock>(box, "PART_RollOut");
        var next = Named<TextBlock>(box, "PART_RollIn");
        var down = Named<RepeatButton>(box, "PART_DecreaseButton");
        var p = Center(down, window);
        // Every value the animations set is kept, as under load a frame read at 90ms can already be in the spring's overshoot.
        var outs = new List<double>();
        var ins = new List<double>();
        gone.PropertyChanged += (_, e) => { if (e.Property == Visual.RenderTransformProperty) outs.Add(gone.RenderTransform?.Value.M32 ?? 0); };
        next.PropertyChanged += (_, e) => { if (e.Property == Visual.RenderTransformProperty) ins.Add(next.RenderTransform?.Value.M32 ?? 0); };
        window.MouseMove(p);
        window.MouseDown(p, MouseButton.Left);
        window.MouseUp(p, MouseButton.Left);
        Pump(window, 300);
        Assert.Equal("4", box.Text);
        Assert.True(outs.Max() > 2, $"out y {outs.Max()}");
        Assert.True(ins[0] < 0, $"in y {ins[0]}");
    }

    [AvaloniaFact]
    public void The_rolled_number_lands_where_the_text_is()
    {
        var (box, window) = Number();
        var next = Named<TextBlock>(box, "PART_RollIn");
        var field = Named<TextBox>(box, "PART_TextBox");
        var presenter = field.GetVisualDescendants().OfType<TextPresenter>().First();
        next.Text = "5";
        Named<Panel>(box, "PART_Roll").IsVisible = true;
        Settle(100);
        var textMiddle = presenter.TranslatePoint(new Point(0, presenter.Bounds.Height / 2), box)!.Value.Y;
        var rollMiddle = next.TranslatePoint(new Point(0, next.Bounds.Height / 2), box)!.Value.Y;
        Assert.Equal(textMiddle, rollMiddle, 0);
        Assert.Equal(presenter.FontSize, next.FontSize);
        Assert.Equal(presenter.FontWeight, next.FontWeight);
    }

    [AvaloniaFact]
    public void Quick_repeated_steps_just_count()
    {
        var (box, window) = Number();
        var roll = Named<Panel>(box, "PART_Roll");
        var field = Named<TextBox>(box, "PART_TextBox");
        field.Focus();
        Settle(100);
        // The clock stands still, so the second step is quick however long the first one takes under load.
        var real = NumberRoll.Now;
        var at = real();
        NumberRoll.Now = () => at;
        try
        {
            window.KeyPress(Key.Up, RawInputModifiers.None, PhysicalKey.ArrowUp, null);
            window.KeyRelease(Key.Up, RawInputModifiers.None, PhysicalKey.ArrowUp, null);
            Assert.True(roll.IsVisible);
            window.KeyPress(Key.Up, RawInputModifiers.None, PhysicalKey.ArrowUp, null);
            window.KeyRelease(Key.Up, RawInputModifiers.None, PhysicalKey.ArrowUp, null);
        }
        finally { NumberRoll.Now = real; }
        Pump(window, 40);
        Assert.False(roll.IsVisible);
        Assert.False(Rolling(box));
        Assert.Equal("7", box.Text);
    }

    [AvaloniaFact]
    public void A_still_window_does_not_roll()
    {
        var (box, window) = Number();
        Still(window, true);
        Click(Named<RepeatButton>(box, "PART_IncreaseButton"), window);
        Assert.False(Named<Panel>(box, "PART_Roll").IsVisible);
        Assert.Equal("6", box.Text);
    }

    [AvaloniaFact]
    public void The_stepper_buttons_dip_deeper()
    {
        var (box, window) = Number();
        var up = Named<RepeatButton>(box, "PART_IncreaseButton");
        var part = up.GetVisualDescendants().OfType<Panel>().First(c => c.Name == "PART_Press");
        var p = Center(up, window);
        window.MouseMove(p);
        Pump(window, 200);
        window.MouseDown(p, MouseButton.Left);
        window.MouseUp(p, MouseButton.Left);
        var samples = Sample(window, 400, () => part.RenderTransform?.Value.M11 ?? 1);
        Assert.InRange(samples.Min(s => s.Value), 0.91, 0.935);
        Assert.Equal(1, samples[^1].Value, 3);
    }

    [AvaloniaFact]
    public void The_select_list_drops_in_4px_as_it_fades()
    {
        var combo = new ComboBox { ItemsSource = new[] { "الرياض", "عمّان", "القاهرة" }, Width = 220 };
        var window = Show(new StackPanel { Margin = new Thickness(20), Children = { combo } }, FlowDirection.RightToLeft);
        combo.IsDropDownOpen = true;
        Settle(300);
        combo.IsDropDownOpen = false;
        Settle(300);
        combo.IsDropDownOpen = true;
        Border? border = null;
        var samples = Sample(window, 400, () =>
        {
            border ??= combo.GetVisualDescendants().OfType<Popup>().FirstOrDefault()?.Child as Border;
            return border is null ? (Y: double.NaN, O: double.NaN) : (Y: border.RenderTransform?.Value.M32 ?? 0, O: border.Opacity);
        });
        var early = samples.First(s => !double.IsNaN(s.Value.Y));
        Assert.True(early.Value.Y < -1, $"at {early.Ms}ms the list is at {early.Value.Y}");
        Assert.True(early.Value.O < 0.8, $"at {early.Ms}ms the list is at opacity {early.Value.O}");
        Assert.InRange(samples.Min(s => s.Value.Y), -4.01, -1);
        Assert.Equal(0, samples[^1].Value.Y, 2);
        Assert.Equal(1, samples[^1].Value.O, 2);
    }

    [AvaloniaFact]
    public void The_autocomplete_list_drops_in_too()
    {
        var auto = new AutoCompleteBox { ItemsSource = new[] { "الرياض", "الرباط", "القاهرة" }, Width = 220, MinimumPrefixLength = 1 };
        var window = Show(new StackPanel { Margin = new Thickness(20), Children = { auto } }, FlowDirection.RightToLeft);
        Named<TextBox>(auto, "PART_TextBox").Focus();
        Settle(100);
        var popup = auto.GetVisualDescendants().OfType<Popup>().First();
        var border = Assert.IsType<Border>(popup.Child);
        window.KeyTextInput("الر");
        Settle(300);
        auto.IsDropDownOpen = false;
        auto.Text = "";
        Settle(300);
        // Every value the animation sets is kept, as under load the first sampled frame can come after most of its 220ms.
        var moves = new List<double>();
        border.PropertyChanged += (_, e) => { if (e.Property == Visual.RenderTransformProperty) moves.Add(border.RenderTransform?.Value.M32 ?? 0); };
        window.KeyTextInput("الر");
        Pump(window, 500);
        Assert.True(auto.IsDropDownOpen);
        Assert.True(popup.IsOpen);
        Assert.InRange(moves.Min(), -4.01, -1);
        Assert.Equal(0, border.RenderTransform?.Value.M32 ?? 0, 2);
    }
}
