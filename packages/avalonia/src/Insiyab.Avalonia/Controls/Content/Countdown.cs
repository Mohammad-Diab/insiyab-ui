using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;
using Avalonia.Threading;

namespace Insiyab.Avalonia.Controls;

/// <summary>Which point a <see cref="Countdown"/> has reached.</summary>
public enum CountdownState
{
    /// <summary>It has come within <see cref="Countdown.Warn"/> seconds of zero.</summary>
    Warn,

    /// <summary>It has reached zero.</summary>
    End,
}

/// <summary>The data of <see cref="Countdown.StateChangedEvent"/>, the web's <c>ins:countdown</c>.</summary>
public sealed class CountdownEventArgs : RoutedEventArgs
{
    internal CountdownEventArgs(RoutedEvent routedEvent, CountdownState state, int left) : base(routedEvent)
    {
        State = state;
        Left = left;
    }

    public CountdownState State { get; }

    /// <summary>The seconds left when it fired.</summary>
    public int Left { get; }
}

/// <summary>Time left until something ends, counting down each second: the web's <c>.ins-countdown</c>. Give it <see cref="Seconds"/> (from when it appears) or <see cref="Until"/> (a moment). It turns amber (<c>:warn</c>) at <see cref="Warn"/> seconds left and red (<c>:done</c>) at zero, raising <see cref="StateChanged"/> at both. Class <c>lg</c> is the large size. Drive it with <see cref="Start"/>, <see cref="Stop"/>, <see cref="Restart()"/> or the matching commands.</summary>
[TemplatePart("PART_Days", typeof(TextBlock))]
[TemplatePart("PART_Clock", typeof(TextBlock))]
[PseudoClasses(":warn", ":done", ":paused", ":days")]
public class Countdown : TemplatedControl
{
    public static readonly StyledProperty<double> SecondsProperty =
        AvaloniaProperty.Register<Countdown, double>(nameof(Seconds), double.NaN);

    public static readonly StyledProperty<DateTimeOffset?> UntilProperty =
        AvaloniaProperty.Register<Countdown, DateTimeOffset?>(nameof(Until));

    public static readonly StyledProperty<double> WarnProperty =
        AvaloniaProperty.Register<Countdown, double>(nameof(Warn), 60);

    public static readonly StyledProperty<bool> StartPausedProperty =
        AvaloniaProperty.Register<Countdown, bool>(nameof(StartPaused));

    public static readonly StyledProperty<bool> PauseAwayProperty =
        AvaloniaProperty.Register<Countdown, bool>(nameof(PauseAway));

    public static readonly DirectProperty<Countdown, int?> LeftProperty =
        AvaloniaProperty.RegisterDirect<Countdown, int?>(nameof(Left), c => c.Left);

    public static readonly DirectProperty<Countdown, bool> IsRunningProperty =
        AvaloniaProperty.RegisterDirect<Countdown, bool>(nameof(IsRunning), c => c.IsRunning);

    public static readonly DirectProperty<Countdown, string> DaysTextProperty =
        AvaloniaProperty.RegisterDirect<Countdown, string>(nameof(DaysText), c => c.DaysText);

    public static readonly DirectProperty<Countdown, string> ClockTextProperty =
        AvaloniaProperty.RegisterDirect<Countdown, string>(nameof(ClockText), c => c.ClockText);

    public static readonly RoutedEvent<CountdownEventArgs> StateChangedEvent =
        RoutedEvent.Register<Countdown, CountdownEventArgs>(nameof(StateChanged), RoutingStrategies.Bubble);

    private readonly DispatcherTimer _timer = new() { Interval = TimeSpan.FromMilliseconds(250) };
    private DateTimeOffset? _end;
    private int? _pausedLeft;
    private int? _left;
    private bool _warned, _done, _away, _begun, _running;
    private string _daysText = "", _clockText = "";
    private WindowBase? _window;
    private IActivatableLifetime? _life;

    public Countdown()
    {
        _timer.Tick += (_, _) => Draw();
        StartCommand = new ContentCommand(() => Start());
        StopCommand = new ContentCommand(() => Stop());
        RestartCommand = new ContentCommand(() => Restart());
    }

