using Avalonia;
using Avalonia.Animation;
using Avalonia.Animation.Easings;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Primitives;
using Avalonia.Data;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Media.Transformation;
using Avalonia.Styling;
using Avalonia.VisualTree;

namespace Insiyab.Avalonia.Controls;

/// <summary>The web's <c>.ins-chip</c>: a small value a person acts on. With <see cref="IsRemovable"/> it carries the × (<c>.ins-chip-x</c>), which raises the cancelable <see cref="RemovingEvent"/> and then takes the chip away. A filter chip is a <c>ToggleButton Classes="chip"</c>, a link chip a <c>Button Classes="chip"</c>.</summary>
[TemplatePart("PART_Remove", typeof(Button))]
[PseudoClasses(":removable")]
public class Chip : ContentControl
{
    public static readonly StyledProperty<bool> IsRemovableProperty =
        AvaloniaProperty.Register<Chip, bool>(nameof(IsRemovable));

    /// <summary>The word before the chip's text in the ×'s accessible name: «إزالة الرياض».</summary>
    public static readonly StyledProperty<string> RemoveLabelProperty =
        AvaloniaProperty.Register<Chip, string>(nameof(RemoveLabel), "إزالة");

    public static readonly StyledProperty<object?> ValueProperty =
        AvaloniaProperty.Register<Chip, object?>(nameof(Value));

    /// <summary>Raised before the chip goes; set <c>Cancel</c> to keep it (<c>ins:chip-remove</c>).</summary>
    public static readonly RoutedEvent<CancelRoutedEventArgs> RemovingEvent =
        RoutedEvent.Register<Chip, CancelRoutedEventArgs>(nameof(Removing), RoutingStrategies.Bubble);

    /// <summary>Raised once the chip is on its way out.</summary>
    public static readonly RoutedEvent<RoutedEventArgs> RemovedEvent =
        RoutedEvent.Register<Chip, RoutedEventArgs>(nameof(Removed), RoutingStrategies.Bubble);

    private Button? _remove;

    static Chip()
    {
        IsRemovableProperty.Changed.AddClassHandler<Chip>((c, _) => c.PseudoClasses.Set(":removable", c.IsRemovable));
    }

    public bool IsRemovable { get => GetValue(IsRemovableProperty); set => SetValue(IsRemovableProperty, value); }
    public string RemoveLabel { get => GetValue(RemoveLabelProperty); set => SetValue(RemoveLabelProperty, value); }

    /// <summary>The value the chip stands for; its text when unset.</summary>
    public object? Value { get => GetValue(ValueProperty); set => SetValue(ValueProperty, value); }

    public event EventHandler<CancelRoutedEventArgs>? Removing { add => AddHandler(RemovingEvent, value); remove => RemoveHandler(RemovingEvent, value); }
    public event EventHandler<RoutedEventArgs>? Removed { add => AddHandler(RemovedEvent, value); remove => RemoveHandler(RemovedEvent, value); }

    /// <summary>Whether the chip has been removed; its exit may still be playing where it was.</summary>
    public bool IsLeaving { get; private set; }

