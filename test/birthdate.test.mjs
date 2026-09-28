// The birth date plugin: the form field and its popover, the inline layout, typing rules, the columns, validation, reset, the API and English.
import { launch, BASE } from './lib/cdp.mjs';
import { suite } from './lib/check.mjs';

const { ok, end } = suite();
const b = await launch();
const E = (js) => b.evaluate(js);
const TY = new Date().getFullYear();

const view = (id) => E(`(() => {
  const i = document.getElementById(${JSON.stringify(id)}), box = i.nextElementSibling;
  const part = (k) => box.querySelector('.ins-birth-seg[data-part="' + k + '"] input');
  const col = (k) => box.querySelector('.ins-birth-col--' + k);
  return { value: i.value, parts: ['d', 'm', 'y'].map((k) => part(k).value).join('/'),
    cols: ['d', 'm', 'y'].map((k) => (col(k).classList.contains('is-unset') ? '~' : '') + ((col(k).querySelector('.is-on') || {}).textContent || '')).join(' '),
    line: box.querySelector(':scope > .ins-birth-line').textContent, focus: (document.activeElement.closest('.ins-birth-seg') || {}).dataset?.part || '',
    msg: part('d').validationMessage, open: !!box.querySelector('.ins-birth-pop:popover-open') };
})()`);
const type = async (s) => { for (const ch of s) { await b.send('Input.insertText', { text: ch }); await b.sleep(30); } await b.sleep(120); };
const fresh = async (id) => { await E(`document.querySelectorAll('.ins-birth-pop:popover-open').forEach((p) => p.hidePopover()); Insiyab.birth('#${id}', ''); document.querySelector('#${id} + .ins-birth input').focus(); 0`); await b.sleep(80); };
const openPop = async (id) => { await b.click(`#${id} + .ins-birth .ins-birth-btn`); await b.sleep(400); };
const colAt = (id, k) => E(`JSON.stringify((() => { const r = document.querySelector('#${id} + .ins-birth .ins-birth-col--${k}').getBoundingClientRect(); return { x: r.x + r.width / 2, y: r.y + r.height / 2, h: document.querySelector('#${id} + .ins-birth .ins-birth-col--${k} .ins-drum-item').offsetHeight }; })())`).then(JSON.parse);
const paste = (id, text) => E(`(() => { const i = document.querySelector('#${id} + .ins-birth input'); const dt = new DataTransfer(); dt.setData('text/plain', ${JSON.stringify(text)}); i.dispatchEvent(new ClipboardEvent('paste', { clipboardData: dt, bubbles: true, cancelable: true })); return 0; })()`);

await b.size(1280, 1000);
await b.goto(`${BASE}/test/fixtures/birthdate.html`);
await E(`window.__ev = []; document.addEventListener('ins:birth', (e) => __ev.push(e.detail.input.id + ':' + e.detail.value)); window.__ch = 0; document.getElementById('b1').addEventListener('change', () => __ch++); 0`);

// ------------------------------------------------------------------ the form field
const built = await E(`(() => {
  const i = document.getElementById('b1'), box = i.nextElementSibling, ins = [...box.querySelectorAll('.ins-birth-in')];
  const x = (k) => box.querySelector('.ins-birth-seg[data-part="' + k + '"] .ins-birth-box').getBoundingClientRect();
  const control = box.querySelector('.ins-birth-control').getBoundingClientRect(), input = document.createElement('input');
  input.className = 'ins-input'; document.querySelector('#f1 .ins-field').appendChild(input);
  const h = input.getBoundingClientRect().height; input.remove();
  return { hidden: i.type === 'hidden' && i.name === 'birth', three: ins.length === 3,
    modes: ins.map((n) => n.inputMode + ':' + n.autocomplete).join(','), rtl: x('d').left > x('m').left && x('m').left > x('y').left,
    height: Math.abs(control.height - h) < 1.5, hh: control.height + '/' + h, label: box.getAttribute('aria-labelledby') === document.querySelector('label[for="b1"]').id,
    btn: !!box.querySelector('.ins-birth-btn[aria-haspopup="dialog"][aria-expanded="false"]'), closed: !box.querySelector('.ins-birth-pop:popover-open') };
})()`);
ok('the date input becomes the hidden value holder, keeping its name', built.hidden);
ok('three real fields, numeric keypad and birthday autofill', built.three && built.modes === 'decimal:bday-day,decimal:bday-month,decimal:bday-year', built.modes);
ok('day, month and year run right to left on an Arabic page', built.rtl);
ok('in a form it is as tall as any other input', built.height, built.hh);
ok('the group is named by the field label', built.label);
ok('a button opens the columns, which start closed', built.btn && built.closed);
ok('the line under the field stays empty until there is a date', (await view('b1')).line === '');
ok('the columns start at 16 June, age 31, not chosen yet', (await view('b1')).cols === `~16 ~يونيو ~${TY - 31}`, (await view('b1')).cols);

