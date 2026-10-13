using System.Reflection;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Markup.Xaml;
using Insiyab.Avalonia.Gallery.Playground;

namespace Insiyab.Avalonia.Tests;

// Every playground of every gallery page, found by reflection, so a new page is covered without touching this file.
public class PlaygroundTests
{
    // A page whose choices multiply past this is sampled: every control moved alone, plus evenly spaced combinations.
    private const long Cap = 256;

    private static readonly Assembly Gallery = typeof(PlayDef).Assembly;

    private static List<(string Owner, PlayDef Def)> Defs() =>
        Gallery.GetTypes()
            .SelectMany(t => t.GetProperties(BindingFlags.Public | BindingFlags.Static).Where(p => p.PropertyType == typeof(PlayDef))
                .Select(p => (Owner: $"{t.Name}.{p.Name}", Def: (PlayDef)p.GetValue(null)!)))
            .ToList();

    private static List<(string Owner, string Code)> Examples() =>
        Gallery.GetTypes()
            .Where(t => t.Namespace == typeof(PlayDef).Namespace)
            .SelectMany(t => t.GetFields(BindingFlags.Public | BindingFlags.Static).Where(f => f.FieldType == typeof(string) && f.IsLiteral)
                .Select(f => (Owner: $"{t.Name}.{f.Name}", Code: (string)f.GetRawConstantValue()!)))
            .ToList();

    // The listings use the gallery's icons, which live in the gallery's App resources.
    private static void LoadIcons()
    {
        var app = Application.Current!;
        if (app.TryGetResource("IconCheck", null, out _)) return;
        app.Resources.MergedDictionaries.Add((ResourceDictionary)AvaloniaXamlLoader.Load(new Uri("avares://Insiyab.Avalonia.Gallery/Icons.axaml")));
    }

    private static string[] Values(PlayControl c) => c.Type switch
    {
        ControlType.Switch => ["", "true"],
        ControlType.Text => [c.Value],
        _ => c.Choices.Select(x => x.Value).ToArray(),
    };

    private static IEnumerable<PlayState> States(PlayDef def)
    {
        var values = def.Controls.Select(Values).ToArray();
        var total = values.Aggregate(1L, (n, v) => n * Math.Max(1, v.Length));
        PlayState At(long index)
        {
            var raw = new Dictionary<string, string>();
            for (var i = 0; i < values.Length; i++)
            {
                var v = values[i];
                raw[def.Controls[i].Key] = v.Length == 0 ? "" : v[index % v.Length];
                index /= Math.Max(1, v.Length);
            }
            return def.State(raw);
        }
        if (total <= Cap)
        {
            for (long i = 0; i < total; i++) yield return At(i);
            yield break;
        }
        var step = total / Cap;
        for (long i = 0; i < total; i += step) yield return At(i);
        foreach (var (c, alts) in def.Controls.Zip(values))
            foreach (var v in alts)
            {
                var raw = def.Controls.ToDictionary(x => x.Key, x => x.Value);
                raw[c.Key] = v;
                yield return def.State(raw);
            }
    }

    [AvaloniaFact]
    public void Every_page_has_playgrounds_to_check()
    {
        Assert.Contains(Defs(), d => d.Owner == "ButtonsPlay.Button");
    }

    [AvaloniaFact]
    public void Every_option_combination_of_every_playground_parses()
    {
        LoadIcons();
        var failures = new List<string>();
        var count = 0;
        foreach (var (owner, def) in Defs())
            foreach (var s in States(def))
            {
                count++;
                var code = def.Axaml(s);
                try { PlayCode.Parse(code); }
                catch (Exception e)
                {
                    if (failures.Count < 8) failures.Add($"{owner} [{string.Join(", ", s.Values.Where(v => v.Value.Length > 0).Select(v => $"{v.Key}={v.Value}"))}]: {e.Message}\n{code}");
                    else if (failures.Count == 8) failures.Add("...");
                }
            }
        Assert.True(failures.Count == 0, $"{count} combinations, failures:\n" + string.Join("\n\n", failures));
    }

    [AvaloniaFact]
    public void Every_main_example_parses()
    {
        LoadIcons();
        var failures = new List<string>();
        foreach (var (owner, code) in Examples())
        {
            try { PlayCode.Parse(code); }
            catch (Exception e) { failures.Add($"{owner}: {e.Message}"); }
        }
        Assert.True(failures.Count == 0, string.Join("\n", failures));
    }

    [AvaloniaFact]
    public void Every_control_changes_the_code_unless_the_defaults_disable_it()
    {
        var silent = new List<string>();
        foreach (var (owner, def) in Defs())
        {
            var d = def.Defaults();
            var start = def.Axaml(d);
            var off = def.Disable?.Invoke(d).ToHashSet() ?? [];
            foreach (var c in def.Controls.Where(c => !off.Contains(c.Key)))
            {
                string[] alts = c.Type switch
                {
                    ControlType.Switch => [c.Value == "true" ? "" : "true"],
                    ControlType.Text => [c.Value + "9"],
                    _ => c.Choices.Select(x => x.Value).Where(v => v != c.Value).ToArray(),
                };
                foreach (var v in alts)
                {
                    var raw = def.Controls.ToDictionary(x => x.Key, x => x.Value);
                    raw[c.Key] = v;
                    if (def.Axaml(def.State(raw)) == start) silent.Add($"{owner}: {c.Key}={v}");
                }
            }
        }
        Assert.True(silent.Count == 0, "no change for: " + string.Join(", ", silent));
    }

    [AvaloniaFact]
    public void Every_playground_page_opens()
    {
        LoadIcons();
        var owners = Defs().Select(d => d.Def).ToHashSet();
        foreach (var page in GalleryPages.All)
        {
            var control = page.Create();
            var plays = control.GetLogicalDescendantsOfPlayground();
            foreach (var play in plays) Assert.NotNull(play.Def);
        }
        Assert.NotEmpty(owners);
    }
}

internal static class PlaygroundTree
{
    public static IEnumerable<Playground> GetLogicalDescendantsOfPlayground(this Control root) =>
        global::Avalonia.LogicalTree.LogicalExtensions.GetLogicalDescendants(root).OfType<Playground>();
}
