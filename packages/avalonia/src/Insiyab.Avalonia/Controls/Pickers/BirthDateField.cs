using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Shapes;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Input.Platform;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;

namespace Insiyab.Avalonia.Controls;

/// <summary>How the birth date field is laid out.</summary>
public enum BirthLayout
{
    /// <summary>One field-sized control with the three parts typed in place and a button that opens the columns.</summary>
    Field,

    /// <summary>Three big blocks with the columns always shown under them, for a step of its own.</summary>
    Inline,
}

/// <summary>Sent when the birth date changes (the web's <c>ins:birth</c>).</summary>
public sealed class BirthChangedEventArgs(string value, DateTime? date) : EventArgs
{
    /// <summary>What the form would send: <c>1993-01-22</c>, or empty when there is no complete, allowed date.</summary>
    public string Value { get; } = value;

    public DateTime? Date { get; } = date;
}

/// <summary>The birth date plugin (the web's <c>input[data-ins-birth]</c>): day, month and year typed in place or chosen from three columns, with the web's typing rules (a part moves on by itself, a lone digit takes its zero, a digit that cannot be part of an allowed date is refused). Always holds a Gregorian date.</summary>
[TemplatePart("PART_Host", typeof(Decorator))]
public class BirthDateField : TemplatedControl
{
    public static readonly StyledProperty<DateTime?> SelectedDateProperty =
        AvaloniaProperty.Register<BirthDateField, DateTime?>(nameof(SelectedDate), defaultBindingMode: BindingMode.TwoWay);

    public static readonly StyledProperty<BirthLayout> LayoutProperty =
        AvaloniaProperty.Register<BirthDateField, BirthLayout>(nameof(Layout));

    public static readonly StyledProperty<int> MinAgeProperty =
        AvaloniaProperty.Register<BirthDateField, int>(nameof(MinAge));

    public static readonly StyledProperty<int> MaxAgeProperty =
        AvaloniaProperty.Register<BirthDateField, int>(nameof(MaxAge), 120);

    public static readonly StyledProperty<int> StartAgeProperty =
        AvaloniaProperty.Register<BirthDateField, int>(nameof(StartAge), 31);

    public static readonly StyledProperty<bool> IsRequiredProperty =
        AvaloniaProperty.Register<BirthDateField, bool>(nameof(IsRequired));

    public static readonly StyledProperty<string?> LocaleProperty =
        AvaloniaProperty.Register<BirthDateField, string?>(nameof(Locale));

    public static readonly StyledProperty<bool> IsDropDownOpenProperty =
        AvaloniaProperty.Register<BirthDateField, bool>(nameof(IsDropDownOpen), defaultBindingMode: BindingMode.TwoWay);

    public static readonly DirectProperty<BirthDateField, string> ValueProperty =
        AvaloniaProperty.RegisterDirect<BirthDateField, string>(nameof(Value), o => o.Value);

    public static readonly DirectProperty<BirthDateField, string> MessageProperty =
        AvaloniaProperty.RegisterDirect<BirthDateField, string>(nameof(Message), o => o.Message);

    private const string Order = "dmy";
    private static readonly double[] InlineWidths = [76, 96, 156];
    private static readonly double[] PopWidths = [56, 112, 72];

    private readonly StackPanel?[] _segs = new StackPanel?[3];
    private readonly TextBox?[] _boxes = new TextBox?[3];
    private readonly TextBlock?[] _vals = new TextBlock?[3];
    private readonly TextBlock?[] _phs = new TextBlock?[3];
    private readonly StackPanel?[] _dots = new StackPanel?[3];
    private readonly TextBlock?[] _caps = new TextBlock?[3];
    private readonly DrumColumn?[] _cols = new DrumColumn?[3];
    private readonly bool[] _intent = new bool[3];
    private Decorator? _host;
    private StackPanel? _root;
    private Panel? _wrap;
    private Border? _control;
    private Button? _btn;
    private TextBlock? _line;
    private TextBlock? _popLine;
    private PickerFlyout? _flyout;
    private BirthEngine _engine;
    private BirthSnapshot? _saved;
    private string _value = string.Empty;
    private string _message = string.Empty;
    private bool _quiet;
    private bool _syncing;
    private bool _painting;

