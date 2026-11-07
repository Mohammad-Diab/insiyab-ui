using System.Collections.Specialized;
using System.Globalization;
using Avalonia;
using Avalonia.Automation;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Primitives;
using Avalonia.Data;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;

namespace Insiyab.Avalonia.Controls;

/// <summary>The data of <see cref="MediaControlBar.SkipRequestedEvent"/>: how far to move from where the media is.</summary>
public sealed class MediaSkipEventArgs : RoutedEventArgs
{
    internal MediaSkipEventArgs(RoutedEvent routedEvent, TimeSpan offset) : base(routedEvent) => Offset = offset;

    /// <summary>The step: negative for back, positive for forward.</summary>
    public TimeSpan Offset { get; }
}

/// <summary>A player's control row with its default buttons wired to bindable state: the volume and its level card, the speed, back, play, forward, the subtitles, fill the frame, full screen and a «⋯» menu. An app adds its own items to each group and hides any default. The row reads in the page's direction, but the centre transport (back, play, forward) runs left to right in every language as the time bar does; the cards and menus it opens read in the page's direction.</summary>
[TemplatePart("PART_StartItems", typeof(Panel))]
[TemplatePart("PART_CenterItems", typeof(Panel))]
[TemplatePart("PART_EndItems", typeof(Panel))]
public class MediaControlBar : TemplatedControl, IWords
{
    public static readonly StyledProperty<bool> IsPlayingProperty =
        AvaloniaProperty.Register<MediaControlBar, bool>(nameof(IsPlaying), defaultBindingMode: BindingMode.TwoWay);

    public static readonly StyledProperty<double> VolumeProperty =
        AvaloniaProperty.Register<MediaControlBar, double>(nameof(Volume), 1, defaultBindingMode: BindingMode.TwoWay, coerce: (_, v) => Math.Clamp(v, 0, 1));

    public static readonly StyledProperty<bool> IsMutedProperty =
        AvaloniaProperty.Register<MediaControlBar, bool>(nameof(IsMuted), defaultBindingMode: BindingMode.TwoWay);

    public static readonly StyledProperty<double> SpeedProperty =
        AvaloniaProperty.Register<MediaControlBar, double>(nameof(Speed), 1, defaultBindingMode: BindingMode.TwoWay);

    public static readonly StyledProperty<IList<double>?> SpeedsProperty =
        AvaloniaProperty.Register<MediaControlBar, IList<double>?>(nameof(Speeds));

    public static readonly StyledProperty<bool> IsSubtitlesOnProperty =
        AvaloniaProperty.Register<MediaControlBar, bool>(nameof(IsSubtitlesOn), defaultBindingMode: BindingMode.TwoWay);

    public static readonly StyledProperty<object?> SubtitlesCardProperty =
        AvaloniaProperty.Register<MediaControlBar, object?>(nameof(SubtitlesCard));

    public static readonly StyledProperty<object?> SubtitlesContentProperty =
        AvaloniaProperty.Register<MediaControlBar, object?>(nameof(SubtitlesContent));

    public static readonly StyledProperty<bool> IsFillingFrameProperty =
        AvaloniaProperty.Register<MediaControlBar, bool>(nameof(IsFillingFrame), defaultBindingMode: BindingMode.TwoWay);

    public static readonly StyledProperty<bool> IsFullScreenProperty =
        AvaloniaProperty.Register<MediaControlBar, bool>(nameof(IsFullScreen), defaultBindingMode: BindingMode.TwoWay);

    public static readonly StyledProperty<TimeSpan> SkipStepProperty =
        AvaloniaProperty.Register<MediaControlBar, TimeSpan>(nameof(SkipStep), TimeSpan.FromSeconds(10));

    public static readonly StyledProperty<bool> ShowVolumeProperty =
        AvaloniaProperty.Register<MediaControlBar, bool>(nameof(ShowVolume), true);

    public static readonly StyledProperty<bool> ShowSpeedProperty =
        AvaloniaProperty.Register<MediaControlBar, bool>(nameof(ShowSpeed), true);

    public static readonly StyledProperty<bool> ShowBackProperty =
        AvaloniaProperty.Register<MediaControlBar, bool>(nameof(ShowBack), true);

