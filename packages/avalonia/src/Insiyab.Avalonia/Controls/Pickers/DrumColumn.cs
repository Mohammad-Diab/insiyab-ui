using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Threading;

namespace Insiyab.Avalonia.Controls;

/// <summary>One wheel of a drum (the web's <c>.ins-drum-col</c>): items under a lens, moved by dragging with a fling, the wheel, a click or the arrow keys, snapping to an item.</summary>
public class DrumColumn : Control
{
    public static readonly StyledProperty<double> RowHeightProperty =
        AvaloniaProperty.Register<DrumColumn, double>(nameof(RowHeight), 36);

    public static readonly StyledProperty<int> VisibleRowsProperty =
        AvaloniaProperty.Register<DrumColumn, int>(nameof(VisibleRows), 5);

    public static readonly StyledProperty<bool> IsLoopingProperty =
        AvaloniaProperty.Register<DrumColumn, bool>(nameof(IsLooping));

    public static readonly StyledProperty<bool> IsPerspectiveProperty =
        AvaloniaProperty.Register<DrumColumn, bool>(nameof(IsPerspective), true);

    public static readonly StyledProperty<bool> IsUnsetProperty =
        AvaloniaProperty.Register<DrumColumn, bool>(nameof(IsUnset));

    public static readonly StyledProperty<IBrush?> ForegroundProperty =
        TextElement.ForegroundProperty.AddOwner<DrumColumn>();

    public static readonly StyledProperty<IBrush?> SelectedForegroundProperty =
        AvaloniaProperty.Register<DrumColumn, IBrush?>(nameof(SelectedForeground));

    public static readonly StyledProperty<IBrush?> DimForegroundProperty =
        AvaloniaProperty.Register<DrumColumn, IBrush?>(nameof(DimForeground));

    public static readonly StyledProperty<double> OutOpacityProperty =
        AvaloniaProperty.Register<DrumColumn, double>(nameof(OutOpacity), 1);

    public static readonly StyledProperty<IBrush?> FocusRingProperty =
        AvaloniaProperty.Register<DrumColumn, IBrush?>(nameof(FocusRing));

    public static readonly StyledProperty<FontFamily> FontFamilyProperty =
        TextElement.FontFamilyProperty.AddOwner<DrumColumn>();

    public static readonly StyledProperty<double> FontSizeProperty =
        TextElement.FontSizeProperty.AddOwner<DrumColumn>();

    public static readonly StyledProperty<FontWeight> FontWeightProperty =
        TextElement.FontWeightProperty.AddOwner<DrumColumn>();

    public static readonly StyledProperty<FontWeight> SelectedFontWeightProperty =
        AvaloniaProperty.Register<DrumColumn, FontWeight>(nameof(SelectedFontWeight), FontWeight.ExtraBold);

    private IReadOnlyList<string> _items = [];
    private double _offset;
    private double _from;
    private double _to;
    private TimeSpan? _animStart;
    private bool _animating;
    private Point? _press;
    private double _pressOffset;
    private bool _dragging;
    private double _velocity;
    private double _lastY;
    private DateTime _lastT;

    static DrumColumn()
    {
        FocusableProperty.OverrideDefaultValue<DrumColumn>(true);
        AffectsRender<DrumColumn>(ForegroundProperty, SelectedForegroundProperty, DimForegroundProperty, IsUnsetProperty, OutOpacityProperty, FontSizeProperty, FontWeightProperty, FontFamilyProperty);
        AffectsMeasure<DrumColumn>(RowHeightProperty, VisibleRowsProperty);
    }

    // A wheel's items are centred words and numbers, so it is drawn the same in both directions.
    public DrumColumn()
    {
        SetCurrentValue(FlowDirectionProperty, FlowDirection.LeftToRight);
        ClipToBounds = true;
        OpacityMask = new LinearGradientBrush
        {
            StartPoint = new RelativePoint(0, 0, RelativeUnit.Relative),
            EndPoint = new RelativePoint(0, 1, RelativeUnit.Relative),
            GradientStops =
            {
                new GradientStop(Colors.Transparent, 0),
                new GradientStop(Colors.Black, 0.3),
                new GradientStop(Colors.Black, 0.7),
                new GradientStop(Colors.Transparent, 1),
            },
        };
    }

