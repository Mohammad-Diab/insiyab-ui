using Avalonia.Controls;
using Avalonia.LogicalTree;
using Insiyab.Avalonia.Controls;
using Insiyab.Avalonia.Gallery.Playground;

namespace Insiyab.Avalonia.Gallery.Pages;

[GalleryPage("palette", "لوحة الأوامر")]
public partial class PalettePage : UserControl
{
    public PalettePage()
    {
        InitializeComponent();
        PalettePlay.Wire(Example, null);
        var palette = Example.GetLogicalDescendants().OfType<Button>().Select(b => b.Resources["Palette"]).OfType<CommandPalette>().First();
        AddCommand.Click += (_, _) =>
        {
            var n = palette.Items.Count(i => i.Key?.StartsWith("demo-") == true) + 1;
            palette.Add(new PaletteItem { Label = $"أمر {n}", Key = $"demo-{n}", Group = "مضافة بالشيفرة", Keywords = "added", Run = () => Toast.Show(AddCommand, $"أمر {n}", ToastTone.Ok) });
            palette.Open(AddCommand);
        };
    }
}
