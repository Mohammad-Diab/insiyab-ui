// The Hijri plugin: the field and the grid in Umm al-Qura (or the civil calendar),
// a footer switch to Gregorian and back, typed Hijri dates, bounds, the page-wide
// attribute, a plugin that loads late, and one that never loads. What the form sends
// stays Gregorian ISO throughout.
import { launch, BASE } from './lib/cdp.mjs';
import { suite } from './lib/check.mjs';

const { ok, end } = suite();
const b = await launch();
const E = (js) => b.evaluate(js);

/* An independent answer, from Intl directly and not from the plugin: the Gregorian
   ISO day whose Umm al-Qura (or civil) date is y/m/d, by walking the days. */
const ORACLE = `window.__g = (y, m, d, cal = 'islamic-umalqura') => {
  const f = new Intl.DateTimeFormat('en', { calendar: cal, numberingSystem: 'latn', day: 'numeric', month: 'numeric', year: 'numeric' });
  for (let i = -800; i < 1200; i++) {
    const x = new Date(2026, 0, 1 + i);
    const p = Object.fromEntries(f.formatToParts(x).map((q) => [q.type, q.value]));
    if (+p.year === y && +p.month === m && +p.day === d) return x.getFullYear() + '-' + String(x.getMonth() + 1).padStart(2, '0') + '-' + String(x.getDate()).padStart(2, '0');
  }
  return null;
}; 0`;

const field = (id) => E(`(() => { const i = document.getElementById(${JSON.stringify(id)});
  const h = document.querySelector('input[type=hidden][name=' + ${JSON.stringify(id === 'h-date' ? 'issued' : id === 'h-min' ? 'bounded' : id.slice(2))} + ']');
  return { text: i.value, hidden: h ? h.value : null, valid: i.validity.valid, msg: i.validationMessage, view: i.getAttribute('data-ins-calendar-view') }; })()`);
const cal = () => E(`(() => { const c = document.querySelector('.ins-cal'); if (!c || !c.matches(':popover-open')) return null;
  const month = c.querySelector('[data-ins-cal=month]'), year = c.querySelector('[data-ins-cal=year]');
  const inMonth = [...c.querySelectorAll('.ins-cal-day:not(.is-outside)')];
  const f = document.activeElement;
  return { month: month.options[month.selectedIndex].text, year: year.options[year.selectedIndex].text,
           first: inMonth[0].dataset.date, firstText: inMonth[0].textContent, count: inMonth.length,
           focus: f.dataset && f.dataset.date, focusText: f.textContent, focusLabel: f.getAttribute('aria-label'),
           sw: [...c.querySelectorAll('[data-ins-cal=system]')].map((x) => x.textContent + (x.classList.contains('is-active') ? '*' : '')).join('|'),
           disabledBefore: !!c.querySelector('.ins-cal-day[data-date="2026-08-31"][aria-disabled="true"]') }; })()`);

await b.size(1440, 1000);
await b.goto(`${BASE}/test/fixtures/hijri.html`);
await E(ORACLE);

// ------------------------------------------------------------------ the field
ok('plugin registered both calendars', await E(`!!Insiyab.calendar('hijri') && !!Insiyab.calendar('hijri-civil')`));
let f1 = await field('h-date');
ok('field reads in Hijri, Latin digits', f1.text.includes('13') && f1.text.includes('ربيع الآخر') && f1.text.includes('1448'), f1.text);
ok('the form still carries Gregorian ISO', f1.hidden === '2026-09-24', f1.hidden);
ok('civil variant is two days off Umm al-Qura here', (await field('h-civil')).text.startsWith('11 ربيع الآخر 1448'), (await field('h-civil')).text);
ok('English locale reads in Hijri too', /Rabi.* II 13, 1448/.test((await field('h-en')).text), (await field('h-en')).text);
ok('a Gregorian field on the same page is untouched', (await field('h-greg')).text.includes('سبتمبر'), (await field('h-greg')).text);

