SitePlay.define({
  id: 'phone',
  wide: true,
  title: 'حقل الهاتف',
  owns: 'ins-phone',
  controls: [
    { key: 'value', label: 'فيه رقم', type: 'switch', name: 'value="+966501234567"', desc: 'الرقم الذي يبدأ به الحقل، بصيغة E.164 أو بالصيغة المحلية، ودولته تغلب `data-ins-phone-country`.' },
    { key: 'country', label: 'الدولة الأولى', type: 'select', value: '', choices: [
      { value: '', label: 'من لغة الصفحة' },
      { value: 'AE', label: 'الإمارات', name: 'data-ins-phone-country="AE"', desc: 'الدولة التي يبدأ بها الحقل، فإن غابت فدولة الرقم نفسه، ثم منطقة لغة الصفحة (`lang="ar-EG"` مصر)، وإلا فالسعودية.' },
      { value: 'KW', label: 'الكويت' },
      { value: 'QA', label: 'قطر' }
    ] },
    { key: 'only', label: 'دول محددة وحدها', type: 'switch', name: 'data-ins-phone-only="SA,AE,KW,QA,BH,OM"', desc: 'لا يعرض الحقل إلا الدول التي تكتب رموزها، أيًّا كانت، ويرفض رقمًا من غيرها برسالة. والمثال هنا دول الخليج.' },
    { key: 'preferred', label: 'دول تتصدر القائمة', type: 'switch', name: 'data-ins-phone-preferred="EG,JO,SY,LB"', desc: 'دول في أول القائمة، فوق الباقي.' },
    { key: 'disabled', label: 'معطل', type: 'switch', name: 'disabled', desc: 'يتعطل معه زر الدولة.' }
  ],
  extra: [
    { group: 'الأساس', first: true, name: 'data-ins-phone', desc: 'على حقل داخل `.ins-input-group`: يضع في أولها زرًا يعرض رمز الدولة ومفتاحها، ويرسل الرقم بصيغة E.164 في حقل مخفي باسم الحقل.' },
    { group: 'الأساس', name: 'ins-phone', desc: 'يضيفه السكربت إلى المجموعة، ويبنيها إن غابت، فلا تكتبه بنفسك.' },
    { group: 'الأساس', name: 'placeholder', desc: 'إن تركته وضع الحقل فيه مثالًا من الدولة المختارة.' },
    { group: 'الأساس', name: 'ins:phone', kind: 'event', desc: 'يُطلق حين تتغير القيمة، وفي `detail` الحقل `input` والرقم `value` والدولة `country` وصلاحه `valid`.' }
  ],
  disable: function (s) { return (s.value ? ['country'] : []).concat(s.only ? ['preferred'] : []); },
  events: ['ins:phone'],
  html: function (s) {
    var attrs = (s.value ? '' : s.country ? ' data-ins-phone-country="' + s.country + '"' : '') +
      (s.only ? ' data-ins-phone-only="SA,AE,KW,QA,BH,OM"' : '') + (s.preferred && !s.only ? ' data-ins-phone-preferred="EG,JO,SY,LB"' : '');
    return '<div class="ins-field">\n' +
      '  <label class="ins-label" for="ph-play">الهاتف</label>\n' +
      '  <div class="ins-input-group">\n' +
      '    <input class="ins-input" id="ph-play" data-ins-phone' + attrs + ' name="mobile"' + (s.value ? ' value="+966501234567"' : '') + (s.disabled ? ' disabled' : '') + '>\n' +
      '  </div>\n' +
      '</div>';
  },
  notes: function (s) {
    if (s.disabled) return [];
    return [s.value ? 'الرقم يختار دولته بنفسه، فلا حاجة إلى الدولة الأولى. واضغط زر الدولة لترى القائمة بخياراتك.' : 'اضغط زر الدولة لترى القائمة بخياراتك.'];
  }

});
