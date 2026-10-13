using System.Diagnostics;
using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Insiyab.Avalonia.Controls;
using static Insiyab.Avalonia.Tests.Ui;

namespace Insiyab.Avalonia.Tests;

public class PluginsTests
{
    private sealed class Recorder : ICommand
    {
        public List<object?> Got { get; } = [];
        public event EventHandler? CanExecuteChanged { add { } remove { } }
        public bool CanExecute(object? parameter) => true;
        public void Execute(object? parameter) => Got.Add(parameter);
    }

    private static StackPanel Page(params Control[] children)
    {
        var page = new StackPanel { Margin = new Thickness(24), Spacing = 16, Width = 420, HorizontalAlignment = HorizontalAlignment.Left };
        page.Children.AddRange(children);
        return page;
    }

    private static void Type(Window window, string text)
    {
        window.KeyTextInput(text);
        Settle(150);
    }

    private static void Press(Window window, Key key, RawInputModifiers modifiers = RawInputModifiers.None)
    {
        window.KeyPress(key, modifiers, PhysicalKey.None, null);
        window.KeyRelease(key, modifiers, PhysicalKey.None, null);
        Settle(250);
    }

    // OTP

    [AvaloniaFact]
    public void Otp_keeps_only_the_code()
    {
        var otp = new OtpBox();
        var completed = 0;
        otp.Completed += (_, _) => completed++;
        Show(Page(otp), FlowDirection.RightToLeft);
        otp.SetCode("رمزك هو ١٢٣ ٤٥٦");
        Assert.Equal("123456", otp.Text);
        Assert.True(otp.IsComplete);
        Assert.Equal(0, completed);
    }

    [AvaloniaFact]
    public void Otp_typing_completes_once_and_submits()
    {
        var command = new Recorder();
        var otp = new OtpBox { SubmitCommand = command };
        var done = new List<string>();
        otp.Completed += (_, e) => done.Add(e.Value);
        var window = Show(Page(otp), FlowDirection.RightToLeft);
        otp.Focus();
        Settle(150);
        foreach (var d in "123456") Type(window, d.ToString());
        Assert.Equal(["123456"], done);
        Assert.Equal(["123456"], command.Got);
        Type(window, "7");
        Assert.Equal("123456", otp.Text);
        Assert.Single(done);
    }

    [AvaloniaFact]
    public void Otp_paste_of_a_whole_code_replaces_a_partial_one()
    {
        var otp = new OtpBox();
        var done = new List<string>();
        otp.Completed += (_, e) => done.Add(e.Value);
        Show(Page(otp), FlowDirection.RightToLeft);
        otp.SetCode("48");
        otp.PasteText("الرمز 123456");
        Assert.Equal("123456", otp.Text);
        Assert.Equal(["123456"], done);
    }

    [AvaloniaFact]
    public void Otp_short_paste_is_added_at_the_end()
    {
        var otp = new OtpBox();
        Show(Page(otp), FlowDirection.RightToLeft);
        otp.SetCode("48");
        otp.PasteText("2");
        Assert.Equal("482", otp.Text);
    }

    // Like a field in error: the normal fill, the border at 55% of the bad colour, and the full colour with the red glow on focus.
    [AvaloniaTheory]
    [InlineData("Light")]
    [InlineData("Dark")]
    public void Otp_invalid_is_red_until_typed_again(string theme)
    {
        var otp = new OtpBox();
        var calm = new OtpBox();
        var window = Show(Page(otp, calm), FlowDirection.RightToLeft, theme == "Dark" ? ThemeVariant.Dark : ThemeVariant.Light);
        otp.IsInvalid = true;
        Settle();
        Assert.Contains(":invalid", otp.Classes);
        var cells = Part<Border>(otp, "PART_Cells");
        var bad = (Color)Res(otp, "InsToneBadColor");
        var wait = Stopwatch.StartNew();
        while (wait.ElapsedMilliseconds < 2000 && !(cells.BorderBrush is ISolidColorBrush b && b.Color == bad && Math.Abs(b.Opacity - 0.55) < 0.01)) Settle(16);
        Assert.Equal(bad, ColorOf(cells.BorderBrush));
        Assert.Equal(0.55, ((ISolidColorBrush)cells.BorderBrush!).Opacity, 2);
        AssertBrush(otp, "InsCtlBg", () => cells.Background);
        Assert.Equal(ColorOf(Part<Border>(calm, "PART_Cells").Background), ColorOf(cells.Background));

        otp.Focus();
        Settle(150);
        AssertBrush(otp, "InsToneBad", () => cells.BorderBrush);
        AssertBrush(otp, "InsGlassStrong", () => cells.Background);
        var glow = (BoxShadows)Res(otp, "InsGlowBad");
        var shadow = Part<Border>(otp, "PART_Shadow");
        var clock = Stopwatch.StartNew();
        while (clock.ElapsedMilliseconds < 2000 && !shadow.BoxShadow.Equals(glow)) Settle(16);
        Assert.Equal(glow, shadow.BoxShadow);
        Type(window, "5");
        Assert.False(otp.IsInvalid);
    }

    [AvaloniaFact]
    public void Otp_partial_code_message()
    {
        var otp = new OtpBox();
        Show(Page(otp), FlowDirection.RightToLeft);
        otp.Text = "12";
        Assert.Equal("اكتب الرمز كاملًا: 6 أرقام.", otp.ValidationMessage);
        otp.Locale = "en";
        Assert.Equal("Enter all 6 digits.", otp.ValidationMessage);
        otp.Text = "";
        Assert.Null(otp.ValidationMessage);
    }

