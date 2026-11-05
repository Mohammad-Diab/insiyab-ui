using Avalonia.Threading;
using Insiyab.Avalonia.Controls;

namespace Insiyab.Avalonia;

public partial class InsiyabTheme
{
    /// <summary>The language of the library's own words, as <see cref="SetLanguage"/> set it; null (the default) writes a control in Arabic when it reads right to left and in English otherwise.</summary>
    public static string? Language => Words.Language;

    /// <summary>The app's words for the library's own, by key (the package README lists the keys), as <see cref="SetLanguage"/> set them.</summary>
    public static IReadOnlyDictionary<string, string> Strings => Words.Strings;

    /// <summary>Writes every built-in word of the library in <paramref name="language"/> ("ar" for Arabic, any other code takes English), with <paramref name="strings"/> by key in place of the library's own, and rewrites them in every open window. Call on the UI thread.</summary>
    public static void SetLanguage(string? language, IReadOnlyDictionary<string, string>? strings = null) => Words.Set(language, strings);

    // The words a theme template shows, kept as resources so a language change reaches them.
    private void Reword() => Resources["InsWordToastClose"] = Words.Fixed["toast.close"];

    internal static void RewordThemes()
    {
        Dispatcher.UIThread.VerifyAccess();
        foreach (var theme in LiveThemes()) theme.Reword();
    }
}
