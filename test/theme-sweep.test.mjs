// A theme switch reveals the new palette as a circle from the pressed switch; instant with effects off, reduced motion, and from the API.
import { launch, BASE } from './lib/cdp.mjs';
import { suite } from './lib/check.mjs';
const { ok, end } = suite();
const b = await launch();
const E = (js) => b.evaluate(js);

const TOGGLE = '.ins-topbar [data-ins-theme-toggle]:not([data-ins-theme-toggle="system"])';
const state = () => E(`(() => { const r = document.documentElement; return {
  theme: r.getAttribute('data-ins-theme'), sweep: r.classList.contains('ins-theme-sweep'), close: r.classList.contains('ins-theme-sweep--close'),
  x: r.style.getPropertyValue('--ins-sweep-x'), y: r.style.getPropertyValue('--ins-sweep-y'),
  rad: r.style.getPropertyValue('--ins-sweep-r'),
  anims: document.getAnimations().map(a => a.animationName).filter(Boolean),
  pressed: document.querySelector(${JSON.stringify(TOGGLE)}).getAttribute('aria-pressed') }; })()`);

// Pauses the running sweep, seeks it to 120ms and 480ms and reads the animated snapshot's circle and the stacking, then lets it run on.
const held = (name, part) => E(`(() => {
  const a = document.getAnimations().find((x) => x.animationName === ${JSON.stringify(name)});
  if (!a) return { missing: true };
  a.pause();
  const other = ${JSON.stringify(part)} === 'old' ? 'new' : 'old';
  const css = (p) => getComputedStyle(document.documentElement, '::view-transition-' + p + '(root)');
  const read = (ms) => { a.currentTime = ms; const m = /circle\\(([\\d.]+)px at ([\\d.]+)px ([\\d.]+)px\\)/.exec(css(${JSON.stringify(part)}).clipPath); return m ? m.slice(1).map(Number) : [NaN, NaN, NaN]; };
  const e = read(120), l = read(480);
  const top = +css(${JSON.stringify(part)}).zIndex > +css(other).zIndex ? ${JSON.stringify(part)} : other;
  const under = css(other).clipPath;
  a.play();
  return { top, under, early: e[0], late: l[0], atX: e[1], atY: e[2] };
})()`);
const near = (a, b) => Math.abs(a - b) < 1;

await b.size(1440, 900);
await b.goto(`${BASE}/demo/?theme=light`);
ok('page has a top-bar theme toggle', await E(`!!document.querySelector(${JSON.stringify(TOGGLE)})`));
ok('startViewTransition available', await E(`typeof document.startViewTransition === 'function'`));
const box = await E(`(() => { const r = document.querySelector(${JSON.stringify(TOGGLE)}).getBoundingClientRect(); return { x: r.left + r.width / 2, y: r.top + r.height / 2 }; })()`);

// Real pointer click on the toggle, then sample mid-sweep.
for (const type of ['mouseMoved', 'mousePressed', 'mouseReleased']) {
  await b.send('Input.dispatchMouseEvent', { type, x: box.x, y: box.y, button: 'left', clickCount: 1 });
}
await b.sleep(90);
const mid = await state();
console.log('  mid', JSON.stringify(mid));
ok('sweep class is on the root mid-flight', mid.sweep);
ok('circle starts at the pressed toggle', Math.abs(parseFloat(mid.x) - box.x) < 1 && Math.abs(parseFloat(mid.y) - box.y) < 1, `${mid.x},${mid.y} vs ${box.x},${box.y}`);
const far = Math.max(...[[0, 0], [1440, 0], [0, 900], [1440, 900]].map(([cx, cy]) => Math.hypot(cx - box.x, cy - box.y)));
ok('radius reaches the furthest corner', Math.abs(parseFloat(mid.rad) - Math.ceil(far)) < 1, `${mid.rad} vs ${Math.ceil(far)}`);
ok('to dark: the old light picture closes onto the toggle', mid.close && mid.anims.includes('ins-theme-sweep-close') && !mid.anims.includes('ins-theme-sweep'), mid.anims.join(','));
const toDark = await held('ins-theme-sweep-close', 'old');
console.log('  to dark', JSON.stringify(toDark));
ok('to dark: the old picture is on top and the new one under it, unclipped', toDark.top === 'old' && toDark.under === 'none', JSON.stringify(toDark));
ok('to dark: the circle round the toggle shrinks', toDark.early > toDark.late && toDark.late > 0 && near(toDark.atX, box.x) && near(toDark.atY, box.y), JSON.stringify(toDark));
await b.sleep(900);
const after = await state();
console.log('  after', JSON.stringify(after));
ok('theme flipped to dark', after.theme === 'dark');
ok('aria-pressed follows', after.pressed === 'true');
ok('class and properties cleaned up', !after.sweep && !after.close && !after.x && !after.y && !after.rad);

