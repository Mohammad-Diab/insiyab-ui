using Avalonia;
using Avalonia.Automation.Peers;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Threading;

namespace Insiyab.Avalonia.Controls;

/// <summary>A phone's main pages at the foot of the screen: three to five <see cref="BottomNavItem"/>s, each an icon over its label, the chosen one with the brand pill behind its icon that flies to the next choice. Bind <c>SelectedIndex</c> or <c>SelectedItem</c>; <see cref="Reselected"/> comes when the chosen item is pressed again. It pads itself by the safe area at the foot and sides, and screen readers hear it as tabs. With <see cref="IsRail"/> the same items stand in a column at the start side instead (a phone on its side), their labels kept only while they fit.</summary>
[TemplatePart("PART_Flight", typeof(Border))]
public class BottomNav : SelectingItemsControl
{
    /// <summary>Raised on the bar when its chosen item is pressed again (a tap, a click, Enter or Space), as an app scrolls its page back to the top.</summary>
    public static readonly RoutedEvent<RoutedEventArgs> ReselectedEvent =
        RoutedEvent.Register<BottomNav, RoutedEventArgs>(nameof(Reselected), RoutingStrategies.Bubble);

    /// <summary>The items in a column at the start side instead of a row at the foot, for a short window such as a phone on its side; see the class summary.</summary>
    public static readonly StyledProperty<bool> IsRailProperty =
        AvaloniaProperty.Register<BottomNav, bool>(nameof(IsRail));

    // The height an item takes in the rail with its label, under which every label goes.
    internal const double RailItemWithLabel = 64;

    private static readonly FuncTemplate<Panel?> DefaultPanel = new(() => new BottomNavPanel());

    private Border? _flight;
    private MarkerFlight? _run;
    private TopLevel? _top;

    static BottomNav()
    {
        SelectionModeProperty.OverrideDefaultValue<BottomNav>(SelectionMode.AlwaysSelected);
        FocusableProperty.OverrideDefaultValue(typeof(BottomNav), false);
        ItemsPanelProperty.OverrideDefaultValue<BottomNav>(DefaultPanel);
        KeyboardNavigation.TabNavigationProperty.OverrideDefaultValue(typeof(BottomNav), KeyboardNavigationMode.Once);
    }

    public bool IsRail { get => GetValue(IsRailProperty); set => SetValue(IsRailProperty, value); }

    public event EventHandler<RoutedEventArgs>? Reselected
    {
        add => AddHandler(ReselectedEvent, value);
        remove => RemoveHandler(ReselectedEvent, value);
    }

    protected override Control CreateContainerForItemOverride(object? item, int index, object? recycleKey) => new BottomNavItem();

    protected override bool NeedsContainerOverride(object? item, int index, out object? recycleKey) =>
        NeedsContainer<BottomNavItem>(item, out recycleKey);

