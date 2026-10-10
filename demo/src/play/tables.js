SitePlay.define({
  id: 'table',
  title: 'الجدول',
  owns: 'ins-table',
  wide: true,
  controls: [
    { key: 'empty', label: 'فارغ', type: 'switch', name: 'ins-empty', desc: 'حالة فارغة في خلية تمتد بعرض الجدول، فيختفي رأسه عن العين ويبقى `<thead>` لقارئ الشاشة.' },
    { key: 'num', label: 'عمود أرقام', type: 'switch', name: 'ins-num', desc: 'على خلايا العمود ورأسه: خط Inter بأرقام متساوية العرض، فتصطف القيم وتبقى إشارة السالب في مكانها.' },
    { key: 'current', label: 'الصف المحدد', group: 'الصف المحدد', type: 'select', value: '', choices: [
      { value: '', label: 'بلا' },
      { value: 'selected', label: 'aria-selected', name: 'aria-selected="true"', desc: 'صف اخترته من القائمة: يتلون، ويظهر على حافته في جهة البداية شريط صغير.' },
      { value: 'current', label: 'aria-current', name: 'aria-current', desc: 'السجل الذي تعرضه الصفحة، بالهيئة نفسها. أما `aria-current="false"` فلا يحدده.' },
      { value: 'cls', label: 'is-selected', name: 'is-selected', kind: 'class', desc: 'مثلهما، إذا أردت أن تحدد الصف بكلاس.' }
    ] },
    { key: 'panel', label: 'في لوح', type: 'switch', name: 'ins-panel-body--flush', desc: 'ضع الغلاف في `.ins-panel-body--flush` داخل لوح، فيمتد الجدول إلى حواف اللوح وتهدأ خلفية صفوفه.' },
    { key: 'actions', label: 'إجراءات الصف', type: 'switch', name: 'details.ins-pop', kind: 'element', desc: 'قائمة منسدلة في آخر خلية من الصف. والغلاف يقص ما يخرج عنه، إلا القائمة المفتوحة فيتركها تظهر كاملة.' }
  ],
  extra: [
    { group: 'الأساس', first: true, name: 'ins-table', desc: 'على `<table>`: رأس عائم فوق بطاقة من الصفوف، ويتلون الصف الذي تحت المؤشر.' }
  ],
  disable: function (s) { return s.empty ? ['num', 'current', 'actions'] : []; },
  html: function (s) {
    var rows = [['ليلى', 'الرياض', '249.00'], ['عمر', 'عمّان', '1,180.00'], ['سارة', 'القاهرة', '-89.50']];
    var full = !s.empty, num = full && s.num ? ' class="ins-num"' : '', act = full && s.actions;
    var mark = full ? { selected: ' aria-selected="true"', current: ' aria-current="true"', cls: ' class="is-selected"' }[s.current] || '' : '';
    var menu = '\n      <td>\n        <details class="ins-pop">\n          <summary class="ins-btn ins-btn--secondary ins-btn--sm">إجراءات</summary>\n' +
      '          <div class="ins-pop-body">\n            <button class="ins-pop-item">تعديل</button>\n            <button class="ins-pop-item ins-pop-item--bad">حذف</button>\n          </div>\n        </details>\n      </td>\n    ';
    var body = s.empty
      ? '    <tr>\n      <td colspan="3">\n        <div class="ins-empty">\n          <span class="ins-empty-title">لا بيانات</span>\n' +
        '          <span class="ins-empty-hint">ستظهر الصفوف هنا.</span>\n        </div>\n      </td>\n    </tr>'
      : rows.map(function (r, i) {
        var cells = '<td>' + r[0] + '</td><td>' + r[1] + '</td><td' + num + '>' + r[2] + '</td>';
        return '    <tr' + (i === 1 ? mark : '') + '>' + (act ? '\n      ' + cells + menu : cells) + '</tr>';
      }).join('\n');
    var out = '<div class="ins-table-wrap">\n  <table class="ins-table">\n    <thead>\n      <tr><th>الاسم</th><th>المدينة</th><th' + num + '>القيمة</th>' + (act ? '<th></th>' : '') + '</tr>\n    </thead>\n' +
      '    <tbody>\n' + body.replace(/^/gm, '  ') + '\n    </tbody>\n  </table>\n</div>';
    if (!s.panel) return out;
    return '<div class="ins-panel">\n  <div class="ins-panel-body ins-panel-body--flush">\n' + out.replace(/^/gm, '    ') + '\n  </div>\n</div>';
  },
  notes: function (s) {
    var out = [];
    if (s.empty) out.push('رأس الجدول مخفي عن العين، وباقٍ في الشيفرة لقارئ الشاشة.');
    else if (s.actions) out.push('افتح قائمة صف، فيتركها الغلاف تظهر كاملة ما دامت مفتوحة.');
    return out;
  }
});

