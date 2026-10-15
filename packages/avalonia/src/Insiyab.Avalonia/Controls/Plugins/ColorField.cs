using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Primitives;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using Avalonia.Threading;

namespace Insiyab.Avalonia.Controls;

/// <summary>The colour a <see cref="ColorField"/> settled on: the web's <c>ins:color</c>.</summary>
public class ColorCommittedEventArgs(RoutedEvent routedEvent, Color value) : RoutedEventArgs(routedEvent)
{
    public Color Value { get; } = value;
    public string Hex => ColorMath.Hex(Value);
}

/// <summary>The colour field, the web's <c>data-ins-color</c>: a hex field with a swatch in front of it that opens the picker (a square, a hue strip, presets and the contrast against white and black).</summary>
[TemplatePart("PART_Swatch", typeof(Button))]
[TemplatePart("PART_Input", typeof(TextBox))]
[TemplatePart("PART_Popup", typeof(Popup))]
[TemplatePart("PART_Picker", typeof(ColorPickerPanel))]
[PseudoClasses(":open", ":empty", ":invalid")]
public class ColorField : TemplatedControl
{
    public static readonly StyledProperty<Color?> ValueProperty =
        AvaloniaProperty.Register<ColorField, Color?>(nameof(Value), defaultBindingMode: BindingMode.TwoWay);

    public static readonly StyledProperty<string?> SwatchesProperty =
        AvaloniaProperty.Register<ColorField, string?>(nameof(Swatches));

    public static readonly StyledProperty<string?> LocaleProperty =
        AvaloniaProperty.Register<ColorField, string?>(nameof(Locale));

    public static readonly StyledProperty<bool> IsDropDownOpenProperty =
        AvaloniaProperty.Register<ColorField, bool>(nameof(IsDropDownOpen));

    public static readonly DirectProperty<ColorField, string> HexProperty =
        AvaloniaProperty.RegisterDirect<ColorField, string>(nameof(Hex), o => o.Hex);

    public static readonly DirectProperty<ColorField, IBrush?> SwatchBrushProperty =
        AvaloniaProperty.RegisterDirect<ColorField, IBrush?>(nameof(SwatchBrush), o => o.SwatchBrush);

    public static readonly DirectProperty<ColorField, IReadOnlyList<Color>> PresetsProperty =
        AvaloniaProperty.RegisterDirect<ColorField, IReadOnlyList<Color>>(nameof(Presets), o => o.Presets);

    public static readonly DirectProperty<ColorField, string?> ValidationMessageProperty =
        AvaloniaProperty.RegisterDirect<ColorField, string?>(nameof(ValidationMessage), o => o.ValidationMessage);

    public static readonly RoutedEvent<ColorCommittedEventArgs> ColorCommittedEvent =
        RoutedEvent.Register<ColorField, ColorCommittedEventArgs>(nameof(ColorCommitted), RoutingStrategies.Bubble);

    private Button? _swatch;
    private TextBox? _input;
    private Popup? _popup;
    private ColorPickerPanel? _picker;
    private Color? _committed;
    private bool _writing, _quiet, _openedByKey;
    private string _hex = string.Empty;
    private IBrush? _swatchBrush;
    private IReadOnlyList<Color> _presets = [];
    private string? _message;

    public ColorField() => UpdatePresets();

    /// <summary>The colour; null while the field is empty. Two-way.</summary>
    public Color? Value { get => GetValue(ValueProperty); set => SetValue(ValueProperty, value); }

    /// <summary>The presets: a comma list of hex colours, or "none" (<c>data-ins-color-swatches</c>); unset, the brand colour and a palette around it.</summary>
    public string? Swatches { get => GetValue(SwatchesProperty); set => SetValue(SwatchesProperty, value); }

    public string? Locale { get => GetValue(LocaleProperty); set => SetValue(LocaleProperty, value); }

    public bool IsDropDownOpen { get => GetValue(IsDropDownOpenProperty); set => SetValue(IsDropDownOpenProperty, value); }

