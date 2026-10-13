using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Controls.Chrome;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Primitives;
using Avalonia.Input;

namespace Insiyab.Avalonia.Controls;

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

    public string MinimizeText { get => GetValue(MinimizeTextProperty); set => SetValue(MinimizeTextProperty, value); }
    public string MaximizeText { get => GetValue(MaximizeTextProperty); set => SetValue(MaximizeTextProperty, value); }
    public string RestoreText { get => GetValue(RestoreTextProperty); set => SetValue(RestoreTextProperty, value); }
    public string CloseText { get => GetValue(CloseTextProperty); set => SetValue(CloseTextProperty, value); }

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
        if (_minimize is not null) { _minimize.Click += OnMinimize; WindowDecorationProperties.SetElementRole(_minimize, WindowDecorationsElementRole.MinimizeButton); }
        if (_maximize is not null) { _maximize.Click += OnMaximize; WindowDecorationProperties.SetElementRole(_maximize, WindowDecorationsElementRole.MaximizeButton); }
        if (_close is not null) { _close.Click += OnClose; WindowDecorationProperties.SetElementRole(_close, WindowDecorationsElementRole.CloseButton); }
        foreach (var button in new[] { _minimize, _maximize, _close }) button?.AddHandler(PointerMovedEvent, KeepMoveFromWindows);
        Refresh();
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        _window = TopLevel.GetTopLevel(this) as Window;
        if (_window is not null) _window.PropertyChanged += OnWindowChanged;
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
        if (change.Property == MaximizeTextProperty || change.Property == RestoreTextProperty) Refresh();
    }

    private void OnWindowChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property == Window.WindowStateProperty || e.Property == WindowBase.IsActiveProperty || e.Property == Window.IsExtendedIntoWindowDecorationsProperty
            || e.Property == Window.CanMinimizeProperty || e.Property == Window.CanMaximizeProperty || e.Property == Window.CanResizeProperty || e.Property == TitleBar.ExtendsProperty)
            Refresh();
    }

    internal void Refresh()
    {
        var w = _window;
        var shown = w is not null && (ForceShown || TitleBar.IsActive(w)) && w.WindowState != WindowState.FullScreen;
        var maximized = w?.WindowState == WindowState.Maximized;
        PseudoClasses.Set(":shown", shown);
        PseudoClasses.Set(":maximized", maximized);
        var inactive = w is not null && !w.IsActive;
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

    private void OnMinimize(object? sender, global::Avalonia.Interactivity.RoutedEventArgs e) { if (_window is not null) _window.WindowState = WindowState.Minimized; }
    private void OnMaximize(object? sender, global::Avalonia.Interactivity.RoutedEventArgs e) { if (_window is not null) _window.WindowState = _window.WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized; }
    private void OnClose(object? sender, global::Avalonia.Interactivity.RoutedEventArgs e) => _window?.Close();
}