// ------------------------------------------------------------------ the grid
await b.click('#h-date ~ .ins-date-btn');
await b.sleep(250);
let c = await cal();
const rabi2 = await E(`__g(1448, 4, 1)`), jumada1 = await E(`__g(1448, 5, 1)`), rabi2len = await E(`(new Date(__g(1448, 5, 1)) - new Date(__g(1448, 4, 1))) / 86400000`);
ok('grid opens on the Hijri month', c && c.month === 'ربيع الآخر' && c.year.includes('1448'), JSON.stringify(c));
ok('its first day is the Gregorian day Intl says', c.first === rabi2 && c.firstText === '1', `${c.first} vs ${rabi2}`);
ok('it has as many days as the Hijri month', c.count === Math.round(rabi2len), `${c.count} vs ${rabi2len}`);
ok('focus on the chosen day, numbered in Hijri', c.focus === '2026-09-24' && c.focusText === '13', `${c.focus} ${c.focusText}`);
ok('each day is announced with its Hijri date', /13.*ربيع الآخر.*1448/.test(c.focusLabel), c.focusLabel);
ok('the switch offers Hijri (on) and Gregorian', c.sw === 'هجري*|ميلادي', c.sw);

await b.key('ArrowLeft');
ok('ArrowLeft is still the next day in RTL', (await cal()).focus === '2026-09-25');
await b.key('PageDown');
c = await cal();
ok('PageDown turns a Hijri month', c.month === 'جمادى الأولى' && c.first === jumada1, JSON.stringify({ m: c.month, first: c.first, jumada1 }));
await b.click('.ins-cal [data-ins-cal="prev"]');
c = await cal();
ok('previous goes back one Hijri month', c.month === 'ربيع الآخر', c.month);
await b.click('.ins-cal [data-ins-cal="next"]');
await b.click('.ins-cal [data-ins-cal="next"]');
c = await cal();
ok('next twice lands two Hijri months on', c.month === 'جمادى الآخرة', c.month);
await E(`(() => { const s = document.querySelector('.ins-cal [data-ins-cal=month]'); s.value = '8'; s.dispatchEvent(new Event('change', { bubbles: true })); return 0; })()`);
c = await cal();
ok('the month list is Hijri: Ramadan chosen from it', c.month === 'رمضان' && c.first === await E(`__g(1448, 9, 1)`), JSON.stringify({ m: c.month, first: c.first }));

// ------------------------------------------------------------------ the switch
await E(`window.__evt = null; document.addEventListener('ins:calendar', (e) => { window.__evt = e.detail.calendar; }, { once: true }); 0`);
await b.click('.ins-cal [data-ins-cal="system"][data-value="gregory"]');
c = await cal();
f1 = await field('h-date');
ok('switch to Gregorian redraws the grid in Gregorian', c && !/[ء-ي]{3,}.*هـ/.test(c.year) && c.sw === 'هجري|ميلادي*', JSON.stringify({ m: c.month, y: c.year, sw: c.sw }));
ok('…and the field follows it', f1.text.includes('سبتمبر 2026') && f1.view === 'gregory', f1.text);
ok('…the value does not change', f1.hidden === '2026-09-24');
ok('…and says so with ins:calendar', (await E(`window.__evt`)) === 'gregory');
ok('focus stays on the switch', await E(`document.activeElement.getAttribute('data-value') === 'gregory'`));
await b.click('.ins-cal [data-ins-cal="system"][data-value="hijri"]');
f1 = await field('h-date');
ok('switch back to Hijri: field in Hijri again', f1.text.includes('ربيع الآخر') && f1.view === null, f1.text);

// choose a day in the Hijri grid
await E(`(() => { const s = document.querySelector('.ins-cal [data-ins-cal=month]'); s.value = '4'; s.dispatchEvent(new Event('change', { bubbles: true })); return 0; })()`);
const pick = await E(`__g(1448, 5, 9)`);
await b.click(`.ins-cal .ins-cal-day[data-date="${pick}"]`);
await b.sleep(200);
f1 = await field('h-date');
ok('picking 9 Jumada I sends its Gregorian ISO', f1.hidden === pick && f1.text.startsWith('9 جمادى الأولى 1448'), `${f1.hidden} / ${f1.text}`);

