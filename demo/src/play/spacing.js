var STEPS = ['0', '1', '2', '3', '4', '5', '6', '8', '12'].map(function (n) { return { value: n, label: n + ' (' + n * 4 + ' بكسل)' }; });

SitePlay.define({
  id: 'margin',
  title: 'الهامش الخارجي',
  owns: 'ins-m',
  controls: [
    { key: 'side', label: 'الجهة', type: 'select', value: '', choices: [
      { value: '', label: 'الجهات الأربع', name: 'ins-m-4', desc: 'الجهات الأربع. والرقم في آخر الكلاس خطوة من السلم: 0 1 2 3 4 5 6 8 12.' },
      { value: 't', label: 'الأعلى', name: 'ins-mt-4', desc: 'الأعلى.' },
      { value: 'b', label: 'الأسفل', name: 'ins-mb-4', desc: 'الأسفل.' },
      { value: 's', label: 'البداية', name: 'ins-ms-4', desc: 'جهة البداية، وهي اليمين في العربية.' },
      { value: 'e', label: 'النهاية', name: 'ins-me-4', desc: 'جهة النهاية، وهي اليسار في العربية.' },
      { value: 'x', label: 'البداية والنهاية', name: 'ins-mx-4', desc: 'البداية والنهاية معًا.' },
      { value: 'y', label: 'الأعلى والأسفل', name: 'ins-my-4', desc: 'الأعلى والأسفل معًا.' }
    ] },
    { key: 'step', label: 'الخطوة', type: 'select', value: '4', choices: STEPS }
  ],
  extra: [
    { group: 'الهامش التلقائي', name: 'ins-m-auto', desc: 'يوسّط العنصر في حاوية مرنة (flex) أو شبكة.' },
    { group: 'الهامش التلقائي', name: 'ins-mx-auto', desc: 'يوسّط العنصر أفقيًا إذا كان له عرض.' },
    { group: 'الهامش التلقائي', name: 'ins-ms-auto', desc: 'يدفع العنصر، ومعه ما بعده، إلى نهاية `ins-row`.' },
    { group: 'الهامش التلقائي', name: 'ins-me-auto', desc: 'يدفع ما بعد العنصر إلى نهاية `ins-row`.' },
    { group: 'الهامش التلقائي', name: 'ins-mt-auto', desc: 'يدفع العنصر إلى أسفل `ins-stack`.' }
  ],
  html: function (s) {
    return '<div class="lay">\n  <div class="demo-box ins-m' + s.side + '-' + s.step + '">المحتوى</div>\n</div>';
  },
  notes: function () { return ['الإطار المنقط هو الحاوية، والفراغ بينه وبين المحتوى هو الهامش.']; }
});

SitePlay.define({
  id: 'padding',
  title: 'الفراغ الداخلي',
  owns: 'ins-p',
  controls: [
    { key: 'side', label: 'الجهة', type: 'select', value: '', choices: [
      { value: '', label: 'الجهات الأربع', name: 'ins-p-4', desc: 'الجهات الأربع. والرقم في آخر الكلاس خطوة من السلم: 0 1 2 3 4 5 6 8 12.' },
      { value: 't', label: 'الأعلى', name: 'ins-pt-4', desc: 'الأعلى.' },
      { value: 'b', label: 'الأسفل', name: 'ins-pb-4', desc: 'الأسفل.' },
      { value: 's', label: 'البداية', name: 'ins-ps-4', desc: 'جهة البداية، وهي اليمين في العربية.' },
      { value: 'e', label: 'النهاية', name: 'ins-pe-4', desc: 'جهة النهاية، وهي اليسار في العربية.' },
      { value: 'x', label: 'البداية والنهاية', name: 'ins-px-4', desc: 'البداية والنهاية معًا.' },
      { value: 'y', label: 'الأعلى والأسفل', name: 'ins-py-4', desc: 'الأعلى والأسفل معًا.' }
    ] },
    { key: 'step', label: 'الخطوة', type: 'select', value: '4', choices: STEPS }
  ],
  html: function (s) {
    return '<div class="lay ins-p' + s.side + '-' + s.step + '">\n  <div class="demo-box">المحتوى</div>\n</div>';
  },
  notes: function () { return ['الإطار المنقط هو العنصر، والفراغ بين حده والمحتوى هو الفراغ الداخلي.']; }
});

SitePlay.define({
  id: 'gap',
  title: 'الفراغ بين الأبناء',
  owns: 'ins-gap',
  controls: [
    { key: 'box', label: 'الحاوية', type: 'seg', value: 'row', choices: [
      { value: 'row', label: 'صف' },
      { value: 'stack', label: 'عمود' }
    ] },
    { key: 'step', label: 'الخطوة', type: 'select', value: '6', choices: STEPS }
  ],
  extra: [
    { group: 'الأساس', first: true, name: 'ins-gap-4', desc: 'يغير الفراغ بين أبناء `ins-row` و`ins-stack` والشبكات. والرقم خطوة من السلم: 0 1 2 3 4 5 6 8 12.' }
  ],
  html: function (s) {
    return '<div class="ins-' + s.box + ' ins-gap-' + s.step + '">\n' +
      '  <div class="demo-box">1</div>\n  <div class="demo-box">2</div>\n  <div class="demo-box">3</div>\n</div>';
  }
});

