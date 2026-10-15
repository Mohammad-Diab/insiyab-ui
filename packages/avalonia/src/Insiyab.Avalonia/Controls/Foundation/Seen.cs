using System.Runtime.CompilerServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.VisualTree;

namespace Insiyab.Avalonia.Controls;

// Whether someone can see a control: attached, shown, in a window that is up and not minimised, and inside the viewport of whatever scrolls it. Motion nobody can see does not run.
internal static class Seen
{
    public const string PseudoClass = ":ins-seen";

    private static readonly ConditionalWeakTable<TopLevel, List<Watcher>> s_windows = new();
    private static readonly ConditionalWeakTable<Control, Request> s_pending = new();

    private static int s_watching;

    // Avalonia 12 keeps its effective-visibility event internal, so one class handler on IsVisible stands in for it.
    static Seen()
    {
        Window.WindowStateProperty.Changed.AddClassHandler<Window>((window, _) => Recheck(window, false));
        Visual.IsVisibleProperty.Changed.AddClassHandler<Visual>((visual, _) => Recheck(visual, true));
    }

    // Whether the window the visual is in is shown and not minimised.
    public static bool WindowShown(Visual v) =>
        OverlayHost.Top(v) is { IsVisible: true } top && top is not Window { WindowState: WindowState.Minimized };

    // Whether the visual has been given its place by a layout pass.
    public static bool Arranged(Visual v) => v.Bounds.Size != default || v is Layoutable { IsArrangeValid: true };

    // Whether someone can see the visual this instant; one that has never been laid out is judged by the nearest ancestor that has.
    public static bool Now(Visual v)
    {
        if (TopLevel.GetTopLevel(v) is null || !v.IsEffectivelyVisible || !WindowShown(v)) return false;
        var probe = v;
        if (!Arranged(v))
        {
            probe = v.GetVisualParent();
            while (probe is not null && probe.Bounds.Size == default) probe = probe.GetVisualParent();
            if (probe is null) return false;
        }
        if (probe.GetTransformedBounds() is not { } tb) return false;
        var r = tb.Bounds.TransformToAABB(tb.Transform);
        return r.Width <= 0 || r.Height <= 0 ? tb.Clip.Contains(r.TopLeft) : tb.Clip.Intersects(r);
    }

    // Calls changed with the first known state and then on every change, never twice with the same value.
    public static IDisposable Watch(Control c, Action<bool> changed) => new Watcher(c, changed);

    // Keeps the control's :ins-seen class true while someone can see it, for a style that runs only then.
    public static IDisposable Track(Control c)
    {
        var watch = Watch(c, on => ((IPseudoClasses)c.Classes).Set(PseudoClass, on));
        return new Undo(() =>
        {
            watch.Dispose();
            ((IPseudoClasses)c.Classes).Set(PseudoClass, false);
        });
    }

    // Runs play once the control is first seen, at once when it already is; a second request on the same control replaces the first, and detaching drops it.
    public static void WhenSeen(Control c, Action play)
    {
        if (s_pending.TryGetValue(c, out var old)) old.Dispose();
        if (Arranged(c) && Now(c))
        {
            play();
            return;
        }
        var request = new Request(c, play);
        if (!request.IsDone) s_pending.AddOrUpdate(c, request);
    }

    // A window changed state, or something in it was shown or hidden: the watchers of that window look again.
    private static void Recheck(Visual changed, bool visibility)
    {
        if (s_watching == 0 || OverlayHost.Top(changed) is not { } top || !s_windows.TryGetValue(top, out var list)) return;
        for (var i = list.Count - 1; i >= 0; i--)
        {
            if (i >= list.Count) continue;
            if (visibility) list[i].Visibility();
            else list[i].Recompute();
        }
    }

    private sealed class Undo(Action undo) : IDisposable
    {
        public void Dispose() => undo();
    }

    private sealed class Request : IDisposable
    {
        private readonly Control _c;
        private IDisposable? _watch;

        public Request(Control c, Action play)
        {
            _c = c;
            c.DetachedFromVisualTree += OnDetached;
            var watch = Watch(c, on =>
            {
                if (!on || IsDone) return;
                Dispose();
                play();
            });
            if (IsDone) watch.Dispose();
            else _watch = watch;
        }

