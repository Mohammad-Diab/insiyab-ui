using System.Globalization;
using System.Text;
using Avalonia.Controls;
using Avalonia.Media;

namespace Insiyab.Avalonia.Controls;

// The plugins' shared text helpers: the page language and Arabic-Indic digits.
internal static class PluginText
{
    // The web reads <html lang>; here an explicit locale wins, else the control's direction decides (RTL is the Arabic page).
    public static bool IsArabic(Control control, string? locale) =>
        string.IsNullOrEmpty(locale)
            ? control.FlowDirection == FlowDirection.RightToLeft
            : locale.StartsWith("ar", StringComparison.OrdinalIgnoreCase);

    public static string Latin(string? s)
    {
        if (string.IsNullOrEmpty(s)) return string.Empty;
        var sb = new StringBuilder(s.Length);
        foreach (var c in s)
        {
            if (c >= '٠' && c <= '٩') sb.Append((char)('0' + (c - '٠')));
            else if (c >= '۰' && c <= '۹') sb.Append((char)('0' + (c - '۰')));
            else sb.Append(c);
        }
        return sb.ToString();
    }

    public static string Fill(string template, params (string Key, string Value)[] map)
    {
        foreach (var (key, value) in map)
            template = template.Replace("{" + key + "}", value);
        return template;
    }

    public static string Num(double value, int decimals) =>
        Math.Round(value, decimals).ToString(decimals == 0 ? "0" : "0.#", CultureInfo.InvariantCulture);
}
