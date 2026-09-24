// The sidebar marker travels between items like the Windows 10 navigation pane, across page loads and on the same page.
import { launch, BASE } from './lib/cdp.mjs';
import { suite } from './lib/check.mjs';
const { ok, end } = suite();
const b = await launch();
const E = (js) => b.evaluate(js);

// A recorder on every document: each frame for 1.2s after DOMContentLoaded, where the
// flying bar is (in sidebar coordinates) and whether the real markers are hidden.
await b.send('Page.addScriptToEvaluateOnNewDocument', { source: `
  window.__rec = [];
  document.addEventListener('DOMContentLoaded', () => {
    const t0 = performance.now();
    const tick = () => {
      const bar = document.querySelector('.ins-shell-indicator');
      const side = document.querySelector('.ins-shell-side');
      window.__rec.push({ t: Math.round(performance.now() - t0), bar: bar ? { top: parseFloat(getComputedStyle(bar).top), h: parseFloat(getComputedStyle(bar).height) } : null,
                          moving: !!(side && side.classList.contains('ins-shell-moving')) });
      if (performance.now() - t0 < 1200) requestAnimationFrame(tick);
    };
    requestAnimationFrame(tick);
  });` });

const marker = (sel) => E(`(() => { const side = document.querySelector('.ins-shell-side'), s = side.getBoundingClientRect();
  const l = document.querySelector(${JSON.stringify(sel)}), r = l.getBoundingClientRect(), cs = getComputedStyle(l, '::before');
  return { top: r.top + parseFloat(cs.top) - s.top - side.clientTop + side.scrollTop, h: r.height - parseFloat(cs.top) - parseFloat(cs.bottom),
           right: r.right, transform: cs.transform, vis: cs.visibility }; })()`);
const rec = () => E(`window.__rec`);
const flight = (r) => r.filter((f) => f.bar);

await b.size(1440, 1000);
await b.goto(`${BASE}/demo/index.html?theme=light`);
const A = await marker('.ins-shell-link[href="index.html"]');

// 1. Hover shows no marker on an unselected item.
await b.hover('.ins-shell-link[href="faq.html"]'); await b.sleep(400);
const hov = await marker('.ins-shell-link[href="faq.html"]');
ok('hover: no marker on an unselected item', hov.transform === 'matrix(1, 0, 0, 0, 0, 0)', hov.transform);
ok('hover: the selected item keeps its marker', A.transform === 'none' || A.transform === 'matrix(1, 0, 0, 1, 0, 0)', A.transform);

// 2. Down the list, across a page load.
await b.click('.ins-shell-link[href="dark.html"]'); await b.sleep(1800);
ok('arrived on dark.html', (await E(`location.pathname`)).endsWith('/dark.html'));
const B = await marker('.ins-shell-link[href="dark.html"]');
let f = flight(await rec());
console.log('  frames', f.length, f.filter((_, i) => i % 4 === 0).map((x) => `${x.t}:${Math.round(x.bar.top)}/${Math.round(x.bar.h)}`).join(' '));
ok('down: the bar flew', f.length > 10);
ok('down: it starts on the old item', f.length && Math.abs(f[0].bar.top - A.top) < 3 && Math.abs(f[0].bar.h - A.h) < 6, f.length ? `${f[0].bar.top}/${f[0].bar.h} vs ${A.top}/${A.h}` : '');
const peak = f.reduce((m, x) => (x.bar.h > m.bar.h ? x : m), f[0] || { bar: { h: 0 } });
ok('down: it stretches to span both items (near end held)', peak.bar.h > (B.top + B.h - A.top) * 0.9 && Math.abs(peak.bar.top - A.top) < 3, `peak h ${Math.round(peak.bar.h)} at ${peak.t}ms, span ${Math.round(B.top + B.h - A.top)}`);
ok('down: the stretch peaks at about a third (200ms)', peak.t > 120 && peak.t < 320, `${peak.t}ms`);
const last = f[f.length - 1];
ok('down: it lands on the new item', last && Math.abs(last.bar.top - B.top) < 3 && Math.abs(last.bar.h - B.h) < 3, last ? `${Math.round(last.bar.top)}/${Math.round(last.bar.h)} vs ${Math.round(B.top)}/${Math.round(B.h)}` : '');
const r = await rec();
ok('down: stand-in removed and markers shown again', !r[r.length - 1].bar && !r[r.length - 1].moving && B.vis === 'visible');
ok('down: flight lasts about 600ms', f[f.length - 1].t - f[0].t > 450 && f[f.length - 1].t - f[0].t < 750, `${f[f.length - 1].t - f[0].t}ms`);

