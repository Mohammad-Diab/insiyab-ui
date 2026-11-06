using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Primitives;
using Avalonia.Data;
using Avalonia.Interactivity;

namespace Insiyab.Avalonia.Controls;

/// <summary>The data of <see cref="MediaSeekRow.LiveRequestedEvent"/>: how far before the live edge to go; zero is the live edge itself.</summary>
public sealed class MediaLiveEventArgs : RoutedEventArgs
{
    internal MediaLiveEventArgs(RoutedEvent routedEvent, TimeSpan behind) : base(routedEvent) => Behind = behind;

    /// <summary>The place asked for, as a time before the live edge.</summary>
    public TimeSpan Behind { get; }
}

/// <summary>A player's seek row: the time played, the <see cref="MediaBar"/> and the length, each time in a slot as wide as its longest form so the bar never shakes as the digits change. A live stream shows «مباشر» at the end instead of the length, and while the person is behind, a button back to live with how far behind.</summary>
[TemplatePart("PART_Bar", typeof(MediaBar))]
[PseudoClasses(":live", ":behind")]
public class MediaSeekRow : TemplatedControl, IWords
{
    /// <summary>Where the media is, from 0 to 1, as <see cref="MediaBar.Value"/>.</summary>
    public static readonly StyledProperty<double> ValueProperty =
        AvaloniaProperty.Register<MediaSeekRow, double>(nameof(Value), defaultBindingMode: BindingMode.TwoWay);

    public static readonly StyledProperty<double> BufferedProperty =
        AvaloniaProperty.Register<MediaSeekRow, double>(nameof(Buffered));

    /// <summary>The media's length; for a live stream, how much of it can be played back so far.</summary>
    public static readonly StyledProperty<TimeSpan> DurationProperty =
        AvaloniaProperty.Register<MediaSeekRow, TimeSpan>(nameof(Duration));

    public static readonly StyledProperty<double> LeadProperty =
        AvaloniaProperty.Register<MediaSeekRow, double>(nameof(Lead));

    public static readonly StyledProperty<string?> LeadTipProperty =
        AvaloniaProperty.Register<MediaSeekRow, string?>(nameof(LeadTip));

    public static readonly StyledProperty<IList<MediaMarker>?> MarkersProperty =
        AvaloniaProperty.Register<MediaSeekRow, IList<MediaMarker>?>(nameof(Markers));

    public static readonly StyledProperty<int> CurrentMarkerProperty =
        AvaloniaProperty.Register<MediaSeekRow, int>(nameof(CurrentMarker), -1);

    /// <summary>A live stream: the end shows «مباشر» instead of the length.</summary>
    public static readonly StyledProperty<bool> IsLiveProperty =
        AvaloniaProperty.Register<MediaSeekRow, bool>(nameof(IsLive));

    /// <summary>How far behind the live edge the media plays.</summary>
    public static readonly StyledProperty<TimeSpan> BehindProperty =
        AvaloniaProperty.Register<MediaSeekRow, TimeSpan>(nameof(Behind));

    /// <summary>From how far behind the button back to live shows; under it the stream counts as live.</summary>
    public static readonly StyledProperty<TimeSpan> BehindFromProperty =
        AvaloniaProperty.Register<MediaSeekRow, TimeSpan>(nameof(BehindFrom), TimeSpan.FromSeconds(3));

    /// <summary>Where the button back to live takes the person, as a time before the live edge; zero is the live edge.</summary>
    public static readonly StyledProperty<TimeSpan> LiveTargetProperty =
        AvaloniaProperty.Register<MediaSeekRow, TimeSpan>(nameof(LiveTarget));

    /// <summary>Raised by the button back to live, with <see cref="LiveTarget"/>.</summary>
    public static readonly RoutedEvent<MediaLiveEventArgs> LiveRequestedEvent =
        RoutedEvent.Register<MediaSeekRow, MediaLiveEventArgs>(nameof(LiveRequested), RoutingStrategies.Bubble);

    private MediaBar? _bar;
    private TextBlock? _now;
    private TextBlock? _nowPattern;
    private TextBlock? _length;
    private TextBlock? _lengthPattern;
    private Control? _atLive;
    private TextBlock? _liveText;
    private Button? _backToLive;
    private TextBlock? _behind;
    private TextBlock? _behindPattern;
    private double? _dragAt;

    static MediaSeekRow()
    {
        ValueProperty.Changed.AddClassHandler<MediaSeekRow>((r, _) => r.Times());
        DurationProperty.Changed.AddClassHandler<MediaSeekRow>((r, _) => r.Times());
        IsLiveProperty.Changed.AddClassHandler<MediaSeekRow>((r, _) => r.Live());
        BehindProperty.Changed.AddClassHandler<MediaSeekRow>((r, _) => r.Live());
        BehindFromProperty.Changed.AddClassHandler<MediaSeekRow>((r, _) => r.Live());
    }

    public double Value { get => GetValue(ValueProperty); set => SetValue(ValueProperty, value); }

    public double Buffered { get => GetValue(BufferedProperty); set => SetValue(BufferedProperty, value); }

