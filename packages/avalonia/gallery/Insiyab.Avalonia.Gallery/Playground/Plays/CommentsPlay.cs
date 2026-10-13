using System.Windows.Input;
using Avalonia.Controls;
using Avalonia.LogicalTree;
using Insiyab.Avalonia.Controls;
using static Insiyab.Avalonia.Gallery.Playground.Play;
using InsComment = Insiyab.Avalonia.Controls.Comment;

namespace Insiyab.Avalonia.Gallery.Playground;

public static class CommentsPlay
{
    public const string Example =
        """
        <StackPanel Classes="thread">
          <ins:Comment Avatar="ل" Author="ليلى" Time="قبل ساعتين" Content="العنوان يحتاج تعديلًا بسيطًا.">
            <ins:Comment.Replies>
              <ins:Comment Avatar="ع" Author="عمر" Time="قبل ساعة" Content="عدلته، وهو جاهز الآن." />
            </ins:Comment.Replies>
          </ins:Comment>
          <ins:Comment Avatar="س" Author="سارة" Time="قبل دقائق" Content="شكرًا لكما." />
        </StackPanel>
        """;

    public static PlayDef Comment { get; } = new()
    {
        Id = "comment",
        Title = "التعليق",
        Wide = _ => true,
        Controls =
        [
            Switch("own", "رسالتك أنت", "own", "رسالة من يقرأ في المحادثة: تنتقل إلى الجهة المقابلة، ويأخذ الاسم لون الهوية."),
            Switch("actions", "إجراءات", "Actions", "صف أزرار تحت النص، كالرد. والأزرار `bare` فيه تهدأ حتى يمر عليها المؤشر.", OptionKind.Property, on: true),
            Switch("menu", "قائمة بالنقر الأيمن", "CopyCommand", "ينسخ ما حُدد من النص، أو النص كله إن لم يُحدد شيء. اجعله `Command` عنصر النسخ في قائمة التعليق `ContextMenu`.", OptionKind.Property),
            Text("text", "النص", "راجعت الملف، والعنوان يحتاج تعديلًا بسيطًا."),
        ],
        Extra =
        [
            Row("الأساس", "ins:Comment", "كلام شخص واحد: صورته، ثم اسمه ووقته، ثم النص.", OptionKind.Element, true),
            Row("الأساس", "Avatar", "ما في القرص: حرف أول، أو صورة تملؤه.", OptionKind.Property),
            Row("الأساس", "Author", "اسم الكاتب.", OptionKind.Property),
            Row("الأساس", "Time", "الوقت كما يُقرأ، مثل «قبل ساعتين» أو «10:05»، وتسبقه نقطة تفصله عن الاسم.", OptionKind.Property),
            Row("الأساس", "Content", "النص نفسه، أو ما شئت من عناصر.", OptionKind.Property),
        ],
        Axaml = s =>
        {
            var (av, name) = s.On("own") ? ("أ", "أنت") : ("ل", "ليلى");
            var head = $"<ins:Comment{Classes(s.On("own") ? "own" : null)} Avatar=\"{av}\" Author=\"{name}\" Time=\"قبل ساعتين\" Content=\"{s["text"]}\"";
            var parts = new List<string>();
            if (s.On("actions"))
                parts.Add("  <ins:Comment.Actions>\n" +
                    "    <StackPanel Orientation=\"Horizontal\" Spacing=\"2\">\n" +
                    "      <Button Classes=\"bare sm\" Content=\"رد\" />\n" +
                    "    </StackPanel>\n" +
                    "  </ins:Comment.Actions>");
            if (s.On("menu"))
                parts.Add("  <ins:Comment.ContextMenu>\n" +
                    "    <ContextMenu>\n" +
                    "      <MenuItem Header=\"رد\" />\n" +
                    "      <MenuItem x:Name=\"CopyItem\" Header=\"نسخ\" InputGesture=\"Ctrl+C\" />\n" +
                    "      <Separator />\n" +
                    "      <MenuItem Classes=\"bad\" Header=\"حذف\" />\n" +
                    "    </ContextMenu>\n" +
                    "  </ins:Comment.ContextMenu>");
            return parts.Count == 0 ? head + " />" : head + ">\n" + string.Join("\n", parts) + "\n</ins:Comment>";
        },
        Notes = s => s.On("menu")
            ? ["انقر التعليق بالزر الأيمن لترى قائمته، وفيها النسخ. ويصله في الشيفرة `CopyItem.Command = comment.CopyCommand`."]
            : [],
        Stage = (view, s, _) =>
        {
            if (!s.On("menu")) return;
            foreach (var c in view.GetLogicalDescendants().OfType<InsComment>())
                if (c.ContextMenu?.Items.OfType<MenuItem>().FirstOrDefault(m => m.Name == "CopyItem") is { } copy)
                    copy.Command = c.CopyCommand;
        },
    };

