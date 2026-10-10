SitePlay.define({
  id: 'combo',
  wide: true,
  title: 'الإكمال التلقائي',
  owns: 'ins-combo',
  controls: [
    { key: 'empty', label: 'رسالة «لا نتائج»', type: 'switch', value: true, name: 'ins-combo-empty', desc: 'سطر في آخر القائمة لا يظهر إلا إذا لم يطابق شيء ما كُتب.' },
    { key: 'hidden', label: 'يرسل رمز الخيار', type: 'switch', value: true, name: 'input[type="hidden"]', kind: 'element', desc: 'حقل مخفي في الصندوق يحمل `name` ويأخذ `data-value` الخيار المختار، فيرسل النموذج الرمز. ومن دونه ضع `name` على الحقل نفسه، فيُرسل ما كُتب فيه.' },
    { key: 'off', label: 'خيار معطل', type: 'switch', name: 'aria-disabled="true"', desc: 'على خيار: يبقى في القائمة باهتًا، فلا يُختار بالنقر ولا تقف عليه الأسهم.' }
  ],
  extra: [
    { group: 'الأساس', first: true, name: 'ins-combo', desc: 'يضم الحقل وقائمته، ويبني عليهما السكربت أدوار ARIA والتنقل بالأسهم.' },
    { group: 'الأساس', name: 'ins-combo-list', desc: 'قائمة الخيارات `<ul>`، تنفتح تحت الحقل وهو يكتب.' },
    { group: 'الأساس', name: 'ins-combo-option', desc: 'خيار واحد `<li>`، ويُرسل `data-value` إن كان عليه، وإلا فنصه.' },
    { group: 'الأساس', name: 'data-label', desc: 'على خيار: النص الذي يُكتب في الحقل حين يُختار، إن كان في الخيار أكثر من اسمه.' },
    { group: 'الأساس', name: 'ins:combo', kind: 'event', desc: 'يُطلق عند الاختيار، وفي `detail` الصندوق `el` والرمز `value` والنص `label` والخيار `option`.' }
  ],
  events: ['ins:combo'],
  html: function (s) {
    return '<div class="ins-combo">\n' +
      '  <input class="ins-input"' + (s.hidden ? '' : ' name="city"') + ' aria-label="المدينة" autocomplete="off">\n' +
      (s.hidden ? '  <input type="hidden" name="city">\n' : '') +
      '  <ul class="ins-combo-list">\n' +
      '    <li class="ins-combo-option" data-value="ruh">الرياض</li>\n' +
      '    <li class="ins-combo-option" data-value="jed">جدّة</li>\n' +
      '    <li class="ins-combo-option" data-value="cai"' + (s.off ? ' aria-disabled="true"' : '') + '>القاهرة</li>\n' +
      '    <li class="ins-combo-option" data-value="alx">الإسكندريّة</li>\n' +
      (s.empty ? '    <li class="ins-combo-empty">لا نتائج</li>\n' : '') +
      '  </ul>\n' +
      '</div>';
  },
  notes: function () {
    return ['اكتب في الحقل لترى القائمة بخياراتك، واكتب «دبي» لترى «لا نتائج». وإذا اخترت مدينة ظهر هنا الحدث الذي يطلقه.'];
  }
});

