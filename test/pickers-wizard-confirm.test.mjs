// Toggle buttons and groups, range, stepper, indeterminate, tiles, the autocomplete,
// the date and range picker, the wizard and confirm, driven with real key and
// pointer events on test/fixtures/components.html.
import { launch, BASE } from './lib/cdp.mjs';
import { suite } from './lib/check.mjs';

const { ok, end } = suite();
const b = await launch();
const E = (js) => b.evaluate(js);

await b.size(1440, 1000);
await b.goto(`${BASE}/test/fixtures/components.html`);

// ------------------------------------------------------------------ toggle + group
await b.click('[data-ins-toggle][aria-label="مائل"]');
ok('toggle: click presses', await E(`document.querySelector('[data-ins-toggle][aria-label="مائل"]').getAttribute('aria-pressed') === 'true'`));
await b.sleep(400);
ok('toggle: pressed look', await E(`/155, 44, 94, 0\\.14\\)/.test(getComputedStyle(document.querySelector('[data-ins-toggle][aria-label="مائل"]')).backgroundColor)`),
  await E(`getComputedStyle(document.querySelector('[data-ins-toggle][aria-label="مائل"]')).backgroundColor`));
const g = await E(`(() => { const btns = [...document.querySelectorAll('.ins-btn-group')[1].children]; const cs = (e) => getComputedStyle(e);
  return { firstRight: btns[0].getBoundingClientRect().left > btns[2].getBoundingClientRect().left,
           fOuter: cs(btns[0]).borderStartStartRadius, fInner: cs(btns[0]).borderStartEndRadius,
           mid: cs(btns[1]).borderStartStartRadius, lOuter: cs(btns[2]).borderEndEndRadius }; })()`);
ok('group: first button is rightmost in RTL', g.firstRight);
ok('group: outer corners round, inner square', g.fOuter === '14px' && g.fInner === '0px' && g.mid === '0px' && g.lOuter === '14px', JSON.stringify(g));

// ------------------------------------------------------------------ range, stepper, indeterminate, tiles
ok('range: fill set on load', await E(`document.getElementById('p-vol').style.getPropertyValue('--ins-fill').startsWith('calc(30.000%')`),
  await E(`document.getElementById('p-vol').style.getPropertyValue('--ins-fill')`));
await E(`document.getElementById('p-vol').focus()`);
await b.key('ArrowUp');
ok('range: key moves value, fill and output follow', await E(`(() => { const r = document.getElementById('p-vol'); return r.value === '31' && r.style.getPropertyValue('--ins-fill').startsWith('calc(31.000%') && document.getElementById('p-vol-out').value === '31'; })()`));

ok('stepper: minus disabled at min', await E(`document.querySelector('[data-ins-spin="down"]').disabled === true`));
await b.click('[data-ins-spin="up"]');
ok('stepper: plus steps up', await E(`document.getElementById('p-qty').value === '2' && !document.querySelector('[data-ins-spin="down"]').disabled`));
ok('stepper: buttons are type=button', await E(`[...document.querySelectorAll('.ins-spin')].every(x => x.type === 'button')`));
await E(`document.getElementById('p-qty').value = '9'; document.getElementById('p-qty').dispatchEvent(new Event('input', { bubbles: true }))`);
await b.click('[data-ins-spin="up"]');
ok('stepper: plus disabled at max', await E(`document.getElementById('p-qty').value === '10' && document.querySelector('[data-ins-spin="up"]').disabled`));
ok('indeterminate from markup', await E(`document.getElementById('p-all').indeterminate === true`));
ok('tile: checked tile tinted', await E(`(() => { const t = document.querySelector('.ins-tile input[value="pro"]').closest('.ins-tile'); return getComputedStyle(t).boxShadow.includes('inset'); })()`));
await b.click('.ins-tile input[value="basic"] + .ins-tile-title');
ok('tile: click chooses it', await E(`document.querySelector('.ins-tile input[value="basic"]').checked`));

