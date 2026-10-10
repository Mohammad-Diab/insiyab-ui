var NOTIFY_ROWS = [
  { title: 'اكتمل التصدير', desc: 'الملف جاهز للتنزيل.', end: 'الآن', ico: 'i-download', who: 'م', href: '/exports' },
  { title: 'تعليق جديد على المستند', desc: 'على الفقرة الثانية.', end: 'قبل ساعة', ico: 'i-message', who: 'س', href: '/documents' },
  { title: 'تم حفظ التغييرات', desc: 'حُفظت نسخة جديدة من الملف.', end: 'أمس', ico: 'i-check', who: 'ل', href: '/files' }
];

function notifyRow(r, unread, style, pad) {
  var lead = style === 'avatar' ? '<span class="ins-avatar ins-avatar--sm" aria-hidden="true">' + r.who + '</span>'
    : '<span class="ins-panel-ico"><svg class="ico"><use href="#' + r.ico + '"/></svg></span>';
  return pad + '<li><a class="ins-list-item" href="' + r.href + '"' + (unread ? ' data-ins-unread' : '') + '>\n' +
    pad + '  ' + lead + '\n' +
    pad + '  <span class="ins-list-text"><span class="ins-list-title">' + r.title + '</span><span class="ins-list-desc">' + r.desc + '</span></span>\n' +
    pad + '  <span class="ins-list-end">' + r.end + '</span>\n' +
    pad + '</a></li>';
}

