SitePlay.define({
  id: 'pagination',
  title: 'الترقيم',
  owns: 'ins-page',
  controls: [
    { key: 'at', label: 'الصفحة الحالية', type: 'seg', value: 'mid', choices: [
      { value: 'first', label: 'الأولى' },
      { value: 'mid', label: 'في الوسط' },
      { value: 'last', label: 'الأخيرة' }
    ] },
    { key: 'compact', label: 'مختصر', type: 'switch', name: '<span class="ins-page-gap">3 من 12</span>', kind: 'element', group: 'الترقيم المختصر', desc: 'اكتف بالسهمين وبينهما موضعك في `.ins-page-gap`، فيناسب الهاتف والقائمة الجانبية.' }
  ],
  extra: [
    { group: 'الأساس', first: true, name: 'ins-pagination', desc: 'على `<nav>` ومعه `aria-label`، ويضم روابط الصفحات، فلا يحتاج سكربتًا.' },
    { group: 'الأساس', name: 'ins-page', desc: 'رابط صفحة، على `<a href>` أو `<button>`.' },
    { group: 'الأساس', name: 'aria-current="page"', desc: 'الصفحة الحالية، على رابط بلا `href`. ويصلح له `.is-active` أيضًا.' },
    { group: 'الأساس', name: 'ins-page--prev', desc: 'رابط «السابق» بسهمه، ويشير السهم إلى اليمين في الصفحة العربية.' },
    { group: 'الأساس', name: 'ins-page--next', desc: 'رابط «التالي» بسهمه.' },
    { group: 'الأساس', name: 'ins-page-gap', desc: 'الفجوة `…` بين الأرقام.' },
    { group: 'عند الطرفين', name: 'aria-disabled="true"', desc: 'في الصفحة الأولى لا وجهة لـ«السابق»، فأعطه هذه السمة ودعه بلا `href`، وكذلك «التالي» في الأخيرة. وعلى `<button>` يكفي `disabled`.' }
  ],
  html: function (s) {
    var n = 12, cur = s.at === 'first' ? 1 : s.at === 'last' ? n : 3;
    var link = function (p, cls, text) {
      var off = p < 1 || p > n;
      return '  <a class="ins-page' + (cls ? ' ins-page--' + cls : '') + '"' + (off ? ' aria-disabled="true"' : ' href="?page=' + p + '"') + '>' + text + '</a>';
    };
    var out = [link(cur - 1, 'prev', 'السابق')];
    if (s.compact) out.push('  <span class="ins-page-gap">' + cur + ' من ' + n + '</span>');
    else {
      var pages = cur === 1 ? [1, 2, 3, 0, 12] : cur === n ? [1, 0, 10, 11, 12] : [1, 2, 3, 4, 0, 12];
      pages.forEach(function (p) {
        if (!p) out.push('  <span class="ins-page-gap">…</span>');
        else if (p === cur) out.push('  <a class="ins-page" aria-current="page">' + p + '</a>');
        else out.push(link(p, '', String(p)));
      });
    }
    out.push(link(cur + 1, 'next', 'التالي'));
    return '<nav class="ins-pagination" aria-label="الصفحات">\n' + out.join('\n') + '\n</nav>';
  },
  notes: function (s) {
    if (s.at === 'first') return ['لا صفحة قبل الأولى، فجاء «السابق» معطلًا بـ `aria-disabled="true"` وبلا `href`.'];
    if (s.at === 'last') return ['لا صفحة بعد الأخيرة، فجاء «التالي» معطلًا بـ `aria-disabled="true"` وبلا `href`.'];
    return [];
  }
});