    /// <summary>Seconds to count from, starting when the control appears; setting it again restarts from the new value.</summary>
    public double Seconds { get => GetValue(SecondsProperty); set => SetValue(SecondsProperty, value); }

    /// <summary>The moment it ends, in place of <see cref="Seconds"/>.</summary>
    public DateTimeOffset? Until { get => GetValue(UntilProperty); set => SetValue(UntilProperty, value); }

    /// <summary>How many seconds before zero it turns amber; a minute by default.</summary>
    public double Warn { get => GetValue(WarnProperty); set => SetValue(WarnProperty, value); }

    /// <summary>Shows its whole time and waits for <see cref="Start"/>.</summary>
    public bool StartPaused { get => GetValue(StartPausedProperty); set => SetValue(StartPausedProperty, value); }

    /// <summary>Stops while its window is inactive or minimised (or the app is in the background on a phone), and carries on when it comes back.</summary>
    public bool PauseAway { get => GetValue(PauseAwayProperty); set => SetValue(PauseAwayProperty, value); }

    /// <summary>The seconds left, or null while it has never started.</summary>
    public int? Left { get => _left; private set => SetAndRaise(LeftProperty, ref _left, value); }

    public bool IsRunning { get => _running; private set => SetAndRaise(IsRunningProperty, ref _running, value); }

    /// <summary>The days, said in words before the clock ("يومان", "2d"), or empty.</summary>
    public string DaysText { get => _daysText; private set => SetAndRaise(DaysTextProperty, ref _daysText, value); }

    /// <summary>The clock, always left to right: 09:59, 1:02:03, 07:33:19 after the days.</summary>
    public string ClockText { get => _clockText; private set => SetAndRaise(ClockTextProperty, ref _clockText, value); }

    public ICommand StartCommand { get; }

    public ICommand StopCommand { get; }

    public ICommand RestartCommand { get; }

    /// <summary>Raised when it reaches the warning point and at zero.</summary>
    public event EventHandler<CountdownEventArgs>? StateChanged
    {
        add => AddHandler(StateChangedEvent, value);
        remove => RemoveHandler(StateChangedEvent, value);
    }

    /// <summary>Starts it, or carries on from where it stopped. Returns the seconds left.</summary>
    public int? Start()
    {
        _away = false;
        if (_end is not null) return LeftNow();
        return _pausedLeft is > 0 ? Set(TimeSpan.FromSeconds(_pausedLeft.Value), true) : Begin();
    }

    /// <summary>Stops it and keeps the time left on show. Returns the seconds left.</summary>
    public int? Stop()
    {
        _away = false;
        return Halt();
    }

    /// <summary>Starts it again from its full time.</summary>
    public int? Restart()
    {
        _away = false;
        return Begin();
    }

    /// <summary>Starts it again from <paramref name="seconds"/>.</summary>
    public int? Restart(double seconds)
    {
        _away = false;
        return Set(TimeSpan.FromSeconds(seconds), false);
    }

    private int? Begin()
    {
        if (Until is { } until) return SetEnd(until, false);
        return double.IsNaN(Seconds) ? null : Set(TimeSpan.FromSeconds(Seconds), false);
    }

    private int? Set(TimeSpan span, bool resume) => SetEnd(DateTimeOffset.UtcNow + span, resume);

    private int? SetEnd(DateTimeOffset end, bool resume)
    {
        _begun = true;
        _end = end;
        _pausedLeft = null;
        if (!resume)
        {
            _warned = _done = false;
            PseudoClasses.Set(":warn", false);
            PseudoClasses.Set(":done", false);
        }
        PseudoClasses.Set(":paused", false);
        IsRunning = true;
        if (TopLevel.GetTopLevel(this) is not null) _timer.Start();
        return Draw();
    }

    private int? Halt()
    {
        if (_end is null) return _pausedLeft;
        var left = LeftNow();
        _end = null;
        _pausedLeft = left;
        _timer.Stop();
        IsRunning = false;
        PseudoClasses.Set(":paused", true);
        Paint(left);
        Left = left;
        return left;
    }

    private int LeftNow() => _end is { } end ? Math.Max(0, (int)Math.Ceiling((end - DateTimeOffset.UtcNow).TotalSeconds)) : _pausedLeft ?? 0;