    public static readonly StyledProperty<bool> ShowPlayProperty =
        AvaloniaProperty.Register<MediaControlBar, bool>(nameof(ShowPlay), true);

    public static readonly StyledProperty<bool> ShowForwardProperty =
        AvaloniaProperty.Register<MediaControlBar, bool>(nameof(ShowForward), true);

    public static readonly StyledProperty<bool> ShowSubtitlesProperty =
        AvaloniaProperty.Register<MediaControlBar, bool>(nameof(ShowSubtitles), true);

    public static readonly StyledProperty<bool> ShowFitProperty =
        AvaloniaProperty.Register<MediaControlBar, bool>(nameof(ShowFit), true);

    public static readonly StyledProperty<bool> ShowFullScreenProperty =
        AvaloniaProperty.Register<MediaControlBar, bool>(nameof(ShowFullScreen), true);

    public static readonly StyledProperty<bool> ShowMoreProperty =
        AvaloniaProperty.Register<MediaControlBar, bool>(nameof(ShowMore), true);

    /// <summary>Raised by back and forward: move the media by <see cref="MediaSkipEventArgs.Offset"/>.</summary>
    public static readonly RoutedEvent<MediaSkipEventArgs> SkipRequestedEvent =
        RoutedEvent.Register<MediaControlBar, MediaSkipEventArgs>(nameof(SkipRequested), RoutingStrategies.Bubble);

    private static readonly double[] s_speeds = [0.75, 1, 1.25, 1.5, 1.75, 2];

    // The glyphs, one 24 box each, drawn at the row's one size: the speaker with up to three waves, struck out while muted.
    private const string Speaker = "M11 5 6 9H2v6h4l5 4z";
    private const string Wave1 = " M14.5 10a3 3 0 0 1 0 4";
    private const string Wave2 = " M17 7.5a7 7 0 0 1 0 9";
    private const string Wave3 = " M19.5 5a10.5 10.5 0 0 1 0 14";
    private static readonly Geometry[] s_volume = [Parse(Speaker), Parse(Speaker + Wave1), Parse(Speaker + Wave1 + Wave2), Parse(Speaker + Wave1 + Wave2 + Wave3)];
    private static readonly Geometry s_muted = Parse(Speaker + " M22 9l-6 6 M16 9l6 6");
    private static readonly Geometry s_play = Parse("M6 3L20 12L6 21Z");
    private static readonly Geometry s_pause = Parse("M15 4h2a1 1 0 0 1 1 1v14a1 1 0 0 1-1 1h-2a1 1 0 0 1-1-1v-14a1 1 0 0 1 1-1ZM7 4h2a1 1 0 0 1 1 1v14a1 1 0 0 1-1 1h-2a1 1 0 0 1-1-1v-14a1 1 0 0 1 1-1Z");
    private static readonly Geometry s_full = Parse("M8 3H5a2 2 0 0 0-2 2v3 M21 8V5a2 2 0 0 0-2-2h-3 M3 16v3a2 2 0 0 0 2 2h3 M16 21h3a2 2 0 0 0 2-2v-3");
    private static readonly Geometry s_fullOff = Parse("M8 3v3a2 2 0 0 1-2 2H3 M21 8h-3a2 2 0 0 1-2-2V3 M3 16h3a2 2 0 0 1 2 2v3 M16 21v-3a2 2 0 0 1 2-2h3");

    private static Geometry Parse(string data) => StreamGeometry.Parse(data);

    /// <summary>The glyphs drawn in code at their fullest (a quieter speaker drops waves), for the test that keeps the row's icons one size.</summary>
    internal static IEnumerable<Geometry> CodeGlyphs => [s_volume[3], s_muted, s_play, s_pause, s_full, s_fullOff];