    [AvaloniaFact]
    public void Otp_alphanumeric_is_upper_case()
    {
        var otp = new OtpBox { Alphanumeric = true };
        Show(Page(otp), FlowDirection.RightToLeft);
        otp.SetCode("a7k");
        Assert.Equal("A7K", otp.Text);
    }

    [AvaloniaFact]
    public void Otp_focus_glow_is_not_clipped()
    {
        var otp = new OtpBox();
        Show(Page(otp), FlowDirection.RightToLeft);
        Assert.False(otp.ClipToBounds);
        otp.Focus();
        Settle();
        Assert.True(Part<Border>(otp, "PART_Halo").Opacity > 0.98);
    }

    // Phone

    private static TextBox InputOf(PhoneBox phone) => Part<TextBox>(phone, "PART_Input");

    private static Popup PopupOf(PhoneBox phone) => Part<Popup>(phone, "PART_Popup");

    private static T InPopup<T>(PhoneBox phone, string name) where T : Control => Part<T>((Visual)PopupOf(phone).Child!, name);

    [AvaloniaFact]
    public void Phone_loads_E164_and_shows_it_local()
    {
        var phone = new PhoneBox { Value = "+966501234567" };
        Show(Page(phone), FlowDirection.RightToLeft);
        Assert.Equal("050 123 4567", phone.DisplayText);
        Assert.Equal("SA", phone.Country);
        Assert.True(phone.IsValid);
        var input = InputOf(phone);
        Assert.Contains("input-group", input.Classes);
        Assert.Equal(FlowDirection.LeftToRight, input.FlowDirection);
        Assert.Equal("050 123 4567", input.Text);
    }

    [AvaloniaTheory]
    [InlineData("+97142345678", "AE", "+97142345678")]
    [InlineData("+18765550123", "JM", "+18765550123")]
    [InlineData("+77012345678", "KZ", "+77012345678")]
    public void Phone_plus_number_picks_its_country(string typed, string country, string value)
    {
        var phone = new PhoneBox();
        Show(Page(phone), FlowDirection.RightToLeft);
        var input = InputOf(phone);
        input.Focus();
        Settle(150);
        input.Text = typed;
        Settle(150);
        Assert.Equal(country, phone.Country);
        Assert.Equal(value, phone.Value);
    }

    [AvaloniaFact]
    public void Phone_reads_Arabic_digits()
    {
        var phone = new PhoneBox();
        Show(Page(phone), FlowDirection.RightToLeft);
        var input = InputOf(phone);
        input.Focus();
        Settle(150);
        input.Text = "٠٥٠١٢٣٤٥٦٧";
        Settle(150);
        Assert.Equal("SA", phone.Country);
        Assert.Equal("+966501234567", phone.Value);
        Assert.Equal("050 123 4567", input.Text);
    }

    [AvaloniaFact]
    public void Phone_wrong_length_shows_the_error_after_leaving()
    {
        var phone = new PhoneBox();
        var other = new TextBox();
        Show(Page(phone, other), FlowDirection.RightToLeft);
        var input = InputOf(phone);
        input.Focus();
        Settle(150);
        input.Text = "05012";
        Settle(150);
        other.Focus();
        Settle();
        Assert.Equal("اكتب رقم هاتف صحيحًا في المملكة العربية السعودية.", phone.ValidationMessage);
        Assert.True(DataValidationErrors.GetHasErrors(input));
        input.Focus();
        Settle(150);
        input.Text = "0501234567";
        Settle(150);
        Assert.Null(phone.ValidationMessage);
        Assert.False(DataValidationErrors.GetHasErrors(input));
    }

    [AvaloniaFact]
    public void Phone_only_refuses_numbers_from_elsewhere()
    {
        var phone = new PhoneBox { OnlyCountries = "SA,AE" };
        Show(Page(phone), FlowDirection.RightToLeft);
        var input = InputOf(phone);
        input.Focus();
        Settle(150);
        input.Text = "+201001234567";
        Settle(150);
        Assert.Equal("", phone.Value);
        Assert.Equal("لا تُقبل هنا أرقام مصر.", phone.ValidationMessage);
        Assert.Equal(2, phone.AllowedCountries.Count);
    }

    [AvaloniaFact]
    public void Phone_list_opens_fast_and_searches()
    {
        var phone = new PhoneBox();
        Show(Page(phone), FlowDirection.RightToLeft);
        var clock = Stopwatch.StartNew();
        phone.IsDropDownOpen = true;
        Dispatcher.UIThread.RunJobs();
        clock.Stop();
        Assert.True(clock.ElapsedMilliseconds < 300, $"opening took {clock.ElapsedMilliseconds} ms");
        Settle(250);
        Assert.Equal(PhoneData.Countries.Count, phone.VisibleRows.Count);
        var search = InPopup<TextBox>(phone, "PART_Search");
        search.Text = "امريكا";
        Settle(150);
        Assert.Equal("US", phone.VisibleRows[0].Iso);
        search.Text = "UAE";
        Settle(150);
        Assert.Equal("AE", phone.VisibleRows[0].Iso);
        search.Text = "+97";
        Settle(150);
        Assert.NotEmpty(phone.VisibleRows);
        Assert.All(phone.VisibleRows, r => Assert.StartsWith("+97", r.Dial));
        search.Text = "zzzz";
        Settle(150);
        Assert.Empty(phone.VisibleRows);
        Assert.True(InPopup<TextBlock>(phone, "PART_None").IsVisible);
    }

