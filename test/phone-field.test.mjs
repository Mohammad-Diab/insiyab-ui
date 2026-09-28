// The phone plugin: the E.164 value behind a national display, grouping as typed
// with the caret kept, Backspace over a space, Arabic-Indic digits, + and 00
// numbers that pick their own country (shared codes narrowed), the length check,
// countries without rules, the searchable picker, the allowed list, the API and a
// form reset.
import { launch, BASE } from './lib/cdp.mjs';
import { suite } from './lib/check.mjs';

const { ok, end } = suite();
const b = await launch();
const E = (js) => b.evaluate(js);

const view = (id) => E(`(() => {
  const i = document.getElementById(${JSON.stringify(id)}), g = i.closest('.ins-input-group');
  const h = g.nextElementSibling, btn = g.querySelector('.ins-phone-cc');
  return { text: i.value, value: h.value, name: h.name, iso: btn.querySelector('.ins-phone-iso').textContent, dial: btn.querySelector('.ins-phone-dial').textContent,
           label: btn.getAttribute('aria-label'), valid: i.validity.valid, msg: i.validationMessage, caret: i.selectionStart, ph: i.placeholder };
})()`);
const empty = (id) => E(`(() => { const i = document.getElementById(${JSON.stringify(id)}); i.focus(); i.value = ''; i.dispatchEvent(new InputEvent('input', { bubbles: true, inputType: 'deleteContentBackward' })); return 0; })()`);
const paste = async (text) => { await b.send('Input.insertText', { text }); await b.sleep(80); };
const raw = async (key, code, vk) => {
  const base = { key, code, windowsVirtualKeyCode: vk, nativeVirtualKeyCode: vk };
  await b.send('Input.dispatchKeyEvent', { type: 'rawKeyDown', ...base });
  await b.send('Input.dispatchKeyEvent', { type: 'keyUp', ...base });
  await b.sleep(80);
};
const leave = () => E(`document.activeElement.blur(); 0`);
const pop = () => E(`(() => { const p = document.querySelector('.ins-phone-pop'); if (!p || !p.matches(':popover-open')) return null;
  const opts = [...p.querySelectorAll('.ins-phone-opt:not([hidden])')];
  const a = p.querySelector('.ins-phone-opt.is-active');
  return { isos: opts.map((o) => o.dataset.iso), names: opts.slice(0, 4).map((o) => o.querySelector('.ins-phone-opt-name').textContent),
           active: a && a.dataset.iso, selected: (p.querySelector('.ins-phone-opt[aria-selected=true]') || {}).dataset?.iso,
           sep: !!p.querySelector('.ins-phone-sep:not([hidden])'), focused: document.activeElement === p.querySelector('.ins-phone-find'),
           none: !p.querySelector('.ins-phone-none').hidden }; })()`);
const find = async (q) => {
  await E(`(() => { const f = document.querySelector('.ins-phone-find'); f.value = ''; f.dispatchEvent(new Event('input')); return 0; })()`);
  if (q) await b.type(q);
  await b.sleep(40);
};

await b.size(1280, 900);
await b.goto(`${BASE}/test/fixtures/phone.html`);
await E(`window.__ph = []; document.addEventListener('ins:phone', (e) => __ph.push(e.detail.input.id + ':' + e.detail.value + ':' + e.detail.country + ':' + e.detail.valid)); 0`);

// ------------------------------------------------------------------ what it builds
let v = await view('p-sa');
const built = await E(`(() => { const i = document.getElementById('p-sa'), g = i.closest('.ins-input-group');
  return { dir: g.dir, cls: g.classList.contains('ins-phone'), first: g.firstElementChild.className, name: i.hasAttribute('name'),
           type: i.type, mode: i.inputMode, auto: i.autocomplete, hiddenType: g.nextElementSibling.type }; })()`);