SitePlay.define({
  id: 'wrap',
  title: 'غلاف الجدول',
  owns: 'ins-table-wrap',
  wide: true,
  controls: [
    { key: 'sticky', label: 'تثبيت الرأس', type: 'switch', name: 'ins-table-wrap--sticky', desc: 'يضع الجدول الطويل في صندوق يتمرر ضمن ارتفاع تحدده `data-ins-height`، وهو 32rem إن لم تذكره، ويبقي رأسه ظاهرًا.' },
    { key: 'pin', label: 'تثبيت العمود الأول', type: 'switch', name: 'ins-table-wrap--pin', desc: 'يبقي العمود الأول ظاهرًا حين يتمرر الجدول العريض أفقيًا. ولك أن تجمعه مع `--sticky`.' }
  ],
  extra: [
    { group: 'الأساس', first: true, name: 'ins-table-wrap', desc: 'يغلف الجدول، فإذا ضاق عنه تمرر أفقيًا وبقيت الصفحة على عرضها.' }
  ],
  html: function (s) {
    var rows = [
      ['ليلى', 'الرياض', 'layla@example.com', '+966 50 000 0001', 'نشط'],
      ['عمر', 'عمّان', 'omar@example.com', '+962 79 000 0002', 'نشط'],
      ['سارة', 'القاهرة', 'sara@example.com', '+20 10 0000 0003', 'موقوف'],
      ['كريم', 'دبي', 'karim@example.com', '+971 50 000 0004', 'نشط'],
      ['نور', 'الدوحة', 'nour@example.com', '+974 5000 0005', 'نشط']
    ];
    var cls = 'ins-table-wrap' + (s.sticky ? ' ins-table-wrap--sticky' : '') + (s.pin ? ' ins-table-wrap--pin' : '');
    return '<div class="' + cls + '"' + (s.sticky ? ' data-ins-height="16rem"' : '') + '>\n  <table class="ins-table" style="min-inline-size: 48rem">\n' +
      '    <thead><tr><th>الاسم</th><th>المدينة</th><th>البريد</th><th>الهاتف</th><th>الحالة</th></tr></thead>\n    <tbody>\n' +
      rows.map(function (r) {
        return '      <tr><td>' + r[0] + '</td><td>' + r[1] + '</td><td dir="ltr">' + r[2] + '</td><td dir="ltr">' + r[3] + '</td><td>' + r[4] + '</td></tr>';
      }).join('\n') + '\n    </tbody>\n  </table>\n</div>';
  },
  notes: function (s) {
    if (s.sticky && s.pin) return ['مرر الجدول عموديًا وأفقيًا، فيبقى رأسه وعموده الأول ظاهرين.'];
    if (s.sticky) return ['مرر الجدول داخل صندوقه، وقد قصّرناه بـ`data-ins-height`، فيبقى رأسه ظاهرًا.'];
    if (s.pin) return ['مرر الجدول أفقيًا، فيبقى العمود الأول في مكانه.'];
    return ['الجدول هنا أعرض من غلافه بـ`min-inline-size`، فمرره أفقيًا.'];
  }
});

