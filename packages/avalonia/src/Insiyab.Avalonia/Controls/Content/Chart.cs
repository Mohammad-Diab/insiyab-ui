using System.Globalization;
using Avalonia;
using Avalonia.Automation;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Input.Platform;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Metadata;

namespace Insiyab.Avalonia.Controls;

/// <summary>A small chart in the library's colours, the web's chart plugin (<c>.ins-chart</c>): <see cref="Type"/> picks bars, a line, a donut, a distribution or a spark. Data comes from <see cref="Series"/> and <see cref="Labels"/>, or one series from <see cref="Values"/>, or <see cref="SetData"/> from code. Categories run the way the control reads (from the right in RTL); pointing at a mark shows its value. Series colours are <see cref="Brush1"/> to <see cref="Brush6"/>: the brand first, then the tones.</summary>
[TemplatePart("PART_Plot", typeof(ChartPlot))]
[TemplatePart("PART_Tip", typeof(Border))]
[TemplatePart("PART_TipRows", typeof(StackPanel))]
[TemplatePart("PART_Legend", typeof(WrapPanel))]
public class Chart : TemplatedControl
{
    public static readonly StyledProperty<ChartType> TypeProperty =
        AvaloniaProperty.Register<Chart, ChartType>(nameof(Type));

    public static readonly StyledProperty<string?> LabelsProperty =
        AvaloniaProperty.Register<Chart, string?>(nameof(Labels));

    public static readonly StyledProperty<string?> ValuesProperty =
        AvaloniaProperty.Register<Chart, string?>(nameof(Values));

    public static readonly StyledProperty<string?> SeriesNameProperty =
        AvaloniaProperty.Register<Chart, string?>(nameof(SeriesName));

    public static readonly StyledProperty<bool> StackedProperty =
        AvaloniaProperty.Register<Chart, bool>(nameof(Stacked));

    public static readonly StyledProperty<string?> CenterProperty =
        AvaloniaProperty.Register<Chart, string?>(nameof(Center));

    public static readonly StyledProperty<string?> CenterLabelProperty =
        AvaloniaProperty.Register<Chart, string?>(nameof(CenterLabel));

    public static readonly StyledProperty<string?> CaptionProperty =
        AvaloniaProperty.Register<Chart, string?>(nameof(Caption));

    public static readonly StyledProperty<double> PlotHeightProperty =
        AvaloniaProperty.Register<Chart, double>(nameof(PlotHeight), double.NaN);

    public static readonly StyledProperty<IBrush?> Brush1Property = AvaloniaProperty.Register<Chart, IBrush?>(nameof(Brush1));
    public static readonly StyledProperty<IBrush?> Brush2Property = AvaloniaProperty.Register<Chart, IBrush?>(nameof(Brush2));
    public static readonly StyledProperty<IBrush?> Brush3Property = AvaloniaProperty.Register<Chart, IBrush?>(nameof(Brush3));
    public static readonly StyledProperty<IBrush?> Brush4Property = AvaloniaProperty.Register<Chart, IBrush?>(nameof(Brush4));
    public static readonly StyledProperty<IBrush?> Brush5Property = AvaloniaProperty.Register<Chart, IBrush?>(nameof(Brush5));
    public static readonly StyledProperty<IBrush?> Brush6Property = AvaloniaProperty.Register<Chart, IBrush?>(nameof(Brush6));
    public static readonly StyledProperty<IBrush?> GridBrushProperty = AvaloniaProperty.Register<Chart, IBrush?>(nameof(GridBrush));
    public static readonly StyledProperty<IBrush?> ZeroBrushProperty = AvaloniaProperty.Register<Chart, IBrush?>(nameof(ZeroBrush));
    public static readonly StyledProperty<IBrush?> AxisBrushProperty = AvaloniaProperty.Register<Chart, IBrush?>(nameof(AxisBrush));
    public static readonly StyledProperty<IBrush?> LabelBrushProperty = AvaloniaProperty.Register<Chart, IBrush?>(nameof(LabelBrush));
    public static readonly StyledProperty<IBrush?> TrackBrushProperty = AvaloniaProperty.Register<Chart, IBrush?>(nameof(TrackBrush));
    public static readonly StyledProperty<IBrush?> DotBrushProperty = AvaloniaProperty.Register<Chart, IBrush?>(nameof(DotBrush));
    public static readonly StyledProperty<IBrush?> TotalBrushProperty = AvaloniaProperty.Register<Chart, IBrush?>(nameof(TotalBrush));

