using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using Insiyab.Avalonia.Brand;

namespace Insiyab.Avalonia.Controls;

// The colour picker's maths, as in src/plugins/color.js.
internal static class ColorMath
{
    public static Color? ParseHex(string? value)
    {
        var s = (value ?? string.Empty).Trim().TrimStart('#').ToLowerInvariant();
        if (s.Length == 3 && s.All(Uri.IsHexDigit)) s = new string([s[0], s[0], s[1], s[1], s[2], s[2]]);
        if (s.Length != 6 || !s.All(Uri.IsHexDigit)) return null;
        var n = int.Parse(s, NumberStyles.HexNumber, CultureInfo.InvariantCulture);
        return Color.FromRgb((byte)(n >> 16), (byte)(n >> 8), (byte)n);
    }

    public static string Hex(Color c) => $"#{c.R:x2}{c.G:x2}{c.B:x2}";

    public static (double H, double S, double V) ToHsv(Color c)
    {
        double r = c.R / 255d, g = c.G / 255d, b = c.B / 255d;
        var max = Math.Max(r, Math.Max(g, b));
        var min = Math.Min(r, Math.Min(g, b));
        var d = max - min;
        double h = 0;
        if (d > 0)
        {
            if (max == r) h = ((g - b) / d) % 6;
            else if (max == g) h = (b - r) / d + 2;
            else h = (r - g) / d + 4;
            h *= 60;
            if (h < 0) h += 360;
        }
        return (h, max > 0 ? d / max : 0, max);
    }

    public static Color FromHsv(double h, double s, double v)
    {
        h = ((h % 360) + 360) % 360;
        var c = v * s;
        var x = c * (1 - Math.Abs((h / 60) % 2 - 1));
        var m = v - c;
        double r = 0, g = 0, b = 0;
        if (h < 60) { r = c; g = x; }
        else if (h < 120) { r = x; g = c; }
        else if (h < 180) { g = c; b = x; }
        else if (h < 240) { g = x; b = c; }
        else if (h < 300) { r = x; b = c; }
        else { r = c; b = x; }
        static byte B(double v) => (byte)Math.Round(Math.Clamp(v * 255, 0, 255));
        return Color.FromRgb(B(r + m), B(g + m), B(b + m));
    }

    public static readonly Color[] DefaultSwatches =
        new[] { "#0f766e", "#0369a1", "#4338ca", "#be123c", "#c2410c", "#a16207", "#15803d", "#334155", "#0f172a" }
            .Select(h => ParseHex(h)!.Value).ToArray();
}

// The two pens of a thumb, made once.
internal static class ColorPens
{
    public static readonly ImmutablePen White = new(new ImmutableSolidColorBrush(Colors.White), 2);
    public static readonly ImmutablePen Shade = new(new ImmutableSolidColorBrush(Color.FromArgb(77, 15, 23, 42)), 3);
}

/// <summary>The picker's square: saturation across, brightness down, over the hue. A slider for the keyboard too.</summary>
public class ColorArea : Control
{
    public static readonly StyledProperty<double> HueProperty = AvaloniaProperty.Register<ColorArea, double>(nameof(Hue));
    public static readonly StyledProperty<double> SaturationProperty = AvaloniaProperty.Register<ColorArea, double>(nameof(Saturation));
    public static readonly StyledProperty<double> BrightnessProperty = AvaloniaProperty.Register<ColorArea, double>(nameof(Brightness));
    public static readonly StyledProperty<Color> ThumbColorProperty = AvaloniaProperty.Register<ColorArea, Color>(nameof(ThumbColor), Colors.Black);
    public static readonly StyledProperty<CornerRadius> CornerRadiusProperty = AvaloniaProperty.Register<ColorArea, CornerRadius>(nameof(CornerRadius));
    public static readonly StyledProperty<IBrush?> RingProperty = AvaloniaProperty.Register<ColorArea, IBrush?>(nameof(Ring));

