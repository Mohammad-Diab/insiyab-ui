using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.LogicalTree;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Insiyab.Avalonia.Controls;
using static Insiyab.Avalonia.Gallery.Playground.Play;

namespace Insiyab.Avalonia.Gallery.Playground;

public static class MediaPlay
{
    // An app's own button in the row: a chapters list, the row's one glyph size.
    private const string ChaptersIcon = "M8 6h13 M8 12h13 M8 18h13 M3 6h.01 M3 12h.01 M3 18h.01";

    private static readonly string[] BarLines =
    [
        "<ins:MediaControlBar x:Name=\"Controls\" ShowFullScreen=\"False\">",
        "  <ins:MediaControlBar.SubtitlesCard>",
        "    <StackPanel Spacing=\"12\" MaxWidth=\"280\">",
        "      <TextBlock Classes=\"label\" Text=\"الترجمة\" />",
        "      <TextBlock Text=\"أضف ملف ترجمة من جهازك، فيظهر سطرها فوق الصورة.\" TextWrapping=\"Wrap\" />",
        "      <Button Classes=\"sm\" Content=\"إضافة ملف\" />",
        "    </StackPanel>",
        "  </ins:MediaControlBar.SubtitlesCard>",
        "  <ins:MediaControlBar.EndItems>",
        "    <ToggleButton Classes=\"bare icon media-btn\" ToolTip.Tip=\"الفصول\" AutomationProperties.Name=\"الفصول\">",
        $"      <ins:Icon Classes=\"media-ico\" Data=\"{ChaptersIcon}\" />",
        "    </ToggleButton>",
        "  </ins:MediaControlBar.EndItems>",
        "  <ins:MediaControlBar.MoreItems>",
        "    <MenuItem Header=\"نسخ رابط هذه اللحظة\" />",
        "    <MenuItem Header=\"تنزيل\" />",
        "    <MenuItem Header=\"اختصارات لوحة المفاتيح\" />",
        "  </ins:MediaControlBar.MoreItems>",
        "</ins:MediaControlBar>",
    ];

    public static string Example { get; } =
        "<Panel Height=\"240\">\n" +
        "  <ins:MediaSurface />\n" +
        "  <Border x:Name=\"Line\" Classes=\"media-caption\" VerticalAlignment=\"Bottom\" HorizontalAlignment=\"Center\" Margin=\"16\" IsVisible=\"False\">\n" +
        "    <TextBlock Text=\"وهنا تبدأ الحكاية\" />\n" +
        "  </Border>\n" +
        "</Panel>\n" +
        "<ins:MediaSeekRow x:Name=\"Seek\" Value=\"0.33\" Buffered=\"0.6\" Duration=\"0:4:00\" />\n" +
        string.Join("\n", BarLines);

    /// <summary>The example's stage: a still scene on the surface, the time beside the bar following a seek and a skip, the fill and subtitles buttons acting on the picture.</summary>
    public static void StageExample(Control view)
    {
        foreach (var surface in view.GetLogicalDescendants().OfType<MediaSurface>()) Paint(surface);
        var seek = view.GetLogicalDescendants().OfType<MediaSeekRow>().FirstOrDefault();
        var screen = view.GetLogicalDescendants().OfType<MediaSurface>().FirstOrDefault();
        var line = view.GetLogicalDescendants().OfType<Border>().FirstOrDefault(b => b.Name == "Line");
        foreach (var controls in view.GetLogicalDescendants().OfType<MediaControlBar>())
        {
            controls.SkipRequested += (_, e) =>
            {
                if (seek is null || seek.Duration <= TimeSpan.Zero) return;
                seek.Value = Math.Clamp(seek.Value + e.Offset.TotalSeconds / seek.Duration.TotalSeconds, 0, 1);
            };
            controls.PropertyChanged += (_, e) =>
            {
                if (e.Property == MediaControlBar.IsFillingFrameProperty && screen is not null)
                    screen.Stretch = controls.IsFillingFrame ? global::Avalonia.Media.Stretch.UniformToFill : global::Avalonia.Media.Stretch.Uniform;
                else if (e.Property == MediaControlBar.IsSubtitlesOnProperty && line is not null)
                    line.IsVisible = controls.IsSubtitlesOn;
            };
        }
    }

