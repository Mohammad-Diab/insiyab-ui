using System.Text.RegularExpressions;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.VisualTree;
using Insiyab.Avalonia.Controls;
using static Insiyab.Avalonia.Tests.Ui;

namespace Insiyab.Avalonia.Tests;

// Type, prose and field text by look classes: every class and pair pictured and dumped for the before/after comparison, runtime class and direction changes, overrides, and which elements take a theme.
public class TypeLookTests
{
    private static readonly string[] TextClasses = ["display", "h1", "h2", "h3", "h4", "lead", "prose", "eyebrow", "muted", "num", "label", "hint", "dt", "dd"];

    private static readonly string[] BorderClasses = ["code", "pre", "blockquote", "th", "td"];

    private static T Cls<T>(T element, string classes) where T : StyledElement
    {
        foreach (var c in classes.Split(' ', StringSplitOptions.RemoveEmptyEntries)) element.Classes.Add(c);
        return element;
    }

    private static TextBlock Text(string text, string classes = "") => Cls(new TextBlock { Text = text }, classes);

    private static Style App<T>(string cls, AvaloniaProperty property, object value) where T : Control =>
        new(x => x.OfType<T>().Class(cls)) { Setters = { new Setter(property, value) } };

    private static FlowDirection Dir(string dir) => dir == "rtl" ? FlowDirection.RightToLeft : FlowDirection.LeftToRight;

    private static ThemeVariant Variant(string theme) => theme == "Dark" ? ThemeVariant.Dark : ThemeVariant.Light;

    private static bool Near(Color a, Color b) =>
        Math.Abs(a.A - b.A) <= 6 && Math.Abs(a.R - b.R) <= 6 && Math.Abs(a.G - b.G) <= 6 && Math.Abs(a.B - b.B) <= 6;

    private static TextBlock Find(Visual root, string classes, int skip = 0)
    {
        var want = classes.Split(' ');
        return root.GetVisualDescendants().OfType<TextBlock>().Where(t => want.All(t.Classes.Contains) && t.Classes.Count(c => !c.StartsWith(':')) == want.Length).ElementAt(skip);
    }

    private static Border Box(Visual root, string cls, int skip = 0) =>
        root.GetVisualDescendants().OfType<Border>().Where(b => b.GetType() == typeof(Border) && b.Classes.Contains(cls)).ElementAt(skip);

    private static StackPanel TypePage() => new()
    {
        Margin = new Thickness(20),
        Spacing = 8,
        Width = 620,
        HorizontalAlignment = HorizontalAlignment.Left,
        Children =
        {
            Text("انسياب", "display"),
            Text("عنوان أول", "h1"),
            Text("عنوان ثان", "h2"),
            Text("عنوان ثالث", "h3"),
            Text("عنوان رابع", "h4"),
            Text("Heading one", "h1"),
            Text("Display Latin", "display"),
            Cls(new TextBlock { Text = "Inter · 1,240,905", FlowDirection = FlowDirection.LeftToRight }, "h3"),
            Text("الاسم والبريد وكلمة المرور، كلها في مكان واحد يسهل الوصول إليه من أي صفحة.", "lead"),
            Text("فقرة عادية تشرح ما تعرضه الصفحة في سطر أو سطرين، وتكمل بجملة ثانية حتى تلتف على أكثر من سطر واحد في العرض الضيق.", "prose"),
            Text("الحساب", "eyebrow"),
            Text("نص خافت", "muted"),
            Text("-2,375,000", "num"),
            Text("عنوان خافت", "h3 muted"),
            Text("رمز 123456", "muted num"),
            Text("مقدمة خافتة", "lead muted"),
            Text("نص عادي بلا صنف"),
        },
    };

    private static Border Code(string text) => Cls(new Border { Child = new TextBlock { Text = text } }, "code");

    private static Border Pre(string text) => Cls(new Border { Child = new TextBlock { Text = text } }, "pre");

    private static Border Quote(string text) => Cls(new Border { Child = new TextBlock { Text = text } }, "blockquote");

