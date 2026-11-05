using System.Globalization;
using System.Runtime.CompilerServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Data;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace Insiyab.Avalonia.Controls;

/// <summary>The web's <c>dir="auto"</c>: a text takes the direction of its first letter (digits are not letters), right to left for Arabic or Hebrew, left to right for any other, and the direction around it when it has none. Bind a FlowDirection to a text through <see cref="Auto"/>, or set <see cref="IsAutoProperty"/> on a TextBlock; give the block <c>HorizontalAlignment="Left"</c> so it stays at its row's start edge.</summary>
public sealed class TextDirection : IValueConverter
{
    /// <summary>On a TextBlock (or SelectableTextBlock): its FlowDirection follows its own Text, so a cut text ends with «…» on its own end; a FlowDirection set on the block itself wins.</summary>
    public static readonly AttachedProperty<bool> IsAutoProperty =
        AvaloniaProperty.RegisterAttached<TextBlock, bool>("IsAuto", typeof(TextDirection));

    private static readonly ConditionalWeakTable<TextBlock, IDisposable> s_followed = new();

    static TextDirection()
    {
        IsAutoProperty.Changed.AddClassHandler<TextBlock>((block, e) =>
        {
            block.PropertyChanged -= OnBlockChanged;
            if (e.GetNewValue<bool>()) block.PropertyChanged += OnBlockChanged;
            FollowText(block);
        });
    }

    private TextDirection()
    {
    }

    /// <summary>The shared converter, for <c>{x:Static ins:TextDirection.Auto}</c>; a text with no letter leaves the direction as it was.</summary>
    public static TextDirection Auto { get; } = new();

    public static bool GetIsAuto(TextBlock block) => block.GetValue(IsAutoProperty);
    public static void SetIsAuto(TextBlock block, bool value) => block.SetValue(IsAutoProperty, value);

    private static void OnBlockChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property == TextBlock.TextProperty && sender is TextBlock block) FollowText(block);
    }

    // Set below a local value, so the block's own FlowDirection wins, and taken back when the text has no letter.
    private static void FollowText(TextBlock block)
    {
        if (s_followed.TryGetValue(block, out var held))
        {
            held.Dispose();
            s_followed.Remove(block);
        }
        if (!GetIsAuto(block) || Of(block.Text) is not { } direction) return;
        if (block.SetValue(Visual.FlowDirectionProperty, direction, BindingPriority.Style) is { } entry) s_followed.Add(block, entry);
    }

    /// <summary>The direction of <paramref name="text"/>'s first letter, or null when it has none.</summary>
    public static FlowDirection? Of(string? text)
    {
        if (string.IsNullOrEmpty(text)) return null;
        foreach (var c in text)
        {
            if (char.IsDigit(c) || c is '٫' or '٬') continue;
            if (IsRightToLeft(c)) return FlowDirection.RightToLeft;
            if (char.IsLetter(c)) return FlowDirection.LeftToRight;
        }
        return null;
    }

    private static readonly ConditionalWeakTable<TextBox, TextPresenter> s_presenters = new();
    private static bool s_registered;

    // Every one-line field lays typed text out in its own direction at the field's start edge, so the caret meets the end of Latin text in an Arabic field.
    internal static void Register()
    {
        if (s_registered) return;
        s_registered = true;
        TemplatedControl.TemplateAppliedEvent.AddClassHandler<TextBox>((box, e) =>
        {
            if (e is TemplateAppliedEventArgs applied && applied.NameScope.Find<TextPresenter>("PART_TextPresenter") is { } presenter) s_presenters.AddOrUpdate(box, presenter);
            Follow(box);
        });
        TextBox.TextProperty.Changed.AddClassHandler<TextBox>((box, _) => Follow(box));
        Visual.FlowDirectionProperty.Changed.AddClassHandler<TextBox>((box, _) => Follow(box));
        TextBox.TextAlignmentProperty.Changed.AddClassHandler<TextBox>((box, _) => Follow(box));
        TextBox.AcceptsReturnProperty.Changed.AddClassHandler<TextBox>((box, _) => Follow(box));
    }

    private static void Follow(TextBox box)
    {
        if (box is DateField or TimeField or MaskedTextBox || !s_presenters.TryGetValue(box, out var presenter)) return;
        if (box.AcceptsReturn)
        {
            presenter.ClearValue(Visual.FlowDirectionProperty);
            presenter.ClearValue(TextPresenter.TextAlignmentProperty);
            return;
        }
        FollowTyped(box, presenter, digitsLtr: false);
    }

    // A date or time field: digits and separators read left to right, so a line with no letters is laid out left to right, held at the field's own start edge.
    internal static void FollowTyped(TextBox box, TextPresenter? presenter, bool digitsLtr = true)
    {
        if (presenter is null) return;
        var own = box.FlowDirection;
        var typed = Of(box.Text) ?? (digitsLtr ? FlowDirection.LeftToRight : own);
        if (typed == own)
        {
            presenter.ClearValue(Visual.FlowDirectionProperty);
            presenter.ClearValue(TextPresenter.TextAlignmentProperty);
            return;
        }
        presenter.FlowDirection = own == FlowDirection.LeftToRight ? FlowDirection.RightToLeft : FlowDirection.LeftToRight;
        presenter.TextAlignment = box.TextAlignment switch
        {
            TextAlignment.Start or TextAlignment.Justify or TextAlignment.DetectFromContent => TextAlignment.End,
            TextAlignment.End => TextAlignment.Start,
            var a => a,
        };
    }

    /// <inheritdoc />
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var text = value switch
        {
            null => null,
            string s => s,
            Exception e => e.Message,
            _ => value.ToString(),
        };
        return Of(text) is { } direction ? direction : BindingOperations.DoNothing;
    }

    /// <inheritdoc />
    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => BindingOperations.DoNothing;

    private static bool IsRightToLeft(char c) => (int)c is (>= 0x0590 and <= 0x08FF) or (>= 0xFB1D and <= 0xFDFF) or (>= 0xFE70 and <= 0xFEFC);
}