    private static readonly ImmutableLinearGradientBrush WhiteFade = new(
        [new ImmutableGradientStop(0, Colors.White), new ImmutableGradientStop(1, Color.FromArgb(0, 255, 255, 255))],
        1, null, null, GradientSpreadMethod.Pad, new RelativePoint(0, 0, RelativeUnit.Relative), new RelativePoint(1, 0, RelativeUnit.Relative));

    private static readonly ImmutableLinearGradientBrush BlackFade = new(
        [new ImmutableGradientStop(0, Colors.Black), new ImmutableGradientStop(1, Color.FromArgb(0, 0, 0, 0))],
        1, null, null, GradientSpreadMethod.Pad, new RelativePoint(0, 1, RelativeUnit.Relative), new RelativePoint(0, 0, RelativeUnit.Relative));

    private IImmutableBrush _hueBrush = new ImmutableSolidColorBrush(ColorMath.FromHsv(0, 1, 1));
    private IImmutableBrush _thumbBrush = new ImmutableSolidColorBrush(Colors.Black);
    private ImmutablePen? _ringPen;
    private bool _dragging;

    static ColorArea()
    {
        FocusableProperty.OverrideDefaultValue<ColorArea>(true);
        AffectsRender<ColorArea>(HueProperty, SaturationProperty, BrightnessProperty, ThumbColorProperty, CornerRadiusProperty, RingProperty);
    }

    public ColorArea()
    {
        SetCurrentValue(FlowDirectionProperty, FlowDirection.LeftToRight);
        Cursor = new Cursor(StandardCursorType.Cross);
    }

    public double Hue { get => GetValue(HueProperty); set => SetValue(HueProperty, value); }
    public double Saturation { get => GetValue(SaturationProperty); set => SetValue(SaturationProperty, value); }
    public double Brightness { get => GetValue(BrightnessProperty); set => SetValue(BrightnessProperty, value); }
    public Color ThumbColor { get => GetValue(ThumbColorProperty); set => SetValue(ThumbColorProperty, value); }
    public CornerRadius CornerRadius { get => GetValue(CornerRadiusProperty); set => SetValue(CornerRadiusProperty, value); }

    /// <summary>The keyboard focus ring's colour; null hides it.</summary>
    public IBrush? Ring { get => GetValue(RingProperty); set => SetValue(RingProperty, value); }

    /// <summary>Saturation or brightness moved: committed is false while the pointer still drags it.</summary>
    public event EventHandler<bool>? Moved;

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == HueProperty) _hueBrush = new ImmutableSolidColorBrush(ColorMath.FromHsv(Hue, 1, 1));
        else if (change.Property == ThumbColorProperty) _thumbBrush = new ImmutableSolidColorBrush(ThumbColor);
        else if (change.Property == RingProperty) _ringPen = Ring is { } ring ? new ImmutablePen(ring.ToImmutable(), 2) : null;
    }

    public override void Render(DrawingContext context)
    {
        var rect = new Rect(Bounds.Size);
        context.FillRectangle(Brushes.Transparent, rect);
        var rr = new RoundedRect(rect, CornerRadius);
        if (_ringPen is not null)
        {
            context.DrawRectangle(null, ColorPens.White, rr.Inflate(1, 1));
            context.DrawRectangle(null, _ringPen, rr.Inflate(3, 3));
        }
        using (context.PushClip(rr))
        {
            context.DrawRectangle(_hueBrush, null, rect);
            context.DrawRectangle(WhiteFade, null, rect);
            context.DrawRectangle(BlackFade, null, rect);
        }
        var p = new Point(Saturation * rect.Width, (1 - Brightness) * rect.Height);
        context.DrawEllipse(null, ColorPens.Shade, p, 8.5, 8.5);
        context.DrawEllipse(_thumbBrush, ColorPens.White, p, 7, 7);
    }

    private void From(Point p, bool committed)
    {
        var w = Math.Max(1, Bounds.Width);
        var h = Math.Max(1, Bounds.Height);
        SetCurrentValue(SaturationProperty, Math.Clamp(p.X / w, 0, 1));
        SetCurrentValue(BrightnessProperty, Math.Clamp(1 - p.Y / h, 0, 1));
        Moved?.Invoke(this, committed);
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);
        if (!e.GetCurrentPoint(this).Properties.IsLeftButtonPressed) return;
        _dragging = true;
        e.Pointer.Capture(this);
        Focus(NavigationMethod.Pointer);
        From(e.GetPosition(this), false);
        e.Handled = true;
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);
        if (_dragging) From(e.GetPosition(this), false);
    }

    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        base.OnPointerReleased(e);
        if (!_dragging) return;
        _dragging = false;
        e.Pointer.Capture(null);
        Moved?.Invoke(this, true);
    }

    protected override void OnPointerCaptureLost(PointerCaptureLostEventArgs e)
    {
        base.OnPointerCaptureLost(e);
        if (!_dragging) return;
        _dragging = false;
        Moved?.Invoke(this, true);
    }

    // ←→ saturation, ↑↓ brightness, with Shift for steps of ten.
    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        var step = e.KeyModifiers.HasFlag(KeyModifiers.Shift) ? 0.1 : 0.01;
        double s = Saturation, v = Brightness;
        switch (e.Key)
        {
            case Key.Right: s = Math.Min(1, s + step); break;
            case Key.Left: s = Math.Max(0, s - step); break;
            case Key.Up: v = Math.Min(1, v + step); break;
            case Key.Down: v = Math.Max(0, v - step); break;
            case Key.PageUp: v = Math.Min(1, v + 0.1); break;
            case Key.PageDown: v = Math.Max(0, v - 0.1); break;
            default: return;
        }
        e.Handled = true;
        SetCurrentValue(SaturationProperty, s);
        SetCurrentValue(BrightnessProperty, v);
        Moved?.Invoke(this, true);
    }
}