ok('the field shows the number as it is written at home', v.text === '050 123 4567', v.text);
ok('and the form carries E.164, under the field\'s name', v.value === '+966501234567' && v.name === 'mobile' && !built.name && built.hiddenType === 'hidden', JSON.stringify(v));
ok('a country button at the start of a left-to-right group', built.dir === 'ltr' && built.cls && built.first === 'ins-phone-cc');
ok('a phone keyboard and the browser\'s phone autofill', built.type === 'tel' && built.mode === 'tel' && built.auto === 'tel');
ok('the button names the country in the page\'s language', v.iso === 'SA' && v.dial === '+966' && /السعودية/.test(v.label) && /\+966/.test(v.label), v.label);
ok('a Saudi example as the placeholder', (await view('p-plain')).ph === '050 123 4567', (await view('p-plain')).ph);
ok('with no country given, a plain Arabic page starts in Saudi Arabia', (await view('p-plain')).iso === 'SA');
ok('lang="ar-EG" starts in Egypt, with an Egyptian example', (await view('p-eg')).iso === 'EG' && (await view('p-eg')).ph === '0100 123 4567', JSON.stringify(await view('p-eg')));
ok('data-ins-phone-country wins', (await view('p-ae')).iso === 'AE');
v = await view('p-us');
ok('a US number: (201) 555-0123, +12015550123', v.text === '(201) 555-0123' && v.value === '+12015550123', JSON.stringify(v));
ok('a field outside a group gets one', (await E(`document.getElementById('p-br').parentNode.classList.contains('ins-input-group')`)));

// ------------------------------------------------------------------ typing
await b.click('#p-plain');
await b.type('5');
v = await view('p-plain');
ok('the first digit gets its trunk 0, and the caret stays after the digit', v.text === '05' && v.caret === 2, JSON.stringify(v));
await b.type('01');
v = await view('p-plain');
ok('grouped as it is typed, caret at the end', v.text === '050 1' && v.caret === 5, JSON.stringify(v));
await raw('Backspace', 'Backspace', 8);
await E(`(() => { const i = document.getElementById('p-plain'); i.setSelectionRange(4, 4); return 0; })()`);
await raw('Backspace', 'Backspace', 8);
v = await view('p-plain');
ok('Backspace just after a space takes the digit before it', v.text === '05' && v.caret === 2, JSON.stringify(v));
await empty('p-plain');
await b.type('0');
ok('a lone 0 stays on screen', (await view('p-plain')).text === '0');
await b.type('501234567');
v = await view('p-plain');
ok('a whole number typed with its 0', v.text === '050 123 4567' && v.value === '+966501234567' && v.valid, JSON.stringify(v));
await leave();
ok('ins:phone says so, valid', (await E(`__ph.at(-1)`)) === 'p-plain:+966501234567:SA:true', await E(`__ph.at(-1)`));
await empty('p-plain');
await b.type('٠٥٠١٢٣٤٥٦٧');
ok('Arabic-Indic digits are read and shown Latin', (await view('p-plain')).text === '050 123 4567' && (await view('p-plain')).value === '+966501234567');
await empty('p-plain');
await b.type('05012');
v = await view('p-plain');
ok('too short for the country: invalid, and the message names it', !v.valid && /السعودية/.test(v.msg), v.msg);
await empty('p-plain');
await paste('966501234567');
ok('the dial code typed without + is understood', (await view('p-plain')).value === '+966501234567');

