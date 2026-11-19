using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Rendering;
using Avalonia.VisualTree;

namespace Insiyab.Avalonia.Controls;

// The Borders, TextBlocks and StackPanels the library builds in code: as with TemplateBorder, the app's Border.*, TextBlock.* and StackPanel.* styles and looks never reach them, and each control's parts carry only that control's own styles.

/// <summary>A Border that <see cref="MonthCalendar"/> builds; its styles name it <c>ins|MonthBorder</c>. The ones a press acts on (a day, a linked event or day heading, the hidden hours' band, the compact «+N») show <c>:pressed</c> while a finger or the left button is down on them, as a button does, so they draw the pressed colour (<c>Press.axaml</c>) and <see cref="PressHold"/> holds it a moment after a quick tap; only the innermost one under the press takes it (an event's chip, not its day), and a press that slides lets go of it. The pressed colour is a state layer laid OVER the part's own fill and under its words (<see cref="PressLayer"/>, set by a style; drawn by a clear part made the first time it is needed), so a day keeps its glass and an event its tone beneath it.</summary>
internal class MonthBorder : Border
{
    private const string Pressed = ":pressed";
    private const double Slop = 10;
    private Point? _down;
    private TemplateBorder? _layer;

    /// <summary>The state layer over the part's fill while it is pressed (set by <c>Press.axaml</c>); none at rest.</summary>
    public static readonly StyledProperty<IBrush?> PressLayerProperty =
        AvaloniaProperty.Register<MonthBorder, IBrush?>(nameof(PressLayer));

    public IBrush? PressLayer { get => GetValue(PressLayerProperty); set => SetValue(PressLayerProperty, value); }

    protected override Type StyleKeyOverride => typeof(MonthBorder);

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == PressLayerProperty)
        {
            if (_layer is null)
            {
                if (PressLayer is null) return;
                // behind the content (the first visual child), over the part's own background, which Border draws before its children
                _layer = new TemplateBorder { IsHitTestVisible = false, Transitions = [new BrushFade { Property = BackgroundProperty, Duration = Motion.StillFade }] };
                _layer[!CornerRadiusProperty] = this[!CornerRadiusProperty];
                VisualChildren.Insert(0, _layer);
                InvalidateMeasure();
            }
            _layer.Background = PressLayer;
        }
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        _layer?.Measure(availableSize);
        return base.MeasureOverride(availableSize);
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        _layer?.Arrange(new Rect(finalSize));
        return base.ArrangeOverride(finalSize);
    }

    public MonthBorder()
    {
        AddHandler(PointerPressedEvent, OnDown, RoutingStrategies.Bubble, handledEventsToo: true);
        AddHandler(PointerMovedEvent, OnMove, RoutingStrategies.Bubble, handledEventsToo: true);
        AddHandler(PointerReleasedEvent, (_, _) => Lift(), RoutingStrategies.Bubble, handledEventsToo: true);
    }

    // what a press on it does something with
    private bool Presses => Classes.Contains("month-day") || Classes.Contains("link") || Classes.Contains("week-band") || Classes.Contains("month-more-link");

    private void OnDown(object? sender, PointerPressedEventArgs e)
    {
        if (!Presses || !e.GetCurrentPoint(this).Properties.IsLeftButtonPressed || e.Source is not Visual source) return;
        // a pressable part inside it (an event's chip in its day) takes the press itself
        for (var v = source; v is not null && !ReferenceEquals(v, this); v = v.GetVisualParent())
            if (v is MonthBorder { Presses: true }) return;
        _down = e.GetPosition(this);
        PseudoClasses.Set(Pressed, true);
    }

    private void OnMove(object? sender, PointerEventArgs e)
    {
        if (_down is { } at && Point.Distance(at, e.GetPosition(this)) > Slop) Lift();
    }

    private void Lift()
    {
        _down = null;
        PseudoClasses.Set(Pressed, false);
    }

    protected override void OnPointerCaptureLost(PointerCaptureLostEventArgs e)
    {
        base.OnPointerCaptureLost(e);
        Lift();
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        Lift();
    }
}

