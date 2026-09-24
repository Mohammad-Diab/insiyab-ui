// Nothing that cannot be clicked moves under the pointer; the same items still react when they are links or buttons.
import { launch, BASE } from './lib/cdp.mjs';
import { suite } from './lib/check.mjs';
const { ok, end } = suite();
const b = await launch();
const E = (js) => b.evaluate(js);

await b.size(1440, 1000);
await b.goto(`${BASE}/demo/?theme=light`);

// A fixture: each item twice, static and clickable, on top of the page.
await E(`(() => {
  const f = document.createElement('div');
  f.id = 'hv'; f.style.cssText = 'position:fixed;inset:80px auto auto 300px;z-index:99999;display:grid;gap:16px;grid-template-columns:repeat(2,420px);background:#fff;padding:16px';
  f.innerHTML = \`
    <div class="ins-stat" id="s-stat"><span class="ins-stat-ico">★</span><span class="ins-stat-value">12</span></div>
    <a class="ins-stat" id="c-stat" href="#"><span class="ins-stat-ico">★</span><span class="ins-stat-value">12</span></a>
    <div class="ins-panel" id="s-panel"><div class="ins-panel-head" id="s-head"><span class="ins-panel-ico">i</span><h3 class="ins-panel-title">t</h3></div><div class="ins-panel-body">x</div></div>
    <div class="ins-panel"><a class="ins-panel-head" id="c-head" href="#"><span class="ins-panel-ico">i</span><span class="ins-panel-title">t</span></a></div>
    <span class="ins-pill" id="s-pill"><span class="ins-pill-dot"></span>ok</span>
    <a class="ins-pill" id="c-pill" href="#"><span class="ins-pill-dot"></span>ok</a>
    <span class="ins-avatar" id="s-av">م</span>
    <a class="ins-avatar" id="c-av" href="#">م</a>
    <div class="ins-alert ins-alert--info" id="s-alert"><span class="ins-alert-ico">i</span><span class="ins-alert-text">x</span></div>
    <div class="ins-empty" id="s-empty"><span class="ins-empty-ico">∅</span><span class="ins-empty-title">x</span></div>
    <div class="ins-shell-brand" id="s-brand"><span class="ins-shell-mark">ان</span></div>
    <a class="ins-shell-brand" id="c-brand" href="#"><span class="ins-shell-mark">ان</span></a>
    <div class="ins-grid"><div class="ins-stat" id="s-gstat"><span class="ins-stat-ico">★</span><span class="ins-stat-value">3</span></div></div>
    <div class="ins-stat ins-hoverable" id="h-stat"><span class="ins-stat-ico">★</span><span class="ins-stat-value">3</span></div>
    <h2 class="ins-h2" id="s-h2">عنوان</h2>
    <button class="ins-btn ins-btn--primary" id="c-btn">زر</button>\`;
  document.body.appendChild(f); return 0; })()`);
await b.sleep(700);

const snap = (sel) => E(`(() => { const out = {}; for (const el of [document.querySelector(${JSON.stringify(sel)}), ...document.querySelectorAll(${JSON.stringify(sel)} + ' *')]) {
  const s = getComputedStyle(el); out[el.className || el.tagName] = [s.transform, s.borderColor, s.boxShadow, s.letterSpacing, s.opacity, s.backgroundColor].join('|'); } return out; })()`);

async function hovered(sel) {
  await b.mouseTo(5, 990); await b.sleep(400);
  const before = await snap(sel);
  const c = await E(`(() => { const r = document.querySelector(${JSON.stringify(sel)}).getBoundingClientRect(); return { x: r.left + Math.min(20, r.width / 2), y: r.top + r.height / 2 }; })()`);
  await b.mouseTo(c.x, c.y); await b.sleep(450);
  const after = await snap(sel);
  return Object.keys(after).filter((k) => after[k] !== before[k]);
}

for (const [sel, label] of [['#s-stat', 'static stat tile'], ['#s-gstat', 'static stat tile in a grid'], ['#s-panel', 'static panel + head icon'],
  ['#s-pill', 'status pill'], ['#s-av', 'avatar'], ['#s-alert', 'alert icon'], ['#s-empty', 'empty-state icon'], ['#s-brand', 'non-link brand mark'], ['#s-h2', 'heading']]) {
  const changed = await hovered(sel);
  ok(`static: ${label} does not react`, changed.length === 0, changed.join(', '));
}
for (const [sel, label] of [['#c-stat', 'link stat tile'], ['#h-stat', '.ins-hoverable stat tile'], ['#c-head', 'link panel head icon'], ['#c-pill', 'link pill'],
  ['#c-av', 'link avatar'], ['#c-brand', 'link brand mark'], ['#c-btn', 'button']]) {
  const changed = await hovered(sel);
  ok(`clickable: ${label} still reacts`, changed.length > 0, changed.join(', '));
}
ok('no console errors or exceptions', b.logs.length === 0, b.logs.join(' | '));
await b.close();
end();