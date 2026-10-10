SitePlay.define({
  id: 'month',
  title: 'تقويم الشهر',
  owns: 'ins-month',
  wide: true,
  controls: [
    { key: 'tone', label: 'النبرة', group: 'الموعد', type: 'select', value: '', choices: [
      { value: '', label: 'الأساسي' },
      { value: 'ok', label: 'نجاح', name: 'data-tone="ok"', desc: 'موعد بلون النجاح.' },
      { value: 'warn', label: 'تحذير', name: 'data-tone="warn"', desc: 'موعد بلون التحذير.' },
      { value: 'bad', label: 'خطر', name: 'data-tone="bad"', desc: 'موعد بلون الخطر.' },
      { value: 'info', label: 'معلومة', name: 'data-tone="info"', desc: 'موعد بلون المعلومة.' }
    ] },
    { key: 'time', label: 'بوقته', group: 'الموعد', type: 'switch', value: true, name: 'data-time', desc: 'وقت الموعد، مثل `14:30`، يُكتب قبل عنوانه وتُرتب به مواعيد اليوم.' },
    { key: 'link', label: 'رابط', group: 'الموعد', type: 'switch', name: 'li > a', kind: 'element', desc: 'اجعل نص الموعد رابطًا، فيبقى في الشهر رابطًا يُنقر.' },
    { key: 'more', label: 'أكثر من ثلاثة في يوم', type: 'switch' },
    { key: 'en', label: 'بالإنجليزية', type: 'switch', name: 'lang="en"', desc: 'يأخذ الشهر لغته من أقرب `lang`، ومنها أسماء الأيام والشهر وأول أيام الأسبوع.' }
  ],
  extra: [
    { group: 'الأساس', first: true, name: 'data-ins-month', desc: 'على `.ins-month`: الشهر الذي يُعرض بصيغة `2026-09`، وبدونها الشهر الحالي.' },
    { group: 'الأساس', name: 'ins-month-events', desc: 'قائمة المواعيد داخل الشهر، منها يُرسم ثم تُخفى، ولكل موعد فيها عنصر `<li>`.' },
    { group: 'الموعد', name: 'data-date', desc: 'على كل `<li>`: يوم الموعد بصيغة `2026-09-08`.' },
    { group: 'الأحداث', name: 'ins:month', kind: 'event', desc: 'يُطلق إذا انتقلت إلى شهر آخر، وفي `detail` الشهر الجديد `month`.' },
    { group: 'الأحداث', name: 'ins:month-day', kind: 'event', desc: 'يُطلق إذا نُقر يوم في موضع خال من مواعيده، وفي `detail` تاريخه `date`.' }
  ],
  events: ['ins:month', 'ins:month-day'],
  html: function (s) {
    var t = s.en
      ? ['Delivery', 'Meeting', 'Review', 'Workshop', 'Call', 'Training', 'Visit', 'Deadline', 'Holiday']
      : ['تسليم', 'اجتماع', 'مراجعة', 'ورشة عمل', 'مكالمة', 'تدريب', 'زيارة', 'موعد نهائي', 'إجازة'];
    var list = [['03', '', 0], ['08', '10:00', 1], ['08', '14:30', 2], ['15', '', 3], ['21', '09:00', 4]];
    if (s.more) list.push(['21', '11:00', 5], ['21', '13:00', 6], ['21', '16:00', 7]);
    list.push(['26', '', 8]);
    var items = list.map(function (e, i) {
      var text = s.link && i === 0 ? '<a href="/events/1">' + t[e[2]] + '</a>' : t[e[2]];
      return '    <li data-date="2026-09-' + e[0] + '"' + (s.time && e[1] ? ' data-time="' + e[1] + '"' : '') +
        (s.tone ? ' data-tone="' + s.tone + '"' : '') + '>' + text + '</li>';
    }).join('\n');
    return '<div class="ins-month" data-ins-month="2026-09"' + (s.en ? ' lang="en" dir="ltr"' : '') + '>\n' +
      '  <ul class="ins-month-events">\n' + items + '\n  </ul>\n</div>';
  },
  notes: function (s) {
    var out = [];
    if (s.link) out.push('صار «تسليم» يوم 3 رابطًا يشبه غيره من المواعيد، فمرر عليه المؤشر يمتلئ بلونه.');
    if (s.more) out.push('لا يعرض اليوم أكثر من ثلاثة مواعيد، فإن زادت كتب عدد ما بقي، مثل «+2».');
    if (s.en) out.push('صار الأسبوع يبدأ بالأحد كما يبدؤه أهل هذه اللغة، ومعها `dir="ltr"` ليجري الشهر من اليسار.');
    out.push('انتقل إلى شهر آخر، أو انقر يومًا خاليًا، فيظهر هنا الحدث.');
    return out;
  }
});