// ------------------------------------------------------------------ autocomplete
const vis = `[...document.querySelectorAll('#combo-city .ins-combo-option')].filter(o => !o.hidden).map(o => o.dataset.value).join(',')`;
await b.click('#p-city');
ok('combo: roles stamped', await E(`(() => { const i = document.getElementById('p-city'); return i.getAttribute('role') === 'combobox' && document.querySelector('#combo-city .ins-combo-list').getAttribute('role') === 'listbox'; })()`));
ok('combo: click opens with all options', await E(`document.getElementById('p-city').getAttribute('aria-expanded') === 'true'`) && (await E(vis)).split(',').length === 8);
await b.type('ادلب');
ok('combo: hamza-less alef finds إدلب', (await E(vis)) === 'idl', await E(vis));
await b.key('ArrowDown');
ok('combo: ArrowDown activates option', await E(`(() => { const i = document.getElementById('p-city'); const a = document.getElementById(i.getAttribute('aria-activedescendant')); return a && a.dataset.value === 'idl'; })()`));
await b.key('Enter');
ok('combo: Enter chooses', await E(`document.getElementById('p-city').value === 'إدلب' && document.querySelector('#combo-city input[type=hidden]').value === 'idl' && document.getElementById('p-city').getAttribute('aria-expanded') === 'false'`),
  await E(`document.getElementById('p-city').value + ' / ' + document.querySelector('#combo-city input[type=hidden]').value`));
await E(`document.getElementById('p-city').select()`);
await b.type('اللاذقيه');
ok('combo: ه finds ة and ignores shadda', (await E(vis)) === 'lat', await E(vis));
await E(`document.getElementById('p-city').select()`);
await b.type('حمص');
ok('combo: ignores tashkeel', (await E(vis)) === 'hom', await E(vis));
await E(`document.getElementById('p-city').select()`);
await b.type('beyrouth');
ok('combo: Latin match', (await E(vis)) === 'bei', await E(vis));
await E(`document.getElementById('p-city').select()`);
await b.type('zzz');
ok('combo: empty message shows', await E(`!document.querySelector('#combo-city .ins-combo-empty').hidden && getComputedStyle(document.querySelector('#combo-city .ins-combo-list')).display !== 'none'`));
await b.key('Escape');
ok('combo: Escape closes', await E(`document.getElementById('p-city').getAttribute('aria-expanded') === 'false'`));

// ------------------------------------------------------------------ date
const d0 = await E(`(() => { const i = document.getElementById('p-date'); const h = document.querySelector('input[type=hidden][name=issued]');
  return { type: i.type, text: i.value, hidden: h && h.value, nameMoved: !i.hasAttribute('name'), btn: !!i.closest('.ins-input-group').querySelector('.ins-date-btn') }; })()`);
ok('date: enhanced into text + hidden ISO', d0.type === 'text' && d0.hidden === '2026-09-23' && d0.nameMoved && d0.btn, JSON.stringify(d0));
ok('date: shown in the page language with Latin digits', /2026/.test(d0.text) && /23/.test(d0.text) && /[؀-ۿ]/.test(d0.text), d0.text);
await b.click('#p-date ~ .ins-date-btn');
await b.sleep(250);
const c0 = await E(`(() => { const c = document.querySelector('.ins-cal'); const f = document.activeElement;
  return { open: c && c.matches(':popover-open'), focus: f.dataset.date, selected: f.getAttribute('aria-selected'), firstDay: c.querySelector('th').abbr, dir: c.dir,
           belowField: c.getBoundingClientRect().top >= document.getElementById('p-date').getBoundingClientRect().bottom }; })()`);
ok('date: button opens the calendar', c0.open, JSON.stringify(c0));
ok('date: focus on the chosen day', c0.focus === '2026-09-23' && c0.selected === 'true');
ok('date: week starts on Saturday for Arabic', c0.firstDay === 'السبت', c0.firstDay);
ok('date: calendar is RTL and below the field', c0.dir === 'rtl' && c0.belowField);
await b.key('ArrowLeft');
ok('date: ArrowLeft is the next day in RTL', await E(`document.activeElement.dataset.date === '2026-09-24'`), await E(`document.activeElement.dataset.date`));
await b.key('ArrowDown');
ok('date: ArrowDown crosses into next month', await E(`document.activeElement.dataset.date === '2026-10-01' && document.querySelector('.ins-cal [data-ins-cal=month]').value === '9'`),
  await E(`document.activeElement.dataset.date`));