    internal Button? RemoveButton => _remove;

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);
        if (_remove is not null) _remove.Click -= OnRemoveClick;
        _remove = e.NameScope.Find<Button>("PART_Remove");
        if (_remove is not null)
        {
            _remove.Click += OnRemoveClick;
            UpdateRemoveName();
        }
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == ContentProperty || change.Property == RemoveLabelProperty) UpdateRemoveName();
    }

    private void UpdateRemoveName()
    {
        if (_remove is not null) AutomationProperties.SetName(_remove, $"{RemoveLabel} {Content}");
    }

    private void OnRemoveClick(object? sender, RoutedEventArgs e)
    {
        e.Handled = true;
        Remove();
    }

    /// <summary>Takes the chip away, as its × does; false when a <see cref="Removing"/> handler kept it.</summary>
    public bool Remove()
    {
        if (IsLeaving) return false;
        var ask = new CancelRoutedEventArgs(RemovingEvent, this);
        RaiseEvent(ask);
        if (ask.Cancel) return false;

        var hadFocus = IsKeyboardFocusWithin;
        var next = Neighbour();
        IsLeaving = true;
        IsHitTestVisible = false;
        RaiseEvent(new RoutedEventArgs(RemovedEvent, this));
        if (hadFocus) next?.Focus();
        Leave();
        return true;
    }

    // The next chip's ×, else the previous one's, else the box of the field the chips are in.
    private Control? Neighbour()
    {
        if (Parent is not Panel panel) return null;
        var at = panel.Children.IndexOf(this);
        for (var i = at + 1; i < panel.Children.Count; i++)
            if (panel.Children[i] is Chip { IsLeaving: false, RemoveButton: { } b }) return b;
        for (var i = at - 1; i >= 0; i--)
            if (panel.Children[i] is Chip { IsLeaving: false, RemoveButton: { } b }) return b;
        for (var i = at + 1; i < panel.Children.Count; i++)
            if (panel.Children[i] is TextBox box) return box;
        return null;
    }

    private static readonly Easing Ease = new SplineEasing(0.22, 1, 0.36, 1);
    private static readonly Easing Spring = new SplineEasing(0.34, 1.56, 0.64, 1);

    // The chip leaves the flow at once and a picture of it plays the exit where it stood, so its neighbours glide into the room at the same time, as the web's chipLeave does.
    private void Leave()
    {
        if (Parent is not Panel panel) return;
        if (!this.IsAttachedToVisualTree() || Motion.IsStill(this) || Bounds.Width <= 0)
        {
            panel.Children.Remove(this);
            return;
        }
        var places = Places(panel, this);
        var ghost = Ghost();
        panel.Children.Remove(this);
        Glide(panel, places);
        if (ghost is { } g) _ = FadeAsync(g.Layer, g.Picture, FlowDirection == FlowDirection.RightToLeft);
    }

    // A bitmap of the chip laid in the adorner layer over its place; an Image is never mirrored and the bitmap of an RTL chip comes out unmirrored, so it is flipped back by hand.
    private (AdornerLayer Layer, Control Picture)? Ghost()
    {
        if (AdornerLayer.GetAdornerLayer(this) is not { } layer || TopLevel.GetTopLevel(this) is not { } top || this.TranslatePoint(default, layer) is not { } at) return null;
        var scale = top.RenderScaling;
        var size = Bounds.Size;
        var bitmap = Snapshot.Take(this, size, scale);
        var image = new Image
        {
            Source = bitmap,
            Width = size.Width,
            Height = size.Height,
            RenderTransformOrigin = RelativePoint.Center,
            RenderTransform = FlowDirection == FlowDirection.RightToLeft ? TransformOperations.Parse("scale(-1, 1)") : null,
        };
        var picture = new Border { Child = image, Width = size.Width, Height = size.Height, IsHitTestVisible = false, RenderTransformOrigin = RelativePoint.Center };
        Canvas.SetLeft(picture, at.X);
        Canvas.SetTop(picture, at.Y);
        layer.Children.Add(picture);
        return (layer, picture);
    }

    // The web's exit, 300ms: it falls 12px as it shrinks to .55 and turns 10deg clockwise (the sign flips in RTL, where a turn is drawn mirrored); the 6px blur is left out.
    private static async Task FadeAsync(AdornerLayer layer, Control picture, bool rtl)
    {
        TransformOperationsAnimator.Ensure();
        var turn = rtl ? -10 : 10;
        var leave = new Animation
        {
            Duration = TimeSpan.FromMilliseconds(300),
            Easing = new SplineEasing(0.4, 0, 1, 1),
            FillMode = FillMode.Forward,
            Children =
            {
                new KeyFrame { Cue = new Cue(0), Setters = { new Setter(OpacityProperty, 1d), new Setter(RenderTransformProperty, TransformOperations.Parse("rotate(0deg) scale(1) translateY(0px)")) } },
                new KeyFrame { Cue = new Cue(1), Setters = { new Setter(OpacityProperty, 0d), new Setter(RenderTransformProperty, TransformOperations.Parse(FormattableString.Invariant($"rotate({turn}deg) scale(0.55) translateY(12px)"))) } },
            },
        };
        try { await leave.RunAsync(picture, CancellationToken.None); }
        finally { layer.Children.Remove(picture); }
    }

    /// <summary>The arrival a chip makes when it is added: it drops in, grows and straightens.</summary>
    public void PlayEnter()
    {
        if (Motion.IsStill(this)) return;
        TransformOperationsAnimator.Ensure();
        var turn = FlowDirection == FlowDirection.RightToLeft ? 8 : -8;
        var enter = new Animation
        {
            Duration = TimeSpan.FromMilliseconds(460),
            Easing = Spring,
            FillMode = FillMode.Backward,
            Children =
            {
                new KeyFrame { Cue = new Cue(0), Setters = { new Setter(RenderTransformProperty, TransformOperations.Parse(FormattableString.Invariant($"rotate({turn}deg) scale(0.4) translateY(-10px)"))) } },
                new KeyFrame { Cue = new Cue(1), Setters = { new Setter(RenderTransformProperty, TransformOperations.Parse("rotate(0deg) scale(1) translateY(0px)")) } },
            },
        };
        // The spring overshoots past 1 and an opacity above 1 wraps to almost clear, so the fade takes the plain curve.
        var appear = new Animation
        {
            Duration = TimeSpan.FromMilliseconds(460),
            Easing = Ease,
            FillMode = FillMode.Backward,
            Children =
            {
                new KeyFrame { Cue = new Cue(0), Setters = { new Setter(OpacityProperty, 0d) } },
                new KeyFrame { Cue = new Cue(1), Setters = { new Setter(OpacityProperty, 1d) } },
            },
        };
        _ = enter.RunAsync(this, CancellationToken.None);
        _ = appear.RunAsync(this, CancellationToken.None);
    }

    /// <summary>Puts <paramref name="chip"/> into <paramref name="panel"/> at <paramref name="index"/> as the web's chipEnter does: its neighbours glide aside and it drops in.</summary>
    internal static void Arrive(Panel panel, int index, Chip chip)
    {
        var moving = panel.IsAttachedToVisualTree() && !Motion.IsStill(panel);
        var places = moving ? Places(panel, null) : [];
        panel.Children.Insert(Math.Clamp(index, 0, panel.Children.Count), chip);
        if (!moving) return;
        Glide(panel, places);
        chip.PlayEnter();
    }

    // Where each visible child of the panel is drawn now, a glide in progress included.
    private static List<(Control Child, Point At)> Places(Panel panel, Control? except)
    {
        var places = new List<(Control, Point)>();
        foreach (var child in panel.Children)
            if (child != except && child.IsVisible && child.TranslatePoint(default, panel) is { } at) places.Add((child, at));
        return places;
    }

    // Once the panel has laid itself out again, each child starts where it was drawn and glides to its new place: 360ms on the brand curve.
    private static void Glide(Panel panel, List<(Control Child, Point At)> places)
    {
        if (places.Count == 0) return;
        void Laid(object? sender, EventArgs e)
        {
            panel.LayoutUpdated -= Laid;
            foreach (var (child, at) in places)
            {
                if (child.Parent != panel) continue;
                var dx = at.X - child.Bounds.X;
                var dy = at.Y - child.Bounds.Y;
                if (Math.Abs(dx) >= 0.5 || Math.Abs(dy) >= 0.5) Slide.Start(child, dx, dy);
            }
        }
        panel.LayoutUpdated += Laid;
    }

    // One child's glide, laid over its own transform at animation priority for its duration only, so its styles are never touched.
    private sealed class Slide
    {
        private static readonly System.Runtime.CompilerServices.ConditionalWeakTable<Control, Slide> Running = new();
        private static readonly System.Diagnostics.Stopwatch Clock = System.Diagnostics.Stopwatch.StartNew();
        private readonly Control _child;
        private readonly double _dx, _dy, _start;
        private IDisposable? _value;
        private bool _done;

        private Slide(Control child, double dx, double dy)
        {
            _child = child;
            _dx = dx;
            _dy = dy;
            _start = Clock.Elapsed.TotalMilliseconds;
        }

        public static void Start(Control child, double dx, double dy)
        {
            if (Running.TryGetValue(child, out var old)) old.Stop();
            var slide = new Slide(child, dx, dy);
            Running.AddOrUpdate(child, slide);
            slide.Step();
        }

        private void Step()
        {
            if (_done) return;
            var t = Math.Clamp((Clock.Elapsed.TotalMilliseconds - _start) / 360, 0, 1);
            if (t >= 1 || TopLevel.GetTopLevel(_child) is not { } top)
            {
                Stop();
                return;
            }
            var k = 1 - Ease.Ease(t);
            var shift = TransformOperations.CreateBuilder(1);
            shift.AppendTranslate(_dx * k, _dy * k);
            _value?.Dispose();
            _value = _child.SetValue(RenderTransformProperty, shift.Build(), BindingPriority.Animation);
            top.RequestAnimationFrame(_ => Step());
        }

        private void Stop()
        {
            _done = true;
            _value?.Dispose();
            _value = null;
            if (Running.TryGetValue(_child, out var current) && current == this) Running.Remove(_child);
        }
    }
}
