SitePlay.define({
  id: 'chart',
  title: 'الرسم البياني',
  owns: 'ins-chart',
  wide: true,
  controls: [
    { key: 'type', label: 'النوع', type: 'select', value: 'bar', choices: [
      { value: 'bar', label: 'أعمدة', name: 'data-ins-chart="bar"', desc: 'أعمدة لكل فئة، تتجاور فيها السلاسل. وهو النوع إذا غابت القيمة.' },
      { value: 'hbar', label: 'أعمدة أفقية', name: 'data-ins-chart="hbar"', desc: 'يأتي الاسم في أول السطر وعموده بعده، فاختره إذا طالت الأسماء.' },
      { value: 'histogram', label: 'توزيع', name: 'data-ins-chart="histogram"', desc: 'أعمدة متلاصقة تبين كيف تتوزع القيم على الفئات، وإذا تعددت السلاسل تراكبت شفافة لتقارن بينها.' },
      { value: 'line', label: 'خط', name: 'data-ins-chart="line"', desc: 'لما يتغير مع الزمن.' },
      { value: 'area', label: 'مساحة', name: 'data-ins-chart="area"', desc: 'خط يملأ ما تحته.' },
      { value: 'donut', label: 'حلقة', name: 'data-ins-chart="donut"', desc: 'لأجزاء من كل، يرسم السلسلة الأولى وحدها، وتحته مفتاحه (legend) بقيمه، وفي وسطه المجموع.' },
      { value: 'spark', label: 'خط مصغر', name: 'data-ins-chart="spark"', desc: 'خط بلا محاور في حجم كلمة (sparkline)، تضعه في بطاقة إحصائية مثلًا.' }
    ] },
    { key: 'source', label: 'البيانات', type: 'seg', value: 'table', choices: [
      { value: 'table', label: 'جدول' },
      { value: 'attr', label: 'سمة', name: 'data-ins-values', desc: 'سلسلة واحدة بلا جدول، أرقامها مفصولة بفواصل: `3,5,4`.' }
    ] },
    { key: 'series', label: 'السلاسل', type: 'seg', value: '2', choices: [
      { value: '1', label: '1' },
      { value: '2', label: '2' },
      { value: '3', label: '3' }
    ] },
    { key: 'stack', label: 'مكدسة', group: 'النوع', type: 'switch', name: 'data-ins-stack', desc: 'مع `bar` أو `hbar`: تجمع السلاسل بعضها فوق بعض في عمود واحد.' },
    { key: 'center', label: 'تحت المجموع', group: 'الحلقة', type: 'text', value: '', name: 'data-ins-center-label', desc: 'نص صغير يُكتب تحت المجموع في وسط الحلقة.' },
    { key: 'caption', label: 'بعنوان', type: 'switch', name: 'figcaption', kind: 'element', desc: 'عنوان قصير يُكتب تحت الرسم.' },
    { key: 'ltr', label: 'من اليسار', type: 'switch', name: 'dir="ltr"', desc: 'على الشكل: تجري الفئات من اليسار وإن كانت الصفحة عربية.' }
  ],
  extra: [
    { group: 'الأساس', first: true, name: 'ins-chart', desc: 'على `<figure>` فيه جدول، عموده الأول للفئات وصف `<thead>` للسلاسل، ومعه `data-ins-chart` لنوع الرسم.' },
    { group: 'الأساس', name: 'table', kind: 'element', desc: 'يبقى الجدول في الصفحة مخفيًا، فيقرأ منه قارئ الشاشة الأرقام، أما الرسم فمخفي عنه.' },
    { group: 'البيانات', name: 'data-ins-labels', desc: 'مع `data-ins-values`: أسماء الفئات مفصولة بفواصل.' },
    { group: 'البيانات', name: 'data-ins-name', desc: 'مع `data-ins-values`: اسم السلسلة، ويظهر مع القيمة تحت المؤشر.' },
    { group: 'البيانات', name: 'data-value', desc: 'على خلية في الجدول: القيمة التي تُرسم، إذا كُتب في الخلية نص غير الرقم.' },
    { group: 'الحلقة', name: 'data-ins-center', desc: 'نص يُكتب في وسط الحلقة مكان المجموع.' }
  ],
  disable: function (s) {
    var off = [], spark = s.type === 'spark', single = spark || s.source === 'attr' || s.type === 'donut';
    if (spark) off.push('source', 'caption');
    if (single) off.push('series');
    if ((s.type !== 'bar' && s.type !== 'hbar') || single || s.series === '1') off.push('stack');
    if (s.type !== 'donut') off.push('center');
    return off;
  },
  html: function (s) {
    var hist = s.type === 'histogram', spark = s.type === 'spark', attr = spark || s.source === 'attr';
    var labels = hist ? ['0–2', '2–4', '4–6', '6–8', '8–10'] : ['يناير', 'فبراير', 'مارس', 'أبريل', 'مايو'];
    var names = hist ? ['المجموعة الأولى', 'المجموعة الثانية', 'المجموعة الثالثة'] : ['هذا العام', 'العام الماضي', 'قبل عامين'];
    var data = hist ? [[3, 8, 19, 27, 12], [2, 6, 15, 31, 16], [1, 4, 10, 22, 20]] : [[120, 150, 98, 174, 160], [90, 130, 110, 140, 150], [60, 80, 95, 100, 120]];
    var n = attr || s.type === 'donut' ? 1 : +s.series;
    var stack = s.stack && n > 1 && (s.type === 'bar' || s.type === 'hbar');
    var at = ' data-ins-chart="' + s.type + '"' + (stack ? ' data-ins-stack' : '') +
      (s.type === 'donut' && s.center ? ' data-ins-center-label="' + s.center + '"' : '') + (s.ltr ? ' dir="ltr"' : '');
    var cap = s.caption ? '  <figcaption>' + (hist ? 'توزيع القيم' : 'الأشهر الخمسة الأولى') + '</figcaption>\n' : '';
    if (attr) {
      var vals = spark ? [12, 18, 14, 22, 19, 27, 31] : data[0];
      var more = spark ? '' : ' data-ins-labels="' + labels.join(',') + '" data-ins-name="' + names[0] + '"';
      if (!spark) return '<figure class="ins-chart"' + at + ' data-ins-values="' + vals.join(',') + '"' + more + '>' + (cap ? '\n' + cap : '') + '</figure>';
      return '<div class="ins-stat ins-glass">\n  <span class="ins-stat-body">\n    <span class="ins-stat-label">الزيارات</span>\n' +
        '    <span class="ins-stat-value">31</span>\n  </span>\n  <span class="ins-chart"' + at + ' data-ins-values="' + vals.join(',') + '"></span>\n</div>';
    }
    var head = '      <tr><th></th>' + names.slice(0, n).map(function (x) { return '<th>' + x + '</th>'; }).join('') + '</tr>\n';
    var rows = labels.map(function (l, i) {
      return '      <tr><th>' + l + '</th>' + data.slice(0, n).map(function (d) { return '<td>' + d[i] + '</td>'; }).join('') + '</tr>';
    }).join('\n');
    return '<figure class="ins-chart"' + at + '>\n' +
      '  <table>\n    <thead>\n' + head + '    </thead>\n    <tbody>\n' + rows + '\n    </tbody>\n  </table>\n' + cap + '</figure>';
  },
  notes: function (s) {
    var out = [];
    if (s.type === 'spark') out.push('الخط المصغر سلسلة واحدة في `data-ins-values`، فلا جدول له، ومكانه بطاقة إحصائية `.ins-stat` كهذه.');
    else if (s.source === 'attr') out.push('بلا جدول يكتب الرسم أرقامه في `aria-label` على الشكل، ليقرأها قارئ الشاشة.');
    if (s.type === 'donut') out.push('ترسم الحلقة السلسلة الأولى وحدها.');
    if (s.type !== 'spark') out.push('مرر المؤشر على الرسم فتظهر القيمة التي تحته.');
    return out;
  }
});
