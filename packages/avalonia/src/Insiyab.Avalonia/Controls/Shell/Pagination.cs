using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;

namespace Insiyab.Avalonia.Controls;

/// <summary>The web's <c>.ins-pagination</c>, built from <see cref="PageCount"/> and <see cref="CurrentPage"/>: previous and next with their arrows, the first and last pages, the pages around the current one, and gaps between.</summary>
[TemplatePart("PART_Items", typeof(Panel))]
public class Pagination : TemplatedControl
{
    public static readonly StyledProperty<int> PageCountProperty =
        AvaloniaProperty.Register<Pagination, int>(nameof(PageCount), 1, coerce: (_, v) => Math.Max(1, v));

    /// <summary>The current page, from 1.</summary>
    public static readonly StyledProperty<int> CurrentPageProperty =
        AvaloniaProperty.Register<Pagination, int>(nameof(CurrentPage), 1, defaultBindingMode: global::Avalonia.Data.BindingMode.TwoWay,
            coerce: (o, v) => Math.Clamp(v, 1, ((Pagination)o).PageCount));

    /// <summary>Only the two arrows with "3 of 12" between them, for a phone or a side list.</summary>
    public static readonly StyledProperty<bool> IsCompactProperty =
        AvaloniaProperty.Register<Pagination, bool>(nameof(IsCompact));

    public static readonly StyledProperty<string> PreviousTextProperty =
        AvaloniaProperty.Register<Pagination, string>(nameof(PreviousText), "السابق");

    public static readonly StyledProperty<string> NextTextProperty =
        AvaloniaProperty.Register<Pagination, string>(nameof(NextText), "التالي");

    /// <summary>The compact position: {0} is the current page, {1} the count.</summary>
    public static readonly StyledProperty<string> CompactFormatProperty =
        AvaloniaProperty.Register<Pagination, string>(nameof(CompactFormat), "{0} من {1}");

    public static readonly RoutedEvent<RoutedEventArgs> PageChangedEvent =
        RoutedEvent.Register<Pagination, RoutedEventArgs>(nameof(PageChanged), RoutingStrategies.Bubble);

    private Panel? _items;

    public int PageCount { get => GetValue(PageCountProperty); set => SetValue(PageCountProperty, value); }
    public int CurrentPage { get => GetValue(CurrentPageProperty); set => SetValue(CurrentPageProperty, value); }
    public bool IsCompact { get => GetValue(IsCompactProperty); set => SetValue(IsCompactProperty, value); }
    public string PreviousText { get => GetValue(PreviousTextProperty); set => SetValue(PreviousTextProperty, value); }
    public string NextText { get => GetValue(NextTextProperty); set => SetValue(NextTextProperty, value); }
    public string CompactFormat { get => GetValue(CompactFormatProperty); set => SetValue(CompactFormatProperty, value); }

    /// <summary>Raised when a page button, an arrow or code changes <see cref="CurrentPage"/>.</summary>
    public event EventHandler<RoutedEventArgs>? PageChanged
    {
        add => AddHandler(PageChangedEvent, value);
        remove => RemoveHandler(PageChangedEvent, value);
    }

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);
        _items = e.NameScope.Find<Panel>("PART_Items");
        Rebuild();
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == PageCountProperty) CoerceValue(CurrentPageProperty);
        if (change.Property == CurrentPageProperty)
        {
            Rebuild();
            RaiseEvent(new RoutedEventArgs(PageChangedEvent, this));
        }
        else if (change.Property == PageCountProperty || change.Property == IsCompactProperty || change.Property == PreviousTextProperty
                 || change.Property == NextTextProperty || change.Property == CompactFormatProperty)
            Rebuild();
    }

    /// <summary>The pages shown, 0 for a gap: the first and last, and three around the current one, with a lone hidden page shown rather than a gap.</summary>
    internal static IReadOnlyList<int> Window(int current, int count)
    {
        if (count <= 7) return Enumerable.Range(1, count).ToList();
        var start = Math.Clamp(current - 1, 1, count - 2);
        var set = new SortedSet<int> { 1, count, start, start + 1, start + 2 };
        var pages = new List<int>();
        var last = 0;
        foreach (var p in set)
        {
            if (p - last == 2) pages.Add(p - 1);
            else if (p - last > 2) pages.Add(0);
            pages.Add(p);
            last = p;
        }
        return pages;
    }

    private void Rebuild()
    {
        if (_items is null) return;
        _items.Children.Clear();
        var current = CurrentPage;
        var count = PageCount;
        _items.Children.Add(PageButton(PreviousText, current - 1, "prev", current > 1));
        if (IsCompact)
            _items.Children.Add(Gap(string.Format(System.Globalization.CultureInfo.InvariantCulture, CompactFormat, current, count)));
        else
            foreach (var p in Window(current, count))
            {
                if (p == 0) _items.Children.Add(Gap("…"));
                else if (p == current)
                {
                    var here = PageButton(p.ToString(System.Globalization.CultureInfo.InvariantCulture), p, null, true);
                    here.Classes.Add("current");
                    _items.Children.Add(here);
                }
                else _items.Children.Add(PageButton(p.ToString(System.Globalization.CultureInfo.InvariantCulture), p, null, true));
            }
        _items.Children.Add(PageButton(NextText, current + 1, "next", current < count));
    }

    private Button PageButton(string text, int page, string? kind, bool enabled)
    {
        var b = new Button { Content = text, IsEnabled = enabled, Classes = { "page" } };
        if (kind is not null) b.Classes.Add(kind);
        b.Click += (_, _) => { if (!b.Classes.Contains("current")) SetCurrentValue(CurrentPageProperty, page); };
        return b;
    }

    private static TextBlock Gap(string text) => new() { Text = text, Classes = { "page-gap" } };
}
