using System.Diagnostics;
using System.Globalization;
using System.Runtime;
using System.Runtime.CompilerServices;
using System.Text;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Threading;
using Avalonia.VisualTree;

namespace PerfLab;

/// <summary>The <c>--headless</c> run: builds pages in PerfLab's window on the headless platform (no GPU, no render) and times create, attach (styling), layout (templates) and the posted work after it (lower quartile of the reps); writes report.csv as <c>--bench</c> does.</summary>
internal static class PageTimer
{
    private static readonly List<(string Section, string Name, string Metric, double Value)> s_rows = [];

    // --pages dashboard,g:month picks the pages; --reps n times each n times after 2 warm-ups; --micro Border,TextBlock.card times one element's attach.
    public static int Run(string dir)
    {
        Watchdog.Start(dir, 900, Csv);
        // No background GC: its threads running beside a timed build made whole processes 30% slower at random.
        GCSettings.LatencyMode = GCLatencyMode.Batch;
        AppBuilder.Configure<App>().UseSkia().UseHeadless(new AvaloniaHeadlessPlatformOptions { UseHeadlessDrawing = false }).SetupWithoutStarting();
        var window = new MainWindow();
        window.Show();
        Settle(window);
        var reps = int.TryParse(Program.Option("--reps"), out var r) && r > 0 ? r : 8;
        var pages = (Program.Option("--pages") ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries);
        // Every page is built before any is timed, and the JIT gets a moment to finish its optimized code, so the first page timed is not the one that pays for it.
        Watchdog.Step("warm-up", 300);
        foreach (var kind in pages) Build(window, kind, 0);
        Thread.Sleep(1500);
        Dispatcher.UIThread.RunJobs();
        foreach (var kind in pages)
        {
            Watchdog.Step("build " + kind, 300);
            Build(window, kind, reps);
        }
        foreach (var element in (Program.Option("--micro") ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries))
        {
            Watchdog.Step("micro " + element, 300);
            Micro(window, element);
        }
        Directory.CreateDirectory(dir);
        File.WriteAllText(Path.Combine(dir, "report.csv"), Csv());
        return 0;
    }

    private static void Settle(TopLevel top)
    {
        for (var i = 0; i < 3; i++)
        {
            Dispatcher.UIThread.RunJobs();
            top.UpdateLayout();
        }
    }

    private static void Quiet()
    {
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
        Dispatcher.UIThread.RunJobs();
    }

    private static void Build(MainWindow window, string kind, int reps)
    {
        var link = MainWindow.Links.First(l => l.Kind == kind);
        var host = window.PageHost;
        host.Content = null;
        window.UpdateLayout();
        Dispatcher.UIThread.RunJobs();
        var m = new Dictionary<string, List<double>> { ["create"] = [], ["attach"] = [], ["layout"] = [], ["posted"] = [], ["total"] = [], ["detach"] = [], ["visuals"] = [] };
        for (var i = 0; i < reps + 2; i++)
        {
            Quiet();
            var clock = Stopwatch.StartNew();
            var page = Create(link);
            var t1 = clock.Elapsed.TotalMilliseconds;
            Attach(host, page);
            var t2 = clock.Elapsed.TotalMilliseconds;
            Layout(window);
            var t3 = clock.Elapsed.TotalMilliseconds;
            Posted(window);
            var t4 = clock.Elapsed.TotalMilliseconds;
            var visuals = Visuals(page);
            clock.Restart();
            Detach(host, window);
            var t5 = clock.Elapsed.TotalMilliseconds;
            if (i < 2) continue;
            m["create"].Add(t1);
            m["attach"].Add(t2 - t1);
            m["layout"].Add(t3 - t2);
            m["posted"].Add(t4 - t3);
            m["total"].Add(t4);
            m["detach"].Add(t5);
            m["visuals"].Add(visuals);
        }
        // The lower quartile of the reps: interference only ever adds time.
        if (reps > 0)
            foreach (var (name, v) in m) Add("build", kind, name == "visuals" ? "visuals" : name + " ms", FrameStats.Pct(v, 0.25));
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static Control Create(Link link) => link.Create();

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void Attach(ContentControl host, Control page) => host.Content = page;

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void Layout(TopLevel top) => top.UpdateLayout();

    // Loaded events and other work the page posts, and the layout it asks for again.
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void Posted(TopLevel top)
    {
        Dispatcher.UIThread.RunJobs();
        top.UpdateLayout();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void Detach(ContentControl host, TopLevel top)
    {
        host.Content = null;
        top.UpdateLayout();
        Dispatcher.UIThread.RunJobs();
    }

    private static int Visuals(Visual v) => 1 + v.GetVisualChildren().Sum(Visuals);

    // Attaches 2000 plain elements of one kind (Type or Type.class.class) to a panel in the page host: microseconds per element.
    private static void Micro(MainWindow window, string element)
    {
        const int count = 2000;
        var parts = element.Split('.');
        var attach = new List<double>();
        var layout = new List<double>();
        for (var round = 0; round < 7; round++)
        {
            var panel = new StackPanel();
            window.PageHost.Content = panel;
            window.UpdateLayout();
            var items = new List<Control>(count);
            for (var i = 0; i < count; i++)
            {
                Control c = parts[0] switch
                {
                    "TextBlock" => new TextBlock { Text = "نص" },
                    "StackPanel" => new StackPanel(),
                    "Canvas" => new Canvas(),
                    "Panel" => new Panel(),
                    "Button" => new Button { Content = "زر" },
                    _ => new Border(),
                };
                c.Classes.AddRange(parts.Skip(1));
                items.Add(c);
            }
            Quiet();
            var clock = Stopwatch.StartNew();
            panel.Children.AddRange(items);
            var a = clock.Elapsed.TotalMilliseconds;
            window.UpdateLayout();
            var l = clock.Elapsed.TotalMilliseconds - a;
            if (round > 1)
            {
                attach.Add(a * 1000 / count);
                layout.Add(l * 1000 / count);
            }
            window.PageHost.Content = null;
            window.UpdateLayout();
            Dispatcher.UIThread.RunJobs();
        }
        Add("micro", element, "attach us per element", FrameStats.Pct(attach, 0.25));
        Add("micro", element, "layout us per element", FrameStats.Pct(layout, 0.25));
    }

    private static void Add(string section, string name, string metric, double value) => s_rows.Add((section, name, metric, value));

    private static string Csv()
    {
        var csv = new StringBuilder("section,name,metric,value\n");
        foreach (var r in s_rows.ToArray()) csv.AppendLine($"\"{r.Section}\",\"{r.Name}\",\"{r.Metric}\",{r.Value.ToString("0.###", CultureInfo.InvariantCulture)}");
        return csv.ToString();
    }
}
