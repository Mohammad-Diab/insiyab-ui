using System.Text.RegularExpressions;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.VisualTree;
using Insiyab.Avalonia.Controls;
using static Insiyab.Avalonia.Tests.Ui;

namespace Insiyab.Avalonia.Tests;

// The layout bits by look classes: fields, commit lines, threads, loading boxes, tree labels, button groups, page heads and pagination, every state pictured and dumped for the before/after comparison, runtime class changes, overrides, and which elements take a theme.
public class LayoutLookTests
{
    private static readonly Geometry Glyph = Geometry.Parse("M4,4 L20,4 L20,20 L4,20 Z");

    private static FlowDirection Dir(string dir) => dir == "rtl" ? FlowDirection.RightToLeft : FlowDirection.LeftToRight;

    private static ThemeVariant Variant(string theme) => theme == "Dark" ? ThemeVariant.Dark : ThemeVariant.Light;

    private static StackPanel Page(params Control[] children)
    {
        var page = new StackPanel { Margin = new Thickness(20), Spacing = 16, Width = 460, HorizontalAlignment = HorizontalAlignment.Left };
        page.Children.AddRange(children);
        return page;
    }

    private static T First<T>(Visual root, string cls) where T : Control => root.GetVisualDescendants().OfType<T>().First(c => c.Classes.Contains(cls));

    private static Style App<T>(string cls, AvaloniaProperty property, object value) where T : Control =>
        new(x => x.OfType<T>().Class(cls)) { Setters = { new Setter(property, value) } };

    private static void Keep(Window w, Visual root, string name) => LookCheck.Keep(w, root, name);

    private static void DumpOnly(Visual root, string name) => File.WriteAllText(System.IO.Path.Combine(ShotsDir(), name + ".txt"), LookCheck.Dump(root));

    private static StackPanel Field(string label, string? hint = null, string? error = null)
    {
        var field = new StackPanel { Classes = { "field" }, Children = { new TextBlock { Classes = { "label" }, Text = label }, new TextBox { Text = "ليلى حداد" } } };
        if (hint is not null) field.Children.Add(new TextBlock { Classes = { "hint" }, Text = hint });
        if (error is not null) field.Children.Add(new ContentControl { Classes = { "error" }, Content = error });
        return field;
    }

    private static StackPanel TreeLabel(string text, string? end = null)
    {
        var label = new StackPanel { Classes = { "tree-label" }, Children = { new Icon { Data = Glyph }, new TextBlock { Text = text } } };
        if (end is not null) label.Children.Add(new TextBlock { Classes = { "tree-end" }, Text = end });
        return label;
    }

    private static TreeView Files(bool select)
    {
        var contract = new TreeViewItem { Header = TreeLabel("عقد.pdf", "٣"), IsSelected = select };
        var contracts = new TreeViewItem { Header = TreeLabel("العقود", "١٢"), IsExpanded = true, Items = { contract, new TreeViewItem { Header = TreeLabel("ملحق بنص أطول من سطر واحد يقطعه الصف بنقاط", "٥") } } };
        var plain = new TreeViewItem { Header = new StackPanel { Children = { new TextBlock { Text = "ملاحظات.txt" } } } };
        return new TreeView { Width = 380, Items = { contracts, plain } };
    }

    private static Comment Cmt(string author, string text, bool own = false)
    {
        var comment = new Comment { Avatar = author[..1], Author = author, Time = "قبل ساعتين", Content = text };
        if (own) comment.Classes.Add("own");
        return comment;
    }

    private static Button Joined(string text) => new() { Content = text, Classes = { "secondary" } };

