using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.VisualTree;
using Insiyab.Avalonia.Controls;

using static Insiyab.Avalonia.Tests.Ui;

namespace Insiyab.Avalonia.Tests;

public class InputsTests
{
    private static StackPanel Page(params Control[] children)
    {
        var page = new StackPanel { Margin = new Thickness(20), Spacing = 16, Width = 360, HorizontalAlignment = HorizontalAlignment.Left };
        page.Children.AddRange(children);
        return page;
    }

    // The left edge in window space: in RTL a control's own origin is its right edge.
    private static double X(Control c, Window w) => Math.Min(c.TranslatePoint(new Point(0, 0), w)!.Value.X, c.TranslatePoint(new Point(c.Bounds.Width, 0), w)!.Value.X);

    // Under a loaded full run a brush transition can still be landing, so this waits for it a little longer.
    private static void SettledBrush(Visual anchor, string key, Func<IBrush?> brush)
    {
        var expected = ColorOf((IBrush)Res(anchor, key));
        for (var i = 0; i < 20; i++)
        {
            var got = ColorOf(brush());
            if (Math.Abs(expected.A - got.A) <= 6 && Math.Abs(expected.R - got.R) <= 6 && Math.Abs(expected.G - got.G) <= 6 && Math.Abs(expected.B - got.B) <= 6) break;
            Settle(150);
        }
        AssertBrush(anchor, key, () => brush());
    }

    private static void Type(Window window, TextBox box, string text)
    {
        box.Focus();
        window.KeyTextInput(text);
        Settle(100);
    }

    // ---------------------------------------------------------------- text field

    [AvaloniaFact]
    public void TextBox_gets_the_field_look()
    {
        var box = new TextBox { PlaceholderText = "الاسم" };
        Show(Page(box));

        var border = Part<Border>(box, "PART_BorderElement");
        Part<TextPresenter>(box, "PART_TextPresenter");
        Assert.Equal(46, box.Bounds.Height);
        AssertBrush(box, "InsCtlBg", () => border.Background);
        AssertBrush(box, "InsCtlBd", () => border.BorderBrush);
        Assert.Equal(Res(box, "InsRMd"), border.CornerRadius);
        Assert.Equal(Res(box, "InsBevelDim"), border.BoxShadow);
        Assert.Equal(Res(box, "InsShSubtle"), Part<Border>(box, "PART_Shadow").BoxShadow);
        AssertBrush(box, "InsText3", () => Part<TextBlock>(box, "PART_Placeholder").Foreground);
        Assert.Equal(14, box.FontSize);
    }

    [AvaloniaFact]
    public void TextBox_focus_glows_and_lifts()
    {
        var box = new TextBox();
        Show(Page(box));

        box.Focus();
        Settle();

        AssertBrush(box, "InsGlassStrong", () => Part<Border>(box, "PART_BorderElement").Background);
        Assert.Equal(Res(box, "InsGlow"), Part<Border>(box, "PART_Shadow").BoxShadow);
        Assert.Equal(1, Part<Border>(box, "PART_Halo").Opacity, 2);
        Assert.Equal(-1, Part<Panel>(box, "PART_Field").RenderTransform!.Value.M32, 2);
    }

    [AvaloniaFact]
    public void Dark_focus_is_a_faint_white_fill()
    {
        var box = new TextBox();
        Show(Page(box), theme: ThemeVariant.Dark);
        box.Focus();
        Settle();
        Assert.Equal(Color.FromArgb(0x17, 255, 255, 255), ColorOf(Part<Border>(box, "PART_BorderElement").Background));
    }

    [AvaloniaFact]
    public void Halo_follows_the_brand()
    {
        var box = new TextBox();
        Show(Page(box));
        var halo = Part<Border>(box, "PART_Halo");
        var primary = (Color)Res(box, "InsPrimaryColor");
        Assert.Equal(Color.FromArgb(0x1A, primary.R, primary.G, primary.B), halo.BoxShadow[0].Color);
        Assert.Equal(16, halo.BoxShadow[0].Blur);
    }

    [AvaloniaTheory]
    [InlineData("sm", 38, 13)]
    [InlineData("lg", 52, 16)]
    public void TextBox_sizes(string size, double height, double font)
    {
        var box = new TextBox { Classes = { size } };
        Show(Page(box));
        Assert.Equal(height, box.Bounds.Height);
        Assert.Equal(font, box.FontSize);
    }

