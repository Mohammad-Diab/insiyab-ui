// The React wrapper, @insiyab/react, in three parts:
//
//   parity     every example in test/fixtures/parity/ (copied from the docs) built
//              with the wrapper's components, and compared, once the core has built
//              both, with the example itself: element by element, attribute by
//              attribute, ids mapped by position.
//   hydration  the behaviour app rendered by react-dom/server and hydrated after the
//              core's own scan has been over the server's HTML, in Strict Mode, with
//              React's development build: any mismatch or warning is a console line.
//   behaviour  real clicks and keys on the hydrated app, read back from React state.
//
// Needs packages/react/node_modules (npm install there); without it the file skips.
import { spawnSync } from 'node:child_process';
import { existsSync } from 'node:fs';
import { dirname, join } from 'node:path';
import { fileURLToPath } from 'node:url';
import { launch, BASE } from './lib/cdp.mjs';
import { suite } from './lib/check.mjs';
import { NORMALIZE } from './lib/parity.mjs';

const ROOT = join(dirname(fileURLToPath(import.meta.url)), '..');
const PKG = join(ROOT, 'packages', 'react');
if (!existsSync(join(PKG, 'node_modules', 'react'))) {
  console.log('SKIP  packages/react has no node_modules; run npm install there');
  process.exit(0);
}
for (const script of ['build.mjs', 'test/build.mjs']) {
  const run = spawnSync(process.execPath, [join(PKG, script)], { cwd: PKG, encoding: 'utf8' });
  if (run.status !== 0) {
    console.log(`FAIL  ${script} builds  ${(run.stdout + run.stderr).trim().split('\n').slice(0, 12).join(' | ')}`);
    process.exit(1);
  }
}

const { ok, end } = suite();

/* The tests themselves are typed against the package, so an API change that would
   break a user's code breaks here first. */
const tsc = spawnSync(process.execPath, [join(PKG, 'node_modules', 'typescript', 'bin', 'tsc'), '-p', join(PKG, 'tsconfig.test.json')], { cwd: PKG, encoding: 'utf8' });
ok('types: the package and its tests type-check', tsc.status === 0, (tsc.stdout + tsc.stderr).trim().split('\n').slice(0, 3).join(' | '));

const b = await launch();
await b.size(1280, 900);
const E = (js) => b.evaluate(js);
const OUT = '/packages/react/test/out';

// ------------------------------------------------------------------ parity
await b.goto(`${BASE}${OUT}/parity.html`);
await E(`${NORMALIZE}; 0`);
const names = await E(`__react.cases`);
const settle = () => E(`new Promise((r) => requestAnimationFrame(() => requestAnimationFrame(() => setTimeout(() => r(0), 120))))`);
let same = 0;
for (const name of names) {
  const expected = await E(`fetch('/test/fixtures/parity/${name}.html').then((r) => r.ok ? r.text() : null)`);
  if (expected == null) { ok(`parity: ${name} has a fixture`, false); continue; }
  await E(`(() => { const s = document.getElementById('stage'); s.innerHTML = '<div id="got"></div>'; __react.render(${JSON.stringify(name)}, document.getElementById('got')); return 0; })()`);
  await settle();
  const got = await E(`__normalize(document.getElementById('got'))`);
  await E(`__react.unmount(); document.getElementById('stage').innerHTML = ''; 0`);
  await E(`(() => { const s = document.getElementById('stage'); s.innerHTML = '<div id="want"></div>'; const w = document.getElementById('want'); w.innerHTML = ${JSON.stringify(expected)}; Insiyab.init(w); return 0; })()`);
  await settle();
  const want = await E(`__normalize(document.getElementById('want'))`);
  await E(`document.getElementById('stage').innerHTML = ''; 0`);
  let at = 0;
  while (at < got.length && at < want.length && got[at] === want[at]) at++;
  const equal = at === got.length && at === want.length;
  if (equal) same++;
  ok(`parity: ${name}`, equal, equal ? '' : `line ${at + 1}: react ${(got[at] ?? '(ends)').trim()}  |  docs ${(want[at] ?? '(ends)').trim()}`);
}
ok(`parity: every case compared (${same}/${names.length} the same)`, names.length >= 80, names.length);
ok('parity: no console errors, warnings or exceptions', b.logs.length === 0, b.logs.join(' | '));
b.logs.length = 0;

