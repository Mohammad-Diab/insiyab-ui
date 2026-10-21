// A table's selected-row marker flies to the newly selected row as the sidebar's marker does (far edge first, then the near edge); none with effects off.
import { launch, BASE } from './lib/cdp.mjs';
import { suite } from './lib/check.mjs';
const { ok, end } = suite();
const b = await launch();
const E = (js) => b.evaluate(js);

// Marks one row of the first table with three rows or more, then moves the mark and reads the flying bar with its animation paused, at every 5ms of the animation's own clock, so a busy machine cannot skew the frames.
const fly = () => E(`(async () => {
  const body = [...document.querySelectorAll('.ins-table-wrap > .ins-table > tbody')].find((t) => t.rows.length >= 3);
  const wrap = body.parentNode.parentNode, rows = [...body.rows];
  const frames = () => new Promise((r) => requestAnimationFrame(() => requestAnimationFrame(r)));
  const finish = async () => {
    await Promise.resolve();
    wrap.getAnimations({ subtree: true }).forEach((a) => { if (a.effect.getComputedTiming().endTime !== Infinity) a.finish(); });
    await frames();
  };
  rows.forEach((r) => { r.removeAttribute('aria-selected'); r.removeAttribute('aria-current'); r.classList.remove('is-selected'); });
  rows[0].classList.add('is-selected');
  await finish();
  const s = wrap.getBoundingClientRect();
  const spot = (tr) => { const td = tr.cells[0], r = td.getBoundingClientRect(), cs = getComputedStyle(td, '::before');
    const top = parseFloat(cs.top), bottom = parseFloat(cs.bottom);
    return { top: r.top + top - s.top + wrap.scrollTop, bottom: r.bottom - bottom - s.top + wrap.scrollTop }; };
  const a = spot(rows[0]), z = spot(rows[2]);
  rows[0].classList.remove('is-selected');
  rows[2].classList.add('is-selected');
  await Promise.resolve();
  const samples = [];
  const read = (ms) => {
    const bar = wrap.querySelector('.ins-table-flight');
    const r = bar && bar.getBoundingClientRect();
    samples.push({ ms, top: r ? r.top - s.top + wrap.scrollTop : null, bottom: r ? r.bottom - s.top + wrap.scrollTop : null, moving: wrap.classList.contains('ins-table-moving') });
  };
  const run = (wrap.querySelector('.ins-table-flight') || { getAnimations: () => [] }).getAnimations()[0];
  if (run) {
    run.pause();
    for (let ms = 0; ms <= 600; ms += 5) { run.currentTime = ms; read(ms); }
    const landed = new Promise((r) => run.addEventListener('finish', r));
    run.finish();
    await landed;
  } else {
    read(0);
    await frames();
    read(1);
  }
  return { a, z, samples, left: !!wrap.querySelector('.ins-table-flight'), moving: wrap.classList.contains('ins-table-moving') };
})()`);

await b.size(1440, 1000);
await b.goto(`${BASE}/demo/tables.html?theme=light`);
await new Promise((r) => setTimeout(r, 900));
let r = await fly();
const flying = r.samples.filter((s) => s.top !== null);
ok('a bar flies while the mark moves', flying.length > 5 && flying.every((s) => s.moving), `${flying.length} steps`);
ok('it starts on the old marker', flying.length > 0 && Math.abs(flying[0].top - r.a.top) < 2, JSON.stringify([flying[0], r.a]));
const reached = flying.find((s) => s.bottom >= r.z.bottom - 1.5);
ok('the far edge reaches the new row first, near a third of the way', !!reached && reached.ms > 120 && reached.ms < 330 && Math.abs(reached.top - r.a.top) < 6, JSON.stringify(reached));
ok('then the near edge follows to the new marker', flying.some((s) => Math.abs(s.top - r.z.top) < 2), JSON.stringify(flying[flying.length - 1]));
ok('it lands and hands back the real marker', !r.left && !r.moving);

// On the first and last rows the marker's outer end stays inside the card's rounded corner, bare and in a panel, in both directions.
const corners = () => E(`(() => {
  const rows = (sel) => [0, 1, 2].map((i) => '<tr' + (i === sel ? ' aria-selected="true"' : '') + '><td>اسم</td><td>دمشق</td></tr>').join('');
  const table = (sel) => '<div class="ins-table-wrap"><table class="ins-table"><thead><tr><th>أ</th><th>ب</th></tr></thead><tbody>' + rows(sel) + '</tbody></table></div>';
  const box = document.createElement('div');
  box.innerHTML = table(0) + table(2) + '<div class="ins-panel"><div class="ins-panel-body">' + table(0) + table(2) + '</div></div>';
  document.querySelector('.site-body').prepend(box);
  const rtl = document.documentElement.getAttribute('dir') === 'rtl';
  const out = [];
  box.querySelectorAll('tr[aria-selected="true"]').forEach((tr) => {
    const td = tr.cells[0], cs = getComputedStyle(td), m = getComputedStyle(td, '::before');
    const first = tr === tr.parentNode.rows[0];
    const r = parseFloat(cs[(first ? 'borderTop' : 'borderBottom') + (rtl ? 'Right' : 'Left') + 'Radius']);
    const side = parseFloat(cs[rtl ? 'borderRightWidth' : 'borderLeftWidth']);
    const end = parseFloat(first ? m.top : m.bottom);
    const d = r - end, curve = d > 0 ? r - Math.sqrt(r * r - d * d) : 0;
    out.push({ where: (td.closest('.ins-panel') ? 'panel' : 'bare') + ' ' + (first ? 'first' : 'last'), r, end, curve: +curve.toFixed(2), side });
  });
  box.remove();
  return out;
})()`);
for (const dir of ['rtl', 'ltr']) {
  await b.goto(`${BASE}/demo/tables.html?theme=light&dir=${dir}`);
  await new Promise((r) => setTimeout(r, 600));
  for (const c of await corners()) ok(`${dir} ${c.where} row: the marker's end clears the card's corner`, c.r > 0 && c.curve <= c.side + .5, JSON.stringify(c));
}

await b.goto(`${BASE}/demo/tables.html?theme=light&fx=off`);
await new Promise((r) => setTimeout(r, 600));
r = await fly();
ok('effects off: nothing flies', r.samples.every((s) => s.top === null && !s.moving));

await b.close();
end();