// ------------------------------------------------------------------ typing
async function typed(text, key = 'Tab') {
  await E(`document.getElementById('h-date').focus(); document.getElementById('h-date').select()`);
  await b.type(text);
  await b.key(key);
  await b.sleep(80);
  return field('h-date');
}
let t = await typed('1/9/1448');
ok('typed Hijri d/m/y is read', t.hidden === await E(`__g(1448, 9, 1)`) && t.text.startsWith('1 رمضان 1448'), `${t.hidden} ${t.text}`);
t = await typed('١٣/٤/١٤٤٨', 'Enter');
ok('typed in Arabic-Indic digits', t.hidden === '2026-09-24', t.hidden);
t = await typed('1448-09-01');
ok('year first works too', t.hidden === await E(`__g(1448, 9, 1)`), t.hidden);
t = await typed('2026-09-24');
ok('an ISO date is always Gregorian', t.hidden === '2026-09-24', t.hidden);
t = await typed('24/9/2026');
ok('a four-digit year past 1700 is read as Gregorian', t.hidden === '2026-09-24', t.hidden);
t = await typed('1/9/48');
ok('a two-digit year is this Hijri century', t.hidden === await E(`__g(1448, 9, 1)`), t.hidden);
const rabi1len = await E(`Math.round((new Date(__g(1448, 4, 1)) - new Date(__g(1448, 3, 1))) / 86400000)`);
t = await typed(`${rabi1len + 1}/3/1448`);
ok(`day ${rabi1len + 1} of a ${rabi1len}-day month is refused`, !t.valid && t.msg === 'اكتب تاريخًا صحيحًا.', `${t.valid} ${t.msg}`);

// ------------------------------------------------------------------ bounds
await b.click('#h-min ~ .ins-date-btn');
await b.sleep(250);
await E(`(() => { const s = document.querySelector('.ins-cal [data-ins-cal=month]'); s.value = '2'; s.dispatchEvent(new Event('change', { bubbles: true })); return 0; })()`);
c = await cal();
ok('days before min are blocked in the Hijri grid', c.disabledBefore, JSON.stringify(c));
await b.key('Escape');
await E(`document.getElementById('h-min').focus(); document.getElementById('h-min').select()`);
await b.type('1/3/1448');
await b.key('Tab');
const m1 = await E(`document.getElementById('h-min').validationMessage`);
ok('the min message names the bound in Hijri', m1.includes('ربيع الأول') && m1.includes('1448'), m1);

// ------------------------------------------------------------------ the Gregorian field has no switch
await b.click('#h-greg ~ .ins-date-btn');
await b.sleep(250);
c = await cal();
ok('a Gregorian field: Gregorian grid, no switch', c.month === 'سبتمبر' && c.sw === '', JSON.stringify({ m: c.month, sw: c.sw }));
await b.key('Escape');
ok('no console errors or exceptions', b.logs.length === 0, b.logs.join(' | '));

// ------------------------------------------------------------------ page-wide, and opting out
await b.goto(`${BASE}/test/fixtures/hijri-page.html`);
ok('data-ins-calendar on <html> makes every field Hijri', (await E(`document.getElementById('p-hijri').value`)).includes('ربيع الآخر'));
ok('a field can opt back out with gregory', (await E(`document.getElementById('p-greg').value`)).includes('سبتمبر'));

// ------------------------------------------------------------------ no plugin, then a late one
b.logs.length = 0;
await b.goto(`${BASE}/test/fixtures/hijri-none.html`);
ok('without the plugin: a working Gregorian field', (await E(`document.getElementById('n-date').value`)).includes('سبتمبر'));
await b.click('#n-date ~ .ins-date-btn');
await b.sleep(250);
ok('…whose calendar has no switch', (await cal()).sw === '');
await b.key('Escape');
ok('…and one console warning naming the plugin', b.logs.length === 1 && /insiyab-hijri\.js/.test(b.logs[0]), b.logs.join(' | '));
await E(`new Promise((r) => { const s = document.createElement('script'); s.src = '../../dist/plugins/insiyab-hijri.js'; s.onload = r; document.head.appendChild(s); })`);
await b.sleep(100);
ok('a plugin loaded late rewrites the field in Hijri', (await E(`document.getElementById('n-date').value`)).includes('ربيع الآخر'));
ok('no errors from the late load', b.logs.length === 1, b.logs.join(' | '));

await b.close();
end();
