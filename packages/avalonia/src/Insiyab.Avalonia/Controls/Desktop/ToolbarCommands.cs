using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;

namespace Insiyab.Avalonia.Controls;

internal interface IToolbarCommand
{
    Geometry? Icon { get; }
    string? Label { get; }
    bool ShowLabel { get; }
    string? ShortcutText { get; }
}

/// <summary>A toolbar command: an icon, its name beside it or only in its tip, and the shortcut in the tip. With a Flyout it is a dropdown and shows a chevron.</summary>
[PseudoClasses(":labelled")]
public class ToolbarButton : Button, IToolbarCommand
{
    public static readonly StyledProperty<Geometry?> IconProperty =
        AvaloniaProperty.Register<ToolbarButton, Geometry?>(nameof(Icon));

    /// <summary>The command's name: beside the icon with <see cref="ShowLabel"/>, otherwise in its tip; always in the «⋯» menu and for screen readers.</summary>
    public static readonly StyledProperty<string?> LabelProperty =
        AvaloniaProperty.Register<ToolbarButton, string?>(nameof(Label));

    /// <summary>The shortcut the tip and the «⋯» menu show, such as «Ctrl+S»; unset, they show <see cref="Button.HotKey"/>. Showing it does not bind the key: set HotKey for that.</summary>
    public static readonly StyledProperty<string?> ShortcutProperty =
        AvaloniaProperty.Register<ToolbarButton, string?>(nameof(Shortcut));

    /// <summary>Shows the name beside the icon. A command without an icon always shows it.</summary>
    public static readonly StyledProperty<bool> ShowLabelProperty =
        AvaloniaProperty.Register<ToolbarButton, bool>(nameof(ShowLabel));

    private readonly ToolbarFace _face;

    public ToolbarButton()
    {
        _face = new ToolbarFace(this, this);
        Refresh();
    }

    public Geometry? Icon { get => GetValue(IconProperty); set => SetValue(IconProperty, value); }
    public string? Label { get => GetValue(LabelProperty); set => SetValue(LabelProperty, value); }
    public string? Shortcut { get => GetValue(ShortcutProperty); set => SetValue(ShortcutProperty, value); }
    public bool ShowLabel { get => GetValue(ShowLabelProperty); set => SetValue(ShowLabelProperty, value); }

    string? IToolbarCommand.ShortcutText => Shortcut ?? HotKey?.ToString();

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (ToolbarFace.Watches(change.Property) || change.Property == FlyoutProperty) Refresh();
    }

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);
        Refresh();
    }

    private void Refresh()
    {
        if (_face is null) return;
        PseudoClasses.Set(":labelled", _face.Update(Flyout is not null && !Classes.Contains("toolbar-more")));
    }
}

/// <summary>A toolbar command that stays on or off, such as bold; in the «⋯» menu it is a check item.</summary>
[PseudoClasses(":labelled")]
public class ToolbarToggleButton : ToggleButton, IToolbarCommand
{
    public static readonly StyledProperty<Geometry?> IconProperty = ToolbarButton.IconProperty.AddOwner<ToolbarToggleButton>();
    public static readonly StyledProperty<string?> LabelProperty = ToolbarButton.LabelProperty.AddOwner<ToolbarToggleButton>();
    public static readonly StyledProperty<string?> ShortcutProperty = ToolbarButton.ShortcutProperty.AddOwner<ToolbarToggleButton>();
    public static readonly StyledProperty<bool> ShowLabelProperty = ToolbarButton.ShowLabelProperty.AddOwner<ToolbarToggleButton>();

    private readonly ToolbarFace _face;

    public ToolbarToggleButton()
    {
        _face = new ToolbarFace(this, this);
        Refresh();
    }

    public Geometry? Icon { get => GetValue(IconProperty); set => SetValue(IconProperty, value); }
    public string? Label { get => GetValue(LabelProperty); set => SetValue(LabelProperty, value); }
    public string? Shortcut { get => GetValue(ShortcutProperty); set => SetValue(ShortcutProperty, value); }
    public bool ShowLabel { get => GetValue(ShowLabelProperty); set => SetValue(ShowLabelProperty, value); }

    string? IToolbarCommand.ShortcutText => Shortcut ?? HotKey?.ToString();

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (ToolbarFace.Watches(change.Property)) Refresh();
    }

    private void Refresh()
    {
        if (_face is null) return;
        PseudoClasses.Set(":labelled", _face.Update(false));
    }
}

/// <summary>A toolbar command with a main action and a chevron that opens more choices in its Flyout; in the «⋯» menu it is a submenu, the main action first.</summary>
[PseudoClasses(":labelled")]
public class ToolbarSplitButton : SplitButton, IToolbarCommand
{
    public static readonly StyledProperty<Geometry?> IconProperty = ToolbarButton.IconProperty.AddOwner<ToolbarSplitButton>();
    public static readonly StyledProperty<string?> LabelProperty = ToolbarButton.LabelProperty.AddOwner<ToolbarSplitButton>();
    public static readonly StyledProperty<string?> ShortcutProperty = ToolbarButton.ShortcutProperty.AddOwner<ToolbarSplitButton>();
    public static readonly StyledProperty<bool> ShowLabelProperty = ToolbarButton.ShowLabelProperty.AddOwner<ToolbarSplitButton>();