    /// <summary>The colour as a form would send it, <c>#rrggbb</c>; empty while there is none.</summary>
    public string Hex { get => _hex; private set => SetAndRaise(HexProperty, ref _hex, value); }

    public IBrush? SwatchBrush { get => _swatchBrush; private set => SetAndRaise(SwatchBrushProperty, ref _swatchBrush, value); }

    public IReadOnlyList<Color> Presets { get => _presets; private set => SetAndRaise(PresetsProperty, ref _presets, value); }

    public string? ValidationMessage { get => _message; private set => SetAndRaise(ValidationMessageProperty, ref _message, value); }

    public event EventHandler<ColorCommittedEventArgs>? ColorCommitted
    {
        add => AddHandler(ColorCommittedEvent, value);
        remove => RemoveHandler(ColorCommittedEvent, value);
    }

    private bool Arabic => PluginText.IsArabic(this, Locale);

    /// <summary>Sets the colour quietly, with no <see cref="ColorCommitted"/>: <c>Insiyab.colorField(field, value)</c>.</summary>
    public void SetColor(Color? value)
    {
        _quiet = true;
        try { SetCurrentValue(ValueProperty, value); }
        finally { _quiet = false; }
        _committed = value;
    }

    private void UpdatePresets()
    {
        var own = Swatches;
        if (string.Equals(own?.Trim(), "none", StringComparison.OrdinalIgnoreCase)) { Presets = []; return; }
        var list = !string.IsNullOrWhiteSpace(own)
            ? own.Split(',').Select(ColorMath.ParseHex).Where(c => c.HasValue).Select(c => c!.Value).ToList()
            : new[] { InsiyabTheme.Primary }.Concat(ColorMath.DefaultSwatches).ToList();
        Presets = list.Select(c => Color.FromRgb(c.R, c.G, c.B)).Distinct().ToList();
    }

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        if (_swatch is not null)
        {
            _swatch.Click -= OnSwatchClick;
            _swatch.RemoveHandler(KeyDownEvent, OnSwatchKey);
        }
        if (_input is not null)
        {
            _input.TextChanged -= OnInputText;
            _input.LostFocus -= OnInputLeave;
            _input.RemoveHandler(KeyDownEvent, OnInputKey);
        }
        if (_picker is not null) _picker.ColorPicked -= OnPicked;
        if (_popup is not null) _popup.Closed -= OnPopupClosed;
        base.OnApplyTemplate(e);
        _swatch = e.NameScope.Find<Button>("PART_Swatch");
        _input = e.NameScope.Find<TextBox>("PART_Input");
        _popup = e.NameScope.Find<Popup>("PART_Popup");
        _picker = e.NameScope.Find<ColorPickerPanel>("PART_Picker");
        if (_swatch is not null)
        {
            _swatch.Click += OnSwatchClick;
            _swatch.AddHandler(KeyDownEvent, OnSwatchKey, RoutingStrategies.Tunnel);
        }
        if (_input is not null)
        {
            _input.TextChanged += OnInputText;
            _input.LostFocus += OnInputLeave;
            _input.AddHandler(KeyDownEvent, OnInputKey, RoutingStrategies.Tunnel);
        }
        if (_picker is not null) _picker.ColorPicked += OnPicked;
        if (_popup is not null) _popup.Closed += OnPopupClosed;
        _committed ??= Value;
        Paint(writeText: true);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == ValueProperty)
        {
            Paint(writeText: !_writing);
            if (_quiet) return;
        }
        else if (change.Property == SwatchesProperty) UpdatePresets();
        else if (change.Property == IsDropDownOpenProperty)
        {
            if (change.GetNewValue<bool>()) Open();
            else Close(false);
        }
    }

    private void Paint(bool writeText)
    {
        var v = Value;
        Hex = v is { } c ? ColorMath.Hex(c) : string.Empty;
        SwatchBrush = v is { } col ? new ImmutableSolidColorBrush(col) : null;
        PseudoClasses.Set(":empty", v is null);
        _swatch?.Classes.Set("empty", v is null);
        if (writeText && _input is not null)
        {
            _writing = true;
            try { _input.Text = Hex.ToUpperInvariant(); }
            finally { _writing = false; }
        }
        if (_picker is not null && !_picking) _picker.Value = v;
    }

    private bool _picking;

    // The colour set: live while still moving under the pointer; otherwise set, with ColorCommitted if it is new.
    private void Set(Color value, bool live, bool writeText = true)
    {
        _writing = !writeText;
        try { SetCurrentValue(ValueProperty, value); }
        finally { _writing = false; }
        if (writeText) Paint(writeText: true);
        SetMessage(null);
        if (!live && value != _committed)
        {
            _committed = value;
            RaiseEvent(new ColorCommittedEventArgs(ColorCommittedEvent, value));
        }
    }

    private void SetMessage(string? message)
    {
        ValidationMessage = message;
        PseudoClasses.Set(":invalid", message is not null);
        if (_input is not null) DataValidationErrors.SetErrors(_input, message is null ? null : new object[] { message });
    }

    private void OnPicked(object? sender, bool committed)
    {
        if (_picker?.Value is not { } c) return;
        _picking = true;
        try { Set(c, !committed); }
        finally { _picking = false; }
    }

    // Typing a hex: the swatch follows as soon as it is a colour.
    private void OnInputText(object? sender, TextChangedEventArgs e)
    {
        if (_writing || _input is null) return;
        if (ColorMath.ParseHex(_input.Text) is { } c)
        {
            _writing = true;
            try { SetCurrentValue(ValueProperty, c); }
            finally { _writing = false; }
            SetMessage(null);
        }
    }

    private void OnInputKey(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter) CommitTyped();
    }

    // Another window taking the keyboard is not leaving the field.
    private void OnInputLeave(object? sender, RoutedEventArgs e)
    {
        if (!WindowActivity.GetIsInactive(this)) CommitTyped();
    }

    // #abc, abc and aabbcc are taken and written out in full; anything else is refused with a message.
    private void CommitTyped()
    {
        if (_input is null) return;
        var raw = (_input.Text ?? string.Empty).Trim();
        if (raw.Length == 0)
        {
            SetCurrentValue(ValueProperty, null);
            SetMessage(null);
            return;
        }
        if (ColorMath.ParseHex(raw) is { } c) Set(c, false);
        else SetMessage(Arabic ? "اكتب لونًا مثل #4C1D95." : "Enter a color like #4C1D95.");
    }

    private void OnSwatchClick(object? sender, RoutedEventArgs e) => SetCurrentValue(IsDropDownOpenProperty, !IsDropDownOpen);

    // A key that opens the picker also shows the focus ring on its square; a click does not.
    private void OnSwatchKey(object? sender, KeyEventArgs e)
    {
        if (e.Key is Key.Enter or Key.Space or Key.Down or Key.F4) _openedByKey = true;
    }

    private void Open()
    {
        if (_popup is null) return;
        UpdatePresets();
        if (_picker is not null)
        {
            _picker.Swatches = Presets;
            _picker.Value = Value;
        }
        _popup.IsOpen = true;
        PseudoClasses.Set(":open", true);
        var method = _openedByKey ? NavigationMethod.Tab : NavigationMethod.Pointer;
        _openedByKey = false;
        Dispatcher.UIThread.Post(() => _picker?.FocusArea(method));
    }

    private void Close(bool refocus)
    {
        if (_popup is { IsOpen: true }) _popup.IsOpen = false;
        PseudoClasses.Set(":open", false);
        if (IsDropDownOpen) SetCurrentValue(IsDropDownOpenProperty, false);
        if (refocus) _swatch?.Focus();
    }

    private void OnPopupClosed(object? sender, EventArgs e) => Close(false);

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        if (e.Key == Key.Escape && IsDropDownOpen)
        {
            e.Handled = true;
            Close(true);
        }
    }
}
