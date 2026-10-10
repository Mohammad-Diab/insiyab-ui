// Pop-ups inside scrolling dialogs and drawers, and overlays stacked on overlays: placement, Escape, outside presses, focus, scroll lock, one dim, closedby.
import { inflateSync } from 'node:zlib';
import { launch, BASE } from './lib/cdp.mjs';
import { suite } from './lib/check.mjs';

const { ok, end } = suite();
const b = await launch({ width: 1280, height: 1000 });
const E = (js) => b.evaluate(js);
await b.size(1280, 1000);

const HELP = `window.__t = {
  shown(sel) { const el = document.querySelector(sel); if (!el) return false; try { return el.matches(':popover-open'); } catch (e) { return false; } },
  hits(sel) { const el = document.querySelector(sel), r = el.getBoundingClientRect();
    return [[.5, .5], [.15, .15], [.85, .15], [.15, .85], [.85, .85]].every(([fx, fy]) => { const h = document.elementFromPoint(r.left + r.width * fx, r.top + r.height * fy); return !!h && el.contains(h); }); },
  rect(sel) { const r = document.querySelector(sel).getBoundingClientRect(); return { x: r.left, y: r.top, w: r.width, h: r.height, b: r.bottom, r: r.right }; },
  modals() { return [...document.querySelectorAll('dialog')].filter((d) => d.open).map((d) => d.id || d.className); },
  ae() { const a = document.activeElement; return a ? (a.id || a.className || a.tagName) : null; },
  dims() { return [...document.querySelectorAll('dialog')].filter((d) => d.open && d.matches(':modal') && getComputedStyle(d, '::backdrop').opacity !== '0').length; }
}; 0`;

async function page(path, html) {
  await b.goto(`${BASE}/test/fixtures/${path}`);
  await E(HELP);
  if (html) await E(`document.body.insertAdjacentHTML('beforeend', ${JSON.stringify('<div id="host">' + html + '</div>')}); Insiyab.init(document.getElementById('host')); 0`);
  await b.sleep(150);
}
const at = async (sel, fx = .5, fy = .5) => {
  const p = await E(`(() => { const r = document.querySelector(${JSON.stringify(sel)}).getBoundingClientRect(); return { x: r.left + r.width * ${fx}, y: r.top + r.height * ${fy} }; })()`);
  await b.mouse(p.x, p.y);
  await b.sleep(250);
  return p;
};
const wheel = async (x, y, dy) => { await b.send('Input.dispatchMouseEvent', { type: 'mouseWheel', x, y, deltaX: 0, deltaY: dy }); await b.sleep(350); };
async function pixel(x, y) {
  const r = await b.send('Page.captureScreenshot', { format: 'png', clip: { x, y, width: 1, height: 1, scale: 1 } });
  const png = Buffer.from(r.result.data, 'base64');
  let p = 8;
  const idat = [];
  while (p < png.length) {
    const len = png.readUInt32BE(p), kind = png.toString('ascii', p + 4, p + 8);
    if (kind === 'IDAT') idat.push(png.subarray(p + 8, p + 8 + len));
    p += 12 + len;
  }
  const raw = inflateSync(Buffer.concat(idat));
  return [raw[1], raw[2], raw[3]].join(',');
}

const FIX = 'position:fixed;top:90px;left:560px;z-index:60';
const fill = (n, w = 'line') => Array.from({ length: n }, (_, i) => `<p style="margin:0">${w} ${i}</p>`).join('');
const dlg = (id, inner, cls = 'ins-dialog', attrs = '') => `<dialog class="${cls}" id="${id}"${attrs}><div class="ins-dialog-head"><h2 class="ins-dialog-title">${id}</h2><button class="ins-dialog-x" id="${id}-x" data-ins-dialog-close aria-label="x"></button></div><div class="ins-dialog-body" id="${id}-b">${inner}</div></dialog>`;
const menu = (id, items) => `<details class="ins-pop" id="${id}"><summary class="ins-btn ins-btn--secondary" id="${id}-s">menu</summary><div class="ins-pop-body">${items}</div></details>`;
const items = (n, p = 'item') => Array.from({ length: n }, (_, i) => `<button class="ins-pop-item" id="${p}${i}">${p} ${i}</button>`).join('');
const scrollTo = (body, trg, from = 'bottom') => E(`(() => { const body = document.getElementById('${body}'), t = document.getElementById('${trg}'); body.scrollTop = 0; const br = body.getBoundingClientRect(), tr = t.getBoundingClientRect(); body.scrollTop = ${from === 'bottom' ? "tr.bottom - br.bottom + 30" : "tr.top - br.top - 30"}; return 0; })()`);

