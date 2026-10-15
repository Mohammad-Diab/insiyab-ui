using System.Diagnostics;
using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Rendering;
using Avalonia.Threading;
using Insiyab.Avalonia;
using Insiyab.Avalonia.Brand;
using Insiyab.Avalonia.Controls;
using PerfLab.Pages;

namespace PerfLab;

/// <summary>A link in the sidebar and the page it opens.</summary>
public sealed record Link(string Group, string Title, string Icon, string Keywords, Func<Control> Create, string Kind);

public partial class MainWindow : Window
{
    private static readonly (string Hex, string Name)[] Brands =
    [
        ("#4C1D95", "بنفسجي"), ("#0F766E", "أخضر مزرق"), ("#1D4ED8", "أزرق"), ("#9B2C5E", "عنابي"), ("#B45309", "كهرماني"),
    ];

    public static readonly Link[] Links =
    [
        new("العمل", "الرئيسية", "IconHome", "dashboard home", () => new DashboardPage(), "dashboard"),
        new("العمل", "الطلبات", "IconCart", "orders table", () => new TablePage("الطلبات", 1200), "table"),
        new("العمل", "التذاكر", "IconInbox", "tickets", () => new TablePage("التذاكر", 1000), "table"),
        new("العمل", "المستندات", "IconFile", "documents tabs", () => new DocumentsPage(), "documents"),
        new("العمل", "التقويم", "IconCalendar", "calendar month week day", () => new CalendarPage(), "calendar"),
        new("العمل", "المهام", "IconList", "tasks lists tree", () => new ListsPage(), "lists"),
        new("العمل", "النوافذ المنبثقة", "IconWindow", "dialogs drawers toasts menus", () => new OverlaysPage(), "overlays"),
        new("النماذج", "طلب جديد", "IconEdit", "new order form", () => new FormPage(), "form"),
        new("النماذج", "بيانات العميل", "IconUser", "customer form", () => new FormPage(), "form"),
        new("النماذج", "الشحن", "IconBox", "shipping form", () => new FormPage(), "form"),
        new("النماذج", "الدفع", "IconWallet", "payment form", () => new FormPage(), "form"),
        new("التقارير", "المبيعات", "IconChart", "sales report", () => new DashboardPage(), "dashboard"),
        new("التقارير", "الأداء الشهري", "IconGauge", "monthly", () => new DashboardPage(), "dashboard"),
        new("التقارير", "المخزون", "IconLayers", "stock", () => new TablePage("المخزون", 1200), "table"),
        new("التقارير", "الفواتير", "IconPages", "invoices", () => new TablePage("الفواتير", 1200), "table"),
        new("التقارير", "المرتجعات", "IconArrow", "returns", () => new TablePage("المرتجعات", 400), "table"),
        new("الفريق", "المستخدمون", "IconUsers", "users", () => new TablePage("المستخدمون", 300), "table"),
        new("الفريق", "الأدوار", "IconShield", "roles", () => new ListsPage(), "lists"),
        new("الفريق", "الاجتماعات", "IconClock", "meetings", () => new CalendarPage(), "calendar"),
        new("الفريق", "الرسائل", "IconMessage", "messages", () => new ListsPage(), "lists"),
        new("الملفات", "العقود", "IconFolder", "contracts", () => new DocumentsPage(), "documents"),
        new("الملفات", "الملاحظات", "IconEdit", "notes", () => new DocumentsPage(), "documents"),
        new("الملفات", "الأرشيف", "IconBox", "archive", () => new ListsPage(), "lists"),
        new("الملفات", "التنزيلات", "IconDownload", "downloads", () => new ListsPage(), "lists"),
        new("الإعدادات", "الحساب", "IconCog", "account settings", () => new FormPage(), "form"),
        new("الإعدادات", "الإشعارات", "IconBell", "notifications", () => new OverlaysPage(), "overlays"),
        new("الإعدادات", "الأمان", "IconLock", "security", () => new FormPage(), "form"),
        new("الإعدادات", "التكاملات", "IconLink", "integrations", () => new DashboardPage(), "dashboard"),
        new("الإعدادات", "النسخ الاحتياطي", "IconUpload", "backup", () => new ListsPage(), "lists"),
        new("الإعدادات", "المساعدة", "IconHelp", "help", () => new DocumentsPage(), "documents"),
    ];

    /// <summary>The last page switch: from the click to the first frame with the new page, in ms.</summary>
    public double LastSwitchMs { get; private set; }

    public Link? Current { get; private set; }

    /// <summary>Layout passes so far, counted at the window.</summary>
    public long LayoutPasses { get; private set; }

    public ScrollViewer PageScroller => Scroller;

    public Control? PageContent => Host.Content as Control;