    public BirthDateField()
    {
        _engine = NewEngine();
    }

    /// <summary>Raised when the date changes, as the web's <c>ins:birth</c>; not when it is set from code.</summary>
    public event EventHandler<BirthChangedEventArgs>? DateChanged;

    /// <summary>The date of birth; set from code it is taken in quietly, as <c>Insiyab.birth(el, iso)</c> does.</summary>
    public DateTime? SelectedDate { get => GetValue(SelectedDateProperty); set => SetValue(SelectedDateProperty, value); }

    public BirthLayout Layout { get => GetValue(LayoutProperty); set => SetValue(LayoutProperty, value); }

    /// <summary>The youngest age allowed (the web's <c>data-ins-age-min</c>); 0 by default.</summary>
    public int MinAge { get => GetValue(MinAgeProperty); set => SetValue(MinAgeProperty, value); }

    /// <summary>The oldest age allowed (<c>data-ins-age-max</c>); 120 by default.</summary>
    public int MaxAge { get => GetValue(MaxAgeProperty); set => SetValue(MaxAgeProperty, value); }

    /// <summary>The age the year column starts on (<c>data-ins-age-start</c>); 31 by default.</summary>
    public int StartAge { get => GetValue(StartAgeProperty); set => SetValue(StartAgeProperty, value); }

    /// <summary>The web's <c>required</c>: an empty field says so.</summary>
    public bool IsRequired { get => GetValue(IsRequiredProperty); set => SetValue(IsRequiredProperty, value); }

    /// <summary>"ar" or "en"; unset, Arabic in a right-to-left layout and English otherwise.</summary>
    public string? Locale { get => GetValue(LocaleProperty); set => SetValue(LocaleProperty, value); }

    /// <summary>Whether the columns pop-up is open (field layout only).</summary>
    public bool IsDropDownOpen { get => GetValue(IsDropDownOpenProperty); set => SetValue(IsDropDownOpenProperty, value); }

    /// <summary>What the form would send: ISO, or empty.</summary>
    public string Value { get => _value; private set => SetAndRaise(ValueProperty, ref _value, value); }

    /// <summary>What is wrong with the date as it stands (too young, too far back, unfinished, missing), or empty.</summary>
    public string Message { get => _message; private set => SetAndRaise(MessageProperty, ref _message, value); }

    internal BirthEngine Engine => _engine;

    internal TextBox DayBox => _boxes[0]!;

    internal TextBox MonthBox => _boxes[1]!;

    internal TextBox YearBox => _boxes[2]!;

    internal DrumColumn DayColumn => _cols[0]!;

    internal DrumColumn MonthColumn => _cols[1]!;

    internal DrumColumn YearColumn => _cols[2]!;

    internal PickerFlyout? Flyout => _flyout;

    internal TextBlock? Line => _line;

    internal TextBlock? PopLine => _popLine;

    internal Button? PickButton => _btn;

    private PickerLocale Words => PickerLocale.For(Locale, FlowDirection);

    private BirthEngine NewEngine() => new(MinAge, MaxAge, StartAge, DateCalendar.Today);

    /// <summary>Sets the date quietly (the web's <c>Insiyab.birth(el, iso)</c>); null clears it.</summary>
    public void SetDate(DateTime? date)
    {
        var was = _quiet;
        _quiet = true;
        if (date is { } d) _engine.FillAll(d.Date);
        else _engine.Clear();
        Refresh(true);
        _quiet = was;
    }