    [AvaloniaFact]
    public void Phone_list_bar_sits_at_the_card_edge()
    {
        var phone = new PhoneBox();
        Show(Page(phone), FlowDirection.RightToLeft, height: 700);
        phone.IsDropDownOpen = true;
        Settle(250);
        var card = Part<Border>((Visual)PopupOf(phone).Child!, "LayoutRoot");
        var list = InPopup<ListBox>(phone, "PART_List");
        var bar = list.GetVisualDescendants().OfType<ScrollBar>().Single(b => b.Orientation == Orientation.Vertical);
        Assert.True(bar.IsEffectivelyVisible);
        var r = new Rect(bar.Bounds.Size).TransformToAABB(bar.TransformToVisual(card)!.Value);
        Assert.Equal(card.Bounds.Width - card.BorderThickness.Right, r.Right, 0.5);
        Assert.Equal(card.Bounds.Height - card.BorderThickness.Bottom, r.Bottom, 0.5);
        var row = list.GetVisualDescendants().OfType<ListBoxItem>().First(i => !i.IsSelected && !i.IsPointerOver);
        Assert.Equal(17, row.TranslatePoint(new Point(0, 0), card)!.Value.X, 0.5);
    }

    [AvaloniaFact]
    public void Phone_preferred_countries_lead_the_list()
    {
        var phone = new PhoneBox { PreferredCountries = "EG,JO" };
        Show(Page(phone), FlowDirection.RightToLeft);
        phone.IsDropDownOpen = true;
        Settle(250);
        Assert.Equal("EG", phone.VisibleRows[0].Iso);
        Assert.Equal("JO", phone.VisibleRows[1].Iso);
        Assert.True(phone.VisibleRows[1].EndsPreferred);
        Assert.False(phone.VisibleRows[0].EndsPreferred);
        Assert.Equal(FontWeight.Bold, phone.VisibleRows.First(r => r.Iso == "SA").Weight);
        Assert.Equal(FontWeight.Normal, phone.VisibleRows[0].Weight);
    }

    [AvaloniaFact]
    public void Phone_keyboard_picks_a_country()
    {
        var phone = new PhoneBox();
        var window = Show(Page(phone), FlowDirection.RightToLeft);
        var button = Part<Button>(phone, "PART_Country");
        button.Focus();
        Settle(150);
        Press(window, Key.Down);
        Assert.True(phone.IsDropDownOpen);
        Assert.True(InPopup<TextBox>(phone, "PART_Search").IsFocused);
        Type(window, "kuw");
        Press(window, Key.Enter);
        Assert.Equal("KW", phone.Country);
        Assert.False(phone.IsDropDownOpen);
        Assert.True(InputOf(phone).IsFocused);
    }

    [AvaloniaFact]
    public void Phone_typing_on_the_button_starts_a_search()
    {
        var phone = new PhoneBox();
        var window = Show(Page(phone), FlowDirection.RightToLeft);
        Part<Button>(phone, "PART_Country").Focus();
        Settle(150);
        Type(window, "م");
        Settle(250);
        Assert.True(phone.IsDropDownOpen);
        Assert.Equal("م", InPopup<TextBox>(phone, "PART_Search").Text);
    }

    [AvaloniaFact]
    public void Phone_second_click_closes()
    {
        var phone = new PhoneBox();
        var window = Show(Page(phone), FlowDirection.RightToLeft);
        var button = Part<Button>(phone, "PART_Country");
        Click(button, window);
        Assert.True(phone.IsDropDownOpen);
        Click(button, window);
        Assert.False(phone.IsDropDownOpen);
        Settle();
        Assert.False(phone.IsDropDownOpen);
    }

    [AvaloniaFact]
    public void Phone_list_is_a_card_in_the_overlay()
    {
        var phone = new PhoneBox();
        Show(Page(phone), FlowDirection.RightToLeft);
        phone.IsDropDownOpen = true;
        Settle(250);
        var popup = PopupOf(phone);
        Assert.True(popup.ShouldUseOverlayLayer);
        var presenter = Assert.IsType<FlyoutPresenter>(popup.Child);
        Assert.Contains("pop", presenter.Classes);
        Assert.Contains("card", presenter.Classes);
    }

    [AvaloniaFact]
    public void Phone_raises_PhoneChanged()
    {
        var phone = new PhoneBox();
        var seen = new List<PhoneChangedEventArgs>();
        phone.PhoneChanged += (_, e) => seen.Add(e);
        Show(Page(phone), FlowDirection.RightToLeft);
        var input = InputOf(phone);
        input.Focus();
        Settle(150);
        input.Text = "0501234567";
        Settle(150);
        var e = Assert.Single(seen);
        Assert.Equal("+966501234567", e.Value);
        Assert.Equal("SA", e.Country);
        Assert.True(e.IsValid);
    }

    [AvaloniaFact]
    public void Phone_locale_region_starts_the_country()
    {
        var phone = new PhoneBox { Locale = "ar-EG" };
        Show(Page(phone), FlowDirection.RightToLeft);
        Assert.Equal("EG", phone.Country);
    }

    [AvaloniaFact]
    public void Phone_escape_closes_and_returns_to_the_button()
    {
        var phone = new PhoneBox();
        var window = Show(Page(phone), FlowDirection.RightToLeft);
        var button = Part<Button>(phone, "PART_Country");
        button.Focus();
        Settle(150);
        Press(window, Key.Down);
        Assert.True(phone.IsDropDownOpen);
        Press(window, Key.Escape);
        Assert.False(phone.IsDropDownOpen);
        Assert.True(button.IsFocused);
    }

    // File

    private static FileEntry Entry(string name, long size = 1000) => new(name, size);

    private static FileUpload Uploader(Action<FileUpload>? set = null)
    {
        var upload = new FileUpload { Multiple = true, Accept = ".pdf,image/*", MaxSize = "5MB", MaxCount = 5 };
        set?.Invoke(upload);
        return upload;
    }