    private static Grid Table()
    {
        var grid = Cls(new Grid { ColumnDefinitions = new ColumnDefinitions("*,*"), RowDefinitions = new RowDefinitions("Auto,Auto,Auto") }, "table");
        void Cell(string cls, int row, int col, string text)
        {
            var cell = Cls(new Border { Child = new TextBlock { Text = text } }, cls);
            Grid.SetRow(cell, row);
            Grid.SetColumn(cell, col);
            grid.Children.Add(cell);
        }
        Cell("th", 0, 0, "الاسم");
        Cell("th", 0, 1, "القيمة");
        Cell("td", 1, 0, "الأول");
        Cell("td", 1, 1, "12");
        Cell("td", 2, 0, "الثاني");
        Cell("td", 2, 1, "7");
        return grid;
    }

    private static Control[] Blocks() =>
    [
        Text("عنوان", "h2"),
        Text("فقرة تسبق الشيفرة وتشرح ما فيها بجملة قصيرة تكفي لأن تلتف على سطر ثان في العرض الضيق."),
        Code("Insiyab.Avalonia"),
        Pre("GET /api/items\n  ?page=2"),
        Quote("اقتباس قصير يقف على حافة البداية."),
        Table(),
        Text("المدة", "dt"),
        Text("14 يومًا", "dd"),
        Text("عنوان فرعي", "h4"),
    ];

    private static TextBlock Required(string text)
    {
        var label = Text("", "label");
        label.Text = null;
        label.Inlines = new InlineCollection { new Run(text), new Run(" *") { Classes = { "req" } } };
        return label;
    }

    private static StackPanel Fields() => new()
    {
        Margin = new Thickness(20),
        Spacing = 4,
        Width = 420,
        HorizontalAlignment = HorizontalAlignment.Left,
        Children =
        {
            Text("الاسم", "label"),
            new TextBox { PlaceholderText = "الاسم الكامل" },
            Text("تلميح قصير تحت الحقل، يلتف على سطر ثان إذا طال حتى يقف عند حد العرض كله.", "hint"),
            Cls(new TextBlock { Text = "Email", FlowDirection = FlowDirection.LeftToRight }, "label"),
            new TextBox { PlaceholderText = "name@example.com" },
            Cls(new TextBlock { Text = "A short hint under the field.", FlowDirection = FlowDirection.LeftToRight }, "hint"),
            Required("كلمة المرور"),
            Text("ثمانية أحرف على الأقل", "hint"),
        },
    };

