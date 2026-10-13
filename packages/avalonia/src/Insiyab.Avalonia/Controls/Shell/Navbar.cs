using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.VisualTree;

namespace Insiyab.Avalonia.Controls;

/// <summary>The web's <c>.ins-navbar</c>, the public site's bar: a glass island with the brand, the links (the selected one is the current page) and <see cref="End"/>; narrower than <see cref="CollapseWidth"/> the links fold behind a toggle.</summary>
[PseudoClasses(":collapsed", ":open")]
public class Navbar : ListBox
{
    public static readonly StyledProperty<object?> BrandProperty =
        AvaloniaProperty.Register<Navbar, object?>(nameof(Brand));

    public static readonly StyledProperty<object?> EndProperty =
        AvaloniaProperty.Register<Navbar, object?>(nameof(End));

    public static readonly StyledProperty<bool> IsOpenProperty =
        AvaloniaProperty.Register<Navbar, bool>(nameof(IsOpen), defaultBindingMode: global::Avalonia.Data.BindingMode.TwoWay);

    /// <summary>Below this width of its own the links fold behind the toggle. The web folds below a 900px viewport, which leaves a full-width bar 868px wide; 0 never folds.</summary>
    public static readonly StyledProperty<double> CollapseWidthProperty =
        AvaloniaProperty.Register<Navbar, double>(nameof(CollapseWidth), 868);

    public static readonly DirectProperty<Navbar, bool> IsCollapsedProperty =
        AvaloniaProperty.RegisterDirect<Navbar, bool>(nameof(IsCollapsed), o => o.IsCollapsed);

    /// <summary>Raised when the folded menu opens or closes (the web's <c>ins:navbar</c>).</summary>
    public static readonly RoutedEvent<RoutedEventArgs> ToggledEvent =
        RoutedEvent.Register<Navbar, RoutedEventArgs>(nameof(Toggled), RoutingStrategies.Bubble);

    private bool _isCollapsed;
    private Control? _menu;

    public Navbar()
    {
        SelectionMode = SelectionMode.Single;
        AddHandler(TappedEvent, OnTapped, RoutingStrategies.Bubble, handledEventsToo: true);
    }

    protected override Type StyleKeyOverride => typeof(Navbar);

    public object? Brand { get => GetValue(BrandProperty); set => SetValue(BrandProperty, value); }
    public object? End { get => GetValue(EndProperty); set => SetValue(EndProperty, value); }
    public bool IsOpen { get => GetValue(IsOpenProperty); set => SetValue(IsOpenProperty, value); }
    public double CollapseWidth { get => GetValue(CollapseWidthProperty); set => SetValue(CollapseWidthProperty, value); }

    public bool IsCollapsed
    {
        get => _isCollapsed;
        private set => SetAndRaise(IsCollapsedProperty, ref _isCollapsed, value);
    }

    public event EventHandler<RoutedEventArgs>? Toggled
    {
        add => AddHandler(ToggledEvent, value);
        remove => RemoveHandler(ToggledEvent, value);
    }

    protected override bool NeedsContainerOverride(object? item, int index, out object? recycleKey) =>
        NeedsContainer<NavbarLink>(item, out recycleKey);

    protected override Control CreateContainerForItemOverride(object? item, int index, object? recycleKey) => new NavbarLink();

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);
        _menu = e.NameScope.Find<Control>("PART_Menu");
    }

    protected override void OnSizeChanged(SizeChangedEventArgs e)
    {
        base.OnSizeChanged(e);
        UpdateCollapsed(e.NewSize.Width);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == IsOpenProperty)
        {
            PseudoClasses.Set(":open", change.GetNewValue<bool>());
            if (change.GetNewValue<bool>() && IsCollapsed && _menu is not null && !Motion.IsStill(this)) Entrance.Play(_menu, EntranceEffect.Rise);
            RaiseEvent(new RoutedEventArgs(ToggledEvent, this));
        }
        else if (change.Property == CollapseWidthProperty) UpdateCollapsed(Bounds.Width);
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        if (e.Key == Key.Escape && IsOpen)
        {
            SetCurrentValue(IsOpenProperty, false);
            e.Handled = true;
            return;
        }
        base.OnKeyDown(e);
    }

    private void UpdateCollapsed(double width)
    {
        var collapsed = CollapseWidth > 0 && width > 0 && width < CollapseWidth;
        IsCollapsed = collapsed;
        PseudoClasses.Set(":collapsed", collapsed);
        if (!collapsed && width > 0 && IsOpen) SetCurrentValue(IsOpenProperty, false);
    }

    // Following a link in the open menu closes it, as on the web.
    private void OnTapped(object? sender, TappedEventArgs e)
    {
        if (IsOpen && e.Source is Visual v && v.FindAncestorOfType<NavbarLink>(includeSelf: true) is not null)
            SetCurrentValue(IsOpenProperty, false);
    }
}

/// <summary>A link in a <see cref="Navbar"/>; selected, it is the current page (the web's <c>aria-current="page"</c>).</summary>
public class NavbarLink : ListBoxItem
{
    protected override Type StyleKeyOverride => typeof(NavbarLink);
}
