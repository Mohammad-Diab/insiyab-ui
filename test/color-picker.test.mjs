// The colour picker plugin: the field kept and sending #rrggbb, typed hex in its
// three forms, the invalid message, the picker's square (pointer and keys), the hue
// slider, presets (default and given), the contrast readout, events, closing,
// reset and the API.
import { launch, BASE } from './lib/cdp.mjs';
import { suite } from './lib/check.mjs';

const { ok, end } = suite();
const b = await launch();
const E = (js) => b.evaluate(js);
const raw = async (key, code, vk, mods = 0) => {
  const base = { key, code, windowsVirtualKeyCode: vk, nativeVirtualKeyCode: vk, modifiers: mods };
  await b.send('Input.dispatchKeyEvent', { type: 'rawKeyDown', ...base });
  await b.send('Input.dispatchKeyEvent', { type: 'keyUp', ...base });
  await b.sleep(60);
};
const val = (id) => E(`document.getElementById('${id}').value`);
const pop = () => E(`(() => { const p = document.querySelector('.ins-color-pop'); if (!p || !p.matches(':popover-open')) return null;
  return { presets: [...p.querySelectorAll('.ins-color-preset')].map((x) => x.dataset.value), hidden: p.querySelector('.ins-color-swatches').hidden,
           focus: document.activeElement.className, text: p.querySelector('.ins-color-readout').textContent,
           pass: [...p.querySelectorAll('.ins-color-ratio')].map((r) => r.classList.contains('is-pass')),
           hue: p.querySelector('.ins-color-hue').value, valuetext: p.querySelector('.ins-color-area').getAttribute('aria-valuetext') }; })()`);

await b.size(1280, 900);
await b.goto(`${BASE}/test/fixtures/color.html`);
await E(`window.__c = []; document.addEventListener('ins:color', (e) => __c.push(e.detail.input.id + ':' + e.detail.value));
  window.__in = 0; window.__ch = 0; const i = document.getElementById('c-brand'); i.addEventListener('input', () => __in++); i.addEventListener('change', () => __ch++); 0`);

// ------------------------------------------------------------------ the field
const built = await E(`(() => { const i = document.getElementById('c-brand'), g = i.closest('.ins-input-group');
  return { type: i.type, name: i.name, value: i.value, dir: g.dir, cls: g.classList.contains('ins-color'), first: g.firstElementChild.className,
           swatch: getComputedStyle(g.firstElementChild).backgroundColor, label: g.firstElementChild.getAttribute('aria-label'),
           bare: document.getElementById('c-bare').parentNode.classList.contains('ins-input-group'), bareValue: document.getElementById('c-bare').value }; })()`);
ok('the field becomes a hex field, keeping its name and value', built.type === 'text' && built.name === 'brand' && built.value === '#9b2c5e', JSON.stringify(built));
ok('with a swatch in front, in a left-to-right group', built.cls && built.dir === 'ltr' && built.first === 'ins-color-swatch' && built.swatch === 'rgb(155, 44, 94)', built.swatch);
ok('the swatch is named in the page\'s language', built.label === 'اختر لونًا');
ok('a field outside a group gets one, its value written in lower case', built.bare && built.bareValue === '#0f766e');
ok('the form sends #rrggbb', (await E(`new FormData(document.getElementById('cf')).get('brand')`)) === '#9b2c5e');

await E(`(() => { const i = document.getElementById('c-bare'); i.focus(); i.select(); return 0; })()`);
await b.type('abc');
ok('three hex digits typed already colour the swatch', (await E(`getComputedStyle(document.querySelector('#c-bare').previousElementSibling).backgroundColor`)) === 'rgb(170, 187, 204)');
await E(`document.activeElement.blur(); 0`);
ok('and are written out in full when the field is left', (await val('c-bare')) === '#aabbcc');
await E(`(() => { const i = document.getElementById('c-bare'); i.focus(); i.select(); return 0; })()`);
await b.type('#12');
await E(`document.activeElement.blur(); 0`);
ok('a hex that is not a colour is invalid, with a message', !(await E(`document.getElementById('c-bare').validity.valid`)) && (await E(`document.getElementById('c-bare').validationMessage`)) === 'اكتب لونًا مثل #9B2C5E.');
await E(`(() => { const i = document.getElementById('c-bare'); i.focus(); i.select(); return 0; })()`);
await b.type('0F766E');
await raw('Enter', 'Enter', 13);
ok('Enter sets it, and the field is valid again', (await val('c-bare')) === '#0f766e' && (await E(`document.getElementById('c-bare').validity.valid`)));

// ------------------------------------------------------------------ the picker
await b.click('.ins-color:has(#c-brand) .ins-color-swatch');
await b.sleep(200);
let p = await pop();
ok('the swatch opens the picker, with the square focused', p && /ins-color-area/.test(p.focus), JSON.stringify(p));
ok('the presets are the brand colour and a palette', p.presets[0] === '#9b2c5e' && p.presets.length === 10, JSON.stringify(p.presets));
ok('the contrast of the colour on white and on black', /على الأبيض/.test(p.text) && /على الأسود/.test(p.text) && /:1/.test(p.text), p.text);
ok('the brand colour passes on white and fails on black', JSON.stringify(p.pass) === JSON.stringify([true, false]), JSON.stringify(p.pass));
ok('the hue slider sits at the colour\'s hue', Math.abs(+p.hue - 333) <= 2, p.hue);
ok('the square says where it is', /تشبّع \d+٪، سطوع \d+٪/.test(p.valuetext), p.valuetext);