    [AvaloniaTheory]
    [MemberData(nameof(Looks), MemberType = typeof(Ui))]
    public void Type_classes_keep_their_look(string theme, string dir)
    {
        var page = TypePage();
        var w = Show(page, Dir(dir), Variant(theme), 660, 900);
        LookCheck.Keep(w, page, $"type-{theme}-{dir}");
        var rtl = dir == "rtl";
        var arabic = (FontFamily)Res(page, "InsFontArabic");
        var heading = (FontFamily)Res(page, "InsFontHeadingEn");
        var body = (FontFamily)Res(page, "InsFontBodyEn");
        var display = Find(page, "display");
        var h1 = Find(page, "h1");
        var h2 = Find(page, "h2");
        var h3 = Find(page, "h3");
        var h4 = Find(page, "h4");
        Assert.Equal(rtl ? arabic : heading, display.FontFamily);
        Assert.Equal(rtl ? arabic : heading, h1.FontFamily);
        Assert.Equal(rtl ? arabic : heading, h2.FontFamily);
        Assert.Equal(rtl ? arabic : heading, h3.FontFamily);
        Assert.Equal(rtl ? arabic : body, h4.FontFamily);
        Assert.Equal(heading, Find(page, "h3", 1).FontFamily);
        Assert.Equal((double)Res(page, "InsFsH1"), h1.FontSize);
        Assert.Equal((double)Res(page, "InsFsDisplay"), display.FontSize);
        Assert.Equal(FontWeight.Bold, h1.FontWeight);
        Assert.Equal(TextWrapping.Wrap, h2.TextWrapping);
        Assert.Equal(57.6, display.LineHeight, 1);
        Assert.Equal(36.4, h1.LineHeight, 1);
        Assert.Equal(28.6, h2.LineHeight, 1);
        Assert.Equal(23.4, h3.LineHeight, 1);
        Assert.Equal(20.8, h4.LineHeight, 1);
        Assert.Equal(rtl ? 0 : -0.96, display.LetterSpacing, 2);
        Assert.Equal(rtl ? 0 : -0.56, h1.LetterSpacing, 2);
        Assert.Equal(rtl ? 0 : -0.44, h2.LetterSpacing, 2);
        Assert.Equal(rtl ? 0 : -0.36, h3.LetterSpacing, 2);
        Assert.Equal(0, h4.LetterSpacing, 2);
        AssertBrush(page, "InsText1", () => h1.Foreground);
        var lead = Find(page, "lead");
        Assert.Equal(18, lead.FontSize);
        Assert.Equal(32.4, lead.LineHeight, 1);
        AssertBrush(page, "InsLabel", () => lead.Foreground);
        var prose = Find(page, "prose");
        Assert.Equal(rtl ? 28.8 : 26.4, prose.LineHeight, 1);
        Assert.Equal(TextWrapping.Wrap, prose.TextWrapping);
        var eyebrow = Find(page, "eyebrow");
        Assert.Equal(FontWeight.Bold, eyebrow.FontWeight);
        Assert.Equal(rtl ? 0 : 0.96, eyebrow.LetterSpacing, 2);
        AssertBrush(page, "InsToneBrand", () => eyebrow.Foreground);
        var muted = Find(page, "muted");
        AssertBrush(page, "InsLabel", () => muted.Foreground);
        var num = Find(page, "num");
        Assert.Equal(FlowDirection.LeftToRight, num.FlowDirection);
        Assert.Equal(HorizontalAlignment.Left, num.HorizontalAlignment);
        Assert.Equal(2, num.FontFeatures?.Count ?? 0);
        var soft = Find(page, "h3 muted");
        AssertBrush(page, "InsLabel", () => soft.Foreground);
        Assert.Equal(FontWeight.Bold, soft.FontWeight);
        Assert.Equal(23.4, soft.LineHeight, 1);
        var code = Find(page, "muted num");
        AssertBrush(page, "InsLabel", () => code.Foreground);
        Assert.Equal(FlowDirection.LeftToRight, code.FlowDirection);
        var plain = page.Children.OfType<TextBlock>().Last();
        Assert.Equal(0, plain.LetterSpacing, 2);
        Assert.Equal(FontWeight.Normal, plain.FontWeight);
        Assert.DoesNotContain(plain.Classes, c => !c.StartsWith(':'));
    }

    [AvaloniaTheory]
    [MemberData(nameof(Looks), MemberType = typeof(Ui))]
    public void Prose_blocks_keep_their_look(string theme, string dir)
    {
        var inside = new Prose();
        foreach (var block in Blocks()) inside.Children.Add(block);
        var outside = new StackPanel { Spacing = 8 };
        foreach (var block in Blocks()) outside.Children.Add(block);
        var page = new StackPanel { Margin = new Thickness(20), Spacing = 20, Width = 700, HorizontalAlignment = HorizontalAlignment.Left, Children = { inside, outside } };
        var w = Show(page, Dir(dir), Variant(theme), 740, 1500);
        LookCheck.Keep(w, page, $"prose-{theme}-{dir}");
        var ltr = dir == "ltr";
        foreach (var root in new Visual[] { inside, outside })
        {
            var inProse = root == inside;
            var code = Box(root, "code");
            Assert.Equal(FlowDirection.LeftToRight, code.FlowDirection);
            Assert.Equal(new Thickness(5.25, 1.75), code.Padding);
            Assert.Equal(new CornerRadius(5), code.CornerRadius);
            Assert.Equal(new Thickness(1), code.BorderThickness);
            Assert.True(double.IsNaN(((TextBlock)code.Child!).LineHeight), $"code text line height in prose {inProse}");
            AssertBrush(root, "InsCtlBg", () => code.Background);
            var pre = Box(root, "pre");
            Assert.Equal(FlowDirection.LeftToRight, pre.FlowDirection);
            Assert.Equal(new Thickness(16.25, 13), pre.Padding);
            var preText = (TextBlock)pre.Child!;
            Assert.Equal(20.8, preText.LineHeight, 1);
            Assert.Equal(TextWrapping.NoWrap, preText.TextWrapping);
            Assert.Equal(HorizontalAlignment.Left, preText.HorizontalAlignment);
            Assert.Equal(13, pre.GetValue(TextElement.FontSizeProperty));
            var quote = Box(root, "blockquote");
            Assert.Equal(new Thickness(20, 12), quote.Padding);
            Assert.Equal(new Thickness(3, 0, 0, 0), quote.BorderThickness);
            Assert.Equal(new CornerRadius(0, 10, 10, 0), quote.CornerRadius);
            AssertBrush(root, "InsToneBrandBg", () => quote.Background);
            var head = Box(root, "th");
            var cell = Box(root, "td");
            Assert.Equal(new Thickness(10.5, 7), head.Padding);
            Assert.Equal(new Thickness(0, 0, 0, 1), cell.BorderThickness);
            AssertBrush(root, "InsCtlBd", () => head.BorderBrush);
            AssertBrush(root, "InsLine", () => cell.BorderBrush);
            Assert.Equal(FontWeight.Bold, head.GetValue(TextElement.FontWeightProperty));
            var cellText = (TextBlock)cell.Child!;
            Assert.Equal(inProse && ltr ? 23.1 : 25.2, cellText.LineHeight, 1);
            var dt = Find(root, "dt");
            var dd = Find(root, "dd");
            Assert.Equal(FontWeight.Bold, dt.FontWeight);
            Assert.Equal(new Thickness(0, 0, 0, 8), dd.Margin);
            AssertBrush(root, "InsLabel", () => dd.Foreground);
            var h2 = Find(root, "h2");
            var h4 = Find(root, "h4");
            Assert.Equal(28.6, h2.LineHeight, 1);
            Assert.Equal(20.8, h4.LineHeight, 1);
            Assert.Equal(FontWeight.Bold, h2.FontWeight);
        }
    }

