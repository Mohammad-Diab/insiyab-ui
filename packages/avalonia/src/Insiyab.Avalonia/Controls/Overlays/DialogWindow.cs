using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Threading;

namespace Insiyab.Avalonia.Controls;

// A dialog in its own owned window, for desktop cases such as a settings window that can move to another screen.
internal sealed class DialogWindow : Window
{
    private readonly Dialog _dialog;
    private readonly Control _from;
    private readonly Window _owner;
    private bool _closing;

    public DialogWindow(Dialog dialog, Control from, Window owner)
    {
        _dialog = dialog;
        _from = from;
        _owner = owner;
        Title = dialog.Title as string ?? string.Empty;
        FlowDirection = from.FlowDirection;
        RequestedThemeVariant = from.ActualThemeVariant;
        Width = double.IsInfinity(dialog.MaxWidth) ? 608 : dialog.MaxWidth;
        SizeToContent = SizeToContent.Height;
        MinWidth = 320;
        CanResize = false;
        ShowInTaskbar = false;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        Content = dialog;
        Closing += OnClosing;
        Closed += (_, _) =>
        {
            Content = null;
            _owner.Activate();
            _from.Focus();
            _dialog.OnWindowGone();
        };
        Opened += (_, _) => Dispatcher.UIThread.Post(() => _dialog.FocusTarget()?.Focus(NavigationMethod.Tab), DispatcherPriority.Loaded);
    }

    public void Present(bool modal)
    {
        if (modal) _ = ShowDialog(_owner);
        else Show(_owner);
    }

    public void CloseFromDialog()
    {
        _closing = true;
        Close();
    }

    private void OnClosing(object? sender, WindowClosingEventArgs e)
    {
        if (_closing) return;
        if (_dialog.EffectiveClosedBy == DialogClosedBy.None) e.Cancel = true;
    }
}
