using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Controls.Chrome;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Primitives;
using Avalonia.Input;

namespace Insiyab.Avalonia.Controls;

/// <summary>A sample of the caption buttons outside a title bar, as in a docs page: shown in that state, inert, and not the window's own.</summary>
public enum CaptionPreview
{
    /// <summary>Not a sample: the buttons serve their window and show only while its title bar is drawn.</summary>
    Off,
    Normal,
    Maximized,
    Inactive,
}

/// <summary>The window's minimise, maximise/restore and close buttons for a title bar drawn with TitleBar.Extends; shown only while that bar is active.</summary>
[TemplatePart("PART_Minimize", typeof(Button))]
[TemplatePart("PART_Maximize", typeof(Button))]
[TemplatePart("PART_Close", typeof(Button))]
[PseudoClasses(":shown", ":maximized", ":inactive")]
public class CaptionButtons : TemplatedControl
{
    public static readonly StyledProperty<string> MinimizeTextProperty = AvaloniaProperty.Register<CaptionButtons, string>(nameof(MinimizeText), "تصغير");
    public static readonly StyledProperty<string> MaximizeTextProperty = AvaloniaProperty.Register<CaptionButtons, string>(nameof(MaximizeText), "تكبير");
    public static readonly StyledProperty<string> RestoreTextProperty = AvaloniaProperty.Register<CaptionButtons, string>(nameof(RestoreText), "استعادة");
    public static readonly StyledProperty<string> CloseTextProperty = AvaloniaProperty.Register<CaptionButtons, string>(nameof(CloseText), "إغلاق");
    public static readonly StyledProperty<CaptionPreview> PreviewProperty = AvaloniaProperty.Register<CaptionButtons, CaptionPreview>(nameof(Preview));

    public string MinimizeText { get => GetValue(MinimizeTextProperty); set => SetValue(MinimizeTextProperty, value); }
    public string MaximizeText { get => GetValue(MaximizeTextProperty); set => SetValue(MaximizeTextProperty, value); }
    public string RestoreText { get => GetValue(RestoreTextProperty); set => SetValue(RestoreTextProperty, value); }
    public string CloseText { get => GetValue(CloseTextProperty); set => SetValue(CloseTextProperty, value); }
    /// <summary>Shows the buttons as a sample in this state; a sample never acts on the window or tells Windows where its buttons are.</summary>
    public CaptionPreview Preview { get => GetValue(PreviewProperty); set => SetValue(PreviewProperty, value); }

    private Window? _window;
    private Button? _minimize, _maximize, _close;

    internal bool ForceShown { get; set; }