    [AvaloniaFact]
    public void Textarea_is_taller_and_wraps()
    {
        var box = new TextBox { AcceptsReturn = true };
        Show(Page(box));
        Assert.Equal(88, box.Bounds.Height);
        Assert.Equal(TextWrapping.Wrap, box.TextWrapping);
    }

    [AvaloniaFact]
    public void Disabled_field_fades()
    {
        var box = new TextBox { IsEnabled = false };
        Show(Page(box));
        Assert.Equal(0.55, box.Opacity, 2);
    }

    [AvaloniaFact]
    public void TextBox_error_turns_red_and_shows_the_message()
    {
        var box = new TextBox { Text = "x" };
        Show(Page(box));

        DataValidationErrors.SetError(box, new Exception("هذا الحقل مطلوب"));
        Settle();

        Assert.Contains(":error", box.Classes);
        var border = Part<Border>(box, "PART_BorderElement");
        SettledBrush(box, "InsToneBadBd", () => border.BorderBrush);
        SettledBrush(box, "InsToneBadBg", () => border.Background);
        var message = box.GetVisualDescendants().OfType<TextBlock>().Single(t => t.Text == "هذا الحقل مطلوب");
        Assert.True(message.IsEffectivelyVisible);
        AssertBrush(box, "InsToneBad", () => message.Foreground);
    }

    [AvaloniaFact]
    public void Error_focus_uses_the_bad_glow()
    {
        var box = new TextBox { Text = "x" };
        Show(Page(box));
        DataValidationErrors.SetErrors(box, ["خطأ"]);
        box.Focus();
        Settle();
        AssertBrush(box, "InsToneBad", () => Part<Border>(box, "PART_BorderElement").BorderBrush);
        Assert.Equal(Res(box, "InsGlowBad"), Part<Border>(box, "PART_Shadow").BoxShadow);
        Assert.Equal(0, Part<Border>(box, "PART_Halo").Opacity, 2);
    }

    [AvaloniaFact]
    public void Bad_class_looks_like_an_error()
    {
        var box = new TextBox { Classes = { "bad" } };
        Show(Page(box));
        AssertBrush(box, "InsToneBadBd", () => Part<Border>(box, "PART_BorderElement").BorderBrush);
    }

    [AvaloniaFact]
    public void Ok_class_turns_green()
    {
        var box = new TextBox { Classes = { "ok" }, Text = "name" };
        Show(Page(box));
        var border = Part<Border>(box, "PART_BorderElement");
        AssertBrush(box, "InsToneOkBg", () => border.Background);
        var ok = (Color)Res(box, "InsToneOkColor");
        var got = ColorOf(border.BorderBrush);
        Assert.Equal(ok.R, got.R);
        Assert.Equal(0.55, ((ISolidColorBrush)border.BorderBrush!).Opacity, 2);

        box.Focus();
        Settle();
        AssertBrush(box, "InsToneOk", () => border.BorderBrush);
        Assert.Equal(Res(box, "InsTextBoxGlowOk"), Part<Border>(box, "PART_Shadow").BoxShadow);
    }

    [AvaloniaFact]
    public void MaskedTextBox_wears_the_field()
    {
        var box = new MaskedTextBox { Mask = "000-000" };
        Show(Page(box));
        AssertBrush(box, "InsCtlBg", () => Part<Border>(box, "PART_BorderElement").Background);
    }

    // ---------------------------------------------------------------- password

    [AvaloniaFact]
    public void Reveal_button_toggles_RevealPassword()
    {
        var box = new TextBox { PasswordChar = '•', Text = "secret", Classes = { "revealPasswordButton" } };
        var window = Show(Page(box));

        var eye = box.GetVisualDescendants().OfType<ToggleButton>().Single();
        Assert.Equal(36, eye.Bounds.Width);
        Assert.False(box.RevealPassword);
        Click(eye, window);
        Assert.True(box.RevealPassword);
        Assert.True(eye.IsChecked);
        AssertBrush(box, "InsToneBrandSoft", () => eye.Foreground);
        Click(eye, window);
        Assert.False(box.RevealPassword);
    }