await b.key('PageUp');
ok('date: PageUp goes back a month', await E(`document.activeElement.dataset.date === '2026-09-01'`), await E(`document.activeElement.dataset.date`));
await b.key('Enter');
await b.sleep(150);
ok('date: Enter chooses and closes', await E(`document.querySelector('input[type=hidden][name=issued]').value === '2026-09-01' && !document.querySelector('.ins-cal').matches(':popover-open') && document.activeElement.id === 'p-date'`),
  await E(`document.querySelector('input[type=hidden][name=issued]').value + ' focus=' + document.activeElement.id`));
await E(`document.getElementById('p-date').select()`);
await b.type('15/10/2026');
await b.key('Tab');
ok('date: typed day/month/year is read', await E(`document.querySelector('input[type=hidden][name=issued]').value === '2026-10-15'`), await E(`document.querySelector('input[type=hidden][name=issued]').value`));
await E(`document.getElementById('p-date').focus(); document.getElementById('p-date').select()`);
await b.type('٣/١١/٢٠٢٦');
await b.key('Enter');
ok('date: Arabic-Indic digits are read', await E(`document.querySelector('input[type=hidden][name=issued]').value === '2026-11-03'`), await E(`document.querySelector('input[type=hidden][name=issued]').value`));
await E(`document.getElementById('p-date').select()`);
await b.type('غدًا');
await b.key('Tab');
ok('date: nonsense is flagged invalid', await E(`!document.getElementById('p-date').validity.valid && document.getElementById('p-date').validationMessage === 'اكتب تاريخًا صحيحًا.'`),
  await E(`document.getElementById('p-date').validationMessage`));

await b.click('#p-to ~ .ins-date-btn');
await b.sleep(250);
const r0 = await E(`(() => { const c = document.querySelector('.ins-cal');
  const before = c.querySelector('[data-date="2026-09-07"]'), start = c.querySelector('[data-date="2026-09-08"]');
  const inRange = [...c.querySelectorAll('td.is-in-range')].length;
  return { beforeBlocked: before.getAttribute('aria-disabled'), startEdge: start.classList.contains('is-range-edge'), inRange }; })()`);
ok('range: days before the start are blocked', r0.beforeBlocked === 'true', JSON.stringify(r0));
ok('range: band drawn from start to end', r0.inRange === 12 && r0.startEdge, JSON.stringify(r0));
await b.key('Escape');
ok('date: Escape closes and refocuses', await E(`!document.querySelector('.ins-cal').matches(':popover-open') && document.activeElement.id === 'p-to'`));
ok('date: a closing calendar lets clicks through while it fades', (await E(`getComputedStyle(document.querySelector('.ins-cal')).pointerEvents`)) === 'none');
await b.click('#p-from ~ .ins-date-btn');
await b.sleep(200);
await b.click('.ins-cal [data-date="2026-09-25"]');
await b.sleep(250);
ok('range: a start after the end clears it and opens the end', await E(`document.querySelector('input[type=hidden][name=to]').value === '' && document.querySelector('.ins-cal').matches(':popover-open') && document.activeElement.closest('.ins-cal') !== null`),
  await E(`document.querySelector('input[type=hidden][name=to]').value`));
await b.mouseTo(5, 5);
await b.mouse(700, 30);
await b.sleep(200);
ok('date: outside press closes', await E(`!document.querySelector('.ins-cal').matches(':popover-open')`));

// ------------------------------------------------------------------ wizard
const wz = `(() => { const w = document.getElementById('demo-wizard'); const ps = [...w.querySelectorAll('.ins-wizard-panel')];
  return { at: ps.findIndex(p => p.classList.contains('is-active')), shown: ps.map(p => getComputedStyle(p).display !== 'none').join(','),
           current: [...w.querySelectorAll('.ins-steps-item')].findIndex(s => s.getAttribute('aria-current') === 'step'),
           done: [...w.querySelectorAll('.ins-steps-item.is-done')].length, focus: document.activeElement.getAttribute('aria-label') || document.activeElement.id }; })()`;
