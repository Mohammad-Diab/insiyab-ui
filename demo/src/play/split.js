SitePlay.define({
  id: 'split',
  title: 'العرض المقسوم',
  owns: 'ins-split',
  wide: true,
  controls: [
    { key: 'start', label: 'حصة اللوح الأول', type: 'seg', value: '', choices: [
      { value: '30', label: '30%', name: 'data-ins-split-default="30"', desc: 'حصة اللوح الأول حين يُفتح أول مرة، وإليها يعيده النقر المزدوج و`Enter`. والأصل 50.' },
      { value: '', label: '50%' },
      { value: '70', label: '70%' }
    ] },
    { key: 'limits', label: 'حدود أضيق للسحب', type: 'switch' },
    { key: 'keep', label: 'يتذكر القسمة', type: 'switch', name: 'data-ins-split', desc: 'أعطها اسمًا، فتحفظ المكتبة القسمة باسمه وتعيدها في الزيارة القادمة.' },
    { key: 'v', label: 'لوح فوق لوح', type: 'switch', level: 'advanced', name: 'ins-split--v', desc: 'يضع اللوحين أحدهما فوق الآخر، فيصير المقبض أفقيًا.' }
  ],
  extra: [
    { group: 'الأساس', first: true, name: 'ins-split', desc: 'يضع لوحين جنبًا إلى جنب يتقاسمان العرض، ولا بد له من ارتفاع تعطيه إياه، فيتمرر كل لوح وحده.' },
    { group: 'الأساس', name: 'ins-split-pane', desc: 'كل لوح من اللوحين.' },
    { group: 'الأساس', name: 'ins-split-handle', desc: 'المقبض بين اللوحين، يضعه السكربت بنفسه، وتحركه الأسهم أيضًا، ويذهب به `Home` و`End` إلى الطرفين.' },
    { group: 'حدود أضيق للسحب', name: 'data-ins-min="60"', desc: 'أصغر حصة للوح الأول بالنسبة المئوية، ولا تنزل عنها ولا الحصة التي يبدأ بها. والأصل 20.' },
    { group: 'حدود أضيق للسحب', name: 'data-ins-max="75"', desc: 'أكبر حصة للوح الأول، والأصل 80.' },
    { group: 'السكربت', name: 'is-dragging', kind: 'class', level: 'advanced', desc: 'يضعه السكربت على العرض المقسوم ومقبضه ما دام السحب جاريًا.' },
    { group: 'السكربت', name: 'ins:split', kind: 'event', level: 'advanced', desc: 'يُطلق كلما تغيرت القسمة بالسحب أو بلوحة المفاتيح، وفي `detail` العرض المقسوم `el` وحصة اللوح الأول `size`.' }
  ],
  events: ['ins:split'],
  html: function (s) {
    var attrs = (s.keep ? ' data-ins-split="panes"' : '') + (s.start ? ' data-ins-split-default="' + s.start + '"' : '') +
      (s.limits ? ' data-ins-min="60" data-ins-max="75"' : '');
    return '<div class="ins-split' + (s.v ? ' ins-split--v' : '') + '"' + attrs + ' style="block-size: 12rem">\n' +
      '  <section class="ins-split-pane"><div class="demo-box demo-box--tall">اللوح الأول</div></section>\n' +
      '  <section class="ins-split-pane"><div class="demo-box demo-box--tall">اللوح الثاني</div></section>\n' +
      '</div>';
  },
  notes: function (s) {
    var out = ['اسحب الخط بين اللوحين، فيظهر هنا الحدث الذي يطلقه.'];
    if (s.limits) out.push('لا تخرج القسمة عن 60% و75%، حتى الحصة التي يبدأ بها اللوح الأول.');
    if (s.keep) out.push('غيّر القسمة ثم أعد تحميل الصفحة، فتجدها كما تركتها.');
    return out;
  }
});
