SitePlay.define({
  id: 'pop',
  title: 'القائمة المنسدلة',
  owns: 'ins-pop',
  controls: [
    { key: 'align', label: 'المحاذاة', type: 'seg', value: '', choices: [
      { value: '', label: 'من البداية' },
      { value: 'end', label: 'من النهاية', name: 'ins-pop--end', desc: 'تحاذي القائمة زرها من نهايته، لزر في آخر الشريط. وتنقلب وحدها إذا كادت تخرج من الشاشة.' }
    ] },
    { key: 'body', label: 'المحتوى', type: 'seg', value: '', choices: [
      { value: '', label: 'عناصر' },
      { value: 'card', label: 'بطاقة', name: 'ins-pop-body--card', desc: 'تضع فيها ما شئت من محتوى بدل العناصر، ويبقى لها سلوك القائمة كله.' }
    ] },
    { key: 'label', label: 'عنوان مجموعة', group: 'العناصر', type: 'switch', name: 'ins-pop-label', desc: 'عنوان صغير فوق مجموعة من العناصر.' },
    { key: 'check', label: 'قابلة للتحديد', group: 'عناصر قابلة للتحديد', type: 'seg', level: 'advanced', value: '', choices: [
      { value: '', label: 'لا' },
      { value: 'checkbox', label: 'تحديد متعدد', name: 'role="menuitemcheckbox"', desc: 'عنصر يُحدد ويُلغى تحديده، وتبقى القائمة مفتوحة ما دمت تحدد فيها.' },
      { value: 'radio', label: 'اختيار واحد', name: 'role="menuitemradio"', desc: 'لا يُحدد إلا عنصر واحد في مجموعته `role="group"`، أو في القائمة كلها إن لم تكن له مجموعة.' }
    ] },
    { key: 'kbd', label: 'اختصار', group: 'العناصر', type: 'switch', name: 'kbd.ins-kbd', kind: 'element', desc: 'ضع `<kbd>` داخل العنصر فيظهر اختصاره في آخره.' },
    { key: 'disabled', label: 'عنصر معطل', group: 'العناصر', type: 'switch', name: 'disabled', desc: 'يعطل العنصر فتتخطاه الأسهم، وعلى الرابط `aria-disabled="true"`.' },
    { key: 'sep', label: 'فاصل', group: 'العناصر', type: 'switch', name: 'ins-pop-sep', desc: 'خط يفصل بين مجموعتين، على `<hr>`.' },
    { key: 'bad', label: 'إجراء خطير', group: 'العناصر', type: 'switch', name: 'ins-pop-item--bad', desc: 'لعنصر يحذف أو لا رجعة فيه.' },
    { key: 'links', label: 'روابط', group: 'العناصر', type: 'switch', name: 'a.ins-pop-item', kind: 'element', desc: 'يصلح `.ins-pop-item` على `<a>` كما يصلح على `<button>`.' },
    { key: 'split', label: 'زر مقسوم', group: 'الزر المقسوم', type: 'switch', level: 'advanced', name: '.ins-btn-group > .ins-pop', kind: 'element', desc: 'ضع القائمة داخل `.ins-btn-group` بجانب زر، فيكون الإجراء الأساسي بضغطة واحدة، وتبقى بدائله في القائمة.' }
  ],
  extra: [
    { group: 'الأساس', first: true, name: 'ins-pop', desc: 'على `<details>`، وعنصر `<summary>` فيها هو الزر، فتنفتح دون جافاسكربت. ويزيدها السكربت الإغلاق بـ `Esc` وبالنقر خارجها، والتنقل بالأسهم.' },
    { group: 'الأساس', name: 'ins-pop-body', desc: 'القائمة نفسها، تحت الزر.' },
    { group: 'الأساس', name: 'ins-pop-item', desc: 'عنصر القائمة، يغلقها إذا اخترته ويعيد التركيز إلى زرها.' },
    { group: 'المحاذاة', name: 'ins-pop--start', desc: 'المحاذاة من البداية، وهي الأصل فلا حاجة إليه، ويبقى مقبولًا لمن كتبه.' },
    { group: 'المحتوى', name: 'ins-pop-title', desc: 'عنوان محتوى البطاقة.' },
    { group: 'المحتوى', name: 'ins-pop-text', desc: 'نص البطاقة.' },
    { group: 'عناصر قابلة للتحديد', name: 'aria-checked="true"', level: 'advanced', desc: 'العنصر محدد. ضعها على كل عنصر قابل للتحديد، `true` أو `false`.' },
    { group: 'عناصر قابلة للتحديد', name: 'ins:menu', kind: 'event', level: 'advanced', desc: 'يُطلق عند كل تحديد، وفي `detail` العنصر `item` وحالته الجديدة `checked`.' }
  ],
  disable: function (s) {
    if (s.body === 'card') return ['label', 'kbd', 'disabled', 'sep', 'bad', 'links', 'check'];
    return s.check ? ['kbd', 'links'] : [];
  },
  html: function (s) {
    var P = s.split ? '      ' : '    ';
    var body;
    if (s.body === 'card') {
      body = [P + '<p class="ins-pop-title">العنوان</p>', P + '<p class="ins-pop-text">نص حر قصير.</p>'];
    } else {
      body = [];
      if (s.label) body.push(P + '<div class="ins-pop-label">العنوان</div>');
      var words = s.check ? ['الاسم', 'التاريخ', 'الحجم'] : s.links ? ['البدء', 'الأسئلة الشائعة', 'المساعدة'] : ['تعديل', 'نسخ', 'نقل'];
      var items = words.map(function (w, i) {
        var off = s.disabled && i === 2;
        if (s.check) {
          var role = s.check === 'checkbox' ? 'menuitemcheckbox' : 'menuitemradio';
          return '<button class="ins-pop-item" role="' + role + '" aria-checked="' + (i === 0) + '"' + (off ? ' disabled' : '') + '>' + w + '</button>';
        }
        var inner = w + (s.kbd && i === 0 ? ' <kbd class="ins-kbd">E</kbd>' : '');
        if (s.links) return '<a class="ins-pop-item"' + (off ? ' aria-disabled="true"' : ' href="' + ['/start', '/faq', '/help'][i] + '"') + '>' + inner + '</a>';
        return '<button class="ins-pop-item"' + (off ? ' disabled' : '') + '>' + inner + '</button>';
      });
      if (s.check === 'radio') body.push(P + '<div role="group">', items.map(function (x) { return P + '  ' + x; }).join('\n'), P + '</div>');
      else body.push(items.map(function (x) { return P + x; }).join('\n'));
      if (s.sep) body.push(P + '<hr class="ins-pop-sep">');
      if (s.bad) body.push(P + (s.links && !s.check ? '<a class="ins-pop-item ins-pop-item--bad" href="/item/delete">حذف</a>' : '<button class="ins-pop-item ins-pop-item--bad">حذف</button>'));
    }
    var Q = s.split ? '  ' : '';
    var summary = s.split
      ? '<summary class="ins-btn ins-btn--primary ins-btn--icon" aria-label="خيارات الحفظ"><svg class="ico"><use href="#i-chev-down"/></svg></summary>'
      : '<summary class="ins-btn ins-btn--secondary">' + (s.body === 'card' ? 'التفاصيل' : 'إجراءات') + '</summary>';
    var pop = Q + '<details class="ins-pop' + (s.align ? ' ins-pop--end' : '') + '">\n' +
      Q + '  ' + summary + '\n' +
      Q + '  <div class="ins-pop-body' + (s.body === 'card' ? ' ins-pop-body--card' : '') + '">\n' + body.join('\n') + '\n' +
      Q + '  </div>\n' +
      Q + '</details>';
    if (!s.split) return pop;
    return '<div class="ins-btn-group" role="group" aria-label="الحفظ">\n  <button class="ins-btn ins-btn--primary">حفظ</button>\n' + pop + '\n</div>';
  },
  // Drawn open, so what each option changes in the menu shows at once.
  stage: function (view) { view.querySelector('details.ins-pop').open = true; },
  notes: function (s) {
    var out = [];
    if (s.body === 'card') out.push('البطاقة لا عناصر فيها، فخيارات العناصر للقائمة وحدها.');
    else if (s.check === 'checkbox') out.push('حدد عنصرًا فتبقى القائمة مفتوحة للتالي.');
    else if (s.check === 'radio') out.push('اختر عنصرًا فينتقل التحديد إليه وتُغلق القائمة.');
    if (s.split) out.push('زر القائمة أيقونة وحدها، فلا بد له من `aria-label`.');
    return out;
  }
});
