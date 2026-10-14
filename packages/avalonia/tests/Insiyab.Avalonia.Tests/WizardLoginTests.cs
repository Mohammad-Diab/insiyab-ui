using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.VisualTree;
using Insiyab.Avalonia.Controls;
using static Insiyab.Avalonia.Tests.Ui;

namespace Insiyab.Avalonia.Tests;

// What a sign-in card asks of the wizard and the code box: the step's Validate rules on next, a server that answers before a move or the finish, the dots, the foot and the code's own messages.
public class WizardLoginTests
{
    private static Wizard SignIn(out TextBox phone, out TextBox password, out OtpBox code)
    {
        phone = new TextBox { FlowDirection = FlowDirection.LeftToRight };
        Validate.SetRequired(phone, true);
        Validate.SetPattern(phone, "09[0-9]{8}");
        password = new TextBox { PasswordChar = '•' };
        Validate.SetRequired(password, true);
        code = new OtpBox { Length = 6 };
        Validate.SetRequired(code, true);
        return new Wizard
        {
            Motion = WizardMotion.Slide,
            StepsLook = WizardStepsLook.Dots,
            Items =
            {
                new WizardPanel { Header = "رقم الهاتف", Content = phone },
                new WizardPanel { Header = "كلمة المرور", Content = password },
                new WizardPanel { Header = "رمز التحقق", Content = code },
            },
        };
    }

    private static string? ErrorOf(Control c) => DataValidationErrors.GetErrors(c)?.Cast<object>().FirstOrDefault()?.ToString();

    private static void Press(Window w, Key key)
    {
        w.KeyPress(key, RawInputModifiers.None, PhysicalKey.None, null);
        w.KeyRelease(key, RawInputModifiers.None, PhysicalKey.None, null);
        Settle(200);
    }

    [AvaloniaFact]
    public void Next_runs_the_validate_rules_of_the_step()
    {
        var wiz = SignIn(out var phone, out var password, out _);
        var w = Show(wiz, FlowDirection.RightToLeft);
        wiz.Next();
        Settle();
        Assert.Equal(0, wiz.CurrentIndex);
        Assert.Equal("املأ هذا الحقل.", ErrorOf(phone));
        phone.Text = "0912";
        Settle(100);
        Assert.Equal("اكتب القيمة بالصيغة المطلوبة.", ErrorOf(phone));
        phone.Focus();
        Press(w, Key.Enter);
        Settle();
        Assert.Equal(0, wiz.CurrentIndex);
        phone.Text = "0912345678";
        Settle(100);
        Assert.False(DataValidationErrors.GetHasErrors(phone));
        Press(w, Key.Enter);
        Settle();
        Assert.Equal(1, wiz.CurrentIndex);
        Assert.False(DataValidationErrors.GetHasErrors(password));
        w.Close();
    }

    [AvaloniaFact]
    public void A_refused_number_holds_the_step_until_it_changes()
    {
        var wiz = SignIn(out var phone, out _, out _);
        var asked = 0;
        wiz.Leaving += (_, e) => { asked++; e.Wait(); };
        var w = Show(wiz, FlowDirection.RightToLeft);
        phone.Text = "0900000000";
        wiz.Next();
        Settle(100);
        Assert.True(wiz.IsBusy);
        DataValidationErrors.SetErrors(phone, ["هذا الرقم غير مسجل."]);
        wiz.Stay();
        Settle(100);
        Assert.False(wiz.IsBusy);
        wiz.Next();
        Settle(100);
        Assert.Equal(1, asked);
        Assert.Equal(0, wiz.CurrentIndex);
        Assert.Equal("هذا الرقم غير مسجل.", ErrorOf(phone));
        w.Close();
    }

