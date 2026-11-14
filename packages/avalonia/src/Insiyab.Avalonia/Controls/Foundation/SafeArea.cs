using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Data;
using Avalonia.Layout;
using Avalonia.Media;

namespace Insiyab.Avalonia.Controls;

/// <summary>The edges <see cref="SafeArea.EdgesProperty"/> pads, on the screen whatever the reading direction, as the bars and cutouts are; <see cref="Keyboard"/> also clears the on-screen keyboard at the bottom.</summary>
[Flags]
public enum SafeAreaEdges
{
    None = 0,
    Left = 1,
    Top = 2,
    Right = 4,
    Bottom = 8,
    All = Left | Top | Right | Bottom,

    /// <summary>While the on-screen keyboard is open, the bottom padding is at least the part of the element it covers.</summary>
    Keyboard = 16,
}

/// <summary>Pads an element by the safe area, the room the system bars, cutouts and gesture bar take on a phone, so its content stays clear of them while its background runs edge to edge. Nothing changes on a desktop window.</summary>
public static class SafeArea
{
    /// <summary>On any control: the edges to pad by the safe area, added to its own padding (a Border, a Decorator, a templated control, a TextBlock) or else its margin; follows rotation and the keyboard live.</summary>
    public static readonly AttachedProperty<SafeAreaEdges> EdgesProperty =
        AvaloniaProperty.RegisterAttached<Control, SafeAreaEdges>("Edges", typeof(SafeArea));

    private static readonly AttachedProperty<Tracker?> TrackerProperty =
        AvaloniaProperty.RegisterAttached<Control, Tracker?>("Tracker", typeof(SafeArea));

    static SafeArea()
    {
        EdgesProperty.Changed.AddClassHandler<Control>((control, e) => Watch(control, e.GetNewValue<SafeAreaEdges>()));
    }

    public static SafeAreaEdges GetEdges(Control control) => control.GetValue(EdgesProperty);
    public static void SetEdges(Control control, SafeAreaEdges value) => control.SetValue(EdgesProperty, value);

    /// <summary>The safe area of the TopLevel the visual is in: zero on a desktop window or outside a TopLevel.</summary>
    public static Thickness Of(Visual visual) => Insets.Of(visual)?.SafeArea ?? default;

    private static void Watch(Control control, SafeAreaEdges edges)
    {
        var tracker = control.GetValue(TrackerProperty);
        if (edges == SafeAreaEdges.None)
        {
            tracker?.Stop();
            control.ClearValue(TrackerProperty);
            return;
        }
        if (tracker is null)
        {
            tracker = new Tracker(control);
            control.SetValue(TrackerProperty, tracker);
        }
        tracker.Apply();
    }

    // The padding property an element takes the safe area in, else its margin.
    internal static StyledProperty<Thickness> Target(Control control) => control switch
    {
        Decorator => Decorator.PaddingProperty,
        TemplatedControl => TemplatedControl.PaddingProperty,
        TextBlock => TextBlock.PaddingProperty,
        _ => Layoutable.MarginProperty,
    };

    /// <summary>The safe area on the chosen edges, the left and right swapped where the element lays out right to left.</summary>
    internal static Thickness For(Control control, Insets insets, SafeAreaEdges edges)
    {
        var safe = insets.SafeArea;
        var left = edges.HasFlag(SafeAreaEdges.Left) ? safe.Left : 0;
        var right = edges.HasFlag(SafeAreaEdges.Right) ? safe.Right : 0;
        var bottom = edges.HasFlag(SafeAreaEdges.Bottom) ? safe.Bottom : 0;
        if (edges.HasFlag(SafeAreaEdges.Keyboard) && insets.KeyboardOpen && TopLevel.GetTopLevel(control) is { } top && control.TranslatePoint(new Point(0, control.Bounds.Height), top) is { } foot)
            bottom = Math.Max(bottom, foot.Y - insets.Keyboard.Top);
        var above = edges.HasFlag(SafeAreaEdges.Top) ? safe.Top : 0;
        return control.FlowDirection == FlowDirection.RightToLeft ? new Thickness(right, above, left, bottom) : new Thickness(left, above, right, bottom);
    }

    private sealed class Tracker
    {
        private readonly Control _control;
        private Insets? _insets;
        private TopLevel? _top;
        private IDisposable? _set;
        private StyledProperty<Thickness>? _on;

        public Tracker(Control control)
        {
            _control = control;
            control.AttachedToVisualTree += OnAttached;
            control.DetachedFromVisualTree += OnDetached;
            control.PropertyChanged += OnChanged;
            if (TopLevel.GetTopLevel(control) is not null) Hook();
        }

        public void Stop()
        {
            _control.AttachedToVisualTree -= OnAttached;
            _control.DetachedFromVisualTree -= OnDetached;
            _control.PropertyChanged -= OnChanged;
            Unhook();
            Clear();
        }

        private void OnAttached(object? sender, VisualTreeAttachmentEventArgs e)
        {
            Hook();
            Apply();
        }

        private void OnDetached(object? sender, VisualTreeAttachmentEventArgs e) => Unhook();

        private void OnChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
        {
            if (e.Property == Visual.FlowDirectionProperty) Apply();
        }

        private void Hook()
        {
            Unhook();
            if (TopLevel.GetTopLevel(_control) is not { } top || Insets.For(top) is not { HasSource: true } insets) return;
            _top = top;
            _insets = insets;
            _insets.Changed += OnInsets;
            top.SizeChanged += OnTopSized;
        }

        private void Unhook()
        {
            if (_insets is not null) _insets.Changed -= OnInsets;
            if (_top is not null) _top.SizeChanged -= OnTopSized;
            _insets = null;
            _top = null;
        }

        private void OnInsets(object? sender, EventArgs e) => Apply();

        // A rotation can change the style padding under the safe area too (a narrow class), so it is read again once the layout has settled.
        private void OnTopSized(object? sender, SizeChangedEventArgs e) => global::Avalonia.Threading.Dispatcher.UIThread.Post(Apply, global::Avalonia.Threading.DispatcherPriority.Background);

        private void Clear()
        {
            _set?.Dispose();
            _set = null;
            _on = null;
        }

        // The padding the element has without the safe area, plus the safe area; set above the element's own values and given back when the safe area goes.
        public void Apply()
        {
            var edges = GetEdges(_control);
            var add = _insets is null || edges == SafeAreaEdges.None ? default : For(_control, _insets, edges);
            var on = Target(_control);
            if (_on is not null && _on != on) Clear();
            if (add == default)
            {
                Clear();
                return;
            }
            var own = _control.GetBaseValue(on);
            var baseValue = own.HasValue ? own.Value : default;
            var want = baseValue + add;
            if (_set is not null && _control.GetValue(on) == want) return;
            _set?.Dispose();
            _set = _control.SetValue(on, want, BindingPriority.Animation);
            _on = on;
        }
    }
}
