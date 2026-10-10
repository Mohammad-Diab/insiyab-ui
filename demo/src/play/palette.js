SitePlay.define({
  id: 'palette',
  title: 'لوحة الأوامر',
  owns: 'ins-palette',
  controls: [
    { key: 'groups', label: 'في مجموعات', type: 'switch', value: true, name: 'data-ins-palette-group', desc: 'اسم المجموعة، على عنصر أو حاوية لعدة عناصر، ويظهر عنوانًا لها. وفي أثناء البحث تجتمع النتائج في قائمة واحدة، ويحمل كل سطر اسم مجموعته.' },
    { key: 'hint', label: 'اختصار في السطر', type: 'switch', value: true, name: 'kbd.ins-kbd', kind: 'element', desc: 'داخل العنصر: يظهر اختصارًا في آخر سطره.' },
    { key: 'keys', label: 'اختصار اللوحة', type: 'seg', value: 'none', choices: [
      { value: 'none', label: 'بلا', name: 'data-ins-palette-keys="none"', desc: 'يلغي الاختصارات، كما في هذه المعاينة حتى لا تزاحم بحث التوثيق.' },
      { value: 'alt+k', label: 'Alt+K', name: 'data-ins-palette-keys', desc: 'الاختصارات التي تفتح اللوحة وتغلقها، بينها مسافة، مثل `mod+p shift+mod+k`، و`mod` هو Ctrl أو ⌘. وإن غابت فهي `mod+k /`، ولا يعمل `/` في أثناء الكتابة في حقل. وتعمل حتى إن كانت لوحة المفاتيح على العربية.' }
    ] },
    { key: 'recent', label: 'الأخيرة', type: 'seg', value: '', choices: [
      { value: '', label: 'خمسة' },
      { value: '3', label: 'ثلاثة', name: 'data-ins-palette-recent', desc: 'قبل أن تكتب شيئًا تعرض اللوحة آخر ما اخترت، الأحدث فالأقدم، وهي خمسة ما لم تحدد عددها هنا. وتُحفظ في المتصفح وحده.' },
      { value: '0', label: 'بلا', name: 'data-ins-palette-recent="0"', desc: 'يخفي «الأخيرة».' }
    ] },
    { key: 'place', label: 'نص الحقل الفارغ', type: 'text', value: '' },
    { key: 'from', label: 'روابط من منطقة أخرى', type: 'switch', name: 'data-ins-palette-from', desc: 'على اللوحة، وقيمتها محدد CSS: تجلب كل رابط في تلك المنطقة بأيقونته وتحت عنوان قسمه `.ins-shell-group`، وتقرؤها من جديد كلما فُتحت، ولا يتكرر فيها رابط موجود. وعلى هذا بُني بحث هذا التوثيق.' }
  ],
  extra: [
    { group: 'الأساس', first: true, name: 'data-ins-palette', desc: 'على `<dialog class="ins-dialog">` تملؤه بروابط وأزرار عادية، فتبني الإضافة حوله حقل البحث والقائمة والعناوين والتنقل بلوحة المفاتيح. واختيار العنصر كالنقر عليه، وتُغلق اللوحة قبل ذلك.' },
    { group: 'الأساس', name: 'ins-palette', desc: 'تضعها الإضافة بنفسها على اللوحة، فتأخذ شكلها من ملف أنماطها.' },
    { group: 'الأساس', name: 'data-ins-palette-placeholder', desc: 'نص `placeholder` في حقل البحث.' },
    { group: 'العناصر', name: 'data-keywords', desc: 'كلمات يشملها البحث ولا تظهر.' },
    { group: 'العناصر', name: 'data-label', desc: 'اسم يُبحث فيه بدل نص العنصر.' },
    { group: 'العناصر', name: 'data-ins-palette-key', desc: 'ما يُحفظ به العنصر في «الأخيرة»، وإن غابت فعنوان الرابط أو اسم العنصر.' },
    { group: 'روابط من منطقة أخرى', name: 'data-ins-palette-skip', desc: 'على رابط في تلك المنطقة: يبقيه خارج اللوحة.' },
    { group: 'الأحداث', name: 'ins:palette', kind: 'event', desc: 'يُطلق عند كل اختيار قبل أن يعمل العنصر، وفي `detail` اسمه `label` ومفتاحه `key` والعنصر `item`. و`preventDefault()` يوقف العنصر، كموجه صفحة واحدة يأخذ الرابط بنفسه.' }
  ],
  events: ['ins:palette'],
  html: function (s) {
    var attrs = ' data-ins-palette-keys="' + s.keys + '"' + (s.recent ? ' data-ins-palette-recent="' + s.recent + '"' : '') +
      (s.place ? ' data-ins-palette-placeholder="' + s.place + '"' : '') + (s.from ? ' data-ins-palette-from="#pal-links"' : '');
    var pages = '<a href="/files" data-keywords="files">الملفات</a>\n<a href="/settings" data-keywords="settings">الإعدادات</a>\n' +
      '<a href="/messages" data-keywords="messages">الرسائل</a>\n<a href="/reports" data-keywords="reports">التقارير</a>\n';
    var actions = '<button data-ins-toast="حُفظ" data-keywords="save">حفظ' + (s.hint ? '<kbd class="ins-kbd">Ctrl S</kbd>' : '') + '</button>\n';
    var inner = s.groups
      ? '<div data-ins-palette-group="الصفحات">\n' + pages.replace(/^(?=.)/gm, '  ') + '</div>\n<div data-ins-palette-group="إجراءات">\n' + actions.replace(/^(?=.)/gm, '  ') + '</div>\n'
      : pages + actions;
    return '<button class="ins-btn ins-btn--secondary" data-ins-dialog="#pal-play">فتح' + (s.keys === 'none' ? '' : '<kbd class="ins-kbd">Alt K</kbd>') + '</button>\n' +
      (s.from ? '<nav class="ins-row" id="pal-links" aria-label="روابط أخرى">\n  <a href="/help">المساعدة</a>\n  <a href="/about">من نحن</a>\n</nav>\n' : '') +
      '<dialog class="ins-dialog ins-palette" id="pal-play" data-ins-palette' + attrs + ' aria-label="لوحة الأوامر">\n' +
      inner.replace(/^(?=.)/gm, '  ') + '</dialog>';
  },
  notes: function (s) {
    return ['اضغط «فتح»' + (s.keys === 'none' ? '' : ' أو Alt+K') + ' لترى اللوحة بخياراتك، واختر منها عنصرًا ثم افتحها ثانية لترى «الأخيرة».'];
  }
});
