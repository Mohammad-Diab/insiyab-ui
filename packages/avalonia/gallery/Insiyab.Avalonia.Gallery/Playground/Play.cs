using Avalonia.Controls;

namespace Insiyab.Avalonia.Gallery.Playground;

public enum OptionKind { Class, Property, Element, Event }

public enum ControlType { Select, Seg, Text, Switch }

/// <summary>One choice of a select or a segmented control; a choice with a Name is also a row of the options list.</summary>
public sealed record PlayChoice(string Value, string Label, string? Name = null, string? Desc = null, OptionKind Kind = OptionKind.Class);

/// <summary>A documented option that no control sets: a row of the options list only.</summary>
public sealed record PlayRow(string Group, string Name, string Desc, OptionKind Kind = OptionKind.Class, bool First = false);

/// <summary>One control of a playground: a select, a segmented choice, a text field or a switch. A Name makes the control itself a row of the options list, under its Group.</summary>
public sealed record PlayControl
{
    public required string Key { get; init; }
    public required string Label { get; init; }
    public required ControlType Type { get; init; }
    public string Value { get; init; } = "";
    public PlayChoice[] Choices { get; init; } = [];
    public string? Name { get; init; }
    public string? Desc { get; init; }
    public OptionKind Kind { get; init; } = OptionKind.Class;
    public string? Group { get; init; }
}

/// <summary>The values of a playground's controls: a choice's value, a field's text, or "true" for a switch that is on.</summary>
public sealed class PlayState(IReadOnlyDictionary<string, string> values)
{
    public string this[string key] => values.TryGetValue(key, out var v) ? v : "";
    public bool On(string key) => this[key] == "true";
    public bool Has(string key) => this[key].Length > 0;
    public IReadOnlyDictionary<string, string> Values => values;
}

/// <summary>A component's playground and its box in the options list, written once as data.</summary>
public sealed class PlayDef
{
    public required string Id { get; init; }
    public required string Title { get; init; }
    public PlayControl[] Controls { get; init; } = [];
    public PlayRow[] Extra { get; init; } = [];
    public required Func<PlayState, string> Axaml { get; init; }
    public Func<PlayState, IEnumerable<string>>? Disable { get; init; }
    public Func<PlayState, IEnumerable<string>>? Notes { get; init; }
    public Func<PlayState, bool>? Wide { get; init; }
    public double WideMax { get; init; } = double.PositiveInfinity;
    public Action<Control, PlayState, Action<string>>? Stage { get; init; }

    public PlayState Defaults() => State(Controls.ToDictionary(c => c.Key, c => c.Value));

    /// <summary>The state the code is built from, a field's text made safe for an attribute.</summary>
    public PlayState State(IReadOnlyDictionary<string, string> raw) =>
        new(Controls.ToDictionary(c => c.Key, c =>
        {
            var v = raw.TryGetValue(c.Key, out var x) ? x : c.Value;
            return c.Type == ControlType.Text ? Play.Esc(v) : v;
        }));

    /// <summary>Every combination of the choices and switches, with the fields at their defaults.</summary>
    public IEnumerable<PlayState> Combinations()
    {
        IEnumerable<Dictionary<string, string>> all = [new()];
        foreach (var c in Controls)
        {
            string[] values = c.Type switch
            {
                ControlType.Switch => ["", "true"],
                ControlType.Text => [c.Value],
                _ => c.Choices.Select(x => x.Value).ToArray(),
            };
            all = all.SelectMany(d => values.Select(v => new Dictionary<string, string>(d) { [c.Key] = v })).ToList();
        }
        return all.Select(State);
    }

    /// <summary>The options list's groups: one per control that documents something, then the extra rows.</summary>
    public List<(string Label, List<PlayRow> Rows)> Groups()
    {
        var groups = new List<(string Label, List<PlayRow> Rows)>();
        List<PlayRow> Group(string label, bool first)
        {
            var found = groups.FindIndex(g => g.Label == label);
            if (found >= 0) return groups[found].Rows;
            var g = (label, new List<PlayRow>());
            if (first) groups.Insert(0, g); else groups.Add(g);
            return g.Item2;
        }
        foreach (var c in Controls)
        {
            var rows = c.Name is null ? new List<PlayRow>() : [new(c.Group ?? "", c.Name, c.Desc ?? "", c.Kind)];
            rows.AddRange(c.Choices.Where(x => x.Name is not null).Select(x => new PlayRow(c.Group ?? "", x.Name!, x.Desc ?? "", x.Kind)));
            if (rows.Count > 0) Group(c.Group ?? (c.Type == ControlType.Switch ? "خيارات أخرى" : c.Label), false).AddRange(rows);
        }
        foreach (var x in Extra) Group(x.Group, x.First).Add(x);
        return groups;
    }
}

/// <summary>Short builders for the data files.</summary>
public static class Play
{
    public static PlayChoice Choice(string value, string label, string? name = null, string? desc = null, OptionKind kind = OptionKind.Class) =>
        new(value, label, name, desc, kind);

    public static PlayControl Select(string key, string label, string value, params PlayChoice[] choices) =>
        new() { Key = key, Label = label, Type = ControlType.Select, Value = value, Choices = choices };

    public static PlayControl Seg(string key, string label, string value, params PlayChoice[] choices) =>
        new() { Key = key, Label = label, Type = ControlType.Seg, Value = value, Choices = choices };

    public static PlayControl Text(string key, string label, string value, string? name = null, string? desc = null, OptionKind kind = OptionKind.Property, string? group = null) =>
        new() { Key = key, Label = label, Type = ControlType.Text, Value = value, Name = name, Desc = desc, Kind = kind, Group = group };

    /// <summary>The control's rows go under <paramref name="group"/> in the options list.</summary>
    public static PlayControl In(this PlayControl control, string group) => control with { Group = group };

    /// <summary>The control is a row of the options list itself: the option it sets and what it does.</summary>
    public static PlayControl Doc(this PlayControl control, string name, string desc, OptionKind kind = OptionKind.Property) =>
        control with { Name = name, Desc = desc, Kind = kind };

    public static PlayControl Switch(string key, string label, string? name = null, string? desc = null, OptionKind kind = OptionKind.Class, string? group = null, bool on = false) =>
        new() { Key = key, Label = label, Type = ControlType.Switch, Name = name, Desc = desc, Kind = kind, Group = group, Value = on ? "true" : "" };

    public static PlayRow Row(string group, string name, string desc, OptionKind kind = OptionKind.Class, bool first = false) =>
        new(group, name, desc, kind, first);

    /// <summary>A Classes attribute from the names given, empty when none is.</summary>
    public static string Classes(params string?[] names)
    {
        var list = string.Join(' ', names.Where(n => !string.IsNullOrEmpty(n)));
        return list.Length == 0 ? "" : $" Classes=\"{list}\"";
    }

    /// <summary>Text made safe for an attribute value.</summary>
    public static string Esc(string text) =>
        text.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;").Replace("\"", "&quot;");

    public static string Kind(OptionKind kind) => kind switch
    {
        OptionKind.Class => "كلاس",
        OptionKind.Property => "خاصية",
        OptionKind.Element => "عنصر",
        _ => "حدث",
    };
}