SitePlay.define({
  id: 'stack',
  title: 'الرص العمودي',
  owns: 'ins-stack',
  controls: [
    { key: 'start', label: 'لا يمد أبناءه', type: 'switch', name: 'ins-items-start', desc: 'يبقي كل ابن على عرضه، فلا يمتد زر وحيد في العمود على عرضه كله.' }
  ],
  extra: [
    { group: 'الأساس', first: true, name: 'ins-stack', desc: 'يرص أبناءه عموديًا بينهم 16 بكسل، ويمدهم على عرضه، ويلغي هوامشهم الخارجية.' },
    { group: 'الأساس', name: 'ins-mt-auto', desc: 'على أحد الأبناء، فيدفعه إلى أسفل العمود إذا كان للعمود ارتفاع.' }
  ],
  html: function (s) {
    return '<div class="ins-stack' + (s.start ? ' ins-items-start' : '') + '">\n' +
      '  <div class="demo-box">الأول</div>\n  <div class="demo-box">الثاني</div>\n  <div class="demo-box">عنصر أعرض من أخويه</div>\n</div>';
  }
});

SitePlay.define({
  id: 'row',
  title: 'الرص الأفقي',
  owns: 'ins-row',
  wide: true,
  controls: [
    { key: 'push', label: 'آخرها في النهاية', type: 'switch', name: 'ins-ms-auto', desc: 'على أحد الأبناء، فيدفعه ومن بعده إلى نهاية الصف.' }
  ],
  extra: [
    { group: 'الأساس', first: true, name: 'ins-row', desc: 'يرص أبناءه أفقيًا بينهم 12 بكسل، ويوسّطهم عموديًا، وينقلهم إلى سطر جديد إذا ضاق المكان.' }
  ],
  html: function (s) {
    return '<div class="ins-row">\n  <div class="demo-box">الأول</div>\n  <div class="demo-box">الثاني</div>\n' +
      '  <div class="demo-box' + (s.push ? ' ins-ms-auto' : '') + '">الثالث</div>\n</div>';
  }
});

SitePlay.define({
  id: 'toolbar',
  title: 'شريط الأدوات',
  owns: 'ins-toolbar',
  wide: true,
  controls: [
    { key: 'grow', label: 'حقل يملأ ما بقي', type: 'switch', value: true, name: 'ins-toolbar-grow', desc: 'على العنصر الذي يملأ ما بقي من عرض الشريط.' },
    { key: 'end', label: 'أزرار في النهاية', type: 'switch', value: true, name: 'ins-toolbar-end', desc: 'يدفع ما فيه إلى نهاية الشريط.' }
  ],
  extra: [
    { group: 'الأساس', first: true, name: 'ins-toolbar', desc: 'صف يعلو جدولًا أو قائمة، تأتي حقوله بارتفاع أزراره وبعرض ما فيها لا بعرض السطر.' }
  ],
  html: function (s) {
    var btn = '<button class="ins-btn ins-btn--primary">إضافة</button>';
    return '<div class="ins-toolbar">\n' +
      '  <input class="ins-input' + (s.grow ? ' ins-toolbar-grow' : '') + '" placeholder="بحث" aria-label="بحث">\n' +
      '  <select class="ins-select" aria-label="الحالة"><option>الكل</option><option>مفتوح</option><option>مغلق</option></select>\n' +
      (s.end ? '  <div class="ins-toolbar-end">\n    ' + btn + '\n  </div>\n' : '  ' + btn + '\n') +
      '</div>';
  }
});

SitePlay.define({
  id: 'field-row',
  title: 'صف الحقول',
  owns: 'ins-field-row',
  wide: true,
  controls: [
    { key: 'grow', label: 'الحقول تملأ الصف', type: 'switch', value: true, name: 'ins-grow', desc: 'على الحقل الذي يملأ ما بقي من عرض الصف.' },
    { key: 'btn', label: 'زر في الصف', type: 'switch', name: 'button.ins-btn', kind: 'element', desc: 'زر في الصف يأخذ ارتفاع الحقول ويقف على خط أسفلها. فلا تضعه في صف فيه حقل له تلميح.' }
  ],
  extra: [
    { group: 'الأساس', first: true, name: 'ins-field-row', desc: 'يضع الحقول جنبًا إلى جنب، فإذا ضاق المكان جعلها بعضها فوق بعض.' }
  ],
  html: function (s) {
    var field = function (id, label) {
      return '  <div class="ins-field' + (s.grow ? ' ins-grow' : '') + '">\n    <label class="ins-label" for="' + id + '">' + label + '</label>\n' +
        '    <input class="ins-input" id="' + id + '">\n  </div>\n';
    };
    return '<div class="ins-field-row">\n' + field('fr-first', 'الاسم') + field('fr-last', 'العائلة') +
      (s.btn ? '  <button class="ins-btn ins-btn--primary">حفظ</button>\n' : '') + '</div>';
  }
});

SitePlay.define({
  id: 'container',
  title: 'العمود في وسط الصفحة',
  owns: 'ins-container',
  controls: [],
  extra: [
    { group: 'الأساس', first: true, name: 'ins-container', desc: 'عمود يتوسط الصفحة عرضه 75rem، ويتسع فراغه الجانبي مع الشاشة من 16 إلى 32 بكسل.' },
    { group: 'العرض', name: 'ins-container--narrow', desc: 'عمود بعرض 48rem، للنماذج والمقالات.' },
    { group: 'العرض', name: 'ins-container--wide', desc: 'عمود بعرض 90rem.' }
  ],
  html: function () { return ''; }
});
