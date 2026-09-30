// On a page that paints before DOMContentLoaded, the destination marker never shows before the flight takes over.
import { launch, BASE } from './lib/cdp.mjs';
import { suite } from './lib/check.mjs';
const { ok, end } = suite();
const b = await launch();
const E = (js) => b.evaluate(js);

/* Make the browser paint before DOMContentLoaded, as it does on a real connection
   with a long page: every demo page gets a parser-blocking script at the end of
   <body> that the server "takes" 700ms to send. Everything above it, the sidebar
   included, is parsed and paintable while the parser waits. And in "old" mode the
   pre-paint hide is stripped out of insiyab.js and insiyab-boot.js as they are served, recreating the
   flicker the user saw. */
let OLD = false;
b.on('Fetch.requestPaused', async (p) => {
  const url = p.request.url;
  try {
    if (url.includes('/__slow.js')) {
      await b.sleep(700);
      await b.send('Fetch.fulfillRequest', { requestId: p.requestId, responseCode: 200,
        responseHeaders: [{ name: 'Content-Type', value: 'text/javascript' }], body: Buffer.from('/* slow */').toString('base64') });
      return;
    }
    if (p.responseStatusCode) {
      const r = await b.send('Fetch.getResponseBody', { requestId: p.requestId });
      let text = r.result.base64Encoded ? Buffer.from(r.result.body, 'base64').toString('utf8') : r.result.body;
      if (url.includes('/demo/') && url.split('?')[0].endsWith('.html')) text = text.replace('</body>', '<script src="/__slow.js"></script></body>');
      if (/insiyab(-boot)?\.js/.test(url) && OLD) text = text.replace("root.classList.add('ins-nav-arriving');", '/* stripped */');
      const headers = (p.responseHeaders || []).filter((h) => !/^content-length$/i.test(h.name));
      await b.send('Fetch.fulfillRequest', { requestId: p.requestId, responseCode: p.responseStatusCode, responseHeaders: headers, body: Buffer.from(text, 'utf8').toString('base64') });
      return;
    }
    await b.send('Fetch.continueRequest', { requestId: p.requestId });
  } catch (e) { console.log('fetch handler error', e.message); }
});
await b.send('Network.setCacheDisabled', { cacheDisabled: true });
await b.send('Network.enable');
await b.send('Fetch.enable', { patterns: [
  { urlPattern: '*/demo/*.html*', requestStage: 'Response' },
  { urlPattern: '*insiyab*.js*', requestStage: 'Response' },
  { urlPattern: '*__slow.js*', requestStage: 'Request' },
] });

// From the very start of every document: on each frame (rAF runs just before a
// paint), is the selected item's marker visible, and is the flying bar up?
await b.send('Page.addScriptToEvaluateOnNewDocument', { source: `
  window.__f = []; const t0 = performance.now(); let dcl = false;
  document.addEventListener('DOMContentLoaded', () => { dcl = true; });
  const tick = () => {
    const a = document.querySelector('.ins-shell-link.is-active');
    if (a) {
      const cs = getComputedStyle(a, '::before');
      window.__f.push({ t: Math.round(performance.now() - t0), dcl,
        marker: cs.visibility === 'visible' && cs.transform !== 'matrix(1, 0, 0, 0, 0, 0)',
        bar: !!document.querySelector('.ins-shell-indicator') });
    }
    if (performance.now() - t0 < 3000) requestAnimationFrame(tick);
  };
  requestAnimationFrame(tick);` });

function flicker(frames) {
  const lastBar = frames.map((f) => f.bar).lastIndexOf(true);
  const pre = frames.filter((f) => !f.dcl).length;
  if (lastBar === -1) return { flew: false, preDclFrames: pre, bad: frames.filter((f) => f.marker).length };
  const bad = frames.slice(0, lastBar + 1).filter((f) => f.marker);
  const after = frames.slice(lastBar + 1);
  return { flew: true, preDclFrames: pre, bad: bad.length, shownAfter: after.length > 0 && after.every((f) => f.marker) };
}

async function trip(from, to) {
  await b.goto(`${BASE}/demo/${from}?theme=light&mode=advanced`);
  // The docs' advanced mode, saved, so the destination lists every link too and the one left behind is there to fly from.
  await E(`localStorage.setItem('insiyab-demo-mode', 'advanced'); 0`);
  await b.sleep(900);
  await b.click(`.ins-shell-link[href="${to}"]`);
  await b.sleep(3200);
  return flicker(await E('window.__f'));
}

await b.size(1440, 1000);
const pairs = [['index.html', 'dark.html'], ['dark.html', 'start.html'], ['start.html', 'motion.html'], ['motion.html', 'index.html']];

OLD = true;
let oldBad = 0, pre = 0;
for (const [a, c] of pairs.slice(0, 2)) { const r = await trip(a, c); oldBad += r.bad; pre += r.preDclFrames; console.log('  old', a, '→', c, JSON.stringify(r)); }
ok('the slow page really paints before DOMContentLoaded', pre > 5, `${pre} frames`);
ok('without the fix, the flicker reproduces', oldBad > 0, `${oldBad} frames with the destination marker showing before the flight`);

OLD = false;
let newBad = 0, flights = 0, shown = 0;
for (const [a, c] of pairs) {
  const r = await trip(a, c);
  console.log('  new', a, '→', c, JSON.stringify(r));
  newBad += r.bad; if (r.flew) flights++; if (r.shownAfter) shown++;
}
ok('with the fix, every trip still flies', flights === pairs.length, `${flights}/${pairs.length}`);
ok('with the fix, no frame shows the destination marker early', newBad === 0, `${newBad} frames`);
ok('with the fix, the marker shows on every frame after landing', shown === pairs.length, `${shown}/${pairs.length}`);

// A plain load, with no note, hides nothing.
await E(`sessionStorage.removeItem('ins-nav-from'); 0`);
await b.goto(`${BASE}/demo/grid.html?theme=light`); await b.sleep(1500);
const plain = await E('window.__f');
ok('plain load: marker visible from the first frame', plain.length > 0 && plain.every((f) => f.marker), `${plain.filter((f) => !f.marker).length} hidden frames of ${plain.length}`);

const errs = b.logs.filter((l) => !/__slow/.test(l));
ok('no console errors or exceptions', errs.length === 0, errs.join(' | '));
await b.close();
end();