    [AvaloniaFact]
    public void Finish_can_wait_for_the_server()
    {
        var wiz = SignIn(out var phone, out var password, out var code);
        var finished = 0;
        wiz.Finished += (_, e) => { finished++; e.Wait(); };
        var w = Show(wiz);
        phone.Text = "0912345678";
        password.Text = "secret";
        code.Text = "111111";
        wiz.GoTo(2);
        Settle();
        wiz.Finish();
        Settle(100);
        Assert.Equal(1, finished);
        Assert.True(wiz.IsBusy);
        Assert.True(Part<Control>(wiz, "PART_FinishSpinner").IsVisible);
        Assert.False(Part<Button>(wiz, "PART_Finish").IsHitTestVisible);
        wiz.Finish();
        code.Focus();
        Press(w, Key.Enter);
        Assert.Equal(1, finished);
        wiz.Stay();
        Settle(100);
        Assert.False(wiz.IsBusy);
        wiz.Finish();
        Settle(100);
        Assert.Equal(2, finished);
        wiz.Restart();
        Settle();
        Assert.False(wiz.IsBusy);
        Assert.Equal(0, wiz.CurrentIndex);
        w.Close();
    }

    [AvaloniaFact]
    public void Finish_goes_back_to_a_step_whose_rules_fail()
    {
        var wiz = SignIn(out var phone, out var password, out _);
        var finished = 0;
        wiz.Finished += (_, _) => finished++;
        var w = Show(wiz);
        password.Text = "secret";
        wiz.GoTo(2);
        Settle();
        wiz.Finish();
        Settle();
        Assert.Equal(0, finished);
        Assert.Equal(0, wiz.CurrentIndex);
        Assert.Equal("املأ هذا الحقل.", ErrorOf(phone));
        w.Close();
    }

    [AvaloniaFact]
    public void Dots_look_draws_the_steps_as_dots()
    {
        var wiz = SignIn(out _, out _, out _);
        var w = Show(wiz);
        var steps = Part<Steps>(wiz, "PART_Steps");
        Assert.Contains("dots", steps.Classes);
        var first = Assert.IsType<StepsItem>(steps.Items[0]);
        Assert.Equal(8, Part<Panel>(first, "PART_Dot").Bounds.Width);
        Assert.False(Part<TextBlock>(first, "PART_Number").IsVisible);
        wiz.StepsLook = WizardStepsLook.Numbers;
        Settle();
        Assert.DoesNotContain("dots", steps.Classes);
        Assert.Equal(32, Part<Panel>(first, "PART_Dot").Bounds.Width);
        Assert.True(Part<TextBlock>(first, "PART_Number").IsVisible);
        w.Close();
    }

    [AvaloniaFact]
    public void Validate_reset_forgets_the_checks()
    {
        var wiz = SignIn(out var phone, out _, out _);
        var w = Show(wiz);
        wiz.Next();
        Settle();
        Assert.True(DataValidationErrors.GetHasErrors(phone));
        Validate.Reset(wiz);
        Settle(100);
        Assert.False(DataValidationErrors.GetHasErrors(phone));
        phone.Text = "09";
        Settle(100);
        Assert.False(DataValidationErrors.GetHasErrors(phone));
        w.Close();
    }

    [AvaloniaFact]
    public void Foot_follows_the_footer_standard()
    {
        var wiz = SignIn(out _, out _, out _);
        wiz.CancelContent = "البدء من جديد";
        wiz.FinishText = "دخول";
        var w = Show(wiz, FlowDirection.RightToLeft);
        Assert.Equal(12, Part<Grid>(wiz, "PART_Foot").ColumnSpacing);
        foreach (var name in new[] { "PART_Cancel", "PART_Previous", "PART_Next", "PART_Finish" })
            Assert.Equal(80, Part<Button>(wiz, name).MinWidth);
        wiz.GoTo(2);
        Settle();
        Assert.True(Part<Button>(wiz, "PART_Finish").Bounds.Width >= 80);
        w.Close();
    }

    private static TextBlock? MessageUnder(OtpBox otp, string text) =>
        otp.GetVisualDescendants().OfType<TextBlock>().FirstOrDefault(t => t.Text == text && t.IsEffectivelyVisible);

