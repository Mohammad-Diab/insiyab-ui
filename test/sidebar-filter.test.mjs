// The sidebar filter: text and keywords, Arabic forms, empty groups, the no-results line, its keys, and the marker after clearing.
import { launch, BASE } from './lib/cdp.mjs';
import { suite } from './lib/check.mjs';
const { ok, end } = suite();
const b = await launch();
const E = (js) => b.evaluate(js);

const state = () => E(`(() => {
  const side = document.querySelector('.ins-shell-side'), input = side.querySelector('.ins-shell-filter input');
  const vis = (el) => !!el && el.getClientRects().length > 0;
  const links = [...side.querySelectorAll('.ins-shell-link')].filter((l) => !l.closest('.ins-shell-bottom'));
  return { shown: links.filter(vis).map((l) => l.getAttribute('href')), groups: [...side.querySelectorAll('.ins-shell-group')].filter(vis).map((g) => g.textContent.trim()),
    empty: vis(side.querySelector('.ins-shell-filter-empty')), value: input.value, focus: document.activeElement === input,
    active: document.activeElement && document.activeElement.getAttribute('href') }; })()`);
const focusField = () => E(`document.querySelector('.ins-shell-filter input').focus(); 0`);
const clear = () => E(`(() => { const i = document.querySelector('.ins-shell-filter input'); i.value = ''; i.dispatchEvent(new Event('input', { bubbles: true })); return 0; })()`);

await b.size(1440, 1000);
await b.goto(`${BASE}/demo/shell.html?theme=light&mode=advanced`);

// 1. Built from the attribute, above the first group, with the docs' placeholder.
const built = await E(`(() => { const side = document.querySelector('.ins-shell-side'), box = side.querySelector('.ins-shell-filter');
  const input = box && box.querySelector('input'), group = side.querySelector('.ins-shell-group');
  return { box: !!box, search: !!(box && box.classList.contains('ins-search') && box.querySelector('.ins-search-ico')), before: !!(box && group && (box.compareDocumentPosition(group) & 4)),
    hint: input && input.placeholder, label: input && input.getAttribute('aria-label'), keys: input && input.getAttribute('aria-keyshortcuts'), one: side.querySelectorAll('.ins-shell-filter').length }; })()`);
ok('built: a search field over the links', built.box && built.search && built.before && built.one === 1, JSON.stringify(built));
ok('built: the placeholder comes from data-ins-filter', built.hint === 'ابحث في الصفحات' && built.label === 'ابحث في الصفحات', built.hint);
ok('built: it announces "/" as its shortcut', built.keys === '/', built.keys);
const all = await state();
ok('built: nothing hidden and no empty line at first', all.shown.length > 40 && !all.empty, `${all.shown.length} links`);

// 2. "/" focuses the field when focus is not in a text field.
await E(`document.activeElement && document.activeElement.blur(); 0`);
await b.key('/');
let s = await state();
ok('/: focuses the field', s.focus && s.value === '', JSON.stringify({ focus: s.focus, value: s.value }));

// 3. By the link text, with the hamza and the harakat typed differently.
await b.type('الازرار');
s = await state();
ok('text: the buttons page is found without its hamza', s.shown.includes('buttons.html') && !s.shown.includes('tables.html') && s.shown.length < 6, s.shown.join(' '));
await clear(); await focusField();
await b.type('الأَزْرار');
s = await state();
ok('text: harakat are ignored', s.shown.includes('buttons.html') && s.shown.length < 6, s.shown.join(' '));
await clear(); await focusField();
await b.type('والحاله الفارغه');
s = await state();
ok('text: ta marbuta and ha meet', s.shown.includes('loading.html') && s.shown.length < 4, s.shown.join(' '));
await clear(); await focusField();

// 4. By the keywords, in any case, and the empty groups go.
await b.type('PICKERS');
s = await state();
ok('keywords: an English name finds its page', s.shown.length === 1 && s.shown[0] === 'pickers.html', s.shown.join(' '));
ok('groups: only the heading with a link left shows', s.groups.length === 1 && s.groups[0] === 'النماذج', s.groups.join(' | '));
ok('groups: no empty line while something matches', !s.empty);

// 5. Down moves into the first link shown.
await b.key('ArrowDown');
s = await state();
ok('Down: focus moves to the first link shown', s.active === 'pickers.html', s.active);
await focusField();

// 6. Nothing found: the empty line.
await clear(); await focusField();
await b.type('zzqqxx');
s = await state();
ok('none: no link and no heading', s.shown.length === 0 && s.groups.length === 0, s.shown.join(' '));
ok('none: the empty line says so', s.empty && (await E(`document.querySelector('.ins-shell-filter-empty').textContent`)) === 'لا نتائج');

