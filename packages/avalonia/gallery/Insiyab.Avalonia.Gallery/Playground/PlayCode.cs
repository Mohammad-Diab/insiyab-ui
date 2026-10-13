using System.Text.RegularExpressions;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Layout;
using Avalonia.Markup.Xaml;
using Avalonia.Media;

namespace Insiyab.Avalonia.Gallery.Playground;

/// <summary>Turns a listing into controls at run time, so the preview is exactly the code shown, and colours the listing.</summary>
public static partial class PlayCode
{
    private const string Namespaces =
        "xmlns=\"https://github.com/avaloniaui\" xmlns:x=\"http://schemas.microsoft.com/winfx/2006/xaml\" xmlns:ins=\"using:Insiyab.Avalonia.Controls\"";

    /// <summary>Parses the code inside a panel of the given element and attributes; throws when the code is not valid.</summary>
    public static Panel Parse(string code, string panel = "Panel", string attributes = "") =>
        AvaloniaRuntimeXamlLoader.Parse<Panel>($"<{panel} {Namespaces} {attributes}>\n{code}\n</{panel}>", typeof(PlayCode).Assembly);

    /// <summary>The parsed code, or the parser's message in its place.</summary>
    public static Control Render(string code, string panel = "Panel", string attributes = "")
    {
        try
        {
            return Parse(code, panel, attributes);
        }
        catch (Exception e)
        {
            var text = new TextBlock
            {
                Text = e.Message,
                TextWrapping = TextWrapping.Wrap,
                FlowDirection = FlowDirection.LeftToRight,
                HorizontalAlignment = HorizontalAlignment.Stretch,
            };
            text.Classes.Add("play-error");
            return text;
        }
    }

    [GeneratedRegex("""<!--[\s\S]*?-->|</?[a-zA-Z][\w:.-]*(?:\s+[^\s=>/]+(?:\s*=\s*(?:"[^"]*"|'[^']*'|[^\s>]+))?)*\s*/?>""")]
    private static partial Regex Tag();

    [GeneratedRegex("""([^\s=]+)(\s*=\s*)?("[^"]*"|'[^']*'|[^\s>]+)?|\s+""")]
    private static partial Regex Attribute();

    // The mono font draws Arabic one letter per cell and breaks the joins, so Arabic runs take the Arabic face, as the web's listings do.
    [GeneratedRegex(@"[\u0600-\u06FF\u0750-\u077F\u08A0-\u08FF\uFB50-\uFDFF\uFE70-\uFEFC]+(?:[ .:]+[\u0600-\u06FF\u0750-\u077F\u08A0-\u08FF\uFB50-\uFDFF\uFE70-\uFEFC]+)*")]
    private static partial Regex Arabic();

    [GeneratedRegex("""^</?[a-zA-Z][\w:.-]*""")]
    private static partial Regex Head();

    [GeneratedRegex("""\s*/?>$""")]
    private static partial Regex Tail();

    /// <summary>Fills a TextBlock with the listing: tags, attribute names and values each in their own colour, as the web's listings.</summary>
    public static void Highlight(TextBlock target, string code)
    {
        var runs = target.Inlines ??= new InlineCollection();
        runs.Clear();
        void Add(string text, string? cls)
        {
            var at = 0;
            foreach (Match ar in Arabic().Matches(text))
            {
                Piece(text[at..ar.Index], cls, false);
                Piece(ar.Value, cls, true);
                at = ar.Index + ar.Length;
            }
            Piece(text[at..], cls, false);
        }
        void Piece(string text, string? cls, bool arabic)
        {
            if (text.Length == 0) return;
            var run = new Run(text);
            if (cls is not null) run.Classes.Add(cls);
            if (arabic) run.Classes.Add("ar");
            runs.Add(run);
        }
        var last = 0;
        foreach (Match m in Tag().Matches(code))
        {
            Add(code[last..m.Index], null);
            var tok = m.Value;
            if (tok.StartsWith("<!--")) Add(tok, "hl-c");
            else
            {
                var head = Head().Match(tok).Value;
                var tail = Tail().Match(tok).Value;
                Add(head, "hl-t");
                var body = tok[head.Length..^tail.Length];
                var at = 0;
                foreach (Match a in Attribute().Matches(body))
                {
                    Add(body[at..a.Index], null);
                    if (!a.Groups[1].Success) Add(a.Value, null);
                    else
                    {
                        Add(a.Groups[1].Value, "hl-a");
                        Add(a.Groups[2].Value, null);
                        Add(a.Groups[3].Value, "hl-s");
                    }
                    at = a.Index + a.Length;
                }
                Add(body[at..], null);
                Add(tail, "hl-t");
            }
            last = m.Index + m.Length;
        }
        Add(code[last..], null);
    }
}
