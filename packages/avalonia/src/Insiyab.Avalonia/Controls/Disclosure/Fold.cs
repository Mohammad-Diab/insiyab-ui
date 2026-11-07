using System.Diagnostics;
using Avalonia;
using Avalonia.Animation.Easings;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;

namespace Insiyab.Avalonia.Controls;

/// <summary>Shows its child at a height that grows from nothing when <see cref="IsOpen"/> turns on and shrinks back when it turns off, fading with it (the web's <c>::details-content</c>); closed, it is hidden, so a panel's spacing skips it. It clips only while it moves, and nothing moves before its first layout, on a new DataContext or in a still window.</summary>
public class Fold : Decorator
{
    public static readonly StyledProperty<bool> IsOpenProperty =
        AvaloniaProperty.Register<Fold, bool>(nameof(IsOpen));

    public static readonly StyledProperty<bool> FadeProperty =
        AvaloniaProperty.Register<Fold, bool>(nameof(Fade), true);

    public static readonly StyledProperty<double> BleedProperty =
        AvaloniaProperty.Register<Fold, double>(nameof(Bleed));

    public static readonly StyledProperty<bool> EasesHeightProperty =
        AvaloniaProperty.Register<Fold, bool>(nameof(EasesHeight));

    private static readonly Stopwatch Clock = Stopwatch.StartNew();

    private bool _armed;
    private int _arming;
    private bool _running;
    private bool _closing;
    private bool _fading;
    private bool _easeNext;
    private int _run;
    private double _from;
    private double _start;
    private double _duration;
    private double _progress;
    private double _natural = double.NaN;
    private Easing _easing = new SplineEasing(0.22, 1, 0.36, 1);
    private IDisposable? _opacity;
    private IDisposable? _clip;

    static Fold()
    {
        IsVisibleProperty.OverrideMetadata<Fold>(new StyledPropertyMetadata<bool>(true, coerce: (o, v) => v && o is Fold f && (f.IsOpen || f._running)));
    }

    public Fold() => CoerceValue(IsVisibleProperty);

    // For tests: the milliseconds every fold is timed by, which a test can hold and step.
    internal static Func<double> Now { get; set; } = () => Clock.Elapsed.TotalMilliseconds;

    public bool IsOpen { get => GetValue(IsOpenProperty); set => SetValue(IsOpenProperty, value); }

    /// <summary>Whether the content fades in and out with its height; true by default.</summary>
    public bool Fade { get => GetValue(FadeProperty); set => SetValue(FadeProperty, value); }

    /// <summary>How far past the sides and the top the content still shows while the fold moves, so shadows and focus rings are not cut there; 0 clips all four sides.</summary>
    public double Bleed { get => GetValue(BleedProperty); set => SetValue(BleedProperty, value); }

    /// <summary>Whether a change in the open content's height eases to the new height instead of jumping.</summary>
    public bool EasesHeight { get => GetValue(EasesHeightProperty); set => SetValue(EasesHeightProperty, value); }

    /// <summary>Eases the next change in the open content's height only.</summary>
    public void EaseNext() => _easeNext = true;

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == IsOpenProperty) Toggle(change.GetNewValue<bool>());
        else if (change.Property == DataContextProperty)
        {
            Finish();
            Arm();
        }
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        Arm();
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        _armed = false;
        _arming++;
        Finish();
    }

    // Motion starts only once a layout has passed since the fold was attached or given new data.
    private void Arm()
    {
        _armed = false;
        var stamp = ++_arming;
        Dispatcher.UIThread.Post(() => { if (stamp == _arming && this.IsAttachedToVisualTree()) _armed = true; }, DispatcherPriority.Loaded);
    }

    private void Toggle(bool open)
    {
        var hidden = !IsVisible;
        var from = hidden ? 0 : Bounds.Height;
        if (_armed && !Motion.IsStill(this) && (open || from > 0))
        {
            Begin(from, !open, Fade);
            // a fold that was hidden kept the bounds of its last close and was never drawn since, so it is judged by its parent (else a second opening jumped)
            if (_running && Seen.Now(hidden && this.GetVisualParent() is { } parent ? parent : this)) return;
        }
        Finish();
    }

    private void Begin(double from, bool closing, bool fading)
    {
        _from = from;
        _closing = closing;
        _fading = fading;
        _progress = 0;
        _running = true;
        _duration = (closing ? Motion.Exit(this) : Motion.Enter(this)).TotalMilliseconds;
        _easing = Motion.Ease(this);
        _start = Now();
        var run = ++_run;
        CoerceValue(IsVisibleProperty);
        InvalidateMeasure();
        if (TopLevel.GetTopLevel(this) is not { } top)
        {
            Finish();
            return;
        }
        top.RequestAnimationFrame(_ => Frame(top, run));
        // Frames stop arriving when the window is hidden, so the fold also lands on a timer.
        DispatcherTimer.RunOnce(() => { if (run == _run && _running && Now() - _start >= _duration) Finish(); }, TimeSpan.FromMilliseconds(_duration + 150));
    }

    private void Frame(TopLevel top, int run)
    {
        if (run != _run || !_running) return;
        var t = _duration <= 0 ? 1 : Math.Clamp((Now() - _start) / _duration, 0, 1);
        _progress = _easing.Ease(t);
        InvalidateMeasure();
        if (t >= 1) Finish();
        else top.RequestAnimationFrame(_ => Frame(top, run));
    }

    private void Finish()
    {
        _running = false;
        _run++;
        _opacity?.Dispose();
        _clip?.Dispose();
        _opacity = _clip = null;
        CoerceValue(IsVisibleProperty);
        InvalidateMeasure();
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        if (Child is not { } child) return default;
        child.Measure(availableSize.WithHeight(double.PositiveInfinity));
        var natural = child.DesiredSize.Height;
        if (IsOpen && !_running && !double.IsNaN(_natural) && Math.Abs(natural - _natural) > 0.5)
        {
            if ((EasesHeight || _easeNext) && _armed && !Motion.IsStill(this) && Seen.Now(this)) Begin(_natural, false, false);
            _easeNext = false;
        }
        if (IsOpen) _natural = natural;
        var height = !_running ? (IsOpen ? natural : 0) : _from + ((_closing ? 0 : natural) - _from) * _progress;
        return new Size(child.DesiredSize.Width, Math.Max(0, height));
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        if (Child is { } child)
            child.Arrange(new Rect(0, 0, finalSize.Width, _running ? child.DesiredSize.Height : Math.Max(child.DesiredSize.Height, finalSize.Height)));
        if (_running)
        {
            var b = Math.Max(0, Bleed);
            _clip?.Dispose();
            _clip = SetValue(ClipProperty, new RectangleGeometry(new Rect(-b, -b, finalSize.Width + 2 * b, finalSize.Height + b)), BindingPriority.Animation);
            if (_fading && Fade)
            {
                var natural = Child?.DesiredSize.Height ?? 0;
                _opacity?.Dispose();
                _opacity = SetValue(OpacityProperty, natural > 0 ? Math.Clamp(finalSize.Height / natural, 0, 1) : 1, BindingPriority.Animation);
            }
        }
        return finalSize;
    }
}
