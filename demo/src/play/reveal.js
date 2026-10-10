var animLoop = 0;

SitePlay.define({
  id: 'reveal',
  title: 'دخول الأبناء متتابعين',
  wide: true,
  controls: [
    { key: 'count', label: 'عدد الأبناء', type: 'seg', value: '4', choices: [
      { value: '4', label: '4' },
      { value: '8', label: '8' }
    ] }
  ],
  extra: [
    { group: 'الأساس', first: true, name: 'data-ins-reveal', desc: 'على حاوية، فيدخل أبناؤها واحدًا بعد واحد، حتى العاشر منهم. وتتوقف الحركة إذا أطفأ المستخدم المؤثرات.' }
  ],
  html: function (s) {
    var boxes = [];
    for (var i = 1; i <= +s.count; i++) boxes.push('  <div class="demo-box">' + i + '</div>');
    return '<div class="ins-cols-4" data-ins-reveal>\n' + boxes.join('\n') + '\n</div>';
  },
  notes: function () { return ['غيّر أي خيار فتعود الحركة من أولها.']; }
});

SitePlay.define({
  id: 'anim',
  title: 'حركة لعنصر بمفرده',
  owns: 'ins-anim',
  controls: [
    { key: 'anim', label: 'الحركة', type: 'seg', value: 'rise', choices: [
      { value: 'rise', label: 'صعود', name: 'ins-anim-rise', desc: 'يصعد العنصر قليلًا وهو يظهر.' },
      { value: 'pop', label: 'نبضة', name: 'ins-anim-pop', desc: 'يكبر العنصر من أصغر من حجمه بنبضة خفيفة.' },
      { value: 'slide', label: 'انزلاق', name: 'ins-anim-slide', desc: 'ينزلق العنصر من حافة البداية مع اتجاه القراءة.' }
    ] },
    { key: 'text', label: 'النص', type: 'text', value: 'عنصر' }
  ],
  html: function (s) {
    return '<div class="demo-box ins-anim-' + s.anim + '">' + s.text + '</div>';
  },
  // An entrance runs once, so the preview replays it every two seconds.
  stage: function (view) {
    clearInterval(animLoop);
    var el = view.firstElementChild, cls = el.className;
    animLoop = setInterval(function () {
      if (!el.isConnected) { clearInterval(animLoop); return; }
      el.className = 'demo-box';
      void el.offsetWidth;
      el.className = cls;
    }, 2000);
  },
  notes: function () { return ['تعيد المعاينة الحركة كل ثانيتين، لأن حركة الدخول تجري مرة واحدة.']; }
});
