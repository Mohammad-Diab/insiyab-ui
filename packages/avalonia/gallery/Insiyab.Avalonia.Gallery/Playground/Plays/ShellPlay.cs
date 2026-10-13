using static Insiyab.Avalonia.Gallery.Playground.Play;

namespace Insiyab.Avalonia.Gallery.Playground;

public static class ShellPlay
{
    public const string StructureExample =
        """
        <SplitView ins:Shell.DrawerWidth="600" IsPaneOpen="True" Height="420">
          <SplitView.Pane>
            <DockPanel>
              <Border Classes="shell-brand" DockPanel.Dock="Top">
                <StackPanel>
                  <Border Classes="shell-mark"><TextBlock Text="م" /></Border>
                  <StackPanel>
                    <TextBlock Classes="shell-brand-name" Text="تطبيقي" />
                    <TextBlock Classes="shell-brand-role" Text="الإدارة" />
                  </StackPanel>
                </StackPanel>
              </Border>
              <Border Classes="shell-bottom" DockPanel.Dock="Bottom">
                <Button Classes="shell-link">
                  <StackPanel Orientation="Horizontal">
                    <ins:Icon Data="{StaticResource IconArrow}" />
                    <TextBlock Text="خروج" />
                  </StackPanel>
                </Button>
              </Border>
              <ListBox Classes="shell-nav" SelectedIndex="1">
                <ListBoxItem Classes="shell-group" Content="القسم" />
                <ListBoxItem>
                  <StackPanel Orientation="Horizontal">
                    <ins:Icon Data="{StaticResource IconHome}" />
                    <TextBlock Text="الرئيسية" />
                  </StackPanel>
                </ListBoxItem>
                <ListBoxItem>
                  <DockPanel>
                    <ins:Icon Data="{StaticResource IconCart}" />
                    <Border Classes="shell-link-badge" DockPanel.Dock="Right"><TextBlock Text="8" /></Border>
                    <TextBlock Text="الطلبات" />
                  </DockPanel>
                </ListBoxItem>
              </ListBox>
            </DockPanel>
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
                  <Border Classes="island brand">
                    <StackPanel>
                      <Border Classes="shell-mark"><TextBlock Text="م" /></Border>
                      <TextBlock Text="تطبيقي" />
                    </StackPanel>
                  </Border>
                </StackPanel>
                <StackPanel Classes="zone center">
                  <Border Classes="island title">
                    <StackPanel Classes="shell-head">
                      <TextBlock Classes="shell-title" Text="العنوان" />
                      <TextBlock Classes="shell-sub" Text="سطر فرعي" />
                    </StackPanel>
                  </Border>
                </StackPanel>
                <StackPanel Classes="zone end">
                  <Button Classes="island icon" AutomationProperties.Name="الإشعارات">
                    <ins:Icon Data="{StaticResource IconBell}" />
                  </Button>
                  <Button Classes="island" Content="حسابي">
                    <Button.Flyout>
                      <MenuFlyout Placement="BottomEdgeAlignedRight">
                        <MenuItem Header="الملف الشخصي" />
                        <MenuItem Header="الإعدادات" />
                        <Separator />
                        <MenuItem Header="خروج" />
                      </MenuFlyout>
                    </Button.Flyout>
                  </Button>
                </StackPanel>
              </Grid>
            </Border>
          </Panel>
        </SplitView>
        """;

    public const string DrawerExample =
        """
        <SplitView Width="390" Height="300">
          <SplitView.Pane>
            <ListBox Classes="shell-nav" SelectedIndex="1">
              <ListBoxItem Classes="shell-group" Content="القسم" />
              <ListBoxItem>
                <StackPanel Orientation="Horizontal">
                  <ins:Icon Data="{StaticResource IconHome}" />
                  <TextBlock Text="الرئيسية" />
                </StackPanel>
              </ListBoxItem>
            </ListBox>
          </SplitView.Pane>
          <Border Classes="topbar" VerticalAlignment="Top">
            <Grid ColumnDefinitions="*,Auto,*">
              <StackPanel Classes="zone start">
                <Button Classes="island icon" ins:Shell.TogglesSidebar="True" AutomationProperties.Name="القائمة">
                  <ins:Icon Data="{StaticResource IconMenu}" />
                </Button>
              </StackPanel>
              <StackPanel Classes="zone center">
                <Border Classes="island title">
                  <StackPanel Classes="shell-head"><TextBlock Classes="shell-title" Text="العنوان" /></StackPanel>
                </Border>
              </StackPanel>
            </Grid>
          </Border>
        </SplitView>
        """;