    private static FileItem RowOf(FileUpload upload, FileEntry entry) =>
        upload.GetVisualDescendants().OfType<FileItem>().First(r => r.Entry == entry);

    // In error the zone keeps the light red wash in light; dark takes 8% of its own light red (#FCA5A5) instead of the maroon slab, and both keep the dashed red rim.
    [AvaloniaTheory]
    [InlineData("Light", "#1AEF4444")]
    [InlineData("Dark", "#14FCA5A5")]
    public void File_zone_in_error_keeps_a_clean_wash(string theme, string fill)
    {
        var upload = Uploader(u => u.Upload = true);
        var window = Show(Page(new TextBlock { Classes = { "label" }, Text = "المرفقات" }, upload), FlowDirection.RightToLeft, theme == "Dark" ? ThemeVariant.Dark : ThemeVariant.Light, 480, 240);
        var one = Entry("a.pdf");
        upload.AddFiles([one]);
        upload.Fail(one, null);
        Settle();
        Assert.Contains(":invalid", upload.Classes);
        var zone = Part<Border>(upload, "PART_Zone");
        var want = Color.Parse(fill);
        var clock = Stopwatch.StartNew();
        while (clock.ElapsedMilliseconds < 2000 && !(zone.Background is ISolidColorBrush b && b.Color == want)) Settle(16);
        Assert.Equal(want, ColorOf(zone.Background));
        AssertBrush(upload, "InsToneBadBd", () => Part<global::Avalonia.Controls.Shapes.Rectangle>(upload, "PART_Rim").Stroke);
        Capture(window, $"file-error-{theme.ToLowerInvariant()}");
    }

    [AvaloniaTheory]
    [InlineData("Light", "#1AEF4444")]
    [InlineData("Dark", "#14FCA5A5")]

    [AvaloniaFact]
    public void File_limits_report_what_was_left_out()
    {
        var upload = Uploader();
        Show(Page(upload), FlowDirection.RightToLeft);
        upload.AddFiles([Entry("تقرير.pdf", 245000), Entry("كبير.pdf", 9000000), Entry("ملاحظات.txt", 1200)]);
        Assert.Single(upload.Files);
        Assert.Equal(["«كبير.pdf» أكبر من 5 م.ب.", "نوع «ملاحظات.txt» غير مقبول."], upload.Issues);
    }

    [AvaloniaFact]
    public void File_count_limit()
    {
        var upload = new FileUpload { Multiple = true, MaxCount = 2 };
        Show(Page(upload), FlowDirection.RightToLeft);
        upload.AddFiles([Entry("a.pdf"), Entry("b.pdf", 2000), Entry("c.pdf", 3000)]);
        Assert.Equal(2, upload.Files.Count);
        Assert.Equal(["لم يُضف «c.pdf»: ملفان على الأكثر."], upload.Issues);
    }

    [AvaloniaFact]
    public void File_single_file_replaces()
    {
        var upload = new FileUpload { Multiple = false };
        Show(Page(upload), FlowDirection.RightToLeft);
        upload.AddFiles([Entry("a.pdf")]);
        upload.AddFiles([Entry("b.pdf", 2000)]);
        Assert.Equal(["b.pdf"], upload.Files.Select(f => f.Name));
    }

    [AvaloniaFact]
    public void File_note_reads_the_limits()
    {
        var upload = Uploader();
        Show(Page(upload), FlowDirection.RightToLeft);
        Assert.Equal("حتى 5 م.ب للملف الواحد، 5 ملفات على الأكثر", upload.NoteText);
        upload.Note = "x";
        Assert.Equal("x", upload.NoteText);
        var english = Uploader();
        Show(Page(english), FlowDirection.LeftToRight);
        Assert.Equal("up to 5 MB each, 5 files at most", english.NoteText);
    }

    [AvaloniaFact]
    public void File_upload_reports_progress_done_and_failure()
    {
        var upload = Uploader(u => u.Upload = true);
        var added = new List<FileAddedEventArgs>();
        upload.FileAdded += (_, e) => added.Add(e);
        Show(Page(upload), FlowDirection.RightToLeft);
        var one = Entry("a.pdf");
        upload.AddFiles([one]);
        Settle(250);
        Assert.True(Assert.Single(added).Upload);
        Assert.Equal("انتظر حتى يكتمل رفع الملفات.", upload.ValidationMessage);
        var bar = Part<ProgressBar>(RowOf(upload, one), "PART_Progress");
        Assert.True(bar.IsVisible);
        Assert.True(bar.IsIndeterminate);
        Assert.True(upload.Progress(one, 0.4));
        Settle(150);
        Assert.Equal(0.4, bar.Value, 3);
        Assert.False(bar.IsIndeterminate);
        Assert.True(upload.Done(one, "id-1"));
        Settle(150);
        Assert.False(bar.IsVisible);
        Assert.Null(upload.ValidationMessage);
        var two = Entry("b.pdf", 2000);
        upload.AddFiles([two]);
        Assert.True(upload.Fail(two, null));
        Settle(250);
        var row = RowOf(upload, two);
        Assert.Contains(":failed", row.Classes);
        Assert.Equal("تعذر الرفع.", row.StatusText);
        Assert.Contains(":invalid", upload.Classes);
        Assert.Equal("أزِل الملفات التي تعذر رفعها أو أعد رفعها.", upload.ValidationMessage);
        upload.Remove(two);
        Assert.False(upload.Progress(two, 0.5));
    }

