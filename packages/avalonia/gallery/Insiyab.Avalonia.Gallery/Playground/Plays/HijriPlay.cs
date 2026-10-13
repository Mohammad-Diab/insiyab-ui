using Avalonia.Controls;
using Avalonia.LogicalTree;
using Insiyab.Avalonia.Controls;
using static Insiyab.Avalonia.Gallery.Playground.Play;

namespace Insiyab.Avalonia.Gallery.Playground;

public static class HijriPlay
{
    public const string Example =
        """
        <StackPanel Classes="field">
          <TextBlock Classes="label" Text="التاريخ" />
          <ins:DateField CalendarKind="Hijri" SelectedDate="2026-09-24" />
        </StackPanel>
        """;

    public static string Page { get; } =
        """
        <Application.Styles>
          <Style Selector="ins|DateField">
            <Setter Property="CalendarKind" Value="Hijri" />
          </Style>
        </Application.Styles>
        …
        <ins:DateField />                            <!-- هجري -->
        <ins:DateField CalendarKind="Gregorian" />   <!-- ميلادي -->
        """;

    public static string Code { get; } =
        """
        var iso = DueField.Value;                          // "2026-09-24": ميلادي دائمًا
        DueField.SelectedDate = new DateTime(2026, 10, 1); // يُعطى ميلاديًا ويظهر هجريًا

        DueField.CalendarSwitched += (_, gregorian) =>
            Debug.WriteLine(gregorian);                    // true إذا قُلب إلى الميلادي
        """;

    public static PlayDef Field { get; } = new()
    {
        Id = "field",
        Title = "الحقل الهجري",
        Wide = _ => true,
        WideMax = 416,
        Controls =
        [
            Switch("range", "فترة بين تاريخين", "Mode=\"Range\"", "يختار الحقل فترة، يبدؤها `SelectedDate` وينهيها `EndDate`، ويُرسل طرفاها ميلاديين.", OptionKind.Property),
            Switch("civil", "الحسابي", "CalendarKind=\"HijriCivil\"", "التقويم الهجري الحسابي (الجدولي)، وقد يفترق عن أم القرى بيوم أو يومين.", OptionKind.Property, "التقويم"),
        ],
        Extra =
        [
            Row("التقويم", "CalendarKind=\"Hijri\"", "على `ins:DateField` فيصير هجريًا بتقويم أم القرى، وفي أسفل تقويمه مفتاح يقلبه إلى الميلادي ثم يعيده.", OptionKind.Property, true),
            Row("التقويم", "CalendarKind=\"Gregorian\"", "يبقي حقلًا بعينه ميلاديًا وإن جعلت الحقول كلها هجرية بنمط.", OptionKind.Property),
            Row("التقويم", "ShowGregorian=\"True\"", "يفتح التقويم ميلاديًا والمفتاح مقلوب.", OptionKind.Property),
            Row("التقويم", "Value", "التاريخ كما يُرسل، ميلاديًا بصيغة ISO مثل `2026-09-24`، أيًّا كان التقويم الظاهر.", OptionKind.Property),
            Row("التقويم", "CalendarSwitched", "يُطلق إذا قُلب التقويم بالمفتاح، وقيمته `true` إذا ظهر الميلادي.", OptionKind.Event),
            Row("التقويم", "DateChanged", "يُطلق حين يتغير التاريخ، وفيه القيمة الميلادية.", OptionKind.Event),
        ],
        Axaml = s =>
        {
            var cal = s.On("civil") ? "HijriCivil" : "Hijri";
            var field = s.On("range")
                ? $"<ins:DateField CalendarKind=\"{cal}\" Mode=\"Range\" SelectedDate=\"2026-09-12\" EndDate=\"2026-10-11\" />"
                : $"<ins:DateField CalendarKind=\"{cal}\" SelectedDate=\"2026-09-24\" />";
            return "<StackPanel Classes=\"field\">\n" +
                $"  <TextBlock Classes=\"label\" Text=\"{(s.On("range") ? "الفترة" : "التاريخ")}\" />\n" +
                $"  {field}\n" +
                "</StackPanel>";
        },
        Notes = s =>
        {
            var notes = new List<string> { "اضغط زر التقويم في آخر الحقل لترى التقويم الهجري، وفي أسفله مفتاح يقلبه إلى الميلادي." };
            if (s.On("range")) notes.Add("يُرسل طرفا الفترة ميلاديين، وتأتي رسائلها بالتقويم الظاهر.");
            if (s.On("civil")) notes.Add("قد يختلف اليوم هنا عن أم القرى بيوم.");
            return notes;
        },
        Stage = (view, _, say) =>
        {
            foreach (var f in view.GetLogicalDescendants().OfType<DateField>())
            {
                f.DateChanged += (_, _) => say($"حدث `DateChanged`، و`Value` الآن `{f.Value}`");
                f.CalendarSwitched += (_, g) => say($"حدث `CalendarSwitched`، والميلادي ظاهر: `{g}`");
            }
        },
    };

    public static PlayDef[] All { get; } = [Field];
}
