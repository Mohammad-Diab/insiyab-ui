using System.Reflection;
using Avalonia.Controls;

namespace Insiyab.Avalonia.Gallery;

/// <summary>Marks a gallery page. <paramref name="slug"/> is the web docs page it mirrors (demo/src/pages/{slug}.html).</summary>
[AttributeUsage(AttributeTargets.Class)]
public sealed class GalleryPageAttribute(string slug, string title) : Attribute
{
    public string Slug { get; } = slug;
    public string Title { get; } = title;
}

/// <summary>A page in the gallery's sidebar: <see cref="Icon"/> is a key in Icons.axaml, <see cref="Fresh"/> shows the «جديد» badge.</summary>
public sealed record GalleryPage(string Slug, string Title, string Group, Type Type, string Icon, bool Fresh)
{
    public Control Create() => (Control)Activator.CreateInstance(Type)!;

    /// <summary>The page's English name, the web NAV's <c>en</c>, which the sidebar filter also finds it by.</summary>
    public string En => GalleryPages.English.GetValueOrDefault(Slug, Slug);
}

public static class GalleryPages
{
    // The web docs' NAV order (demo/src/site.mjs), plus the pages only Avalonia has.
    public static readonly (string Group, string[] Slugs)[] Nav =
    [
        ("التخصيص", ["color", "tokens", "fonts", "window"]),
        ("التخطيط", ["shell", "titlebar", "toolbar", "statusbar", "split"]),
        ("المحتوى", ["typography", "surfaces", "tables", "description", "quote"]),
        ("النماذج", ["inputs", "checks", "input-group", "pickers", "chips", "matrix", "validation"]),
        ("المكونات", ["buttons", "accordion", "tabs", "stats", "loading", "progress", "countdown", "save", "tooltips", "alerts", "dialog", "drawer", "toasts", "notify", "badges", "list", "comments", "dropdown", "steps", "pagination", "page-head"]),
        ("الإضافات", ["hijri", "palette", "otp", "phone", "birthdate", "file", "scrollspy", "timeline", "tree", "charts", "month"]),
        ("القوالب", ["login-simple", "login"]),
        ("الملحقات", ["carousel", "colorpicker", "reveal"]),
    ];

    // Each link's icon, the web NAV's `icon`; tokens, fonts, window, titlebar, toolbar and statusbar are Avalonia's own and borrow one from the sprite.
    private static readonly Dictionary<string, string> Icons = new()
    {
        ["color"] = "IconPalette", ["tokens"] = "IconSliders", ["fonts"] = "IconType", ["window"] = "IconWindow",
        ["shell"] = "IconLayout", ["titlebar"] = "IconWindow", ["toolbar"] = "IconWrench", ["statusbar"] = "IconInfo", ["split"] = "IconSidebar",
        ["typography"] = "IconType", ["surfaces"] = "IconLayers", ["tables"] = "IconTable", ["description"] = "IconList", ["quote"] = "IconMessage",
        ["inputs"] = "IconEdit", ["checks"] = "IconCheckSquare", ["input-group"] = "IconLink", ["pickers"] = "IconCalendar", ["chips"] = "IconTag", ["matrix"] = "IconGrid", ["validation"] = "IconShield",
        ["buttons"] = "IconPointer", ["accordion"] = "IconChevrons", ["tabs"] = "IconTabs", ["stats"] = "IconChart", ["loading"] = "IconLoader", ["progress"] = "IconGauge",
        ["countdown"] = "IconClock", ["save"] = "IconCheck", ["tooltips"] = "IconMessage", ["alerts"] = "IconAlert", ["dialog"] = "IconWindow", ["drawer"] = "IconSidebar",
        ["toasts"] = "IconBell", ["notify"] = "IconBell", ["badges"] = "IconTag", ["list"] = "IconList", ["comments"] = "IconInbox", ["dropdown"] = "IconMenuDown",
        ["steps"] = "IconSteps", ["pagination"] = "IconPages", ["page-head"] = "IconRoute",
        ["hijri"] = "IconMoon", ["palette"] = "IconSearch", ["otp"] = "IconLock", ["phone"] = "IconPhone", ["birthdate"] = "IconCalendar", ["file"] = "IconUpload",
        ["scrollspy"] = "IconList", ["timeline"] = "IconClock", ["tree"] = "IconFolder", ["charts"] = "IconChart", ["month"] = "IconCalendar",
        ["login-simple"] = "IconLock", ["login"] = "IconUser",
        ["carousel"] = "IconSlides", ["colorpicker"] = "IconDroplet", ["reveal"] = "IconSparkle",
    };

