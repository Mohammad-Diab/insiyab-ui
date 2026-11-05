using Avalonia.Controls;

namespace Insiyab.Avalonia.Controls;

/// <summary>The right-click menu of a text: a pop menu whose words follow the app's language, or the direction it opens in.</summary>
internal sealed class TextMenu : PopMenu
{
    public TextMenu() => Opening += (_, _) =>
    {
        var words = Words.Of(Target);
        foreach (var item in Items.OfType<MenuItem>())
            if (item.Tag is string key) item.Header = words["text." + key];
    };
}