    public static readonly StyledProperty<FontFamily> NumberFontFamilyProperty =
        AvaloniaProperty.Register<Chart, FontFamily>(nameof(NumberFontFamily), FontFamily.Default);

    private ChartPlot? _plot;
    private Border? _tip;
    private TextBlock? _tipLabel, _tipValue;
    private StackPanel? _tipRows;
    private int _kIndex = -1, _kSeries;
    private ContextMenu? _menu;
    private MenuItem? _copyValue, _copyData;
    private string? _menuValue;
    private WrapPanel? _legend;
    private (IReadOnlyList<string> Labels, IReadOnlyList<ChartSeries> Series)? _code;

    public Chart()
    {
        Series.CollectionChanged += (_, _) => Changed(true);
        AddHandler(ContextRequestedEvent, OnContextRequested);
    }

    public ChartType Type { get => GetValue(TypeProperty); set => SetValue(TypeProperty, value); }

    /// <summary>The category names, comma separated, as <c>data-ins-labels</c> or the table's first column.</summary>
    public string? Labels { get => GetValue(LabelsProperty); set => SetValue(LabelsProperty, value); }

    /// <summary>One series with no <see cref="Series"/>: its values, comma separated, as <c>data-ins-values</c>.</summary>
    public string? Values { get => GetValue(ValuesProperty); set => SetValue(ValuesProperty, value); }

    /// <summary>The name of the <see cref="Values"/> series, shown with the value under the pointer.</summary>
    public string? SeriesName { get => GetValue(SeriesNameProperty); set => SetValue(SeriesNameProperty, value); }

    /// <summary>The series, the web's table columns.</summary>
    [Content]
    public AvaloniaList<ChartSeries> Series { get; } = [];

    /// <summary>With <see cref="ChartType.Bar"/> or <see cref="ChartType.HBar"/>: the series piled into one bar.</summary>
    public bool Stacked { get => GetValue(StackedProperty); set => SetValue(StackedProperty, value); }

    /// <summary>A donut's middle, in place of the total.</summary>
    public string? Center { get => GetValue(CenterProperty); set => SetValue(CenterProperty, value); }

    /// <summary>A small line under a donut's total.</summary>
    public string? CenterLabel { get => GetValue(CenterLabelProperty); set => SetValue(CenterLabelProperty, value); }

    /// <summary>A short title under the chart, the web's <c>figcaption</c>.</summary>
    public string? Caption { get => GetValue(CaptionProperty); set => SetValue(CaptionProperty, value); }

    /// <summary>The plot's height, the web's <c>data-ins-height</c>; unset, 16rem (2rem for a spark).</summary>
    public double PlotHeight { get => GetValue(PlotHeightProperty); set => SetValue(PlotHeightProperty, value); }

    public IBrush? Brush1 { get => GetValue(Brush1Property); set => SetValue(Brush1Property, value); }
    public IBrush? Brush2 { get => GetValue(Brush2Property); set => SetValue(Brush2Property, value); }
    public IBrush? Brush3 { get => GetValue(Brush3Property); set => SetValue(Brush3Property, value); }
    public IBrush? Brush4 { get => GetValue(Brush4Property); set => SetValue(Brush4Property, value); }
    public IBrush? Brush5 { get => GetValue(Brush5Property); set => SetValue(Brush5Property, value); }
    public IBrush? Brush6 { get => GetValue(Brush6Property); set => SetValue(Brush6Property, value); }

    /// <summary>The grid lines.</summary>
    public IBrush? GridBrush { get => GetValue(GridBrushProperty); set => SetValue(GridBrushProperty, value); }

    /// <summary>The zero line.</summary>
    public IBrush? ZeroBrush { get => GetValue(ZeroBrushProperty); set => SetValue(ZeroBrushProperty, value); }

