using Avalonia.Controls;
using Avalonia.LogicalTree;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using Insiyab.Avalonia.Controls;
using static Insiyab.Avalonia.Gallery.Playground.Play;

namespace Insiyab.Avalonia.Gallery.Playground;

public static class FilePlay
{
    public const string Example =
        """
        <StackPanel Classes="field">
          <TextBlock Classes="label" Text="المرفقات" />
          <ins:FileUpload Multiple="True" Accept=".pdf,image/*" MaxSize="5MB" MaxCount="5" />
          <TextBlock Classes="hint" Text="ملفات PDF أو صور." />
        </StackPanel>
        """;

    public static string Code { get; } =
        """
        Docs.FileAdded += async (_, e) =>
        {
            if (!e.Upload) return;
            try
            {
                var id = await SendAsync(e.Entry, fraction => Docs.Progress(e.Entry, fraction));
                Docs.Done(e.Entry, id);              // يحمل الحقل المعرف مكان الملف
            }
            catch
            {
                Docs.Fail(e.Entry, "تعذر الرفع، أزِل الملف أو أعد إضافته.");
            }
        };

        var files = Docs.Files;          // الملفات في القائمة
        Docs.SetFiles(null);             // أفرغها
        """;

    // The one-pixel picture the docs use, added at once to the "uploads at once" field.
    private const string Pixel = "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mNk+M9QDwADhgGAWjR9awAAAABJRU5ErkJggg==";

    /// <summary>The page's own upload, pretended: it climbs by a fifth every 300 ms, then finishes or fails by the file name.</summary>
    public static void Pretend(FileUpload field, FileEntry entry)
    {
        var fraction = 0.0;
        var timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(300) };
        timer.Tick += (_, _) =>
        {
            fraction += 0.2;
            if (fraction >= 1)
            {
                timer.Stop();
                if (entry.Name.Contains("خطأ")) field.Fail(entry, "تعذر الرفع، أزِل الملف أو أعد إضافته.");
                else field.Done(entry, "photo-" + Random.Shared.Next(1_000_000));
                return;
            }
            if (!field.Progress(entry, fraction)) timer.Stop();
        };
        timer.Start();
    }

    public static PlayDef File { get; } = new()
    {
        Id = "file",
        Title = "منطقة الإفلات",
        Wide = _ => true,
        Controls =
        [
            Switch("compact", "مضغوط", "compact", "على الحقل، إذا ضاق النموذج: زر وقائمة بلا منطقة إفلات، ويبقى الإفلات على الزر ممكنًا."),
            Switch("multiple", "عدة ملفات", "Multiple=\"True\"", "ينضم ما تختاره إلى القائمة، ومن دونها يحل الملف الجديد محل القديم.", OptionKind.Property, on: true),
            Select("accept", "الأنواع المقبولة", ".pdf,image/*",
                Choice("", "كل الأنواع"),
                Choice(".pdf,image/*", "PDF وصور", "Accept", "يفهمه الحقل كما يفهمه منتقي الملفات، ويطبقه على الإفلات واللصق أيضًا.", OptionKind.Property),
                Choice("image/*", "صور فقط")).In("الحدود"),
            Seg("max", "أكبر حجم", "5MB",
                Choice("", "بلا"),
                Choice("500KB", "500KB"),
                Choice("5MB", "5MB", "MaxSize=\"5MB\"", "أكبر حجم للملف الواحد، بـ `B` أو `KB` أو `MB` أو `GB`.", OptionKind.Property)).In("الحدود"),
            Seg("count", "أكبر عدد", "5",
                Choice("", "بلا"),
                Choice("2", "2"),
                Choice("5", "5", "MaxCount=\"5\"", "أكبر عدد للملفات، مع `Multiple`.", OptionKind.Property)).In("الحدود"),
            Text("note", "السطر الصغير", ""),
            Switch("upload", "يُرفع فورًا", "Upload=\"True\"", "يُطلق كل ملف تضيفه `FileAdded` فترفعه الصفحة، ثم تخبر الحقل بما جرى.", OptionKind.Property, "الرفع الفوري"),
            Switch("disabled", "معطل", "IsEnabled=\"False\"", "يبهت الحقل، فلا تُختار فيه ملفات ولا تُفلت.", OptionKind.Property),
        ],
        Extra =
        [
            Row("الأساس", "ins:FileUpload", "منطقة تسحب الملفات إليها أو تختارها أو تلصقها، وقائمة بها تحتها: للصورة نسخة مصغرة ولغيرها نوعه، ومع كل ملف اسمه وحجمه وزر لإزالته.", OptionKind.Element, true),
            Row("الحدود", "Note", "السطر الصغير في المنطقة، مكان ما يكتبه الحقل من الحدود.", OptionKind.Property),
            Row("الرفع الفوري", "FileAdded", "يُطلق لكل ملف يُضاف، والملف في `Entry`، و`Upload` إن كان الرفع للصفحة.", OptionKind.Event),
            Row("الرفع الفوري", "Progress", "تخبر بها الصفحة الحقل كم بلغ الرفع، ثم `Done` بالمعرف أو `Fail` برسالة. ويبقى الحقل غير صالح ما دام ملف قيد الرفع أو تعذر رفعه.", OptionKind.Property),
        ],
        Disable = s => s.On("multiple") ? [] : ["count"],
        Axaml = s =>
        {
            var attrs = Classes(s.On("compact") ? "compact" : null) + (s.On("multiple") ? " Multiple=\"True\"" : "") +
                (s.Has("accept") ? $" Accept=\"{s["accept"]}\"" : "") + (s.Has("max") ? $" MaxSize=\"{s["max"]}\"" : "") +
                (s.Has("count") && s.On("multiple") ? $" MaxCount=\"{s["count"]}\"" : "") + (s.Has("note") ? $" Note=\"{s["note"]}\"" : "") +
                (s.On("upload") ? " Upload=\"True\"" : "") + (s.On("disabled") ? " IsEnabled=\"False\"" : "");
            var hint = s["accept"] switch { "" => "أي نوع من الملفات.", "image/*" => "صور فقط.", _ => "ملفات PDF أو صور." };
            return "<StackPanel Classes=\"field\">\n" +
                "  <TextBlock Classes=\"label\" Text=\"المرفقات\" />\n" +
                $"  <ins:FileUpload{attrs} />\n" +
                $"  <TextBlock Classes=\"hint\" Text=\"{hint}\" />\n" +
                "</StackPanel>";
        },
        Notes = s =>
        {
            var notes = new List<string>();
            if (s.On("upload")) notes.Add("هذه صورة أُضيفت للتو وهي تُرفع. والرفع هنا مصطنع، وكل ملف في اسمه «خطأ» يتعذر رفعه عمدًا.");
            if (!s.On("multiple")) notes.Add("الحقل لملف واحد، فلا معنى لأكبر عدد.");
            return notes;
        },
        Stage = (view, s, say) =>
        {
            foreach (var field in view.GetLogicalDescendants().OfType<FileUpload>())
            {
                field.FileAdded += (_, e) =>
                {
                    say($"حدث `FileAdded` على «{e.Entry.Name}»");
                    if (e.Upload) Pretend(field, e.Entry);
                };
                if (!s.On("upload") || s.On("disabled")) continue;
                var bytes = Convert.FromBase64String(Pixel);
                field.AddFiles([new FileEntry("صورة.png", bytes.Length, () => Task.FromResult<Stream>(new MemoryStream(bytes)), new Bitmap(new MemoryStream(bytes)))]);
            }
        },
    };

    public static PlayDef[] All { get; } = [File];
}