    public static PlayDef MediaBarPlay { get; } = new()
    {
        Id = "media-bar",
        Title = "شريط الوقت",
        Controls =
        [
            Seg("marks", "العلامات", "",
                Choice("", "بلا"),
                Choice("marks", "فصول", "Markers", "علامات فوق الشريط، كبدايات الفصول. لكل علامة مكانها في `At` من 0 إلى 1، وكلامها في `Tip` يظهر بعد الوقت حين يمر المؤشر عليها، ولونها في `Tone`.", OptionKind.Property)),
            Switch("current", "الفصل الحالي", "CurrentMarker", "رقم العلامة التي تُرسم بلون العلامة التجارية وأطول من غيرها، كالفصل الذي يُعرض الآن، و-1 لا شيء.", OptionKind.Property, "العلامات"),
            Switch("buffered", "ما حُمّل", "Buffered", "إلى أين وصل التحميل، من 0 إلى 1، بلون أغمق من المسار وأخف من الجزء الذي شوهد.", OptionKind.Property, "التقدم"),
            Switch("lead", "بداية لا تُشغَّل", "Lead", "جزء في أول الشريط لا يمكن تشغيله، كبث بدأ قبل أن تنضم إليه: يُرسم أرق وعند نهايته خط قائم، ولا ينتقل إليه نقر ولا سحب، وتقول فقاعته ما في `LeadTip`.", OptionKind.Property, "التقدم"),
            Switch("off", "معطّل", "IsEnabled=\"False\"", "شريط باهت لا يستجيب، كأثناء فتح الملف.", OptionKind.Property),
        ],
        Extra =
        [
            Row("الأساس", "ins:MediaBar", "شريط وقت المشغل، من اليسار إلى اليمين دائمًا كما في كل مشغل، وفي الصفحة العربية أيضًا. يتبع المؤشر بفقاعة فيها الوقت، وينتقل بالنقر والسحب والأسهم.", OptionKind.Element, true),
            Row("الأساس", "Value", "مكان التشغيل من 0 إلى 1، وتحركه النقرة والسحب والأسهم. وأثناء السحب يرسم الشريط مكان السحب لا ما يقوله المشغل.", OptionKind.Property, true),
            Row("الأساس", "Duration", "طول المقطع، ومنه الوقت في الفقاعة وخطوات الأسهم وما يقرؤه قارئ الشاشة.", OptionKind.Property, true),
            Row("الأساس", "SeekRequested", "يُطلق عند النقر وفي كل حركة من السحب وعند نهايته ومع كل مفتاح، وفيه `Value` و`Time` و`IsDragging`، فينقل المشغل التشغيل إليه.", OptionKind.Event),
            Row("المفاتيح", "SmallStep", "خطوة السهمين، خمس ثوانٍ إن لم تذكرها، وعُشر الشريط بلا مدة. و`LargeStep` خطوة Page Up وPage Down، نصف دقيقة إن لم تذكرها. وHome وEnd إلى الطرفين.", OptionKind.Property),
            Row("المفاتيح", "MediaBar.Clock", "يكتب الوقت كما تكتبه المشغلات بأرقام لاتينية: `4:05`، ومن ساعة فأكثر `1:02:09`.", OptionKind.Property),
        ],
        Axaml = s =>
        {
            var attrs = " Value=\"0.42\" Duration=\"0:12:30\"" +
                (s.On("buffered") ? " Buffered=\"0.7\"" : "") +
                (s.On("lead") ? " Lead=\"0.18\" LeadTip=\"لم تكن قد انضممت بعد\"" : "") +
                (s.On("current") ? " CurrentMarker=\"1\"" : "") +
                (s.On("off") ? " IsEnabled=\"False\"" : "");
            if (!s.Has("marks")) return $"<ins:MediaBar Width=\"420\"{attrs} />";
            return $"<ins:MediaBar Width=\"420\"{attrs}>\n" +
                "  <ins:MediaBar.Markers>\n" +
                "    <ins:MediaMarker At=\"0.2\" Tip=\"المقدمة\" />\n" +
                "    <ins:MediaMarker At=\"0.4\" Tip=\"الفكرة\" />\n" +
                "    <ins:MediaMarker At=\"0.75\" Tip=\"مثال\" Tone=\"ok\" />\n" +
                "  </ins:MediaBar.Markers>\n" +
                "</ins:MediaBar>";
        },
        Disable = s => s.Has("marks") ? [] : ["current"],
        Notes = _ => ["مرر المؤشر على الشريط ليظهر الوقت، ثم انقر أو اسحب، أو انقر عليه واستعمل الأسهم وHome وEnd."],
        Stage = (view, _, say) =>
        {
            foreach (var bar in view.GetLogicalDescendants().OfType<MediaBar>())
                bar.SeekRequested += (_, e) => say(e.IsDragging ? $"`SeekRequested` أثناء السحب إلى `{MediaBar.Clock(e.Time)}`" : $"`SeekRequested` إلى `{MediaBar.Clock(e.Time)}`");
        },
    };

