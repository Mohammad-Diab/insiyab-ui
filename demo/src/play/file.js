SitePlay.define({
  id: 'file',
  wide: true,
  title: 'منطقة الإفلات',
  owns: 'ins-file',
  controls: [
    { key: 'compact', label: 'مضغوط', type: 'switch', name: 'ins-file--compact', desc: 'على الحقل، إذا ضاق النموذج: زر وقائمة بلا منطقة إفلات، ويبقى الإفلات على الزر ممكنًا.' },
    { key: 'multiple', label: 'عدة ملفات', type: 'switch', value: true, name: 'multiple', desc: 'ينضم ما تختاره إلى القائمة، ومن دونها يحل الملف الجديد محل القديم.' },
    { key: 'accept', label: 'الأنواع المقبولة', group: 'الحدود', type: 'select', value: '.pdf,image/*', choices: [
      { value: '', label: 'كل الأنواع' },
      { value: '.pdf,image/*', label: 'PDF وصور', name: 'accept', desc: 'يفهمه الحقل كما يفهمه منتقي الملفات، ويطبقه على الإفلات واللصق أيضًا.' },
      { value: 'image/*', label: 'صور فقط' }
    ] },
    { key: 'max', label: 'أكبر حجم', group: 'الحدود', type: 'seg', value: '5MB', choices: [
      { value: '', label: 'بلا' },
      { value: '500KB', label: '500KB' },
      { value: '5MB', label: '5MB', name: 'data-ins-file-max="5MB"', desc: 'أكبر حجم للملف الواحد، بـ `B` أو `KB` أو `MB` أو `GB`.' }
    ] },
    { key: 'count', label: 'أكبر عدد', group: 'الحدود', type: 'seg', value: '5', choices: [
      { value: '', label: 'بلا' },
      { value: '2', label: '2' },
      { value: '5', label: '5', name: 'data-ins-file-count="5"', desc: 'أكبر عدد للملفات، مع `multiple`.' }
    ] },
    { key: 'note', label: 'السطر الصغير', type: 'text', value: '' },
    { key: 'upload', label: 'يُرفع فورًا', group: 'الرفع الفوري', type: 'switch', name: 'data-ins-file-upload', desc: 'يُطلق كل ملف تضيفه `ins:file` فترفعه الصفحة، ويرسل الحقل مكان الملفات معرفاتها في حقول مخفية باسمه.' },
    { key: 'disabled', label: 'معطل', type: 'switch', name: 'disabled', desc: 'يبهت الحقل، فلا تُختار فيه ملفات ولا تُفلت.' }
  ],
  extra: [
    { group: 'الأساس', first: true, name: 'data-ins-file', desc: 'على `<input type="file">`: منطقة تسحب الملفات إليها أو تختارها أو تلصقها، وقائمة بها تحتها، وتبقى الملفات في الحقل نفسه.' },
    { group: 'الأساس', name: 'ins-file', desc: 'الكتلة التي يلف بها السكربت الحقل، فلا تكتبها بنفسك.' },
    { group: 'الحدود', name: 'data-ins-file-note', desc: 'السطر الصغير في المنطقة، مكان ما يكتبه الحقل من الحدود.' },
    { group: 'الرفع الفوري', name: 'ins:file', kind: 'event', desc: 'يُطلق لكل ملف يُضاف، وفي `detail` الحقل `input` والملف `file` وسطره في القائمة `item`، و`upload` إن كان الرفع للصفحة.' }
  ],
  disable: function (s) { return s.multiple ? [] : ['count']; },
  events: ['ins:file'],
  html: function (s) {
    var attrs = (s.compact ? ' class="ins-file--compact"' : '') + ' name="docs"' + (s.multiple ? ' multiple' : '') +
      (s.accept ? ' accept="' + s.accept + '"' : '') + ' data-ins-file' + (s.max ? ' data-ins-file-max="' + s.max + '"' : '') +
      (s.count && s.multiple ? ' data-ins-file-count="' + s.count + '"' : '') + (s.note ? ' data-ins-file-note="' + s.note + '"' : '') +
      (s.upload ? ' data-ins-file-upload' : '') + (s.disabled ? ' disabled' : '');
    return '<div class="ins-field">\n' +
      '  <label class="ins-label" for="fl-play">المرفقات</label>\n' +
      '  <input type="file" id="fl-play"' + attrs + '>\n' +
      '  <div class="ins-hint">' + { '': 'أي نوع من الملفات.', '.pdf,image/*': 'ملفات PDF أو صور.', 'image/*': 'صور فقط.' }[s.accept] + '</div>\n' +
      '</div>';
  },
  stage: function (view, s) {
    var zone = view.querySelector('.ins-file-zone');
    if (!s.upload || s.disabled || !zone || !window.DataTransfer) return;
    var png = atob('iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mNk+M9QDwADhgGAWjR9awAAAABJRU5ErkJggg=='), bytes = new Uint8Array(png.length);
    for (var i = 0; i < png.length; i++) bytes[i] = png.charCodeAt(i);
    var dt = new DataTransfer();
    dt.items.add(new File([bytes], 'صورة.png', { type: 'image/png' }));
    zone.dispatchEvent(new DragEvent('drop', { dataTransfer: dt, bubbles: true, cancelable: true }));
  },
  notes: function (s) {
    var out = [];
    if (s.upload) out.push('هذه صورة أُضيفت للتو وهي تُرفع. والرفع هنا مصطنع، وكل ملف في اسمه «خطأ» يتعذر رفعه عمدًا.');
    if (!s.multiple) out.push('الحقل لملف واحد، فلا معنى لأكبر عدد.');
    return out;
  }
});
