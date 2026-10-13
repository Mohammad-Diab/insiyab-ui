using System.Collections.Specialized;
using Avalonia;
using Avalonia.Animation;
using Avalonia.Animation.Easings;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Media.Transformation;
using Avalonia.Styling;
using Avalonia.Threading;
using Avalonia.VisualTree;

namespace Insiyab.Avalonia.Controls;

/// <summary>Raised when the person marks a notification read: one <see cref="Item"/>, or <see cref="All"/>.</summary>
public sealed class NotifyReadEventArgs(RoutedEvent routedEvent, NotifyItem? item, bool all) : RoutedEventArgs(routedEvent)
{
    public NotifyItem? Item { get; } = item;
    public bool All { get; } = all;
}

/// <summary>The notification centre plugin (<c>details.ins-notify</c>): a bell with the unread count on its corner, opening a panel with a head, the list and a foot link. Its items are <see cref="NotifyItem"/>s; clicking one marks it read, and the head's button marks them all. A count that grows rings the bell once. The panel is a popup in the window's overlay layer, with the look of a <see cref="PopCard"/>; the arrows, Home and End walk the rows and Escape closes it.</summary>
[TemplatePart("PART_Bell", typeof(Button))]
[TemplatePart("PART_Popup", typeof(Popup))]
[TemplatePart("PART_ReadAll", typeof(Button))]
[TemplatePart("PART_Foot", typeof(Button))]
[PseudoClasses(":open", ":empty", ":unread")]
public class Notify : ItemsControl
{
    public static readonly StyledProperty<string> TitleProperty =
        AvaloniaProperty.Register<Notify, string>(nameof(Title), "الإشعارات");

    public static readonly StyledProperty<string?> ReadAllTextProperty =
        AvaloniaProperty.Register<Notify, string?>(nameof(ReadAllText), "تعليم الكل مقروءًا");

    public static readonly StyledProperty<string?> FootTextProperty =
        AvaloniaProperty.Register<Notify, string?>(nameof(FootText));

    public static readonly StyledProperty<string> EmptyTextProperty =
        AvaloniaProperty.Register<Notify, string>(nameof(EmptyText), "لا إشعارات");

    public static readonly StyledProperty<bool> IsOpenProperty =
        AvaloniaProperty.Register<Notify, bool>(nameof(IsOpen));

    public static readonly StyledProperty<Geometry?> IconProperty =
        AvaloniaProperty.Register<Notify, Geometry?>(nameof(Icon));

    public static readonly StyledProperty<bool> IsEndProperty =
        AvaloniaProperty.Register<Notify, bool>(nameof(IsEnd));

    public static readonly StyledProperty<bool> CloseOnItemClickProperty =
        AvaloniaProperty.Register<Notify, bool>(nameof(CloseOnItemClick));

    public static readonly DirectProperty<Notify, int> UnreadCountProperty =
        AvaloniaProperty.RegisterDirect<Notify, int>(nameof(UnreadCount), n => n.UnreadCount);

    public static readonly DirectProperty<Notify, string> CountTextProperty =
        AvaloniaProperty.RegisterDirect<Notify, string>(nameof(CountText), n => n.CountText);

    public static readonly RoutedEvent<NotifyReadEventArgs> ReadEvent =
        RoutedEvent.Register<Notify, NotifyReadEventArgs>(nameof(Read), RoutingStrategies.Bubble);

    /// <summary>Raised on the foot link.</summary>
    public static readonly RoutedEvent<RoutedEventArgs> FootClickEvent =
        RoutedEvent.Register<Notify, RoutedEventArgs>(nameof(FootClick), RoutingStrategies.Bubble);

    private int _unread;
    private string _countText = "0";
    private bool _ready;
    private Button? _bell;
    private Popup? _popup;
    private Button? _readAll;
    private Button? _foot;

    static Notify()
    {
        TransformOperationsAnimator.Ensure();
    }

    public Notify()
    {
        Items.CollectionChanged += OnItemsChanged;
        AddHandler(NotifyItem.UnreadChangedEvent, (_, _) => Sync());
        AddHandler(Button.ClickEvent, OnItemClick, RoutingStrategies.Bubble);
    }

    public string Title { get => GetValue(TitleProperty); set => SetValue(TitleProperty, value); }

    /// <summary>The head's mark-all button, as <c>data-ins-notify-read</c>; none when null.</summary>
    public string? ReadAllText { get => GetValue(ReadAllTextProperty); set => SetValue(ReadAllTextProperty, value); }

    /// <summary>The foot link to every notification, as <c>.ins-notify-foot</c>; none when null.</summary>
    public string? FootText { get => GetValue(FootTextProperty); set => SetValue(FootTextProperty, value); }

    public string EmptyText { get => GetValue(EmptyTextProperty); set => SetValue(EmptyTextProperty, value); }

