using System.Windows.Input;

namespace Insiyab.Avalonia.Controls;

// A command that always runs, for the content controls' Start/Stop/Copy commands.
internal sealed class ContentCommand(Action run) : ICommand
{
    public event EventHandler? CanExecuteChanged { add { } remove { } }

    public bool CanExecute(object? parameter) => true;

    public void Execute(object? parameter) => run();
}
