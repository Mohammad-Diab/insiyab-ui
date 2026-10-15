using System.Diagnostics;
using System.Globalization;
using System.Text;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Controls.Primitives;
using Avalonia.Media;
using Avalonia.Rendering.Composition;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Insiyab.Avalonia;
using Insiyab.Avalonia.Controls;
using PerfLab.Pages;

namespace PerfLab;

/// <summary>Frame times while something runs: the gap between frames, the UI thread's work in each, and the process CPU per frame.</summary>
internal sealed class Recorder
{
    private readonly TopLevel _top;
    private readonly Action<int>? _step;
    private readonly List<double> _gaps = [];
    private readonly List<double> _work = [];
    private readonly Stopwatch _clock = Stopwatch.StartNew();
    private TimeSpan _cpu;
    private double _last = -1;
    private int _frame;
    private bool _on;
    private TaskCompletionSource? _ended;
    private Func<int, bool>? _until;

    public Recorder(TopLevel top, Action<int>? step = null)
    {
        _top = top;
        _step = step;
    }

    /// <summary>Runs until <paramref name="until"/> says the frame is the last, or <paramref name="ms"/> pass.</summary>
    public Task Run(double ms, Func<int, bool>? until = null)
    {
        _until = until;
        _ended = new TaskCompletionSource();
        _on = true;
        _cpu = Process.GetCurrentProcess().TotalProcessorTime;
        var start = _clock.Elapsed.TotalMilliseconds;
        void Frame(TimeSpan _)
        {
            if (!_on) return;
            var now = _clock.Elapsed.TotalMilliseconds;
            if (_last >= 0) _gaps.Add(now - _last);
            _last = now;
            Dispatcher.UIThread.Post(() => _work.Add(_clock.Elapsed.TotalMilliseconds - now), DispatcherPriority.Background);
            _step?.Invoke(_frame);
            _frame++;
            if (now - start >= ms || (_until?.Invoke(_frame) ?? false))
            {
                _on = false;
                _cpu = Process.GetCurrentProcess().TotalProcessorTime - _cpu;
                Dispatcher.UIThread.Post(() => _ended.TrySetResult(), DispatcherPriority.Background);
                return;
            }
            _top.RequestAnimationFrame(Frame);
        }
        _top.RequestAnimationFrame(Frame);
        return _ended.Task;
    }

    public FrameStats Stats() => new(_gaps, _work, _cpu.TotalMilliseconds / Math.Max(1, _frame));
}

internal sealed record FrameStats(List<double> Gaps, List<double> Work, double CpuPerFrame)
{
    public static double Pct(IReadOnlyList<double> v, double p)
    {
        if (v.Count == 0) return 0;
        var s = v.OrderBy(x => x).ToArray();
        return s[(int)Math.Clamp(Math.Ceiling(s.Length * p) - 1, 0, s.Length - 1)];
    }

    public int Frames => Gaps.Count;
    public double P50 => Pct(Gaps, 0.5);
    public double P95 => Pct(Gaps, 0.95);
    public double Max => Gaps.Count > 0 ? Gaps.Max() : 0;
    public int Janky => Gaps.Count(g => g > 25);
    public double WorkP50 => Pct(Work, 0.5);
    public double WorkP95 => Pct(Work, 0.95);
}

/// <summary>Guards a scripted run from a background thread: past the memory cap, a step's time or the whole run's time, it writes why and kills the process, so a runaway never sits on the screen.</summary>
internal static class Watchdog
{
    public const long MemoryCap = 2L * 1024 * 1024 * 1024;
    private static readonly Stopwatch s_clock = Stopwatch.StartNew();
    private static string s_step = "start";
    private static double s_stepEnd = 60;
    private static Func<string>? s_partial;

    public static void Start(string dir, double totalSeconds, Func<string>? partial = null)
    {
        s_partial = partial;
        new Thread(() =>
        {
            while (true)
            {
                Thread.Sleep(500);
                var ws = Environment.WorkingSet;
                var t = s_clock.Elapsed.TotalSeconds;
                string? why = ws > MemoryCap ? $"working set {ws / 1048576} MB passed the cap"
                    : t > totalSeconds ? $"the run passed {totalSeconds:0} s"
                    : t > s_stepEnd ? $"the step passed its time" : null;
                if (why is null) continue;
                try
                {
                    Directory.CreateDirectory(dir);
                    File.WriteAllText(Path.Combine(dir, "aborted.txt"), $"{why}; step «{s_step}» at {t:0.0} s\n\n{s_partial?.Invoke()}");
                }
                finally
                {
                    Process.GetCurrentProcess().Kill();
                }
            }
        }) { IsBackground = true, Name = "watchdog" }.Start();
    }

