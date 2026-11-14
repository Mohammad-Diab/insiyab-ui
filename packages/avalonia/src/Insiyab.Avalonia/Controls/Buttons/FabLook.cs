using System.Runtime.CompilerServices;
using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;

namespace Insiyab.Avalonia.Controls;

/// <summary>The floating action button's own work, set by its look (class fab): placed at its container's foot it rises above what covers the foot of the screen, and a screen reader hears its tip as its name.</summary>
internal static class FabLook
{
    public static readonly AttachedProperty<bool> IsOnProperty =
        AvaloniaProperty.RegisterAttached<Button, bool>("IsOn", typeof(FabLook));

    private static readonly AttachedProperty<Lift?> LiftProperty =
        AvaloniaProperty.RegisterAttached<Button, Lift?>("Lift", typeof(FabLook));

    // The floating buttons shown in each TopLevel, so a bar that comes, goes or moves lifts them again.
    private static readonly ConditionalWeakTable<TopLevel, HashSet<Lift>> s_shown = new();

    static FabLook()
    {
        IsOnProperty.Changed.AddClassHandler<Button>((button, e) => Watch(button, e.GetNewValue<bool>()));
    }

    public static bool GetIsOn(Button button) => button.GetValue(IsOnProperty);
    public static void SetIsOn(Button button, bool value) => button.SetValue(IsOnProperty, value);

    /// <summary>How far the button is lifted above its place right now; 0 when nothing covers its foot.</summary>
    internal static double LiftOf(Button button) => button.GetValue(LiftProperty)?.By ?? 0;

    /// <summary>Lifts the floating buttons of the visual's TopLevel again, after a bar along its foot changed.</summary>
    internal static void Refit(Visual anchor)
    {
        if (OverlayHost.Top(anchor) is { } top && s_shown.TryGetValue(top, out var lifts))
            foreach (var lift in lifts.ToArray()) lift.Schedule();
    }

    private static void Watch(Button button, bool on)
    {
        var lift = button.GetValue(LiftProperty);
        if (!on)
        {
            lift?.Stop();
            button.ClearValue(LiftProperty);
            return;
        }
        if (lift is null) button.SetValue(LiftProperty, new Lift(button));
    }

    private sealed class Lift
    {
        private readonly Button _button;
        private TopLevel? _top;
        private Visual? _parent;
        private IDisposable? _shift;
        private IDisposable? _name;
        private bool _posted;
        private bool _naming;

        public double By { get; private set; }

        public Lift(Button button)
        {
            _button = button;
            button.AttachedToVisualTree += OnAttached;
            button.DetachedFromVisualTree += OnDetached;
            button.PropertyChanged += OnChanged;
            if (TopLevel.GetTopLevel(button) is not null) Hook();
            Name();
        }

        public void Stop()
        {
            _button.AttachedToVisualTree -= OnAttached;
            _button.DetachedFromVisualTree -= OnDetached;
            _button.PropertyChanged -= OnChanged;
            Unhook();
            Shift(0);
            _name?.Dispose();
            _name = null;
        }

        private void OnAttached(object? sender, VisualTreeAttachmentEventArgs e) => Hook();

        private void OnDetached(object? sender, VisualTreeAttachmentEventArgs e) => Unhook();

        private void OnChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
        {
            if (e.Property == Layoutable.VerticalAlignmentProperty) Schedule();
            else if (e.Property == ToolTip.TipProperty || e.Property == AutomationProperties.NameProperty || e.Property == ContentControl.ContentProperty) Name();
        }

        private void Hook()
        {
            Unhook();
            if (OverlayHost.Top(_button) is not { } top) return;
            _top = top;
            if (!s_shown.TryGetValue(top, out var lifts))
            {
                lifts = [];
                s_shown.Add(top, lifts);
            }
            lifts.Add(this);
            Insets.For(top).Changed += OnInsets;
            top.PropertyChanged += OnTop;
            _parent = _button.GetVisualParent();
            if (_parent is not null) _parent.PropertyChanged += OnParent;
            Schedule();
        }

        private void Unhook()
        {
            if (_top is { } top)
            {
                if (s_shown.TryGetValue(top, out var lifts)) lifts.Remove(this);
                Insets.For(top).Changed -= OnInsets;
                top.PropertyChanged -= OnTop;
            }
            if (_parent is not null) _parent.PropertyChanged -= OnParent;
            _top = null;
            _parent = null;
        }

        private void OnInsets(object? sender, EventArgs e) => Schedule();

        private void OnTop(object? sender, AvaloniaPropertyChangedEventArgs e)
        {
            if (e.Property == Visual.BoundsProperty) Schedule();
        }

        private void OnParent(object? sender, AvaloniaPropertyChangedEventArgs e)
        {
            if (e.Property == Visual.BoundsProperty) Schedule();
        }

        // After the layout the change brings, so the container and the bars are measured where they now are.
        public void Schedule()
        {
            if (_posted) return;
            _posted = true;
            Dispatcher.UIThread.Post(() =>
            {
                _posted = false;
                Fit();
            }, DispatcherPriority.Loaded);
        }

        // The part of the container's foot the safe area, the keyboard or a bar along the screen's foot covers.
        private void Fit()
        {
            var by = 0d;
            if (_top is { } top && _parent is { } parent && _button.VerticalAlignment == VerticalAlignment.Bottom
                && parent.TranslatePoint(new Point(0, parent.Bounds.Height), top) is { } foot)
            {
                var height = top.Bounds.Height > 0 ? top.Bounds.Height : top.ClientSize.Height;
                var covered = Math.Max(Insets.For(top).Covered.Bottom, OverlayHost.FootRoom(top));
                by = Math.Max(0, foot.Y - (height - covered));
            }
            Shift(by);
        }

        // A render offset, so the container's layout never moves with it.
        private void Shift(double by)
        {
            if (Math.Abs(by - By) < 0.5 && (by > 0) == (_shift is not null)) return;
            By = by;
            _shift?.Dispose();
            _shift = by > 0 ? _button.SetValue(Visual.RenderTransformProperty, new TranslateTransform(0, -by), BindingPriority.Animation) : null;
        }

        // A button that shows only an icon is named by its tip, unless the app names it.
        private void Name()
        {
            if (_naming) return;
            _naming = true;
            try
            {
                _name?.Dispose();
                _name = null;
                if (string.IsNullOrEmpty(AutomationProperties.GetName(_button)) && _button.Content is not string && ToolTip.GetTip(_button) is string { Length: > 0 } tip)
                    _name = _button.SetValue(AutomationProperties.NameProperty, tip, BindingPriority.Style);
            }
            finally { _naming = false; }
        }
    }
}
