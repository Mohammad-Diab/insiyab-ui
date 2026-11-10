using System.Collections.Specialized;
using System.Runtime.CompilerServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;

namespace Insiyab.Avalonia.Controls;

/// <summary>Pins the heading of the group at the top of a scrolled list over its rows, the next heading pushing it up as it arrives. Headings are learned from the rows the list has realised, so a virtualized list works too.</summary>
internal sealed class ListPin
{
    public static readonly AttachedProperty<bool> IsOnProperty =
        AvaloniaProperty.RegisterAttached<ListBox, bool>("IsOn", typeof(ListPin));

    private static readonly ConditionalWeakTable<ListBox, ListPin> s_pins = new();

    private readonly ListBox _list;
    private readonly SortedDictionary<int, Heading> _known = [];
    private ScrollViewer? _scroll;
    private Border? _pin;
    private TextBlock? _text;
    private bool _watching;
    private bool _posted;

    private readonly record struct Heading(string Text, double Height, Thickness Padding, CornerRadius Radius);

    static ListPin()
    {
        IsOnProperty.Changed.AddClassHandler<ListBox>((list, e) =>
        {
            if (s_pins.TryGetValue(list, out var old))
            {
                old.Stop();
                s_pins.Remove(list);
            }
            if (e.GetNewValue<bool>()) s_pins.Add(list, new ListPin(list));
        });
    }

    public static bool GetIsOn(ListBox list) => list.GetValue(IsOnProperty);
    public static void SetIsOn(ListBox list, bool value) => list.SetValue(IsOnProperty, value);

    private ListPin(ListBox list)
    {
        _list = list;
        list.TemplateApplied += OnTemplate;
        list.AttachedToVisualTree += OnAttached;
        list.DetachedFromVisualTree += OnDetached;
        ((INotifyCollectionChanged)list.ItemsView).CollectionChanged += OnItems;
        foreach (var c in list.GetVisualDescendants().OfType<Control>())
        {
            if (c.TemplatedParent != list) continue;
            if (c is ScrollViewer { Name: "PART_ScrollViewer" } s) _scroll = s;
            else if (c is Border { Name: "PART_Pin" } b) _pin = b;
            else if (c is TextBlock { Name: "PART_PinText" } t) _text = t;
        }
        if (list.IsAttachedToVisualTree()) Watch(true);
    }

    private void Stop()
    {
        Watch(false);
        _list.TemplateApplied -= OnTemplate;
        _list.AttachedToVisualTree -= OnAttached;
        _list.DetachedFromVisualTree -= OnDetached;
        ((INotifyCollectionChanged)_list.ItemsView).CollectionChanged -= OnItems;
        if (_pin is not null) _pin.IsVisible = false;
    }

    private void OnTemplate(object? sender, TemplateAppliedEventArgs e)
    {
        var was = _watching;
        Watch(false);
        _scroll = e.NameScope.Find<ScrollViewer>("PART_ScrollViewer");
        _pin = e.NameScope.Find<Border>("PART_Pin");
        _text = e.NameScope.Find<TextBlock>("PART_PinText");
        if (was || _list.IsAttachedToVisualTree()) Watch(true);
    }

    private void OnAttached(object? sender, VisualTreeAttachmentEventArgs e) => Watch(true);

    private void OnDetached(object? sender, VisualTreeAttachmentEventArgs e) => Watch(false);

    private void OnItems(object? sender, NotifyCollectionChangedEventArgs e)
    {
        _known.Clear();
        Later();
    }

    private void OnScroll(object? sender, ScrollChangedEventArgs e) => Later();

    private void Watch(bool on)
    {
        if (on == _watching) return;
        _watching = on;
        if (_scroll is not null)
        {
            if (on) _scroll.ScrollChanged += OnScroll;
            else _scroll.ScrollChanged -= OnScroll;
        }
        if (on) Later();
    }

    // The rows are placed by the layout that follows a scroll, so the pin is worked out after it.
    private void Later()
    {
        if (_posted) return;
        _posted = true;
        Dispatcher.UIThread.Post(() =>
        {
            _posted = false;
            if (_watching) Update();
        }, DispatcherPriority.Loaded);
    }

    private void Update()
    {
        if (_scroll is null || _pin is null || _text is null) return;
        var first = int.MaxValue;
        var heads = new List<(int Index, double Top)>();
        foreach (var c in _list.GetRealizedContainers())
        {
            if (c is not ListBoxItem row || !row.IsVisible) continue;
            var index = _list.IndexFromContainer(row);
            if (index < 0 || row.TranslatePoint(default, _scroll) is not { } p) continue;
            if (p.Y + row.Bounds.Height > 0 && index < first) first = index;
            if (!ListRow.GetIsHeading(row)) continue;
            _known[index] = new Heading(TextOf(row), row.Bounds.Height, row.Padding, row.CornerRadius);
            heads.Add((index, p.Y));
        }
        var current = -1;
        foreach (var index in _known.Keys)
        {
            if (index > first) break;
            current = index;
        }
        var inPlace = heads.Any(h => h.Index == current && h.Top >= 0);
        if (current < 0 || first == int.MaxValue || inPlace)
        {
            _pin.IsVisible = false;
            return;
        }
        var heading = _known[current];
        var next = heads.Where(h => h.Index > current && h.Top >= 0).Select(h => h.Top).DefaultIfEmpty(double.PositiveInfinity).Min();
        var pad = _list.Padding;
        _pin.Margin = new Thickness(pad.Left, 0, pad.Right, 0);
        _pin.Height = heading.Height;
        _pin.Padding = heading.Padding;
        _pin.CornerRadius = new CornerRadius(0, 0, heading.Radius.BottomRight, heading.Radius.BottomLeft); // flush on the list's top edge: square on top
        _text.Text = heading.Text;
        var push = Math.Min(0, next - heading.Height);
        if (_pin.RenderTransform is not TranslateTransform { X: 0 } t || t.Y != push) _pin.RenderTransform = new TranslateTransform(0, push);
        _pin.IsVisible = true;
    }

    private static string TextOf(ListBoxItem row) => row.Content switch
    {
        string s => s,
        TextBlock t => t.Text ?? "",
        _ => row.GetVisualDescendants().OfType<TextBlock>().FirstOrDefault(t => !string.IsNullOrEmpty(t.Text))?.Text ?? row.Content?.ToString() ?? "",
    };
}