    [AvaloniaFact]
    public void Otp_error_shows_under_the_block_and_turns_it_red()
    {
        var otp = new OtpBox();
        var w = Show(new StackPanel { Width = 420, Children = { otp } }, FlowDirection.RightToLeft);
        var block = Part<Border>(otp, "PART_Cells");
        var height = block.Bounds.Height;
        DataValidationErrors.SetErrors(otp, ["الرمز غير صحيح."]);
        Settle();
        Assert.Contains(":invalid", otp.Classes);
        AssertBrush(otp, "InsTextBoxBadBorder", () => block.BorderBrush);
        var message = MessageUnder(otp, "الرمز غير صحيح.");
        Assert.NotNull(message);
        Assert.True(message!.TranslatePoint(default, w)!.Value.Y >= block.TranslatePoint(new Point(0, block.Bounds.Height), w)!.Value.Y);
        Assert.Equal(height, block.Bounds.Height);
        Assert.Equal(52, block.Bounds.Height);
        otp.Focus(NavigationMethod.Tab);
        Settle();
        Assert.DoesNotContain(AdornerLayer.GetAdornerLayer(otp)!.Children, c => c is FocusRing);
        DataValidationErrors.ClearErrors(otp);
        Settle();
        Assert.DoesNotContain(":invalid", otp.Classes);
        Assert.Null(MessageUnder(otp, "الرمز غير صحيح."));
        w.Close();
    }

    [AvaloniaFact]
    public void Validate_reads_the_otp_incomplete_code()
    {
        var otp = new OtpBox { Length = 6 };
        Validate.SetRequired(otp, true);
        var form = new StackPanel { Width = 420, Children = { otp } };
        var w = Show(form, FlowDirection.RightToLeft);
        Assert.False(Validate.Check(form));
        Assert.Equal("املأ هذا الحقل.", ErrorOf(otp));
        otp.Text = "12";
        Settle(100);
        Assert.Equal("اكتب الرمز كاملًا: 6 أرقام.", ErrorOf(otp));
        Assert.NotNull(MessageUnder(otp, "اكتب الرمز كاملًا: 6 أرقام."));
        Assert.False(Validate.Check(form));
        otp.Text = "123456";
        Settle(100);
        Assert.False(DataValidationErrors.GetHasErrors(otp));
        Assert.True(Validate.Check(form));
        w.Close();
    }

    [AvaloniaFact]
    public void Wizard_required_reads_the_otp_incomplete_code()
    {
        var otp = new OtpBox { Length = 6 };
        Wizard.SetRequired(otp, true);
        var wiz = new Wizard
        {
            Items =
            {
                new WizardPanel { Header = "رمز التحقق", Content = otp },
                new WizardPanel { Header = "تم", Content = new TextBlock { Text = "تم" } },
            },
        };
        var w = Show(wiz, FlowDirection.RightToLeft);
        wiz.Next();
        Settle();
        Assert.Equal(0, wiz.CurrentIndex);
        Assert.Equal("هذا الحقل مطلوب.", ErrorOf(otp));
        otp.Text = "12";
        Settle(100);
        Assert.Equal("اكتب الرمز كاملًا: 6 أرقام.", ErrorOf(otp));
        wiz.Next();
        Settle();
        Assert.Equal(0, wiz.CurrentIndex);
        otp.Text = "123456";
        Settle(100);
        Assert.False(DataValidationErrors.GetHasErrors(otp));
        wiz.Next();
        Settle();
        Assert.Equal(1, wiz.CurrentIndex);
        w.Close();
    }

    [AvaloniaFact]
    public void Finish_stops_on_an_empty_or_part_typed_code()
    {
        var wiz = SignIn(out var phone, out var password, out var code);
        var finished = 0;
        wiz.Finished += (_, _) => finished++;
        var w = Show(wiz, FlowDirection.RightToLeft);
        phone.Text = "0912345678";
        password.Text = "secret";
        wiz.GoTo(2);
        Settle();
        wiz.Finish();
        Settle();
        Assert.Equal(0, finished);
        Assert.Equal(2, wiz.CurrentIndex);
        Assert.Equal("املأ هذا الحقل.", ErrorOf(code));
        Assert.Contains(":invalid", code.Classes);
        code.Text = "12";
        Settle(100);
        Assert.Equal("اكتب الرمز كاملًا: 6 أرقام.", ErrorOf(code));
        wiz.Finish();
        Settle();
        Assert.Equal(0, finished);
        code.Text = "123456";
        Settle(100);
        Assert.False(DataValidationErrors.GetHasErrors(code));
        wiz.Finish();
        Settle();
        Assert.Equal(1, finished);
        w.Close();
    }

