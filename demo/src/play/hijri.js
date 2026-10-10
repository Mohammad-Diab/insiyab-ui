SitePlay.define({
  id: 'field',
  title: 'الحقل الهجري',
  controls: [
    { key: 'range', label: 'فترة بين تاريخين', type: 'switch' },
    { key: 'civil', label: 'الحسابي', group: 'التقويم', type: 'switch', level: 'advanced', name: 'data-ins-calendar="hijri-civil"', desc: 'التقويم الهجري الحسابي (الجدولي)، وقد يفترق عن أم القرى بيوم أو يومين.' }
  ],
  extra: [
    { group: 'التقويم', first: true, name: 'data-ins-calendar="hijri"', desc: 'على حقل تاريخ `data-ins-date` فيصير هجريًا بتقويم أم القرى، وفي أسفل تقويمه مفتاح يقلبه إلى الميلادي ثم يعيده.' },
    { group: 'التقويم', name: 'html[data-ins-calendar]', kind: 'element', desc: 'ضع السمة على `<html>` يصر كل حقل تاريخ في الصفحة هجريًا.' },
    { group: 'التقويم', name: 'data-ins-calendar="gregory"', desc: 'يبقي حقلًا بعينه ميلاديًا وإن كانت الصفحة كلها هجرية.' },
    { group: 'التقويم', name: 'ins:calendar', kind: 'event', level: 'advanced', desc: 'يُطلق إذا قُلب التقويم بالمفتاح، وفي `detail` الحقل `input` والتقويم الظاهر `calendar`.' }
  ],
  events: ['ins:calendar'],
  html: function (s) {
    var cal = s.civil ? 'hijri-civil' : 'hijri';
    var input = s.range
      ? '<input class="ins-input" id="hj-play" type="date" data-ins-date data-ins-calendar="' + cal + '" data-ins-date-range="to" name="from" value="2026-09-12" data-ins-date-to="2026-10-11">'
      : '<input class="ins-input" id="hj-play" type="date" data-ins-date data-ins-calendar="' + cal + '" name="date" value="2026-09-24">';
    return '<div class="ins-field">\n' +
      '  <label class="ins-label" for="hj-play">' + (s.range ? 'الفترة' : 'التاريخ') + '</label>\n' +
      '  <div class="ins-input-group">\n' +
      '    ' + input + '\n' +
      '  </div>\n' +
      '</div>';
  },
  notes: function (s) {
    var out = [];
    if (s.range) out.push('يُرسل طرفا الفترة ميلاديين، وتأتي رسائلها بالتقويم الظاهر.');
    if (s.civil) out.push('قد يختلف اليوم هنا عن أم القرى بيوم.');
    return out;
  }
});
