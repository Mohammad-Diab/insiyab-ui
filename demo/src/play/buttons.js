SitePlay.define({
  id: 'button',
  title: 'الزر',
  owns: 'ins-btn',
  controls: [
    { key: 'look', label: 'النمط', type: 'select', value: 'primary', choices: [
      { value: 'primary', label: 'رئيسي', name: 'ins-btn--primary', desc: 'للإجراء الرئيسي في الشاشة.' },
      { value: 'secondary', label: 'ثانوي', name: 'ins-btn--secondary', desc: 'لما يجاور الزر الرئيسي.' },
      { value: 'success', label: 'نجاح', name: 'ins-btn--success', desc: 'لإجراء يؤكد أو يقبل.' },
      { value: 'danger', label: 'خطر', name: 'ins-btn--danger', desc: 'لإجراء يحذف، أو لا رجعة فيه.' },
      { value: 'warning', label: 'تحذير', name: 'ins-btn--warning', desc: 'لإجراء يوقف أو يؤجل.' },
      { value: 'info', label: 'معلومة', name: 'ins-btn--info', desc: 'لإجراء يشرح أو يفصّل.' },
      { value: 'ghost', label: 'هادئ', name: 'ins-btn--ghost', desc: 'أهدأ من الثانوي، لإجراء لا يلفت النظر.' },
      { value: 'ghost-success', label: 'هادئ بنبرة النجاح', name: 'ins-btn--ghost-success', desc: 'هادئ بلون النجاح.' },
      { value: 'ghost-danger', label: 'هادئ بنبرة الخطر', name: 'ins-btn--ghost-danger', desc: 'هادئ بلون الخطر.' },
      { value: 'ghost-warning', label: 'هادئ بنبرة التحذير', name: 'ins-btn--ghost-warning', desc: 'هادئ بلون التحذير.' },
      { value: 'ghost-info', label: 'هادئ بنبرة المعلومة', name: 'ins-btn--ghost-info', desc: 'هادئ بلون المعلومة.' },
      { value: 'bare', label: 'بلا خلفية', name: 'ins-btn--bare', desc: 'لإجراء يكاد يكون رابطًا، كالإلغاء.' }
    ] },
    { key: 'size', label: 'المقاس', type: 'seg', value: '', choices: [
      { value: 'sm', label: 'صغير', name: 'ins-btn--sm', desc: 'زر أصغر، للجداول والأشرطة.' },
      { value: '', label: 'عادي' },
      { value: 'lg', label: 'كبير', name: 'ins-btn--lg', desc: 'زر أكبر، للإجراء الوحيد في الشاشة.' }
    ] },
    { key: 'icon', label: 'الأيقونة', type: 'seg', value: '', choices: [
      { value: '', label: 'بلا' },
      { value: 'start', label: 'قبل النص' },
      { value: 'only', label: 'وحدها', name: 'ins-btn--icon', desc: 'زر مربع يسع أيقونة واحدة، ولا بد له عندئذ من `aria-label`.' }
    ] },
    { key: 'text', label: 'النص', type: 'text', value: 'حفظ' },
    { key: 'full', label: 'بعرض الحاوية', type: 'switch', name: 'ins-btn--full', desc: 'يمد الزر على عرض حاويته كله.' },
    { key: 'lift', label: 'يرتفع تحت المؤشر', type: 'switch', name: 'ins-btn--lift', desc: 'يرفع الزر قليلًا إذا مر عليه المؤشر.' },
    { key: 'link', label: 'رابط بهيئة زر', type: 'switch', name: 'a.ins-btn', kind: 'element', desc: 'ضع الكلاس على `<a>` فيصير الرابط في هيئة زر، ويبقى رابطًا.' },
    { key: 'disabled', label: 'معطل', type: 'switch', name: 'disabled', desc: 'يعطل الزر، فلا يُنقر ولا يصل إليه التنقل بلوحة المفاتيح.' },
    { key: 'toggle', label: 'زر تبديل', group: 'زر تبديل', type: 'switch', level: 'advanced', name: 'data-ins-toggle', desc: 'يحفظ حالة الزر في `aria-pressed` عند كل ضغطة، ويطلق حدث `ins:toggle`.' }
  ],
  extra: [
    { group: 'الأساس', first: true, name: 'ins-btn', desc: 'أساس كل زر، على `<button>` أو `<a>`، ومعه كلاس للنمط.' },
    { group: 'زر تبديل', name: 'aria-pressed="true"', level: 'advanced', desc: 'الزر مضغوط. ضعها بنفسك ليبدأ الزر مضغوطًا.' },
    { group: 'زر تبديل', name: 'ins:toggle', kind: 'event', level: 'advanced', desc: 'يُطلق عند كل ضغطة، وفي `detail` الزر نفسه `el` وحالته `pressed`.' }
  ],
  events: ['ins:toggle'],
  disable: function (s) { return s.link ? ['disabled', 'toggle'] : []; },
  stage: function (view, s) { if (s.toggle && !s.link) view.querySelector('button').setAttribute('aria-pressed', 'true'); },
  html: function (s) {
    var cls = 'ins-btn ins-btn--' + s.look + (s.size ? ' ins-btn--' + s.size : '') + (s.icon === 'only' ? ' ins-btn--icon' : '') +
      (s.full ? ' ins-btn--full' : '') + (s.lift ? ' ins-btn--lift' : '');
    var tag = s.link ? 'a' : 'button';
    var attrs = s.link ? ' href="/orders"' : (s.disabled ? ' disabled' : '') + (s.toggle ? ' data-ins-toggle aria-pressed="false"' : '');
    if (s.icon === 'only') attrs += ' aria-label="' + s.text + '"';
    var ico = s.icon ? '<svg class="ico"><use href="#i-check"/></svg>' : '';
    return '<' + tag + ' class="' + cls + '"' + attrs + '>' + ico + (s.icon === 'only' ? '' : s.text) + '</' + tag + '>';
  },
  notes: function (s) {
    var out = [];
    if (s.icon === 'only') out.push('لا نص على الزر، فصار نصه تسمية `aria-label` يقرؤها قارئ الشاشة.');
    if (s.lift) out.push('مرر المؤشر على الزر، فيرتفع قليلًا.');
    if (s.link) out.push('الرابط لا يُعطَّل ولا يكون زر تبديل، فهذان الخياران للأزرار وحدها.');
    if (s.toggle && !s.link) out.push(s.look === 'primary'
      ? 'الزر الرئيسي لا يتغير شكله حين يُضغط، فاختر نمطًا آخر لترى حالته المضغوطة. واضغطه يظهر هنا الحدث الذي يطلقه.'
      : 'يبدو الزر هنا مضغوطًا، فاضغطه يرجع إلى حاله ويظهر هنا الحدث الذي يطلقه.');
    return out;
  }
});

