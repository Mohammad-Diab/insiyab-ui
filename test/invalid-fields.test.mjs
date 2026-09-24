// A class-marked invalid field stays red under the pointer, and turns its input group red, like the other invalid routes.
import { launch, BASE } from './lib/cdp.mjs';
import { suite } from './lib/check.mjs';
const { ok, end } = suite();
const b = await launch();
const E = (js) => b.evaluate(js);

await b.size(1440, 1000);
await b.goto(`${BASE}/demo/validation.html?theme=light`);
await E(`(() => {
  const f = document.createElement('div');
  f.style.cssText = 'position:fixed;inset:90px auto auto 320px;z-index:99999;display:grid;gap:14px;inline-size:420px;padding:16px;background:#fff';
  f.innerHTML = \`
    <input class="ins-input ins-input--bad" id="b-input" value="x">
    <select class="ins-select ins-select--bad" id="b-select"><option>x</option></select>
    <div class="ins-input-group" id="b-group"><input class="ins-input ins-input--bad" id="b-ginput" value="x"><span class="ins-addon">ل.س</span></div>
    <input class="ins-input" aria-invalid="true" id="a-input" value="x">
    <div class="ins-input-group" id="a-group"><input class="ins-input" aria-invalid="true" value="x"><span class="ins-addon">ل.س</span></div>
    <input class="ins-input" id="plain" value="x">\`;
  document.body.appendChild(f); return 0; })()`);
await b.sleep(500);

const bad = await E(`getComputedStyle(document.documentElement).getPropertyValue('--ins-tone-bad-bd').trim()`);
const look = (sel) => E(`(() => { const s = getComputedStyle(document.querySelector(${JSON.stringify(sel)})); return { bd: s.borderTopColor, bg: s.backgroundColor, sh: s.boxShadow }; })()`);
// The tone token resolved to a colour, for comparing against computed values.
const red = await E(`(() => { const p = document.createElement('div'); p.style.borderTopColor = 'var(--ins-tone-bad-bd)'; p.style.borderTopStyle = 'solid'; document.body.appendChild(p); const c = getComputedStyle(p).borderTopColor; p.remove(); return c; })()`);
const redFocus = await E(`(() => { const p = document.createElement('div'); p.style.borderTopColor = 'var(--ins-tone-bad)'; p.style.borderTopStyle = 'solid'; document.body.appendChild(p); const c = getComputedStyle(p).borderTopColor; p.remove(); return c; })()`);
console.log('  tone-bad-bd =', red, ' tone-bad =', redFocus);

async function hovered(sel, target = sel) {
  await b.mouseTo(5, 990); await b.sleep(350);
  const rest = await look(sel);
  await b.hover(target); await b.sleep(350);
  const hover = await look(sel);
  return { rest, hover };
}

for (const [sel, label, target] of [['#b-input', 'class-marked input'], ['#b-select', 'class-marked select'], ['#b-group', 'group holding a class-marked input', '#b-ginput'],
  ['#a-input', 'aria-invalid input'], ['#a-group', 'group holding an aria-invalid input', '#a-group input']]) {
  const r = await hovered(sel, target);
  ok(`${label}: red at rest`, r.rest.bd === red, r.rest.bd);
  ok(`${label}: still red on hover`, r.hover.bd === red, r.hover.bd);
}

// Focus: the class-marked field takes the stronger red and the red glow, like the others.
await b.click('#b-input'); await b.sleep(350);
const f1 = await look('#b-input');
ok('class-marked input: focus is the strong red with the glow', f1.bd === redFocus && /rgba?\(/.test(f1.sh), f1.bd);
await b.click('#b-ginput'); await b.sleep(350);
const f2 = await look('#b-group');
ok('group holding a class-marked input: focus is the strong red', f2.bd === redFocus, f2.bd);

// A plain field still gets the normal hover.
const p = await hovered('#plain');
ok('plain field: hover still changes it', p.rest.bd !== p.hover.bd || p.rest.bg !== p.hover.bg);

ok('no console errors or exceptions', b.logs.length === 0, b.logs.join(' | '));
await b.close();
end();