    // Items given inline keep the IsSelected of their last showing; prepared again, Avalonia would read that as a toggle.
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
        else if (change.Property == IsRailProperty || change.Property == FlowDirectionProperty)
        {
            if (change.Property == IsRailProperty)
            {
                _run?.Land();
                PseudoClasses.Set(":rail", IsRail);
                if (ItemsPanelRoot is BottomNavPanel panel) panel.Vertical = IsRail;
            }
            PadRail();
            ToastStrip.Refit(this);
            FabLook.Refit(this);
        }
        else if (change.Property == BoundsProperty || change.Property == IsVisibleProperty)
        {
            ToastStrip.Refit(this);
            FabLook.Refit(this);
        }
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        _top = TopLevel.GetTopLevel(this);
    }

    // The rail pads the top, the foot and the screen side it stands on (the safe area's edges are the screen's, whatever the reading direction); the bar keeps its theme's edges.
    private void PadRail()
    {
        if (IsRail)
            SafeArea.SetEdges(this, SafeAreaEdges.Top | SafeAreaEdges.Bottom | (FlowDirection == FlowDirection.RightToLeft ? SafeAreaEdges.Right : SafeAreaEdges.Left));
        else
            ClearValue(SafeArea.EdgesProperty);
    }

    // The rail drops every label while the column is too short for all of them with their labels, and takes them back when it grows. The labelled column's height is the one it measured last (a finger's items are taller than the nominal 64: five did not fit a phone on its side, 360 tall).
    private double _labelled;

    protected override Size MeasureOverride(Size availableSize)
    {
        var limited = IsRail && !double.IsInfinity(availableSize.Height);
        var labelled = Math.Max(_labelled, Padding.Top + Padding.Bottom + RailItemWithLabel * Math.Max(1, ItemCount));
        var bare = limited && availableSize.Height < labelled;
        if (PseudoClasses.Contains(":bare") != bare) PseudoClasses.Set(":bare", bare);
        var size = base.MeasureOverride(availableSize);
        if (limited && !bare)
        {
            _labelled = size.Height;
            if (size.Height > availableSize.Height + 0.5)
            {
                PseudoClasses.Set(":bare", true);
                size = base.MeasureOverride(availableSize);
            }
        }
        return size;
    }

    // The column's own measure can miss what stands above its items (the status bar's room the safe area gives it): an item drawn past the column's foot, once arranged, says the labelled column is taller than the window, so it drops its labels on the next measure (a phone on its side, 360 tall).
    protected override Size ArrangeOverride(Size finalSize)
    {
        var size = base.ArrangeOverride(finalSize);
        if (IsRail && !PseudoClasses.Contains(":bare") && ItemsPanelRoot is { } items && items.Children.LastOrDefault(c => c.IsVisible) is { } last
            && last.TranslatePoint(new Point(0, last.Bounds.Height), this) is { } foot && foot.Y > finalSize.Height + 0.5)
        {
            _labelled = Math.Max(_labelled, finalSize.Height + foot.Y - finalSize.Height + 1);
            Dispatcher.UIThread.Post(InvalidateMeasure);
        }
        return size;
    }

    // Toasts and floating buttons at the foot sit above the bar, and come back down when it goes.
    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        if (_top is { } top) Dispatcher.UIThread.Post(() =>
        {
            ToastStrip.Refit(top);
            FabLook.Refit(top);
        });
        _top = null;
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        if (!e.Handled && e.KeyModifiers == KeyModifiers.None && e.Key is Key.Home or Key.End && ItemCount > 0)
        {
            var to = e.Key == Key.Home ? 0 : ItemCount - 1;
            if (ContainerFromIndex(to) is Control c && c.Focus(NavigationMethod.Directional)) e.Handled = true;
        }
        base.OnKeyDown(e);
    }

    internal void RaiseReselected() => RaiseEvent(new RoutedEventArgs(ReselectedEvent));

    // The pill's span in the flight layer: the item's own pill part.
    private Rect? PillOf(int index, Visual layer)
    {
        if (ContainerFromIndex(index) is not BottomNavItem item || item.Pill is not { } pill || pill.Bounds.Width <= 0) return null;
        return pill.TranslatePoint(default, layer) is { } p ? new Rect(p, pill.Bounds.Size) : null;
    }

    private void Fly(int from, int to)
    {
        if (_flight?.Parent is not Visual layer || from < 0 || to < 0 || from == to || !IsEffectivelyVisible) return;
        if (PillOf(from, layer) is not { } a || PillOf(to, layer) is not { } b) return;
        var rail = IsRail;
        var start = rail ? (a.Y, a.Height) : (a.X, a.Width);
        if (_run is { IsDone: false } previous)
        {
            start = (previous.Start, previous.Length);
            previous.Land();
        }
        MarkerFlight? run = null;
        run = MarkerFlight.Run(this, start, rail ? (b.Y, b.Height) : (b.X, b.Width), rail ? PlaceDown : Place, () => Land(run));
        if (run is null) return;
        _run = run;
        if (rail)
        {
            Canvas.SetLeft(_flight, b.X);
            _flight.Width = b.Width;
        }
        else
        {
            Canvas.SetTop(_flight, b.Y);
            _flight.Height = b.Height;
        }
        _flight.IsVisible = true;
        Classes.Add("moving");
    }

    private void Place(double x, double width)
    {
        Canvas.SetLeft(_flight!, x);
        _flight!.Width = Math.Max(0, width);
    }

    private void PlaceDown(double y, double height)
    {
        Canvas.SetTop(_flight!, y);
        _flight!.Height = Math.Max(0, height);
    }

    private void Land(MarkerFlight? run)
    {
        if (run is null || _run != run) return;
        _run = null;
        _flight!.IsVisible = false;
        Classes.Remove("moving");
    }

    protected override AutomationPeer OnCreateAutomationPeer() => new BottomNavAutomationPeer(this);
}

