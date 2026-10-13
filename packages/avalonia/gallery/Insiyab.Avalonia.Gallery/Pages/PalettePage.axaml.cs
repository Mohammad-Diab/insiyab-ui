using Avalonia.Controls;
using Avalonia.Input;
using Insiyab.Avalonia.Controls;

namespace Insiyab.Avalonia.Gallery.Pages;

[GalleryPage("palette", "لوحة الأوامر")]
public partial class PalettePage : UserControl
{
    private readonly CommandPalette _demo;
    private readonly CommandPalette _play = Play();
    private readonly CommandPalette _altK = Play();
    private IDisposable? _keys;

    public PalettePage()
    {
        InitializeComponent();
        _demo = (CommandPalette)Resources["DemoPalette"]!;
        foreach (var item in Play().Items.ToList()) Still.Items.Add(Copy(item));
        _altK.Gestures.Clear();
        _altK.Gestures.Add(new KeyGesture(Key.K, KeyModifiers.Alt));
        _altK.SlashOpens = false;
        OpenDemo.Click += (_, _) => _demo.Open(this);
        OpenPlay.Click += (_, _) => _play.Open(this);
        OpenAltK.Click += (_, _) => _altK.Open(this);
        AddCommand.Click += (_, _) =>
        {
            var n = _demo.Items.Count(i => i.Key?.StartsWith("demo-") == true) + 1;
            _demo.Add(new PaletteItem { Label = $"أمر {n}", Key = $"demo-{n}", Group = "مضافة بالشيفرة", Keywords = "added" });
            _demo.Open(this);
        };
        AttachedToVisualTree += (_, _) =>
        {
            if (TopLevel.GetTopLevel(this) is { } top) _keys = _altK.AttachTo(top);
        };
        DetachedFromVisualTree += (_, _) =>
        {
            _keys?.Dispose();
            _keys = null;
        };
    }

    private static CommandPalette Play()
    {
        var p = new CommandPalette();
        p.Add(
            new PaletteItem { Label = "الملفات", Group = "الصفحات", Keywords = "files" },
            new PaletteItem { Label = "الإعدادات", Group = "الصفحات", Keywords = "settings" },
            new PaletteItem { Label = "الرسائل", Group = "الصفحات", Keywords = "messages" },
            new PaletteItem { Label = "التقارير", Group = "الصفحات", Keywords = "reports" },
            new PaletteItem { Label = "حفظ", Group = "إجراءات", Keywords = "save", Hint = "Ctrl S" });
        return p;
    }

    private static PaletteItem Copy(PaletteItem i) =>
        new() { Label = i.Label, Group = i.Group, Keywords = i.Keywords, Hint = i.Hint };
}
