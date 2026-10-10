SitePlay.define({
  id: 'dialog',
  title: 'مربع الحوار',
  owns: 'ins-dialog',
  controls: [
    { key: 'size', label: 'المقاس', type: 'seg', value: '', choices: [
      { value: 'sm', label: 'صغير', name: 'ins-dialog--sm', desc: 'لسؤال قصير، فيقع زراه تحته مباشرة.' },
      { value: '', label: 'عادي' },
      { value: 'lg', label: 'كبير', name: 'ins-dialog--lg', desc: 'لمحتوى عريض، كجدول أو نموذج من عمودين.' }
    ] },
    { key: 'title', label: 'العنوان', type: 'text', value: 'عنوان الحوار' },
    { key: 'foot', label: 'الذيل', group: 'الأجزاء', type: 'switch', value: true, name: 'ins-dialog-foot', desc: 'ذيل الحوار، وفيه أزراره مصفوفة إلى نهاية السطر.' },
    { key: 'form', label: 'فيه نموذج', type: 'switch', level: 'advanced', name: 'form', kind: 'element', desc: 'لحوار يجمع بيانات: ضع رأسه ومتنه وذيله في `<form method="dialog">`، فيغلقه أي زر في النموذج ويبقى المتن وحده يتمرر بين رأس وذيل ثابتين. وتجد قيمة الزر الذي أغلقه في `returnValue`.' },
    { key: 'closedby', label: 'ما يغلقه', type: 'select', value: '', group: 'الإغلاق', choices: [
      { value: '', label: 'Esc وأزراره' },
      { value: 'any', label: 'والنقر خارجه أيضًا', name: 'closedby="any"', desc: 'يغلقه النقر خارجه مع `Esc`. ولا يُغلق الحوار بالنقر خارجه إن لم تكتبها، حتى لا تذهب نقرة شاردة بما كُتب فيه.' },
      { value: 'none', label: 'أزراره وحدها', name: 'closedby="none"', desc: 'لا يغلقه `Esc` ولا النقر خارجه، بل أزراره وحدها، لحوار لا بد فيه من جواب.' }
    ] }
  ],
  extra: [
    { group: 'الأساس', first: true, name: 'ins-dialog', desc: 'على `<dialog>`، فيحبس التركيز ما دام مفتوحًا، ويغلقه `Esc` ويعيد التركيز إلى الزر الذي فتحه.' },
    { group: 'الأساس', name: 'data-ins-dialog="#id"', desc: 'على الزر الذي يفتح الحوار، وقيمتها معرّف الحوار.' },
    { group: 'الأساس', name: 'data-ins-dialog-close', desc: 'على زر داخل الحوار يغلقه، ويصلح له `data-ins-dismiss` أيضًا.' },
    { group: 'الأجزاء', name: 'ins-dialog-head', desc: 'رأس الحوار، وفيه العنوان وزر الإغلاق `.ins-close`.' },
    { group: 'الأجزاء', name: 'ins-dialog-title', desc: 'عنوان الحوار.' },
    { group: 'الأجزاء', name: 'ins-dialog-body', desc: 'متن الحوار، يتمرر وحده إذا طال.' },
    { group: 'الأجزاء', name: 'ins-dialog-x', level: 'advanced', desc: 'زر إغلاق أقدم من `.ins-close` وباقٍ لمن استعمله، وتضع فيه الأيقونة بنفسك.' },
    { group: 'الأجزاء', name: 'ins-dialog-body--spill', level: 'advanced', desc: 'كانت تلغي تمرير المتن لتخرج منه القائمة المفتوحة. ولم تعد تلزم، فالقوائم تطفو الآن فوق الحوار، وبقيت لمن استعملها.' }
  ],
  html: function (s) {
    var pad = s.form ? '    ' : '  ';
    var close = s.form ? '' : ' data-ins-dialog-close';
    var inner = [
      '<div class="ins-dialog-head">',
      '  <h3 class="ins-dialog-title">' + s.title + '</h3>',
      '  <button class="ins-close" data-ins-dialog-close aria-label="إغلاق"></button>',
      '</div>'
    ];
    if (!s.form) inner.push('<div class="ins-dialog-body">نص الحوار.</div>');
    else {
      inner.push('<div class="ins-dialog-body">',
        '  <div class="ins-field">',
        '    <label class="ins-label" for="dlg-name">الاسم</label>',
        '    <input class="ins-input" id="dlg-name" name="name" type="text">',
        '  </div>',
        '  <div class="ins-field">',
        '    <label class="ins-label" for="dlg-kind">النوع</label>',
        '    <select class="ins-select" id="dlg-kind" name="kind">',
        '      <option>عام</option>',
        '      <option>خاص</option>',
        '    </select>',
        '  </div>',
        '  <div class="ins-field">',
        '    <label class="ins-label" for="dlg-note">ملاحظة</label>',
        '    <textarea class="ins-textarea" id="dlg-note" name="note" rows="3"></textarea>',
        '  </div>',
        '  <label class="ins-check"><input type="checkbox" name="copy"><span class="ins-check-text">أرسل لي نسخة</span></label>',
        '</div>');
    }
    if (s.foot) {
      inner.push('<div class="ins-dialog-foot">',
        '  <button class="ins-btn ins-btn--bare"' + close + (s.form ? ' value="cancel"' : '') + '>إلغاء</button>',
        '  <button class="ins-btn ins-btn--primary"' + close + (s.form ? ' value="ok"' : '') + '>موافق</button>',
        '</div>');
    }
    var body = inner.map(function (l) { return l.split('\n').map(function (x) { return pad + x; }).join('\n'); }).join('\n');
    if (s.form) body = '  <form method="dialog">\n' + body + '\n  </form>';
    return '<button class="ins-btn ins-btn--primary" data-ins-dialog="#dlg-demo">فتح الحوار</button>\n\n' +
      '<dialog class="ins-dialog' + (s.size ? ' ins-dialog--' + s.size : '') + '" id="dlg-demo"' + (s.closedby ? ' closedby="' + s.closedby + '"' : '') + '>\n' + body + '\n</dialog>';
  },
  notes: function (s) {
    if (s.closedby === 'any') return ['اضغط الزر لترى الحوار بخياراتك، ثم انقر خارجه فيُغلق.'];
    if (s.closedby === 'none') return ['اضغط الزر لترى الحوار بخياراتك، فلا يغلقه `Esc` ولا النقر خارجه.'];
    return ['اضغط الزر لترى الحوار بخياراتك.'];
  }
});