    [AvaloniaFact]
    public void File_remove_uses_the_close_button()
    {
        var upload = Uploader();
        var changed = 0;
        upload.FilesChanged += (_, _) => changed++;
        var window = Show(Page(upload), FlowDirection.RightToLeft);
        var one = Entry("a.pdf");
        upload.AddFiles([one]);
        Settle(250);
        changed = 0;
        var remove = Part<Button>(RowOf(upload, one), "PART_Remove");
        Assert.Contains("close", remove.Classes);
        Assert.Contains("sm", remove.Classes);
        Click(remove, window);
        Assert.Empty(upload.Files);
        Assert.Equal(1, changed);
        Assert.True(upload.IsFocused);
    }

    [AvaloniaFact]
    public void File_compact_is_a_button_and_the_list()
    {
        var upload = Uploader();
        upload.Classes.Add("compact");
        Show(Page(upload), FlowDirection.RightToLeft);
        Assert.False(Part<WrapPanel>(upload, "PART_Lead").IsVisible);
        Assert.False(Part<TextBlock>(upload, "PART_Note").IsVisible);
        Assert.True(Part<TextBlock>(upload, "PART_CompactPick").IsVisible);
    }

    [AvaloniaFact]
    public void File_disabled_dims_the_zone()
    {
        var upload = Uploader();
        upload.IsEnabled = false;
        Show(Page(upload), FlowDirection.RightToLeft);
        Settle();
        Assert.Equal(0.55, Part<Panel>(upload, "PART_ZoneBox").Opacity, 2);
    }

    // Colour

    private static ColorField OpenColor(Action<ColorField>? set = null)
    {
        var field = new ColorField { Value = Color.Parse("#0f766e") };
        set?.Invoke(field);
        return field;
    }

    private static ColorPickerPanel PickerOf(ColorField field) => InPopup<ColorPickerPanel>(field, "PART_Picker");

    private static T InPopup<T>(ColorField field, string name) where T : Control => Part<T>((Visual)Part<Popup>(field, "PART_Popup").Child!, name);

    [AvaloniaFact]
    public void Color_typed_hex_commits_once()
    {
        var field = new ColorField();
        var other = new TextBox();
        var seen = new List<ColorCommittedEventArgs>();
        field.ColorCommitted += (_, e) => seen.Add(e);
        var window = Show(Page(field, other), FlowDirection.RightToLeft);
        var input = Part<TextBox>(field, "PART_Input");
        input.Focus();
        Settle(150);
        Type(window, "abc");
        Press(window, Key.Enter);
        Assert.Equal(Color.Parse("#aabbcc"), field.Value);
        Assert.Equal("#aabbcc", field.Hex);
        Assert.Equal("#AABBCC", input.Text);
        Assert.Single(seen);
        other.Focus();
        Settle(150);
        Assert.Single(seen);
    }

    [AvaloniaFact]
    public void Color_bad_hex_shows_the_error()
    {
        var field = new ColorField();
        var other = new TextBox();
        var window = Show(Page(field, other), FlowDirection.RightToLeft);
        var input = Part<TextBox>(field, "PART_Input");
        input.Focus();
        Settle(150);
        Type(window, "xyz");
        other.Focus();
        Settle();
        Assert.Equal("اكتب لونًا مثل #4C1D95.", field.ValidationMessage);
        Assert.True(DataValidationErrors.GetHasErrors(input));
    }

    [AvaloniaFact]
    public void Color_picker_is_a_272_card_in_the_overlay()
    {
        var field = OpenColor();
        Show(Page(field), FlowDirection.RightToLeft);
        field.IsDropDownOpen = true;
        Settle(300);
        var popup = Part<Popup>(field, "PART_Popup");
        Assert.True(popup.ShouldUseOverlayLayer);
        var presenter = Assert.IsType<FlyoutPresenter>(popup.Child);
        Assert.Contains("pop", presenter.Classes);
        Assert.Contains("card", presenter.Classes);
        Assert.Contains("color", presenter.Classes);
        Assert.Equal(272, Part<Border>(presenter, "LayoutRoot").Bounds.Width, 0.5);
    }

    [AvaloniaFact]
    public void Color_square_moves_with_the_keyboard()
    {
        var field = OpenColor();
        var committed = 0;
        field.ColorCommitted += (_, _) => committed++;
        var window = Show(Page(field), FlowDirection.RightToLeft);
        var swatch = Part<Button>(field, "PART_Swatch");
        swatch.Focus();
        Settle(150);
        Press(window, Key.Enter);
        Settle(300);
        Assert.True(field.IsDropDownOpen);
        var area = InPopup<ColorArea>(field, "PART_Area");
        var picker = PickerOf(field);
        Assert.True(area.IsFocused);
        Assert.Contains(":focus-visible", area.Classes);
        var s = picker.Saturation;
        var v = picker.Brightness;
        Press(window, Key.Right);
        Assert.Equal(Math.Min(1, s + 0.01), picker.Saturation, 4);
        Press(window, Key.Up, RawInputModifiers.Shift);
        Assert.Equal(Math.Min(1, v + 0.1), picker.Brightness, 4);
        Assert.True(committed >= 1);
        Assert.Equal(ColorMath.FromHsv(picker.Hue, picker.Saturation, picker.Brightness), field.Value);
    }

    [AvaloniaFact]
    public void Color_hue_takes_a_press_on_its_whole_height()
    {
        var field = OpenColor();
        Show(Page(field), FlowDirection.RightToLeft);
        field.IsDropDownOpen = true;
        Settle(300);
        var hue = ((Visual)Part<Popup>(field, "PART_Popup").Child!).GetVisualDescendants().OfType<ColorHueSlider>().Single();
        var top = TopLevel.GetTopLevel(hue)!;
        var edge = hue.TranslatePoint(new Point(hue.Bounds.Width * 0.75, 0.5), top)!.Value;
        Assert.Same(hue, top.InputHitTest(edge));
        var before = hue.Hue;
        top.MouseDown(edge, MouseButton.Left);
        top.MouseUp(edge, MouseButton.Left);
        Settle(150);
        Assert.NotEqual(before, hue.Hue);
    }

