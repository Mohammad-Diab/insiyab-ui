using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Data.Converters;

namespace Insiyab.Avalonia.Controls;

/// <summary>Draws a <see cref="MaskedTextBox"/>'s text with the mask's empty places (its <see cref="MaskedTextBox.PromptChar"/>) in the muted colour: the TextBox theme lays this ink over a see-through text presenter, in tabular figures, as the OTP and birth-date boxes do.</summary>
public static class MaskInk
{
    /// <summary>True for a <see cref="MaskedTextBox"/>: the TextBox template turns its ink on with it.</summary>
    public static readonly IValueConverter IsMasked = new FuncValueConverter<object?, bool>(o => o is MaskedTextBox);

    /// <summary>The field the ink copies, set by the TextBox template on its ink TextBlock.</summary>
    public static readonly AttachedProperty<TextBox?> SourceProperty =
        AvaloniaProperty.RegisterAttached<TextBlock, TextBox?>("Source", typeof(MaskInk));

    private static readonly AttachedProperty<IDisposable?> WatchProperty =
        AvaloniaProperty.RegisterAttached<TextBlock, IDisposable?>("Watch", typeof(MaskInk));

    static MaskInk()
    {
        SourceProperty.Changed.AddClassHandler<TextBlock>((ink, e) =>
        {
            ink.GetValue(WatchProperty)?.Dispose();
            ink.ClearValue(WatchProperty);
            if (e.GetNewValue<TextBox?>() is not MaskedTextBox box)
            {
                ink.Inlines?.Clear();
                return;
            }
            void Changed(object? sender, AvaloniaPropertyChangedEventArgs a)
            {
                if (a.Property == TextBox.TextProperty || a.Property == MaskedTextBox.PromptCharProperty) Fill(ink, box);
            }
            box.PropertyChanged += Changed;
            ink.SetValue(WatchProperty, new Unhook(() => box.PropertyChanged -= Changed));
            Fill(ink, box);
        });
    }

    public static TextBox? GetSource(TextBlock ink) => ink.GetValue(SourceProperty);

    public static void SetSource(TextBlock ink, TextBox? value) => ink.SetValue(SourceProperty, value);

    /// <summary>The text cut into runs, each empty place of the mask a run with the <c>prompt</c> class.</summary>
    public static IEnumerable<(string Text, bool Prompt)> Pieces(string? text, char prompt)
    {
        if (string.IsNullOrEmpty(text)) yield break;
        var start = 0;
        for (var i = 1; i <= text.Length; i++)
        {
            if (i < text.Length && (text[i] == prompt) == (text[start] == prompt)) continue;
            yield return (text[start..i], text[start] == prompt);
            start = i;
        }
    }

    private static void Fill(TextBlock ink, MaskedTextBox box)
    {
        var runs = ink.Inlines ??= [];
        runs.Clear();
        foreach (var (text, prompt) in Pieces(box.Text, box.PromptChar))
        {
            var run = new Run(text);
            if (prompt) run.Classes.Add("prompt");
            runs.Add(run);
        }
    }

    private sealed class Unhook(Action undo) : IDisposable
    {
        public void Dispose() => undo();
    }
}