SitePlay.define({
  id: 'notify',
  title: 'مركز الإشعارات',
  owns: 'ins-notify',
  controls: [
    { key: 'empty', label: 'بلا إشعارات', type: 'switch', name: 'ins-empty', desc: 'إذا خلت القائمة ظهرت مكانها حالة فارغة: `.ins-empty` تكتبها أنت داخل `.ins-notify-body`، وإلا وضع السكربت جملة «لا إشعارات».' },
    { key: 'unread', label: 'غير المقروءة', type: 'seg', value: '3', choices: [
      { value: '0', label: 'لا شيء' },
      { value: '1', label: 'واحد' },
      { value: '3', label: 'ثلاثة', name: 'data-ins-unread', desc: 'على صف لم يُقرأ: نقطة بلون الهوية في أوله، وخلفية خفيفة، وعنوان أثقل، ويعده الجرس. وتزول حين يُنقر الصف.' },
      { value: 'many', label: 'أكثر من 99' }
    ] },
    { key: 'style', label: 'أول الصف', type: 'seg', value: 'ico', choices: [
      { value: 'ico', label: 'أيقونة', name: 'ins-panel-ico', desc: 'رقاقة أيقونة تبدأ بها الإشعارات، لما يأتي من النظام.' },
      { value: 'avatar', label: 'صورة رمزية', name: 'ins-avatar', desc: 'صورة صاحب الإشعار أو حرفه الأول، لما يأتي من شخص.' }
    ] },
    { key: 'read', label: 'زر تعليم الكل', type: 'switch', value: true, name: 'data-ins-notify-read', desc: 'على زر في رأس اللوحة: يعلم الإشعارات كلها مقروءة.' },
    { key: 'foot', label: 'رابط عرض الكل', type: 'switch', value: true, name: 'ins-notify-foot', desc: 'رابط في ذيل اللوحة إلى صفحة الإشعارات كلها، يبقى ثابتًا تحت القائمة.' }
  ],
  extra: [
    { group: 'الأساس', first: true, name: 'data-ins-notify', desc: 'على `details.ins-pop`: يجعل القائمة المنسدلة مركز إشعارات يعد ما لم يُقرأ ويعلمه مقروءًا، ولها سلوك القائمة كله: `Esc` والنقر خارجها وإعادة التركيز إلى الجرس.' },
    { group: 'الأساس', name: 'ins-notify', desc: 'على `details` نفسها، مع `.ins-pop`.' },
    { group: 'الأساس', name: 'ins-notify-body', desc: 'اللوحة، مع `.ins-pop-body`، بعرض 22rem تقريبًا، رأسها وذيلها ثابتان وتتمرر القائمة بينهما.' },
    { group: 'الأساس', name: 'ins-notify-head', desc: 'رأس اللوحة، فيه العنوان والزر.' },
    { group: 'الأساس', name: 'ins-notify-title', desc: 'عنوان اللوحة.' },
    { group: 'الأساس', name: 'ins-notify-list', desc: 'على `.ins-list`: صفوف الإشعارات `.ins-list-item`، روابط أو صفوفًا عادية.' },
    { group: 'الأساس', name: 'ins-notify-count', desc: 'العدد على زاوية الجرس، يضيفه السكربت ويخفيه عند الصفر، وفوق 99 يكتب 99+. ويقرأ قارئ الشاشة مع اسم الجرس العدد كاملًا: «الإشعارات، 3 غير مقروءة».' },
    { group: 'الأساس', name: 'ins:notify', kind: 'event', desc: 'يُطلق حين يُعلم إشعار مقروءًا بنقرة، وفي `detail` المركز `el` والصف `item`، و`all` إذا عُلمت كلها معًا.' }
  ],
  disable: function (s) { return s.empty ? ['unread', 'style', 'read'] : []; },
  events: ['ins:notify'],
  html: function (s) {
    var unread = s.unread === 'many' ? 3 : +s.unread;
    var rows = s.empty ? [] : NOTIFY_ROWS.map(function (r, i) { return notifyRow(r, i < unread, s.style, '      '); });
    if (!s.empty && s.unread === 'many') rows.push('      <!-- … وغيرها حتى 120 إشعارًا لم يُقرأ -->');
    return '<details class="ins-pop ins-notify" data-ins-notify>\n' +
      '  <summary class="ins-island ins-island--icon" aria-label="الإشعارات"><svg class="ico"><use href="#i-bell"/></svg></summary>\n' +
      '  <div class="ins-pop-body ins-notify-body">\n' +
      '    <div class="ins-notify-head">\n' +
      '      <b class="ins-notify-title">الإشعارات</b>\n' +
      (s.read && !s.empty ? '      <button class="ins-btn ins-btn--bare ins-btn--sm" type="button" data-ins-notify-read>تعليم الكل مقروءًا</button>\n' : '') +
      '    </div>\n' +
      (rows.length ? '    <ul class="ins-list ins-notify-list">\n' + rows.join('\n') + '\n    </ul>\n' : '    <ul class="ins-list ins-notify-list"></ul>\n') +
      (s.foot ? '    <a class="ins-notify-foot" href="/notifications">عرض الكل</a>\n' : '') +
      '  </div>\n' +
      '</details>';
  },
  // The rest of a long list, and a button that delivers one more, so the count and the ring can be seen live.
  stage: function (view, s) {
    var list = view.querySelector('.ins-notify-list');
    if (!list) return;
    if (s.unread === 'many' && list.lastElementChild) {
      for (var i = 0; i < 117; i++) list.appendChild(list.lastElementChild.cloneNode(true));
    }
    var add = document.createElement('button');
    add.type = 'button';
    add.className = 'ins-btn ins-btn--secondary ins-btn--sm site-adv';
    add.textContent = 'إشعار جديد';
    add.addEventListener('click', function () {
      list.insertAdjacentHTML('afterbegin', notifyRow({ title: 'إشعار جديد', desc: 'وصل للتو.', end: 'الآن', ico: 'i-bell', who: 'ن', href: '/notifications' }, true, s.style, ''));
    });
    view.appendChild(add);
    view.style.alignContent = 'center';
    view.style.rowGap = '1rem';
  },
  notes: function () { return ['اضغط الجرس لترى اللوحة بخياراتك، وانقر إشعارًا ليصير مقروءًا.']; }
});
