SitePlay.define({
  id: 'group',
  title: 'مجموعة الحقل',
  wide: true,
  owns: 'ins-input-group',
  controls: [
    { key: 'addon', label: 'الملحق', type: 'seg', value: 'text', choices: [
      { value: '', label: 'بلا' },
      { value: 'text', label: 'نص' },
      { value: 'icon', label: 'أيقونة' }
    ] },
    { key: 'side', label: 'موضع الملحق', type: 'seg', value: 'end', choices: [
      { value: 'start', label: 'قبل الحقل' },
      { value: 'end', label: 'بعده' }
    ] },
    { key: 'text', label: 'نص الملحق', type: 'text', value: 'ر.س' },
    { key: 'btn', label: 'زر ملحق', type: 'seg', value: '', choices: [
      { value: '', label: 'بلا' },
      { value: 'text', label: 'بنص', name: 'ins-btn', desc: 'زر داخل المجموعة، أصغر من الحقل بدرجة لتبقى زاويتاهما متوازيتين.' },
      { value: 'icon', label: 'بأيقونة', name: 'ins-btn--icon', desc: 'زر مربع بأيقونة وحدها، ولا بد له من `aria-label`.' }
    ] },
    { key: 'bad', label: 'خطأ', type: 'switch', name: 'aria-invalid="true"', desc: 'على الحقل داخل المجموعة، فتتلون المجموعة كلها، ومعه رسالة `.ins-error` تحتها.' },
    { key: 'ltr', label: 'باتجاه LTR', type: 'switch', level: 'advanced', name: 'dir="ltr"', desc: 'على المجموعة كلها، لعنوان موقع أو اسم مستخدم، فتبقى البادئة قبل النص.' }
  ],
  extra: [
    { group: 'الأساس', first: true, name: 'ins-input-group', desc: 'يضم الحقل وملحقاته في قطعة واحدة، لها إطار واحد وحلقة تركيز واحدة.' },
    { group: 'الملحق', name: 'ins-addon', desc: 'نص أو أيقونة ثابتة قبل الحقل أو بعده، وترتيبه في الشيفرة هو ترتيبه في اتجاه القراءة.' }
  ],
  disable: function (s) { return s.addon ? (s.addon === 'icon' ? ['text'] : []) : ['side', 'text']; },
  html: function (s) {
    var input = '    <input class="ins-input" id="p-group"' + (s.ltr ? ' placeholder="my-site"' : ' inputmode="decimal" value="249"') + (s.bad ? ' aria-invalid="true"' : '') + '>';
    var addon = s.addon === 'icon' ? '    <span class="ins-addon"><svg class="ico"><use href="#i-wallet"/></svg></span>'
      : '    <span class="ins-addon">' + s.text + '</span>';
    var parts = [input];
    if (s.addon && s.side === 'start') parts.unshift(addon);
    if (s.addon && s.side === 'end') parts.push(addon);
    if (s.btn === 'text') parts.push('    <button class="ins-btn ins-btn--secondary">تطبيق</button>');
    if (s.btn === 'icon') parts.push('    <button class="ins-btn ins-btn--secondary ins-btn--icon" aria-label="تطبيق"><svg class="ico"><use href="#i-check"/></svg></button>');
    return '<div class="ins-field">\n' +
      '  <label class="ins-label" for="p-group">' + (s.ltr ? 'الرابط' : 'المبلغ') + '</label>\n' +
      '  <div class="ins-input-group"' + (s.ltr ? ' dir="ltr"' : '') + '>\n' + parts.join('\n') + '\n  </div>\n' +
      (s.bad ? '  <p class="ins-error">رسالة الخطأ.</p>\n' : '') +
      '</div>';
  },
  notes: function (s) {
    var out = [];
    if (s.addon && s.side === 'start' && !s.ltr) out.push('الملحق قبل الحقل في الشيفرة، فهو في أوله على يمين الصفحة العربية.');
    if (s.ltr) out.push('صارت المجموعة من اليسار إلى اليمين، فالملحق قبل الحقل يبقى على يساره.');
    return out;
  }
});

