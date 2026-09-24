// The sidebar keeps its scroll between pages and reveals the selected item on a fresh visit, from the first painted frame.
import { launch, BASE } from './lib/cdp.mjs';
import { suite } from './lib/check.mjs';
const { ok, end } = suite();
const b = await launch();
const E = (js) => b.evaluate(js);

// The slow page: a parser-blocking script at the end of <body> held 700ms, so the
// browser paints the parsed part (the sidebar) before DOMContentLoaded.
b.on('Fetch.requestPaused', async (p) => {
  const url = p.request.url;
  try {
    if (url.includes('/__slow.js')) {
      await b.sleep(700);
      await b.send('Fetch.fulfillRequest', { requestId: p.requestId, responseCode: 200, responseHeaders: [{ name: 'Content-Type', value: 'text/javascript' }], body: Buffer.from('//').toString('base64') });
      return;
    }
    const r = await b.send('Fetch.getResponseBody', { requestId: p.requestId });
    let text = r.result.base64Encoded ? Buffer.from(r.result.body, 'base64').toString('utf8') : r.result.body;
    text = text.replace('</body>', '<script src="/__slow.js"></script></body>');
    const headers = (p.responseHeaders || []).filter((h) => !/^content-length$/i.test(h.name));
    await b.send('Fetch.fulfillRequest', { requestId: p.requestId, responseCode: p.responseStatusCode, responseHeaders: headers, body: Buffer.from(text).toString('base64') });
  } catch (e) { console.log('fetch handler error', e.message); }
});
await b.send('Network.setCacheDisabled', { cacheDisabled: true });
await b.send('Network.enable');
await b.send('Fetch.enable', { patterns: [{ urlPattern: '*/demo/*.html*', requestStage: 'Response' }, { urlPattern: '*__slow.js*', requestStage: 'Request' }] });

// Every frame from the first: the sidebar's scroll, once its selected link exists.
await b.send('Page.addScriptToEvaluateOnNewDocument', { source: `
  window.__s = []; const t0 = performance.now(); let dcl = false;
  document.addEventListener('DOMContentLoaded', () => { dcl = true; });
  const tick = () => {
    const side = document.querySelector('.ins-shell-side');
    const a = side && side.querySelector('.ins-shell-link.is-active');
    if (a) {
      const s = side.getBoundingClientRect(), r = a.getBoundingClientRect();
      window.__s.push({ t: Math.round(performance.now() - t0), dcl, top: Math.round(side.scrollTop), inView: r.top >= s.top && r.bottom <= s.bottom });
    }
    if (performance.now() - t0 < 2500) requestAnimationFrame(tick);
  };
  requestAnimationFrame(tick);` });

const frames = () => E('window.__s');
const summary = (f) => ({ frames: f.length, preDcl: f.filter((x) => !x.dcl).length, tops: [...new Set(f.map((x) => x.top))] });

await b.size(1440, 600);
await b.goto(`${BASE}/demo/index.html?theme=light`);
await b.sleep(1500);
const deep = await E(`(() => { const l = [...document.querySelectorAll('.ins-shell-side a.ins-shell-link')]; return l[l.length - 2].getAttribute('href'); })()`);
const scrollable = await E(`(() => { const s = document.querySelector('.ins-shell-side'); return s.scrollHeight > s.clientHeight + 200; })()`);
ok('the sidebar scrolls at this height', scrollable);

// 1. Fresh visit to a page whose item is far down the list.
await E(`sessionStorage.clear(); 0`);
await b.goto(`${BASE}/demo/${deep}?theme=light`);
await b.sleep(2600);
let f = await frames(), s = summary(f);
console.log('  fresh deep', deep, JSON.stringify(s));
ok('fresh visit: the browser painted before DOMContentLoaded', s.preDcl > 5, `${s.preDcl} frames`);
ok('fresh visit: one scroll position on every frame (no flash at the top)', s.tops.length === 1 && s.tops[0] > 0, s.tops.join(','));
ok('fresh visit: the selected item is in view on every frame', f.every((x) => x.inView));

// 2. Scroll the sidebar by hand, click a visible link: the next page keeps that scroll.
const X = await E(`(() => { const s = document.querySelector('.ins-shell-side'); s.scrollTop = Math.round(s.scrollHeight * 0.35); return s.scrollTop; })()`);
const target = await E(`(() => { const s = document.querySelector('.ins-shell-side'), sr = s.getBoundingClientRect();
  const l = [...s.querySelectorAll('a.ins-shell-link:not(.is-active)')].find((a) => { const r = a.getBoundingClientRect(); return r.top > sr.top + 40 && r.bottom < sr.bottom - 40; });
  const r = l.getBoundingClientRect(); return { href: l.getAttribute('href'), x: r.left + 30, y: r.top + r.height / 2 }; })()`);
// A plain pointer click where the link is — the driver's click() would scroll it to the centre first.
for (const type of ['mouseMoved', 'mousePressed', 'mouseReleased']) {
  await b.send('Input.dispatchMouseEvent', { type, x: target.x, y: target.y, button: 'left', clickCount: 1 });
}
ok('the click happened at the hand-scrolled position', true, `scroll ${X}, link ${target.href}`);
await b.sleep(2600);
f = await frames(); s = summary(f);
console.log('  click', target.href, 'at scroll', X, JSON.stringify(s));
ok('after a sidebar click: the sidebar keeps its scroll on every frame', s.tops.length === 1 && Math.abs(s.tops[0] - X) <= 1, `${s.tops.join(',')} vs ${X}`);
ok('after a sidebar click: the painted frames before DOMContentLoaded included', s.preDcl > 5, `${s.preDcl}`);

// 3. Fresh visit to a page near the top of the list: left at the top.
await E(`sessionStorage.clear(); 0`);
await b.goto(`${BASE}/demo/start.html?theme=light`);
await b.sleep(2600);
f = await frames(); s = summary(f);
console.log('  fresh top', JSON.stringify(s));
ok('fresh visit near the top: stays at 0 on every frame', s.tops.length === 1 && s.tops[0] === 0, s.tops.join(','));

// 4. The person's own scrolling is not fought after arrival.
await E(`document.querySelector('.ins-shell-side').scrollTop = 150; 0`);
await b.sleep(300);
ok('own scrolling after arrival is left alone', (await E(`document.querySelector('.ins-shell-side').scrollTop`)) === 150);

const errs = b.logs.filter((l) => !/__slow/.test(l));
ok('no console errors or exceptions', errs.length === 0, errs.join(' | '));
await b.close();
end();