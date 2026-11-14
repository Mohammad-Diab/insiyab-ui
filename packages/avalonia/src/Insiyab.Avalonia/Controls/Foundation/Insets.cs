using System.Runtime.CompilerServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Platform;

namespace Insiyab.Avalonia.Controls;

/// <summary>Where a TopLevel's edges are covered: the safe area (system bars, cutouts) and the on-screen keyboard. A tests' stand-in can replace the platform.</summary>
internal interface IInsetsSource
{
    /// <summary>The room the system bars and cutouts take at each edge, in device-independent pixels.</summary>
    Thickness SafeArea { get; }

    /// <summary>The keyboard's box in the TopLevel's coordinates; empty while it is closed or floats.</summary>
    Rect Keyboard { get; }

    event EventHandler? Changed;
}

/// <summary>A TopLevel's covered edges, read from the platform (nothing on a desktop) and watched for changes, such as a rotation or the keyboard.</summary>
internal sealed class Insets
{
    private static readonly ConditionalWeakTable<TopLevel, Insets> s_all = new();

    // Tests give a TopLevel a stand-in source; null means the platform's.
    internal static Func<TopLevel, IInsetsSource?>? SourceFor { get; set; }

    private readonly TopLevel _top;
    private readonly IInsetsSource? _source;

    private Insets(TopLevel top)
    {
        _top = top;
        _source = SourceFor?.Invoke(top) ?? PlatformInsets.For(top);
        if (_source is not null) _source.Changed += (_, _) => Changed?.Invoke(this, EventArgs.Empty);
    }

    public static Insets For(TopLevel top)
    {
        if (!s_all.TryGetValue(top, out var insets))
        {
            insets = new Insets(top);
            s_all.Add(top, insets);
        }
        return insets;
    }

    /// <summary>The covered edges of the TopLevel the visual is in, or null outside one.</summary>
    public static Insets? Of(Visual? visual) => OverlayHost.Top(visual) is { } top ? For(top) : null;

    /// <summary>False on a desktop window: nothing ever covers its edges.</summary>
    public bool HasSource => _source is not null;

    public Thickness SafeArea => _source?.SafeArea ?? default;

    public Rect Keyboard => _source?.Keyboard ?? default;

    public bool KeyboardOpen => Keyboard is { Width: > 0, Height: > 0 };

    /// <summary>The part of the TopLevel nothing covers: inside the safe area and above the keyboard; all of it on a desktop.</summary>
    public Rect Room
    {
        get
        {
            var size = _top.Bounds.Width > 0 ? _top.Bounds.Size : _top.ClientSize;
            var safe = SafeArea;
            var room = new Rect(safe.Left, safe.Top, Math.Max(0, size.Width - safe.Left - safe.Right), Math.Max(0, size.Height - safe.Top - safe.Bottom));
            if (KeyboardOpen && Keyboard.Top < room.Bottom)
                room = room.WithHeight(Math.Max(0, Keyboard.Top - room.Top));
            return room;
        }
    }

    /// <summary>The room at each edge the safe area and the keyboard take together.</summary>
    public Thickness Covered
    {
        get
        {
            var size = _top.Bounds.Width > 0 ? _top.Bounds.Size : _top.ClientSize;
            var room = Room;
            return new Thickness(room.Left, room.Top, Math.Max(0, size.Width - room.Right), Math.Max(0, size.Height - room.Bottom));
        }
    }

    public event EventHandler? Changed;
}

/// <summary>The platform's insets: Avalonia's insets manager for the safe area and its input pane for the keyboard.</summary>
internal sealed class PlatformInsets : IInsetsSource
{
    private readonly IInsetsManager? _insets;
    private readonly IInputPane? _pane;

    private PlatformInsets(IInsetsManager? insets, IInputPane? pane)
    {
        _insets = insets;
        _pane = pane;
        if (insets is not null) insets.SafeAreaChanged += (_, _) => Changed?.Invoke(this, EventArgs.Empty);
        if (pane is not null) pane.StateChanged += (_, _) => Changed?.Invoke(this, EventArgs.Empty);
    }

    // A desktop window keeps its whole client area; only a single view (a phone, a browser) has bars and a keyboard over it.
    public static PlatformInsets? For(TopLevel top)
    {
        if (top is Window) return null;
        IInsetsManager? insets = null;
        IInputPane? pane = null;
        try { insets = top.InsetsManager; } catch (Exception) { }
        try { pane = top.InputPane; } catch (Exception) { }
        return insets is null && pane is null ? null : new PlatformInsets(insets, pane);
    }

    public Thickness SafeArea => _insets?.SafeAreaPadding ?? default;

    public Rect Keyboard => _pane is { State: InputPaneState.Open } pane ? pane.OccludedRect : default;

    public event EventHandler? Changed;
}