SitePlay.define({
  id: 'sort',
  title: 'الترتيب',
  owns: 'ins-sort',
  wide: true,
  controls: [
    { key: 'value', label: 'قيمة للترتيب', group: 'قيمة الخلية', type: 'switch', name: 'data-sort-value', desc: 'على الخلية إذا لم يكن نصها هو قيمتها، كتاريخ مكتوب بالكلمات، فيرتبها بهذه القيمة.' },
    { key: 'start', label: 'مرتب من البداية', type: 'seg', value: '', choices: [
      { value: '', label: 'لا' },
      { value: 'ascending', label: 'تصاعديًا', name: 'aria-sort="ascending"', desc: 'على رأس العمود إذا جاءت الصفوف من الخادم مرتبة به تصاعديًا، فيظهر السهم في مكانه.' },
      { value: 'descending', label: 'تنازليًا', name: 'aria-sort="descending"', desc: 'مثلها، للصفوف المرتبة تنازليًا.' }
    ] }
  ],
  extra: [
    { group: 'الأساس', first: true, name: 'data-ins-sort', desc: 'على رأس العمود: يصير زرًا يرتب الصفوف تصاعديًا ثم تنازليًا، ويدل السهم على الاتجاه. والخلية الفارغة تأتي في الآخر دائمًا.' },
    { group: 'الأساس', name: 'ins-sort', desc: 'الزر الذي تضعه المكتبة في رأس العمود، فلا تكتبه بنفسك.' },
    { group: 'نوع القيم', name: 'data-ins-sort="number"', desc: 'يرتب العمود بالأرقام في كل حال. ومن دونها يرتبه بالأرقام إذا كانت خلاياه كلها أرقامًا، وبالنص على لغة الصفحة فيما سوى ذلك.' },
    { group: 'نوع القيم', name: 'data-ins-sort="text"', desc: 'يرتب العمود نصًا ولو كانت خلاياه أرقامًا.' },
    { group: 'قيمة الخلية', name: 'data-value', desc: 'يرتب بها إذا لم تجد `data-sort-value`، ويبدأ بها التعديل في مكانه أيضًا.' },
    { group: 'الحدث', name: 'ins:sort', kind: 'event', desc: 'يُطلق قبل الترتيب، وفي `detail` الجدول `el` ورأس العمود `th` ورقمه `column` والاتجاه `dir`. ألغه إذا كان الترتيب على الخادم، فيتحرك السهم وحده.' }
  ],
  events: ['ins:sort'],
  html: function (s) {
    var rows = [['عمر', '2026-01-15', '15 يناير', '1,180.00'], ['ليلى', '2026-03-02', '2 مارس', '249.00'], ['سارة', '2026-02-20', '20 فبراير', '89.50']];
    if (s.start) {
      var order = ['سارة', 'عمر', 'ليلى'];
      rows.sort(function (a, b) { return order.indexOf(a[0]) - order.indexOf(b[0]); });
      if (s.start === 'descending') rows.reverse();
    }
    return '<div class="ins-table-wrap">\n  <table class="ins-table">\n    <thead>\n' +
      '      <tr><th data-ins-sort' + (s.start ? ' aria-sort="' + s.start + '"' : '') + '>الاسم</th><th data-ins-sort>التاريخ</th><th data-ins-sort class="ins-num">القيمة</th></tr>\n' +
      '    </thead>\n    <tbody>\n' +
      rows.map(function (r) {
        return '      <tr><td>' + r[0] + '</td><td' + (s.value ? ' data-sort-value="' + r[1] + '"' : '') + '>' + r[2] + '</td><td class="ins-num">' + r[3] + '</td></tr>';
      }).join('\n') + '\n    </tbody>\n  </table>\n</div>';
  },
  notes: function (s) {
    return [s.value
      ? 'رتب عمود التاريخ، فيرتبه بالقيمة التي في `data-sort-value`، ويظهر هنا الحدث.'
      : 'رتب عمود التاريخ، فيرتبه بنصه فيأتي 2 مارس قبل 15 يناير، ثم شغّل «قيمة للترتيب» وقارن.'];
  }
});

SitePlay.define({
  id: 'select',
  title: 'تحديد الصفوف',
  owns: 'ins-bulkbar',
  wide: true,
  controls: [
    { key: 'all', label: 'مربع للكل', type: 'switch', value: true, name: 'data-ins-select="all"', desc: 'على مربع رأس الجدول: يحدد الصفوف كلها أو يلغي تحديدها، ويصير محددًا كله أو جزئيًا بحسبها.' },
    { key: 'checked', label: 'صف محدد من البداية', type: 'switch', value: true, name: 'checked', desc: 'على مربع صف أرسله الخادم محددًا، فيتلون صفه ويظهر الشريط من البداية.' },
    { key: 'bulk', label: 'شريط الإجراءات', group: 'شريط الإجراءات', type: 'switch', value: true, name: 'ins-bulkbar', desc: 'شريط يظهر ما دام في الجدول صف محدد، فيه عدد المحدد والإجراءات المتاحة له، ويلتصق بأسفل الشاشة في القائمة الطويلة.' },
    { key: 'text', label: 'صيغة العدد', group: 'شريط الإجراءات', type: 'switch', name: 'data-ins-text', desc: 'على `.ins-bulkbar-count`: صيغة للعدد غير الافتراضية، و`{n}` مكان العدد.' }
  ],
  extra: [
    { group: 'الأساس', first: true, name: 'ins-table-check', desc: 'على خلايا عمود المربعات، فيضيق العمود على قدر المربع.' },
    { group: 'الأساس', name: 'data-ins-select', desc: 'على مربع كل صف: يتلون الصف إذا حددته، وإذا نقرت مع `Shift` حددت ما بين صفين.' },
    { group: 'شريط الإجراءات', name: 'data-ins-bulk', desc: 'على الشريط، يربطه بجدوله: `data-ins-bulk="#people"`.' },
    { group: 'شريط الإجراءات', name: 'ins-bulkbar-count', desc: 'تكتب فيه المكتبة عدد الصفوف المحددة.' },
    { group: 'شريط الإجراءات', name: 'data-ins-select="none"', desc: 'على زر في الشريط يلغي التحديد كله.' },
    { group: 'الحدث', name: 'ins:select', kind: 'event', desc: 'يُطلق مع كل تغيير في التحديد، وفي `detail` الجدول `el` والصفوف المحددة `rows` وعددها `count`.' }
  ],
  disable: function (s) { return s.bulk ? [] : ['text']; },
  html: function (s) {
    var box = function (name, on) {
      return '<td class="ins-table-check"><label class="ins-check"><input type="checkbox" data-ins-select aria-label="' + name + '"' + (on ? ' checked' : '') + '></label></td>';
    };
    var head = s.all ? '<th class="ins-table-check"><label class="ins-check"><input type="checkbox" data-ins-select="all" aria-label="تحديد الكل"></label></th>' : '<th class="ins-table-check"></th>';
    var out = '<div class="ins-table-wrap">\n  <table class="ins-table" id="people">\n    <thead>\n      <tr>\n        ' + head + '\n        <th>الاسم</th><th>المدينة</th>\n      </tr>\n    </thead>\n    <tbody>\n' +
      [['ليلى', 'الرياض'], ['عمر', 'عمّان'], ['سارة', 'القاهرة']].map(function (r, i) {
        return '      <tr>' + box(r[0], s.checked && i === 0) + '<td>' + r[0] + '</td><td>' + r[1] + '</td></tr>';
      }).join('\n') + '\n    </tbody>\n  </table>\n</div>';
    if (!s.bulk) return out;
    return out + '\n<div class="ins-bulkbar" data-ins-bulk="#people" hidden>\n' +
      '  <span class="ins-bulkbar-count"' + (s.text ? ' data-ins-text="الصفوف المحددة: {n}"' : '') + '></span>\n' +
      '  <button class="ins-btn ins-btn--secondary ins-btn--sm">تصدير</button>\n' +
      '  <button class="ins-btn ins-btn--danger ins-btn--sm">حذف</button>\n' +
      '  <button class="ins-btn ins-btn--bare ins-btn--sm" data-ins-select="none">إلغاء</button>\n</div>';
  },
  notes: function (s) {
    return s.bulk ? ['حدد الصفوف أو ألغ تحديدها، فيظهر الشريط ويختفي معها. وتكتب المكتبة العدد بنفسها.'] : [];
  }
});