    /// <summary>Empties the field, as Escape does.</summary>
    public void Clear()
    {
        _engine.Clear();
        Refresh(true);
    }

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);
        _host = e.NameScope.Find<Decorator>("PART_Host");
        Build();
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        ActualThemeVariantChanged += OnThemeChanged;
        PaintParts();
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        ActualThemeVariantChanged -= OnThemeChanged;
    }

    private void OnThemeChanged(object? sender, EventArgs e) => PaintParts();

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        var p = change.Property;
        if (p == SelectedDateProperty)
        {
            if (!_syncing) SetDate(SelectedDate);
        }
        else if (p == MinAgeProperty || p == MaxAgeProperty || p == StartAgeProperty) Reconfigure();
        else if (p == LayoutProperty || p == LocaleProperty || p == FlowDirectionProperty) Build();
        else if (p == IsRequiredProperty) Publish();
        else if (p == IsDropDownOpenProperty)
        {
            if (IsDropDownOpen && _flyout is { IsOpen: false }) Open(false);
            else if (!IsDropDownOpen && _flyout is { IsOpen: true }) Close();
        }
        else if (p == IsEnabledProperty && !IsEnabled) Close();
    }

    // reconfigure: new limits and a new start, so a new engine; a date the property still holds is put back in it.
    private void Reconfigure()
    {
        _engine = NewEngine();
        _quiet = true;
        if (SelectedDate is { } d) _engine.FillAll(d.Date);
        Build();
        _quiet = false;
    }

    private char FirstOpen() => !_engine.IsSet('d') ? 'd' : !_engine.IsSet('m') ? 'm' : 'y';

    private char? PartOf(Control c)
    {
        for (var i = 0; i < 3; i++) if (ReferenceEquals(_boxes[i], c)) return Order[i];
        return null;
    }

    private IBrush? Res(string key) => this.TryFindResource(key, ActualThemeVariant, out var v) ? v as IBrush : null;

    // ------------------------------------------------------------------ build

    private void Build()
    {
        if (_host is null) return;
        Close();
        var words = Words;
        var inline = Layout == BirthLayout.Inline;
        AutomationProperties.SetName(this, words.BirthGroup);
        _root = new StackPanel { Classes = { "birth-root", inline ? "inline" : "field" } };
        _line = new BirthText { Classes = { "birth-line" } };
        _popLine = null;
        _flyout = null;
        _btn = null;
        _control = null;
        _wrap = null;

        var segs = new Control[3];
        for (var i = 0; i < 3; i++) segs[i] = MakeSeg(Order[i], words, inline);

        if (inline)
        {
            var row = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 12, HorizontalAlignment = HorizontalAlignment.Center };
            row.Children.AddRange(segs);
            _root.Children.Add(row);
            _root.Children.Add(MakeDrum(false));
            _line.HorizontalAlignment = HorizontalAlignment.Center;
            _root.Children.Add(_line);
        }
        else
        {
            var parts = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 2, VerticalAlignment = VerticalAlignment.Center };
            for (var i = 0; i < 3; i++)
            {
                if (i > 0) parts.Children.Add(new BirthText { Classes = { "birth-sep" }, Text = "/", VerticalAlignment = VerticalAlignment.Center });
                parts.Children.Add(segs[i]);
            }
            _btn = new Button { Classes = { "birth-btn" }, Focusable = false };
            AutomationProperties.SetName(_btn, words.BirthPick);
            _btn.Click += (_, _) => { if (_flyout?.IsOpen == true) Close(); else Open(false); };
            DockPanel.SetDock(_btn, Dock.Right);
            var dock = new DockPanel();
            dock.Children.Add(_btn);
            dock.Children.Add(parts);
            _control = new BirthBorder { Classes = { "birth-control" }, Child = dock };
            _wrap = new Panel { Classes = { "birth-wrap" } };
            _wrap.Children.Add(new BirthBorder { Classes = { "birth-shadow" } });
            _wrap.Children.Add(_control);
            _root.Children.Add(_wrap);
            _root.Children.Add(_line);
            BuildPop(words);
        }
        _host.Child = _root;
        for (var i = 0; i < 3; i++) FillItems(Order[i]);
        for (var i = 0; i < 3; i++) GoValue(Order[i]);
        var was = _quiet;
        _quiet = true;
        Paint();
        _quiet = was;
    }

    private Control MakeSeg(char k, PickerLocale words, bool inline)
    {
        var i = Order.IndexOf(k);
        var val = new BirthText { Classes = { "birth-val" }, FlowDirection = FlowDirection.LeftToRight, IsVisible = false };
        var ph = new BirthText { Classes = { "birth-ph" }, Text = words.BirthWord(k) };
        var face = new Panel();
        face.Children.Add(ph);
        face.Children.Add(val);
        if (inline)
        {
            var dots = new StackPanel { Classes = { "birth-dots" }, Orientation = Orientation.Horizontal };
            for (var n = 0; n < (k == 'y' ? 4 : 2); n++) dots.Children.Add(new Ellipse { Classes = { "birth-dot" } });
            face.Children.Add(dots);
            _dots[i] = dots;
        }
        var box = new BirthBorder { Classes = { "birth-box" }, Child = face };
        var tb = new TextBox { Classes = { "birth-in" }, MaxLength = 4, FlowDirection = FlowDirection.LeftToRight };
        AutomationProperties.SetName(tb, words.BirthCap(k));
        tb.AddHandler(TextInputEvent, OnText, RoutingStrategies.Tunnel);
        tb.AddHandler(KeyDownEvent, OnKey, RoutingStrategies.Tunnel);
        tb.AddHandler(TextBox.PastingFromClipboardEvent, OnPasting);
        tb.TextChanged += (_, _) => { if (!_painting && tb.Text != _engine.Str(k)) PaintParts(); };
        var stage = new Panel();
        stage.Children.Add(box);
        stage.Children.Add(tb);
        var seg = new StackPanel { Classes = { "birth-seg", k.ToString() } };
        seg.Children.Add(stage);
        if (inline)
        {
            var cap = new BirthText { Classes = { "birth-cap" }, Text = words.BirthCap(k) };
            seg.Children.Add(cap);
            _caps[i] = cap;
        }
        else _caps[i] = null;
        _segs[i] = seg;
        _boxes[i] = tb;
        _vals[i] = val;
        _phs[i] = ph;
        if (!inline) _dots[i] = null;
        return seg;
    }

    private Control MakeDrum(bool pop)
    {
        var row = new StackPanel { Classes = { "birth-drum" }, Orientation = Orientation.Horizontal, Spacing = pop ? 6 : 12, HorizontalAlignment = HorizontalAlignment.Center };
        for (var i = 0; i < 3; i++)
        {
            var k = Order[i];
            var col = new DrumColumn
            {
                Classes = { "birth-col", k.ToString() },
                IsPerspective = false,
                VisibleRows = pop ? 5 : 3,
                Width = pop ? PopWidths[i] : InlineWidths[i],
                OutOpacity = 0.3,
                IsTabStop = pop,
            };
            if (pop) col.Classes.Add("pop");
            col.Touched += (_, _) => _intent[Order.IndexOf(k)] = true;
            col.Settled += (_, v) => OnSettled(k, v);
            if (pop) col.KeyDown += (_, e) => OnColumnKey(k, col, e);
            if (k == 'm') col.IsOut = n => _engine.IsSet('y') && _engine.Val('y') == _engine.LastYear && n + 1 > _engine.Today.Month;
            AutomationProperties.SetName(col, Words.BirthCap(k));
            row.Children.Add(col);
            _cols[i] = col;
        }
        var lens = new Panel { HorizontalAlignment = HorizontalAlignment.Center };
        lens.Children.Add(new Border { Classes = { "drum-lens" } });
        lens.Children.Add(row);
        return lens;
    }

    private void BuildPop(PickerLocale words)
    {
        _popLine = new BirthText { Classes = { "birth-line", "pop" } };
        var cancel = new Button { Classes = { "bare", "sm" }, Content = words.Cancel };
        cancel.Click += (_, _) => Cancel();
        var done = new Button { Classes = { "primary", "sm" }, Content = words.Done };
        done.Click += (_, _) => { Close(); FocusPart(FirstOpen()); };
        var buttons = new StackPanel { Classes = { "birth-foot-row" }, Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right };
        buttons.Children.Add(cancel);
        buttons.Children.Add(done);
        var foot = new BirthBorder { Classes = { "birth-foot" }, Child = buttons };
        var body = new StackPanel { Margin = new Thickness(12), Spacing = 8 };
        body.Children.Add(_popLine);
        body.Children.Add(MakeDrum(true));
        body.Children.Add(foot);
        var inner = new Panel();
        inner.Children.Add(new Border { Classes = { "picker-glass" } });
        inner.Children.Add(body);
        var pop = new BirthBorder { Classes = { "birth-pop" }, Child = inner };
        pop.AddHandler(KeyDownEvent, OnPopKey);
        _flyout = new PickerFlyout { Content = pop };
        _flyout.Closed += (_, _) =>
        {
            SetCurrentValue(IsDropDownOpenProperty, false);
            _btn?.Classes.Set("open", false);
            _wrap?.Classes.Set("open", false);
        };
    }

    private void FillItems(char k)
    {
        var i = Order.IndexOf(k);
        if (_cols[i] is not { } col) return;
        if (k == 'd') col.Items = [.. Enumerable.Range(1, _engine.DaysInMonth).Select(n => n.ToString())];
        else if (k == 'm') col.Items = [.. Enumerable.Range(0, 12).Select(n => Words.MonthName(PickerCalendar.Gregorian, n))];
        else col.Items = [.. Enumerable.Range(_engine.FirstYear, _engine.LastYear - _engine.FirstYear + 1).Select(n => n.ToString())];
    }

    private void GoValue(char k)
    {
        var i = Order.IndexOf(k);
        _cols[i]?.ScrollTo(k == 'y' ? _engine.Val('y') - _engine.FirstYear : _engine.Val(k) - 1, false);
    }

    private int ValueOf(char k, int index) => k == 'y' ? _engine.FirstYear + index : index + 1;

    // ------------------------------------------------------------------ painting

    // What every change ends in: the day column follows the month, the columns follow the parts, the field is painted.
    private void Refresh(bool all)
    {
        var rebuilt = _cols[0] is { } day && day.Items.Count != _engine.DaysInMonth;
        if (rebuilt) FillItems('d');
        for (var i = 0; i < 3; i++)
            if (all || _engine.IsSet(Order[i]) || (i == 0 && rebuilt)) GoValue(Order[i]);
        Paint();
    }

    private void Paint()
    {
        PaintParts();
        Publish();
    }

    private IBrush? AutoBrush
    {
        get
        {
            var brush = Res("InsToneBrand");
            return brush is ISolidColorBrush s ? new SolidColorBrush(s.Color, 0.8) : brush;
        }
    }

    // paint: the digits, the placeholders, the dots and the lines.
    private void PaintParts()
    {
        if (_root is null) return;
        var words = Words;
        var eng = _engine;
        _painting = true;
        var auto = AutoBrush;
        for (var i = 0; i < 3; i++)
        {
            var k = Order[i];
            var s = eng.Str(k);
            var val = _vals[i]!;
            val.Inlines!.Clear();
            for (var n = 0; n < s.Length; n++)
            {
                var digit = new Run(s[n].ToString());
                if (eng.IsAuto(k, n)) digit.Foreground = auto;
                val.Inlines.Add(digit);
            }
            val.IsVisible = s.Length > 0;
            _phs[i]!.IsVisible = s.Length == 0;
            if (_dots[i] is { } dots)
                for (var n = 0; n < dots.Children.Count; n++) dots.Children[n].Classes.Set("on", n < s.Length);
            _segs[i]!.Classes.Set("filled", s.Length > 0);
            if (_boxes[i]!.Text != s) _boxes[i]!.Text = s;
            if (_cols[i] is { } col) col.IsUnset = !eng.IsSet(k);
        }
        _cols[1]?.InvalidateVisual();
        if (_caps[1] is { } cap) cap.Text = eng.IsSet('m') ? words.MonthName(PickerCalendar.Gregorian, eng.Val('m') - 1) : words.BirthCap('m');
        var msg = eng.Message(IsRequired, words);
        var date = eng.Date;
        var open = Order.Where(c => !eng.IsSet(c)).Select(words.BirthCap).ToList();
        var hard = msg.Length > 0 && msg != words.BirthIncomplete && msg != words.BirthRequired ? msg : string.Empty;
        var status = hard.Length > 0 ? hard : open.Count == 3 ? words.BirthStart : open.Count > 0 ? words.BirthLeft(open) : string.Empty;
        var inline = Layout == BirthLayout.Inline;
        foreach (var line in new[] { _line, _popLine })
        {
            if (line is null) continue;
            line.Inlines!.Clear();
            if (date is { } d)
            {
                var day = new Run(words.LongDay(d.DayOfWeek) + " " + words.FormatDay(PickerCalendar.Gregorian, d)) { FontWeight = FontWeight.Bold };
                if (Res("InsText1") is { } strong) day.Foreground = strong;
                line.Inlines.Add(day);
                var tail = " · " + words.BirthAge(eng.AgeOf(d)) + (words.IsArabic ? " · " + words.FormatDay(PickerCalendar.Hijri, d) : string.Empty);
                line.Inlines.Add(new Run(tail));
            }
            else line.Inlines.Add(new Run(ReferenceEquals(line, _line) && !inline ? hard : status));
        }
        if (!inline && _line is not null) ToolTip.SetTip(_line, string.Join(string.Empty, _line.Inlines!.OfType<Run>().Select(r => r.Text)));
        _painting = false;
    }

    // The value, the message, the error under the field and the event.
    private void Publish()
    {
        var words = Words;
        var eng = _engine;
        var msg = eng.Message(IsRequired, words);
        var date = eng.Date;
        var value = date is { } d ? PickerParse.IsoOf(d) : string.Empty;
        Message = msg;
        if (msg.Length > 0 && (eng.Any || IsRequired)) DataValidationErrors.SetError(this, new DataValidationException(msg));
        else DataValidationErrors.ClearErrors(this);
        if (SelectedDate != date)
        {
            _syncing = true;
            SetCurrentValue(SelectedDateProperty, date);
            _syncing = false;
        }
        if (value == _value) return;
        Value = value;
        if (!_quiet) DateChanged?.Invoke(this, new BirthChangedEventArgs(value, date));
    }

    private void Flash(char k)
    {
        var seg = _segs[Order.IndexOf(k)];
        if (seg is null) return;
        seg.Classes.Set("no", true);
        DispatcherTimer.RunOnce(() => seg.Classes.Set("no", false), TimeSpan.FromMilliseconds(350));
        if (k == 'y' && _engine.MinAge > 0 && _line is not null)
        {
            _line.Inlines!.Clear();
            _line.Inlines.Add(new Run(Words.BirthYoung(_engine.MinAge)));
        }
    }

    // After the engine has been given a key: repaint, flash a refused one, and move focus where it says.
    private void Step(char? focus)
    {
        Refresh(false);
        if (_engine.Flashed) Flash(_engine.FlashedPart);
        if (focus is { } f) FocusPart(f);
    }

    private void FocusPart(char k)
    {
        if (_boxes[Order.IndexOf(k)] is not { } box) return;
        box.Focus();
        box.CaretIndex = box.Text?.Length ?? 0;
    }

    // ------------------------------------------------------------------ typing

    private void OnText(object? sender, TextInputEventArgs e)
    {
        if (sender is not TextBox tb || PartOf(tb) is not { } k) return;
        e.Handled = true;
        var cur = k;
        foreach (var c in PickerParse.ToLatin(e.Text))
        {
            char? next = null;
            if (c is >= '0' and <= '9') next = _engine.Press(cur, c);
            else if ("/\\.,- ".Contains(c)) next = _engine.Separate(cur);
            else continue;
            Step(next);
            if (next is { } n) cur = n;
        }
    }

    private void OnKey(object? sender, KeyEventArgs e)
    {
        if (sender is not TextBox tb || PartOf(tb) is not { } k) return;
        var alt = e.KeyModifiers.HasFlag(KeyModifiers.Alt);
        if (e.Key == Key.Escape && _flyout is { IsOpen: true })
        {
            e.Handled = true;
            Cancel();
        }
        else if (_flyout is not null && ((e.Key == Key.Down && alt) || e.Key == Key.F4))
        {
            e.Handled = true;
            if (_flyout.IsOpen) Close();
            else Open(false);
        }
        else if (e.Key == Key.Back)
        {
            e.Handled = true;
            Step(_engine.Backspace(k));
        }
        else if (e.Key == Key.Enter || (e.Key == Key.Tab && !e.KeyModifiers.HasFlag(KeyModifiers.Shift)))
        {
            if (_engine.Separate(k) is { } next)
            {
                e.Handled = true;
                Step(next);
            }
        }
        else if (e.Key == Key.Escape && _engine.Any)
        {
            e.Handled = true;
            _engine.Clear();
            Refresh(true);
            FocusPart('d');
        }
        else if ((e.Key == Key.Up || e.Key == Key.Down) && e.KeyModifiers == KeyModifiers.None)
        {
            e.Handled = true;
            Nudge(k, e.Key == Key.Up ? 1 : -1);
        }
    }

    // The arrow keys step a part, as a native date field does; an empty part takes its start value first.
    private void Nudge(char k, int by)
    {
        var lo = k == 'y' ? _engine.FirstYear : 1;
        var hi = k == 'd' ? _engine.DaysInMonth : k == 'm' ? 12 : _engine.LastYear;
        var v = _engine.IsSet(k) ? _engine.Val(k) + by : _engine.Val(k);
        _engine.Choose(k, Math.Clamp(v, lo, hi));
        Refresh(true);
    }

    private void OnPasting(object? sender, RoutedEventArgs e)
    {
        e.Handled = true;
        _ = PasteFromClipboard();
    }

    private async Task PasteFromClipboard()
    {
        try
        {
            var clipboard = TopLevel.GetTopLevel(this)?.Clipboard;
            if (clipboard is null) return;
            var text = await clipboard.TryGetTextAsync();
            if (!string.IsNullOrEmpty(text)) Paste(text);
        }
        catch (InvalidOperationException)
        {
        }
    }

    // Pasted text: a date in any common form fills all three parts; otherwise its digits are typed.
    internal void Paste(string raw)
    {
        if (BirthEngine.Understand(raw, _engine.Today) is { } date)
        {
            _engine.FillAll(date);
            Refresh(false);
            FocusPart('y');
            return;
        }
        var cur = FocusedPart() ?? 'd';
        foreach (var c in PickerParse.ToLatin(raw))
        {
            if (c is < '0' or > '9') continue;
            var next = _engine.Press(cur, c);
            Step(next);
            if (next is { } n) cur = n;
        }
    }

    private char? FocusedPart()
    {
        var focused = TopLevel.GetTopLevel(this)?.FocusManager?.GetFocusedElement();
        for (var i = 0; i < 3; i++) if (ReferenceEquals(_boxes[i], focused)) return Order[i];
        return null;
    }

    // ------------------------------------------------------------------ columns

    private void OnSettled(char k, int index)
    {
        var i = Order.IndexOf(k);
        if (!IsEnabled) _intent[i] = false;
        if (!_intent[i]) return;
        _intent[i] = false;
        _engine.Choose(k, ValueOf(k, index));
        Refresh(false);
    }

    // choose: Enter in the pop-up takes the value under the lens, then moves on to the next column left unset, or closes.
    private void OnColumnKey(char k, DrumColumn col, KeyEventArgs e)
    {
        if (e.Key != Key.Enter || e.Handled) return;
        e.Handled = true;
        _intent[Order.IndexOf(k)] = false;
        _engine.Choose(k, ValueOf(k, col.Value));
        Refresh(false);
        var next = Order.Where(c => !_engine.IsSet(c)).Select(c => (char?)c).FirstOrDefault();
        if (next is { } n) _cols[Order.IndexOf(n)]?.Focus(NavigationMethod.Tab);
        else
        {
            Close();
            FocusPart('y');
        }
    }

    // Escape anywhere in the pop-up gives its changes up.
    private void OnPopKey(object? sender, KeyEventArgs e)
    {
        if (e.Key != Key.Escape || e.Handled) return;
        e.Handled = true;
        Cancel();
    }

    // ------------------------------------------------------------------ pop-up

    /// <summary>Opens the columns pop-up (field layout); with <paramref name="keepFocus"/> focus stays on the part being typed in, else it goes to the first column left to choose.</summary>
    internal void Open(bool keepFocus)
    {
        if (_flyout is null || _control is null || !IsEnabled || _flyout.IsOpen) return;
        _saved = _engine.Save();
        _flyout.IsEnd = false;
        _flyout.ShowAt(_control);
        SetCurrentValue(IsDropDownOpenProperty, true);
        _btn?.Classes.Set("open", true);
        _wrap?.Classes.Set("open", true);
        for (var i = 0; i < 3; i++) GoValue(Order[i]);
        if (!keepFocus) Dispatcher.UIThread.Post(() => _cols[Order.IndexOf(FirstOpen())]?.Focus(NavigationMethod.Tab), DispatcherPriority.Input);
    }

    internal void Close()
    {
        if (_flyout is { IsOpen: true }) _flyout.Hide();
    }

    // cancel: the parts as they were when the pop-up opened.
    private void Cancel()
    {
        if (_saved is { } saved)
        {
            _engine.Restore(saved);
            Refresh(true);
        }
        Close();
        FocusPart(FirstOpen());
    }
}
