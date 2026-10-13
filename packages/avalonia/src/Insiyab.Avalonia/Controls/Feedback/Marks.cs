using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Primitives;

namespace Insiyab.Avalonia.Controls;

/// <summary>A status label, the web's <c>.ins-pill</c>. Tone classes <c>ok warn bad info brand</c> (none is neutral); <c>dot</c> adds the dot. A pressable one is a <c>Button</c> with the classes <c>pill</c>, a tone and <c>dot</c>.</summary>
public class Pill : ContentControl
{
}

/// <summary>A small count, the web's <c>.ins-badge</c>, in the brand tint or a tone class <c>ok warn bad mute</c>. It pops in when it appears and whenever its count changes.</summary>
public class Badge : ContentControl
{
    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        Pop();
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == ContentProperty && VisualRoot is not null) Pop();
    }

    private void Pop() => Entrance.Play(this, EntranceEffect.Pop);
}

/// <summary>A brand disc with one or two letters or an image in it, the web's <c>.ins-avatar</c>. Classes <c>sm lg</c>, and <c>more</c> for the "+4" that ends a group.</summary>
[PseudoClasses(":grouped")]
public class Avatar : ContentControl
{
    protected override void OnAttachedToLogicalTree(global::Avalonia.LogicalTree.LogicalTreeAttachmentEventArgs e)
    {
        base.OnAttachedToLogicalTree(e);
        PseudoClasses.Set(":grouped", Parent is AvatarGroup);
    }
}

/// <summary>Several avatars as one overlapping row, each ringed in the surface colour, the first in reading order on top: the web's <c>.ins-avatars</c>.</summary>
public class AvatarGroup : Panel
{
    public static readonly StyledProperty<double> OverlapProperty =
        AvaloniaProperty.Register<AvatarGroup, double>(nameof(Overlap), 8);

    static AvatarGroup() => AffectsMeasure<AvatarGroup>(OverlapProperty);

    /// <summary>How far each avatar tucks under the one before it.</summary>
    public double Overlap { get => GetValue(OverlapProperty); set => SetValue(OverlapProperty, value); }

    protected override Size MeasureOverride(Size availableSize)
    {
        double width = 0, height = 0;
        var visible = 0;
        foreach (var child in Children)
        {
            child.Measure(Size.Infinity);
            if (!child.IsVisible) continue;
            width += child.DesiredSize.Width - (visible > 0 ? Overlap : 0);
            height = Math.Max(height, child.DesiredSize.Height);
            visible++;
        }
        return new Size(Math.Max(0, width), height);
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        double x = 0;
        var count = Children.Count;
        for (var i = 0; i < count; i++)
        {
            var child = Children[i];
            child.ZIndex = count - i;
            if (!child.IsVisible) continue;
            var size = child.DesiredSize;
            child.Arrange(new Rect(x, (finalSize.Height - size.Height) / 2, size.Width, size.Height));
            x += size.Width - Overlap;
        }
        return finalSize;
    }
}

/// <summary>An amount and its currency, the web's <c>.ins-money</c>: the figure is its own LTR run so a minus sign stays on it, and the currency follows smaller and quieter. Classes <c>pos neg</c>.</summary>
[TemplatePart("PART_Currency", typeof(TextBlock))]
public class Money : TemplatedControl
{
    public static readonly StyledProperty<string?> ValueProperty =
        AvaloniaProperty.Register<Money, string?>(nameof(Value));

    public static readonly StyledProperty<string?> CurrencyProperty =
        AvaloniaProperty.Register<Money, string?>(nameof(Currency));

    public static readonly StyledProperty<double> CurrencyScaleProperty =
        AvaloniaProperty.Register<Money, double>(nameof(CurrencyScale), 0.68);

    public static readonly DirectProperty<Money, double> CurrencyFontSizeProperty =
        AvaloniaProperty.RegisterDirect<Money, double>(nameof(CurrencyFontSize), o => o.CurrencyFontSize);

    private double _currencyFontSize = 14 * 0.68;

    /// <summary>The figure as written, sign and separators included: <c>-1,500.00</c>.</summary>
    public string? Value { get => GetValue(ValueProperty); set => SetValue(ValueProperty, value); }

    public string? Currency { get => GetValue(CurrencyProperty); set => SetValue(CurrencyProperty, value); }

    /// <summary>The currency's size against the figure's, .68 as on the web.</summary>
    public double CurrencyScale { get => GetValue(CurrencyScaleProperty); set => SetValue(CurrencyScaleProperty, value); }

    /// <summary>The currency's font size, the figure's times <see cref="CurrencyScale"/>.</summary>
    public double CurrencyFontSize { get => _currencyFontSize; private set => SetAndRaise(CurrencyFontSizeProperty, ref _currencyFontSize, value); }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == FontSizeProperty || change.Property == CurrencyScaleProperty)
            CurrencyFontSize = FontSize * CurrencyScale;
    }

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);
        CurrencyFontSize = FontSize * CurrencyScale;
    }
}

/// <summary>A faded icon, a title, a hint and an optional action (the content): the web's <c>.ins-empty</c>.</summary>
public class EmptyState : ContentControl
{
    public static readonly StyledProperty<global::Avalonia.Media.Geometry?> IconProperty =
        AvaloniaProperty.Register<EmptyState, global::Avalonia.Media.Geometry?>(nameof(Icon));

    public static readonly StyledProperty<string?> TitleProperty =
        AvaloniaProperty.Register<EmptyState, string?>(nameof(Title));

    public static readonly StyledProperty<string?> HintProperty =
        AvaloniaProperty.Register<EmptyState, string?>(nameof(Hint));

    public global::Avalonia.Media.Geometry? Icon { get => GetValue(IconProperty); set => SetValue(IconProperty, value); }

    public string? Title { get => GetValue(TitleProperty); set => SetValue(TitleProperty, value); }

    public string? Hint { get => GetValue(HintProperty); set => SetValue(HintProperty, value); }
}
