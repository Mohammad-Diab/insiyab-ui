SitePlay.define({
  id: 'steps',
  title: 'الخطوات',
  owns: 'ins-steps',
  wide: true,
  controls: [
    { key: 'look', label: 'الشكل', type: 'seg', value: '', choices: [
      { value: '', label: 'أرقام' },
      { value: 'dots', label: 'نقاط', name: 'ins-steps--dots', desc: 'نقاط صغيرة مكان الأرقام تليق ببطاقة الدخول، وتبقى الأسماء لقارئ الشاشة.' },
      { value: 'grid', label: 'شبكة', name: 'ins-steps--grid', desc: 'أرقام تلتف على الأسطر بلا خطوط تصلها، لخطوات كثيرة لا تظهر أسماؤها، كأسئلة نموذج طويل. واجعل `.ins-steps-label` في كل عنصر رابطًا أو زرًا، فتصير الدائرة كلها ذلك العنصر.' }
    ] },
    { key: 'at', label: 'الخطوة الحالية', type: 'seg', value: '2', choices: [
      { value: '1', label: '1' },
      { value: '2', label: '2' },
      { value: '3', label: '3' },
      { value: '4', label: '4' }
    ] },
    { key: 'flagged', label: 'خطوة مؤجلة', group: 'حالات أخرى', type: 'switch', name: 'is-flagged', kind: 'class', desc: 'نقطة صفراء على خطوة مؤجلة يعود إليها المستخدم، وتجتمع مع المنجزة والحالية.' },
    { key: 'error', label: 'خطوة فيها خطأ', group: 'حالات أخرى', type: 'switch', name: 'is-error', kind: 'class', desc: 'دائرة حمراء على خطوة فيها ما يحتاج إلى إصلاح، وتجتمع مع المنجزة والحالية.' }
  ],
  extra: [
    { group: 'الأساس', first: true, name: 'ins-steps', desc: 'قائمة `<ol>` مرقمة لا تحتاج إلى سكربت، تصلح وحدها لمسار موزع على عدة صفحات من الخادم.' },
    { group: 'الأساس', name: 'ins-steps-item', desc: 'عنصر الخطوة، ورقمها يكتبه CSS بنفسه.' },
    { group: 'الأساس', name: 'ins-steps-label', desc: 'اسم الخطوة. وعلى الهاتف لا يظهر إلا اسم الحالية، وتبقى البقية لقارئ الشاشة.' },
    { group: 'الأساس', name: 'is-done', kind: 'class', desc: 'خطوة منجزة، يمتلئ رقمها بلون الهوية.' },
    { group: 'الأساس', name: 'aria-current="step"', desc: 'الخطوة الحالية، ويعلنها قارئ الشاشة.' }
  ],
  disable: function (s) {
    var off = s.look === 'dots' ? ['flagged', 'error'] : [];
    if (s.at === '1' && off.indexOf('error') === -1) off.push('error');
    return off;
  },
  html: function (s) {
    var grid = s.look === 'grid', at = +s.at;
    var names = ['الأولى', 'الثانية', 'الثالثة', 'الرابعة'];
    var out = '<ol class="ins-steps' + (s.look ? ' ins-steps--' + s.look : '') + '">\n';
    for (var i = 1; i <= (grid ? 12 : 4); i++) {
      var cls = 'ins-steps-item' + (i < at ? ' is-done' : '') + (s.flagged && i === 1 ? ' is-flagged' : '') + (s.error && i === at - 1 ? ' is-error' : '');
      var label = grid ? '<button class="ins-steps-label" type="button">البند ' + i + '</button>' : '<span class="ins-steps-label">' + names[i - 1] + '</span>';
      out += '  <li class="' + cls + '"' + (i === at ? ' aria-current="step"' : '') + '>' + label + '</li>\n';
    }
    return out + '</ol>';
  },
  notes: function (s) {
    var out = [];
    if (s.look === 'grid') out.push('تضع الصفحة الحالات بنفسها: منجزة لما أُجيب عنه، وحالية للمفتوح، ومؤجلة لما ينتظر المراجعة.');
    if (s.look === 'dots') out.push('لا تظهر النقطة المؤجلة ولا الخطأ في هذا الشكل، فالنقاط لا تسع إلا المنجز والحالي.');
    return out;
  }
});

