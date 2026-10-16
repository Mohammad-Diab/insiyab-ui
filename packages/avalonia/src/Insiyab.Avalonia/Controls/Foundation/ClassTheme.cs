using System.Collections;
using System.Collections.Specialized;
using System.Runtime.CompilerServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Layout;
using Avalonia.LogicalTree;
using Avalonia.Markup.Xaml.Styling;
using Avalonia.Styling;

namespace Insiyab.Avalonia.Controls;

/// <summary>Gives a plain Border, TextBlock, StackPanel or Panel that carries a look class the theme of that class, so no global class rule is tried on every element.</summary>
internal static class ClassTheme
{
    /// <summary>On a look theme (its key starts with InsLook): the classes, separated by spaces, that give an element of its target type this theme.</summary>
    public static readonly AttachedProperty<string?> ForProperty =
        AvaloniaProperty.RegisterAttached<ControlTheme, string?>("For", typeof(ClassTheme));

    /// <summary>On a look theme: its elements carry <c>:ins-nested</c> while an ancestor carries one of their look classes.</summary>
    public static readonly AttachedProperty<bool> NestsProperty =
        AvaloniaProperty.RegisterAttached<ControlTheme, bool>("Nests", typeof(ClassTheme));

    /// <summary>The window's stillness (<see cref="Motion"/>), passed down so a theme can drop its movement.</summary>
    public static readonly AttachedProperty<bool> StillProperty =
        AvaloniaProperty.RegisterAttached<StyledElement, bool>("Still", typeof(ClassTheme), inherits: true);

    /// <summary>Set by a look: the theme its direct children of that theme's exact type take, when they have no theme of their own.</summary>
    public static readonly AttachedProperty<ControlTheme?> ChildThemeProperty =
        AvaloniaProperty.RegisterAttached<StyledElement, ControlTheme?>("ChildTheme", typeof(ClassTheme));

    /// <summary>Set by a look: the horizontal alignment of its direct children.</summary>
    public static readonly AttachedProperty<HorizontalAlignment?> ChildHorizontalAlignmentProperty =
        AvaloniaProperty.RegisterAttached<StyledElement, HorizontalAlignment?>("ChildHorizontalAlignment", typeof(ClassTheme));

    /// <summary>Set by a look: the vertical alignment of its direct children.</summary>
    public static readonly AttachedProperty<VerticalAlignment?> ChildVerticalAlignmentProperty =
        AvaloniaProperty.RegisterAttached<StyledElement, VerticalAlignment?>("ChildVerticalAlignment", typeof(ClassTheme));

    /// <summary>Set by a look: the margin of its direct children.</summary>
    public static readonly AttachedProperty<Thickness?> ChildMarginProperty =
        AvaloniaProperty.RegisterAttached<StyledElement, Thickness?>("ChildMargin", typeof(ClassTheme));

    /// <summary>Set by a look: the maximum width of its direct children.</summary>
    public static readonly AttachedProperty<double?> ChildMaxWidthProperty =
        AvaloniaProperty.RegisterAttached<StyledElement, double?>("ChildMaxWidth", typeof(ClassTheme));

    /// <summary>True on a child while its parent's look gives it a theme or a layout value; a child theme's setters sit under it.</summary>
    public static readonly AttachedProperty<bool> RelayedProperty =
        AvaloniaProperty.RegisterAttached<StyledElement, bool>("Relayed", typeof(ClassTheme));

    internal const string NestedPseudoClass = ":ins-nested";

    // Values a look gives its children rank with a class rule's, as the child rules they replace did.
    private const BindingPriority RelayPriority = BindingPriority.StyleTrigger;

    private static readonly Type[] Hosts = [typeof(Border), typeof(TextBlock), typeof(StackPanel), typeof(Panel)];
    private static readonly Dictionary<string, ControlTheme>[] s_index = [new(StringComparer.Ordinal), new(StringComparer.Ordinal), new(StringComparer.Ordinal), new(StringComparer.Ordinal)];
    private static readonly Dictionary<ControlTheme, int> s_rank = [];
    private static readonly HashSet<ControlTheme> s_ours = [];
    private static readonly ConditionalWeakTable<StyledElement, Watch> s_watched = new();
    private static readonly ConditionalWeakTable<StyledElement, List<IDisposable>> s_relayed = new();
    private static readonly ConditionalWeakTable<StyledElement, Parent> s_parents = new();
    private static Styles? s_root;
    private static bool s_indexed;
    private static bool _registered;

