using System.Runtime.CompilerServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using Avalonia.Styling;

namespace Insiyab.Avalonia.Controls;

/// <summary>The status bar's own band (<see cref="SafeArea.StatusBandProperty"/>): the safe area's top filled a step darker than the app's ground, the wash shaded toward the text's ink in light and toward black in dark. It lives at the top of the window's adorner layer (over the dialogs and the toasts), takes no taps, and follows the insets, the theme and the brand live.</summary>
internal sealed class StatusBand : Control
{
    // how far the wash is shaded: a step in light, deeper in dark (the dark wash is near black already)
    private const double LightShade = 0.07, DarkShade = 0.45;

    private static readonly ConditionalWeakTable<TopLevel, StatusBand> Bands = new();
    private static readonly AttachedProperty<bool> HookedProperty = AvaloniaProperty.RegisterAttached<Control, bool>("StatusBandHooked", typeof(StatusBand));

    private Insets? _insets;
    private IDisposable? _wash;

    private StatusBand()
    {
        IsHitTestVisible = false;
        ZIndex = 2; // over the dialog stack (0) and the toasts (1)
    }

    /// <summary>The band of the TopLevel <paramref name="view"/> is in: shown while the view asks for it and is in a TopLevel.</summary>
    internal static void Follow(Control view, bool on)
    {
        if (!view.GetValue(HookedProperty))
        {
            view.SetValue(HookedProperty, true);
            view.AttachedToVisualTree += (_, _) => Sync(view);
            view.DetachedFromVisualTree += (_, _) => Hide(view);
        }
        Sync(view);
    }

    /// <summary>The band shown over <paramref name="top"/>, if any (tests).</summary>
    internal static StatusBand? Of(TopLevel top) => Bands.TryGetValue(top, out var band) && band.Parent is not null ? band : null;

    private static void Sync(Control view)
    {
        if (!SafeArea.GetStatusBand(view))
        {
            Hide(view);
            return;
        }
        if (OverlayHost.Top(view) is not { } top || OverlayHost.Layer(top) is not { } layer) return;
        if (!Bands.TryGetValue(top, out var band))
        {
            band = new StatusBand();
            Bands.Add(top, band);
        }
        if (band.Parent is null)
        {
            layer.Children.Add(band);
            AdornerLayer.SetAdornedElement(band, layer);
        }
        band.IsVisible = true;
    }

    private static void Hide(Control view)
    {
        if (OverlayHost.Top(view) is { } top && Bands.TryGetValue(top, out var band))
            band.IsVisible = false;
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        _insets = Insets.Of(this);
        if (_insets is not null) _insets.Changed += OnInsets;
        // the wash changes with the theme and the brand: drawn again each time
        _wash = this.GetResourceObservable("InsWash1Color").Subscribe(new Observer(InvalidateVisual));
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        if (_insets is not null) _insets.Changed -= OnInsets;
        _insets = null;
        _wash?.Dispose();
        _wash = null;
        base.OnDetachedFromVisualTree(e);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == ThemeVariantScope.ActualThemeVariantProperty) InvalidateVisual();
    }

    private void OnInsets(object? sender, EventArgs e) => InvalidateVisual();

    /// <summary>The band's colour now: the wash shaded a step.</summary>
    internal Color Shade()
    {
        var dark = ActualThemeVariant == ThemeVariant.Dark;
        var wash = this.TryFindResource("InsWash1Color", ActualThemeVariant, out var w) && w is Color c ? c : dark ? Color.FromRgb(0x0f, 0x11, 0x15) : Colors.White;
        var ink = dark ? Colors.Black : this.TryFindResource("InsText1Color", ActualThemeVariant, out var t) && t is Color i ? i : Colors.Black;
        var k = dark ? DarkShade : LightShade;
        byte Mix(byte a, byte b) => (byte)Math.Round(a + (b - a) * k);
        return Color.FromRgb(Mix(wash.R, ink.R), Mix(wash.G, ink.G), Mix(wash.B, ink.B));
    }

    public override void Render(DrawingContext context)
    {
        var height = _insets?.SafeArea.Top ?? 0;
        if (height <= 0 || Bounds.Width <= 0) return;
        context.FillRectangle(new ImmutableSolidColorBrush(Shade()), new Rect(0, 0, Bounds.Width, height));
    }

    private sealed class Observer(Action changed) : IObserver<object?>
    {
        public void OnCompleted() { }
        public void OnError(Exception error) { }
        public void OnNext(object? value) => changed();
    }
}
