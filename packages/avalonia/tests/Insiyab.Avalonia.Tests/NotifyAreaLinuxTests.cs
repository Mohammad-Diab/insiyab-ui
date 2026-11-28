using Avalonia.Headless.XUnit;
using Insiyab.Avalonia.Controls;

namespace Insiyab.Avalonia.Tests;

// The Linux icon's pure parts (the menu as dbusmenu reads it, the icon's pixels) and how the icon follows a shell that adds it later. No test talks to a bus.
public class NotifyAreaLinuxTests
{
    private sealed class LateShell : INotifyAreaShell
    {
        public readonly List<string> Calls = [];

        public Action<NotifyAreaEvent, int, int>? Raised { get; set; }

        public bool Add(string tip)
        {
            Calls.Add("add " + tip);
            return false; // the desktop takes it later
        }

        public void SetTip(string tip) => Calls.Add("tip " + tip);

        public void ShowBalloon(string title, string text, NotifyAreaLevel level) => Calls.Add("balloon");

        public int ShowMenu(IReadOnlyList<NotifyAreaMenuItem> rows, int x, int y, bool rightToLeft)
        {
            Calls.Add($"rows {rows.Count} {(rightToLeft ? "rtl" : "ltr")}");
            return 0;
        }

        public void Dispose() => Calls.Add("dispose");
    }

    private static readonly byte[] NoIco = [0, 0, 1, 0, 0, 0];

    [AvaloniaFact]
    public void An_icon_the_desktop_takes_later_is_added_then_and_says_so_once()
    {
        var shell = new LateShell();
        var before = NotifyAreaIcon.MakeShell;
        NotifyAreaIcon.MakeShell = _ => shell;
        try
        {
            using var icon = new NotifyAreaIcon(NoIco, "تطبيق");
            Assert.False(icon.IsAdded);
            var changes = new List<bool>();
            icon.IsAddedChanged += (_, _) => changes.Add(icon.IsAdded);
            shell.Raised!(NotifyAreaEvent.Added, 1, 0);
            shell.Raised!(NotifyAreaEvent.Added, 1, 0);
            Assert.True(icon.IsAdded);
            Assert.Equal(new[] { true }, changes);
            shell.Raised!(NotifyAreaEvent.Added, 0, 0);
            Assert.False(icon.IsAdded);
            Assert.Equal(new[] { true, false }, changes);

            // the desktop asks for the rows: they are written again and handed over, and nothing is picked here
            var opening = 0;
            icon.MenuOpening += (_, _) =>
            {
                opening++;
                icon.Menu.Clear();
                icon.Menu.Add(new NotifyAreaMenuItem("فتح"));
            };
            shell.Raised!(NotifyAreaEvent.Menu, 0, 0);
            Assert.Equal(1, opening);
            Assert.Equal("rows 1 ltr", shell.Calls[^1]);
        }
        finally
        {
            NotifyAreaIcon.MakeShell = before;
        }
    }

    [AvaloniaFact]
    public void On_linux_with_no_session_bus_here_nothing_shows_and_nothing_fails()
    {
        Os.Fake = OsKind.Linux;
        try
        {
            Assert.False(NotifyAreaIcon.IsSupported);
            using var icon = new NotifyAreaIcon(NoIco, "tip");
            Assert.False(icon.IsAdded);
            icon.ShowBalloon("a", "b");
            icon.Tip = "x";
        }
        finally
        {
            Os.Fake = null;
        }
    }

    [Fact]
    public void Rows_read_as_dbusmenu_expects_them()
    {
        Assert.Equal("org.kde.StatusNotifierItem-412-3", StatusNotifier.BusName(412, 3));
        Assert.Equal("a__b", StatusNotifier.Label("a_b"));
        Assert.Equal("rtl", StatusNotifier.Direction(true));
        Assert.Equal("ltr", StatusNotifier.Direction(false));

        Assert.Equal([new StatusMenuProperty("label", "فتح")], StatusNotifier.Properties(new NotifyAreaMenuItem("فتح")));
        Assert.Equal([new StatusMenuProperty("type", "separator")], StatusNotifier.Properties(NotifyAreaMenuItem.Separator()));
        Assert.Equal(
            [new StatusMenuProperty("label", "الإشعارات"), new("enabled", false), new("toggle-type", "checkmark"), new("toggle-state", 1)],
            StatusNotifier.Properties(new NotifyAreaMenuItem("الإشعارات") { IsChecked = true, IsEnabled = false }));

        var rows = StatusNotifier.Rows([new NotifyAreaMenuItem("فتح"), NotifyAreaMenuItem.Separator(), new NotifyAreaMenuItem("إنهاء")]);
        Assert.Equal(new[] { 1, 2, 3 }, rows.Select(r => r.Id));
        Assert.True(StatusNotifier.Same(rows, StatusNotifier.Rows([new NotifyAreaMenuItem("فتح"), NotifyAreaMenuItem.Separator(), new NotifyAreaMenuItem("إنهاء")])));
        Assert.False(StatusNotifier.Same(rows, StatusNotifier.Rows([new NotifyAreaMenuItem("فتح"), NotifyAreaMenuItem.Separator(), new NotifyAreaMenuItem("إنهاء") { IsEnabled = false }])));
        Assert.False(StatusNotifier.Same(rows, StatusNotifier.Rows([new NotifyAreaMenuItem("فتح")])));
    }