    /// <summary>Elements that carried look classes of two unrelated themes, as "Type: classes"; the later theme won.</summary>
    internal static readonly HashSet<string> Conflicts = new(StringComparer.Ordinal);

    public static string? GetFor(ControlTheme theme) => theme.GetValue(ForProperty);
    public static void SetFor(ControlTheme theme, string? value) => theme.SetValue(ForProperty, value);
    public static bool GetNests(ControlTheme theme) => theme.GetValue(NestsProperty);
    public static void SetNests(ControlTheme theme, bool value) => theme.SetValue(NestsProperty, value);
    public static bool GetStill(StyledElement element) => element.GetValue(StillProperty);
    public static void SetStill(StyledElement element, bool value) => element.SetValue(StillProperty, value);
    public static ControlTheme? GetChildTheme(StyledElement element) => element.GetValue(ChildThemeProperty);
    public static void SetChildTheme(StyledElement element, ControlTheme? value) => element.SetValue(ChildThemeProperty, value);
    public static HorizontalAlignment? GetChildHorizontalAlignment(StyledElement element) => element.GetValue(ChildHorizontalAlignmentProperty);
    public static void SetChildHorizontalAlignment(StyledElement element, HorizontalAlignment? value) => element.SetValue(ChildHorizontalAlignmentProperty, value);
    public static VerticalAlignment? GetChildVerticalAlignment(StyledElement element) => element.GetValue(ChildVerticalAlignmentProperty);
    public static void SetChildVerticalAlignment(StyledElement element, VerticalAlignment? value) => element.SetValue(ChildVerticalAlignmentProperty, value);
    public static Thickness? GetChildMargin(StyledElement element) => element.GetValue(ChildMarginProperty);
    public static void SetChildMargin(StyledElement element, Thickness? value) => element.SetValue(ChildMarginProperty, value);
    public static double? GetChildMaxWidth(StyledElement element) => element.GetValue(ChildMaxWidthProperty);
    public static void SetChildMaxWidth(StyledElement element, double? value) => element.SetValue(ChildMaxWidthProperty, value);
    public static bool GetRelayed(StyledElement element) => element.GetValue(RelayedProperty);
    public static void SetRelayed(StyledElement element, bool value) => element.SetValue(RelayedProperty, value);

    internal static void Register(Styles root)
    {
        s_root = root;
        s_indexed = false;
        if (_registered) return;
        _registered = true;
        StyledElement.ParentProperty.Changed.AddClassHandler<StyledElement>((element, e) => OnParent(element, e.OldValue as StyledElement));
        ChildThemeProperty.Changed.AddClassHandler<StyledElement>((element, _) => OnRelay(element));
        ChildHorizontalAlignmentProperty.Changed.AddClassHandler<StyledElement>((element, _) => OnRelay(element));
        ChildVerticalAlignmentProperty.Changed.AddClassHandler<StyledElement>((element, _) => OnRelay(element));
        ChildMarginProperty.Changed.AddClassHandler<StyledElement>((element, _) => OnRelay(element));
        ChildMaxWidthProperty.Changed.AddClassHandler<StyledElement>((element, _) => OnRelay(element));
    }

    /// <summary>Every look class the theme declares, with its element type and theme, in load order.</summary>
    internal static IEnumerable<(Type Type, string Class, ControlTheme Theme)> All()
    {
        EnsureIndex();
        for (var i = 0; i < Hosts.Length; i++)
            foreach (var (cls, theme) in s_index[i].OrderBy(p => s_rank[p.Value]).ThenBy(p => p.Key, StringComparer.Ordinal))
                yield return (Hosts[i], cls, theme);
    }

    private static int HostIndex(Type key) =>
        key == typeof(Border) ? 0 : key == typeof(TextBlock) ? 1 : key == typeof(StackPanel) ? 2 : key == typeof(Panel) ? 3 : -1;

    private static void EnsureIndex()
    {
        if (s_indexed || s_root is null) return;
        s_indexed = true;
        foreach (var map in s_index) map.Clear();
        s_rank.Clear();
        Walk(s_root);
    }

