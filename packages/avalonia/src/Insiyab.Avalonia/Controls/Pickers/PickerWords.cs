using System.Collections.Specialized;
using CultureInfo = System.Globalization.CultureInfo;
using DateTimeFormatInfo = System.Globalization.DateTimeFormatInfo;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Data.Converters;

namespace Insiyab.Avalonia.Controls;

/// <summary>Gives Avalonia's Calendar, CalendarDatePicker, DatePicker and TimePicker the library's words and Gregorian dates: Arabic when the control flows right to left, English otherwise, whatever the machine's culture.</summary>
public static class PickerWords
{
    public static readonly AttachedProperty<bool> IsEnabledProperty =
        AvaloniaProperty.RegisterAttached<TemplatedControl, bool>("IsEnabled", typeof(PickerWords));

    private static bool s_writing;

    static PickerWords()
    {
        TemplatedControl.TemplateAppliedEvent.AddClassHandler<CalendarItem>(OnCalendarItem);
        TemplatedControl.TemplateAppliedEvent.AddClassHandler<CalendarDatePicker>(OnDateField);
        TemplatedControl.TemplateAppliedEvent.AddClassHandler<DatePicker>(OnDateFace);
        TemplatedControl.TemplateAppliedEvent.AddClassHandler<TimePicker>(OnTimeFace);
        TemplatedControl.TemplateAppliedEvent.AddClassHandler<DatePickerPresenter>(OnDateDrum);
        TemplatedControl.TemplateAppliedEvent.AddClassHandler<TimePickerPresenter>(OnTimeDrum);
    }

    public static bool GetIsEnabled(TemplatedControl element) => element.GetValue(IsEnabledProperty);
    public static void SetIsEnabled(TemplatedControl element, bool value) => element.SetValue(IsEnabledProperty, value);

    private static PickerLocale Words(Visual v) => PickerLocale.For(null, v.FlowDirection);

    // Avalonia writes its culture's text into these parts whenever it refreshes them; each write is answered here with the library's text.
    private static void Write(Action write)
    {
        if (s_writing) return;
        s_writing = true;
        try { write(); }
        finally { s_writing = false; }
    }

    private static void OnChange(AvaloniaObject target, AvaloniaProperty property, Action fix) =>
        target.PropertyChanged += (_, e) => { if (e.Property == property) fix(); };

    private static void Set(TextBlock? block, string text)
    {
        if (block is not null && block.Text != text) Write(() => block.Text = text);
    }

    private static void OnCalendarItem(CalendarItem item, TemplateAppliedEventArgs e)
    {
        if (!GetIsEnabled(item) || item.TemplatedParent is not Calendar calendar) return;
        var header = e.NameScope.Find<Button>("PART_HeaderButton");
        var titles = e.NameScope.Find<Grid>("PART_MonthView")?.Children.Take(7).ToArray() ?? [];
        var months = e.NameScope.Find<Grid>("PART_YearView")?.Children.OfType<CalendarButton>().ToArray() ?? [];
        void Header()
        {
            if (header is null || calendar.DisplayMode != CalendarMode.Month) return;
            var text = Words(item).MonthYear(PickerCalendar.Gregorian, calendar.DisplayDate);
            if (!Equals(header.Content, text)) Write(() => header.Content = text);
        }
        void Title(int i)
        {
            var text = Words(item).Narrow((DayOfWeek)(((int)calendar.FirstDayOfWeek + i) % 7));
            if (!Equals(titles[i].DataContext, text)) Write(() => titles[i].DataContext = text);
        }
        void Month(CalendarButton button)
        {
            if (calendar.DisplayMode != CalendarMode.Year || button.DataContext is not DateTime d) return;
            var text = Words(item).ShortMonth(d.Month - 1);
            if (!Equals(button.Content, text)) Write(() => button.Content = text);
        }
        void All()
        {
            Header();
            for (var i = 0; i < titles.Length; i++) Title(i);
            foreach (var button in months) Month(button);
        }
        if (header is not null) OnChange(header, ContentControl.ContentProperty, Header);
        for (var i = 0; i < titles.Length; i++)
        {
            var k = i;
            OnChange(titles[i], StyledElement.DataContextProperty, () => Title(k));
        }
        foreach (var button in months)
        {
            OnChange(button, ContentControl.ContentProperty, () => Month(button));
            OnChange(button, StyledElement.DataContextProperty, () => Month(button));
        }
        OnChange(item, Visual.FlowDirectionProperty, All);
        All();
    }

    private static void OnDateField(CalendarDatePicker picker, TemplateAppliedEventArgs e)
    {
        if (!GetIsEnabled(picker) || e.NameScope.Find<TextBox>("PART_TextBox") is not { } box) return;
        if (picker.TextConverter is null) picker.SetCurrentValue(CalendarDatePicker.TextConverterProperty, new FieldText(picker));
        if (picker.TextConverter is not FieldText text) return;
        void Placeholder()
        {
            if (!string.IsNullOrEmpty(picker.PlaceholderText) || picker.UseFloatingPlaceholder || string.IsNullOrEmpty(box.PlaceholderText)) return;
            var want = "<" + text.Pattern + ">";
            if (box.PlaceholderText != want) Write(() => box.PlaceholderText = want);
        }
        void Refresh()
        {
            if (picker.SelectedDate is { } d) picker.SetCurrentValue(CalendarDatePicker.TextProperty, text.Format(d));
            Placeholder();
        }
        OnChange(box, TextBox.PlaceholderTextProperty, Placeholder);
        OnChange(picker, Visual.FlowDirectionProperty, Refresh);
        Refresh();
    }

