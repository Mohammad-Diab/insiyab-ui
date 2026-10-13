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
}

public static class GalleryPages
{
    // The web docs' NAV order (demo/src/site.mjs), plus the pages only Avalonia has.
    public static readonly (string Group, string[] Slugs)[] Nav =
    [
        ("التخصيص", ["color", "tokens", "fonts", "window"]),
        ("التخطيط", ["shell", "split"]),
        ("المحتوى", ["typography", "surfaces", "tables", "description", "quote"]),
        ("النماذج", ["inputs", "checks", "input-group", "pickers", "chips", "matrix", "validation"]),
        ("المكونات", ["buttons", "accordion", "tabs", "stats", "loading", "progress", "countdown", "save", "tooltips", "alerts", "dialog", "drawer", "toasts", "notify", "badges", "list", "comments", "dropdown", "steps", "pagination", "page-head"]),
        ("الإضافات", ["hijri", "palette", "otp", "phone", "birthdate", "file", "scrollspy", "timeline", "tree", "charts", "month"]),
        ("القوالب", ["login-simple", "login", "tpl-dashboard", "tpl-tickets", "tpl-order", "tpl-settings", "tpl-review", "tpl-booking", "tpl-error"]),
        ("الملحقات", ["navbar", "carousel", "colorpicker", "reveal"]),
    ];

    // Each link's icon, the web NAV's `icon`; tokens, fonts and window are Avalonia's own and borrow one from the sprite.
    private static readonly Dictionary<string, string> Icons = new()
    {
        ["color"] = "IconPalette", ["tokens"] = "IconSliders", ["fonts"] = "IconType", ["window"] = "IconWindow",
        ["shell"] = "IconLayout", ["split"] = "IconSidebar",
        ["typography"] = "IconType", ["surfaces"] = "IconLayers", ["tables"] = "IconTable", ["description"] = "IconList", ["quote"] = "IconMessage",
        ["inputs"] = "IconEdit", ["checks"] = "IconCheckSquare", ["input-group"] = "IconLink", ["pickers"] = "IconCalendar", ["chips"] = "IconTag", ["matrix"] = "IconGrid", ["validation"] = "IconShield",
        ["buttons"] = "IconPointer", ["accordion"] = "IconChevrons", ["tabs"] = "IconTabs", ["stats"] = "IconChart", ["loading"] = "IconLoader", ["progress"] = "IconGauge",
        ["countdown"] = "IconClock", ["save"] = "IconCheck", ["tooltips"] = "IconMessage", ["alerts"] = "IconAlert", ["dialog"] = "IconWindow", ["drawer"] = "IconSidebar",
        ["toasts"] = "IconBell", ["notify"] = "IconBell", ["badges"] = "IconTag", ["list"] = "IconList", ["comments"] = "IconInbox", ["dropdown"] = "IconMenuDown",
        ["steps"] = "IconSteps", ["pagination"] = "IconPages", ["page-head"] = "IconRoute",
        ["hijri"] = "IconMoon", ["palette"] = "IconSearch", ["otp"] = "IconLock", ["phone"] = "IconPhone", ["birthdate"] = "IconCalendar", ["file"] = "IconUpload",
        ["scrollspy"] = "IconList", ["timeline"] = "IconClock", ["tree"] = "IconFolder", ["charts"] = "IconChart", ["month"] = "IconCalendar",
        ["login-simple"] = "IconLock", ["login"] = "IconUser", ["tpl-dashboard"] = "IconHome", ["tpl-tickets"] = "IconInbox", ["tpl-order"] = "IconCart",
        ["tpl-settings"] = "IconCog", ["tpl-review"] = "IconFile", ["tpl-booking"] = "IconClock", ["tpl-error"] = "IconAlert",
        ["navbar"] = "IconCompass", ["carousel"] = "IconSlides", ["colorpicker"] = "IconDroplet", ["reveal"] = "IconSparkle",
    };

    // The pages the web NAV flags `fresh`.
    private static readonly HashSet<string> Fresh =
    [
        "split", "tables", "description", "quote", "inputs", "pickers", "chips", "matrix", "stats", "countdown", "save", "notify", "badges", "comments", "steps",
        "birthdate", "charts", "month", "login-simple", "tpl-dashboard", "tpl-tickets", "tpl-order", "tpl-settings", "tpl-review", "tpl-booking", "tpl-error",
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