    [AvaloniaFact]
    public void Color_picker_opened_by_a_click_has_no_focus_ring()
    {
        var field = OpenColor();
        var window = Show(Page(field), FlowDirection.RightToLeft);
        Click(Part<Button>(field, "PART_Swatch"), window);
        Settle(300);
        var area = InPopup<ColorArea>(field, "PART_Area");
        Assert.True(area.IsFocused);
        Assert.DoesNotContain(":focus-visible", area.Classes);
    }

    [AvaloniaFact]
    public void Color_presets_follow_swatches()
    {
        var field = OpenColor();
        var window = Show(Page(field), FlowDirection.RightToLeft);
        field.IsDropDownOpen = true;
        Settle(300);
        var presets = InPopup<Panel>(field, "PART_Swatches").Children.OfType<ColorPreset>().ToList();
        Assert.Equal(10, presets.Count);
        var primary = InsiyabTheme.Primary;
        var first = ColorOf(presets[0].Brush);
        Assert.Equal((primary.R, primary.G, primary.B), (first.R, first.G, first.B));

        var none = OpenColor(f => f.Swatches = "none");
        Show(Page(none), FlowDirection.RightToLeft);
        none.IsDropDownOpen = true;
        Settle(300);
        Assert.False(InPopup<Panel>(none, "PART_Swatches").IsVisible);

        var three = OpenColor(f => f.Swatches = "#15803d,#a16207,#b91c1c");
        var second = Show(Page(three), FlowDirection.RightToLeft);
        three.IsDropDownOpen = true;
        Settle(300);
        var list = InPopup<Panel>(three, "PART_Swatches").Children.OfType<ColorPreset>().ToList();
        Assert.Equal(["#15803d", "#a16207", "#b91c1c"], list.Select(p => ColorMath.Hex(ColorOf(p.Brush))));
        Click(list[1], second);
        Assert.Equal(Color.Parse("#a16207"), three.Value);
    }

    [AvaloniaFact]
    public void Color_contrast_readout()
    {
        var field = OpenColor(f => f.Value = Color.Parse("#4c1d95"));
        Show(Page(field), FlowDirection.RightToLeft);
        field.IsDropDownOpen = true;
        Settle(300);
        var white = InPopup<ColorContrastLine>(field, "PART_OnWhite");
        var black = InPopup<ColorContrastLine>(field, "PART_OnBlack");
        Assert.Equal("11.0:1", white.Ratio);
        Assert.Contains(":pass", white.Classes);
        Assert.Equal("1.9:1", black.Ratio);
        Assert.Contains(":fail", black.Classes);
    }

    [AvaloniaFact]
    public void Color_escape_closes_and_returns_to_the_swatch()
    {
        var field = OpenColor();
        var window = Show(Page(field), FlowDirection.RightToLeft);
        var swatch = Part<Button>(field, "PART_Swatch");
        Click(swatch, window);
        Settle(300);
        Assert.True(field.IsDropDownOpen);
        Press(window, Key.Escape);
        Assert.False(field.IsDropDownOpen);
        Assert.True(swatch.IsFocused);
    }

    [AvaloniaFact]
    public void Color_second_click_closes()
    {
        var field = OpenColor();
        var window = Show(Page(field), FlowDirection.RightToLeft);
        var swatch = Part<Button>(field, "PART_Swatch");
        Click(swatch, window);
        Assert.True(field.IsDropDownOpen);
        Click(swatch, window);
        Assert.False(field.IsDropDownOpen);
        Settle();
        Assert.False(field.IsDropDownOpen);
    }

    [AvaloniaFact]
    public void Color_empty_swatch_shows_the_slash()
    {
        var field = new ColorField();
        Show(Page(field), FlowDirection.RightToLeft);
        var swatch = Part<Button>(field, "PART_Swatch");
        Assert.Contains("empty", swatch.Classes);
        Assert.True(Part<Border>(swatch, "PART_Empty").IsVisible);
    }

    // Carousel

    private static Carousel Make(int n, string? cls = null)
    {
        var carousel = new Carousel();
        if (cls is not null) carousel.Classes.Add(cls);
        for (var i = 0; i < n; i++) carousel.Items.Add(new Border { Height = 240, Background = Brushes.Teal });
        return carousel;
    }

    private static Window ShowCarousel(Carousel carousel, FlowDirection direction = FlowDirection.RightToLeft, double width = 880)
    {
        var page = new StackPanel { Margin = new Thickness(24) };
        page.Children.Add(carousel);
        return Show(page, direction, null, width);
    }

    private static CarouselTrack TrackOf(Carousel carousel) => carousel.GetVisualDescendants().OfType<CarouselTrack>().First();

    [AvaloniaFact]
    public void Carousel_starts_with_next_and_dots()
    {
        var carousel = Make(4);
        ShowCarousel(carousel);
        Assert.Equal(4, CarouselOptions.GetStopCount(carousel));
        Assert.False(Part<Button>(carousel, "PART_Prev").IsEnabled);
        Assert.True(Part<Button>(carousel, "PART_Next").IsEnabled);
        var dots = carousel.GetVisualDescendants().OfType<Button>().Where(b => b.Classes.Contains("carousel-dot")).ToList();
        Assert.Equal(4, dots.Count);
        Assert.Contains("current", dots[0].Classes);
    }

