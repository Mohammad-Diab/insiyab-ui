SitePlay.define({
  id: 'pill',
  title: 'شارة الحالة',
  owns: 'ins-pill',
  controls: [
    { key: 'tone', label: 'النبرة', type: 'select', value: 'ok', choices: [
      { value: '', label: 'محايدة' },
      { value: 'ok', label: 'نجاح', name: 'ins-pill--ok', desc: 'لحالة تمت أو تسير كما ينبغي.' },
      { value: 'warn', label: 'تحذير', name: 'ins-pill--warn', desc: 'لحالة معلقة أو تنتظر شيئًا.' },
      { value: 'bad', label: 'خطر', name: 'ins-pill--bad', desc: 'لحالة ملغاة أو متعثرة.' },
      { value: 'info', label: 'معلومة', name: 'ins-pill--info', desc: 'لحالة جارية.' },
      { value: 'brand', label: 'العلامة', name: 'ins-pill--brand', desc: 'بلون العلامة، لما يستحق أن يتميز.' }
    ] },
    { key: 'text', label: 'النص', type: 'text', value: 'مكتمل' },
    { key: 'dot', label: 'بنقطة', type: 'switch', value: true, name: 'ins-pill-dot', desc: 'نقطة بلون الشارة قبل النص، لحالة تكفيها علامة بلا أيقونة.' },
    { key: 'press', label: 'قابلة للنقر', type: 'switch', name: 'button.ins-pill', kind: 'element', desc: 'ضع الكلاس على `<button>` أو `<a>` فترتفع الشارة قليلًا تحت المؤشر. أما الشارة التي تُقرأ فحسب فلا تتحرك.' }
  ],
  extra: [
    { group: 'الأساس', first: true, name: 'ins-pill', desc: 'شارة للحالة، ومعها كلاس للنبرة. وهي محايدة إن لم تذكر نبرة.' }
  ],
  html: function (s) {
    var dot = s.dot ? '<span class="ins-pill-dot"></span>' : '';
    var cls = 'ins-pill' + (s.tone ? ' ins-pill--' + s.tone : '');
    if (!s.press) return '<span class="' + cls + '">' + dot + s.text + '</span>';
    return '<div class="ins-flex ins-items-center ins-gap-2">\n' +
      '  <button class="' + cls + '">' + dot + s.text + '</button>\n' +
      '  <button class="ins-pill">' + dot + 'الأول</button>\n' +
      '  <button class="ins-pill">' + dot + 'الثاني</button>\n' +
      '</div>';
  },
  notes: function (s) {
    return s.press ? ['شارات تُنقر، كأزرار تصفية. مر عليها بالمؤشر فترتفع قليلًا.'] : [];
  }
});

SitePlay.define({
  id: 'count',
  title: 'شارة العدد',
  owns: 'ins-badge',
  controls: [
    { key: 'tone', label: 'النبرة', type: 'select', value: '', choices: [
      { value: '', label: 'العلامة' },
      { value: 'ok', label: 'نجاح', name: 'ins-badge--ok', desc: 'لعدد ما تم.' },
      { value: 'warn', label: 'تحذير', name: 'ins-badge--warn', desc: 'لعدد ما ينتظر.' },
      { value: 'bad', label: 'خطر', name: 'ins-badge--bad', desc: 'لعدد ما تعثر، أو ما يستدعي الانتباه.' },
      { value: 'mute', label: 'هادئة', name: 'ins-badge--mute', desc: 'لعدد لا يطلب شيئًا، كالصفر.' }
    ] },
    { key: 'text', label: 'العدد', type: 'text', value: '12' }
  ],
  extra: [
    { group: 'الأساس', first: true, name: 'ins-badge', desc: 'تحمل عددًا صغيرًا، بلون العلامة إن لم تذكر نبرة. وإذا تغير العدد فاستبدلت الشارة ظهرت بقفزة خفيفة.' }
  ],
  html: function (s) {
    return '<span class="ins-badge' + (s.tone ? ' ins-badge--' + s.tone : '') + '">' + s.text + '</span>';
  }
});