    private static void Walk(IStyle style)
    {
        if (style is StyleInclude include)
        {
            if (include.Loaded is { } loaded) Walk(loaded);
            return;
        }
        if (style is Styles styles)
        {
            if (styles.Resources is ResourceDictionary resources) Scan(resources);
            foreach (var child in styles) Walk(child);
        }
    }

    private static void Scan(ResourceDictionary resources)
    {
        foreach (var key in resources.Keys.ToList())
        {
            if (key is not string name || !name.StartsWith("InsLook", StringComparison.Ordinal)) continue;
            if (!resources.TryGetResource(key, null, out var value) || value is not ControlTheme theme) continue;
            if (theme.GetValue(ForProperty) is not { } classes) continue;
            var host = theme.TargetType is { } target ? HostIndex(target) : -1;
            if (host < 0) throw new InvalidOperationException($"Look {name} targets {theme.TargetType}; a look class theme targets Border, TextBlock, StackPanel or Panel.");
            s_rank[theme] = s_rank.Count;
            s_ours.Add(theme);
            foreach (var cls in classes.Split(' ', StringSplitOptions.RemoveEmptyEntries))
                if (!s_index[host].TryAdd(cls, theme)) throw new InvalidOperationException($"Look class {theme.TargetType!.Name}.{cls} is claimed twice.");
        }
        foreach (var merged in resources.MergedDictionaries)
            if (merged is ResourceDictionary dictionary) Scan(dictionary);
    }

    // Every element passes here when its parent changes: plain hosts are watched for look classes, and any element under a look takes what that look relays to its children.
    private static void OnParent(StyledElement element, StyledElement? old)
    {
        if (old is not null && s_relayed.TryGetValue(element, out _)) Unrelay(element);
        var parent = element.Parent;
        if (parent is null) return;
        var host = HostIndex(element.StyleKey);
        if (host >= 0)
        {
            if (!s_watched.TryGetValue(element, out _))
            {
                var watch = new Watch(element);
                s_watched.Add(element, watch);
                element.Classes.CollectionChanged += watch.Changed;
            }
            if (element.Classes.Count > 0 || element.Theme is not null) Apply(element);
        }
        if (s_parents.TryGetValue(parent, out _)) Relay(element, parent);
    }

    private static ControlTheme? Pick(StyledElement element)
    {
        var host = HostIndex(element.StyleKey);
        if (host < 0 || element.Classes.Count == 0) return null;
        EnsureIndex();
        var map = s_index[host];
        ControlTheme? best = null;
        var conflict = false;
        foreach (var cls in element.Classes)
        {
            if (cls.Length == 0 || cls[0] == ':' || !map.TryGetValue(cls, out var theme) || ReferenceEquals(theme, best)) continue;
            if (best is null || Derives(theme, best)) best = theme;
            else if (!Derives(best, theme))
            {
                conflict = true;
                if (s_rank[theme] > s_rank[best]) best = theme;
            }
        }
        if (conflict) Conflicts.Add(element.StyleKey.Name + ": " + string.Join(' ', element.Classes.Where(c => c.Length > 0 && c[0] != ':' && map.ContainsKey(c)).OrderBy(c => c, StringComparer.Ordinal)));
        return best;
    }

    private static bool Derives(ControlTheme theme, ControlTheme from)
    {
        for (var t = theme.BasedOn; t is not null; t = t.BasedOn)
            if (ReferenceEquals(t, from)) return true;
        return false;
    }

    private static void Apply(StyledElement element)
    {
        var want = Pick(element) ?? Relayed(element);
        if (want is not null) s_ours.Add(want);
        var now = element.Theme;
        if (ReferenceEquals(now, want) || (now is not null && !s_ours.Contains(now))) return;
        if (want is null) element.ClearValue(StyledElement.ThemeProperty);
        else element.Theme = want;
        if (want is not null && Nests(want) && s_watched.TryGetValue(element, out var watch)) watch.FollowNesting();
        if (((ILogical)element).IsAttachedToLogicalTree) element.ApplyStyling();
    }

    private static bool Nests(ControlTheme theme)
    {
        for (var t = theme; t is not null; t = t.BasedOn)
            if (t.GetValue(NestsProperty)) return true;
        return false;
    }