// ------------------------------------------------------------------ typing
await fresh('b1'); await type('2/3/93');
let v = await view('b1');
ok('2/3/93 is 2 March 1993', v.value === '1993-03-02' && v.parts === '02/03/1993', v.parts);
ok('the columns follow what is typed', v.cols === '2 مارس 1993', v.cols);
ok('the line under the field reads the date back', /الثلاثاء 2 مارس 1993/.test(v.line) && /العمر/.test(v.line), v.line);
ok('change and ins:birth fire with the ISO value', (await E(`__ch`)) >= 1 && (await E(`__ev.at(-1)`)) === 'b1:1993-03-02', await E(`__ev.at(-1)`));
await fresh('b1'); await type('4');
v = await view('b1');
ok('a day that cannot take a second digit gets its zero and moves on', v.parts === '04//' && v.focus === 'm', `${v.parts} ${v.focus}`);
await fresh('b1'); await type('22193');
ok('a digit that does not fit carries into the next field', (await view('b1')).value === '1993-01-22', (await view('b1')).parts);
await fresh('b1'); await type('2'); await b.key('Enter'); await type('3'); await b.key('Tab'); await type('1993');
ok('Enter and Tab close a one-digit field too', (await view('b1')).value === '1993-03-02', (await view('b1')).parts);
await fresh('b1'); await type('1.5.12');
ok('a two-digit year fills its century', (await view('b1')).value === '2012-05-01', (await view('b1')).parts);
await fresh('b1'); await type('٢٢١٩٣');
ok('Arabic-Indic digits are read as Latin', (await view('b1')).value === '1993-01-22');
await fresh('b1'); await type('3102');
ok('an impossible date cannot be typed: no 31 February', (await view('b1')).parts === '31/0/', (await view('b1')).parts);
await fresh('b1'); await type('2203'); await b.key('Backspace'); await b.key('Backspace'); await b.key('Backspace');
v = await view('b1');
ok('Backspace in an empty field steps back into the one before', v.parts === '2//' && v.focus === 'd', `${v.parts} ${v.focus}`);
await fresh('b1'); await paste('b1', '29 رجب 1413'); await b.sleep(100);
ok('a pasted Hijri date is understood', (await view('b1')).value === '1993-01-22', (await view('b1')).parts);
await fresh('b1'); await paste('b1', '22 كانون الثاني 93'); await b.sleep(100);
ok('a pasted Levantine month name is understood', (await view('b1')).value === '1993-01-22');

// ------------------------------------------------------------------ the popover
await fresh('b1');
const size = () => E(`(() => { const box = document.querySelector('#b1 + .ins-birth'); box.style.inlineSize = 'max-content'; const r = box.getBoundingClientRect(); box.style.inlineSize = ''; return r.width + 'x' + r.height; })()`);
const before = await size(); await E(`Insiyab.birth('#b1', '1993-01-22'); 0`); const after = await size();
ok('choosing a date does not change the size of the field', before === after, `${before} ${after}`);
await fresh('b1');
await openPop('b1');
const pop = await E(`(() => { const box = document.querySelector('#b1 + .ins-birth'), p = box.querySelector('.ins-birth-pop'), c = box.querySelector('.ins-birth-control').getBoundingClientRect(), r = p.getBoundingClientRect();
  return { open: p.matches(':popover-open'), expanded: box.querySelector('.ins-birth-btn').getAttribute('aria-expanded'), below: r.top >= c.bottom && r.top - c.bottom < 12,
    start: Math.abs(r.right - c.right) < 1, inside: r.left >= 0 && r.right <= innerWidth, focus: document.activeElement.classList.contains('ins-birth-col--d'), line: p.querySelector('.ins-birth-line').textContent }; })()`);
