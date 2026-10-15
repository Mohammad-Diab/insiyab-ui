using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Notifications;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Insiyab.Avalonia.Controls;

using static Insiyab.Avalonia.Tests.Ui;

namespace Insiyab.Avalonia.Tests;

// Switching to another window is not leaving the field: nothing is checked or committed, and the field is as it was when the window comes back.
[Collection("Pickers")]
public class WindowSwitchTests
{
    private static void Platform(Window w, string callback) =>
        ((Action?)w.PlatformImpl!.GetType().GetProperty(callback)!.GetValue(w.PlatformImpl))?.Invoke();

    // Windows sends WM_ACTIVATE before WM_KILLFOCUS: the window deactivates, then loses the keyboard.
    private static void Deactivate(Window w)
    {
        Assert.True(w.IsActive);
        Platform(w, "Deactivated");
        Platform(w, "LostFocus");
        Dispatcher.UIThread.RunJobs();
        Settle(100);
    }

    private static void Activate(Window w)
    {
        Platform(w, "Activated");
        Dispatcher.UIThread.RunJobs();
        Settle(100);
    }

    private static void Tab(Window w)
    {
        w.KeyPress(Key.Tab, RawInputModifiers.None, PhysicalKey.Tab, null);
        w.KeyRelease(Key.Tab, RawInputModifiers.None, PhysicalKey.Tab, null);
        Settle(150);
    }

    private static StackPanel Page(params Control[] children)
    {
        var page = new StackPanel { Margin = new Thickness(20), Spacing = 16, Width = 360, HorizontalAlignment = HorizontalAlignment.Left };
        page.Children.AddRange(children);
        return page;
    }

    [AvaloniaFact]
    public void A_changed_field_is_not_checked_when_the_window_is_left()
    {
        var mail = new TextBox();
        Validate.SetEmail(mail, true);
        var next = new TextBox();
        var w = Show(Page(mail, next));
        mail.Focus(NavigationMethod.Tab);
        Settle(100);
        w.KeyTextInput("abc");
        Settle(100);
        Assert.Equal("abc", mail.Text);

        Deactivate(w);
        Assert.False(mail.IsFocused);
        Assert.DoesNotContain(":error", mail.Classes);
        Assert.False(DataValidationErrors.GetHasErrors(mail));

        Activate(w);
        Assert.True(mail.IsFocused);
        Assert.Equal("abc", mail.Text);
        Assert.DoesNotContain(":error", mail.Classes);

        Tab(w);
        Assert.True(next.IsFocused);
        Assert.Contains(":error", mail.Classes);
        Assert.Contains("اكتب بريدًا إلكترونيًا صحيحًا.", DataValidationErrors.GetErrors(mail)!.Cast<object>());
    }

    [AvaloniaFact]
    public void A_typed_date_is_not_committed_when_the_window_is_left()
    {
        var sep30 = new DateTime(2026, 9, 30);
        var field = new DateField { SelectedDate = sep30 };
        var changes = 0;
        field.DateChanged += (_, _) => changes++;
        var next = new TextBox();
        var w = Show(Page(field, next), FlowDirection.RightToLeft);
        field.Focus(NavigationMethod.Tab);
        field.SelectAll();
        w.KeyTextInput("1/10/2026");
        Settle(100);

        Deactivate(w);
        Assert.False(field.IsFocused);
        Assert.Equal(sep30, field.SelectedDate);
        Assert.Equal("1/10/2026", field.Text);
        Assert.Equal(0, changes);

        Activate(w);
        Assert.True(field.IsFocused);
        Assert.Equal("1/10/2026", field.Text);
        Assert.Equal(sep30, field.SelectedDate);

        Tab(w);
        Assert.False(field.IsFocused);
        Assert.Equal(new DateTime(2026, 10, 1), field.SelectedDate);
        Assert.Equal("1 أكتوبر 2026", field.Text);
        Assert.Equal(1, changes);
    }

