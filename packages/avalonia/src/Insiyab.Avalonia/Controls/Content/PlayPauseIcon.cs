using Avalonia;
using Avalonia.Controls;

namespace Insiyab.Avalonia.Controls;

/// <summary>The media row's play glyph on its own, for a play button anywhere (a phone player's large one over the picture): it turns into pause and back as <see cref="IsPlaying"/> changes, the triangle's two halves sliding apart into the two bars (at once in a still window or before it shows). Styled as an <see cref="Icon"/>; set <see cref="Icon.IsFilled"/> for the solid glyph.</summary>
public class PlayPauseIcon : Icon
{
    public static readonly StyledProperty<bool> IsPlayingProperty =
        AvaloniaProperty.Register<PlayPauseIcon, bool>(nameof(IsPlaying));

    private readonly PlayMorph _morph;

    public PlayPauseIcon() => _morph = new PlayMorph(this, false);

    /// <summary>Shows pause while true, play while false.</summary>
    public bool IsPlaying { get => GetValue(IsPlayingProperty); set => SetValue(IsPlayingProperty, value); }

    /// <summary>How far the glyph is from play (0) to pause (1).</summary>
    internal double Progress => _morph.Progress;

    protected override Type StyleKeyOverride => typeof(Icon);

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == IsPlayingProperty) _morph.To(change.GetNewValue<bool>());
    }
}
