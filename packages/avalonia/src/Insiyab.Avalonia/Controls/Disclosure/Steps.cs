using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.VisualTree;

namespace Insiyab.Avalonia.Controls;

/// <summary>Where you are in a sequence, the web's <c>ol.ins-steps</c>: numbered circles joined by a line, the done ones filled with the brand. Classes <c>dots</c> (small dots, no names) and <c>grid</c> (numbers that wrap, no lines, no names on show). Grid steps are one Tab stop: the arrows, Home and End move between them.</summary>
public class Steps : ItemsControl
{
    protected override Control CreateContainerForItemOverride(object? item, int index, object? recycleKey) => new StepsItem();

    protected override bool NeedsContainerOverride(object? item, int index, out object? recycleKey) =>
        NeedsContainer<StepsItem>(item, out recycleKey);

    protected override void ContainerForItemPreparedOverride(Control container, object? item, int index)
    {
        base.ContainerForItemPreparedOverride(container, item, index);
        Refresh();
    }

    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);
        Refresh();
    }

    protected override void ContainerIndexChangedOverride(Control container, int oldIndex, int newIndex)
    {
        base.ContainerIndexChangedOverride(container, oldIndex, newIndex);
        Refresh();
    }

    protected override void ClearContainerForItemOverride(Control container)
    {
        base.ClearContainerForItemOverride(container);
        Refresh();
    }

    private StepsItem? _stop;
    private bool _roved;

    public Steps()
    {
        AddHandler(GotFocusEvent, (_, e) =>
        {
            if (Classes.Contains("grid") && (e.Source as Visual)?.FindAncestorOfType<StepsItem>(true) is { } item) Rove(Shown(), item);
        });
    }

    private List<StepsItem> Shown() =>
        ItemsPanelRoot is { } panel ? panel.Children.OfType<StepsItem>().Where(s => s.IsVisible).ToList() : [];

    /// <summary>Works out every step's number and its joins again: called whenever a step changes.</summary>
    internal void Refresh()
    {
        var shown = Shown();
        for (var i = 0; i < shown.Count; i++)
        {
            shown[i].Number = i + 1;
            shown[i].SetJoins(i == 0, i == shown.Count - 1, i > 0 && shown[i - 1].IsDone);
        }
        if (Classes.Contains("grid")) Rove(shown, null);
    }

    private void Rove(List<StepsItem> shown, StepsItem? focused)
    {
        if (focused is not null)
        {
            _stop = focused;
            _roved = true;
        }
        else if (!_roved || _stop is null || !shown.Contains(_stop)) _stop = shown.FirstOrDefault(s => s.IsCurrent) ?? shown.FirstOrDefault();
        foreach (var s in shown) s.IsTabStop = s == _stop;
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        if (e.Handled || e.KeyModifiers != KeyModifiers.None || !Classes.Contains("grid") || e.Key is not (Key.Left or Key.Right or Key.Up or Key.Down or Key.Home or Key.End))
        {
            base.OnKeyDown(e);
            return;
        }
        var shown = Shown();
        var from = (e.Source as Visual)?.FindAncestorOfType<StepsItem>(true);
        var at = from is null ? -1 : shown.IndexOf(from);
        if (from is null || at < 0)
        {
            base.OnKeyDown(e);
            return;
        }
        var rtl = FlowDirection == FlowDirection.RightToLeft;
        var to = e.Key switch
        {
            Key.Right => shown.ElementAtOrDefault(rtl ? at - 1 : at + 1),
            Key.Left => shown.ElementAtOrDefault(rtl ? at + 1 : at - 1),
            Key.Home => shown[0],
            Key.End => shown[^1],
            Key.Up => Row(shown, from, -1),
            _ => Row(shown, from, 1),
        };
        e.Handled = true;
        to?.Focus(NavigationMethod.Tab);
    }

    private StepsItem? Row(List<StepsItem> shown, StepsItem from, int direction)
    {
        var c = Centre(from);
        StepsItem? best = null;
        double bestDy = double.MaxValue, bestDx = double.MaxValue;
        foreach (var s in shown)
        {
            var p = Centre(s);
            var dy = (p.Y - c.Y) * direction;
            if (dy < 1) continue;
            var dx = Math.Abs(p.X - c.X);
            if (dy < bestDy - 1 || Math.Abs(dy - bestDy) <= 1 && dx < bestDx)
            {
                best = s;
                bestDy = dy;
                bestDx = dx;
            }
        }
        return best;
    }

    private Point Centre(StepsItem s) => s.TranslatePoint(new Point(s.Bounds.Width / 2, s.Bounds.Height / 2), this) ?? default;
}