SitePlay.define({
  id: 'wizard',
  title: 'المعالج',
  owns: 'ins-wizard',
  wide: true,
  controls: [
    { key: 'body', label: 'ارتفاع يتدرج', type: 'switch', value: true, name: 'ins-wizard-body', desc: 'حاوية اختيارية حول الألواح، يتدرج بها ارتفاع المعالج بسلاسة بين خطوة وأخرى.' },
    { key: 'slide', label: 'خطوات تنزلق', type: 'switch', name: 'data-ins-wizard-motion="slide"', desc: 'على المعالج، ومعه `.ins-wizard-body`: تنزلق الخطوات جنبًا إلى جنب بعرضها كاملًا، كما في بطاقة الدخول.' },
    { key: 'validate', label: 'رسائل تحت الحقول', type: 'switch', value: true, name: 'data-ins-validate', desc: 'على النموذج: تظهر رسالة كل حقل ناقص تحته. وبدونها يعرض المتصفح رسالة الحقل الأول وحده في فقاعته.' },
    { key: 'skip', label: 'تخطي خطوة', type: 'switch', name: 'hidden', desc: 'على لوح: يتخطاه المعالج ويسقط رقمه من الخطوات، فيصلح لمسار يحدد الخادم طوله.' },
    { key: 'focus', label: 'تركيز على حقل', type: 'switch', name: 'autofocus', desc: 'على حقل في اللوح: يأخذ التركيز حين تبلغ خطوته، بدل أن يذهب إلى اللوح نفسه.' },
    { key: 'echo', label: 'عرض قيمة سابقة', type: 'switch', name: 'data-ins-echo="#id"', desc: 'على عنصر: يعرض فيه قيمة حقل من خطوة سابقة، كرقم الهاتف في خطوة الرمز.' },
    { key: 'restart', label: 'زر البدء من جديد', type: 'switch', name: 'data-ins-wizard="restart"', desc: 'على زر: يعيد المعالج إلى الخطوة الأولى.' },
    { key: 'dialog', label: 'داخل مربع حوار', type: 'switch', name: 'data-ins-dialog-close', desc: 'يعمل المعالج نفسه داخل `.ins-dialog`، فيوضع زر الإلغاء بهذه السمة في أول الشريط السفلي، وزرا السابق والتالي في آخره.' }
  ],
  extra: [
    { group: 'الأساس', first: true, name: 'ins-wizard', desc: 'على نموذج مقسوم إلى ألواح، يُظهر منها لوحًا واحدًا في كل مرة ويقود خطوات `.ins-steps` فوقه. وبلا جافاسكربت يعود نموذجًا واحدًا في آخره زر إرسال.' },
    { group: 'الأساس', name: 'ins-wizard-panel', desc: 'لوح الخطوة، ويُسمى بـ `aria-label`.' },
    { group: 'الأساس', name: 'ins-wizard-foot', desc: 'شريط الأزرار تحت الألواح.' },
    { group: 'الأساس', name: 'data-ins-wizard="next"', desc: 'زر «التالي»: لا ينتقل حتى يتحقق من حقول الخطوة، ومفتاح Enter داخل الحقل يعمل عمله.' },
    { group: 'الأساس', name: 'data-ins-wizard="prev"', desc: 'زر «السابق»، ويختفي في الخطوة الأولى.' },
    { group: 'الأساس', name: 'ins-wizard-finish', desc: 'زر الإرسال، ولا يظهر إلا في الخطوة الأخيرة.' },
    { group: 'الأحداث', name: 'ins:wizard-leave', kind: 'event', desc: 'يُطلق قبل أن ينتقل «التالي». وإذا استدعى مستمعك `preventDefault()` انتظر المعالج مشغولًا حتى تستدعي `Insiyab.wizard(w, \'next\')` أو `\'stay\'`.' },
    { group: 'الأحداث', name: 'ins:wizard', kind: 'event', level: 'advanced', desc: 'يُطلق بعد كل انتقال، وفي `detail` المعالج `el` ورقم الخطوة `step` ولوحها `panel`.' }
  ],
  events: ['ins:wizard', 'ins:wizard-leave'],
  disable: function (s) {
    var off = [];
    if (!s.body) off.push('slide');
    if (s.skip) off.push('focus');
    return off;
  },
  html: function (s) {
    var d = s.dialog ? '  ' : '';
    var i2 = d + '  ', i3 = i2 + (s.body ? '  ' : '');
    var err = s.validate ? '\n' + i3 + '    <p class="ins-error"></p>' : '';
    var steps = i2 + '<ol class="ins-steps">\n' +
      i2 + '  <li class="ins-steps-item"><span class="ins-steps-label">الاسم</span></li>\n' +
      i2 + '  <li class="ins-steps-item"><span class="ins-steps-label">البريد</span></li>\n' +
      i2 + '  <li class="ins-steps-item"><span class="ins-steps-label">التأكيد</span></li>\n' +
      i2 + '</ol>\n';
    var panels = i3 + '<section class="ins-wizard-panel" aria-label="الاسم">\n' +
      i3 + '  <div class="ins-field">\n' +
      i3 + '    <label class="ins-label" for="wp-name">الاسم</label>\n' +
      i3 + '    <input class="ins-input" id="wp-name" required>' + err + '\n' +
      i3 + '  </div>\n' +
      i3 + '</section>\n' +
      i3 + '<section class="ins-wizard-panel" aria-label="البريد"' + (s.skip ? ' hidden' : '') + '>\n' +
      i3 + '  <div class="ins-field">\n' +
      i3 + '    <label class="ins-label" for="wp-email">البريد</label>\n' +
      i3 + '    <input class="ins-input" id="wp-email" type="email" dir="ltr" required' + (s.skip ? ' disabled' : s.focus ? ' autofocus' : '') + '>' + err + '\n' +
      i3 + '  </div>\n' +
      i3 + '</section>\n' +
      i3 + '<section class="ins-wizard-panel ins-stack" aria-label="التأكيد">\n' +
      (s.echo ? i3 + '  <p class="ins-m-0">الاسم: <b data-ins-echo="#wp-name"></b></p>\n' : '') +
      i3 + '  <label class="ins-check"><input type="checkbox" required><span class="ins-check-text">أوافق</span></label>\n' +
      (s.restart ? i3 + '  <button type="button" class="ins-btn ins-btn--bare" data-ins-wizard="restart">البدء من جديد</button>\n' : '') +
      i3 + '</section>\n';
    var body = s.body ? i2 + '<div class="ins-wizard-body">\n' + panels + i2 + '</div>\n' : panels;
    var foot = i2 + '<div class="' + (s.dialog ? 'ins-dialog-foot ' : '') + 'ins-wizard-foot">\n' +
      (s.dialog ? i2 + '  <button type="button" class="ins-btn ins-btn--bare" data-ins-dialog-close>إلغاء</button>\n' : '') +
      i2 + '  <button type="button" class="ins-btn ins-btn--secondary" data-ins-wizard="prev">السابق</button>\n' +
      i2 + '  <button type="button" class="ins-btn ins-btn--primary" data-ins-wizard="next">التالي</button>\n' +
      i2 + '  <button class="ins-btn ins-btn--primary ins-wizard-finish">إرسال</button>\n' +
      i2 + '</div>\n';
    var open = d + '<form class="ins-wizard"' + (s.validate ? ' data-ins-validate' : '') + (s.body && s.slide ? ' data-ins-wizard-motion="slide"' : '') + '>\n';
    if (!s.dialog) return open + steps + body + foot + '</form>';
    return '<button class="ins-btn ins-btn--primary" data-ins-dialog="#wp-dialog">فتح المعالج</button>\n' +
      '<dialog class="ins-dialog" id="wp-dialog" aria-labelledby="wp-dialog-title">\n' + open +
      i2 + '<div class="ins-dialog-head">\n' +
      i2 + '  <h3 class="ins-dialog-title" id="wp-dialog-title">إضافة</h3>\n' +
      i2 + '  <button type="button" class="ins-close" data-ins-dialog-close aria-label="إغلاق"></button>\n' +
      i2 + '</div>\n' +
      i2 + '<div class="ins-dialog-body">\n' +
      (steps + body).replace(/^(?=.)/gm, '  ') +
      i2 + '</div>\n' + foot +
      '  </form>\n</dialog>';
  },
  stage: function (view, s) {
    var w = view.querySelector('.ins-wizard');
    var name = w.querySelector('.ins-wizard-panel input');
    var panels = w.querySelectorAll('.ins-wizard-panel:not([hidden])');
    name.value = 'سارة';
    if (s.echo || s.restart || s.skip) {
      Insiyab.wizard(w, panels.length - 1);
      return;
    }
    Insiyab.wizard(w, 1);
    var email = panels[1].querySelector('input');
    var msg = email.parentNode.querySelector('.ins-error');
    email.value = 'name@';
    if (!msg) return;
    email.setAttribute('data-ins-reported', '');
    msg.setAttribute('dir', 'auto');
    msg.setAttribute('data-ins-auto', '');
    msg.textContent = email.validationMessage;
  },
  notes: function (s) {
    if (s.dialog) return ['اضغط «فتح المعالج» لترى المعالج بخياراتك في مربع الحوار.'];
    var out = [];
    var last = s.echo || s.restart || s.skip;
    if (last) out.push('المعالج في خطوته الأخيرة، فاضغط «السابق» أو «إرسال» يظهر هنا الحدث.');
    else out.push('المعالج في خطوته الثانية وفي البريد خطأ، فصححه واضغط «التالي» يظهر هنا الحدث.');
    if (!s.validate && !last) out.push('بلا `data-ins-validate` لا تظهر الرسالة تحت الحقل، ويعرضها المتصفح في فقاعة حين تضغط «التالي».');
    if (!s.body) out.push('بلا `.ins-wizard-body` يقفز ارتفاع المعالج بين الخطوات ولا يتدرج، فاضغط «السابق» لترى ذلك.');
    if (s.slide) out.push('اضغط «السابق» أو «التالي» فتنزلق الخطوات جنبًا إلى جنب.');
    if (s.focus) out.push('اضغط «السابق» ثم «التالي»، فيأخذ حقل البريد التركيز حين تبلغ خطوته.');
    if (s.skip) out.push('تخطى المعالج لوح البريد وسقط رقمه من الخطوات. وحقله معطل، لأن الحقل المطلوب في لوح مخفي يمنع الإرسال.');
    return out;
  }
});
