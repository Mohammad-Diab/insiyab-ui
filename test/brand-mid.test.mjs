// Small brand accents take the middle shade brand() derives, in light and dark, and follow a custom primary; big fills keep the primary.
import { launch, BASE } from './lib/cdp.mjs';
import { suite } from './lib/check.mjs';
const { ok, end } = suite();
const b = await launch();
const E = (js) => b.evaluate(js);

await b.size(1200, 900);
await b.goto(`${BASE}/test/fixtures/components.html`);
await b.mouseTo(5, 890);

const derived = await E(`[Insiyab.color.derive('#4C1D95', 'light'), Insiyab.color.derive('#4C1D95', 'dark')]`);
ok('light: 20% toward white', derived[0]['primary-mid'] === '#704aaa' && derived[0]['on-primary-mid'] === '#ffffff', JSON.stringify(derived[0]));
ok('dark: 45% toward white, with a near-black label', derived[1]['primary-mid'] === '#9d83c5' && derived[1]['on-primary-mid'] === '#111111', JSON.stringify(derived[1]));
ok('each label holds AA on its shade', await E(`[['light'], ['dark']].every(([t]) => { const d = Insiyab.color.derive('#4C1D95', t); return Insiyab.color.contrast(d['primary-mid'], d['on-primary-mid']) >= 4.5; })`));

await E(`(() => {
  const f = document.createElement('div');
  f.id = 'mid-box';
  f.style.cssText = 'position:fixed;inset:20px auto auto 20px;z-index:99999;display:grid;gap:12px;inline-size:420px;padding:16px;background:#fff';
  f.innerHTML = '<label class="ins-check"><input type="checkbox" id="mid-check" checked> نعم</label>' +
    '<label class="ins-switch"><input type="checkbox" id="mid-switch" checked><span class="ins-switch-track" id="mid-track"></span> مفعّل</label>' +
    '<progress class="ins-progress" id="mid-progress" value="40" max="100"></progress>' +
    '<button class="ins-btn ins-btn--primary" id="mid-primary">حفظ</button>';
  document.body.appendChild(f); return 0; })()`);

const probe = (css) => E(`(() => { const p = document.createElement('div'); p.style.color = ${JSON.stringify(css)}; document.body.appendChild(p); const c = getComputedStyle(p).color; p.remove(); return c; })()`);
const look = () => E(`(() => {
  const g = (id, pseudo) => getComputedStyle(document.getElementById(id), pseudo || null);
  return {
    check: g('mid-check', '::before').backgroundColor, tick: g('mid-check', '::after').borderRightColor,
    track: g('mid-track').backgroundColor, progress: g('mid-progress').getPropertyValue('--ins-progress-a').trim(),
    primary: g('mid-primary').backgroundImage };
})()`);

for (const [theme, primary] of [['light', null], ['dark', null], ['light', '#0F766E'], ['dark', '#0F766E']]) {
  await E(`Insiyab.theme(${JSON.stringify(theme)}); ${primary ? `Insiyab.brand(${JSON.stringify(primary)});` : ''} 0`);
  await b.sleep(450);
  const want = primary ? await E(`Insiyab.color.derive(${JSON.stringify(primary)}, ${JSON.stringify(theme)})`) : derived[theme === 'light' ? 0 : 1];
  const mid = await probe(want['primary-mid']), label = await probe(want['on-primary-mid']);
  const v = await look();
  const tag = `${theme}${primary ? ' ' + primary : ''}`;
  ok(`${tag}: brand() stamps the middle shade`, (await E(`getComputedStyle(document.documentElement).getPropertyValue('--ins-primary-mid').trim().toLowerCase()`)) === want['primary-mid']);
  ok(`${tag}: a checked box is filled with it`, v.check === mid, `${v.check} vs ${mid}`);
  ok(`${tag}: and its tick takes the label it needs`, v.tick === label, `${v.tick} vs ${label}`);
  ok(`${tag}: a switch that is on`, v.track === mid, `${v.track} vs ${mid}`);
  ok(`${tag}: the progress bar`, v.progress.toLowerCase() === want['primary-mid'], v.progress);
  ok(`${tag}: a primary button keeps the primary`, v.primary.includes(await probe(primary || '#4C1D95')), v.primary);
}

ok('no console errors, warnings or exceptions', b.logs.length === 0, b.logs.join(' | '));
await b.close();
end();
