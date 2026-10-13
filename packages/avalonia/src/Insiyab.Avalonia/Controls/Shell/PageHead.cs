using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;

namespace Insiyab.Avalonia.Controls;

/// <summary>The web's <c>.ins-page-head</c>: an optional breadcrumb on its own line, the title with a line under it, and the page's actions at the end, which drop under the title when the row is too narrow for both.</summary>
[TemplatePart("PART_Actions", typeof(ContentPresenter))]
[PseudoClasses(":stacked")]
public class PageHead : TemplatedControl
{
    public static readonly StyledProperty<string?> TitleProperty =
        AvaloniaProperty.Register<PageHead, string?>(nameof(Title));

    public static readonly StyledProperty<string?> SubtitleProperty =
        AvaloniaProperty.Register<PageHead, string?>(nameof(Subtitle));

    public static readonly StyledProperty<object?> BreadcrumbProperty =
        AvaloniaProperty.Register<PageHead, object?>(nameof(Breadcrumb));

    public static readonly StyledProperty<object?> ActionsProperty =
        AvaloniaProperty.Register<PageHead, object?>(nameof(Actions));

    /// <summary>The narrowest the title column gets before the actions drop under it: the web's <c>flex: 1 1 18rem</c>.</summary>
    public static readonly StyledProperty<double> TitleMinWidthProperty =
        AvaloniaProperty.Register<PageHead, double>(nameof(TitleMinWidth), 288);

    private Control? _actions;

    public string? Title { get => GetValue(TitleProperty); set => SetValue(TitleProperty, value); }
    public string? Subtitle { get => GetValue(SubtitleProperty); set => SetValue(SubtitleProperty, value); }
    public object? Breadcrumb { get => GetValue(BreadcrumbProperty); set => SetValue(BreadcrumbProperty, value); }
    public object? Actions { get => GetValue(ActionsProperty); set => SetValue(ActionsProperty, value); }
    public double TitleMinWidth { get => GetValue(TitleMinWidthProperty); set => SetValue(TitleMinWidthProperty, value); }

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);
        _actions = e.NameScope.Find<Control>("PART_Actions");
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        if (_actions is not null && Actions is not null && !double.IsInfinity(availableSize.Width))
        {
            _actions.Measure(Size.Infinity);
            var need = TitleMinWidth + 16 + _actions.DesiredSize.Width + Padding.Left + Padding.Right;
            PseudoClasses.Set(":stacked", availableSize.Width < need);
        }
        else PseudoClasses.Set(":stacked", false);
        return base.MeasureOverride(availableSize);
    }
}
