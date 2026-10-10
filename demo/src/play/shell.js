SitePlay.define({
  id: 'topbar',
  title: 'الشريط العلوي',
  owns: 'ins-island',
  wide: true,
  controls: [
    { key: 'title', label: 'العنوان', type: 'text', value: 'العنوان' },
    { key: 'sub', label: 'سطر فرعي', type: 'switch', value: true, name: 'ins-shell-sub', desc: 'سطر اختياري تحت العنوان، يختفي على الهاتف، ولا يظهر إذا كان فارغًا.' },
    { key: 'menu', label: 'زر القائمة', type: 'switch', value: true, name: 'ins-island--icon', desc: 'جزيرة مربعة تتسع لأيقونة واحدة، ولا بد لها عندئذ من `aria-label`.' },
    { key: 'end', label: 'في آخر الشريط', type: 'seg', value: '', level: 'advanced', choices: [
      { value: '', label: 'جزر' },
      { value: 'pill', label: 'شارة', name: 'ins-pill', desc: 'شارة في إحدى المناطق تأخذ ارتفاع الجزيرة وظلها، وتبقي لونها.' },
      { value: 'btn', label: 'زر', name: 'ins-btn', desc: 'زر في إحدى المناطق يأخذ ارتفاع الجزيرة، فيبقى الشريط سطرًا واحدًا.' }
    ] },
    { key: 'still', label: 'جزيرة لا تُنقر', type: 'switch', level: 'advanced', name: 'span.ins-island', kind: 'element', desc: 'جزيرة تحمل معلومة كحالة الصفحة ولا تُنقر، فلا ترتفع تحت المؤشر كما يرتفع الزر والرابط.' }
  ],
  extra: [
    { group: 'الأساس', first: true, name: 'ins-topbar', desc: 'شريط شفاف تطفو عليه الجزر، فإذا مر المحتوى تحته عند التمرير صار زجاجًا مصنفرًا.' },
    { group: 'الأساس', name: 'ins-zone--start', desc: 'منطقة البداية، ومعها `ins-zone`، لأدوات الصفحة كزر القائمة.' },
    { group: 'الأساس', name: 'ins-zone--center', desc: 'المنطقة الوسطى، تتوسط الصفحة كلها لا ما بقي منها، وفيها العنوان.' },
    { group: 'الأساس', name: 'ins-zone--end', desc: 'منطقة النهاية، لما يخص من يقرأ الصفحة كالإشعارات وحسابه.' },
    { group: 'الأساس', name: 'ins-island', desc: 'جزيرة على هيئة حبة تحمل نصًا أو أي محتوى، فإذا كانت زرًا أو رابطًا ارتفعت تحت المؤشر.' },
    { group: 'الأساس', name: 'ins-island--title', desc: 'تحمل اسم الصفحة في `ins-shell-head`، فإذا طال انتهى بنقاط ولم ينكسر إلى سطرين.' },
    { group: 'الأساس', name: 'ins-shell-title', desc: 'اسم الصفحة في جزيرة العنوان. اجعله `<h1>` ليكون عنوانها، ولا تكرره في المحتوى.' },
    { group: 'في آخر الشريط', name: 'ins-island--brand', level: 'advanced', desc: 'تحمل الهوية، ولا تظهر إلا حين يُطوى الشريط الجانبي أو يصير درجًا.' }
  ],
  disable: function (s) { return s.end ? ['still'] : []; },
  html: function (s) {
    var out = ['<header class="ins-topbar">', '  <div class="ins-zone ins-zone--start">'];
    if (s.menu) out.push('    <button class="ins-island ins-island--icon" aria-label="القائمة"><svg class="ico"><use href="#i-menu"/></svg></button>');
    out.push('  </div>', '  <div class="ins-zone ins-zone--center">', '    <div class="ins-island ins-island--title">',
      '      <span class="ins-shell-head">', '        <span class="ins-shell-title">' + s.title + '</span>');
    if (s.sub) out.push('        <span class="ins-shell-sub">سطر فرعي</span>');
    out.push('      </span>', '    </div>', '  </div>', '  <div class="ins-zone ins-zone--end">');
    if (s.end === 'pill') out.push('    <span class="ins-pill ins-pill--ok">متصل</span>');
    else if (s.end === 'btn') out.push('    <button class="ins-btn ins-btn--primary">حفظ</button>');
    else {
      out.push('    <button class="ins-island ins-island--icon" aria-label="الإشعارات"><svg class="ico"><use href="#i-bell"/></svg></button>',
        s.still ? '    <span class="ins-island">مسودة</span>' : '    <button class="ins-island">حسابي</button>');
    }
    return out.concat('  </div>', '</header>').join('\n');
  },
  notes: function (s) {
    var out = s.menu ? ['زر القائمة هنا للعرض، فأضف إليه `data-ins-sidebar` ليطوي الشريط الجانبي.'] : [];
    if (s.still && !s.end) out.push('مرّر المؤشر على جزيرتي النهاية: الزر يرتفع، والتي لا تُنقر تبقى مكانها.');
    return out;
  }
});

