using Avalonia.Controls;
using Avalonia.LogicalTree;
using Insiyab.Avalonia.Controls;
using static Insiyab.Avalonia.Gallery.Playground.Play;

namespace Insiyab.Avalonia.Gallery.Playground;

public static class ColorpickerPlay
{
    public const string Example =
        """
        <StackPanel Classes="field">
          <TextBlock Classes="label" Text="لون الهوية" />
          <ins:ColorField Value="#4c1d95" />
        </StackPanel>
        """;

    public static string Code { get; } =
        """
        Brand.ColorCommitted += (_, e) =>
            InsiyabTheme.SetPrimary(e.Value);    // لون التطبيق كله باللون المختار

        var color = Brand.Value;                 // اللون، و Brand.Hex رمزه
        Brand.Value = Color.Parse("#0f766e");    // ضبطه بهدوء، دون حدث
        """;

    public static PlayDef Color { get; } = new()
    {
        Id = "color",
        Title = "حقل اللون",
        Wide = _ => true,
        WideMax = 416,
        Controls =
        [
            Select("swatches", "الألوان الجاهزة", "",
                Choice("", "لون الهوية وما حوله"),
                Choice("set", "ألوان محددة", "Swatches=\"#15803d,#a16207,#b91c1c\"", "الألوان الجاهزة في المنتقي، مكان لون الهوية ولوحة الألوان حوله.", OptionKind.Property),
                Choice("none", "بلا ألوان جاهزة", "Swatches=\"none\"", "يخفي الألوان الجاهزة من المنتقي.", OptionKind.Property)),
            Switch("disabled", "معطل", "IsEnabled=\"False\"", "يتعطل الحقل وعينته، فلا يُفتح المنتقي.", OptionKind.Property),
        ],
        Extra =
        [
            Row("الأساس", "ins:ColorField", "حقل لرمز hex أمامه عينة تفتح المنتقي، وفي أسفل المنتقي نص بهذا اللون على الأبيض وعلى الأسود ونسبة تباينه.", OptionKind.Element, true),
            Row("الأساس", "Value", "اللون، ورمزه `#rrggbb` في `Hex`.", OptionKind.Property),
            Row("الأساس", "ColorCommitted", "يُطلق حين يستقر اللون، واللون في `Value`.", OptionKind.Event),
        ],
        Axaml = s =>
        {
            var list = s["swatches"] switch { "set" => " Swatches=\"#15803d,#a16207,#b91c1c\"", "none" => " Swatches=\"none\"", _ => "" };
            return "<StackPanel Classes=\"field\">\n" +
                "  <TextBlock Classes=\"label\" Text=\"اللون\" />\n" +
                $"  <ins:ColorField Value=\"#0f766e\"{list}{(s.On("disabled") ? " IsEnabled=\"False\"" : "")} />\n" +
                "</StackPanel>";
        },
        Notes = s => s.On("disabled") ? [] : ["اضغط العينة لترى المنتقي بخياراتك."],
        Stage = (view, _, say) =>
        {
            foreach (var f in view.GetLogicalDescendants().OfType<ColorField>())
                f.ColorCommitted += (_, e) => say($"حدث `ColorCommitted`، واللون `{e.Hex}`");
        },
    };

    /// <summary>The main example's field sets the gallery's brand colour.</summary>
    public static Action<Control> ExampleStage { get; } = view =>
        view.GetLogicalDescendants().OfType<ColorField>().First().ColorCommitted += (_, e) => InsiyabTheme.SetPrimary(e.Value);

    public static PlayDef[] All { get; } = [Color];
}
