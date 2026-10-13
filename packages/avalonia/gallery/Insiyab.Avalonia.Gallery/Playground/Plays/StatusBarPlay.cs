using Avalonia.Controls;
using Avalonia.LogicalTree;
using Insiyab.Avalonia.Controls;
using static Insiyab.Avalonia.Gallery.Playground.Play;

namespace Insiyab.Avalonia.Gallery.Playground;

public static class StatusBarPlay
{
    public const string Example =
        """
        <ins:StatusBar>
          <ins:StatusItem Icon="{StaticResource IconCheck}" Content="جاهز" />
          <Separator />
          <ins:StatusItem Content="12 طلبًا محددًا" />
          <ins:StatusBar.End>
            <ins:SaveStatus State="Saved" />
            <Separator />
            <ins:StatusButton Content="العربية">
              <ins:StatusButton.Flyout>
                <ins:PopMenu IsEnd="True">
                  <MenuItem Header="العربية" />
                  <MenuItem Header="English" />
                </ins:PopMenu>
              </ins:StatusButton.Flyout>
            </ins:StatusButton>
            <ins:StatusButton Icon="{StaticResource IconBell}" Content="3" ToolTip.Tip="الإشعارات" />
          </ins:StatusBar.End>
        </ins:StatusBar>
        """;

    public const string Window =
        """
        <DockPanel Height="380">
          <ins:StatusBar DockPanel.Dock="Bottom">
            <ins:StatusItem IsBusy="True" Content="جارٍ المزامنة…" />
            <ins:StatusBar.End>
              <ins:SaveStatus State="Saved" />
              <ins:StatusButton Icon="{StaticResource IconBell}" Content="3" ToolTip.Tip="الإشعارات" />
            </ins:StatusBar.End>
          </ins:StatusBar>
          <SplitView ins:Shell.DrawerWidth="600" IsPaneOpen="True">
            <SplitView.Pane>
              <ListBox Classes="shell-nav" SelectedIndex="1">
                <ListBoxItem Classes="shell-group" Content="القسم" />
                <ListBoxItem>
                  <StackPanel Orientation="Horizontal">
                    <ins:Icon Data="{StaticResource IconCart}" />
                    <TextBlock Text="الطلبات" />
                  </StackPanel>
                </ListBoxItem>
              </ListBox>
            </SplitView.Pane>
            <Panel>
              <ScrollViewer ins:Shell.FrostsTopbar="True">
                <Border Classes="shell-content" Margin="0,72,0,0" />
              </ScrollViewer>
              <Border Classes="topbar" VerticalAlignment="Top">
                <Grid ColumnDefinitions="*,Auto,*">
                  <StackPanel Classes="zone start">
                    <Button Classes="island icon" ins:Shell.TogglesSidebar="True" AutomationProperties.Name="القائمة">
                      <ins:Icon Data="{StaticResource IconMenu}" />
                    </Button>
                  </StackPanel>
                  <StackPanel Classes="zone center">
                    <Border Classes="island title">
                      <StackPanel Classes="shell-head">
                        <TextBlock Classes="shell-title" Text="الطلبات" />
                      </StackPanel>
                    </Border>
                  </StackPanel>
                </Grid>
              </Border>
            </Panel>
          </SplitView>
        </DockPanel>
        """;

