SitePlay.define({
  id: 'chip',
  title: 'الرقاقة',
  owns: 'ins-chip',
  controls: [
    { key: 'kind', label: 'النوع', type: 'seg', value: 'remove', choices: [
      { value: 'remove', label: 'تُزال', name: 'ins-chip-x', desc: 'زر الإزالة داخل الرقاقة، يرسم علامته بنفسه، ولا بد له من `aria-label`.' },
      { value: 'filter', label: 'فلتر', name: 'data-ins-toggle', desc: 'على `button.ins-chip`: فلتر يُشغَّل ويُطفأ، فإذا شُغّل أخذ لون الهوية وظهرت عليه علامة صح.' },
      { value: 'link', label: 'رابط', name: 'a.ins-chip', kind: 'element', desc: 'رقاقة هي رابط، لفلتر ينقلك إلى صفحة مصفّاة.' }
    ] },
    { key: 'on', label: 'الأولى مختارة', type: 'switch', value: true, name: 'aria-pressed="true"', desc: 'الفلتر مشغّل. ضعها بنفسك ليبدأ الفلتر مشغّلًا.' }
  ],
  extra: [
    { group: 'الأساس', first: true, name: 'ins-chip', desc: 'قيمة صغيرة يتصرف فيها المستخدم، على `<span>` أو `<button>` أو `<a>`. أما الشارة فللقراءة وحدها.' },
    { group: 'الأساس', name: 'ins-chips', desc: 'يجمع الرقاقات في سطر يلتف إذا ضاق.' },
    { group: 'النوع', name: 'ins:chip-remove', kind: 'event', desc: 'يُطلق قبل أن تزول الرقاقة، وفي `detail` الرقاقة `el` وقيمتها `value`. فإن ألغيته بقيت.' },
    { group: 'النوع', name: 'ins:toggle', kind: 'event', desc: 'يُطلق عند كل ضغطة على الفلتر، وفي `detail` الرقاقة `el` وحالتها `pressed`.' },
    { group: 'خيارات أخرى', name: 'is-active', kind: 'class', desc: 'مثل `aria-pressed="true"` للرابط: يلون الرقاقة المختارة.' }
  ],
  events: ['ins:chip-remove', 'ins:toggle'],
  disable: function (s) { return s.kind === 'remove' ? ['on'] : []; },
  html: function (s) {
    var items = ['الأول', 'الثاني', 'الثالث'];
    var chips = items.map(function (t, i) {
      var on = s.on && i === 0;
      if (s.kind === 'filter') return '  <button class="ins-chip" data-ins-toggle aria-pressed="' + on + '">' + t + '</button>';
      if (s.kind === 'link') return '  <a class="ins-chip' + (on ? ' is-active' : '') + '" href="?filter=' + (i + 1) + '"' + (on ? ' aria-current="page"' : '') + '>' + t + '</a>';
      return '  <span class="ins-chip">' + t + '<button type="button" class="ins-chip-x" aria-label="إزالة ' + t + '"></button></span>';
    });
    return '<div class="ins-chips">\n' + chips.join('\n') + '\n</div>';
  },
  notes: function (s) {
    if (s.kind === 'remove') return ['أزل رقاقة، فيظهر هنا الحدث، وغيّر أي خيار لتعيدها.'];
    if (s.kind === 'filter') return ['اضغط رقاقة، فيظهر هنا الحدث الذي تطلقه.'];
    return ['الرابط يدل على المختار بـ `is-active`، وتضعه صفحتك بنفسها.'];
  }
});