SitePlay.define({
  id: 'date',
  wide: true,
  title: 'التاريخ',
  owns: 'ins-date-btn',
  controls: [
    { key: 'mode', label: 'ما يُختار', type: 'select', value: 'day', choices: [
      { value: 'day', label: 'يوم واحد' },
      { value: 'range', label: 'فترة بين تاريخين', name: 'data-ins-date-range="to"', desc: 'فترة في حقل واحد: النقرة الأولى تحدد البداية والثانية النهاية، وقيمة السمة اسم النهاية، فيتلقى الخادم `from` و`to`.' },
      { value: 'datetime', label: 'تاريخ ووقت', name: 'type="datetime-local"', desc: 'مع `data-ins-date`: تقويم وإلى جانبه أعمدة للوقت، ويتلقى الخادم `2026-09-26T14:30`.' },
      { value: 'pair', label: 'تاريخ ووقت في حقلين', name: 'data-ins-datetime="when"', desc: 'على مجموعة فيها حقل تاريخ وحقل وقت `data-ins-time`: يرسلهما معًا باسم السمة، `2026-09-26T14:30`.' }
    ] },
    { key: 'bounds', label: 'حدود للتاريخ', group: 'الحدود', type: 'switch', name: 'min', desc: 'أول يوم يُختار، فتبهت الأيام قبله في التقويم، ويُرفض ما يُكتب قبله برسالة.' }
  ],
  extra: [
    { group: 'الأساس', first: true, name: 'data-ins-date', desc: 'على `<input type="date">` داخل `.ins-input-group`: يكتب التاريخ بلغة الصفحة ويفتح تقويمًا، ويبقى حقل المتصفح نفسه إن غاب السكربت.' },
    { group: 'الأساس', name: 'ins-date-btn', desc: 'زر التقويم، يضيفه السكربت إلى المجموعة، فلا تكتبه بنفسك.' },
    { group: 'الأساس', name: 'data-ins-calendar', desc: 'تقويم آخر للحقل، مثل `hijri` مع إضافة التقويم الهجري، ويبقى ما يُرسل ميلاديًا.' },
    { group: 'الأساس', name: 'ins:date', kind: 'event', desc: 'يُطلق حين يتغير التاريخ، وفي `detail` الحقل `input` وقيمته `value`، ونهاية الفترة `end`.' },
    { group: 'ما يُختار', name: 'data-ins-date-to', desc: 'تاريخ النهاية الذي تبدأ به الفترة.' },
    { group: 'الحدود', name: 'max', desc: 'آخر يوم يُختار.' }
  ],
  events: ['ins:date'],
  html: function (s) {
    var b = s.bounds, input;
    if (s.mode === 'range') {
      input = '<input class="ins-input" id="p-date" type="date" data-ins-date data-ins-date-range="to" name="from" value="2026-09-16" data-ins-date-to="2026-09-23"' +
        (b ? ' min="2026-09-01" max="2026-10-31"' : '') + '>';
    } else if (s.mode === 'datetime') {
      input = '<input class="ins-input" id="p-date" type="datetime-local" data-ins-date name="when" value="2026-09-26T14:30" step="900"' +
        (b ? ' min="2026-09-01T00:00" max="2026-10-31T23:59"' : '') + '>';
    } else if (s.mode === 'pair') {
      input = '<input class="ins-input" id="p-date" type="date" data-ins-date value="2026-09-26"' + (b ? ' min="2026-09-01" max="2026-10-31"' : '') + '>\n' +
        '    <input class="ins-input" type="time" data-ins-time value="14:30" aria-label="الوقت">';
    } else {
      input = '<input class="ins-input" id="p-date" type="date" data-ins-date name="date" value="2026-09-30"' + (b ? ' min="2026-09-01" max="2026-10-31"' : '') + '>';
    }
    var label = s.mode === 'range' ? 'الفترة' : s.mode === 'day' ? 'التاريخ' : 'الموعد';
    return '<div class="ins-field">\n' +
      '  <label class="ins-label" for="p-date">' + label + '</label>\n' +
      '  <div class="ins-input-group"' + (s.mode === 'pair' ? ' data-ins-datetime="when"' : '') + '>\n' +
      '    ' + input + '\n' +
      '  </div>\n' +
      '</div>';
  },
  notes: function () {
    return ['افتح التقويم لترى خياراتك فيه.'];
  }
});

SitePlay.define({
  id: 'time',
  wide: true,
  title: 'الوقت',
  owns: 'ins-time',
  controls: [
    { key: 'step', label: 'الخطوة بالدقائق', type: 'seg', value: '', choices: [
      { value: '900', label: '15', name: 'step="900"', desc: 'كم ثانية تقفز الدقائق في عمودها، ونصف ساعة إن غابت.' },
      { value: '', label: '30' },
      { value: '3600', label: '60' }
    ] },
    { key: 'bounds', label: 'حدود للوقت', group: 'الحدود', type: 'switch', name: 'min', desc: 'أول وقت يُختار، ويُرفض ما يُكتب قبله برسالة.' }
  ],
  extra: [
    { group: 'الأساس', first: true, name: 'data-ins-time', desc: 'على `<input type="time">`: يعرض الوقت بلغة الصفحة، ويقبل كتابته مثل «14:30» و«2:30 م»، ويرسل `HH:MM` باسم الحقل.' },
    { group: 'الأساس', name: 'ins-time', desc: 'يلف السكربت الحقل به ومعه الحقل المخفي، فلا تكتبه بنفسك.' },
    { group: 'الأساس', name: 'ins:time', kind: 'event', desc: 'يُطلق حين يتغير الوقت، وفي `detail` الحقل `input` وقيمته `value`.' },
    { group: 'الحدود', name: 'max', desc: 'آخر وقت يُختار.' }
  ],
  events: ['ins:time'],
  html: function (s) {
    return '<div class="ins-field">\n' +
      '  <label class="ins-label" for="p-time">الموعد</label>\n' +
      '  <input class="ins-input" id="p-time" type="time" data-ins-time name="at" value="09:00"' +
      (s.step ? ' step="' + s.step + '"' : '') + (s.bounds ? ' min="08:00" max="18:00"' : '') + '>\n' +
      '</div>';
  },
  notes: function () {
    return ['افتح الحقل لترى الأعمدة بالخطوة والحدود التي اخترتها.'];
  }
});