    private readonly AvaloniaList<MenuItem> _speedItems = [];
    private readonly PopMenu _speedMenu;
    private readonly PopMenu _moreMenu;
    private readonly PopCard _volumeCard;
    private readonly PopCard _subtitlesCard = new();
    private readonly TextBlock _volumeLabel = new() { Classes = { "label" } };
    private readonly Slider _volumeSlider = new() { Minimum = 0, Maximum = 100, SmallChange = 5, LargeChange = 10, VerticalAlignment = VerticalAlignment.Center };
    private readonly TextBlock _volumeText = new() { Classes = { "num" }, FontWeight = FontWeight.SemiBold, MinWidth = 36, TextAlignment = TextAlignment.Right, VerticalAlignment = VerticalAlignment.Center };

    private Panel? _startItems, _centerItems, _endItems;
    private Control? _volumeGroup, _subtitlesGroup;
    private SplitButton? _volume;
    private Icon? _volumeIcon, _playIcon, _fullScreenIcon;
    private PlayMorph? _playMorph;
    private Icon? _playMorphIcon;
    private Button? _speed, _back, _play, _forward, _more;
    private TextBlock? _backSeconds, _forwardSeconds;
    private ToggleSplitButton? _subtitles;
    private ToggleButton? _subtitlesToggle, _fit, _fullScreen;
    private bool _syncing;

    public MediaControlBar()
    {
        _speedMenu = new PopMenu { ItemsSource = _speedItems };
        _moreMenu = new PopMenu { ItemsSource = MoreItems };
        var level = new Grid { ColumnDefinitions = new ColumnDefinitions("*,Auto"), ColumnSpacing = 12, FlowDirection = FlowDirection.LeftToRight, Children = { _volumeSlider, _volumeText } };
        Grid.SetColumn(_volumeText, 1);
        _volumeCard = new PopCard { Content = new StackPanel { Spacing = 10, MinWidth = 220, Children = { _volumeLabel, level } } };
        _volumeSlider.ValueChanged += OnLevel;
        StartItems.CollectionChanged += (_, e) => Items(e, _startItems, StartItems);
        CenterItems.CollectionChanged += (_, e) => Items(e, _centerItems, CenterItems);
        EndItems.CollectionChanged += (_, e) => Items(e, _endItems, EndItems);
        MoreItems.CollectionChanged += (_, _) => Show();
        MakeSpeeds();
        ShowVolumeLevel();
    }

    /// <summary>Whether the media plays: play and pause turn it, and its button shows the other.</summary>
    public bool IsPlaying { get => GetValue(IsPlayingProperty); set => SetValue(IsPlayingProperty, value); }

    /// <summary>The sound's level from 0 to 1: the card's slider sets it, and the speaker's waves tell it. Raising it while muted gives the sound back.</summary>
    public double Volume { get => GetValue(VolumeProperty); set => SetValue(VolumeProperty, value); }

    /// <summary>Whether the sound is off: a click on the speaker turns it.</summary>
    public bool IsMuted { get => GetValue(IsMutedProperty); set => SetValue(IsMutedProperty, value); }

    /// <summary>The playback speed, 1 for normal: the speed's menu sets it, and the button writes it, as <c>1.25×</c>.</summary>
    public double Speed { get => GetValue(SpeedProperty); set => SetValue(SpeedProperty, value); }

    /// <summary>The speeds the menu offers; unset, 0.75 to 2 by quarters.</summary>
    public IList<double>? Speeds { get => GetValue(SpeedsProperty); set => SetValue(SpeedsProperty, value); }

    /// <summary>Whether the subtitles show: the subtitles' button turns it.</summary>
    public bool IsSubtitlesOn { get => GetValue(IsSubtitlesOnProperty); set => SetValue(IsSubtitlesOnProperty, value); }

    /// <summary>The card the subtitles' arrow opens (a file to add, a language). Unset, the subtitles are a plain toggle with no arrow.</summary>
    public object? SubtitlesCard { get => GetValue(SubtitlesCardProperty); set => SetValue(SubtitlesCardProperty, value); }

    /// <summary>Shown beside the subtitles' icon, inside its button: how far subtitles being made have come, say. A control here is one of the row's own.</summary>
    public object? SubtitlesContent { get => GetValue(SubtitlesContentProperty); set => SetValue(SubtitlesContentProperty, value); }

