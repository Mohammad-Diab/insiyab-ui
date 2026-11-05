using System.Collections;
using System.Collections.Specialized;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using Avalonia.LogicalTree;
using Avalonia.Metadata;
using Avalonia.Threading;
using Avalonia.Utilities;

namespace Insiyab.Avalonia.Controls;

/// <summary>Facts about one thing as label and value, the web's <c>.ins-dl</c>: a column of labels with the values beside them, each label above its value when the list is narrower than 26rem. Class <c>grid</c> sets the pairs out in columns instead, as <c>.ins-dl--grid</c>.</summary>
public class DescriptionList : Panel
{
    public static readonly StyledProperty<IEnumerable?> ItemsSourceProperty =
        AvaloniaProperty.Register<DescriptionList, IEnumerable?>(nameof(ItemsSource));

    public static readonly StyledProperty<IDataTemplate?> ValueTemplateProperty =
        AvaloniaProperty.Register<DescriptionList, IDataTemplate?>(nameof(ValueTemplate));

    /// <summary>Narrower than this, a label goes above its value (26rem).</summary>
    internal const double StackBelow = 416;

    private const double GridColumn = 192;
    private const double GridGapX = 24;
    private const double GridGapY = 20;

    private readonly Listener _listener;
    private INotifyCollectionChanged? _watched;
    private BindingBase? _term;
    private BindingBase? _value;

    public DescriptionList()
    {
        _listener = new Listener(this);
        Classes.CollectionChanged += (_, e) =>
        {
            if (e.Action == NotifyCollectionChangedAction.Reset || (e.NewItems?.Contains("grid") ?? false) || (e.OldItems?.Contains("grid") ?? false)) InvalidateMeasure();
        };
    }

    /// <summary>Items to make one pair each from, after any pairs written by hand; the pairs are reused by position as the items change.</summary>
    public IEnumerable? ItemsSource { get => GetValue(ItemsSourceProperty); set => SetValue(ItemsSourceProperty, value); }

    /// <summary>For items from <see cref="ItemsSource"/>: the label of each pair.</summary>
    [AssignBinding]
    [InheritDataTypeFromItems(nameof(ItemsSource))]
    public BindingBase? TermBinding { get => _term; set { _term = value; Rebind(); } }

    /// <summary>For items from <see cref="ItemsSource"/>: the value of each pair; without it the value is the item itself.</summary>
    [AssignBinding]
    [InheritDataTypeFromItems(nameof(ItemsSource))]
    public BindingBase? ValueBinding { get => _value; set { _value = value; Rebind(); } }

    /// <summary>For items from <see cref="ItemsSource"/>: draws each value; without it a text is selectable and a control shows as itself.</summary>
    [InheritDataTypeFromItems(nameof(ItemsSource))]
    public IDataTemplate? ValueTemplate { get => GetValue(ValueTemplateProperty); set => SetValue(ValueTemplateProperty, value); }

    private bool IsGrid => Classes.Contains("grid");

    private int ColumnsFor(double width) =>
        !IsGrid || double.IsInfinity(width) ? 1 : Math.Max(1, (int)Math.Floor((width + GridGapX) / (GridColumn + GridGapX)));

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == ItemsSourceProperty)
        {
            if (_watched is not null) WeakEvents.CollectionChanged.Unsubscribe(_watched, _listener);
            _watched = change.GetNewValue<IEnumerable?>() as INotifyCollectionChanged;
            if (_watched is not null) WeakEvents.CollectionChanged.Subscribe(_watched, _listener);
            Fill();
        }
        else if (change.Property == ValueTemplateProperty) Rebind();
    }

    private List<DescriptionRow> Live() => Children.OfType<DescriptionRow>().Where(r => !r.IsLeaving).ToList();

    // A list that is showing moves as its pairs come and go; the first fill, a list out of sight and a still window change at once.
    private void Fill()
    {
        var items = ItemsSource?.Cast<object?>().ToList() ?? [];
        var live = Live();
        var animate = IsEffectivelyVisible && Bounds.Height > 0 && !Motion.IsStill(this);
        for (var i = 0; i < items.Count; i++)
        {
            if (i < live.Count)
            {
                Apply(live[i].Item, items[i]);
                continue;
            }
            var row = new DescriptionRow(animate);
            Apply(row.Item, items[i]);
            Children.Add(row);
            if (animate) Dispatcher.UIThread.Post(row.Open, DispatcherPriority.Loaded);
        }
        for (var i = items.Count; i < live.Count; i++) live[i].Leave(animate);
    }

    private void Rebind()
    {
        foreach (var row in Live()) Apply(row.Item, row.Item.DataContext);
    }

    private void Apply(DescriptionItem item, object? data)
    {
        item.DataContext = data;
        if (TermBinding is { } term) item.Bind(DescriptionItem.TermProperty, term);
        else item.ClearValue(DescriptionItem.TermProperty);
        if (ValueBinding is { } value) item.Bind(ContentControl.ContentProperty, value);
        else item.Content = data;
        if (ValueTemplate is { } template) item.ContentTemplate = template;
        else item.ClearValue(ContentControl.ContentTemplateProperty);
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        var grid = IsGrid;
        var stacked = grid || availableSize.Width < StackBelow;
        foreach (var child in Children)
            (child as DescriptionItem ?? (child as DescriptionRow)?.Item)?.SetLayout(stacked, grid);

        var columns = ColumnsFor(availableSize.Width);
        var cellWidth = columns == 1 ? availableSize.Width : Math.Floor((availableSize.Width - (columns - 1) * GridGapX) / columns);
        var height = 0.0;
        var rowHeight = 0.0;
        var width = 0.0;
        var visible = 0;
        foreach (var child in Children)
        {
            child.Measure(new Size(cellWidth, double.PositiveInfinity));
            if (!child.IsVisible) continue;
            width = Math.Max(width, child.DesiredSize.Width);
            rowHeight = Math.Max(rowHeight, child.DesiredSize.Height);
            visible++;
            if (visible % columns == 0)
            {
                height += rowHeight + (grid ? GridGapY : 0);
                rowHeight = 0;
            }
        }
        height += rowHeight;
        if (grid && visible > 0 && visible % columns == 0) height -= GridGapY;
        return new Size(columns == 1 ? width : availableSize.Width, height);
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        var grid = IsGrid;
        var columns = ColumnsFor(finalSize.Width);
        var cellWidth = columns == 1 ? finalSize.Width : Math.Floor((finalSize.Width - (columns - 1) * GridGapX) / columns);
        var y = 0.0;
        var rowHeight = 0.0;
        var col = 0;
        foreach (var child in Children)
        {
            if (!child.IsVisible) continue;
            child.Arrange(new Rect(col * (cellWidth + GridGapX), y, cellWidth, child.DesiredSize.Height));
            rowHeight = Math.Max(rowHeight, child.DesiredSize.Height);
            if (++col == columns)
            {
                col = 0;
                y += rowHeight + (grid ? GridGapY : 0);
                rowHeight = 0;
            }
        }
        return finalSize;
    }

    // Held weakly by the items' collection, so a long-lived list of items never keeps the page alive.
    private sealed class Listener(DescriptionList owner) : IWeakEventSubscriber<NotifyCollectionChangedEventArgs>
    {
        public void OnEvent(object? sender, WeakEvent ev, NotifyCollectionChangedEventArgs e) => owner.Fill();
    }
}