// ------------------------------------------------------------------ F1, F7: a menu and a list in a scrolling dialog
await b.size(1280, 1300);
await page('palette.html', `<button class="ins-btn" id="op1" style="${FIX}" data-ins-dialog="#d1">open</button>${dlg('d1', fill(30) + menu('m1', items(7)) + '<div class="ins-combo" id="cb"><input class="ins-input" id="cb-in" autocomplete="off"><ul class="ins-combo-list">' + Array.from({ length: 9 }, (_, i) => `<li class="ins-combo-option">option ${i}</li>`).join('') + '</ul></div>')}`);
await at('#op1'); await b.sleep(450);
await scrollTo('d1-b', 'cb-in');
const sh0 = await E(`document.getElementById('d1-b').scrollHeight`);
await at('#cb-in'); await b.sleep(300);
ok('F1 combo in a dialog: the list is open in the top layer', await E(`__t.shown('#cb .ins-combo-list')`));
ok('F1 combo: the dialog body does not grow', (await E(`document.getElementById('d1-b').scrollHeight`)) === sh0, sh0);
ok('F1 combo: every part of the list takes the pointer (not clipped)', await E(`__t.hits('#cb .ins-combo-list')`));
const cr = await E(`({ list: __t.rect('#cb .ins-combo-list'), box: __t.rect('#cb') })`);
ok('F1 combo: as wide as its field, beside it', Math.abs(cr.list.w - cr.box.w) < 1 && Math.abs(cr.list.x - cr.box.x) < 1, JSON.stringify(cr));
const bp = await E(`(() => { const r = document.getElementById('d1-b').getBoundingClientRect(); return { x: r.left + 30, y: r.top + 60 }; })()`);
const st0 = await E(`document.getElementById('d1-b').scrollTop`);
await wheel(bp.x, bp.y, -40);
const follow = await E(`({ list: __t.rect('#cb .ins-combo-list'), box: __t.rect('#cb'), open: __t.shown('#cb .ins-combo-list'), st: document.getElementById('d1-b').scrollTop })`);
const beside = Math.min(Math.abs(follow.list.y - follow.box.b - 6), Math.abs(follow.box.y - follow.list.b - 6));
ok('F7 combo: the body still scrolls with the list open, and the list follows its field', follow.open && follow.st !== st0 && beside < 2, JSON.stringify(follow));
await E(`document.getElementById('d1-b').scrollTop = 0; 0`); await b.sleep(300);
ok('F7 combo: closes once its field has scrolled out of the body', !(await E(`__t.shown('#cb .ins-combo-list')`)));
await E(`document.getElementById('d1-b').scrollTop = 99999; 0`); await b.sleep(200);
const sh1 = await E(`document.getElementById('d1-b').scrollHeight`);
await at('#m1-s'); await b.sleep(300);
ok('F1 menu in a dialog: open in the top layer', await E(`__t.shown('#m1 > .ins-pop-body')`));
ok('F1 menu: the dialog body does not grow', (await E(`document.getElementById('d1-b').scrollHeight`)) === sh1);
ok('F1 menu: not clipped', await E(`__t.hits('#m1 > .ins-pop-body')`));
await at('#m1-s'); await b.sleep(300);
ok('F1 menu: a second click on the trigger closes it', !(await E(`document.getElementById('m1').open`)));
await at('#m1-s'); await b.sleep(250);
await at('#item2'); await b.sleep(250);
ok('F1 menu: choosing an item closes it and hands focus back', !(await E(`document.getElementById('m1').open`)) && (await E(`__t.ae()`)) === 'm1-s');
await b.key('Escape'); await b.sleep(400);

