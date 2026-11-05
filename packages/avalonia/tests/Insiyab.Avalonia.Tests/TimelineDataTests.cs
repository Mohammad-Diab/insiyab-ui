using System.Collections;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Insiyab.Avalonia.Controls;
using static Insiyab.Avalonia.Tests.Ui;

namespace Insiyab.Avalonia.Tests;

// A timeline from data: member bindings, a live tone, the unread dot, day headings from a real date, and every event in view realised in a page scroller.
public class TimelineDataTests
{
    public sealed class EventRow : INotifyPropertyChanged
    {
        private string? _tone;
        private bool _isNow, _isAhead, _isNew;
        public string Title { get; init; } = "";
        public string? Body { get; init; }
        public string? When { get; init; }
        public DateTime? At { get; init; }
        public Geometry? Icon { get; init; }
        public string? Tone { get => _tone; set => Set(ref _tone, value); }
        public bool IsNow { get => _isNow; set => Set(ref _isNow, value); }
        public bool IsAhead { get => _isAhead; set => Set(ref _isAhead, value); }
        public bool IsNew { get => _isNew; set => Set(ref _isNew, value); }
        public event PropertyChangedEventHandler? PropertyChanged;
        private void Set<T>(ref T field, T value, [CallerMemberName] string? name = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value)) return;
            field = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }

    private static readonly Geometry Line = Geometry.Parse("M5 12h14");
    private const string Long = "نص أطول من عنوان اليوم، يصف ما جرى في هذا الحدث على سطر أو سطرين.";

    private static Timeline Bound(IEnumerable rows) => new()
    {
        ItemsSource = rows,
        HeaderBinding = new Binding(nameof(EventRow.Title)),
        ContentBinding = new Binding(nameof(EventRow.Body)),
        TimeBinding = new Binding(nameof(EventRow.When)),
        IconBinding = new Binding(nameof(EventRow.Icon)),
        ToneBinding = new Binding(nameof(EventRow.Tone)),
        IsCurrentBinding = new Binding(nameof(EventRow.IsNow)),
        IsPendingBinding = new Binding(nameof(EventRow.IsAhead)),
        IsUnreadBinding = new Binding(nameof(EventRow.IsNew)),
    };

    private static List<Control> Entries(Timeline tl) =>
        tl.GetRealizedContainers().Where(c => c.IsVisible).OrderBy(tl.IndexFromContainer).ToList();

    private static object?[] DayTexts(Timeline tl) => Entries(tl).OfType<TimelineDay>().Select(d => d.Content).ToArray();

    private static bool Has(Control c, string pseudo) => c.Classes.Contains(pseudo);

    private static ScrollViewer Page(Timeline tl) =>
        new() { Content = new StackPanel { Spacing = 12, Children = { new TextBlock { Text = "صفحة" }, tl } } };

    private static ObservableCollection<EventRow> Days(DateTime now) =>
    [
        new() { Title = "أ", At = now.Date.AddDays(-4).AddHours(8) },
        new() { Title = "ب", At = now.Date.AddDays(-4).AddHours(9) },
        new() { Title = "ج", At = now.Date.AddDays(-1).AddHours(18) },
        new() { Title = "د", At = now.Date.AddHours(9) },
        new() { Title = "هـ", At = now.Date.AddHours(10) },
    ];

    // A rectangle of a visual in window pixels, whichever way the window flows.
    private static Rect Box(Visual c, Rect r, Visual to)
    {
        var a = c.TranslatePoint(r.TopLeft, to)!.Value;
        var b = c.TranslatePoint(r.BottomRight, to)!.Value;
        return new Rect(new Point(Math.Min(a.X, b.X), Math.Min(a.Y, b.Y)), new Point(Math.Max(a.X, b.X), Math.Max(a.Y, b.Y)));
    }

    [AvaloniaFact]
    public void Members_fill_each_event()
    {
        var rows = new ObservableCollection<EventRow>
        {
            new() { Title = "أُرسل الطلب", When = "8:02 ص", Body = "نص الحدث.", Tone = "ok" },
            new() { Title = "قيد المراجعة", Icon = Line, Tone = "warn", IsNow = true },
            new() { Title = "الاعتماد", IsAhead = true },
        };
        var tl = Bound(rows);
        Show(tl, FlowDirection.RightToLeft);
        var e = Entries(tl).OfType<TimelineItem>().ToList();
        Assert.Equal(3, e.Count);
        Assert.Equal("أُرسل الطلب", e[0].Header);
        Assert.Equal("8:02 ص", e[0].TimeText);
        Assert.Equal("نص الحدث.", e[0].Content);
        Assert.True(Has(e[0], ":has-body"));
        Assert.Contains("ok", e[0].Classes);
        Assert.Null(e[1].Content);
        Assert.False(Has(e[1], ":has-body"));
        Assert.Same(Line, e[1].Icon);
        Assert.True(Has(e[1], ":icon") && Has(e[1], ":current"));
        Assert.Contains("warn", e[1].Classes);
        Assert.True(Has(e[2], ":pending"));
        Assert.True(Has(e[1], ":next-pending"));
        Assert.True(Has(e[2], ":last"));
    }

    [AvaloniaFact]
    public void A_tone_follows_its_member_live()
    {
        var row = new EventRow { Title = "التسليم", Tone = "warn" };
        var tl = Bound(new[] { row });
        Show(tl);
        var item = Entries(tl).OfType<TimelineItem>().Single();
        Assert.Contains("warn", item.Classes);
        row.Tone = "ok";
        Settle(100);
        Assert.Contains("ok", item.Classes);
        Assert.DoesNotContain("warn", item.Classes);
        row.Tone = null;
        Settle(100);
        Assert.DoesNotContain("ok", item.Classes);
    }

    [AvaloniaFact]
    public void At_writes_the_time_as_people_say_it()
    {
        var real = TimelineItem.Now;
        var now = new DateTime(2026, 9, 24, 12, 0, 0);
        TimelineItem.Now = () => now;
        try
        {
            var tl = new Timeline
            {
                ItemsSource = new[] { new EventRow { Title = "أُرسل", At = now.AddMinutes(-5) } },
                HeaderBinding = new Binding(nameof(EventRow.Title)),
                AtBinding = new Binding(nameof(EventRow.At)),
            };
            Show(tl, FlowDirection.RightToLeft);
            var item = Entries(tl).OfType<TimelineItem>().Single();
            Assert.Equal(TimeMode.Relative, item.TimeMode);
            Assert.Equal("قبل 5 دقائق", item.TimeText);
        }
        finally { TimelineItem.Now = real; }
    }

    [AvaloniaTheory]
    [InlineData("rtl")]
    [InlineData("ltr")]
    public void Unread_draws_a_brand_dot_just_after_the_title(string dir)
    {
        var row = new EventRow { Title = "رسالة جديدة", IsNew = true };
        var tl = Bound(new[] { row });
        var w = Show(new StackPanel { Margin = new Thickness(40), Children = { tl } }, dir == "rtl" ? FlowDirection.RightToLeft : FlowDirection.LeftToRight);
        var item = Entries(tl).OfType<TimelineItem>().Single();
        Assert.True(Has(item, ":unread"));
        var head = Part<TimelineHead>(item, "PART_Head");
        var title = Part<ContentPresenter>(item, "PART_HeaderPresenter");
        Assert.True(head.Dot);
        AssertBrush(head, "InsToneBrandSoft", () => head.DotBrush);
        Assert.Equal(8, head.DotRect.Width);
        Assert.InRange(head.DotRect.Left - title.DesiredSize.Width, 5.5, 6.5);
        var dot = Box(head, head.DotRect, w);
        var text = Box(title, new Rect(title.Bounds.Size), w);
        if (dir == "ltr") Assert.InRange(dot.Left - (text.Left + title.DesiredSize.Width), 5, 7);
        else Assert.InRange(text.Right - title.DesiredSize.Width - dot.Right, 5, 7);
        Capture(w, $"timeline-unread-{dir}");
        row.IsNew = false;
        Settle(100);
        Assert.False(head.Dot);
        Assert.Equal(default(Rect), head.DotRect);
    }

    [AvaloniaFact]
    public void A_heading_goes_before_each_day_from_a_real_date()
    {
        var real = TimelineItem.Now;
        var now = new DateTime(2026, 9, 24, 12, 0, 0);
        TimelineItem.Now = () => now;
        try
        {
            var rows = Days(now);
            var tl = Bound(rows);
            tl.DayOf = r => ((EventRow)r!).At;
            Show(tl, FlowDirection.RightToLeft, height: 900);
            var all = Entries(tl);
            Assert.Equal(new[] { "day", "e", "e", "day", "e", "day", "e", "e" }, all.Select(c => c is TimelineDay ? "day" : "e").ToArray());
            var days = all.OfType<TimelineDay>().ToList();
            Assert.Equal(new object?[] { "20 سبتمبر", "أمس", "اليوم" }, DayTexts(tl));
            Assert.Equal(new[] { false, false, true }, days.Select(d => Has(d, ":today")).ToArray());
            Assert.True(Has(days[0], ":first"));
            Assert.Equal(5, rows.Count);
        }
        finally { TimelineItem.Now = real; }
    }

    [AvaloniaFact]
    public void Day_headings_follow_changes_to_the_items()
    {
        var real = TimelineItem.Now;
        var now = new DateTime(2026, 9, 24, 12, 0, 0);
        TimelineItem.Now = () => now;
        try
        {
            var rows = Days(now);
            var tl = Bound(rows);
            tl.DayOf = r => ((EventRow)r!).At;
            Show(tl, FlowDirection.RightToLeft, height: 900);
            rows.Add(new EventRow { Title = "و", At = now.Date.AddDays(1).AddHours(9) });
            Settle(100);
            Assert.Equal(new object?[] { "20 سبتمبر", "أمس", "اليوم", "غدًا" }, DayTexts(tl));
            rows.RemoveAt(2);
            Settle(100);
            Assert.Equal(new object?[] { "20 سبتمبر", "اليوم", "غدًا" }, DayTexts(tl));
        }
        finally { TimelineItem.Now = real; }
    }

    [AvaloniaFact]
    public void A_day_header_writes_the_heading()
    {
        var real = TimelineItem.Now;
        var now = new DateTime(2026, 9, 24, 12, 0, 0);
        TimelineItem.Now = () => now;
        try
        {
            var tl = Bound(Days(now));
            tl.DayHeader = d => $"يوم {d.Day}";
            tl.DayOf = r => ((EventRow)r!).At;
            Show(tl, FlowDirection.RightToLeft, height: 900);
            Assert.Equal(new object?[] { "يوم 20", "يوم 23", "يوم 24" }, DayTexts(tl));
        }
        finally { TimelineItem.Now = real; }
    }

    [AvaloniaFact]
    public void Every_event_in_view_shows_inside_a_page_scroller()
    {
        var tl = new Timeline();
        tl.Items.Add(new TimelineDay { Content = "24 سبتمبر" });
        for (var i = 0; i < 8; i++) tl.Items.Add(new TimelineItem { Header = $"حدث {i}", Time = "8:02 ص", Content = Long });
        Show(Page(tl), FlowDirection.RightToLeft, height: 1600);
        var shown = Entries(tl);
        Assert.Equal(9, shown.Count);
        Assert.InRange(shown.Max(c => c.Bounds.Bottom), tl.Bounds.Height - 1, tl.Bounds.Height + 1);
    }

    [AvaloniaFact]
    public void Events_that_arrive_later_all_show_inside_a_page_scroller()
    {
        var rows = new ObservableCollection<EventRow>();
        var tl = Bound(rows);
        Show(Page(tl), FlowDirection.RightToLeft, height: 1600);
        for (var i = 0; i < 8; i++) rows.Add(new EventRow { Title = $"حدث {i}", When = "8:02 ص", Body = Long });
        Settle();
        Assert.Equal(8, Entries(tl).Count);
    }
}