    private int Draw()
    {
        var left = LeftNow();
        Paint(left);
        Left = left;
        Finish(left);
        if (left == 0)
        {
            _timer.Stop();
            _end = null;
            IsRunning = false;
        }
        return left;
    }

    // While it waits to start, it shows its whole time.
    private void Idle()
    {
        int secs;
        if (Until is { } until) secs = Math.Max(0, (int)Math.Ceiling((until - DateTimeOffset.UtcNow).TotalSeconds));
        else if (!double.IsNaN(Seconds)) secs = (int)Math.Ceiling(Seconds);
        else return;
        PseudoClasses.Set(":paused", true);
        Paint(secs);
    }

    private void Paint(int left)
    {
        int d = left / 86400, h = left % 86400 / 3600, m = left % 3600 / 60, s = left % 60;
        ClockText = (d > 0 || h > 0 ? (d > 0 ? h.ToString("00") : h.ToString()) + ":" : "") + m.ToString("00") + ":" + s.ToString("00");
        DaysText = d > 0 ? SayDays(d) : "";
        PseudoClasses.Set(":days", d > 0);
    }

    // The web says days in the page's language; here an RTL countdown speaks Arabic and an LTR one English.
    private string SayDays(int d)
    {
        if (FlowDirection != global::Avalonia.Media.FlowDirection.RightToLeft) return d + "d";
        var r = d % 100;
        return d == 1 ? "يوم واحد" : d == 2 ? "يومان" : d + " " + (r is >= 3 and <= 10 ? "أيام" : r >= 11 ? "يومًا" : "يوم");
    }

    private void Finish(int left)
    {
        var warn = double.IsNaN(Warn) ? 60 : Warn;
        if (!_warned && left <= warn && left > 0)
        {
            _warned = true;
            PseudoClasses.Set(":warn", true);
            RaiseEvent(new CountdownEventArgs(StateChangedEvent, CountdownState.Warn, left));
        }
        if (!_done && left == 0)
        {
            _done = true;
            PseudoClasses.Set(":warn", false);
            PseudoClasses.Set(":done", true);
            RaiseEvent(new CountdownEventArgs(StateChangedEvent, CountdownState.End, 0));
        }
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == SecondsProperty || change.Property == UntilProperty)
        {
            if (TopLevel.GetTopLevel(this) is null) return;
            if (_begun && !StartPaused) Begin();
            else Idle();
        }
        else if (change.Property == FlowDirectionProperty && _clockText.Length > 0)
        {
            Paint(_end is not null ? LeftNow() : _pausedLeft ?? _left ?? 0);
        }
        else if (change.Property == StartPausedProperty && TopLevel.GetTopLevel(this) is not null && !_begun && !change.GetNewValue<bool>())
        {
            Begin();
        }
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        if (_end is not null) { _timer.Start(); Draw(); }
        else if (!_begun) { if (StartPaused) Idle(); else Begin(); }
        _window = TopLevel.GetTopLevel(this) as WindowBase;
        if (_window is not null)
        {
            _window.Activated += OnBack;
            _window.Deactivated += OnAway;
        }
        if (_window is null && Application.Current?.TryGetFeature<IActivatableLifetime>() is { } life)
        {
            _life = life;
            life.Activated += OnLifeBack;
            life.Deactivated += OnLifeAway;
        }
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        _timer.Stop();
        if (_window is not null)
        {
            _window.Activated -= OnBack;
            _window.Deactivated -= OnAway;
            _window = null;
        }
        if (_life is not null)
        {
            _life.Activated -= OnLifeBack;
            _life.Deactivated -= OnLifeAway;
            _life = null;
        }
    }

    private void OnAway(object? sender, EventArgs e)
    {
        if (!PauseAway || _end is null || LeftNow() == 0) return;
        Halt();
        _away = true;
    }

    private void OnBack(object? sender, EventArgs e)
    {
        if (!_away) return;
        _away = false;
        Start();
    }

    private void OnLifeAway(object? sender, ActivatedEventArgs e) { if (e.Kind == ActivationKind.Background) OnAway(sender, e); }

    private void OnLifeBack(object? sender, ActivatedEventArgs e) { if (e.Kind == ActivationKind.Background) OnBack(sender, e); }
}
