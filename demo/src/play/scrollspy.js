SitePlay.define({
  id: 'toc',
  title: 'قائمة المحتويات',
  owns: 'ins-toc',
  wide: true,
  controls: [
    { key: 'title', label: 'عنوان القائمة', type: 'switch', value: true, name: 'ins-toc-title', desc: 'عنوان صغير في أول القائمة.' },
    { key: 'sub', label: 'رابط إلى عنوان أعمق', type: 'switch', value: true, name: 'ins-toc-sub', desc: 'على رابط إلى عنوان أعمق، فيتأخر خطوة عن غيره.' },
    { key: 'offset', label: 'خط القسم الحالي', type: 'seg', value: '', choices: [
      { value: '', label: 'ثلث المنطقة' },
      { value: '40', label: '40 بكسلًا', name: 'data-ins-scrollspy-offset', desc: 'على القائمة: بُعد الخط الذي يُعرف به القسم الحالي عن أعلى منطقة التمرير، بالبكسل، بدل ثلثها. لصفحة شريطها العلوي الثابت طويل.' }
    ] }
  ],
  extra: [
    { group: 'الأساس', first: true, name: 'data-ins-scrollspy', desc: 'على أي قائمة تنقل تشير روابطها إلى أقسام في الصفحة، فيتميز رابط القسم الذي يُقرأ.' },
    { group: 'الأساس', name: 'ins-toc', desc: 'قائمة المحتويات الجاهزة من ملف أنماط الإضافة: بطاقة زجاجية تثبت تحت الشريط العلوي، وتضبط مسافتها من الأعلى بـ `--ins-toc-top`.' },
    { group: 'الأساس', name: 'is-active', kind: 'class', desc: 'يضعها السكربت على الرابط الحالي، ومعها `aria-current="location"`.' },
    { group: 'الأساس', name: 'ins:scrollspy', kind: 'event', desc: 'يُطلق كلما تغير الرابط الحالي، وفي `detail` القائمة `nav` والرابط `link` وقسمه `section`.' }
  ],
  events: ['ins:scrollspy'],
  html: function (s) {
    return '<div class="spy-demo">\n' +
      '  <div class="spy-demo-box">\n' +
      '    <section id="spy-1"><h3>القسم الأول</h3></section>\n' +
      '    <section id="spy-2"><h3>القسم الثاني</h3></section>\n' +
      '    <section id="spy-3"><h3>قسم فرعي</h3></section>\n' +
      '    <section id="spy-4"><h3>القسم الثالث</h3></section>\n' +
      '  </div>\n' +
      '  <nav class="ins-toc" data-ins-scrollspy' + (s.offset ? ' data-ins-scrollspy-offset="' + s.offset + '"' : '') + ' aria-label="المحتويات">\n' +
      (s.title ? '    <div class="ins-toc-title">المحتويات</div>\n' : '') +
      '    <a href="#spy-1">القسم الأول</a>\n' +
      '    <a href="#spy-2">القسم الثاني</a>\n' +
      '    <a' + (s.sub ? ' class="ins-toc-sub"' : '') + ' href="#spy-3">قسم فرعي</a>\n' +
      '    <a href="#spy-4">القسم الثالث</a>\n' +
      '  </nav>\n' +
      '</div>';
  },
  stage: function (view) {
    view.querySelector('.spy-demo-box').scrollTop = 130;
  },
  notes: function () {
    return ['الصندوق ممرر قليلًا، فغيّر الخط تنتقل العلامة.'];
  }
});
