using Avalonia;
using Avalonia.Controls;

namespace Insiyab.Avalonia.Gallery.Pages;

[GalleryPage("window", "النافذة والزجاج")]
public partial class WindowPage : UserControl
{
    private Window? _window;

    public WindowPage()
    {
        InitializeComponent();
        MaterialSwitch.IsCheckedChanged += (_, _) =>
        {
            if (_window is not null)
                Frost.SetMaterial(_window, MaterialSwitch.IsChecked == true);
        };
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        _window = TopLevel.GetTopLevel(this) as Window;
        if (_window is null)
            return;
        _window.PropertyChanged += OnWindowPropertyChanged;
        MaterialSwitch.IsChecked = Frost.GetMaterial(_window);
        Show();
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        if (_window is not null)
            _window.PropertyChanged -= OnWindowPropertyChanged;
        _window = null;
    }

    private void OnWindowPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property == TopLevel.ActualTransparencyLevelProperty || e.Property == Frost.MaterialProperty)
            Show();
    }

    private void Show()
    {
        if (_window is null)
            return;
        Level.Text = _window.ActualTransparencyLevel.ToString();
        var on = Frost.IsMaterialOn(_window);
        State.Text = on ? "تعمل" : "متوقفة";
        Explain.Text = on
            ? "ترى سطح المكتب خلف النافذة مموّها، وفوقه طبقة من لون الخلفية تكفي ليبقى النص واضحا مهما كانت الصورة خلفها."
            : Frost.GetMaterial(_window)
                ? "لم يمنح النظام النافذة خامة شفافة الآن، فتظهر الخلفية كاملة. يحدث هذا مع توفير البطارية، أو عبر سطح المكتب البعيد، أو حين تُطفأ الشفافية في إعدادات النظام، وتعود الخامة وحدها حين يسمح النظام بها."
                : "طلبت هذه النافذة أن تبقى معتمة، فتظهر الخلفية كاملة ولا يظهر شيء خلفها.";
    }
}
