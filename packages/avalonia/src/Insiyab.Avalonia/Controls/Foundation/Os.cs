using System.Runtime.Versioning;

namespace Insiyab.Avalonia.Controls;

internal enum OsKind
{
    Windows,
    MacOS,
    Linux,
    Android,
    Browser,
}

/// <summary>The system the library runs on, asked in one place so the tests can stand in another one.</summary>
internal static class Os
{
    internal static OsKind? Fake { get; set; }

    [SupportedOSPlatformGuard("windows")]
    public static bool IsWindows => Fake is { } f ? f == OsKind.Windows : OperatingSystem.IsWindows();

    [SupportedOSPlatformGuard("macos")]
    public static bool IsMacOS => Fake is { } f ? f == OsKind.MacOS : OperatingSystem.IsMacOS();

    [SupportedOSPlatformGuard("linux")]
    public static bool IsLinux => Fake is { } f ? f == OsKind.Linux : OperatingSystem.IsLinux();

    [SupportedOSPlatformGuard("android")]
    public static bool IsAndroid => Fake is { } f ? f == OsKind.Android : OperatingSystem.IsAndroid();
}
