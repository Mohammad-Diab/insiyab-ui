using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;

namespace Insiyab.Avalonia.Gallery.Playground;

/// <summary>Every option of a page's components, one collapsible box per component and one line per option; the web's <c>&lt;demo-options&gt;</c>.</summary>
public class OptionsList : Border
{
    public static readonly StyledProperty<PlayDef[]?> DefsProperty =
        AvaloniaProperty.Register<OptionsList, PlayDef[]?>(nameof(Defs));

    protected override Type StyleKeyOverride => typeof(Border);

    public OptionsList() => Classes.Add("panel");

    public PlayDef[]? Defs { get => GetValue(DefsProperty); set => SetValue(DefsProperty, value); }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property != DefsProperty) return;
        var boxes = new StackPanel();
        boxes.Classes.Add("accordion");
        foreach (var def in Defs ?? []) boxes.Children.Add(new Expander { Header = def.Title, Content = Table(def) });
        Child = boxes;
    }

    private static Grid Table(PlayDef def)
    {
        var grid = new Grid { ColumnDefinitions = new("Auto,Auto,*") };
        var row = 0;
        void Line(params Control[] cells)
        {
            grid.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
            for (var i = 0; i < cells.Length; i++)
            {
                Grid.SetRow(cells[i], row);
                Grid.SetColumn(cells[i], i);
                if (cells.Length == 1) Grid.SetColumnSpan(cells[i], 3);
                grid.Children.Add(cells[i]);
            }
            row++;
        }
        TextBlock Text(string text, string cls)
        {
            var t = new TextBlock { Text = text };
            t.Classes.Add(cls);
            return t;
        }

        Line(Text("الخيار", "opt-head"), Text("النوع", "opt-head"), Text("ما يفعله", "opt-head"));
        foreach (var (label, rows) in def.Groups())
        {
            Line(Text(label, "opt-group"));
            foreach (var r in rows)
            {
                var name = new Border { Child = new TextBlock { Text = r.Name, FlowDirection = FlowDirection.LeftToRight } };
                name.Classes.AddRange(["code", "opt-name"]);
                var desc = new TextBlock();
                desc.Classes.Add("opt-desc");
                PlayText.Fill(desc, r.Desc);
                var line = new Border();
                line.Classes.Add("opt-line");
                Grid.SetRow(line, row);
                Grid.SetColumnSpan(line, 3);
                grid.Children.Add(line);
                Line(name, Text(Play.Kind(r.Kind), "opt-kind"), desc);
            }
        }
        grid.HorizontalAlignment = HorizontalAlignment.Stretch;
        return grid;
    }
}
