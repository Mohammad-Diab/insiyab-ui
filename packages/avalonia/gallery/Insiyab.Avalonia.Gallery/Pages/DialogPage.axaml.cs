using System.Collections.ObjectModel;
using System.Windows.Input;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Insiyab.Avalonia.Controls;

namespace Insiyab.Avalonia.Gallery.Pages;

/// <summary>A product in the choice dialog's sample: its name, its code (searched too, shown muted at the row's end) and the group it is listed under.</summary>
public sealed record GalleryProduct(string Name, string Code, string Group);

[GalleryPage("dialog", "مربع الحوار")]
public partial class DialogPage : UserControl
{
    private static readonly GalleryProduct[] Products = MakeProducts();

    public DialogPage() => InitializeComponent();

    private static GalleryProduct[] MakeProducts()
    {
        var own = new[] { ("كرسي مكتب دوار", "OFC601"), ("طاولة الاجتماعات الكبيرة", "TBL601"), ("شاشة عرض منحنية", "MON601") };
        var all = new[]
        {
            ("خزانة ملفات", "CAB402"), ("مصباح مكتب", "LMP301"), ("رف كتب", "SHL501"), ("لوح أبيض", "BRD401"), ("حامل شاشة", "STD302"),
            ("لوحة مفاتيح لاسلكية", "KEY201"), ("فأرة لاسلكية", "MSE202"), ("سماعات رأس", "HDP203"), ("آلة طباعة", "PRN301"),
            ("سلة مهملات", "BIN101"), ("ساعة حائط", "CLK502"), ("مقص ورق", "SCI401"), ("دبّاسة", "STP402"),
        };
        return [.. own.Select(c => new GalleryProduct(c.Item1, c.Item2, "المفضلة")), .. all.Select(c => new GalleryProduct(c.Item1, c.Item2, "كل المنتجات"))];
    }

    private ChoiceDialog Dialog => (ChoiceDialog)Resources["ProductDialog"]!;

    private ChoicePicker Picker => (ChoicePicker)Dialog.Content!;

    // Opens the dialog over the page; a pick shows the product beside the buttons.
    private async Task Choose(System.Collections.IEnumerable items, Control opener, Action<ChoicePicker>? state = null)
    {
        var picker = Picker;
        picker.SearchText = "";
        picker.IsLoading = false;
        picker.Error = null;
        picker.RetryCommand = null;
        picker.ItemsSource = items;
        state?.Invoke(picker);
        if (await Dialog.ShowAsync(opener) is GalleryProduct product)
            Chosen.Text = $"{product.Name} · {product.Code}";
    }

    private async void OnChooseProduct(object? sender, RoutedEventArgs e) => await Choose(Products, (Control)sender!);

    // The items come a moment after the dialog opens: skeleton rows until then, and what is typed meanwhile is kept.
    private async void OnChooseLate(object? sender, RoutedEventArgs e)
    {
        var items = new ObservableCollection<GalleryProduct>();
        await Choose(items, (Control)sender!, picker =>
        {
            picker.IsLoading = true;
            DispatcherTimer.RunOnce(() => Arrive(picker, items), TimeSpan.FromSeconds(1.5));
        });
    }

    // The first reading fails; the retry reads them after a moment.
    private async void OnChooseFailed(object? sender, RoutedEventArgs e)
    {
        var items = new ObservableCollection<GalleryProduct>();
        await Choose(items, (Control)sender!, picker =>
        {
            picker.Error = "تعذرت قراءة المنتجات.";
            picker.RetryCommand = new Run(() =>
            {
                picker.Error = null;
                picker.IsLoading = true;
                DispatcherTimer.RunOnce(() => Arrive(picker, items), TimeSpan.FromSeconds(1));
            });
        });
    }

    private static void Arrive(ChoicePicker picker, ObservableCollection<GalleryProduct> items)
    {
        foreach (var c in Products) items.Add(c);
        picker.IsLoading = false;
    }

    private sealed class Run(Action run) : ICommand
    {
        public event EventHandler? CanExecuteChanged { add { } remove { } }

        public bool CanExecute(object? parameter) => true;

        public void Execute(object? parameter) => run();
    }
}