// ------------------------------------------------------------------ other countries
await empty('p-plain');
await b.type('+971501234567');
v = await view('p-plain');
ok('a + number picks its country as it is typed', v.iso === 'AE' && v.value === '+971501234567', JSON.stringify(v));
ok('and is left as typed until the field is left', v.text === '+971501234567');
await leave();
v = await view('p-plain');
ok('then written the way the country writes it', v.text === '050 123 4567' && v.placeholder !== '', v.text);
await empty('p-plain');
await paste('00965 5001 2345');
await leave();
v = await view('p-plain');
ok('00 works like +: Kuwait, no trunk 0', v.iso === 'KW' && v.value === '+96550012345' && v.text === '5001 2345', JSON.stringify(v));
await empty('p-plain');
await paste('+1 876 555 1234');
ok('+1 narrowed by its area code: Jamaica', (await view('p-plain')).iso === 'JM' && (await view('p-plain')).value === '+18765551234');
await empty('p-plain');
await paste('+77011234567');
ok('+7 narrowed: Kazakhstan', (await view('p-plain')).iso === 'KZ');
await empty('p-plain');
await paste('+74951234567');
ok('+7 otherwise: Russia', (await view('p-plain')).iso === 'RU');
await empty('p-plain');
await paste('+9');
await leave();
v = await view('p-plain');
ok('an unfinished country code is invalid, and says why', !v.valid && v.msg === 'اكتب رمز الدولة كاملًا بعد +.' && v.value === '', v.msg);
await b.click('#p-br');
await b.type('11987654321');
v = await view('p-br');
ok('a country without rules: digits kept, E.164 made, length within 15', v.text === '11987654321' && v.value === '+5511987654321' && v.valid, JSON.stringify(v));
await b.click('#p-de');
await b.type('015123456789');
v = await view('p-de');
ok('Germany: checked but not grouped, the 0 kept on screen', v.text === '015123456789' && v.value === '+4915123456789' && v.valid, JSON.stringify(v));
await leave();

// ------------------------------------------------------------------ the picker
await b.click('#p-ae ~ .x, .ins-phone:has(#p-ae) .ins-phone-cc');
await b.sleep(250);
let p = await pop();
ok('the button opens the list, with the search focused', p && p.focused, JSON.stringify(p));
await b.click('.ins-phone:has(#p-ae) .ins-phone-cc'); await b.sleep(250);
ok('a second click on the button closes it', !(await pop()));
await b.click('.ins-phone:has(#p-ae) .ins-phone-cc'); await b.sleep(250);
p = await pop();
ok('preferred countries first, then a separator', JSON.stringify(p.isos.slice(0, 3)) === JSON.stringify(['AE', 'SA', 'KW']) && p.sep, JSON.stringify(p.isos.slice(0, 4)));
ok('the current country is selected and active', p.selected === 'AE' && p.active === 'AE');
ok('names in Arabic, from the browser', /الإمارات/.test(p.names[0]) && p.isos.length > 240, `${p.names[0]} · ${p.isos.length}`);
await find('مصر');
ok('search by Arabic name', (await pop()).isos[0] === 'EG');
await find('الامارات');
ok('without the hamza too', (await pop()).isos[0] === 'AE', JSON.stringify((await pop()).isos));
await find('egy');
ok('by English name', (await pop()).isos[0] === 'EG');
await find('+44');
ok('by dial code: every +44 country', JSON.stringify((await pop()).isos.slice().sort()) === JSON.stringify(['GB', 'GG', 'IM', 'JE']), JSON.stringify((await pop()).isos));
await find('uae');
ok('by a short name: UAE', (await pop()).isos[0] === 'AE', JSON.stringify((await pop()).isos.slice(0, 3)));
await find('ksa');
ok('KSA', (await pop()).isos[0] === 'SA', JSON.stringify((await pop()).isos.slice(0, 3)));
await find('امريكا');
ok('and an everyday Arabic one: أمريكا', (await pop()).isos[0] === 'US', JSON.stringify((await pop()).isos.slice(0, 3)));
await find('بريطانيا');
ok('بريطانيا', (await pop()).isos[0] === 'GB', JSON.stringify((await pop()).isos.slice(0, 3)));
await find('zzz');
ok('nothing found says so', (await pop()).none);
await find('قطر');
await b.key('Enter');
await b.sleep(150);
v = await view('p-ae');
ok('Enter chooses: the button follows, the list closes', v.iso === 'QA' && !(await pop()), JSON.stringify(v));
ok('focus goes back to the number, with a Qatari example', (await E(`document.activeElement.id`)) === 'p-ae' && v.ph === '3312 3456', v.ph);
await b.type('33123456');
ok('the number is read in the new country', (await view('p-ae')).value === '+97433123456' && (await view('p-ae')).text === '3312 3456');
await E(`document.querySelector('.ins-phone:has(#p-ae) .ins-phone-cc').focus(); 0`);
await raw('ArrowDown', 'ArrowDown', 40);
await b.sleep(150);
ok('ArrowDown on the button opens it', !!(await pop()));
await raw('ArrowDown', 'ArrowDown', 40);
const next = (await pop()).active;
ok('ArrowDown moves the highlight', next && next !== 'QA', next);
await b.key('Escape');
await b.sleep(150);
ok('a closing list lets clicks through while it fades', (await E(`getComputedStyle(document.querySelector('.ins-phone-pop')).pointerEvents`)) === 'none');
ok('Escape closes it and focuses the button, the country unchanged', !(await pop()) && (await E(`document.activeElement.classList.contains('ins-phone-cc')`)) && (await view('p-ae')).iso === 'QA');
await b.click('.ins-phone:has(#p-ae) .ins-phone-cc');
await b.sleep(150);
await b.mouse(5, 5);
await b.sleep(150);
ok('a press outside closes it', !(await pop()));
await b.click('.ins-phone:has(#p-ae) .ins-phone-cc');
await b.sleep(150);
await b.click('.ins-phone-opt[data-iso="BH"]');
await b.sleep(150);
ok('a click chooses too, and the number follows', (await view('p-ae')).iso === 'BH' && (await view('p-ae')).value === '+97333123456', JSON.stringify(await view('p-ae')));