    /// <summary>The chevron's name for screen readers, «خيارات أخرى» unless set.</summary>
    public static readonly StyledProperty<string> MoreTextProperty =
        AvaloniaProperty.Register<ToolbarSplitButton, string>(nameof(MoreText), "خيارات أخرى");

    private readonly ToolbarFace _face;

    public ToolbarSplitButton()
    {
        _face = new ToolbarFace(this, this, hosted: false);
        Refresh();
    }

    public Geometry? Icon { get => GetValue(IconProperty); set => SetValue(IconProperty, value); }
    public string? Label { get => GetValue(LabelProperty); set => SetValue(LabelProperty, value); }
    public string? Shortcut { get => GetValue(ShortcutProperty); set => SetValue(ShortcutProperty, value); }
    public bool ShowLabel { get => GetValue(ShowLabelProperty); set => SetValue(ShowLabelProperty, value); }
    public string MoreText { get => GetValue(MoreTextProperty); set => SetValue(MoreTextProperty, value); }

    string? IToolbarCommand.ShortcutText => Shortcut ?? HotKey?.ToString();

    internal void ClickPrimary() => OnClickPrimary(new RoutedEventArgs());

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (ToolbarFace.Watches(change.Property)) Refresh();
    }

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);
        _face.Host = e.NameScope.Find<Button>("PART_PrimaryButton");
        if (e.NameScope.Find<Button>("PART_SecondaryButton") is { } chevron) AutomationProperties.SetName(chevron, MoreText);
        Refresh();
    }

    private void Refresh()
    {
        if (_face is null) return;
        PseudoClasses.Set(":labelled", _face.Update(false));
    }
}

/// <summary>What a toolbar command shows: the icon, the name when it is shown, a dropdown's chevron, and the tip and screen-reader name it gives itself unless the app set its own.</summary>
internal sealed class ToolbarFace
{
    private readonly ContentControl _owner;
    private readonly IToolbarCommand _cmd;
    private readonly StackPanel _row = new() { Orientation = Orientation.Horizontal, Spacing = 6 };
    private readonly Icon _icon = new();
    private readonly TextBlock _label = new();
    private readonly Icon _chevron = new();
    private object? _tip;
    private string? _name;

    private ContentControl? _host;

    public ToolbarFace(ContentControl owner, IToolbarCommand cmd, bool hosted = true)
    {
        _owner = owner;
        _cmd = cmd;
        _host = hosted ? owner : null;
        _icon.Classes.Add("toolbar-icon");
        _label.Classes.Add("toolbar-label");
        _chevron.Classes.Add("toolbar-chevron");
        _row.Children.AddRange([_icon, _label, _chevron]);
    }

    /// <summary>The control that shows the face as its content: the command itself, or a split button's main part.</summary>
    public ContentControl? Host
    {
        get => _host;
        set
        {
            if (ReferenceEquals(_host, value)) return;
            if (_host is not null && ReferenceEquals(_host.Content, _row)) _host.SetCurrentValue(ContentControl.ContentProperty, null);
            _host = value;
        }
    }

    // By name, since AddOwner gives the other commands their own property objects.
    public static bool Watches(AvaloniaProperty p) => p.Name is "Icon" or "Label" or "Shortcut" or "ShowLabel" or "HotKey" or "Content";

    /// <summary>Brings the face up to date; returns whether the name shows.</summary>
    public bool Update(bool chevron)
    {
        var label = _cmd.Label;
        var hasLabel = !string.IsNullOrEmpty(label);
        _icon.Data = _cmd.Icon;
        _icon.IsVisible = _cmd.Icon is not null;
        _label.Text = label;
        var shown = hasLabel && (_cmd.ShowLabel || _cmd.Icon is null);
        _label.IsVisible = shown;
        _chevron.IsVisible = chevron;
        if (_host is { Content: null } host) host.SetCurrentValue(ContentControl.ContentProperty, _row);

        var shortcut = _cmd.ShortcutText;
        var current = ToolTip.GetTip(_owner);
        if (current is null || ReferenceEquals(current, _tip))
        {
            _tip = hasLabel && (!shown || !string.IsNullOrEmpty(shortcut)) ? Tip(label!, shortcut) : null;
            if (!ReferenceEquals(current, _tip)) _owner.SetCurrentValue(ToolTip.TipProperty, _tip);
        }
        var name = AutomationProperties.GetName(_owner);
        if (string.IsNullOrEmpty(name) || name == _name)
        {
            _name = label;
            if (name != label) AutomationProperties.SetName(_owner, label);
        }
        return shown;
    }

    // The name, then the shortcut dimmed and left to right.
    private static object Tip(string label, string? shortcut)
    {
        if (string.IsNullOrEmpty(shortcut)) return label;
        var key = new TextBlock { Text = shortcut, FlowDirection = FlowDirection.LeftToRight };
        key.Classes.Add("toolbar-key");
        var row = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };
        row.Children.Add(new TextBlock { Text = label });
        row.Children.Add(key);
        return row;
    }
}
