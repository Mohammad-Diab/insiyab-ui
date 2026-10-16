using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Documents;
using Avalonia.Markup.Xaml.Styling;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.VisualTree;
using Insiyab.Avalonia.Controls;
using static Insiyab.Avalonia.Tests.Ui;

namespace Insiyab.Avalonia.Tests;

// Shared by the look-class tests: a text dump of what styles decide under a root, kept beside its picture, and the global rules.
public static class LookCheck
{
    private static string F(double d) => double.IsNaN(d) ? "NaN" : Math.Round(d, 2).ToString(CultureInfo.InvariantCulture);

    private static string B(IBrush? brush) => brush switch
    {
        null => "-",
        ISolidColorBrush s => s.Color + "@" + F(s.Opacity),
        IGradientBrush g => g.GetType().Name + "(" + string.Join(" ", g.GradientStops.Select(x => x.Color + ":" + F(x.Offset))) + ")@" + F(g.Opacity),
        _ => brush.GetType().Name,
    };

    private static string M(ITransform? t) => t is null ? "-" : string.Join(",", new[] { t.Value.M11, t.Value.M12, t.Value.M21, t.Value.M22, t.Value.M31, t.Value.M32 }.Select(F));

    /// <summary>Every control under the root, in tree order, with what styles decide on it.</summary>
    public static string Dump(Visual root)
    {
        var sb = new StringBuilder();
        foreach (var v in root.GetSelfAndVisualDescendants())
        {
            if (v is FocusRing || v is not Control c) continue;
            var at = c.TranslatePoint(default, root) ?? default;
            sb.Append(v.GetType().Name).Append(" [").Append(string.Join(" ", c.Classes.Where(k => !k.StartsWith(':')).OrderBy(k => k, StringComparer.Ordinal))).Append("] ")
                .Append(F(at.X)).Append(',').Append(F(at.Y)).Append(' ').Append(F(c.Bounds.Width)).Append('x').Append(F(c.Bounds.Height))
                .Append(" vis=").Append(c.IsEffectivelyVisible ? 1 : 0).Append(" op=").Append(F(c.Opacity)).Append(" m=").Append(c.Margin)
                .Append(" ha=").Append(c.HorizontalAlignment).Append(" va=").Append(c.VerticalAlignment).Append(" dock=").Append(DockPanel.GetDock(c))
                .Append(" rt=").Append(M(c.RenderTransform)).Append(" tr=").Append(c.Transitions?.Count ?? 0).Append(" cur=").Append(c.Cursor?.ToString() ?? "-");
            if (v is Border b)
                sb.Append(" bg=").Append(B(b.Background)).Append(" bd=").Append(B(b.BorderBrush)).Append(" bt=").Append(b.BorderThickness)
                    .Append(" cr=").Append(b.CornerRadius).Append(" pad=").Append(b.Padding).Append(" sh=").Append(b.BoxShadow)
                    .Append(" fg=").Append(B(b.GetValue(TextElement.ForegroundProperty))).Append(" fs=").Append(F(b.GetValue(TextElement.FontSizeProperty)))
                    .Append(" fw=").Append(b.GetValue(TextElement.FontWeightProperty)).Append(" ff=").Append(b.GetValue(TextElement.FontFamilyProperty));
            if (v is TextBlock t)
                sb.Append(" text=\"").Append(t.Text).Append("\" fs=").Append(F(t.FontSize)).Append(" fw=").Append(t.FontWeight).Append(" ff=").Append(t.FontFamily)
                    .Append(" feat=").Append(t.FontFeatures is null ? "-" : string.Join(",", t.FontFeatures.Select(x => x.ToString())))
                    .Append(" fg=").Append(B(t.Foreground)).Append(" bg=").Append(B(t.Background)).Append(" lh=").Append(F(t.LineHeight)).Append(" ls=").Append(F(t.LetterSpacing))
                    .Append(" ta=").Append(t.TextAlignment).Append(" trim=").Append(t.TextTrimming).Append(" wrap=").Append(t.TextWrapping)
                    .Append(" pad=").Append(t.Padding).Append(" fd=").Append(t.FlowDirection).Append(" clip=").Append(t.ClipToBounds);
            if (v is StackPanel s)
                sb.Append(" sp=").Append(F(s.Spacing)).Append(" or=").Append(s.Orientation);
            if (v is DockPanel d)
                sb.Append(" hs=").Append(F(d.HorizontalSpacing)).Append(" vs=").Append(F(d.VerticalSpacing));
            if (v is Grid g)
                sb.Append(" cs=").Append(F(g.ColumnSpacing)).Append(" rs=").Append(F(g.RowSpacing)).Append(" col=").Append(Grid.GetColumn(c));
            if (v is Panel p)
                sb.Append(" pbg=").Append(B(p.Background));
            if (v is TemplatedControl tc)
                sb.Append(" tfg=").Append(B(tc.Foreground)).Append(" tbg=").Append(B(tc.Background)).Append(" tfs=").Append(F(tc.FontSize)).Append(" tpad=").Append(tc.Padding).Append(" tcr=").Append(tc.CornerRadius);
            sb.Append('\n');
        }
        return sb.ToString();
    }

    /// <summary>The window's picture and the root's dump under the shots folder, for the before and after comparison.</summary>
    public static void Keep(Window w, Visual root, string name)
    {
        Capture(w, name);
        File.WriteAllText(System.IO.Path.Combine(ShotsDir(), name + ".txt"), Dump(root));
    }

    /// <summary>Every global Style's selector, as Avalonia prints it.</summary>
    public static IEnumerable<string> GlobalSelectors() => Walk(Application.Current!.Styles);

    private static IEnumerable<string> Walk(IStyle style)
    {
        if (style is StyleInclude include)
        {
            if (include.Loaded is { } loaded)
                foreach (var s in Walk(loaded)) yield return s;
            yield break;
        }
        if (style is Style { Selector: { } selector }) yield return selector.ToString();
        if (style is ControlTheme) yield break;
        foreach (var kid in style.Children)
            foreach (var s in Walk(kid)) yield return s;
    }

    private static readonly HashSet<string> Plain = ["Border", "TextBlock", "StackPanel", "Panel", "Grid", "DockPanel"];

    // Scoped under a control type (Prose, Table, SplitView...): the rule leaves no activator on elements outside that control.
    private static bool Scoped(string alt)
    {
        var first = Regex.Match(alt, @"^([\w|]+)[^\s>]*[\s>]");
        return first.Success && !Plain.Contains(first.Groups[1].Value.Split('|')[^1]);
    }

    /// <summary>The global selectors whose subject, the part after the last combinator, is the type with this class, leaving out rules scoped under a control type and rules that contain one of the given strings.</summary>
    public static List<string> SubjectRules(Type type, string cls, params string[] skip)
    {
        var subject = new Regex(@"(?:^|[\s>])" + Regex.Escape(type.Name) + @"(?:\.[\w-]+|:[\w-]+(?:\([^)]*\))?|\[[^\]]*\])*\." + Regex.Escape(cls) + @"(?![\w-])(?:\.[\w-]+|:[\w-]+(?:\([^)]*\))?|\[[^\]]*\])*\s*$");
        return GlobalSelectors()
            .SelectMany(s => s.Split(", ").Select(alt => alt.Trim()))
            .Where(alt => subject.IsMatch(alt) && !Scoped(alt) && !skip.Any(k => alt.Contains(k, StringComparison.Ordinal)))
            .Distinct().OrderBy(s => s, StringComparer.Ordinal).ToList();
    }
}
