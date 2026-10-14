// The docs' code listings: the «التفاف» toggle beside «نسخ» wraps one listing's long lines under their indentation, and copying still gives the code as written.
import { launch, BASE } from './lib/cdp.mjs';
import { suite } from './lib/check.mjs';
const { ok, end } = suite();
const b = await launch();
const E = (js) => b.evaluate(js);

// The longest listing on the page, its tools, and how its lines sit.
const look = () => E(`(() => {
  const all = [...document.querySelectorAll('.dx-code')];
  const box = all.reduce((a, c) => (c.querySelector('pre').scrollWidth > a.querySelector('pre').scrollWidth ? c : a));
  window.__box = box;
  const pre = box.querySelector('pre'), copy = box.querySelector('.dx-tools > .dx-copy'), wrap = box.querySelector('.dx-tools > .dx-wrap');
  const r = (el) => el.getBoundingClientRect();
  const lines = [...box.querySelectorAll('.dx-ln')];
  let hang = null;
  for (const line of lines) {
    const range = document.createRange();
    range.selectNodeContents(line);
    const rects = [...range.getClientRects()].filter((x) => x.width > 0);
    const tops = [...new Set(rects.map((x) => Math.round(x.top)))];
    const ind = Number(line.style.getPropertyValue('--dx-ind'));
    if (tops.length > 1 && ind > 0) {
      const first = tops[0];
      const later = rects.filter((x) => Math.round(x.top) !== first).map((x) => x.left);
      const words = document.createRange();
      const text = line.textContent, at = text.length - text.trimStart().length;
      let node = line, seen = 0;
      const walker = document.createTreeWalker(line, NodeFilter.SHOW_TEXT);
      while ((node = walker.nextNode())) { if (seen + node.length > at) break; seen += node.length; }
      words.setStart(node, at - seen); words.setEnd(node, at - seen + 1);
      hang = { ind, start: words.getBoundingClientRect().left, later: Math.min(...later), line: r(line).left };
      break;
    }
  }
  return { count: all.length, tools: all.every((c) => c.querySelector('.dx-tools > .dx-copy + .dx-wrap')),
    pressed: wrap.getAttribute('aria-pressed'), wrapped: box.classList.contains('is-wrap'), label: wrap.textContent,
    over: pre.scrollWidth - pre.clientWidth, lines: lines.length, hang,
    box: r(box).toJSON(), copy: r(copy).toJSON(), wrapBtn: r(wrap).toJSON(),
    others: all.filter((c) => c !== box && c.classList.contains('is-wrap')).length,
    color: getComputedStyle(wrap).color, brand: getComputedStyle(document.documentElement).getPropertyValue('--ins-tone-brand').trim() };
})()`);

for (const dir of ['rtl', 'ltr']) {
  await b.size(820, 1000);
  await b.goto(`${BASE}/demo/shell.html?theme=light&mode=advanced&dir=${dir}`);
  await E(`window.__copied = null; navigator.clipboard.writeText = (t) => { window.__copied = t; return Promise.resolve(); }; 0`);
  let s = await look();
  ok(`${dir}: every listing has copy then «التفاف»`, s.count > 2 && s.tools && s.label === 'التفاف', `${s.count} listings`);
  ok(`${dir}: off by default, the long lines scroll sideways`, s.pressed === 'false' && !s.wrapped && s.over > 40, `over ${s.over}`);
  ok(`${dir}: the buttons sit together at the listing's start corner, copy outermost`, dir === 'rtl'
    ? s.box.right - s.copy.right < 12 && s.wrapBtn.right <= s.copy.left + 0.5 && s.copy.left - s.wrapBtn.right < 8
    : s.copy.left - s.box.left < 12 && s.wrapBtn.left >= s.copy.right - 0.5 && s.wrapBtn.left - s.copy.right < 8, JSON.stringify([s.box, s.copy, s.wrapBtn]));
  const html = await E(`window.__box.querySelector('code').innerHTML`);
  const text = await E(`window.__box.querySelector('code').textContent.replace(/\\n$/, '')`);

  await E(`window.__box.querySelector('.dx-wrap').click(); 0`);
  s = await look();
  ok(`${dir}: on, the listing wraps instead of scrolling`, s.pressed === 'true' && s.wrapped && s.over <= 1, `over ${s.over}`);
  ok(`${dir}: on, the toggle shows its state in the brand`, s.color !== 'rgb(100, 116, 139)', s.color);
  ok(`${dir}: only that listing wraps`, s.others === 0);
  ok(`${dir}: a wrapped line continues under its own indentation`, s.hang && Math.abs(s.hang.later - s.hang.start) <= 1 && s.hang.later > s.hang.line + 4, JSON.stringify(s.hang));

  await E(`window.__box.querySelector('.dx-copy').click(); 0`);
  await new Promise((r) => setTimeout(r, 100));
  const copied = await E(`window.__copied`);
  ok(`${dir}: copy gives the code as written, not the wrapped lines`, copied === text, `${copied && copied.length} vs ${text.length}`);

  await E(`window.__box.querySelector('.dx-wrap').click(); 0`);
  s = await look();
  ok(`${dir}: a second click unwraps`, s.pressed === 'false' && !s.wrapped && s.over > 40 && s.lines === 0);
  ok(`${dir}: unwrapped, the listing is exactly as it was`, (await E(`window.__box.querySelector('code').innerHTML`)) === html);
}

await b.close();
end();
