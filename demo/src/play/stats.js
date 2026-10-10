SitePlay.define({
  id: 'stat',
  title: 'البطاقة',
  owns: 'ins-stat',
  controls: [
    { key: 'tone', label: 'النبرة', type: 'select', value: '', choices: [
      { value: '', label: 'عادية' },
      { value: 'ok', label: 'نجاح', name: 'ins-stat--ok', desc: 'لرقم محمود، كالمكتمل.' },
      { value: 'warn', label: 'تحذير', name: 'ins-stat--warn', desc: 'لرقم ينتظر أو يحتاج انتباهًا، كالمعلق.' },
      { value: 'bad', label: 'خطر', name: 'ins-stat--bad', desc: 'لرقم يدل على خلل، كالفاشل.' },
      { value: 'info', label: 'معلومة', name: 'ins-stat--info', desc: 'لرقم يُعرض للعلم وحسب.' },
      { value: 'zero', label: 'صفر', name: 'ins-stat--zero', desc: 'للقيمة الصفرية، فتطمئن القارئ بدل أن تنبهه.' },
      { value: 'mute', label: 'خافتة', name: 'ins-stat--mute', desc: 'للقيمة الثانوية، فتخفت بين أخواتها.' }
    ] },
    { key: 'size', label: 'المقاس', type: 'seg', value: '', choices: [
      { value: 'sm', label: 'صغير', name: 'ins-stat--sm', desc: 'لشريط تتزاحم فيه البطاقات.' },
      { value: '', label: 'عادي' },
      { value: 'lg', label: 'كبير', name: 'ins-stat--lg', desc: 'لأهم رقم في الصفحة، ويأخذ الصف كله على الهاتف.' }
    ] },
    { key: 'label', label: 'التسمية', type: 'text', value: 'الملفات' },
    { key: 'value', label: 'القيمة', type: 'text', value: '128' },
    { key: 'glass', label: 'سطح قائم بذاته', type: 'switch', value: true, name: 'ins-glass', desc: 'يجعل البطاقة سطحًا زجاجيًا قائمًا بذاته. ومن دونه ضعها داخل لوح.' },
    { key: 'hint', label: 'تلميح', type: 'switch', name: 'ins-stat-hint', desc: 'سطر تحت القيمة يبين سياقها.' },
    { key: 'trend', label: 'التغير', type: 'switch', name: 'ins-stat-trend', desc: 'سطر تحت القيمة يضم `.ins-delta` و`.ins-delta-ref`.' },
    { key: 'link', label: 'رابط', type: 'switch', name: 'a.ins-stat', kind: 'element', desc: 'بطاقة تنقل إلى مكان، فتتحرك أيقونتها تحت المؤشر. وكذلك على `<button>`، أو مع `.ins-hoverable`.' }
  ],
  extra: [
    { group: 'الأساس', first: true, name: 'ins-stat', desc: 'البطاقة: أيقونة في رقاقة، ثم تسمية وقيمة.' },
    { group: 'الأساس', name: 'ins-stat-ico', desc: 'رقاقة الأيقونة، بلون النبرة.' },
    { group: 'الأساس', name: 'ins-stat-body', desc: 'يضم التسمية والقيمة وما تحتهما.' },
    { group: 'الأساس', name: 'ins-stat-label', desc: 'ما يعنيه الرقم.' },
    { group: 'الأساس', name: 'ins-stat-value', desc: 'الرقم نفسه، بخط Inter وأرقام متساوية العرض، وبلون النبرة.' }
  ],
  html: function (s) {
    var cls = 'ins-stat' + (s.glass ? ' ins-glass' : '') + (s.tone ? ' ins-stat--' + s.tone : '') + (s.size ? ' ins-stat--' + s.size : '');
    var tag = s.link ? 'a' : 'div';
    var body = [
      '<span class="ins-stat-label">' + s.label + '</span>',
      '<span class="ins-stat-value">' + s.value + '</span>'
    ];
    if (s.hint) body.push('<span class="ins-stat-hint">هذا الشهر</span>');
    if (s.trend) body.push('<span class="ins-stat-trend"><span class="ins-delta ins-delta--up">12%</span><span class="ins-delta-ref">عن الشهر الماضي</span></span>');
    var inner = body.length > 2 ? '\n    ' + body.join('\n    ') + '\n  ' : body.join('');
    return '<' + tag + ' class="' + cls + '"' + (s.link ? ' href="/files"' : '') + '>\n' +
      '  <span class="ins-stat-ico"><svg class="ico"><use href="#i-file"/></svg></span>\n' +
      '  <span class="ins-stat-body">' + inner + '</span>\n</' + tag + '>';
  },
  notes: function (s) {
    var out = [];
    if (s.tone === 'zero') out.push('اجعل القيمة صفرًا مع `--zero`، فهو للقيمة الصفرية.');
    if (s.link) out.push('مرر المؤشر على البطاقة، فتتحرك أيقونتها.');
    return out;
  }
});

SitePlay.define({
  id: 'delta',
  title: 'التغير',
  owns: 'ins-delta',
  controls: [
    { key: 'dir', label: 'الاتجاه', type: 'seg', value: 'up', choices: [
      { value: 'up', label: 'صعود', name: 'ins-delta--up', desc: 'سهم إلى أعلى باللون الأخضر.' },
      { value: 'down', label: 'هبوط', name: 'ins-delta--down', desc: 'سهم إلى أسفل باللون الأحمر.' },
      { value: 'flat', label: 'ثبات', name: 'ins-delta--flat', desc: 'خط أفقي بلون خافت، لما بقي على حاله.' }
    ] },
    { key: 'invert', label: 'النقص خير', type: 'switch', name: 'ins-delta--invert', desc: 'يعكس اللونين حين يكون النقص خيرًا من الزيادة، كالتكاليف ووقت الانتظار.' },
    { key: 'ref', label: 'ما يقارن به', type: 'switch', value: true, name: 'ins-delta-ref', desc: 'بعد `.ins-delta`، يذكر ما يقارن به الرقم.' },
    { key: 'text', label: 'المقدار', type: 'text', value: '12%' }
  ],
  extra: [
    { group: 'الأساس', first: true, name: 'ins-delta', desc: 'مقدار ما تغير الرقم: يدل سهمه على الاتجاه، ولونه على أن التغير محمود أو مذموم. ويبقى الرقم من اليسار فتبقى الإشارة أمامه.' }
  ],
  disable: function (s) { return s.dir === 'flat' ? ['invert'] : []; },
  html: function (s) {
    var d = '<span class="ins-delta ins-delta--' + s.dir + (s.invert && s.dir !== 'flat' ? ' ins-delta--invert' : '') + '">' + s.text + '</span>';
    return s.ref ? '<span>' + d + '<span class="ins-delta-ref">عن الشهر الماضي</span></span>' : d;
  },
  notes: function (s) {
    return s.invert && s.dir !== 'flat' ? ['الاتجاه على حاله، وانعكس اللون فقط.'] : [];
  }
});