    private static string One(string pad, string? cls, string av, string name, string time, string text, string inside = "")
    {
        var head = $"{pad}<ins:Comment{Classes(cls)} Avatar=\"{av}\" Author=\"{name}\" Time=\"{time}\" Content=\"{text}\"";
        return inside.Length == 0 ? head + " />" : $"{head}>\n{inside}\n{pad}</ins:Comment>";
    }

    public static PlayDef Thread { get; } = new()
    {
        Id = "thread",
        Title = "السلسلة",
        Wide = _ => true,
        Controls =
        [
            Switch("chat", "محادثة"),
            Switch("replies", "ردود متداخلة", "Replies", "ردود التعليق: يزيحها تحته إلى الداخل، ويجعل بجانبها خطًا ينزل من صورة صاحبه.", OptionKind.Property, on: true),
            Switch("box", "مربع للرد", "ins:Comment.SubmitCommand", "على `TextBox` الرد: `Ctrl+Enter` يرسل النص إلى هذا الأمر، و`Enter` وحده يبدأ سطرًا جديدًا.", OptionKind.Property, "مربع للرد"),
        ],
        Extra =
        [
            Row("الأساس", "thread", "على `StackPanel`: يرص التعليقات بعضها فوق بعض، بمسافة بينها.", OptionKind.Class, true),
        ],
        Disable = s => s.On("chat") ? ["replies"] : [],
        Axaml = s =>
        {
            string body;
            if (s.On("chat"))
                body = One("  ", null, "ل", "ليلى", "10:02", "هل وصل الملف؟") + "\n" + One("  ", "own", "أ", "أنت", "10:05", "نعم، وصل صباحًا.");
            else
            {
                var reply = s.On("replies")
                    ? "    <ins:Comment.Replies>\n" + One("      ", null, "ع", "عمر", "قبل ساعة", "عدلته، وهو جاهز الآن.") + "\n    </ins:Comment.Replies>"
                    : "";
                body = One("  ", null, "ل", "ليلى", "قبل ساعتين", "العنوان يحتاج تعديلًا بسيطًا.", reply) +
                    (s.On("replies") ? "" : "\n" + One("  ", null, "ع", "عمر", "قبل ساعة", "عدلته، وهو جاهز الآن.")) +
                    "\n" + One("  ", null, "س", "سارة", "قبل دقائق", "شكرًا لكما.");
            }
            var thread = $"<StackPanel{(s.On("box") ? " x:Name=\"Thread\"" : "")} Classes=\"thread\">\n{body}\n</StackPanel>";
            if (!s.On("box")) return thread;
            return "<StackPanel Spacing=\"16\">\n" +
                string.Join("\n", thread.Split('\n').Select(l => "  " + l)) + "\n" +
                "  <DockPanel>\n" +
                "    <Button DockPanel.Dock=\"Right\" Classes=\"primary\" Content=\"أرسل\" Margin=\"8,0,0,0\" VerticalAlignment=\"Bottom\" ToolTip.Tip=\"Ctrl+Enter\" />\n" +
                "    <TextBox AcceptsReturn=\"True\" TextWrapping=\"Wrap\" PlaceholderText=\"اكتب ردك\" />\n" +
                "  </DockPanel>\n" +
                "</StackPanel>";
        },
        Notes = s => s.On("box") ? ["اكتب ردًا واضغط `Ctrl+Enter` أو «أرسل»، فيُضاف إلى السلسلة رسالةً منك. ويصل المربع بأمر الإرسال في الشيفرة."] : [],
        Stage = (view, s, _) =>
        {
            if (!s.On("box")) return;
            var all = view.GetLogicalDescendants().ToList();
            var thread = all.OfType<StackPanel>().FirstOrDefault(p => p.Name == "Thread");
            var box = all.OfType<TextBox>().FirstOrDefault();
            var send = all.OfType<Button>().FirstOrDefault(b => b.Content as string == "أرسل");
            if (thread is null || box is null || send is null) return;
            void Post(string text)
            {
                if (text.Trim().Length == 0) return;
                var mine = new InsComment { Avatar = "أ", Author = "أنت", Time = "الآن", Content = text.Trim() };
                mine.Classes.Add("own");
                thread.Children.Add(mine);
                box.Text = "";
            }
            InsComment.SetSubmitCommand(box, new Run(Post));
            send.Click += (_, _) => Post(box.Text ?? "");
        },
    };

    private sealed class Run(Action<string> run) : ICommand
    {
        public event EventHandler? CanExecuteChanged { add { } remove { } }

        public bool CanExecute(object? parameter) => true;

        public void Execute(object? parameter) => run(parameter as string ?? "");
    }

    public static PlayDef[] All { get; } = [Comment, Thread];
}