    public static PlayDef LivePlay { get; } = new()
    {
        Id = "media-seek-row",
        Title = "صف الوقت",
        Controls =
        [
            Switch("live", "بث مباشر", "IsLive", "مكان الطول في آخر الصف يصير «مباشر»، ويبقى بعرض واحد فلا يتحرك الشريط.", OptionKind.Property, on: true),
            Switch("behind", "متأخر عن المباشر", "Behind", "كم يتأخر التشغيل عن المباشر. ومن `BehindFrom` (ثلاث ثوانٍ إن لم تذكرها) فأكثر يظهر زر العودة إلى المباشر ومعه مقدار التأخر.", OptionKind.Property, on: true),
            Switch("target", "قبل المباشر بقليل", "LiveTarget", "إلى أين يأخذ زر العودة: المباشر نفسه إن لم تذكره، أو وقت قبله، كعشر ثوانٍ.", OptionKind.Property),
        ],
        Extra =
        [
            Row("الأساس", "ins:MediaSeekRow", "الوقت الذي شوهد، ثم `ins:MediaBar`، ثم الطول، من اليسار إلى اليمين في كل لغة. وكل وقت في مكان بعرض أطول صيغة له، فلا يهتز الشريط حين تتغير الأرقام.", OptionKind.Element, true),
            Row("الأساس", "Value", "مكان التشغيل من 0 إلى 1، في الاتجاهين، ويمر إلى الشريط مع `Duration` و`Buffered` و`Markers` و`Lead`. وأحداث الشريط تصل من الصف، كـ `SeekRequested`.", OptionKind.Property, true),
            Row("المباشر", "LiveRequested", "يُطلق حين يُنقر زر العودة إلى المباشر، وفيه `Behind`: كم قبل المباشر يُراد الذهاب، من `LiveTarget`.", OptionKind.Event),
        ],
        Axaml = s =>
            "<ins:MediaSeekRow Width=\"520\" Value=\"" + (s.On("live") && s.On("behind") ? "0.82" : s.On("live") ? "1" : "0.42") + "\" Buffered=\"0.9\" Duration=\"0:12:30\"" +
            (s.On("live") ? " IsLive=\"True\"" : "") +
            (s.On("live") && s.On("behind") ? " Behind=\"0:0:42\"" : "") +
            (s.On("live") && s.On("target") ? " LiveTarget=\"0:0:10\"" : "") + " />",
        Disable = s => s.On("live") ? [] : ["behind", "target"],
        Notes = _ => ["اسحب الشريط وراقب الوقت على اليسار: لا يتحرك الشريط مهما تغيرت الأرقام."],
        Stage = (view, _, say) =>
        {
            foreach (var row in view.GetLogicalDescendants().OfType<MediaSeekRow>())
                row.LiveRequested += (_, e) => say(e.Behind == TimeSpan.Zero ? "`LiveRequested` إلى المباشر" : $"`LiveRequested` إلى ما قبل المباشر بـ `{MediaBar.Clock(e.Behind)}`");
        },
    };