    public TimeSpan Duration { get => GetValue(DurationProperty); set => SetValue(DurationProperty, value); }

    public double Lead { get => GetValue(LeadProperty); set => SetValue(LeadProperty, value); }

    public string? LeadTip { get => GetValue(LeadTipProperty); set => SetValue(LeadTipProperty, value); }

    public IList<MediaMarker>? Markers { get => GetValue(MarkersProperty); set => SetValue(MarkersProperty, value); }

    public int CurrentMarker { get => GetValue(CurrentMarkerProperty); set => SetValue(CurrentMarkerProperty, value); }

    public bool IsLive { get => GetValue(IsLiveProperty); set => SetValue(IsLiveProperty, value); }

    public TimeSpan Behind { get => GetValue(BehindProperty); set => SetValue(BehindProperty, value); }

    public TimeSpan BehindFrom { get => GetValue(BehindFromProperty); set => SetValue(BehindFromProperty, value); }

    public TimeSpan LiveTarget { get => GetValue(LiveTargetProperty); set => SetValue(LiveTargetProperty, value); }

    /// <summary>True while a live stream plays further behind than <see cref="BehindFrom"/>: the button back to live shows.</summary>
    public bool IsBehind => IsLive && Behind > BehindFrom;

    /// <summary>The bar inside the row, for what the row does not pass on.</summary>
    public MediaBar? Bar => _bar;

    public event EventHandler<MediaLiveEventArgs>? LiveRequested
    {
        add => AddHandler(LiveRequestedEvent, value);
        remove => RemoveHandler(LiveRequestedEvent, value);
    }

    void IWords.Reword() => Words();

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);
        if (_bar is not null) _bar.SeekRequested -= OnSeek;
        if (_backToLive is not null) _backToLive.Click -= OnBackToLive;
        var n = e.NameScope;
        _bar = n.Find<MediaBar>("PART_Bar");
        _now = n.Find<TextBlock>("PART_Now");
        _nowPattern = n.Find<TextBlock>("PART_NowPattern");
        _length = n.Find<TextBlock>("PART_Length");
        _lengthPattern = n.Find<TextBlock>("PART_LengthPattern");
        _atLive = n.Find<Control>("PART_AtLive");
        _liveText = n.Find<TextBlock>("PART_LiveText");
        _backToLive = n.Find<Button>("PART_BackToLive");
        _behind = n.Find<TextBlock>("PART_Behind");
        _behindPattern = n.Find<TextBlock>("PART_BehindPattern");
        if (_bar is not null) _bar.SeekRequested += OnSeek;
        if (_backToLive is not null) _backToLive.Click += OnBackToLive;
        Times();
        Live();
        Words();
    }

    private void OnSeek(object? sender, MediaSeekEventArgs e)
    {
        _dragAt = e.IsDragging ? e.Value : null;
        Times();
    }

    private void OnBackToLive(object? sender, RoutedEventArgs e)
    {
        e.Handled = true;
        RaiseEvent(new MediaLiveEventArgs(LiveRequestedEvent, LiveTarget));
    }

    // Each time sits over an invisible copy of the length written in zeros, so its slot is as wide as the longest time and the bar never moves.
    internal static string Pattern(TimeSpan length)
    {
        var text = MediaBar.Clock(length);
        var chars = text.ToCharArray();
        for (var i = 0; i < chars.Length; i++)
            if (char.IsDigit(chars[i])) chars[i] = '0';
        var pattern = new string(chars);
        return pattern.Length < 5 ? "00:00" : pattern;
    }

    private void Times()
    {
        var at = TimeSpan.FromTicks((long)(Duration.Ticks * Math.Clamp(_dragAt ?? Value, 0, 1)));
        var pattern = Pattern(Duration);
        if (_now is not null) _now.Text = MediaBar.Clock(at);
        if (_nowPattern is not null) _nowPattern.Text = pattern;
        if (_length is not null) _length.Text = MediaBar.Clock(Duration);
        if (_lengthPattern is not null) _lengthPattern.Text = pattern;
        if (_behindPattern is not null) _behindPattern.Text = "−" + pattern;
    }

    private void Live()
    {
        var behind = IsBehind;
        PseudoClasses.Set(":live", IsLive);
        PseudoClasses.Set(":behind", behind);
        if (_behind is not null) _behind.Text = "−" + MediaBar.Clock(Behind);
        if (_atLive is not null) _atLive.Opacity = behind ? 0 : 1;
        if (_backToLive is not null)
        {
            _backToLive.Opacity = behind ? 1 : 0;
            _backToLive.IsHitTestVisible = behind;
            _backToLive.IsEnabled = behind;
        }
    }

    private void Words()
    {
        var words = Controls.Words.Of(this);
        if (_liveText is not null) _liveText.Text = words["media.live"];
        if (_backToLive is not null)
        {
            ToolTip.SetTip(_backToLive, words["media.backToLive"]);
            AutomationProperties.SetName(_backToLive, words["media.backToLive"]);
        }
    }
}
