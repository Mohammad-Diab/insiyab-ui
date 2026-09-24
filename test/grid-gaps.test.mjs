// The card grid owns its gaps: rows and columns spaced by the gap alone, not gap plus each child margin.
import { launch, BASE } from './lib/cdp.mjs';
import { suite } from './lib/check.mjs';
const { ok, end } = suite();
const b = await launch();
const E = (js) => b.evaluate(js);

await b.size(1440, 1000);
await b.goto(`${BASE}/demo/grid.html?theme=light`);
const r = await E(`(() => {
  const host = document.createElement('div');
  host.style.cssText = 'position:absolute;inset:0 auto auto 0;inline-size:900px;z-index:99999;background:#fff;padding:20px';
  host.innerHTML = \`
    <div class="ins-grid ins-grid--wide" id="g">
      <div class="ins-panel"><div class="ins-panel-body">a</div></div>
      <div class="ins-panel"><div class="ins-panel-body">b</div></div>
      <div class="ins-panel"><div class="ins-panel-body">c</div></div>
      <div class="ins-panel"><div class="ins-panel-body">d</div></div>
      <div class="ins-panel ins-mb-8"><div class="ins-panel-body">e</div></div>
      <div class="ins-panel"><div class="ins-panel-body">f</div></div>
    </div>
    <div class="ins-grid" id="g2"><div class="ins-stat">1</div><div class="ins-stat">2</div></div>
    <p id="after">after</p>\`;
  document.body.appendChild(host);
  const p = [...host.querySelectorAll('#g > .ins-panel')].map((x) => x.getBoundingClientRect());
  const g = getComputedStyle(document.getElementById('g'));
  const rowGap = parseFloat(g.rowGap), colGap = parseFloat(g.columnGap);
  const between = p.find((x) => x.top > p[0].bottom + 1).top - p[0].bottom;         // row 1 bottom -> row 2 top
  const beside = p[1].left > p[0].right ? p[1].left - p[0].right : p[0].left - p[1].right; // RTL-safe column spacing
  const rows = [...new Set(p.map((x) => Math.round(x.top)))];
  const marginE = parseFloat(getComputedStyle(host.querySelectorAll('#g > .ins-panel')[4]).marginBlockEnd);
  const gridBottom = parseFloat(g.marginBlockEnd);
  return { rowGap, colGap, between: Math.round(between * 100) / 100, beside: Math.round(beside * 100) / 100, rows, marginE, gridBottom };
})()`);
console.log('  ', JSON.stringify(r));
ok('panels in a grid: the space between rows is the grid gap, not gap + margin', Math.abs(r.between - r.rowGap) < 0.5, `${r.between}px vs gap ${r.rowGap}px`);
ok('rows and columns are spaced the same', Math.abs(r.between - r.beside) < 0.5, `${r.between} vs ${r.beside}`);
ok('a margin utility on a child still wins (ins-mb-8 = 32px)', r.marginE === 32, `${r.marginE}px`);
ok('the grid keeps its own margin below it', r.gridBottom > 0, `${r.gridBottom}px`);
ok('no console errors or exceptions', b.logs.length === 0, b.logs.join(' | '));
await b.close();
end();