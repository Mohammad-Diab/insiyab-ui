using System.Diagnostics;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;

namespace PerfLab;

/// <summary>A small always-on-top readout: frame rate and frame times over the last second, the last page switch, layout time, CPU and memory. F12 or the status bar button toggles it.</summary>
public sealed class PerfReadout : Border
{
    private readonly TextBlock _text = new()
    {
        FontFamily = new FontFamily("Consolas, Cascadia Mono, monospace"),
        FontSize = 12,
        Foreground = Brushes.White,
        LineHeight = 17,
    };
    private readonly List<double> _frames = [];
    private readonly Stopwatch _clock = Stopwatch.StartNew();
    private double _last = -1;
    private double _windowStart;
    private TimeSpan _cpuStart;
    private bool _running;
    private long _passes;

    public PerfReadout()
    {
        Child = _text;
        Background = new SolidColorBrush(Color.FromArgb(0xD8, 0x10, 0x10, 0x18));
        CornerRadius = new CornerRadius(8);
        Padding = new Thickness(10, 6);
        Margin = new Thickness(12, 0, 12, 40);
        HorizontalAlignment = HorizontalAlignment.Left;
        VerticalAlignment = VerticalAlignment.Bottom;
        FlowDirection = FlowDirection.LeftToRight;
        IsHitTestVisible = false;
        _text.Text = "…";
    }

    public void Toggle() => IsVisible = !IsVisible;

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        Start();
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == IsVisibleProperty && IsVisible) Start();
    }

    private void Start()
    {
        if (_running || !IsVisible || TopLevel.GetTopLevel(this) is not { } top) return;
        _running = true;
        _last = -1;
        _frames.Clear();
        _windowStart = _clock.Elapsed.TotalMilliseconds;
        _cpuStart = Process.GetCurrentProcess().TotalProcessorTime;
        top.RequestAnimationFrame(Frame);
    }

    private void Frame(TimeSpan _)
    {
        if (!IsVisible || TopLevel.GetTopLevel(this) is not { } top)
        {
            _running = false;
            return;
        }
        var now = _clock.Elapsed.TotalMilliseconds;
        if (_last >= 0) _frames.Add(now - _last);
        _last = now;
        if (now - _windowStart >= 1000) Show(top, now);
        top.RequestAnimationFrame(Frame);
    }

    private void Show(TopLevel top, double now)
    {
        var span = now - _windowStart;
        var process = Process.GetCurrentProcess();
        var cpu = process.TotalProcessorTime;
        var cpuPct = (cpu - _cpuStart).TotalMilliseconds / span / Environment.ProcessorCount * 100;
        _cpuStart = cpu;
        var sorted = _frames.OrderBy(f => f).ToArray();
        var fps = _frames.Count * 1000 / span;
        var avg = sorted.Length > 0 ? sorted.Average() : 0;
        var p95 = sorted.Length > 0 ? sorted[(int)Math.Min(sorted.Length - 1, Math.Ceiling(sorted.Length * 0.95) - 1)] : 0;
        var worst = sorted.Length > 0 ? sorted[^1] : 0;
        var main = top as MainWindow;
        var page = main?.LastSwitchMs ?? 0;
        var passes = (main?.LayoutPasses ?? 0) - _passes;
        _passes += passes;
        _text.Text =
            $"FPS {fps,5:0}   frame avg {avg,5:0.0}  p95 {p95,5:0.0}  max {worst,5:0.0} ms\n" +
            $"page switch {page,6:0} ms   layout passes {passes,3}/s\n" +
            $"CPU {cpuPct,5:0.0}%   GC heap {GC.GetTotalMemory(false) / 1048576.0,6:0.0} MB   working set {process.WorkingSet64 / 1048576.0,6:0} MB";
        _frames.Clear();
        _windowStart = now;
    }
}
