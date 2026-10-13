using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Insiyab.Avalonia.Brand;

namespace Insiyab.Avalonia.Gallery.Pages;

[GalleryPage("color", "اللون والهوية")]
public partial class ColorPage : UserControl
{
    private static readonly string[] TokenNames = ["primary", "primary-dark", "primary-darker", "on-primary", "wash-1", "wash-2"];

    public ColorPage()
    {
        InitializeComponent();
        ActualThemeVariantChanged += (_, _) => Paint();
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        Paint();
    }

    private void OnSwatch(object? sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: string hex })
            Apply(Color.Parse(hex));
    }

    private void OnCustomKey(object? sender, KeyEventArgs e)
    {
        if (e.Key != Key.Enter || !Color.TryParse(CustomHex.Text?.Trim(), out var color))
            return;
        CustomDot.Background = new SolidColorBrush(color);
        Apply(color);
    }

    private void Apply(Color color)
    {
        InsiyabTheme.SetPrimary(color);
        Paint();
    }

    private static string Hex(Color c) => $"#{c.R:X2}{c.G:X2}{c.B:X2}";

    // The derived values as the page reads them back, as the web reads its computed styles.
    private void Paint()
    {
        Tokens.Children.Clear();
        foreach (var name in TokenNames)
        {
            var key = "Ins" + string.Concat(name.Split('-').Select(p => char.ToUpperInvariant(p[0]) + p[1..])) + "Color";
            var color = this.TryFindResource(key, ActualThemeVariant, out var value) && value is Color c ? c : Colors.Transparent;
            Tokens.Children.Add(new Border
            {
                Classes = { "token" },
                Child = new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    Spacing = 10,
                    Children =
                    {
                        new Border { Classes = { "chip" }, Background = new SolidColorBrush(color) },
                        new TextBlock { Text = $"--ins-{name}\n{Hex(color)}", VerticalAlignment = VerticalAlignment.Center, TextTrimming = TextTrimming.CharacterEllipsis },
                    },
                },
            });
        }

        var primary = InsiyabTheme.Primary;
        var white = BrandDerivation.Contrast(Colors.White, primary).ToString("0.0", CultureInfo.InvariantCulture);
        var dark = BrandDerivation.Contrast(Color.FromRgb(0x11, 0x11, 0x11), primary).ToString("0.0", CultureInfo.InvariantCulture);
        var label = BrandDerivation.LabelFor(primary) == Colors.White ? "الأبيض" : "الأسود القريب";
        LabelNote.Text = $"الأبيض على هذا اللون ⁦{white}:1⁩، والأسود القريب ⁦{dark}:1⁩ — فاختير {label}.";
    }
}