    [AvaloniaTheory]
    [MemberData(nameof(Looks), MemberType = typeof(Ui))]
    public void Field_and_form_commit_keep_their_look(string theme, string dir)
    {
        var commit = new StackPanel { Classes = { "form-commit" }, Children = { new Button { Classes = { "primary" }, Content = "حفظ" }, new Button { Classes = { "ghost" }, Content = "إلغاء" } } };
        var name = Field("الاسم", hint: "يظهر في الفاتورة.");
        var company = Field("اسم الشركة", error: "الاسم مطلوب");
        var page = Page(name, company, commit);
        var w = Show(page, Dir(dir), Variant(theme), 520, 520);
        Keep(w, page, $"layout-field-{theme}-{dir}");
        Assert.Equal(6, name.Spacing);
        Assert.Equal(6, company.Spacing);
        var box = (Control)name.Children[1];
        var label = (Control)name.Children[0];
        Assert.Equal(label.Bounds.Bottom + 6, box.Bounds.Top, 1);
        Assert.Equal(Orientation.Horizontal, commit.Orientation);
        Assert.Equal(8, commit.Spacing);
        Assert.Equal(new Thickness(0, 16, 0, 0), commit.Margin);
        var main = (Control)commit.Children[0];
        var cancel = (Control)commit.Children[1];
        Assert.Equal(main.Bounds.Right + 8, cancel.Bounds.Left, 1);
    }

    [AvaloniaTheory]
    [MemberData(nameof(Looks), MemberType = typeof(Ui))]
    public void Thread_keeps_its_look(string theme, string dir)
    {
        var first = Cmt("ليلى", "هل راجعت البند الثالث من العقد؟");
        first.Actions = new Button { Content = "رد", Classes = { "bare", "sm" } };
        var reply = Cmt("عمر", "عدلته، وهو جاهز الآن.");
        first.Replies.Add(reply);
        var thread = new StackPanel { Classes = { "thread" }, Children = { first, Cmt("أنت", "شكرًا، سأرسله اليوم.", own: true), Cmt("سارة", "وصلني الملف.") } };
        var page = Page(thread);
        var w = Show(page, Dir(dir), Variant(theme), 520, 620);
        Keep(w, page, $"layout-thread-{theme}-{dir}");
        Assert.Equal(20, thread.Spacing);
        var gap = thread.Children[1].Bounds.Top - thread.Children[0].Bounds.Bottom;
        Assert.Equal(20, gap, 1);
    }

    [AvaloniaTheory]
    [MemberData(nameof(Looks), MemberType = typeof(Ui))]
    public void Loading_box_keeps_its_look(string theme, string dir)
    {
        var text = new Border { Classes = { "loading" }, Child = new TextBlock { Text = "جارٍ التحميل…" } };
        var page = Page(text);
        var w = Show(page, Dir(dir), Variant(theme), 520, 320);
        Keep(w, page, $"layout-loading-{theme}-{dir}");
        Assert.Equal(200, text.MinHeight);
        var label = (TextBlock)text.Child!;
        Assert.Equal(HorizontalAlignment.Center, label.HorizontalAlignment);
        Assert.Equal(VerticalAlignment.Center, label.VerticalAlignment);
        Assert.Equal(200, text.Bounds.Height, 1);

        var spinner = new LoadingSpinner();
        var box = new Border { Classes = { "loading" }, Child = spinner };
        var other = Page(box);
        var w2 = Show(other, Dir(dir), Variant(theme), 520, 320);
        Settle(400);
        DumpOnly(other, $"layout-loading-spinner-{theme}-{dir}");
        Assert.Equal(HorizontalAlignment.Center, spinner.HorizontalAlignment);
        Assert.Equal(VerticalAlignment.Center, spinner.VerticalAlignment);
        Assert.Equal(box.Bounds.Width / 2, spinner.Bounds.Left + spinner.Bounds.Width / 2, 1);
        Assert.Equal(box.Bounds.Height / 2, spinner.Bounds.Top + spinner.Bounds.Height / 2, 1);
        Assert.NotNull(w2);
    }