    [AvaloniaTheory]
    [MemberData(nameof(Looks), MemberType = typeof(Ui))]
    public void Field_labels_and_hints_keep_their_look(string theme, string dir)
    {
        var page = Fields();
        var w = Show(page, Dir(dir), Variant(theme), 460, 420);
        LookCheck.Keep(w, page, $"fields-{theme}-{dir}");
        var labels = page.Children.OfType<TextBlock>().Where(t => t.Classes.Contains("label")).ToList();
        var hints = page.Children.OfType<TextBlock>().Where(t => t.Classes.Contains("hint")).ToList();
        Assert.Equal(3, labels.Count);
        Assert.Equal(3, hints.Count);
        foreach (var label in labels)
        {
            Assert.Equal(FontWeight.Bold, label.FontWeight);
            Assert.Equal(13, label.FontSize);
            AssertBrush(page, "InsLabel", () => label.Foreground);
            Assert.Equal(label.FlowDirection == FlowDirection.LeftToRight ? 0.13 : 0, label.LetterSpacing, 2);
        }
        foreach (var hint in hints)
        {
            Assert.Equal(11, hint.FontSize);
            Assert.Equal(TextWrapping.Wrap, hint.TextWrapping);
            AssertBrush(page, "InsText3", () => hint.Foreground);
        }
    }