    public static PlayDef Topbar { get; } = new()
    {
        Id = "topbar",
        Title = "الشريط العلوي",
        Wide = _ => true,
        Controls =
        [
            Text("title", "العنوان", "العنوان"),
            Switch("sub", "سطر فرعي", "shell-sub", "على `TextBlock`: سطر اختياري تحت العنوان، لا يظهر إذا كان فارغًا.", on: true),
            Switch("menu", "زر القائمة", "icon", "مع `island`: جزيرة مربعة تتسع لأيقونة واحدة، ولا بد لها عندئذ من `AutomationProperties.Name`.", on: true),
            Seg("end", "في آخر الشريط", "",
                Choice("", "جزر"),
                Choice("pill", "شارة", "ins:Pill", "شارة في إحدى المناطق تأخذ ارتفاع الجزيرة وظلها، وتبقي لونها.", OptionKind.Element),
                Choice("btn", "زر", "Button", "زر في إحدى المناطق يأخذ ارتفاع الجزيرة، فيبقى الشريط سطرًا واحدًا.", OptionKind.Element)),
            Switch("still", "جزيرة لا تُنقر", "Border.island", "جزيرة تحمل معلومة كحالة الشاشة ولا تُنقر، فلا ترتفع تحت المؤشر كما يرتفع الزر.", OptionKind.Element),
        ],
        Extra =
        [
            Row("الأساس", "topbar", "على `Border` فوق المحتوى: شريط شفاف تطفو عليه الجزر، فإذا مر المحتوى تحته عند التمرير صار زجاجًا مصنفرًا. وفيه `Grid` بثلاثة أعمدة `*,Auto,*`.", OptionKind.Class, true),
            Row("الأساس", "zone start", "على `StackPanel`: منطقة البداية، لأدوات الشاشة كزر القائمة.", OptionKind.Class),
            Row("الأساس", "zone center", "المنطقة الوسطى، تتوسط الشريط كله لا ما بقي منه، وفيها العنوان.", OptionKind.Class),
            Row("الأساس", "zone end", "منطقة النهاية، لما يخص المستخدم كالإشعارات وحسابه.", OptionKind.Class),
            Row("الأساس", "island", "على `Button` أو `Border`: جزيرة على هيئة حبة تحمل نصًا أو أي محتوى، فإذا كانت زرًا ارتفعت تحت المؤشر.", OptionKind.Class),
            Row("الأساس", "island title", "تحمل اسم الشاشة في `StackPanel.shell-head`، فإذا طال انتهى بنقاط ولم ينكسر إلى سطرين.", OptionKind.Class),
            Row("الأساس", "shell-title", "على `TextBlock`: اسم الشاشة في جزيرة العنوان، فلا تكرره في المحتوى.", OptionKind.Class),
            Row("في آخر الشريط", "island brand", "تحمل الهوية، ولا تظهر إلا حين يُطوى الشريط الجانبي أو يصير درجًا.", OptionKind.Class),
        ],
        Disable = s => s.Has("end") ? ["still"] : [],
        Axaml = s =>
        {
            var lines = new List<string> { "<Border Classes=\"topbar\">", "  <Grid ColumnDefinitions=\"*,Auto,*\">" };
            if (s.On("menu"))
                lines.AddRange(["    <StackPanel Classes=\"zone start\">", "      <Button Classes=\"island icon\" AutomationProperties.Name=\"القائمة\"><ins:Icon Data=\"{StaticResource IconMenu}\" /></Button>", "    </StackPanel>"]);
            else lines.Add("    <StackPanel Classes=\"zone start\" />");
            lines.AddRange([
                "    <StackPanel Classes=\"zone center\">",
                "      <Border Classes=\"island title\">",
                "        <StackPanel Classes=\"shell-head\">",
                $"          <TextBlock Classes=\"shell-title\" Text=\"{s["title"]}\" />",
            ]);
            if (s.On("sub")) lines.Add("          <TextBlock Classes=\"shell-sub\" Text=\"سطر فرعي\" />");
            lines.AddRange(["        </StackPanel>", "      </Border>", "    </StackPanel>", "    <StackPanel Classes=\"zone end\">"]);
            if (s["end"] == "pill") lines.Add("      <ins:Pill Classes=\"ok\" Content=\"متصل\" />");
            else if (s["end"] == "btn") lines.Add("      <Button Classes=\"primary\" Content=\"حفظ\" />");
            else
            {
                lines.Add("      <Button Classes=\"island icon\" AutomationProperties.Name=\"الإشعارات\"><ins:Icon Data=\"{StaticResource IconBell}\" /></Button>");
                lines.Add(s.On("still") ? "      <Border Classes=\"island\"><TextBlock Text=\"مسودة\" /></Border>" : "      <Button Classes=\"island\" Content=\"حسابي\" />");
            }
            lines.AddRange(["    </StackPanel>", "  </Grid>", "</Border>"]);
            return string.Join("\n", lines);
        },
        Notes = s =>
        {
            var notes = new List<string>();
            if (s.On("menu")) notes.Add("زر القائمة هنا للعرض، فأضف إليه `ins:Shell.TogglesSidebar=\"True\"` ليطوي الشريط الجانبي.");
            if (s.On("still") && !s.Has("end")) notes.Add("مرّر المؤشر على جزيرتي النهاية: الزر يرتفع، والتي لا تُنقر تبقى مكانها.");
            return notes;
        },
    };

