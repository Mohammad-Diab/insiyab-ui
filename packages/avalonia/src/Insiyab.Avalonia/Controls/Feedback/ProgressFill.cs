using Avalonia;
using Avalonia.Animation;
using Avalonia.Animation.Easings;
using Avalonia.Controls;
using Avalonia.Threading;

namespace Insiyab.Avalonia.Controls;

/// <summary>The ProgressBar's fill. It takes its first width at once and glides to every later one, as the web's bar transitions its value but not its first paint; on a bar with the class <c>grow</c> the first width glides in from empty once the bar is seen, and is set at once in a still window.</summary>
internal class ProgressFill : Border
{
    private static readonly TimeSpan Glide = TimeSpan.FromMilliseconds(280);
    private readonly Transitions _glide = [new DoubleTransition { Property = WidthProperty, Duration = Glide, Easing = new SplineEasing(0.22, 1, 0.36, 1) }];
    private bool _armed;
    private bool _seen;
    private bool _holding;
    private bool _revealing;
    private IDisposable? _watch;

    protected override Type StyleKeyOverride => typeof(TemplateBorder);

    private bool Grows => TemplatedParent is ProgressBar bar && bar.Classes.Contains("grow");

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        // The track is watched: a fill held empty has no size of its own to be seen by.
        _watch ??= Seen.Watch(Parent as Control ?? this, on =>
        {
            _seen = on;
            if (on && _holding) Later();
            else if (!_revealing) Transitions = on && _armed ? _glide : null;
        });
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        _watch?.Dispose();
        _watch = null;
        _seen = false;
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property != WidthProperty || double.IsNaN(change.GetNewValue<double>())) return;
        // The bar sets the width on every layout, so a grow bar is held empty through MaxWidth, which the bar leaves alone; while it is revealed the cap follows the bar.
        if (_revealing) MaxWidth = change.GetNewValue<double>();
        if (_armed || _holding) return;
        if (Grows && change.GetNewValue<double>() > 0 && !Motion.IsStill(this))
        {
            _holding = true;
            MaxWidth = 0;
            if (_seen) Later();
            return;
        }
        if ((Parent as Control)?.Bounds.Width is not > 0) return;
        _armed = true;
        if (_seen) Transitions = _glide;
    }

    // After this layout pass, so the fill has taken its empty width first, and only if it is in sight once laid out.
    private void Later() => Dispatcher.UIThread.Post(() =>
    {
        if (_holding && _seen && Seen.Now(Parent as Control ?? this)) Reveal();
    }, DispatcherPriority.Background);

    private void Reveal()
    {
        _holding = false;
        _armed = true;
        if (Motion.IsStill(this))
        {
            ClearValue(MaxWidthProperty);
            Transitions = _glide;
            return;
        }
        _revealing = true;
        Transitions = [new DoubleTransition { Property = MaxWidthProperty, Duration = Glide, Easing = new SplineEasing(0.22, 1, 0.36, 1) }];
        MaxWidth = Width;
        DispatcherTimer.RunOnce(() =>
        {
            _revealing = false;
            Transitions = null;
            ClearValue(MaxWidthProperty);
            Transitions = _seen ? _glide : null;
        }, Glide + TimeSpan.FromMilliseconds(40));
    }
}
