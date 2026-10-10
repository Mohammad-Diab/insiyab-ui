SitePlay.define({
  id: 'tree',
  title: 'الشجرة',
  owns: 'ins-tree',
  wide: true,
  controls: [
    { key: 'checks', label: 'بمربعات اختيار', group: 'مربعات الاختيار', type: 'switch', name: 'data-ins-tree-checks', desc: 'اجعل تسمية كل بند مربع اختيار `.ins-check`، فإذا حددت فرعًا تحدد كل ما فيه، والفرع المحدد بعضه يظهر في حالة `indeterminate` ولا يُرسل مع النموذج.' },
    { key: 'icons', label: 'بأيقونات', type: 'switch', value: true },
    { key: 'open', label: 'فرع مفتوح', group: 'البند', type: 'switch', value: true, name: 'data-open', desc: 'على `<li>`: فرع مفتوح من البداية، وإلا بقي مغلقًا.' },
    { key: 'link', label: 'رابط', group: 'البند', type: 'switch', name: 'li > a', kind: 'element', desc: 'اجعل تسمية البند رابطًا، فينتقل إليه Enter، ويبقى رابطًا إن غاب السكربت.' },
    { key: 'count', label: 'عدد في آخر الصف', group: 'البند', type: 'switch', name: 'ins-tree-end', desc: 'بعد التسمية: ينتقل إلى آخر الصف، ومثله شارة `.ins-pill` أو `.ins-badge`.' },
    { key: 'none', label: 'بلا اختيار', type: 'switch', level: 'advanced', name: 'data-ins-tree-select="none"', desc: 'تلغي الاختيار، حين تكون الشجرة للتنقل وحده.' },
    { key: 'selected', label: 'بند مختار', group: 'البند', type: 'switch', level: 'advanced', name: 'data-selected', desc: 'على `<li>`: بند مختار من البداية.' }
  ],
  extra: [
    { group: 'الأساس', first: true, name: 'data-ins-tree', desc: 'على `<ul>` فيه قوائم متداخلة يجعلها شجرة: أول عنصر في كل بند تسميته، `<span>` أو رابط، والقائمة التي فيه فرعه.' },
    { group: 'الأساس', name: 'ins-tree', desc: 'هيئة الشجرة، تضعها الإضافة وحدها إن غابت.' },
    { group: 'الأساس', name: 'aria-label', desc: 'اسم الشجرة يقرؤه قارئ الشاشة.' },
    { group: 'الأحداث', name: 'ins:tree', kind: 'event', level: 'advanced', desc: 'يُطلق عند كل تغيير، وفي `detail` البند `item` وتسميته `label` وما جرى `action`: `select` أو `open` أو `close` أو `check`.' }
  ],
  events: ['ins:tree'],
  disable: function (s) {
    var off = [];
    if (s.checks) off.push('icons', 'link', 'selected', 'none');
    else if (s.none) off.push('selected');
    return off;
  },
  html: function (s) {
    var folder = '<svg class="ico" aria-hidden="true"><use href="#i-folder"/></svg>';
    var file = '<svg class="ico" aria-hidden="true"><use href="#i-file"/></svg>';
    var n = 0;
    var label = function (text, ico, href) {
      if (s.checks) return '<label class="ins-check"><input type="checkbox" name="files" value="f' + (++n) + '"' + (text === 'ملف.pdf' ? ' checked' : '') + '><span class="ins-check-text">' + text + '</span></label>';
      var inner = (s.icons ? ico : '') + text;
      return href && s.link ? '<a href="' + href + '">' + inner + '</a>' : '<span>' + inner + '</span>';
    };
    var end = function (k) { return s.count ? '<span class="ins-tree-end">' + k + '</span>' : ''; };
    var tree = s.checks ? ' data-ins-tree-checks' : s.none ? ' data-ins-tree-select="none"' : '';
    var picked = s.selected && !s.checks && !s.none ? ' data-selected' : '';
    return '<ul class="ins-tree" data-ins-tree' + tree + ' aria-label="الملفات">\n' +
      '  <li' + (s.open ? ' data-open' : '') + '>' + label('المستندات', folder) + end(2) + '\n' +
      '    <ul>\n' +
      '      <li>' + label('ملف.pdf', file, '/files/1') + '</li>\n' +
      '      <li>' + label('الصور', folder) + end(2) + '\n' +
      '        <ul>\n' +
      '          <li>' + label('صورة 1.png', file) + '</li>\n' +
      '          <li>' + label('صورة 2.png', file) + '</li>\n' +
      '        </ul>\n' +
      '      </li>\n' +
      '    </ul>\n' +
      '  </li>\n' +
      '  <li' + picked + '>' + label('ملاحظات.txt', file) + '</li>\n' +
      '</ul>';
  },
  notes: function (s) {
    var out = ['انقر بندًا أو افتح فرعًا، فيظهر هنا الحدث.'];
    if (s.link && !s.checks) out.push('صار «ملف.pdf» رابطًا يشبه غيره من البنود، فإذا نقرته أو ضغطت Enter عليه انتقلت إليه.');
    if (s.checks) out.push('حُدد `ملف.pdf` وحده، فظهر فرعه محددًا بعضه.');
    if (!s.checks && s.none) out.push('لا يُختار البند هنا، وتبقى الأسهم تنقلك وتفتح الفروع.');
    return out;
  }
});