SitePlay.define({
  id: 'edit',
  title: 'التعديل في مكانه',
  wide: true,
  controls: [
    { key: 'number', label: 'عمود أرقام', type: 'switch', name: 'data-ins-edit="number"', desc: 'حقل للأرقام، يُكتب من اليسار وتظهر له على الهاتف لوحة الأرقام.' },
    { key: 'select', label: 'اختيار من قائمة', type: 'switch', name: 'data-ins-edit="select"', desc: 'قائمة تختار منها بدل الحقل، ومعها خياراتها في `data-ins-options`.' }
  ],
  extra: [
    { group: 'الأساس', first: true, name: 'data-ins-edit', desc: 'على الخلية: ينقرها المستخدم أو يضغط عليها `Enter` فتصير حقلًا، ثم يحفظ `Enter` أو مغادرة الحقل ما كتب، ويعيد `Esc` القيمة السابقة.' },
    { group: 'خيارات أخرى', name: 'data-ins-options', desc: 'خيارات القائمة، بينها `|`: `data-ins-options="نشط|موقوف"`.' },
    { group: 'خيارات أخرى', name: 'data-value', desc: 'قيمة الخلية إذا اختلفت عن نصها، فيبدأ بها الحقل وتتحدث بعد الحفظ.' },
    { group: 'الحدث', name: 'ins:edit', kind: 'event', desc: 'يُطلق قبل حفظ التغيير، وفي `detail` الخلية `el` والصف `row` والقيمة الجديدة `value` والسابقة `old`، و`revert()` تعيد السابقة. ألغه لترفض القيمة.' }
  ],
  html: function (s) {
    var rows = [['ليلى', '12', 'نشط'], ['عمر', '7', 'موقوف']];
    return '<div class="ins-table-wrap">\n  <table class="ins-table">\n    <thead><tr><th>الاسم</th><th class="ins-num">الكمية</th><th>الحالة</th></tr></thead>\n    <tbody>\n' +
      rows.map(function (r) {
        return '      <tr><td data-ins-edit>' + r[0] + '</td><td class="ins-num"' + (s.number ? ' data-ins-edit="number"' : '') + '>' + r[1] + '</td>' +
          '<td' + (s.select ? ' data-ins-edit="select" data-ins-options="نشط|موقوف"' : '') + '>' + r[2] + '</td></tr>';
      }).join('\n') + '\n    </tbody>\n  </table>\n</div>';
  },
  notes: function () {
    return ['انقر خلية تقبل التعديل، أو اضغط عليها `Enter`، ثم احفظ بـ`Enter` أو تراجع بـ`Esc`.'];
  }
});
