using System.Text;
using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Input.Platform;
using Avalonia.Input.TextInput;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;

namespace Insiyab.Avalonia.Controls;

/// <summary>Carries the code an <see cref="OtpBox"/> just completed.</summary>
public class OtpCompletedEventArgs(RoutedEvent routedEvent, string value) : RoutedEventArgs(routedEvent)
{
    public string Value { get; } = value;
}

/// <summary>The one-time code field, the web's <c>data-ins-otp</c>: one real text box drawn as a block with a dot per character. Paste, an OTP suggestion and a screen reader all meet one ordinary field; one cell is selected or the caret waits at the end, so the code never has a gap. Its errors show under the block, which turns red.</summary>
[TemplatePart("PART_CellsHost", typeof(Panel))]
[PseudoClasses(":invalid", ":complete")]
public class OtpBox : TextBox
{
    public static readonly StyledProperty<int> LengthProperty =
        AvaloniaProperty.Register<OtpBox, int>(nameof(Length), 6, coerce: (_, v) => Math.Clamp(v, 4, 10));

    public static readonly StyledProperty<bool> AlphanumericProperty =
        AvaloniaProperty.Register<OtpBox, bool>(nameof(Alphanumeric));

    public static readonly StyledProperty<bool> IsInvalidProperty =
        AvaloniaProperty.Register<OtpBox, bool>(nameof(IsInvalid));

    public static readonly StyledProperty<ICommand?> SubmitCommandProperty =
        AvaloniaProperty.Register<OtpBox, ICommand?>(nameof(SubmitCommand));

    public static readonly StyledProperty<string?> LocaleProperty =
        AvaloniaProperty.Register<OtpBox, string?>(nameof(Locale));

    public static readonly DirectProperty<OtpBox, bool> IsCompleteProperty =
        AvaloniaProperty.RegisterDirect<OtpBox, bool>(nameof(IsComplete), o => o.IsComplete);

    public static readonly DirectProperty<OtpBox, string?> ValidationMessageProperty =
        AvaloniaProperty.RegisterDirect<OtpBox, string?>(nameof(ValidationMessage), o => o.ValidationMessage);

    public static readonly RoutedEvent<OtpCompletedEventArgs> CompletedEvent =
        RoutedEvent.Register<OtpBox, OtpCompletedEventArgs>(nameof(Completed), RoutingStrategies.Bubble);

    private readonly List<OtpCell> _cells = [];
    private Panel? _host;
    private bool _syncing, _quiet, _edited, _left, _complete, _user, _picking;
    private string _last = string.Empty;
    private string? _message;

    static OtpBox()
    {
        AcceptsReturnProperty.OverrideDefaultValue<OtpBox>(false);
        FocusAdornerProperty.OverrideDefaultValue<OtpBox>(null);
    }

    public OtpBox()
    {
        SetCurrentValue(FlowDirectionProperty, FlowDirection.LeftToRight);
        TextInputOptions.SetContentType(this, TextInputContentType.Digits);
        AddHandler(PastingFromClipboardEvent, OnPasting);
    }

    /// <summary>How many characters, 4 to 10.</summary>
    public int Length { get => GetValue(LengthProperty); set => SetValue(LengthProperty, value); }

    /// <summary>Latin letters too, upper-cased; digits only otherwise (<c>data-ins-otp-chars="alnum"</c>).</summary>
    public bool Alphanumeric { get => GetValue(AlphanumericProperty); set => SetValue(AlphanumericProperty, value); }

    /// <summary>A code the server turned down (<c>aria-invalid</c>): the block turns red until the code is typed again.</summary>
    public bool IsInvalid { get => GetValue(IsInvalidProperty); set => SetValue(IsInvalidProperty, value); }

    /// <summary>Run with the code when the last character is entered (<c>data-ins-otp-submit</c>); unset, nothing is submitted.</summary>
    public ICommand? SubmitCommand { get => GetValue(SubmitCommandProperty); set => SetValue(SubmitCommandProperty, value); }