    /// <summary>Raised when the wheel comes to rest, with the value it rests on.</summary>
    public event EventHandler<int>? Settled;

    /// <summary>Raised when the person moves the wheel themselves (drag, wheel, click or key), before it settles.</summary>
    public event EventHandler? Touched;

    public double RowHeight { get => GetValue(RowHeightProperty); set => SetValue(RowHeightProperty, value); }

    public int VisibleRows { get => GetValue(VisibleRowsProperty); set => SetValue(VisibleRowsProperty, value); }

    /// <summary>Whether the wheel runs round (hours, minutes) rather than stopping at its ends.</summary>
    public bool IsLooping { get => GetValue(IsLoopingProperty); set => SetValue(IsLoopingProperty, value); }

    /// <summary>The time drum's turning wheel (items tilt and shrink away from the lens); off, the items stay flat, as the birth date's columns do.</summary>
    public bool IsPerspective { get => GetValue(IsPerspectiveProperty); set => SetValue(IsPerspectiveProperty, value); }

    /// <summary>A column nobody has chosen from yet: the item under the lens is dimmed and does not count.</summary>
    public bool IsUnset { get => GetValue(IsUnsetProperty); set => SetValue(IsUnsetProperty, value); }

    public IBrush? Foreground { get => GetValue(ForegroundProperty); set => SetValue(ForegroundProperty, value); }

    public IBrush? SelectedForeground { get => GetValue(SelectedForegroundProperty); set => SetValue(SelectedForegroundProperty, value); }

    public IBrush? DimForeground { get => GetValue(DimForegroundProperty); set => SetValue(DimForegroundProperty, value); }

    public double OutOpacity { get => GetValue(OutOpacityProperty); set => SetValue(OutOpacityProperty, value); }

    public IBrush? FocusRing { get => GetValue(FocusRingProperty); set => SetValue(FocusRingProperty, value); }

    public FontFamily FontFamily { get => GetValue(FontFamilyProperty); set => SetValue(FontFamilyProperty, value); }

    public double FontSize { get => GetValue(FontSizeProperty); set => SetValue(FontSizeProperty, value); }

    public FontWeight FontWeight { get => GetValue(FontWeightProperty); set => SetValue(FontWeightProperty, value); }

    public FontWeight SelectedFontWeight { get => GetValue(SelectedFontWeightProperty); set => SetValue(SelectedFontWeightProperty, value); }

    /// <summary>Which values are outside the allowed range: drawn dimmed, so the snap back is expected.</summary>
    public Func<int, bool>? IsOut { get; set; }

    /// <summary>The PageUp and PageDown step.</summary>
    public int PageStep { get; set; } = 5;

    public IReadOnlyList<string> Items
    {
        get => _items;
        set
        {
            _items = value ?? [];
            if (!IsLooping && _items.Count > 0) _offset = Math.Clamp(_offset, 0, (_items.Count - 1) * RowHeight);
            InvalidateVisual();
        }
    }

    /// <summary>The value under the lens, 0 to Items.Count - 1.</summary>
    public int Value => Wrap((int)Math.Round(_offset / RowHeight));

    internal double Offset => _offset;

    internal bool IsMoving => _animating || _dragging;

    private int Wrap(int i)
    {
        var n = _items.Count;
        if (n == 0) return 0;
        return IsLooping ? ((i % n) + n) % n : Math.Clamp(i, 0, n - 1);
    }

    /// <summary>Puts the wheel on a value at once, or glides there.</summary>
    public void ScrollTo(int value, bool smooth = false)
    {
        if (_items.Count == 0) return;
        var target = NearestPosition(value);
        if (smooth) GlideTo(target);
        else
        {
            _animating = false;
            _offset = target * RowHeight;
            InvalidateVisual();
        }
    }

    // The nearest position showing this value, so a looping wheel goes the short way round.
    private int NearestPosition(int value)
    {
        var cur = (int)Math.Round(_offset / RowHeight);
        if (!IsLooping) return Math.Clamp(value, 0, Math.Max(0, _items.Count - 1));
        var n = _items.Count;
        var basePos = cur - Wrap(cur);
        var best = basePos + value;
        foreach (var c in new[] { best - n, best + n })
            if (Math.Abs(c - cur) < Math.Abs(best - cur)) best = c;
        return best;
    }

