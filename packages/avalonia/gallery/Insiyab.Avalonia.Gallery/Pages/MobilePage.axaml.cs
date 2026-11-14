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
        NavSample.SelectionChanged += (_, _) => NavNote.Text = "أنت في " + Chosen() + ".";
        NavSample.Reselected += (_, _) => NavNote.Text = "ضغطت «" + Chosen() + "» مرة أخرى، فيعيد التطبيق الصفحة هنا إلى أعلاها.";
        RefreshList.ItemsSource = _files;
        PagesSample.Root = PlacesPage();
        PhoneMonth.Events = PhoneEvents();
        PhoneMonth.DayInvoked += (_, day) => PhoneMonthNote.Text = $"فتحت يوم {day.Day}، فيعرض التطبيق مواعيده كلها.";
        PhoneMonth.MonthChanged += (_, month) => PhoneMonthNote.Text = $"انتقلت إلى الشهر {month.Month} من {month.Year}.";
        PlaceChips.ItemsSource = PlaceKinds;
        PlaceChips.SelectedIndex = 0;
        PlaceChips.SelectionChanged += (_, _) => PlaceChipsNote.Text = PlaceChips.SelectedIndex == 0 ? "تعرض القائمة الأماكن كلها." : $"تعرض القائمة {PlaceChips.SelectedItem} وحدها.";
        PlaceChipsNote.Text = "تعرض القائمة الأماكن كلها.";
    }

    private static readonly string[] PlaceKinds = ["الكل", "المكاتب", "المستودعات", "المتاجر", "المطاعم", "الحدائق", "المتاحف"];

    // This month's events around today, so the phone calendar opens on them.
    private static List<MonthEvent> PhoneEvents()
    {
        var first = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
        MonthEvent At(int day, int hour, string title, string tag, MonthEventTone tone = MonthEventTone.Brand, int minutes = 60) =>
            new() { Date = first.AddDays(day - 1), Time = hour < 0 ? null : TimeSpan.FromHours(hour), Duration = TimeSpan.FromMinutes(minutes), Title = title, Tag = tag, Tone = tone };
        return
        [
            At(3, 9, "اجتماع الفريق", "في قاعة الاجتماعات، ساعة"),
            At(3, 13, "توصيل الطلبية", "إلى المستودع الشمالي", MonthEventTone.Info),
            At(3, 16, "مكالمة مع العميل", "مكالمة فيديو، نصف ساعة", minutes: 30),
            At(8, -1, "رحلة إلى جدة", "طوال اليوم، العودة مساء", MonthEventTone.Ok),
            At(12, 10, "زيارة المورد", "في مقر المورد، ساعة ونصف", minutes: 90),
            At(12, 15, "مراجعة الأسعار", "مع فريق المبيعات"),
            At(17, 11, "تسليم المنتج الجديد", "آخر موعد للتسليم", MonthEventTone.Bad),
            At(21, 9, "ورشة عمل", "للفريق الجديد، ساعتان", minutes: 120),
            At(21, 12, "غداء مع الشركاء", "في المطعم المجاور"),
            At(21, 14, "جرد المخزون", "في المستودع", MonthEventTone.Info),
            At(21, 17, "اجتماع المساء", "عن خطة الشهر القادم"),
            At(26, 8, "رحلة مبكرة", "من المطار، ساعتان", minutes: 120),
        ];
    }

    private CommandPalette? _search;

    private void OnFullSearch(object? sender, RoutedEventArgs e)
    {
        _search ??= new CommandPalette
        {
            Classes = { "full" },
            Items =
            {
                new PaletteItem { Label = "مصباح مكتب", Group = "المنتجات", Description = "120 ريالا" },
                new PaletteItem { Label = "كرسي مريح", Group = "المنتجات", Description = "450 ريالا" },
                new PaletteItem { Label = "طاولة قابلة للطي", Group = "المنتجات", Description = "300 ريال" },
                new PaletteItem { Label = "المتجر في الرياض", Group = "الأماكن" },
                new PaletteItem { Label = "المتجر في جدة", Group = "الأماكن" },
                new PaletteItem { Label = "طلباتي", Group = "الصفحات" },
                new PaletteItem { Label = "سلة الشراء", Group = "الصفحات" },
            },
        };
        _search.Open(FullSearch);
    }

    private readonly System.Collections.ObjectModel.ObservableCollection<string> _files =
        ["التقرير السنوي.pdf", "صور الرحلة.zip", "قائمة الأسعار.xlsx", "عقد الإيجار.pdf", "خطة المشروع.docx", "شعار المتجر.png", "فاتورة الكهرباء.pdf", "جدول المواعيد.xlsx", "ملاحظات الاجتماع.txt", "عرض المنتج.pptx"];

    private int _fresh;

    private string Chosen() => (NavSample.SelectedItem as BottomNavItem)?.Label ?? "";

    private static Dialog NewSheet(string title, Control content, object? foot = null) =>
        new() { Title = title, Content = content, Foot = foot, Classes = { "drawer", "bottom", "sheet" } };

    private void OnShortSheet(object? sender, RoutedEventArgs e)
    {
        var body = new StackPanel
        {
            Spacing = 8,
            Children =
            {
                new TextBlock { Classes = { "h4" }, Text = "مصباح مكتب قابل للطي" },
                new TextBlock { Classes = { "muted" }, Text = "ضوء دافئ بثلاث درجات، وقاعدة تشحن الهاتف." },
                new TextBlock { Text = "السعر: 120 ريالا", Margin = new Thickness(0, 8, 0, 0) },
            },
        };
        NewSheet("تفاصيل المنتج", body).Open(ShortSheet);
    }

    private void OnLongSheet(object? sender, RoutedEventArgs e)
    {
        var places = new[] { "الرياض", "جدة", "مكة المكرمة", "المدينة المنورة", "الدمام", "الخبر", "الطائف", "تبوك", "أبها", "حائل", "بريدة", "نجران", "جازان", "ينبع", "الجبيل", "القطيف", "الأحساء", "عرعر", "سكاكا", "الباحة", "القنفذة", "رابغ", "الخرج", "عنيزة" };
        var list = new ListBox { Classes = { "static" }, ItemsSource = places, Background = Brushes.Transparent };
        var sheet = NewSheet("اختر المدينة", list);
        ListRow.AddInvokedHandler(list, (_, args) => sheet.Close(args.Item));
        sheet.Padding = new Thickness(8, 0, 8, 8);
        sheet.Open(LongSheet);
    }

    private void OnFormSheet(object? sender, RoutedEventArgs e)
    {
        var cancel = new Button { Classes = { "bare" }, Content = "إلغاء" };
        var save = new Button { Classes = { "primary" }, Content = "حفظ" };
        Dialog.SetResult(cancel, "cancel");
        Dialog.SetResult(save, "save");
        var body = new StackPanel
        {
            Spacing = 16,
            Children =
            {
                new StackPanel { Classes = { "field" }, Children = { new TextBlock { Classes = { "label" }, Text = "الاسم" }, new TextBox { PlaceholderText = "اسم جهة الاتصال" } } },
                new StackPanel { Classes = { "field" }, Children = { new TextBlock { Classes = { "label" }, Text = "ملاحظة" }, new TextBox { Classes = { "grow" }, PlaceholderText = "ما تريد تذكره" } } },
            },
        };
        NewSheet("جهة اتصال جديدة", body, new DialogFoot { Children = { cancel, save } }).Open(FormSheet);
    }

    private async void OnRefresh(object? sender, RefreshRequestedEventArgs e)
    {
        var done = e.GetDeferral();
        RefreshNote.Text = "يجري التحديث…";
        await System.Threading.Tasks.Task.Delay(1500);
        _fresh++;
        _files.Insert(0, "ملف جديد " + _fresh.ToString(System.Globalization.CultureInfo.InvariantCulture) + ".pdf");
        RefreshNote.Text = "وصل ملف جديد في أعلى القائمة.";
        done.Complete();
    }

    private Control PlacesPage()
    {
        var places = new[] { "حديقة الملك عبدالله", "المتحف الوطني", "سوق الزل", "واجهة البحر", "قلعة المصمك", "مكتبة الحي" };
        var list = new ListBox { ItemsSource = places, Background = Brushes.Transparent };
        ListRow.AddInvokedHandler(list, (_, args) => PagesSample.Push(PlacePage((string)args.Item!, 1)));
        var head = new TextBlock { Classes = { "h4" }, Text = "أماكن قريبة", Margin = new Thickness(16, 16, 16, 8) };
        DockPanel.SetDock(head, Dock.Top);
        return new DockPanel { Children = { head, list } };
    }

    private Control PlacePage(string place, int depth)
    {
        var back = new Button { Classes = { "ghost", "sm" }, Content = "رجوع" };
        back.Click += (_, _) => PagesSample.Pop();
        var more = new Button { Classes = { "secondary", "sm" }, Content = "صور المكان", HorizontalAlignment = HorizontalAlignment.Left };
        more.Click += (_, _) => PagesSample.Push(PlacePage(place + "، الصور", depth + 1));
        var head = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8, Margin = new Thickness(12), Children = { back, new TextBlock { Classes = { "h4" }, Text = place, VerticalAlignment = VerticalAlignment.Center } } };
        DockPanel.SetDock(head, Dock.Top);
        var body = new StackPanel
        {
            Spacing = 12,
            Margin = new Thickness(16, 4, 16, 16),
            Children =
            {
                new TextBlock { Text = "مفتوح كل يوم من التاسعة صباحا إلى العاشرة مساء.", TextWrapping = TextWrapping.Wrap },
                new TextBlock { Classes = { "muted" }, Text = "الصفحة " + depth.ToString(System.Globalization.CultureInfo.InvariantCulture) + " فوق القائمة. ارجع بالزر، أو بزر الرجوع، أو اسحب من حافة البداية.", TextWrapping = TextWrapping.Wrap },
            },
        };
        if (depth < 3) body.Children.Add(more);
        return new DockPanel { Children = { head, body } };
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
