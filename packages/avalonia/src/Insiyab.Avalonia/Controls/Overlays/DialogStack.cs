using System.Runtime.CompilerServices;
using Avalonia;
using Avalonia.Animation;
using Avalonia.Animation.Easings;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.Transformation;
using Avalonia.Styling;
using Avalonia.Threading;
using Avalonia.VisualTree;

namespace Insiyab.Avalonia.Controls;

/// <summary>The window's modal stack, the top layer the web's <c>showModal()</c> gives a dialog. It lives in the window's adorner layer, under the toasts: each open dialog sits on its own scrim, only the top scrim is tinted, and Escape, an outside press and focus return go to the top one.</summary>
internal sealed class DialogStack : Panel
{
    private static readonly ConditionalWeakTable<TopLevel, DialogStack> Stacks = new();

    private readonly TopLevel _top;
    private readonly List<DialogLayer> _layers = [];
    private Control? _blurred;
    private BlurEffect? _blur;
    private long _blurRun;

    private DialogStack(TopLevel top)
    {
        _top = top;
        ZIndex = 0;
        top.AddHandler(KeyDownEvent, OnKeyDown, RoutingStrategies.Bubble);
        top.BackRequested += OnBack;
    }

    internal IReadOnlyList<DialogLayer> Layers => _layers;

    internal static DialogStack? Of(Visual visual) =>
        OverlayHost.Top(visual) is { } top && Stacks.TryGetValue(top, out var stack) ? stack : null;

    internal bool IsTop(Dialog dialog) => _layers.Count > 0 && _layers[^1].Dialog == dialog;

    internal static bool Open(Dialog dialog, Control anchor)
    {
        var top = OverlayHost.Top(anchor);
        var adorners = top is null ? null : OverlayHost.Layer(top);
        if (top is null || adorners is null) return false;
        if (!Stacks.TryGetValue(top, out var stack))
        {
            stack = new DialogStack(top);
            Stacks.Add(top, stack);
        }
        if (stack.Parent is null)
        {
            adorners.Children.Add(stack);
            AdornerLayer.SetAdornedElement(stack, adorners);
        }
        stack.Push(dialog, anchor);
        return true;
    }

    internal static void Close(Dialog dialog)
    {
        if (Of(dialog) is { } stack) stack.Pop(dialog);
    }

    // A temporary pane that becomes pinned leaves the stack at once, with no exit.
    internal static void Drop(Dialog dialog)
    {
        if (Of(dialog) is not { } stack) return;
        var at = stack._layers.FindIndex(l => l.Dialog == dialog);
        if (at >= 0)
        {
            stack._layers.RemoveAt(at);
            if (stack._layers.Count > 0) stack._layers[^1].Cover(false);
            else stack.Blur(false);
        }
        foreach (var gone in stack.Children.OfType<DialogLayer>().Where(l => l.Dialog == dialog).ToList())
        {
            gone.Release();
            stack.Children.Remove(gone);
        }
    }

    private void Push(Dialog dialog, Control anchor)
    {
        foreach (var leaving in Children.OfType<DialogLayer>().Where(l => l.Dialog == dialog).ToList())
        {
            leaving.Release();
            Children.Remove(leaving);
        }
        var from = anchor.Focusable ? anchor : _top.FocusManager?.GetFocusedElement() as Control ?? anchor;
        foreach (var below in _layers) below.Cover(true);
        var layer = new DialogLayer(this, dialog, from)
        {
            FlowDirection = anchor.FlowDirection,
            RequestedThemeVariant = anchor.ActualThemeVariant,
        };
        _layers.Add(layer);
        Children.Add(layer);
        layer.Enter();
        if (_layers.Count == 1) Blur(true);
        Dispatcher.UIThread.Post(() => layer.FocusFirst(), DispatcherPriority.Loaded);
    }