    /// <summary>The axis figures and the donut's small label.</summary>
    public IBrush? AxisBrush { get => GetValue(AxisBrushProperty); set => SetValue(AxisBrushProperty, value); }

    /// <summary>The category names.</summary>
    public IBrush? LabelBrush { get => GetValue(LabelBrushProperty); set => SetValue(LabelBrushProperty, value); }

    /// <summary>The donut's empty ring.</summary>
    public IBrush? TrackBrush { get => GetValue(TrackBrushProperty); set => SetValue(TrackBrushProperty, value); }

    /// <summary>The fill of a line's points.</summary>
    public IBrush? DotBrush { get => GetValue(DotBrushProperty); set => SetValue(DotBrushProperty, value); }

    /// <summary>The donut's total and the legend's values.</summary>
    public IBrush? TotalBrush { get => GetValue(TotalBrushProperty); set => SetValue(TotalBrushProperty, value); }

    /// <summary>The face of the figures (Latin digits read better in Inter, even in Arabic).</summary>
    public FontFamily NumberFontFamily { get => GetValue(NumberFontFamilyProperty); set => SetValue(NumberFontFamilyProperty, value); }

    /// <summary>Draws from data, the web's <c>Insiyab.chart(el, data)</c>; it takes the place of <see cref="Series"/> and <see cref="Labels"/> until <see cref="ClearData"/>.</summary>
    public void SetData(IEnumerable<string> labels, params ChartSeries[] series)
    {
        _code = (labels.ToArray(), series);
        Changed(true);
    }

    /// <summary>Goes back to <see cref="Series"/>, <see cref="Labels"/> and <see cref="Values"/>.</summary>
    public void ClearData()
    {
        _code = null;
        Changed(true);
    }

    internal IReadOnlyList<string> DataLabels { get; private set; } = [];

    internal IReadOnlyList<ChartSeries> DataSeries { get; private set; } = [];

    internal bool IsRtl => FlowDirection == FlowDirection.RightToLeft;

    internal IBrush? BrushAt(int i) => (i % 6) switch
    {
        0 => Brush1,
        1 => Brush2,
        2 => Brush3,
        3 => Brush4,
        4 => Brush5,
        _ => Brush6,
    };

    private void Rebuild()
    {
        IReadOnlyList<string> labels;
        IReadOnlyList<ChartSeries> series;
        if (_code is { } code)
        {
            labels = code.Labels;
            series = code.Series;
        }
        else
        {
            series = Series.Count > 0 ? Series.ToArray()
                : !string.IsNullOrWhiteSpace(Values) ? [new ChartSeries { Name = SeriesName ?? "", Values = Values! }]
                : [];
            var given = string.IsNullOrWhiteSpace(Labels) ? [] : Labels!.Split(',').Select(s => s.Trim()).ToArray();
            var count = series.Count > 0 ? series.Max(s => s.Data.Count) : given.Length;
            labels = Enumerable.Range(0, Math.Max(count, given.Length)).Select(i => i < given.Length ? given[i] : "").ToArray();
        }
        DataLabels = labels;
        DataSeries = series;
        if (series.Count > 0)
        {
            var first = series[0].Data;
            AutomationProperties.SetName(this, string.Join("، ", labels.Select((l, i) =>
                (l.Length > 0 ? l + ": " : "") + (i < first.Count ? Format(first[i], false) : ""))));
        }
    }

    private void Changed(bool data)
    {
        if (data)
        {
            Rebuild();
            _kIndex = -1;
            _kSeries = 0;
            _plot?.Clear();
        }
        BuildLegend();
        if (_plot is not null)
        {
            _plot.Height = ActualPlotHeight;
            if (data) _plot.Replay();
            _plot.ResetPens();
            _plot.InvalidateVisual();
        }
        HideTip();
    }