/// <summary>A pair made from <see cref="DescriptionList.ItemsSource"/>: it unfolds in when it comes after the list was shown and folds away before it leaves.</summary>
internal sealed class DescriptionRow : Fold
{
    public DescriptionRow(bool folded)
    {
        Bleed = 40;
        Child = Item;
        IsOpen = !folded;
    }

    protected override Type StyleKeyOverride => typeof(Fold);

    public DescriptionItem Item { get; } = new();

    public bool IsLeaving { get; private set; }

    public void Open()
    {
        if (!IsLeaving) IsOpen = true;
    }

    public void Leave(bool animate)
    {
        IsLeaving = true;
        if (!animate || !IsVisible)
        {
            Remove();
            return;
        }
        IsOpen = false;
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == IsVisibleProperty && IsLeaving && !change.GetNewValue<bool>()) Remove();
    }

    private void Remove() => (Parent as Panel)?.Children.Remove(this);
}

/// <summary>One label and its value in a <see cref="DescriptionList"/>: the web's <c>&lt;div&gt;&lt;dt&gt;&lt;dd&gt;</c>. Class <c>num</c> keeps a figure in its own direction, starting where the other values start.</summary>
[PseudoClasses(":stacked", ":grid", ":first", ":last")]
public class DescriptionItem : ContentControl
{
    public static readonly StyledProperty<object?> TermProperty =
        AvaloniaProperty.Register<DescriptionItem, object?>(nameof(Term));

    private IChildIndexProvider? _siblings;
    private ILogical? _row;

    /// <summary>The label: the web's <c>&lt;dt&gt;</c>.</summary>
    public object? Term { get => GetValue(TermProperty); set => SetValue(TermProperty, value); }

    internal void SetLayout(bool stacked, bool grid)
    {
        PseudoClasses.Set(":stacked", stacked);
        PseudoClasses.Set(":grid", grid);
    }

    // The first and last pair (:first, :last) are watched here: an :nth-child rule in the theme held a removed item through its parent. A pair made from data counts by the fold that holds it.
    protected override void OnAttachedToLogicalTree(LogicalTreeAttachmentEventArgs e)
    {
        base.OnAttachedToLogicalTree(e);
        var parent = ((ILogical)this).LogicalParent;
        _row = parent is DescriptionRow ? parent : this;
        _siblings = _row.LogicalParent as IChildIndexProvider;
        if (_siblings is not null) _siblings.ChildIndexChanged += OnSiblings;
        Place();
    }

    protected override void OnDetachedFromLogicalTree(LogicalTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromLogicalTree(e);
        if (_siblings is not null) _siblings.ChildIndexChanged -= OnSiblings;
        _siblings = null;
        _row = null;
    }

    private void OnSiblings(object? sender, ChildIndexChangedEventArgs e) => Place();

    private void Place()
    {
        var i = _row is null ? -1 : _siblings?.GetChildIndex(_row) ?? -1;
        PseudoClasses.Set(":first", i == 0);
        PseudoClasses.Set(":last", i >= 0 && _siblings!.TryGetTotalCount(out var count) && i == count - 1);
    }
}
