SitePlay.define({
  id: 'cols',
  title: 'الأعمدة',
  owns: 'ins-cols',
  wide: true,
  controls: [
    { key: 'count', label: 'عدد الأعمدة', type: 'seg', value: '3', choices: [
      { value: '2', label: '2', name: 'ins-cols-2', desc: 'عمودان، يصيران عمودًا واحدًا على الهاتف.' },
      { value: '3', label: '3', name: 'ins-cols-3', desc: 'ثلاثة أعمدة، تصير عمودين عند 900px ثم عمودًا واحدًا على الهاتف.' },
      { value: '4', label: '4', name: 'ins-cols-4', desc: 'أربعة أعمدة، تنطوي كما تنطوي الثلاثة.' },
      { value: '6', label: '6', name: 'ins-cols-6', desc: 'ستة أعمدة، تنطوي كما تنطوي الثلاثة.' },
      { value: '12', label: '12', name: 'ins-cols-12', desc: 'اثنا عشر عمودًا تتقاسمها خلايا `ins-span-N`، وتنطوي كما تنطوي الثلاثة.' }
    ] },
    { key: 'fixed', label: 'عدد ثابت في كل مقاس', type: 'switch', name: 'ins-cols--fixed', desc: 'يبقي الشبكة على عدد أعمدتها في كل مقاس، لما كان عدده ثابتًا بطبيعته كأيام الأسبوع.' }
  ],
  html: function (s) {
    var n = +s.count, cells = [];
    for (var i = 1; i <= n; i++) cells.push('  <div class="demo-box">' + i + '</div>');
    return '<div class="ins-cols-' + n + (s.fixed ? ' ins-cols--fixed' : '') + '">\n' + cells.join('\n') + '\n</div>';
  },
  notes: function (s) {
    if (s.fixed) return ['تبقى الأعمدة على عددها مهما ضاقت الشاشة، فاختره لما يتسع له الهاتف.'];
    return [s.count === '2' ? 'ضيّق النافذة، فيصير العمودان عمودًا واحدًا على الهاتف.' : 'ضيّق النافذة، فتصير الأعمدة عمودين عند 900px، ثم عمودًا واحدًا على الهاتف.'];
  }
});

SitePlay.define({
  id: 'span',
  title: 'الامتداد',
  owns: 'ins-span',
  wide: true,
  controls: [
    { key: 'span', label: 'تمتد الخلية الأولى على', type: 'select', value: '8', choices: [
      { value: '2', label: 'عمودين', name: 'ins-span-2', desc: 'تمتد الخلية على عمودين.' },
      { value: '3', label: '3 أعمدة', name: 'ins-span-3', desc: 'على 3 أعمدة، أي ربع السطر.' },
      { value: '4', label: '4 أعمدة', name: 'ins-span-4', desc: 'على 4 أعمدة، أي ثلث السطر.' },
      { value: '5', label: '5 أعمدة', name: 'ins-span-5', desc: 'على 5 أعمدة.' },
      { value: '6', label: '6 أعمدة', name: 'ins-span-6', desc: 'على 6 أعمدة، أي نصف السطر.' },
      { value: '7', label: '7 أعمدة', name: 'ins-span-7', desc: 'على 7 أعمدة.' },
      { value: '8', label: '8 أعمدة', name: 'ins-span-8', desc: 'على 8 أعمدة، أي ثلثي السطر، كعمود المحتوى بجوار عمود جانبي.' },
      { value: '9', label: '9 أعمدة', name: 'ins-span-9', desc: 'على 9 أعمدة، أي ثلاثة أرباع السطر.' },
      { value: '10', label: '10 أعمدة', name: 'ins-span-10', desc: 'على 10 أعمدة.' },
      { value: '11', label: '11 عمودًا', name: 'ins-span-11', desc: 'على 11 عمودًا.' },
      { value: '12', label: '12 عمودًا', name: 'ins-span-12', desc: 'على الأعمدة الاثني عشر كلها.' },
      { value: 'full', label: 'السطر كله', name: 'ins-span-full', desc: 'على السطر كله، في أي شبكة وبأي عدد من الأعمدة.' }
    ] }
  ],
  html: function (s) {
    var rows = ['<div class="ins-cols-12">', '  <div class="demo-box ins-span-' + s.span + '">span-' + s.span + '</div>'];
    var rest = s.span === 'full' ? 0 : 12 - s.span;
    if (rest > 1) rows.push('  <div class="demo-box ins-span-' + rest + '">span-' + rest + '</div>');
    if (rest === 1) rows.push('  <div class="demo-box">1</div>');
    return rows.concat('</div>').join('\n');
  },
  notes: function () {
    return ['تأخذ الخلية الممتدة السطر كله عند 900px، ثم تصير الشبكة عمودًا واحدًا على الهاتف.'];
  }
});

SitePlay.define({
  id: 'grid',
  title: 'الشبكة المرنة',
  owns: 'ins-grid',
  wide: true,
  controls: [
    { key: 'min', label: 'أقل عرض للخلية', type: 'select', value: '', choices: [
      { value: '', label: '11rem' },
      { value: 'wide', label: '20rem', name: 'ins-grid--wide', desc: 'لا يقل عرض الخلية عن 20rem، لبطاقات فيها أكثر من رقم.' },
      { value: 'half', label: '26rem', name: 'ins-grid--half', desc: 'لا يقل عرضها عن 26rem، فلا يتسع السطر إلا لخليتين على الشاشة العريضة.' },
      { value: 'own', label: 'بنفسك', name: 'data-ins-col="14rem"', desc: 'تضبط أقل عرض للخلية بنفسك.' }
    ] }
  ],
  extra: [
    { group: 'الأساس', first: true, name: 'ins-grid', desc: 'يضع في السطر ما يتسع له من خلايا لا يقل عرضها عن 11rem، وتدخل خلاياها واحدة تلو الأخرى.' }
  ],
  html: function (s) {
    var cls = 'ins-grid' + (s.min === 'wide' || s.min === 'half' ? ' ins-grid--' + s.min : '') + '';
    var cells = [];
    for (var i = 1; i <= 5; i++) cells.push('  <div class="demo-box demo-box--tall">' + i + '</div>');
    return '<div class="' + cls + '"' + (s.min === 'own' ? ' data-ins-col="14rem"' : '') + '>\n' + cells.join('\n') + '\n</div>';
  }
});
