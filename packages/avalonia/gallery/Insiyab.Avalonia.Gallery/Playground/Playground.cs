using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Layout;
using Avalonia.LogicalTree;
using Insiyab.Avalonia.Controls;

namespace Insiyab.Avalonia.Gallery.Playground;

/// <summary>A live preview, the controls that change it and the AXAML that makes it, from one <see cref="PlayDef"/>; the web's <c>&lt;demo-play&gt;</c>.</summary>
public class Playground : Border
{
    public static readonly StyledProperty<PlayDef?> DefProperty =
        AvaloniaProperty.Register<Playground, PlayDef?>(nameof(Def));

    // Below this width the controls move under the preview.
    private const double NarrowBelow = 600;

    private readonly Grid _grid = new() { ColumnDefinitions = new("*,Auto"), RowDefinitions = new("Auto,Auto") };
    private readonly Border _view = new();
    private readonly StackPanel _notes = new();
    private readonly Border _controls = new();
    private readonly StackPanel _controlStack = new() { Spacing = 14 };
    private readonly PlayListing _listing = new();
    private readonly Dictionary<string, string> _values = [];
    private readonly Dictionary<string, Control> _boxes = [];
    private readonly List<Action> _resets = [];
    private TextBlock? _event;
    private bool _quiet;

    protected override Type StyleKeyOverride => typeof(Border);

    public Playground()
    {
        _view.Classes.Add("play-view");
        _notes.Classes.Add("play-notes");
        _controls.Classes.Add("play-controls");
        _controls.Child = _controlStack;
        _notes.SizeChanged += (_, _) => FitNotes();

        var stage = new Panel { Children = { _view, _notes } };
        Grid.SetColumn(_controls, 1);
        _grid.Children.Add(stage);
        _grid.Children.Add(_controls);
        var top = new Border { Child = _grid };
        top.Classes.Add("play-top");
        Child = new StackPanel { Children = { top, _listing } };
    }

