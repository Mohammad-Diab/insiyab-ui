using System.Text;
using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.VisualTree;

namespace Insiyab.Avalonia.Controls;

/// <summary>Marks the parts of a screen that <see cref="FindBar"/> searches: every TextBlock (and SelectableTextBlock) inside a container with <c>ins:Find.IsSearchable="True"</c>.</summary>
public static class Find
{
    public static readonly AttachedProperty<bool> IsSearchableProperty =
        AvaloniaProperty.RegisterAttached<Control, bool>("IsSearchable", typeof(Find));

    public static bool GetIsSearchable(Control control) => control.GetValue(IsSearchableProperty);

    public static void SetIsSearchable(Control control, bool value) => control.SetValue(IsSearchableProperty, value);

    /// <summary>Where <paramref name="query"/> occurs in <paramref name="text"/>, as start and length in the text itself, matched the Arabic-aware way of <see cref="ArabicSearch.Normalize"/>: diacritics, tatweel and hamza seats ignored, ة with ه, ى with ي, either set of digits.</summary>
    public static IReadOnlyList<(int Start, int Length)> Matches(string? text, string? query)
    {
        var q = ArabicSearch.Normalize(query);
        if (q.Length == 0 || string.IsNullOrEmpty(text)) return [];
        var map = new List<int>(text.Length);
        var folded = new StringBuilder(text.Length);
        for (var i = 0; i < text.Length; i++)
        {
            var ch = text[i];
            if (char.IsSurrogate(ch))
            {
                folded.Append(ch);
                map.Add(i);
                continue;
            }
            foreach (var c in char.ToLowerInvariant(ch).ToString().Normalize(NormalizationForm.FormD))
            {
                if (c is >= '̀' and <= 'ͯ' or >= 'ً' and <= 'ٟ' or 'ٰ' or 'ـ') continue;
                folded.Append(c switch
                {
                    'ة' => 'ه',
                    'ى' => 'ي',
                    >= '٠' and <= '٩' => (char)('0' + (c - '٠')),
                    >= '۰' and <= '۹' => (char)('0' + (c - '۰')),
                    _ => c,
                });
                map.Add(i);
            }
        }
        var hay = folded.ToString();
        var found = new List<(int, int)>();
        for (var at = hay.IndexOf(q, StringComparison.Ordinal); at >= 0; at = hay.IndexOf(q, at + q.Length, StringComparison.Ordinal))
        {
            var start = map[at];
            var end = at + q.Length < map.Count ? map[at + q.Length] : text.Length;
            found.Add((start, end - start));
        }
        return found;
    }
}

/// <summary>A small find bar, Ctrl+F's: a field, the match count, previous and next, and close. It highlights the matches in the containers marked <see cref="Find.IsSearchableProperty"/> (or in <see cref="Target"/>), and scrolls the current one into view. Enter and Shift+Enter (or F3 and Shift+F3) step through them; Esc closes it.</summary>
[TemplatePart("PART_Field", typeof(TextBox))]
[TemplatePart("PART_Count", typeof(TextBlock))]
[TemplatePart("PART_Prev", typeof(Button))]
[TemplatePart("PART_Next", typeof(Button))]
[TemplatePart("PART_Close", typeof(Button))]
[PseudoClasses(":open", ":none")]
public class FindBar : TemplatedControl
{
    public static readonly StyledProperty<bool> IsOpenProperty =
        AvaloniaProperty.Register<FindBar, bool>(nameof(IsOpen));

    public static readonly StyledProperty<Control?> TargetProperty =
        AvaloniaProperty.Register<FindBar, Control?>(nameof(Target));

    public static readonly StyledProperty<string?> TextProperty =
        AvaloniaProperty.Register<FindBar, string?>(nameof(Text));

    public static readonly DirectProperty<FindBar, int> MatchCountProperty =
        AvaloniaProperty.RegisterDirect<FindBar, int>(nameof(MatchCount), o => o.MatchCount);

    public static readonly DirectProperty<FindBar, int> CurrentMatchProperty =
        AvaloniaProperty.RegisterDirect<FindBar, int>(nameof(CurrentMatch), o => o.CurrentMatch);

