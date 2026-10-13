using Avalonia.LogicalTree;
using static Insiyab.Avalonia.Gallery.Playground.Play;

namespace Insiyab.Avalonia.Gallery.Playground;

public static class CountdownPlay
{
    public const string Example =
        """
        <ins:Countdown Seconds="600" />
        <ins:Countdown Seconds="75" />
        <ins:Countdown Seconds="200000" />
        """;

    public static PlayDef Countdown { get; } = new()
    {
        Id = "countdown",
        Title = "العد التنازلي",
        Controls =
        [
            Select("from", "الوقت", "600",
                Choice("600", "عشر دقائق"),
                Choice("75", "دقيقة وربع"),
                Choice("10", "عشر ثوانٍ"),
                Choice("200000", "أيام"),
                Choice("date", "حتى لحظة", "Until", "لحظة الانتهاء بدل `Seconds`، مثل `2028-01-01T00:00:00+03:00`.", OptionKind.Property)),
            Seg("warn", "التحذير قبل الصفر بـ", "",
                Choice("", "دقيقة"),
                Choice("900", "ربع ساعة", "Warn", "كم ثانية قبل الصفر يتلون العداد بالأصفر، وهي دقيقة إن لم تذكرها.", OptionKind.Property)),
            Seg("size", "المقاس", "",
                Choice("", "عادي"),
                Choice("lg", "كبير", "lg", "عداد أكبر، إذا كان العد محور الشاشة.")),
            Switch("paused", "يبدأ عند الطلب", "StartPaused", "يعرض العداد وقته كاملًا وينتظر، ثم تتحكم فيه بأزرار تربطها بأوامره.", OptionKind.Property, "البدء والإيقاف"),
            Switch("away", "يتوقف في الخلفية", "PauseAway", "يتوقف العداد ما دامت نافذته غير نشطة أو مصغرة، ثم يكمل حين تعود، فلا يُحسب إلا الوقت الذي يقضيه صاحبه أمامه.", OptionKind.Property),
        ],
        Extra =
        [
            Row("الأساس", "ins:Countdown", "يعد الوقت الباقي ثانية بعد ثانية، فيتلون بالأصفر حين يقترب الصفر، ثم بالأحمر عنده. والساعة تُكتب من اليسار دائمًا، والأيام قبلها بالكلمات.", OptionKind.Element, true),
            Row("الأساس", "Seconds", "الثواني التي يعدها، من لحظة ظهوره. وإذا غيّرتها بدأ من القيمة الجديدة.", OptionKind.Property, true),
            Row("الأساس", "StateChanged", "يُطلق مرتين: حين يبلغ العداد حد التحذير، وعند الصفر. ومعه الثواني الباقية والحالة `Warn` أو `End`.", OptionKind.Event),
            Row("البدء والإيقاف", "StartCommand", "يبدؤه، أو يكمله من حيث توقف. ومن الشيفرة `Start()`.", OptionKind.Property),
            Row("البدء والإيقاف", "StopCommand", "يوقفه ويبقي الوقت الباقي ظاهرًا. ومن الشيفرة `Stop()`، وتعيد الثواني الباقية.", OptionKind.Property),
            Row("البدء والإيقاف", "RestartCommand", "يعيده من أوله، وهو ما يحتاجه زر «أعد إرسال الرمز». ومن الشيفرة `Restart()`.", OptionKind.Property),
        ],
        Axaml = s =>
        {
            var attrs = Classes(s["size"]) +
                (s["from"] == "date" ? " Until=\"2028-01-01T00:00:00+03:00\"" : $" Seconds=\"{s["from"]}\"") +
                (s.Has("warn") ? $" Warn=\"{s["warn"]}\"" : "") + (s.On("away") ? " PauseAway=\"True\"" : "");
            if (!s.On("paused")) return $"<ins:Countdown{attrs} />";
            return "<WrapPanel ItemSpacing=\"8\" LineSpacing=\"8\">\n" +
                $"  <ins:Countdown x:Name=\"Timer\"{attrs} StartPaused=\"True\" />\n" +
                "  <Button Classes=\"primary sm\" Content=\"ابدأ\" Command=\"{Binding #Timer.StartCommand}\" VerticalAlignment=\"Center\" />\n" +
                "  <Button Classes=\"secondary sm\" Content=\"أوقف\" Command=\"{Binding #Timer.StopCommand}\" VerticalAlignment=\"Center\" />\n" +
                "  <Button Classes=\"bare sm\" Content=\"من جديد\" Command=\"{Binding #Timer.RestartCommand}\" VerticalAlignment=\"Center\" />\n" +
                "</WrapPanel>";
        },
        Notes = s =>
        {
            var notes = new List<string>();
            if (s["from"] == "date") notes.Add("القيمة لحظة بعينها، فيعد العداد إليها أيامًا وساعات.");
            if (s.On("away")) notes.Add("انتقل إلى نافذة أخرى ثم عد بعد لحظات، فتجد العداد وقف حيث تركته ثم أكمل.");
            if (s.On("paused")) notes.Add("اضغط «ابدأ» فيبدأ العد، و«أوقف» فيقف، و«من جديد» فيعيده من أوله.");
            else if (s["from"] is "10" or "75") notes.Add("انتظر قليلًا، فيتلون العداد حين يقترب الصفر ويظهر هنا الحدث.");
            return notes;
        },
        Stage = (view, _, say) =>
        {
            foreach (var c in view.GetLogicalDescendants().OfType<Controls.Countdown>())
                c.StateChanged += (_, e) => say($"حدث `StateChanged`، والحالة `{e.State}`");
        },
    };

    public static PlayDef[] All { get; } = [Countdown];
}
