SitePlay.define({
  id: 'bar',
  title: 'الشريط',
  owns: 'ins-progress',
  wide: true,
  controls: [
    { key: 'tone', label: 'النبرة', type: 'select', value: '', choices: [
      { value: '', label: 'عادي' },
      { value: 'ok', label: 'نجاح', name: 'ins-progress--ok', desc: 'لما اكتمل، أو يسير كما ينبغي.' },
      { value: 'warn', label: 'تحذير', name: 'ins-progress--warn', desc: 'لنسبة تقترب من حدها، كمساحة تكاد تمتلئ.' },
      { value: 'bad', label: 'خطر', name: 'ins-progress--bad', desc: 'لنسبة بلغت حدها أو تجاوزته.' }
    ] },
    { key: 'size', label: 'المقاس', type: 'seg', value: '', choices: [
      { value: 'sm', label: 'صغير', name: 'ins-progress--sm', desc: 'شريط أنحف، لصف في جدول أو بطاقة.' },
      { value: '', label: 'عادي' },
      { value: 'lg', label: 'كبير', name: 'ins-progress--lg', desc: 'شريط أغلظ، إذا كان التقدم محور الشاشة.' }
    ] },
    { key: 'indet', label: 'مدة غير معروفة', type: 'switch' },
    { key: 'value', label: 'القيمة', type: 'text', value: '64' },
    { key: 'live', label: 'تحديث القيمة', type: 'switch', level: 'advanced' }
  ],
  extra: [
    { group: 'الأساس', first: true, name: 'ins-progress', desc: 'على عنصر `<progress>` الأصلي، فيعلن قارئ الشاشة قيمته من غير ARIA. ويمتلئ من جهة البداية.' },
    { group: 'الأساس', name: 'value', desc: 'القيمة الحالية، من صفر إلى `max`. فإن خلا منها الشريط دل على عمل جارٍ لا تُعرف نسبته. وإن غيّرتها من جافاسكربت انساب الشريط إلى قيمته الجديدة.' },
    { group: 'الأساس', name: 'max', desc: 'القيمة عند الامتلاء، وهي `1` إن لم تذكرها.' },
    { group: 'الأساس', name: 'aria-label', desc: 'اسم الشريط، يقرؤه قارئ الشاشة مع قيمته.' }
  ],
  disable: function (s) { return s.indet ? ['value', 'live'] : []; },
  html: function (s) {
    var cls = 'ins-progress' + (s.tone ? ' ins-progress--' + s.tone : '') + (s.size ? ' ins-progress--' + s.size : '');
    if (s.indet) return '<progress class="' + cls + '" aria-label="جارٍ التحميل"></progress>';
    var id = s.live ? ' id="up-bar"' : '';
    var bar = '<progress class="' + cls + '"' + id + ' aria-label="التقدم" value="' + s.value + '" max="100">' + s.value + '%</progress>';
    if (!s.live) return bar;
    return '<div class="ins-flex ins-items-center ins-gap-3">\n  ' + bar +
      '\n  <button class="ins-btn ins-btn--secondary ins-btn--sm" onclick="document.getElementById(\'up-bar\').value += 10">تقدم</button>\n</div>';
  },
  notes: function (s) {
    var out = [];
    if (s.indet) out.push('لا قيمة للشريط، فهو يدل على عمل جارٍ لا تُعرف نسبته.');
    if (s.live && !s.indet) out.push('اضغط الزر، فتزيد القيمة وينساب الشريط إليها.');
    return out;
  }
});

SitePlay.define({
  id: 'ring',
  title: 'الحلقة',
  owns: 'ins-ring',
  controls: [
    { key: 'size', label: 'المقاس', type: 'seg', value: '', choices: [
      { value: '', label: 'عادي' },
      { value: 'lg', label: 'كبير', name: 'ins-ring--lg', desc: 'حلقة أكبر، في بطاقة إحصائية أو بجوار عنوان.' }
    ] },
    { key: 'value', label: 'النسبة', type: 'text', value: '72' }
  ],
  extra: [
    { group: 'الأساس', first: true, name: 'ins-ring', desc: 'تعرض النسبة حين تضيق المساحة، وتدور مع عقارب الساعة في الاتجاهين كما يدور القرص.' },
    { group: 'الأساس', name: 'role="progressbar"', desc: 'فالحلقة ليست عنصرًا أصليًا، ولا يفهمها قارئ الشاشة من غيرها.' },
    { group: 'الأساس', name: 'aria-valuenow="72"', desc: 'القيمة، ومنها تمتلئ الحلقة بنسبتها بين `aria-valuemin` و`aria-valuemax`، فلا تكتب القيمة مرتين.' },
    { group: 'الأساس', name: 'data-ins-size="4rem"', desc: 'قطر الحلقة إن أردت غير المقاسين.' }
  ],
  html: function (s) {
    return '<div class="ins-ring' + (s.size ? ' ins-ring--lg' : '') + '" role="progressbar" aria-label="التقدم" aria-valuenow="' + s.value +
      '" aria-valuemin="0" aria-valuemax="100">' + s.value + '%</div>';
  }
});
