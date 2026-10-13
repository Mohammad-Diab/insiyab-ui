using System.Globalization;
using Avalonia;
using Avalonia.Automation;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Metadata;
using Avalonia.Styling;

namespace Insiyab.Avalonia.Controls;

/// <summary>One answer column of a <see cref="ChoiceMatrix"/>: its heading, its value, and for the <c>cards</c> look a description and points.</summary>
public class MatrixColumn : AvaloniaObject
{
    public static readonly StyledProperty<string?> HeaderProperty =
        AvaloniaProperty.Register<MatrixColumn, string?>(nameof(Header));

    public static readonly StyledProperty<string?> ValueProperty =
        AvaloniaProperty.Register<MatrixColumn, string?>(nameof(Value));

    public static readonly StyledProperty<string?> DescriptionProperty =
        AvaloniaProperty.Register<MatrixColumn, string?>(nameof(Description));

    public static readonly StyledProperty<string?> PointsProperty =
        AvaloniaProperty.Register<MatrixColumn, string?>(nameof(Points));

    public string? Header { get => GetValue(HeaderProperty); set => SetValue(HeaderProperty, value); }

    /// <summary>The value a choice in this column stands for; numeric values add up in <see cref="ChoiceMatrix.Sum"/>.</summary>
    public string? Value { get => GetValue(ValueProperty); set => SetValue(ValueProperty, value); }

    /// <summary>The words in a card: the web's <c>.ins-matrix-cell</c> text.</summary>
    public string? Description { get => GetValue(DescriptionProperty); set => SetValue(DescriptionProperty, value); }

    /// <summary>The level's points, shown in a pill at the top of the card: <c>.ins-matrix-points</c>.</summary>
    public string? Points { get => GetValue(PointsProperty); set => SetValue(PointsProperty, value); }
}

/// <summary>One question row of a <see cref="ChoiceMatrix"/>.</summary>
public class MatrixRow : AvaloniaObject
{
    public static readonly StyledProperty<string?> HeaderProperty =
        AvaloniaProperty.Register<MatrixRow, string?>(nameof(Header));

    public static readonly StyledProperty<int> SelectedIndexProperty =
        AvaloniaProperty.Register<MatrixRow, int>(nameof(SelectedIndex), -1, defaultBindingMode: global::Avalonia.Data.BindingMode.TwoWay);

    public string? Header { get => GetValue(HeaderProperty); set => SetValue(HeaderProperty, value); }

    /// <summary>The chosen column, or -1.</summary>
    public int SelectedIndex { get => GetValue(SelectedIndexProperty); set => SetValue(SelectedIndexProperty, value); }
}

/// <summary>Rows of questions sharing one set of answers, the web's <c>.ins-matrix</c>: one radio group per row, moved along with the arrow keys. Classes: <c>cards</c> (each choice a card with words), <c>deselect</c> (a second click clears a choice).</summary>
public class ChoiceMatrix : TemplatedControl
{
    public static readonly DirectProperty<ChoiceMatrix, double> SumProperty =
        AvaloniaProperty.RegisterDirect<ChoiceMatrix, double>(nameof(Sum), o => o.Sum);

    public static readonly DirectProperty<ChoiceMatrix, bool> IsCompleteProperty =
        AvaloniaProperty.RegisterDirect<ChoiceMatrix, bool>(nameof(IsComplete), o => o.IsComplete);

    private readonly ColumnLayout _layout = new();
    private readonly List<List<RadioButton>> _radios = [];
    private readonly List<List<Border>> _cells = [];
    private Panel? _host;
    private double _sum;
    private bool _complete;
    private bool _building;
    private bool _cards;
    private (RadioButton Radio, bool Was)? _press;

    public ChoiceMatrix()
    {
        Columns.CollectionChanged += (_, _) => Build();
        Rows.CollectionChanged += (_, _) => Build();
        Classes.CollectionChanged += (_, _) =>
        {
            var cards = Classes.Contains("cards");
            if (cards == _cards) return;
            _cards = cards;
            Build();
        };
    }

    public AvaloniaList<MatrixColumn> Columns { get; } = [];

    [Content]
    public AvaloniaList<MatrixRow> Rows { get; } = [];

    /// <summary>The total of the chosen values: the web's <c>data-ins-sum</c>.</summary>
    public double Sum { get => _sum; private set => SetAndRaise(SumProperty, ref _sum, value); }

    /// <summary>Whether every row has an answer, for a matrix whose rows are all required.</summary>
    public bool IsComplete { get => _complete; private set => SetAndRaise(IsCompleteProperty, ref _complete, value); }

    /// <summary>Raised when a choice changes.</summary>
    public event EventHandler? SelectionChanged;

