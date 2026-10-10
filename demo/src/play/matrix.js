SitePlay.define({
  id: 'matrix',
  title: 'مصفوفة الاختيار',
  wide: true,
  owns: 'ins-matrix',
  controls: [
    { key: 'cards', label: 'بطاقات بوصف', type: 'switch', name: 'ins-matrix--cards', desc: 'إذا حملت الخلايا كلامًا: كل خلية بطاقة تحيط بزر الاختيار ووصفه، فالبطاقة كلها هي الاختيار.' },
    { key: 'points', label: 'درجات', type: 'switch', name: 'ins-matrix-points', desc: 'درجة المستوى في أول البطاقة.' },
    { key: 'sum', label: 'المجموع', type: 'switch', name: 'data-ins-sum', desc: 'على أي عنصر، ومعه معرّف المصفوفة `data-ins-sum="#id"`: يعرض مجموع القيم المختارة.' },
    { key: 'deselect', label: 'الإلغاء بنقرة ثانية', type: 'switch', name: 'data-ins-deselect', desc: 'على المصفوفة أو أي عنصر يحويها: إذا نقرت الخيار المختار مرة ثانية أُلغي اختياره، وكذلك مفتاح المسافة.' },
    { key: 'required', label: 'الإجابة لازمة', type: 'switch', name: 'required', desc: 'على أزرار الصف، فلا يُرسل النموذج حتى يُجاب عن كل صف. فقل ذلك للمستخدم بسطر فوق المصفوفة.' }
  ],
  extra: [
    { group: 'الأساس', first: true, name: 'ins-matrix', desc: 'على `<table>`: صفوف من الأسئلة تشترك في إجابات واحدة، لكل صف مجموعة أزرار اختيار تتنقل بينها بالأسهم.' },
    { group: 'الأساس', name: 'aria-label', desc: 'يضعه السكربت على كل زر من اسم صفه وعموده، مثل «الأول: جيد»، إلا إذا وضعته بنفسك.' },
    { group: 'بطاقات بوصف', name: 'ins-matrix-cell', desc: 'البطاقة، على `<label>` يحيط بزر الاختيار ووصفه.' }
  ],
  disable: function (s) { return s.cards ? [] : ['points']; },
  html: function (s) {
    var cols = s.cards ? [['1', 'ضعيف', 'وصف قصير للمستوى.'], ['3', 'جيد', 'وصف قصير للمستوى.'], ['5', 'ممتاز', 'وصف قصير للمستوى.']]
      : [['1', 'ضعيف'], ['2', 'مقبول'], ['3', 'جيد'], ['4', 'ممتاز']];
    var rows = [['p-m1', 'الأول'], ['p-m2', 'الثاني']];
    var req = s.required ? ' required' : '';
    var cell = function (row, c, i) {
      var on = row[0] === 'p-m1' && i === 1 ? ' checked' : '';
      var input = '<input type="radio" name="' + row[0] + '" value="' + c[0] + '"' + req + on + '>';
      if (!s.cards) return '<td><label class="ins-check">' + input + '</label></td>';
      return '<td><label class="ins-matrix-cell">' + input + (s.points ? '<span class="ins-matrix-points">' + c[0] + '</span>' : '') + c[2] + '</label></td>';
    };
    var body = rows.map(function (row) {
      return '        <tr><th scope="row">' + row[1] + '</th>\n' + cols.map(function (c, i) { return '          ' + cell(row, c, i); }).join('\n') + '\n        </tr>';
    }).join('\n');
    var table = '<div class="ins-table-wrap">\n' +
      '  <table class="ins-matrix' + (s.cards ? ' ins-matrix--cards' : '') + '"' + (s.sum ? ' id="p-matrix"' : '') + (s.deselect ? ' data-ins-deselect' : '') + '>\n' +
      '    <thead><tr><th></th>' + cols.map(function (c) { return '<th>' + c[1] + '</th>'; }).join('') + '</tr></thead>\n' +
      '    <tbody>\n' + body + '\n    </tbody>\n' +
      '  </table>\n' +
      '</div>';
    return (s.required ? '<p class="ins-hint">أجب عن كل صف.</p>\n' : '') + table + (s.sum ? '\n<p>المجموع: <b data-ins-sum="#p-matrix"></b></p>' : '');
  },
  notes: function (s) {
    var out = s.sum ? ['اختر في الصفوف، فيتغير المجموع تحت المصفوفة.'] : [];
    if (s.deselect) out.push('انقر الخيار المختار في الصف الأول مرة ثانية فيُلغى.');
    return out;
  }
});
