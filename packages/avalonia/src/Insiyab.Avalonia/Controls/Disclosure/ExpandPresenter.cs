using Avalonia;
using Avalonia.Animation;
using Avalonia.Animation.Easings;
using Avalonia.Controls;

namespace Insiyab.Avalonia.Controls;

/// <summary>Shows its child at a height that grows from nothing when <see cref="IsOpen"/> turns on and shrinks back when it turns off: the web's <c>::details-content</c> block-size transition.</summary>
internal class ExpandPresenter : Decorator
{
    public static readonly StyledProperty<bool> IsOpenProperty =
        AvaloniaProperty.Register<ExpandPresenter, bool>(nameof(IsOpen));

    public static readonly StyledProperty<double> ProgressProperty =
        AvaloniaProperty.Register<ExpandPresenter, double>(nameof(Progress));

    public static readonly StyledProperty<TimeSpan> OpenDurationProperty =
        AvaloniaProperty.Register<ExpandPresenter, TimeSpan>(nameof(OpenDuration), TimeSpan.FromMilliseconds(280));

    public static readonly StyledProperty<TimeSpan> CloseDurationProperty =
        AvaloniaProperty.Register<ExpandPresenter, TimeSpan>(nameof(CloseDuration), TimeSpan.FromMilliseconds(180));

    public static readonly StyledProperty<Easing> EasingProperty =
        AvaloniaProperty.Register<ExpandPresenter, Easing>(nameof(Easing), new SplineEasing(0.22, 1, 0.36, 1));

    private DoubleTransition? _transition;
    private bool _laidOut;

    static ExpandPresenter()
    {
        ClipToBoundsProperty.OverrideDefaultValue<ExpandPresenter>(true);
        AffectsMeasure<ExpandPresenter>(ProgressProperty);
    }

    public bool IsOpen { get => GetValue(IsOpenProperty); set => SetValue(IsOpenProperty, value); }

    /// <summary>How much of the child shows, 0 to 1; animated by <see cref="IsOpen"/>.</summary>
    public double Progress { get => GetValue(ProgressProperty); set => SetValue(ProgressProperty, value); }

    public TimeSpan OpenDuration { get => GetValue(OpenDurationProperty); set => SetValue(OpenDurationProperty, value); }

    public TimeSpan CloseDuration { get => GetValue(CloseDurationProperty); set => SetValue(CloseDurationProperty, value); }

    public Easing Easing { get => GetValue(EasingProperty); set => SetValue(EasingProperty, value); }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == IsOpenProperty)
        {
            var open = change.GetNewValue<bool>();
            if (Child is { } child && open) child.IsVisible = true;
            if (_laidOut)
            {
                _transition ??= new DoubleTransition { Property = ProgressProperty };
                _transition.Duration = open ? OpenDuration : CloseDuration;
                _transition.Easing = Easing;
                Transitions ??= [_transition];
            }
            SetValue(ProgressProperty, open ? 1d : 0d);
        }
        else if (change.Property == ProgressProperty)
        {
            if (Child is { } child) child.IsVisible = IsOpen || change.GetNewValue<double>() > 0;
        }
        else if (change.Property == ChildProperty && change.GetNewValue<Control?>() is { } added)
        {
            added.IsVisible = IsOpen || Progress > 0;
        }
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        if (Child is not { } child) return default;
        child.Measure(availableSize.WithHeight(double.PositiveInfinity));
        var p = Math.Clamp(Progress, 0, 1);
        return new Size(child.DesiredSize.Width, child.DesiredSize.Height * p);
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        _laidOut = true;
        Child?.Arrange(new Rect(0, 0, finalSize.Width, Child.DesiredSize.Height));
        return finalSize;
    }
}
