SitePlay.define({
  id: 'color',
  wide: true,
  title: 'حقل اللون',
  owns: 'ins-color',
  controls: [
    { key: 'swatches', label: 'الألوان الجاهزة', type: 'select', value: '', choices: [
      { value: '', label: 'لون الهوية وما حوله' },
      { value: 'set', label: 'ألوان محددة', name: 'data-ins-color-swatches="#15803d,#a16207,#b91c1c"', desc: 'الألوان الجاهزة في المنتقي، مكان لون الهوية ولوحة الألوان حوله.' },
      { value: 'none', label: 'بلا ألوان جاهزة', name: 'data-ins-color-swatches="none"', desc: 'يخفي الألوان الجاهزة من المنتقي.' }
    ] },
    { key: 'disabled', label: 'معطل', type: 'switch', name: 'disabled', desc: 'يتعطل الحقل وعينته، فلا يُفتح المنتقي.' }
  ],
  extra: [
    { group: 'الأساس', first: true, name: 'data-ins-color', desc: 'على `<input type="color">`: حقل لرمز hex أمامه عينة تفتح المنتقي، ويرسل النموذج `#rrggbb` باسم الحقل.' },
    { group: 'الأساس', name: 'ins-color', desc: 'يضيفه السكربت إلى مجموعة الحقل، ويبنيها إن غابت، فلا تكتبه بنفسك.' },
    { group: 'الأساس', name: 'ins:color', kind: 'event', desc: 'يُطلق حين يستقر اللون، وفي `detail` الحقل `input` واللون `value`. ويُطلق `input` ما دام اللون يتغير.' }
  ],
  events: ['ins:color'],
  html: function (s) {
    var list = s.swatches === 'set' ? ' data-ins-color-swatches="#15803d,#a16207,#b91c1c"' : s.swatches === 'none' ? ' data-ins-color-swatches="none"' : '';
    return '<div class="ins-field">\n' +
      '  <label class="ins-label" for="cp-play">اللون</label>\n' +
      '  <div class="ins-input-group">\n' +
      '    <input type="color" id="cp-play" name="color" value="#0f766e" data-ins-color' + list + (s.disabled ? ' disabled' : '') + '>\n' +
      '  </div>\n' +
      '</div>';
  },
  notes: function (s) {
    return s.disabled ? [] : ['اضغط العينة لترى المنتقي بخياراتك.'];
  }

});
