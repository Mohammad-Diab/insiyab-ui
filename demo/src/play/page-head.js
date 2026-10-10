SitePlay.define({
  id: 'head',
  title: 'رأس الصفحة',
  owns: 'ins-page-head',
  wide: true,
  controls: [
    { key: 'title', label: 'العنوان', type: 'text', value: 'عنوان الصفحة' },
    { key: 'sub', label: 'سطر الوصف', type: 'switch', value: true, name: 'ins-page-sub', desc: 'سطر قصير تحت العنوان، داخل `.ins-page-head-text`.' },
    { key: 'actions', label: 'الإجراءات', type: 'switch', value: true, name: 'ins-page-actions', desc: 'أزرار الصفحة في آخر الرأس، وتنزل تحت العنوان إذا ضاقت الشاشة.' },
    { key: 'crumbs', label: 'مسار التنقل', type: 'switch', value: true, name: 'ins-breadcrumb', desc: 'مسار التنقل في أول الرأس، على سطر وحده.' }
  ],
  extra: [
    { group: 'الأساس', first: true, name: 'ins-page-head', desc: 'رأس الصفحة، على `<header>`: العنوان وما تحته، والإجراءات في آخره.' },
    { group: 'الأساس', name: 'ins-page-head-text', desc: 'يضم العنوان وسطر الوصف، ويأخذ ما بقي من السطر.' },
    { group: 'الأساس', name: 'ins-page-title', desc: 'عنوان الصفحة، ويكون `<h1>` في صفحتك. وهو هنا `<h3>` لأن لهذه الصفحة عنوانها.' }
  ],
  html: function (s) {
    var out = '<header class="ins-page-head">\n';
    if (s.crumbs) {
      out += '  <nav class="ins-breadcrumb" aria-label="مسار التنقل">\n' +
        '    <ol>\n' +
        '      <li><a href="/">الرئيسية</a></li>\n' +
        '      <li><a href="/section">القسم</a></li>\n' +
        '      <li><span aria-current="page">الصفحة</span></li>\n' +
        '    </ol>\n' +
        '  </nav>\n';
    }
    out += '  <div class="ins-page-head-text">\n' +
      '    <h3 class="ins-page-title">' + s.title + '</h3>\n' +
      (s.sub ? '    <p class="ins-page-sub">سطر وصف قصير</p>\n' : '') +
      '  </div>\n';
    if (s.actions) {
      out += '  <div class="ins-page-actions">\n' +
        '    <button class="ins-btn ins-btn--primary">حفظ</button>\n' +
        '  </div>\n';
    }
    return out + '</header>';
  }
});

SitePlay.define({
  id: 'breadcrumb',
  title: 'مسار التنقل',
  owns: 'ins-breadcrumb',
  wide: true,
  controls: [
    { key: 'depth', label: 'عدد المستويات', type: 'seg', value: '3', choices: [
      { value: '2', label: '2' },
      { value: '3', label: '3' },
      { value: '4', label: '4' }
    ] },
    { key: 'here', label: 'الصفحة الحالية', type: 'text', value: 'الفواتير' }
  ],
  extra: [
    { group: 'الأساس', first: true, name: 'ins-breadcrumb', desc: 'على `<nav>` ومعها `aria-label`، وفيها قائمة `<ol>`. والفاصل بين عناصرها مرسوم بـ CSS لا حرف مكتوب، فلا يقرؤه قارئ الشاشة، وينقلب مع اتجاه الصفحة.' },
    { group: 'الأساس', name: 'aria-current="page"', desc: 'على العنصر الأخير، بلا رابط: الصفحة الحالية. ويُختصر النص الطويل بثلاث نقاط.' }
  ],
  html: function (s) {
    var trail = ['الرئيسية', 'الإعدادات', 'الحساب'].slice(0, +s.depth - 1);
    var paths = ['/', '/settings', '/settings/account'];
    var out = '<nav class="ins-breadcrumb" aria-label="مسار التنقل">\n  <ol>\n';
    trail.forEach(function (name, i) { out += '    <li><a href="' + paths[i] + '">' + name + '</a></li>\n'; });
    return out + '    <li><span aria-current="page">' + s.here + '</span></li>\n  </ol>\n</nav>';
  },
  notes: function (s) {
    return s.here.length > 24 ? ['النص أطول مما يسعه العنصر، فاختُصر بثلاث نقاط.'] : [];
  }
});
