SitePlay.define({
  id: 'month',
  title: 'تقويم الشهر',
  owns: 'ins-month',
  wide: true,
  controls: [
    { key: 'view', label: 'العرض', type: 'seg', value: '', choices: [
      { value: '', label: 'الشهر' },
      { value: 'week', label: 'الأسبوع', name: 'data-ins-view="week"', desc: 'أسبوع بالساعات، أيامه أعمدة تبدأ من أول أيام الأسبوع في لغة الصفحة، ويقع كل موعد في ساعته بطول مدته. ويُعرض الأسبوع الذي فيه `data-ins-date`.' },
      { value: 'day', label: 'اليوم', name: 'data-ins-view="day"', desc: 'يوم واحد بالساعات، هو `data-ins-date`.' }
    ] },
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
    { key: 'allhours', label: 'اليوم كله', group: 'الأسبوع واليوم', type: 'switch', name: 'data-ins-work-hours="off"', desc: 'يعرض الأسبوع واليوم ساعات الدوام وحدها، وهذا هو الأصل. والموعد الذي يقع كله خارجها لا يختفي، بل يُعد في شريط فوق الساعات أو تحتها، مثل «+2»، فإن نقرته ظهر اليوم كله عند أول تلك المواعيد. والموعد الذي يتجاوز طرف الدوام يُقص عنده بزاوية مستقيمة. واكتب `data-ins-work-hours="off"` ليبدأ باليوم كله في صندوق بارتفاع الدوام، تمر بساعاته بالتمرير ويبدأ عند أول الدوام.' },
    { key: 'hours', label: 'دوام من 8 إلى 17', group: 'الأسبوع واليوم', type: 'switch', name: 'data-ins-day-hours="8-17"', desc: 'ساعات الدوام بصيغة `6-18`، وهي كذلك إن لم تذكرها. وتكون الساعة بين 0 و24، والنهاية بعد البداية.' },
    { key: 'nohours', label: 'بلا زر الساعات', group: 'الأسبوع واليوم', type: 'switch', name: 'data-ins-hours-switch="off"', desc: 'يخفي زر «ساعات الدوام» من الرأس، ويبقى ما في `data-ins-work-hours`.' },
    { key: 'en', label: 'بالإنجليزية', type: 'switch', name: 'lang="en"', desc: 'يأخذ الشهر لغته من أقرب `lang`، ومنها أسماء الأيام والشهر وأول أيام الأسبوع.' }
  ],
  extra: [
    { group: 'الأساس', first: true, name: 'ins-month', desc: 'الصندوق الذي يحمل التقويم، وفيه قائمة المواعيد.' },
    { group: 'الأساس', name: 'data-ins-month', desc: 'على `.ins-month`: الشهر الذي يُعرض بصيغة `2026-09`، وبدونها الشهر الحالي.' },
    { group: 'الأساس', name: 'ins-month-events', desc: 'قائمة المواعيد داخل الشهر، منها يُرسم ثم تُخفى، ولكل موعد فيها عنصر `<li>`.' },
    { group: 'الموعد', name: 'data-date', desc: 'على كل `<li>`: يوم الموعد بصيغة `2026-09-08`.' },
    { group: 'الموعد', name: 'data-duration', desc: 'على كل `<li>`: مدة الموعد بالدقائق، ومنها طوله في الأسبوع واليوم، وهي ساعة إن لم تذكرها.' },
    { group: 'الأسبوع واليوم', name: 'data-ins-date', desc: 'على `.ins-month`: اليوم الذي يعرضه اليوم، والأسبوع الذي فيه يعرضه الأسبوع، بصيغة `2026-09-21`. وهو اليوم الحالي إن وقع في `data-ins-month`، وإلا فأول يوم في الشهر.' },
    { group: 'الأسبوع واليوم', name: 'data-ins-view-switch="off"', desc: 'على `.ins-month`: يخفي مبدّل الشهر والأسبوع واليوم من الرأس، ويبقى العرض الذي في `data-ins-view`.' },
    { group: 'الأحداث', name: 'ins:month', kind: 'event', desc: 'يُطلق إذا انتقلت إلى شهر آخر، وفي `detail` الشهر الجديد `month`.' },
    { group: 'الأحداث', name: 'ins:month-date', kind: 'event', desc: 'يُطلق إذا انتقلت إلى أسبوع آخر أو يوم آخر، وفي `detail` اليوم الجديد `date`.' },
    { group: 'الأحداث', name: 'ins:month-view', kind: 'event', desc: 'يُطلق إذا بدّلت العرض، أو نقرت اسم يوم في الأسبوع ففتح يومه، وفي `detail` العرض الجديد `view`.' },
    { group: 'الأحداث', name: 'ins:month-day', kind: 'event', desc: 'يُطلق إذا نُقر يوم في موضع خال من مواعيده، وفي `detail` تاريخه `date`، وفي الأسبوع واليوم ساعته `time` أيضًا.' }
  ],
  events: ['ins:month', 'ins:month-date', 'ins:month-view', 'ins:month-day'],
  disable: function (s) { return s.view ? [] : ['allhours', 'hours', 'nohours']; },
  html: function (s) {
    var t = s.en
      ? ['Delivery', 'Meeting', 'Review', 'Workshop', 'Call', 'Training', 'Visit', 'Deadline', 'Holiday', 'Early flight', 'Team dinner']
      : ['تسليم', 'اجتماع', 'مراجعة', 'ورشة عمل', 'مكالمة', 'تدريب', 'زيارة', 'موعد نهائي', 'إجازة', 'رحلة مبكرة', 'عشاء العمل'];
    var list = [['03', '', 0], ['08', '10:00', 1, 90], ['08', '14:30', 2], ['15', '', 3], ['21', '09:00', 4, 90], ['22', '05:30', 9, 60], ['22', '19:00', 10, 90]];
    if (s.more) list.push(['21', '10:00', 5], ['21', '13:00', 6, 120], ['21', '16:00', 7, 30]);
    list.push(['26', '', 8]);
    var items = list.map(function (e, i) {
      var text = s.link && i === 0 ? '<a href="/events/1">' + t[e[2]] + '</a>' : t[e[2]];
      return '    <li data-date="2026-09-' + e[0] + '"' + (s.time && e[1] ? ' data-time="' + e[1] + '"' : '') +
        (s.time && e[1] && e[3] ? ' data-duration="' + e[3] + '"' : '') + (s.tone ? ' data-tone="' + s.tone + '"' : '') + '>' + text + '</li>';
    }).join('\n');
    var week = s.view ? ' data-ins-view="' + s.view + '" data-ins-date="2026-09-21"' +
      (s.allhours ? ' data-ins-work-hours="off"' : '') + (s.hours ? ' data-ins-day-hours="8-17"' : '') + (s.nohours ? ' data-ins-hours-switch="off"' : '') : '';
    return '<div class="ins-month" data-ins-month="2026-09"' + week + (s.en ? ' lang="en" dir="ltr"' : '') + '>\n' +
      '  <ul class="ins-month-events">\n' + items + '\n  </ul>\n</div>';
  },
  notes: function (s) {
    var out = [];
    if (s.view) {
      out.push('يقع كل موعد في ساعته بطول مدته، وإذا تداخل موعدان صارا جنبًا إلى جنب، والمواعيد بلا وقت في صف «طوال اليوم». ويمر خط أحمر على الساعة الحالية في اليوم الحالي.');
      out.push(s.allhours ? 'يعرض اليوم كله في صندوق بارتفاع الدوام، ويبدأ عند أول الدوام، فمرر لترى ما قبله وما بعده.'
        : 'يعرض ساعات الدوام وحدها، ' + (s.hours ? 'من 8:00 ص إلى 5:00 م' : 'من 6:00 ص إلى 6:00 م') + '. ' +
          (s.hours ? 'وفي يوم 22 موعد قبلها وآخر بعدها، فتجد عدد كل منهما في شريط فوق الساعات أو تحتها'
            : 'وفي يوم 22 موعد يبدأ قبلها فيُقص عند أولها، وآخر بعدها فتجد عدده في شريط تحت الساعات') +
          '، انقره ليظهر اليوم كله عند موعده. وزر «ساعات الدوام» في الرأس يبدّل بين الحالين.');
      if (s.view === 'week') out.push('انقر اسم يوم في رأس الأسبوع فيفتح يومه.');
      if (!s.more) out.push('شغّل «أكثر من ثلاثة في يوم» لترى موعدين متداخلين يوم 21.');
    }
    if (s.link) out.push('صار «تسليم» يوم 3 رابطًا يشبه غيره من المواعيد، فمرر عليه المؤشر يمتلئ بلونه.');
    if (s.more && !s.view) out.push('لا يعرض اليوم أكثر من ثلاثة مواعيد، فإن زادت كتب عدد ما بقي، مثل «+2».');
    if (s.en) out.push('صار الأسبوع يبدأ بالأحد كما يبدؤه أهل هذه اللغة، ومعها `dir="ltr"` ليجري الشهر من اليسار.');
    out.push('انتقل إلى شهر آخر أو أسبوع آخر، أو بدّل العرض، أو انقر يومًا خاليًا، فيظهر هنا الحدث.');
    return out;
  }
});
