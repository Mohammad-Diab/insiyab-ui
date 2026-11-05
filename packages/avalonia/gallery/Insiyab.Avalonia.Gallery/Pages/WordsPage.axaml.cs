using Avalonia.Controls;

namespace Insiyab.Avalonia.Gallery.Pages;

[GalleryPage("words", "كلمات المكتبة")]
public partial class WordsPage : UserControl
{
    public static string Code { get; } =
        """
        InsiyabTheme.SetLanguage("fr", new Dictionary<string, string>
        {
            ["text.copy"] = "Copier",
            ["page.next"] = "Suivant",
        });
        """;

    private static readonly Dictionary<string, string> French = new()
    {
        ["text.cut"] = "Couper",
        ["text.copy"] = "Copier",
        ["text.paste"] = "Coller",
        ["page.previous"] = "Précédent",
        ["page.next"] = "Suivant",
        ["page.compact"] = "{0} sur {1}",
        ["save.saved"] = "Enregistré",
    };

    public WordsPage()
    {
        InitializeComponent();
        Language.SelectionChanged += (_, _) =>
        {
            switch (Language.SelectedIndex)
            {
                case 1: InsiyabTheme.SetLanguage("ar"); break;
                case 2: InsiyabTheme.SetLanguage("en"); break;
                case 3: InsiyabTheme.SetLanguage("fr", French); break;
                default: InsiyabTheme.SetLanguage(null); break;
            }
        };
        DetachedFromVisualTree += (_, _) =>
        {
            if (InsiyabTheme.Language is not null || InsiyabTheme.Strings.Count > 0) InsiyabTheme.SetLanguage(null);
        };
    }
}