    [AvaloniaTheory]

    [AvaloniaTheory]
    [InlineData(true)]
    [InlineData(false)]
    public void Carousel_arrows_follow_reading_direction(bool rtl)
    {
        var carousel = Make(4);
        var window = ShowCarousel(carousel, rtl ? FlowDirection.RightToLeft : FlowDirection.LeftToRight);
        carousel.Focus();
        Settle(150);
        Press(window, rtl ? Key.Left : Key.Right);
        Assert.Equal(1, CarouselOptions.GetStop(carousel));
        Press(window, Key.End);
        Assert.Equal(3, CarouselOptions.GetStop(carousel));
        Press(window, Key.Home);
        Assert.Equal(0, CarouselOptions.GetStop(carousel));
    }

    [AvaloniaFact]
    public void Carousel_up_and_down_do_not_change_slides()
    {
        var carousel = Make(4);
        var window = ShowCarousel(carousel);
        carousel.Focus();
        Settle(150);
        Press(window, Key.Down);
        Press(window, Key.Up);
        Assert.Equal(0, carousel.SelectedIndex);
        Assert.Equal(0, CarouselOptions.GetStop(carousel));
    }

    [AvaloniaFact]
    public void Carousel_wheel_moves_one_stop_per_gesture()
    {
        var carousel = Make(4);
        var window = ShowCarousel(carousel);
        var at = Center(carousel, window);
        // The gesture clock moves only where the test says, so a slow first step under load does not end the gesture.
        var real = CarouselNav.Now;
        var ms = 0L;
        CarouselNav.Now = () => ms;
        try
        {
            window.MouseWheel(at, new Vector(0, -1), RawInputModifiers.Shift);
            Assert.Equal(1, CarouselOptions.GetStop(carousel));
            ms += 100;
            window.MouseWheel(at, new Vector(0, -1), RawInputModifiers.Shift);
            Assert.Equal(1, CarouselOptions.GetStop(carousel));
            Settle(400);
            ms += 400;
            window.MouseWheel(at, new Vector(0, -1), RawInputModifiers.Shift);
            Settle(100);
            Assert.Equal(2, CarouselOptions.GetStop(carousel));
            Settle(400);
            window.MouseWheel(at, new Vector(0, -1), RawInputModifiers.None);
            Settle(400);
            Assert.Equal(2, CarouselOptions.GetStop(carousel));
            ms += 900;
            window.MouseWheel(at, new Vector(-1, 0), RawInputModifiers.None);
            Settle(100);
            Assert.Equal(3, CarouselOptions.GetStop(carousel));
        }
        finally { CarouselNav.Now = real; }
    }

    [AvaloniaFact]
    public void Carousel_wheel_works_over_the_gap_between_slides()
    {
        var carousel = Make(4, "two");
        var window = ShowCarousel(carousel);
        var slides = carousel.Items.OfType<Border>().ToArray();
        var a = Center(slides[0], window);
        var b = Center(slides[1], window);
        var gap = new Point((a.X + b.X) / 2, a.Y);
        Assert.Same(Part<Border>(carousel, "PART_Track"), window.InputHitTest(gap));
        window.MouseWheel(gap, new Vector(0, -1), RawInputModifiers.Shift);
        Settle(100);
        Assert.Equal(1, CarouselOptions.GetStop(carousel));
    }

    [AvaloniaFact]
    public void Carousel_two_and_three_follow_its_own_width()
    {
        var two = Make(4, "two");
        ShowCarousel(two);
        Assert.Equal(2, TrackOf(two).EffectivePerView);
        Assert.Equal(3, CarouselOptions.GetStopCount(two));

        var three = Make(4, "three");
        ShowCarousel(three);
        Assert.Equal(2, TrackOf(three).EffectivePerView);

        var wide = Make(4, "three");
        ShowCarousel(wide, FlowDirection.RightToLeft, 1048);
        Assert.Equal(3, TrackOf(wide).EffectivePerView);
        Assert.Equal(2, CarouselOptions.GetStopCount(wide));
    }

    [AvaloniaFact]
    public void Carousel_peek_shows_the_next_edge()
    {
        var carousel = Make(4, "peek");
        ShowCarousel(carousel);
        var track = TrackOf(carousel);
        foreach (var child in track.Children) Assert.InRange(Math.Abs(child.Bounds.Width - track.Bounds.Width * 0.86), 0, 1);
    }

    [AvaloniaFact]
    public void Carousel_single_slide_hides_its_controls()
    {
        var carousel = Make(1);
        ShowCarousel(carousel);
        Assert.False(Part<Button>(carousel, "PART_Prev").IsVisible);
        Assert.False(Part<Button>(carousel, "PART_Next").IsVisible);
        Assert.False(Part<Panel>(carousel, "PART_Dots").IsVisible);
    }

    [AvaloniaFact]
    public void Carousel_no_dots()
    {
        var carousel = Make(4, "no-dots");
        ShowCarousel(carousel);
        Assert.False(Part<Panel>(carousel, "PART_Dots").IsVisible);
        Assert.True(Part<Button>(carousel, "PART_Next").IsVisible);
        Assert.True(Part<Button>(carousel, "PART_Prev").IsVisible);
    }

    [AvaloniaFact]
    public void Carousel_swipe_target()
    {
        Assert.Equal(2, CarouselOptions.SwipeTarget(1, -200, 800, 4));
        Assert.Equal(0, CarouselOptions.SwipeTarget(1, 200, 800, 4));
        Assert.Equal(1, CarouselOptions.SwipeTarget(1, -50, 800, 4));
        Assert.Equal(3, CarouselOptions.SwipeTarget(3, -300, 800, 4));
    }

