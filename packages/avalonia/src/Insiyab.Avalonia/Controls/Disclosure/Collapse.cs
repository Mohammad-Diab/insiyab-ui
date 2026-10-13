using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.VisualTree;

namespace Insiyab.Avalonia.Controls;

/// <summary>The web's <c>&lt;details name="…"&gt;</c>: expanders beside each other with the same <c>Group</c> keep one section open at a time.</summary>
public static class Collapse
{
    public static readonly AttachedProperty<string?> GroupProperty =
        AvaloniaProperty.RegisterAttached<Expander, string?>("Group", typeof(Collapse));

    /// <summary>Up, Down, Home and End move the focus between the headers of the expanders in this panel, as a desktop list of sections does. The <c>StackPanel.accordion</c> style switches it on.</summary>
    public static readonly AttachedProperty<bool> KeysProperty =
        AvaloniaProperty.RegisterAttached<Panel, bool>("Keys", typeof(Collapse));

    static Collapse()
    {
        Expander.IsExpandedProperty.Changed.AddClassHandler<Expander>(OnExpandedChanged);
        GroupProperty.Changed.AddClassHandler<Expander>((e, _) => { if (e.IsExpanded) CloseOthers(e); });
        KeysProperty.Changed.AddClassHandler<Panel>((p, e) =>
        {
            p.RemoveHandler(InputElement.KeyDownEvent, OnPanelKeyDown);
            if (e.GetNewValue<bool>()) p.AddHandler(InputElement.KeyDownEvent, OnPanelKeyDown, RoutingStrategies.Bubble);
        });
    }

    public static string? GetGroup(Expander element) => element.GetValue(GroupProperty);

    public static void SetGroup(Expander element, string? value) => element.SetValue(GroupProperty, value);

    public static bool GetKeys(Panel element) => element.GetValue(KeysProperty);

    public static void SetKeys(Panel element, bool value) => element.SetValue(KeysProperty, value);

    private static void OnPanelKeyDown(object? sender, KeyEventArgs e)
    {
        if (sender is not Panel panel || e.Handled || e.KeyModifiers != KeyModifiers.None) return;
        if (e.Source is not ToggleButton { Name: "ExpanderHeader", TemplatedParent: Expander current } || current.Parent != panel) return;
        var list = panel.Children.OfType<Expander>().Where(x => x.IsVisible && x.IsEffectivelyEnabled).ToList();
        var at = list.IndexOf(current);
        var to = e.Key switch
        {
            Key.Down => Math.Min(at + 1, list.Count - 1),
            Key.Up => Math.Max(at - 1, 0),
            Key.Home => 0,
            Key.End => list.Count - 1,
            _ => -1,
        };
        if (to < 0) return;
        e.Handled = true;
        HeaderOf(list[to])?.Focus(NavigationMethod.Tab);
    }

    private static ToggleButton? HeaderOf(Expander expander) =>
        expander.GetVisualDescendants().OfType<ToggleButton>().FirstOrDefault(t => t.Name == "ExpanderHeader");

    private static void OnExpandedChanged(Expander expander, AvaloniaPropertyChangedEventArgs change)
    {
        if (change.GetNewValue<bool>()) CloseOthers(expander);
    }

    private static void CloseOthers(Expander expander)
    {
        if (GetGroup(expander) is not { Length: > 0 } group || expander.Parent is not Panel panel) return;
        foreach (var child in panel.Children)
            if (child is Expander other && other != expander && other.IsExpanded && GetGroup(other) == group)
                other.SetCurrentValue(Expander.IsExpandedProperty, false);
    }
}