// ------------------------------------------------------------------ hydration
await b.goto(`${BASE}${OUT}/ssr.html`);
for (let i = 0; i < 50 && !(await E(`!!window.__hydrated`)); i++) await b.sleep(100);
await b.sleep(400);
ok('hydration: the server-rendered app hydrates', await E(`!!window.__hydrated`));
ok('hydration: no mismatch, warning or error in Strict Mode', b.logs.length === 0, b.logs.join(' | '));
const built = await E(`(() => {
  const date = document.getElementById('date');
  return {
    date: date.type === 'text' && !date.name && document.querySelector('input[type=hidden][name=delivery]')?.value,
    dateBtn: !!date.closest('.ins-input-group').querySelector('.ins-date-btn'),
    phone: !!document.querySelector('#phone').closest('.ins-input-group').querySelector('.ins-phone-cc'),
    phoneHidden: document.querySelectorAll('input[type=hidden][name=mobile]').length,
    otp: document.querySelectorAll('#otp ~ .ins-otp-cells .ins-otp-cell').length,
    color: !!document.querySelector('#color').closest('.ins-input-group').querySelector('.ins-color-swatch'),
    file: !!document.querySelector('#files').parentNode.querySelector('.ins-file-zone'),
    tree: document.getElementById('tree').getAttribute('role'),
    car: document.querySelectorAll('#car .ins-carousel-dots button, #car .ins-carousel-dots > *').length,
    time: document.getElementById('rel').textContent,
    palette: !!document.querySelector('#pal .ins-palette-item'),
    toggle: document.getElementById('theme').getAttribute('aria-pressed')
  };
})()`);
ok('hydration: the date field is built, its name on the hidden ISO input', built.date === '2026-09-30' && built.dateBtn, JSON.stringify(built));
ok('hydration: phone, OTP, colour and file fields are built', built.phone && built.phoneHidden === 1 && built.otp === 4 && built.color && built.file, JSON.stringify(built));
ok('hydration: the tree, the carousel and the palette are built', built.tree === 'tree' && built.car === 3 && built.palette, JSON.stringify(built));
ok('hydration: the timeline plugin writes the relative time', built.time && built.time !== '٢ يناير', built.time);
ok('hydration: the theme toggle says which theme is showing', built.toggle === 'false' || built.toggle === 'true', built.toggle);

// ------------------------------------------------------------------ behaviour
const out = (id) => E(`document.getElementById('${id}').textContent`);
const click = async (sel) => { await b.click(sel); await b.sleep(120); };

await click('[role=tab]:nth-child(2)');
ok('tabs: a click switches the panel and React follows', (await out('o-tab')) === 'b' && (await E(`document.querySelector('[role=tabpanel].is-active').textContent`)) === 'لوحة ب');
await E(`document.querySelector('[role=tab][aria-selected=true]').focus(); 0`);
await b.key('ArrowLeft');
await b.sleep(120);
ok('tabs: the arrow keys too, in the page’s direction', (await out('o-tab')) === 'c', await out('o-tab'));

await click('#seg [data-ins-value=week]');
ok('seg: a click reaches onValueChange', (await out('o-seg')) === 'week');
await click('#seg-fixed [data-ins-value=y]');
ok('seg: a controlled value the page kept is put back', await E(`document.querySelector('#seg-fixed [data-ins-value=x]').classList.contains('is-active') && !document.querySelector('#seg-fixed [data-ins-value=y]').classList.contains('is-active')`));

await click('#toggle');
ok('toggle button: pressed, and React knows', (await out('o-toggle')) === 'true' && (await E(`document.getElementById('toggle').getAttribute('aria-pressed')`)) === 'true');

await click('#date-set');
ok('date: a value set from React is shown and sent', await E(`document.querySelector('input[type=hidden][name=delivery]').value === '2026-10-05' && document.getElementById('date').value !== ''`));
ok('date: and is not reported back as a change', (await out('o-date')) === '2026-10-05');
await click('#date ~ .ins-date-btn');
await click('.ins-cal [data-ins-day], .ins-cal td button:not([disabled])');
ok('date: a day picked in the calendar reaches onValueChange', /^2026-\d\d-\d\d$/.test(await out('o-date')) && (await out('o-date')) === (await E(`document.querySelector('input[type=hidden][name=delivery]').value`)), await out('o-date'));

await E(`document.getElementById('city').focus(); 0`);
await b.type('اسكندريه');
await b.sleep(150);
await b.key('ArrowDown');
await b.key('Enter');
await b.sleep(150);
ok('combo: Arabic-aware matching, then the choice reaches onValueChange', (await out('o-city')) === 'alx', await out('o-city'));