    public static PlayDef Surface { get; } = new()
    {
        Id = "media-surface",
        Title = "سطح الصورة",
        Controls =
        [
            Seg("stretch", "الملاءمة", "",
                Choice("", "تظهر كلها"),
                Choice("UniformToFill", "تملؤه", "Stretch=\"UniformToFill\"", "تملأ الصورة السطح ويُقص ما زاد منها، وتبقى بنسبتها.", OptionKind.Property),
                Choice("Fill", "تمتد", "Stretch=\"Fill\"", "تمتد الصورة على السطح كله وإن تغيرت نسبتها.", OptionKind.Property)),
            Switch("mirror", "معكوسة", "IsMirrored", "يقلب الصورة يمينًا ويسارًا، ككاميرا تعرض صاحبها. أما اتجاه الصفحة فلا يقلبها أبدًا.", OptionKind.Property),
            Switch("caption", "ترجمة فوقها", "media-caption", "شريط ترجمة على `Border` فوق الصورة: أرضية داكنة وخط أبيض عريض، في الوضعين.", OptionKind.Class, "الطبقات"),
        ],
        Extra =
        [
            Row("الأساس", "ins:MediaSurface", "صورة أو إطارات فيديو على أرضية المشهد الداكنة، تظهر كلها في وسطه إن لم تذكر غير ذلك.", OptionKind.Element, true),
            Row("الأساس", "Source", "صورة ثابتة تظهر ما لم يُكتب إطار.", OptionKind.Property, true),
            Row("الأساس", "WriteFrame", "من الشيفرة: `surface.WriteFrame(pixels, width, height)` يرسم إطار فيديو من بكسلات BGRA، ولا تُصنع صورته من جديد إلا إذا تغير مقاسه. و`ClearFrame()` يمحوه.", OptionKind.Property),
            Row("الطبقات", "media-note", "ملاحظة داكنة فوق صورة، كاسم أو رقم، بحبر فاتح في الوضعين.", OptionKind.Class),
        ],
        Axaml = s =>
        {
            var attrs = (s.Has("stretch") ? $" Stretch=\"{s["stretch"]}\"" : "") + (s.On("mirror") ? " IsMirrored=\"True\"" : "");
            if (!s.On("caption")) return $"<ins:MediaSurface Width=\"420\" Height=\"236\"{attrs} />";
            return "<Panel Width=\"420\" Height=\"236\">\n" +
                $"  <ins:MediaSurface{attrs} />\n" +
                "  <Border Classes=\"media-caption\" VerticalAlignment=\"Bottom\" HorizontalAlignment=\"Center\" Margin=\"16\">\n" +
                "    <TextBlock Text=\"وهنا تبدأ الحكاية\" />\n" +
                "  </Border>\n" +
                "</Panel>";
        },
        Notes = _ => ["الصورة هنا مرسومة في الشيفرة بـ `WriteFrame`، كما يرسم مشغل الفيديو إطاراته."],
        Stage = (view, _, _) =>
        {
            foreach (var surface in view.GetLogicalDescendants().OfType<MediaSurface>()) PaintSquare(surface);
        },
    };

    public static PlayDef Tile { get; } = new()
    {
        Id = "media-tile",
        Title = "البلاطة",
        Controls =
        [
            Switch("caption", "اسم في أسفلها", "Caption", "كلام على ملاحظة داكنة عند بداية أسفل البلاطة، كاسم صاحب الكاميرا.", OptionKind.Property, on: true),
            Switch("mirror", "معكوسة", "IsMirrored", "يقلب الصورة وحدها، ويبقى الاسم مقروءًا.", OptionKind.Property),
        ],
        Extra =
        [
            Row("الأساس", "ins:MediaTile", "بلاطة مستديرة من أرضية المشهد بحافة فاتحة خفيفة حول صورة: كاميرا أو صورة ثابتة أو `ins:MediaSurface`. وتعطيها مقاسها بـ `Width` و`Height`.", OptionKind.Element, true),
        ],
        Axaml = s =>
        {
            var attrs = (s.On("caption") ? " Caption=\"سارة\"" : "") + (s.On("mirror") ? " IsMirrored=\"True\"" : "");
            return $"<ins:MediaTile Width=\"208\" Height=\"170\"{attrs}>\n" +
                "  <ins:MediaSurface Stretch=\"UniformToFill\" />\n" +
                "</ins:MediaTile>";
        },
        Stage = (view, _, _) =>
        {
            foreach (var surface in view.GetLogicalDescendants().OfType<MediaSurface>()) Paint(surface);
        },
    };

