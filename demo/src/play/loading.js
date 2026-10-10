SitePlay.define({
  id: 'skeleton',
  title: 'هيكل التحميل',
  owns: 'ins-skel',
  wide: true,
  controls: [
    { key: 'shape', label: 'الشكل', type: 'select', value: 'text', choices: [
      { value: 'text', label: 'سطور', name: 'ins-skel--text', desc: 'سطر من النص، وآخر السطور في مجموعتها أقصرها، كما في الفقرة.' },
      { value: 'title', label: 'عنوان', name: 'ins-skel--title', desc: 'عنوان، أغلظ من السطر وأقصر منه.' },
      { value: 'circle', label: 'دائرة', name: 'ins-skel--circle', desc: 'صورة رمزية أو أيقونة.' },
      { value: 'block', label: 'كتلة', name: 'ins-skel--block', desc: 'صورة أو رسم بياني أو بطاقة.' }
    ] },
    { key: 'lines', label: 'عدد السطور', type: 'seg', value: '3', choices: [
      { value: '1', label: '1' },
      { value: '2', label: '2' },
      { value: '3', label: '3' }
    ] }
  ],
  extra: [
    { group: 'الأساس', first: true, name: 'ins-skel', desc: 'يرسم هيئة المحتوى قبل أن يصل، ومعه كلاس للشكل. فإذا وصل المحتوى أخذ مكانه، فلا تقفز الصفحة.' },
    { group: 'الشكل', name: 'data-ins-size="3rem"', desc: 'قطر الدائرة، فاجعله بقدر ما سيحل محلها. وهو `2.5rem` إن لم تذكره.' }
  ],
  disable: function (s) { return s.shape === 'text' ? [] : ['lines']; },
  html: function (s) {
    var n = s.shape === 'text' ? +s.lines : 1, rows = [];
    for (var i = 0; i < n; i++) rows.push('<div class="ins-skel ins-skel--' + s.shape + '"></div>');
    return rows.join('\n');
  },
  notes: function (s) {
    return s.shape === 'text' && s.lines !== '1' ? ['آخر السطور أقصرها، فتُقرأ السطور فقرة لم تصل بعد، لا جدولًا من الأشرطة.'] : [];
  }
});

SitePlay.define({
  id: 'spinner',
  title: 'مؤشر التحميل',
  owns: 'ins-spinner',
  controls: [
    { key: 'place', label: 'موضعه', type: 'seg', value: '', choices: [
      { value: '', label: 'وحده' },
      { value: 'btn', label: 'في زر' },
      { value: 'box', label: 'في حاوية', name: 'ins-loading', desc: 'حاوية يتوسطها المؤشر، وتحفظ للمنطقة التي تنتظر محتواها ارتفاعًا معقولًا.' }
    ] },
    { key: 'size', label: 'المقاس', type: 'seg', value: '', choices: [
      { value: '', label: 'عادي' },
      { value: 'sm', label: 'صغير', name: 'ins-spinner--sm', desc: 'مؤشر أصغر، يسكن داخل زر أو بجوار نص.' }
    ] }
  ],
  extra: [
    { group: 'الأساس', first: true, name: 'ins-spinner', desc: 'لانتظار قصير لا تُعرف هيئة ما يأتي بعده، فإن عرفتها فهيكل التحميل أولى.' },
    { group: 'الأساس', name: 'role="status"', desc: 'ومعها `aria-label`، فيعلم قارئ الشاشة أن شيئًا يُحمَّل.' },
    { group: 'الأساس', name: 'aria-hidden="true"', desc: 'مكان `role="status"` إذا كان نص الزر أو ما حول المؤشر يغني عنه.' }
  ],
  disable: function (s) { return s.place === 'btn' ? ['size'] : []; },
  html: function (s) {
    if (s.place === 'btn') return '<button class="ins-btn ins-btn--primary" disabled><span class="ins-spinner ins-spinner--sm" aria-hidden="true"></span>جارٍ الحفظ…</button>';
    var spin = '<span class="ins-spinner' + (s.size ? ' ins-spinner--sm' : '') + '" role="status" aria-label="جارٍ التحميل"></span>';
    return s.place === 'box' ? '<div class="ins-loading">\n  ' + spin + '\n</div>' : spin;
  },
  notes: function (s) {
    return s.place === 'btn' ? ['في الزر يصغر المؤشر ويأخذ لون نص الزر، ويُخفى عن قارئ الشاشة لأن نص الزر يغني عنه.'] : [];
  }
});

SitePlay.define({
  id: 'empty',
  title: 'الحالة الفارغة',
  owns: 'ins-empty',
  wide: true,
  controls: [
    { key: 'kind', label: 'السبب', type: 'seg', value: 'new', choices: [
      { value: 'new', label: 'لا شيء بعد' },
      { value: 'search', label: 'لا نتائج' }
    ] },
    { key: 'ico', label: 'أيقونة', type: 'switch', value: true, group: 'الأساس', name: 'ins-empty-ico', desc: 'أيقونة باهتة فوق العنوان، تدل على ما كان سيظهر هنا.' },
    { key: 'action', label: 'مع زر', type: 'switch', value: true }
  ],
  extra: [
    { group: 'الأساس', first: true, name: 'ins-empty', desc: 'تخبر القارئ بما سيظهر هنا، وبما يفعله بعد ذلك.' },
    { group: 'الأساس', name: 'ins-empty-title', desc: 'العنوان: ما الذي ليس هنا.' },
    { group: 'الأساس', name: 'ins-empty-hint', desc: 'التلميح تحت العنوان: ما الذي يفعله القارئ الآن.' }
  ],
  html: function (s) {
    var k = s.kind === 'search'
      ? { ico: 'search', title: 'لا نتائج', hint: 'جرب كلمة أخرى.', btn: '<button class="ins-btn ins-btn--secondary">امسح البحث</button>' }
      : { ico: 'folder', title: 'لا ملفات بعد', hint: 'أضف أول ملف ليظهر هنا.', btn: '<button class="ins-btn ins-btn--primary">ملف جديد</button>' };
    return '<div class="ins-empty">\n' +
      (s.ico ? '  <span class="ins-empty-ico"><svg class="ico"><use href="#i-' + k.ico + '"/></svg></span>\n' : '') +
      '  <span class="ins-empty-title">' + k.title + '</span>\n' +
      '  <span class="ins-empty-hint">' + k.hint + '</span>\n' +
      (s.action ? '  ' + k.btn + '\n' : '') +
      '</div>';
  }
});