    [AvaloniaTheory]
    [MemberData(nameof(Looks), MemberType = typeof(Ui))]
    public void Tree_label_and_end_keep_their_look(string theme, string dir)
    {
        var tree = Files(select: false);
        var w = Show(Page(tree), Dir(dir), Variant(theme), 520, 360);
        Keep(w, tree, $"layout-tree-{theme}-{dir}");
        var label = First<StackPanel>(tree, "tree-label");
        Assert.Equal(Orientation.Horizontal, label.Orientation);
        Assert.Equal(8, label.Spacing);
        var end = First<TextBlock>(tree, "tree-end");
        Assert.Equal(new Thickness(8, 0, 0, 0), end.Margin);
        Assert.Equal(TextTrimming.None, end.TextTrimming);
        var text = label.Children.OfType<TextBlock>().First();
        Assert.Equal(TextTrimming.CharacterEllipsis, text.TextTrimming);
        Assert.Equal(VerticalAlignment.Center, text.VerticalAlignment);

        var selected = Files(select: true);
        var w2 = Show(Page(selected), Dir(dir), Variant(theme), 520, 360);
        Keep(w2, selected, $"layout-tree-selected-{theme}-{dir}");
        Assert.Equal(Orientation.Horizontal, First<StackPanel>(selected, "tree-label").Orientation);
    }

    [AvaloniaTheory]
    [MemberData(nameof(Looks), MemberType = typeof(Ui))]
    public void Button_group_keeps_its_look(string theme, string dir)
    {
        var group = new StackPanel { Classes = { "btn-group" }, Children = { Joined("السابق"), Joined("اليوم"), Joined("التالي") } };
        var small = new StackPanel { Classes = { "btn-group", "sm" }, Children = { new Button { Content = "أ", Classes = { "ghost", "sm" } }, new Button { Content = "ب", Classes = { "ghost", "sm" } } } };
        var single = new StackPanel { Classes = { "btn-group" }, Children = { new Button { Content = "وحده" } } };
        var page = Page(group, small, single);
        var w = Show(page, Dir(dir), Variant(theme), 520, 260);
        Keep(w, page, $"layout-btn-group-{theme}-{dir}");
        Assert.Equal(Orientation.Horizontal, group.Orientation);
        Assert.Equal(-1, group.Spacing);
        Assert.Equal(HorizontalAlignment.Left, group.HorizontalAlignment);
        Assert.Equal(VerticalAlignment.Center, group.VerticalAlignment);
        var buttons = group.Children.OfType<Button>().ToList();
        Assert.Equal(new CornerRadius(0), buttons[1].CornerRadius);
        Assert.Equal(new CornerRadius(14, 0, 0, 14), buttons[0].CornerRadius);
        Assert.Equal(new CornerRadius(0, 14, 14, 0), buttons[2].CornerRadius);
        Assert.Equal(new CornerRadius(10, 0, 0, 10), ((Button)small.Children[0]).CornerRadius);
        Assert.Equal(Res(single, "InsRMd"), ((Button)single.Children[0]).CornerRadius);
        Assert.Equal(Orientation.Horizontal, small.Orientation);
        Assert.Equal(-1, small.Spacing);
    }

