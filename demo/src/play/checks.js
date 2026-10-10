SitePlay.define({
  id: 'check',
  title: 'مربع الاختيار وزر الاختيار',
  owns: 'ins-check',
  controls: [
    { key: 'type', label: 'النوع', type: 'seg', value: 'checkbox', choices: [
      { value: 'checkbox', label: 'مربع' },
      { value: 'radio', label: 'دائرة' }
    ] },
    { key: 'text', label: 'النص', type: 'text', value: 'الأول' },
    { key: 'checked', label: 'محدد', type: 'switch', value: true },
    { key: 'disabled', label: 'معطل', type: 'switch', name: 'disabled', desc: 'يعطل الخيار ويبهت لونه، فلا يُنقر.' },
    { key: 'deselect', label: 'الإلغاء بنقرة ثانية', type: 'switch', name: 'data-ins-deselect', desc: 'لأزرار الاختيار، على الزر أو أي عنصر يحويه، كالمجموعة أو النموذج أو `<html>` للصفحة كلها: إذا نقرت الزر المختار مرة ثانية أُلغي اختياره، وكذلك مفتاح المسافة.' },
    { key: 'mixed', label: 'بعضها محدد', type: 'switch', level: 'advanced', name: 'data-ins-indeterminate', desc: 'حالة «بعضها محدد» لمربع يحدد الكل. لا سمة لها في HTML، فتضبطها المكتبة في الخاصية `indeterminate`، وتحدّثها صفحتك بعد ذلك.' }
  ],
  extra: [
    { group: 'الأساس', first: true, name: 'ins-check', desc: 'على `<label>` يحيط بـ `<input type="checkbox">` أو `type="radio"`، فإذا نقرت النص تبدلت حالته.' },
    { group: 'الأساس', name: 'ins-check-text', desc: 'نص الخيار بعد المربع.' }
  ],
  disable: function (s) { return s.type === 'radio' ? ['mixed'] : ['deselect']; },
  html: function (s) {
    var box = function (text, on, name) {
      return '<label class="ins-check"><input type="' + s.type + '"' + name + (on ? ' checked' : '') + (s.disabled ? ' disabled' : '') +
        (s.mixed && s.type === 'checkbox' ? ' data-ins-indeterminate' : '') + '><span class="ins-check-text">' + text + '</span></label>';
    };
    if (s.type === 'checkbox') return box(s.text, s.checked, '');
    return '<div class="ins-stack" role="radiogroup" aria-label="الخيارات"' + (s.deselect ? ' data-ins-deselect' : '') + '>\n  ' +
      box(s.text, s.checked, ' name="p-radio"') + '\n  ' + box('الثاني', false, ' name="p-radio"') + '\n</div>';
  },
  notes: function (s) {
    var out = [];
    if (s.type === 'radio') out.push('زر الاختيار لا يأتي وحده، فالأزرار التي تشترك في `name` مجموعة واحدة لا يُختار منها إلا واحد.');
    if (s.type === 'radio' && s.deselect) out.push('انقر الزر المختار مرة ثانية فيُلغى، ولا يبقى في المجموعة اختيار.');
    if (s.mixed && s.type === 'checkbox') out.push('نقرة على المربع تنهي حالة «بعضها محدد»، وغيّر أي خيار لتعيدها.');
    return out;
  }
});

SitePlay.define({
  id: 'switch',
  title: 'المفتاح',
  wide: true,
  owns: 'ins-switch',
  controls: [
    { key: 'label', label: 'التسمية', type: 'text', value: 'الإشعارات' },
    { key: 'checked', label: 'مفعّل', type: 'switch', value: true },
    { key: 'desc', label: 'وصف', type: 'switch', name: 'ins-switch-desc', desc: 'سطر وصف تحت التسمية، إن شئت.' },
    { key: 'grid', label: 'في شبكة', type: 'switch', name: 'ins-switch-grid', desc: 'يرتب المفاتيح في شبكة من البطاقات، تليق بصفحة الإعدادات.' },
    { key: 'card', label: 'في بطاقة', type: 'switch', name: 'ins-switch--card', desc: 'يضع المفتاح الواحد في إطار، لإعداد هو سطر وحده.' }
  ],
  extra: [
    { group: 'الأساس', first: true, name: 'ins-switch', desc: 'لكل إعداد له حالتان، على `<label>` يحيط بـ `<input type="checkbox">` حقيقي.' },
    { group: 'الأساس', name: 'ins-switch-track', desc: 'المسار الذي يتحرك فيه المقبض، بعد الحقل مباشرة.' },
    { group: 'الأساس', name: 'ins-switch-text', desc: 'يضم التسمية والوصف.' },
    { group: 'الأساس', name: 'ins-switch-label', desc: 'تسمية المفتاح.' }
  ],
  disable: function (s) { return s.grid ? ['card'] : []; },
  html: function (s) {
    var one = function (label, on, card, pad) {
      var text = '<span class="ins-switch-label">' + label + '</span>' + (s.desc ? '<span class="ins-switch-desc">وصف قصير</span>' : '');
      return pad + '<label class="ins-switch' + (card ? ' ins-switch--card' : '') + '">\n' +
        pad + '  <input type="checkbox"' + (on ? ' checked' : '') + '><span class="ins-switch-track"></span>\n' +
        pad + '  <span class="ins-switch-text">' + text + '</span>\n' +
        pad + '</label>';
    };
    if (!s.grid) return one(s.label, s.checked, s.card, '');
    return '<div class="ins-switch-grid">\n' + one(s.label, s.checked, false, '  ') + '\n' + one('التقرير اليومي', false, false, '  ') + '\n</div>';
  },
  notes: function (s) {
    return s.grid ? ['المفاتيح في الشبكة بطاقات من تلقاء نفسها، فلا حاجة فيها إلى `ins-switch--card`.'] : [];
  }
});

