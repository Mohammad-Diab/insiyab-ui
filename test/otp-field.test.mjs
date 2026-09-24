// The one-time code plugin: one real input under drawn boxes, typing at the end,
// Arabic-Indic digits, a paste that keeps only the code, the complete event and the
// opt-in submit, letters when asked for, the turned-down state, the API and a reset.
import { launch, BASE } from './lib/cdp.mjs';
import { suite } from './lib/check.mjs';

const { ok, end } = suite();
const b = await launch();
const E = (js) => b.evaluate(js);

const view = (id) => E(`(() => {
  const i = document.getElementById(${JSON.stringify(id)}), cells = [...i.parentNode.querySelectorAll('.ins-otp-cell')];
  return { value: i.value, cells: cells.map((c) => c.textContent).join('|'), active: cells.findIndex((c) => c.classList.contains('is-active')),
           caret: i.selectionStart, focused: document.activeElement === i, msg: i.validationMessage, valid: i.validity.valid };
})()`);
const clear = (id) => E(`(() => { const i = document.getElementById(${JSON.stringify(id)}); i.value = ''; i.dispatchEvent(new Event('input', { bubbles: true })); return 0; })()`);
const paste = async (text) => { await b.send('Input.insertText', { text }); await b.sleep(80); };
const raw = async (key, code, vk) => {
  const base = { key, code, windowsVirtualKeyCode: vk, nativeVirtualKeyCode: vk };
  await b.send('Input.dispatchKeyEvent', { type: 'rawKeyDown', ...base });
  await b.send('Input.dispatchKeyEvent', { type: 'keyUp', ...base });
  await b.sleep(80);
};

await b.size(1280, 900);
await b.goto(`${BASE}/test/fixtures/otp.html`);
await E(`window.__otp = []; document.addEventListener('ins:otp', (e) => __otp.push(e.detail.input.id + ':' + e.detail.value)); 0`);

// ------------------------------------------------------------------ what it builds
const built = await E(`(() => {
  const i = document.getElementById('otp'), box = i.parentNode, row = box.querySelector('.ins-otp-cells');
  return { box: box.className, dir: box.dir, cells: row.children.length, hidden: row.getAttribute('aria-hidden'),
           type: i.type, mode: i.inputMode, auto: i.autocomplete, max: i.hasAttribute('maxlength'), name: i.name, idir: i.dir,
           lookless: !i.classList.contains('ins-input'), inField: box.parentNode.classList.contains('ins-field'),
           five: document.querySelectorAll('#otp-max ~ .ins-otp-cells .ins-otp-cell').length,
           ten: document.querySelectorAll('#otp-ten ~ .ins-otp-cells .ins-otp-cell').length };
})()`);
ok('one input, wrapped, with six drawn boxes hidden from assistive tech', built.box === 'ins-otp' && built.cells === 6 && built.hidden === 'true', JSON.stringify(built));
ok('it keeps its name, and reads left to right', built.name === 'code' && built.dir === 'ltr' && built.idir === 'ltr');
ok('a numeric keyboard and the SMS code suggestion', built.type === 'text' && built.mode === 'numeric' && built.auto === 'one-time-code');
ok('no maxlength left to cut a paste short', !built.max);
ok('the boxes are the look, not the input', built.lookless && built.inField);
ok('the length can come from maxlength, and go to ten', built.five === 5 && built.ten === 10, `${built.five} ${built.ten}`);