    private readonly List<FindMatch> _matches = [];
    private TextBox? _field;
    private TextBlock? _count;
    private Button? _prev;
    private Button? _next;
    private Button? _close;
    private TopLevel? _top;
    private FindHighlights? _marks;
    private IInputElement? _before;
    private int _matchCount;
    private int _current;

    /// <summary>Whether the bar shows; Ctrl+F opens it, Esc and its close button shut it.</summary>
    public bool IsOpen { get => GetValue(IsOpenProperty); set => SetValue(IsOpenProperty, value); }

    /// <summary>The one container to search; unset, every container marked <see cref="Find.IsSearchableProperty"/> in the window.</summary>
    public Control? Target { get => GetValue(TargetProperty); set => SetValue(TargetProperty, value); }

    /// <summary>What is searched for: the field's text.</summary>
    public string? Text { get => GetValue(TextProperty); set => SetValue(TextProperty, value); }

    /// <summary>How many matches there are.</summary>
    public int MatchCount { get => _matchCount; private set => SetAndRaise(MatchCountProperty, ref _matchCount, value); }

    /// <summary>The current match, counted from 1; 0 when there is none.</summary>
    public int CurrentMatch { get => _current; private set => SetAndRaise(CurrentMatchProperty, ref _current, value); }

    internal IReadOnlyList<FindMatch> Found => _matches;

