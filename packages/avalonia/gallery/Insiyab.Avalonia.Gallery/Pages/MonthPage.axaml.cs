using Avalonia.Controls;
using Insiyab.Avalonia.Controls;

namespace Insiyab.Avalonia.Gallery.Pages;

[GalleryPage("month", "تقويم الشهر")]
public partial class MonthPage : UserControl
{
    private static readonly DateTime Sep = new(2026, 9, 1);

    public MonthPage()
    {
        InitializeComponent();

        Demo.Month = Sep;
        Demo.Events =
        [
            Event(3, null, "تسليم الطلب 1042", MonthEventTone.Warn, "#month"),
            Event(8, new TimeSpan(10, 0, 0), "اجتماع الفريق"),
            Event(8, new TimeSpan(14, 30, 0), "مراجعة العقد", MonthEventTone.Info),
            Event(15, null, "إطلاق الحملة", MonthEventTone.Ok),
            Event(21, new TimeSpan(9, 0, 0), "زيارة المورد"),
            Event(21, new TimeSpan(11, 0, 0), "مكالمة العميل"),
            Event(21, new TimeSpan(13, 0, 0), "تدريب"),
            Event(21, new TimeSpan(16, 0, 0), "موعد نهائي", MonthEventTone.Bad),
            Event(26, null, "جرد المخزون", MonthEventTone.Info),
        ];

        string[] ar = ["تسليم", "اجتماع", "مراجعة", "ورشة عمل", "مكالمة", "تدريب", "زيارة", "موعد نهائي", "إجازة"];
        string[] en = ["Delivery", "Meeting", "Review", "Workshop", "Call", "Training", "Visit", "Deadline", "Holiday"];

        Show(Play, ar, false, MonthEventTone.Brand, false);
        Show(PlayLink, ar, false, MonthEventTone.Brand, true);
        Show(PlayMany, ar, true, MonthEventTone.Brand, false);
        Show(PlayWarn, ar, false, MonthEventTone.Warn, false);
        Show(PlayEnglish, en, false, MonthEventTone.Brand, false);
        Show(PlayPhone, ar, false, MonthEventTone.Brand, false);
    }

    private static MonthEvent Event(int day, TimeSpan? time, string title, MonthEventTone tone = MonthEventTone.Brand, string? href = null) =>
        new() { Date = new DateTime(2026, 9, day), Time = time, Title = title, Tone = tone, Href = href };

    // The web playground's list: the day's three events, and three more on the 21st with the "more than three" switch.
    private static void Show(MonthCalendar month, string[] t, bool more, MonthEventTone tone, bool link)
    {
        var list = new List<MonthEvent>
        {
            Event(3, null, t[0], tone, link ? "/events/1" : null),
            Event(8, new TimeSpan(10, 0, 0), t[1], tone),
            Event(8, new TimeSpan(14, 30, 0), t[2], tone),
            Event(15, null, t[3], tone),
            Event(21, new TimeSpan(9, 0, 0), t[4], tone),
        };
        if (more)
        {
            list.Add(Event(21, new TimeSpan(11, 0, 0), t[5], tone));
            list.Add(Event(21, new TimeSpan(13, 0, 0), t[6], tone));
            list.Add(Event(21, new TimeSpan(16, 0, 0), t[7], tone));
        }
        list.Add(Event(26, null, t[8], tone));
        month.Month = Sep;
        month.Events = list;
    }
}