/// <summary>One step. <see cref="IsDone"/>, <see cref="IsCurrent"/> (aria-current="step"), <see cref="IsFlagged"/> (an amber dot, left to come back to) and <see cref="IsError"/> (red, needs fixing) show as <c>:done :current :flagged :error</c>.</summary>
[PseudoClasses(":done", ":current", ":flagged", ":error", ":first", ":last", ":after-done")]
public class StepsItem : ContentControl
{
    public static readonly StyledProperty<bool> IsDoneProperty =
        AvaloniaProperty.Register<StepsItem, bool>(nameof(IsDone));

    public static readonly StyledProperty<bool> IsCurrentProperty =
        AvaloniaProperty.Register<StepsItem, bool>(nameof(IsCurrent));

    public static readonly StyledProperty<bool> IsFlaggedProperty =
        AvaloniaProperty.Register<StepsItem, bool>(nameof(IsFlagged));

    public static readonly StyledProperty<bool> IsErrorProperty =
        AvaloniaProperty.Register<StepsItem, bool>(nameof(IsError));

    public static readonly DirectProperty<StepsItem, int> NumberProperty =
        AvaloniaProperty.RegisterDirect<StepsItem, int>(nameof(Number), o => o.Number);

    public static readonly RoutedEvent<RoutedEventArgs> ClickEvent =
        RoutedEvent.Register<StepsItem, RoutedEventArgs>(nameof(Click), RoutingStrategies.Bubble);

    private int _number;

    static StepsItem()
    {
        IsDoneProperty.Changed.AddClassHandler<StepsItem>((x, e) => { x.PseudoClasses.Set(":done", e.GetNewValue<bool>()); x.Owner?.Refresh(); });
        IsCurrentProperty.Changed.AddClassHandler<StepsItem>((x, e) => x.PseudoClasses.Set(":current", e.GetNewValue<bool>()));
        IsFlaggedProperty.Changed.AddClassHandler<StepsItem>((x, e) => x.PseudoClasses.Set(":flagged", e.GetNewValue<bool>()));
        IsErrorProperty.Changed.AddClassHandler<StepsItem>((x, e) => x.PseudoClasses.Set(":error", e.GetNewValue<bool>()));
        IsVisibleProperty.Changed.AddClassHandler<StepsItem>((x, _) => x.Owner?.Refresh());
    }

    public bool IsDone { get => GetValue(IsDoneProperty); set => SetValue(IsDoneProperty, value); }

    public bool IsCurrent { get => GetValue(IsCurrentProperty); set => SetValue(IsCurrentProperty, value); }

    public bool IsFlagged { get => GetValue(IsFlaggedProperty); set => SetValue(IsFlaggedProperty, value); }

    public bool IsError { get => GetValue(IsErrorProperty); set => SetValue(IsErrorProperty, value); }

    /// <summary>The step's number among the steps showing, written by <see cref="Steps"/>.</summary>
    public int Number { get => _number; internal set => SetAndRaise(NumberProperty, ref _number, value); }

    /// <summary>A press on the step: in the <c>grid</c> look each step is the control for its question.</summary>
    public event EventHandler<RoutedEventArgs>? Click { add => AddHandler(ClickEvent, value); remove => RemoveHandler(ClickEvent, value); }

    private Steps? Owner => ItemsControl.ItemsControlFromItemContainer(this) as Steps;

    internal void SetJoins(bool first, bool last, bool afterDone)
    {
        PseudoClasses.Set(":first", first);
        PseudoClasses.Set(":last", last);
        PseudoClasses.Set(":after-done", afterDone);
    }

    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        base.OnPointerReleased(e);
        if (e.InitialPressMouseButton == MouseButton.Left && new Rect(Bounds.Size).Contains(e.GetPosition(this)))
            RaiseEvent(new RoutedEventArgs(ClickEvent));
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        if (e.Handled || e.Key is not (Key.Enter or Key.Space)) return;
        e.Handled = true;
        RaiseEvent(new RoutedEventArgs(ClickEvent));
    }
}

/// <summary>The steps' row: every step showing gets the same width, as <c>flex: 1 1 0</c> does on the web.</summary>
internal class StepsPanel : Panel
{
    protected override Size MeasureOverride(Size availableSize)
    {
        var shown = Children.Where(c => c.IsVisible).ToList();
        if (shown.Count == 0) return default;
        var width = double.IsInfinity(availableSize.Width) ? double.PositiveInfinity : availableSize.Width / shown.Count;
        double height = 0, widest = 0;
        foreach (var child in shown)
        {
            child.Measure(new Size(width, availableSize.Height));
            height = Math.Max(height, child.DesiredSize.Height);
            widest = Math.Max(widest, child.DesiredSize.Width);
        }
        return new Size(double.IsInfinity(availableSize.Width) ? widest * shown.Count : availableSize.Width, height);
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        var shown = Children.Where(c => c.IsVisible).ToList();
        if (shown.Count == 0) return finalSize;
        var width = finalSize.Width / shown.Count;
        for (var i = 0; i < shown.Count; i++) shown[i].Arrange(new Rect(i * width, 0, width, finalSize.Height));
        return finalSize;
    }
}