/// <summary>One page of a <see cref="BottomNav"/>: <see cref="Icon"/> over <see cref="Label"/>, with a count (<see cref="Badge"/>) or a dot (<see cref="ShowDot"/>) at the icon's top end.</summary>
[TemplatePart("PART_Pill", typeof(Border))]
public class BottomNavItem : ListBoxItem
{
    public static readonly StyledProperty<Geometry?> IconProperty =
        AvaloniaProperty.Register<BottomNavItem, Geometry?>(nameof(Icon));

    public static readonly StyledProperty<string?> LabelProperty =
        AvaloniaProperty.Register<BottomNavItem, string?>(nameof(Label));

    /// <summary>A count at the icon's top end; 0 shows none, above 99 it reads 99+.</summary>
    public static readonly StyledProperty<int> BadgeProperty =
        AvaloniaProperty.Register<BottomNavItem, int>(nameof(Badge));

    /// <summary>A small dot at the icon's top end, for something new without a count; a count wins.</summary>
    public static readonly StyledProperty<bool> ShowDotProperty =
        AvaloniaProperty.Register<BottomNavItem, bool>(nameof(ShowDot));

    /// <summary>The count as drawn: empty without one, 99+ above 99.</summary>
    internal static readonly StyledProperty<string> BadgeTextProperty =
        AvaloniaProperty.Register<BottomNavItem, string>(nameof(BadgeText), "");

    private bool _wasSelected;

    public Geometry? Icon { get => GetValue(IconProperty); set => SetValue(IconProperty, value); }

    public string? Label { get => GetValue(LabelProperty); set => SetValue(LabelProperty, value); }

    public int Badge { get => GetValue(BadgeProperty); set => SetValue(BadgeProperty, value); }

    public bool ShowDot { get => GetValue(ShowDotProperty); set => SetValue(ShowDotProperty, value); }

    internal string BadgeText => GetValue(BadgeTextProperty);

    internal Border? Pill { get; private set; }

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);
        Pill = e.NameScope.Find<Border>("PART_Pill");
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == BadgeProperty || change.Property == ShowDotProperty)
        {
            var n = Badge;
            SetValue(BadgeTextProperty, n <= 0 ? "" : n > 99 ? "99+" : n.ToString(System.Globalization.CultureInfo.InvariantCulture));
            PseudoClasses.Set(":badge", n > 0);
            PseudoClasses.Set(":dot", n <= 0 && ShowDot);
        }
    }

    protected override void OnGotFocus(FocusChangedEventArgs e)
    {
        base.OnGotFocus(e);
        UpdateSelectionFromEvent(e);
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        _wasSelected = IsSelected && e.GetCurrentPoint(this).Properties.IsLeftButtonPressed;
        base.OnPointerPressed(e);
    }

    // A press that starts and ends on the chosen item, and is a tap for a finger, chooses it again.
    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        var again = _wasSelected && IsSelected && e.InitialPressMouseButton == MouseButton.Left && new Rect(Bounds.Size).Contains(e.GetPosition(this)) && Touch.IsTap(e);
        _wasSelected = false;
        base.OnPointerReleased(e);
        if (again) Owner()?.RaiseReselected();
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        if (!e.Handled && e.KeyModifiers == KeyModifiers.None && e.Key is Key.Enter or Key.Space && Owner() is { } nav)
        {
            e.Handled = true;
            if (IsSelected) nav.RaiseReselected();
            else nav.SelectedIndex = nav.IndexFromContainer(this);
            return;
        }
        base.OnKeyDown(e);
    }

    private BottomNav? Owner() => ItemsControl.ItemsControlFromItemContainer(this) as BottomNav;

    protected override AutomationPeer OnCreateAutomationPeer() => new BottomNavItemAutomationPeer(this);
}

