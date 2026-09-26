// Smooth scrolling is opt-in, per page or per box, and anchors land below the sticky top bar.
import { launch, BASE } from './lib/cdp.mjs';
import { suite } from './lib/check.mjs';
const { ok, end } = suite();
const b = await launch();
const E = (js) => b.evaluate(js);

await b.size(1440, 900);
await b.goto(`${BASE}/demo/?theme=light`);
await E(`(() => {
  const host = document.querySelector('.ins-shell-content') || document.body;
  const pad = document.createElement('div'); pad.style.height = '4000px'; host.appendChild(pad);
  const t = document.createElement('h2'); t.id = 'far-target'; t.textContent = 'الهدف'; host.appendChild(t);
  const tail = document.createElement('div'); tail.style.height = '1200px'; host.appendChild(tail);
  const a = document.createElement('a'); a.id = 'jump'; a.href = '#far-target'; a.textContent = 'jump';
  a.style.cssText = 'position:fixed;bottom:20px;left:20px;z-index:99999;padding:8px;background:#fff';
  document.body.appendChild(a); return 0; })()`);

const Y = () => E(`Insiyab.scrollTop()`);
const top0 = () => E(`window.scrollTo({ top: 0, behavior: 'instant' }); document.body.scrollTo({ top: 0, behavior: 'instant' }); history.replaceState(null, '', location.pathname + location.search); 0`);
const landing = () => E(`(() => { const t = document.getElementById('far-target').getBoundingClientRect().top;
  const bar = document.querySelector('.ins-topbar').getBoundingClientRect().bottom; return { t: Math.round(t), bar: Math.round(bar) }; })()`);

// 1. Default: instant, and below the bar. The docs site opts its pages in, so take
// the class off first: this is the library's default being tested, not the site's.
await E(`document.documentElement.classList.remove('ins-scroll-smooth'); 0`);
await top0(); await b.sleep(200);
await b.click('#jump'); await b.sleep(40);
const y1 = await Y(); await b.sleep(400); const y1f = await Y();
ok('default: anchor jump is instant', y1 > 0 && Math.abs(y1 - y1f) < 2, `${y1} → ${y1f}`);
const l1 = await landing();
ok('default: target lands below the sticky bar, not under it', l1.t >= l1.bar, `target top ${l1.t}, bar bottom ${l1.bar}`);

// 2. With the class: a glide.
await E(`document.documentElement.classList.add('ins-scroll-smooth'); 0`);
ok('class: <html> computes scroll-behavior smooth', (await E(`getComputedStyle(document.documentElement).scrollBehavior`)) === 'smooth');
await top0(); await b.sleep(200);
await b.click('#jump'); await b.sleep(120);
const mid = await Y(); await b.sleep(1600); const final = await Y();
ok('class: anchor jump glides (caught mid-way)', mid > 0 && mid < final - 50, `mid ${mid}, end ${final}`);
ok('class: glide ends where the instant jump did', Math.abs(final - y1f) < 2, `${final} vs ${y1f}`);
const l2 = await landing();
ok('class: target lands below the sticky bar', l2.t >= l2.bar, `target top ${l2.t}, bar bottom ${l2.bar}`);

// 3. Effects off.
await E(`document.documentElement.setAttribute('data-ins-fx', 'off'); 0`);
ok('fx off: back to auto', (await E(`getComputedStyle(document.documentElement).scrollBehavior`)) === 'auto');
await E(`document.documentElement.removeAttribute('data-ins-fx'); 0`);

// 4. Reduced motion.
await b.send('Emulation.setEmulatedMedia', { features: [{ name: 'prefers-reduced-motion', value: 'reduce' }] });
ok('reduced motion: back to auto', (await E(`getComputedStyle(document.documentElement).scrollBehavior`)) === 'auto');
await b.send('Emulation.setEmulatedMedia', { features: [] });

// 5. Lead scrollbar mode: <body> is the scroller and must carry it.
await E(`document.documentElement.setAttribute('data-ins-scrollbar', 'lead'); 0`);
ok('lead mode: <body> computes smooth', (await E(`getComputedStyle(document.body).scrollBehavior`)) === 'smooth');
ok('lead mode: <body> carries the bar offset', (await E(`parseFloat(getComputedStyle(document.body).scrollPaddingBlockStart)`)) > 50);
await E(`document.documentElement.setAttribute('data-ins-fx', 'off'); 0`);
ok('lead mode + fx off: <body> back to auto', (await E(`getComputedStyle(document.body).scrollBehavior`)) === 'auto');
await E(`document.documentElement.removeAttribute('data-ins-fx'); document.documentElement.removeAttribute('data-ins-scrollbar'); document.documentElement.classList.remove('ins-scroll-smooth'); 0`);

// 6. One scroller on its own.
const box = await E(`(() => { const d = document.createElement('div'); d.className = 'ins-scroll-smooth'; document.body.appendChild(d);
  const e = document.createElement('div'); document.body.appendChild(e);
  return [getComputedStyle(d).scrollBehavior, getComputedStyle(e).scrollBehavior, getComputedStyle(document.documentElement).scrollBehavior]; })()`);
ok('a single scroller opts in on its own, the page stays instant', box[0] === 'smooth' && box[1] === 'auto' && box[2] === 'auto', box.join(', '));

ok('no console errors or exceptions', b.logs.length === 0, b.logs.join(' | '));
await b.close();
end();