// ------------------------------------------------------------------ F2, F3: the pickers and the tooltip work over a modal
await b.size(1280, 1000);
await page('components.html', `<button class="ins-btn" id="op2" style="${FIX}" data-ins-dialog="#d2">open</button>${dlg('d2', '<div class="ins-input-group"><input class="ins-input" id="dd" type="date" data-ins-date value="2026-09-30"></div><input class="ins-input" id="tt" type="time" data-ins-time value="09:30"><button class="ins-btn" id="tipb" aria-label="Tip text here" data-ins-tip>tip</button>' + fill(20))}`);
await at('#op2'); await b.sleep(450);
await at('#dd'); await b.sleep(350);
ok('F2 calendar over a modal: it lives in the dialog', await E(`document.getElementById('d2').contains(document.querySelector('.ins-cal'))`));
await at('.ins-cal .ins-cal-day[data-date="2026-09-15"]'); await b.sleep(300);
ok('F2 calendar: a click on a day chooses it', (await E(`document.querySelector('#d2 input[type=hidden]').value`)) === '2026-09-15', await E(`document.querySelector('#d2 input[type=hidden]').value`));
await E(`document.getElementById('dd').focus(); 0`);
await b.key('ArrowDown'); await b.sleep(250);
ok('F2 calendar: ArrowDown takes focus into the grid', await E(`document.activeElement.classList.contains('ins-cal-day')`));
await b.key('Escape'); await b.sleep(300);
ok('F3 calendar: Escape closes it and not the dialog', !(await E(`__t.shown('.ins-cal')`)) && (await E(`document.getElementById('d2').open`)));
await at('#tt'); await b.sleep(350);
const col = await E(`(() => { const c = document.querySelector('.ins-time-pop .ins-drum-col'), r = c.getBoundingClientRect(); return { x: r.left + r.width / 2, y: r.top + r.height / 2, top: c.scrollTop }; })()`);
await wheel(col.x, col.y, 120);
ok('F2 time: the wheel turns the drum, not the dialog', (await E(`document.querySelector('.ins-time-pop .ins-drum-col').scrollTop`)) !== col.top && (await E(`document.getElementById('d2-b').scrollTop`)) === 0);
await b.key('Escape'); await b.sleep(250);
const tb = await E(`__t.rect('#tipb')`);
await b.mouseTo(tb.x + tb.w / 2, tb.y + tb.h / 2); await b.sleep(650);
const tip = await E(`__t.rect('#ins-tooltip')`);
await b.mouseTo(tip.x + tip.w / 2, tip.y + tip.h / 2); await b.sleep(400);
ok('F2 tooltip over a modal: the pointer can rest on it', await E(`__t.shown('#ins-tooltip')`));
await b.key('Escape'); await b.sleep(400);
ok('F3 tooltip: Escape hides the tip and leaves the dialog open', !(await E(`__t.shown('#ins-tooltip')`)) && (await E(`document.getElementById('d2').open`)));
await b.mouseTo(5, 5);
await b.key('Escape'); await b.sleep(400);

await page('phone.html', `<button class="ins-btn" id="op3" style="${FIX}" data-ins-dialog="#d3">open</button>${dlg('d3', '<div class="ins-input-group"><input class="ins-input" id="ph" data-ins-phone value="+966501234567"></div>')}`);
await at('#op3'); await b.sleep(450);
await at('#d3 .ins-phone-cc'); await b.sleep(350);
ok('F2 phone list over a modal: the search field has focus', await E(`document.activeElement.classList.contains('ins-phone-find')`));
await b.type('egy'); await b.sleep(200);
ok('F2 phone: typing filters the list', (await E(`document.querySelectorAll('.ins-phone-opt:not([hidden])').length`)) === 1);
await E(`document.querySelector('#d3 .ins-phone-cc').focus(); 0`);
await b.key('Escape'); await b.sleep(400);
ok('F3 phone: Escape with focus on its button closes the list, not the dialog', !(await E(`__t.shown('.ins-phone-pop')`)) && (await E(`document.getElementById('d3').open`)));
await at('#d3 .ins-phone-cc'); await b.sleep(300);
await E(`document.getElementById('d3').close(); 0`); await b.sleep(300);
ok('F3 phone: closing the dialog closes the list in it', !(await E(`__t.shown('.ins-phone-pop')`)));

await page('color.html', `<button class="ins-btn" id="op4" style="${FIX}" data-ins-dialog="#d4">open</button>${dlg('d4', '<div class="ins-input-group"><input type="color" id="co" value="#9b2c5e" data-ins-color></div>')}`);
await at('#op4'); await b.sleep(450);
await at('#d4 .ins-color-swatch'); await b.sleep(350);
ok('F2 colour picker over a modal: its area has focus and takes the pointer', (await E(`document.activeElement.classList.contains('ins-color-area')`)) && (await E(`__t.hits('.ins-color-pop')`)));