    [AvaloniaFact]
    public void Type_classes_added_at_runtime_take_their_look()
    {
        var heading = Text("عنوان");
        var note = Text("ملاحظة");
        var label = Text("الاسم");
        var term = Text("المدة");
        var chip = new Border { Child = new TextBlock { Text = "code" } };
        var pre = new Border { Child = new TextBlock { Text = "GET /" } };
        var quote = new Border { Child = new TextBlock { Text = "اقتباس" } };
        var cell = new Border { Child = new TextBlock { Text = "خلية" } };
        var table = Cls(new Grid { Children = { cell } }, "table");
        var page = new StackPanel { Spacing = 8, Width = 500, Children = { heading, note, label, term, chip, pre, quote, table } };
        var w = Show(page, FlowDirection.RightToLeft, height: 600);
        var plainSize = heading.FontSize;
        var plainFamily = heading.FontFamily;
        Assert.Equal(FontWeight.Normal, heading.FontWeight);
        heading.Classes.Add("h3");
        Settle();
        Assert.Equal(FontWeight.Bold, heading.FontWeight);
        Assert.Equal(23.4, heading.LineHeight, 1);
        Assert.Equal(Res(page, "InsFontArabic"), heading.FontFamily);
        AssertBrush(page, "InsText1", () => heading.Foreground);
        heading.Classes.Add("muted");
        AssertBrush(page, "InsLabel", () => heading.Foreground);
        Assert.Equal(FontWeight.Bold, heading.FontWeight);
        heading.Classes.Remove("muted");
        AssertBrush(page, "InsText1", () => heading.Foreground);
        w.FlowDirection = FlowDirection.LeftToRight;
        Settle();
        Assert.Equal(Res(page, "InsFontHeadingEn"), heading.FontFamily);
        Assert.Equal(-0.36, heading.LetterSpacing, 2);
        w.FlowDirection = FlowDirection.RightToLeft;
        Settle();
        Assert.Equal(Res(page, "InsFontArabic"), heading.FontFamily);
        Assert.Equal(0, heading.LetterSpacing, 2);
        heading.Classes.Remove("h3");
        Settle();
        Assert.Equal(plainSize, heading.FontSize);
        Assert.Equal(plainFamily, heading.FontFamily);
        Assert.Equal(FontWeight.Normal, heading.FontWeight);
        Assert.True(double.IsNaN(heading.LineHeight));

        note.Classes.Add("num");
        Settle();
        Assert.Equal(FlowDirection.LeftToRight, note.FlowDirection);
        Assert.Equal(HorizontalAlignment.Left, note.HorizontalAlignment);
        note.Classes.Remove("num");
        Settle();
        Assert.Equal(FlowDirection.RightToLeft, note.FlowDirection);
        Assert.Equal(HorizontalAlignment.Stretch, note.HorizontalAlignment);

        label.Classes.Add("label");
        Settle();
        Assert.Equal(FontWeight.Bold, label.FontWeight);
        AssertBrush(page, "InsLabel", () => label.Foreground);
        label.Classes.Remove("label");
        label.Classes.Add("hint");
        Settle();
        Assert.Equal(11, label.FontSize);
        Assert.Equal(FontWeight.Normal, label.FontWeight);
        label.Classes.Remove("hint");

        term.Classes.Add("dt");
        Settle();
        Assert.Equal(FontWeight.Bold, term.FontWeight);
        term.Classes.Remove("dt");
        term.Classes.Add("dd");
        Settle();
        Assert.Equal(new Thickness(0, 0, 0, 8), term.Margin);
        Assert.Equal(FontWeight.Normal, term.FontWeight);
        term.Classes.Remove("dd");
        Settle();
        Assert.Equal(new Thickness(0), term.Margin);

        chip.Classes.Add("code");
        pre.Classes.Add("pre");
        quote.Classes.Add("blockquote");
        cell.Classes.Add("th");
        Settle();
        Assert.Equal(new Thickness(5.25, 1.75), chip.Padding);
        Assert.Equal(20.8, ((TextBlock)pre.Child!).LineHeight, 1);
        Assert.Equal(new Thickness(3, 0, 0, 0), quote.BorderThickness);
        Assert.Equal(new Thickness(0, 0, 0, 1), cell.BorderThickness);
        cell.Classes.Remove("th");
        cell.Classes.Add("td");
        Settle();
        Assert.Equal(new Thickness(10.5, 7), cell.Padding);
        Assert.Equal(FontWeight.Normal, cell.GetValue(TextElement.FontWeightProperty));
        chip.Classes.Remove("code");
        pre.Classes.Remove("pre");
        quote.Classes.Remove("blockquote");
        cell.Classes.Remove("td");
        Settle();
        Assert.Equal(new Thickness(0), chip.Padding);
        Assert.True(double.IsNaN(((TextBlock)pre.Child!).LineHeight));
        Assert.Equal(new Thickness(0), quote.BorderThickness);
        Assert.Equal(new Thickness(0), cell.Padding);
    }

