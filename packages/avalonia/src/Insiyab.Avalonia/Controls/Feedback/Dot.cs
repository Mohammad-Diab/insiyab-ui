using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.VisualTree;

namespace Insiyab.Avalonia.Controls;

/// <summary>The "something new" dot on any control (a top bar's icon button): <c>ins:Dot.IsShown</c> draws the bottom bar's dot at its top end corner in the adorner layer, so its size, look and taps never change. <c>ins:Dot.Count</c> above 0 makes it a small count instead («3», «9+» from ten), where a touch screen has no hover to tell how many: the notification bell's count (<c>ins:Notify</c>), out at the same corner and ringed by the page's colour. The adorner layer is drawn over every page and takes none of their opacity, so the dot takes its control's own (its opacity times every parent's, followed as they change): a page fading out or in takes its dot with it.</summary>
public static class Dot
{
    public static readonly AttachedProperty<bool> IsShownProperty =
        AvaloniaProperty.RegisterAttached<Control, bool>("IsShown", typeof(Dot));

    public static readonly AttachedProperty<int> CountProperty =
        AvaloniaProperty.RegisterAttached<Control, int>("Count", typeof(Dot));

    /// <summary>How far a count stands past the control's top and end edges (the notification bell's count, <c>Notify</c>'s <c>PART_Count</c>).</summary>
    internal const double CountOut = 7;

    /// <summary>The page-coloured ring around a count, drawn outside its 20px badge (<c>InsCountRing</c>).</summary>
    internal const double CountRing = 2;

    // the dot made for a control the first time it is shown, kept as its adorner
    private static readonly AttachedProperty<Control?> MarkProperty =
        AvaloniaProperty.RegisterAttached<Control, Control?>("Mark", typeof(Dot));

    // the control and its parents up to the window, whose opacity the dot follows while it is in the window
    private static readonly System.Runtime.CompilerServices.ConditionalWeakTable<Control, List<Visual>> s_watched = new();

    static Dot()
    {
        IsShownProperty.Changed.AddClassHandler<Control>((control, _) => Update(control));
        CountProperty.Changed.AddClassHandler<Control>((control, _) => Update(control));
    }

    public static int GetCount(Control control) => control.GetValue(CountProperty);
    public static void SetCount(Control control, int value) => control.SetValue(CountProperty, value);

    // shown as a dot, or as a count
    private static bool Wanted(Control control) => GetIsShown(control) || GetCount(control) > 0;

    public static bool GetIsShown(Control control) => control.GetValue(IsShownProperty);
    public static void SetIsShown(Control control, bool value) => control.SetValue(IsShownProperty, value);

    /// <summary>The dot drawn for <paramref name="control"/>; null until it was first shown.</summary>
    internal static Control? MarkOf(Control control) => control.GetValue(MarkProperty);

    private static void Update(Control control)
    {
        var mark = control.GetValue(MarkProperty);
        if (mark is null)
        {
            if (!Wanted(control)) return;
            var figure = new TextBlock { Classes = { "ins-dot-count" }, FlowDirection = FlowDirection.LeftToRight, HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center };
            var dot = new TemplateBorder { Classes = { "ins-dot" }, HorizontalAlignment = HorizontalAlignment.Right, VerticalAlignment = VerticalAlignment.Top, Child = figure };
            mark = new Panel
            {
                IsHitTestVisible = false,
                Children = { dot },
                // the adorner layer's own direction is the window's: the dot takes the control's, so it stands at its end
                [!Visual.FlowDirectionProperty] = control[!Visual.FlowDirectionProperty],
            };
            control.SetValue(MarkProperty, mark);
            AdornerLayer.SetAdorner(control, mark);
            // the dot inside the top end corner, also on a round control: its rim passes the box's corner 0.146 of its size in, so the dot moves in that far (never under 4px). A count stands OUT at the corner as the notification bell's count does (Notify's PART_Count: 7px past the top and the end, ringed by the page's colour), so the icon under it stays whole
            void Place()
            {
                if (dot.Classes.Contains("count"))
                {
                    // the ring's outer edge stands CountOut past the corner, as Notify's ringed count does
                    dot.Margin = new Thickness(0, -(CountOut - CountRing), -(CountOut - CountRing), 0);
                    return;
                }
                var inset = Math.Max(4, Math.Round(Math.Min(control.Bounds.Width, control.Bounds.Height) * 0.146));
                dot.Margin = new Thickness(0, inset, inset, 0);
            }
            control.SizeChanged += (_, _) => Place();
            dot.Classes.CollectionChanged += (_, _) => Place();
            // a control out of sight (its page hidden, the control itself hidden) shows no dot: an adorner is drawn whatever its control's visibility
            control.PropertyChanged += (_, e) =>
            {
                if (e.Property == Visual.IsVisibleProperty) Sync(control);
            };
            control.AttachedToVisualTree += (_, _) =>
            {
                Watch(control);
                Sync(control);
            };
            control.DetachedFromVisualTree += (_, _) =>
            {
                Unwatch(control);
                Sync(control);
            };
            Watch(control);
            control.LayoutUpdated += (_, _) => Sync(control);
            Place();
        }
        Sync(control);
    }

    private static void Sync(Control control)
    {
        if (control.GetValue(MarkProperty) is not { } mark) return;
        if (mark is Panel { Children: [TemplateBorder { Child: TextBlock figure } dot] })
        {
            var count = GetCount(control);
            var text = count <= 0 ? null : count > 9 ? "9+" : count.ToString(System.Globalization.CultureInfo.InvariantCulture);
            if (figure.Text != text) figure.Text = text;
            dot.Classes.Set("count", count > 0);
        }
        var shown = Wanted(control) && control.IsEffectivelyVisible && control.IsAttachedToVisualTree();
        if (mark.IsVisible != shown) mark.IsVisible = shown;
        var opacity = 1.0;
        if (shown && s_watched.TryGetValue(control, out var line))
            foreach (var v in line)
                opacity *= v.Opacity;
        if (mark.Opacity != opacity) mark.Opacity = opacity;
    }

    // Follows the opacity of the control and of each of its parents up to the window (a page fading in or out over another).
    private static void Watch(Control control)
    {
        Unwatch(control);
        if (!control.IsAttachedToVisualTree()) return;
        var line = new List<Visual>();
        foreach (var v in control.GetSelfAndVisualAncestors())
        {
            if (v is TopLevel) break;
            line.Add(v);
            v.PropertyChanged += OnLineChanged;
        }
        s_watched.AddOrUpdate(control, line);

        void OnLineChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
        {
            if (e.Property == Visual.OpacityProperty) Sync(control);
        }
        s_handlers.AddOrUpdate(control, OnLineChanged);
    }

    private static readonly System.Runtime.CompilerServices.ConditionalWeakTable<Control, EventHandler<AvaloniaPropertyChangedEventArgs>> s_handlers = new();

    private static void Unwatch(Control control)
    {
        if (s_watched.TryGetValue(control, out var line) && s_handlers.TryGetValue(control, out var handler))
            foreach (var v in line)
                v.PropertyChanged -= handler;
        s_watched.Remove(control);
        s_handlers.Remove(control);
    }
}