    private void Pop(Dialog dialog)
    {
        var at = _layers.FindIndex(l => l.Dialog == dialog);
        if (at < 0) return;
        var layer = _layers[at];
        _layers.RemoveAt(at);
        if (_layers.Count > 0) _layers[^1].Cover(false);
        else Blur(false);
        layer.Leave(() => Children.Remove(layer));
        var focused = _top.FocusManager?.GetFocusedElement() as Visual;
        if (focused is null || focused == _top || dialog.IsVisualAncestorOf(focused) || focused == dialog)
            layer.From?.Focus();
    }

    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Handled || e.Key != Key.Escape || _layers.Count == 0) return;
        _layers[^1].Dialog.RequestClose(outside: false);
        e.Handled = true;
    }

    private void OnBack(object? sender, RoutedEventArgs e)
    {
        if (e.Handled || _layers.Count == 0) return;
        _layers[^1].Dialog.RequestClose(outside: false);
        e.Handled = true;
    }

    // The page behind goes out of focus, as the web's backdrop-filter: blur(3px), fading with the scrim; skipped when the content already has an effect.
    private void Blur(bool on)
    {
        if (on)
        {
            if (_blurred is not null) return;
            var content = _top.GetVisualDescendants().OfType<VisualLayerManager>().FirstOrDefault()?.Child;
            if (content is null || content.Effect is not null) return;
            _blurred = content;
            _blur = new BlurEffect { Radius = 0 };
            content.Effect = _blur;
            Tween(0, 3, Motion.Enter(this));
        }
        else if (_blur is not null)
        {
            Tween(_blur.Radius, 0, Motion.Exit(this), () =>
            {
                if (_blurred is not null && _blurred.Effect == _blur) _blurred.Effect = null;
                _blurred = null;
                _blur = null;
            });
        }
    }

    private void Tween(double from, double to, TimeSpan duration, Action? done = null)
    {
        var blur = _blur;
        if (blur is null) return;
        if (!Seen.WindowShown(_top))
        {
            blur.Radius = to;
            done?.Invoke();
            return;
        }
        var run = ++_blurRun;
        var easing = Motion.Ease(this);
        TimeSpan? start = null;
        void Frame(TimeSpan now)
        {
            if (run != _blurRun) return;
            start ??= now;
            var t = duration <= TimeSpan.Zero ? 1 : Math.Clamp((now - start.Value).TotalMilliseconds / duration.TotalMilliseconds, 0, 1);
            blur.Radius = from + (to - from) * easing.Ease(t);
            if (t < 1) _top.RequestAnimationFrame(Frame);
            else done?.Invoke();
        }
        _top.RequestAnimationFrame(Frame);
    }
}

/// <summary>One open dialog: its scrim and the dialog placed on it, centred, or against an edge for a drawer.</summary>
internal sealed class DialogLayer : ThemeVariantScope
{
    private const double PhoneGutter = 16;
    private readonly DialogStack _stack;
    private readonly Border _scrim;
    private readonly LayerPanel _panel;
    private bool _pressed;

    public DialogLayer(DialogStack stack, Dialog dialog, Control? from)
    {
        _stack = stack;
        Dialog = dialog;
        From = from;
        _scrim = new Border { Classes = { "dialog-scrim" }, Opacity = 0 };
        _scrim.PointerPressed += (_, e) => _pressed = e.GetCurrentPoint(_scrim).Properties.IsLeftButtonPressed;
        _scrim.PointerReleased += (_, e) =>
        {
            if (!_pressed) return;
            _pressed = false;
            if (_scrim.IsHitTestVisible) Dialog.RequestClose(outside: true);
        };
        _panel = new LayerPanel(dialog) { Children = { _scrim, dialog } };
        Child = _panel;
    }

    public Dialog Dialog { get; }
    public Control? From { get; }
    public Border Scrim => _scrim;

    // A modal under another gives up its tint and its input: the page is dimmed once, by the top one.
    public void Cover(bool covered)
    {
        _scrim.IsHitTestVisible = !covered;
        Dialog.IsHitTestVisible = !covered;
        Fade(_scrim, covered ? 0 : 1, covered ? Motion.Exit(this) : Motion.Enter(this));
    }

    public void Enter()
    {
        Fade(_scrim, 1, Motion.Enter(this));
        Move(Dialog, true);
    }

    // A dialog reopened while its exit still plays leaves this layer at once.
    public void Release() => _panel.Children.Remove(Dialog);

    public void Leave(Action removed)
    {
        _scrim.IsHitTestVisible = false;
        Dialog.IsHitTestVisible = false;
        Fade(_scrim, 0, Motion.Exit(this));
        Move(Dialog, false);
        DispatcherTimer.RunOnce(() => { if (Dialog.Parent == _panel && !Dialog.IsOpen) Release(); removed(); }, Motion.Exit(this) + TimeSpan.FromMilliseconds(40));
    }

    public void FocusFirst()
    {
        if (!Dialog.IsOpen) return;
        var focused = TopLevel.GetTopLevel(this)?.FocusManager?.GetFocusedElement() as Visual;
        if (focused is not null && (focused == Dialog || Dialog.IsVisualAncestorOf(focused))) return;
        if (Dialog.FocusTarget() is { } target) target.Focus(NavigationMethod.Tab);
        else Dialog.Focus();
    }

