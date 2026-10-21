// Latin typed into a one-line field on an Arabic page: the caret sits right after the last letter, and the text stays at the field's start edge.
import { launch, BASE } from './lib/cdp.mjs';
import { suite } from './lib/check.mjs';
const { ok, end } = suite();
const b = await launch();
const E = (js) => b.evaluate(js);

await b.size(1440, 1000);
await b.goto(`${BASE}/test/fixtures/components.html`);
await E(`(() => {
  const f = document.createElement('div');
  f.style.cssText = 'position:fixed;inset:20px auto auto 320px;z-index:99999;display:grid;gap:14px;inline-size:420px;padding:16px;background:#fff';
  f.innerHTML = ['text', 'email', 'password'].map((t) => '<input class="ins-input" id="t-' + t + '" type="' + t + '" style="color:#000;background:#fff;caret-color:rgb(255,0,0);transition:none;transform:none">').join('');
  document.body.appendChild(f); return 0; })()`);

// The field as pixels, read in the page from a screenshot: the caret's column (pure red) and the ink's right edge, along the field's middle row band.
const look = async (id) => {
  for (let k = 0; k < 8; k++) {
    const r = await E(`(() => { const r = document.getElementById('${id}').getBoundingClientRect(); return { x: r.left, y: r.top, width: r.width, height: r.height }; })()`);
    const shot = await b.send('Page.captureScreenshot', { format: 'png', clip: { ...r, scale: 1 } });
    const m = await E(`new Promise((done) => {
      const img = new Image();
      img.onload = () => {
        const c = document.createElement('canvas'); c.width = img.width; c.height = img.height;
        const g = c.getContext('2d'); g.drawImage(img, 0, 0);
        const d = g.getImageData(0, 0, c.width, c.height).data, mid = Math.round(c.height / 2);
        let caret = null, inkLeft = null, inkRight = null;
        for (let x = 4; x < c.width - 4; x++) for (let y = mid - 6; y <= mid + 6; y++) {
          const i = (y * c.width + x) * 4, R = d[i], G = d[i + 1], B = d[i + 2];
          if (R > 200 && G < 80 && B < 80) caret = caret === null ? x : caret;
          else if (R + G + B < 300) { inkLeft = inkLeft === null ? x : Math.min(inkLeft, x); inkRight = Math.max(inkRight ?? 0, x); }
        }
        done({ caret, inkLeft, inkRight, width: c.width });
      };
      img.src = 'data:image/png;base64,${shot.result.data}';
    })`);
    if (m.caret !== null) return m;
    await b.sleep(170);
  }
  return null;
};

for (const t of ['text', 'email', 'password']) {
  const id = 't-' + t, word = t === 'email' ? 'name@example.com' : 'cvDherherherhersf';
  await E(`document.getElementById('${id}').focus(); 0`);
  await b.type(word);
  const sel = await E(`(() => { const el = document.getElementById('${id}'); return { at: el.selectionStart, len: el.value.length, pad: parseFloat(getComputedStyle(el).paddingRight) }; })()`);
  ok(`${t}: the caret is at the end of the value`, (sel.at === sel.len || (t === 'email' && sel.at === null)) && sel.len === word.length, JSON.stringify(sel));
  const m = await look(id);
  ok(`${t}: the caret is drawn right after the last letter`, !!m && m.inkRight !== null && Math.abs(m.caret - m.inkRight) <= 4, JSON.stringify(m));
  ok(`${t}: the text starts at the field's start edge (right)`, !!m && m.inkRight !== null && m.width - m.inkRight <= sel.pad + 6, JSON.stringify(m));
}

await b.close();
end();