    /// <summary>"ar" or "en" for the message; unset, the direction decides.</summary>
    public string? Locale { get => GetValue(LocaleProperty); set => SetValue(LocaleProperty, value); }

    public bool IsComplete { get => _complete; private set => SetAndRaise(IsCompleteProperty, ref _complete, value); }

    /// <summary>"Enter the whole code" while the code is part-typed; null when empty or complete.</summary>
    public string? ValidationMessage { get => _message; private set => SetAndRaise(ValidationMessageProperty, ref _message, value); }

    /// <summary>The code was completed: the web's <c>ins:otp</c>.</summary>
    public event EventHandler<OtpCompletedEventArgs>? Completed
    {
        add => AddHandler(CompletedEvent, value);
        remove => RemoveHandler(CompletedEvent, value);
    }

    /// <summary>Sets the code quietly, with no <see cref="Completed"/>: <c>Insiyab.otp(field, value)</c>. An empty string clears it.</summary>
    public void SetCode(string? value)
    {
        _quiet = true;
        try
        {
            SetCurrentValue(TextProperty, value ?? string.Empty);
            Sync();
        }
        finally { _quiet = false; }
    }

    private void OnPasting(object? sender, RoutedEventArgs e)
    {
        e.Handled = true;
        _ = PasteAsync();
    }

    private async Task PasteAsync()
    {
        if (TopLevel.GetTopLevel(this)?.Clipboard is not { } clipboard) return;
        PasteText(await clipboard.TryGetTextAsync());
    }

    // A paste that is a whole code replaces the field; a shorter one is added at the end, as typing is.
    internal void PasteText(string? pasted)
    {
        if (!IsEffectivelyEnabled || IsReadOnly) return;
        var code = Clean(pasted);
        if (code.Length == 0) return;
        SetCurrentValue(TextProperty, code.Length == Length || IsWholeSelected() ? code : (Text ?? string.Empty) + (pasted ?? string.Empty));
        ToEnd();
    }