    [AvaloniaTheory]
    [MemberData(nameof(Looks), MemberType = typeof(Ui))]
    public void Page_head_and_a_hand_made_header_keep_their_look(string theme, string dir)
    {
        var save = new Button { Classes = { "primary" }, Content = "حفظ" };
        var head = new PageHead { Title = "عنوان الصفحة", Subtitle = "سطر وصف قصير", Actions = save, Breadcrumb = new Breadcrumb { ItemsSource = new[] { "الرئيسية", "الصفحة" } } };
        var narrow = new PageHead { Title = "عنوان صفحة طويل قليلًا يلتف", Subtitle = "سطر وصف قصير", Actions = new Button { Content = "حفظ" } };
        var wide = new Border { Width = 460, Child = head };
        var tight = new Border { Width = 240, Child = narrow };
        var actions = new StackPanel { Classes = { "page-actions" }, Children = { new Button { Classes = { "primary" }, Content = "حفظ" }, new Button { Classes = { "ghost" }, Content = "إلغاء" } } };
        var title = new TextBlock { Classes = { "page-title" }, Text = "عنوان الصفحة" };
        var sub = new TextBlock { Classes = { "page-sub" }, Text = "سطر وصف قصير يشرح الصفحة ويلتف على أكثر من سطر لنرى ارتفاع السطر." };
        var hand = new StackPanel { Children = { title, sub, actions } };
        var page = Page(wide, tight, hand);
        var w = Show(page, Dir(dir), Variant(theme), 520, 700);
        Keep(w, page, $"layout-page-head-{theme}-{dir}");
        var partTitle = Part<TextBlock>(head, "PART_Title");
        var partSub = Part<TextBlock>(head, "PART_Subtitle");
        Assert.Equal(28, partTitle.FontSize);
        Assert.Equal(36.4, partTitle.LineHeight, 2);
        Assert.Equal(FontWeight.Bold, partTitle.FontWeight);
        Assert.Equal(TextWrapping.Wrap, partTitle.TextWrapping);
        AssertBrush(partTitle, "InsText1", () => partTitle.Foreground);
        Assert.Equal(new Thickness(0, 6, 0, 0), partSub.Margin);
        Assert.Equal(23.8, partSub.LineHeight, 2);
        AssertBrush(partSub, "InsLabel", () => partSub.Foreground);
        Assert.Contains(":stacked", narrow.Classes);
        Assert.Equal(28, title.FontSize);
        Assert.Equal(36.4, title.LineHeight, 2);
        Assert.Equal(Dir(dir) == FlowDirection.LeftToRight ? -0.56 : 0, title.LetterSpacing, 2);
        if (dir == "ltr") Assert.Equal(Res(title, "InsFontHeadingEn"), title.FontFamily);
        Assert.Equal(new Thickness(0, 6, 0, 0), sub.Margin);
        Assert.Equal((double)Res(sub, "InsFsMd"), sub.FontSize);
        Assert.Equal(Orientation.Horizontal, actions.Orientation);
        Assert.Equal(8, actions.Spacing);
        Assert.Equal(VerticalAlignment.Bottom, actions.VerticalAlignment);
    }

    [AvaloniaTheory]
    [MemberData(nameof(Looks), MemberType = typeof(Ui))]
    public void Pagination_keeps_its_look(string theme, string dir)
    {
        var pages = new Pagination { PageCount = 12, CurrentPage = 3 };
        var compact = new Pagination { PageCount = 12, CurrentPage = 3, IsCompact = true };
        var row = new StackPanel
        {
            Children =
            {
                new Button { Classes = { "page", "prev" }, Content = "السابق" },
                new Button { Classes = { "page", "current" }, Content = "1" },
                new Button { Classes = { "page" }, Content = "2" },
                new TextBlock { Classes = { "page-gap" }, Text = "…" },
                new Button { Classes = { "page" }, Content = "12" },
                new Button { Classes = { "page", "next" }, Content = "التالي" },
            },
        };
        var hand = new Border { Classes = { "pagination" }, Child = row };
        var page = Page(pages, compact, hand);
        var w = Show(page, Dir(dir), Variant(theme), 560, 260);
        Keep(w, page, $"layout-pagination-{theme}-{dir}");
        var gap = First<TextBlock>(compact, "page-gap");
        Assert.Equal(24, gap.MinWidth);
        Assert.Equal(TextAlignment.Center, gap.TextAlignment);
        Assert.Equal(VerticalAlignment.Center, gap.VerticalAlignment);
        Assert.Equal((double)Res(gap, "InsFsSm"), gap.FontSize);
        AssertBrush(gap, "InsText3", () => gap.Foreground);
        Assert.Equal(HorizontalAlignment.Left, hand.HorizontalAlignment);
        Assert.Equal(new Thickness(4), hand.Padding);
        Assert.Equal(new Thickness(1), hand.BorderThickness);
        Assert.Equal(Res(hand, "InsRMd"), hand.CornerRadius);
        AssertBrush(hand, "InsCtlBgHover", () => hand.Background);
        AssertBrush(hand, "InsLineGlass", () => hand.BorderBrush);
        Assert.Equal(Orientation.Horizontal, row.Orientation);
        Assert.Equal(3, row.Spacing);
        var handGap = (TextBlock)row.Children[3];
        Assert.Equal(24, handGap.MinWidth);
    }