SitePlay.define({
  id: 'tags',
  title: 'الاختيار المتعدد والوسوم',
  wide: true,
  owns: 'ins-tags',
  controls: [
    { key: 'preset', label: 'قيمة مختارة', type: 'switch', value: true },
    { key: 'list', label: 'من قائمة', type: 'switch', value: true, level: 'advanced', name: 'ins-combo-list', desc: 'قائمة في الحقل تجعله اختيارًا متعددًا يطابق كالإكمال التلقائي، وكلما اخترت خيارًا صار رقاقة وخرج من القائمة. وبلا قائمة يصير ما تكتبه وسمًا إذا ضغطت Enter أو كتبت فاصلة.' },
    { key: 'free', label: 'كتابة حرة مع القائمة', type: 'switch', level: 'advanced', name: 'data-ins-free', desc: 'يقبل ما تكتبه وسمًا ولو لم يكن في القائمة.' },
    { key: 'max', label: 'حد أعلى', type: 'seg', value: '', level: 'advanced', choices: [
      { value: '', label: 'بلا' },
      { value: '3', label: '3', name: 'data-ins-max', desc: 'أكثر عدد من القيم يقبله الحقل.' },
      { value: '5', label: '5' }
    ] }
  ],
  extra: [
    { group: 'الأساس', first: true, name: 'ins-tags', desc: 'يجمع قيمًا عدة في حقل واحد: رقاقات، وبعدها مربع الكتابة. وإذا ضغطت Backspace والمربع فارغ محوت آخر رقاقة.' },
    { group: 'الأساس', name: 'ins-tags-input', desc: 'مربع الكتابة بعد الرقاقات.' },
    { group: 'الأساس', name: 'data-ins-name', desc: 'تحمل كل رقاقة حقلًا مخفيًا بهذا الاسم، فيرسل النموذج القيم كلها.' },
    { group: 'الأساس', name: 'ins:tags', kind: 'event', level: 'advanced', desc: 'يُطلق مع كل إضافة أو إزالة، وفي `detail` القيم كلها `values` وما أُضيف `added` أو أُزيل `removed`.' }
  ],
  events: ['ins:tags'],
  disable: function (s) { return s.list ? [] : ['free']; },
  html: function (s) {
    var list = s.list ? '    <ul class="ins-combo-list">\n' +
      [['a', 'الأول'], ['b', 'الثاني'], ['c', 'الثالث'], ['d', 'الرابع'], ['e', 'الخامس']].map(function (o) {
        return '      <li class="ins-combo-option" data-value="' + o[0] + '">' + o[1] + '</li>';
      }).join('\n') + '\n      <li class="ins-combo-empty">لا نتائج</li>\n    </ul>\n' : '';
    var first = s.list ? ['الأول', 'a'] : ['وسم', 'وسم'];
    var chip = s.preset ? '    <span class="ins-chip">' + first[0] + '<button type="button" class="ins-chip-x" aria-label="إزالة ' + first[0] + '"></button><input type="hidden" name="items" value="' + first[1] + '"></span>\n' : '';
    return '<div class="ins-field">\n' +
      '  <label class="ins-label" for="p-tags">' + (s.list ? 'العناصر' : 'الوسوم') + '</label>\n' +
      '  <div class="ins-tags" data-ins-name="items"' + (s.free && s.list ? ' data-ins-free' : '') + (s.max ? ' data-ins-max="' + s.max + '"' : '') + '>\n' +
      chip +
      '    <input class="ins-tags-input" id="p-tags" placeholder="' + (s.list ? 'اختر' : 'اكتب واضغط Enter') + '">\n' +
      list +
      '  </div>\n' +
      '</div>';
  },
  stage: function (view, s) {
    var tags = view.querySelector('.ins-tags'), input = tags.querySelector('.ins-tags-input');
    var have = function () { return tags.querySelectorAll('.ins-chip').length; };
    var add = function (label, value) {
      if (tags.querySelector('input[type="hidden"][value="' + value + '"]')) return;
      input.insertAdjacentHTML('beforebegin', '<span class="ins-chip">' + label + '<button type="button" class="ins-chip-x" aria-label="إزالة ' + label + '"></button><input type="hidden" name="items" value="' + value + '"></span>');
      var opt = tags.querySelector('.ins-combo-option[data-value="' + value + '"]');
      if (opt) { opt.setAttribute('data-ins-chosen', ''); opt.setAttribute('aria-selected', 'true'); }
    };
    if (s.free && s.list) add('قيمة حرة', 'قيمة حرة');
    var pool = s.list ? [['الأول', 'a'], ['الثاني', 'b'], ['الثالث', 'c'], ['الرابع', 'd'], ['الخامس', 'e']]
      : [['وسم', 'وسم'], ['وسم آخر', 'وسم آخر'], ['وسم ثالث', 'وسم ثالث'], ['وسم رابع', 'وسم رابع'], ['وسم خامس', 'وسم خامس']];
    for (var i = 0; s.max && have() < +s.max && i < pool.length; i++) add(pool[i][0], pool[i][1]);
  },
  notes: function (s) {
    var out = [s.list ? 'اكتب في الحقل أو انقره، فتظهر القائمة، وما تختاره يصير رقاقة.' : 'اكتب كلمة واضغط Enter، فتصير وسمًا.'];
    if (s.free && s.list) out.push('«قيمة حرة» ليست في القائمة، وقبلها الحقل لأنه يقبل الكتابة الحرة.');
    if (s.max) out.push('بلغ الحقل حده، فلا يقبل قيمة أخرى حتى تزيل واحدة.');
    return out;
  }
});
