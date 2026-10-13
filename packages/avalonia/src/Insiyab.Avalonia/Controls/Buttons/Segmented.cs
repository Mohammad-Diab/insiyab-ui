using Avalonia;
using Avalonia.Animation.Easings;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Threading;

namespace Insiyab.Avalonia.Controls;

/// <summary>The web's <c>.ins-seg</c>: one choice out of a few short ones, a raised face in a recessed track that flies to the segment you pick. Items are <see cref="SegmentedItem"/>s (or any content, wrapped in one); <c>SelectionChanged</c> is the web's <c>ins:seg</c>.</summary>
[TemplatePart("PART_Flight", typeof(Border))]
public class Segmented : SelectingItemsControl
{
    private static readonly FuncTemplate<Panel?> DefaultPanel =
        new(() => new StackPanel { Orientation = Orientation.Horizontal, Spacing = 3 });

    private Border? _flight;

    static Segmented()
    {
        SelectionModeProperty.OverrideDefaultValue<Segmented>(SelectionMode.AlwaysSelected);
        FocusableProperty.OverrideDefaultValue(typeof(Segmented), false);
        ItemsPanelProperty.OverrideDefaultValue<Segmented>(DefaultPanel);
    }

    protected override Control CreateContainerForItemOverride(object? item, int index, object? recycleKey) => new SegmentedItem();

    protected override bool NeedsContainerOverride(object? item, int index, out object? recycleKey) =>
        NeedsContainer<SegmentedItem>(item, out recycleKey);

    protected override bool ShouldTriggerSelection(Visual selectable, PointerEventArgs e) =>
        e.Properties.PointerUpdateKind is PointerUpdateKind.LeftButtonPressed or PointerUpdateKind.LeftButtonReleased && base.ShouldTriggerSelection(selectable, e);

    public override bool UpdateSelectionFromEvent(Control container, RoutedEventArgs eventArgs)
    {
        if (eventArgs is FocusChangedEventArgs { NavigationMethod: not NavigationMethod.Directional }) return false;
        return base.UpdateSelectionFromEvent(container, eventArgs);
    }

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);
        _flight = e.NameScope.Find<Border>("PART_Flight");
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == SelectedIndexProperty)
            Fly(change.GetOldValue<int>(), change.GetNewValue<int>());
    }

    private void Fly(int from, int to)
    {
        if (_flight?.Parent is not Visual layer || from < 0 || to < 0 || from == to || !IsEffectivelyVisible) return;
        if (ContainerFromIndex(from) is not Control a || ContainerFromIndex(to) is not Control b) return;
        if (a.TranslatePoint(default, layer) is not Point pa || b.TranslatePoint(default, layer) is not Point pb || b.Bounds.Width <= 0) return;

        double ax = pa.X, aw = a.Bounds.Width;
        if (_run is not null && _flight.IsVisible)
        {
            ax = Canvas.GetLeft(_flight);
            aw = _flight.Bounds.Width > 0 ? _flight.Bounds.Width : aw;
        }
        double bx = pb.X, bw = b.Bounds.Width;
        double left = Math.Min(ax, bx), right = Math.Max(ax + aw, bx + bw);

        Canvas.SetTop(_flight, pb.Y);
        _flight.Height = b.Bounds.Height;
        Place(ax, aw);
        _flight.IsVisible = true;
        Classes.Add("moving");

        var run = _run = new object();
        var clock = System.Diagnostics.Stopwatch.StartNew();
        var top = TopLevel.GetTopLevel(this);
        if (top is null) { Land(run); return; }
        // Frames stop arriving when the window is hidden, so the face also lands on a timer.
        DispatcherTimer.RunOnce(() => Land(run), TimeSpan.FromMilliseconds(Duration + 150));
        void Frame(TimeSpan _)
        {
            if (_run != run) return;
            var t = Math.Clamp(clock.Elapsed.TotalMilliseconds / Duration, 0, 1);
            var e = Ease.Ease(t);
            if (e < 1d / 3) { var k = e * 3; Place(ax + (left - ax) * k, aw + (right - left - aw) * k); }
            else { var k = (e - 1d / 3) * 1.5; Place(left + (bx - left) * k, right - left + (bw - (right - left)) * k); }
            if (t >= 1) Land(run); else top.RequestAnimationFrame(Frame);
        }
        top.RequestAnimationFrame(Frame);
    }

    private const double Duration = 600;
    private static readonly SplineEasing Ease = new(0.22, 1, 0.36, 1);
    private object? _run;

    private void Place(double x, double width)
    {
        Canvas.SetLeft(_flight!, x);
        _flight!.Width = Math.Max(0, width);
    }

    private void Land(object run)
    {
        if (_run != run) return;
        _run = null;
        _flight!.IsVisible = false;
        Classes.Remove("moving");
    }
}

/// <summary>One segment of a <see cref="Segmented"/>; <c>:selected</c> is the web's <c>is-active</c>.</summary>
public class SegmentedItem : ListBoxItem
{
    protected override void OnGotFocus(FocusChangedEventArgs e)
    {
        base.OnGotFocus(e);
        UpdateSelectionFromEvent(e);
    }
}