    [AvaloniaFact]
    public void Layout_classes_added_at_runtime_take_their_look()
    {
        var field = new StackPanel { Children = { new TextBlock { Text = "الاسم" }, new TextBox() } };
        var commit = new StackPanel { Children = { new Button { Content = "حفظ" } } };
        var thread = new StackPanel { Children = { Cmt("ليلى", "نص"), Cmt("عمر", "نص") } };
        var loadingText = new TextBlock { Text = "جارٍ التحميل" };
        var loading = new Border { Child = loadingText };
        var group = new StackPanel { Children = { new Button { Content = "أ" }, new Button { Content = "ب" } } };
        var title = new TextBlock { Text = "عنوان" };
        var sub = new TextBlock { Text = "وصف" };
        var actions = new StackPanel { Children = { new Button { Content = "حفظ" } } };
        var rowStack = new StackPanel { Children = { new TextBlock { Text = "1" }, new TextBlock { Text = "2" } } };
        var pagination = new Border { Child = rowStack };
        var pageGap = new TextBlock { Text = "…" };
        var treeLabel = new StackPanel { Children = { new Icon { Data = Glyph }, new TextBlock { Text = "العقود" } } };
        var treeEnd = new TextBlock { Text = "١٢" };
        treeLabel.Children.Add(treeEnd);
        var tree = new TreeView { Items = { new TreeViewItem { Header = treeLabel } } };
        var page = new StackPanel { Children = { field, commit, thread, loading, group, title, sub, actions, pagination, pageGap, tree } };
        Show(page, FlowDirection.RightToLeft, null, 600, 1000);

        var fieldSpacing = field.Spacing;
        var commitSpacing = commit.Spacing;
        var threadSpacing = thread.Spacing;
        var groupSpacing = group.Spacing;
        var titleSize = title.FontSize;
        var subSize = sub.FontSize;
        var actionsSpacing = actions.Spacing;
        var rowSpacing = rowStack.Spacing;
        var gapMin = pageGap.MinWidth;
        var labelSpacing = treeLabel.Spacing;
        var endTrimming = treeEnd.TextTrimming;
        Assert.Equal(0, fieldSpacing);
        Assert.Equal(Orientation.Vertical, commit.Orientation);
        Assert.Equal(Orientation.Vertical, group.Orientation);
        Assert.Equal(Orientation.Vertical, actions.Orientation);
        Assert.Equal(Orientation.Vertical, rowStack.Orientation);
        Assert.Equal(Orientation.Vertical, treeLabel.Orientation);
        Assert.Equal(0, loading.MinHeight);
        Assert.Equal(HorizontalAlignment.Stretch, loadingText.HorizontalAlignment);
        Assert.Equal(new Thickness(0), treeEnd.Margin);

        field.Classes.Add("field");
        commit.Classes.Add("form-commit");
        thread.Classes.Add("thread");
        loading.Classes.Add("loading");
        group.Classes.Add("btn-group");
        title.Classes.Add("page-title");
        sub.Classes.Add("page-sub");
        actions.Classes.Add("page-actions");
        pagination.Classes.Add("pagination");
        pageGap.Classes.Add("page-gap");
        treeLabel.Classes.Add("tree-label");
        treeEnd.Classes.Add("tree-end");
        Settle(300);
        Assert.Equal(6, field.Spacing);
        Assert.Equal(Orientation.Horizontal, commit.Orientation);
        Assert.Equal(8, commit.Spacing);
        Assert.Equal(new Thickness(0, 16, 0, 0), commit.Margin);
        Assert.Equal(20, thread.Spacing);
        Assert.Equal(200, loading.MinHeight);
        Assert.Equal(HorizontalAlignment.Center, loadingText.HorizontalAlignment);
        Assert.Equal(VerticalAlignment.Center, loadingText.VerticalAlignment);
        Assert.Equal(Orientation.Horizontal, group.Orientation);
        Assert.Equal(-1, group.Spacing);
        Assert.Equal(new CornerRadius(14, 0, 0, 14), ((Button)group.Children[0]).CornerRadius);
        Assert.Equal(28, title.FontSize);
        Assert.Equal(36.4, title.LineHeight, 2);
        Assert.Equal(new Thickness(0, 6, 0, 0), sub.Margin);
        Assert.Equal(Orientation.Horizontal, actions.Orientation);
        Assert.Equal(8, actions.Spacing);
        Assert.Equal(new Thickness(4), pagination.Padding);
        Assert.Equal(Orientation.Horizontal, rowStack.Orientation);
        Assert.Equal(3, rowStack.Spacing);
        Assert.Equal(24, pageGap.MinWidth);
        Assert.Equal(Orientation.Horizontal, treeLabel.Orientation);
        Assert.Equal(8, treeLabel.Spacing);
        Assert.Equal(new Thickness(8, 0, 0, 0), treeEnd.Margin);
        Assert.Equal(TextTrimming.None, treeEnd.TextTrimming);

        field.Classes.Remove("field");
        commit.Classes.Remove("form-commit");
        thread.Classes.Remove("thread");
        loading.Classes.Remove("loading");
        group.Classes.Remove("btn-group");
        title.Classes.Remove("page-title");
        sub.Classes.Remove("page-sub");
        actions.Classes.Remove("page-actions");
        pagination.Classes.Remove("pagination");
        pageGap.Classes.Remove("page-gap");
        treeLabel.Classes.Remove("tree-label");
        treeEnd.Classes.Remove("tree-end");
        Settle(300);
        Assert.Equal(fieldSpacing, field.Spacing);
        Assert.Equal(Orientation.Vertical, commit.Orientation);
        Assert.Equal(commitSpacing, commit.Spacing);
        Assert.Equal(threadSpacing, thread.Spacing);
        Assert.Equal(0, loading.MinHeight);
        Assert.Equal(HorizontalAlignment.Stretch, loadingText.HorizontalAlignment);
        Assert.Equal(VerticalAlignment.Stretch, loadingText.VerticalAlignment);
        Assert.Equal(Orientation.Vertical, group.Orientation);
        Assert.Equal(groupSpacing, group.Spacing);
        Assert.Equal(titleSize, title.FontSize);
        Assert.Equal(subSize, sub.FontSize);
        Assert.Equal(new Thickness(0), sub.Margin);
        Assert.Equal(Orientation.Vertical, actions.Orientation);
        Assert.Equal(actionsSpacing, actions.Spacing);
        Assert.Equal(new Thickness(0), pagination.Padding);
        Assert.Equal(Orientation.Vertical, rowStack.Orientation);
        Assert.Equal(rowSpacing, rowStack.Spacing);
        Assert.Equal(gapMin, pageGap.MinWidth);
        Assert.Equal(Orientation.Vertical, treeLabel.Orientation);
        Assert.Equal(labelSpacing, treeLabel.Spacing);
        Assert.Equal(new Thickness(0), treeEnd.Margin);
        Assert.Equal(endTrimming, treeEnd.TextTrimming);
    }