let w = await E(wz);
ok('wizard: starts on step one only', w.at === 0 && w.shown === 'true,false,false' && w.current === 0, JSON.stringify(w));
ok('wizard: prev hidden on first step', await E(`getComputedStyle(document.querySelector('#demo-wizard [data-ins-wizard=prev]')).visibility === 'hidden'`));
await b.click('#demo-wizard [data-ins-wizard="next"]');
await b.sleep(150);
w = await E(wz);
ok('wizard: next is blocked by an empty required field', w.at === 0, JSON.stringify(w));
ok('wizard: messages shown for the step', await E(`[...document.querySelectorAll('#demo-wizard .ins-wizard-panel.is-active .ins-error')].every(e => getComputedStyle(e).display !== 'none' && e.textContent.length > 2)`));
await E(`document.getElementById('w-name').focus()`); await b.type('رنا العلي');
await E(`document.getElementById('w-mail').focus()`); await b.type('rana@example.com');
await b.click('#demo-wizard [data-ins-wizard="next"]');
await b.sleep(150);
w = await E(wz);
ok('wizard: valid step moves on, marks done, focuses panel', w.at === 1 && w.done === 1 && w.current === 1 && w.focus === 'الخطّة', JSON.stringify(w));
await b.click('#demo-wizard [data-ins-wizard="next"]');
w = await E(wz);
ok('wizard: required radio blocks', w.at === 1, JSON.stringify(w));
await b.click('#demo-wizard .ins-tile input[value="pro"] + .ins-tile-title');
await b.click('#demo-wizard [data-ins-wizard="next"]');
await b.sleep(150);
w = await E(wz);
ok('wizard: reaches last step', w.at === 2 && w.done === 2, JSON.stringify(w));
ok('wizard: finish shown, next hidden on last step', await E(`getComputedStyle(document.querySelector('#demo-wizard .ins-wizard-finish')).display !== 'none' && getComputedStyle(document.querySelector('#demo-wizard [data-ins-wizard=next]')).display === 'none'`));
// an earlier step made invalid behind the person's back: the submit goes to it
await E(`document.getElementById('w-name').value = ''; document.getElementById('w-terms').checked = true;`);
await b.click('#demo-wizard .ins-wizard-finish');
await b.sleep(200);
w = await E(wz);
ok('wizard: invalid field in a hidden step brings its step back', w.at === 0, JSON.stringify(w));
await E(`document.getElementById('w-name').focus()`); await b.type('رنا');
await E(`Insiyab.wizard('#demo-wizard', 2)`);
await b.click('#demo-wizard .ins-wizard-finish');
await b.sleep(300);
ok('wizard: final submit reaches the page', await E(`[...document.querySelectorAll('.ins-toast')].some(t => t.textContent.includes('تمّ إنشاء الحساب'))`));

// ------------------------------------------------------------------ confirm
await E(`document.querySelectorAll('.ins-toast').forEach(t => t.remove())`);
await b.click('#confirm-attr');
await b.sleep(300);
const cf = await E(`(() => { const d = [...document.querySelectorAll('dialog.ins-dialog--sm')].find(x => x.open && !x.id); return d ? { danger: !!d.querySelector('.ins-btn--danger'), text: d.textContent.includes('حذف العملية'), focus: document.activeElement.value } : null; })()`);
ok('confirm: attribute asks first', cf && cf.text && cf.danger, JSON.stringify(cf));
ok('confirm: focus starts on cancel', cf && cf.focus === 'cancel');
ok('confirm: action held back until answered', await E(`document.querySelectorAll('.ins-toast').length === 0`));
await b.key('Escape');
await b.sleep(300);
ok('confirm: Escape declines, action never runs', await E(`document.querySelectorAll('.ins-toast').length === 0 && ![...document.querySelectorAll('dialog')].some(d => d.open)`));
await b.click('#confirm-attr');
await b.sleep(300);
await E(`[...document.querySelectorAll('dialog')].find(d => d.open).querySelector('button[value=ok]').click()`);
await b.sleep(300);
ok('confirm: yes replays the original click', await E(`[...document.querySelectorAll('.ins-toast')].some(t => t.textContent.includes('حُذفت العملية'))`));
await E(`window.__ans = 'pending'; Insiyab.confirm('؟').then(v => { window.__ans = v; }); 0`);
await b.sleep(250);
await E(`[...document.querySelectorAll('dialog')].find(d => d.open).querySelector('button[value=ok]').click()`);
await b.sleep(200);
ok('confirm: API resolves true', await E(`window.__ans === true`), String(await E(`window.__ans`)));

ok('no console errors or exceptions', b.logs.length === 0, b.logs.join(' | '));
await b.close();
end();