    private bool IsCards => Classes.Contains("cards");

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);
        _host = e.NameScope.Find<Panel>("PART_Host");
        Build();
    }

    private void Build()
    {
        if (_host is null) return;
        _building = true;
        _host.Children.Clear();
        _radios.Clear();
        _cells.Clear();
        var cards = IsCards;
        var radius = this.TryFindResource("InsRSm", out var r) && r is CornerRadius cr ? cr.TopLeft : 10;

        var head = new ColumnsPanel { Layout = _layout, Classes = { "matrix-head" } };
        head.Children.Add(new TextBlock());
        foreach (var column in Columns)
            head.Children.Add(new TextBlock { Text = column.Header, Classes = { "matrix-col" } });
        _host.Children.Add(head);

        for (var ri = 0; ri < Rows.Count; ri++)
        {
            var row = Rows[ri];
            var panel = new ColumnsPanel { Layout = _layout };
            KeyboardNavigation.SetTabNavigation(panel, KeyboardNavigationMode.Once);
            var th = new Border
            {
                Classes = { "matrix-th" },
                CornerRadius = cards ? new CornerRadius(radius) : new CornerRadius(radius, 0, 0, radius),
                Child = new TextBlock { Text = row.Header, VerticalAlignment = VerticalAlignment.Center },
            };
            panel.Children.Add(th);
            var radios = new List<RadioButton>();
            var cells = new List<Border>();
            var group = $"ins-matrix-{GetHashCode()}-{ri}";
            var thisRow = ri;
            for (var ci = 0; ci < Columns.Count; ci++)
            {
                var column = Columns[ci];
                var radio = new RadioButton { GroupName = group, IsChecked = row.SelectedIndex == ci };
                AutomationProperties.SetName(radio, $"{row.Header}: {column.Header}");
                if (cards)
                {
                    radio.Classes.Add("matrix-card");
                    if (this.TryFindResource("InsMatrixCard", out var theme) && theme is ControlTheme t) radio.Theme = t;
                    var body = new StackPanel { Spacing = 6 };
                    if (column.Points is { } points)
                        body.Children.Add(new Border { Classes = { "matrix-points" }, Child = new TextBlock { Text = points } });
                    body.Children.Add(new TextBlock { Text = column.Description, Classes = { "matrix-desc" } });
                    radio.Content = body;
                }
                else
                {
                    radio.Classes.Add("matrix-radio");
                }
                var rowIndex = ri;
                var colIndex = ci;
                radio.AddHandler(PointerPressedEvent, (_, _) => _press = (radio, radio.IsChecked == true), RoutingStrategies.Tunnel);
                radio.AddHandler(KeyDownEvent, (_, e) => _press = e.Key == Key.Space ? (radio, radio.IsChecked == true) : null, RoutingStrategies.Tunnel);
                radio.Click += (_, _) => OnClick(radio);
                radio.IsCheckedChanged += (_, _) => OnChecked(rowIndex, colIndex, radio);
                var cell = new Border { Classes = { "matrix-cell" }, Child = radio };
                if (!cards && ci == Columns.Count - 1) cell.CornerRadius = new CornerRadius(0, radius, radius, 0);
                if (radio.IsChecked == true) cell.Classes.Add("checked");
                radios.Add(radio);
                cells.Add(cell);
                panel.Children.Add(cell);
            }
            panel.AddHandler(KeyDownEvent, (_, e) => OnRowKey(thisRow, e), RoutingStrategies.Tunnel);
            _radios.Add(radios);
            _cells.Add(cells);
            _host.Children.Add(new Border { Classes = { "matrix-row" }, Child = panel });
        }
        _building = false;
        Update();
    }

    private bool Deselects()
    {
        for (StyledElement? e = this; e is not null; e = e.Parent)
            if (e.Classes.Contains("deselect")) return true;
        return false;
    }

    private void OnClick(RadioButton radio)
    {
        var press = _press;
        _press = null;
        if (press is { } p && p.Radio == radio && p.Was && Deselects()) radio.IsChecked = false;
    }

    private void OnChecked(int row, int col, RadioButton radio)
    {
        if (_building) return;
        _cells[row][col].Classes.Set("checked", radio.IsChecked == true);
        if (radio.IsChecked == true) Rows[row].SelectedIndex = col;
        else if (Rows[row].SelectedIndex == col) Rows[row].SelectedIndex = -1;
        Update();
        SelectionChanged?.Invoke(this, EventArgs.Empty);
    }

    private void OnRowKey(int row, KeyEventArgs e)
    {
        var radios = _radios[row];
        var current = radios.FindIndex(r => r.IsFocused);
        if (current < 0) return;
        var rtl = FlowDirection == FlowDirection.RightToLeft;
        if (e.Key is Key.Up or Key.Down)
        {
            e.Handled = true;
            var target = row + (e.Key == Key.Down ? 1 : -1);
            if (target < 0 || target >= _radios.Count) return;
            var chosen = Rows[target].SelectedIndex;
            _radios[target][chosen >= 0 && chosen < Columns.Count ? chosen : current].Focus(NavigationMethod.Directional);
            return;
        }
        var next = e.Key switch
        {
            Key.Home => 0,
            Key.End => radios.Count - 1,
            Key.Right => current + (rtl ? -1 : 1),
            Key.Left => current + (rtl ? 1 : -1),
            _ => int.MinValue,
        };
        if (next == int.MinValue) return;
        e.Handled = true;
        if (next < 0 || next >= radios.Count) return;
        radios[next].IsChecked = true;
        radios[next].Focus(NavigationMethod.Directional);
    }

    private void Update()
    {
        var sum = 0.0;
        var complete = Rows.Count > 0;
        foreach (var row in Rows)
        {
            if (row.SelectedIndex < 0 || row.SelectedIndex >= Columns.Count)
            {
                complete = false;
                continue;
            }
            if (double.TryParse(Columns[row.SelectedIndex].Value, NumberStyles.Float, CultureInfo.InvariantCulture, out var n)) sum += n;
        }
        Sum = Math.Round(sum * 100) / 100;
        IsComplete = complete;
    }
}