// 3. Up the list: far end leads upward, near (bottom) end held.
await b.click('.ins-shell-link[href="start.html"]'); await b.sleep(1800);
const C = await marker('.ins-shell-link[href="start.html"]');
f = flight(await rec());
console.log('  frames', f.length, f.filter((_, i) => i % 4 === 0).map((x) => `${x.t}:${Math.round(x.bar.top)}/${Math.round(x.bar.h)}`).join(' '));
// Checked by shape, not by time: the leading frames with the bottom still on the old
// item are the stretch, everything after is the catch-up. The exact moment of full
// stretch is instantaneous and often falls between two frames, so no fixed split works.
const bottomOld = B.top + B.h;
const held = (x) => Math.abs(x.bar.top + x.bar.h - bottomOld) < 3;
const split = f.findIndex((x) => !held(x));
const beat1 = f.slice(0, split === -1 ? f.length : split), beat2 = split === -1 ? [] : f.slice(split);
const mono = (xs, key) => xs.every((x, i) => i === 0 || key(x) <= key(xs[i - 1]) + 0.5);
ok('up, beat 1: bottom end held on the old item while the top rises', beat1.length > 3 && mono(beat1, (x) => x.bar.top) && beat1[beat1.length - 1].bar.top < beat1[0].bar.top - 100,
  beat1.map((x) => `${Math.round(x.bar.top)}..${Math.round(x.bar.top + x.bar.h)}`).join(' '));
ok('up, beat 2: top end held on the new item while the bottom catches up', beat2.length > 3 && beat2.every((x) => Math.abs(x.bar.top - C.top) < 3) && mono(beat2, (x) => x.bar.h),
  beat2.map((x) => `${Math.round(x.bar.top)}..${Math.round(x.bar.top + x.bar.h)}`).join(' '));
ok('up: lands on the new item', f.length && Math.abs(f[f.length - 1].bar.top - C.top) < 3);

// 4. RTL: the bar sits on the right-hand edge, like the marker.
await E(`Insiyab.sidebarSelect('.ins-shell-link[href="faq.html"]'); 0`);
await b.sleep(100);
const bx = await E(`(() => { const b = document.querySelector('.ins-shell-indicator'); const l = document.querySelector('.ins-shell-link[href="faq.html"]');
  return b ? { bar: Math.round(b.getBoundingClientRect().right), link: Math.round(l.getBoundingClientRect().right) } : null; })()`);
ok('same page (sidebarSelect): the bar flies, on the right edge in RTL', bx && Math.abs(bx.bar - bx.link) <= 1, JSON.stringify(bx));
await b.sleep(700);
ok('same page: selection and aria-current moved', await E(`(() => { const a = document.querySelectorAll('.ins-shell-link.is-active'); return a.length === 1 && a[0].getAttribute('href') === 'faq.html' && a[0].getAttribute('aria-current') === 'page'; })()`));

// 5. Effects off: selection moves, nothing flies.
await E(`document.documentElement.setAttribute('data-ins-fx', 'off'); Insiyab.sidebarSelect('.ins-shell-link[href="color.html"]'); 0`);
ok('fx off: no stand-in bar', !(await E(`!!document.querySelector('.ins-shell-indicator')`)));
await E(`document.documentElement.removeAttribute('data-ins-fx'); 0`);

// 6. A stale note does not replay.
await E(`sessionStorage.setItem('ins-nav-from', JSON.stringify({ from: new URL('index.html', location.href).href, t: Date.now() - 60000 })); 0`);
await b.goto(`${BASE}/demo/grid.html?theme=light`); await b.sleep(900);
ok('stale note: no flight', flight(await rec()).length === 0);

// 7. Ctrl-click (new tab) leaves no note.
await E(`sessionStorage.removeItem('ins-nav-from'); 0`);
const c = await E(`(() => { const r = document.querySelector('.ins-shell-link[href="faq.html"]').getBoundingClientRect(); return { x: r.left + 20, y: r.top + r.height / 2 }; })()`);
await E(`document.addEventListener('click', e => e.preventDefault(), { once: true }); 0`);
for (const type of ['mousePressed', 'mouseReleased']) await b.send('Input.dispatchMouseEvent', { type, x: c.x, y: c.y, button: 'left', clickCount: 1, modifiers: 2 });
await b.sleep(200);
ok('ctrl-click: no note written', (await E(`sessionStorage.getItem('ins-nav-from')`)) === null);

ok('no console errors or exceptions', b.logs.length === 0, b.logs.join(' | '));
await b.close();
end();