    [AvaloniaFact]
    public void App_styles_and_local_values_still_win_over_the_layout_looks()
    {
        var field = new StackPanel { Classes = { "field" }, Children = { new TextBlock { Text = "الاسم" } } };
        var thread = new StackPanel { Classes = { "thread" }, Children = { new TextBlock { Text = "أ" } } };
        var commit = new StackPanel { Classes = { "form-commit" }, Children = { new Button { Content = "حفظ" } } };
        var sub = new TextBlock { Classes = { "page-sub" }, Text = "وصف" };
        var gap = new TextBlock { Classes = { "page-gap" }, Text = "…" };
        var pagination = new Border { Classes = { "pagination" }, Child = new StackPanel { Children = { new TextBlock { Text = "1" } } } };
        var local = new Border { Classes = { "loading" }, MinHeight = 90, Child = new TextBlock { Text = "…" } };
        var page = new StackPanel { Children = { field, thread, commit, sub, gap, pagination, local } };
        page.Styles.Add(App<StackPanel>("field", StackPanel.SpacingProperty, 11.0));
        page.Styles.Add(App<TextBlock>("page-sub", TextBlock.FontSizeProperty, 20.0));
        var appStyle = App<Border>("pagination", Border.CornerRadiusProperty, new CornerRadius(7));
        var appText = App<TextBlock>("page-gap", TextBlock.MinWidthProperty, 31.0);
        var typeOnly = new Style(x => x.OfType<StackPanel>()) { Setters = { new Setter(StackPanel.SpacingProperty, 9.0) } };
        var typeOnlyText = new Style(x => x.OfType<TextBlock>()) { Setters = { new Setter(TextBlock.TextAlignmentProperty, TextAlignment.End) } };
        Application.Current!.Styles.Add(appStyle);
        Application.Current.Styles.Add(appText);
        Application.Current.Styles.Add(typeOnly);
        Application.Current.Styles.Add(typeOnlyText);
        try
        {
            Show(page, FlowDirection.RightToLeft, height: 600);
            Assert.Equal(11, field.Spacing);
            Assert.Equal(20, sub.FontSize);
            Assert.Equal(new CornerRadius(7), pagination.CornerRadius);
            Assert.Equal(31, gap.MinWidth);
            Assert.Equal(90, local.MinHeight);
            Assert.Equal(20, thread.Spacing);
            Assert.Equal(8, commit.Spacing);
            Assert.Equal(TextAlignment.Center, gap.TextAlignment);
            Assert.Equal(3, ((StackPanel)pagination.Child!).Spacing);
        }
        finally
        {
            Application.Current.Styles.Remove(appStyle);
            Application.Current.Styles.Remove(appText);
            Application.Current.Styles.Remove(typeOnly);
            Application.Current.Styles.Remove(typeOnlyText);
        }
        Settle();
        Assert.Equal(Res(pagination, "InsRMd"), pagination.CornerRadius);
        Assert.Equal(24, gap.MinWidth);
    }

