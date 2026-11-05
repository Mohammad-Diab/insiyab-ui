using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.LogicalTree;
using Avalonia.Media;

namespace Insiyab.Avalonia.Controls;

/// <summary>One piece of a rich paragraph: its text and how it is set. Pieces that turn inside a joined Arabic word are evened out so the word keeps its joins.</summary>
public sealed class RichSpan(string text)
{
    public string Text { get; } = text ?? "";

    public bool Bold { get; init; }

    /// <summary>Emphasis: Insiyab draws it in the brand colour, never as a slant (Arabic has no italic), as the web's <c>em</c>.</summary>
    public bool Italic { get; init; }

    public bool Underline { get; init; }

    public bool Strike { get; init; }

    /// <summary>Code in the sentence: the mono face on the control ground.</summary>
    public bool Code { get; init; }

    /// <summary>Marked in the brand's soft tint, as the web's <c>mark</c>.</summary>
    public bool Mark { get; init; }

    /// <summary>The text in a tone's colour: <c>ok</c>, <c>warn</c>, <c>bad</c>, <c>info</c> or <c>brand</c>; never a raw colour, which could vanish on the dark theme.</summary>
    public string? Tone { get; init; }

    /// <summary>The address the piece links to; a click raises <see cref="RichText.LinkClickEvent"/>.</summary>
    public string? Link { get; init; }

    internal bool SameLook(RichSpan o) =>
        Bold == o.Bold && Italic == o.Italic && Underline == o.Underline && Strike == o.Strike && Code == o.Code && Mark == o.Mark && Tone == o.Tone && Link == o.Link;

    internal RichSpan With(string text) => new(text) { Bold = Bold, Italic = Italic, Underline = Underline, Strike = Strike, Code = Code, Mark = Mark, Tone = Tone, Link = Link };

    internal RichSpan Linked(string url) => new(Text) { Bold = Bold, Italic = Italic, Underline = Underline, Strike = Strike, Code = Code, Mark = Mark, Tone = Tone, Link = url };
}

/// <summary>A link in rich text was clicked: <see cref="Url"/> is its address and <see cref="Text"/> its words. The app decides whether to open it.</summary>
public sealed class RichLinkEventArgs : RoutedEventArgs
{
    internal RichLinkEventArgs(string url, string text) : base(RichText.LinkClickEvent)
    {
        Url = url;
        Text = text;
    }

    public string Url { get; }

    public string Text { get; }
}

/// <summary>A document of paragraphs with the <see cref="Prose"/> look whose text selects as one: a drag runs across paragraphs (scrolling the page at its edge), Shift with the arrows, Home, End and Ctrl moves the selection's end over them, Ctrl+A takes it all and Ctrl+C copies it in reading order, a list item with its marker. Its children are paragraphs as in a Prose (SelectableTextBlocks select); or set <see cref="Text"/> for plain text, a paragraph per blank line, each in its own direction with its addresses as links. Rich pieces come from <see cref="SpansProperty"/> or from inline spans in markup (<c>Span.strong</c>, <c>em</c>, <c>code</c>, <c>mark</c>, and <see cref="LinkProperty"/> on a Span). <see cref="SelectsAcrossProperty"/> gives the same selection to any container, such as an <c>ItemsControl.prose</c>.</summary>
public partial class RichText : Prose
{
    /// <summary>Plain text for the whole document: each block between blank lines is a paragraph in its own script's direction, its addresses links.</summary>
    public static readonly StyledProperty<string?> TextProperty =
        AvaloniaProperty.Register<RichText, string?>(nameof(Text));

    /// <summary>Raised from the paragraph when a link in it is clicked (pressed and let go on it, without a drag); it bubbles, and nothing opens unless the app opens it.</summary>
    public static readonly RoutedEvent<RichLinkEventArgs> LinkClickEvent =
        RoutedEvent.Register<RichText, RichLinkEventArgs>("LinkClick", RoutingStrategies.Bubble);

    /// <summary>On any container: its SelectableTextBlocks select as one text, as in a RichText.</summary>
    public static readonly AttachedProperty<bool> SelectsAcrossProperty =
        AvaloniaProperty.RegisterAttached<RichText, Control, bool>("SelectsAcross");

