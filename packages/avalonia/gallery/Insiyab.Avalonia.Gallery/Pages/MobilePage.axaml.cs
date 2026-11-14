using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Platform;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Insiyab.Avalonia.Controls;

namespace Insiyab.Avalonia.Gallery.Pages;

[GalleryPage("mobile", "على الهاتف")]
public partial class MobilePage : UserControl
{
    private IInsetsManager? _insets;

    public MobilePage()
    {
        InitializeComponent();
        EffectsSeg.SelectedIndex = InsiyabTheme.Effects == EffectsLevel.Light ? 1 : 0;
        EffectsSeg.SelectionChanged += (_, _) => InsiyabTheme.Effects = EffectsSeg.SelectedIndex == 1 ? EffectsLevel.Light : EffectsLevel.Full;
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        _insets = TopLevel.GetTopLevel(this) is Window ? null : TopLevel.GetTopLevel(this)?.InsetsManager;
        if (_insets is not null) _insets.SafeAreaChanged += OnSafeArea;
        ShowSafeArea();
        MotionNow.Text = Motion.SystemReducesMotion ? "النظام يطلب حركة أقل الآن، فالمكتبة ساكنة." : "النظام لا يطلب حركة أقل الآن.";
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        if (_insets is not null) _insets.SafeAreaChanged -= OnSafeArea;
        _insets = null;
    }

    private void OnSafeArea(object? sender, SafeAreaChangedArgs e) => ShowSafeArea();

    private void ShowSafeArea()
    {
        var safe = SafeArea.Of(this);
        SafeNow.Text = safe == default
            ? "لا شيء يغطي حواف هذه الشاشة الآن، فالمسافات كلها صفر."
            : FormattableString.Invariant($"الحواف الآمنة الآن: أعلى {safe.Top:0}، أسفل {safe.Bottom:0}، يسار {safe.Left:0}، يمين {safe.Right:0}");
    }

    private void OnTouchAction(object? sender, RoutedEventArgs e)
    {
        if (sender is Control c) TouchNote.Text = "ضغطت: " + global::Avalonia.Automation.AutomationProperties.GetName(c);
    }

    private void OnOpenLayers(object? sender, RoutedEventArgs e)
    {
        var menu = new PopMenu
        {
            Items =
            {
                new MenuItem { Header = "نسخ" },
                new MenuItem { Header = "إعادة التسمية" },
                new MenuItem { Header = "حذف", Classes = { "danger" } },
            },
        };
        var more = new Button { Classes = { "secondary", "sm" }, Content = "افتح قائمة", Flyout = menu };
        var back = new Button { Classes = { "bare", "sm" }, Content = "رجوع" };
        back.Click += (_, _) => Back();
        var dialog = new Dialog
        {
            Title = "ملف المشروع",
            Content = new StackPanel
            {
                Spacing = 16,
                Children =
                {
                    new TextBlock { Text = "افتح القائمة ثم اضغط زر الرجوع: تُغلق القائمة أولا، ثم الحوار.", TextWrapping = TextWrapping.Wrap },
                    new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8, Children = { more, back } },
                },
            },
        };
        dialog.Open(OpenLayers);
    }

    private void OnBack(object? sender, RoutedEventArgs e) => Back();

    // The same Back the system sends, so it can be tried with a mouse too.
    private void Back()
    {
        if (TopLevel.GetTopLevel(this) is not { } top) return;
        var args = new RoutedEventArgs(TopLevel.BackRequestedEvent);
        top.RaiseEvent(args);
        BackNote.Text = args.Handled ? "أغلق الرجوع آخر طبقة فُتحت." : "لم يكن شيء مفتوحا، فوصل الرجوع إلى التطبيق.";
    }

    private void OnPlainDialog(object? sender, RoutedEventArgs e)
    {
        var dialog = new Dialog { Title = "حوار", Content = new TextBlock { Text = "انظر إلى الصفحة خلف الحوار: مموّهة مع المؤثرات الكاملة، وواضحة مع الخفيفة.", TextWrapping = TextWrapping.Wrap } };
        if (sender is Control from) dialog.Open(from);
    }
}
