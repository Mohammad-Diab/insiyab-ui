using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Headless.XUnit;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.VisualTree;
using Insiyab.Avalonia.Brand;
using Insiyab.Avalonia.Controls;
using static Insiyab.Avalonia.Tests.Ui;

namespace Insiyab.Avalonia.Tests;

// A hovered linked event fills with its tone and its time takes its title's on-colour, readable (4.5:1), in the month and the week, every tone, light and dark.
public class MonthChipHoverTests
{
    private static readonly DateTime Day = new(2026, 9, 21);

    private static readonly MonthEventTone[] Tones = [MonthEventTone.Brand, MonthEventTone.Ok, MonthEventTone.Warn, MonthEventTone.Bad, MonthEventTone.Info];

    private static MonthCalendar Calendar(MonthCalendarView view) => new()
    {
        View = view,
        Date = Day,
        Locale = "ar",
        Events = [.. Tones.Select((t, i) => new MonthEvent { Date = Day.AddDays(i % 3), Time = new TimeSpan(9 + 2 * i, 0, 0), Title = $"Event {i}", Tone = t, Href = $"e{i}" })],
        EventDetails = new FuncDataTemplate<MonthEvent>((e, _) => new TextBlock { Text = e?.Title }, supportsRecycling: false),
    };

    [AvaloniaTheory]
    [InlineData("Light", MonthCalendarView.Month, "month-time")]
    [InlineData("Dark", MonthCalendarView.Month, "month-time")]
    [InlineData("Light", MonthCalendarView.Week, "week-event-time")]
    [InlineData("Dark", MonthCalendarView.Week, "week-event-time")]
    public void A_hovered_events_time_takes_its_titles_on_colour(string theme, MonthCalendarView view, string timeClass)
    {
        var month = Calendar(view);
        Show(month, FlowDirection.RightToLeft, theme == "Dark" ? ThemeVariant.Dark : ThemeVariant.Light, width: 1100, height: 900);
        Settle(200);

        var bad = new List<string>();
        foreach (var ev in month.Events)
        {
            var chip = Assert.IsAssignableFrom<Border>(month.ChipFor(ev));
            var texts = chip.GetVisualDescendants().OfType<TextBlock>().ToList();
            var time = texts.Single(t => t.Classes.Contains(timeClass));
            var title = texts.Single(t => t.Classes.Contains("month-event-title"));
            var resting = ColorOf(time.Foreground);

            ((IPseudoClasses)chip.Classes).Set(":pointerover", true);
            Settle(50);
            var fill = ColorOf(chip.Background);
            Assert.Equal(ColorOf(title.Foreground), ColorOf(time.Foreground));
            Assert.NotEqual(resting, ColorOf(time.Foreground));
            if (BrandDerivation.Contrast(ColorOf(time.Foreground), fill) < 4.5)
                bad.Add($"{ev.Tone}: {ColorOf(time.Foreground)} on {fill} is {BrandDerivation.Contrast(ColorOf(time.Foreground), fill):F2}");

            ((IPseudoClasses)chip.Classes).Set(":pointerover", false);
            Settle(50);
            Assert.Equal(resting, ColorOf(time.Foreground));
        }
        Assert.True(bad.Count == 0, string.Join("; ", bad));
    }
}
