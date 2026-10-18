using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Layout;

namespace Insiyab.Avalonia.Controls;

/// <summary>The footer actions every footer shares, as the web's <c>.ins-dialog-foot</c>: at the end of the line with the main action outermost, 12px apart, each at least 5rem wide; a child with the <c>me-auto</c> class (Delete, Today) stands alone at the start. At a phone's width (34rem) the actions share the line equally at the field height, and the panel takes <c>:phone</c>.</summary>
[PseudoClasses(":phone")]
public class DialogFoot : Panel
{
    public static readonly StyledProperty<double> SpacingProperty =
        AvaloniaProperty.Register<DialogFoot, double>(nameof(Spacing), 12);

    public static readonly StyledProperty<double> PhoneWidthProperty =
        AvaloniaProperty.Register<DialogFoot, double>(nameof(PhoneWidth), 544);

    private TopLevel? _watched;

    static DialogFoot()
    {
        AffectsMeasure<DialogFoot>(SpacingProperty, PhoneWidthProperty);
    }

    public double Spacing { get => GetValue(SpacingProperty); set => SetValue(SpacingProperty, value); }

    /// <summary>The window width at or under which the actions share the line: the web's <c>max-width: 34rem</c>.</summary>
    public double PhoneWidth { get => GetValue(PhoneWidthProperty); set => SetValue(PhoneWidthProperty, value); }

    internal bool IsPhone { get; private set; }

    private static bool Shares(Control c) => !c.Classes.Contains("icon");

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        _watched = OverlayHost.Top(this);
        if (_watched is not null) _watched.PropertyChanged += OnTopChanged;
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        if (_watched is not null) _watched.PropertyChanged -= OnTopChanged;
        _watched = null;
    }

    private void OnTopChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property == TopLevel.ClientSizeProperty || e.Property == BoundsProperty) InvalidateMeasure();
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        var top = TopLevel.GetTopLevel(this);
        var room = OverlayHost.Width(this);
        var phone = top is not DialogWindow && room > 0 && room <= PhoneWidth;
        if (phone != IsPhone)
        {
            IsPhone = phone;
            PseudoClasses.Set(":phone", phone);
        }

        var visible = Children.Where(c => c.IsVisible).ToList();
        var width = 0.0;
        var height = 0.0;
        foreach (var child in visible)
        {
            child.Measure(new Size(double.PositiveInfinity, availableSize.Height));
            width += child.DesiredSize.Width;
            height = Math.Max(height, child.DesiredSize.Height);
        }
        width += Math.Max(0, visible.Count - 1) * Spacing;
        if (phone && !double.IsInfinity(availableSize.Width)) width = availableSize.Width;
        return new Size(width, height);
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        var visible = Children.Where(c => c.IsVisible).ToList();
        if (visible.Count == 0) return finalSize;

        if (IsPhone)
        {
            var fixedWidth = visible.Where(c => !Shares(c)).Sum(c => c.DesiredSize.Width);
            var sharing = visible.Count(Shares);
            var share = sharing == 0 ? 0 : Math.Max(0, (finalSize.Width - fixedWidth - (visible.Count - 1) * Spacing) / sharing);
            var x = 0.0;
            foreach (var child in visible)
            {
                var w = Shares(child) ? share : child.DesiredSize.Width;
                child.Arrange(new Rect(x, 0, w, finalSize.Height));
                x += w + Spacing;
            }
            return finalSize;
        }

        var start = visible.Where(c => c.Classes.Contains("me-auto")).ToList();
        var end = visible.Where(c => !c.Classes.Contains("me-auto")).ToList();
        var left = 0.0;
        foreach (var child in start)
        {
            child.Arrange(new Rect(left, 0, child.DesiredSize.Width, finalSize.Height));
            left += child.DesiredSize.Width + Spacing;
        }
        var right = finalSize.Width;
        for (var i = end.Count - 1; i >= 0; i--)
        {
            var child = end[i];
            right -= child.DesiredSize.Width;
            child.Arrange(new Rect(right, 0, child.DesiredSize.Width, finalSize.Height));
            right -= Spacing;
        }
        return finalSize;
    }
}
