using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.VisualTree;

namespace Insiyab.Avalonia.Controls;

/// <summary>Where a row stands in time: <c>Past</c> is quieter, <c>Current</c> wears the brand's start mark.</summary>
public enum ListRowState
{
    None,
    Past,
    Current,
}

/// <summary>What a <c>ListBoxItem</c> is in its list: a group heading (<c>IsHeading</c>) or a row's place in time (<c>State</c>); a row opens on a click, Enter or Space, raising <c>Invoked</c> and then the list's <c>Command</c>.</summary>
public static class ListRow
{
    /// <summary>Raised from a row (never a heading) when it is clicked with the left button (pressed and released on it), or Enter or Space is pressed on it; bubbles to the list. A button, text box, combo box or slider inside the row keeps its own click and keys.</summary>
    public static readonly RoutedEvent<ListRowInvokedEventArgs> InvokedEvent =
        RoutedEvent.Register<ListRowInvokedEventArgs>("Invoked", RoutingStrategies.Bubble, typeof(ListRow));

    /// <summary>Set on a <c>ListBox</c>: run with the invoked row's item (the row itself for a <c>ListBoxItem</c> written in the list) when <c>Invoked</c> is left unhandled and the command can run.</summary>
    public static readonly AttachedProperty<ICommand?> CommandProperty =
        AvaloniaProperty.RegisterAttached<ListBox, ICommand?>("Command", typeof(ListRow));

    // the row the left button went down on, so a release over another row (or outside) is not a click
    private static readonly AttachedProperty<ListBoxItem?> PressedProperty =
        AvaloniaProperty.RegisterAttached<ListBox, ListBoxItem?>("Pressed", typeof(ListRow));

    public static readonly AttachedProperty<bool> IsHeadingProperty =
        AvaloniaProperty.RegisterAttached<ListBoxItem, bool>("IsHeading", typeof(ListRow));

    public static readonly AttachedProperty<ListRowState> StateProperty =
        AvaloniaProperty.RegisterAttached<ListBoxItem, ListRowState>("State", typeof(ListRow));

    static ListRow()
    {
        IsHeadingProperty.Changed.AddClassHandler<ListBoxItem>((item, e) => ListRows.HeadingChanged(item, e.GetNewValue<bool>()));
        // the list's own selection handles the press first, so the click is read from handled events too
        InputElement.PointerPressedEvent.AddClassHandler<ListBox>(OnPressed, RoutingStrategies.Bubble, handledEventsToo: true);
        InputElement.PointerReleasedEvent.AddClassHandler<ListBox>(OnReleased, RoutingStrategies.Bubble, handledEventsToo: true);
        // the list's own keys mark Enter handled, so the keys are read from handled events too (a button, text box, combo box or slider inside a row is left out by RowOf)
        InputElement.KeyDownEvent.AddClassHandler<ListBox>(OnKeyDown, RoutingStrategies.Bubble, handledEventsToo: true);
    }

    // Called by the rows' tracker, which every themed row turns on, so the click handlers are in place before any list takes input.
    internal static void EnsureListening() { }

    public static ICommand? GetCommand(ListBox list) => list.GetValue(CommandProperty);
    public static void SetCommand(ListBox list, ICommand? value) => list.SetValue(CommandProperty, value);

    public static void AddInvokedHandler(Interactive element, EventHandler<ListRowInvokedEventArgs> handler) => element.AddHandler(InvokedEvent, handler);
    public static void RemoveInvokedHandler(Interactive element, EventHandler<ListRowInvokedEventArgs> handler) => element.RemoveHandler(InvokedEvent, handler);

    // The list's own row the input came from: none for a heading, a disabled row, a row of a list nested in it, or a button, text box, combo box or slider inside the row.
    private static ListBoxItem? RowOf(ListBox list, object? source)
    {
        for (var v = source as Visual; v is not null && v != list; v = v.GetVisualParent())
        {
            if (v is Button or TextBox or ComboBox or Slider) return null;
            if (v is ListBoxItem row)
                return ItemsControl.ItemsControlFromItemContainer(row) == list && !GetIsHeading(row) && row.IsEffectivelyEnabled ? row : null;
        }
        return null;
    }

    private static void OnPressed(ListBox list, PointerPressedEventArgs e) =>
        list.SetValue(PressedProperty, e.GetCurrentPoint(list).Properties.IsLeftButtonPressed ? RowOf(list, e.Source) : null);

    private static void OnReleased(ListBox list, PointerReleasedEventArgs e)
    {
        var pressed = list.GetValue(PressedProperty);
        list.ClearValue(PressedProperty);
        if (e.InitialPressMouseButton != MouseButton.Left || pressed is null || RowOf(list, e.Source) != pressed) return;
        // the pressed row holds the pointer, so the release comes from it wherever the pointer is: it counts only over the row
        if (!new Rect(pressed.Bounds.Size).Contains(e.GetPosition(pressed))) return;
        Invoke(list, pressed);
    }

    private static void OnKeyDown(ListBox list, KeyEventArgs e)
    {
        if (e.KeyModifiers != KeyModifiers.None || e.Key is not (Key.Enter or Key.Space)) return;
        var row = e.Source == list ? (list.SelectedItem is { } item ? list.ContainerFromItem(item) as ListBoxItem : null) : RowOf(list, e.Source);
        if (row is not null && !GetIsHeading(row) && Invoke(list, row)) e.Handled = true;
    }

    // Raises Invoked, then runs the command; true when either acted on it.
    private static bool Invoke(ListBox list, ListBoxItem row)
    {
        var item = list.ItemFromContainer(row);
        var args = new ListRowInvokedEventArgs(row, item);
        row.RaiseEvent(args);
        if (args.Handled) return true;
        if (GetCommand(list) is not { } command || !command.CanExecute(item)) return false;
        command.Execute(item);
        return true;
    }

    public static bool GetIsHeading(ListBoxItem item) => item.GetValue(IsHeadingProperty);
    public static void SetIsHeading(ListBoxItem item, bool value) => item.SetValue(IsHeadingProperty, value);
    public static ListRowState GetState(ListBoxItem item) => item.GetValue(StateProperty);
    public static void SetState(ListBoxItem item, ListRowState value) => item.SetValue(StateProperty, value);
}

/// <summary>The row a list raised <see cref="ListRow.InvokedEvent"/> for, and its item.</summary>
public sealed class ListRowInvokedEventArgs : RoutedEventArgs
{
    internal ListRowInvokedEventArgs(ListBoxItem row, object? item) : base(ListRow.InvokedEvent, row)
    {
        Row = row;
        Item = item;
    }

    /// <summary>The row clicked or keyed.</summary>
    public ListBoxItem Row { get; }

    /// <summary>The row's item in the list's <c>ItemsSource</c>, or the row itself when it was written in the list.</summary>
    public object? Item { get; }
}