/// <summary>An event chip <see cref="MonthCalendar"/> builds: a pressable one reaches a finger around it. Its styles name it <c>ins|MonthBorder</c>.</summary>
internal sealed class MonthChip : MonthBorder, ICustomHitTest, IReach
{
    public bool HitTest(Point point) => new Rect(Bounds.Size).Contains(point) ? Reach.Inside(this) : Focusable && Reach.Claims(this, point);
}

/// <summary>A TextBlock that <see cref="MonthCalendar"/> builds; its styles name it <c>ins|MonthText</c>.</summary>
internal sealed class MonthText : TextBlock
{
    protected override Type StyleKeyOverride => typeof(MonthText);

    /// <summary>Set by a style: the text breaks between whole words only, a word too long for a line standing alone and cut with «…» (Avalonia's own wrapping breaks it letter by letter).</summary>
    public static readonly StyledProperty<bool> WholeWordsProperty =
        AvaloniaProperty.Register<MonthText, bool>(nameof(WholeWords));

    public bool WholeWords { get => GetValue(WholeWordsProperty); set => SetValue(WholeWordsProperty, value); }

    private string? _words;  // the text as given, while it is laid out by lines
    private double _laidFor = double.NaN;
    private bool _laying;

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == TextProperty && !_laying)
        {
            _words = Text;
            _laidFor = double.NaN;
        }
        else if (change.Property == WholeWordsProperty)
        {
            _laidFor = double.NaN;
            if (!WholeWords && _words is not null) Show(_words);
            InvalidateMeasure();
        }
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        if (WholeWords && _words is { } words && double.IsFinite(availableSize.Width) && availableSize.Width != _laidFor)
        {
            _laidFor = availableSize.Width;
            Show(Lines(words, Math.Max(0, availableSize.Width - Padding.Left - Padding.Right)));
        }
        return base.MeasureOverride(availableSize);
    }

    private void Show(string text)
    {
        if (text == Text) return;
        _laying = true;
        SetCurrentValue(TextProperty, text);
        _laying = false;
    }

    // Packs the words into lines no wider than the room, as many to a line as fit.
    private string Lines(string text, double room)
    {
        var face = new global::Avalonia.Media.Typeface(FontFamily, FontStyle, FontWeight, FontStretch);
        double Width(string s)
        {
            using var layout = new global::Avalonia.Media.TextFormatting.TextLayout(s, face, FontSize, null);
            return layout.Width;
        }
        var lines = new List<string>();
        var line = "";
        foreach (var word in text.Split(' ', StringSplitOptions.RemoveEmptyEntries))
        {
            var longer = line.Length == 0 ? word : line + " " + word;
            if (line.Length == 0 || Width(longer) <= room) line = longer;
            else
            {
                lines.Add(line);
                line = word;
            }
        }
        if (line.Length > 0) lines.Add(line);
        return string.Join('\n', lines);
    }
}

/// <summary>A StackPanel that <see cref="MonthCalendar"/> builds; its styles name it <c>ins|MonthStack</c>. A header group (<see cref="Wraps"/>) given less room than its row needs goes on over more lines, each packed toward the group's side.</summary>
internal sealed class MonthStack : StackPanel
{
    protected override Type StyleKeyOverride => typeof(MonthStack);

    public bool Wraps { get; init; }

    /// <summary>Wrapped lines sit at the end side, as the header's end group does.</summary>
    public bool AtEnd { get; init; }

    /// <summary>How many of the last children wrap together, as the arrows and Today do.</summary>
    public int KeepLast { get; set; }

    private double LineGap => Math.Max(Spacing, 8);

    private bool Wrapping(double width) => Wraps && Orientation == Orientation.Horizontal && double.IsFinite(width) && Row() > width + 0.5;

    // The one-line width of the shown children, measured unconstrained.
    private double Row()
    {
        var shown = Children.Where(c => c.IsVisible).ToList();
        return shown.Sum(c => c.DesiredSize.Width) + Math.Max(0, shown.Count - 1) * Spacing;
    }

