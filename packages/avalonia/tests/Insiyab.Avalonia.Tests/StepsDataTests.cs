using System.Collections;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Insiyab.Avalonia.Controls;
using static Insiyab.Avalonia.Tests.Ui;

namespace Insiyab.Avalonia.Tests;

// Steps from data, a marker in place of the number, and the pulse on the current step.
public class StepsDataTests
{
    public sealed class StepRow : INotifyPropertyChanged
    {
        private bool _isDone, _isCurrent;
        public string Name { get; init; } = "";
        public object? Mark { get; init; }
        public bool IsDone { get => _isDone; set { if (_isDone == value) return; _isDone = value; PropertyChanged?.Invoke(this, new(nameof(IsDone))); } }
        public bool IsCurrent { get => _isCurrent; set { if (_isCurrent == value) return; _isCurrent = value; PropertyChanged?.Invoke(this, new(nameof(IsCurrent))); } }
        public event PropertyChangedEventHandler? PropertyChanged;
    }

    private static Steps Bound(IEnumerable rows) => new()
    {
        ItemsSource = rows,
        DisplayMemberBinding = new Binding(nameof(StepRow.Name)),
        IsDoneBinding = new Binding(nameof(StepRow.IsDone)),
        IsCurrentBinding = new Binding(nameof(StepRow.IsCurrent)),
        MarkerBinding = new Binding(nameof(StepRow.Mark)),
    };

    private static List<StepsItem> Items(Steps s) => s.GetRealizedContainers().OfType<StepsItem>().OrderBy(s.IndexFromContainer).ToList();

    private static Steps Pulsing() => new()
    {
        Classes = { "pulse" },
        Items = { new StepsItem { Content = "الأولى", IsDone = true }, new StepsItem { Content = "الثانية", IsCurrent = true }, new StepsItem { Content = "الثالثة" } },
    };

    private static bool Has(Control c, string pseudo) => c.Classes.Contains(pseudo);

    private static bool Gone(WeakReference weak)
    {
        Settle(500);
        for (var i = 0; i < 3; i++)
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
        }
        return !weak.IsAlive;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static WeakReference Shown(Panel host, Func<Control> make)
    {
        var control = make();
        host.Children.Add(control);
        Settle(300);
        host.Children.Remove(control);
        return new WeakReference(control);
    }

    [AvaloniaFact]
    public void Members_set_done_current_and_the_marker()
    {
        var rows = new ObservableCollection<StepRow>
        {
            new() { Name = "الشحن", Mark = "4", IsDone = true },
            new() { Name = "التسليم", Mark = "5", IsCurrent = true },
            new() { Name = "التقييم" },
        };
        var steps = Bound(rows);
        Show(steps, FlowDirection.RightToLeft);
        var items = Items(steps);
        Assert.True(items[0].IsDone && Has(items[0], ":done"));
        Assert.True(items[1].IsCurrent && Has(items[1], ":current"));
        Assert.True(Has(items[0], ":marker"));
        var marker = Part<ContentPresenter>(items[0], "PART_Marker");
        Assert.True(marker.IsVisible);
        Assert.Equal("4", marker.Content);
        AssertBrush(marker, "InsOnPrimaryMid", () => marker.Foreground);
        Assert.False(Part<TextBlock>(items[0], "PART_Number").IsVisible);
        Assert.False(Has(items[2], ":marker"));
        Assert.True(Part<TextBlock>(items[2], "PART_Number").IsVisible);
        Assert.Equal(3, items[2].Number);
        Assert.Contains(items[1].GetVisualDescendants().OfType<TextBlock>(), t => t.Text == "التسليم");
        rows[1].IsCurrent = false;
        rows[1].IsDone = true;
        rows[2].IsCurrent = true;
        Settle(100);
        Assert.True(items[1].IsDone);
        Assert.True(Has(items[2], ":current"));
        Assert.True(Has(items[2], ":after-done"));
    }

    [AvaloniaFact]
    public void A_marker_written_by_hand_replaces_the_number_until_cleared()
    {
        var step = new StepsItem { Content = "الأولى", Marker = "ب" };
        var steps = new Steps { Items = { step, new StepsItem { Content = "الثانية" } } };
        Show(steps);
        Assert.False(Part<TextBlock>(step, "PART_Number").IsVisible);
        Assert.True(Part<ContentPresenter>(step, "PART_Marker").IsVisible);
        step.Marker = null;
        Settle(100);
        Assert.True(Part<TextBlock>(step, "PART_Number").IsVisible);
        Assert.False(Part<ContentPresenter>(step, "PART_Marker").IsVisible);
    }

    [AvaloniaFact]
    public void The_pulse_rings_the_current_step_in_the_brand_of_the_theme()
    {
        var steps = Pulsing();
        var w = Show(steps);
        var items = steps.Items.Cast<StepsItem>().ToList();
        var ring = Part<StepsRing>(items[1], "PART_Pulse");
        Assert.True(ring.IsOn);
        Assert.True(Has(ring, ":ins-seen"));
        Assert.False(Part<StepsRing>(items[0], "PART_Pulse").IsOn);
        Assert.False(Part<StepsRing>(items[2], "PART_Pulse").IsOn);
        var a = ring.Phase;
        Settle(150);
        Assert.NotEqual(a, ring.Phase);
        Assert.InRange(ring.Phase, 0, 1);
        AssertBrush(ring, "InsPrimaryMid", () => ring.Brush);
        w.RequestedThemeVariant = ThemeVariant.Dark;
        Settle(200);
        AssertBrush(ring, "InsPrimaryMid", () => ring.Brush);
        items[1].IsCurrent = false;
        items[2].IsCurrent = true;
        Settle(100);
        Assert.False(ring.IsOn);
        Assert.True(Part<StepsRing>(items[2], "PART_Pulse").IsOn);
    }

    [AvaloniaFact]
    public void No_pulse_without_the_class_or_in_a_still_window()
    {
        var plain = new Steps { Items = { new StepsItem { Content = "أ", IsCurrent = true } } };
        var pulsing = Pulsing();
        var w = Show(new StackPanel { Children = { plain, pulsing } });
        var off = Part<StepsRing>((StepsItem)plain.Items[0]!, "PART_Pulse");
        Assert.False(off.IsOn);
        Settle(200);
        Assert.Equal(0, off.Phase);
        var ring = Part<StepsRing>((StepsItem)pulsing.Items[1]!, "PART_Pulse");
        Motion.SetStill(w, true);
        Settle(200);
        var a = ring.Phase;
        Settle(200);
        Assert.Equal(a, ring.Phase);
        Assert.Equal(0, ring.Phase);
    }

    [AvaloniaFact]
    public void A_pulsing_steps_row_is_let_go_once_removed()
    {
        var host = new StackPanel();
        Show(host);
        Assert.True(Gone(Shown(host, Pulsing)));
    }
}