    /// <summary>On a TextBlock or SelectableTextBlock: its text as rich pieces, in place of <c>Text</c>.</summary>
    public static readonly AttachedProperty<IEnumerable<RichSpan>?> SpansProperty =
        AvaloniaProperty.RegisterAttached<RichText, TextBlock, IEnumerable<RichSpan>?>("Spans");

    /// <summary>On a TextBlock or SelectableTextBlock: the web addresses in its text (http, https or www.) become links.</summary>
    public static readonly AttachedProperty<bool> LinksProperty =
        AvaloniaProperty.RegisterAttached<RichText, TextBlock, bool>("Links");

    /// <summary>On a Span inside a TextBlock: the address it links to, with the link look; a click raises <see cref="LinkClickEvent"/>.</summary>
    public static readonly AttachedProperty<string?> LinkProperty =
        AvaloniaProperty.RegisterAttached<RichText, Span, string?>("Link");

    private static readonly ConditionalWeakTable<Control, TextSelection> s_selections = new();
    private static readonly ConditionalWeakTable<TextBlock, LinkHost> s_hosts = new();
    private static readonly ConditionalWeakTable<TextBlock, object> s_filling = new();

    static RichText()
    {
        TextProperty.Changed.AddClassHandler<RichText>((r, _) => r.Write());
        SelectsAcrossProperty.Changed.AddClassHandler<Control>((c, e) =>
        {
            if (s_selections.TryGetValue(c, out var old))
            {
                old.Detach();
                s_selections.Remove(c);
            }
            if (e.GetNewValue<bool>()) s_selections.Add(c, new TextSelection(c));
        });
        SpansProperty.Changed.AddClassHandler<TextBlock>((t, _) => Fill(t));
        LinksProperty.Changed.AddClassHandler<TextBlock>((t, e) =>
        {
            t.PropertyChanged -= OnLinkedText;
            if (e.GetNewValue<bool>()) t.PropertyChanged += OnLinkedText;
            Fill(t);
        });
        LinkProperty.Changed.AddClassHandler<Span>((span, e) =>
        {
            span.Classes.Set("a", e.GetNewValue<string?>() is not null);
            span.AttachedToLogicalTree -= OnLinkAttached;
            if (e.GetNewValue<string?>() is null) return;
            if (span.FindLogicalAncestorOfType<TextBlock>() is { } host) LinkHost.For(host);
            else span.AttachedToLogicalTree += OnLinkAttached;
        });
    }

    public RichText() => SetSelectsAcross(this, true);

