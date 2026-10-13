using Avalonia;
using Avalonia.Animation;
using Avalonia.Animation.Easings;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Styling;

namespace Insiyab.Avalonia.Controls;

/// <summary>The notched alert, the web's <c>.ins-alert</c>: a solid circle with the icon nested in a bite cut from the tinted message. Tone classes <c>ok info warn bad</c>.</summary>
[TemplatePart("PART_Message", typeof(Border))]
[TemplatePart("PART_Close", typeof(Button))]
public class Alert : ContentControl
{
    // The bite is 1.65rem, a little more than half the 2.75rem circle, which leaves the hairline gap.
    private const double Bite = 26.4;

    public static readonly StyledProperty<Geometry?> IconProperty =
        AvaloniaProperty.Register<Alert, Geometry?>(nameof(Icon));

    public static readonly StyledProperty<IBrush?> IconBackgroundProperty =
        AvaloniaProperty.Register<Alert, IBrush?>(nameof(IconBackground));

    public static readonly StyledProperty<IBrush?> IconForegroundProperty =
        AvaloniaProperty.Register<Alert, IBrush?>(nameof(IconForeground));

    public static readonly StyledProperty<bool> IsDismissibleProperty =
        AvaloniaProperty.Register<Alert, bool>(nameof(IsDismissible));

    public static readonly StyledProperty<string> CloseLabelProperty =
        AvaloniaProperty.Register<Alert, string>(nameof(CloseLabel), "إغلاق التنبيه");

    /// <summary>Raised once the alert has been dismissed and hidden, the web's <c>ins:dismiss</c>.</summary>
    public static readonly RoutedEvent<RoutedEventArgs> DismissedEvent =
        RoutedEvent.Register<Alert, RoutedEventArgs>(nameof(Dismissed), RoutingStrategies.Bubble);

    private Border? _message;
    private Button? _close;
    private bool _going;

    public Geometry? Icon { get => GetValue(IconProperty); set => SetValue(IconProperty, value); }

    /// <summary>The circle's solid fill, the tone's saturated hue.</summary>
    public IBrush? IconBackground { get => GetValue(IconBackgroundProperty); set => SetValue(IconBackgroundProperty, value); }

    /// <summary>The glyph's colour on the circle.</summary>
    public IBrush? IconForeground { get => GetValue(IconForegroundProperty); set => SetValue(IconForegroundProperty, value); }

    /// <summary>Shows the bare close button that dismisses the alert, the web's <c>ins-close</c> with <c>data-ins-dismiss</c>.</summary>
    public bool IsDismissible { get => GetValue(IsDismissibleProperty); set => SetValue(IsDismissibleProperty, value); }

    /// <summary>The close button's accessible name.</summary>
    public string CloseLabel { get => GetValue(CloseLabelProperty); set => SetValue(CloseLabelProperty, value); }

    public event EventHandler<RoutedEventArgs>? Dismissed { add => AddHandler(DismissedEvent, value); remove => RemoveHandler(DismissedEvent, value); }

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);
        if (_message is not null) _message.SizeChanged -= OnMessageSized;
        if (_close is not null) _close.Click -= OnCloseClick;
        _message = e.NameScope.Find<Border>("PART_Message");
        _close = e.NameScope.Find<Button>("PART_Close");
        if (_message is not null) _message.SizeChanged += OnMessageSized;
        if (_close is not null) _close.Click += OnCloseClick;
    }

    // It arrives from the leading edge: an interruption that slides in reads as one.
    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        Entrance.Play(this, EntranceEffect.Slide);
    }

    private void OnCloseClick(object? sender, RoutedEventArgs e) => Dismiss();

    // The bite is centred on the message's start edge, where the circle's centre sits; RTL mirrors it with the rest of the layout.
    private void OnMessageSized(object? sender, SizeChangedEventArgs e)
    {
        if (_message is null) return;
        var size = e.NewSize;
        _message.Clip = new CombinedGeometry(GeometryCombineMode.Exclude,
            new RectangleGeometry(new Rect(size)),
            new EllipseGeometry(new Rect(-Bite, size.Height / 2 - Bite, Bite * 2, Bite * 2)));
    }

    /// <summary>Fades the alert, closes its space and hides it; <c>IsVisible = true</c> brings it back.</summary>
    public async void Dismiss()
    {
        if (_going || !IsVisible) return;
        _going = true;
        var height = Bounds.Height;
        var exit = this.TryFindResource("InsTExit", ActualThemeVariant, out var t) && t is TimeSpan span ? span : TimeSpan.FromMilliseconds(180);
        var ease = this.TryFindResource("InsEase", ActualThemeVariant, out var e) && e is Easing found ? found : new SplineEasing(0.22, 1, 0.36, 1);
        if (height > 0 && IsEffectivelyVisible)
        {
            var animation = new Animation
            {
                Duration = exit * 2,
                Easing = ease,
                Children =
                {
                    new KeyFrame { Cue = new Cue(0), Setters = { new Setter(OpacityProperty, 1d), new Setter(HeightProperty, height) } },
                    new KeyFrame { Cue = new Cue(0.45), Setters = { new Setter(OpacityProperty, 0d), new Setter(HeightProperty, height) } },
                    new KeyFrame { Cue = new Cue(1), Setters = { new Setter(OpacityProperty, 0d), new Setter(HeightProperty, 0d) } },
                },
            };
            SetCurrentValue(OpacityProperty, 0d);
            SetCurrentValue(HeightProperty, 0d);
            await animation.RunAsync(this);
        }
        SetCurrentValue(IsVisibleProperty, false);
        ClearValue(OpacityProperty);
        ClearValue(HeightProperty);
        _going = false;
        RaiseEvent(new RoutedEventArgs(DismissedEvent, this));
    }
}
