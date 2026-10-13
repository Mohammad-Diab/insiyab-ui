// Shoots a gallery page at the web docs' content width (880), the way tools/compare/web-shot.mjs shoots its web page: Shots <slug> <out-dir> [--theme] [--dir] [--width].
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.Threading;
using Insiyab.Avalonia.Gallery;

var rest = new List<string>();
var flags = new Dictionary<string, string>();
for (var i = 0; i < args.Length; i++)
{
    if (args[i].StartsWith("--") && i + 1 < args.Length) flags[args[i][2..]] = args[++i];
    else rest.Add(args[i]);
}
if (rest.Count < 2)
{
    Console.Error.WriteLine("usage: Shots <slug> <out-dir> [--theme light|dark|both] [--dir rtl|ltr|both] [--width 880]");
    return 2;
}

AppBuilder.Configure<App>().UseSkia().UseHeadless(new AvaloniaHeadlessPlatformOptions { UseHeadlessDrawing = false }).SetupWithoutStarting();

var page = GalleryPages.Find(rest[0]);
if (page is null)
{
    Console.Error.WriteLine($"Shots: no gallery page '{rest[0]}'. Known: {string.Join(", ", GalleryPages.All.Select(p => p.Slug))}");
    return 1;
}
var outDir = Path.GetFullPath(rest[1]);
Directory.CreateDirectory(outDir);
var width = double.Parse(flags.GetValueOrDefault("width", "880"));
string[] themes = flags.GetValueOrDefault("theme", "both") is "both" ? ["light", "dark"] : [flags["theme"]];
string[] dirs = flags.GetValueOrDefault("dir", "both") is "both" ? ["rtl", "ltr"] : [flags["dir"]];

foreach (var theme in themes)
foreach (var dir in dirs)
{
    Application.Current!.RequestedThemeVariant = theme == "dark" ? ThemeVariant.Dark : ThemeVariant.Light;
    var content = new Border { Padding = new Thickness(24), Child = page.Create() };
    var window = new Window
    {
        Width = width,
        Height = 400,
        FlowDirection = dir == "ltr" ? FlowDirection.LeftToRight : FlowDirection.RightToLeft,
        Content = content,
    };
    window.Show();
    Settle();
    content.Measure(new Size(width, double.PositiveInfinity));
    window.Height = Math.Min(Math.Ceiling(content.DesiredSize.Height), 12000);
    Settle();
    var file = Path.Combine(outDir, $"ava-{page.Slug}-{theme}-{dir}.png");
#pragma warning disable CS0618
    window.CaptureRenderedFrame()?.Save(file);
#pragma warning restore CS0618
    window.Close();
    Console.WriteLine(file);
}
return 0;

static void Settle()
{
    for (var i = 0; i < 60; i++)
    {
        Dispatcher.UIThread.RunJobs();
        AvaloniaHeadlessPlatform.ForceRenderTimerTick();
        Thread.Sleep(10);
    }
}
