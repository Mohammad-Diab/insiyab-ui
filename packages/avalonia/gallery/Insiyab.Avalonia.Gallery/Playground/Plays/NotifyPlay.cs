using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.LogicalTree;
using Avalonia.Media;
using Insiyab.Avalonia.Controls;
using static Insiyab.Avalonia.Gallery.Playground.Play;

namespace Insiyab.Avalonia.Gallery.Playground;

public static class NotifyPlay
{
    public const string Example =
        """
        <ins:Notify>
          <ins:NotifyItem Icon="{StaticResource IconDownload}" Title="اكتمل التصدير" Description="الملف جاهز للتنزيل." End="الآن" IsUnread="True" />
          <ins:NotifyItem Icon="{StaticResource IconMessage}" Title="تعليق جديد على المستند" Description="على الفقرة الثانية." End="قبل ساعة" IsUnread="True" />
          <ins:NotifyItem Icon="{StaticResource IconCheck}" Title="تم حفظ التغييرات" Description="حُفظت نسخة جديدة من الملف." End="أمس" />
        </ins:Notify>
        """;

    public static string Code { get; } =
        """
        var count = Inbox.UnreadCount;    // عدد غير المقروءة
        Inbox.MarkRead(item);             // تعليم صف مقروءًا، بهدوء
        Inbox.MarkAllRead();              // تعليمها كلها مقروءة
        Inbox.Items.Insert(0, item);      // إشعار جديد يعده الجرس ويهتز له

        Inbox.Read += (_, e) =>
            Debug.WriteLine($"{e.Item} {e.All}");  // حين يعلمها المستخدم مقروءة
        """;

    private static readonly (string Title, string Desc, string End, string Icon, string Who)[] Rows =
    [
        ("اكتمل التصدير", "الملف جاهز للتنزيل.", "الآن", "IconDownload", "م"),
        ("تعليق جديد على المستند", "على الفقرة الثانية.", "قبل ساعة", "IconMessage", "س"),
        ("تم حفظ التغييرات", "حُفظت نسخة جديدة من الملف.", "أمس", "IconCheck", "ل"),
    ];

