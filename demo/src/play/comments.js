SitePlay.define({
  id: 'comment',
  title: 'التعليق',
  owns: 'ins-comment',
  wide: true,
  controls: [
    { key: 'own', label: 'رسالتك أنت', type: 'switch', name: 'ins-comment--own', desc: 'رسالة من يقرأ في المحادثة: تنتقل إلى الجهة المقابلة، ويأخذ الاسم لون الهوية.' },
    { key: 'actions', label: 'إجراءات', type: 'switch', value: true, name: 'ins-comment-actions', desc: 'صف أزرار تحت النص، كالرد. والأزرار `ins-btn--bare` فيه تهدأ حتى يمر عليها المؤشر.' },
    { key: 'text', label: 'النص', type: 'text', value: 'راجعت الملف، والعنوان يحتاج تعديلًا بسيطًا.' }
  ],
  extra: [
    { group: 'الأساس', first: true, name: 'ins-comment', desc: 'كلام شخص واحد: صورته `.ins-avatar ins-avatar--sm` ثم جسم التعليق.' },
    { group: 'الأساس', name: 'ins-comment-body', desc: 'يضم الرأس والنص والإجراءات.' },
    { group: 'الأساس', name: 'ins-comment-head', desc: 'سطر الاسم والوقت.' },
    { group: 'الأساس', name: 'ins-comment-name', desc: 'اسم الكاتب.' },
    { group: 'الأساس', name: 'ins-comment-time', desc: 'على `<time>`، ومعها `datetime`، وتسبقها نقطة تفصلها عن الاسم.' },
    { group: 'الأساس', name: 'ins-comment-text', desc: 'النص نفسه، وفيه ما شئت من فقرات.' }
  ],
  html: function (s) {
    var who = s.own ? ['أ', 'أنت'] : ['ل', 'ليلى'];
    return '<article class="ins-comment' + (s.own ? ' ins-comment--own' : '') + '">\n' +
      '  <span class="ins-avatar ins-avatar--sm">' + who[0] + '</span>\n' +
      '  <div class="ins-comment-body">\n' +
      '    <div class="ins-comment-head">\n' +
      '      <b class="ins-comment-name">' + who[1] + '</b>\n' +
      '      <time class="ins-comment-time" datetime="2026-09-26T10:00">قبل ساعتين</time>\n' +
      '    </div>\n' +
      '    <div class="ins-comment-text">' + s.text + '</div>\n' +
      (s.actions ? '    <div class="ins-comment-actions">\n      <button class="ins-btn ins-btn--bare ins-btn--sm">رد</button>\n    </div>\n' : '') +
      '  </div>\n</article>';
  }
});

SitePlay.define({
  id: 'thread',
  title: 'السلسلة',
  owns: 'ins-thread',
  wide: true,
  controls: [
    { key: 'chat', label: 'محادثة', type: 'switch' },
    { key: 'replies', label: 'ردود متداخلة', type: 'switch', value: true, name: 'ins-comment-replies', desc: 'داخل جسم التعليق: يزيح الردود تحته إلى الداخل، ويجعل بجانبها خطًا ينزل من صورة صاحبه.' }
  ],
  extra: [
    { group: 'الأساس', first: true, name: 'ins-thread', desc: 'يرص التعليقات بعضها فوق بعض، بمسافة بينها.' }
  ],
  disable: function (s) { return s.chat ? ['replies'] : []; },
  html: function (s) {
    var one = function (pad, cls, av, name, time, text, inside) {
      return pad + '<article class="ins-comment' + cls + '">\n' +
        pad + '  <span class="ins-avatar ins-avatar--sm">' + av + '</span>\n' +
        pad + '  <div class="ins-comment-body">\n' +
        pad + '    <div class="ins-comment-head"><b class="ins-comment-name">' + name + '</b><time class="ins-comment-time">' + time + '</time></div>\n' +
        pad + '    <div class="ins-comment-text">' + text + '</div>\n' + (inside || '') +
        pad + '  </div>\n' + pad + '</article>';
    };
    if (s.chat) {
      return '<div class="ins-thread">\n' + one('  ', '', 'ل', 'ليلى', '10:02', 'هل وصل الملف؟') + '\n' +
        one('  ', ' ins-comment--own', 'أ', 'أنت', '10:05', 'نعم، وصل صباحًا.') + '\n</div>';
    }
    var reply = s.replies
      ? '      <div class="ins-comment-replies">\n' + one('        ', '', 'ع', 'عمر', 'قبل ساعة', 'عدلته، وهو جاهز الآن.') + '\n      </div>\n'
      : '';
    var first = one('  ', '', 'ل', 'ليلى', 'قبل ساعتين', 'العنوان يحتاج تعديلًا بسيطًا.', reply);
    var more = s.replies ? '' : '\n' + one('  ', '', 'ع', 'عمر', 'قبل ساعة', 'عدلته، وهو جاهز الآن.');
    return '<div class="ins-thread">\n' + first + more + '\n' + one('  ', '', 'س', 'سارة', 'قبل دقائق', 'شكرًا لكما.') + '\n</div>';
  }
});