        public bool IsDone { get; private set; }

        public void Dispose()
        {
            if (IsDone) return;
            IsDone = true;
            _c.DetachedFromVisualTree -= OnDetached;
            _watch?.Dispose();
            if (s_pending.TryGetValue(_c, out var current) && current == this) s_pending.Remove(_c);
        }

        private void OnDetached(object? sender, VisualTreeAttachmentEventArgs e) => Dispose();
    }

    private sealed class Watcher : IDisposable
    {
        private readonly Control _c;
        private readonly Action<bool> _changed;
        private Rect? _viewport;
        private bool? _last;
        private TopLevel? _top;
        private bool _viewing;
        private bool _waiting;
        private bool _disposed;

        public Watcher(Control c, Action<bool> changed)
        {
            _c = c;
            _changed = changed;
            c.AttachedToVisualTree += OnAttached;
            c.DetachedFromVisualTree += OnDetached;
            c.SizeChanged += OnSized;
            if (TopLevel.GetTopLevel(c) is not null) Start();
            else Set(false);
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            _c.AttachedToVisualTree -= OnAttached;
            _c.DetachedFromVisualTree -= OnDetached;
            _c.SizeChanged -= OnSized;
            Release();
        }

        // Something above the control was shown or hidden; the control looks again only if that changed what it can be seen through.
        public void Visibility()
        {
            if (_disposed || _top is null || _c.IsEffectivelyVisible == _viewing) return;
            Follow();
            Recompute();
        }

        // Whether someone can see the control, from the last viewport when there is one.
        public void Recompute()
        {
            if (_disposed) return;
            if (_top is null || !_c.IsEffectivelyVisible || !WindowShown(_c)) Set(false);
            else if (_viewport is { } viewport && Arranged(_c)) Set(Meets(viewport));
            else Set(Now(_c));
        }

        private void Start()
        {
            if (_top is not null || _disposed) return;
            _top = OverlayHost.Top(_c);
            if (_top is not null)
            {
                s_windows.GetOrCreateValue(_top).Add(this);
                s_watching++;
            }
            Follow();
            if (_top is null || !_c.IsEffectivelyVisible || !WindowShown(_c)) Set(false);
            else if (Arranged(_c)) Set(Now(_c));
            else if (!_waiting)
            {
                _waiting = true;
                _c.LayoutUpdated += OnLayout;
            }
        }

        // Nothing is listened to for the viewport while the control is hidden, so a hidden control costs the layout nothing.
        private void Follow()
        {
            var want = !_disposed && _top is not null && _c.IsEffectivelyVisible;
            if (want == _viewing) return;
            _viewing = want;
            if (want)
            {
                _c.EffectiveViewportChanged += OnViewport;
            }
            else
            {
                _c.EffectiveViewportChanged -= OnViewport;
                _viewport = null;
            }
        }

        private void Release()
        {
            if (_waiting)
            {
                _waiting = false;
                _c.LayoutUpdated -= OnLayout;
            }
            if (_top is not null && s_windows.TryGetValue(_top, out var list) && list.Remove(this)) s_watching--;
            _top = null;
            Follow();
        }

        private bool Meets(Rect viewport)
        {
            var size = _c.Bounds.Size;
            return size.Width <= 0 || size.Height <= 0 ? viewport.Contains(new Point()) : viewport.Intersects(new Rect(size));
        }

        private void Set(bool on)
        {
            if (_disposed || _last == on) return;
            _last = on;
            _changed(on);
        }

        private void OnAttached(object? sender, VisualTreeAttachmentEventArgs e) => Start();

        private void OnDetached(object? sender, VisualTreeAttachmentEventArgs e)
        {
            Release();
            Set(false);
        }

        private void OnSized(object? sender, SizeChangedEventArgs e) => Recompute();

        private void OnViewport(object? sender, EffectiveViewportChangedEventArgs e)
        {
            _viewport = e.EffectiveViewport;
            Recompute();
        }

        private void OnLayout(object? sender, EventArgs e)
        {
            _waiting = false;
            _c.LayoutUpdated -= OnLayout;
            Recompute();
        }
    }
}