SitePlay.define({
  id: 'group',
  title: 'مجموعة الأزرار',
  owns: 'ins-btn-group',
  controls: [
    { key: 'size', label: 'المقاس', type: 'seg', value: '', choices: [
      { value: '', label: 'عادي' },
      { value: 'sm', label: 'صغير', name: 'ins-btn-group--sm', desc: 'لمجموعة من أزرار `ins-btn--sm`: يضبط انحناء طرفيها على مقاس أزرارها.' }
    ] },
    { key: 'look', label: 'نمط الأزرار', type: 'seg', value: 'secondary', choices: [
      { value: 'secondary', label: 'ثانوي' },
      { value: 'ghost', label: 'هادئ' }
    ] }
  ],
  extra: [
    { group: 'الأساس', first: true, name: 'ins-btn-group', desc: 'يضم الأزرار المتجاورة في قطعة واحدة.' },
    { group: 'الأساس', name: 'role="group"', desc: 'ومعها `aria-label`، فيعرف قارئ الشاشة أن الأزرار مجموعة واحدة وما هي.' }
  ],
  html: function (s) {
    var btn = 'ins-btn ins-btn--' + s.look + (s.size ? ' ins-btn--sm' : '');
    return '<div class="ins-btn-group' + (s.size ? ' ins-btn-group--sm' : '') + '" role="group" aria-label="التنقل بين الأيام">\n' +
      '  <button class="' + btn + '">السابق</button>\n' +
      '  <button class="' + btn + '">اليوم</button>\n' +
      '  <button class="' + btn + '">التالي</button>\n' +
      '</div>';
  },
  notes: function (s) {
    return s.size ? ['يصغّر `ins-btn-group--sm` انحناء طرفي المجموعة فقط، فضع `ins-btn--sm` على أزرارها أيضًا.'] : [];
  }
});

SitePlay.define({
  id: 'close',
  title: 'زر الإغلاق',
  owns: 'ins-close',
  controls: [
    { key: 'size', label: 'المقاس', type: 'seg', value: '', choices: [
      { value: '', label: 'عادي' },
      { value: 'sm', label: 'صغير', name: 'ins-close--sm', desc: 'زر أصغر، لتنبيه أو رقاقة.' }
    ] },
    { key: 'bare', label: 'بلا خلفية', type: 'switch', name: 'ins-close--bare', desc: 'يزيل الخلفية، لزر يسكن داخل تنبيه أو لوح.' },
    { key: 'dismiss', label: 'يغلق ما يحتويه', group: 'يغلق ما يحتويه', type: 'switch', name: 'data-ins-dismiss', desc: 'يغلق أقرب تنبيه أو مربع حوار أو درج يحتويه، أو ما تسميه له: `data-ins-dismiss="#id"`.' }
  ],
  extra: [
    { group: 'الأساس', first: true, name: 'ins-close', desc: 'يرسم علامة الإغلاق بنفسه، فلا يحتاج أيقونة، ولا بد له من `aria-label`.' },
    { group: 'يغلق ما يحتويه', name: 'ins:dismiss', kind: 'event', desc: 'يُطلق عند الإغلاق، وفي `detail` ما أُغلق `target`.' }
  ],
  events: ['ins:dismiss'],
  html: function (s) {
    var btn = '<button class="ins-close' + (s.size ? ' ins-close--sm' : '') + (s.bare ? ' ins-close--bare' : '') + '"' +
      (s.dismiss ? ' data-ins-dismiss aria-label="إغلاق التنبيه"' : ' aria-label="إغلاق"') + '></button>';
    if (!s.dismiss) return btn;
    return '<div class="ins-alert ins-alert--info">\n' +
      '  <span class="ins-alert-ico"><svg class="ico"><use href="#i-info"/></svg></span>\n' +
      '  <div class="ins-alert-msg">\n' +
      '    <span class="ins-alert-text">تنبيه يغلقه الزر.</span>\n' +
      '    ' + btn + '\n' +
      '  </div>\n' +
      '</div>';
  },
  notes: function (s) {
    return s.dismiss ? ['اضغط الزر فيُغلق التنبيه ويظهر هنا الحدث، وغيّر أي خيار لتعيده.'] : [];
  }
});
