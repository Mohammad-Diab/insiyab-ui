using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Insiyab.Avalonia.Controls;
using static Insiyab.Avalonia.Tests.Ui;

namespace Insiyab.Avalonia.Tests;

// ListRow.Invoked and ListRow.Command: a destination list's row opens on a click, Enter or Space; headings, buttons inside a row and a drag off the row do not.
public class ListRowInvokeTests
{
    private sealed class Recorder : ICommand
    {
        public List<object?> Calls { get; } = [];
        public event EventHandler? CanExecuteChanged { add { } remove { } }
        public bool CanExecute(object? parameter) => true;
        public void Execute(object? parameter) => Calls.Add(parameter);
    }

    private static void Key(Window w, Key key, PhysicalKey physical)
    {
        w.KeyPress(key, RawInputModifiers.None, physical, null);
        w.KeyRelease(key, RawInputModifiers.None, physical, null);
        Settle(120);
    }

    private static (ListBox List, Window Window, Recorder Command) Accounts(params string[] names)
    {
        var command = new Recorder();
        var list = new ListBox { ItemsSource = names, ItemTemplate = new FuncDataTemplate<string>((n, _) => new TextBlock { Classes = { "list-title" }, Text = n }) };
        ListRow.SetCommand(list, command);
        var window = Show(list, FlowDirection.RightToLeft, width: 420, height: 300);
        return (list, window, command);
    }

    private static ListBoxItem RowAt(ListBox list, int index) => Assert.IsType<ListBoxItem>(list.ContainerFromIndex(index));

    [AvaloniaFact]
    public void A_click_on_a_row_runs_the_command_with_its_item_and_raises_invoked()
    {
        var (list, w, command) = Accounts("محمد", "سارة");
        var raised = new List<object?>();
        ListRow.AddInvokedHandler(list, (_, e) => raised.Add(e.Item));

        Click(RowAt(list, 1), w);
        Assert.Equal(["سارة"], raised);
        Assert.Equal(["سارة"], command.Calls);

        // pressed again while already selected: still a click
        Click(RowAt(list, 1), w);
        Assert.Equal(2, command.Calls.Count);
    }

    [AvaloniaFact]
    public void A_handled_invoked_skips_the_command()
    {
        var (list, w, command) = Accounts("محمد");
        ListRow.AddInvokedHandler(list, (_, e) => e.Handled = true);
        Click(RowAt(list, 0), w);
        Assert.Empty(command.Calls);
    }

    [AvaloniaFact]
    public void A_press_released_over_another_row_is_not_a_click()
    {
        var (list, w, command) = Accounts("محمد", "سارة");
        w.MouseMove(Center(RowAt(list, 0), w));
        w.MouseDown(Center(RowAt(list, 0), w), MouseButton.Left);
        w.MouseMove(Center(RowAt(list, 1), w));
        w.MouseUp(Center(RowAt(list, 1), w), MouseButton.Left);
        Settle(120);
        Assert.Empty(command.Calls);

        // nor a right click
        var at = Center(RowAt(list, 0), w);
        w.MouseDown(at, MouseButton.Right);
        w.MouseUp(at, MouseButton.Right);
        Settle(120);
        Assert.Empty(command.Calls);
    }

    [AvaloniaFact]
    public void Enter_and_space_on_the_focused_row_invoke_it()
    {
        var (list, w, command) = Accounts("محمد", "سارة");
        RowAt(list, 0).Focus(NavigationMethod.Tab);
        Settle(120);
        Key(w, global::Avalonia.Input.Key.Down, PhysicalKey.ArrowDown);
        Key(w, global::Avalonia.Input.Key.Enter, PhysicalKey.Enter);
        Assert.Equal(["سارة"], command.Calls);
        Key(w, global::Avalonia.Input.Key.Space, PhysicalKey.Space);
        Assert.Equal(["سارة", "سارة"], command.Calls);
    }

    [AvaloniaFact]
    public void Headings_and_buttons_inside_a_row_do_not_invoke()
    {
        var heading = new ListBoxItem { Content = "الحسابات" };
        ListRow.SetIsHeading(heading, true);
        var button = new Button { Content = "احذف" };
        var row = new ListBoxItem { Content = new DockPanel { Children = { button, new TextBlock { Classes = { "list-title" }, Text = "محمد" } } } };
        var list = new ListBox { Items = { heading, row } };
        var raised = new List<object?>();
        ListRow.AddInvokedHandler(list, (_, e) => raised.Add(e.Item));
        var w = Show(list, FlowDirection.RightToLeft, width: 420, height: 300);

        Click(heading, w);
        Click(button, w);
        Assert.Empty(raised);
        Click(row, w);
        Assert.Equal([row], raised);

        // Enter on the button inside the row is the button's
        button.Focus(NavigationMethod.Tab);
        Settle(120);
        Key(w, global::Avalonia.Input.Key.Enter, PhysicalKey.Enter);
        Assert.Single(raised);
    }

    [AvaloniaFact]
    public void A_combo_box_or_slider_inside_a_row_keeps_its_click()
    {
        var combo = new ComboBox { Width = 110, ItemsSource = new[] { "يومي", "أسبوعي" } };
        var slider = new Slider { Width = 110 };
        var title = new TextBlock { Classes = { "list-title" }, Text = "التنبيهات" };
        DockPanel.SetDock(combo, Dock.Right);
        DockPanel.SetDock(slider, Dock.Right);
        var row = new ListBoxItem { Content = new DockPanel { Children = { combo, slider, title } } };
        var list = new ListBox { Items = { row } };
        var raised = new List<object?>();
        ListRow.AddInvokedHandler(list, (_, e) => raised.Add(e.Item));
        var w = Show(list, FlowDirection.RightToLeft, width: 520, height: 300);

        Click(slider, w);
        Assert.Empty(raised);
        Click(combo, w);
        Assert.Empty(raised);
        Key(w, global::Avalonia.Input.Key.Escape, PhysicalKey.Escape);
        Click(title, w);
        Assert.Equal([row], raised);
    }
}