    internal string Clean(string? value)
    {
        var s = PluginText.Latin(value);
        var sb = new StringBuilder(s.Length);
        foreach (var raw in s)
        {
            var c = Alphanumeric ? char.ToUpperInvariant(raw) : raw;
            if (c >= '0' && c <= '9') sb.Append(c);
            else if (Alphanumeric && c >= 'A' && c <= 'Z') sb.Append(c);
            if (sb.Length == Length) break;
        }
        return sb.ToString();
    }

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);
        _host = e.NameScope.Find<Panel>("PART_CellsHost");
        BuildCells();
        Sync(quiet: true);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == TextProperty)
        {
            if (_syncing) return;
            if (!_quiet && _attached)
            {
                _edited = true;
                if (IsInvalid) SetCurrentValue(IsInvalidProperty, false);
            }
            Sync(_quiet || !_attached, !_user);
        }
        else if (change.Property == LengthProperty)
        {
            BuildCells();
            Sync(quiet: true);
        }
        else if (change.Property == AlphanumericProperty)
        {
            TextInputOptions.SetContentType(this, Alphanumeric ? TextInputContentType.Normal : TextInputContentType.Digits);
            TextInputOptions.SetUppercase(this, Alphanumeric);
            Sync(quiet: true);
        }
        else if (change.Property == CaretIndexProperty || change.Property == SelectionStartProperty || change.Property == SelectionEndProperty)
        {
            if (_user || _picking) return;
            if (IsTidy()) Render();
            else Dispatcher.UIThread.Post(Normalize);
        }
        else if (change.Property == IsInvalidProperty || change.Property == IsEnabledProperty || change.Property == DataValidationErrors.HasErrorsProperty)
        {
            Render();
        }
        else if (change.Property == LocaleProperty || change.Property == FlowDirectionProperty)
        {
            Check();
        }
    }

    private bool _attached;
    private bool _all;

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        _attached = true;
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        _attached = false;
    }

    // Left, Right, Home and End move between cells, and the rest of the keys edit the one selected.
    protected override void OnKeyDown(KeyEventArgs e)
    {
        if (e.Key is Key.Up or Key.Down or Key.PageUp or Key.PageDown)
        {
            e.Handled = true;
            return;
        }
        if (e.Key is Key.Left or Key.Right or Key.Home or Key.End)
        {
            e.Handled = true;
            Move(e.Key);
            return;
        }
        _all = e.Key == Key.A && (e.KeyModifiers & (KeyModifiers.Control | KeyModifiers.Meta)) != 0;
        _user = true;
        try { base.OnKeyDown(e); }
        finally { _user = false; }
        Normalize();
    }

    protected override void OnTextInput(TextInputEventArgs e)
    {
        var typed = Clean(e.Text);
        if (typed.Length == 0)
        {
            e.Handled = true;
            return;
        }
        e.Text = typed;
        _user = true;
        try { base.OnTextInput(e); }
        finally { _user = false; }
        Normalize();
    }

    // A press picks the cell under it; the text underneath is see-through, so its own hit test is not used.
    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        if (_host is null || !e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
        {
            base.OnPointerPressed(e);
            return;
        }
        e.Handled = true;
        if (!IsFocused) Focus(NavigationMethod.Pointer);
        Pick(CellAt(e.GetPosition(_host).X));
    }

    // Dragging selects nothing: a code is fixed one cell at a time, so the pressed cell stays the one picked.
    protected override void OnPointerMoved(PointerEventArgs e)
    {
        if (_host is not null && e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
        {
            e.Handled = true;
            return;
        }
        base.OnPointerMoved(e);
    }

    protected override void OnGotFocus(FocusChangedEventArgs e)
    {
        base.OnGotFocus(e);
        if (e.NavigationMethod != NavigationMethod.Pointer) Dispatcher.UIThread.Post(ToEnd);
        Render();
    }

    protected override void OnLostFocus(FocusChangedEventArgs e)
    {
        base.OnLostFocus(e);
        if (_edited) _left = true;
        Render();
    }

    private void ToEnd() => Pick(int.MaxValue);

    // The cell at i is selected, or the caret goes to the end when there is none there.
    private void Pick(int i)
    {
        var len = (Text ?? string.Empty).Length;
        var (start, end) = i >= len ? (len, len) : (i, i + 1);
        _all = false;
        _picking = true;
        try
        {
            if (CaretIndex != start) SetCurrentValue(CaretIndexProperty, start);
            if (SelectionStart != start) SetCurrentValue(SelectionStartProperty, start);
            if (SelectionEnd != end) SetCurrentValue(SelectionEndProperty, end);
        }
        finally { _picking = false; }
        Render();
    }

    private (int Start, int End) Picked()
    {
        var len = (Text ?? string.Empty).Length;
        return (Math.Clamp(Math.Min(SelectionStart, SelectionEnd), 0, len), Math.Clamp(Math.Max(SelectionStart, SelectionEnd), 0, len));
    }

    private bool IsWholeSelected()
    {
        var (start, end) = Picked();
        return _all && end - start > 1 && start == 0 && end == (Text ?? string.Empty).Length;
    }

    // One cell, the whole code after Ctrl+A, or the caret at the end: anything else, a drag included, collapses to a cell or the end.
    private bool IsTidy()
    {
        var len = (Text ?? string.Empty).Length;
        var (start, end) = Picked();
        return end - start == 1 || IsWholeSelected() || (start == end && start == len && CaretIndex == len);
    }

    private void Normalize()
    {
        if (IsTidy()) Render();
        else
        {
            var (start, end) = Picked();
            Pick(end > start ? start : Math.Clamp(CaretIndex, 0, (Text ?? string.Empty).Length));
        }
    }

    private void Move(Key key)
    {
        var len = (Text ?? string.Empty).Length;
        var (start, end) = Picked();
        var whole = end - start > 1;
        var at = end - start == 1 ? start : len;
        Pick(key switch
        {
            Key.Home => 0,
            Key.End => len,
            Key.Left => whole ? 0 : Math.Max(0, at - 1),
            _ => whole ? len : Math.Min(len, at + 1),
        });
    }

    // The gap between two cells belongs half to each; past the last cell is the last.
    private int CellAt(double x)
    {
        for (var i = 0; i < _cells.Count; i++)
            if (x < _cells[i].Bounds.Right + 2) return i;
        return _cells.Count - 1;
    }

    // After every change: cleaned, redrawn, checked, and announced the moment the last box fills.
    private void Sync(bool quiet = true, bool toEnd = true)
    {
        var was = _last;
        var value = Clean(Text);
        if (value != (Text ?? string.Empty))
        {
            _syncing = true;
            try { SetCurrentValue(TextProperty, value); }
            finally { _syncing = false; }
        }
        if (toEnd) ToEnd();
        _last = value;
        IsComplete = value.Length == Length;
        Check();
        Render();
        if (!quiet && IsComplete && value != was)
        {
            RaiseEvent(new OtpCompletedEventArgs(CompletedEvent, value));
            if (SubmitCommand is { } command && command.CanExecute(value)) command.Execute(value);
        }
    }

    // The box is always left to right, so the page's language is the one around it.
    private void Check()
    {
        var len = (Text ?? string.Empty).Length;
        if (len == 0 || len >= Length) { ValidationMessage = null; return; }
        ValidationMessage = PluginText.IsArabic(this.GetVisualParent() as Control ?? this, Locale)
            ? $"اكتب الرمز كاملًا: {Length}{(Alphanumeric ? " خانات." : " أرقام.")}"
            : $"Enter all {Length}{(Alphanumeric ? " characters." : " digits.")}";
    }

    private void BuildCells()
    {
        if (_host is null) return;
        _host.Children.Clear();
        _cells.Clear();
        for (var i = 0; i < Length; i++)
        {
            var cell = new OtpCell();
            _cells.Add(cell);
            _host.Children.Add(cell);
        }
        Render();
    }

    private void Render()
    {
        var value = Text ?? string.Empty;
        var invalid = IsInvalid || (_left && ValidationMessage is not null) || DataValidationErrors.GetHasErrors(this);
        PseudoClasses.Set(":invalid", invalid);
        PseudoClasses.Set(":complete", value.Length == Length);
        var focused = IsFocused && IsEffectivelyEnabled;
        var (start, end) = Picked();
        for (var i = 0; i < _cells.Count; i++)
        {
            var ch = i < value.Length ? value[i].ToString() : string.Empty;
            _cells[i].Character = ch;
            _cells[i].SetState(ch.Length > 0, focused && (end > start ? i >= start && i < end : i == value.Length), invalid);
        }
    }
}

/// <summary>One drawn box of an <see cref="OtpBox"/>: the character, the dot under it, and a tint when selected or the caret when it is the next to type.</summary>
[PseudoClasses(":filled", ":active", ":invalid")]
public class OtpCell : TemplatedControl
{
    public static readonly StyledProperty<string?> CharacterProperty =
        AvaloniaProperty.Register<OtpCell, string?>(nameof(Character));

    static OtpCell() => TransformOperationsAnimator.Ensure();

    public OtpCell()
    {
        IsHitTestVisible = false;
        Focusable = false;
    }

    public string? Character { get => GetValue(CharacterProperty); set => SetValue(CharacterProperty, value); }

    internal void SetState(bool filled, bool active, bool invalid)
    {
        PseudoClasses.Set(":filled", filled);
        PseudoClasses.Set(":active", active);
        PseudoClasses.Set(":invalid", invalid);
    }
}