    public static PlayDef ControlRow { get; } = new()
    {
        Id = "media-controls",
        Title = "شريط الأزرار",
        Controls =
        [
            Seg("width", "العرض", "820",
                Choice("820", "واسع"),
                Choice("560", "أضيق"),
                Choice("380", "ضيق")),
            Switch("volume", "الصوت", "ShowVolume", "زر الصوت: نقرته تكتم الصوت وتعيده، وسهمه يفتح بطاقة فيها مستوى الصوت. و`False` تخرجه من الشريط، وكذلك سائر خصائص `Show`.", OptionKind.Property, "الأزرار", on: true),
            Switch("speed", "السرعة", "ShowSpeed", "زر السرعة وقائمتها. وإذا أخفيته بقي مكانه، فيبقى التشغيل في المنتصف.", OptionKind.Property, "الأزرار", on: true),
            Switch("skip", "الرجوع والتقديم", "ShowBack", "زر الرجوع، و`ShowForward` زر التقديم.", OptionKind.Property, "الأزرار", on: true),
            Switch("subtitles", "الترجمة", "ShowSubtitles", "زر الترجمة. وله سهم يفتح بطاقة إذا أعطيته `SubtitlesCard`، وإلا فهو زر يُفعَّل ويُطفأ.", OptionKind.Property, "الأزرار", on: true),
            Switch("fit", "ملء الإطار", "ShowFit", "زر يجعل الصورة تملأ إطارها.", OptionKind.Property, "الأزرار", on: true),
            Switch("full", "ملء الشاشة", "ShowFullScreen", "زر ملء الشاشة، وأيقونته تنقلب حين يكون مفعّلًا.", OptionKind.Property, "الأزرار", on: true),
            Switch("card", "بطاقة الترجمة", "SubtitlesCard", "ما يفتحه سهم الترجمة، كإضافة ملف أو اختيار لغة.", OptionKind.Property, "البطاقات", on: true),
            Switch("more", "قائمة المزيد", "MoreItems", "عناصر قائمة «⋯» في آخر الشريط، وعادة تكون `MenuItem`. ولا يظهر الزر ما دامت فارغة، و`ShowMore` تخفيه.", OptionKind.Property, "البطاقات", on: true),
            Switch("mine", "زر من عندك", "EndItems", "أزرارك بعد ملء الشاشة وقبل «⋯». أعطها `bare icon media-btn` لتأخذ شكل أزرار الشريط. و`StartItems` بعد الصوت، و`CenterItems` بعد التقديم ويتسع الطرف الآخر بقدرها فيبقى التشغيل في المنتصف.", OptionKind.Property, "إضافة أزرار"),
            Switch("on", "مفعّلة", "IsSubtitlesOn", "الترجمة ظاهرة، و`IsFillingFrame` الصورة تملأ إطارها، و`IsFullScreen` ملء الشاشة. تتبدل بالنقر، وتربطها بمشغلك في الاتجاهين.", OptionKind.Property, "الحالة"),
        ],
        Extra =
        [
            Row("الأساس", "ins:MediaControlBar", "شريط أزرار المشغل بأزراره المعتادة: في البداية الصوت، وفي الوسط السرعة والرجوع والتشغيل والتقديم، وفي النهاية الترجمة وملء الإطار وملء الشاشة و«⋯». كل زر مربوط بخاصية تربطها بمشغلك، فلا تبني الصف بيدك. ولا ينقلب الشريط مع لغة الصفحة، كشريط الوقت: الصوت على اليسار دائمًا، أما البطاقات والقوائم فتُقرأ باتجاه الصفحة.", OptionKind.Element, true),
            Row("الحالة", "IsPlaying", "يعمل المقطع أو يتوقف، يبدله زر التشغيل ويعرض أيقونة الإيقاف ما دام يعمل.", OptionKind.Property),
            Row("الحالة", "Volume", "مستوى الصوت من 0 إلى 1، تحركه بطاقة الصوت وتدل عليه موجات السماعة. و`IsMuted` الصوت مكتوم، تبدله نقرة على السماعة، ورفع المستوى يعيده.", OptionKind.Property),
            Row("الحالة", "Speed", "سرعة التشغيل، 1 للعادية، تختارها من قائمة السرعة ويكتبها الزر هكذا: `1.25×`. و`Speeds` السرعات في القائمة، من 0.75 إلى 2 إن لم تذكرها.", OptionKind.Property),
            Row("الحالة", "SkipStep", "خطوة الرجوع والتقديم، عشر ثوانٍ إن لم تذكرها، ورقمها مكتوب داخل سهميهما.", OptionKind.Property),
            Row("الحالة", "SkipRequested", "يُطلق عند الرجوع والتقديم، وفيه `Offset` سالب للرجوع وموجب للتقديم، فينقل مشغلك التشغيل بقدره.", OptionKind.Event),
            Row("الأزرار", "ShowPlay", "زر التشغيل. و`ShowMore` زر «⋯».", OptionKind.Property),
            Row("الأشكال", "ins:MediaControls", "اللوح الذي يرتب المجموعات الثلاث في الشريط: الوسط في منتصف الصف تمامًا ما دام الطرفان يتركان له مكانًا، وإذا ضاق صعد الوسط إلى سطر فوق الطرفين، ثم أخذت كل مجموعة سطرًا. وتبني به صفًا من عندك إن أردت.", OptionKind.Element),
            Row("الأشكال", "media-btn", "زر أيقونة بشكل أزرار الشريط، 44 في 44، بلا خلفية حتى يمر عليه المؤشر، وإذا كان مفعّلًا أخذ لون العلامة التجارية الخفيف. و`media-ico` على الأيقونة يعطيها مقاس أيقونات الشريط، 22.", OptionKind.Class),
            Row("الأشكال", "media-split", "على `SplitButton` أو `ToggleSplitButton`: نصفه يفعل وسهمه يفتح بطاقته، كالصوت والترجمة. و`media-dd` لزر يفتح بطاقة، و`media-speed` لزر بكلام كزر السرعة، و`media-play` لزر دائري قطره 52 كزر التشغيل.", OptionKind.Class),
            Row("الأشكال", "media-group", "إطار رفيع حول أزرار متصلة، كالصوت والترجمة، وأزراره أصغر بدرجة. و`media-skip` على رقم الثواني داخل سهم الرجوع والتقديم.", OptionKind.Class),
        ],
        Axaml = s =>
        {
            var attrs = (s.On("volume") ? "" : " ShowVolume=\"False\"") +
                (s.On("speed") ? "" : " ShowSpeed=\"False\"") +
                (s.On("skip") ? "" : " ShowBack=\"False\" ShowForward=\"False\"") +
                (s.On("subtitles") ? "" : " ShowSubtitles=\"False\"") +
                (s.On("fit") ? "" : " ShowFit=\"False\"") +
                (s.On("full") ? "" : " ShowFullScreen=\"False\"") +
                (s.On("on") ? " IsSubtitlesOn=\"True\" IsFillingFrame=\"True\"" : "");
            var inner = new List<string>();
            if (s.On("card"))
                inner.AddRange(["  <ins:MediaControlBar.SubtitlesCard>", "    <TextBlock Text=\"أضف ملف ترجمة من جهازك.\" TextWrapping=\"Wrap\" MaxWidth=\"240\" />", "  </ins:MediaControlBar.SubtitlesCard>"]);
            if (s.On("mine"))
                inner.AddRange(["  <ins:MediaControlBar.EndItems>", "    <ToggleButton Classes=\"bare icon media-btn\" ToolTip.Tip=\"الفصول\" AutomationProperties.Name=\"الفصول\">", $"      <ins:Icon Classes=\"media-ico\" Data=\"{ChaptersIcon}\" />", "    </ToggleButton>", "  </ins:MediaControlBar.EndItems>"]);
            if (s.On("more"))
                inner.AddRange(["  <ins:MediaControlBar.MoreItems>", "    <MenuItem Header=\"تنزيل\" />", "    <MenuItem Header=\"اختصارات لوحة المفاتيح\" />", "  </ins:MediaControlBar.MoreItems>"]);
            var open = $"<ins:MediaControlBar Width=\"{s["width"]}\"{attrs}";
            return inner.Count == 0 ? open + " />" : open + ">\n" + string.Join("\n", inner) + "\n</ins:MediaControlBar>";
        },
        Notes = _ => ["ضيّق العرض لترى الوسط يصعد فوق الطرفين، ثم تأخذ كل مجموعة سطرًا.", "انقر الأزرار لترى ما تبدله من خصائص، وما يطلقه الرجوع والتقديم."],
        Stage = (view, _, say) =>
        {
            foreach (var bar in view.GetLogicalDescendants().OfType<MediaControlBar>())
            {
                bar.SkipRequested += (_, e) => say($"`SkipRequested` بمقدار `{e.Offset.TotalSeconds:+0;-0}` ثانية");
                bar.PropertyChanged += (_, e) =>
                {
                    if (e.Property == MediaControlBar.IsPlayingProperty || e.Property == MediaControlBar.IsMutedProperty || e.Property == MediaControlBar.IsSubtitlesOnProperty
                        || e.Property == MediaControlBar.IsFillingFrameProperty || e.Property == MediaControlBar.IsFullScreenProperty || e.Property == MediaControlBar.SpeedProperty)
                        say($"`{e.Property.Name}` صارت `{e.NewValue}`");
                };
            }
        },
    };