/// <summary>The hue strip: a rainbow pill with a ring for a thumb, dragged or moved with the keyboard as a range input is.</summary>
public class ColorHueSlider : Control
{
    public static readonly StyledProperty<double> HueProperty = AvaloniaProperty.Register<ColorHueSlider, double>(nameof(Hue));
    public static readonly StyledProperty<IBrush?> RingProperty = AvaloniaProperty.Register<ColorHueSlider, IBrush?>(nameof(Ring));

    private static readonly ImmutableLinearGradientBrush Rainbow = new(
        new (double Offset, string Hex)[] { (0, "#f00"), (0.17, "#ff0"), (0.33, "#0f0"), (0.5, "#0ff"), (0.67, "#00f"), (0.83, "#f0f"), (1, "#f00") }
            .Select(s => new ImmutableGradientStop(s.Offset, ColorMath.ParseHex(s.Hex)!.Value)).ToArray(),
        1, null, null, GradientSpreadMethod.Pad, new RelativePoint(0, 0, RelativeUnit.Relative), new RelativePoint(1, 0, RelativeUnit.Relative));

    private ImmutablePen? _ringPen;
    private bool _dragging;

    static ColorHueSlider()
    {
        FocusableProperty.OverrideDefaultValue<ColorHueSlider>(true);
        AffectsRender<ColorHueSlider>(HueProperty, RingProperty);
    }

    public ColorHueSlider()
    {
        SetCurrentValue(FlowDirectionProperty, FlowDirection.LeftToRight);
        Cursor = new Cursor(StandardCursorType.Hand);
    }

    public double Hue { get => GetValue(HueProperty); set => SetValue(HueProperty, value); }
    public IBrush? Ring { get => GetValue(RingProperty); set => SetValue(RingProperty, value); }

    public event EventHandler<bool>? Moved;

