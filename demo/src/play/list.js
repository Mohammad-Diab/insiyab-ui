SitePlay.define({
  id: 'list',
  title: 'القائمة',
  owns: 'ins-list',
  wide: true,
  controls: [
    { key: 'panel', label: 'في لوح', type: 'switch', value: true, name: 'ins-panel', desc: 'في لوح تمتد القائمة إلى حوافه. وليس للقائمة سطح خاص بها، فتصلح خارجه أيضًا وتكفيها فواصلها.' },
    { key: 'start', label: 'أيقونة في البداية', type: 'switch', value: true, name: 'ins-panel-ico', desc: 'رقاقة أيقونة يبدأ بها الصف، ولك أن تضع مكانها صورة `.ins-avatar`.' },
    { key: 'desc', label: 'سطر الوصف', type: 'switch', value: true, name: 'ins-list-desc', desc: 'سطر ثان تحت العنوان، بخط أصغر ولون أهدأ.' },
    { key: 'end', label: 'خاتمة', type: 'switch', value: true, name: 'ins-list-end', desc: 'ما ينتهي به الصف، كوقت أو شارة أو زر.' },
    { key: 'link', label: 'صفوف روابط', group: 'صفوف روابط', type: 'switch', name: 'a.ins-list-item', kind: 'element', desc: 'إذا كان الصف `<a>` أو `<button>` صار كله قابلًا للنقر، وظهر في آخره سهم يشير إلى وجهته.' },
    { key: 'current', label: 'الرابط الحالي', group: 'صفوف روابط', type: 'switch', name: 'aria-current="page"', desc: 'يميز الرابط الحالي بخلفية ولون الهوية على عنوانه.' }
  ],
  extra: [
    { group: 'الأساس', first: true, name: 'ins-list', desc: 'على `<ul>` أو `<nav>`: صفوف متتالية بينها فواصل.' },
    { group: 'الأساس', name: 'ins-list-item', desc: 'الصف، وكل ما فيه اختياري.' },
    { group: 'الأساس', name: 'ins-list-text', desc: 'يضم نصي الصف، ويأخذ ما بقي من عرضه.' },
    { group: 'الأساس', name: 'ins-list-title', desc: 'عنوان الصف، وإذا طال قُطع بنقاط.' },
    { group: 'صفوف روابط', name: 'is-active', kind: 'class', desc: 'مثل `aria-current="page"`، لصف `<button>` محدد.' }
  ],
  disable: function (s) { return s.link ? [] : ['current']; },
  html: function (s) {
    var items = [['عام', 'الاسم واللغة', 'أمس', 'i-cog', '/general'], ['الحساب', 'البريد وكلمة المرور', 'اليوم', 'i-user', '/account'], ['الأمان', 'الجلسات والأجهزة', 'قبل ساعة', 'i-lock', '/security']];
    var rows = items.map(function (r, i) {
      var parts = [];
      if (s.start) parts.push('<span class="ins-panel-ico"><svg class="ico"><use href="#' + r[3] + '"/></svg></span>');
      parts.push('<span class="ins-list-text"><span class="ins-list-title">' + r[0] + '</span>' + (s.desc ? '<span class="ins-list-desc">' + r[1] + '</span>' : '') + '</span>');
      if (s.end) parts.push('<span class="ins-list-end">' + r[2] + '</span>');
      var inner = '\n    ' + parts.join('\n    ') + '\n  ';
      if (s.link) return '  <a class="ins-list-item" href="' + r[4] + '"' + (s.current && i === 0 ? ' aria-current="page"' : '') + '>' + inner + '</a>';
      return '  <li class="ins-list-item">' + inner + '</li>';
    }).join('\n');
    var list = s.link ? '<nav class="ins-list" aria-label="الإعدادات">\n' + rows + '\n</nav>' : '<ul class="ins-list">\n' + rows + '\n</ul>';
    return s.panel ? '<div class="ins-panel">\n' + list.replace(/^/gm, '  ') + '\n</div>' : list;
  }
});