    internal double ActualPlotHeight => !double.IsNaN(PlotHeight) ? PlotHeight : Type == ChartType.Spark ? 32 : 256;

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);
        if (_plot is not null) _plot.Owner = null;
        _plot = e.NameScope.Find<ChartPlot>("PART_Plot");
        _tip = e.NameScope.Find<Border>("PART_Tip");
        _tipLabel = e.NameScope.Find<TextBlock>("PART_TipLabel");
        _tipValue = e.NameScope.Find<TextBlock>("PART_TipValue");
        _tipRows = e.NameScope.Find<StackPanel>("PART_TipRows");
        _legend = e.NameScope.Find<WrapPanel>("PART_Legend");
        if (_plot is not null) _plot.Owner = this;
        Changed(true);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        var p = change.Property;
        if (p == TypeProperty || p == LabelsProperty || p == ValuesProperty || p == SeriesNameProperty || p == StackedProperty)
            Changed(true);
        else if (p == PlotHeightProperty || p == CenterProperty || p == CenterLabelProperty || p == FlowDirectionProperty
                 || p == Brush1Property || p == Brush2Property || p == Brush3Property || p == Brush4Property || p == Brush5Property || p == Brush6Property
                 || p == GridBrushProperty || p == ZeroBrushProperty || p == AxisBrushProperty || p == LabelBrushProperty
                 || p == TrackBrushProperty || p == DotBrushProperty || p == TotalBrushProperty || p == NumberFontFamilyProperty || p == FontFamilyProperty)
            Changed(false);
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        _plot?.Replay();
    }

    // The legend: the series names, or a donut's categories with their values; none for one series or a spark.
    private void BuildLegend()
    {
        if (_legend is null) return;
        _legend.Children.Clear();
        var donut = Type == ChartType.Donut;
        var items = donut ? DataLabels : DataSeries.Count > 1 ? DataSeries.Select(s => s.Name).ToArray() : [];
        _legend.IsVisible = items.Count > 0 && Type != ChartType.Spark;
        _legend.HorizontalAlignment = donut ? HorizontalAlignment.Center : HorizontalAlignment.Stretch;
        if (!_legend.IsVisible) return;
        for (var i = 0; i < items.Count; i++)
        {
            var row = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 6 };
            row.Children.Add(new Border { Width = 10, Height = 10, CornerRadius = new CornerRadius(3), Background = BrushAt(i), VerticalAlignment = VerticalAlignment.Center, Classes = { "chart-key" } });
            row.Children.Add(new TextBlock { Text = items[i], VerticalAlignment = VerticalAlignment.Center });
            if (donut && DataSeries.Count > 0)
            {
                var data = DataSeries[0].Data;
                var v = i < data.Count ? data[i] : double.NaN;
                row.Children.Add(new TextBlock
                {
                    Text = double.IsNaN(v) ? "" : Format(v, false),
                    FontFamily = NumberFontFamily,
                    FontWeight = FontWeight.Bold,
                    Foreground = TotalBrush,
                    VerticalAlignment = VerticalAlignment.Center,
                    Classes = { "chart-key-value" },
                });
            }
            _legend.Children.Add(row);
        }
    }

    internal void ShowTip(string label, string name, double value, Rect mark)
    {
        if (_tip is null || _tipLabel is null || _tipValue is null || _plot is null) return;
        _tipLabel.Text = label + (name.Length > 0 && name != label ? " · " + name : "");
        _tipValue.Text = Format(value, false);
        _tipValue.IsVisible = true;
        if (_tipRows is not null) _tipRows.IsVisible = false;
        AutomationProperties.SetName(_tip, _tipLabel.Text + ": " + _tipValue.Text);
        _tip.IsVisible = true;
        _tip.Measure(Size.Infinity);
        var size = _tip.DesiredSize;
        var w = _plot.Bounds.Width;
        var x = Math.Max(4, Math.Min(w - size.Width - 4, mark.Center.X - size.Width / 2));
        var y = Math.Max(0, mark.Top - size.Height - 8);
        Canvas.SetLeft(_tip, x);
        Canvas.SetTop(_tip, y);
    }

    // One tip for a whole category: its label, then a row per series with a colour key, the name and the value.
    internal void ShowCategoryTip(int index, double x, double top)
    {
        if (_tip is null || _tipLabel is null || _tipValue is null || _tipRows is null || _plot is null) return;
        _tipLabel.Text = index < DataLabels.Count ? DataLabels[index] : "";
        _tipValue.IsVisible = false;
        _tipRows.IsVisible = true;
        _tipRows.Children.Clear();
        var said = new List<string> { _tipLabel.Text };
        for (var k = 0; k < DataSeries.Count; k++)
        {
            var data = DataSeries[k].Data;
            if (index >= data.Count || double.IsNaN(data[index])) continue;
            var row = new Grid { ColumnDefinitions = new ColumnDefinitions("Auto,*,Auto"), ColumnSpacing = 8 };
            row.Children.Add(new Border { Width = 8, Height = 8, CornerRadius = new CornerRadius(2), Background = BrushAt(k), VerticalAlignment = VerticalAlignment.Center });
            var name = new TextBlock { Text = DataSeries[k].Name, FontSize = 12, Foreground = _tipLabel.Foreground, VerticalAlignment = VerticalAlignment.Center };
            Grid.SetColumn(name, 1);
            row.Children.Add(name);
            var value = new TextBlock { Text = Format(data[index], false), FontFamily = NumberFontFamily, FontWeight = FontWeight.Bold, FontSize = 13, Foreground = _tipValue.Foreground, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(12, 0, 0, 0) };
            Grid.SetColumn(value, 2);
            row.Children.Add(value);
            _tipRows.Children.Add(row);
            said.Add(DataSeries[k].Name + ": " + value.Text);
        }
        AutomationProperties.SetName(_tip, string.Join("، ", said));
        _tip.IsVisible = true;
        _tip.Measure(Size.Infinity);
        var size = _tip.DesiredSize;
        var w = _plot.Bounds.Width;
        var left = IsRtl ? x - 12 - size.Width : x + 12;
        if (left < 4 || left + size.Width > w - 4) left = IsRtl ? x + 12 : x - 12 - size.Width;
        Canvas.SetLeft(_tip, Math.Max(4, Math.Min(w - size.Width - 4, left)));
        Canvas.SetTop(_tip, top);
    }

    internal void HideTip()
    {
        if (_tip is not null) _tip.IsVisible = false;
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        if (e.Handled || _plot is null || DataSeries.Count == 0 || Type == ChartType.Spark) return;
        if (e.Key == Key.C && (e.KeyModifiers & (KeyModifiers.Control | KeyModifiers.Meta)) != 0)
        {
            _ = CopyTextAsync(_plot.LitHit is { } hit ? hit.Value.ToString(CultureInfo.InvariantCulture) : ToTabSeparated());
            e.Handled = true;
            return;
        }
        var n = Type == ChartType.Donut ? DataSeries[0].Data.Count : DataLabels.Count;
        if (n == 0) return;
        if (e.Key == Key.Escape)
        {
            if (_kIndex < 0 && _plot.LitHit is null && _plot.CrossIndex < 0) return;
            ClearHighlight();
            e.Handled = true;
            return;
        }
        var cross = Type is ChartType.Line or ChartType.Area;
        var sets = DataSeries.Count;
        int index = Math.Max(0, _kIndex), series = _kSeries;
        var forward = IsRtl ? Key.Left : Key.Right;
        var back = IsRtl ? Key.Right : Key.Left;
        if (e.Key == Key.Home) index = 0;
        else if (e.Key == Key.End) index = n - 1;
        else if (Type == ChartType.Donut)
        {
            if (e.Key is Key.Right or Key.Down) index = (_kIndex + 1) % n;
            else if (e.Key is Key.Left or Key.Up) index = (index - 1 + n) % n;
            else return;
        }
        else if (Type == ChartType.HBar)
        {
            if (e.Key == Key.Down) index = Math.Min(n - 1, _kIndex + 1);
            else if (e.Key == Key.Up) index = Math.Max(0, index - 1);
            else if (e.Key == forward) series = (series + 1) % sets;
            else if (e.Key == back) series = (series - 1 + sets) % sets;
            else return;
        }
        else
        {
            if (e.Key == forward) index = Math.Min(n - 1, _kIndex + 1);
            else if (e.Key == back) index = Math.Max(0, index - 1);
            else if (!cross && e.Key == Key.Down) series = (series + 1) % sets;
            else if (!cross && e.Key == Key.Up) series = (series - 1 + sets) % sets;
            else return;
        }
        e.Handled = true;
        _kIndex = index;
        _kSeries = series;
        if (cross) _plot.LightCategory(index);
        else _plot.LightMark(Type == ChartType.Donut ? 0 : series, index);
    }

    protected override void OnLostFocus(FocusChangedEventArgs e)
    {
        base.OnLostFocus(e);
        // The keyboard's point stays while another window has the keyboard, so the arrows go on from it.
        if (!WindowActivity.GetIsInactive(this)) ClearHighlight();
    }

    internal void ClearHighlight()
    {
        _kIndex = -1;
        _kSeries = 0;
        _plot?.Clear();
    }

    internal ContextMenu? CopyMenu => _menu;

    // The built-in right-click menu: copy what is under the pointer, or the whole table; an app's own menu takes its place.
    private void OnContextRequested(object? sender, ContextRequestedEventArgs e)
    {
        if (e.Handled || ContextMenu is not null || ContextFlyout is not null || DataSeries.Count == 0 || Type == ChartType.Spark) return;
        e.Handled = true;
        var cross = _plot?.CrossIndex ?? -1;
        var many = _plot?.LitHit is null && cross >= 0 && DataSeries.Count > 1;
        _menuValue = _plot?.LitHit is { } hit ? hit.Value.ToString(CultureInfo.InvariantCulture)
            : cross >= 0 ? string.Join("\t", DataSeries.Select(s => cross < s.Data.Count && !double.IsNaN(s.Data[cross]) ? s.Data[cross].ToString(CultureInfo.InvariantCulture) : ""))
            : null;
        if (_menu is null)
        {
            _copyValue = new MenuItem();
            _copyValue.Click += (_, _) => { if (_menuValue is { } v) _ = CopyTextAsync(v); };
            _copyData = new MenuItem();
            _copyData.Click += (_, _) => _ = CopyTextAsync(ToTabSeparated());
            _menu = new ContextMenu { Items = { _copyValue, _copyData } };
        }
        var ar = IsRtl;
        _copyValue!.Header = ar ? (many ? "نسخ القيم" : "نسخ القيمة") : (many ? "Copy values" : "Copy value");
        _copyValue.IsEnabled = _menuValue is not null;
        _copyData!.Header = ar ? "نسخ البيانات" : "Copy data";
        _menu.FlowDirection = FlowDirection;
        _menu.Placement = e.TryGetPosition(this, out _) ? PlacementMode.Pointer : PlacementMode.Bottom;
        _menu.PlacementTarget = this;
        _menu.Open(this);
    }

    internal async Task CopyTextAsync(string text)
    {
        if (TopLevel.GetTopLevel(this)?.Clipboard is { } clipboard) await clipboard.SetTextAsync(text);
    }

    /// <summary>The data as tab-separated text, a header row of series names then one row per label, ready to paste into a spreadsheet.</summary>
    public string ToTabSeparated()
    {
        var b = new System.Text.StringBuilder();
        foreach (var s in DataSeries) b.Append('\t').Append(s.Name);
        for (var i = 0; i < DataLabels.Count; i++)
        {
            b.Append(Environment.NewLine).Append(DataLabels[i]);
            foreach (var s in DataSeries) b.Append('\t').Append(i < s.Data.Count && !double.IsNaN(s.Data[i]) ? s.Data[i].ToString(CultureInfo.InvariantCulture) : "");
        }
        return b.ToString();
    }

    // Figures with Latin digits; a compact one shortens thousands in the reading language, as Intl does.
    internal string Format(double v, bool compact)
    {
        if (!compact || Math.Abs(v) < 1000) return Math.Round(v, 2).ToString("#,##0.##", CultureInfo.InvariantCulture);
        var ar = IsRtl;
        var (div, en, word) = Math.Abs(v) >= 1e9 ? (1e9, "B", "مليار") : Math.Abs(v) >= 1e6 ? (1e6, "M", "مليون") : (1e3, "K", "ألف");
        var n = Math.Round(v / div, 1).ToString("0.#", CultureInfo.InvariantCulture);
        return ar ? n + " " + word : n + en;
    }
}
