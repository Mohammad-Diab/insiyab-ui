SitePlay.define({
  id: 'drawer',
  title: 'الدرج',
  owns: 'ins-drawer',
  controls: [
    { key: 'edge', label: 'الحافة', type: 'seg', value: '', choices: [
      { value: '', label: 'البداية' },
      { value: 'end', label: 'النهاية', name: 'ins-drawer--end', desc: 'يخرج الدرج من حافة النهاية، وهي اليسار في العربية.' },
      { value: 'bottom', label: 'الأسفل', name: 'ins-drawer--bottom', desc: 'لوح يصعد من أسفل الشاشة ويمتد على عرضها، ويناسب الهاتف.' }
    ] },
    { key: 'title', label: 'العنوان', type: 'text', value: 'عنوان الدرج' },
    { key: 'foot', label: 'الذيل', type: 'switch', value: true, name: 'ins-dialog-foot', group: 'الأجزاء', desc: 'ذيل الدرج، يثبت في أسفله وفيه أزراره.' },
    { key: 'closedby', label: 'ما يغلقه', type: 'select', value: '', group: 'الإغلاق', choices: [
      { value: '', label: 'Esc والنقر خارجه' },
      { value: 'closerequest', label: 'Esc وأزراره', name: 'closedby="closerequest"', desc: 'لا يغلقه النقر خارجه، بل `Esc` وأزراره، لدرج فيه نموذج لا تريد أن تضيعه نقرة شاردة.' },
      { value: 'none', label: 'أزراره وحدها', name: 'closedby="none"', desc: 'لا يغلقه `Esc` ولا النقر خارجه، بل أزراره وحدها.' }
    ] }
  ],
  extra: [
    { group: 'الأساس', first: true, name: 'ins-drawer', desc: 'على `<dialog>`، فيخرج من حافة البداية، وهي اليمين في العربية. ويغلقه `Esc` والنقر خارجه.' },
    { group: 'الأساس', name: 'data-ins-dialog="#id"', desc: 'على الزر الذي يفتح الدرج، كما يفتح مربع الحوار.' },
    { group: 'الأساس', name: 'data-ins-dismiss', desc: 'على زر داخل الدرج يغلقه.' },
    { group: 'الأجزاء', name: 'ins-dialog-head', desc: 'رأس الدرج، وفيه العنوان `.ins-dialog-title` وزر الإغلاق.' },
    { group: 'الأجزاء', name: 'ins-dialog-body', desc: 'متن الدرج، يملأ ما بين الرأس والذيل ويتمرر وحده.' }
  ],
  html: function (s) {
    var lines = [
      '  <div class="ins-dialog-head">',
      '    <h3 class="ins-dialog-title">' + s.title + '</h3>',
      '    <button class="ins-close" data-ins-dismiss aria-label="إغلاق"></button>',
      '  </div>',
      '  <div class="ins-dialog-body">محتوى الدرج.</div>'
    ];
    if (s.foot) {
      lines.push('  <div class="ins-dialog-foot">',
        '    <button class="ins-btn ins-btn--bare" data-ins-dismiss>إلغاء</button>',
        '    <button class="ins-btn ins-btn--primary" data-ins-dismiss>تطبيق</button>',
        '  </div>');
    }
    return '<button class="ins-btn ins-btn--secondary" data-ins-dialog="#drawer-demo">فتح الدرج</button>\n\n' +
      '<dialog class="ins-drawer' + (s.edge ? ' ins-drawer--' + s.edge : '') + '" id="drawer-demo"' + (s.closedby ? ' closedby="' + s.closedby + '"' : '') + '>\n' + lines.join('\n') + '\n</dialog>';
  },
  notes: function (s) {
    if (s.closedby) return ['اضغط الزر لترى الدرج بخياراتك، ثم انقر خارجه فلا يُغلق.'];
    return ['اضغط الزر لترى الدرج بخياراتك، ثم انقر خارجه فيُغلق.'];
  }
});
