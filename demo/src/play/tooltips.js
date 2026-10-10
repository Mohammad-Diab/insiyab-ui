SitePlay.define({
  id: 'tip',
  title: 'التلميح',
  owns: 'ins-tooltip',
  controls: [
    { key: 'on', label: 'على', type: 'select', value: 'icon', choices: [
      { value: 'icon', label: 'زر أيقونة' },
      { value: 'button', label: 'زر بنص' },
      { value: 'span', label: 'نص عادي', name: 'tabindex="0"', desc: 'لعنصر لا يقبل التركيز، حتى يبلغ تلميحه من يستعمل لوحة المفاتيح.' }
    ] },
    { key: 'text', label: 'نص التلميح', type: 'text', value: 'تعديل' },
    { key: 'side', label: 'الجهة', type: 'select', value: '', choices: [
      { value: '', label: 'فوق' },
      { value: 'bottom', label: 'تحت', name: 'data-ins-tip-side="bottom"', desc: 'تحت العنصر.' },
      { value: 'start', label: 'البداية', name: 'data-ins-tip-side="start"', desc: 'في جهة البداية، وهي اليمين في العربية.' },
      { value: 'end', label: 'النهاية', name: 'data-ins-tip-side="end"', desc: 'في جهة النهاية، وهي اليسار في العربية.' }
    ] }
  ],
  extra: [
    { group: 'الأساس', first: true, name: 'data-ins-tip', desc: 'يظهر التلميح حين يقف المؤشر على العنصر أو يبلغه التركيز. وقيمتها نص التلميح، فإن تركتها بلا قيمة أخذ نصه من `aria-label`.' },
    { group: 'الأساس', name: 'aria-describedby', desc: 'تضعها المكتبة بنفسها إذا قال التلميح غير اسم العنصر، فيسمعه قارئ الشاشة بعد الاسم.' },
    { group: 'الأساس', name: 'ins-tooltip', desc: 'التلميح نفسه: عنصر واحد للصفحة كلها في الطبقة العليا، تبنيه المكتبة فلا تكتبه بنفسك، ولا يقصه `overflow` لوح أو جدول.' },
    { group: 'الجهة', name: 'data-ins-tip-side="top"', desc: 'فوق العنصر، وهي الجهة إن لم تذكر غيرها. وإذا ضاق المكان بالتلميح انتقل إلى الجهة المقابلة.' }
  ],
  html: function (s) {
    var side = s.side ? ' data-ins-tip-side="' + s.side + '"' : '';
    if (s.on === 'icon') return '<button class="ins-btn ins-btn--secondary ins-btn--icon" aria-label="' + s.text + '" data-ins-tip' + side + '><svg class="ico"><use href="#i-edit"/></svg></button>';
    if (s.on === 'button') return '<button class="ins-btn ins-btn--secondary" data-ins-tip="' + s.text + '"' + side + '>حذف</button>';
    return '<span tabindex="0" data-ins-tip="' + s.text + '"' + side + '>نص عادي</span>';
  },
  notes: function () { return ['قف بالمؤشر على العنصر أو بلغه بمفتاح Tab لترى التلميح بخياراتك.']; }
});