SitePlay.define({
  id: 'avatar',
  title: 'الصورة الرمزية',
  owns: 'ins-avatar',
  controls: [
    { key: 'size', label: 'المقاس', type: 'seg', value: '', choices: [
      { value: 'sm', label: 'صغير', name: 'ins-avatar--sm', desc: 'صورة أصغر، لقائمة أو سطر.' },
      { value: '', label: 'عادي' },
      { value: 'lg', label: 'كبير', name: 'ins-avatar--lg', desc: 'صورة أكبر، لرأس صفحة أو بطاقة.' }
    ] },
    { key: 'group', label: 'مجموعة صور', group: 'مجموعة صور', type: 'switch', name: 'ins-avatars', desc: 'يرص عدة صور يتداخل بعضها في بعض، وتحيط بكل صورة حلقة بلون السطح فيبقى بين كل صورتين فاصل. ومعه `aria-label` يقول كم هم.' },
    { key: 'text', label: 'الحروف', type: 'text', value: 'ل ح' }
  ],
  extra: [
    { group: 'الأساس', first: true, name: 'ins-avatar', desc: 'قرص بلون العلامة يحمل حرفًا أو حرفين، أو صورة `<img>` تملؤه.' },
    { group: 'مجموعة صور', name: 'ins-avatar--more', desc: 'آخر المجموعة، يعد من بقي، مثل `+4`.' }
  ],
  disable: function (s) { return s.group ? ['text'] : []; },
  html: function (s) {
    var cls = 'ins-avatar' + (s.size ? ' ins-avatar--' + s.size : '');
    if (!s.group) return '<span class="' + cls + '">' + s.text + '</span>';
    return '<div class="ins-avatars" aria-label="المشاركون: 7">\n' +
      ['ل', 'ع', 'س'].map(function (t) { return '  <span class="' + cls + '">' + t + '</span>\n'; }).join('') +
      '  <span class="' + cls + ' ins-avatar--more">+4</span>\n' +
      '</div>';
  }
});

SitePlay.define({
  id: 'money',
  title: 'المبلغ',
  owns: 'ins-money',
  controls: [
    { key: 'sign', label: 'الاتجاه', type: 'seg', value: '', level: 'advanced', choices: [
      { value: '', label: 'بلا' },
      { value: 'pos', label: 'زيادة', name: 'ins-money--pos', desc: 'بلون النجاح، لمبلغ زاد.' },
      { value: 'neg', label: 'نقص', name: 'ins-money--neg', desc: 'بلون الخطر، لمبلغ نقص.' }
    ] },
    { key: 'val', label: 'القيمة', type: 'text', value: '1,249.00', level: 'advanced' },
    { key: 'cur', label: 'العملة', type: 'text', value: 'ر.س', level: 'advanced' }
  ],
  extra: [
    { group: 'الأساس', first: true, level: 'advanced', name: 'ins-money', desc: 'مبلغ بأرقام متساوية العرض، يصلح لكل عملة.' },
    { group: 'الأساس', level: 'advanced', name: 'ins-money-val', desc: 'القيمة، معزولة الاتجاه، فتبقى إشارة السالب في مكانها في السطر العربي.' },
    { group: 'الأساس', level: 'advanced', name: 'ins-money-cur', desc: 'العملة بعد القيمة، أصغر منها وأهدأ لونًا.' }
  ],
  html: function (s) {
    return '<span class="ins-money' + (s.sign ? ' ins-money--' + s.sign : '') + '"><span class="ins-money-val">' + s.val +
      '</span><span class="ins-money-cur">' + s.cur + '</span></span>';
  },
  notes: function (s) {
    return s.sign === 'neg' && s.val.charAt(0) !== '-' ? ['اكتب الإشارة في القيمة، مثل `-1,500.00`، فالكلاس يلوّن ولا يضيفها.'] : [];
  }
});

SitePlay.define({
  id: 'num',
  title: 'الرقم',
  owns: 'ins-num',
  controls: [
    { key: 'text', label: 'الرقم', type: 'text', value: '#40218' }
  ],
  extra: [
    { group: 'الأساس', first: true, name: 'ins-num', desc: 'لرقم ليس مبلغًا، كرقم المرجع والهاتف والتاريخ: يعزل اتجاهه كما تُعزل قيمة المبلغ، فلا يضطرب في السطر العربي.' }
  ],
  html: function (s) {
    return '<p>رقم المرجع <span class="ins-num">' + s.text + '</span></p>';
  }
});