    private static void OnDateFace(DatePicker picker, TemplateAppliedEventArgs e)
    {
        if (!GetIsEnabled(picker)) return;
        var day = e.NameScope.Find<TextBlock>("PART_DayTextBlock");
        var month = e.NameScope.Find<TextBlock>("PART_MonthTextBlock");
        var year = e.NameScope.Find<TextBlock>("PART_YearTextBlock");
        void Fix()
        {
            var words = Words(picker);
            if (picker.SelectedDate is { } d)
            {
                Set(day, d.DateTime.ToString(picker.DayFormat, words.Format));
                Set(month, d.DateTime.ToString(picker.MonthFormat, words.Format));
                Set(year, d.DateTime.ToString(picker.YearFormat, words.Format));
                return;
            }
            Set(day, words.Day);
            Set(month, words.Month);
            Set(year, words.Year);
        }
        foreach (var block in new[] { day, month, year })
            if (block is not null) OnChange(block, TextBlock.TextProperty, Fix);
        OnChange(picker, Visual.FlowDirectionProperty, Fix);
        Fix();
    }

    private static void OnTimeFace(TimePicker picker, TemplateAppliedEventArgs e)
    {
        if (!GetIsEnabled(picker)) return;
        var hour = e.NameScope.Find<TextBlock>("PART_HourTextBlock");
        var minute = e.NameScope.Find<TextBlock>("PART_MinuteTextBlock");
        var second = e.NameScope.Find<TextBlock>("PART_SecondTextBlock");
        var period = e.NameScope.Find<TextBlock>("PART_PeriodTextBlock");
        void Fix()
        {
            var words = Words(picker);
            Set(period, (picker.SelectedTime?.Hours ?? DateTime.Now.Hour) >= 12 ? words.Pm : words.Am);
            if (picker.SelectedTime is not null) return;
            Set(hour, words.Hour);
            Set(minute, words.Minute);
            Set(second, words.Second);
        }
        foreach (var block in new[] { hour, minute, second, period })
            if (block is not null) OnChange(block, TextBlock.TextProperty, Fix);
        OnChange(picker, Visual.FlowDirectionProperty, Fix);
        Fix();
    }

    private static void OnDateDrum(DatePickerPresenter presenter, TemplateAppliedEventArgs e)
    {
        if (!GetIsEnabled(presenter)) return;
        var days = e.NameScope.Find<DateTimePickerPanel>("PART_DaySelector");
        var months = e.NameScope.Find<DateTimePickerPanel>("PART_MonthSelector");
        var years = e.NameScope.Find<DateTimePickerPanel>("PART_YearSelector");
        string Text(DateTimePickerPanel panel, int v)
        {
            var format = Words(presenter).Format;
            if (panel == years) return new DateTime(v, 1, 1).ToString(panel.ItemFormat, format);
            if (panel == months) return new DateTime(2000, v, 1).ToString(panel.ItemFormat, format);
            var y = years?.SelectedValue is >= 1 and <= 9999 and var sy ? sy : 2000;
            var m = months?.SelectedValue is >= 1 and <= 12 and var sm ? sm : 1;
            var date = v <= DateTime.DaysInMonth(y, m) ? new DateTime(y, m, v) : new DateTime(2000, 1, v);
            return date.ToString(panel.ItemFormat, format);
        }
        foreach (var panel in new[] { days, months, years })
            if (panel is not null) Drum(presenter, panel, v => Text(panel, v));
    }

    private static void OnTimeDrum(TimePickerPresenter presenter, TemplateAppliedEventArgs e)
    {
        if (!GetIsEnabled(presenter) || e.NameScope.Find<DateTimePickerPanel>("PART_PeriodSelector") is not { } panel) return;
        Drum(presenter, panel, v => v == panel.MinimumValue ? Words(presenter).Am : Words(presenter).Pm);
    }

    // A wheel's items carry their value in Tag and are reused as it turns, so each new value is written again.
    private static void Drum(Control presenter, DateTimePickerPanel panel, Func<int, string> text)
    {
        void Fix(ListBoxItem item)
        {
            if (item.Tag is not int v) return;
            var want = text(v);
            if (!Equals(item.Content, want)) Write(() => item.Content = want);
        }
        void Hook(ListBoxItem item)
        {
            OnChange(item, ContentControl.ContentProperty, () => Fix(item));
            OnChange(item, Control.TagProperty, () => Fix(item));
            Fix(item);
        }
        foreach (var item in panel.Children.OfType<ListBoxItem>()) Hook(item);
        panel.Children.CollectionChanged += (_, e) =>
        {
            if (e.Action == NotifyCollectionChangedAction.Add && e.NewItems is not null)
                foreach (var item in e.NewItems.OfType<ListBoxItem>()) Hook(item);
        };
        OnChange(presenter, Visual.FlowDirectionProperty, () => { foreach (var item in panel.Children.OfType<ListBoxItem>()) Fix(item); });
    }

    private sealed class FieldText(CalendarDatePicker picker) : IValueConverter
    {
        private DateTimeFormatInfo Culture => Words(picker).Format;

        public string Pattern => picker.SelectedDateFormat switch
        {
            CalendarDatePickerFormat.Long => Culture.LongDatePattern,
            CalendarDatePickerFormat.Custom => picker.CustomDateFormatString,
            _ => Culture.ShortDatePattern,
        };

        public string Format(DateTime d) => d.ToString(Pattern, Culture);

        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture) => value is DateTime d ? Format(d) : null;

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            var text = value as string ?? string.Empty;
            return picker.SelectedDateFormat == CalendarDatePickerFormat.Custom && !string.IsNullOrEmpty(picker.CustomDateFormatString)
                ? DateTime.ParseExact(text, picker.CustomDateFormatString, Culture)
                : DateTime.Parse(text, Culture);
        }
    }
}
