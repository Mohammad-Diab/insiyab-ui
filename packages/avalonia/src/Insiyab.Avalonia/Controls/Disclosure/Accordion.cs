using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.LogicalTree;
using Avalonia.Metadata;
using Avalonia.VisualTree;

namespace Insiyab.Avalonia.Controls;

/// <summary>An accordion filled from data, the web's <c>.ins-accordion</c>: an <see cref="Expander"/> per item, each divided from the one before, with the panel's corners inside <c>Border.panel</c>; hidden sections are skipped and the arrow keys move between the heads.</summary>
public class Accordion : ItemsControl
{
    public static readonly StyledProperty<IDataTemplate?> HeaderTemplateProperty =
        AvaloniaProperty.Register<Accordion, IDataTemplate?>(nameof(HeaderTemplate));

    public static readonly StyledProperty<bool> IsSingleProperty =
        AvaloniaProperty.Register<Accordion, bool>(nameof(IsSingle));

    private static readonly string[] Marks = [":ins-item", ":ins-first", ":ins-last", ":ins-panel"];

    private bool _stale;

    /// <summary>For items from <see cref="ItemsControl.ItemsSource"/>: each section's head; without it the head is the item, drawn by <see cref="HeaderTemplate"/>.</summary>
    [AssignBinding]
    [InheritDataTypeFromItems(nameof(ItemsSource))]
    public BindingBase? HeaderBinding { get; set; }

    /// <summary>Draws each section's head from its item, as <see cref="ItemsControl.ItemTemplate"/> draws its body.</summary>
    [InheritDataTypeFromItems(nameof(ItemsSource))]
    public IDataTemplate? HeaderTemplate { get => GetValue(HeaderTemplateProperty); set => SetValue(HeaderTemplateProperty, value); }

    /// <summary>For items from <see cref="ItemsControl.ItemsSource"/>: whether each section is open, kept both ways.</summary>
    [AssignBinding]
    [InheritDataTypeFromItems(nameof(ItemsSource))]
    public BindingBase? IsExpandedBinding { get; set; }

    /// <summary>Opening a section closes the others.</summary>
    public bool IsSingle { get => GetValue(IsSingleProperty); set => SetValue(IsSingleProperty, value); }

    protected override bool NeedsContainerOverride(object? item, int index, out object? recycleKey) =>
        NeedsContainer<Expander>(item, out recycleKey);

    protected override Control CreateContainerForItemOverride(object? item, int index, object? recycleKey) => new Expander();

    protected override void PrepareContainerForItemOverride(Control container, object? item, int index)
    {
        if (ReferenceEquals(container, item) || container is not Expander section)
        {
            base.PrepareContainerForItemOverride(container, item, index);
            return;
        }
        if (HeaderBinding is { } header) section.Bind(HeaderedContentControl.HeaderProperty, header);
        else Default(section, HeaderedContentControl.HeaderProperty, item);
        Default(section, HeaderedContentControl.HeaderTemplateProperty, HeaderTemplate);
        Default(section, ContentControl.ContentProperty, item);
        Default(section, ContentControl.ContentTemplateProperty, ItemTemplate);
        if (IsExpandedBinding is { } open) section.Bind(Expander.IsExpandedProperty, open);
    }

    private static void Default<T>(AvaloniaObject target, StyledProperty<T> property, T value)
    {
        if (value is not null && !target.IsSet(property)) target.SetCurrentValue(property, value);
    }

    protected override void ContainerForItemPreparedOverride(Control container, object? item, int index)
    {
        base.ContainerForItemPreparedOverride(container, item, index);
        container.PropertyChanged -= OnSectionChanged;
        container.PropertyChanged += OnSectionChanged;
        Stale();
    }

    protected override void ContainerIndexChangedOverride(Control container, int oldIndex, int newIndex)
    {
        base.ContainerIndexChangedOverride(container, oldIndex, newIndex);
        Stale();
    }

    protected override void ClearContainerForItemOverride(Control container)
    {
        base.ClearContainerForItemOverride(container);
        container.PropertyChanged -= OnSectionChanged;
        var marks = (IPseudoClasses)container.Classes;
        foreach (var mark in Marks) marks.Set(mark, false);
        Stale();
    }

    protected override void OnAttachedToLogicalTree(LogicalTreeAttachmentEventArgs e)
    {
        base.OnAttachedToLogicalTree(e);
        Stale();
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == IsSingleProperty && change.GetNewValue<bool>() && Sections().FirstOrDefault(s => s.IsExpanded) is { } first) CloseOthers(first);
    }

    private void OnSectionChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property == IsVisibleProperty) Stale();
        else if (e.Property == Expander.IsExpandedProperty && IsSingle && e.GetNewValue<bool>() && sender is Expander open) CloseOthers(open);
    }

    private void CloseOthers(Expander open)
    {
        foreach (var other in Sections())
            if (other != open && other.IsExpanded) other.SetCurrentValue(Expander.IsExpandedProperty, false);
    }

    // Places are worked out in the arrange after a change, when every section has joined and has its index.
    private void Stale()
    {
        _stale = true;
        InvalidateArrange();
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        if (_stale)
        {
            _stale = false;
            Place();
        }
        return base.ArrangeOverride(finalSize);
    }

    private List<Expander> Sections() =>
        GetRealizedContainers().OfType<Expander>().Select(c => (Section: c, At: IndexFromContainer(c))).Where(x => x.At >= 0).OrderBy(x => x.At).Select(x => x.Section).ToList();

    private void Place()
    {
        var all = Sections();
        var shown = all.Where(s => s.IsVisible).ToList();
        var inPanel = Parent is Border border && border.Classes.Contains("panel");
        foreach (var section in all)
        {
            var marks = (IPseudoClasses)section.Classes;
            marks.Set(":ins-item", true);
            marks.Set(":ins-first", shown.Count > 0 && section == shown[0]);
            marks.Set(":ins-last", shown.Count > 0 && section == shown[^1]);
            marks.Set(":ins-panel", inPanel);
        }
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        if (!e.Handled && e.KeyModifiers == KeyModifiers.None && e.Source is ToggleButton { Name: "ExpanderHeader", TemplatedParent: Expander current })
        {
            var list = Sections().Where(s => s.IsVisible && s.IsEffectivelyEnabled).ToList();
            var at = list.IndexOf(current);
            var to = at < 0 ? -1 : e.Key switch
            {
                Key.Down => Math.Min(at + 1, list.Count - 1),
                Key.Up => Math.Max(at - 1, 0),
                Key.Home => 0,
                Key.End => list.Count - 1,
                _ => -1,
            };
            if (to >= 0)
            {
                e.Handled = true;
                list[to].GetVisualDescendants().OfType<ToggleButton>().FirstOrDefault(t => t.Name == "ExpanderHeader")?.Focus(NavigationMethod.Tab);
                return;
            }
        }
        base.OnKeyDown(e);
    }
}
