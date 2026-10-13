using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Documents;

namespace Insiyab.Avalonia.Gallery.Playground;

/// <summary>A line of docs prose in which `backticks` mark code, as in the web's play files.</summary>
public class PlayText : TextBlock
{
    public static readonly StyledProperty<string?> MarkupProperty =
        AvaloniaProperty.Register<PlayText, string?>(nameof(Markup));

    protected override Type StyleKeyOverride => typeof(TextBlock);

    public string? Markup { get => GetValue(MarkupProperty); set => SetValue(MarkupProperty, value); }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == MarkupProperty) Fill(this, Markup ?? "");
    }

    // Avalonia ignores bidi isolates, so a left-to-right run takes LRM marks to keep its order in Arabic; word joiners and no-break spaces keep it on one line.
    public static string Code(string code)
    {
        var whole = string.Join("\u2060", code.Select(ch => ch == ' ' ? '\u00A0' : ch));
        var mark = code.Any(ch => ch is >= '\u0600' and <= '\u06FF') ? "" : "\u200E";
        return mark + "\u202F" + whole + "\u202F" + mark;
    }

    public static void Fill(TextBlock target, string markup)
    {
        var runs = target.Inlines ??= new InlineCollection();
        runs.Clear();
        var parts = markup.Split('`');
        for (var i = 0; i < parts.Length; i++)
        {
            if (parts[i].Length == 0) continue;
            if (i % 2 == 0) { runs.Add(new Run(parts[i])); continue; }
            var code = new Run(Code(parts[i]));
            code.Classes.Add("code");
            runs.Add(code);
        }
    }
}