    private void GoPosition(int position)
    {
        if (!IsLooping) position = Math.Clamp(position, 0, Math.Max(0, _items.Count - 1));
        GlideTo(position);
    }

    private void GlideTo(int position)
    {
        var target = position * RowHeight;
        if (Math.Abs(target - _offset) < 0.5)
        {
            _offset = target;
            Settle();
            return;
        }
        _from = _offset;
        _to = target;
        _animStart = null;
        if (_animating) return;
        _animating = true;
        var top = TopLevel.GetTopLevel(this);
        if (top is null)
        {
            _offset = _to;
            _animating = false;
            Settle();
            return;
        }
        top.RequestAnimationFrame(Tick);
    }

    private void Tick(TimeSpan now)
    {
        if (!_animating) return;
        _animStart ??= now;
        var t = Math.Min(1, (now - _animStart.Value).TotalMilliseconds / 260);
        var eased = 1 - Math.Pow(1 - t, 3);
        _offset = _from + (_to - _from) * eased;
        InvalidateVisual();
        if (t >= 1)
        {
            _animating = false;
            _offset = _to;
            Settle();
            return;
        }
        var top = TopLevel.GetTopLevel(this);
        if (top is null) { _animating = false; _offset = _to; Settle(); return; }
        top.RequestAnimationFrame(Tick);
    }

    private void Settle()
    {
        InvalidateVisual();
        var v = Value;
        if (IsLooping) _offset = v * RowHeight;
        Settled?.Invoke(this, v);
    }

