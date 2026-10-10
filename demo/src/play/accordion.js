SitePlay.define({
  id: 'accordion',
  title: 'الأكورديون',
  owns: 'ins-accordion',
  wide: true,
  controls: [
    { key: 'one', label: 'قسم واحد مفتوح', type: 'switch', name: 'name="…"', desc: 'أعط الأقسام كلها `name` واحدًا، فإذا فتحت أحدها أغلق المتصفح الباقي.' },
    { key: 'open', label: 'الأول مفتوح', type: 'switch', value: true },
    { key: 'panel', label: 'في لوح', type: 'switch', value: true }
  ],
  extra: [
    { group: 'الأساس', first: true, name: 'ins-accordion', desc: 'يجمع أقسامًا من `.ins-collapse` ويفصل بينها بخط رفيع. ولا سطح له، فضعه في لوح `.ins-panel` فتستقيم زوايا أقسامه على زواياه.' }
  ],
  html: function (s) {
    var name = s.one ? ' name="group"' : '';
    var pad = s.panel ? '    ' : '  ';
    var words = ['الأول', 'الثاني', 'الثالث'];
    var items = words.map(function (w, i) {
      return pad + '<details class="ins-collapse"' + name + (i === 0 && s.open ? ' open' : '') + '>\n' +
        pad + '  <summary>القسم ' + w + '</summary>\n' +
        pad + '  <div class="ins-collapse-body">محتوى القسم ' + w + '.</div>\n' +
        pad + '</details>';
    }).join('\n');
    var acc = (s.panel ? '  ' : '') + '<div class="ins-accordion">\n' + items + '\n' + (s.panel ? '  ' : '') + '</div>';
    return s.panel ? '<div class="ins-panel">\n' + acc + '\n</div>' : acc;
  },
  // The third section opened, as if the reader had clicked it, so what `name` closes can be seen.
  stage: function (view) {
    var all = view.querySelectorAll('details.ins-collapse');
    all[all.length - 1].open = true;
  },
  notes: function (s) {
    return ['في المعاينة فُتح القسم الثالث كأنك نقرته' + (s.one ? '، فأغلق المتصفح ما كان مفتوحًا قبله.' : '، وبقي ما كان مفتوحًا قبله. فعّل «قسم واحد مفتوح» ليُغلق.')];
  }
});

SitePlay.define({
  id: 'collapse',
  title: 'القسم المنفرد',
  owns: 'ins-collapse',
  wide: true,
  controls: [
    { key: 'title', label: 'العنوان', type: 'text', value: 'خيارات متقدمة' },
    { key: 'open', label: 'مفتوح', type: 'switch', name: 'open', desc: 'يبدأ القسم مفتوحًا.' }
  ],
  extra: [
    { group: 'الأساس', first: true, name: 'ins-collapse', desc: 'على `<details>`، فينفتح بالنقر وبلوحة المفاتيح دون جافاسكربت، ويصلح وحده أو داخل `.ins-accordion`.' },
    { group: 'الأساس', name: 'summary', kind: 'element', desc: 'عنوان القسم، وبالنقر عليه ينفتح وينغلق.' },
    { group: 'الأساس', name: 'ins-collapse-body', desc: 'محتوى القسم.' }
  ],
  html: function (s) {
    return '<details class="ins-collapse"' + (s.open ? ' open' : '') + '>\n' +
      '  <summary>' + s.title + '</summary>\n' +
      '  <div class="ins-collapse-body">محتوى إضافي.</div>\n' +
      '</details>';
  }
});
