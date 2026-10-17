using Avalonia.Controls;

namespace Insiyab.Avalonia.Controls;

// The radius of a desktop window's own corners, for what sits flush against them; NaN outside a window.
internal static class WindowCorner
{
    // For tests: a restored window's radius on this OS.
    internal static double Os { get; set; } = OperatingSystem.IsWindowsVersionAtLeast(10, 0, 22000) ? 8 : OperatingSystem.IsMacOS() ? 10 : 0;

    public static double Of(TopLevel? top) => top is not Window w ? double.NaN : w.WindowState is WindowState.Maximized or WindowState.FullScreen ? 0 : Os;
}