SitePlay.define({
  id: 'password',
  title: 'إظهار كلمة المرور',
  wide: true,
  owns: 'ins-password-toggle',
  controls: [
    { key: 'target', label: 'يجد الحقل', type: 'seg', value: '', choices: [
      { value: '', label: 'في المجموعة' },
      { value: 'id', label: 'بمعرّفه', name: 'data-ins-password="#id"', desc: 'يسمي الحقل الذي يظهره الزر، إذا لم يكن حقل كلمة المرور في المجموعة نفسها.' }
    ] }
  ],
  extra: [
    { group: 'الأساس', first: true, name: 'ins-password-toggle', desc: 'زر العين في المجموعة، يرسم أيقونته بنفسه، ولا يظهر إذا غابت جافاسكربت.' },
    { group: 'الأساس', name: 'data-ins-password', desc: 'يجعل الزر يبدل نوع الحقل (`type`)، فتظهر كلمة المرور أو تختفي.' },
    { group: 'الأساس', name: 'aria-pressed', desc: 'يضعها السكربت ويبدلها، فيعرف قارئ الشاشة أظاهرة كلمة المرور أم مخفية.' }
  ],
  html: function (s) {
    if (s.target) {
      return '<div class="ins-field">\n' +
        '  <label class="ins-label" for="p-pass">كلمة المرور</label>\n' +
        '  <input class="ins-input" id="p-pass" type="password">\n' +
        '</div>\n' +
        '<button class="ins-password-toggle" data-ins-password="#p-pass" aria-label="إظهار كلمة المرور"></button>';
    }
    return '<div class="ins-field">\n' +
      '  <label class="ins-label" for="p-pass">كلمة المرور</label>\n' +
      '  <div class="ins-input-group">\n' +
      '    <input class="ins-input" id="p-pass" type="password">\n' +
      '    <button class="ins-password-toggle" data-ins-password aria-label="إظهار كلمة المرور"></button>\n' +
      '  </div>\n' +
      '</div>';
  },
  stage: function (view) { view.querySelector('input').value = 'secret'; },
  notes: function (s) {
    var out = ['اضغط العين، فتظهر كلمة المرور، ثم اضغطها ثانية لتخفيها.'];
    if (s.target) out.push('الزر هنا خارج الحقل، فيسمي الحقل بمعرّفه.');
    return out;
  }
});

SitePlay.define({
  id: 'stepper',
  title: 'العداد',
  owns: 'ins-spin',
  controls: [
    { key: 'step', label: 'الخطوة', type: 'seg', value: '', choices: [
      { value: '', label: '1' },
      { value: '5', label: '5' },
      { value: '0.5', label: '0.5' }
    ] },
    { key: 'limits', label: 'حد أدنى وأعلى', type: 'switch', value: true, name: 'min', desc: 'ومعها `max`: لا يتجاوزهما الزران، ويتعطل كل منهما عند حده.' },
    { key: 'disabled', label: 'معطل', type: 'switch', name: 'disabled', desc: 'على الحقل، فيتعطل الزران معه.' }
  ],
  extra: [
    { group: 'الأساس', first: true, name: 'ins-spin', desc: 'زر الإنقاص أو الزيادة، على جانبي حقل رقمي في `.ins-input-group`.' },
    { group: 'الأساس', name: 'data-ins-spin="down"', desc: 'زر الإنقاص، قبل الحقل.' },
    { group: 'الأساس', name: 'data-ins-spin="up"', desc: 'زر الزيادة، بعد الحقل.' },
    { group: 'الخطوة', name: 'step', desc: 'مقدار الزيادة والإنقاص في كل ضغطة، كما يفهمه المتصفح.' }
  ],
  html: function (s) {
    var attrs = (s.step ? ' step="' + s.step + '"' : '') + (s.limits ? ' min="0" max="10"' : '') + (s.disabled ? ' disabled' : '');
    var value = s.step === '5' ? '5' : s.step === '0.5' ? '2.5' : '2';
    return '<div class="ins-field">\n' +
      '  <label class="ins-label" for="p-qty">الكمية</label>\n' +
      '  <div class="ins-input-group">\n' +
      '    <button class="ins-spin" data-ins-spin="down" aria-label="إنقاص"></button>\n' +
      '    <input class="ins-input" id="p-qty" type="number"' + attrs + ' value="' + value + '">\n' +
      '    <button class="ins-spin" data-ins-spin="up" aria-label="زيادة"></button>\n' +
      '  </div>\n' +
      '</div>';
  }
});