    // A paragraph whose addresses are links finds them again when its plain text changes.
    private static void OnLinkedText(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property == TextBlock.TextProperty && sender is TextBlock t && GetSpans(t) is null && !s_filling.TryGetValue(t, out _)) Fill(t);
    }

    // A link written in markup gets its paragraph only once it is placed in it.
    private static void OnLinkAttached(object? sender, LogicalTreeAttachmentEventArgs e)
    {
        if (sender is not Span span) return;
        span.AttachedToLogicalTree -= OnLinkAttached;
        if (span.FindLogicalAncestorOfType<TextBlock>() is { } host) LinkHost.For(host);
    }

    protected override Type StyleKeyOverride => typeof(Prose);

    public string? Text { get => GetValue(TextProperty); set => SetValue(TextProperty, value); }

    public event EventHandler<RichLinkEventArgs>? LinkClick
    {
        add => AddHandler(LinkClickEvent, value);
        remove => RemoveHandler(LinkClickEvent, value);
    }

    public static bool GetSelectsAcross(Control c) => c.GetValue(SelectsAcrossProperty);
    public static void SetSelectsAcross(Control c, bool value) => c.SetValue(SelectsAcrossProperty, value);
    public static IEnumerable<RichSpan>? GetSpans(TextBlock t) => t.GetValue(SpansProperty);
    public static void SetSpans(TextBlock t, IEnumerable<RichSpan>? value) => t.SetValue(SpansProperty, value);
    public static bool GetLinks(TextBlock t) => t.GetValue(LinksProperty);
    public static void SetLinks(TextBlock t, bool value) => t.SetValue(LinksProperty, value);
    public static string? GetLink(Span s) => s.GetValue(LinkProperty);
    public static void SetLink(Span s, string? value) => s.SetValue(LinkProperty, value);

    internal static TextSelection? SelectionOf(Control host) => s_selections.TryGetValue(host, out var s) ? s : null;

    // The plain text as paragraphs: the ones written before are replaced, so children the app adds itself stay as they are without Text.
    private void Write()
    {
        Children.Clear();
        if (string.IsNullOrEmpty(Text)) return;
        foreach (var part in Paragraphs().Split(Text.Replace("\r\n", "\n")))
        {
            var said = part.Trim('\n');
            if (said.Trim().Length == 0) continue;
            var block = new SelectableTextBlock { Text = said };
            TextDirection.SetIsAuto(block, true);
            SetLinks(block, true);
            Children.Add(block);
        }
    }

    // A text block's inlines from its pieces (or its plain text), its addresses cut out as links and no style turning inside a joined Arabic word.
    private static void Fill(TextBlock block)
    {
        var given = GetSpans(block)?.ToList();
        if (given is null && !GetLinks(block)) return;
        var spans = given ?? [new RichSpan(block.Text ?? "")];
        spans = WholeWords(spans);
        if (GetLinks(block)) spans = FindLinks(spans);
        s_filling.AddOrUpdate(block, new object());
        try
        {
            var inlines = new InlineCollection();
            Span? link = null;
            foreach (var piece in spans)
            {
                if (piece.Text.Length == 0) continue;
                var run = Piece(block, piece);
                if (piece.Link is { } url)
                {
                    if (link is null || GetLink(link) != url)
                    {
                        link = new Span();
                        SetLink(link, url);
                        inlines.Add(link);
                    }
                    link.Inlines.Add(run);
                    continue;
                }
                link = null;
                inlines.Add(run);
            }
            block.Inlines = inlines;
        }
        finally
        {
            s_filling.Remove(block);
        }
        LinkHost.For(block);
    }

    private static Run Piece(TextBlock host, RichSpan piece)
    {
        var run = new Run(piece.Text);
        if (piece.Bold) run.Classes.Add("strong");
        if (piece.Italic) run.Classes.Add("em");
        if (piece.Code) run.Classes.Add("code");
        if (piece.Mark) run.Classes.Add("mark");
        if (piece.Underline || piece.Strike)
        {
            var lines = new TextDecorationCollection();
            // The font's own underline sits on the baseline, hairline thin, where it melts into Arabic's joining strokes: drawn a little under it instead.
            if (piece.Underline)
                lines.Add(new TextDecoration
                {
                    Location = TextDecorationLocation.Underline,
                    StrokeThickness = 0.07,
                    StrokeThicknessUnit = TextDecorationUnit.FontRenderingEmSize,
                    StrokeOffset = 0.14,
                    StrokeOffsetUnit = TextDecorationUnit.FontRenderingEmSize,
                });
            if (piece.Strike) lines.AddRange(TextDecorations.Strikethrough);
            run.TextDecorations = lines;
        }
        if (ToneKey(piece.Tone) is { } key) run.Bind(TextElement.ForegroundProperty, host.GetResourceObservable(key));
        return run;
    }

    private static string? ToneKey(string? tone) => tone?.Trim() switch
    {
        "ok" => "InsToneOk",
        "warn" => "InsToneWarn",
        "bad" => "InsToneBad",
        "info" => "InsToneInfo",
        "brand" => "InsToneBrand",
        _ => null,
    };

    // A letter that joins its neighbours: Arabic letters and marks, and the tatweel.
    private static bool Joins(char c) => c is >= 'ؐ' and <= 'ؚ' or >= 'ؠ' and <= 'ٟ' or >= 'ٮ' and <= 'ۓ' or >= 'ە' and <= 'ۿ'
        or >= 'ݐ' and <= 'ݿ' or >= 'ࢠ' and <= 'ࣿ' or >= 'ﭐ' and <= '﷿' or >= 'ﹰ' and <= '﻿';

    // No style turns inside a joined Arabic word, or the shaper breaks its joins: the word takes the style most of its letters have, a tie going to its first letter's.
    internal static List<RichSpan> WholeWords(List<RichSpan> spans)
    {
        if (spans.Count < 2) return spans;
        var text = string.Concat(spans.Select(s => s.Text));
        var owner = new int[text.Length];
        var at = 0;
        for (var i = 0; i < spans.Count; i++)
            for (var k = 0; k < spans[i].Text.Length; k++) owner[at++] = i;
        var changed = false;
        for (var start = 0; start < text.Length;)
        {
            if (!Joins(text[start])) { start++; continue; }
            var end = start;
            while (end < text.Length && Joins(text[end])) end++;
            var first = owner[start];
            var mixed = false;
            for (var k = start; k < end; k++) mixed |= !spans[owner[k]].SameLook(spans[first]);
            if (mixed)
            {
                var counts = new Dictionary<int, int>();
                for (var k = start; k < end; k++) counts[owner[k]] = counts.GetValueOrDefault(owner[k]) + 1;
                var best = first;
                foreach (var (span, n) in counts) if (n > counts[best]) best = span;
                for (var k = start; k < end; k++) owner[k] = best;
                changed = true;
            }
            start = end;
        }
        if (!changed) return spans;
        var pieces = new List<RichSpan>();
        for (var k = 0; k < text.Length;)
        {
            var from = k;
            var look = spans[owner[k]];
            while (k < text.Length && spans[owner[k]].SameLook(look)) k++;
            pieces.Add(look.With(text[from..k]));
        }
        return pieces;
    }

    [GeneratedRegex(@"(?:https?://|(?<![A-Za-z0-9./@-])www\.)[^\s<>""'«»()\[\]{}\p{IsArabic}​-‏‪-‮]+", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex Address();

    [GeneratedRegex(@"\n[ \t]*\n")]
    private static partial Regex Paragraphs();

    /// <summary>The web addresses in a text, as start, length and the address to open; closing sentence marks are left out.</summary>
    internal static List<(int Start, int Length, string Url)> Addresses(string text)
    {
        var found = new List<(int, int, string)>();
        foreach (Match m in Address().Matches(text))
        {
            var length = m.Length;
            while (length > 0 && m.Value[length - 1] is '.' or ',' or '،' or ';' or '؛' or ':' or '!' or '?' or '؟' or '۔') length--;
            if (length == 0) continue;
            var said = m.Value[..length];
            found.Add((m.Index, length, said.StartsWith("www.", StringComparison.OrdinalIgnoreCase) ? "https://" + said : said));
        }
        return found;
    }

    // The pieces cut where an address starts and ends, each piece inside an address taking its link unless it has its own.
    private static List<RichSpan> FindLinks(List<RichSpan> spans)
    {
        var text = string.Concat(spans.Select(s => s.Text));
        var links = Addresses(text);
        if (links.Count == 0) return spans;
        var pieces = new List<RichSpan>();
        var at = 0;
        foreach (var span in spans)
        {
            var from = 0;
            for (var i = 0; i <= span.Text.Length; i++)
            {
                var here = at + i;
                var cut = i == span.Text.Length || (i > 0 && links.Any(l => l.Start == here || l.Start + l.Length == here));
                if (!cut) continue;
                if (i > from)
                {
                    var piece = span.With(span.Text[from..i]);
                    var start = at + from;
                    var inside = links.FirstOrDefault(l => start >= l.Start && start < l.Start + l.Length);
                    pieces.Add(inside.Url is { } url && span.Link is null ? piece.Linked(url) : piece);
                }
                from = i;
            }
            at += span.Text.Length;
        }
        return pieces;
    }

    // A paragraph's links: the hand and the hover look on the one under the pointer, and a click raised for a press let go on the same link close to where it began, with nothing selected.
    private sealed class LinkHost
    {
        private readonly TextBlock _block;
        private Span? _hot, _pressed;
        private Point _pressAt;
        private static Cursor? s_hand;

        private LinkHost(TextBlock block)
        {
            _block = block;
            block.AddHandler(PointerPressedEvent, OnPressed, RoutingStrategies.Bubble, handledEventsToo: true);
            block.AddHandler(PointerReleasedEvent, OnReleased, RoutingStrategies.Bubble, handledEventsToo: true);
            block.AddHandler(PointerMovedEvent, OnMoved, RoutingStrategies.Bubble, handledEventsToo: true);
            block.AddHandler(PointerExitedEvent, OnExited, RoutingStrategies.Bubble, handledEventsToo: true);
        }

        public static void For(TextBlock block)
        {
            if (!s_hosts.TryGetValue(block, out _)) s_hosts.Add(block, new LinkHost(block));
        }

        // The links of the block with their place in its text, counted as the inlines build their runs.
        private IEnumerable<(Span Link, int Start, int End)> Links()
        {
            var found = new List<(Span, int, int)>();
            var at = 0;
            Walk(_block.Inlines, ref at, found);
            return found;
        }

        private static void Walk(InlineCollection? inlines, ref int at, List<(Span, int, int)> found)
        {
            if (inlines is null) return;
            foreach (var inline in inlines)
            {
                var start = at;
                if (inline is Span span)
                {
                    Walk(span.Inlines, ref at, found);
                    if (GetLink(span) is not null) found.Add((span, start, at));
                }
                else if (inline is Run run) at += run.Text?.Length ?? 0;
                else at += 1;
            }
        }

        // The link drawn under a point, by the boxes its text takes in the layout (a hit's own inside flag is wrong in right-to-left lines).
        internal Span? LinkAt(Point point)
        {
            var p = point - new Point(_block.Padding.Left, _block.Padding.Top);
            var layout = _block.TextLayout;
            foreach (var (link, start, end) in Links())
                foreach (var box in layout.HitTestTextRange(start, end - start))
                    if (box.Inflate(1).Contains(p)) return link;
            return null;
        }

        private void Hover(Span? link)
        {
            if (_hot == link) return;
            _hot?.Classes.Remove("hot");
            _hot = link;
            if (link is null)
            {
                _block.ClearValue(InputElement.CursorProperty);
                return;
            }
            link.Classes.Add("hot");
            _block.Cursor = s_hand ??= new Cursor(StandardCursorType.Hand);
        }

        private void OnMoved(object? sender, PointerEventArgs e)
        {
            if (!e.GetCurrentPoint(_block).Properties.IsLeftButtonPressed) Hover(LinkAt(e.GetPosition(_block)));
        }

        private void OnExited(object? sender, PointerEventArgs e) => Hover(null);

        private void OnPressed(object? sender, PointerPressedEventArgs e)
        {
            _pressed = null;
            if (!e.GetCurrentPoint(_block).Properties.IsLeftButtonPressed || e.ClickCount != 1 || e.KeyModifiers.HasFlag(KeyModifiers.Shift)) return;
            _pressAt = e.GetPosition(_block);
            _pressed = LinkAt(_pressAt);
        }

        private void OnReleased(object? sender, PointerReleasedEventArgs e)
        {
            if (_pressed is not { } link || e.InitialPressMouseButton != MouseButton.Left) return;
            _pressed = null;
            var at = e.GetPosition(_block);
            if (Math.Abs(at.X - _pressAt.X) > 4 || Math.Abs(at.Y - _pressAt.Y) > 4 || LinkAt(at) != link) return;
            if (_block is SelectableTextBlock s && s.SelectionStart != s.SelectionEnd) return;
            if (GetLink(link) is not { } url) return;
            var words = string.Concat(Runs(link));
            _block.RaiseEvent(new RichLinkEventArgs(url, words) { Source = _block });
        }

        private static IEnumerable<string> Runs(Span span)
        {
            foreach (var inline in span.Inlines)
            {
                if (inline is Run run) yield return run.Text ?? "";
                else if (inline is Span inner) foreach (var part in Runs(inner)) yield return part;
            }
        }
    }
}
