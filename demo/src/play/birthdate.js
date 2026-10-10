SitePlay.define({
  id: 'birth',
  title: 'حقل الميلاد',
  owns: 'ins-birth',
  controls: [
    { key: 'look', label: 'الشكل', type: 'seg', value: '', choices: [
      { value: '', label: 'حقل', name: 'ins-birth--field', desc: 'الشكل المعتاد: حقل بارتفاع أي حقل في النموذج، وأعمدته في نافذة منبثقة. يضعه السكربت، فلا تكتبه بنفسك.' },
      { value: 'inline', label: 'كبير', name: 'data-ins-birth="inline"', desc: 'حين يكون تاريخ الميلاد هو السؤال الوحيد في الشاشة، كخطوة في معالج تسجيل: كتل كبيرة ينمو فيها الرقم، وتحت كل كتلة عمودها، دون نافذة منبثقة.' }
    ] },
    { key: 'min', label: 'أقل عمر', group: 'العمر', type: 'seg', value: '', choices: [
      { value: '', label: 'بلا' },
      { value: '18', label: '18', name: 'data-ins-age-min="18"', desc: 'أقل عمر مقبول، فينتهي عمود السنة عنده، ويُرفض كل تاريخ أقرب منه برسالة تقول السبب. الافتراض 0.' }
    ] },
    { key: 'max', label: 'أكبر عمر', group: 'العمر', type: 'seg', value: '', choices: [
      { value: '', label: '120' },
      { value: '70', label: '70', name: 'data-ins-age-max="70"', desc: 'أكبر عمر، فيقصر عمود السنة. الافتراض 120.' }
    ] },
    { key: 'start', label: 'عمر البداية', group: 'العمر', type: 'seg', value: '', choices: [
      { value: '', label: '31' },
      { value: '22', label: '22', name: 'data-ins-age-start="22"', desc: 'العمر الذي يبدأ منه عمود السنة، لموقع يعرف أعمار جمهوره. الافتراض 31، وهو الوسيط العالمي للعمر.' }
    ] },
    { key: 'value', label: 'فيه تاريخ', type: 'switch', name: 'value="1993-01-22"', desc: 'التاريخ الذي يبدأ منه الحقل، ويعود إليه إذا أُعيد ضبط النموذج.' },
    { key: 'disabled', label: 'معطل', type: 'switch', name: 'disabled', desc: 'يبهت الحقل كله، فلا يُكتب فيه ولا تُفتح أعمدته.' }
  ],
  extra: [
    { group: 'الأساس', first: true, name: 'data-ins-birth', desc: 'على `<input type="date">`: يصير حقلًا لليوم والشهر والسنة تكتب فيه أو تختار من الأعمدة، ويبقى الأصلي مخفيًا يحمل التاريخ بصيغة ISO.' },
    { group: 'الأساس', name: 'ins-birth', desc: 'الكتلة التي يبنيها السكربت بعد الحقل، فلا تكتبها بنفسك.' },
    { group: 'الأساس', name: 'required', desc: 'يمنع الحقل الفارغ إرسال النموذج، وتظهر رسالته في `.ins-error` مع `data-ins-validate`.' },
    { group: 'الأساس', name: 'ins:birth', kind: 'event', desc: 'يُطلق حين يتغير التاريخ، وفي `detail` الحقل `input` والتاريخ `value`، أو فراغ ما دام ناقصًا، و`date`.' },
    { group: 'الشكل', name: 'ins-birth--inline', desc: 'كلاس الشكل الكبير، يضعه السكربت مع `data-ins-birth="inline"`.' }
  ],
  events: ['ins:birth'],
  html: function (s) {
    var attrs = (s.look ? ' data-ins-birth="inline"' : ' data-ins-birth') +
      (s.min ? ' data-ins-age-min="' + s.min + '"' : '') + (s.max ? ' data-ins-age-max="' + s.max + '"' : '') + (s.start ? ' data-ins-age-start="' + s.start + '"' : '') +
      (s.value ? ' value="1993-01-22"' : '') + (s.disabled ? ' disabled' : '');
    return '<div class="ins-field">\n' +
      '  <label class="ins-label" for="birth-play">تاريخ الميلاد</label>\n' +
      '  <input type="date" id="birth-play" name="birth"' + attrs + '>\n' +
      '</div>';
  },
  notes: function (s) {
    var out = [];
    if (s.min || s.max || s.start) out.push(s.look ? 'انظر عمود السنة، فقد تغيرت سنواته.' : 'افتح الأعمدة، فتجد سنوات عمود السنة قد تغيرت.');
    return out;
  }
});