    [AvaloniaFact]
    public void Carousel_names_each_slide()
    {
        var carousel = Make(4);
        ShowCarousel(carousel);
        Assert.Equal("2 من 4", global::Avalonia.Automation.AutomationProperties.GetName((Control)carousel.ContainerFromIndex(1)!));
    }

    // Scrollspy

    private static readonly string[] Sections = ["القسم الأول", "القسم الثاني", "قسم فرعي", "القسم الثالث"];

    private sealed record TocExample(Grid Grid, ScrollViewer Scroller, Toc Toc, List<TocLink> Links);

    private static TocExample MakeToc(Action<Toc>? set = null)
    {
        var column = new StackPanel();
        var toc = new Toc { Title = "المحتويات" };
        var links = new List<TocLink>();
        for (var i = 0; i < Sections.Length; i++)
        {
            var section = new Border { MinHeight = 208, Child = new TextBlock { Text = Sections[i], FontSize = 16, FontWeight = FontWeight.Bold, Margin = new Thickness(0, 0, 0, 8) } };
            column.Children.Add(section);
            var link = new TocLink { Content = Sections[i], Section = section };
            if (i == 2) link.Classes.Add("sub");
            links.Add(link);
            toc.Items.Add(link);
        }
        set?.Invoke(toc);
        var scroller = new ScrollViewer { Height = 256, Content = column };
        var grid = new Grid { Margin = new Thickness(24), ColumnDefinitions = new ColumnDefinitions("*,20,176"), VerticalAlignment = VerticalAlignment.Top };
        grid.Children.Add(scroller);
        Grid.SetColumn(toc, 2);
        grid.Children.Add(toc);
        return new TocExample(grid, scroller, toc, links);
    }

    private static string? Current(Toc toc) => toc.ActiveLink?.Content as string;

    [AvaloniaFact]
    public void Toc_marks_the_section_past_a_third()
    {
        var x = MakeToc();
        Show(x.Grid, FlowDirection.RightToLeft);
        Assert.Equal("القسم الأول", Current(x.Toc));
        x.Scroller.Offset = new Vector(0, 130);
        Settle(250);
        Assert.Equal("القسم الثاني", Current(x.Toc));
        x.Scroller.Offset = new Vector(0, x.Scroller.Extent.Height);
        Settle(250);
        Assert.Equal("القسم الثالث", Current(x.Toc));
    }

    [AvaloniaFact]
    public void Toc_offset_moves_the_line()
    {
        var x = MakeToc(t => t.Offset = 40);
        Show(x.Grid, FlowDirection.RightToLeft);
        x.Scroller.Offset = new Vector(0, 130);
        Settle(250);
        Assert.Equal("القسم الأول", Current(x.Toc));
    }

    [AvaloniaFact]
    public void Toc_click_marks_at_once_and_scrolls()
    {
        var x = MakeToc();
        var window = Show(x.Grid, FlowDirection.RightToLeft);
        var last = x.Links[3];
        var p = Center(last, window);
        window.MouseMove(p);
        window.MouseDown(p, MouseButton.Left);
        window.MouseUp(p, MouseButton.Left);
        Dispatcher.UIThread.RunJobs();
        Assert.Same(last, x.Toc.ActiveLink);
        Settle(800);
        Assert.Equal(x.Scroller.Extent.Height - x.Scroller.Viewport.Height, x.Scroller.Offset.Y, 1);
        Assert.Same(last, x.Toc.ActiveLink);
    }

    [AvaloniaFact]
    public void Toc_raises_ActiveChanged_with_its_section()
    {
        var x = MakeToc();
        var seen = new List<ScrollspyEventArgs>();
        x.Toc.ActiveChanged += (_, e) => seen.Add(e);
        Show(x.Grid, FlowDirection.RightToLeft);
        seen.Clear();
        x.Scroller.Offset = new Vector(0, 130);
        Settle(250);
        var e = Assert.Single(seen);
        Assert.Same(x.Links[1], e.Link);
        Assert.Same(x.Links[1].Section, e.Section);
    }

    [AvaloniaFact]
    public void Toc_rows_match_the_web()
    {
        var x = MakeToc();
        Show(x.Grid, FlowDirection.RightToLeft);
        Assert.True(x.Links[0].Bounds.Height >= 35);
        Assert.True(x.Links[2].Bounds.Height >= 33);
        Assert.Equal(3, Part<Border>(x.Toc, "PART_Marker").Bounds.Width);
    }

    // Look capture

    [AvaloniaTheory]
    [MemberData(nameof(Looks), MemberType = typeof(Ui))]
    public void Plugins_popups_capture(string theme, string dir)
    {
        var phone = new PhoneBox();
        var color = new ColorField { Value = Color.Parse("#4c1d95") };
        var page = new StackPanel { Margin = new Thickness(64), Spacing = 24, Width = 360, HorizontalAlignment = HorizontalAlignment.Left };
        page.Children.Add(phone);
        page.Children.Add(color);
        var window = Show(page, dir == "rtl" ? FlowDirection.RightToLeft : FlowDirection.LeftToRight, theme == "Dark" ? ThemeVariant.Dark : ThemeVariant.Light, 640, 640);
        phone.IsDropDownOpen = true;
        Settle(400);
        Assert.True(phone.IsDropDownOpen);
        Capture(window, $"plugins-phone-open-{theme}-{dir}");
        phone.IsDropDownOpen = false;
        Settle(400);
        color.IsDropDownOpen = true;
        Settle(400);
        Assert.True(color.IsDropDownOpen);
        Capture(window, $"plugins-color-open-{theme}-{dir}");
    }
}
