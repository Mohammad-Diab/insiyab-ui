SitePlay.define({
  id: 'validate',
  title: 'تحقق المتصفح',
  wide: true,
  owns: 'ins-error',
  controls: [
    { key: 'rule', label: 'القيد', type: 'select', value: 'required', choices: [
      { value: 'required', label: 'مطلوب' },
      { value: 'email', label: 'بريد صحيح' },
      { value: 'minlength', label: 'ثمانية أحرف على الأقل' },
      { value: 'pattern', label: 'أرقام فقط' }
    ] },
    { key: 'msg', label: 'الرسالة', type: 'seg', value: '', choices: [
      { value: '', label: 'رسالة المتصفح', name: 'p.ins-error', kind: 'element', desc: 'فارغًا: يملؤه المتصفح برسالته وبلغته، ويجددها وأنت تكتب.' },
      { value: 'own', label: 'رسالتك', name: 'ins-error', desc: 'رسالة الخطأ تحت الحقل. فإن كتبت فيها نصًا ظهرت رسالتك كما كتبتها.' }
    ] },
    { key: 'hint', label: 'تلميح', type: 'switch' }
  ],
  extra: [
    { group: 'الأساس', first: true, name: 'data-ins-validate', desc: 'على `<form>`: يأخذ حكم المتصفح على قيود الحقول ويكتب رسالته تحت الحقل بدل فقاعته. ولا يظهر الخطأ إلا إذا غادر المستخدم حقلًا غيّره أو حاول الإرسال.' }
  ],
  html: function (s) {
    var attrs = s.rule === 'email' ? ' type="email" dir="ltr" required' : s.rule === 'minlength' ? ' type="password" required minlength="8"'
      : s.rule === 'pattern' ? ' inputmode="numeric" pattern="[0-9]+" required' : ' required';
    var label = s.rule === 'email' ? 'البريد' : s.rule === 'minlength' ? 'كلمة المرور' : s.rule === 'pattern' ? 'الرمز' : 'الاسم';
    var own = s.rule === 'email' ? 'اكتب بريدًا صحيحًا.' : s.rule === 'minlength' ? 'كلمة المرور قصيرة.' : s.rule === 'pattern' ? 'اكتب أرقامًا فقط.' : 'اكتب الاسم.';
    var hint = s.rule === 'minlength' ? 'ثمانية أحرف على الأقل.' : 'تلميح قصير.';
    return '<form class="ins-stack" data-ins-validate>\n' +
      '  <div class="ins-field">\n' +
      '    <label class="ins-label" for="p-val">' + label + '</label>\n' +
      '    <input class="ins-input" id="p-val"' + attrs + '>\n' +
      (s.hint ? '    <span class="ins-hint">' + hint + '</span>\n' : '') +
      '    <p class="ins-error">' + (s.msg ? own : '') + '</p>\n' +
      '  </div>\n' +
      '  <div class="ins-form-commit">\n' +
      '    <button class="ins-btn ins-btn--primary">إرسال</button>\n' +
      '  </div>\n' +
      '</form>';
  },
  stage: function (view) {
    var input = view.querySelector('input'), msg = view.querySelector('.ins-error');
    input.setAttribute('data-ins-reported', '');
    if (!msg.textContent.trim()) { msg.setAttribute('data-ins-auto', ''); msg.setAttribute('dir', 'auto'); msg.textContent = input.validationMessage; }
  },
  notes: function (s) {
    var out = ['هكذا يبدو الحقل بعد محاولة إرسال فاشلة. اكتب فيه، فتتغير الرسالة أو تزول.'];
    if (s.hint) out.push('يربط السكربت التلميح والرسالة بالحقل في `aria-describedby`، فيقرؤهما قارئ الشاشة معه.');
    return out;
  }
});

SitePlay.define({
  id: 'server',
  title: 'أخطاء من الخادم',
  wide: true,
  owns: 'ins-success',
  controls: [
    { key: 'state', label: 'الحالة', type: 'seg', value: 'bad', choices: [
      { value: 'bad', label: 'خطأ', name: 'aria-invalid="true"', desc: 'على الحقل أو القائمة، ومعه نص في `.ins-error`: خطأ لا يعرفه المتصفح، كاسم محجوز.' },
      { value: 'ok', label: 'مقبول', name: 'ins-input--ok', desc: 'على الحقل، ومعه نص في `.ins-success`: قيمة تستحق التأكيد، كاسم متاح.' }
    ] },
    { key: 'el', label: 'العنصر', type: 'seg', value: 'input', choices: [
      { value: 'input', label: 'حقل' },
      { value: 'select', label: 'قائمة' }
    ] },
    { key: 'cls', label: 'بالكلاس وحده', type: 'switch', name: 'ins-input--bad', desc: 'بدل `aria-invalid`، لمن لا يملك أن يضيف إلا كلاسًا.' }
  ],
  extra: [
    { group: 'الأساس', first: true, name: 'ins-success', desc: 'رسالة القبول تحت الحقل، وتكفي وحدها ليخضرّ الحقل.' },
    { group: 'خيارات أخرى', name: 'ins-select--bad', desc: 'مثله للقائمة.' }
  ],
  disable: function (s) { return s.state === 'ok' ? ['cls'] : []; },
  html: function (s) {
    var bad = s.state === 'bad';
    var mark = bad ? (s.cls ? '' : ' aria-invalid="true"') : '';
    var cls = s.el === 'select' ? 'ins-select' + (bad && s.cls ? ' ins-select--bad' : '') : 'ins-input' + (bad && s.cls ? ' ins-input--bad' : '') + (bad ? '' : ' ins-input--ok');
    var ctl = s.el === 'select'
      ? '<select class="' + cls + '" id="p-srv"' + mark + '>\n    <option value="">اختر…</option>\n    <option' + (bad ? '' : ' selected') + '>الأول</option>\n    <option>الثاني</option>\n  </select>'
      : '<input class="' + cls + '" id="p-srv" value="name" dir="ltr"' + mark + '>';
    var msg = bad ? '<p class="ins-error">' + (s.el === 'select' ? 'اختر واحدًا.' : 'هذا الاسم محجوز.') + '</p>'
      : '<p class="ins-success">' + (s.el === 'select' ? 'اختيار صحيح.' : 'الاسم متاح.') + '</p>';
    return '<div class="ins-field">\n' +
      '  <label class="ins-label" for="p-srv">' + (s.el === 'select' ? 'الخيار' : 'اسم المستخدم') + '</label>\n' +
      '  ' + ctl + '\n' +
      '  ' + msg + '\n' +
      '</div>';
  },
  notes: function (s) {
    var out = [];
    if (s.state === 'ok' && s.el === 'select') out.push('لا كلاس قبول للقائمة، فتكفيها رسالة `.ins-success`.');
    if (s.state === 'bad' && s.cls) out.push('يبدو الحقل كما كان، والفرق أن قارئ الشاشة لا يسمع أن فيه خطأ، فضع `aria-invalid` متى استطعت.');
    return out;
  }
});
