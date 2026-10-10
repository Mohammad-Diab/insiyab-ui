SitePlay.define({
  id: 'alert',
  title: 'التنبيه',
  owns: 'ins-alert',
  controls: [
    { key: 'tone', label: 'النبرة', type: 'select', value: 'info', choices: [
      { value: 'ok', label: 'نجاح', name: 'ins-alert--ok', desc: 'لما تم بنجاح.' },
      { value: 'info', label: 'معلومة', name: 'ins-alert--info', desc: 'لمعلومة يحسن أن يعرفها القارئ، بلون العلامة.' },
      { value: 'warn', label: 'تحذير', name: 'ins-alert--warn', desc: 'لما يستحق الانتباه قبل المتابعة.' },
      { value: 'bad', label: 'خطأ', name: 'ins-alert--bad', desc: 'لخطأ وقع، أو عملية تعذرت.' }
    ] },
    { key: 'text', label: 'النص', type: 'text', value: 'رسالة قصيرة للاطلاع.' },
    { key: 'action', label: 'مع رابط', type: 'switch' },
    { key: 'dismiss', label: 'يمكن إغلاقه', group: 'الإغلاق', type: 'switch', name: 'data-ins-dismiss', desc: 'ضعها على زر `ins-close` داخل الرسالة، فيُغلق التنبيه بضغطة. ويُخفى التنبيه ولا يُحذف، فتعيده بإزالة `hidden`.' }
  ],
  extra: [
    { group: 'الأساس', first: true, name: 'ins-alert', desc: 'أساس التنبيه، ومعه كلاس للنبرة. يدخل الصفحة منزلقًا من جهة البداية.' },
    { group: 'الأساس', name: 'ins-alert-ico', desc: 'الدائرة التي تحمل رمز التنبيه، وتستقر في تجويف الرسالة.' },
    { group: 'الأساس', name: 'ins-alert-msg', desc: 'الرسالة، وفيها النص وما يلحق به من رابط أو زر إغلاق.' },
    { group: 'الأساس', name: 'ins-alert-text', desc: 'نص التنبيه، ويأخذ ما بقي من عرض الرسالة.' },
    { group: 'الأساس', name: 'ins-alert-x', level: 'advanced', desc: 'زر إغلاق أقدم من `.ins-close` وباقٍ لمن استعمله، وتضع فيه الأيقونة بنفسك.' },
    { group: 'الإغلاق', name: 'ins:dismiss', kind: 'event', desc: 'يُطلق عند الإغلاق، وفي `detail` التنبيه نفسه `target`.' }
  ],
  events: ['ins:dismiss'],
  html: function (s) {
    var icon = { ok: 'check', info: 'info', warn: 'warn', bad: 'x' }[s.tone];
    var text = s.text + (s.action ? ' <a href="/details">التفاصيل</a>' : '');
    var close = s.dismiss ? '\n    <button class="ins-close ins-close--sm ins-close--bare" data-ins-dismiss aria-label="إغلاق التنبيه"></button>' : '';
    return '<div class="ins-alert ins-alert--' + s.tone + '">\n' +
      '  <span class="ins-alert-ico"><svg class="ico"><use href="#i-' + icon + '"/></svg></span>\n' +
      '  <div class="ins-alert-msg">\n' +
      '    <span class="ins-alert-text">' + text + '</span>' + close + '\n' +
      '  </div>\n' +
      '</div>';
  },
  notes: function (s) {
    var out = [];
    if (s.action) out.push('إن كان للمشكلة حل فدل عليه برابط أو زر صغير داخل النص.');
    if (s.dismiss) out.push('اضغط الزر فيُغلق التنبيه ويظهر هنا الحدث، وغيّر أي خيار لتعيده.');
    return out;
  }
});
