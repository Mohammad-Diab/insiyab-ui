SitePlay.define({
  id: 'save',
  title: 'حالة الحفظ',
  owns: 'ins-save',
  wide: function (s) { return s.auto; },
  controls: [
    { key: 'auto', label: 'في نموذج يحفظ نفسه', group: 'الحفظ التلقائي', type: 'switch', name: 'data-ins-autosave', desc: 'على النموذج، فإذا مضت لحظة على آخر تغيير أظهر المؤشر الذي فيه «جارٍ الحفظ» وأطلق `ins:autosave`. وقيمتها مدة الانتظار بالميلي ثانية، وهي `800` إن لم تذكرها.' },
    { key: 'state', label: 'الحالة', type: 'select', value: 'saved', choices: [
      { value: '', label: 'بلا حالة' },
      { value: 'saving', label: 'جارٍ الحفظ', name: 'data-ins-state="saving"', desc: 'التغيير في طريقه إلى الحفظ.' },
      { value: 'saved', label: 'حُفظ', name: 'data-ins-state="saved"', desc: 'حُفظ آخر تغيير.' },
      { value: 'error', label: 'تعذر', name: 'data-ins-state="error"', desc: 'تعذر حفظ آخر تغيير.' }
    ] }
  ],
  extra: [
    { group: 'الأساس', first: true, name: 'ins-save', desc: 'يخبر بمصير آخر تغيير. فإن خلا من الحالة حجز مكانه فارغًا، حتى إذا ظهر لم يقفز السطر. ومن جافاسكربت: `Insiyab.saveState(el, \'saved\')`، ولك أن تضع كلماتك في `data-ins-text-saved` وأخواتها.' },
    { group: 'الحفظ التلقائي', name: 'ins:autosave', kind: 'event', desc: 'يحمل في `detail` النموذج `el` وبياناته `data` والدالة `done`. فإن كنت ستحفظ على الخادم فاستدع `preventDefault()`، ثم `done(true)` أو `done(false)` حين تنتهي، وإلا عُدَّ التغيير محفوظًا في الحال.' },
    { group: 'الحفظ التلقائي', name: 'data-ins-save-for', desc: 'على مؤشر خارج النموذج، مثل `data-ins-save-for="#form"`، فيخبر بحفظ ذلك النموذج.' }
  ],
  disable: function (s) { return s.auto ? ['state'] : []; },
  html: function (s) {
    if (!s.auto) return '<span class="ins-save"' + (s.state ? ' data-ins-state="' + s.state + '"' : '') + '></span>';
    return '<form class="ins-field" data-ins-autosave onsubmit="event.preventDefault()">\n' +
      '  <div class="ins-flex ins-items-center ins-justify-between">\n' +
      '    <label class="ins-label" for="as-note">ملاحظة</label>\n' +
      '    <span class="ins-save"></span>\n' +
      '  </div>\n' +
      '  <textarea class="ins-textarea" id="as-note"></textarea>\n' +
      '</form>';
  },
  notes: function (s) {
    if (s.auto) return ['اكتب في الحقل ثم توقف لحظة، فيظهر «جارٍ الحفظ» ثم «حُفظ».'];
    return s.state ? [] : ['لا حالة، فالمؤشر فارغ يحجز مكانه.'];
  }
});
