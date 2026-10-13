using Avalonia.Media;

namespace Insiyab.Avalonia.Brand;

/// <summary>Derives the brand set from one primary colour, step for step as <c>Insiyab.brand()</c> does on the web.</summary>
public static class BrandDerivation
{
    private static readonly Color White = Color.FromRgb(0xff, 0xff, 0xff);
    private static readonly Color NearBlack = Color.FromRgb(0x11, 0x11, 0x11);
    private static readonly Color DarkWash1 = Color.FromRgb(0x0f, 0x11, 0x15);
    private static readonly Color DarkWash2 = Color.FromRgb(0x12, 0x14, 0x1a);

    /// <summary>The brand set for one theme. The alpha of <paramref name="primary"/> is ignored.</summary>
    public static BrandSet Derive(Color primary, bool dark)
    {
        var hex = Color.FromRgb(primary.R, primary.G, primary.B);
        var onPrimary = LabelFor(hex);
        var primaryDark = GradientEnd(hex, onPrimary);
        return new BrandSet(
            Primary: hex,
            PrimaryDark: primaryDark,
            PrimaryDarker: Shift(hex, dark ? 0.55 : -0.55),
            PrimaryRgb: hex,
            GradRgb: primaryDark,
            AccentRgb: primaryDark,
            OnPrimary: onPrimary,
            Wash1: dark ? DarkWash1 : Shift(hex, 0.93),
            Wash2: dark ? DarkWash2 : Shift(primaryDark, 0.94));
    }

    /// <summary>Toward black when <paramref name="amount"/> is negative, toward white when positive.</summary>
    public static Color Shift(Color color, double amount) =>
        Color.FromRgb(ShiftChannel(color.R, amount), ShiftChannel(color.G, amount), ShiftChannel(color.B, amount));

    /// <summary>The WCAG contrast ratio of two colours, alpha ignored.</summary>
    public static double Contrast(Color a, Color b)
    {
        double la = Luminance(a), lb = Luminance(b);
        return (Math.Max(la, lb) + 0.05) / (Math.Min(la, lb) + 0.05);
    }

    /// <summary>The label on a primary fill: white, or near-black when that measures better.</summary>
    public static Color LabelFor(Color color) =>
        Contrast(color, White) >= Contrast(color, NearBlack) ? White : NearBlack;

    private static Color GradientEnd(Color color, Color label) =>
        label == White ? Shift(color, -0.32) : Shift(color, 0.30);

    private static byte ShiftChannel(byte channel, double amount)
    {
        int c = channel;
        var value = amount < 0 ? JsRound(c * (1 + amount)) : JsRound(c + (255 - c) * amount);
        return (byte)Math.Max(0, Math.Min(255, value));
    }

    // JavaScript's Math.round: halves go toward positive infinity.
    private static double JsRound(double x)
    {
        var floor = Math.Floor(x);
        return x - floor >= 0.5 ? floor + 1 : floor;
    }

    private static double Luminance(Color color) =>
        0.2126 * Linear(color.R) + 0.7152 * Linear(color.G) + 0.0722 * Linear(color.B);

    private static double Linear(byte raw)
    {
        var c = raw / 255.0;
        return c <= 0.03928 ? c / 12.92 : Math.Pow((c + 0.055) / 1.055, 2.4);
    }
}