    [AvaloniaFact]
    public void Only_the_layout_parts_take_a_theme()
    {
        var plain = new StackPanel { Children = { new TextBlock { Text = "أ" } } };
        var plainText = new TextBlock { Text = "ب" };
        var plainBorder = new Border { Child = new TextBlock { Text = "ج" } };
        var stacks = new[] { "field", "form-commit", "thread", "btn-group", "page-actions" }.Select(c => new StackPanel { Classes = { c } }).ToList();
        var texts = new[] { "page-title", "page-sub", "page-gap" }.Select(c => new TextBlock { Classes = { c }, Text = c }).ToList();
        var loadingChild = new TextBlock { Text = "…" };
        var loading = new Border { Classes = { "loading" }, Child = loadingChild };
        var row = new StackPanel();
        var pagination = new Border { Classes = { "pagination" }, Child = row };
        var tree = Files(select: false);
        var page = new StackPanel { Children = { plain, plainText, plainBorder, loading, pagination, tree } };
        page.Children.AddRange(stacks);
        page.Children.AddRange(texts);
        Show(page, FlowDirection.RightToLeft, height: 900);
        Assert.Null(plain.Theme);
        Assert.Null(((TextBlock)plain.Children[0]).Theme);
        Assert.Null(plainText.Theme);
        Assert.Null(plainBorder.Theme);
        Assert.All(stacks, s => Assert.NotNull(s.Theme));
        Assert.All(texts, t => Assert.NotNull(t.Theme));
        Assert.NotNull(loading.Theme);
        Assert.Null(loadingChild.Theme);
        Assert.NotNull(pagination.Theme);
        Assert.NotNull(row.Theme);
        Assert.All(tree.GetVisualDescendants().OfType<StackPanel>().Where(s => s.Classes.Contains("tree-label")), s => Assert.NotNull(s.Theme));
        Assert.All(tree.GetVisualDescendants().OfType<TextBlock>().Where(t => t.Classes.Contains("tree-end")), t => Assert.NotNull(t.Theme));
        var plainHeader = tree.GetVisualDescendants().OfType<StackPanel>().First(s => s.Classes.Count == 0 && s.Children.Count == 1 && s.Children[0] is TextBlock { Text: "ملاحظات.txt" });
        Assert.Null(plainHeader.Theme);
        Assert.Null(((TextBlock)plainHeader.Children[0]).Theme);

        var head = new PageHead { Title = "عنوان", Subtitle = "وصف", Actions = new Button { Content = "حفظ" } };
        Show(head, FlowDirection.RightToLeft);
        Assert.NotNull(Part<TextBlock>(head, "PART_Title").Theme);
        Assert.NotNull(Part<TextBlock>(head, "PART_Subtitle").Theme);
        var pages = new Pagination { PageCount = 12, CurrentPage = 3, IsCompact = true };
        Show(pages, FlowDirection.RightToLeft);
        Assert.NotNull(First<TextBlock>(pages, "page-gap").Theme);
    }