    private bool Arabic => FlowDirection == FlowDirection.RightToLeft;

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);
        if (_field is not null) _field.RemoveHandler(KeyDownEvent, OnFieldKey);
        if (_prev is not null) _prev.Click -= OnPrev;
        if (_next is not null) _next.Click -= OnNext;
        if (_close is not null) _close.Click -= OnClose;
        _field = e.NameScope.Find<TextBox>("PART_Field");
        _count = e.NameScope.Find<TextBlock>("PART_Count");
        _prev = e.NameScope.Find<Button>("PART_Prev");
        _next = e.NameScope.Find<Button>("PART_Next");
        _close = e.NameScope.Find<Button>("PART_Close");
        _field?.AddHandler(KeyDownEvent, OnFieldKey, RoutingStrategies.Tunnel);
        if (_prev is not null) _prev.Click += OnPrev;
        if (_next is not null) _next.Click += OnNext;
        if (_close is not null) _close.Click += OnClose;
        Words();
        ShowCount();
    }

    private void OnPrev(object? sender, RoutedEventArgs e) => Step(-1);

    private void OnNext(object? sender, RoutedEventArgs e) => Step(1);

    private void OnClose(object? sender, RoutedEventArgs e) => Close();

    private void Words()
    {
        var ar = Arabic;
        if (_field is not null)
        {
            _field.PlaceholderText = ar ? "ابحث في الصفحة" : "Find in page";
            AutomationProperties.SetName(_field, ar ? "البحث في الصفحة" : "Find in page");
        }
        if (_prev is not null) AutomationProperties.SetName(_prev, ar ? "النتيجة السابقة" : "Previous match");
        if (_next is not null) AutomationProperties.SetName(_next, ar ? "النتيجة التالية" : "Next match");
        if (_close is not null) AutomationProperties.SetName(_close, ar ? "إغلاق البحث" : "Close find");
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        _top = TopLevel.GetTopLevel(this);
        _top?.AddHandler(KeyDownEvent, OnTopKey, RoutingStrategies.Bubble);
        global::Avalonia.Threading.Dispatcher.UIThread.Post(() =>
        {
            if (IsOpen && _top is not null) Search();
        }, global::Avalonia.Threading.DispatcherPriority.Loaded);
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        _top?.RemoveHandler(KeyDownEvent, OnTopKey);
        _top = null;
        Unmark();
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == IsOpenProperty)
        {
            PseudoClasses.Set(":open", IsOpen);
            if (IsOpen) Search();
            else Unmark();
        }
        else if (change.Property == TextProperty || change.Property == TargetProperty)
        {
            if (IsOpen) Search();
        }
        else if (change.Property == FlowDirectionProperty)
        {
            Words();
            ShowCount();
        }
    }

    // Ctrl+F anywhere in the window opens the bar whose page is showing; F3 steps while it is open.
    private void OnTopKey(object? sender, KeyEventArgs e)
    {
        if (e.Handled || Parent is not Visual { IsEffectivelyVisible: true }) return;
        var ctrl = e.KeyModifiers.HasFlag(KeyModifiers.Control) || e.KeyModifiers.HasFlag(KeyModifiers.Meta);
        if (e.Key == Key.F && ctrl && !e.KeyModifiers.HasFlag(KeyModifiers.Shift) && !e.KeyModifiers.HasFlag(KeyModifiers.Alt))
        {
            e.Handled = true;
            Open();
        }
        else if (e.Key == Key.F3 && IsOpen)
        {
            e.Handled = true;
            Step(e.KeyModifiers.HasFlag(KeyModifiers.Shift) ? -1 : 1);
        }
    }

    private void OnFieldKey(object? sender, KeyEventArgs e)
    {
        switch (e.Key)
        {
            case Key.Enter:
                e.Handled = true;
                Step(e.KeyModifiers.HasFlag(KeyModifiers.Shift) ? -1 : 1);
                break;
            case Key.Escape:
                e.Handled = true;
                Close();
                break;
        }
    }

    /// <summary>Shows the bar and puts the keyboard in its field, its text selected.</summary>
    public void Open()
    {
        if (!IsOpen) _before = _top?.FocusManager?.GetFocusedElement();
        SetCurrentValue(IsOpenProperty, true);
        global::Avalonia.Threading.Dispatcher.UIThread.Post(() =>
        {
            if (_field is null || !IsOpen) return;
            _field.Focus(NavigationMethod.Tab);
            _field.SelectAll();
        }, global::Avalonia.Threading.DispatcherPriority.Input);
    }

    /// <summary>Hides the bar and its highlights, and gives the keyboard back to where it was.</summary>
    public void Close()
    {
        SetCurrentValue(IsOpenProperty, false);
        if (_before is InputElement { IsEffectivelyVisible: true } back) back.Focus();
        _before = null;
    }

    /// <summary>Moves to the next match (1) or the previous one (-1), round the ends, and scrolls it into view.</summary>
    public void Step(int by)
    {
        if (_matches.Count == 0) return;
        var at = ((CurrentMatch - 1 + by) % _matches.Count + _matches.Count) % _matches.Count;
        CurrentMatch = at + 1;
        Reveal();
        ShowCount();
        _marks?.InvalidateVisual();
    }

    private IEnumerable<Control> Scopes()
    {
        if (Target is { } t) return [t];
        if (_top is null) return [];
        return _top.GetVisualDescendants().OfType<Control>().Where(c => Find.GetIsSearchable(c) && c.IsEffectivelyVisible);
    }

    private static string? TextOf(TextBlock tb) => tb.Inlines is { Count: > 0 } inlines ? inlines.Text : tb.Text;

    /// <summary>Searches again, as when the text under it changes.</summary>
    public void Search()
    {
        _matches.Clear();
        var query = Text ?? "";
        if (IsOpen && ArabicSearch.Normalize(query).Length > 0)
        {
            var seen = new HashSet<TextBlock>();
            foreach (var scope in Scopes())
                foreach (var tb in scope.GetVisualDescendants().OfType<TextBlock>())
                {
                    if (!tb.IsEffectivelyVisible || !seen.Add(tb) || tb.FindAncestorOfType<FindBar>() is not null) continue;
                    foreach (var (start, length) in Find.Matches(TextOf(tb), query)) _matches.Add(new FindMatch(tb, start, length));
                }
        }
        MatchCount = _matches.Count;
        CurrentMatch = _matches.Count > 0 ? 1 : 0;
        if (IsOpen) Mark();
        if (CurrentMatch > 0) Reveal();
        ShowCount();
        _marks?.InvalidateVisual();
    }

    private void Reveal()
    {
        if (CurrentMatch < 1 || CurrentMatch > _matches.Count) return;
        var m = _matches[CurrentMatch - 1];
        if (m.Rects().FirstOrDefault() is { Width: > 0 } r) m.Block.BringIntoView(r.Inflate(24));
    }

    private void ShowCount()
    {
        if (_count is null) return;
        var none = Text is { Length: > 0 } && _matches.Count == 0;
        PseudoClasses.Set(":none", none);
        _count.Text = _matches.Count == 0
            ? none ? Arabic ? "لا نتائج" : "No results" : ""
            : Arabic ? $"{CurrentMatch} من {_matches.Count}" : $"{CurrentMatch} of {_matches.Count}";
        if (_prev is not null) _prev.IsEnabled = _matches.Count > 0;
        if (_next is not null) _next.IsEnabled = _matches.Count > 0;
    }

    private void Mark()
    {
        if (_marks is not null || _top is null) return;
        var layer = AdornerLayer.GetAdornerLayer(this);
        if (layer is null) return;
        _marks = new FindHighlights(this);
        layer.Children.Add(_marks);
    }

    private void Unmark()
    {
        if (_marks?.Parent is AdornerLayer layer) layer.Children.Remove(_marks);
        _marks = null;
    }

    internal bool IsCurrent(FindMatch m) => CurrentMatch > 0 && CurrentMatch <= _matches.Count && ReferenceEquals(_matches[CurrentMatch - 1], m);
}