/// <summary>The bar's panel: equal columns side by side, and the arrow keys (mirrored in right to left) moving along them; in a rail (<see cref="Vertical"/>) the items stacked in the middle of the column, and the up and down keys moving along them.</summary>
internal sealed class BottomNavPanel : Panel, INavigableContainer
{
    private bool _vertical;

    public bool Vertical
    {
        get => _vertical;
        set
        {
            if (_vertical == value) return;
            _vertical = value;
            InvalidateMeasure();
        }
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        if (TemplatedParent is BottomNav nav) Vertical = nav.IsRail;
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        var shown = Children.Where(c => c.IsVisible).ToList();
        if (_vertical)
        {
            double tall = 0, wide = 0;
            foreach (var child in shown)
            {
                child.Measure(new Size(availableSize.Width, double.PositiveInfinity));
                tall += child.DesiredSize.Height;
                wide = Math.Max(wide, child.DesiredSize.Width);
            }
            return new Size(wide, tall);
        }
        var width = double.IsInfinity(availableSize.Width) ? double.PositiveInfinity : availableSize.Width / Math.Max(1, shown.Count);
        var height = 0d;
        var widest = 0d;
        foreach (var child in shown)
        {
            child.Measure(new Size(width, availableSize.Height));
            height = Math.Max(height, child.DesiredSize.Height);
            widest = Math.Max(widest, child.DesiredSize.Width);
        }
        return new Size(double.IsInfinity(width) ? widest * shown.Count : availableSize.Width, height);
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        var shown = Children.Where(c => c.IsVisible).ToList();
        if (_vertical)
        {
            var tall = shown.Sum(c => c.DesiredSize.Height);
            var y = Math.Max(0, (finalSize.Height - tall) / 2);
            foreach (var child in shown)
            {
                child.Arrange(new Rect(0, y, finalSize.Width, child.DesiredSize.Height));
                y += child.DesiredSize.Height;
            }
            return finalSize;
        }
        var width = finalSize.Width / Math.Max(1, shown.Count);
        for (var i = 0; i < shown.Count; i++)
            shown[i].Arrange(new Rect(i * width, 0, width, finalSize.Height));
        return finalSize;
    }

    IInputElement? INavigableContainer.GetControl(NavigationDirection direction, IInputElement? from, bool wrap)
    {
        var shown = Children.Where(c => c.IsVisible && c.IsEffectivelyEnabled).ToList();
        if (shown.Count == 0) return null;
        var at = from is Control c ? shown.IndexOf(c) : -1;
        var rtl = FlowDirection == FlowDirection.RightToLeft;
        var step = direction switch
        {
            NavigationDirection.Up when _vertical => -1,
            NavigationDirection.Down when _vertical => 1,
            NavigationDirection.Left when !_vertical => rtl ? 1 : -1,
            NavigationDirection.Right when !_vertical => rtl ? -1 : 1,
            NavigationDirection.Next => 1,
            NavigationDirection.Previous => -1,
            _ => 0,
        };
        if (direction == NavigationDirection.First) return shown[0];
        if (direction == NavigationDirection.Last) return shown[^1];
        if (step == 0 || at < 0) return null;
        var to = at + step;
        if (to < 0 || to >= shown.Count) return wrap ? shown[(to + shown.Count) % shown.Count] : null;
        return shown[to];
    }
}

/// <summary>Heard as a tab list.</summary>
internal sealed class BottomNavAutomationPeer(BottomNav owner) : SelectingItemsControlAutomationPeer(owner)
{
    protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Tab;
}

/// <summary>Heard as a tab, named by its label and its count, or "new" for a dot.</summary>
internal sealed class BottomNavItemAutomationPeer(BottomNavItem owner) : ListItemAutomationPeer(owner)
{
    protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.TabItem;

    protected override string? GetNameCore()
    {
        var item = (BottomNavItem)Owner;
        var name = global::Avalonia.Automation.AutomationProperties.GetName(item);
        if (string.IsNullOrEmpty(name)) name = item.Label;
        if (item.Badge > 0) return name + ", " + item.BadgeText;
        if (item.ShowDot) return name + ", " + Words.Of(item)["nav.new"];
        return name;
    }
}