// 7. Escape clears, a second leaves; clearing restores everything.
await b.key('Escape');
s = await state();
ok('Esc: the first clears the field and keeps focus', s.value === '' && s.focus);
ok('Esc: clearing restores every link and heading', s.shown.length === all.shown.length && s.groups.length === all.groups.length && !s.empty, `${s.shown.length}/${all.shown.length}`);
await b.key('Escape');
s = await state();
ok('Esc: the second leaves the field', !s.focus);

// 8. The marker after the active link was hidden and shown again, and a flight cut short by filtering.
await focusField();
await b.type('pickers');
await b.key('Escape');
await b.sleep(400);
const marker = await E(`(() => { const l = document.querySelector('.ins-shell-link.is-active'), cs = getComputedStyle(l, '::before');
  return { href: l.getAttribute('href'), vis: l.getClientRects().length > 0, transform: cs.transform, visibility: cs.visibility,
    bars: document.querySelectorAll('.ins-shell-indicator').length, moving: document.querySelector('.ins-shell-side').classList.contains('ins-shell-moving') }; })()`);
ok('marker: back on the active link after clearing', marker.href === 'shell.html' && marker.vis && (marker.transform === 'none' || marker.transform === 'matrix(1, 0, 0, 1, 0, 0)') && marker.visibility === 'visible', JSON.stringify(marker));
ok('marker: no stand-in bar left over', marker.bars === 0 && !marker.moving, JSON.stringify(marker));
await E(`Insiyab.sidebarSelect('.ins-shell-link[href="grid.html"]'); 0`);
await b.sleep(80);
const flying = await E(`document.querySelectorAll('.ins-shell-indicator').length`);
await focusField();
await b.type('grid');
const cut = await E(`({ bars: document.querySelectorAll('.ins-shell-indicator').length, moving: document.querySelector('.ins-shell-side').classList.contains('ins-shell-moving') })`);
ok('marker: filtering lands a flight at once', flying === 1 && cut.bars === 0 && !cut.moving, JSON.stringify({ flying, ...cut }));
await b.key('Escape');
await E(`Insiyab.sidebarSelect('.ins-shell-link[href="shell.html"]'); 0`);
await b.sleep(800);

// 9. "/" is left alone while typing in another field, and when a palette claims it.
await E(`(() => { const i = document.createElement('input'); i.id = 'other'; document.querySelector('.ins-shell-content').prepend(i); i.focus(); return 0; })()`);
await b.key('/');
s = await state();
ok('/: not taken from another text field', !s.focus && (await E(`document.activeElement.id`)) === 'other');
await E(`document.getElementById('other').remove(); document.querySelector('#site-search').removeAttribute('data-ins-palette-keys'); document.activeElement && document.activeElement.blur(); 0`);
await b.key('/');
await b.sleep(300);
const pal = await E(`({ open: document.querySelector('#site-search').open, focus: document.activeElement === document.querySelector('.ins-shell-filter input') })`);
ok('/: a palette that claims it keeps it', pal.open && !pal.focus, JSON.stringify(pal));
await E(`Insiyab.dialog('#site-search', 'close'); document.querySelector('#site-search').setAttribute('data-ins-palette-keys', 'mod+k'); 0`);
await b.sleep(400);

// 10. Enter opens the first link shown.
await focusField();
await b.type('grid');
const first = (await state()).shown[0];
await b.key('Enter');
await b.sleep(1500);
ok('Enter: opens the first link shown', !!first && (await E(`location.pathname`)).endsWith('/' + first), `${first} → ${await E(`location.pathname`)}`);

// 11. The drawer: "/" opens it and focuses the field; Escape on an empty field leaves it and closes the drawer.
await b.size(800, 1000);
await b.goto(`${BASE}/demo/shell.html?theme=light&mode=advanced`);
await b.key('/');
await b.sleep(400);
let d = await E(`({ open: document.querySelector('.ins-shell-side').classList.contains('is-open'), focus: document.activeElement === document.querySelector('.ins-shell-filter input'),
  shown: document.querySelector('.ins-shell-filter').getClientRects().length > 0 })`);
ok('drawer: "/" opens it with the field focused', d.open && d.focus && d.shown, JSON.stringify(d));
await b.key('Escape');
await b.sleep(400);
d = await E(`({ open: document.querySelector('.ins-shell-side').classList.contains('is-open'), focus: document.activeElement === document.querySelector('.ins-shell-filter input') })`);
ok('drawer: Escape on the empty field leaves it and closes the drawer', !d.open && !d.focus, JSON.stringify(d));

ok('no console errors, warnings or exceptions', b.logs.length === 0, b.logs.join(' | '));
await b.close();
end();
