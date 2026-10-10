SitePlay.define({
  id: 'glass',
  title: 'الزجاج',
  owns: 'ins-glass',
  controls: [
    { key: 'kind', label: 'السطح', type: 'seg', value: '', choices: [
      { value: '', label: 'زجاج' },
      { value: 'inner', label: 'مسطح', name: 'ins-glass-inner', desc: 'مظهر مسطح لسطح يوضع داخل سطح آخر: لون خفيف وحد رفيع، بلا ضبابية.' }
    ] },
    { key: 'nest', label: 'زجاج داخل زجاج', type: 'switch', level: 'advanced' }
  ],
  extra: [
    { group: 'الأساس', first: true, name: 'ins-glass', desc: 'السطح الأساسي بطبقاته الأربع، لا فراغ داخليًا فيه ولا سلوك. وإذا وضعته داخل سطح آخر تخلى وحده عن حده وظله ولمعته وضبابيته.' }
  ],
  disable: function (s) { return s.kind ? ['nest'] : []; },
  html: function (s) {
    if (s.kind) return '<div class="ins-glass-inner ins-p-5">سطح مسطح</div>';
    if (!s.nest) return '<div class="ins-glass ins-p-5">سطح من زجاج</div>';
    return '<div class="ins-glass ins-p-5">\n  <p>السطح الخارجي</p>\n  <div class="ins-glass ins-p-4">السطح الداخلي</div>\n</div>';
  },
  notes: function (s) {
    return s.nest && !s.kind ? ['السطحان يحملان الكلاس نفسه، والداخلي منهما صار مسطحًا وحده.'] : [];
  }
});

SitePlay.define({
  id: 'card',
  title: 'البطاقة',
  owns: 'ins-card',
  controls: [
    { key: 'link', label: 'رابط بهيئة بطاقة', type: 'switch', name: 'a.ins-card', kind: 'element', desc: 'ضع الكلاس على `<a>` أو `<button>`، فترتفع البطاقة تحت المؤشر دون `ins-hoverable`.' },
    { key: 'hover', label: 'ترتفع تحت المؤشر', type: 'switch', name: 'ins-hoverable', desc: 'ترفع البطاقة قليلًا إذا مر عليها المؤشر، لبطاقة تُنقر.' }
  ],
  extra: [
    { group: 'الأساس', first: true, name: 'ins-card', desc: 'بطاقة من زجاج بطبقاته الأربع، باسم يقول ما هي.' }
  ],
  disable: function (s) { return s.link ? ['hover'] : []; },
  html: function (s) {
    if (s.link) return '<a class="ins-card ins-p-5" href="/item">بطاقة تُنقر</a>';
    return '<div class="ins-card ins-p-5' + (s.hover ? ' ins-hoverable' : '') + '">بطاقة</div>';
  },
  notes: function (s) {
    return s.hover || s.link ? ['مرّر المؤشر على البطاقة.'] : [];
  }
});

SitePlay.define({
  id: 'panel',
  title: 'اللوح',
  owns: 'ins-panel',
  controls: [
    { key: 'head', label: 'رأس', type: 'switch', value: true, name: 'ins-panel-head', desc: 'رأس اللوح، وفيه عنوانه `ins-panel-title`.' },
    { key: 'ico', label: 'أيقونة في الرأس', type: 'switch', value: true, name: 'ins-panel-ico', desc: 'أيقونة القسم على رقعة ملونة قبل العنوان.' },
    { key: 'actions', label: 'إجراءات في الرأس', type: 'switch', value: true, name: 'ins-panel-actions', desc: 'تدفع ما فيها من أزرار إلى آخر الرأس.' },
    { key: 'foot', label: 'تذييل', type: 'switch', value: true, name: 'ins-panel-foot', desc: 'سطر في أسفل اللوح لأزراره.' },
    { key: 'alert', label: 'يحتاج انتباهًا', type: 'switch', name: 'ins-panel--alert', desc: 'يلون رأس اللوح وحده بلون الخطر، لا اللوح كله.' },
    { key: 'flush', label: 'جسم بلا فراغ داخلي', type: 'switch', name: 'ins-panel-body--flush', desc: 'لجدول أو قائمة يمتدان إلى حواف اللوح.' },
    { key: 'note', label: 'سطر شرح', type: 'switch', name: 'ins-panel-note', desc: 'سطر شرح قصير تحت الرأس، أو في أعلى جسم بلا فراغ داخلي.' }
  ],
  extra: [
    { group: 'الأساس', first: true, name: 'ins-panel', desc: 'حاوية من زجاج بثلاثة أجزاء: رأس ثم جسم ثم تذييل. ولا يرتفع اللوح تحت المؤشر، وإنما تضيء حافته.' },
    { group: 'الأساس', name: 'ins-panel-body', desc: 'جسم اللوح.' },
    { group: 'الأساس', name: 'ins-panel-title', desc: 'عنوان اللوح في رأسه.' },
    { group: 'ما يفتح من اللوح', name: 'ins-panel--open', level: 'advanced', desc: 'يترك ما يفتح داخل اللوح يخرج من حدوده ويعلو اللوح الذي يليه. يفعل ذلك وحده ما دام فيه شيء مفتوح، فلا تحتاجه إلا لمتصفح لا يعرف `:has()`.' }
  ],
  disable: function (s) { return s.head ? [] : ['ico', 'actions', 'alert']; },
  html: function (s) {
    var out = ['<div class="ins-panel' + (s.alert && s.head ? ' ins-panel--alert' : '') + '">'];
    if (s.head) {
      out.push('  <div class="ins-panel-head">');
      if (s.ico) out.push('    <span class="ins-panel-ico"><svg class="ico"><use href="#i-users"/></svg></span>');
      out.push('    <h3 class="ins-panel-title">العنوان</h3>');
      if (s.actions) out.push('    <div class="ins-panel-actions"><button class="ins-btn ins-btn--bare ins-btn--sm">تعديل</button></div>');
      out.push('  </div>');
    }
    if (s.flush) {
      out.push('  <div class="ins-panel-body ins-panel-body--flush">');
      if (s.note) out.push('    <p class="ins-panel-note">سطر شرح.</p>');
      out.push('    <ul class="ins-list">', '      <li class="ins-list-item">البند الأول</li>', '      <li class="ins-list-item">البند الثاني</li>', '    </ul>', '  </div>');
    } else {
      if (s.note) out.push('  <p class="ins-panel-note">سطر شرح.</p>');
      out.push('  <div class="ins-panel-body">المحتوى.</div>');
    }
    if (s.foot) out.push('  <div class="ins-panel-foot">', '    <button class="ins-btn ins-btn--primary ins-btn--sm">حفظ</button>', '  </div>');
    return out.concat('</div>').join('\n');
  }
});