    // Rules of these classes, scoped under a control or not; the still and inactive lines stay until the last rollout step.
    private static List<string> Rules(Type type, string cls)
    {
        var subject = new Regex(@"(?:^|[\s>])" + Regex.Escape(type.Name) + @"(?:\.[\w-]+|:[\w-]+(?:\([^)]*\))?|\[[^\]]*\])*\." + Regex.Escape(cls) + @"(?![\w-])(?:\.[\w-]+|:[\w-]+(?:\([^)]*\))?|\[[^\]]*\])*\s*$");
        return LookCheck.GlobalSelectors().SelectMany(s => s.Split(", ")).Select(a => a.Trim())
            .Where(a => subject.IsMatch(a) && !a.Contains("ins-still") && !a.Contains("IsInactive")).ToList();
    }

    [AvaloniaFact]
    public void No_global_rule_names_a_layout_class()
    {
        var found = new List<string>();
        foreach (var cls in new[] { "field", "form-commit", "thread", "btn-group", "page-actions", "tree-label" })
            found.AddRange(Rules(typeof(StackPanel), cls));
        foreach (var cls in new[] { "loading", "pagination" })
            found.AddRange(Rules(typeof(Border), cls));
        foreach (var cls in new[] { "page-title", "page-sub", "page-gap", "tree-end" })
            found.AddRange(Rules(typeof(TextBlock), cls));
        var ancestor = new Regex(@"(?:^|[\s>])Border\.(?:loading|pagination)(?![\w-]).*[\s>]\S+\s*$");
        found.AddRange(LookCheck.GlobalSelectors().SelectMany(s => s.Split(", ")).Select(a => a.Trim()).Where(a => ancestor.IsMatch(a) && !a.Contains("ins-still") && !a.Contains("IsInactive")));
        found = found.Distinct().OrderBy(s => s, StringComparer.Ordinal).ToList();
        Assert.True(found.Count == 0, "found: " + string.Join(" ; ", found));
    }
}