    protected override Size MeasureOverride(Size availableSize) => new(double.IsInfinity(availableSize.Width) ? 160 : availableSize.Width, 16);

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == RingProperty) _ringPen = Ring is { } ring ? new ImmutablePen(ring.ToImmutable(), 2) : null;
    }

    public override void Render(DrawingContext context)
    {
        var w = Bounds.Width;
        context.FillRectangle(Brushes.Transparent, new Rect(Bounds.Size));
        var track = new RoundedRect(new Rect(0, 2, w, 12), 6);
        if (_ringPen is not null) context.DrawRectangle(null, _ringPen, track.Inflate(1, 1));
        context.DrawRectangle(Rainbow, null, track);
        var x = 8 + (w - 16) * Math.Clamp(Hue / 359, 0, 1);
        var c = new Point(x, 8);
        context.DrawEllipse(null, ColorPens.Shade, c, 8.5, 8.5);
        context.DrawEllipse(null, ColorPens.White, c, 7, 7);
    }

    private void From(Point p, bool committed)
    {
        var w = Math.Max(1, Bounds.Width - 16);
        SetCurrentValue(HueProperty, Math.Round(Math.Clamp((p.X - 8) / w, 0, 1) * 359));
        Moved?.Invoke(this, committed);
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);
        if (!e.GetCurrentPoint(this).Properties.IsLeftButtonPressed) return;
        _dragging = true;
        e.Pointer.Capture(this);
        Focus(NavigationMethod.Pointer);
        From(e.GetPosition(this), false);
        e.Handled = true;
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);
        if (_dragging) From(e.GetPosition(this), false);
    }

    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        base.OnPointerReleased(e);
        if (!_dragging) return;
        _dragging = false;
        e.Pointer.Capture(null);
        Moved?.Invoke(this, true);
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        var h = Hue;
        switch (e.Key)
        {
            case Key.Right or Key.Up: h = Math.Min(359, h + 1); break;
            case Key.Left or Key.Down: h = Math.Max(0, h - 1); break;
            case Key.PageUp: h = Math.Min(359, h + 36); break;
            case Key.PageDown: h = Math.Max(0, h - 36); break;
            case Key.Home: h = 0; break;
            case Key.End: h = 359; break;
            default: return;
        }
        e.Handled = true;
        SetCurrentValue(HueProperty, h);
        Moved?.Invoke(this, true);
    }
}

/// <summary>A preset in the picker: a square chip of its colour.</summary>
public class ColorPreset : Button
{
    public static readonly StyledProperty<Color> ColorProperty = AvaloniaProperty.Register<ColorPreset, Color>(nameof(Color));

    public static readonly DirectProperty<ColorPreset, IBrush?> BrushProperty =
        AvaloniaProperty.RegisterDirect<ColorPreset, IBrush?>(nameof(Brush), o => o.Brush);

    private IBrush? _brush = new ImmutableSolidColorBrush(Colors.Transparent);

    public Color Color { get => GetValue(ColorProperty); set => SetValue(ColorProperty, value); }

    /// <summary>The chip's fill: <see cref="Color"/> as one brush made when the colour is set.</summary>
    public IBrush? Brush { get => _brush; private set => SetAndRaise(BrushProperty, ref _brush, value); }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == ColorProperty) Brush = new ImmutableSolidColorBrush(Color);
    }

    protected override Type StyleKeyOverride => typeof(ColorPreset);

    protected override Size MeasureOverride(Size availableSize)
    {
        base.MeasureOverride(availableSize);
        var side = double.IsInfinity(availableSize.Width) ? 24 : availableSize.Width;
        return new Size(side, side);
    }
}

/// <summary>One line of the picker's readout: the colour as text on white or on black, the ratio, and whether it carries body text.</summary>
[PseudoClasses(":pass", ":fail")]
public class ColorContrastLine : TemplatedControl
{
    public static readonly StyledProperty<IBrush?> SampleBackgroundProperty = AvaloniaProperty.Register<ColorContrastLine, IBrush?>(nameof(SampleBackground));
    public static readonly StyledProperty<IBrush?> SampleForegroundProperty = AvaloniaProperty.Register<ColorContrastLine, IBrush?>(nameof(SampleForeground));
    public static readonly StyledProperty<string?> WhereProperty = AvaloniaProperty.Register<ColorContrastLine, string?>(nameof(Where));
    public static readonly StyledProperty<string?> RatioProperty = AvaloniaProperty.Register<ColorContrastLine, string?>(nameof(Ratio));
    public static readonly StyledProperty<string?> VerdictProperty = AvaloniaProperty.Register<ColorContrastLine, string?>(nameof(Verdict));