    [AvaloniaFact]
    public void A_cell_editor_stays_open_when_the_window_is_left()
    {
        var table = new Table();
        table.Columns.Add(new TableColumn { Header = "الاسم", Edit = TableEdit.Text });
        table.Columns.Add(new TableColumn { Header = "المدينة", Edit = TableEdit.Text });
        foreach (var (name, city) in new[] { ("ليلى", "الرياض"), ("عمر", "عمّان") })
        {
            var row = new TableRow();
            row.Cells.Add(new TableCell { Content = name });
            row.Cells.Add(new TableCell { Content = city });
            table.Rows.Add(row);
        }
        var w = Show(table, width: 700);
        var cell = table.DisplayedRows[0].Cells[0];
        var p = Center(cell, w);
        w.MouseMove(p);
        w.MouseDown(p, MouseButton.Left);
        w.MouseUp(p, MouseButton.Left);
        w.MouseDown(p, MouseButton.Left);
        w.MouseUp(p, MouseButton.Left);
        Settle(300);
        Assert.True(cell.IsEditing);
        var editor = cell.GetVisualDescendants().OfType<TextBox>().Single();
        editor.Text = "ليلى حداد";

        Deactivate(w);
        Assert.True(cell.IsEditing);
        Assert.Equal("ليلى", cell.Text);

        Activate(w);
        Assert.True(cell.IsEditing);
        Assert.True(editor.IsFocused);
        Assert.Equal("ليلى حداد", editor.Text);

        Tab(w);
        Assert.False(cell.IsEditing);
        Assert.Equal("ليلى حداد", cell.Text);
    }

    [AvaloniaFact]
    public void A_typed_phone_is_not_checked_when_the_window_is_left()
    {
        var phone = new PhoneBox();
        var next = new TextBox();
        var w = Show(Page(phone, next), FlowDirection.RightToLeft);
        var input = Part<TextBox>(phone, "PART_Input");
        input.Focus(NavigationMethod.Tab);
        Settle(150);
        input.Text = "05012";
        Settle(150);
        var typed = input.Text;

        Deactivate(w);
        Assert.DoesNotContain(":invalid", phone.Classes);
        Assert.False(DataValidationErrors.GetHasErrors(input));

        Activate(w);
        Assert.True(input.IsFocused);
        Assert.Equal(typed, input.Text);
        Assert.False(DataValidationErrors.GetHasErrors(input));

        Tab(w);
        Assert.False(input.IsFocused);
        Assert.Contains(":invalid", phone.Classes);
        Assert.Equal("اكتب رقم هاتف صحيحًا في المملكة العربية السعودية.", phone.ValidationMessage);
        Assert.True(DataValidationErrors.GetHasErrors(input));
    }

    private static void Tick(int milliseconds)
    {
        var clock = System.Diagnostics.Stopwatch.StartNew();
        while (clock.ElapsedMilliseconds < milliseconds)
        {
            Dispatcher.UIThread.Post(() => { });
            Settle(16);
        }
    }

    [AvaloniaFact]
    public void A_toast_the_keyboard_is_on_waits_while_the_window_is_away()
    {
        var button = new Button { Content = "حفظ" };
        var w = Show(Page(button));
        var hold = Toast.Duration;
        Toast.Duration = TimeSpan.FromMilliseconds(300);
        try
        {
            Toast.Show(button, "تم الحفظ", ToastTone.Ok);
            Settle(100);
            var card = Toast.ManagerFor(button)!.GetVisualDescendants().OfType<NotificationCard>().Single();
            card.Focus();
            Settle(50);

            Deactivate(w);
            Tick(700);
            Assert.False(card.IsClosing, "the toast left while its window was away");

            Activate(w);
            button.Focus();
            Tick(700);
            Assert.True(card.IsClosing, "the toast did not leave once the reader left it");
        }
        finally
        {
            Toast.Duration = hold;
        }
    }
}