    public bool IsOpen { get => GetValue(IsOpenProperty); set => SetValue(IsOpenProperty, value); }

    /// <summary>The bell's geometry, a 24×24 stroked path; the theme gives the library's bell.</summary>
    public Geometry? Icon { get => GetValue(IconProperty); set => SetValue(IconProperty, value); }

    /// <summary>Hangs the panel from the bell's trailing edge instead of its leading edge, for a bell at the end of a topbar.</summary>
    public bool IsEnd { get => GetValue(IsEndProperty); set => SetValue(IsEndProperty, value); }

    /// <summary>Closes the panel when a row is clicked.</summary>
    public bool CloseOnItemClick { get => GetValue(CloseOnItemClickProperty); set => SetValue(CloseOnItemClickProperty, value); }

    public int UnreadCount { get => _unread; private set => SetAndRaise(UnreadCountProperty, ref _unread, value); }

    /// <summary>The corner count: the number, or 99+.</summary>
    public string CountText { get => _countText; private set => SetAndRaise(CountTextProperty, ref _countText, value); }

    public event EventHandler<NotifyReadEventArgs>? Read
    {
        add => AddHandler(ReadEvent, value);
        remove => RemoveHandler(ReadEvent, value);
    }

    public event EventHandler<RoutedEventArgs>? FootClick
    {
        add => AddHandler(FootClickEvent, value);
        remove => RemoveHandler(FootClickEvent, value);
    }

    /// <summary>Marks one item read, quietly, as <c>Insiyab.notify(box, item)</c>; gives the count left.</summary>
    public int MarkRead(NotifyItem item)
    {
        item.IsUnread = false;
        return Sync();
    }

    /// <summary>Marks every item read, quietly, as <c>Insiyab.notify(box, 'all')</c>.</summary>
    public int MarkAllRead()
    {
        foreach (var item in Rows()) item.IsUnread = false;
        return Sync();
    }

    protected override bool NeedsContainerOverride(object? item, int index, out object? recycleKey) =>
        NeedsContainer<NotifyItem>(item, out recycleKey);

