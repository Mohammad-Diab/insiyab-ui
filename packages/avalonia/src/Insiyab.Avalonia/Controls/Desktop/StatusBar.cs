using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Primitives;
using Avalonia.Media;
using Avalonia.Metadata;
using Avalonia.Threading;

namespace Insiyab.Avalonia.Controls;

/// <summary>The thin strip at the foot of a window: items on the start side and on the end side, muted and compact. Dock it at the bottom of the window, around the app shell, so it never covers the content; toasts in the window's corner sit above it.</summary>
[TemplatePart("PART_Start", typeof(Panel))]
[TemplatePart("PART_End", typeof(Panel))]
public class StatusBar : TemplatedControl
{
    private Panel? _start;
    private Panel? _end;
    private TopLevel? _top;

    public StatusBar()
    {
        Start.CollectionChanged += (_, e) =>
        {
            Hosted.Adopt(LogicalChildren, e);
            Fill(_start, Start);
        };
        End.CollectionChanged += (_, e) =>
        {
            Hosted.Adopt(LogicalChildren, e);
            Fill(_end, End);
        };
    }

    /// <summary>The items on the start side; past the room they have, they are cut, so the end side always shows.</summary>
    [Content]
    public global::Avalonia.Controls.Controls Start { get; } = new();

    /// <summary>The items on the end side.</summary>
    public global::Avalonia.Controls.Controls End { get; } = new();

    // Toasts in the window's corner sit above the bar.
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == BoundsProperty || change.Property == IsVisibleProperty) ToastStrip.Refit(this);
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        _top = TopLevel.GetTopLevel(this);
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        if (_top is { } top) Dispatcher.UIThread.Post(() => ToastStrip.Refit(top));
        _top = null;
    }

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);
        _start?.Children.Clear();
        _end?.Children.Clear();
        _start = e.NameScope.Find<Panel>("PART_Start");
        _end = e.NameScope.Find<Panel>("PART_End");
        Fill(_start, Start);
        Fill(_end, End);
    }

    private static void Fill(Panel? panel, global::Avalonia.Controls.Controls items)
    {
        if (panel is null) return;
        panel.Children.Clear();
        panel.Children.AddRange(items);
    }
}

/// <summary>A status bar item that reports: an icon or a spinner, then its content (text, or any control).</summary>
[PseudoClasses(":icon", ":busy")]
public class StatusItem : ContentControl
{
    public static readonly StyledProperty<Geometry?> IconProperty =
        AvaloniaProperty.Register<StatusItem, Geometry?>(nameof(Icon));

    /// <summary>A small spinner in place of the icon, while something runs.</summary>
    public static readonly StyledProperty<bool> IsBusyProperty =
        AvaloniaProperty.Register<StatusItem, bool>(nameof(IsBusy));

    public StatusItem() => Update();

    public Geometry? Icon { get => GetValue(IconProperty); set => SetValue(IconProperty, value); }
    public bool IsBusy { get => GetValue(IsBusyProperty); set => SetValue(IsBusyProperty, value); }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == IconProperty || change.Property == IsBusyProperty) Update();
    }

    private void Update()
    {
        PseudoClasses.Set(":icon", Icon is not null && !IsBusy);
        PseudoClasses.Set(":busy", IsBusy);
    }
}

/// <summary>A status bar item you can click: it lights under the pointer, and a PopMenu or PopCard in its Flyout opens upward at the window's foot.</summary>
[PseudoClasses(":icon")]
public class StatusButton : Button
{
    public static readonly StyledProperty<Geometry?> IconProperty =
        AvaloniaProperty.Register<StatusButton, Geometry?>(nameof(Icon));

    public Geometry? Icon { get => GetValue(IconProperty); set => SetValue(IconProperty, value); }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == IconProperty) PseudoClasses.Set(":icon", Icon is not null);
    }
}
