using Avalonia.Controls;
using Insiyab.Avalonia.Controls;

namespace Insiyab.Avalonia.Gallery.Pages;

[GalleryPage("login", "معالج تسجيل الدخول")]
public partial class LoginPage : UserControl
{
    private const string NotRegistered = "هذا الرقم غير مسجل.";
    private const string WrongCode = "الرمز غير صحيح.";

    public LoginPage()
    {
        InitializeComponent();
        Phone.TextChanged += (_, _) =>
        {
            Drop(Phone, NotRegistered);
            PhoneEcho.Text = Phone.Text;
            CodeTo.Text = Phone.Text;
        };
        Code.TextChanged += (_, _) => Drop(Code, WrongCode);
        Login.Leaving += OnLeaving;
        Login.Finished += OnFinished;
        Loaded += (_, _) => Phone.Focus();
    }

    // A stand-in for the server: each step waits a moment, one number is refused, and only 123456 signs in.
    private async void OnLeaving(object? sender, WizardLeavingEventArgs e)
    {
        e.Wait();
        await Task.Delay(700);
        if (!Login.IsBusy) return;
        if (e.Step == 0 && Phone.Text == "0900000000")
        {
            DataValidationErrors.SetErrors(Phone, [NotRegistered]);
            Phone.Focus();
            Login.Stay();
            return;
        }
        Login.Proceed();
    }

    private async void OnFinished(object? sender, WizardFinishedEventArgs e)
    {
        e.Wait();
        await Task.Delay(700);
        if (!Login.IsBusy) return;
        if (Code.Text != "123456")
        {
            DataValidationErrors.SetErrors(Code, [WrongCode]);
            Code.Focus();
            Login.Stay();
            return;
        }
        Toast.Show(this, "تم الدخول", ToastTone.Ok);
        Phone.Text = string.Empty;
        Password.Text = string.Empty;
        Password.RevealPassword = false;
        Code.SetCode(string.Empty);
        Validate.Reset(Login);
        Login.Restart();
    }

    private static void Drop(Control box, string message)
    {
        var errors = DataValidationErrors.GetErrors(box)?.Cast<object>().ToList();
        if (errors is null || !errors.Remove(message)) return;
        if (errors.Count == 0) DataValidationErrors.ClearErrors(box);
        else DataValidationErrors.SetErrors(box, errors);
    }
}