    private static void Fade(Control control, double to, TimeSpan duration)
    {
        control.Transitions = [new DoubleTransition { Property = OpacityProperty, Duration = duration, Easing = Motion.Ease(control) }];
        control.Opacity = to;
    }

    // The entrance from the web's @starting-style: the dialog rises 10px and grows from 98%; a drawer slides in from its edge.
    private void Move(Dialog dialog, bool inward)
    {
        var drawer = dialog.IsDrawer;
        var duration = inward ? (drawer ? TimeSpan.FromMilliseconds(300) : Motion.Enter(this)) : Motion.Exit(this);
        var away = drawer ? DrawerAway(dialog) : TransformOperations.Parse("translateY(10px) scale(0.98)");
        var easing = Motion.Ease(this);
        if (inward)
        {
            dialog.Transitions = null;
            dialog.RenderTransform = away;
            if (!drawer) dialog.Opacity = 0;
        }
        var transitions = new Transitions { new TransformOperationsTransition { Property = RenderTransformProperty, Duration = duration, Easing = easing } };
        if (!drawer) transitions.Add(new DoubleTransition { Property = OpacityProperty, Duration = duration, Easing = easing });
        dialog.Transitions = transitions;
        dialog.RenderTransform = inward ? TransformOperations.Identity : away;
        if (!drawer) dialog.Opacity = inward ? 1 : 0;
    }

    // A transform has no logical form on the web; here the layer's own mirroring turns "toward the start" into the right side in RTL.
    private TransformOperations DrawerAway(Dialog dialog)
    {
        var size = _panel.SizeFor(Bounds.Size.Width > 0 ? Bounds.Size : TopLevel.GetTopLevel(_stack)?.ClientSize ?? default);
        if (dialog.Classes.Contains("bottom")) return TransformOperations.Parse(FormattableString.Invariant($"translateY({size.Height + 2}px)"));
        var x = size.Width + 2;
        return TransformOperations.Parse(FormattableString.Invariant($"translateX({(dialog.Classes.Contains("end") ? x : -x)}px)"));
    }

    private sealed class LayerPanel(Dialog dialog) : Panel
    {
        public Size SizeFor(Size room)
        {
            dialog.Measure(Slot(room, dialog));
            return Place(room, dialog).Size;
        }

        protected override Size MeasureOverride(Size availableSize)
        {
            var room = new Size(double.IsInfinity(availableSize.Width) ? 1024 : availableSize.Width, double.IsInfinity(availableSize.Height) ? 768 : availableSize.Height);
            foreach (var child in Children)
            {
                if (child == dialog) dialog.Measure(Slot(room, dialog));
                else child.Measure(room);
            }
            return room;
        }

        protected override Size ArrangeOverride(Size finalSize)
        {
            foreach (var child in Children)
            {
                if (child == dialog) dialog.Arrange(Place(finalSize, dialog));
                else child.Arrange(new Rect(finalSize));
            }
            return finalSize;
        }

        // The room the web's sizes leave: min(width, 100vw - 2rem) by min(max-height, 100vh - 3rem) for a dialog; min(24rem, 88vw) by the full height for a drawer.
        private static Size Slot(Size room, Dialog d)
        {
            if (d.IsDrawer)
            {
                if (d.Classes.Contains("bottom")) return new Size(room.Width, Math.Min(d.MaxHeight, room.Height * 0.85));
                return new Size(Math.Min(d.MaxWidth, room.Width * 0.88), room.Height);
            }
            return new Size(Math.Max(0, Math.Min(d.MaxWidth, room.Width - 2 * PhoneGutter)), Math.Max(0, Math.Min(d.MaxHeight, room.Height - 48)));
        }

        private static Rect Place(Size room, Dialog d)
        {
            var slot = Slot(room, d);
            if (d.IsDrawer)
            {
                if (d.Classes.Contains("bottom"))
                {
                    var h = Math.Min(slot.Height, d.DesiredSize.Height);
                    return new Rect(0, room.Height - h, room.Width, h);
                }
                var x = d.Classes.Contains("end") ? room.Width - slot.Width : 0;
                return new Rect(x, 0, slot.Width, room.Height);
            }
            var height = Math.Min(slot.Height, d.DesiredSize.Height);
            return new Rect((room.Width - slot.Width) / 2, (room.Height - height) / 2, slot.Width, height);
        }
    }
}
