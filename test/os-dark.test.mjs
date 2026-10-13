// OS dark mode with no theme chosen paints exactly like an explicit dark choice, element by element, across docs pages.
import { launch, BASE } from './lib/cdp.mjs';
import { suite } from './lib/check.mjs';
const { ok, end } = suite();
const b = await launch();
const E = (js) => b.evaluate(js);

/* The same page, dark two ways: chosen explicitly, and followed from the OS with no
   choice made. Every element's painted colours must be identical. Pseudo-elements
   too — the glass sheen and the select's chevron live on them. */
const SNAP = `(() => {
  const props = ['color', 'background-color', 'background-image', 'border-top-color', 'box-shadow', 'fill', 'stroke', 'outline-color'];
  const out = {};
  const all = [...document.querySelectorAll('body *')].filter((el) => !el.closest('script, style, svg defs, details:not([open]) > :not(summary)'));
  all.forEach((el, i) => {
    for (const pseudo of [null, '::before', '::after']) {
      const cs = getComputedStyle(el, pseudo);
      if (pseudo && cs.content === 'none') continue;
      out[i + (pseudo || '') + ' ' + el.tagName.toLowerCase() + (el.className && typeof el.className === 'string' ? '.' + el.className.trim().split(/\\s+/).slice(0, 2).join('.') : '')] =
        props.map((p) => cs.getPropertyValue(p)).join(' | ');
    }
  });
  return out;
})()`;

// goto waits a fixed time, so a loaded machine can hand over a page whose scripts have not run yet.
const ready = () => E(`new Promise((done) => { const t = () => (document.readyState === 'complete' && window.Insiyab ? done(true) : setTimeout(t, 25)); t(); })`);

// A theme change starts colour transitions, and on a loaded machine one can still be running at the snapshot, so every finite one is run to its end first.
const settle = () => E(`(async () => {
  for (let i = 0; i < 20; i++) {
    const running = document.getAnimations().filter((a) => a.playState === 'running' && Number.isFinite(a.effect && a.effect.getComputedTiming().endTime));
    if (!running.length) break;
    running.forEach((a) => a.finish());
    await new Promise((r) => requestAnimationFrame(() => requestAnimationFrame(r)));
  }
  return true;
})()`);

const PAGES = ['index.html', 'inputs.html', 'alerts.html', 'shell.html', 'surfaces.html', 'tables.html', 'badges.html', 'dropdown.html', 'pickers.html'];
await b.size(1440, 1000);
let totalDiff = 0;
const seen = [];
for (const page of PAGES) {
  await b.send('Emulation.setEmulatedMedia', { features: [{ name: 'prefers-color-scheme', value: 'light' }, { name: 'prefers-reduced-motion', value: 'reduce' }] });
  await b.goto(`${BASE}/demo/${page}`);
  await ready();
  await E(`localStorage.clear(); Insiyab.theme('dark'); document.querySelectorAll('details[open]').forEach(d => d.removeAttribute('open')); 0`);
  await b.sleep(400);
  await settle();
  const explicit = await E(SNAP);

  await E(`Insiyab.theme('system'); 0`);
  await b.send('Emulation.setEmulatedMedia', { features: [{ name: 'prefers-color-scheme', value: 'dark' }, { name: 'prefers-reduced-motion', value: 'reduce' }] });
  await b.sleep(400);
  await settle();
  const attr = await E(`document.documentElement.getAttribute('data-ins-theme')`);
  const system = await E(SNAP);

  const diffs = Object.keys(explicit).filter((k) => explicit[k] !== system[k]);
  totalDiff += diffs.length;
  console.log(`  ${page}: ${Object.keys(explicit).length} boxes, ${diffs.length} differ${attr ? '  (attr still ' + attr + '!)' : ''}`);
  for (const k of diffs.slice(0, 4)) {
    const a = explicit[k].split(' | '), s = system[k].split(' | ');
    const which = a.map((v, i) => (v !== s[i] ? i : -1)).filter((i) => i >= 0);
    console.log(`      ${k.slice(0, 60)}  prop#${which.join(',')}  explicit=${a[which[0]].slice(0, 60)}  system=${s[which[0]].slice(0, 60)}`);
    seen.push(`${page} ${k.slice(0, 50)} prop#${which.join(',')} explicit=${a[which[0]].slice(0, 50)} system=${s[which[0]].slice(0, 50)}`);
  }
}
ok('OS dark paints exactly like explicit dark on every element', totalDiff === 0, `${totalDiff} differences: ${seen.join(' || ')}`);

// And the escape hatch still holds: explicit light on an OS set to dark stays light.
await b.goto(`${BASE}/demo/inputs.html`);
await ready();
await E(`localStorage.clear(); Insiyab.theme('light'); 0`);
await b.send('Emulation.setEmulatedMedia', { features: [{ name: 'prefers-color-scheme', value: 'light' }, { name: 'prefers-reduced-motion', value: 'reduce' }] });
await b.sleep(600);
await settle();
const lightOnLight = await E(SNAP);
await b.send('Emulation.setEmulatedMedia', { features: [{ name: 'prefers-color-scheme', value: 'dark' }, { name: 'prefers-reduced-motion', value: 'reduce' }] });
await b.sleep(600);
await settle();
const lightOnDark = await E(SNAP);
const d2 = Object.keys(lightOnLight).filter((k) => lightOnLight[k] !== lightOnDark[k]);
ok('explicit light on an OS set to dark is untouched by the OS', d2.length === 0, `${d2.length} differences`);

ok('no console errors or exceptions', b.logs.length === 0, b.logs.join(' | '));
await b.close();
end();