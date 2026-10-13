using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Insiyab.Avalonia.Controls;

namespace Insiyab.Avalonia.Gallery.Pages;

[GalleryPage("login-simple", "تسجيل الدخول")]
public partial class LoginSimplePage : UserControl
{
    private const string Unknown = "هذا الرقم غير مسجل.";
    private bool _busy;

    public LoginSimplePage()
    {
        InitializeComponent();
        Loaded += (_, _) => Account.Focus();
        Account.TextChanged += (_, _) => Drop(Account, Unknown);
        Validate.AddSubmittedHandler(Form, OnSubmitted);
    }

    // A stand-in for the server: it waits a moment, and one number is refused.
    private void OnSubmitted(object? sender, RoutedEventArgs e)
    {
        if (_busy) return;
        _busy = true;
        Busy.IsVisible = true;
        Password.RevealPassword = false;
        DispatcherTimer.RunOnce(() =>
        {
            _busy = false;
            Busy.IsVisible = false;
            if (Account.Text == "0900000000")
            {
                DataValidationErrors.SetErrors(Account, [Unknown]);
                Account.Focus();
                return;
            }
            Toast.Show(this, "تم الدخول", ToastTone.Ok);
        }, TimeSpan.FromMilliseconds(700));
    }

    private static void Drop(TextBox box, string message)
    {
        var errors = DataValidationErrors.GetErrors(box)?.Cast<object>().ToList();
        if (errors is null || !errors.Remove(message)) return;
        if (errors.Count == 0) DataValidationErrors.ClearErrors(box);
        else DataValidationErrors.SetErrors(box, errors);
    }
}
