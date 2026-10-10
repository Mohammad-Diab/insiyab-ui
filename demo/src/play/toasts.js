SitePlay.define({
  id: 'toast',
  title: 'الرسالة العابرة',
  owns: 'ins-toast',
  controls: [
    { key: 'how', label: 'تظهر', type: 'seg', value: 'attr', choices: [
      { value: 'attr', label: 'بسمة على الزر', name: 'data-ins-toast', desc: 'ضعها على زر فتظهر الرسالة حين يُضغط، وقيمتها نص الرسالة. ومن جافاسكربت تستدعي `Insiyab.toast(text, tone)`.' },
      { value: 'js', label: 'بجافاسكربت' }
    ] },
    { key: 'tone', label: 'النبرة', type: 'select', value: 'ok', choices: [
      { value: 'ok', label: 'نجاح', name: 'data-ins-tone="ok"', desc: 'لما تم للتو بفعل المستخدم: حُفظ، أُرسل، نُسخ.' },
      { value: 'info', label: 'معلومة', name: 'data-ins-tone="info"', desc: 'لمعلومة عابرة، وهي النبرة إن لم تذكر غيرها.' },
      { value: 'warn', label: 'تحذير', name: 'data-ins-tone="warn"', desc: 'لما يستحق الانتباه ولا يوقف العمل.' },
      { value: 'bad', label: 'خطأ', name: 'data-ins-tone="bad"', desc: 'لعملية تعذرت.' }
    ] },
    { key: 'text', label: 'النص', type: 'text', value: 'تم الحفظ' }
  ],
  extra: [
    { group: 'الأساس', first: true, name: 'ins-toast', desc: 'الرسالة نفسها، تبنيها المكتبة فلا تكتبها بنفسك. تطل من أعلى الصفحة وتغيب وحدها بعد خمس ثوانٍ، أو حين تُنقر.' },
    { group: 'الأساس', name: 'ins-toasts', desc: 'الشريط الذي تصطف فيه الرسائل، تبنيه المكتبة مرة واحدة، ويقرأ قارئ الشاشة ما فيه دون أن ينتزع التركيز.' }
  ],
  html: function (s) {
    var btn = '<button class="ins-btn ins-btn--secondary"';
    if (s.how === 'js') {
      var text = s.text.replace(/\\/g, '\\\\').replace(/'/g, '\\\'');
      return btn + ' onclick="Insiyab.toast(\'' + text + '\', \'' + s.tone + '\')">اعرض الرسالة</button>';
    }
    return btn + ' data-ins-toast="' + s.text + '" data-ins-tone="' + s.tone + '">اعرض الرسالة</button>';
  },
  notes: function () { return ['اضغط الزر لترى الرسالة بخياراتك.']; }
});