    private List<List<Control>> Lines(double width)
    {
        var shown = Children.Where(c => c.IsVisible).ToList();
        var keep = Math.Min(KeepLast, shown.Count);
        var units = shown.Take(shown.Count - keep).Select(c => new List<Control> { c }).ToList();
        if (keep > 0) units.Add(shown.Skip(shown.Count - keep).ToList());
        var lines = new List<List<Control>> { new() };
        var used = 0d;
        foreach (var unit in units)
        {
            var w = unit.Sum(c => c.DesiredSize.Width) + (unit.Count - 1) * Spacing;
            if (lines[^1].Count > 0 && used + Spacing + w > width + 0.5)
            {
                lines.Add([]);
                used = 0;
            }
            used += (lines[^1].Count > 0 ? Spacing : 0) + w;
            lines[^1].AddRange(unit);
        }
        return lines;
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        if (!Wraps || Orientation != Orientation.Horizontal) return base.MeasureOverride(availableSize);
        foreach (var c in Children) c.Measure(Size.Infinity);
        if (!Wrapping(availableSize.Width)) return base.MeasureOverride(availableSize);
        var lines = Lines(availableSize.Width);
        var height = lines.Sum(l => l.Max(c => c.DesiredSize.Height)) + (lines.Count - 1) * LineGap;
        return new Size(availableSize.Width, height);
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        if (!Wrapping(finalSize.Width)) return base.ArrangeOverride(finalSize);
        var y = 0d;
        foreach (var line in Lines(finalSize.Width))
        {
            var h = line.Max(c => c.DesiredSize.Height);
            var w = line.Sum(c => c.DesiredSize.Width) + (line.Count - 1) * Spacing;
            var x = AtEnd ? finalSize.Width - w : 0;
            foreach (var c in line)
            {
                c.Arrange(new Rect(x, y + (h - c.DesiredSize.Height) / 2, c.DesiredSize.Width, c.DesiredSize.Height));
                x += c.DesiredSize.Width + Spacing;
            }
            y += h + LineGap;
        }
        return finalSize;
    }
}

/// <summary>A Border that <see cref="BirthDateField"/> builds; its styles name it <c>ins|BirthBorder</c>.</summary>
internal sealed class BirthBorder : Border
{
    protected override Type StyleKeyOverride => typeof(BirthBorder);
}

/// <summary>A TextBlock that <see cref="BirthDateField"/> builds; its styles name it <c>ins|BirthText</c>.</summary>
internal sealed class BirthText : TextBlock
{
    protected override Type StyleKeyOverride => typeof(BirthText);
}

/// <summary>A StackPanel that <see cref="BirthDateField"/> builds; its styles name it <c>ins|BirthStack</c>.</summary>
internal sealed class BirthStack : StackPanel
{
    protected override Type StyleKeyOverride => typeof(BirthStack);
}

/// <summary>A Border that <see cref="DateCalendar"/> and its switch turn build; its styles name it <c>ins|CalendarBorder</c>.</summary>
internal sealed class CalendarBorder : Border
{
    protected override Type StyleKeyOverride => typeof(CalendarBorder);
}

/// <summary>A TextBlock that <see cref="DateCalendar"/> builds; its styles name it <c>ins|CalendarText</c>.</summary>
internal sealed class CalendarText : TextBlock
{
    protected override Type StyleKeyOverride => typeof(CalendarText);
}

/// <summary>A Border that <see cref="ChoiceMatrix"/> builds; its styles name it <c>ins|MatrixBorder</c>.</summary>
internal sealed class MatrixBorder : Border
{
    protected override Type StyleKeyOverride => typeof(MatrixBorder);
}

/// <summary>A TextBlock that <see cref="ChoiceMatrix"/> builds; its styles name it <c>ins|MatrixText</c>.</summary>
internal sealed class MatrixText : TextBlock
{
    protected override Type StyleKeyOverride => typeof(MatrixText);
}

/// <summary>A Border that <see cref="CommandPalette"/> builds; its styles name it <c>ins|PaletteBorder</c>.</summary>
internal sealed class PaletteBorder : Border
{
    protected override Type StyleKeyOverride => typeof(PaletteBorder);
}

/// <summary>A TextBlock that <see cref="CommandPalette"/> builds; its styles name it <c>ins|PaletteText</c>.</summary>
internal sealed class PaletteText : TextBlock
{
    protected override Type StyleKeyOverride => typeof(PaletteText);
}
