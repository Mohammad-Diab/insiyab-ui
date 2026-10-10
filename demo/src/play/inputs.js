SitePlay.define({
  id: 'field',
  title: 'الحقل',
  wide: true,
  owns: 'ins-input',
  controls: [
    { key: 'el', label: 'العنصر', type: 'seg', value: 'input', choices: [
      { value: 'input', label: 'حقل' },
      { value: 'select', label: 'قائمة', name: 'ins-select', desc: 'لقائمة `<select>`. وحيث يسمح المتصفح (كروم وإيدج) تفتح بطاقة زجاجية كقوائم المكتبة، وهي مع ذلك القائمة الأصلية نفسها.' },
      { value: 'textarea', label: 'نص طويل', name: 'ins-textarea', desc: 'لمربع النص الطويل `<textarea>`، ويطول بالسحب.' }
    ] },
    { key: 'type', label: 'النوع', type: 'select', value: 'text', choices: [
      { value: 'text', label: 'نص' },
      { value: 'email', label: 'بريد' },
      { value: 'tel', label: 'هاتف' },
      { value: 'number', label: 'رقم' },
      { value: 'password', label: 'كلمة مرور' }
    ] },
    { key: 'size', label: 'المقاس', type: 'seg', value: '', choices: [
      { value: 'sm', label: 'صغير', name: 'ins-input--sm', desc: 'حقل أصغر، للجداول والأشرطة.' },
      { value: '', label: 'عادي' },
      { value: 'lg', label: 'كبير', name: 'ins-input--lg', desc: 'حقل أكبر، لصفحة دخول أو بحث هو كل الصفحة.' }
    ] },
    { key: 'state', label: 'الحالة', type: 'select', value: '', choices: [
      { value: '', label: 'عادي' },
      { value: 'disabled', label: 'معطل', name: 'disabled', desc: 'يعطل الحقل، فلا يُكتب فيه ولا يُرسل مع النموذج.' },
      { value: 'readonly', label: 'للقراءة فقط', name: 'readonly', desc: 'تُقرأ قيمة الحقل وتُنسخ ولا تُغيَّر، وتُرسل مع النموذج.' },
      { value: 'bad', label: 'خطأ', name: 'aria-invalid="true"', desc: 'يدل على الخطأ، ومعه رسالة `.ins-error` تحت الحقل. وتجد التحقق التلقائي في صفحة التحقق.' },
      { value: 'ok', label: 'مقبول', name: 'ins-input--ok', desc: 'يدل على قبول القيمة، ومعه رسالة `.ins-success`، وتكفي الرسالة وحدها للقائمة ومربع النص.' }
    ] },
    { key: 'count', label: 'العداد', type: 'seg', value: '', choices: [
      { value: '', label: 'بلا' },
      { value: 'chars', label: 'أحرف', name: 'data-ins-count', desc: 'يعد تحت الحقل ما كُتب من أصل `maxlength`، ويصفرّ إذا دنا من الحد ويحمرّ إذا تجاوزه.' },
      { value: 'words', label: 'كلمات', name: 'data-ins-count="words"', desc: 'يعد الكلمات من أصل `data-ins-max`، فإذا تجاوزها صار الحقل غير صالح وامتنع النموذج عن الإرسال.' }
    ] },
    { key: 'label', label: 'التسمية', type: 'text', value: 'الاسم' },
    { key: 'hint', label: 'تلميح', type: 'switch', name: 'ins-hint', desc: 'سطر قصير تحت الحقل يشرح ما يُكتب فيه.' },
    { key: 'req', label: 'مطلوب', type: 'switch', name: 'ins-req', desc: 'علامة الحقل المطلوب بعد التسمية، ومعها `aria-hidden`. أما `required` على الحقل فهو ما يقرؤه قارئ الشاشة.' }
  ],
  extra: [
    { group: 'الأساس', first: true, name: 'ins-input', desc: 'الحقل نفسه، على `<input>` بأي نوع.' },
    { group: 'الأساس', name: 'ins-label', desc: 'التسمية، على `<label for>` حقيقي: إذا نقرتها انتقل التركيز إلى الحقل، وإذا بلغها قارئ الشاشة قرأها.' },
    { group: 'النوع', name: 'dir="ltr"', desc: 'على حقل البريد أو الهاتف وحده، فيُكتب من اليسار ويبقى سائر النموذج عربيًا.' },
    { group: 'المقاس', name: 'ins-select--sm', desc: 'قائمة صغيرة، تجاور `ins-input--sm`. ولا مقاس لمربع النص الطويل.' },
    { group: 'المقاس', name: 'ins-select--lg', desc: 'قائمة كبيرة، تجاور `ins-input--lg`.' },
    { group: 'الحالة', name: 'ins-input--bad', desc: 'مثل `aria-invalid="true"`، لمن لا يملك أن يضيف إلا كلاسًا، ومثله `ins-select--bad` للقائمة.' },
    { group: 'العداد', name: 'data-ins-max', desc: 'حد الكلمات للعداد، ويحد الأحرف أيضًا إذا غاب `maxlength`.' },
    { group: 'العداد', name: 'data-ins-count-el', level: 'advanced', desc: 'معرّف عنصر تختاره ليكتب فيه العداد بدل الذي تضيفه المكتبة.' },
    { group: 'العداد', name: 'data-ins-text', level: 'advanced', desc: 'على ذلك العنصر: صيغة العداد، مثل `{n} من {max}`.' }
  ],
  disable: function (s) {
    var off = [];
    if (s.el !== 'input') off.push('type');
    if (s.el === 'textarea') off.push('size');
    if (s.el === 'select') off.push('count');
    return off;
  },
  html: function (s) {
    var tag = s.el;
    var cls = 'ins-' + tag + (s.size && tag !== 'textarea' ? ' ins-' + tag + '--' + s.size : '') + (s.state === 'ok' && tag === 'input' ? ' ins-input--ok' : '');
    var attrs = ' id="p-field"';
    var ltr = tag === 'input' && (s.type === 'email' || s.type === 'tel');
    if (tag === 'input' && s.type !== 'text') attrs += ' type="' + s.type + '"';
    if (ltr) attrs += ' dir="ltr"';
    if (s.req) attrs += ' required';
    if (s.count === 'chars' && tag !== 'select') attrs += ' maxlength="60" data-ins-count';
    if (s.count === 'words' && tag !== 'select') attrs += ' data-ins-count="words" data-ins-max="50"';
    if (s.state === 'disabled') attrs += ' disabled';
    if (s.state === 'readonly' && tag !== 'select') attrs += ' readonly';
    if (s.state === 'bad') attrs += ' aria-invalid="true"';
    var value = s.state === 'disabled' || s.state === 'readonly' || s.state === 'bad' || s.state === 'ok' ? 'نص قصير' : '';
    if (tag === 'input' && s.type === 'email' && value) value = 'name@example.com';
    if (tag === 'input' && s.type === 'tel' && value) value = '+966 50 000 0000';
    if (tag === 'input' && s.type === 'number' && value) value = '12';
    if (tag === 'input' && s.type === 'password') value = '';
    if (ltr && !value) attrs += ' placeholder="' + (s.type === 'email' ? 'name@example.com' : '+966 50 000 0000') + '"';
    var ctl;
    if (tag === 'select') ctl = '<select class="' + cls + '"' + attrs + '>\n    <option>الأول</option>\n    <option>الثاني</option>\n    <option>الثالث</option>\n  </select>';
    else if (tag === 'textarea') ctl = '<textarea class="' + cls + '"' + attrs + '>' + value + '</textarea>';
    else ctl = '<input class="' + cls + '"' + attrs + (value ? ' value="' + value + '"' : '') + '>';
    var out = '<div class="ins-field">\n' +
      '  <label class="ins-label" for="p-field">' + s.label + (s.req ? '<span class="ins-req" aria-hidden="true">*</span>' : '') + '</label>\n' +
      '  ' + ctl + '\n';
    if (s.hint) out += '  <span class="ins-hint">تلميح قصير.</span>\n';
    if (s.state === 'bad') out += '  <p class="ins-error">رسالة الخطأ.</p>\n';
    if (s.state === 'ok') out += '  <p class="ins-success">رسالة القبول.</p>\n';
    return out + '</div>';
  },
  stage: function (view, s) {
    var input = view.querySelector('input');
    if (s.el === 'input' && !input.value && (s.type === 'number' || s.type === 'password')) input.value = s.type === 'number' ? '12' : 'secret';
  },
  notes: function (s) {
    var out = [];
    if (s.el === 'input' && (s.type === 'email' || s.type === 'tel')) out.push('يُكتب البريد والهاتف من اليسار، فأخذ الحقل وحده `dir="ltr"`.');
    if (s.el === 'select' && s.state === 'readonly') out.push('القائمة لا تعرف `readonly`، فإما أن تبقى كما هي أو تُعطَّل.');
    if (s.count && s.el !== 'select') out.push('اكتب في الحقل، فيتغير العداد تحته.');
    return out;
  }
});

