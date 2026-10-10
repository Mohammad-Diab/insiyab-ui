SitePlay.define({
  id: 'dl',
  title: 'قائمة الوصف',
  owns: 'ins-dl',
  wide: true,
  controls: [
    { key: 'grid', label: 'في أعمدة', type: 'switch', name: 'ins-dl--grid', desc: 'يوزع الأزواج على أعمدة تعلو فيها التسمية قيمتها، لملخص يجمع حقائق قصيرة كثيرة.' },
    { key: 'count', label: 'عدد الأزواج', type: 'seg', value: '3', choices: [
      { value: '3', label: '3' },
      { value: '6', label: '6' }
    ] },
    { key: 'num', label: 'محاذاة الرقم', type: 'switch', value: true, name: 'ins-num', desc: 'على `<dd>` يحمل رقمًا، فيبقى على اتجاهه فلا تنتقل إشارة السالب إلى آخره، ويبدأ من حيث تبدأ القيم الأخرى.' }
  ],
  extra: [
    { group: 'الأساس', first: true, name: 'ins-dl', desc: 'على `<dl>`، ولكل زوج من `<dt>` و`<dd>` عنصر `<div>` خاص به، فتصطف التسميات في عمود وتقف القيم بجوارها.' }
  ],
  html: function (s) {
    var pairs = [
      ['الاسم', 'سلمى'],
      ['الفرق', '-12', true],
      ['الحالة', 'مفتوح'],
      ['التاريخ', '26 سبتمبر 2026'],
      ['المدة', '14 يومًا'],
      ['ملاحظات', 'رسالة قصيرة']
    ].slice(0, +s.count);
    var rows = pairs.map(function (p) {
      return '  <div><dt>' + p[0] + '</dt><dd' + (p[2] && s.num ? ' class="ins-num"' : '') + '>' + p[1] + '</dd></div>';
    });
    return '<dl class="ins-dl' + (s.grid ? ' ins-dl--grid' : '') + '">\n' + rows.join('\n') + '\n</dl>';
  },
  notes: function (s) {
    return s.grid ? [] : ['تقيس القائمة عرضها هي لا عرض الشاشة، فإذا ضاق مكانها عن 26rem نزلت كل تسمية فوق قيمتها.'];
  }
});
