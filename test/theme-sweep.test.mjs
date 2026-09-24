// A theme switch reveals the new palette as a circle from the pressed switch; instant with effects off, reduced motion, and from the API.
import { launch, BASE } from './lib/cdp.mjs';
import { suite } from './lib/check.mjs';
const { ok, end } = suite();
const b = await launch();
const E = (js) => b.evaluate(js);

const TOGGLE = '.ins-topbar [data-ins-theme-toggle]:not([data-ins-theme-toggle="system"])';
const state = () => E(`(() => { const r = document.documentElement; return {
  theme: r.getAttribute('data-ins-theme'), sweep: r.classList.contains('ins-theme-sweep'),
  x: r.style.getPropertyValue('--ins-sweep-x'), y: r.style.getPropertyValue('--ins-sweep-y'),
  rad: r.style.getPropertyValue('--ins-sweep-r'),
  anims: document.getAnimations().map(a => a.animationName).filter(Boolean),
  pressed: document.querySelector(${JSON.stringify(TOGGLE)}).getAttribute('aria-pressed') }; })()`);

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
ok('the ins-theme-sweep animation is running', mid.anims.includes('ins-theme-sweep'), mid.anims.join(','));
await b.sleep(150);
await b.sleep(700);
const after = await state();
console.log('  after', JSON.stringify(after));
ok('theme flipped to dark', after.theme === 'dark');
ok('aria-pressed follows', after.pressed === 'true');
ok('class and properties cleaned up', !after.sweep && !after.x && !after.y && !after.rad);

// And back, dark to light.
await b.click(TOGGLE); await b.sleep(900);
ok('toggles back to light', (await state()).theme === 'light');

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