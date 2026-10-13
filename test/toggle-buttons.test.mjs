// A toggle button of any style is the neutral grey button when off and takes its own style's colour when on, in light and dark.
import { launch, BASE } from './lib/cdp.mjs';
import { suite } from './lib/check.mjs';
const { ok, end } = suite();
const b = await launch();
const E = (js) => b.evaluate(js);

const STYLES = ['primary', 'secondary', 'success', 'danger', 'warning', 'info', 'ghost', 'ghost-success', 'ghost-danger', 'ghost-warning', 'ghost-info', 'bare', ''];
const COLOURED = ['primary', 'success', 'danger', 'warning', 'info', 'ghost', 'ghost-success', 'ghost-danger', 'ghost-warning', 'ghost-info'];

await b.size(1440, 1000);
await b.goto(`${BASE}/test/fixtures/components.html`);
await b.mouseTo(5, 990);

const look = (id) => E(`(() => { const s = getComputedStyle(document.getElementById(${JSON.stringify(id)})); return [s.backgroundImage, s.backgroundColor, s.borderTopColor, s.color].join(' | '); })()`);

for (const theme of ['light', 'dark']) {
  await E(`(() => {
    document.documentElement.setAttribute('data-ins-theme', ${JSON.stringify(theme)});
    document.getElementById('tg-box')?.remove();
    const f = document.createElement('div');
    f.id = 'tg-box';
    f.style.cssText = 'position:fixed;inset:20px auto auto 20px;z-index:99999;display:flex;flex-wrap:wrap;gap:8px;inline-size:900px;padding:16px;background:var(--ins-bg, #fff)';
    const cls = (s) => 'ins-btn' + (s ? ' ins-btn--' + s : '');
    f.innerHTML = ${JSON.stringify(STYLES)}.map((s) =>
      '<button class="' + cls(s) + '" id="plain-' + (s || 'none') + '">ز</button>' +
      '<button class="' + cls(s) + '" data-ins-toggle aria-pressed="false" id="off-' + (s || 'none') + '">ز</button>' +
      '<button class="' + cls(s) + '" data-ins-toggle aria-pressed="true" id="on-' + (s || 'none') + '">ز</button>').join('');
    document.body.appendChild(f); return 0; })()`);
  await b.sleep(450);

  const grey = await look('plain-secondary');
  const brandTint = await E(`(() => { const p = document.createElement('div'); p.style.background = 'rgba(var(--ins-primary-rgb), .14)'; document.body.appendChild(p); const c = getComputedStyle(p).backgroundColor; p.remove(); return c; })()`);
  const brandInk = await E(`(() => { const p = document.createElement('div'); p.style.color = 'var(--ins-primary-darker)'; document.body.appendChild(p); const c = getComputedStyle(p).color; p.remove(); return c; })()`);

  for (const s of STYLES) {
    const n = s || 'none';
    const off = await look(`off-${n}`), on = await look(`on-${n}`), plain = await look(`plain-${n}`);
    ok(`${theme} ${n}: on and off look different`, off !== on, `off ${off} / on ${on}`);
    if (s !== 'bare') ok(`${theme} ${n}: off is the neutral grey button`, off === grey, `${off} vs ${grey}`);
    if (COLOURED.includes(s)) ok(`${theme} ${n}: on is the style's own look`, on === plain, `${on} vs ${plain}`);
    if (s === 'secondary' || s === '') ok(`${theme} ${n}: on takes the brand tint`, on.includes(brandTint) && on.endsWith(brandInk), on);
    if (s === 'bare') ok(`${theme} bare: on takes the brand ink`, on.endsWith(brandInk), on);
  }
}

// A click flips a primary toggle from the grey to the brand.
await E(`document.documentElement.setAttribute('data-ins-theme', 'light')`);
await b.sleep(450);
await b.click('#off-primary');
await b.mouseTo(5, 990);
await b.sleep(450);
ok('click: aria-pressed flips', await E(`document.getElementById('off-primary').getAttribute('aria-pressed') === 'true'`));
ok('click: the toggle takes the primary look', await look('off-primary') === await look('plain-primary'), await look('off-primary'));

ok('no console errors or exceptions', b.logs.length === 0, b.logs.join(' | '));
await b.close();
end();
