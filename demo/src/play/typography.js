SitePlay.define({
  id: 'heading',
  title: 'العنوان',
  owns: 'ins-(?:display|h[1-4]|eyebrow|lead)',
  controls: [
    { key: 'size', label: 'المقاس', group: 'المقاس', type: 'select', value: 'h2', choices: [
      { value: 'display', label: 'عنوان الواجهة', name: 'ins-display', desc: 'أكبر العناوين، لعنوان واحد يتصدر صفحة أو واجهة.' },
      { value: 'h1', label: 'أول', name: 'ins-h1', desc: 'بمقاس العنوان الأول.' },
      { value: 'h2', label: 'ثان', name: 'ins-h2', desc: 'بمقاس العنوان الثاني.' },
      { value: 'h3', label: 'ثالث', name: 'ins-h3', desc: 'بمقاس العنوان الثالث.' },
      { value: 'h4', label: 'رابع', name: 'ins-h4', desc: 'بمقاس العنوان الرابع.' }
    ] },
    { key: 'tag', label: 'العنصر', type: 'seg', value: 'h2', choices: [
      { value: 'h1', label: 'h1' },
      { value: 'h2', label: 'h2' },
      { value: 'h3', label: 'h3' },
      { value: 'p', label: 'p' }
    ] },
    { key: 'text', label: 'النص', type: 'text', value: 'الإعدادات' },
    { key: 'eyebrow', label: 'عنوان صغير فوقه', type: 'switch', name: 'ins-eyebrow', desc: 'عنوان صغير بلون الهوية يعلو العنوان، يسمي القسم الذي هو فيه.' },
    { key: 'lead', label: 'تمهيد تحته', type: 'switch', name: 'ins-lead', desc: 'فقرة التمهيد التي تلي العنوان، بخط أكبر من المتن ولون أهدأ.' }
  ],
  html: function (s) {
    var out = [];
    if (s.eyebrow) out.push('<span class="ins-eyebrow">الحساب</span>');
    out.push('<' + s.tag + ' class="ins-' + s.size + '">' + s.text + '</' + s.tag + '>');
    if (s.lead) out.push('<p class="ins-lead">الاسم والبريد وكلمة المرور.</p>');
    if (out.length === 1) return out[0];
    return '<div>\n  ' + out.join('\n  ') + '\n</div>';
  },
  notes: function (s) {
    var level = s.tag === 'p' ? '' : s.tag;
    if (!level) return ['فقرة بهيئة عنوان، لا يعدها قارئ الشاشة عنوانًا.'];
    return level.slice(1) !== s.size.slice(1) ? ['المستوى `' + level + '` لبنية الصفحة، والمقاس من الكلاس، ولا يلزم أن يتفقا.'] : [];
  }
});

