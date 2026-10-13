using System.Globalization;

namespace Insiyab.Avalonia.Controls;

/// <summary>What a <see cref="Chart"/> draws: the web's <c>data-ins-chart</c> values.</summary>
public enum ChartType
{
    /// <summary>Bars per category, the series side by side (<see cref="Chart.Stacked"/> piles them).</summary>
    Bar,

    /// <summary>Bars along the line, the name first; for long names.</summary>
    HBar,

    /// <summary>Bars that touch, for a distribution; several series overlap, see-through.</summary>
    Histogram,

    /// <summary>A line, for what changes over time.</summary>
    Line,

    /// <summary>A line with its area filled.</summary>
    Area,

    /// <summary>Parts of a whole: the first series only, a legend with the values, the total in the middle.</summary>
    Donut,

    /// <summary>A line with no axes, the size of a word (a sparkline).</summary>
    Spark,
}

/// <summary>One series of a <see cref="Chart"/>: a name and a value per category, the web's table column.</summary>
public sealed class ChartSeries
{
    private IReadOnlyList<double> _data = [];

    public ChartSeries() { }

    public ChartSeries(string name, params double[] values)
    {
        Name = name;
        _data = values;
    }

    /// <summary>The name, shown in the legend and the tooltip.</summary>
    public string Name { get; set; } = "";

    /// <summary>The values, one per label.</summary>
    public IReadOnlyList<double> Data { get => _data; set => _data = value ?? []; }

    /// <summary>The values as text, comma separated ("120,150,98"), as <c>data-ins-values</c> takes them; Arabic digits are read too.</summary>
    public string Values
    {
        get => string.Join(",", _data.Select(v => v.ToString(CultureInfo.InvariantCulture)));
        set => _data = ParseList(value);
    }

    internal static double[] ParseList(string? text) =>
        string.IsNullOrWhiteSpace(text) ? [] : text.Split(',').Select(ParseNumber).ToArray();

    // A figure in either set of digits, with its separators and signs, like the web's num().
    internal static double ParseNumber(string? text)
    {
        if (string.IsNullOrWhiteSpace(text)) return double.NaN;
        var b = new System.Text.StringBuilder(text.Length);
        foreach (var ch in text)
        {
            if (ch is >= '٠' and <= '٩') b.Append((char)('0' + ch - '٠'));
            else if (ch is >= '۰' and <= '۹') b.Append((char)('0' + ch - '۰'));
            else if (ch == '٫') b.Append('.');
            else if (ch == '−') b.Append('-');
            else if (char.IsWhiteSpace(ch) || ch is ',' or '٬') continue;
            else b.Append(ch);
        }
        var s = b.ToString();
        var start = -1;
        for (var i = 0; i < s.Length; i++)
            if (char.IsAsciiDigit(s[i]) || s[i] == '.' && i + 1 < s.Length && char.IsAsciiDigit(s[i + 1])) { start = i; break; }
        if (start < 0) return double.NaN;
        var end = start;
        var dot = false;
        while (end < s.Length && (char.IsAsciiDigit(s[end]) || s[end] == '.' && !dot)) { if (s[end] == '.') dot = true; end++; }
        if (start > 0 && s[start - 1] == '-') start--;
        return double.TryParse(s[start..end], NumberStyles.Float, CultureInfo.InvariantCulture, out var v) ? v : double.NaN;
    }
}
