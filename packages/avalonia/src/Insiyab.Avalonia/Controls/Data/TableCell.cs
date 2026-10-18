using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;

namespace Insiyab.Avalonia.Controls;

/// <summary>One cell of a <see cref="TableRow"/>: the web's <c>&lt;td&gt;</c>.</summary>
[TemplatePart("PART_EditorHost", typeof(Panel))]
[PseudoClasses(":editable", ":editing", ":pinned", ":pin-edge", ":pin-flip", ":check", ":current")]
public class TableCell : ContentControl
{
    public static readonly StyledProperty<string?> SortValueProperty =
        AvaloniaProperty.Register<TableCell, string?>(nameof(SortValue));

    public static readonly StyledProperty<string?> ValueProperty =
        AvaloniaProperty.Register<TableCell, string?>(nameof(Value));

    private Panel? _editorHost;
    private Control? _editor;
    private object? _oldContent;
    private string? _oldValue;
    private TableColumn? _column;

    static TableCell()
    {
        FocusableProperty.OverrideDefaultValue<TableCell>(false);
        // A cell's text is a TemplateText, not a TextBlock: a big table has thousands of them, and each TextBlock would carry every app-wide TextBlock style.
        ContentTemplateProperty.OverrideDefaultValue<TableCell>(TemplateText.Content);
    }

    /// <summary>What the column sorts this cell by, when its text is not its value (a date in words): the web's <c>data-sort-value</c>.</summary>
    public string? SortValue { get => GetValue(SortValueProperty); set => SetValue(SortValueProperty, value); }

    /// <summary>The cell's value when it differs from its text: what sorting falls back to and what an edit starts from. The web's <c>data-value</c>.</summary>
    public string? Value { get => GetValue(ValueProperty); set => SetValue(ValueProperty, value); }

    /// <summary>The column this cell sits in.</summary>
    public TableColumn? Column
    {
        get => _column;
        internal set
        {
            _column = value;
            var editable = value is { Edit: not TableEdit.None };
            PseudoClasses.Set(":editable", editable);
        }
    }

    /// <summary>The row this cell belongs to.</summary>
    public TableRow? Row { get; internal set; }

    /// <summary>Whether the cell is a field right now.</summary>
    public bool IsEditing => _editor is not null;

    internal bool IsCheck { get; set; }

    internal void SetPinned(bool on, bool edge = false, bool flip = false)
    {
        PseudoClasses.Set(":pinned", on);
        PseudoClasses.Set(":pin-edge", edge);
        PseudoClasses.Set(":pin-flip", flip);
    }

    internal void SetCurrent(bool on) => PseudoClasses.Set(":current", on);

    internal void MarkCheck()
    {
        IsCheck = true;
        PseudoClasses.Set(":check", true);
    }

    /// <summary>The cell's text: its content as text, or the column's <see cref="TableColumn.CellText"/> for its row's item.</summary>
    public string Text
    {
        get
        {
            if (Row?.Item is { } item && _column?.CellText is { } text) return text(item) ?? "";
            return Content switch
            {
                null => "",
                string s => s,
                TextBlock t => t.Text ?? "",
                _ => Content.ToString() ?? "",
            };
        }
    }

