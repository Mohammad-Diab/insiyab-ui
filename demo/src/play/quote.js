SitePlay.define({
  id: 'quote',
  title: 'الاقتباس',
  owns: 'ins-quote',
  controls: [
    { key: 'source', label: 'المصدر', group: 'المصدر', type: 'seg', value: 'link', choices: [
      { value: '', label: 'بلا' },
      { value: 'text', label: 'نص', name: 'ins-quote-source', desc: 'على `<figcaption>`: مصدر الاقتباس بخط أصغر، تسبقه أيقونة مستند.' },
      { value: 'link', label: 'رابط', name: 'a', kind: 'element', desc: 'رابط في المصدر يأخذ لون الهوية، ليصل القارئ إلى النص الأصلي.' }
    ] },
    { key: 'text', label: 'النص', type: 'text', value: 'يُحفظ الملف كل خمس دقائق، ويمكنك استعادة أي نسخة سابقة منه.' }
  ],
  extra: [
    { group: 'الأساس', first: true, name: 'ins-quote', desc: 'على `<figure>` يضم `<blockquote>`: نص منقول من مكان آخر، على حافته شريط بلون الهوية.' }
  ],
  html: function (s) {
    var src = s.source === 'link' ? '<a href="/guide">دليل الاستخدام</a> · ص 12' : 'دليل الاستخدام · ص 12';
    return '<figure class="ins-quote">\n  <blockquote>' + s.text + '</blockquote>' +
      (s.source ? '\n  <figcaption class="ins-quote-source">' + src + '</figcaption>' : '') + '\n</figure>';
  }
});

SitePlay.define({
  id: 'ref',
  title: 'علامة المرجع',
  owns: 'ins-ref',
  controls: [
    { key: 'num', label: 'الرقم', type: 'text', value: '1' },
    { key: 'note', label: 'تضيء مع ملاحظتها', type: 'switch', name: 'data-ins-note', desc: 'تربط العلامة بملاحظة `.ins-note`، فإذا مررت المؤشر على إحداهما أضاءتا معًا.' }
  ],
  extra: [
    { group: 'الأساس', first: true, name: 'ins-ref', desc: 'على `<a>`: رابط صغير مرقم في متن النص، يحيل إلى مصدر أو ملاحظة. وهو أكبر من الرقم المرفوع العادي فيسهل لمسه.' }
  ],
  html: function (s) {
    return '<div class="ins-stack">\n  <p class="ins-m-0">نص يحيل إلى مرجعه<a class="ins-ref" href="#ref-1"' + (s.note ? ' data-ins-note="#ref-1"' : '') + '>' + s.num + '</a>.</p>\n' +
      '  <div class="ins-note ins-note--info" id="ref-1">المرجع الذي تحيل إليه العلامة.</div>\n</div>';
  },
  notes: function (s) {
    return s.note ? ['مرر المؤشر على العلامة أو على الملاحظة، فتضيئان معًا.'] : [];
  }
});

SitePlay.define({
  id: 'mark',
  title: 'التظليل',
  owns: 'ins-mark',
  controls: [
    { key: 'tone', label: 'النبرة', group: 'النبرة', type: 'select', value: '', choices: [
      { value: '', label: 'تحذير' },
      { value: 'ok', label: 'نجاح', name: 'ins-mark--ok', desc: 'لجزء صحيح أو مقبول.' },
      { value: 'bad', label: 'خطأ', name: 'ins-mark--bad', desc: 'لجزء خاطئ أو مرفوض.' },
      { value: 'info', label: 'معلومة', name: 'ins-mark--info', desc: 'لجزء يحتاج شرحًا.' },
      { value: 'brand', label: 'الهوية', name: 'ins-mark--brand', desc: 'بلون الهوية، لتظليل لا يحكم على النص.' }
    ] },
    { key: 'note', label: 'مع ملاحظة', group: 'الملاحظة', type: 'switch', name: 'data-ins-note', desc: 'تربط التظليل بملاحظة: `data-ins-note="#id"`. فإذا مررت المؤشر على أحدهما أضاءا معًا، وإذا نقرت التظليل ظهرت ملاحظته، ويقرأ قارئ الشاشة الملاحظة مع التظليل. وقد تشترك عدة تظليلات في ملاحظة واحدة.' }
  ],
  extra: [
    { group: 'الأساس', first: true, name: 'ins-mark', desc: 'على `<mark>`: يظلل جزءًا من النص بنبرة التحذير ما لم تختر غيرها، ويمتد على الأسطر كأنه قطعة واحدة.' },
    { group: 'الملاحظة', name: 'is-lit', kind: 'class', desc: 'تضعه المكتبة على التظليل وملاحظته ما داما مضاءين.' }
  ],
  html: function (s) {
    var tone = s.tone ? ' ins-mark--' + s.tone : '';
    var mark = '<mark class="ins-mark' + tone + '"' + (s.note ? ' data-ins-note="#mark-1"' : '') + '>جملة تحتاج مراجعة</mark>';
    var p = '<p class="ins-m-0">في هذه الفقرة ' + mark + '، وبعدها بقية النص.</p>';
    if (!s.note) return p;
    return '<div class="ins-stack">\n  ' + p + '\n  <div class="ins-note' + (s.tone ? ' ins-note--' + s.tone : '') + '" id="mark-1">سبب التظليل، يشرحه لمن يقرأ.</div>\n</div>';
  },
  notes: function (s) {
    return s.note ? ['مرر المؤشر على التظليل أو على الملاحظة، فيضيئان معًا.'] : [];
  }
});

SitePlay.define({
  id: 'note',
  title: 'الملاحظة',
  owns: 'ins-note',
  controls: [
    { key: 'tone', label: 'النبرة', type: 'seg', value: '', choices: [
      { value: '', label: 'تحذير' },
      { value: 'ok', label: 'نجاح', name: 'ins-note--ok', desc: 'ملاحظة على جزء صحيح.' },
      { value: 'bad', label: 'خطأ', name: 'ins-note--bad', desc: 'ملاحظة على جزء خاطئ.' },
      { value: 'info', label: 'معلومة', name: 'ins-note--info', desc: 'ملاحظة تشرح.' },
      { value: 'brand', label: 'الهوية', name: 'ins-note--brand', desc: 'ملاحظة بلون الهوية.' }
    ] },
    { key: 'text', label: 'النص', type: 'text', value: 'ملاحظة قصيرة على النص.' }
  ],
  extra: [
    { group: 'الأساس', first: true, name: 'ins-note', desc: 'تعليق على النص بنبرة التحذير ما لم تختر غيرها، يشير إليه التظليل أو علامة المرجع بمعرفها `id`.' }
  ],
  html: function (s) {
    return '<div class="ins-note' + (s.tone ? ' ins-note--' + s.tone : '') + '">' + s.text + '</div>';
  },
  notes: function () {
    return ['اجعل نبرة الملاحظة كنبرة التظليل الذي يشير إليها.'];
  }
});