SitePlay.define({
  id: 'shell',
  title: 'الهيكل والشريط الجانبي',
  owns: 'ins-shell',
  controls: [],
  extra: [
    { group: 'الأساس', first: true, name: 'ins-shell', desc: 'الهيكل كله: شريط جانبي وعمود رئيسي، يتبعان اتجاه الصفحة.' },
    { group: 'الأساس', name: 'ins-shell-main', desc: 'العمود الرئيسي، وفيه الشريط العلوي والمحتوى.' },
    { group: 'الأساس', name: 'ins-shell-content', desc: 'المحتوى، يملأ ما بقي من العرض بلا حد أقصى.' },
    { group: 'الشريط الجانبي', name: 'ins-shell-side', desc: 'الشريط الجانبي. أعطه `id`، فيفتحه رابط إليه درجًا على الشاشة الضيقة بلا جافاسكربت.' },
    { group: 'الشريط الجانبي', name: 'ins-shell-scrim', desc: 'الخلفية التي تغطي الصفحة خلف الدرج، وتأتي بعد الشريط الجانبي مباشرة.' },
    { group: 'الشريط الجانبي', name: 'ins-shell-brand', desc: 'الهوية في أعلى الشريط، وإذا جعلتها رابطًا مالت علامتها تحت المؤشر.' },
    { group: 'الشريط الجانبي', name: 'ins-shell-mark', desc: 'علامة الهوية: حرف أو صورة في مربع بلون الهوية.' },
    { group: 'الشريط الجانبي', name: 'ins-shell-brand-text', desc: 'يضم اسم التطبيق `ins-shell-brand-name` وسطرًا تحته `ins-shell-brand-role`.' },
    { group: 'الشريط الجانبي', name: 'ins-shell-brand-name', desc: 'اسم التطبيق.' },
    { group: 'الشريط الجانبي', name: 'ins-shell-brand-role', desc: 'سطر صغير تحت الاسم.' },
    { group: 'الشريط الجانبي', name: 'ins-shell-group', desc: 'عنوان مجموعة من الروابط.' },
    { group: 'الشريط الجانبي', name: 'ins-shell-link', desc: 'رابط في الشريط، تسبقه أيقونة.' },
    { group: 'الشريط الجانبي', name: 'is-active', kind: 'class', desc: 'على رابط الصفحة الحالية، ومعه `aria-current="page"`.' },
    { group: 'الشريط الجانبي', name: 'ins-shell-link-badge', desc: 'عداد في آخر الرابط.' },
    { group: 'الشريط الجانبي', name: 'ins-shell-bottom', desc: 'يعزل في أسفل الشريط ما لا يقود إلى صفحة، كالخروج، عن بقية الروابط.' },
    { group: 'الطي والدرج', name: 'data-ins-sidebar', desc: 'على زر يطوي الشريط على الحاسوب ويحفظ حالته، ويفتحه درجًا على الشاشة الضيقة. وقيمته `toggle` أو `open` أو `collapsed`.' },
    { group: 'الطي والدرج', name: 'ins-sidebar-collapsed', desc: 'يضعه السكربت على `<html>` ما دام الشريط مطويًا.' },
    { group: 'الطي والدرج', name: 'ins:sidebar', kind: 'event', desc: 'يُطلق كلما طُوي الشريط أو فُتح، وفي `detail` حالته `state`، و`drawer` إن كان درجًا.' },
    { group: 'الخلفية', name: 'data-ins-orbs="off"', level: 'advanced', desc: 'على `<html>`، فتوقف الكرات الثلاث التي تضعها المكتبة خلف الصفحة ليظهر عليها أثر الزجاج.' }
  ],
  html: function () { return ''; }
});