    public MainWindow()
    {
        InitializeComponent();
        if (Program.Option("--still") is not null) Motion.SetStill(this, true);
        // Diagnostics for a scripted run: an opaque window, or no box shadows anywhere, to see what a frame costs without them.
        if (Program.Option("--opaque") is not null) Frost.SetMaterial(this, false);
        if (Program.Option("--noshadow") is not null)
            Styles.Add(new Avalonia.Styling.Style(x => Avalonia.Styling.Selectors.Is<Border>(x)) { Setters = { new Avalonia.Styling.Setter(Border.BoxShadowProperty, default(BoxShadows)) } });
        // A scripted run opens without taking focus from whoever is using the machine.
        if (Program.Option("--bench") is not null || Program.Option("--startup") is not null) ShowActivated = false;
        foreach (var (hex, name) in Brands)
        {
            var swatch = new Button { Width = 30, Height = 30, Padding = default, CornerRadius = new CornerRadius(15), Background = new SolidColorBrush(Color.Parse(hex)), Tag = Color.Parse(hex) };
            swatch.Classes.Add("bare");
            ToolTip.SetTip(swatch, name);
            AutomationProperties.SetName(swatch, name);
            swatch.Click += (_, _) => InsiyabTheme.SetPrimary((Color)swatch.Tag!);
            Swatches.Children.Add(swatch);
        }
        PaintSwatches();
        ActualThemeVariantChanged += (_, _) => PaintSwatches();
        DirectionSeg.SelectionChanged += (_, _) => FlowDirection = DirectionSeg.SelectedIndex == 1 ? FlowDirection.LeftToRight : FlowDirection.RightToLeft;
        EffectsButton.Click += (_, _) => Motion.SetStill(this, !Motion.GetStill(this));
        ReadoutButton.Click += (_, _) => Readout.Toggle();
        AddHandler(KeyDownEvent, OnKey, RoutingStrategies.Tunnel);
        LayoutUpdated += (_, _) => LayoutPasses++;

        string? group = null;
        foreach (var link in Links)
        {
            if (link.Group != group)
            {
                group = link.Group;
                Nav.Items.Add(new ListBoxItem { Classes = { "shell-group" }, Content = group });
            }
            Nav.Items.Add(Item(link));
        }
        Nav.SelectionChanged += (_, _) =>
        {
            if (Nav.SelectedItem is ListBoxItem { Tag: Link link }) Open(link);
        };
        Nav.SelectedItem = Nav.Items.OfType<ListBoxItem>().First(i => i.Tag is Link);

        if (Program.Option("--bench") is { } dir) Opened += (_, _) => _ = new Bench(this, dir.Length > 0 ? dir : Path.Combine(AppContext.BaseDirectory, "bench")).RunAsync();
        else if (Program.Option("--startup") is { } file) Opened += (_, _) => Bench.ReportStartup(this, file);
    }

    // On dark the brand reads as its lighter middle shade, so each swatch shows the colour it gives there.
    private void PaintSwatches()
    {
        var dark = ActualThemeVariant == Avalonia.Styling.ThemeVariant.Dark;
        foreach (var swatch in Swatches.Children.OfType<Button>())
            if (swatch.Tag is Color c) swatch.Background = new SolidColorBrush(dark ? BrandDerivation.Derive(c, true).PrimaryMid : c);
    }

    private void OnKey(object? sender, KeyEventArgs e)
    {
        if (e.Key != Key.F12) return;
        if (e.KeyModifiers == KeyModifiers.Shift)
            RendererDiagnostics.DebugOverlays = RendererDiagnostics.DebugOverlays == RendererDebugOverlays.None
                ? RendererDebugOverlays.Fps | RendererDebugOverlays.LayoutTimeGraph | RendererDebugOverlays.RenderTimeGraph
                : RendererDebugOverlays.None;
        else Readout.Toggle();
        e.Handled = true;
    }

    private static ListBoxItem Item(Link link)
    {
        var row = new DockPanel();
        if (Application.Current!.TryFindResource(link.Icon, out var data) && data is Geometry geometry)
            row.Children.Add(new Icon { Data = geometry });
        row.Children.Add(new TextBlock { Text = link.Title, VerticalAlignment = VerticalAlignment.Center });
        var item = new ListBoxItem { Content = row, Tag = link };
        Shell.SetKeywords(item, link.Keywords);
        return item;
    }

    /// <summary>Opens a sidebar link as a click on it does.</summary>
    public void Select(Link link) => Nav.SelectedItem = Nav.Items.OfType<ListBoxItem>().First(i => ReferenceEquals(i.Tag, link));

    private void Open(Link link)
    {
        var clock = Stopwatch.StartNew();
        Current = link;
        Host.Content = link.Create();
        Scroller.Offset = default;
        PageTitle.Text = link.Title;
        PageStatus.Content = link.Title;
        Title = $"{link.Title} · PerfLab";
        RequestAnimationFrame(_ => Dispatcher.UIThread.Post(() => LastSwitchMs = clock.Elapsed.TotalMilliseconds, DispatcherPriority.Background));
    }
}