// ------------------------------------------------------------------ only these countries
await b.click('.ins-phone:has(#p-only) .ins-phone-cc');
await b.sleep(150);
ok('data-ins-phone-only lists only those', JSON.stringify((await pop()).isos.slice().sort()) === JSON.stringify(['AE', 'SA']), JSON.stringify((await pop()).isos));
await b.key('Escape');
await b.click('#p-only');
await paste('+201001234567');
await leave();
v = await view('p-only');
ok('a number from elsewhere is refused, with a reason', !v.valid && v.value === '' && /مصر/.test(v.msg) && v.iso === 'SA', JSON.stringify(v));

// ------------------------------------------------------------------ English, API, reset
v = await view('p-us');
ok('an English field is labelled in English', /^Country: United States \+1$/.test(v.label), v.label);
await b.click('#p-us');
await E(`document.getElementById('p-us').setSelectionRange(99, 99); 0`);
await raw('Backspace', 'Backspace', 8);
ok('and complains in English', /valid phone number for United States/.test((await view('p-us')).msg), (await view('p-us')).msg);
ok('Insiyab.phone() reads it', JSON.stringify(await E(`Insiyab.phone('#p-sa')`)) === JSON.stringify({ value: '+966501234567', country: 'SA', valid: true }));
await E(`Insiyab.phone('#p-sa', '+97142345678'); 0`);
v = await view('p-sa');
ok('Insiyab.phone(field, number) sets it, country and all', v.iso === 'AE' && v.text === '04 234 5678' && v.value === '+97142345678', JSON.stringify(v));
ok('what the form submits', (await E(`new FormData(document.getElementById('pf')).get('mobile')`)) === '+97142345678');
await E(`document.getElementById('pf').reset(); 0`);
await b.sleep(80);
v = await view('p-sa');
ok('a form reset brings back the number and its country', v.iso === 'SA' && v.value === '+966501234567' && v.text === '050 123 4567', JSON.stringify(v));

await E(`Insiyab.theme('dark'); 0`);
await b.sleep(300);
await b.click('#p-plain');
await b.mouseTo(5, 5);
await b.sleep(500);
ok('in the dark theme, a focused grouped field paints no box of its own inside the group',
  (await E(`getComputedStyle(document.getElementById('p-plain')).backgroundColor`)) === 'rgba(0, 0, 0, 0)', await E(`getComputedStyle(document.getElementById('p-plain')).backgroundColor`));
await E(`Insiyab.theme('light'); 0`);

ok('no console errors, warnings or exceptions', b.logs.length === 0, b.logs.join(' | '));
await b.close();
end();