    protected override Control CreateContainerForItemOverride(object? item, int index, object? recycleKey) => new NotifyItem();

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);
        if (_bell is not null) _bell.Click -= OnBellClick;
        if (_readAll is not null) _readAll.Click -= OnReadAll;
        if (_foot is not null) _foot.Click -= OnFoot;
        if (_popup is not null)
        {
            _popup.Opened -= OnPopupOpened;
            _popup.Closed -= OnPopupClosed;
            _popup.RemoveHandler(KeyDownEvent, OnPopupKey);
        }
        _bell = e.NameScope.Find<Button>("PART_Bell");
        _popup = e.NameScope.Find<Popup>("PART_Popup");
        _readAll = e.NameScope.Find<Button>("PART_ReadAll");
        _foot = e.NameScope.Find<Button>("PART_Foot");
        if (_bell is not null) _bell.Click += OnBellClick;
        if (_readAll is not null) _readAll.Click += OnReadAll;
        if (_foot is not null) _foot.Click += OnFoot;
        if (_popup is not null)
        {
            _popup.ShouldUseOverlayLayer = true;
            _popup.Opened += OnPopupOpened;
            _popup.Closed += OnPopupClosed;
            _popup.AddHandler(KeyDownEvent, OnPopupKey, RoutingStrategies.Bubble);
        }
        PlacePopup();
        Sync();
        _ready = true;
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == IsOpenProperty) PseudoClasses.Set(":open", change.GetNewValue<bool>());
        else if ((change.Property == FlowDirectionProperty || change.Property == IsEndProperty) && _popup is not null) PlacePopup();
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        PlacePopup();
    }

    // The panel hangs from the bell's leading edge, or the trailing one with IsEnd, and grows toward the other; Avalonia mirrors popup placement in RTL, so Left is the leading edge on either page.
    private void PlacePopup()
    {
        if (_popup is null) return;
        var rtl = FlowDirection == FlowDirection.RightToLeft;
        _popup.Placement = IsEnd ? PlacementMode.BottomEdgeAlignedRight : PlacementMode.BottomEdgeAlignedLeft;
        _popup.HorizontalOffset = rtl ^ IsEnd ? Pop.Room.Right : -Pop.Room.Left;
        _popup.VerticalOffset = Pop.Gap - Pop.Room.Top;
    }

    private IEnumerable<NotifyItem> Rows()
    {
        foreach (var item in Items)
        {
            if (item is NotifyItem row) yield return row;
            else if (ContainerFromItem(item!) is NotifyItem container) yield return container;
        }
    }

    private void OnItemsChanged(object? sender, NotifyCollectionChangedEventArgs e) => Sync();

    private int Sync()
    {
        var n = Rows().Count(r => r.IsUnread);
        var grew = _ready && n > UnreadCount;
        UnreadCount = n;
        CountText = n > 99 ? "99+" : n.ToString(System.Globalization.CultureInfo.InvariantCulture);
        PseudoClasses.Set(":unread", n > 0);
        PseudoClasses.Set(":empty", Items.Count == 0);
        var label = n == 0 ? Title : FlowDirection == FlowDirection.RightToLeft
            ? Title + "، " + (n == 1 ? "إشعار واحد غير مقروء" : n == 2 ? "إشعاران غير مقروءين" : n + " غير مقروءة")
            : Title + ", " + n + " unread";
        if (_bell is not null) AutomationProperties.SetName(_bell, label);
        if (grew) Ring();
        return n;
    }

    // One short shake of the bell for a notification that has just arrived.
    private void Ring()
    {
        if (_bell?.Content is not Control bell) return;
        var keys = new[] { 0.0, 14, -12, 8, -4, 0 };
        var animation = new Animation { Duration = TimeSpan.FromMilliseconds(650), Easing = new SineEaseInOut() };
        for (var i = 0; i < keys.Length; i++)
            animation.Children.Add(new KeyFrame { Cue = new Cue(i / (double)(keys.Length - 1)), Setters = { new Setter(RenderTransformProperty, TransformOperations.Parse(FormattableString.Invariant($"rotate({keys[i]}deg)"))) } });
        _ = animation.RunAsync(bell);
    }

    // The popup lets presses on the bell through, so a second click on the bell closes the panel instead of reopening it.
    private void OnBellClick(object? sender, RoutedEventArgs e) => IsOpen = !IsOpen;

    private void OnItemClick(object? sender, RoutedEventArgs e)
    {
        if (e.Source is not NotifyItem item) return;
        if (item.IsUnread)
        {
            item.IsUnread = false;
            Sync();
            RaiseEvent(new NotifyReadEventArgs(ReadEvent, item, false));
        }
        if (!CloseOnItemClick) return;
        IsOpen = false;
        _bell?.Focus();
    }

    private void OnReadAll(object? sender, RoutedEventArgs e)
    {
        e.Handled = true;
        var changed = Rows().Any(r => r.IsUnread);
        MarkAllRead();
        if (changed) RaiseEvent(new NotifyReadEventArgs(ReadEvent, null, true));
    }

    private void OnFoot(object? sender, RoutedEventArgs e)
    {
        e.Handled = true;
        RaiseEvent(new RoutedEventArgs(FootClickEvent));
    }

    // The panel is 22rem wide and 30rem tall at most, and never wider than the window less a margin.
    private void OnPopupOpened(object? sender, EventArgs e)
    {
        if (_popup?.Child is FlyoutPresenter presenter)
        {
            var root = presenter.GetVisualDescendants().OfType<Border>().FirstOrDefault(b => b.Name == "LayoutRoot");
            if (root is null)
            {
                presenter.ApplyTemplate();
                root = presenter.GetVisualDescendants().OfType<Border>().FirstOrDefault(b => b.Name == "LayoutRoot");
            }
            if (root is not null)
            {
                root.Width = Math.Max(240, Math.Min(352, OverlayHost.Width(this) - 16));
                root.MaxHeight = Math.Max(200, Math.Min(480, OverlayHost.Height(this) - 32));
            }
        }
        Dispatcher.UIThread.Post(() => (Rows().FirstOrDefault(r => r.IsEffectivelyVisible) as Control ?? _foot)?.Focus(NavigationMethod.Directional), DispatcherPriority.Loaded);
    }

    private void OnPopupKey(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            e.Handled = true;
            IsOpen = false;
            _bell?.Focus();
            return;
        }
        var rows = Rows().Where(r => r.IsEffectivelyVisible).ToList();
        var at = rows.FindIndex(r => r.IsFocused);
        Control? to = e.Key switch
        {
            Key.Down => rows.Count == 0 ? null : rows[at < 0 ? 0 : Math.Min(at + 1, rows.Count - 1)],
            Key.Up => rows.Count == 0 ? null : rows[at <= 0 ? 0 : at - 1],
            Key.Home => rows.FirstOrDefault(),
            Key.End => rows.LastOrDefault(),
            _ => null,
        };
        if (to is null) return;
        to.Focus(NavigationMethod.Directional);
        e.Handled = true;
    }

    private void OnPopupClosed(object? sender, EventArgs e)
    {
        if (IsOpen) IsOpen = false;
        var focused = TopLevel.GetTopLevel(this)?.FocusManager?.GetFocusedElement() as Visual;
        var inside = focused is not null && _popup?.Child is Visual child && (focused == child || child.IsVisualAncestorOf(focused));
        if (focused is null || inside) _bell?.Focus();
    }
}
