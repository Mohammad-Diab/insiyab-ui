SitePlay.define({
  id: 'countdown',
  title: 'العد التنازلي',
  owns: 'ins-countdown',
  controls: [
    { key: 'from', label: 'الوقت', type: 'select', value: '600', choices: [
      { value: '600', label: 'عشر دقائق' },
      { value: '75', label: 'دقيقة وربع' },
      { value: '10', label: 'عشر ثوانٍ' },
      { value: '200000', label: 'أيام' },
      { value: 'date', label: 'حتى لحظة' }
    ] },
    { key: 'warn', label: 'التحذير قبل الصفر بـ', type: 'seg', value: '', choices: [
      { value: '', label: 'دقيقة' },
      { value: '900', label: 'ربع ساعة', name: 'data-ins-warn', desc: 'كم ثانية قبل الصفر يتلون العداد بالأصفر، وهي دقيقة إن لم تذكرها.' }
    ] },
    { key: 'size', label: 'المقاس', type: 'seg', value: '', choices: [
      { value: '', label: 'عادي' },
      { value: 'lg', label: 'كبير', name: 'ins-countdown--lg', desc: 'عداد أكبر، إذا كان العد محور الصفحة.' }
    ] },
    { key: 'paused', label: 'يبدأ عند الطلب', group: 'البدء والإيقاف', type: 'switch', name: 'data-ins-countdown-paused', desc: 'يعرض العداد وقته كاملًا وينتظر، ثم تتحكم فيه بأزرار تشير إليه.' },
    { key: 'away', label: 'يتوقف في الخلفية', type: 'switch', name: 'data-ins-countdown-pause-away', desc: 'يتوقف العداد ما دامت الصفحة في الخلفية، ثم يكمل حين تعود، فلا يُحسب إلا الوقت الذي يقضيه صاحبه أمامه.' }
  ],
  extra: [
    { group: 'الأساس', first: true, name: 'ins-countdown', desc: 'يعد الوقت الباقي ثانية بعد ثانية، فيتلون بالأصفر حين يقترب الصفر، ثم بالأحمر عنده. ويعرفه قارئ الشاشة مؤقتًا فلا يقرؤه كل ثانية.' },
    { group: 'الأساس', name: 'data-ins-countdown', desc: 'الوقت: رقم هو ثوانٍ تبدأ من فتح الصفحة، أو لحظة الانتهاء مثل `2026-09-26T18:00:00+03:00`. ومن جافاسكربت يبدؤه `Insiyab.countdown(el, 600)` من جديد، ويعيد `Insiyab.countdown(el)` الثواني الباقية.' },
    { group: 'الأساس', name: 'ins:countdown', kind: 'event', desc: 'يُطلق مرتين: حين يبلغ العداد حد التحذير، وعند الصفر. وفي `detail` العداد `el` والثواني الباقية `left` والحالة `state`، وهي `warn` أو `end`.' },
    { group: 'البدء والإيقاف', name: 'data-ins-countdown-start', desc: 'على زر يشير إلى العداد، مثل `data-ins-countdown-start="#timer"`: يبدؤه، أو يكمله من حيث توقف. ومن جافاسكربت: `Insiyab.countdown(el, \'start\')`.' },
    { group: 'البدء والإيقاف', name: 'data-ins-countdown-stop', desc: 'يوقفه ويبقي الوقت الباقي ظاهرًا. ومن جافاسكربت: `Insiyab.countdown(el, \'stop\')`، ويعيد الثواني الباقية.' },
    { group: 'البدء والإيقاف', name: 'data-ins-countdown-restart', desc: 'يعيده من أوله، وهو ما يحتاجه زر «أعد إرسال الرمز». ومن جافاسكربت: `Insiyab.countdown(el, \'restart\')`.' }
  ],
  events: ['ins:countdown'],
  html: function (s) {
    var attrs = ' data-ins-countdown="' + (s.from === 'date' ? '2028-01-01T00:00:00+03:00' : s.from) + '"' +
      (s.warn ? ' data-ins-warn="' + s.warn + '"' : '') + (s.away ? ' data-ins-countdown-pause-away' : '');
    var cls = 'ins-countdown' + (s.size ? ' ins-countdown--lg' : '');
    if (!s.paused) return '<span class="' + cls + '"' + attrs + '></span>';
    return '<div class="ins-flex ins-flex-wrap ins-items-center ins-gap-2">\n' +
      '  <span class="' + cls + '" id="cd-timer"' + attrs + ' data-ins-countdown-paused></span>\n' +
      '  <button class="ins-btn ins-btn--primary ins-btn--sm" data-ins-countdown-start="#cd-timer">ابدأ</button>\n' +
      '  <button class="ins-btn ins-btn--secondary ins-btn--sm" data-ins-countdown-stop="#cd-timer">أوقف</button>\n' +
      '  <button class="ins-btn ins-btn--bare ins-btn--sm" data-ins-countdown-restart="#cd-timer">من جديد</button>\n' +
      '</div>';
  },
  notes: function (s) {
    var out = [];
    if (s.from === 'date') out.push('القيمة لحظة بعينها، فيعد العداد إليها أيامًا وساعات.');
    if (s.away) out.push('انتقل إلى نافذة أخرى ثم عد بعد لحظات، فتجد العداد وقف حيث تركته ثم أكمل.');
    if (s.paused) out.push('اضغط «ابدأ» فيبدأ العد، و«أوقف» فيقف، و«من جديد» فيعيده من أوله.');
    else if (s.from === '10' || s.from === '75') out.push('انتظر قليلًا، فيتلون العداد حين يقترب الصفر ويظهر هنا الحدث.');
    return out;
  }
});