    [AvaloniaTheory]
    [InlineData(FlowDirection.LeftToRight, FlowDirection.LeftToRight, true)]
    [InlineData(FlowDirection.RightToLeft, FlowDirection.LeftToRight, true)]
    [InlineData(FlowDirection.RightToLeft, FlowDirection.RightToLeft, false)]
    [InlineData(FlowDirection.LeftToRight, FlowDirection.RightToLeft, false)]
    public void Reveal_button_sits_on_the_end_side(FlowDirection windowDirection, FlowDirection boxDirection, bool onRight)
    {
        var box = new TextBox { PasswordChar = '•', Text = "secret", Width = 300, FlowDirection = boxDirection, Classes = { "revealPasswordButton" } };
        var window = Show(new StackPanel { Margin = new Thickness(20), Children = { box } }, windowDirection);

        var eye = box.GetVisualDescendants().OfType<ToggleButton>().Single();
        Assert.Equal(onRight, Center(eye, window).X > Center(box, window).X);
        var gap = onRight ? X(box, window) + box.Bounds.Width - (X(eye, window) + eye.Bounds.Width) : X(eye, window) - X(box, window);
        Assert.Equal(7, gap, 0);
    }

    [AvaloniaFact]
    public void An_error_under_an_ltr_field_reads_in_the_page_direction()
    {
        var field = new TextBox { Text = "name@example", FlowDirection = FlowDirection.LeftToRight };
        DataValidationErrors.SetErrors(field, ["أدخل بريدًا صحيحًا."]);
        Show(new StackPanel { Children = { field } }, FlowDirection.RightToLeft);

        var message = field.GetVisualDescendants().OfType<TextBlock>().First(t => t.Text == "أدخل بريدًا صحيحًا.");
        Assert.Equal(FlowDirection.RightToLeft, message.FlowDirection);
    }

    // ---------------------------------------------------------------- search

    [AvaloniaTheory]
    [InlineData(FlowDirection.RightToLeft)]
    [InlineData(FlowDirection.LeftToRight)]
    public void Search_glyph_sits_at_the_start_and_takes_the_brand_on_focus(FlowDirection dir)
    {
        var box = new TextBox { Classes = { "search" }, PlaceholderText = "بحث" };
        var window = Show(Page(box), dir);
        var icon = box.GetVisualDescendants().OfType<Icon>().Single();
        var slot = Part<ContentPresenter>(box, "PART_InnerLeft");
        Assert.Equal(13, icon.Bounds.Width);
        var fromStart = dir == FlowDirection.RightToLeft ? X(box, window) + box.Bounds.Width - (X(icon, window) + icon.Bounds.Width) : X(icon, window) - X(box, window);
        Assert.Equal(14, fromStart, 0);
        AssertBrush(box, "InsText3", () => slot.Foreground);
        box.Focus();
        Settle();
        AssertBrush(box, "InsToneBrandSoft", () => slot.Foreground);
        Assert.Equal(1.12, slot.RenderTransform!.Value.M11, 2);
    }

    // ---------------------------------------------------------------- input group

    [AvaloniaTheory]
    [InlineData(FlowDirection.RightToLeft)]
    [InlineData(FlowDirection.LeftToRight)]
    public void Addon_at_the_end_has_its_hairline_toward_the_text(FlowDirection dir)
    {
        var addon = new ContentControl { Classes = { "addon" }, Content = "ر.س" };
        var box = new TextBox { Classes = { "input-group" }, Text = "249", InnerRightContent = addon };
        var window = Show(Page(box), dir);

        Assert.Equal(new Thickness(1, 0, 0, 0), addon.BorderThickness);
        AssertBrush(box, "InsLabel", () => addon.Foreground);
        Assert.Equal(FontWeight.Bold, addon.FontWeight);
        var addonAtRight = Center(addon, window).X > Center(box, window).X;
        Assert.Equal(dir == FlowDirection.LeftToRight, addonAtRight);
        Assert.Equal(46 - 2 - 16, addon.Bounds.Height, 0);
    }

    [AvaloniaFact]
    public void Addon_at_the_start_has_its_hairline_on_its_end()
    {
        var addon = new ContentControl { Classes = { "addon" }, Content = "ر.س" };
        var box = new TextBox { Classes = { "input-group" }, Text = "249", InnerLeftContent = addon };
        var window = Show(Page(box), FlowDirection.RightToLeft);
        Assert.Equal(new Thickness(0, 0, 1, 0), addon.BorderThickness);
        Assert.True(Center(addon, window).X > Center(box, window).X);
    }

