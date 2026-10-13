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

public sealed record GalleryPage(string Slug, string Title, string Group, Type Type)
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
                    pages.Add(new GalleryPage(slug, hit.Info!.Title, group, hit.Type));
        foreach (var (slug, hit) in found)
            pages.Add(new GalleryPage(slug, hit.Info!.Title, "أخرى", hit.Type));
        return pages;
    }
}