// And back, dark to light: the new light picture grows out of the toggle.
for (const type of ['mouseMoved', 'mousePressed', 'mouseReleased']) {
  await b.send('Input.dispatchMouseEvent', { type, x: box.x, y: box.y, button: 'left', clickCount: 1 });
}
await b.sleep(90);
const back = await state();
ok('to light: the new picture grows from the toggle', back.sweep && !back.close && back.anims.includes('ins-theme-sweep') && !back.anims.includes('ins-theme-sweep-close'), JSON.stringify(back.anims));
const toLight = await held('ins-theme-sweep', 'new');
console.log('  to light', JSON.stringify(toLight));
ok('to light: the new picture is on top and the old one under it, unclipped', toLight.top === 'new' && toLight.under === 'none', JSON.stringify(toLight));
ok('to light: the circle round the toggle grows', toLight.early < toLight.late && toLight.early > 0 && near(toLight.atX, box.x) && near(toLight.atY, box.y), JSON.stringify(toLight));
await b.sleep(900);
ok('toggles back to light', (await state()).theme === 'light');
ok('and cleans up again', !(await state()).sweep && !(await state()).close);

// Effects off: instant, no transition.
await E(`document.documentElement.setAttribute('data-ins-fx', 'off'); 0`);
await E(`document.querySelector(${JSON.stringify(TOGGLE)}).click(); 0`);
const fx = await state();
ok('fx off: flips synchronously with no sweep', fx.theme === 'dark' && !fx.sweep, JSON.stringify(fx));
await E(`document.documentElement.removeAttribute('data-ins-fx'); 0`);

// Reduced motion: instant, no transition.
await b.send('Emulation.setEmulatedMedia', { features: [{ name: 'prefers-reduced-motion', value: 'reduce' }] });
await E(`document.querySelector(${JSON.stringify(TOGGLE)}).click(); 0`);
const rm = await state();
ok('reduced motion: flips synchronously with no sweep', rm.theme === 'light' && !rm.sweep, JSON.stringify(rm));
await b.send('Emulation.setEmulatedMedia', { features: [] });

// The API stays instant: theme() is not a click, so there is no point of contact.
await E(`Insiyab.theme('dark'); 0`);
ok('Insiyab.theme() applies synchronously', (await state()).theme === 'dark');
await E(`Insiyab.theme('light'); 0`);

// Phone, RTL: the toggle is at the other end; the circle still starts on it.
await b.size(400, 800);
await b.goto(`${BASE}/demo/?theme=light`);
const pbox = await E(`(() => { const r = document.querySelector(${JSON.stringify(TOGGLE)}).getBoundingClientRect(); return { x: r.left + r.width / 2, y: r.top + r.height / 2 }; })()`);
for (const type of ['mouseMoved', 'mousePressed', 'mouseReleased']) {
  await b.send('Input.dispatchMouseEvent', { type, x: pbox.x, y: pbox.y, button: 'left', clickCount: 1 });
}
await b.sleep(160);
await b.sleep(800);
ok('phone: theme flipped', (await state()).theme === 'dark');

ok('no console errors or exceptions', b.logs.length === 0, b.logs.join(' | '));
await b.close();
end();