await click('#dialog-open');
ok('dialog: opened from React state', await E(`document.getElementById('dlg').open`));
await b.key('Escape');
await b.sleep(200);
ok('dialog: Escape closes it and onOpenChange hears both', !(await E(`document.getElementById('dlg').open`)) && (await out('o-dialog')) === 'oc', await out('o-dialog'));
await click('#dialog-open');
await click('#dlg-ok');
ok('dialog: so does a dismiss button, and it opens again after', (await out('o-dialog')) === 'ococ', await out('o-dialog'));

await click('#menu > summary');
await click('#menu-archived');
ok('menu: a checkbox item flips and stays in an open menu', (await out('o-menu')) === 'true/false', await out('o-menu'));
await b.key('Escape');
await b.sleep(150);
ok('menu: Escape closes it, and onOpenChange hears', (await out('o-menu')) === 'false/false', await out('o-menu'));

await click('#wiz [data-ins-wizard=next]');
ok('wizard: an empty required field keeps it on its step', (await out('o-step')) === '0' && (await E(`document.getElementById('wiz-name').matches(':invalid')`)));
await E(`document.getElementById('wiz-name').focus(); 0`);
await b.type('متجر');
await click('#wiz [data-ins-wizard=next]');
ok('wizard: filled in, Next moves and React follows', (await out('o-step')) === '1' && (await E(`document.querySelectorAll('#wiz .ins-wizard-panel')[1].classList.contains('is-active')`)));

await E(`(() => { const i = document.getElementById('phone'); i.focus(); i.select(); return 0; })()`);
await b.type('0551234567');
await b.sleep(150);
ok('phone: typing reaches onValueChange as E.164', (await out('o-phone')) === '+966551234567', await out('o-phone'));
await E(`document.getElementById('otp').focus(); 0`);
await b.type('1234');
await b.sleep(150);
ok('otp: a full code reaches onComplete', (await out('o-otp')) === '1234', await out('o-otp'));

await click('#color-set');
ok('colour: a value set from React is shown', (await E(`document.getElementById('color').value`)) === '#1d4ed8');

await E(`document.querySelector('#tree [role=treeitem]').focus(); 0`);
await b.key('ArrowLeft');
await b.key('Enter');
await b.sleep(120);
ok('tree: the arrow key opens a branch and Enter selects, reported to React', (await out('o-tree')).includes('o') && (await out('o-tree')).includes('s'), await out('o-tree'));

await click('#car .ins-carousel-btn--next');
await b.sleep(700);
ok('carousel: next moves it and onIndexChange hears', (await out('o-slide')) === '1', await out('o-slide'));

await click('#pages .ins-page:not(.ins-page--prev):not(.ins-page--next):nth-of-type(3)');
ok('pagination: a page button reaches onPageChange', (await out('o-page')) !== '1', await out('o-page'));

await E(`Insiyab.palette('#pal', 'open'); 0`);
await b.sleep(150);
await b.type('تجريبي');
await b.key('Enter');
await b.sleep(250);
ok('palette: an item given as data runs its own function', (await out('o-tab')) === 'c' && !(await E(`document.getElementById('pal').open`)));

await click('#nav-orders');
await b.sleep(700);
ok('sidebar: a moved `current` moves the selection', await E(`document.getElementById('nav-orders').classList.contains('is-active') && !document.getElementById('nav-home').classList.contains('is-active')`));

await click('#toggle-show');
await b.sleep(150);
ok('unmount: the phone fields go, and their hidden inputs with them', (await E(`document.querySelectorAll('input[type=hidden][name=mobile], input[type=hidden][name=second]').length`)) === 0);
await click('#toggle-show');
await b.sleep(300);
ok('remount: they come back built', (await E(`document.querySelectorAll('input[type=hidden][name=mobile]').length === 1 && !!document.querySelector('#phone2').closest('.ins-input-group').querySelector('.ins-phone-cc')`)));

ok('behaviour: no console errors, warnings or exceptions', b.logs.length === 0, b.logs.join(' | '));
b.logs.length = 0;

// ------------------------------------------------------------------ from nothing
await b.goto(`${BASE}${OUT}/spa.html`);
for (let i = 0; i < 50 && !(await E(`!!window.__mounted`)); i++) await b.sleep(100);
await b.sleep(300);
const spa = await E(`({ date: document.getElementById('date').type, tree: document.getElementById('tree').getAttribute('role'), otp: document.querySelectorAll('.ins-otp-cell').length })`);
ok('client render: the core imported by a bundle, the app mounted after its scan, every builder ran', spa.date === 'text' && spa.tree === 'tree' && spa.otp === 4, JSON.stringify(spa));
ok('client render: no console errors, warnings or exceptions', b.logs.length === 0, b.logs.join(' | '));

await b.close();
end();