    protected override Size MeasureOverride(Size availableSize) => new(0, RowHeight * VisibleRows);

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);
        if (!e.GetCurrentPoint(this).Properties.IsLeftButtonPressed || !IsEffectivelyEnabled) return;
        Focus(NavigationMethod.Pointer);
        _press = e.GetPosition(this);
        _pressOffset = _offset;
        _dragging = false;
        _velocity = 0;
        _lastY = _press.Value.Y;
        _lastT = DateTime.UtcNow;
        _animating = false;
        e.Pointer.Capture(this);
        e.Handled = true;
        Touched?.Invoke(this, EventArgs.Empty);
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);
        if (_press is null) return;
        var p = e.GetPosition(this);
        var dy = p.Y - _press.Value.Y;
        if (!_dragging && Math.Abs(dy) > 3) _dragging = true;
        if (!_dragging) return;
        var now = DateTime.UtcNow;
        var dt = Math.Max(1, (now - _lastT).TotalMilliseconds);
        _offset = _pressOffset - dy;
        if (!IsLooping) _offset = Math.Clamp(_offset, -RowHeight, _items.Count * RowHeight);
        _velocity = _velocity * 0.6 + (p.Y - _lastY) / dt * 0.4;
        _lastY = p.Y;
        _lastT = now;
        InvalidateVisual();
    }

    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        base.OnPointerReleased(e);
        if (_press is null) return;
        _press = null;
        e.Pointer.Capture(null);
        if (!_dragging)
        {
            var y = e.GetPosition(this).Y;
            var pos = (int)Math.Round((_offset + y - Bounds.Height / 2) / RowHeight);
            GoPosition(pos);
            return;
        }
        _dragging = false;
        var fling = (DateTime.UtcNow - _lastT).TotalMilliseconds > 80 || Math.Abs(_velocity) < 0.6 ? 0 : _velocity;
        GoPosition((int)Math.Round((_offset - fling * 180) / RowHeight));
    }

    protected override void OnPointerCaptureLost(PointerCaptureLostEventArgs e)
    {
        base.OnPointerCaptureLost(e);
        if (_press is null) return;
        _press = null;
        _dragging = false;
        GoPosition((int)Math.Round(_offset / RowHeight));
    }

    protected override void OnPointerWheelChanged(PointerWheelEventArgs e)
    {
        base.OnPointerWheelChanged(e);
        if (!IsEffectivelyEnabled || _items.Count == 0) return;
        Touched?.Invoke(this, EventArgs.Empty);
        var steps = e.Delta.Y > 0 ? -1 : e.Delta.Y < 0 ? 1 : 0;
        if (steps == 0) return;
        var target = (_animating ? (int)Math.Round(_to / RowHeight) : (int)Math.Round(_offset / RowHeight)) + steps;
        GoPosition(target);
        e.Handled = true;
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        if (e.Handled || _items.Count == 0) return;
        var cur = _animating ? (int)Math.Round(_to / RowHeight) : (int)Math.Round(_offset / RowHeight);
        var v = Wrap(cur);
        int? to = e.Key switch
        {
            Key.Up => cur - 1,
            Key.Down => cur + 1,
            Key.PageUp => cur - PageStep,
            Key.PageDown => cur + PageStep,
            Key.Home => cur - v,
            Key.End => cur - v + _items.Count - 1,
            _ => null,
        };
        if (to is null) return;
        e.Handled = true;
        Touched?.Invoke(this, EventArgs.Empty);
        GoPosition(to.Value);
    }

    protected override void OnGotFocus(FocusChangedEventArgs e)
    {
        base.OnGotFocus(e);
        InvalidateVisual();
    }

    protected override void OnLostFocus(FocusChangedEventArgs e)
    {
        base.OnLostFocus(e);
        InvalidateVisual();
    }

    public override void Render(DrawingContext context)
    {
        var n = _items.Count;
        var row = RowHeight;
        var h = Bounds.Height;
        var w = Bounds.Width;
        if (n > 0)
        {
            var mid = _offset / row;
            var first = (int)Math.Floor(mid - VisibleRows / 2.0 - 1);
            var last = (int)Math.Ceiling(mid + VisibleRows / 2.0 + 1);
            var typeface = new Typeface(FontFamily, FontStyle.Normal, FontWeight);
            var onFace = new Typeface(FontFamily, FontStyle.Normal, IsUnset ? FontWeight.SemiBold : SelectedFontWeight);
            for (var i = first; i <= last; i++)
            {
                if (!IsLooping && (i < 0 || i >= n)) continue;
                var value = Wrap(i);
                var d = i - mid;
                if (Math.Abs(d) > 4) continue;
                var on = Math.Abs(d) < 0.5;
                var brush = on ? (IsUnset ? DimForeground : SelectedForeground) ?? Foreground : Foreground;
                var opacity = 1.0;
                var scaleX = 1.0;
                var scaleY = 1.0;
                if (IsPerspective)
                {
                    var a = Math.Min(Math.Abs(d), 3);
                    opacity = 1 - a * 0.3;
                    scaleX = 1 - a * 0.1;
                    scaleY = scaleX * Math.Cos(Math.Min(Math.Abs(d) * 22, 89) * Math.PI / 180);
                }
                var outside = IsOut?.Invoke(value) == true;
                if (outside && !on)
                {
                    if (DimForeground is not null && OutOpacity >= 1) brush = DimForeground;
                    opacity *= OutOpacity;
                }
                else if (outside) opacity *= OutOpacity;
                if (opacity <= 0.01 || brush is null) continue;
                var text = new FormattedText(_items[value], CultureInfo.InvariantCulture, FlowDirection.LeftToRight, on ? onFace : typeface, FontSize, brush);
                var cy = h / 2 + d * row;
                var origin = new Point(w / 2, cy);
                using (context.PushOpacity(opacity))
                using (context.PushTransform(Matrix.CreateTranslation(-origin.X, -origin.Y) * Matrix.CreateScale(scaleX, scaleY) * Matrix.CreateTranslation(origin.X, origin.Y)))
                    context.DrawText(text, new Point(w / 2 - text.Width / 2, cy - text.Height / 2));
            }
        }
        if (IsFocused && IsKeyboardFocusWithin && FocusRing is not null && PseudoClasses.Contains(":focus-visible"))
            context.DrawRectangle(null, new Pen(FocusRing, 3), new Rect(1.5, 1.5, Math.Max(0, w - 3), Math.Max(0, h - 3)), 8, 8);
    }
}