    public static void Step(string name, double seconds = 60)
    {
        s_step = name;
        s_stepEnd = s_clock.Elapsed.TotalSeconds + seconds;
    }
}

/// <summary>The <c>--bench</c> run: drives the app from code (no real input), times each scenario, writes report.md and report.csv, and exits.</summary>
internal sealed class Bench(MainWindow window, string dir)
{
    private readonly List<(string Section, string Name, string Metric, double Value)> _rows = [];

    public static async void ReportStartup(MainWindow window, string file)
    {
        Watchdog.Start(Path.GetDirectoryName(Path.GetFullPath(file.Length > 0 ? file : "startup.txt"))!, 60);
        await Shown(window);
        var ms = (DateTime.Now - Process.GetCurrentProcess().StartTime).TotalMilliseconds;
        File.AppendAllText(file.Length > 0 ? file : Path.Combine(AppContext.BaseDirectory, "startup.txt"), ms.ToString("0", CultureInfo.InvariantCulture) + Environment.NewLine);
        Shutdown(0);
    }

    private static void Shutdown(int code)
    {
        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime d) d.Shutdown(code);
    }

    /// <summary>After the next frame's UI work: animation callbacks, layout and the scene update.</summary>
    public static Task Frame(TopLevel top)
    {
        var done = new TaskCompletionSource();
        top.RequestAnimationFrame(_ => Dispatcher.UIThread.Post(() => done.TrySetResult(), DispatcherPriority.Background));
        return done.Task;
    }

    /// <summary>After the next frame is on screen: its UI work done and the render thread has drawn it.</summary>
    public static async Task Shown(TopLevel top)
    {
        await Frame(top);
        if (ElementComposition.GetElementVisual(top)?.Compositor is { } compositor) await compositor.RequestCommitAsync();
    }

    private static Task Wait(double ms) => Task.Delay(TimeSpan.FromMilliseconds(ms));

    private void Add(string section, string name, string metric, double value)
    {
        lock (_rows) _rows.Add((section, name, metric, value));
    }

    private void AddFrames(string section, string name, FrameStats s)
    {
        Add(section, name, "frames", s.Frames);
        Add(section, name, "gap p50 ms", s.P50);
        Add(section, name, "gap p95 ms", s.P95);
        Add(section, name, "gap max ms", s.Max);
        Add(section, name, "janky (>25ms)", s.Janky);
        Add(section, name, "ui work p50 ms", s.WorkP50);
        Add(section, name, "ui work p95 ms", s.WorkP95);
        Add(section, name, "cpu ms/frame", s.CpuPerFrame);
    }

    private async Task<(double Ui, double Shown)> Time(Action act)
    {
        var clock = Stopwatch.StartNew();
        act();
        await Frame(window);
        var ui = clock.Elapsed.TotalMilliseconds;
        if (ElementComposition.GetElementVisual(window)?.Compositor is { } compositor) await compositor.RequestCommitAsync();
        return (ui, clock.Elapsed.TotalMilliseconds);
    }

    public async Task RunAsync()
    {
        Watchdog.Start(dir, 900, Markdown);
        try
        {
            Watchdog.Step("first frame");
            await Shown(window);
            Add("startup", "process start to first frame", "ms", (DateTime.Now - Process.GetCurrentProcess().StartTime).TotalMilliseconds);
            window.Readout.IsVisible = false;
            await Wait(1500);
            var still = Motion.GetStill(window);
            var tag = still ? " (effects off)" : "";

            // --hold <kind>: leaves that page, collects, and waits so a heap tool can look for what still holds it.
            if (Program.Option("--hold") is { Length: > 0 } held)
            {
                Watchdog.Step("hold", 120);
                var weak = await Visit(held);
                window.Select(First(held == "overlays" ? "dashboard" : "overlays"));
                await Wait(1500);
                for (var i = 0; i < 3; i++)
                {
                    GC.Collect();
                    GC.WaitForPendingFinalizers();
                }
                Directory.CreateDirectory(dir);
                File.WriteAllText(Path.Combine(dir, "hold.txt"), $"{Environment.ProcessId} {(weak.TryGetTarget(out _) ? 1 : 0)}");
                await Wait(60000);
                Shutdown(0);
                return;
            }
            // --probe <kind>: shows one part of the page at a time and reads the CPU it costs while nothing happens.
            if (Program.Option("--probe") is { Length: > 0 } probed)
            {
                Watchdog.Step("probe", 400);
                window.Select(First(probed));
                await Wait(1000);
                var parts = Probe(window.PageContent!);
                for (var i = -1; i < parts.Count; i++)
                {
                    for (var j = 0; j < parts.Count; j++) parts[j].IsVisible = i < 0 || j == i;
                    await Wait(1200);
                    var cpu = Process.GetCurrentProcess().TotalProcessorTime;
                    await Wait(2000);
                    var name = i < 0 ? "all" : $"{i} {parts[i].GetType().Name} {Describe(parts[i])}";
                    Add("probe " + probed, name, "cpu ms / 2s", (Process.GetCurrentProcess().TotalProcessorTime - cpu).TotalMilliseconds);
                }
                Write();
                Shutdown(0);
                return;
            }
            await Suite(tag);
            if (!still && On("still"))
            {
                Motion.SetStill(window, true);
                await Wait(500);
                tag = " (effects off)";
                if (On("idle")) await Idle("idle" + tag, "dashboard");
                if (On("pages")) await Pages(tag);
                if (On("scroll")) await Scrolls(tag);
                if (On("popups")) await Popups(tag);
                if (On("theme")) await Themes(tag);
                Motion.SetStill(window, false);
            }
            Write();
            Shutdown(0);
        }
        catch (Exception e)
        {
            Directory.CreateDirectory(dir);
            File.WriteAllText(Path.Combine(dir, "error.txt"), e.ToString());
            Write();
            Shutdown(1);
        }
    }

    // The leaves a probe can switch on one by one: every control under the page's panels.
    private static List<Control> Probe(Control page)
    {
        var parts = new List<Control>();
        void Walk(Control c, int depth)
        {
            var kids = c.GetVisualChildren().OfType<Control>().ToList();
            if (depth > 8 || kids.Count == 0 || c is TemplatedControl && depth > 0) { parts.Add(c); return; }
            foreach (var k in kids) Walk(k, depth + 1);
        }
        Walk(page, 0);
        return parts;
    }

    private static string Describe(Control c) => c is ContentControl { Content: string s } ? s : c is TextBlock t ? t.Text ?? "" : string.Join(".", c.Classes);

    // --only idle,pages,scroll,popups,theme,brand,direction,memory,inactive,still runs just those groups.
    private static bool On(string group) => Program.Option("--only") is not { Length: > 0 } only || only.Split(',').Contains(group);

    private async Task Suite(string tag)
    {
        if (On("idle"))
        {
            await Idle("idle" + tag, "dashboard");
            await Idle("idle" + tag, "overlays");
            await Idle("idle" + tag, "form");
        }
        if (On("pages")) await Pages(tag);
        if (On("scroll")) await Scrolls(tag);
        if (On("popups")) await Popups(tag);
        if (On("theme")) await Themes(tag);
        if (On("brand")) await Brand(tag);
        if (On("direction")) await Direction(tag);
        if (On("memory")) await Memory(tag);
        if (On("inactive")) await Inactive(tag);
    }

    private Link First(string kind) => MainWindow.Links.First(l => l.Kind == kind);

    private async Task Idle(string section, string kind)
    {
        Watchdog.Step(section);
        window.Select(First(kind));
        await Wait(1500);
        var layout = window.LayoutPasses;
        var cpu = Process.GetCurrentProcess().TotalProcessorTime;
        await Wait(3000);
        Add(section, kind, "cpu ms / 3s", (Process.GetCurrentProcess().TotalProcessorTime - cpu).TotalMilliseconds);
        Add(section, kind, "layout passes / 3s", window.LayoutPasses - layout);
    }

    private static readonly string[] Kinds = ["dashboard", "form", "table", "calendar", "documents", "lists", "overlays"];

    private async Task Pages(string tag)
    {
        foreach (var kind in Kinds)
        {
            Watchdog.Step("page switch " + kind + tag);
            var link = First(kind);
            var warm = new List<double>();
            var warmShown = new List<double>();
            for (var round = 0; round < 4; round++)
            {
                window.Select(MainWindow.Links.First(l => l.Kind != kind));
                await Wait(400);
                var t = await Time(() => window.Select(link));
                if (round == 0 && tag.Length == 0)
                {
                    Add("page switch" + tag, kind, "first visit ui ms", t.Ui);
                    Add("page switch" + tag, kind, "first visit shown ms", t.Shown);
                }
                else
                {
                    warm.Add(t.Ui);
                    warmShown.Add(t.Shown);
                }
            }
            Add("page switch" + tag, kind, "warm ui ms (median)", FrameStats.Pct(warm, 0.5));
            Add("page switch" + tag, kind, "warm shown ms (median)", FrameStats.Pct(warmShown, 0.5));
        }
    }

    private async Task Scroll(string section, string name, ScrollViewer viewer, double speed)
    {
        Watchdog.Step(section + " " + name);
        viewer.Offset = default;
        await Wait(300);
        var max = Math.Max(0, viewer.Extent.Height - viewer.Viewport.Height);
        if (max < 1)
        {
            Add(section, name, "no room to scroll", 0);
            return;
        }
        var down = true;
        var y = 0.0;
        var rec = new Recorder(window, _ =>
        {
            y += down ? speed : -speed;
            if (y >= max) { y = max; down = false; }
            viewer.Offset = new Vector(viewer.Offset.X, Math.Max(0, y));
        });
        await rec.Run(4000, _ => !down && y <= 0);
        AddFrames(section, name, rec.Stats());
        Add(section, name, "scroll range px", max);
    }

    // Scrolls down unrecorded, then records only the way back up over rows that were just passed.
    private async Task Revisit(string section, string name, ScrollViewer viewer, double speed, double distance)
    {
        Watchdog.Step(section + " " + name);
        viewer.Offset = default;
        await Wait(300);
        var max = Math.Min(distance, Math.Max(0, viewer.Extent.Height - viewer.Viewport.Height));
        var y = 0.0;
        await new Recorder(window, _ =>
        {
            y = Math.Min(max, y + speed);
            viewer.Offset = new Vector(viewer.Offset.X, y);
        }).Run(8000, _ => y >= max);
        var rec = new Recorder(window, _ =>
        {
            y = Math.Max(0, y - speed);
            viewer.Offset = new Vector(viewer.Offset.X, y);
        });
        await rec.Run(8000, _ => y <= 0);
        AddFrames(section, name, rec.Stats());
    }

    private async Task Scrolls(string tag)
    {
        var section = "scroll" + tag;
        Watchdog.Step(section + " open pages");
        window.Select(First("form"));
        await Wait(800);
        if (Program.Option("--formonly") is not null)
        {
            // The first pass warms the code up; the second is the one reported.
            await Scroll(section + " warm-up", "long form (page)", window.PageScroller, 40);
            await Scroll(section, "long form (page)", window.PageScroller, 40);
            return;
        }
        await Scroll(section, "long form (page)", window.PageScroller, 40);

        window.Select(First("table"));
        await Wait(1200);
        var table = window.PageContent!.GetVisualDescendants().OfType<Table>().First();
        var inner = table.GetVisualDescendants().OfType<ScrollViewer>().First();
        await Scroll(section, "table 1200 rows (inner)", inner, 40);
        await Revisit(section, "table 1200 rows back up (revisit)", inner, 40, 2400);

        inner.Offset = default;
        await Wait(200);
        var maxX = Math.Max(0, inner.Extent.Width - inner.Viewport.Width);
        if (maxX > 1)
        {
            var x = 0.0;
            var rec = new Recorder(window, _ =>
            {
                x = Math.Min(maxX, x + 8);
                inner.Offset = new Vector(x, inner.Offset.Y);
            });
            await rec.Run(3000, _ => x >= maxX);
            AddFrames(section, "table sideways (frozen column)", rec.Stats());
        }

        window.Select(First("calendar"));
        await Wait(800);
        var month = window.PageContent!.GetVisualDescendants().OfType<MonthCalendar>().First();
        var toWeek = await Time(() => month.View = MonthCalendarView.Week);
        Add("calendar view" + tag, "month to week", "shown ms", toWeek.Shown);
        await Wait(400);
        await Scroll(section, "calendar week (page)", window.PageScroller, 30);
        var toDay = await Time(() => month.View = MonthCalendarView.Day);
        Add("calendar view" + tag, "week to day", "shown ms", toDay.Shown);
        await Wait(400);
        var toMonth = await Time(() => month.View = MonthCalendarView.Month);
        Add("calendar view" + tag, "day to month", "shown ms", toMonth.Shown);
        await Wait(400);
        var next = await Time(() => month.Month = month.Month.AddMonths(1));
        Add("calendar view" + tag, "next month", "shown ms", next.Shown);
        var rec2 = new Recorder(window);
        await rec2.Run(600);
        AddFrames("calendar view" + tag, "month turn frames", rec2.Stats());
    }

    private async Task Popup(string section, string name, Action open, Action close)
    {
        Watchdog.Step(section + " " + name);
        var rec = new Recorder(window);
        var clock = Stopwatch.StartNew();
        open();
        await Frame(window);
        var ui = clock.Elapsed.TotalMilliseconds;
        Add(section, name, "open ui ms", ui);
        await rec.Run(450);
        AddFrames(section, name + " open anim", rec.Stats());
        close();
        await Wait(500);
    }

    private async Task Popups(string tag)
    {
        var section = "popups" + tag;
        window.Select(First("overlays"));
        await Wait(1000);
        var page = (OverlaysPage)window.PageContent!;
        for (var round = 0; round < 2; round++)
        {
            var r = round == 0 ? " (first)" : " (again)";
            var dialog = Dialog.GetOpens(page.DialogButton)!;
            await Popup(section, "dialog" + r, () => dialog.Open(page.DialogButton), () => dialog.Close());
            var drawer = Dialog.GetOpens(page.DrawerButton)!;
            await Popup(section, "drawer" + r, () => drawer.Open(page.DrawerButton), () => drawer.Close());
            await Popup(section, "menu flyout" + r, () => page.MenuButton.Flyout!.ShowAt(page.MenuButton), () => page.MenuButton.Flyout!.Hide());
            await Popup(section, "notify flyout" + r, () => page.NotifyButton.Flyout!.ShowAt(page.NotifyButton), () => page.NotifyButton.Flyout!.Hide());
            await Popup(section, "context menu" + r, () => page.Card.ContextMenu!.Open(page.Card), () => page.Card.ContextMenu!.Close());
            await Popup(section, "toast" + r, () => Toast.Show(window, "تم حفظ الطلب", ToastTone.Ok), () => { });
        }
        Watchdog.Step(section + " toasts leave");
        await Wait(5500);
    }

    private async Task Themes(string tag)
    {
        var section = "theme" + tag;
        Watchdog.Step(section);
        window.Select(First("dashboard"));
        await Wait(1000);
        foreach (var to in new[] { "dark", "light", "dark", "light" })
        {
            var apply = Stopwatch.StartNew();
            ThemeToggle.Apply(to);
            Add(section, "apply to " + to, "apply alone ms", apply.Elapsed.TotalMilliseconds);
            await Frame(window);
            await Wait(600);
        }
        foreach (var to in new[] { "dark", "light", "dark", "light" })
        {
            var rec = new Recorder(window);
            var applied = new TaskCompletionSource();
            var clock = Stopwatch.StartNew();
            var appliedAt = 0.0;
            ThemeSweep.Run(window.DayNightToggle, () =>
            {
                ThemeToggle.Apply(to);
                appliedAt = clock.Elapsed.TotalMilliseconds;
                applied.TrySetResult();
            });
            var sync = clock.Elapsed.TotalMilliseconds;
            async Task<double> Painted()
            {
                await applied.Task;
                await Shown(window);
                return clock.Elapsed.TotalMilliseconds;
            }
            var painted = Painted();
            await rec.Run(750);
            Add(section, "sweep to " + to, "click to first paint (sync) ms", sync);
            Add(section, "sweep to " + to, "click to theme applied ms", appliedAt);
            Add(section, "sweep to " + to, "click to first paint ms", await painted);
            AddFrames(section, "sweep to " + to, rec.Stats());
            await Wait(500);
        }
    }

    private async Task Brand(string tag)
    {
        var section = "brand" + tag;
        Watchdog.Step(section);
        var times = new List<double>();
        foreach (var hex in new[] { "#0F766E", "#1D4ED8", "#9B2C5E", "#B45309", "#4C1D95" })
        {
            var t = await Time(() => InsiyabTheme.SetPrimary(Color.Parse(hex)));
            times.Add(t.Shown);
            await Wait(300);
        }
        Add(section, "SetPrimary (dashboard)", "shown ms (median)", FrameStats.Pct(times, 0.5));
        Add(section, "SetPrimary (dashboard)", "shown ms (max)", times.Max());
    }

    private async Task Direction(string tag)
    {
        var section = "direction" + tag;
        Watchdog.Step(section);
        foreach (var kind in new[] { "dashboard", "table" })
        {
            window.Select(First(kind));
            await Wait(1000);
            var ltr = await Time(() => window.FlowDirection = FlowDirection.LeftToRight);
            await Wait(400);
            var rtl = await Time(() => window.FlowDirection = FlowDirection.RightToLeft);
            await Wait(400);
            Add(section, kind, "to LTR shown ms", ltr.Shown);
            Add(section, kind, "to RTL shown ms", rtl.Shown);
        }
    }

    // Leaving the window and coming back through the platform's own callbacks, as a bench window never takes the OS focus: the selection's grey and the focus ring.
    private async Task Inactive(string tag)
    {
        var section = "inactive" + tag;
        var impl = window.PlatformImpl!;
        void Call(string callback) => ((Action?)impl.GetType().GetProperty(callback)!.GetValue(impl))?.Invoke();
        foreach (var kind in new[] { "dashboard", "table", "lists" })
        {
            Watchdog.Step(section + " " + kind);
            window.Select(First(kind));
            await Wait(1000);
            Call("Activated");
            await Wait(300);
            var away = new List<double>();
            var back = new List<double>();
            for (var round = 0; round < 3; round++)
            {
                away.Add((await Time(() => { Call("Deactivated"); Call("LostFocus"); })).Shown);
                await Wait(300);
                back.Add((await Time(() => Call("Activated"))).Shown);
                await Wait(300);
            }
            Add(section, kind, "to inactive shown ms (median)", FrameStats.Pct(away, 0.5));
            Add(section, kind, "to active shown ms (median)", FrameStats.Pct(back, 0.5));
        }
    }

    private async Task Memory(string tag)
    {
        var section = "memory" + tag;
        Watchdog.Step(section, 120);
        foreach (var link in MainWindow.Links)
        {
            window.Select(link);
            await Frame(window);
            await Wait(150);
        }
        window.Select(First("dashboard"));
        await Wait(1500);
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
        var p = Process.GetCurrentProcess();
        Add(section, "after all 30 pages", "GC heap MB", GC.GetTotalMemory(true) / 1048576.0);
        Add(section, "after all 30 pages", "working set MB", p.WorkingSet64 / 1048576.0);
        Add(section, "after all 30 pages", "private MB", p.PrivateMemorySize64 / 1048576.0);
        await Idle("idle after all pages" + tag, "dashboard");
        await Leaks(tag);
    }

    // Whether a page is still held once the app has moved off it: 1 is a leak.
    private async Task Leaks(string tag)
    {
        Watchdog.Step("leaks" + tag, 120);
        foreach (var kind in Kinds)
        {
            var weak = await Visit(kind);
            window.Select(First(kind == "overlays" ? "dashboard" : "overlays"));
            await Wait(1500);
            // A transition started on a part as its page left only finishes on a later frame, so a few frames run first.
            await new Recorder(window).Run(400);
            for (var i = 0; i < 3; i++)
            {
                GC.Collect();
                GC.WaitForPendingFinalizers();
            }
            Add("leak" + tag, kind, "page still held", weak.TryGetTarget(out _) ? 1 : 0);
        }
    }

    private async Task<WeakReference<Control>> Visit(string kind)
    {
        window.Select(First(kind));
        await Wait(800);
        return new WeakReference<Control>(window.PageContent!);
    }

    private void Write()
    {
        Directory.CreateDirectory(dir);
        var csv = new StringBuilder("section,name,metric,value\n");
        lock (_rows)
            foreach (var r in _rows) csv.AppendLine(string.Join(",", Q(r.Section), Q(r.Name), Q(r.Metric), r.Value.ToString("0.##", CultureInfo.InvariantCulture)));
        File.WriteAllText(Path.Combine(dir, "report.csv"), csv.ToString());
        File.WriteAllText(Path.Combine(dir, "report.md"), Markdown());
    }

    private string Markdown()
    {
        var md = new StringBuilder("| section | name | metric | value |\n|---|---|---|---|\n");
        lock (_rows)
            foreach (var r in _rows) md.AppendLine($"| {r.Section} | {r.Name} | {r.Metric} | {r.Value.ToString("0.#", CultureInfo.InvariantCulture)} |");
        return md.ToString();
    }

    private static string Q(string s) => "\"" + s.Replace("\"", "\"\"") + "\"";
}
