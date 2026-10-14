using Avalonia;

namespace PerfLab;

internal static class Program
{
    /// <summary>The command line: <c>--bench [report-dir]</c> runs the scenarios and exits; <c>--startup [file]</c> exits at the first frame and appends its time.</summary>
    public static string[] Args { get; private set; } = [];

    [STAThread]
    public static int Main(string[] args)
    {
        Args = args;
        var builder = AppBuilder.Configure<App>().UsePlatformDetect();
        // Diagnostics: --comp WinUIComposition|DirectComposition|LowLatencyDxgiSwapChain|RedirectionSurface and --render AngleEgl|Wgl|Software try the window's other drawing paths.
        if (Option("--comp") is { Length: > 0 } || Option("--render") is { Length: > 0 })
        {
            var options = new Win32PlatformOptions();
            if (Option("--comp") is { Length: > 0 } c) options.CompositionMode = [Enum.Parse<Win32CompositionMode>(c)];
            if (Option("--render") is { Length: > 0 } r) options.RenderingMode = [Enum.Parse<Win32RenderingMode>(r)];
            builder = builder.With(options);
        }
        return builder.StartWithClassicDesktopLifetime(args);
    }

    public static string? Option(string name)
    {
        var i = Array.IndexOf(Args, name);
        if (i < 0) return null;
        return i + 1 < Args.Length && !Args[i + 1].StartsWith("--") ? Args[i + 1] : "";
    }
}
