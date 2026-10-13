using Avalonia.Controls;
using Insiyab.Avalonia.Controls;

namespace Insiyab.Avalonia.Gallery.Pages;

[GalleryPage("hijri", "التقويم الهجري")]
public partial class HijriPage : UserControl
{
    private static readonly DateTime Day = new(2026, 9, 24);

    public HijriPage()
    {
        InitializeComponent();

        Demo.SelectedDate = Day;
        Play.SelectedDate = Day;
        PlayRange.SelectedDate = new DateTime(2026, 9, 12);
        PlayRange.EndDate = new DateTime(2026, 10, 11);
        PlayCivil.SelectedDate = Day;

        StillHijri.SelectedDate = Day;
        StillHijri.Show(Day);
        StillGregorian.SelectedDate = Day;
        StillGregorian.Show(Day);
    }
}