SitePlay.define({
  id: 'search',
  title: 'حقل البحث',
  wide: true,
  owns: 'ins-search',
  controls: [
    { key: 'size', label: 'المقاس', type: 'seg', value: '', choices: [
      { value: 'sm', label: 'صغير' },
      { value: '', label: 'عادي' },
      { value: 'lg', label: 'كبير' }
    ] },
    { key: 'bar', label: 'مع زر', type: 'switch', name: 'ins-searchbar', desc: 'يضم حقل البحث وزره في سطر واحد بعرض محدود، ويدفعهما إلى آخر شريط الأدوات.' }
  ],
  extra: [
    { group: 'الأساس', first: true, name: 'ins-search', desc: 'يضع أيقونة البحث داخل الحقل في أوله، وتأخذ لون الهوية إذا تركّز فيه.' },
    { group: 'الأساس', name: 'ins-search-ico', desc: 'الأيقونة نفسها، على `<svg>` قبل الحقل.' }
  ],
  html: function (s) {
    var search = '<div class="ins-search">\n' +
      '  <svg class="ico ins-search-ico"><use href="#i-search"/></svg>\n' +
      '  <input class="ins-input' + (s.size ? ' ins-input--' + s.size : '') + '" type="search" placeholder="بحث" aria-label="بحث">\n' +
      '</div>';
    if (!s.bar) return search;
    return '<div class="ins-searchbar">\n  ' + search.replace(/\n/g, '\n  ') + '\n  <button class="ins-btn ins-btn--primary' + (s.size === 'sm' ? ' ins-btn--sm' : s.size === 'lg' ? ' ins-btn--lg' : '') + '">بحث</button>\n</div>';
  }
});

