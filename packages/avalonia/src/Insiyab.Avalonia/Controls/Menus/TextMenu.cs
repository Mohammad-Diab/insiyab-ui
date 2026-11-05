using Avalonia.Controls;
using Avalonia.Media;

namespace Insiyab.Avalonia.Controls;

/// <summary>The right-click menu of a text: a pop menu whose words follow the direction it opens in.</summary>
internal sealed class TextMenu : PopMenu
{
    public TextMenu() => Opening += (_, _) =>
    {
        var ar = Target?.FlowDirection == FlowDirection.RightToLeft;
        foreach (var item in Items.OfType<MenuItem>())
            if (item.Tag is string key) item.Header = Word(key, ar);
    };

    // The library's own words for its text menus, in one place; a string table in the app's language is to take their place.
    internal static string Word(string key, bool ar) => key switch
    {
        "cut" => ar ? "قص" : "Cut",
        "copy" => ar ? "نسخ" : "Copy",
        "paste" => ar ? "لصق" : "Paste",
        _ => key,
    };
}
