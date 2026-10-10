SitePlay.define({
  id: 'carousel',
  title: 'عرض الشرائح',
  owns: 'ins-carousel',
  wide: true,
  events: ['ins:carousel'],
  controls: [
    { key: 'per', label: 'شرائح معًا', type: 'seg', value: '', choices: [
      { value: '', label: 'واحدة' },
      { value: '2', label: 'اثنتان', name: 'ins-carousel--2', desc: 'شريحتان معًا في حاوية عرضها `36rem` فأكثر، وإلا فواحدة. فالحاوية هي التي تقرر لا الشاشة.' },
      { value: '3', label: 'ثلاث', name: 'ins-carousel--3', desc: 'ثلاث شرائح معًا في حاوية عرضها `56rem` فأكثر، واثنتان تحت ذلك، وواحدة تحت `36rem`.' }
    ] },
    { key: 'count', label: 'عدد الشرائح', type: 'seg', value: '4', choices: [
      { value: '1', label: '1' },
      { value: '4', label: '4' },
      { value: '6', label: '6' }
    ] },
    { key: 'peek', label: 'طرف التالية', type: 'switch', name: 'ins-carousel--peek', desc: 'يكشف طرف الشريحة التالية، فيعرف المستخدم أن بعدها المزيد.' },
    { key: 'dots', label: 'بلا نقاط', type: 'switch', name: 'data-ins-carousel-dots="off"', desc: 'يخفي النقاط تحت العرض، ويبقى الزران.' }
  ],
  extra: [
    { group: 'الأساس', first: true, name: 'data-ins-carousel', desc: 'يجعل العنصر عرض شرائح، وكل ابن له شريحة. ويضبط `--ins-carousel-gap` المسافة بين الشرائح.' },
    { group: 'الأساس', name: 'ins-carousel', desc: 'كلاس العرض، ويضيفه السكربت إن لم تكتبه، ومعه `aria-label` يسمي العرض.' },
    { group: 'الأساس', name: 'ins-carousel-slide', desc: 'يضيفه السكربت لكل ابن، وإذا كانت الشريحة صورة `<img>` ملأت عرضها.' },
    { group: 'الأساس', name: 'ins:carousel', kind: 'event', desc: 'يُطلق كلما تغيرت الشريحة في أول العرض، وفي `detail` موضعها `index`.' }
  ],
  html: function (s) {
    var words = ['الأولى', 'الثانية', 'الثالثة', 'الرابعة', 'الخامسة', 'السادسة'];
    var cls = 'ins-carousel' + (s.per ? ' ins-carousel--' + s.per : '') + (s.peek ? ' ins-carousel--peek' : '');
    var slides = words.slice(0, +s.count).map(function (w, i) {
      return '  <div class="car-demo car-demo--' + (i % 4 + 1) + '"><b>' + w + '</b></div>';
    }).join('\n');
    return '<div class="' + cls + '" data-ins-carousel' + (s.dots ? ' data-ins-carousel-dots="off"' : '') + ' aria-label="شرائح">\n' + slides + '\n</div>';
  },
  notes: function (s) {
    if (s.count === '1') return ['شريحة واحدة لا تحتاج زرين ولا نقاطًا، فأخفاها السكربت.'];
    if (s.per === '2') return ['لا تجتمع شريحتان في الصف إلا في حاوية عرضها `36rem` فأكثر، وهذه المعاينة أضيق من ذلك، فتبقى فيها شريحة واحدة.'];
    if (s.per === '3') return ['لا تجتمع ثلاث شرائح في الصف إلا في حاوية عرضها `56rem` فأكثر، واثنتان من `36rem`. وهذه المعاينة أضيق من ذلك، فتبقى فيها شريحة واحدة.'];
    return ['انتقل إلى شريحة أخرى، فيظهر هنا الحدث الذي يطلقه العرض.'];
  }
});