    [AvaloniaFact]
    public void Group_text_starts_where_a_plain_field_text_does()
    {
        var plain = new TextBox { Text = "249" };
        var group = new TextBox { Classes = { "input-group" }, Text = "249" };
        var window = Show(Page(plain, group));
        var a = X(Part<TextPresenter>(plain, "PART_TextPresenter"), window) - X(plain, window);
        var b = X(Part<TextPresenter>(group, "PART_TextPresenter"), window) - X(group, window);
        Assert.Equal(15, a, 0);
        Assert.Equal(a, b, 0);
    }

    [AvaloniaFact]
    public void A_button_in_a_group_is_a_step_smaller()
    {
        var button = new Button { Content = "تطبيق" };
        var icon = new Button { Classes = { "icon" }, Content = "+" };
        var box = new TextBox { Classes = { "input-group" }, InnerRightContent = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 4, Children = { button, icon } } };
        Show(Page(box));
        Assert.Equal(34, button.Bounds.Height);
        Assert.Equal(new Size(34, 34), icon.Bounds.Size);
        Assert.Equal(Res(box, "InsRSm"), button.CornerRadius);
    }

    [AvaloniaFact]
    public void Group_focus_uses_the_strong_glass_in_dark()
    {
        var box = new TextBox { Classes = { "input-group" } };
        Show(Page(box), theme: ThemeVariant.Dark);
        box.Focus();
        Settle();
        AssertBrush(box, "InsGlassStrong", () => Part<Border>(box, "PART_BorderElement").Background);
    }

    // ---------------------------------------------------------------- counter

    [AvaloniaFact]
    public void Char_counter_counts_against_max_length()
    {
        var box = new TextBox { MaxLength = 10 };
        InputCount.SetMode(box, InputCountMode.Chars);
        var window = Show(Page(box));
        var count = Part<TextBlock>(box, "PART_Count");
        Assert.Equal("0/10", count.Text);
        Assert.True(count.IsVisible);
        AssertBrush(box, "InsText3", () => count.Foreground);
        Assert.Equal(11, count.FontSize);

        box.Text = "123456789";
        Settle();
        Assert.Equal(InputCountState.Near, InputCount.GetState(box));
        AssertBrush(box, "InsToneWarn", () => count.Foreground);
        Assert.Equal(46 + 6 + count.Bounds.Height, box.Bounds.Height, 0);
    }

    [AvaloniaTheory]
    [InlineData(FlowDirection.RightToLeft)]
    [InlineData(FlowDirection.LeftToRight)]
    public void Counter_sits_at_the_end_of_the_page_line(FlowDirection dir)
    {
        var box = new TextBox { MaxLength = 60, FlowDirection = FlowDirection.LeftToRight };
        InputCount.SetMode(box, InputCountMode.Chars);
        var window = Show(Page(box), dir);
        var count = Part<TextBlock>(box, "PART_Count");
        Assert.Equal(dir == FlowDirection.LeftToRight, Center(count, window).X > Center(box, window).X);
    }

    [AvaloniaFact]
    public void Word_counter_past_its_limit_puts_the_field_in_error()
    {
        var box = new TextBox();
        InputCount.SetMode(box, InputCountMode.Words);
        InputCount.SetMax(box, 3);
        Show(Page(box));
        var count = Part<TextBlock>(box, "PART_Count");
        Assert.Equal("0/3 كلمات", count.Text);

        box.Text = "واحد اثنان ثلاثة أربعة";
        Settle();
        Assert.Equal("4/3 كلمات", count.Text);
        Assert.Equal(InputCountState.Over, InputCount.GetState(box));
        Assert.Equal(FontWeight.Bold, count.FontWeight);
        Assert.Contains(":error", box.Classes);
        Assert.Contains("لا تتجاوز 3 كلمات.", DataValidationErrors.GetErrors(box)!.Cast<object>());

        box.Text = "واحد";
        Settle();
        Assert.DoesNotContain(":error", box.Classes);
    }

    [AvaloniaFact]
    public void Counter_keeps_an_error_the_app_set()
    {
        var box = new TextBox();
        InputCount.SetMode(box, InputCountMode.Words);
        InputCount.SetMax(box, 1);
        Show(Page(box));
        DataValidationErrors.SetErrors(box, ["من التطبيق"]);
        box.Text = "أ ب";
        Settle();
        box.Text = "أ";
        Settle();
        Assert.Equal(["من التطبيق"], DataValidationErrors.GetErrors(box)!.Cast<object>());
    }

    [AvaloniaFact]
    public void Counter_format_and_free_words()
    {
        var a = new TextBox { Text = "أ ب" };
        InputCount.SetMode(a, InputCountMode.Words);
        var b = new TextBox { Text = "abc", MaxLength = 5 };
        InputCount.SetMode(b, InputCountMode.Chars);
        InputCount.SetFormat(b, "{n} من {max}");
        Show(Page(a, b));
        Assert.Equal("كلمتان", InputCount.GetText(a));
        Assert.Equal("3 من 5", InputCount.GetText(b));
        InputCount.SetMode(b, InputCountMode.None);
        Assert.False(Part<TextBlock>(b, "PART_Count").IsEffectivelyVisible);
    }

    // ---------------------------------------------------------------- validation

    private static (StackPanel form, TextBox name, TextBox mail, Button send) ValidateForm()
    {
        var name = new TextBox();
        Validate.SetRequired(name, true);
        var mail = new TextBox();
        Validate.SetEmail(mail, true);
        var send = new Button { Content = "إرسال" };
        Validate.SetSubmit(send, true);
        var form = Page(name, mail, send);
        Validate.SetForm(form, true);
        return (form, name, mail, send);
    }

    [AvaloniaFact]
    public void Submit_reports_every_invalid_field_and_stops_the_click()
    {
        var (form, name, mail, send) = ValidateForm();
        var window = Show(form);
        mail.Text = "abc";
        var clicked = 0;
        var submitted = 0;
        send.Click += (_, _) => clicked++;
        Validate.AddSubmittedHandler(form, (_, _) => submitted++);

        Assert.DoesNotContain(":error", name.Classes);
        Click(send, window);

        Assert.Equal(0, clicked);
        Assert.Equal(0, submitted);
        Assert.Contains(":error", name.Classes);
        Assert.Contains("املأ هذا الحقل.", DataValidationErrors.GetErrors(name)!.Cast<object>());
        Assert.Contains("اكتب بريدًا إلكترونيًا صحيحًا.", DataValidationErrors.GetErrors(mail)!.Cast<object>());
        Assert.True(name.IsFocused);

        name.Text = "سارة";
        mail.Text = "sara@example.com";
        Settle(100);
        Assert.DoesNotContain(":error", name.Classes);
        Assert.DoesNotContain(":error", mail.Classes);
        Click(send, window);
        Assert.Equal(1, clicked);
        Assert.Equal(1, submitted);
    }

    [AvaloniaFact]
    public void A_field_is_checked_once_left_after_a_change()
    {
        var (form, name, mail, _) = ValidateForm();
        var window = Show(form);
        mail.Focus();
        Settle(100);
        name.Focus();
        Settle(100);
        Assert.DoesNotContain(":error", mail.Classes);

        Type(window, mail, "x");
        Assert.DoesNotContain(":error", mail.Classes);
        name.Focus();
        Settle(100);
        Assert.Contains(":error", mail.Classes);
    }

    [AvaloniaTheory]
    [InlineData(0, "", null, "املأ هذا الحقل.")]
    [InlineData(8, "abc", null, "اكتب 8 أحرف على الأقل.")]
    [InlineData(0, "12a", "[0-9]+", "اكتب القيمة بالصيغة المطلوبة.")]
    [InlineData(0, "123", "[0-9]+", null)]
    public void Rules_and_their_messages(int min, string text, string? pattern, string? expected)
    {
        var box = new TextBox { Text = text };
        Validate.SetRequired(box, true);
        Validate.SetMinLength(box, min);
        Validate.SetPattern(box, pattern);
        Assert.Equal(expected, Validate.Problem(box));
        Validate.SetMessage(box, "رسالتي.");
        Assert.Equal(expected is null ? null : "رسالتي.", Validate.Problem(box));
    }

    [AvaloniaFact]
    public void Success_and_error_messages_on_their_own()
    {
        var ok = new ContentControl { Classes = { "success" }, Content = "الاسم متاح." };
        var bad = new ContentControl { Classes = { "error" }, Content = "هذا الاسم محجوز." };
        Show(Page(ok, bad));
        AssertBrush(ok, "InsToneOk", () => ok.Foreground);
        AssertBrush(bad, "InsToneBad", () => bad.Foreground);
        Assert.Equal(12, ok.FontSize);
        Assert.NotNull(Part<global::Avalonia.Controls.Shapes.Path>(ok, "PART_Mark").Data);
    }

    // ---------------------------------------------------------------- autocomplete

    [AvaloniaTheory]
    [InlineData("اسامة", "أسامة", true)]
    [InlineData("مكه", "مكة", true)]
    [InlineData("مصطفي", "مصطفى", true)]
    [InlineData("١٢", "شقة 12", true)]
    [InlineData("دمشق", "حلب", false)]
    public void Arabic_matching(string search, string item, bool hit)
    {
        Assert.Equal(hit, ArabicMatch.Contains(search, item));
    }

    [AvaloniaFact]
    public void AutoComplete_suggests_with_arabic_matching()
    {
        var box = new AutoCompleteBox { ItemsSource = new[] { "أسامة", "سارة", "مريم" }, MinimumPopulateDelay = TimeSpan.Zero };
        var window = Show(Page(box));
        var inner = Part<TextBox>(box, "PART_TextBox");
        AssertBrush(inner, "InsCtlBg", () => Part<Border>(inner, "PART_BorderElement").Background);
        inner.Focus();
        window.KeyTextInput("اسا");
        Settle();
        Assert.True(box.IsDropDownOpen);
        var list = (ListBox)((Border)Part<Popup>(box, "PART_Popup").Child!).Child!;
        var items = list.GetRealizedContainers().OfType<ListBoxItem>().ToList();
        Assert.Single(items);
        Assert.Equal("أسامة", items[0].Content);
        Assert.Equal(Res(box, "InsRXs"), items[0].CornerRadius);
    }

    // ---------------------------------------------------------------- select

    [AvaloniaTheory]
    [InlineData(FlowDirection.RightToLeft)]
    [InlineData(FlowDirection.LeftToRight)]
    public void Select_wears_the_field_with_the_chevron_at_the_end(FlowDirection dir)
    {
        var combo = new ComboBox { Items = { "الأول", "الثاني" }, SelectedIndex = 0 };
        var window = Show(Page(combo), dir);
        Assert.Equal(46, combo.Bounds.Height);
        Assert.Equal(360, combo.Bounds.Width);
        var face = Part<Border>(combo, "Background");
        AssertBrush(combo, "InsCtlBg", () => face.Background);
        var chevron = Part<Icon>(combo, "DropDownGlyph");
        Assert.Equal(16, chevron.Bounds.Width);
        var fromEnd = dir == FlowDirection.LeftToRight ? X(combo, window) + combo.Bounds.Width - (X(chevron, window) + 16) : X(chevron, window) - X(combo, window);
        Assert.Equal(14, fromEnd, 0);
    }

    [AvaloniaFact]
    public void Select_opens_a_glass_list_with_the_choice_ticked()
    {
        var combo = new ComboBox { Items = { "الأول", "الثاني", "الثالث" }, SelectedIndex = 1 };
        var window = Show(Page(combo), height: 500);
        combo.IsDropDownOpen = true;
        Settle();
        var popup = ((Border)Part<Popup>(combo, "PART_Popup").Child!);
        AssertBrush(combo, "InsGlassStrong", () => popup.Background);
        Assert.Equal(Res(combo, "InsRMd"), popup.CornerRadius);
        var items = combo.GetRealizedContainers().OfType<ComboBoxItem>().ToList();
        Assert.Equal(3, items.Count);
        var chosen = items[1];
        Assert.Equal(FontWeight.Bold, chosen.FontWeight);
        Assert.Equal(1, Part<Icon>(chosen, "PART_Check").Opacity);
        Assert.Equal(0, Part<Icon>(items[0], "PART_Check").Opacity);
        Assert.Equal(Res(combo, "InsRXs"), chosen.CornerRadius);
        Assert.True(((Border)Part<Popup>(combo, "PART_Popup").Child!).TranslatePoint(new Point(0, 0), combo)!.Value.Y >= 46 + 5);
    }

    // A long list's bar sits at the card's inner edge, its padding scrolling with the items.
    private static void AssertBarAtEdge(Border card)
    {
        var bar = card.GetVisualDescendants().OfType<ScrollBar>().Single(b => b.Orientation == Orientation.Vertical && b.IsEffectivelyVisible);
        var r = new Rect(bar.Bounds.Size).TransformToAABB(bar.TransformToVisual(card)!.Value);
        Assert.Equal(card.Bounds.Width - card.BorderThickness.Right, r.Right, 0.5);
        Assert.Equal(card.Bounds.Height - card.BorderThickness.Top - card.BorderThickness.Bottom, r.Height, 0.5);
    }

    [AvaloniaFact]
    public void Select_and_autocomplete_lists_keep_the_bar_at_their_edge()
    {
        var combo = new ComboBox { SelectedIndex = 0 };
        for (var i = 0; i < 30; i++) combo.Items.Add($"خيار {i}");
        var auto = new AutoCompleteBox { ItemsSource = Enumerable.Range(0, 30).Select(i => $"اسم {i}").ToArray(), MinimumPopulateDelay = TimeSpan.Zero };
        var window = Show(Page(combo, auto), height: 700);
        combo.IsDropDownOpen = true;
        Settle();
        var list = (Border)Part<Popup>(combo, "PART_Popup").Child!;
        AssertBarAtEdge(list);
        var first = combo.ContainerFromIndex(0)!;
        Assert.Equal(7, first.TranslatePoint(new Point(0, 0), list)!.Value.X, 0.5);
        combo.IsDropDownOpen = false;
        Settle(300);

        Part<TextBox>(auto, "PART_TextBox").Focus();
        window.KeyTextInput("اسم");
        Settle();
        Assert.True(auto.IsDropDownOpen);
        var suggestions = (Border)Part<Popup>(auto, "PART_Popup").Child!;
        AssertBarAtEdge(suggestions);
        var row = suggestions.GetVisualDescendants().OfType<ListBoxItem>().First();
        Assert.Equal(7, row.TranslatePoint(new Point(0, 0), suggestions)!.Value.X, 0.5);
    }

    [AvaloniaFact]
    public void Select_error_turns_red()
    {
        var combo = new ComboBox { Items = { "الأول" } };
        Show(Page(combo));
        DataValidationErrors.SetErrors(combo, ["اختر واحدًا."]);
        Settle();
        SettledBrush(combo, "InsToneBadBd", () => Part<Border>(combo, "Background").BorderBrush);
        Assert.Contains(combo.GetVisualDescendants().OfType<TextBlock>(), t => t.Text == "اختر واحدًا.");
    }

    [AvaloniaTheory]
    [InlineData("sm", 38)]
    [InlineData("lg", 52)]
    public void Select_sizes(string size, double height)
    {
        var combo = new ComboBox { Items = { "الأول" }, SelectedIndex = 0, Classes = { size } };
        Show(Page(combo));
        Assert.Equal(height, combo.Bounds.Height);
    }

    // ---------------------------------------------------------------- range

    [AvaloniaTheory]
    [InlineData(FlowDirection.RightToLeft)]
    [InlineData(FlowDirection.LeftToRight)]
    public void Range_starts_at_the_reading_start(FlowDirection dir)
    {
        var slider = new Slider { Minimum = 0, Maximum = 100, Value = 0 };
        var window = Show(Page(slider), dir);
        var thumb = slider.GetVisualDescendants().OfType<Thumb>().Single();
        Assert.Equal(new Size(20, 20), thumb.Bounds.Size);
        Assert.Equal(24, slider.Bounds.Height);
        Assert.Equal(dir == FlowDirection.RightToLeft, Center(thumb, window).X > Center(slider, window).X);
        var fill = Part<RepeatButton>(slider, "PART_DecreaseButton");
        var rest = Part<RepeatButton>(slider, "PART_IncreaseButton");
        AssertBrush(slider, "InsToneBrandSoft", () => fill.Background);
        AssertBrush(slider, "InsCtlBgHover", () => rest.Background);
        Assert.Equal(6, Part<Border>(rest, "TrackBackground").Bounds.Height);
    }

    [AvaloniaFact]
    public void Range_thumb_grows_on_hover()
    {
        var slider = new Slider { Minimum = 0, Maximum = 100, Value = 50 };
        var window = Show(Page(slider));
        window.MouseMove(Center(slider, window));
        Settle();
        var thumb = slider.GetVisualDescendants().OfType<Thumb>().Single();
        Assert.Equal(1.1, thumb.RenderTransform!.Value.M11, 2);
    }

    // ---------------------------------------------------------------- stepper

    [AvaloniaTheory]
    [InlineData(FlowDirection.RightToLeft)]
    [InlineData(FlowDirection.LeftToRight)]
    public void Stepper_puts_minus_at_the_start_and_plus_at_the_end(FlowDirection dir)
    {
        var num = new NumericUpDown { Width = 137, Value = 2, Minimum = 0, Maximum = 3, Increment = 1 };
        var window = Show(Page(num), dir);
        var down = Part<RepeatButton>(num, "PART_DecreaseButton");
        var up = Part<RepeatButton>(num, "PART_IncreaseButton");
        Assert.Equal(new Size(36, 36), up.Bounds.Size);
        Assert.Equal(46, num.Bounds.Height);
        Assert.Equal(dir == FlowDirection.RightToLeft, Center(up, window).X < Center(down, window).X);
        AssertBrush(num, "InsCtlBgHover", () => up.Background);
        Assert.Equal(TextAlignment.Center, Part<TextBox>(num, "PART_TextBox").TextAlignment);

        Click(up, window);
        Assert.Equal(3m, num.Value);
        Assert.False(up.IsEnabled);
        Assert.True(down.IsEnabled);
    }

    [AvaloniaFact]
    public void Stepper_inner_text_has_no_box_of_its_own()
    {
        var num = new NumericUpDown { Value = 2 };
        Show(Page(num));
        var inner = Part<TextBox>(num, "PART_TextBox");
        Assert.False(Part<Border>(inner, "PART_BorderElement").IsVisible);
        Assert.False(Part<Border>(inner, "PART_Shadow").IsVisible);
        AssertBrush(num, "InsCtlBg", () => Part<Border>(num, "PART_BorderElement").Background);
    }

    // ---------------------------------------------------------------- shots

    [AvaloniaTheory]
    [MemberData(nameof(Ui.Looks), MemberType = typeof(Ui))]
    public void Capture_fields(string theme, string dir)
    {
        var focused = new TextBox { PlaceholderText = "اكتب هنا" };
        var error = new TextBox { Text = "abc" };
        var count = new TextBox { MaxLength = 60, Text = "نص قصير" };
        InputCount.SetMode(count, InputCountMode.Chars);
        var page = new StackPanel
        {
            Margin = new Thickness(24),
            Spacing = 16,
            Width = 420,
            HorizontalAlignment = HorizontalAlignment.Left,
            Children =
            {
                new TextBox { PlaceholderText = "الاسم الكامل" },
                new TextBox { Text = "user@example.com", FlowDirection = FlowDirection.LeftToRight },
                new TextBox { Text = "secret123", PasswordChar = '•', FlowDirection = FlowDirection.LeftToRight, Classes = { "revealPasswordButton" } },
                new TextBox { Classes = { "search" }, PlaceholderText = "بحث" },
                new TextBox { Classes = { "input-group" }, Text = "249", InnerRightContent = new ContentControl { Classes = { "addon" }, Content = "ر.س" } },
                new ComboBox { Items = { "الأول", "الثاني" }, SelectedIndex = 0 },
                new NumericUpDown { Value = 2, Minimum = 0, Maximum = 10, Width = 137, HorizontalAlignment = HorizontalAlignment.Left },
                new Slider { Minimum = 0, Maximum = 100, Value = 40 },
                count,
                focused,
                error,
                new TextBox { Text = "معطّل", IsEnabled = false },
            },
        };
        var window = Show(page, dir == "rtl" ? FlowDirection.RightToLeft : FlowDirection.LeftToRight, theme == "Dark" ? ThemeVariant.Dark : ThemeVariant.Light, 520, 1000);
        DataValidationErrors.SetErrors(error, ["هذه القيمة غير صحيحة"]);
        focused.Focus();
        Capture(window, $"inputs-{theme}-{dir}");
    }
}
