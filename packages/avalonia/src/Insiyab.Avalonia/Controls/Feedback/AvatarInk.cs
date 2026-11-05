using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Controls.Templates;
using Avalonia.Media;

namespace Insiyab.Avalonia.Controls;

/// <summary>An avatar's letters, centred by their ink rather than their line box, so a tall «ل» or a low «ع» sits in the middle of the disc.</summary>
internal sealed class AvatarInk : Control
{
    public static readonly StyledProperty<string?> TextProperty =
        AvaloniaProperty.Register<AvatarInk, string?>(nameof(Text));

    public static readonly StyledProperty<FontFamily> FontFamilyProperty = TextElement.FontFamilyProperty.AddOwner<AvatarInk>();
    public static readonly StyledProperty<double> FontSizeProperty = TextElement.FontSizeProperty.AddOwner<AvatarInk>();
    public static readonly StyledProperty<FontWeight> FontWeightProperty = TextElement.FontWeightProperty.AddOwner<AvatarInk>();
    public static readonly StyledProperty<FontStyle> FontStyleProperty = TextElement.FontStyleProperty.AddOwner<AvatarInk>();
    public static readonly StyledProperty<IBrush?> ForegroundProperty = TextElement.ForegroundProperty.AddOwner<AvatarInk>();

    private FormattedText? _text;
    private Rect _ink;

    static AvatarInk()
    {
        AffectsMeasure<AvatarInk>(TextProperty, FontFamilyProperty, FontSizeProperty, FontWeightProperty, FontStyleProperty);
        AffectsRender<AvatarInk>(ForegroundProperty);
    }

    // Drawn left to right whatever the page: an Arabic run still reads right to left inside it, and "+4" keeps its sign first.
    public AvatarInk() => SetCurrentValue(FlowDirectionProperty, FlowDirection.LeftToRight);

    /// <summary>Shows text content as ink-centred letters and a control (a picture) as itself.</summary>
    public static IDataTemplate Template { get; } = new FuncDataTemplate<object?>((data, _) => data as Control ?? new AvatarInk { Text = data?.ToString() });

    public string? Text { get => GetValue(TextProperty); set => SetValue(TextProperty, value); }
    public FontFamily FontFamily { get => GetValue(FontFamilyProperty); set => SetValue(FontFamilyProperty, value); }
    public double FontSize { get => GetValue(FontSizeProperty); set => SetValue(FontSizeProperty, value); }
    public FontWeight FontWeight { get => GetValue(FontWeightProperty); set => SetValue(FontWeightProperty, value); }
    public FontStyle FontStyle { get => GetValue(FontStyleProperty); set => SetValue(FontStyleProperty, value); }
    public IBrush? Foreground { get => GetValue(ForegroundProperty); set => SetValue(ForegroundProperty, value); }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == TextProperty || change.Property == FontFamilyProperty || change.Property == FontSizeProperty
            || change.Property == FontWeightProperty || change.Property == FontStyleProperty || change.Property == ForegroundProperty)
            _text = null;
    }

    protected override Size MeasureOverride(Size availableSize) => Build() is null ? default : _ink.Size;

    public override void Render(DrawingContext context)
    {
        if (Build() is not { } text) return;
        context.DrawText(text, new Point((Bounds.Width - _ink.Width) / 2 - _ink.X, (Bounds.Height - _ink.Height) / 2 - _ink.Y));
    }

    private FormattedText? Build()
    {
        if (_text is not null || string.IsNullOrEmpty(Text)) return _text;
        _text = new FormattedText(Text, CultureInfo.CurrentUICulture, FlowDirection.LeftToRight, new Typeface(FontFamily, FontStyle, FontWeight), FontSize, Foreground);
        var ink = _text.BuildGeometry(default)?.Bounds ?? default;
        _ink = ink.Width > 0 && ink.Height > 0 ? ink : new Rect(0, 0, _text.WidthIncludingTrailingWhitespace, _text.Height);
        return _text;
    }
}