    // Each page's English name, the web NAV's `en`; tokens, fonts, window, titlebar, toolbar and statusbar are Avalonia's own.
    internal static readonly Dictionary<string, string> English = new()
    {
        ["index"] = "Introduction", ["start"] = "Quick start", ["javascript"] = "JavaScript", ["faq"] = "FAQ", ["color"] = "Color & branding", ["dark"] = "Dark mode",
        ["rtl"] = "Arabic & RTL", ["motion"] = "Motion", ["shell"] = "App shell", ["grid"] = "Grid", ["spacing"] = "Spacing & page layout", ["split"] = "Split view",
        ["typography"] = "Typography", ["surfaces"] = "Surfaces & panels", ["tables"] = "Tables", ["description"] = "Description list",
        ["quote"] = "Quote, reference & highlight", ["inputs"] = "Form controls", ["checks"] = "Checks & switches", ["input-group"] = "Input groups",
        ["pickers"] = "Pickers", ["chips"] = "Chips, tags & multi-select", ["matrix"] = "Choice matrix", ["validation"] = "Validation", ["buttons"] = "Buttons",
        ["accordion"] = "Accordion", ["tabs"] = "Tabs", ["stats"] = "Stat tiles", ["loading"] = "Loading & empty states", ["progress"] = "Progress",
        ["countdown"] = "Countdown timer", ["save"] = "Save status autosave", ["tooltips"] = "Tooltips", ["alerts"] = "Alerts", ["dialog"] = "Dialog",
        ["drawer"] = "Drawer", ["toasts"] = "Toasts", ["notify"] = "Notification center", ["badges"] = "Badges & numbers", ["list"] = "Lists",
        ["comments"] = "Comments & messages", ["dropdown"] = "Dropdowns", ["steps"] = "Steps & wizard", ["pagination"] = "Pagination",
        ["page-head"] = "Page header & breadcrumb", ["utilities"] = "Utilities", ["hijri"] = "Hijri calendar", ["palette"] = "Command palette", ["otp"] = "One-time code",
        ["phone"] = "Phone number", ["birthdate"] = "Birth date", ["file"] = "File upload", ["scrollspy"] = "Scrollspy", ["timeline"] = "Timeline", ["tree"] = "Tree",
        ["charts"] = "Charts", ["month"] = "Month calendar", ["login-simple"] = "Login template", ["login"] = "Login wizard template",
        ["tpl-dashboard"] = "Dashboard template", ["tpl-tickets"] = "List page template", ["tpl-order"] = "Detail page template", ["tpl-settings"] = "Settings template",
        ["tpl-review"] = "Criteria review template", ["tpl-booking"] = "Timed multi-step form template", ["tpl-error"] = "Error page template", ["navbar"] = "Navbar",
        ["carousel"] = "Carousel", ["colorpicker"] = "Color picker", ["reveal"] = "Entrance motion & scroll reveal", ["tokens"] = "Design tokens", ["fonts"] = "Fonts",
        ["window"] = "Window", ["titlebar"] = "Title bar", ["toolbar"] = "Toolbar", ["statusbar"] = "Status bar",
    };

    // The pages the web NAV flags `fresh`.
    private static readonly HashSet<string> Fresh =
    [
        "titlebar", "toolbar", "statusbar", "split", "tables", "description", "quote", "inputs", "pickers", "chips", "matrix", "stats", "countdown", "save", "notify", "badges", "comments", "steps",
        "birthdate", "charts", "month", "login-simple",
    ];

    public static IReadOnlyList<GalleryPage> All { get; } = Discover();

    public static GalleryPage? Find(string slug) => All.FirstOrDefault(p => p.Slug == slug);

    private static List<GalleryPage> Discover()
    {
        var found = typeof(GalleryPages).Assembly.GetTypes()
            .Select(t => (Type: t, Info: t.GetCustomAttribute<GalleryPageAttribute>()))
            .Where(x => x.Info is not null)
            .ToDictionary(x => x.Info!.Slug, x => x);
        var pages = new List<GalleryPage>();
        foreach (var (group, slugs) in Nav)
            foreach (var slug in slugs)
                if (found.Remove(slug, out var hit))
                    pages.Add(Page(slug, hit.Info!.Title, group, hit.Type));
        foreach (var (slug, hit) in found)
            pages.Add(Page(slug, hit.Info!.Title, "أخرى", hit.Type));
        return pages;
    }

    private static GalleryPage Page(string slug, string title, string group, Type type) =>
        new(slug, title, group, type, Icons.GetValueOrDefault(slug, "IconFile"), Fresh.Contains(slug));
}
