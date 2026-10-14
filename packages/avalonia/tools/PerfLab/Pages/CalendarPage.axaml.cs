using Avalonia.Controls;
using Insiyab.Avalonia.Controls;

namespace PerfLab.Pages;

public partial class CalendarPage : UserControl
{
    public CalendarPage()
    {
        InitializeComponent();
        var random = new Random(7);
        var tones = Enum.GetValues<MonthEventTone>();
        var events = new List<MonthEvent>();
        for (var day = 1; day <= 30; day++)
        {
            var count = random.Next(2, 7);
            for (var i = 0; i < count; i++)
            {
                var allDay = random.Next(6) == 0;
                events.Add(new MonthEvent
                {
                    Date = new DateTime(2026, 9, day),
                    Title = Sample.Words[random.Next(Sample.Words.Length)] + " " + (1000 + random.Next(9000)),
                    Time = allDay ? null : TimeSpan.FromMinutes(random.Next(7 * 2, 20 * 2) * 30),
                    Duration = allDay ? null : TimeSpan.FromMinutes(30 * random.Next(1, 5)),
                    Tone = tones[random.Next(tones.Length)],
                });
            }
        }
        Calendar.Events = events;
    }
}