/// <summary>One match: where it sits in its TextBlock's text.</summary>
internal sealed class FindMatch(TextBlock block, int start, int length)
{
    public TextBlock Block { get; } = block;
    public int Start { get; } = start;
    public int Length { get; } = length;

    // The match's boxes in the TextBlock's own space, one per line it runs over, from the same hit test that draws a selection.
    public IEnumerable<Rect> Rects()
    {
        var layout = Block.TextLayout;
        var padding = Block.Padding;
        foreach (var r in layout.HitTestTextRange(Start, Length))
            yield return r.Translate(new Vector(padding.Left, padding.Top));
    }
}

/// <summary>The highlights over the matches, drawn in the window's adorner layer so the text itself is never split or reshaped.</summary>
internal sealed class FindHighlights : Control
{
    private readonly FindBar _bar;

    public FindHighlights(FindBar bar)
    {
        _bar = bar;
        IsHitTestVisible = false;
        LayoutUpdated += (_, _) => InvalidateVisual();
        ActualThemeVariantChanged += (_, _) => InvalidateVisual();
    }

    public override void Render(DrawingContext context)
    {
        base.Render(context);
        var theme = ActualThemeVariant;
        var match = _bar.TryFindResource("InsFindMatch", theme, out var a) && a is IBrush ma ? ma : new SolidColorBrush(Color.FromArgb(0x59, 0xFA, 0xCC, 0x15));
        var current = _bar.TryFindResource("InsFindCurrent", theme, out var b) && b is IBrush mb ? mb : new SolidColorBrush(Color.FromArgb(0xB3, 0xF5, 0x9E, 0x0B));
        foreach (var m in _bar.Found)
        {
            if (!m.Block.IsEffectivelyVisible || m.Block.TransformToVisual(this) is not { } toThis) continue;
            var clip = ClipOf(m.Block);
            var brush = _bar.IsCurrent(m) ? current : match;
            foreach (var r in m.Rects())
            {
                var box = r.TransformToAABB(toThis).Inflate(new Thickness(1, 0));
                if (clip is { } c) box = box.Intersect(c);
                if (box.Width > 0 && box.Height > 0) context.DrawRectangle(brush, null, box, 3, 3);
            }
        }
    }

    // A match scrolled out of its box stays hidden: the nearest scrolling box is the clip.
    private Rect? ClipOf(Visual block)
    {
        var viewer = block.FindAncestorOfType<ScrollViewer>();
        if (viewer is null || viewer.TransformToVisual(this) is not { } m) return null;
        return new Rect(viewer.Bounds.Size).TransformToAABB(m);
    }
}
