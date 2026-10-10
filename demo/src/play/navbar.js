SitePlay.define({
  id: 'navbar',
  title: 'شريط الموقع',
  owns: 'ins-navbar',
  wide: true,
  controls: [
    { key: 'brand', label: 'الاسم', type: 'text', value: 'الموقع' },
    { key: 'current', label: 'رابط حالي', type: 'switch', value: true, name: 'aria-current="page"', desc: 'على رابط الصفحة الحالية، ويغنيك عن أي كلاس: يعلن قارئ الشاشة أنه «الصفحة الحالية»، ويميزه التصميم للعين.' },
    { key: 'end', label: 'أزرار في آخره', type: 'switch', value: true, name: 'ins-navbar-end', desc: 'حاوية لأزرار الشريط في آخره، كالدخول ومفتاح الوضع الداكن، داخل `.ins-navbar-menu`.' }
  ],
  extra: [
    { group: 'الأساس', first: true, name: 'ins-navbar', desc: 'شريط للمواقع دون التطبيقات، كصفحة الهبوط والمدونة والتوثيق: جزيرة زجاجية تلتصق بأعلى الصفحة.' },
    { group: 'الأساس', name: 'ins-navbar--static', desc: 'يبقى الشريط في مكانه فلا يلتصق بأعلى الصفحة. وهو في المعاينة دائمًا، حتى لا يعلو شريطها شريط التوثيق.' },
    { group: 'الأساس', name: 'ins-navbar-brand', desc: 'اسم الموقع أو شعاره، رابطًا إلى الرئيسية.' },
    { group: 'الأساس', name: 'ins-navbar-menu', desc: 'الروابط، على `<nav>` ومعها `aria-label`.' },
    { group: 'الأساس', name: 'ins-navbar-link', desc: 'رابط في الشريط.' },
    { group: 'على الهاتف', name: 'ins-navbar-toggle', desc: 'زر القائمة، ومعه `data-ins-navbar` و`aria-label`. يظهر إذا ضاقت الشاشة عن 900px، وتنطوي الروابط خلفه.' },
    { group: 'على الهاتف', name: 'data-ins-navbar', desc: 'على زر القائمة: يفتحها ويغلقها ويضبط `aria-expanded`، وتنغلق إذا ضغطت رابطًا أو Esc. وبلا جافاسكربت لا يظهر الزر، وتلتف الروابط إلى سطر ثان.' },
    { group: 'على الهاتف', name: 'is-open', kind: 'class', desc: 'القائمة مفتوحة. يضعها السكربت على `.ins-navbar`، أو `Insiyab.navbar(bar, true)`.' },
    { group: 'على الهاتف', name: 'ins:navbar', kind: 'event', desc: 'يُطلق عند الفتح والإغلاق، وفي `detail` الشريط `el` وحالته `open`.' }
  ],
  events: ['ins:navbar'],
  html: function (s) {
    return '<header class="ins-navbar ins-navbar--static">\n' +
      '  <a class="ins-navbar-brand" href="/">' + s.brand + '</a>\n' +
      '  <button class="ins-navbar-toggle" data-ins-navbar aria-label="القائمة"></button>\n' +
      '  <nav class="ins-navbar-menu" aria-label="الموقع">\n' +
      '    <a class="ins-navbar-link" href="/"' + (s.current ? ' aria-current="page"' : '') + '>الرئيسية</a>\n' +
      '    <a class="ins-navbar-link" href="/blog">المدونة</a>\n' +
      '    <a class="ins-navbar-link" href="/contact">تواصل معنا</a>\n' +
      (s.end ? '    <div class="ins-navbar-end">\n      <button class="ins-btn ins-btn--secondary ins-btn--sm">دخول</button>\n    </div>\n' : '') +
      '  </nav>\n' +
      '</header>';
  },
  notes: function () {
    return ['إذا ضاقت النافذة عن 900px انطوت الروابط خلف زر القائمة، فاضغطه يظهر هنا الحدث.'];
  }
});