ok('the button opens the columns in a popover', pop.open && pop.expanded === 'true');
ok('the popover opens under the field, at its start edge, on screen', pop.below && pop.start && pop.inside, JSON.stringify(pop));
ok('focus goes to the first column still to choose', pop.focus);
ok('the popover says what to do', pop.line === 'اكتب التاريخ أو اسحب الأعمدة', pop.line);
await b.key('ArrowDown'); await b.sleep(600);
v = await view('b1');
ok('the arrow keys move a column and fill its field', v.parts === '17//' && !v.cols.startsWith('~'), `${v.parts} ${v.cols}`);
let p = await colAt('b1', 'm');
await b.send('Input.dispatchMouseEvent', { type: 'mouseWheel', x: p.x, y: p.y, deltaX: 0, deltaY: 60 });
await b.sleep(700);
v = await view('b1');
ok('the wheel on a column chooses its value', v.parts.split('/')[1] !== '' && !v.cols.split(' ')[1].startsWith('~'), `${v.parts} ${v.cols}`);
p = await colAt('b1', 'y');
const M = (t, x, y) => b.send('Input.dispatchMouseEvent', { type: t, x, y, button: 'left', buttons: t === 'mouseReleased' ? 0 : 1, clickCount: 1 });
await M('mousePressed', p.x, p.y);
for (let i = 1; i <= 12; i++) { await M('mouseMoved', p.x, p.y - p.h * 3 * i / 12); await b.sleep(30); }
await M('mouseReleased', p.x, p.y - p.h * 3);
await b.sleep(800);
v = await view('b1');
ok('dragging a column with the mouse moves it row for row', v.parts.split('/')[2] === String(TY - 31 + 3), v.parts);
ok('once all three are chosen the date is complete', /^\d{4}-\d{2}-17$/.test(v.value) && v.open, v.value);
await b.key('Escape'); await b.sleep(150);
v = await view('b1');
ok('Esc closes the popover and puts back the date it opened with', !v.open && v.value === '' && v.parts === '//', `${v.open} ${v.value} ${v.parts}`);
await fresh('b1'); await openPop('b1');
await b.key('Enter'); await b.sleep(150);
v = await view('b1');
ok('Enter chooses the middle value and moves to the next column', v.parts === '16//' && (await E(`document.activeElement.classList.contains('ins-birth-col--m')`)), v.parts);
await b.key('Enter'); await b.sleep(150); await b.key('Enter'); await b.sleep(250);
v = await view('b1');
ok('Enter on the last column closes it with the date chosen', !v.open && v.value === `${TY - 31}-06-16` && v.focus === 'y', `${v.open} ${v.value} ${v.focus}`);
await openPop('b1'); await b.key('ArrowDown'); await b.sleep(600);
await E(`document.querySelector('#b1 + .ins-birth .ins-birth-foot .ins-btn--bare').click(); 0`); await b.sleep(150);
v = await view('b1');
ok('Cancel closes it and puts back the date it opened with', !v.open && v.value === `${TY - 31}-06-16`, `${v.open} ${v.value}`);
await E(`document.querySelector('#b1 + .ins-birth .ins-birth-in').focus(); 0`);
await b.key('ArrowDown', 1); await b.sleep(200);
ok('Alt+ArrowDown in the field opens it', (await view('b1')).open);
await E(`document.querySelector('#b1 + .ins-birth .ins-birth-foot .ins-btn').click(); 0`); await b.sleep(150);
v = await view('b1');
ok('Done closes it and returns to the field', !v.open && v.focus !== '', `${v.open} ${v.focus}`);
await fresh('b1');
await b.click('#b1 + .ins-birth .ins-birth-seg[data-part="m"]'); await b.sleep(400);
v = await view('b1');
ok('a click anywhere in the field opens the columns and keeps focus where it landed', v.open && v.focus === 'm', `${v.open} ${v.focus}`);
await type('3'); v = await view('b1');
ok('and typing still fills the field with the columns open', v.open && v.parts === '/03/', `${v.open} ${v.parts}`);
for (const type of ['mousePressed', 'mouseReleased']) await b.send('Input.dispatchMouseEvent', { type, x: 4, y: 996, button: 'left', clickCount: 1 });
await b.sleep(250);
ok('a click outside closes them', !(await view('b1')).open);

// ------------------------------------------------------------------ the inline layout
const inline = await E(`(() => { const box = document.querySelector('#b6 + .ins-birth'); const x = (k) => box.querySelector('.ins-birth-seg[data-part="' + k + '"] .ins-birth-box').getBoundingClientRect(); const c = (k) => box.querySelector('.ins-birth-col--' + k).getBoundingClientRect();
  return { cls: box.classList.contains('ins-birth--inline'), noBtn: !box.querySelector('.ins-birth-btn, .ins-birth-pop'), shown: c('y').height > 0,
    aligned: ['d', 'm', 'y'].every((k) => Math.abs(x(k).left - c(k).left) < 1 && Math.abs(x(k).width - c(k).width) < 1), line: box.querySelector('.ins-birth-line').textContent }; })()`);