    public static PlayDef StatusBar { get; } = new()
    {
        Id = "statusbar",
        Title = "شريط الحالة",
        Wide = _ => true,
        Controls =
        [
            Select("start", "عنصر البداية", "ready",
                Choice("ready", "نص برمز"),
                Choice("busy", "مهمة جارية", "IsBusy=\"True\"", "على `ins:StatusItem`: دائرة صغيرة تدور مكان الرمز ما دامت المهمة جارية.", OptionKind.Property),
                Choice("progress", "شريط تقدم")),
            Select("save", "حالة الحفظ", "Saved",
                Choice("", "بلا مؤشر"),
                Choice("Saving", "جارٍ الحفظ"),
                Choice("Saved", "حُفظ"),
                Choice("Error", "تعذر")),
            Switch("menu", "عنصر يفتح قائمة", "Flyout", "على `ins:StatusButton`: قائمة `ins:PopMenu` أو بطاقة `ins:PopCard` تنفتح منه، وتصعد إلى أعلى إذا كان في أسفل النافذة.", OptionKind.Property, "العنصر القابل للنقر", true),
            Switch("seps", "فواصل", "Separator", "خط عمودي قصير بين العناصر.", OptionKind.Element, "الأساس", true),
        ],
        Extra =
        [
            Row("الأساس", "ins:StatusBar", "شريط رفيع في أسفل النافذة، فيه عناصر في جهة البداية وأخرى في جهة النهاية. ضعه في `DockPanel` حول هيكل التطبيق بـ`DockPanel.Dock=\"Bottom\"`، فلا يغطي المحتوى.", OptionKind.Element, true),
            Row("الأساس", "ins:StatusBar.End", "عناصر جهة النهاية. وما يوضع في الشريط مباشرة يكون في جهة البداية، وإذا ضاق الشريط قُص من آخرها وبقيت جهة النهاية ظاهرة.", OptionKind.Property),
            Row("العنصر", "ins:StatusItem", "عنصر يخبر بشيء: رمز ثم محتواه، نصًا كان أو أي عنصر.", OptionKind.Element),
            Row("العنصر", "Icon", "رمز العنصر، مثل `{StaticResource IconCheck}`.", OptionKind.Property),
            Row("العنصر القابل للنقر", "ins:StatusButton", "عنصر يُنقر، يظلله المؤشر. وله `Icon` كذلك.", OptionKind.Element),
            Row("العنصر القابل للنقر", "Click", "يُطلق حين يُنقر العنصر، و`Command` يعمل كما في أي زر.", OptionKind.Event),
            Row("عناصر أخرى", "ins:SaveStatus", "مؤشر الحفظ نفسه، بحجم الشريط.", OptionKind.Element),
            Row("عناصر أخرى", "ProgressBar", "شريط تقدم قصير رفيع، بعرض 96.", OptionKind.Element),
        ],
        Axaml = s =>
        {
            var sep = s.On("seps") ? new[] { "<Separator />" } : [];
            var start = s["start"] switch
            {
                "busy" => new[] { "<ins:StatusItem IsBusy=\"True\" Content=\"جارٍ المزامنة…\" />" },
                "progress" => ["<ins:StatusItem Content=\"رفع الملفات\" />", "<ProgressBar Value=\"40\" />"],
                _ => ["<ins:StatusItem Icon=\"{StaticResource IconCheck}\" Content=\"جاهز\" />"],
            };
            var lines = new List<string>(start);
            lines.AddRange(sep);
            lines.Add("<ins:StatusItem Content=\"12 طلبًا محددًا\" />");
            var end = new List<string>();
            if (s.Has("save"))
            {
                end.Add($"<ins:SaveStatus State=\"{s["save"]}\" />");
                end.AddRange(sep);
            }
            if (s.On("menu"))
                end.AddRange([
                    "<ins:StatusButton Content=\"العربية\">",
                    "  <ins:StatusButton.Flyout>",
                    "    <ins:PopMenu IsEnd=\"True\">",
                    "      <MenuItem Header=\"العربية\" />",
                    "      <MenuItem Header=\"English\" />",
                    "    </ins:PopMenu>",
                    "  </ins:StatusButton.Flyout>",
                    "</ins:StatusButton>",
                ]);
            end.Add("<ins:StatusButton Icon=\"{StaticResource IconBell}\" Content=\"3\" ToolTip.Tip=\"الإشعارات\" />");
            return "<ins:StatusBar>\n" + string.Join("\n", lines.Select(l => "  " + l)) +
                "\n  <ins:StatusBar.End>\n" + string.Join("\n", end.Select(l => "    " + l)) + "\n  </ins:StatusBar.End>\n</ins:StatusBar>";
        },
        Stage = (view, _, say) =>
        {
            foreach (var b in view.GetLogicalDescendants().OfType<StatusButton>())
                if (b.Flyout is null) b.Click += (_, _) => say($"حدث `Click` على «{b.Content}»");
        },
    };

    public static PlayDef[] All { get; } = [StatusBar];
}