    /// <summary>What the column sorts this cell by: <see cref="SortValue"/>, then <see cref="Value"/>, then <see cref="Text"/>.</summary>
    internal string SortKey => (SortValue ?? Value ?? Text).Trim();

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);
        _editorHost = e.NameScope.Find<Panel>("PART_EditorHost");
    }

    /// <summary>Turns the cell into a field holding its value, or <paramref name="text"/> when given.</summary>
    public void BeginEdit(string? text = null)
    {
        if (IsEditing || _column is null || _column.Edit == TableEdit.None) return;
        ApplyTemplate();
        if (_editorHost is null) return;
        var old = (Value ?? Text).Trim();
        _oldValue = old;
        _oldContent = Content;
        Control editor;
        if (_column.Edit == TableEdit.Select)
        {
            var options = (_column.Options ?? "").Split('|', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
            var box = new ComboBox { ItemsSource = options, SelectedItem = Array.IndexOf(options, old) >= 0 ? old : null };
            box.DropDownClosed += (_, _) =>
            {
                EndEdit(true);
                Row?.Owner?.Focus();
            };
            editor = box;
        }
        else
        {
            var box = new TextBox { Text = text ?? old };
            if (_column.Edit == TableEdit.Number) box.FlowDirection = FlowDirection.LeftToRight;
            editor = box;
        }
        editor.Classes.Add("sm");
        editor.Classes.Add("table-editor");
        editor.VerticalAlignment = VerticalAlignment.Center;
        editor.HorizontalAlignment = HorizontalAlignment.Stretch;
        if (_column.Header is string name) AutomationProperties.SetName(editor, name);
        editor.AddHandler(KeyDownEvent, OnEditorKey, RoutingStrategies.Tunnel);
        _editor = editor;
        _editorHost.Children.Add(editor);
        PseudoClasses.Set(":editing", true);
        Dispatcher.UIThread.Post(() =>
        {
            if (_editor != editor) return;
            editor.Focus();
            if (editor is TextBox t) { if (text is null) t.SelectAll(); else t.CaretIndex = t.Text?.Length ?? 0; }
            editor.LostFocus += OnEditorLostFocus;
        }, DispatcherPriority.Input);
    }

    private void OnEditorKey(object? sender, KeyEventArgs e)
    {
        if (e.Key is not (Key.Enter or Key.Escape or Key.Tab)) return;
        if (sender is ComboBox { IsDropDownOpen: true } && e.Key is Key.Enter or Key.Escape) return;
        e.Handled = true;
        EndEdit(e.Key != Key.Escape);
        Row?.Owner?.AfterEdit(this, e.Key, e.KeyModifiers.HasFlag(KeyModifiers.Shift));
    }

    private void OnEditorLostFocus(object? sender, RoutedEventArgs e)
    {
        if (sender is ComboBox { IsDropDownOpen: true }) return;
        if (sender is Control c && c.IsKeyboardFocusWithin) return;
        // Another window took the keyboard: the editor stays open and gets it back with this window.
        if (WindowActivity.GetIsInactive(this)) return;
        EndEdit(true);
    }

    /// <summary>Keeps the change (unless <see cref="Table.CellEditing"/> refuses it) or puts the old value back. Returns whether the cell changed.</summary>
    public bool EndEdit(bool keep)
    {
        if (_editor is not { } editor) return false;
        _editor = null;
        editor.LostFocus -= OnEditorLostFocus;
        var value = (editor switch
        {
            TextBox t => t.Text,
            ComboBox c => c.SelectedItem as string,
            _ => null,
        } ?? _oldValue ?? "").Trim();
        _editorHost?.Children.Remove(editor);
        PseudoClasses.Set(":editing", false);
        var old = _oldValue ?? "";
        var oldContent = _oldContent;
        var hadValue = Value is not null;
        var changed = keep && value != old;
        if (changed && Row?.Owner is { } table)
        {
            var args = new TableEditEventArgs(this, Row, value, old, () =>
            {
                Content = oldContent;
                if (hadValue) Value = old;
            });
            table.RaiseCellEditing(args);
            if (args.Cancel) changed = false;
        }
        if (changed)
        {
            Content = value;
            if (hadValue) Value = value;
        }
        return changed;
    }
}

/// <summary>A cell edit about to be kept: set <see cref="System.ComponentModel.CancelEventArgs.Cancel"/> to refuse the value, or call <see cref="Revert"/> later if saving it fails.</summary>
public sealed class TableEditEventArgs : System.ComponentModel.CancelEventArgs
{
    private readonly Action _revert;

    internal TableEditEventArgs(TableCell cell, TableRow row, string value, string old, Action revert)
    {
        Cell = cell;
        Row = row;
        Value = value;
        Old = old;
        _revert = revert;
    }

    public TableCell Cell { get; }
    public TableRow Row { get; }
    public string Value { get; }
    public string Old { get; }

    /// <summary>Puts the old value back.</summary>
    public void Revert() => _revert();
}
