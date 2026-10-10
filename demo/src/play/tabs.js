SitePlay.define({
  id: 'tabs',
  title: 'علامات التبويب',
  owns: 'ins-tablist',
  wide: true,
  controls: [
    { key: 'look', label: 'الهيئة', type: 'seg', value: '', choices: [
      { value: '', label: 'عادية' },
      { value: 'line', label: 'بخط', name: 'ins-tablist--line', desc: 'هيئة أخف: التبويبات صف فوق خط رفيع، وتحت المختار منها خط ينتقل إلى التبويب الجديد.' },
      { value: 'seg', label: 'مقسمة', name: 'ins-seg', desc: 'ضع `data-ins-tab` على أزرار `.ins-seg`، فيبدل الشريط المقسم الألواح بالآلية نفسها.' }
    ] },
    { key: 'link', label: 'تبويب رابط', type: 'switch', name: 'a.ins-tab', kind: 'element', desc: 'تبويب `<a href>` يذهب إلى صفحة أخرى، فتتركه المكتبة وشأنه، وتميزه `aria-current="page"` إن كان للصفحة الحالية.' }
  ],
  extra: [
    { group: 'الأساس', first: true, name: 'ins-tablist', desc: 'يضم أزرار التبويب، ومعه `aria-label` يسمي المجموعة.' },
    { group: 'الأساس', name: 'ins-tab', desc: 'زر التبويب. ويضيف السكربت أدوار ARIA والتنقل بالأسهم، والسهم الأيسر في العربية هو «التالي».' },
    { group: 'الأساس', name: 'data-ins-tab="#id"', desc: 'على زر التبويب، وقيمتها معرّف اللوح الذي يعرضه.' },
    { group: 'الأساس', name: 'ins-tabpanel', desc: 'لوح التبويب. وإن غابت جافاسكربت ظهرت الألواح كلها متتالية، فلا يختفي منها شيء.' },
    { group: 'الأساس', name: 'ins:tab', kind: 'event', level: 'advanced', desc: 'يُطلق عند اختيار تبويب، وفي `detail` التبويب `tab` ولوحه `panel`.' }
  ],
  disable: function (s) { return s.look === 'seg' ? ['link'] : []; },
  html: function (s) {
    var tabs = [['tab-1', 'عام'], ['tab-2', 'الحساب'], ['tab-3', 'الإشعارات']];
    var seg = s.look === 'seg';
    var cls = seg ? 'ins-seg' : 'ins-tablist' + (s.look === 'line' ? ' ins-tablist--line' : '');
    var item = seg ? '' : 'ins-tab';
    var out = '<div class="' + cls + '" aria-label="الإعدادات">\n' +
      tabs.map(function (t) { return '  <button' + (item ? ' class="' + item + '"' : '') + ' data-ins-tab="#' + t[0] + '">' + t[1] + '</button>'; }).join('\n') +
      (s.link && !seg ? '\n  <a class="ins-tab" href="/help">المساعدة</a>' : '') + '\n</div>\n';
    return out + tabs.map(function (t) { return '<div class="ins-tabpanel" id="' + t[0] + '">محتوى «' + t[1] + '».</div>'; }).join('\n');
  },
  notes: function (s) {
    return s.look === 'seg' ? ['الشريط المقسم لا يحمل رابطًا، فخيار التبويب الرابط للهيئتين الأخريين.'] : [];
  }
});