// ------------------------------------------------------------------ typing
await b.click('#otp');
await b.type('12');
let v = await view('otp');
ok('typed digits fill the boxes in order', v.cells === '1|2||||' && v.value === '12', v.cells);
ok('the next box is the active one', v.active === 2 && v.focused, v.active);
await b.type('٣٤');
ok('Arabic-Indic digits are read as Latin', (await view('otp')).value === '1234');
await b.type('a-');
ok('anything but a digit is ignored', (await view('otp')).value === '1234');
v = await view('otp');
ok('an incomplete code is invalid, with a message', !v.valid && v.msg === 'أدخل الرمز كاملًا: 6 أرقام.', v.msg);
await raw('ArrowLeft', 'ArrowLeft', 37);
await raw('Home', 'Home', 36);
ok('arrow keys do not move the caret off the end', (await view('otp')).caret === 4, (await view('otp')).caret);
await b.type('56');
v = await view('otp');
ok('the last digit completes it: valid, all boxes full', v.valid && v.cells === '1|2|3|4|5|6' && v.value === '123456', JSON.stringify(v));
ok('with the last box still marked while focused', v.active === 5);
ok('ins:otp fires once, with the code', JSON.stringify(await E(`__otp`)) === JSON.stringify(['otp:123456']), JSON.stringify(await E(`__otp`)));
ok('and the form is not submitted without the opt-in', !(await E(`window.__sent1`)));
await b.type('7');
ok('a seventh digit is refused, and nothing fires again', (await view('otp')).value === '123456' && (await E(`__otp.length`)) === 1);
await raw('Backspace', 'Backspace', 8);
v = await view('otp');
ok('Backspace takes the last digit off', v.value === '12345' && v.cells === '1|2|3|4|5|' && v.active === 5, JSON.stringify(v));
await b.type('9');
ok('completing it again fires again', (await E(`__otp.at(-1)`)) === 'otp:123459');
await E(`document.activeElement.blur(); 0`);
await b.sleep(60);
ok('no box is marked once it loses focus', (await view('otp')).active === -1);

// ------------------------------------------------------------------ paste, submit
await b.click('#otp-submit');
await paste('رمزك هو ٦٥٤ ٣٢١ صالح لخمس دقائق');
ok('a pasted message keeps only the code', (await view('otp-submit')).value === '654321', (await view('otp-submit')).value);
ok('data-ins-otp-submit submits the form when complete', (await E(`window.__sent2`)) === 1);
await clear('otp-submit');
await paste('123 456');
ok('a code pasted with a space keeps its last digit', (await view('otp-submit')).value === '123456');

// ------------------------------------------------------------------ letters
await b.click('#otp-alnum');
await b.type('ab1c');
v = await view('otp-alnum');
ok('with data-ins-otp-chars="alnum", letters too, upper-cased', v.value === 'AB1C' && v.cells === 'A|B|1|C', v.value);
ok('and a text keyboard', (await E(`document.getElementById('otp-alnum').inputMode`)) === 'text');

// ------------------------------------------------------------------ turned down
const red = () => E(`getComputedStyle(document.querySelector('#otp ~ .ins-otp-cells .ins-otp-cell')).borderTopColor`);
const before = await red();
await E(`document.getElementById('otp').setAttribute('aria-invalid', 'true'); 0`);
await b.sleep(400);                                 /* the border eases over */
ok('aria-invalid turns the boxes red', (await red()) !== before, `${before} → ${await red()}`);
await b.click('#otp');
await raw('Backspace', 'Backspace', 8);
ok('and comes off as the code is typed again', !(await E(`document.getElementById('otp').hasAttribute('aria-invalid')`)));

// ------------------------------------------------------------------ API, reset, the rest
ok('Insiyab.otp() reads the code', (await E(`Insiyab.otp('#otp')`)) === '12345');
const n = await E(`__otp.length`);
await E(`Insiyab.otp('#otp', '٩٨٧٦٥٤'); 0`);
ok('Insiyab.otp(field, code) sets it, cleaned, quietly', (await view('otp')).cells === '9|8|7|6|5|4' && (await E(`__otp.length`)) === n);
await E(`Insiyab.otp('#otp', ''); 0`);
ok("Insiyab.otp(field, '') clears it", (await view('otp')).cells === '|||||');
await E(`Insiyab.otp('#otp', '1234'); document.getElementById('f1').reset(); 0`);
await b.sleep(80);
ok('a form reset empties the boxes too', (await view('otp')).cells === '|||||' && (await view('otp')).value === '');
await b.click('#otp-en');
await b.type('12');
ok('an English field says so in English', (await view('otp-en')).msg === 'Enter all 4 digits.', (await view('otp-en')).msg);
ok('a disabled field is dimmed', (await E(`getComputedStyle(document.getElementById('otp-off').parentNode).opacity`)) < 1);

await b.size(360, 800);
await b.sleep(150);
const fit = await E(`(() => { const r = document.querySelector('#otp-ten').parentNode.getBoundingClientRect(); return r.right <= innerWidth && r.left >= 0; })()`);
ok('ten boxes still fit a 360px screen', fit);

ok('no console errors, warnings or exceptions', b.logs.length === 0, b.logs.join(' | '));
await b.close();
end();