    private static Wizard OwnButtons(out TextBox phone, out Button next, out Button restart, out Button finish)
    {
        phone = new TextBox { FlowDirection = FlowDirection.LeftToRight };
        Validate.SetRequired(phone, true);
        next = new Button { Content = "متابعة" };
        Wizard.SetAction(next, WizardAction.Next);
        restart = new Button { Content = "البدء من جديد" };
        Wizard.SetAction(restart, WizardAction.Restart);
        finish = new Button { Content = "دخول" };
        Wizard.SetAction(finish, WizardAction.Finish);
        return new Wizard
        {
            ShowFoot = false,
            Items =
            {
                new WizardPanel { Header = "رقم الهاتف", Content = new StackPanel { Spacing = 16, Children = { phone, next } } },
                new WizardPanel { Header = "رمز التحقق", Content = new StackPanel { Spacing = 16, Children = { finish, restart } } },
            },
        };
    }

    [AvaloniaFact]
    public void Body_clips_only_while_a_step_moves()
    {
        var wiz = SignIn(out var phone, out _, out _);
        var w = Show(wiz, FlowDirection.RightToLeft);
        var body = Part<Border>(wiz, "PART_Body");
        Assert.False(body.ClipToBounds);
        phone.Text = "0912345678";
        wiz.Next();
        Settle(60);
        Assert.True(body.ClipToBounds);
        Settle(1500);
        Assert.Equal(1, wiz.CurrentIndex);
        Assert.False(body.ClipToBounds);
        w.Close();
    }

    [AvaloniaFact]
    public void ShowFoot_off_hides_the_foot()
    {
        var wiz = SignIn(out _, out _, out _);
        var w = Show(wiz);
        var foot = Part<Grid>(wiz, "PART_Foot");
        Assert.True(foot.IsVisible);
        wiz.ShowFoot = false;
        Settle(100);
        Assert.False(foot.IsVisible);
        w.Close();
    }

    [AvaloniaFact]
    public void Step_buttons_go_next_restart_and_finish()
    {
        var wiz = OwnButtons(out var phone, out var next, out var restart, out var finish);
        var finished = 0;
        wiz.Finished += (_, _) => finished++;
        var w = Show(wiz, FlowDirection.RightToLeft);
        Click(next, w);
        Assert.Equal(0, wiz.CurrentIndex);
        Assert.Equal("املأ هذا الحقل.", ErrorOf(phone));
        phone.Text = "0912345678";
        Settle(100);
        Click(next, w);
        Settle();
        Assert.Equal(1, wiz.CurrentIndex);
        Click(restart, w);
        Settle();
        Assert.Equal(0, wiz.CurrentIndex);
        Click(next, w);
        Settle();
        Click(finish, w);
        Assert.Equal(1, finished);
        w.Close();
    }

    [AvaloniaFact]
    public void Step_buttons_wait_with_a_spinner()
    {
        var wiz = OwnButtons(out var phone, out var next, out _, out var finish);
        var asked = 0;
        wiz.Leaving += (_, e) => { asked++; e.Wait(); };
        wiz.Finished += (_, e) => e.Wait();
        var w = Show(wiz, FlowDirection.RightToLeft);
        phone.Text = "0912345678";
        Click(next, w);
        Assert.True(wiz.IsBusy);
        Assert.Equal(0.75, next.Opacity);
        Assert.False(next.IsHitTestVisible);
        Assert.Contains(next.GetVisualDescendants().OfType<LoadingSpinner>(), s => s.IsEffectivelyVisible);
        Assert.Contains(next.GetVisualDescendants().OfType<TextBlock>(), t => t.Text == "متابعة");
        next.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        Settle(100);
        Assert.Equal(1, asked);
        wiz.Proceed();
        Settle();
        Assert.False(wiz.IsBusy);
        Assert.Equal(1, next.Opacity);
        Assert.Empty(next.GetVisualDescendants().OfType<LoadingSpinner>());
        Click(finish, w);
        Assert.True(wiz.IsBusy);
        Assert.Contains(finish.GetVisualDescendants().OfType<LoadingSpinner>(), s => s.IsEffectivelyVisible);
        wiz.Stay();
        Settle(100);
        Assert.Empty(finish.GetVisualDescendants().OfType<LoadingSpinner>());
        w.Close();
    }
}
