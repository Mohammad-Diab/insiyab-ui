SitePlay.define({
  id: 'timeline',
  title: 'الخط الزمني',
  owns: 'ins-timeline',
  wide: true,
  controls: [
    { key: 'layout', label: 'التخطيط', type: 'seg', value: '', choices: [
      { value: '', label: 'عادي' },
      { value: 'compact', label: 'مضغوط', name: 'ins-timeline--compact', desc: 'نقاط أصغر ومسافات أقل، لعمود ضيق.' },
      { value: 'split', label: 'على جانبين', name: 'ins-timeline--split', desc: 'يوزع الأحداث على جانبي خط في الوسط إذا بلغ عرض القائمة `40rem`، ويجمعها في جانب واحد إذا ضاقت.' }
    ] },
    { key: 'tone', label: 'نبرة ما مضى', group: 'الحدث', type: 'select', value: 'ok', choices: [
      { value: '', label: 'الأساسي' },
      { value: 'ok', label: 'نجاح', name: 'ins-timeline-item--ok', desc: 'حدث بلون النجاح.' },
      { value: 'bad', label: 'خطر', name: 'ins-timeline-item--bad', desc: 'حدث بلون الخطر.' },
      { value: 'warn', label: 'تحذير', name: 'ins-timeline-item--warn', desc: 'حدث بلون التحذير.' },
      { value: 'info', label: 'معلومة', name: 'ins-timeline-item--info', desc: 'حدث بلون المعلومة.' }
    ] },
    { key: 'time', label: 'الوقت', type: 'seg', value: 'fixed', choices: [
      { value: '', label: 'بلا' },
      { value: 'fixed', label: 'مكتوب' },
      { value: 'ago', label: 'نسبي', name: 'data-ins-time', desc: 'على `<time datetime="…">` أينما كان في الصفحة: يكتب الوقت كما يقوله الناس، «قبل 5 دقائق» و«أمس»، ويجدده كل دقيقة. فإذا مضى أسبوع كتب التاريخ.' },
      { value: 'date', label: 'تاريخ', name: 'data-ins-time="date"', desc: 'يكتب التاريخ دائمًا بدل الوقت النسبي.' }
    ] },
    { key: 'current', label: 'حدث جار', group: 'الحدث', type: 'switch', value: true, name: 'is-current', kind: 'class', desc: 'الحدث الجاري الآن، حول نقطته هالة بلونها.' },
    { key: 'pending', label: 'حدث لم يأت', group: 'الحدث', type: 'switch', value: true, name: 'is-pending', kind: 'class', desc: 'حدث لم يأت بعد، نقطته فارغة والخط إليه متقطع.' },
    { key: 'icons', label: 'بالأيقونات', group: 'الحدث', type: 'switch', name: 'ins-timeline-ico', desc: 'أول عنصر في الحدث: رقاقة فيها أيقونة تحل مكان النقطة، بلون نبرته.' },
    { key: 'body', label: 'بنص', group: 'الحدث', type: 'switch', value: true, name: 'ins-timeline-body', desc: 'نص الحدث تحت عنوانه، ولك أن تضع مكانه لوحًا `.ins-panel` أو بطاقة `.ins-card`.' },
    { key: 'day', label: 'عنوان اليوم', type: 'switch', value: true, name: 'ins-timeline-day', desc: 'عنصر في القائمة يفصل بين الأيام بعنوان.' }
  ],
  extra: [
    { group: 'الأساس', first: true, name: 'ins-timeline', desc: 'على `<ol>`، لكل حدث فيها نقطة على الخط.' },
    { group: 'الحدث', name: 'ins-timeline-item', desc: 'الحدث، عنصر `<li>` في القائمة.' },
    { group: 'الحدث', name: 'ins-timeline-title', desc: 'عنوان الحدث.' },
    { group: 'الحدث', name: 'ins-timeline-time', desc: 'وقت الحدث، على `<time>`، يُكتب بجانب عنوانه.' }
  ],
  html: function (s) {
    var now = Math.floor(Date.now() / 60000) * 60000;
    var list = [
      { title: 'أُنشئ الملف', ico: '<use href="#i-file"/>', at: '2026-09-24T08:02', said: '8:02 ص', ago: 26 * 60, done: true },
      { title: 'أُرسل للمراجعة', ico: '<use href="#i-upload"/>', at: '2026-09-24T08:05', said: '8:05 ص', ago: 5, done: true, body: 'نص قصير عن الحدث.' },
      { title: 'قيد المراجعة', ico: '<use href="#i-clock"/>', state: s.current ? 'is-current' : '' },
      { title: 'الاعتماد', ico: '<use href="#i-check"/>', state: s.pending ? 'is-pending' : '' }
    ];
    var items = list.map(function (e) {
      var cls = 'ins-timeline-item' + (e.done && s.tone ? ' ins-timeline-item--' + s.tone : '') + (e.state ? ' ' + e.state : '');
      var out = '  <li class="' + cls + '">\n';
      if (s.icons) out += '    <span class="ins-timeline-ico"><svg class="ico" aria-hidden="true">' + e.ico + '</svg></span>\n';
      out += '    <span class="ins-timeline-title">' + e.title + '</span>\n';
      if (e.at && s.time === 'fixed') out += '    <time class="ins-timeline-time" datetime="' + e.at + '">' + e.said + '</time>\n';
      if (e.at && s.time === 'ago') out += '    <time class="ins-timeline-time" datetime="' + new Date(now - e.ago * 60000).toISOString().replace(/:00\.000Z$/, 'Z') + '" data-ins-time></time>\n';
      if (e.at && s.time === 'date') out += '    <time class="ins-timeline-time" datetime="' + e.at + '" data-ins-time="date"></time>\n';
      if (e.body && s.body) out += '    <p class="ins-timeline-body">' + e.body + '</p>\n';
      return out + '  </li>';
    }).join('\n');
    return '<ol class="ins-timeline' + (s.layout ? ' ins-timeline--' + s.layout : '') + '">\n' +
      (s.day ? '  <li class="ins-timeline-day">24 سبتمبر</li>\n' : '') + items + '\n</ol>';
  },
  notes: function (s) {
    var out = [];
    if (s.time === 'ago' || s.time === 'date') out.push('يكتب السكربت النص في `<time>`، ويحمل `title` التاريخ والوقت كاملين.');
    if (s.layout === 'split') out.push('تتوزع الأحداث على جانبين إذا بلغ عرض القائمة `40rem`، والمعاينة هنا أضيق من ذلك.');
    return out;
  }
});
