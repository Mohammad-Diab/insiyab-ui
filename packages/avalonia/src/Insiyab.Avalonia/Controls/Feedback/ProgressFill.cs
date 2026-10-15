using Avalonia;
using Avalonia.Animation;
using Avalonia.Animation.Easings;
using Avalonia.Controls;

namespace Insiyab.Avalonia.Controls;

/// <summary>The ProgressBar's fill. It takes its first width at once and glides to every later one, as the web's bar transitions its value but not its first paint.</summary>
public class ProgressFill : Border
{
    private readonly Transitions _glide = [new DoubleTransition { Property = WidthProperty, Duration = TimeSpan.FromMilliseconds(280), Easing = new SplineEasing(0.22, 1, 0.36, 1) }];
    private bool _armed;
    private bool _seen;
    private IDisposable? _watch;

    protected override Type StyleKeyOverride => typeof(TemplateBorder);

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        _watch ??= Seen.Watch(this, on =>
        {
            _seen = on;
            Transitions = on && _armed ? _glide : null;
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
        if (_armed || change.Property != WidthProperty || double.IsNaN(change.GetNewValue<double>())) return;
        if ((Parent as Control)?.Bounds.Width is not > 0) return;
        _armed = true;
        if (_seen) Transitions = _glide;
    }
}
