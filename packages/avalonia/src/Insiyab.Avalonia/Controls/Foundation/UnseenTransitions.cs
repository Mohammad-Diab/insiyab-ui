using Avalonia;
using Avalonia.Animation;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data;
using Avalonia.Threading;
using Avalonia.VisualTree;

namespace Insiyab.Avalonia.Controls;

// While the theme changes, the controls nobody can see (hidden, scrolled out of view, outside their window) take the new colours at once: their transitions are held off until the change has gone through, so only what is on screen fades.
internal static class UnseenTransitions
{
    private static bool s_running;

    // Runs change with the transitions of every unseen control in the app's windows, and in origin's, held off; a run inside another is part of it.
    public static void During(Visual? origin, Action change)
    {
        if (s_running)
        {
            change();
            return;
        }
        s_running = true;
        var held = new List<IDisposable>();
        try
        {
            foreach (var top in Tops(origin))
            {
                var shown = top.IsVisible && top is not Window { WindowState: WindowState.Minimized };
                Walk(top, shown ? new Rect(top.Bounds.Size) : default, Matrix.Identity, held);
            }
            change();
        }
        finally
        {
            s_running = false;
            if (held.Count > 0) Dispatcher.UIThread.Post(() => { foreach (var h in held) h.Dispose(); }, DispatcherPriority.Background);
        }
    }

    private static IEnumerable<TopLevel> Tops(Visual? origin)
    {
        var tops = new List<TopLevel>();
        if (origin is not null && TopLevel.GetTopLevel(origin) is { } own) tops.Add(own);
        switch (Application.Current?.ApplicationLifetime)
        {
            case IClassicDesktopStyleApplicationLifetime desktop:
                foreach (var w in desktop.Windows)
                    if (!tops.Contains(w)) tops.Add(w);
                break;
            case ISingleViewApplicationLifetime { MainView: { } view } when TopLevel.GetTopLevel(view) is { } single && !tops.Contains(single):
                tops.Add(single);
                break;
        }
        return tops;
    }

    // seen is the part of the window, in its own coordinates, where this visual can show; empty once an ancestor is hidden or clips it away.
    private static void Walk(Visual v, Rect seen, Matrix parent, List<IDisposable> held)
    {
        var m = Local(v) * parent;
        if (!v.IsVisible) seen = default;
        var transitions = v.Transitions is { Count: > 0 };
        var inView = seen.Width > 0 && seen.Height > 0;
        if (inView && (transitions || v.ClipToBounds))
        {
            var box = new Rect(v.Bounds.Size).TransformToAABB(m);
            inView = box.Intersects(seen);
            if (v.ClipToBounds) seen = inView ? seen.Intersect(box) : default;
        }
        if (transitions && !inView && v.SetValue(Animatable.TransitionsProperty, null, BindingPriority.Animation) is { } hold) held.Add(hold);
        foreach (var child in v.GetVisualChildren()) Walk(child, seen, m, held);
    }

    // The visual's own placement in its parent, as Avalonia draws it: its mirror, its render transform about its origin, then its offset.
    private static Matrix Local(Visual v)
    {
        var m = v.HasMirrorTransform ? new Matrix(-1, 0, 0, 1, v.Bounds.Width, 0) : Matrix.Identity;
        if (v.RenderTransform is { } transform)
        {
            var origin = Matrix.CreateTranslation(v.RenderTransformOrigin.ToPixels(v.Bounds.Size));
            m *= -origin * transform.Value * origin;
        }
        return m * Matrix.CreateTranslation(v.Bounds.Position);
    }
}