    /// <summary>Opens the subtitles' card (<see cref="SubtitlesCard"/>) on its arrow, as when the app has something to say there (nothing to show yet, a file refused).</summary>
    public void ShowSubtitlesCard()
    {
        if (SubtitlesCard is not null && _subtitles is { IsEffectivelyVisible: true } at) _subtitlesCard.ShowAt(at);
    }

    /// <summary>Closes the subtitles' card, as after a choice made in it.</summary>
    public void HideSubtitlesCard() => _subtitlesCard.Hide();

    /// <summary>Whether the picture fills its frame, cut at the edges, rather than showing whole: the fill button turns it.</summary>
    public bool IsFillingFrame { get => GetValue(IsFillingFrameProperty); set => SetValue(IsFillingFrameProperty, value); }

    /// <summary>Whether the player is full screen: the full screen button turns it, and the app makes the window so.</summary>
    public bool IsFullScreen { get => GetValue(IsFullScreenProperty); set => SetValue(IsFullScreenProperty, value); }

    /// <summary>How far back and forward move; 10 seconds unless set, written inside their arrows.</summary>
    public TimeSpan SkipStep { get => GetValue(SkipStepProperty); set => SetValue(SkipStepProperty, value); }

    /// <summary>Whether the row shows the volume and its level card; false takes it out.</summary>
    public bool ShowVolume { get => GetValue(ShowVolumeProperty); set => SetValue(ShowVolumeProperty, value); }

    /// <summary>Whether the row shows the speed; false hides it and keeps its room, so play stays in the middle.</summary>
    public bool ShowSpeed { get => GetValue(ShowSpeedProperty); set => SetValue(ShowSpeedProperty, value); }

    /// <summary>Whether the row shows back; false takes it out.</summary>
    public bool ShowBack { get => GetValue(ShowBackProperty); set => SetValue(ShowBackProperty, value); }

    /// <summary>Whether the row shows play and pause; false takes it out.</summary>
    public bool ShowPlay { get => GetValue(ShowPlayProperty); set => SetValue(ShowPlayProperty, value); }

    /// <summary>Whether the row shows forward; false takes it out.</summary>
    public bool ShowForward { get => GetValue(ShowForwardProperty); set => SetValue(ShowForwardProperty, value); }

    /// <summary>Whether the row shows the subtitles; false takes it out.</summary>
    public bool ShowSubtitles { get => GetValue(ShowSubtitlesProperty); set => SetValue(ShowSubtitlesProperty, value); }

    /// <summary>Whether the row shows the fill button; false takes it out.</summary>
    public bool ShowFit { get => GetValue(ShowFitProperty); set => SetValue(ShowFitProperty, value); }

    /// <summary>Whether the row shows full screen; false takes it out.</summary>
    public bool ShowFullScreen { get => GetValue(ShowFullScreenProperty); set => SetValue(ShowFullScreenProperty, value); }

    /// <summary>Whether the «⋯» button shows; it shows only while <see cref="MoreItems"/> holds something.</summary>
    public bool ShowMore { get => GetValue(ShowMoreProperty); set => SetValue(ShowMoreProperty, value); }

    /// <summary>The app's own items after the start group's defaults.</summary>
    public global::Avalonia.Controls.Controls StartItems { get; } = [];

    /// <summary>The app's own items after forward; the speed's side grows as much, so play stays in the middle.</summary>
    public global::Avalonia.Controls.Controls CenterItems { get; } = [];

    /// <summary>The app's own items after full screen, before the «⋯» button.</summary>
    public global::Avalonia.Controls.Controls EndItems { get; } = [];

    /// <summary>The «⋯» menu's items, usually <see cref="MenuItem"/>s.</summary>
    public AvaloniaList<object> MoreItems { get; } = [];

    public event EventHandler<MediaSkipEventArgs>? SkipRequested
    {
        add => AddHandler(SkipRequestedEvent, value);
        remove => RemoveHandler(SkipRequestedEvent, value);
    }

    void IWords.Reword() => Words();

    private void Items(NotifyCollectionChangedEventArgs e, Panel? panel, global::Avalonia.Controls.Controls items)
    {
        Hosted.Adopt(LogicalChildren, e);
        if (e.NewItems is not null)
            foreach (var c in e.NewItems.OfType<Control>()) RunsLeftToRight(c);
        Fill(panel, items);
    }

