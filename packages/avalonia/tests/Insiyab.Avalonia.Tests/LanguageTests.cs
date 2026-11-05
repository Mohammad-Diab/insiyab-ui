using System.Runtime.CompilerServices;
using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.VisualTree;
using Insiyab.Avalonia.Controls;
using static Insiyab.Avalonia.Tests.Ui;

namespace Insiyab.Avalonia.Tests;

public class LanguageTests
{
    private static void With(string? language, IReadOnlyDictionary<string, string>? strings, Action test)
    {
        InsiyabTheme.SetLanguage(language, strings);
        try { test(); }
        finally { InsiyabTheme.SetLanguage(null); }
    }

    private static void RightClick(Window w, Control c)
    {
        var p = Center(c, w);
        w.MouseMove(p);
        w.MouseDown(p, MouseButton.Right);
        w.MouseUp(p, MouseButton.Right);
        Settle(200);
    }

    private static string ReadmePath([CallerFilePath] string here = "") =>
        Path.GetFullPath(Path.Combine(Path.GetDirectoryName(here)!, "..", "..", "README.md"));

    [AvaloniaFact]
    public void With_no_language_the_words_stay_as_they_were()
    {
        Assert.Null(InsiyabTheme.Language);
        var palette = new CommandPalette();
        var wizard = new Wizard();
        var text = new SelectableTextBlock { Text = "Some text", HorizontalAlignment = HorizontalAlignment.Left };
        var w = Show(new StackPanel { Margin = new Thickness(40), Children = { text, wizard } }, FlowDirection.LeftToRight);
        Assert.Equal("ابحث عن صفحة أو أمر…", palette.PlaceholderText);
        Assert.Equal("التالي", wizard.NextText);
        RightClick(w, text);
        Assert.Equal("Copy", Assert.Single(((PopMenu)text.ContextFlyout!).Items.OfType<MenuItem>()).Header);
    }

    [AvaloniaFact]
    public void A_language_writes_the_fixed_words_in_it()
    {
        With("en", null, () =>
        {
            var wizard = new Wizard();
            var dialog = new Dialog();
            var notify = new Notify();
            var caption = new CaptionButtons();
            var palette = new CommandPalette();
            Show(new StackPanel { Children = { wizard, caption } }, FlowDirection.RightToLeft);
            Assert.Equal(("Previous", "Next", "Submit"), (wizard.PreviousText, wizard.NextText, wizard.FinishText));
            Assert.Equal("Close", dialog.CloseLabel);
            Assert.Equal("Notifications", notify.Title);
            Assert.Equal(("Minimize", "Close"), (caption.MinimizeLabel, caption.CloseLabel));
            Assert.Equal("Search for a page or command…", palette.PlaceholderText);
            Assert.Equal("Fill in this field.", Validate.Problem(new TextBox { [Validate.RequiredProperty] = true }));
        });
    }

    [AvaloniaFact]
    public void The_app_words_take_the_place_of_the_library_words_and_the_rest_fall_back_to_english()
    {
        var words = new Dictionary<string, string> { ["text.copy"] = "Copier", ["text.paste"] = "Coller", ["wizard.next"] = "Suivant" };
        With("fr", words, () =>
        {
            Assert.Equal("fr", InsiyabTheme.Language);
            Assert.Equal("Copier", InsiyabTheme.Strings["text.copy"]);
            var box = new TextBox { Text = "texte", Width = 200 };
            var wizard = new Wizard();
            var w = Show(new StackPanel { Margin = new Thickness(40), Children = { box, wizard } }, FlowDirection.RightToLeft);
            RightClick(w, box);
            var heads = ((PopMenu)box.ContextFlyout!).Items.OfType<MenuItem>().Select(i => i.Header).ToList();
            Assert.Equal(new object[] { "Cut", "Copier", "Coller" }, heads);
            Assert.Equal(("Suivant", "Previous"), (wizard.NextText, wizard.PreviousText));
        });
    }

    [AvaloniaFact]
    public void A_value_the_app_gave_is_kept()
    {
        With("en", null, () =>
        {
            var own = new Wizard { NextText = "Go on" };
            var styled = new Wizard { Classes = { "mine" } };
            var host = new StackPanel { Children = { own, styled } };
            host.Styles.Add(new Style(x => x.OfType<Wizard>().Class("mine")) { Setters = { new Setter(Wizard.NextTextProperty, "Onward") } });
            Show(host);
            InsiyabTheme.SetLanguage("ar");
            Assert.Equal("Go on", own.NextText);
            Assert.Equal("Onward", styled.NextText);
            Assert.Equal("السابق", own.PreviousText);
        });
    }