    public IBrush? SampleBackground { get => GetValue(SampleBackgroundProperty); set => SetValue(SampleBackgroundProperty, value); }
    public IBrush? SampleForeground { get => GetValue(SampleForegroundProperty); set => SetValue(SampleForegroundProperty, value); }
    public string? Where { get => GetValue(WhereProperty); set => SetValue(WhereProperty, value); }
    public string? Ratio { get => GetValue(RatioProperty); set => SetValue(RatioProperty, value); }
    public string? Verdict { get => GetValue(VerdictProperty); set => SetValue(VerdictProperty, value); }

    internal void Set(Color color, Color against, string where, bool arabic)
    {
        var ratio = BrandDerivation.Contrast(color, against);
        var ok = ratio >= 4.5;
        SampleBackground = new SolidColorBrush(against);
        SampleForeground = new SolidColorBrush(color);
        Where = where;
        Ratio = (Math.Round(ratio * 10) / 10).ToString("0.0", CultureInfo.InvariantCulture) + ":1";
        Verdict = ok ? (arabic ? "يصلح للنص" : "fine for text") : (arabic ? "لا يكفي للنص" : "too low for text");
        PseudoClasses.Set(":pass", ok);
        PseudoClasses.Set(":fail", !ok);
    }
}

/// <summary>The colour picker's body: the square, the hue strip, the presets and the contrast readout. <see cref="ColorField"/> opens it in a popup.</summary>
[TemplatePart("PART_Area", typeof(ColorArea))]
[TemplatePart("PART_Hue", typeof(ColorHueSlider))]
[TemplatePart("PART_Swatches", typeof(Panel))]
[TemplatePart("PART_OnWhite", typeof(ColorContrastLine))]
[TemplatePart("PART_OnBlack", typeof(ColorContrastLine))]
public class ColorPickerPanel : TemplatedControl
{
    public static readonly StyledProperty<Color?> ValueProperty =
        AvaloniaProperty.Register<ColorPickerPanel, Color?>(nameof(Value));

    public static readonly StyledProperty<IReadOnlyList<Color>?> SwatchesProperty =
        AvaloniaProperty.Register<ColorPickerPanel, IReadOnlyList<Color>?>(nameof(Swatches));

    public static readonly StyledProperty<string?> LocaleProperty =
        AvaloniaProperty.Register<ColorPickerPanel, string?>(nameof(Locale));

    public static readonly DirectProperty<ColorPickerPanel, string> HexTextProperty =
        AvaloniaProperty.RegisterDirect<ColorPickerPanel, string>(nameof(HexText), o => o.HexText);

    public static readonly DirectProperty<ColorPickerPanel, string> ContrastLabelProperty =
        AvaloniaProperty.RegisterDirect<ColorPickerPanel, string>(nameof(ContrastLabel), o => o.ContrastLabel);

    private ColorArea? _area;
    private ColorHueSlider? _hue;
    private Panel? _swatches;
    private ColorContrastLine? _white, _black;
    private double _h, _s, _v;
    private bool _applying;
    private string _hex = string.Empty, _contrast = string.Empty;

    public Color? Value { get => GetValue(ValueProperty); set => SetValue(ValueProperty, value); }

    /// <summary>The presets; an empty list hides them.</summary>
    public IReadOnlyList<Color>? Swatches { get => GetValue(SwatchesProperty); set => SetValue(SwatchesProperty, value); }

    public string? Locale { get => GetValue(LocaleProperty); set => SetValue(LocaleProperty, value); }

    public string HexText { get => _hex; private set => SetAndRaise(HexTextProperty, ref _hex, value); }
    public string ContrastLabel { get => _contrast; private set => SetAndRaise(ContrastLabelProperty, ref _contrast, value); }

    public double Hue => _h;
    public double Saturation => _s;
    public double Brightness => _v;

    /// <summary>The colour moved; committed is false while it is still being dragged (the web's input, then change).</summary>
    public event EventHandler<bool>? ColorPicked;

