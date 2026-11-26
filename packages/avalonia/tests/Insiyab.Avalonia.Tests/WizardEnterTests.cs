using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Input.TextInput;
using Insiyab.Avalonia.Controls;
using static Insiyab.Avalonia.Tests.Ui;

namespace Insiyab.Avalonia.Tests;

// A sign-in step's Enter (EnterFillsFields, KeyboardEcho): Enter in a field goes to the next empty required one before it finishes, a touch keyboard's keys say "next" then "done", and the Enter a touch keyboard sends right after its "next" moved on is not the user's.
public class WizardEnterTests
{
    private static Wizard SignIn(out TextBox name, out TextBox password, bool fills = true, TimeSpan echo = default)
    {
        name = new TextBox();
        Wizard.SetRequired(name, true);
        password = new TextBox { PasswordChar = '•' };
        Wizard.SetRequired(password, true);
        return new Wizard
        {
            EnterFillsFields = fills,
            KeyboardEcho = echo,
            Items = { new WizardPanel { Header = "الدخول", Content = new StackPanel { Children = { name, password } } } },
        };
    }

    private static void Enter(Window w)
    {
        w.KeyPress(Key.Enter, RawInputModifiers.None, PhysicalKey.Enter, null);
        w.KeyRelease(Key.Enter, RawInputModifiers.None, PhysicalKey.Enter, null);
        Settle(150);
    }

    [AvaloniaFact]
    public void Enter_goes_to_the_next_empty_required_field_then_finishes()
    {
        var wiz = SignIn(out var name, out var password);
        var finished = 0;
        wiz.Finished += (_, _) => finished++;
        var w = Show(wiz);
        name.Text = "user_123456";
        name.Focus();
        Settle(50);
        Enter(w);
        Assert.True(password.IsFocused);   // the password is empty: it takes the focus, nothing is finished
        Assert.Equal(0, finished);

        // typed, the password's own Enter finishes
        password.Text = "secret";
        Enter(w);
        Assert.Equal(1, finished);

        // with the password filled, Enter in the name finishes as well
        name.Focus();
        Settle(50);
        Enter(w);
        Assert.Equal(2, finished);
    }

    [AvaloniaFact]
    public void Off_Enter_finishes_at_once_as_before()
    {
        var wiz = SignIn(out var name, out var password, fills: false);
        var w = Show(wiz);
        name.Text = "user_123456";
        name.Focus();
        Settle(50);
        Enter(w);
        Assert.Equal(TextInputReturnKeyType.Default, TextInputOptions.GetReturnKeyType(name));
        // the step was checked: the empty password is flagged
        Assert.True(DataValidationErrors.GetHasErrors(password));
    }

    [AvaloniaFact]
    public void A_touch_keyboard_shows_next_then_done()
    {
        var wiz = SignIn(out var name, out var password);
        Show(wiz);
        Assert.Equal(TextInputReturnKeyType.Next, TextInputOptions.GetReturnKeyType(name));
        Assert.Equal(TextInputReturnKeyType.Done, TextInputOptions.GetReturnKeyType(password));
    }

    [AvaloniaFact]
    public void The_enter_right_after_the_keyboard_moved_on_is_its_echo_and_is_ignored()
    {
        var wiz = SignIn(out var name, out var password, echo: TimeSpan.FromMilliseconds(300));
        var finished = 0;
        wiz.Finished += (_, _) => finished++;
        var w = Show(wiz);
        name.Text = "user_123456";
        password.Text = "secret";
        name.Focus();
        Settle(50);
        // the keyboard's "next" moves the focus on, then sends an Enter to the field it reached
        password.Focus(NavigationMethod.Tab);
        w.KeyPress(Key.Enter, RawInputModifiers.None, PhysicalKey.Enter, null);
        w.KeyRelease(Key.Enter, RawInputModifiers.None, PhysicalKey.Enter, null);
        Settle(50);
        Assert.Equal(0, finished);
        Assert.True(password.IsFocused);

        // the user's own Enter later finishes
        Settle(400);
        Enter(w);
        Assert.Equal(1, finished);
    }
}
