using Avalonia.Controls;
using Insiyab.Avalonia.Controls;

namespace Insiyab.Avalonia.Gallery.Pages;

[GalleryPage("pickers", "أدوات الاختيار")]
public partial class PickersPage : UserControl
{
    private static readonly DateTime Sep30 = new(2026, 9, 30);

    public PickersPage()
    {
        InitializeComponent();

        MainCity.ItemsSource = new[] { "الرياض", "جدّة", "القاهرة" };
        MainCity.TextFilter = ArabicMatch.Contains;
        MainDate.SelectedDate = Sep30;
        MainTime.SelectedTime = new TimeSpan(9, 30, 0);

        ComboPlay.ItemsSource = new[] { "الرياض", "جدّة", "القاهرة", "الإسكندريّة" };
        ComboPlay.TextFilter = ArabicMatch.Contains;

        DatePlay.SelectedDate = Sep30;
        RangePlay.SelectedDate = new DateTime(2026, 9, 16);
        RangePlay.EndDate = new DateTime(2026, 9, 23);
        DateTimePlay.SelectedDate = new DateTime(2026, 9, 26);
        DateTimePlay.SelectedTime = new TimeSpan(14, 30, 0);
        GroupPlay.SelectedDate = new DateTime(2026, 9, 26);
        GroupPlay.SelectedTime = new TimeSpan(14, 30, 0);
        BoundedPlay.SelectedDate = Sep30;
        BoundedPlay.MinDate = new DateTime(2026, 9, 1);
        BoundedPlay.MaxDate = new DateTime(2026, 10, 31);

        StillSingle.SelectedDate = Sep30;
        StillSingle.Show(Sep30);
        StillRange.SelectedDate = new DateTime(2026, 9, 16);
        StillRange.EndDate = new DateTime(2026, 9, 23);
        StillRange.Show(new DateTime(2026, 9, 16));
        StillDateTime.SelectedDate = new DateTime(2026, 9, 26);
        StillDateTime.Time = new TimeSpan(14, 30, 0);
        StillDateTime.Show(new DateTime(2026, 9, 26));

        TimePlay.SelectedTime = new TimeSpan(9, 0, 0);
        Step15.SelectedTime = new TimeSpan(9, 0, 0);
        Step60.SelectedTime = new TimeSpan(9, 0, 0);
        BoundedTime.SelectedTime = new TimeSpan(9, 0, 0);
        BoundedTime.MinTime = new TimeSpan(8, 0, 0);
        BoundedTime.MaxTime = new TimeSpan(18, 0, 0);
        StillDrum.Time = new TimeSpan(9, 0, 0);

        var native = new DateTimeOffset(Sep30);
        NativeDatePicker.SelectedDate = Sep30;
        NativeDate.SelectedDate = native;
        NativeTime.SelectedTime = new TimeSpan(9, 30, 0);
        NativeCalendar.SelectedDate = Sep30;
        NativeCalendar.DisplayDate = Sep30;
        StillDatePresenter.Date = native;
        StillTimePresenter.Time = new TimeSpan(9, 30, 0);
    }
}
