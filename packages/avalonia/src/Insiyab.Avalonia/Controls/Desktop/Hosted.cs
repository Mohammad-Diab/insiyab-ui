using System.Collections.Specialized;
using Avalonia;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.LogicalTree;
using Avalonia.Media;

namespace Insiyab.Avalonia.Controls;

/// <summary>A control that keeps its items in its own logical tree while a panel in its template shows them, as ItemsControl does with its panel.</summary>
internal static class Hosted
{
    /// <summary>Keeps the owner's logical children in step with its item collection.</summary>
    public static void Adopt(IAvaloniaList<ILogical> logical, NotifyCollectionChangedEventArgs e)
    {
        if (e.OldItems is not null)
            foreach (var c in e.OldItems.OfType<Control>()) logical.Remove(c);
        if (e.NewItems is not null)
            foreach (var c in e.NewItems.OfType<Control>()) logical.Add(c);
    }

    /// <summary>Shows a panel's children in order; only a child no control owns (a template's own part) becomes the panel's logical child.</summary>
    public static void Show(Panel panel, IAvaloniaList<Visual> visual, IAvaloniaList<ILogical> logical)
    {
        foreach (var v in visual.ToList())
            if (v is not Control c || !panel.Children.Contains(c)) visual.Remove(v);
        for (var i = 0; i < panel.Children.Count; i++)
        {
            var c = panel.Children[i];
            if (i < visual.Count && ReferenceEquals(visual[i], c)) continue;
            visual.Remove(c);
            visual.Insert(i, c);
        }
        foreach (var l in logical.ToList())
            if (l is not Control c || !panel.Children.Contains(c)) logical.Remove(l);
        foreach (var c in panel.Children)
            if (c.Parent is null) logical.Add(c);
    }
}

/// <summary>One side of a status bar: a row that shows the bar's items. Part of the StatusBar template.</summary>
internal class StatusBarZone : StackPanel
{
    private const double Room = 6;

    /// <summary>Cuts what runs past the zone's end, but leaves room above, below and at the start for a focus ring or a glow.</summary>
    public static readonly StyledProperty<bool> CutsOverflowProperty =
        AvaloniaProperty.Register<StatusBarZone, bool>(nameof(CutsOverflow));

    static StatusBarZone() => AffectsArrange<StatusBarZone>(CutsOverflowProperty);

    public bool CutsOverflow { get => GetValue(CutsOverflowProperty); set => SetValue(CutsOverflowProperty, value); }

    protected override Size ArrangeOverride(Size finalSize)
    {
        var size = base.ArrangeOverride(finalSize);
        Clip = CutsOverflow ? new RectangleGeometry(new Rect(-Room, -Room, finalSize.Width + Room, finalSize.Height + 2 * Room)) : null;
        return size;
    }

    protected override void ChildrenChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        Hosted.Show(this, VisualChildren, LogicalChildren);
        InvalidateMeasure();
    }
}
