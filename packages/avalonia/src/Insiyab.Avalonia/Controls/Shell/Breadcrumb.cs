using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Interactivity;

namespace Insiyab.Avalonia.Controls;

/// <summary>The web's <c>.ins-breadcrumb</c>: a glass trail of crumbs, the last one the current page; the others raise <see cref="ItemClickEvent"/>.</summary>
public class Breadcrumb : ItemsControl
{
    public static readonly RoutedEvent<BreadcrumbClickEventArgs> ItemClickEvent =
        RoutedEvent.Register<Breadcrumb, BreadcrumbClickEventArgs>(nameof(ItemClick), RoutingStrategies.Bubble);

    public Breadcrumb() => AddHandler(Button.ClickEvent, OnCrumbClick);

    /// <summary>A crumb before the current one was clicked.</summary>
    public event EventHandler<BreadcrumbClickEventArgs>? ItemClick
    {
        add => AddHandler(ItemClickEvent, value);
        remove => RemoveHandler(ItemClickEvent, value);
    }

    protected override bool NeedsContainerOverride(object? item, int index, out object? recycleKey) =>
        NeedsContainer<BreadcrumbItem>(item, out recycleKey);

    protected override Control CreateContainerForItemOverride(object? item, int index, object? recycleKey) => new BreadcrumbItem();

    protected override void PrepareContainerForItemOverride(Control container, object? item, int index)
    {
        base.PrepareContainerForItemOverride(container, item, index);
        Mark(container, index);
    }

    protected override void ContainerIndexChangedOverride(Control container, int oldIndex, int newIndex)
    {
        base.ContainerIndexChangedOverride(container, oldIndex, newIndex);
        Mark(container, newIndex);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == ItemCountProperty)
            foreach (var container in GetRealizedContainers())
                Mark(container, IndexFromContainer(container));
    }

    private void Mark(Control container, int index)
    {
        if (container is BreadcrumbItem crumb) crumb.SetPlace(index == 0, index == ItemCount - 1);
    }

    private void OnCrumbClick(object? sender, RoutedEventArgs e)
    {
        if (e.Source is not BreadcrumbItem crumb || crumb.IsCurrent) return;
        e.Handled = true;
        var index = IndexFromContainer(crumb);
        RaiseEvent(new BreadcrumbClickEventArgs(ItemClickEvent, this, ItemFromContainer(crumb), index));
    }
}

/// <summary>One crumb: a link, or the current page when it is the trail's last.</summary>
[PseudoClasses(":first", ":current")]
public class BreadcrumbItem : Button
{
    public static readonly DirectProperty<BreadcrumbItem, bool> IsCurrentProperty =
        AvaloniaProperty.RegisterDirect<BreadcrumbItem, bool>(nameof(IsCurrent), o => o.IsCurrent);

    private bool _isCurrent;

    protected override Type StyleKeyOverride => typeof(BreadcrumbItem);

    /// <summary>The trail's last crumb, the page you are on (the web's <c>aria-current="page"</c>).</summary>
    public bool IsCurrent
    {
        get => _isCurrent;
        private set => SetAndRaise(IsCurrentProperty, ref _isCurrent, value);
    }

    internal void SetPlace(bool first, bool current)
    {
        PseudoClasses.Set(":first", first);
        PseudoClasses.Set(":current", current);
        IsCurrent = current;
    }
}

public sealed class BreadcrumbClickEventArgs : RoutedEventArgs
{
    internal BreadcrumbClickEventArgs(RoutedEvent routedEvent, object source, object? item, int index) : base(routedEvent, source)
    {
        Item = item;
        Index = index;
    }

    public object? Item { get; }
    public int Index { get; }
}