SitePlay.define({
  id: 'range',
  title: 'المنزلق',
  owns: 'ins-range',
  controls: [
    { key: 'step', label: 'الخطوة', type: 'seg', value: '5', level: 'advanced', choices: [
      { value: '1', label: '1' },
      { value: '5', label: '5' },
      { value: '10', label: '10', name: 'step', desc: 'مقدار كل خطوة، بالسحب وبالأسهم.' }
    ] },
    { key: 'disabled', label: 'معطل', type: 'switch', level: 'advanced', name: 'disabled', desc: 'يبهت المنزلق ولا يتحرك.' }
  ],
  extra: [
    { group: 'الأساس', first: true, level: 'advanced', name: 'ins-range', desc: 'على `<input type="range">`: يملأ المسار بلون الهوية حتى المقبض، ولا بد له من تسمية.' },
    { group: 'الأساس', level: 'advanced', name: 'min', desc: 'ومعها `max` و`value`، كما في أي منزلق، وفي الصفحة العربية يقع الحد الأدنى على اليمين.' }
  ],
  html: function (s) {
    return '<input class="ins-range" type="range" min="0" max="50" step="' + s.step + '" value="20" aria-label="النسبة"' + (s.disabled ? ' disabled' : '') + '>';
  },
  stage: function (view) {
    var input = view.querySelector('.ins-range');
    input.stepUp();
    input.dispatchEvent(new Event('input', { bubbles: true }));
  },
  notes: function (s) {
    return ['هكذا يبدو بعد خطوة واحدة: من 20 إلى `' + (20 + +s.step) + '`.'];
  }
});

SitePlay.define({
  id: 'stepper',
  title: 'العداد',
  owns: 'ins-spin',
  controls: [
    { key: 'step', label: 'الخطوة', type: 'seg', value: '', choices: [
      { value: '', label: '1' },
      { value: '5', label: '5', name: 'step', desc: 'مقدار كل ضغطة، كما يفهمه المتصفح نفسه.' }
    ] },
    { key: 'bounds', label: 'حدود للعدد', group: 'الحدود', type: 'switch', value: true, name: 'min', desc: 'أقل عدد، فيتعطل زر الإنقاص عنده.' },
    { key: 'disabled', label: 'معطل', type: 'switch', name: 'disabled', desc: 'على الحقل: يتعطل معه الزران.' }
  ],
  extra: [
    { group: 'الأساس', first: true, name: 'ins-spin', desc: 'زر بجانب حقل رقمي داخل `.ins-input-group`، يرسم علامته بنفسه ولا بد له من `aria-label`.' },
    { group: 'الأساس', name: 'data-ins-spin="down"', desc: 'زر الإنقاص، في أول المجموعة.' },
    { group: 'الأساس', name: 'data-ins-spin="up"', desc: 'زر الزيادة، في آخرها.' },
    { group: 'الحدود', name: 'max', desc: 'أكبر عدد، فيتعطل زر الزيادة عنده.' }
  ],
  html: function (s) {
    return '<div class="ins-input-group">\n' +
      '  <button class="ins-spin" data-ins-spin="down" aria-label="إنقاص"></button>\n' +
      '  <input class="ins-input" type="number"' + (s.bounds ? ' min="0" max="20"' : '') + (s.step ? ' step="' + s.step + '"' : '') +
      ' value="5" aria-label="العدد"' + (s.disabled ? ' disabled' : '') + '>\n' +
      '  <button class="ins-spin" data-ins-spin="up" aria-label="زيادة"></button>\n' +
      '</div>';
  },
  stage: function (view, s) {
    if (s.disabled) return;
    var input = view.querySelector('input[type="number"]');
    input.stepUp();
    input.dispatchEvent(new Event('input', { bubbles: true }));
  },
  notes: function (s) {
    if (s.disabled) return [];
    var out = ['هكذا يبدو بعد ضغطة واحدة على +: من 5 إلى `' + (5 + (+s.step || 1)) + '`.'];
    if (s.bounds) out.push('اضغط − حتى الصفر، فيتعطل الزر عنده.');
    return out;
  }
});