    internal Button? MinimizeButton => _minimize;
    internal Button? MaximizeButton => _maximize;
    internal Button? CloseButton => _close;

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);
        if (_minimize is not null) _minimize.Click -= OnMinimize;
        if (_maximize is not null) _maximize.Click -= OnMaximize;
        if (_close is not null) _close.Click -= OnClose;
        foreach (var old in new[] { _minimize, _maximize, _close }) old?.RemoveHandler(PointerMovedEvent, KeepMoveFromWindows);
        _minimize = e.NameScope.Find<Button>("PART_Minimize");
        _maximize = e.NameScope.Find<Button>("PART_Maximize");
        _close = e.NameScope.Find<Button>("PART_Close");
        if (_minimize is not null) _minimize.Click += OnMinimize;
        if (_maximize is not null) _maximize.Click += OnMaximize;
        if (_close is not null) _close.Click += OnClose;
        foreach (var button in new[] { _minimize, _maximize, _close }) button?.AddHandler(PointerMovedEvent, KeepMoveFromWindows);
        SetRoles();
        Refresh();
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        _window = TopLevel.GetTopLevel(this) as Window;
        if (_window is not null)
        {
            _window.PropertyChanged += OnWindowChanged;
            if (Preview == CaptionPreview.Off) _window.SetValue(TitleBar.CaptionsProperty, this);
        }
        Refresh();
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        if (_window is not null)
        {
            _window.PropertyChanged -= OnWindowChanged;
            if (_window.GetValue(TitleBar.CaptionsProperty) == this) _window.ClearValue(TitleBar.CaptionsProperty);
        }
        _window = null;
        Refresh();
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == PreviewProperty)
        {
            if (_window is not null && change.GetNewValue<CaptionPreview>() == CaptionPreview.Off) _window.SetValue(TitleBar.CaptionsProperty, this);
            else if (_window is not null && _window.GetValue(TitleBar.CaptionsProperty) == this) _window.ClearValue(TitleBar.CaptionsProperty);
            SetRoles();
        }
        if (change.Property == MaximizeTextProperty || change.Property == RestoreTextProperty || change.Property == PreviewProperty) Refresh();
    }

    private void OnWindowChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property == Window.WindowStateProperty || e.Property == WindowBase.IsActiveProperty || e.Property == Window.IsExtendedIntoWindowDecorationsProperty
            || e.Property == Window.CanMinimizeProperty || e.Property == Window.CanMaximizeProperty || e.Property == Window.CanResizeProperty || e.Property == TitleBar.ExtendsProperty)
            Refresh();
    }

    // A sample keeps no caption roles, or Windows would treat a click on it as one on the window's own buttons
    private void SetRoles()
    {
        var sample = Preview != CaptionPreview.Off;
        if (_minimize is not null) WindowDecorationProperties.SetElementRole(_minimize, sample ? WindowDecorationsElementRole.None : WindowDecorationsElementRole.MinimizeButton);
        if (_maximize is not null) WindowDecorationProperties.SetElementRole(_maximize, sample ? WindowDecorationsElementRole.None : WindowDecorationsElementRole.MaximizeButton);
        if (_close is not null) WindowDecorationProperties.SetElementRole(_close, sample ? WindowDecorationsElementRole.None : WindowDecorationsElementRole.CloseButton);
    }

    internal void Refresh()
    {
        var preview = Preview;
        var w = preview == CaptionPreview.Off ? _window : null;
        var shown = preview != CaptionPreview.Off || (w is not null && (ForceShown || TitleBar.IsActive(w)) && w.WindowState != WindowState.FullScreen);
        var maximized = preview == CaptionPreview.Maximized || w?.WindowState == WindowState.Maximized;
        PseudoClasses.Set(":shown", shown);
        PseudoClasses.Set(":maximized", maximized);
        var inactive = preview == CaptionPreview.Inactive || (w is not null && !w.IsActive);
        PseudoClasses.Set(":inactive", inactive);
        foreach (var button in new[] { _minimize, _maximize, _close }) button?.Classes.Set("inactive", inactive);
        if (_minimize is not null) _minimize.IsVisible = w?.CanMinimize ?? true;
        if (_maximize is not null)
        {
            _maximize.IsVisible = w is null || (w.CanMaximize && w.CanResize);
            var text = maximized ? RestoreText : MaximizeText;
            ToolTip.SetTip(_maximize, text);
            AutomationProperties.SetName(_maximize, text);
        }
    }

    // A handled move over a caption button never reaches DefWindowProc, so Windows shows no tooltip of its own in the OS language beside ours
    private static void KeepMoveFromWindows(object? sender, PointerEventArgs e) => e.Handled = true;

    private Window? Served => Preview == CaptionPreview.Off ? _window : null;

    private void OnMinimize(object? sender, global::Avalonia.Interactivity.RoutedEventArgs e) { if (Served is { } w) w.WindowState = WindowState.Minimized; }
    private void OnMaximize(object? sender, global::Avalonia.Interactivity.RoutedEventArgs e) { if (Served is { } w) w.WindowState = w.WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized; }
    private void OnClose(object? sender, global::Avalonia.Interactivity.RoutedEventArgs e) => Served?.Close();
}
