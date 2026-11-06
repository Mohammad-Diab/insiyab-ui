using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;

namespace Insiyab.Avalonia.Controls;

/// <summary>A rounded tile of the dark media stage around a picture (a camera, a still, a <see cref="MediaSurface"/>), with an optional <see cref="Caption"/> on a dark note at its foot and <see cref="IsMirrored"/> for a camera showing its own person.</summary>
[PseudoClasses(":captioned", ":mirrored")]
public class MediaTile : ContentControl
{
    /// <summary>The words at the tile's foot, on the media note: a name, a number.</summary>
    public static readonly StyledProperty<object?> CaptionProperty =
        AvaloniaProperty.Register<MediaTile, object?>(nameof(Caption));

    public static readonly StyledProperty<bool> IsMirroredProperty =
        AvaloniaProperty.Register<MediaTile, bool>(nameof(IsMirrored));

    public object? Caption { get => GetValue(CaptionProperty); set => SetValue(CaptionProperty, value); }

    /// <summary>Flips the picture left to right; the caption stays as it reads.</summary>
    public bool IsMirrored { get => GetValue(IsMirroredProperty); set => SetValue(IsMirroredProperty, value); }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == CaptionProperty) PseudoClasses.Set(":captioned", change.NewValue is not null);
        else if (change.Property == IsMirroredProperty) PseudoClasses.Set(":mirrored", IsMirrored);
    }
}