    // A 32-bit .ico picture: the header, the rows from the bottom up as BGRA, then the 1-bit mask (rows of 4 bytes).
    private static byte[] Dib(int width, int height, Func<int, int, (byte B, byte G, byte R, byte A)> pixel, Func<int, int, bool>? masked = null)
    {
        var data = new List<byte>();
        data.AddRange(BitConverter.GetBytes(40));
        data.AddRange(BitConverter.GetBytes(width));
        data.AddRange(BitConverter.GetBytes(height * 2));
        data.AddRange(BitConverter.GetBytes((short)1));
        data.AddRange(BitConverter.GetBytes((short)32));
        data.AddRange(new byte[24]);
        for (var y = height - 1; y >= 0; y--)
            for (var x = 0; x < width; x++)
            {
                var (b, g, r, a) = pixel(x, y);
                data.AddRange([b, g, r, a]);
            }
        for (var y = height - 1; y >= 0; y--)
        {
            var row = new byte[(width + 31) / 32 * 4];
            for (var x = 0; x < width; x++)
                if (masked?.Invoke(x, y) == true) row[x / 8] |= (byte)(0x80 >> (x % 8));
            data.AddRange(row);
        }
        return [.. data];
    }

    [Fact]
    public void A_32_bit_picture_becomes_argb_from_the_top()
    {
        // top-left red and half see-through, the rest blue
        var dib = Dib(2, 2, (x, y) => x == 0 && y == 0 ? ((byte)0, (byte)0, (byte)255, (byte)128) : ((byte)255, (byte)0, (byte)0, (byte)255));
        var pixmap = StatusNotifier.FromDib(dib)!;
        Assert.Equal((2, 2), (pixmap.Width, pixmap.Height));
        Assert.Equal(new byte[] { 128, 255, 0, 0 }, pixmap.Argb[..4]);
        Assert.Equal(new byte[] { 255, 0, 0, 255 }, pixmap.Argb[4..8]);
        Assert.Equal(new byte[] { 255, 0, 0, 255 }, pixmap.Argb[12..16]);

        // no alpha at all: the mask says what is see-through
        var old = StatusNotifier.FromDib(Dib(2, 1, (_, _) => ((byte)1, (byte)2, (byte)3, (byte)0), (x, _) => x == 1))!;
        Assert.Equal(new byte[] { 255, 3, 2, 1, 0, 3, 2, 1 }, old.Argb);

        var eightBit = Dib(2, 2, (_, _) => default);
        eightBit[14] = 8;
        Assert.Null(StatusNotifier.FromDib(eightBit));
        Assert.Null(StatusNotifier.FromDib(new byte[12]));
    }

    [Fact]
    public void The_icon_takes_every_32_bit_picture_of_the_ico_smallest_first_and_leaves_out_png()
    {
        var pictures = new List<byte[]>
        {
            Dib(4, 4, (_, _) => ((byte)0, (byte)0, (byte)0, (byte)255)),
            new byte[] { 0x89, (byte)'P', (byte)'N', (byte)'G', 0, 0, 0, 0 },
            Dib(2, 2, (_, _) => ((byte)0, (byte)0, (byte)0, (byte)255)),
        };
        var ico = new List<byte> { 0, 0, 1, 0, (byte)pictures.Count, 0 };
        var offset = 6 + 16 * pictures.Count;
        foreach (var p in pictures)
        {
            ico.AddRange([0, 0, 0, 0, 1, 0, 32, 0]);
            ico.AddRange(BitConverter.GetBytes(p.Length));
            ico.AddRange(BitConverter.GetBytes(offset));
            offset += p.Length;
        }
        foreach (var p in pictures) ico.AddRange(p);

        var pixmaps = StatusNotifier.Pixmaps([.. ico]);
        Assert.Equal(new[] { 2, 4 }, pixmaps.Select(p => p.Width));
        Assert.Empty(StatusNotifier.Pixmaps(NoIco));
        Assert.Empty(StatusNotifier.Pixmaps([1, 2, 3]));
    }
}
