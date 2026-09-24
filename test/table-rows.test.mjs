// Hovering a table row fills it; only a selected row carries the leading-edge marker.
import { launch, BASE } from './lib/cdp.mjs';
import { suite } from './lib/check.mjs';
const { ok, end } = suite();
const b = await launch();
const E = (js) => b.evaluate(js);

await b.size(1440, 1000);
await b.goto(`${BASE}/demo/tables.html?theme=light`);
await b.sleep(900);   // let the row entrances finish
const row = (i) => E(`(() => { const tr = document.querySelectorAll('#basic ~ .dx-example .ins-table tbody tr, .ins-table tbody tr')[${i}];
  const td = tr.querySelector('td'); const m = getComputedStyle(td, '::before');
  return { marker: m.transform, bg: getComputedStyle(td).backgroundColor, sel: tr.getAttribute('aria-selected') }; })()`);

const NONE = 'matrix(1, 0, 0, 0, 0, 0)';
await b.mouseTo(5, 990); await b.sleep(400);
const rest0 = await row(0), sel = await row(1);
ok('an ordinary row at rest: no marker', rest0.marker === NONE, rest0.marker);
ok('the selected row: marker shown without any hover', sel.sel === 'true' && sel.marker !== NONE, sel.marker);
ok('the selected row: filled like a hovered row', sel.bg !== rest0.bg, `${sel.bg} vs ${rest0.bg}`);

await b.hover('.ins-table tbody tr:nth-child(3) td:nth-child(3)'); await b.sleep(400);
const hov = await row(2);
ok('a hovered row: filled', hov.bg !== rest0.bg, hov.bg);
ok('a hovered row: no marker', hov.marker === NONE, hov.marker);
ok('hovered fill and selected fill are the same', hov.bg === sel.bg);

// aria-current marks it too, and aria-current="false" does not.
await E(`(() => { const r = document.querySelectorAll('.ins-table tbody tr'); r[3].setAttribute('aria-current', 'page'); r[0].setAttribute('aria-current', 'false'); r[0].classList.remove('is-selected'); return 0; })()`);
await b.mouseTo(5, 990); await b.sleep(400);
const cur = await row(3), falsy = await row(0);
ok('aria-current row: marker shown', cur.marker !== NONE, cur.marker);
ok('aria-current="false": no marker', falsy.marker === NONE, falsy.marker);
await E(`document.querySelectorAll('.ins-table tbody tr')[2].classList.add('is-selected'); 0`);
await b.sleep(400);
ok('.is-selected row: marker shown', (await row(2)).marker !== NONE);

ok('no console errors or exceptions', b.logs.length === 0, b.logs.join(' | '));
await b.close();
end();