// ------------------------------------------------------------------ F4, F5, F6, F8: stacked
await page('palette.html', `<div style="height:3000px"></div><div style="${FIX}">${menu('mA', '<button class="ins-pop-item" id="mA-i" data-ins-dialog="#dA">open dialog</button>')}</div>${dlg('dA', menu('mF', '<button class="ins-pop-item" id="mF-i" data-ins-dialog="#dB">open dialog B</button><button class="ins-pop-item" id="mF-c" data-ins-confirm="Sure?">ask</button>') + '<button class="ins-btn" id="dA-toast" data-ins-toast="Saved">toast</button>' + fill(10))}${dlg('dB', '<button class="ins-btn" id="dB-btn">x</button>', 'ins-dialog ins-dialog--sm')}`);
await E(`window.scrollTo(0, 400); 0`); await b.sleep(200);
const y0 = await E(`Math.round(window.scrollY)`);
await at('#mA-s'); await at('#mA-i'); await b.sleep(450);
ok('F6 scroll lock: the page is locked while a modal is open', (await E(`getComputedStyle(document.documentElement).overflow`)) === 'hidden');
await wheel(30, 500, 400);
ok('F6 scroll lock: a wheel over the backdrop does not scroll the page', (await E(`Math.round(window.scrollY)`)) === y0);
await at('#mF-s'); await at('#mF-i'); await b.sleep(450);
ok('F5 stacked: the dialog opened from a menu item in a dialog is on top', (await E(`__t.modals().join()`)) === 'dA,dB' && (await E(`__t.hits('#dB')`)));
await b.key('Escape'); await b.sleep(400);
ok('F5 focus: back on the summary of the menu inside the dialog', (await E(`__t.ae()`)) === 'mF-s', await E(`__t.ae()`));
ok('F6 scroll lock: still held with one modal left', (await E(`getComputedStyle(document.documentElement).overflow`)) === 'hidden');
await at('#mF-s'); await at('#mF-c'); await b.sleep(450);
await b.key('Escape'); await b.sleep(400);
ok('F5 focus: a confirm asked from a menu item hands focus back to the summary', (await E(`__t.ae()`)) === 'mF-s' && (await E(`document.getElementById('dA').open`)), await E(`__t.ae()`));
await at('#dA-toast'); await b.sleep(400);
ok('F8 toast from a dialog: above its backdrop and under the pointer', await E(`__t.hits('.ins-toast')`));
await at('.ins-toast'); await b.sleep(100);
ok('F8 toast: a click on it dismisses it', await E(`!!document.querySelector('.ins-toast.is-going') || !document.querySelector('.ins-toast')`));
await b.key('Escape'); await b.sleep(400);
ok('F5 focus: a dialog opened from a page menu item returns focus to that menu', (await E(`__t.ae()`)) === 'mA-s', await E(`__t.ae()`));
ok('F6 scroll lock: released once every modal is closed, at the same place', (await E(`getComputedStyle(document.documentElement).overflow`)) !== 'hidden' && (await E(`Math.round(window.scrollY)`)) === y0);

await page('palette.html', `<button class="ins-btn" id="op5" style="${FIX}" data-ins-dialog="#dr">open</button>${dlg('dr', menu('mD', items(3, 'dm')) + fill(10), 'ins-drawer')}`);
await at('#op5'); await b.sleep(600);
await at('#mD-s'); await b.sleep(250);
const drR = await E(`__t.rect('#dr')`);
const outX = drR.x > 200 ? 40 : 1240;
await b.mouse(outX, 500); await b.sleep(450);
ok('F4 drawer: an outside press closes its open menu and not the drawer', !(await E(`document.getElementById('mD').open`)) && (await E(`document.getElementById('dr').open`)));
await b.mouse(outX, 500); await b.sleep(500);
ok('F4 drawer: the next outside press closes the drawer', !(await E(`document.getElementById('dr').open`)));
ok('F4 drawer: focus back on its opener', (await E(`__t.ae()`)) === 'op5');

// ------------------------------------------------------------------ F9: one dim, whatever the depth
await page('palette.html', `<button class="ins-btn" id="op6" style="${FIX}" data-ins-dialog="#e1">open</button>${dlg('e1', '<button class="ins-btn" id="e1-btn" data-ins-dialog="#e2">next</button>', 'ins-drawer')}${dlg('e2', '<button class="ins-btn" id="e2-btn" data-ins-dialog="#pal">palette</button>', 'ins-dialog ins-dialog--sm')}`);
const SX = 20, SY = 980;
await at('#op6'); await b.sleep(700);
const px1 = await pixel(SX, SY), n1 = await E(`__t.dims()`);
await at('#e1-btn'); await b.sleep(700);
const px2 = await pixel(SX, SY), n2 = await E(`__t.dims()`);
await at('#e2-btn'); await b.sleep(700);
const px3 = await pixel(SX, SY), n3 = await E(`__t.dims()`);
ok('F9 one backdrop shows at each depth (drawer, dialog, palette)', n1 === 1 && n2 === 1 && n3 === 1, [n1, n2, n3].join(','));
ok('F9 the page behind is dimmed the same at depth 1, 2 and 3', px1 === px2 && px2 === px3, [px1, px2, px3].join(' | '));
ok('F9 the lower modals sit under the top one\'s tint', await E(`document.getElementById('e1').hasAttribute('data-ins-under') && document.getElementById('e2').hasAttribute('data-ins-under') && !document.getElementById('pal').hasAttribute('data-ins-under')`));
await b.key('Escape'); await b.sleep(500);
ok('F9 closing the top gives the one below its backdrop back', !(await E(`document.getElementById('e2').hasAttribute('data-ins-under')`)) && (await E(`__t.dims()`)) === 1 && (await pixel(SX, SY)) === px1);