    public const string PictureExample =
        """
        <Button Classes="secondary" Content="عرض الصورة" ins:PictureViewer.Title="غروب على التلال" />
        <Button Classes="bare" Padding="0" CornerRadius="999" ins:PictureViewer.Title="سارة">
          <ins:Avatar Classes="lg" Content="س" />
        </Button>
        """;

    public const string PictureCode =
        """
        await PictureViewer.ShowAsync(this, bitmap, "غروب على التلال");
        """;

    /// <summary>The picture example's stage: both buttons get the scene, so a press opens the viewer.</summary>
    public static void StagePicture(Control view)
    {
        var picture = Picture(1600, 1000);
        foreach (var button in view.GetLogicalDescendants().OfType<Button>()) PictureViewer.SetSource(button, picture);
    }

    public static PlayDef[] All { get; } = [MediaBarPlay, LivePlay, Surface, Tile, ControlRow];

    private static void Paint(MediaSurface surface) => surface.WriteFrame(Scene(640, 360), 640, 360);

    // A square picture: on the wide stage the three stretches differ at a glance (bars at the sides, cut top and bottom, a stretched sun).
    private static void PaintSquare(MediaSurface surface) => surface.WriteFrame(Scene(360, 360), 360, 360);

    /// <summary>A small landscape in opaque BGRA: a dusk sky, the sun on the left, two lines of hills; its left and right differ, so a mirror shows.</summary>
    public static byte[] Scene(int width, int height)
    {
        var bytes = new byte[width * height * 4];
        double sunX = width * 0.28, sunY = height * 0.52, sunR = height * 0.13;
        for (var y = 0; y < height; y++)
        {
            var t = (double)y / height;
            for (var x = 0; x < width; x++)
            {
                var u = (double)x / width;
                double r = 40 + 215 * t, g = 60 + 90 * t, b = 130 - 40 * t;
                var d = Math.Sqrt((x - sunX) * (x - sunX) + (y - sunY) * (y - sunY));
                if (d < sunR) (r, g, b) = (255, 214, 120);
                else if (d < sunR * 2.4) { var k = 1 - (d - sunR) / (sunR * 1.4); r += (255 - r) * k * 0.35; g += (190 - g) * k * 0.35; }
                var far = height * (0.66 + 0.06 * Math.Sin(u * 7 + 1));
                var near = height * (0.8 + 0.07 * Math.Sin(u * 4.3 + 2.4) + 0.02 * Math.Sin(u * 17));
                if (y > near) (r, g, b) = (28, 44, 38);
                else if (y > far) (r, g, b) = (58, 72, 78);
                var i = (y * width + x) * 4;
                bytes[i] = (byte)Math.Clamp(b, 0, 255);
                bytes[i + 1] = (byte)Math.Clamp(g, 0, 255);
                bytes[i + 2] = (byte)Math.Clamp(r, 0, 255);
                bytes[i + 3] = 255;
            }
        }
        return bytes;
    }

    /// <summary>The scene as a bitmap, for the picture viewer.</summary>
    public static WriteableBitmap Picture(int width, int height)
    {
        var bitmap = new WriteableBitmap(new PixelSize(width, height), new Vector(96, 96), PixelFormat.Bgra8888, AlphaFormat.Opaque);
        var scene = Scene(width, height);
        using var target = bitmap.Lock();
        for (var y = 0; y < height; y++)
            System.Runtime.InteropServices.Marshal.Copy(scene, y * width * 4, target.Address + y * target.RowBytes, width * 4);
        return bitmap;
    }
}