const before = await val('c-brand');
await raw('ArrowUp', 'ArrowUp', 38, 8);
ok('Shift+↑ brightens by ten', (await val('c-brand')) !== before && (await E(`document.querySelector('.ins-color-area').getAttribute('aria-valuetext')`)).includes('سطوع 71٪'), await E(`document.querySelector('.ins-color-area').getAttribute('aria-valuetext')`));
await raw('ArrowLeft', 'ArrowLeft', 37, 8);
ok('Shift+← takes saturation off', (await E(`document.querySelector('.ins-color-area').getAttribute('aria-valuetext')`)).startsWith('تشبّع 62٪'), await E(`document.querySelector('.ins-color-area').getAttribute('aria-valuetext')`));
ok('each key sets the colour: input, change and ins:color', (await E(`__in`)) >= 2 && (await E(`__ch`)) === 2 && (await E(`__c.filter((x) => x.startsWith('c-brand')).length`)) === 2, `${await E('__in')} ${await E('__ch')} ${await E('__c.length')}`);

const area = await E(`(() => { const r = document.querySelector('.ins-color-area').getBoundingClientRect(); return { x: r.left, y: r.top, w: r.width, h: r.height }; })()`);
/* Pressed just inside: the square's corners are rounded, and so is where it can be hit. */
await b.send('Input.dispatchMouseEvent', { type: 'mouseMoved', x: area.x + area.w - 12, y: area.y + 12 });
await b.send('Input.dispatchMouseEvent', { type: 'mousePressed', x: area.x + area.w - 12, y: area.y + 12, button: 'left', clickCount: 1 });
const chBefore = await E(`__ch`);
await b.send('Input.dispatchMouseEvent', { type: 'mouseMoved', x: area.x + area.w / 2, y: area.y + area.h / 2, button: 'left', buttons: 1 });
await b.sleep(60);
ok('dragging moves the colour live, without a change yet', (await E(`__ch`)) === chBefore && (await val('c-brand')) !== '#9b2c5e');
/* Past the corner, outside the square: the pointer is captured, so it clamps. */
await b.send('Input.dispatchMouseEvent', { type: 'mouseMoved', x: area.x - 30, y: area.y + area.h + 30, button: 'left', buttons: 1 });
await b.send('Input.dispatchMouseEvent', { type: 'mouseReleased', x: area.x - 30, y: area.y + area.h + 30, button: 'left', clickCount: 1 });
await b.sleep(60);
ok('dragged past the corner and let go: clamped to black, and set', (await val('c-brand')) === '#000000' && (await E(`__ch`)) === chBefore + 1, await val('c-brand'));
ok('black on white passes, on black fails', JSON.stringify((await pop()).pass) === JSON.stringify([true, false]));

await E(`(() => { const h = document.querySelector('.ins-color-hue'); h.value = '120'; h.dispatchEvent(new Event('input', { bubbles: true })); h.dispatchEvent(new Event('change', { bubbles: true })); return 0; })()`);
ok('a black stays black when the hue moves', (await val('c-brand')) === '#000000');
await b.click('.ins-color-preset[data-value="#0f766e"]');
await b.sleep(80);
ok('a preset sets the colour', (await val('c-brand')) === '#0f766e' && (await E(`__c.at(-1)`)) === 'c-brand:#0f766e');
ok('and the picker follows it', Math.abs(+(await pop()).hue - 175) <= 2, (await pop()).hue);
await raw('Escape', 'Escape', 27);
await b.sleep(150);
ok('Escape closes it and focuses the swatch', !(await pop()) && (await E(`document.activeElement.className`)) === 'ins-color-swatch');

await b.click('.ins-color:has(#c-bare) .ins-color-swatch');
await b.sleep(150);
ok('given presets, cleaned: duplicates and non-colours dropped', JSON.stringify((await pop()).presets) === JSON.stringify(['#ff0000', '#00ff00']), JSON.stringify((await pop()).presets));
await b.mouse(5, 5);
await b.sleep(150);
ok('a press outside closes it', !(await pop()));
await b.click('.ins-color:has(#c-en) .ins-color-swatch');
await b.sleep(150);
p = await pop();
ok('data-ins-color-swatches="none": no presets', p.hidden);
ok('in English where the field is English', /on white/.test(p.text) && /fine for text|too low for text/.test(p.text), p.text);
await raw('Escape', 'Escape', 27);

// ------------------------------------------------------------------ reset, API
await E(`document.getElementById('cf').reset(); 0`);
await b.sleep(80);
ok('a form reset brings back the first colours', (await val('c-brand')) === '#9b2c5e' && (await val('c-bare')) === '#0f766e');
const n = await E(`__c.length`);
ok('Insiyab.colorField(field, colour) sets it quietly', (await E(`Insiyab.colorField('#c-brand', '#ABC')`)) === '#aabbcc' && (await E(`__c.length`)) === n);
ok('Insiyab.colorField(field) reads it', (await E(`Insiyab.colorField('#c-brand')`)) === '#aabbcc');
ok('the core\'s colour maths is still Insiyab.color', (await E(`typeof Insiyab.color.contrast`)) === 'function');

ok('no console errors, warnings or exceptions', b.logs.length === 0, b.logs.join(' | '));
await b.close();
end();
