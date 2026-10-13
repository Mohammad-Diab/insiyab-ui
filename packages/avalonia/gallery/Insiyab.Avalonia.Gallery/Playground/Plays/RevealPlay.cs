using Avalonia.Controls;
using Avalonia.Threading;
using Insiyab.Avalonia.Controls;
using static Insiyab.Avalonia.Gallery.Playground.Play;

namespace Insiyab.Avalonia.Gallery.Playground;

public static class RevealPlay
{
    public const string Example =
        """
        <UniformGrid ins:Entrance.Reveal="True" Columns="4" ColumnSpacing="16" RowSpacing="16">
          <ContentControl Classes="demo-box" Content="1" />
          <ContentControl Classes="demo-box" Content="2" />
          <ContentControl Classes="demo-box" Content="3" />
          <ContentControl Classes="demo-box" Content="4" />
        </UniformGrid>
        <WrapPanel ItemSpacing="12" LineSpacing="12">
          <ContentControl Classes="demo-box" ins:Entrance.Effect="Rise" Content="Rise" />
          <ContentControl Classes="demo-box" ins:Entrance.Effect="Pop" Content="Pop" />
          <ContentControl Classes="demo-box" ins:Entrance.Effect="Slide" Content="Slide" />
        </WrapPanel>
        """;

    public static PlayDef Reveal { get; } = new()
    {
        Id = "reveal",
        Title = "دخول الأبناء متتابعين",
        Wide = _ => true,
        Controls =
        [
            Seg("count", "عدد الأبناء", "4",
                Choice("4", "4"),
                Choice("8", "8")),
        ],
        Extra =
        [
            Row("الأساس", "ins:Entrance.Reveal=\"True\"", "على لوح، فيدخل أبناؤه واحدًا بعد واحد، حتى العاشر منهم. وتتوقف الحركة إذا أطفأ المستخدم المؤثرات.", OptionKind.Property, true),
            Row("الأساس", "ins:Entrance.RevealStep", "المدة بين دخول ابن والذي يليه.", OptionKind.Property),
            Row("الأساس", "ins:Entrance.RevealCount", "كم ابنًا يدخل متتابعًا، وهي عشرة إن لم تذكرها، ثم يدخل الباقون معًا.", OptionKind.Property),
        ],
        Axaml = s =>
            "<UniformGrid ins:Entrance.Reveal=\"True\" Columns=\"4\" ColumnSpacing=\"16\" RowSpacing=\"16\">\n" +
            string.Join("\n", Enumerable.Range(1, int.Parse(s["count"])).Select(i => $"  <ContentControl Classes=\"demo-box\" Content=\"{i}\" />")) +
            "\n</UniformGrid>",
        Notes = _ => ["غيّر أي خيار فتعود الحركة من أولها. والصناديق `demo-box` للمعرض وحده."],
    };

    public static PlayDef Anim { get; } = new()
    {
        Id = "anim",
        Title = "حركة لعنصر بمفرده",
        Controls =
        [
            Seg("anim", "الحركة", "Rise",
                Choice("Rise", "صعود", "ins:Entrance.Effect=\"Rise\"", "يصعد العنصر قليلًا وهو يظهر.", OptionKind.Property),
                Choice("Pop", "نبضة", "ins:Entrance.Effect=\"Pop\"", "يكبر العنصر من أصغر من حجمه بنبضة خفيفة.", OptionKind.Property),
                Choice("Slide", "انزلاق", "ins:Entrance.Effect=\"Slide\"", "ينزلق العنصر من حافة البداية مع اتجاه القراءة.", OptionKind.Property)),
            Text("text", "النص", "عنصر"),
        ],
        Axaml = s => $"<ContentControl Classes=\"demo-box\" ins:Entrance.Effect=\"{s["anim"]}\" Content=\"{s["text"]}\" />",
        Notes = _ => ["تعيد المعاينة الحركة كل ثانيتين، لأن حركة الدخول تجري مرة واحدة."],
        Stage = (view, _, _) =>
        {
            var loop = new DispatcherTimer { Interval = TimeSpan.FromSeconds(2) };
            loop.Tick += (_, _) =>
            {
                if (TopLevel.GetTopLevel(view) is null) loop.Stop();
                else Entrance.Replay(view);
            };
            view.DetachedFromVisualTree += (_, _) => loop.Stop();
            loop.Start();
        },
    };

    public static PlayDef[] All { get; } = [Reveal, Anim];
}
