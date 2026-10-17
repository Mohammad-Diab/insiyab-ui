using System.Reflection;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Markup.Xaml;
using Avalonia.VisualTree;
using Insiyab.Avalonia.Controls;
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

    // Only a member named as an example is XAML; a C# listing beside it (Code, Server, Page) is not.
    private static bool IsExample(string name) => name.EndsWith("Example", StringComparison.Ordinal);

    private static List<(string Owner, string Code)> Examples() =>
        Gallery.GetTypes()
            .Where(t => t.Namespace == typeof(PlayDef).Namespace)
            .SelectMany(t => t.GetFields(BindingFlags.Public | BindingFlags.Static).Where(f => f.FieldType == typeof(string) && f.IsLiteral && IsExample(f.Name))
                .Select(f => (Owner: $"{t.Name}.{f.Name}", Code: (string)f.GetRawConstantValue()!))
                .Concat(t.GetProperties(BindingFlags.Public | BindingFlags.Static).Where(p => p.PropertyType == typeof(string) && IsExample(p.Name))
                    .Select(p => (Owner: $"{t.Name}.{p.Name}", Code: (string)p.GetValue(null)!))))
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
        var examples = Examples();
        Assert.True(examples.Count >= 50, $"only {examples.Count} examples found");
        Assert.DoesNotContain(examples, e => e.Owner.EndsWith(".Code"));
        foreach (var (owner, code) in examples)
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

    // A select, a segmented choice or a field can sit under a group of its own and document an option itself.
    [Fact]
    public void Controls_take_a_group_a_name_and_a_description()
    {
        var def = new PlayDef
        {
            Id = "t",
            Title = "t",
            Controls =
            [
                Play.Seg("min", "أقل", "", Play.Choice("", "بلا"), Play.Choice("18", "18", "MinAge=18", "أقل عمر.", OptionKind.Property)).In("العمر"),
                Play.Select("max", "أكبر", "").Doc("MaxAge", "أكبر عمر.").In("العمر"),
                Play.Text("title", "العنوان", "عنوان", "Title", "عنوان الحوار."),
            ],
            Axaml = _ => "<TextBlock />",
        };
        var groups = def.Groups();
        var age = Assert.Single(groups, g => g.Label == "العمر").Rows;
        Assert.Equal(["MinAge=18", "MaxAge"], age.Select(r => r.Name));
        var title = Assert.Single(groups, g => g.Label == "العنوان").Rows;
        Assert.Equal(("Title", "عنوان الحوار.", OptionKind.Property), (title[0].Name, title[0].Desc, title[0].Kind));
    }

    // A code run reads left to right inside Arabic (LRM marks, as Avalonia ignores bidi isolates) and never breaks across lines.
    [Fact]
    public void A_code_run_stays_whole_and_in_order()
    {
        const char lrm = (char)0x200E, joiner = (char)0x2060, isolate = (char)0x2066;
        var code = PlayText.Code("Classes=a b x-y");
        Assert.Equal(lrm, code[0]);
        Assert.Equal(lrm, code[^1]);
        Assert.DoesNotContain(' ', code);
        Assert.DoesNotContain(isolate, code);
        Assert.Contains($"C{joiner}l{joiner}a", code);
        Assert.Contains($"x{joiner}-{joiner}y", code);
        Assert.DoesNotContain(lrm, PlayText.Code("الأولى"));
    }

    // A main example's stage wires each rendering of it, the first and any after its code changes.
    [AvaloniaFact]
    public void A_stage_wires_every_rendering_of_the_example()
    {
        var seen = new List<Control>();
        var example = new PlayExample { Code = "<Button Content=\"أ\" />" };
        example.Stage = seen.Add;
        Assert.Single(seen);
        Assert.IsType<Button>(Assert.IsAssignableFrom<Panel>(seen[0]).Children.Single());
        example.Code = "<CheckBox />";
        Assert.Equal(2, seen.Count);
        Assert.IsType<CheckBox>(((Panel)seen[1]).Children.Single());
    }
    // The toolbar playground with «الحذف في القائمة وحدها» on, drawn the way the stage draws it: the others stay in the row.
    [AvaloniaTheory]
    [InlineData(false)]
    [InlineData(true)]
    public void The_toolbar_play_keeps_its_row_with_delete_in_the_menu(bool disabled)
    {
        LoadIcons();
        var def = ToolbarPlay.Toolbar;
        var raw = def.Controls.ToDictionary(c => c.Key, c => c.Value);
        raw["always"] = "true";
        if (disabled) raw["disabled"] = "true";
        var preview = PlayCode.Render(def.Axaml(def.State(raw)));
        preview.HorizontalAlignment = global::Avalonia.Layout.HorizontalAlignment.Stretch;
        preview.VerticalAlignment = global::Avalonia.Layout.VerticalAlignment.Center;
        var page = new Border { Padding = new Thickness(24, 64), Child = preview };
        Ui.Show(page, global::Avalonia.Media.FlowDirection.RightToLeft, width: 900, height: 400);
        var bar = preview.GetVisualDescendants().OfType<Insiyab.Avalonia.Controls.Toolbar>().Single();
        var commands = bar.Items.Where(c => c is not Separator).ToList();
        var delete = commands.OfType<ToolbarButton>().Single(b => b.Label == "حذف");
        ToolbarTests.AssertRowKeepsItsCommands(bar, commands.Where(c => c != delete).ToArray(), delete, disabled);
    }

    // The preview holds still while a note comes and goes: its share changes, a note is raised, and the split stays where it was.
    [AvaloniaFact]
    public void The_split_play_holds_still_while_its_share_changes()
    {
        LoadIcons();
        var page = Insiyab.Avalonia.Gallery.GalleryPages.Find("split")!.Create();
        var window = Ui.Show(new ScrollViewer { Content = page }, global::Avalonia.Media.FlowDirection.RightToLeft, width: 1100, height: 900);
        Ui.Settle(1000);
        var play = page.GetVisualDescendants().OfType<Insiyab.Avalonia.Gallery.Playground.Playground>().First(p => p.Def == SplitPlay.Split);
        var vertical = play.GetVisualDescendants().OfType<ToggleSwitch>().First(t => (string?)t.Content == "لوح فوق لوح");
        vertical.IsChecked = true;
        Ui.Settle(300);
        var shares = play.GetVisualDescendants().OfType<Insiyab.Avalonia.Controls.Segmented>().First(s => s.Items.OfType<Insiyab.Avalonia.Controls.SegmentedItem>().Any(i => (string?)i.Content == "70%"));
        double? first = null;
        for (var i = 0; i < 3; i++)
        {
            shares.SelectedIndex = i;
            Ui.Settle(300);
            var split = play.GetVisualDescendants().OfType<Insiyab.Avalonia.Controls.Split>().Single();
            var share = new[] { 30.0, 50.0, 70.0 }[i];
            Assert.Equal(share, split.Share, 0);
            if (first is { } held)
            {
                var before = split.TranslatePoint(new Point(0, 0), window)!.Value.Y;
                Assert.True(Math.Abs(before - held) <= 0.5, $"the split moved to {before} when its share became {share}, it was at {held}");
            }
            split.RaiseEvent(new Insiyab.Avalonia.Controls.SplitShareEventArgs(Insiyab.Avalonia.Controls.Split.ShareCommittedEvent, split, split.Share));
            Ui.Settle(300);
            var y = split.TranslatePoint(new Point(0, 0), window)!.Value.Y;
            first ??= y;
            Assert.True(Math.Abs(y - first.Value) <= 0.5, $"the split is at {y} with share {share}, it was at {first}");
            foreach (var scroll in split.GetVisualDescendants().OfType<ScrollViewer>())
                Assert.True(scroll.Extent.Height <= scroll.Viewport.Height + 0.5, $"a pane scrolls at share {share}: {scroll.Extent.Height} in {scroll.Viewport.Height}");
        }
    }
}