    // The theme the parent's look gives this element, if it is of that theme's exact type.
    private static ControlTheme? Relayed(StyledElement element) =>
        element.Parent?.GetValue(ChildThemeProperty) is { } theme && theme.TargetType == element.StyleKey ? theme : null;

    private static void OnRelay(StyledElement parent)
    {
        var any = parent.GetValue(ChildThemeProperty) is not null || parent.GetValue(ChildHorizontalAlignmentProperty).HasValue
            || parent.GetValue(ChildVerticalAlignmentProperty).HasValue || parent.GetValue(ChildMarginProperty).HasValue || parent.GetValue(ChildMaxWidthProperty).HasValue;
        if (any) s_parents.AddOrUpdate(parent, Parent.Instance);
        else s_parents.Remove(parent);
        foreach (var child in ((ILogical)parent).LogicalChildren)
            if (child is StyledElement element)
            {
                if (s_relayed.TryGetValue(element, out _)) Unrelay(element);
                if (any) Relay(element, parent);
            }
    }

    private static void Relay(StyledElement child, StyledElement parent)
    {
        var held = new List<IDisposable>();
        if (child is Layoutable layoutable)
        {
            if (parent.GetValue(ChildHorizontalAlignmentProperty) is { } h) held.Add(layoutable.SetValue(Layoutable.HorizontalAlignmentProperty, h, RelayPriority)!);
            if (parent.GetValue(ChildVerticalAlignmentProperty) is { } v) held.Add(layoutable.SetValue(Layoutable.VerticalAlignmentProperty, v, RelayPriority)!);
            if (parent.GetValue(ChildMarginProperty) is { } m) held.Add(layoutable.SetValue(Layoutable.MarginProperty, m, RelayPriority)!);
            if (parent.GetValue(ChildMaxWidthProperty) is { } w) held.Add(layoutable.SetValue(Layoutable.MaxWidthProperty, w, RelayPriority)!);
        }
        if (Relayed(child) is not null)
        {
            held.Add(child.SetValue(RelayedProperty, true, RelayPriority)!);
            Apply(child);
        }
        if (held.Count > 0) s_relayed.AddOrUpdate(child, held);
    }

    private static void Unrelay(StyledElement child)
    {
        if (!s_relayed.TryGetValue(child, out var held)) return;
        s_relayed.Remove(child);
        foreach (var value in held) value.Dispose();
        if (child.Theme is { } theme && s_ours.Contains(theme) && Pick(child) is null && Relayed(child) is null)
        {
            child.ClearValue(StyledElement.ThemeProperty);
            if (((ILogical)child).IsAttachedToLogicalTree) child.ApplyStyling();
        }
    }

    // One element's classes; pseudo-classes such as :pointerover change the same list and are skipped.
    private sealed class Watch(StyledElement owner)
    {
        private bool _nesting;

        public void Changed(object? sender, NotifyCollectionChangedEventArgs args)
        {
            if (args.Action == NotifyCollectionChangedAction.Reset || Named(args.NewItems) || Named(args.OldItems)) Apply(owner);
        }

        public void FollowNesting()
        {
            if (!_nesting)
            {
                _nesting = true;
                owner.AttachedToLogicalTree += (_, _) => Nest();
            }
            if (((ILogical)owner).IsAttachedToLogicalTree) Nest();
        }

        // Nested while a logical ancestor of the same type carries one of this element's look classes.
        private void Nest()
        {
            var host = HostIndex(owner.StyleKey);
            var map = s_index[host];
            var nested = false;
            for (var up = owner.Parent; up is not null && !nested; up = up.Parent)
            {
                if (HostIndex(up.StyleKey) != host) continue;
                foreach (var cls in owner.Classes)
                    if (cls.Length > 0 && cls[0] != ':' && map.ContainsKey(cls) && up.Classes.Contains(cls)) { nested = true; break; }
            }
            ((IPseudoClasses)owner.Classes).Set(NestedPseudoClass, nested);
        }

        private static bool Named(IList? items)
        {
            if (items is null) return false;
            foreach (var item in items)
                if (item is string name && !name.StartsWith(':')) return true;
            return false;
        }
    }

    private sealed class Parent
    {
        public static readonly Parent Instance = new();
    }
}