SitePlay.define({
  id: 'form',
  title: 'النموذج',
  wide: true,
  owns: 'ins-field',
  controls: [
    { key: 'layout', label: 'ترتيب الحقول', type: 'seg', value: '', choices: [
      { value: '', label: 'عمود' },
      { value: 'row', label: 'سطر', name: 'ins-field-row', desc: 'يجعل الحقول متجاورة في سطر يلتف إذا ضاق، وتلتقي رؤوسها على خط واحد.' },
      { value: 'grid', label: 'شبكة', name: 'ins-field-grid', desc: 'لحقول قصيرة كثيرة لا يسعها سطر: شبكة تتوازى حقولها مهما اختلفت تسمياتها.' }
    ] },
    { key: 'commit', label: 'سطر الحفظ', type: 'switch', value: true, name: 'ins-form-commit', desc: 'يجمع أزرار الحفظ في آخر النموذج، ويبدأ بالزر الرئيسي.' }
  ],
  extra: [
    { group: 'الأساس', first: true, name: 'ins-field', desc: 'يجمع التسمية والحقل والتلميح في عمود واحد.' }
  ],
  html: function (s) {
    var grow = s.layout === 'row' ? ' ins-grow' : '';
    var field = function (id, label) {
      return '    <div class="ins-field' + grow + '">\n' +
        '      <label class="ins-label" for="' + id + '">' + label + '</label>\n' +
        '      <input class="ins-input" id="' + id + '">\n' +
        '    </div>\n';
    };
    var wrap = s.layout === 'row' ? 'ins-field-row' : s.layout === 'grid' ? 'ins-field-grid' : 'ins-stack';
    var commit = s.commit ? '  <div class="ins-form-commit">\n    <button class="ins-btn ins-btn--primary">حفظ</button>\n  </div>\n' : '';
    return '<form>\n  <div class="' + wrap + '">\n' + field('p-f1', 'الاسم') + field('p-f2', 'المدينة') + field('p-f3', 'الهاتف') + '  </div>\n' + commit + '</form>';
  }
});
