using System.Reflection;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Styling;
using Insiyab.Avalonia.Controls;
using Insiyab.Avalonia.Gallery;
using Insiyab.Avalonia.Gallery.Playground;
using static Insiyab.Avalonia.Tests.Ui;

namespace Insiyab.Avalonia.Tests;

// The look-class dispatcher as a whole: every look class has one theme, no global rule targets one, no page mixes two unrelated looks on one element.
public class ClassThemeTests
{
    private static readonly Assembly Gallery = typeof(PlayDef).Assembly;

    // The still and inactive rules keep their look-class lines until the last rollout step moves them into the themes.
    private static readonly string[] Pending = ["ins-still", "IsInactive"];

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

    [AvaloniaFact]
    public void Every_look_class_has_one_theme_of_its_type()
    {
        var all = ClassTheme.All().ToList();
        Assert.Contains(all, l => l.Type == typeof(Border) && l.Class == "stat");
        Assert.All(all, l => Assert.Equal(l.Type, l.Theme.TargetType));
        Assert.Equal(all.Count, all.Select(l => (l.Type, l.Class)).Distinct().Count());
    }

    [AvaloniaFact]
    public void No_global_rule_has_a_look_class_as_its_subject()
    {
        var found = ClassTheme.All().SelectMany(l => LookCheck.SubjectRules(l.Type, l.Class, Pending)).Distinct().ToList();
        Assert.True(found.Count == 0, string.Join("\n", found));
    }

    [AvaloniaFact]
    public void No_page_or_example_mixes_two_unrelated_looks_on_one_element()
    {
        LoadIcons();
        ClassTheme.Conflicts.Clear();
        _ = new GalleryShell();
        foreach (var page in GalleryPages.All) _ = page.Create();
        foreach (var (_, code) in Examples())
        {
            try { _ = PlayCode.Parse(code); }
            catch { }
        }
        Assert.True(ClassTheme.Conflicts.Count == 0, string.Join("\n", ClassTheme.Conflicts));
    }

    [AvaloniaFact]
    public void A_theme_the_app_sets_is_left_alone()
    {
        var own = new ControlTheme(typeof(Border)) { Setters = { new Setter(Border.PaddingProperty, new Thickness(3)) } };
        var tile = new Border { Classes = { "stat" }, Theme = own, Child = new DockPanel() };
        Show(tile, FlowDirection.RightToLeft);
        Assert.Same(own, tile.Theme);
        Assert.Equal(new Thickness(3), tile.Padding);
        tile.Classes.Add("ok");
        Settle();
        Assert.Same(own, tile.Theme);
    }

    [AvaloniaFact]
    public void A_look_that_nests_marks_itself_inside_the_same_look()
    {
        var theme = ClassTheme.All().First(l => l.Type == typeof(Border) && l.Class == "stat").Theme;
        ClassTheme.SetNests(theme, true);
        try
        {
            var inner = new Border { Classes = { "stat" }, Child = new DockPanel() };
            var outer = new Border { Classes = { "stat" }, Child = new StackPanel { Children = { inner } } };
            var other = new Border { Classes = { "stat-ico" }, Child = new StackPanel { Children = { new Border { Classes = { "stat" } } } } };
            Show(new StackPanel { Children = { outer, other } }, FlowDirection.RightToLeft);
            Assert.Contains(ClassTheme.NestedPseudoClass, inner.Classes);
            Assert.DoesNotContain(ClassTheme.NestedPseudoClass, outer.Classes);
            Assert.DoesNotContain(ClassTheme.NestedPseudoClass, ((Border)((StackPanel)other.Child!).Children[0]).Classes);
        }
        finally
        {
            ClassTheme.SetNests(theme, false);
        }
    }
}