    // The app's own items run left to right (a split's arrow after its icon), unless the app says otherwise on the item.
    private static void RunsLeftToRight(Control c) => c.SetValue(FlowDirectionProperty, FlowDirection.LeftToRight, BindingPriority.Style);

    private static void Fill(Panel? panel, global::Avalonia.Controls.Controls items)
    {
        if (panel is null) return;
        panel.Children.Clear();
        panel.Children.AddRange(items);
        panel.IsVisible = items.Count > 0;
    }

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);
        Unhook();
        _startItems?.Children.Clear();
        _centerItems?.Children.Clear();
        _endItems?.Children.Clear();
        var n = e.NameScope;
        _startItems = n.Find<Panel>("PART_StartItems");
        _centerItems = n.Find<Panel>("PART_CenterItems");
        _endItems = n.Find<Panel>("PART_EndItems");
        _volumeGroup = n.Find<Control>("PART_VolumeGroup");
        _volume = n.Find<SplitButton>("PART_Volume");
        _volumeIcon = n.Find<Icon>("PART_VolumeIcon");
        _speed = n.Find<Button>("PART_Speed");
        _back = n.Find<Button>("PART_Back");
        _backSeconds = n.Find<TextBlock>("PART_BackSeconds");
        _play = n.Find<Button>("PART_Play");
        _playIcon = n.Find<Icon>("PART_PlayIcon");
        _forward = n.Find<Button>("PART_Forward");
        _forwardSeconds = n.Find<TextBlock>("PART_ForwardSeconds");
        _subtitlesGroup = n.Find<Control>("PART_SubtitlesGroup");
        _subtitles = n.Find<ToggleSplitButton>("PART_Subtitles");
        _subtitlesToggle = n.Find<ToggleButton>("PART_SubtitlesToggle");
        _fit = n.Find<ToggleButton>("PART_Fit");
        _fullScreen = n.Find<ToggleButton>("PART_FullScreen");
        _fullScreenIcon = n.Find<Icon>("PART_FullScreenIcon");
        _more = n.Find<Button>("PART_More");
        Hook();
        Fill(_startItems, StartItems);
        Fill(_centerItems, CenterItems);
        Fill(_endItems, EndItems);
        Sync();
        Show();
        Glyphs();
        MakeSpeeds();
        Words();
    }

    private void Hook()
    {
        if (_volume is not null)
        {
            _volume.Click += OnMute;
            _volume.Flyout = _volumeCard;
        }
        if (_speed is not null) _speed.Flyout = _speedMenu;
        if (_back is not null) _back.Click += OnBack;
        if (_play is not null) _play.Click += OnPlay;
        if (_forward is not null) _forward.Click += OnForward;
        if (_subtitles is not null)
        {
            _subtitles.IsCheckedChanged += OnSubtitles;
            _subtitles.Flyout = _subtitlesCard;
        }
        if (_subtitlesToggle is not null) _subtitlesToggle.IsCheckedChanged += OnSubtitles;
        if (_fit is not null) _fit.IsCheckedChanged += OnFit;
        if (_fullScreen is not null) _fullScreen.IsCheckedChanged += OnFullScreen;
        if (_more is not null) _more.Flyout = _moreMenu;
    }

    private void Unhook()
    {
        if (_volume is not null)
        {
            _volume.Click -= OnMute;
            _volume.Flyout = null;
        }
        if (_speed is not null) _speed.Flyout = null;
        if (_back is not null) _back.Click -= OnBack;
        if (_play is not null) _play.Click -= OnPlay;
        if (_forward is not null) _forward.Click -= OnForward;
        if (_subtitles is not null)
        {
            _subtitles.IsCheckedChanged -= OnSubtitles;
            _subtitles.Flyout = null;
        }
        if (_subtitlesToggle is not null) _subtitlesToggle.IsCheckedChanged -= OnSubtitles;
        if (_fit is not null) _fit.IsCheckedChanged -= OnFit;
        if (_fullScreen is not null) _fullScreen.IsCheckedChanged -= OnFullScreen;
        if (_more is not null) _more.Flyout = null;
    }

    private void OnMute(object? sender, RoutedEventArgs e) => SetCurrentValue(IsMutedProperty, !IsMuted);

    private void OnPlay(object? sender, RoutedEventArgs e) => SetCurrentValue(IsPlayingProperty, !IsPlaying);

    private void OnBack(object? sender, RoutedEventArgs e) => RaiseEvent(new MediaSkipEventArgs(SkipRequestedEvent, -SkipStep));

    private void OnForward(object? sender, RoutedEventArgs e) => RaiseEvent(new MediaSkipEventArgs(SkipRequestedEvent, SkipStep));

    private void OnSubtitles(object? sender, RoutedEventArgs e)
    {
        if (_syncing) return;
        var on = sender switch
        {
            ToggleSplitButton s => s.IsChecked,
            ToggleButton t => t.IsChecked == true,
            _ => IsSubtitlesOn,
        };
        SetCurrentValue(IsSubtitlesOnProperty, on);
    }

    private void OnFit(object? sender, RoutedEventArgs e)
    {
        if (!_syncing && _fit is not null) SetCurrentValue(IsFillingFrameProperty, _fit.IsChecked == true);
    }

    private void OnFullScreen(object? sender, RoutedEventArgs e)
    {
        if (!_syncing && _fullScreen is not null) SetCurrentValue(IsFullScreenProperty, _fullScreen.IsChecked == true);
    }

    // The slider moved: the level follows, and raising it while muted gives the sound back.
    private void OnLevel(object? sender, RangeBaseValueChangedEventArgs e)
    {
        if (_syncing) return;
        var level = e.NewValue / 100;
        SetCurrentValue(VolumeProperty, level);
        if (level > 0 && IsMuted) SetCurrentValue(IsMutedProperty, false);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        var p = change.Property;
        if (p == IsPlayingProperty || p == IsMutedProperty || p == IsFullScreenProperty)
        {
            Sync();
            Glyphs();
            Words();
        }
        else if (p == VolumeProperty)
        {
            ShowVolumeLevel();
            Glyphs();
        }
        else if (p == IsSubtitlesOnProperty || p == IsFillingFrameProperty) Sync();
        else if (p == SpeedProperty || p == SpeedsProperty) MakeSpeeds();
        else if (p == SkipStepProperty) Words();
        else if (p == SubtitlesContentProperty)
        {
            // the content is the row's logical child, so its bindings read the row's data
            if (change.OldValue is Control old) LogicalChildren.Remove(old);
            if (change.NewValue is Control now)
            {
                LogicalChildren.Add(now);
                RunsLeftToRight(now);
            }
        }
        else if (p == SubtitlesCardProperty)
        {
            _subtitlesCard.Content = change.NewValue;
            Show();
        }
        else if (p == ShowVolumeProperty || p == ShowSpeedProperty || p == ShowBackProperty || p == ShowPlayProperty || p == ShowForwardProperty
                 || p == ShowSubtitlesProperty || p == ShowFitProperty || p == ShowFullScreenProperty || p == ShowMoreProperty) Show();
    }

    // The toggles show the state they were given.
    private void Sync()
    {
        _syncing = true;
        try
        {
            if (_subtitles is not null) _subtitles.IsChecked = IsSubtitlesOn;
            if (_subtitlesToggle is not null) _subtitlesToggle.IsChecked = IsSubtitlesOn;
            if (_fit is not null) _fit.IsChecked = IsFillingFrame;
            if (_fullScreen is not null) _fullScreen.IsChecked = IsFullScreen;
        }
        finally
        {
            _syncing = false;
        }
    }

    private void Show()
    {
        var card = SubtitlesCard is not null;
        if (_volumeGroup is not null) _volumeGroup.IsVisible = ShowVolume;
        if (_speed is not null) _speed.IsVisible = ShowSpeed;
        if (_back is not null) _back.IsVisible = ShowBack;
        if (_play is not null) _play.IsVisible = ShowPlay;
        if (_forward is not null) _forward.IsVisible = ShowForward;
        if (_subtitlesGroup is not null) _subtitlesGroup.IsVisible = ShowSubtitles && card;
        if (_subtitlesToggle is not null) _subtitlesToggle.IsVisible = ShowSubtitles && !card;
        if (_fit is not null) _fit.IsVisible = ShowFit;
        if (_fullScreen is not null) _fullScreen.IsVisible = ShowFullScreen;
        if (_more is not null) _more.IsVisible = ShowMore && MoreItems.Count > 0;
    }

    private void Glyphs()
    {
        if (_volumeIcon is not null) _volumeIcon.Data = IsMuted ? s_muted : s_volume[Volume <= 0 ? 0 : Volume < 0.34 ? 1 : Volume < 0.67 ? 2 : 3];
        if (_playIcon is not null)
        {
            if (_playMorph is null || _playMorphIcon != _playIcon)
            {
                _playMorph = new PlayMorph(_playIcon, IsPlaying);
                _playMorphIcon = _playIcon;
            }
            else _playMorph.To(IsPlaying);
        }
        if (_fullScreenIcon is not null) _fullScreenIcon.Data = IsFullScreen ? s_fullOff : s_full;
    }

    private void ShowVolumeLevel()
    {
        var percent = Math.Round(Volume * 100);
        _syncing = true;
        try
        {
            _volumeSlider.Value = Volume * 100;
        }
        finally
        {
            _syncing = false;
        }
        _volumeText.Text = percent.ToString(CultureInfo.InvariantCulture) + "%";
    }

    /// <summary>A speed as the button writes it: <c>1×</c>, <c>1.25×</c>.</summary>
    internal static string SpeedText(double speed) => speed.ToString("0.##", CultureInfo.InvariantCulture) + "×";

    private void MakeSpeeds()
    {
        var speeds = Speeds ?? s_speeds;
        if (_speedItems.Count != speeds.Count || _speedItems.Select(i => i.Tag).Cast<double>().Where((s, k) => s != speeds[k]).Any())
        {
            foreach (var old in _speedItems) old.Click -= OnSpeed;
            _speedItems.Clear();
            foreach (var s in speeds)
            {
                var item = new MenuItem { Header = SpeedText(s), Tag = s };
                item.Click += OnSpeed;
                _speedItems.Add(item);
            }
        }
        if (_speed is not null) _speed.Content = SpeedText(Speed);
    }

    private void OnSpeed(object? sender, RoutedEventArgs e)
    {
        if (sender is MenuItem { Tag: double s }) SetCurrentValue(SpeedProperty, s);
    }

    private void Words()
    {
        var words = Controls.Words.Of(this);
        var seconds = (int)Math.Round(SkipStep.TotalSeconds);
        Say(_volume, words[IsMuted ? "media.unmute" : "media.mute"]);
        _volumeLabel.Text = words["media.volume"];
        AutomationProperties.SetName(_volumeSlider, words["media.volume"]);
        Say(_speed, words["media.speed"]);
        Say(_back, words.Count("media.back", seconds, n => "رجوع " + ArabicSeconds(n)));
        Say(_forward, words.Count("media.forward", seconds, n => "تقديم " + ArabicSeconds(n)));
        Say(_play, words[IsPlaying ? "media.pause" : "media.play"]);
        Say(_subtitles, words["media.subtitles"]);
        Say(_subtitlesToggle, words["media.subtitles"]);
        Say(_fit, words["media.fill"]);
        Say(_fullScreen, words[IsFullScreen ? "media.exitFullScreen" : "media.fullScreen"]);
        Say(_more, words["media.more"]);
        var number = seconds.ToString(CultureInfo.InvariantCulture);
        if (_backSeconds is not null) _backSeconds.Text = number;
        if (_forwardSeconds is not null) _forwardSeconds.Text = number;
    }

    private static string ArabicSeconds(int n) => n switch
    {
        1 => "ثانية",
        2 => "ثانيتين",
        >= 3 and <= 10 => n.ToString(CultureInfo.InvariantCulture) + " ثوانٍ",
        _ => n.ToString(CultureInfo.InvariantCulture) + " ثانية",
    };

    private static void Say(Control? part, string words)
    {
        if (part is null) return;
        ToolTip.SetTip(part, words);
        AutomationProperties.SetName(part, words);
    }
}