    [AvaloniaFact]
    public void A_language_switch_rewrites_the_controls_already_shown()
    {
        var before = TimelineItem.Now;
        TimelineItem.Now = () => new DateTime(2026, 9, 25, 9, 0, 0);
        try
        {
            var wizard = new Wizard();
            var find = new FindBar { IsOpen = true };
            var item = new TimelineItem { Header = "حدث", TimeMode = TimeMode.Relative, At = new DateTime(2026, 9, 25, 8, 55, 0) };
            var timeline = new Timeline { Items = { item } };
            var bell = new Notify();
            Show(new StackPanel { Children = { wizard, find, timeline, bell } }, FlowDirection.RightToLeft);
            Assert.Equal("قبل 5 دقائق", item.TimeText);
            With("en", null, () =>
            {
                Assert.Equal("Next", wizard.NextText);
                Assert.Equal("Notifications", bell.Title);
                Assert.Equal("5 minutes ago", item.TimeText);
                var field = find.GetVisualDescendants().OfType<TextBox>().First();
                Assert.Equal("Find in page", field.PlaceholderText);
            });
            Assert.Equal("التالي", wizard.NextText);
            Assert.Equal("الإشعارات", bell.Title);
            Assert.Equal("قبل 5 دقائق", item.TimeText);
            Assert.Equal("ابحث في الصفحة", find.GetVisualDescendants().OfType<TextBox>().First().PlaceholderText);
        }
        finally
        {
            TimelineItem.Now = before;
        }
    }

    [AvaloniaFact]
    public void Relative_times_and_dates_take_the_app_words()
    {
        var before = TimelineItem.Now;
        TimelineItem.Now = () => new DateTime(2026, 9, 25, 9, 0, 0);
        var words = new Dictionary<string, string>
        {
            ["time.ago"] = "il y a {0}", ["time.minutes"] = "{0} minutes", ["time.yesterday"] = "hier",
            ["date.month.9"] = "septembre",
        };
        try
        {
            With("fr", words, () =>
            {
                var five = new TimelineItem { Header = "a", TimeMode = TimeMode.Relative, At = new DateTime(2026, 9, 25, 8, 55, 0) };
                var day = new TimelineItem { Header = "b", TimeMode = TimeMode.Relative, At = new DateTime(2026, 9, 24, 7, 0, 0) };
                var date = new TimelineItem { Header = "c", TimeMode = TimeMode.Date, At = new DateTime(2026, 9, 2, 7, 0, 0) };
                Show(new Timeline { Items = { five, day, date } }, FlowDirection.RightToLeft);
                Assert.Equal("il y a 5 minutes", five.TimeText);
                Assert.Equal("hier", day.TimeText);
                Assert.Equal("septembre 2", date.TimeText);
            });
        }
        finally
        {
            TimelineItem.Now = before;
        }
    }

    [AvaloniaFact]
    public void Pickers_and_calendars_speak_the_app_language()
    {
        With("en", new Dictionary<string, string> { ["date.today"] = "Now" }, () =>
        {
            var month = new MonthCalendar { Date = new DateTime(2026, 9, 25), Width = 600, Height = 500 };
            var field = new DateField();
            Show(new StackPanel { Children = { field, month } }, FlowDirection.RightToLeft, width: 700, height: 700);
            var buttons = month.GetVisualDescendants().OfType<Button>().Select(b => b.Content).OfType<string>().ToList();
            Assert.Contains("Now", buttons);
            Assert.Contains(month.GetVisualDescendants().OfType<TextBlock>(), t => t.Text == "September 2026");
            Assert.Equal("Choose a date", AutomationProperties.GetName(field.GetVisualDescendants().OfType<Button>().First()));
        });
    }

    [AvaloniaFact]
    public void A_control_with_its_own_locale_keeps_the_library_words()
    {
        With("en", new Dictionary<string, string> { ["file.dragOne"] = "Drop it here or" }, () =>
        {
            var own = new FileUpload { Locale = "ar" };
            var app = new FileUpload();
            Show(new StackPanel { Children = { own, app } }, FlowDirection.LeftToRight);
            Assert.Equal("اسحب الملف إلى هنا أو", own.DragText);
            Assert.Equal("Drop it here or", app.DragText);
        });
    }

    [AvaloniaFact]
    public void Counted_words_take_the_app_form_for_one()
    {
        var words = new Dictionary<string, string> { ["check.minLength"] = "Au moins {0} caractères.", ["check.minLength.one"] = "Au moins {0} caractère." };
        With("fr", words, () =>
        {
            var box = new TextBox { Text = "a" };
            Validate.SetMinLength(box, 3);
            Assert.Equal("Au moins 3 caractères.", Validate.Problem(box));
            Assert.Equal("Au moins 1 caractère.", Words.Fixed.Count("check.minLength", 1));
        });
        With("en", null, () =>
        {
            var box = new TextBox { Text = "a" };
            Validate.SetMinLength(box, 3);
            Assert.Equal("Enter at least 3 characters.", Validate.Problem(box));
        });
    }

    [AvaloniaFact]
    public void The_toast_close_button_speaks_the_language()
    {
        Assert.Equal("إغلاق", Res(Show(new Border()), "InsWordToastClose"));
        With("en", null, () => Assert.Equal("Close", Res(Show(new Border()), "InsWordToastClose")));
    }

    [AvaloniaFact]
    public void Every_word_key_is_listed_in_the_readme()
    {
        var readme = File.ReadAllText(ReadmePath());
        var missing = Words.Keys.Where(k => !readme.Contains("`" + k + "`", StringComparison.Ordinal)).ToList();
        Assert.True(missing.Count == 0, "missing from the README: " + string.Join(", ", missing));
    }
}