ok('data-ins-birth="inline" shows the columns in place, with no popover', inline.cls && inline.noBtn && inline.shown);
ok('there each column sits under its field', inline.aligned);
ok('and the line says what to do', inline.line === 'اكتب التاريخ أو اسحب الأعمدة', inline.line);

// ------------------------------------------------------------------ validation
await fresh('b1');
await E(`document.getElementById('f1-send').click(); 0`);
await b.sleep(150);
ok('an empty required field blocks the form and says so', !(await E(`window.__sent1`)) && (await E(`document.querySelector('#f1 .ins-error').textContent`)) === 'اكتب تاريخ ميلادك', await E(`document.querySelector('#f1 .ins-error').textContent`));
await fresh('b1'); await type('22');
ok('a half-typed date is not valid', (await view('b1')).msg === 'أكمل تاريخ الميلاد', (await view('b1')).msg);
await type('0193');
await E(`document.getElementById('f1-send').click(); 0`);
await b.sleep(100);
ok('a complete date lets the form go', (await E(`window.__sent1`)) === 1);

// ------------------------------------------------------------------ options
v = await view('b2');
ok('a value on the date input fills the field', v.parts === '22/01/1993' && v.value === '1993-01-22', v.parts);
ok('data-ins-age-min ends the year column at that age', (await E(`document.querySelector('#b2 + .ins-birth .ins-birth-col--y').lastElementChild.textContent`)) === String(TY - 18));
await fresh('b2'); await type('1/1/' + String(TY - 10));
ok('a year that makes the person too young is refused, with the reason', (await view('b2')).value === '' && /18 سنة/.test((await view('b2')).line), (await view('b2')).line);
ok('data-ins-age-start moves the starting year', (await view('b5')).cols === `~16 ~يونيو ~${TY - 25}`, (await view('b5')).cols);

// ------------------------------------------------------------------ API and reset
await E(`Insiyab.birth('#b1', '1993-01-22'); window.__evn = __ev.length; Insiyab.birth('#b1', '2000-02-29'); 0`);
await b.sleep(80);
ok('Insiyab.birth(field, iso) sets it, quietly', (await view('b1')).parts === '29/02/2000' && (await E(`__ev.length === __evn`)));
ok('Insiyab.birth(field) reads it, even 29 February over an earlier year', (await E(`Insiyab.birth('#b1')`)) === '2000-02-29', await E(`Insiyab.birth('#b1')`));
await E(`Insiyab.birth('#b2', ''); document.getElementById('f2').reset(); 0`);
await b.sleep(120);
ok('a form reset brings back the first value', (await view('b2')).parts === '22/01/1993', (await view('b2')).parts);

// ------------------------------------------------------------------ language, state, size
const en = await E(`(() => { const box = document.querySelector('#b3 + .ins-birth'); const x = (k) => box.querySelector('.ins-birth-seg[data-part="' + k + '"] .ins-birth-box').getBoundingClientRect().left;
  return { ltr: x('d') < x('m') && x('m') < x('y'), ph: box.querySelector('.ins-birth-ph').textContent, btn: box.querySelector('.ins-birth-btn').getAttribute('aria-label'), m: box.querySelector('.ins-birth-col--m .is-on').textContent }; })()`);
ok('an English field runs left to right, in English', en.ltr && en.ph === 'Day' && en.btn === 'Pick from the columns' && en.m === 'June', JSON.stringify(en));
ok('a disabled field is dimmed and its fields and button disabled', (await E(`(() => { const box = document.querySelector('#b4 + .ins-birth'); return getComputedStyle(box).opacity < 1 && box.querySelector('input').disabled && box.querySelector('.ins-birth-btn').disabled; })()`)));
await b.size(360, 800);
await b.sleep(150);
ok('the field and the inline layout fit a 360px screen', await E(`['#b1', '#b6'].every((id) => { const r = document.querySelector(id + ' + .ins-birth').getBoundingClientRect(); const s = document.querySelector(id + ' + .ins-birth .ins-birth-segs').getBoundingClientRect(); return s.left >= 0 && s.right <= innerWidth && r.right <= innerWidth; })`));
await openPop('b1');
ok('the popover fits a 360px screen', await E(`(() => { const r = document.querySelector('#b1 + .ins-birth .ins-birth-pop').getBoundingClientRect(); return r.left >= 0 && r.right <= innerWidth; })()`));

ok('no console errors, warnings or exceptions', b.logs.length === 0, b.logs.join(' | '));
await b.close();
end();