SitePlay.define({
  id: 'prose',
  title: 'النص الطويل',
  owns: 'ins-prose',
  wide: true,
  controls: [
    { key: 'list', label: 'قائمة', type: 'switch', name: 'ul, ol', kind: 'element', desc: 'قوائم بعلامات بلون الهوية، ويتداخل بعضها في بعض.' },
    { key: 'quote', label: 'اقتباس', type: 'switch', name: 'blockquote', kind: 'element', desc: 'اقتباس بشريط على حافته في جهة البداية وخلفية هادئة، ولا يميل.' },
    { key: 'code', label: 'شيفرة', type: 'switch', name: 'pre', kind: 'element', desc: 'كتلة شيفرة تبدأ من اليسار في الاتجاهين، وتتمرر أفقيًا إذا طالت أسطرها.' },
    { key: 'table', label: 'جدول', type: 'switch', name: 'table', kind: 'element', desc: 'جدول بسيط بخطوط وحسب، للجدول الذي يأتي في وسط المقال. أما جدول البيانات فهو `.ins-table`.' },
    { key: 'dl', label: 'قائمة وصف', type: 'switch', name: 'dl', kind: 'element', desc: 'تسمية عريضة وقيمتها تحتها بلون أهدأ.' }
  ],
  extra: [
    { group: 'الأساس', first: true, name: 'ins-prose', desc: 'ينسق ما في داخله من نص طويل، كمقال أو صفحة مساعدة، ولا يمس شيئًا خارجه. ويحد عرض السطر ليسهل على العين.' },
    { group: 'في النص', name: 'h1, h2, h3, h4', kind: 'element', desc: 'بمقاسات `.ins-h1` إلى `.ins-h4`، وأقرب إلى ما بعدها منها إلى ما قبلها.' },
    { group: 'في النص', name: 'a', kind: 'element', desc: 'روابط تحتها خط، بعيد عن نقاط الحروف العربية.' },
    { group: 'في النص', name: 'em', kind: 'element', desc: 'توكيد باللون لا بالميل، فلا يميل الحرف العربي.' },
    { group: 'في النص', name: 'mark', kind: 'element', desc: 'تظليل بلون الهوية الخفيف.' },
    { group: 'في النص', name: 'code', kind: 'element', desc: 'شيفرة قصيرة في السطر، معزولة عن اتجاهه فلا تنقلب في الجملة العربية.' },
    { group: 'في النص', name: 'kbd', kind: 'element', desc: 'مفتاح بهيئة `.ins-kbd`، دون كلاس.' },
    { group: 'في النص', name: 'hr', kind: 'element', desc: 'خط فاصل بين قسمين.' },
    { group: 'في النص', name: 'figure', kind: 'element', desc: 'صورة أو فيديو بزوايا مستديرة لا يتجاوز عرض النص، ومعه `<figcaption>` بخط أصغر.' }
  ],
  html: function (s) {
    var out = ['<article class="ins-prose">', '  <h2>عنوان</h2>',
      '  <p>فقرة فيها <strong>نص عريض</strong> و<em>توكيد</em> و<mark>تظليل</mark> و<code>code</code> و<a href="#prose">رابط</a>.</p>'];
    if (s.list) out.push('  <ol>', '    <li>البند الأول</li>', '    <li>البند الثاني', '      <ul>', '        <li>بند متداخل</li>', '      </ul>', '    </li>', '  </ol>');
    if (s.quote) out.push('  <blockquote>اقتباس قصير.</blockquote>');
    if (s.code) out.push('  <pre><code>GET /api/items</code></pre>');
    if (s.table) out.push('  <table>', '    <thead><tr><th>الاسم</th><th>القيمة</th></tr></thead>', '    <tbody><tr><td>الأول</td><td>12</td></tr><tr><td>الثاني</td><td>7</td></tr></tbody>', '  </table>');
    if (s.dl) out.push('  <dl>', '    <dt>المدة</dt><dd>14 يومًا</dd>', '  </dl>');
    out.push('</article>');
    return out.join('\n');
  }
});

SitePlay.define({
  id: 'divider',
  title: 'الفاصل',
  owns: 'ins-divider',
  wide: true,
  controls: [
    { key: 'v', label: 'عمودي', group: 'فاصل عمودي', type: 'switch', name: 'ins-divider--v', desc: 'فاصل عمودي بين عناصر صف، بطول الصف نفسه.' },
    { key: 'text', label: 'النص', type: 'text', value: 'أو' },
    { key: 'start', label: 'النص في البداية', type: 'switch', name: 'ins-divider--start', desc: 'يضع النص في البداية، فيصير عنوان قسم يمتد بعده الخط.' }
  ],
  extra: [
    { group: 'الأساس', first: true, name: 'ins-divider', desc: 'على `<hr>` فيكون خطًا، أو على عنصر فيه نص فيكون النص بين خطين. ولا تضع `role="separator"` على فاصل فيه نص، فقارئ الشاشة يتجاهل ما في داخل الفاصل.' },
    { group: 'فاصل عمودي', name: 'aria-orientation="vertical"', desc: 'ضعها على `<hr>` العمودي، فيعرف قارئ الشاشة اتجاهه.' }
  ],
  disable: function (s) { return s.v ? ['text', 'start'] : s.text ? [] : ['start']; },
  html: function (s) {
    if (s.v) {
      return '<div class="ins-row">\n  <span>ملف</span>\n  <hr class="ins-divider ins-divider--v" aria-orientation="vertical">\n  <span>تعديل</span>\n</div>';
    }
    var cls = 'ins-divider' + (s.start ? ' ins-divider--start' : '');
    return s.text ? '<div class="' + cls + '">' + s.text + '</div>' : '<hr class="' + cls + '">';
  }
});

SitePlay.define({
  id: 'kbd',
  title: 'المفتاح',
  owns: 'ins-kbd',
  controls: [
    { key: 'key', label: 'المفتاح', type: 'text', value: 'K', level: 'advanced' },
    { key: 'ctrl', label: 'مع Ctrl', type: 'switch', value: true, level: 'advanced' }
  ],
  extra: [
    { group: 'الأساس', first: true, name: 'ins-kbd', level: 'advanced', desc: 'على `<kbd>`: مفتاح بحافة سفلية أثقل، يُكتب اسمه من اليسار ولو كان في جملة عربية. وداخل `.ins-prose` يكفي `<kbd>` وحده.' }
  ],
  html: function (s) {
    var keys = (s.ctrl ? '<kbd class="ins-kbd">Ctrl</kbd> + ' : '') + '<kbd class="ins-kbd">' + s.key + '</kbd>';
    return '<p>اضغط ' + keys + ' للبحث.</p>';
  }
});