    public static PlayDef Sidebar { get; } = new()
    {
        Id = "sidebar",
        Title = "الهيكل والشريط الجانبي",
        Wide = _ => true,
        WideMax = 520,
        Controls =
        [
            Switch("role", "سطر تحت الاسم", "shell-brand-role", "سطر صغير تحت اسم التطبيق.", on: true),
            Switch("group", "عنوان مجموعة", "shell-group", "على `ListBoxItem`: عنوان مجموعة من الروابط، لا يُختار.", on: true),
            Switch("badge", "عداد في الرابط", "shell-link-badge", "على `Border` في آخر الرابط: عداد صغير.", on: true),
            Switch("bottom", "أسفل الشريط", "shell-bottom", "على `Border` في `DockPanel.Dock=\"Bottom\"`: يعزل ما لا يقود إلى شاشة، كالخروج، عن بقية الروابط.", on: true),
            Switch("filter", "حقل تصفية", "ins:Shell.HasFilter", "على `ListBox.shell-nav`: حقل فوق الروابط يخفي منها ما لا يطابق ما تكتبه، ومعها عنوان كل مجموعة لم يبقَ فيها رابط. ولا يفرق بين أشكال الهمزة ولا يلتفت إلى الحركات، ويُنتقل إليه بمفتاح «/».", OptionKind.Property),
            Switch("scrolls", "الرأس يتمرر", "ins:Shell.HeadScrolls", "على `SplitView`: يتمرر رأس الشريط مع روابطه كما في الويب، بدل أن يبقى ثابتًا وتتمرر الروابط وحدها.", OptionKind.Property),
        ],
        Extra =
        [
            Row("الأساس", "SplitView", "الهيكل كله: الشريط الجانبي في `Pane` والعمود الرئيسي في محتواه، ويتبعان اتجاه الواجهة.", OptionKind.Element, true),
            Row("الأساس", "shell-content", "على `Border` في العمود الرئيسي: المحتوى، يملأ ما بقي من العرض.", OptionKind.Class, true),
            Row("الأساس", "ins:Shell.FrostsTopbar", "على `ScrollViewer` المحتوى: إذا تمرر يصير الشريط العلوي بجانبه زجاجًا مصنفرًا.", OptionKind.Property, true),
            Row("الشريط الجانبي", "shell-brand", "على `Border` في أعلى الشريط: الهوية، وفيها `Border.shell-mark` واسم التطبيق `TextBlock.shell-brand-name`.", OptionKind.Class),
            Row("الشريط الجانبي", "shell-nav", "على `ListBox`: روابط الشريط، كل رابط `ListBoxItem` تسبقه أيقونة، ويطير المؤشر من المختار إلى ما تختاره.", OptionKind.Class),
            Row("الشريط الجانبي", "shell-link", "على `Button`: رابط في الشريط خارج القائمة، كالخروج.", OptionKind.Class),
            Row("التصفية", "ins:Shell.FilterPlaceholder", "النص الباهت في حقل التصفية، وهو «بحث» إن لم تذكره.", OptionKind.Property),
            Row("التصفية", "ins:Shell.Keywords", "على `ListBoxItem`: كلمات أخرى يُعثر على الرابط بها، كاسمه بالإنجليزية.", OptionKind.Property),
            Row("التصفية", "ins:Shell.FilterEmptyText", "ما يظهر حين لا يبقى رابط، وهو «لا نتائج» إن لم تذكره.", OptionKind.Property),
            Row("الطي والدرج", "ins:Shell.TogglesSidebar", "على زر يطوي الشريط على الحاسوب، ويفتحه درجًا على الشاشة الضيقة. ومعه `ins:Shell.SidebarAction` بالقيمة `toggle` أو `open` أو `collapsed` أو `closed`.", OptionKind.Property),
            Row("الطي والدرج", "ins:Shell.DrawerWidth", "على `SplitView`: إذا ضاق عن هذا العرض صار الشريط درجًا، وهو 900 إن لم تذكره، و`0` يلغيه.", OptionKind.Property),
            Row("الطي والدرج", "ins:Shell.SidebarChanged", "يُطلق على الهيكل كلما طُوي الشريط أو فُتح. و`Shell.Sidebar(view, \"collapsed\")` تطويه من الشيفرة.", OptionKind.Event),
        ],
        Axaml = s =>
        {
            var lines = new List<string>
            {
                $"<SplitView IsPaneOpen=\"True\" ins:Shell.DrawerWidth=\"0\"{(s.On("scrolls") ? " ins:Shell.HeadScrolls=\"True\"" : "")} Height=\"300\">",
                "  <SplitView.Pane>",
                "    <DockPanel>",
                "      <Border Classes=\"shell-brand\" DockPanel.Dock=\"Top\">",
                "        <StackPanel>",
                "          <Border Classes=\"shell-mark\"><TextBlock Text=\"م\" /></Border>",
            };
            if (s.On("role"))
                lines.AddRange(["          <StackPanel>", "            <TextBlock Classes=\"shell-brand-name\" Text=\"تطبيقي\" />", "            <TextBlock Classes=\"shell-brand-role\" Text=\"الإدارة\" />", "          </StackPanel>"]);
            else lines.Add("          <TextBlock Classes=\"shell-brand-name\" Text=\"تطبيقي\" />");
            lines.AddRange(["        </StackPanel>", "      </Border>"]);
            if (s.On("bottom"))
                lines.AddRange([
                    "      <Border Classes=\"shell-bottom\" DockPanel.Dock=\"Bottom\">",
                    "        <Button Classes=\"shell-link\">",
                    "          <StackPanel Orientation=\"Horizontal\">",
                    "            <ins:Icon Data=\"{StaticResource IconArrow}\" />",
                    "            <TextBlock Text=\"خروج\" />",
                    "          </StackPanel>",
                    "        </Button>",
                    "      </Border>",
                ]);
            lines.Add($"      <ListBox Classes=\"shell-nav\" SelectedIndex=\"{(s.On("group") ? 1 : 0)}\"{(s.On("filter") ? " ins:Shell.HasFilter=\"True\" ins:Shell.FilterPlaceholder=\"ابحث في الشاشات\"" : "")}>");
            if (s.On("group")) lines.Add("        <ListBoxItem Classes=\"shell-group\" Content=\"القسم\" />");
            lines.AddRange([
                "        <ListBoxItem>",
                "          <StackPanel Orientation=\"Horizontal\">",
                "            <ins:Icon Data=\"{StaticResource IconHome}\" />",
                "            <TextBlock Text=\"الرئيسية\" />",
                "          </StackPanel>",
                "        </ListBoxItem>",
                s.On("filter") ? "        <ListBoxItem ins:Shell.Keywords=\"orders\">" : "        <ListBoxItem>",
                "          <DockPanel>",
                "            <ins:Icon Data=\"{StaticResource IconCart}\" />",
            ]);
            if (s.On("badge")) lines.Add("            <Border Classes=\"shell-link-badge\" DockPanel.Dock=\"Right\"><TextBlock Text=\"8\" /></Border>");
            lines.AddRange(["            <TextBlock Text=\"الطلبات\" />", "          </DockPanel>", "        </ListBoxItem>"]);
            lines.AddRange(new[] { "العملاء", "المنتجات", "الفواتير", "التقارير", "الإعدادات" }.Select(t => $"        <ListBoxItem Content=\"{t}\" />"));
            lines.AddRange(["      </ListBox>", "    </DockPanel>", "  </SplitView.Pane>", "  <Border Classes=\"shell-content\" />", "</SplitView>"]);
            return string.Join("\n", lines);
        },
        Notes = s => s.On("filter")
            ? [s.On("scrolls") ? "مرر الروابط، فيتمرر الرأس والحقل معها." : "مرر الروابط، فتتمرر وحدها ويبقى الرأس والحقل في مكانهما.", "اكتب orders في الحقل، فيبقى رابط «الطلبات» وحده."]
            : [s.On("scrolls") ? "مرر الروابط، فيتمرر الرأس معها." : "مرر الروابط، فتتمرر وحدها ويبقى الرأس في مكانه."],
    };

    public static PlayDef[] All { get; } = [Topbar, Sidebar];
}