SitePlay.define({
  id: 'tile',
  title: 'بطاقات الاختيار',
  wide: true,
  owns: 'ins-tile',
  controls: [
    { key: 'type', label: 'النوع', type: 'seg', value: 'radio', choices: [
      { value: 'radio', label: 'واحد منها' },
      { value: 'checkbox', label: 'أكثر من واحد' }
    ] },
    { key: 'desc', label: 'وصف', type: 'switch', value: true, name: 'ins-tile-desc', desc: 'سطر وصف تحت العنوان.' },
    { key: 'disabled', label: 'بطاقة معطلة', type: 'switch', name: 'disabled', desc: 'على حقل البطاقة، فتبهت ولا تُختار.' }
  ],
  extra: [
    { group: 'الأساس', first: true, name: 'ins-tile', desc: 'على `<label>` يحيط بزر اختيار أو مربع اختيار حقيقي، لخيار يحتاج إلى أكثر من سطر. وما البطاقة إلا مظهر لحالة `:checked`.' },
    { group: 'الأساس', name: 'ins-tile-title', desc: 'عنوان البطاقة.' },
    { group: 'الأساس', name: 'ins-tile-grid', desc: 'يرتب البطاقات في شبكة.' }
  ],
  html: function (s) {
    var name = s.type === 'radio' ? ' name="p-tile"' : '';
    var tile = function (value, title, on, off) {
      return '  <label class="ins-tile"><input type="' + s.type + '"' + name + ' value="' + value + '"' + (on ? ' checked' : '') + (off ? ' disabled' : '') + '>' +
        '<span class="ins-tile-title">' + title + '</span>' + (s.desc ? '<span class="ins-tile-desc">وصف قصير</span>' : '') + '</label>';
    };
    return '<div class="ins-tile-grid" role="' + (s.type === 'radio' ? 'radiogroup' : 'group') + '" aria-label="الخيارات">\n' +
      tile('a', 'الأول', false, false) + '\n' + tile('b', 'الثاني', true, false) + '\n' + tile('c', 'الثالث', false, s.disabled) + '\n</div>';
  }
});

SitePlay.define({
  id: 'seg',
  title: 'الشريط المقسم',
  owns: 'ins-seg',
  controls: [
    { key: 'icons', label: 'بأيقونات', type: 'switch' },
    { key: 'link', label: 'روابط', type: 'switch', name: '.ins-seg > a', kind: 'element', desc: 'اجعل الأجزاء روابط `<a href>` حين ينقل كل جزء إلى صفحته، فلا تحرك المكتبة الاختيار قبل الانتقال.' }
  ],
  extra: [
    { group: 'الأساس', first: true, name: 'ins-seg', desc: 'لبديل واحد من بدائل قليلة قصيرة، ومعها `role="group"` و`aria-label`.' },
    { group: 'الأساس', name: 'is-active', kind: 'class', desc: 'على الجزء المختار، وتنقله المكتبة إلى ما تنقره.' },
    { group: 'الأساس', name: 'data-ins-value', desc: 'قيمة الجزء في الحدث، وإن غابت أُخذ نصه.' },
    { group: 'الأساس', name: 'ins:seg', kind: 'event', desc: 'يُطلق عند كل اختيار، وفي `detail` القيمة `value` والجزء نفسه `el`.' }
  ],
  events: ['ins:seg'],
  html: function (s) {
    var parts = [['day', 'اليوم', 'calendar'], ['week', 'الأسبوع', 'list'], ['month', 'الشهر', 'grid']];
    var tag = s.link ? 'a' : 'button';
    var items = parts.map(function (p, i) {
      var attrs = (i === 0 ? ' class="is-active"' : '') + (s.link ? ' href="?view=' + p[0] + '"' + (i === 0 ? ' aria-current="page"' : '') : ' data-ins-value="' + p[0] + '"');
      return '  <' + tag + attrs + '>' + (s.icons ? '<svg class="ico"><use href="#i-' + p[2] + '"/></svg>' : '') + p[1] + '</' + tag + '>';
    });
    return '<div class="ins-seg" role="group" aria-label="الفترة">\n' + items.join('\n') + '\n</div>';
  },
  notes: function (s) {
    return s.link ? ['الروابط تنقل إلى صفحاتها، فالمختار منها تضعه صفحتك بنفسها، ولا حدث.'] : ['اختر جزءًا، فيظهر هنا الحدث الذي يطلقه.'];
  }
});
