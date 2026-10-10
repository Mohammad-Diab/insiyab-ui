SitePlay.define({
  id: 'otp',
  title: 'حقل الرمز',
  owns: 'ins-otp',
  controls: [
    { key: 'size', label: 'عدد الخانات', type: 'seg', value: '6', choices: [
      { value: '4', label: '4' },
      { value: '6', label: '6' },
      { value: '8', label: '8' }
    ] },
    { key: 'alnum', label: 'حروف وأرقام', type: 'switch', name: 'data-ins-otp-chars="alnum"', desc: 'لرمز فيه حروف لاتينية وأرقام، فتظهر حروفه كبيرة. والأرقام وحدها هي الأصل.' },
    { key: 'disabled', label: 'معطل', type: 'switch', name: 'disabled', desc: 'يبهت الحقل ولا يُكتب فيه.' },
    { key: 'submit', label: 'يُرسل وحده', type: 'switch', name: 'data-ins-otp-submit', desc: 'يرسل النموذج حين تمتلئ آخر خانة.' },
    { key: 'invalid', label: 'رمز مرفوض', type: 'switch', name: 'aria-invalid="true"', desc: 'رمز رفضه الخادم، فيحمر الحقل حتى يعود صاحبه إلى الكتابة.' }
  ],
  extra: [
    { group: 'الأساس', first: true, name: 'data-ins-otp="6"', desc: 'تجعل الحقل حقل رمز بهذا العدد من الخانات، من 4 إلى 10، فإن خلت من العدد أُخذ من `maxlength`، وإلا فستة.' },
    { group: 'الأساس', name: 'ins-otp', desc: 'الكتلة التي يلف بها السكربت الحقل ويرسم فيها الخانات، فلا تكتبها بنفسك.' },
    { group: 'الأساس', name: 'ins:otp', kind: 'event', desc: 'يُطلق حين يكتمل الرمز، وفي `detail` الحقل `input` والرمز `value`.' }
  ],
  disable: function (s) { return s.disabled ? ['invalid', 'submit'] : []; },
  events: ['ins:otp'],
  html: function (s) {
    var input = '<input id="otp-play" data-ins-otp="' + s.size + '"' + (s.alnum ? ' data-ins-otp-chars="alnum"' : '') +
      (s.submit ? ' data-ins-otp-submit' : '') + (s.invalid && !s.disabled ? ' aria-invalid="true"' : '') + (s.disabled ? ' disabled' : '') + ' name="code">';
    var tag = s.submit && !s.disabled ? 'form' : 'div';
    return '<' + tag + ' class="ins-field">\n' +
      '  <label class="ins-label" for="otp-play">رمز التحقق</label>\n' +
      '  ' + input + '\n' +
      '</' + tag + '>';
  },
  stage: function (view, s) {
    if (window.Insiyab && Insiyab.otp) Insiyab.otp(view.querySelector('input[data-ins-otp]'), s.alnum ? 'A7K' : '482');
  },
  notes: function (s) {
    var out = [];
    if (s.submit && !s.disabled) out.push('املأ الخانات، فيُرسل النموذج وحده. والإرسال هنا رسالة عابرة لا أكثر.');
    if (s.invalid && !s.disabled) out.push('اكتب رقمًا، فيعود الحقل إلى لونه.');
    return out;
  }
});