    private static string Line((string Title, string Desc, string End, string Icon, string Who) r, bool unread, bool avatar) =>
        $"  <ins:NotifyItem {(avatar ? $"Avatar=\"{r.Who}\"" : $"Icon=\"{{StaticResource {r.Icon}}}\"")} Title=\"{r.Title}\" Description=\"{r.Desc}\" End=\"{r.End}\"{(unread ? " IsUnread=\"True\"" : "")} />";

    public static PlayDef Notify { get; } = new()
    {
        Id = "notify",
        Title = "مركز الإشعارات",
        Controls =
        [
            Switch("empty", "بلا إشعارات", "EmptyText", "إذا خلت القائمة ظهرت مكانها جملة «لا إشعارات»، وتغيرها في `EmptyText`.", OptionKind.Property),
            Seg("unread", "غير المقروءة", "3",
                Choice("0", "لا شيء"),
                Choice("1", "واحد"),
                Choice("3", "ثلاثة", "IsUnread=\"True\"", "على صف لم يُقرأ: نقطة بلون الهوية في أوله، وخلفية خفيفة، وعنوان أثقل، ويعده الجرس. وتزول حين يُنقر الصف.", OptionKind.Property),
                Choice("many", "120")),
            Seg("style", "أول الصف", "ico",
                Choice("ico", "أيقونة", "Icon", "رقاقة أيقونة تبدأ بها الإشعارات، لما يأتي من النظام.", OptionKind.Property),
                Choice("avatar", "صورة رمزية", "Avatar", "صورة صاحب الإشعار أو حرفه الأول، لما يأتي من شخص.", OptionKind.Property)),
            Switch("read", "زر تعليم الكل", "ReadAllText", "نص زر في رأس اللوحة يعلم الإشعارات كلها مقروءة، وهو «تعليم الكل مقروءًا». اجعله `{x:Null}` فيختفي الزر.", OptionKind.Property, on: true),
            Switch("foot", "رابط عرض الكل", "FootText", "نص رابط في ذيل اللوحة إلى صفحة الإشعارات كلها، يبقى ثابتًا تحت القائمة، ويُطلق `FootClick` إذا نُقر.", OptionKind.Property, on: true),
        ],
        Extra =
        [
            Row("الأساس", "ins:Notify", "جرس يعد ما لم يُقرأ ويكتب عدده على زاويته، وفوق 99 يكتب 99+، ويهتز مرة إذا زاد العدد. ويفتح لوحة فيها الإشعارات، يغلقها `Esc` والنقر خارجها، ويعود التركيز إلى الجرس.", OptionKind.Element, true),
            Row("الأساس", "ins:NotifyItem", "صف إشعار: `Title` و`Description` و`End` للوقت، ويصير مقروءًا حين يُنقر.", OptionKind.Element),
            Row("الأساس", "Title", "عنوان اللوحة، وهو «الإشعارات».", OptionKind.Property),
            Row("الأساس", "IsEnd", "يعلق اللوحة من طرف الجرس الآخر، لجرس في آخر الشريط العلوي.", OptionKind.Property),
            Row("الأساس", "UnreadCount", "عدد غير المقروءة، تقرؤه ولا تكتبه. ويقرأ قارئ الشاشة مع اسم الجرس العدد كاملًا.", OptionKind.Property),
            Row("الأساس", "Read", "يُطلق حين يُعلم إشعار مقروءًا بنقرة، والصف في `Item`، و`All` إذا عُلمت كلها معًا.", OptionKind.Event),
        ],
        Disable = s => s.On("empty") ? ["unread", "style", "read"] : [],
        Axaml = s =>
        {
            var attrs = (s.On("read") && !s.On("empty") ? "" : " ReadAllText=\"{x:Null}\"") + (s.On("foot") ? " FootText=\"عرض الكل\"" : "");
            if (s.On("empty")) return $"<ins:Notify{attrs} />";
            var unread = s["unread"] == "many" ? 3 : int.Parse(s["unread"]);
            var rows = Rows.Select((r, i) => Line(r, i < unread, s["style"] == "avatar")).ToList();
            if (s["unread"] == "many") rows.Add("  <!-- … وغيرها حتى 120 إشعارًا لم يُقرأ -->");
            return $"<ins:Notify{attrs}>\n{string.Join("\n", rows)}\n</ins:Notify>";
        },
        Notes = _ => ["اضغط الجرس لترى اللوحة بخياراتك، وانقر إشعارًا ليصير مقروءًا، أو أضف إشعارًا جديدًا يهتز له الجرس."],
        Stage = (view, s, say) =>
        {
            if (view is not Panel panel || panel.GetLogicalDescendants().OfType<Notify>().FirstOrDefault() is not { } notify) return;
            Geometry? Icon(string key) => panel.TryFindResource(key, out var g) ? g as Geometry : null;
            var avatar = s["style"] == "avatar";
            if (s["unread"] == "many" && !s.On("empty"))
                for (var i = 0; i < 117; i++)
                    notify.Items.Add(new NotifyItem { Icon = avatar ? null : Icon("IconCheck"), Avatar = avatar ? "ل" : null, Title = Rows[2].Title, Description = Rows[2].Desc, End = Rows[2].End, IsUnread = true });
            notify.Read += (_, e) => say(e.All ? "حدث `Read`، و`All` هو `True`" : $"حدث `Read` على «{e.Item?.Title}»");
            var add = new Button { Content = "إشعار جديد", HorizontalAlignment = HorizontalAlignment.Center };
            add.Classes.AddRange(["secondary", "sm"]);
            add.Click += (_, _) => notify.Items.Insert(0, new NotifyItem { Icon = avatar ? null : Icon("IconBell"), Avatar = avatar ? "ن" : null, Title = "إشعار جديد", Description = "وصل للتو.", End = "الآن", IsUnread = true });
            panel.Children.Remove(notify);
            notify.HorizontalAlignment = HorizontalAlignment.Center;
            panel.Children.Add(new StackPanel { Spacing = 16, Children = { notify, add } });
        },
    };

    public static PlayDef[] All { get; } = [Notify];
}