    public PlayDef? Def { get => GetValue(DefProperty); set => SetValue(DefProperty, value); }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == DefProperty) Build();
        else if (change.Property == BoundsProperty) Arrange(Bounds.Width < NarrowBelow);
    }

    private void Arrange(bool narrow)
    {
        if (_controls.Classes.Contains("narrow") == narrow) return;
        _controls.Classes.Set("narrow", narrow);
        Grid.SetColumn(_controls, narrow ? 0 : 1);
        Grid.SetRow(_controls, narrow ? 1 : 0);
    }

    private void Build()
    {
        _controlStack.Children.Clear();
        _values.Clear();
        _boxes.Clear();
        _resets.Clear();
        if (Def is not { } def) return;
        foreach (var c in def.Controls)
        {
            _values[c.Key] = c.Value;
            var box = MakeControl(c);
            _boxes[c.Key] = box;
            _controlStack.Children.Add(box);
        }
        var reset = new Button { Content = "الافتراضي", HorizontalAlignment = HorizontalAlignment.Left, Margin = new Thickness(0, 4, 0, 0) };
        reset.Classes.AddRange(["ghost", "sm"]);
        reset.Click += (_, _) => Reset();
        _controlStack.Children.Add(reset);
        Render();
    }

    private Control MakeControl(PlayControl c)
    {
        var index = Math.Max(0, Array.FindIndex(c.Choices, x => x.Value == c.Value));
        Control input;
        switch (c.Type)
        {
            case ControlType.Switch:
            {
                var toggle = new ToggleSwitch { Content = c.Label, IsChecked = c.Value == "true" };
                toggle.IsCheckedChanged += (_, _) => Set(c.Key, toggle.IsChecked == true ? "true" : "");
                _resets.Add(() => toggle.IsChecked = c.Value == "true");
                return Box(toggle, null);
            }
            case ControlType.Select:
            {
                var combo = new ComboBox { ItemsSource = c.Choices.Select(x => x.Label).ToList(), SelectedIndex = index, HorizontalAlignment = HorizontalAlignment.Stretch };
                combo.Classes.Add("sm");
                combo.SelectionChanged += (_, _) => { if (combo.SelectedIndex >= 0) Set(c.Key, c.Choices[combo.SelectedIndex].Value); };
                _resets.Add(() => combo.SelectedIndex = index);
                input = combo;
                break;
            }
            case ControlType.Seg:
            {
                var seg = new Segmented
                {
                    HorizontalAlignment = HorizontalAlignment.Stretch,
                    ItemsPanel = new FuncTemplate<Panel?>(() => new UniformGrid { Rows = 1, ColumnSpacing = 3 }),
                };
                foreach (var x in c.Choices) seg.Items.Add(new SegmentedItem { Content = x.Label, Padding = new Thickness(6, 8) });
                seg.SelectedIndex = index;
                seg.SelectionChanged += (_, _) => { if (seg.SelectedIndex >= 0) Set(c.Key, c.Choices[seg.SelectedIndex].Value); };
                _resets.Add(() => seg.SelectedIndex = index);
                input = seg;
                break;
            }
            default:
            {
                var text = new TextBox { Text = c.Value };
                text.Classes.Add("sm");
                text.TextChanged += (_, _) => Set(c.Key, text.Text ?? "");
                _resets.Add(() => text.Text = c.Value);
                input = text;
                break;
            }
        }
        AutomationProperties.SetName(input, c.Label);
        return Box(input, c.Label);
    }

    private static Control Box(Control input, string? label)
    {
        var box = new StackPanel();
        box.Classes.Add("play-ctl");
        if (label is not null)
        {
            var head = new TextBlock { Text = label };
            head.Classes.Add("play-label");
            box.Children.Add(head);
        }
        box.Children.Add(input);
        return box;
    }

    private void Set(string key, string value)
    {
        if (_values.TryGetValue(key, out var old) && old == value) return;
        _values[key] = value;
        if (!_quiet) Render();
    }

    private void Reset()
    {
        _quiet = true;
        foreach (var reset in _resets) reset();
        _quiet = false;
        Render();
    }

    private void Render()
    {
        if (Def is not { } def) return;
        var s = def.State(_values);
        var off = def.Disable?.Invoke(s).ToHashSet() ?? [];
        foreach (var (key, box) in _boxes) box.IsEnabled = !off.Contains(key);

        var code = def.Axaml(s);
        var preview = PlayCode.Render(code);
        var wide = def.Wide?.Invoke(s) ?? false;
        preview.HorizontalAlignment = wide ? HorizontalAlignment.Stretch : HorizontalAlignment.Center;
        preview.VerticalAlignment = VerticalAlignment.Center;
        if (wide) preview.MaxWidth = def.WideMax;
        // A link in the preview shows what it is without leaving the gallery, as the web's preview does.
        foreach (var link in preview.GetLogicalDescendants().OfType<HyperlinkButton>()) link.NavigateUri = null;
        _view.Child = preview;
        _event = null;
        _notes.Children.Clear();
        foreach (var note in def.Notes?.Invoke(s) ?? []) _notes.Children.Add(Note(note));
        if (preview is Panel) def.Stage?.Invoke(preview, s, Say);
        _listing.Code = code;
    }

    private static TextBlock Note(string markup)
    {
        var line = new TextBlock();
        line.Classes.Add("play-note");
        PlayText.Fill(line, markup);
        return line;
    }

    private void Say(string markup)
    {
        if (_event is null)
        {
            _event = Note(markup);
            _event.Classes.Add("play-event");
            _notes.Children.Add(_event);
        }
        else PlayText.Fill(_event, markup);
    }

    // The notes sit at the foot of the preview, so the preview keeps as much room above and below its component as they take.
    private void FitNotes()
    {
        var room = _notes.Children.Count == 0 ? 0 : _notes.Bounds.Height + 24;
        var pad = Math.Max(64, room);
        if (_view.Padding.Top != pad) _view.Padding = new Thickness(24, pad);
    }
}