SitePlay.define({
  id: 'confirm',
  title: 'التأكيد',
  controls: [
    { key: 'look', label: 'نمط الزر', type: 'seg', value: 'secondary', choices: [
      { value: 'secondary', label: 'ثانوي' },
      { value: 'ghost-danger', label: 'خطر' }
    ] },
    { key: 'text', label: 'السؤال', type: 'text', value: 'أرشفة الملف؟' },
    { key: 'ok', label: 'زر التأكيد', type: 'text', value: 'أرشفة' },
    { key: 'tone', label: 'تأكيد بلون الخطر', type: 'switch', name: 'data-ins-confirm-tone="danger"', desc: 'يجعل زر التأكيد أحمر وإن لم يكن الزر نفسه من أزرار الخطر.' }
  ],
  extra: [
    { group: 'الأساس', first: true, name: 'data-ins-confirm', desc: 'على زر أو رابط أو نموذج، وقيمتها السؤال، فلا يمضي الإجراء إلا إذا أكده المستخدم.' },
    { group: 'الأساس', name: 'data-ins-confirm-ok', desc: 'نص زر التأكيد، وهو «تأكيد» إن لم تكتبه.' }
  ],
  disable: function (s) { return s.look === 'ghost-danger' ? ['tone'] : []; },
  html: function (s) {
    var tone = s.tone && s.look !== 'ghost-danger' ? ' data-ins-confirm-tone="danger"' : '';
    return '<button class="ins-btn ins-btn--' + s.look + '" data-ins-confirm="' + s.text + '"' +
      (s.ok ? ' data-ins-confirm-ok="' + s.ok + '"' : '') + tone + '>' + (s.ok || 'متابعة') + '</button>';
  },
  notes: function () { return ['اضغط الزر لترى السؤال بخياراتك.']; }
});
