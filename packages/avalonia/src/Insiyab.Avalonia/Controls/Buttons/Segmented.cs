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

    // Segments given inline are their own containers and keep the IsSelected of their last showing; prepared again (a card that reopens) Avalonia reads that as a toggle, which drops the chosen one and falls back to the first.
    protected override void PrepareContainerForItemOverride(Control container, object? item, int index)
    {
        if (container == item && container.IsSet(IsSelectedProperty) && container.GetValue(IsSelectedProperty) == Selection.IsSelected(index))
            container.ClearValue(IsSelectedProperty);
        base.PrepareContainerForItemOverride(container, item, index);
    }

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
        var start = (pa.X, a.Bounds.Width);
        if (_run is { IsDone: false } previous)
        {
            start = (previous.Start, previous.Length);
            previous.Land();
        }
        MarkerFlight? run = null;
        run = MarkerFlight.Run(this, start, (pb.X, b.Bounds.Width), Place, () => Land(run));
        if (run is null) return;
        _run = run;
        Canvas.SetTop(_flight, pb.Y);
        _flight.Height = b.Bounds.Height;
        _flight.IsVisible = true;
        Classes.Add("moving");
    }

    private MarkerFlight? _run;

    private void Place(double x, double width)
    {
        Canvas.SetLeft(_flight!, x);
        _flight!.Width = Math.Max(0, width);
    }

    private void Land(MarkerFlight? run)
    {
        if (run is null || _run != run) return;
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