    [AvaloniaFact]
    public void App_styles_and_local_values_still_win_over_the_type_looks()
    {
        var h1 = Text("عنوان", "h1");
        var other = Text("آخر", "h1");
        var label = Text("الاسم", "label");
        var hint = Text("تلميح", "hint");
        var page = new StackPanel { Spacing = 8, Width = 500, Children = { h1, other, label, hint } };
        page.Styles.Add(App<TextBlock>("label", TextBlock.ForegroundProperty, Brushes.Red));
        var appStyle = App<TextBlock>("hint", TextBlock.FontSizeProperty, 30d);
        var typeOnly = new Style(x => x.OfType<TextBlock>()) { Setters = { new Setter(TextBlock.LineHeightProperty, 99d) } };
        Application.Current!.Styles.Add(appStyle);
        Application.Current.Styles.Add(typeOnly);
        try
        {
            Show(page, FlowDirection.RightToLeft, height: 400);
            other.FontSize = 40;
            Settle();
            Assert.True(Near(Colors.Red, ColorOf(label.Foreground)));
            Assert.Equal(30, hint.FontSize);
            Assert.Equal(40, other.FontSize);
            Assert.Equal(36.4, h1.LineHeight, 1);
            Assert.Equal(99, label.LineHeight);
        }
        finally
        {
            Application.Current.Styles.Remove(appStyle);
            Application.Current.Styles.Remove(typeOnly);
        }
        Settle();
        Assert.Equal(11, hint.FontSize);
        Assert.True(double.IsNaN(label.LineHeight));
    }

    [AvaloniaFact]
    public void Prose_blocks_keep_their_text_rules_when_the_block_sits_in_prose()
    {
        var chip = Code("x");
        var pre = Pre("y");
        var inside = new Prose { Children = { Text("فقرة"), chip, pre } };
        var plain = Text("عادي");
        var page = new StackPanel { Width = 600, Children = { inside, plain } };
        Show(page, FlowDirection.LeftToRight, height: 400, width: 640);
        Assert.True(double.IsNaN(plain.LineHeight));
        Assert.True(double.IsNaN(((TextBlock)chip.Child!).LineHeight));
        Assert.Equal(20.8, ((TextBlock)pre.Child!).LineHeight, 1);
        Assert.Equal(TextWrapping.NoWrap, ((TextBlock)pre.Child!).TextWrapping);
        Assert.Equal(26.4, ((TextBlock)inside.Children[0]).LineHeight, 1);
    }

    [AvaloniaFact]
    public void Only_the_type_parts_take_a_theme()
    {
        var plainText = Text("أ");
        var plainBox = new Border { Child = new TextBlock { Text = "ب" } };
        var page = TypePage();
        var blocks = new StackPanel();
        foreach (var block in Blocks()) blocks.Children.Add(block);
        var fields = Fields();
        Show(new StackPanel { Children = { plainText, plainBox, page, blocks, fields } }, FlowDirection.RightToLeft, height: 2400);
        Assert.Null(plainText.Theme);
        Assert.Null(plainBox.Theme);
        Assert.Null(((TextBlock)plainBox.Child!).Theme);
        foreach (var cls in TextClasses)
        {
            var found = new[] { page, blocks, fields }.SelectMany(r => r.GetVisualDescendants().OfType<TextBlock>()).Where(t => t.Classes.Contains(cls)).ToList();
            Assert.True(found.Count > 0, cls);
            Assert.All(found, t => Assert.NotNull(t.Theme));
        }
        foreach (var cls in BorderClasses)
        {
            var found = blocks.GetVisualDescendants().OfType<Border>().Where(b => b.GetType() == typeof(Border) && b.Classes.Contains(cls)).ToList();
            Assert.True(found.Count > 0, cls);
            Assert.All(found, b => Assert.NotNull(b.Theme));
        }
        Assert.Null(page.Children.OfType<TextBlock>().Last().Theme);
    }

    [AvaloniaFact]
    public void No_global_rule_names_type_classes()
    {
        var found = new List<string>();
        foreach (var cls in TextClasses)
            found.AddRange(LookCheck.SubjectRules(typeof(TextBlock), cls, "ins-still", "IsInactive"));
        foreach (var cls in BorderClasses)
            found.AddRange(LookCheck.SubjectRules(typeof(Border), cls, "ins-still", "IsInactive"));
        var loose = new Regex(@":is\(TextBlock\)\.(display|h1|h2|h3|h4|lead|prose|eyebrow|muted|num|label|hint|dt|dd)(?![\w-])");
        found.AddRange(LookCheck.GlobalSelectors().SelectMany(s => s.Split(", ")).Where(alt => loose.IsMatch(alt)));
        found = found.Distinct().ToList();
        Assert.True(found.Count == 0, "found: " + string.Join(" ; ", found));
    }
}
