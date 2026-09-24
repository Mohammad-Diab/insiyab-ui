// The timeline plugin: the dot on the start side (the right, in Arabic), the rule
// between dots and not past the last, the tones and states, the icon chip, the
// split layout by container width, and the relative times — minutes, hours,
// yesterday, the future, past a week as a date, a date-only value, English, and
// ar-SA kept Gregorian.
import { launch, BASE } from './lib/cdp.mjs';
import { suite } from './lib/check.mjs';

const { ok, end } = suite();
const b = await launch();
const E = (js) => b.evaluate(js);

await b.size(1280, 900);
await b.goto(`${BASE}/test/fixtures/timeline.html`);

/* Times a known distance from now, written in, then rendered. */
await E(`(() => {
  const at = (id, ms, mode) => { const el = document.getElementById(id); el.setAttribute('datetime', new Date(Date.now() + ms).toISOString()); el.setAttribute('data-ins-time', mode || ''); };
  at('t1', -5 * 60000); at('t2', -2 * 3600000);
  at('r-yesterday', -26 * 3600000); at('r-hours', -3 * 3600000); at('r-now', -10000); at('r-soon', 2 * 3600000);
  at('r-old', -20 * 86400000); at('r-date', -60000, 'date'); at('r-en', -5 * 60000);
  const d = new Date(Date.now() - 86400000); const iso = d.getFullYear() + '-' + String(d.getMonth() + 1).padStart(2, '0') + '-' + String(d.getDate()).padStart(2, '0');
  const day = document.getElementById('r-day'); day.setAttribute('datetime', iso); day.setAttribute('data-ins-time', '');
  const sa = document.getElementById('r-sa'); sa.setAttribute('datetime', '2026-01-15T10:00:00'); sa.setAttribute('data-ins-time', 'date');
  return 0;
})()`);
const wrote = await E(`Insiyab.time()`);
const t = (id) => E(`document.getElementById('${id}').textContent`);

// ------------------------------------------------------------------ the look
const geo = await E(`(() => {
  const item = document.getElementById('t-ok'), r = item.getBoundingClientRect(), dot = getComputedStyle(item, '::before'), rule = getComputedStyle(item, '::after');
  const last = getComputedStyle(document.getElementById('t-pend'), '::after');
  return { dotRight: parseFloat(dot.right), dotLeft: dot.left, w: r.width, dotColor: dot.backgroundColor, rule: rule.content, last: last.content,
           pend: getComputedStyle(document.getElementById('t-pend'), '::before').borderTopStyle,
           cur: getComputedStyle(document.getElementById('t-cur'), '::before').boxShadow,
           ico: getComputedStyle(document.getElementById('t-ico'), '::before').content, icoColor: getComputedStyle(document.querySelector('#t-ico .ins-timeline-ico')).color };
})()`);
ok('in Arabic the dot sits on the right, the start side', geo.dotRight === 0, JSON.stringify(geo));
const rgb = (c) => (c.match(/[\d.]+/g) || []).map(Number);
ok('the ok tone colours its dot green', ((c) => c[1] > c[0] && c[1] > c[2])(rgb(geo.dotColor)), geo.dotColor);
ok('a rule runs from each dot to the next', geo.rule === '""');
ok('and not past the last', geo.last === 'none');
ok('a pending step is a hollow dot', geo.pend === 'solid');
ok('the current step wears a halo', /3px.*6px/.test(geo.cur), geo.cur);
ok('an icon chip replaces the dot, in the tone', geo.ico === 'none' && ((c) => c[0] > c[1] && c[0] > c[2])(rgb(geo.icoColor)), `${geo.ico} ${geo.icoColor}`);
const split = await E(`(() => { const x = (id) => document.getElementById(id).getBoundingClientRect(); const box = document.getElementById('tl-split').getBoundingClientRect();
  const n1 = x('nr1'), n2 = x('nr2');
  /* Right to left: the first item on the end side, the left half. */
  const mid = box.left + box.width / 2;
  return { a: x('sp1').right <= mid + 2, b: x('sp2').left >= mid - 2, c: x('sp3').right <= mid + 2, narrow: n1.left === n2.left && n1.width === n2.width }; })()`);
ok('--split alternates sides of a centre rule when there is room', split.a && split.b && split.c, JSON.stringify(split));
ok('and stays one-sided in a narrow container', split.narrow);

// ------------------------------------------------------------------ relative times
ok('Insiyab.time() writes every time on the page', wrote === 11, wrote);
ok('minutes ago, in Arabic, in Latin digits', (await t('t1')) === 'قبل 5 دقائق', await t('t1'));
ok('hours ago', (await t('r-hours')) === 'قبل 3 ساعات', await t('r-hours'));
ok('a day and more ago is "yesterday"', (await t('r-yesterday')) === 'أمس', await t('r-yesterday'));
ok('seconds ago is "now"', (await t('r-now')) === 'الآن', await t('r-now'));
ok('the future too', (await t('r-soon')) === 'خلال ساعتين', await t('r-soon'));
ok('past a week, the date instead', /^\d{1,2} [^\d]+( \d{4})?$/.test(await t('r-old')) && !/قبل/.test(await t('r-old')), await t('r-old'));
ok('data-ins-time="date" always writes the date', !/قبل|الآن/.test(await t('r-date')) && /\d/.test(await t('r-date')), await t('r-date'));
ok('a date-only value is compared as a day', (await t('r-day')) === 'أمس', await t('r-day'));
ok('the full date and time is the title', /\d{4}/.test(await E(`document.getElementById('t1').title`)), await E(`document.getElementById('t1').title`));
ok('in English where the page is English', (await t('r-en')) === '5 minutes ago', await t('r-en'));
ok('ar-SA still writes a Gregorian date', /يناير|كانون/.test(await t('r-sa')) && !/رجب|شعبان|رمضان/.test(await t('r-sa')), await t('r-sa'));

ok('no console errors, warnings or exceptions', b.logs.length === 0, b.logs.join(' | '));
await b.close();
end();
