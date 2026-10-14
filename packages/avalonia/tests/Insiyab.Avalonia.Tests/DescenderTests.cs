using System.Reflection;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Headless.XUnit;
using Avalonia.Layout;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Media.TextFormatting;
using Avalonia.VisualTree;
using Insiyab.Avalonia.Gallery;
using Insiyab.Avalonia.Gallery.Playground;
using static Insiyab.Avalonia.Tests.Ui;

namespace Insiyab.Avalonia.Tests;

// Letters that hang below the line (ع ح ر ي م و) must not be cut by the box around them: a line that fits its clip keeps its descenders too.
public class DescenderTests
{
    private static readonly Assembly Gallery = typeof(PlayDef).Assembly;

    private static void LoadIcons()
    {
        var app = Application.Current!;
        if (app.TryGetResource("IconCheck", null, out _)) return;
        app.Resources.MergedDictionaries.Add((ResourceDictionary)AvaloniaXamlLoader.Load(new Uri("avares://Insiyab.Avalonia.Gallery/Icons.axaml")));
    }

    private static List<(string Owner, string Code)> Examples() =>
        Gallery.GetTypes()
            .Where(t => t.Namespace == typeof(PlayDef).Namespace)
            .SelectMany(t => t.GetFields(BindingFlags.Public | BindingFlags.Static).Where(f => f.FieldType == typeof(string) && f.IsLiteral && f.Name.EndsWith("Example", StringComparison.Ordinal))
                .Select(f => (Owner: $"{t.Name}.{f.Name}", Code: (string)f.GetRawConstantValue()!))
                .Concat(t.GetProperties(BindingFlags.Public | BindingFlags.Static).Where(p => p.PropertyType == typeof(string) && p.Name.EndsWith("Example", StringComparison.Ordinal))
                    .Select(p => (Owner: $"{t.Name}.{p.Name}", Code: (string)p.GetValue(null)!))))
            .ToList();

    private static bool HasArabic(string? s) => s is not null && s.Any(c => c is >= '؀' and <= 'ۿ');

    // Each text's line box and ink bottom in its own pixels, with the text it draws.
    private static IEnumerable<(Visual Owner, string Text, double LineBottom, double InkBottom)> Texts(Visual root)
    {
        foreach (var v in root.GetVisualDescendants().Prepend(root))
        {
            if (!v.IsEffectivelyVisible || v.Bounds.Height <= 0 || v.Bounds.Width <= 0) continue;
            TextLayout? layout = null;
            string? text = null;
            double top = 0;
            if (v is TextBlock tb && HasArabic(tb.Text ?? tb.Inlines?.Text))
            {
                layout = tb.TextLayout;
                text = tb.Text ?? tb.Inlines?.Text;
                top = tb.Padding.Top;
            }
            else if (v is TextPresenter tp && HasArabic(tp.Text))
            {
                layout = tp.TextLayout;
                text = tp.Text;
            }
            if (layout is null || layout.TextLines.Count == 0) continue;
            var line = layout.TextLines[^1];
            var lineBottom = top + layout.Height;
            yield return (v, text!, lineBottom, lineBottom + Math.Max(0, line.OverhangAfter));
        }
    }

    private static string Path(Visual v) =>
        string.Join(" < ", v.GetSelfAndVisualAncestors().Take(6).Select(a => a.GetType().Name + (a is StyledElement s && s.Classes.Count > 0 ? "." + string.Join(".", s.Classes) : "") + (a is Control { Name: { } n } ? "#" + n : "")));

    private static List<string> Cut(Visual root)
    {
        var cut = new List<string>();
        foreach (var (owner, text, lineBottom, inkBottom) in Texts(root))
        {
            if (inkBottom - lineBottom < 0.5) continue;
            var x = owner.Bounds.Width / 2;
            foreach (var clip in owner.GetSelfAndVisualAncestors().TakeWhile(a => a is not TopLevel).Where(a => a.ClipToBounds))
            {
                var line = owner.TranslatePoint(new Point(x, lineBottom), clip);
                var ink = owner.TranslatePoint(new Point(x, inkBottom), clip);
                if (line is null || ink is null) continue;
                var limit = clip.Bounds.Height + 0.5;
                if (line.Value.Y <= limit && ink.Value.Y > limit)
                {
                    cut.Add($"'{text}' in {Path(owner)}: cut {ink.Value.Y - clip.Bounds.Height:0.0}px by {Path(clip)}");
                    break;
                }
            }
        }
        return cut;
    }

    [AvaloniaFact]
    public void Tajawal_hangs_below_its_line_box()
    {
        var probe = new TextBlock { Text = "عيون حمراء مريم", FontSize = 14 };
        Show(probe, FlowDirection.RightToLeft);
        Assert.True(probe.TextLayout.TextLines[0].OverhangAfter > 0.5, $"overhang {probe.TextLayout.TextLines[0].OverhangAfter}");
    }

    [AvaloniaFact]
    public void No_example_cuts_Arabic_descenders()
    {
        LoadIcons();
        var all = new List<string>();
        foreach (var (owner, code) in Examples())
        {
            Control content;
            try { content = PlayCode.Parse(code); }
            catch { continue; }
            var host = new Border { Padding = new Thickness(24), Width = 760, HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Top, Child = content };
            Show(host, FlowDirection.RightToLeft, width: 820, height: 1400);
            all.AddRange(Cut(host).Select(c => $"{owner}: {c}"));
        }
        Assert.True(all.Count == 0, string.Join("\n", all.Distinct()));
    }

    [AvaloniaFact]
    public void The_gallery_shell_keeps_Arabic_descenders()
    {
        LoadIcons();
        var shell = new GalleryShell();
        var w = Show(shell, FlowDirection.RightToLeft, width: 1280, height: 860);
        Settle(1200);
        var cut = Cut(w);
        Assert.True(cut.Count == 0, string.Join("\n", cut.Distinct()));
    }
}