    private bool Arabic => PluginText.IsArabic(this, Locale);

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        if (_area is not null) _area.Moved -= OnAreaMoved;
        if (_hue is not null) _hue.Moved -= OnHueMoved;
        base.OnApplyTemplate(e);
        _area = e.NameScope.Find<ColorArea>("PART_Area");
        _hue = e.NameScope.Find<ColorHueSlider>("PART_Hue");
        _swatches = e.NameScope.Find<Panel>("PART_Swatches");
        _white = e.NameScope.Find<ColorContrastLine>("PART_OnWhite");
        _black = e.NameScope.Find<ColorContrastLine>("PART_OnBlack");
        if (_area is not null) _area.Moved += OnAreaMoved;
        if (_hue is not null) _hue.Moved += OnHueMoved;
        BuildSwatches();
        Sync(true);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == ValueProperty && !_applying) Sync(true);
        else if (change.Property == SwatchesProperty) BuildSwatches();
        else if (change.Property == LocaleProperty || change.Property == FlowDirectionProperty) Sync(false);
    }

    /// <summary>Focuses the square, as the web picker does when it opens; Tab shows the focus ring, Pointer does not.</summary>
    public void FocusArea(NavigationMethod method) => _area?.Focus(method);

    /// <summary>Focuses the square with the ring, as a key press would.</summary>
    public void FocusArea() => FocusArea(NavigationMethod.Tab);

    private void BuildSwatches()
    {
        if (_swatches is null) return;
        foreach (var b in _swatches.Children.OfType<ColorPreset>()) b.Click -= OnPreset;
        _swatches.Children.Clear();
        var list = Swatches ?? [];
        foreach (var c in list)
        {
            var b = new ColorPreset { Color = c };
            global::Avalonia.Automation.AutomationProperties.SetName(b, ColorMath.Hex(c).ToUpperInvariant());
            b.Click += OnPreset;
            _swatches.Children.Add(b);
        }
        _swatches.IsVisible = list.Count > 0;
    }

    private void OnPreset(object? sender, RoutedEventArgs e)
    {
        if (sender is not ColorPreset p) return;
        SetCurrentValue(ValueProperty, p.Color);
        ColorPicked?.Invoke(this, true);
    }

    private void OnAreaMoved(object? sender, bool committed)
    {
        if (_area is null) return;
        _s = _area.Saturation;
        _v = _area.Brightness;
        Apply(committed);
    }

    private void OnHueMoved(object? sender, bool committed)
    {
        if (_hue is null) return;
        _h = _hue.Hue;
        Apply(committed);
    }

    // The picker's own state to the colour, without reading it back: rounded to a hex, it would nudge the thumb on every move.
    private void Apply(bool committed)
    {
        _applying = true;
        try { SetCurrentValue(ValueProperty, ColorMath.FromHsv(_h, _s, _v)); }
        finally { _applying = false; }
        Sync(false);
        ColorPicked?.Invoke(this, committed);
    }

    private void Sync(bool fromValue)
    {
        if (fromValue)
        {
            if (Value is { } c)
            {
                var (h, s, v) = ColorMath.ToHsv(c);
                if (s > 0 && v > 0) _h = h;
                _s = s;
                _v = v;
            }
            else { _h = 0; _s = 0; _v = 0; }
        }
        if (_area is not null)
        {
            _area.Hue = _h;
            _area.Saturation = _s;
            _area.Brightness = _v;
            _area.ThumbColor = Value ?? Colors.Black;
        }
        if (_hue is not null) _hue.Hue = Math.Round(_h) % 360;
        var ar = Arabic;
        ContrastLabel = ar ? "التباين" : "Contrast";
        if (Value is { } value)
        {
            HexText = ColorMath.Hex(value).ToUpperInvariant();
            _white?.Set(value, Colors.White, ar ? "على الأبيض" : "on white", ar);
            _black?.Set(value, Colors.Black, ar ? "على الأسود" : "on black", ar);
        }
        else HexText = string.Empty;
    }
}
