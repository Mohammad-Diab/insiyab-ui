using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Primitives;

namespace Insiyab.Avalonia.Controls;

/// <summary>An icon (its content) with work running for it: <see cref="Value"/> 0 to 100 shrinks it inside a thin ring that fills and keeps turning while the work runs, as the Play Store's install ring does (its arc easing to each new value, never shorter than a short arc; a still window: the fill from twelve, not turning), <see cref="IsIndeterminate"/> turns an arc there, <c>NaN</c> shows the icon alone; the ring takes no room.</summary>
[TemplatePart("PART_Ring", typeof(ProgressRing))]
[PseudoClasses(":progress", ":waiting", ":busy")]
public class ProgressIcon : ContentControl
{
    /// <summary>How far the work has come, 0 to 100; <c>NaN</c> (the default) shows no ring.</summary>
    public static readonly StyledProperty<double> ValueProperty =
        AvaloniaProperty.Register<ProgressIcon, double>(nameof(Value), double.NaN);

    /// <summary>The work waits to start, its share not known: a turning arc in the ring's place, whatever <see cref="Value"/> says.</summary>
    public static readonly StyledProperty<bool> IsIndeterminateProperty =
        AvaloniaProperty.Register<ProgressIcon, bool>(nameof(IsIndeterminate));

    private ProgressRing? _ring;
    private readonly TurningFill _filling = new();
    private long _run;          // the ring's frames: a newer run (or none) stops them
    private long _turnRun = -1; // the run of the frames now turning the ring
    private bool _shown;        // the ring showed at the last fill

    /// <summary>How far the work has come, 0 to 100 (clamped); <c>NaN</c> (the default) shows no ring.</summary>
    public double Value { get => GetValue(ValueProperty); set => SetValue(ValueProperty, value); }

    /// <summary>The work waits to start: a turning arc in the ring's place.</summary>
    public bool IsIndeterminate { get => GetValue(IsIndeterminateProperty); set => SetValue(IsIndeterminateProperty, value); }

    /// <summary>Whether the ring with a fill shows: not waiting, and <see cref="Value"/> is a number.</summary>
    internal bool ShowsRing => !IsIndeterminate && double.IsFinite(Value);

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);
        _ring = e.NameScope.Find<ProgressRing>("PART_Ring");
        Fill();
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == ValueProperty || change.Property == IsIndeterminateProperty) Fill();
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        Draw();
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        _run++;
    }

    // The ring takes a number only: on NaN it keeps its last fill, so it fades out as it was (turning on until it is gone). Each time it shows again it starts from a short arc.
    private void Fill()
    {
        var ring = ShowsRing;
        PseudoClasses.Set(":progress", ring);
        PseudoClasses.Set(":waiting", IsIndeterminate);
        PseudoClasses.Set(":busy", ring || IsIndeterminate);
        if (ring && !_shown) _filling.Clear();
        _shown = ring;
        if (double.IsFinite(Value)) _filling.Aim(Math.Clamp(Value, 0, 100) / 100);
        Draw();
    }

    // The ring's fill and turn (TurningFill): frames while it shows or fades; a still window or no window draws the fill at once from twelve.
    private void Draw()
    {
        if (_ring is not { } ring || !_filling.IsStarted) return;
        if (Motion.IsStill(this) || TopLevel.GetTopLevel(this) is not { } top)
        {
            Still(ring);
            return;
        }
        if (_turnRun == _run) return;
        var run = _turnRun = ++_run;
        _filling.Pause();
        void Frame(TimeSpan now)
        {
            if (run != _run) return;
            if (Motion.IsStill(this))
            {
                Still(ring);
                return;
            }
            _filling.Step(now);
            Show(ring);
            if (ShowsRing || _filling.IsEasing || ring.Opacity > 0.001) top.RequestAnimationFrame(Frame);
            else _run++;
        }
        top.RequestAnimationFrame(Frame);
    }

    private void Still(ProgressRing ring)
    {
        _run++;
        _filling.Settle(0);
        Show(ring);
    }

    private void Show(ProgressRing ring)
    {
        ring.Value = _filling.Length * 100;
        ring.Turn = _filling.Turn * 360;
    }
}