// ------------------------------------------------------------------ closedby
await page('palette.html', `<button class="ins-btn" id="op7" style="${FIX}" data-ins-dialog="#c1">a</button><button class="ins-btn" id="op8" style="${FIX};top:150px" data-ins-dialog="#c2">b</button><button class="ins-btn" id="op9" style="${FIX};top:210px" data-ins-dialog="#c3">c</button><button class="ins-btn" id="op10" style="${FIX};top:270px" data-ins-dialog="#c4">d</button>`
  + dlg('c1', menu('mC', items(2, 'cm')) + fill(5), 'ins-dialog', ' closedby="any"')
  + dlg('c2', fill(5), 'ins-drawer', ' closedby="closerequest"')
  + dlg('c3', '<button class="ins-btn" id="c3-close" data-ins-dialog-close>close</button>', 'ins-dialog', ' closedby="none"')
  + dlg('c4', fill(5)));
await at('#op7'); await b.sleep(450);
await at('#mC-s'); await b.sleep(250);
await b.mouse(30, 500); await b.sleep(450);
ok('closedby="any" on a dialog: the first outside press closes its menu only', !(await E(`document.getElementById('mC').open`)) && (await E(`document.getElementById('c1').open`)));
await b.mouse(30, 500); await b.sleep(450);
ok('closedby="any": the next outside press closes the dialog', !(await E(`document.getElementById('c1').open`)));
await at('#op8'); await b.sleep(600);
const c2 = await E(`__t.rect('#c2')`);
await b.mouse(c2.x > 200 ? 40 : 1240, 500); await b.sleep(450);
ok('closedby="closerequest" on a drawer: an outside press leaves it open', await E(`document.getElementById('c2').open`));
await b.key('Escape'); await b.sleep(450);
ok('closedby="closerequest": Escape closes it', !(await E(`document.getElementById('c2').open`)));
await at('#op9'); await b.sleep(450);
await b.key('Escape'); await b.sleep(300);
await b.mouse(30, 500); await b.sleep(300);
ok('closedby="none": neither Escape nor an outside press closes it', await E(`document.getElementById('c3').open`));
await at('#c3-close'); await b.sleep(400);
ok('closedby="none": its own button does', !(await E(`document.getElementById('c3').open`)));
await at('#op10'); await b.sleep(450);
await b.mouse(30, 500); await b.sleep(400);
ok('a dialog without closedby ignores an outside press', await E(`document.getElementById('c4').open`));
await b.key('Escape'); await b.sleep(400);
await page('palette.html', `<button class="ins-btn" id="op11" style="${FIX}" data-ins-dialog="#c5">e</button>${dlg('c5', fill(5), 'ins-drawer')}`);
await at('#op11'); await b.sleep(600);
const c5 = await E(`__t.rect('#c5')`);
const inX = c5.x + 40, inY = c5.y + 120, outsideX = c5.x > 200 ? 40 : 1240;
await b.send('Input.dispatchMouseEvent', { type: 'mouseMoved', x: inX, y: inY });
await b.send('Input.dispatchMouseEvent', { type: 'mousePressed', x: inX, y: inY, button: 'left', clickCount: 1 });
await b.send('Input.dispatchMouseEvent', { type: 'mouseMoved', x: outsideX, y: 500, button: 'left' });
await b.send('Input.dispatchMouseEvent', { type: 'mouseReleased', x: outsideX, y: 500, button: 'left', clickCount: 1 });
await b.sleep(400);
ok('a drawer stays open when a press starts inside it and ends outside', await E(`document.getElementById('c5').open`));
await b.mouse(outsideX, 500); await b.sleep(500);
ok('a drawer without closedby still closes on an outside press', !(await E(`document.getElementById('c5').open`)));

const errs = b.logs.filter((l) => !/favicon/.test(l));
ok('no console errors or exceptions', errs.length === 0, errs.join(' | '));
await b.close();
end();
