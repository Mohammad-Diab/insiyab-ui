using Avalonia.Controls;
using Insiyab.Avalonia.Controls;

namespace Insiyab.Avalonia.Gallery.Pages;

[GalleryPage("otp", "رمز التحقق")]
public partial class OtpPage : UserControl
{
    public OtpPage()
    {
        InitializeComponent();

        // The right code in this example is 123456; any other is turned down.
        Demo.Completed += (_, e) =>
        {
            if (e.Value == "123456") DemoHint.Text = "الرمز صحيح.";
            else
            {
                Demo.IsInvalid = true;
                DemoHint.Text = "الرمز غير صحيح.";
            }
        };

        Play.SetCode("482");
        Play4.SetCode("482");
        Play8.SetCode("482");
        PlayAlnum.SetCode("A7K");
        PlayDisabled.SetCode("482");
        PlaySubmit.SetCode("482");
        PlayInvalid.SetCode("482");
        PlayInvalid.IsInvalid = true;

        PlaySubmit.SubmitCommand = new SubmitCode(code =>
        {
            SubmitNote.Text = "أُرسل " + code;
            SubmitNote.IsVisible = true;
            PlaySubmit.SetCode("");
        });
    }

    private sealed class SubmitCode(Action<string> run) : System.Windows.Input.ICommand
    {
        public event EventHandler? CanExecuteChanged { add { } remove { } }
        public bool CanExecute(object? parameter) => true;
        public void Execute(object? parameter) => run(parameter as string ?? "");
    }
}
