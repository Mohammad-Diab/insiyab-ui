// The command palette plugin: what it builds from the page's links and buttons,
// the navigation it collects, the ranking (Arabic-aware), the keyboard, recents that
// survive a reload, items that are actions, the shortcuts on an Arabic layout, the
// cancelable event, the JS API, and a plugin that loads after the page.
import { launch, BASE } from './lib/cdp.mjs';
import { suite } from './lib/check.mjs';

const { ok, end } = suite();
const b = await launch();
const E = (js) => b.evaluate(js);
const CTRL = 2, SHIFT = 8;

/* The palette as a person would read it: open or not, the rows in order (their
   names, without the shortcut and the group tag), the headings, what is active. */
const view = (id = 'pal') => E(`(() => {
  const d = document.getElementById(${JSON.stringify(id)});
  const name = (el) => { const c = el.cloneNode(true); c.querySelectorAll('.ins-kbd, .ins-palette-where').forEach((x) => x.remove()); return c.textContent.trim(); };
  const input = d.querySelector('.ins-palette-input');
  const rows = [...d.querySelectorAll('.ins-palette-item:not([hidden])')];
  const active = d.querySelector('.ins-palette-item.is-active');
  return {
    open: d.open, rows: rows.map(name),
    heads: [...d.querySelectorAll('.ins-palette-head')].map((h) => h.textContent),
    active: active ? name(active) : null,
    descendant: input.getAttribute('aria-activedescendant') === (active && active.id),
    focused: document.activeElement === input,
    empty: !d.querySelector('.ins-palette-empty').hidden,
    status: d.querySelector('.ins-palette-status').textContent,
    searching: d.classList.contains('is-searching')
  };
})()`);
const query = async (text, id = 'pal') => {
  await E(`(() => { const i = document.querySelector('#${id} .ins-palette-input'); i.value = ''; i.dispatchEvent(new Event('input', { bubbles: true })); return 0; })()`);
  if (text) await b.type(text);
  await b.sleep(40);
};
const raw = async (key, code, modifiers = 0) => {
  const base = { key, code, windowsVirtualKeyCode: code.startsWith('Key') ? code.charCodeAt(3) : 191, modifiers };
  await b.send('Input.dispatchKeyEvent', { type: 'keyDown', ...base });
  await b.send('Input.dispatchKeyEvent', { type: 'keyUp', ...base });
  await b.sleep(80);
};
const close = () => E(`document.querySelectorAll('dialog[open]').forEach((d) => d.close()); 0`);

await b.size(1280, 900);
await b.goto(`${BASE}/test/fixtures/palette.html`);
await E(`localStorage.clear(); 0`);
await b.goto(`${BASE}/test/fixtures/palette.html`);

// ------------------------------------------------------------------ what it builds
const built = await E(`(() => {
  const d = document.getElementById('pal'), i = d.querySelector('.ins-palette-input'), l = d.querySelector('.ins-palette-list');
  const items = [...d.querySelectorAll('.ins-palette-item')];
  return { ready: d.hasAttribute('data-ins-palette-ready'), cls: d.className, role: i.getAttribute('role'), controls: i.getAttribute('aria-controls') === l.id,
           list: l.getAttribute('role'), count: items.length, options: items.every((x) => x.getAttribute('role') === 'option' && x.tabIndex === -1),
           buttonType: document.getElementById('act-toast').type, label: d.getAttribute('aria-label'),
           keys: document.getElementById('open').getAttribute('aria-keyshortcuts'), keys2: document.getElementById('open2').getAttribute('aria-keyshortcuts'),
           placeholder: i.placeholder, foot: d.querySelectorAll('.ins-palette-foot .ins-kbd').length };
})()`);
ok('the dialog is built and styled as a palette', built.ready && /ins-dialog/.test(built.cls) && /ins-palette/.test(built.cls), built.cls);
ok('a combobox field controlling a listbox', built.role === 'combobox' && built.controls && built.list === 'listbox');
ok('every written link and button became an option', built.count === 8 && built.options, built.count);
ok('a button item cannot submit a form', built.buttonType === 'button');
ok("the page's own label is kept", built.label === 'لوحة الاختبار');
ok('the opener announces the shortcuts', built.keys === 'Control+K /', built.keys);
ok('an opener of a palette without shortcuts announces none', built.keys2 === null, built.keys2);
ok('Arabic placeholder and key hints', built.placeholder === 'ابحث عن صفحة أو أمر…' && built.foot === 4, `${built.placeholder} ${built.foot}`);

// ------------------------------------------------------------------ opening
await b.key('k', CTRL);
await b.sleep(150);
let v = await view();
ok('Ctrl+K opens the first palette with shortcuts', v.open && !(await E(`document.getElementById('pal2').open`)));
ok('the field has focus', v.focused);
ok('the first row is ready for Enter', v.active === 'فاتورة شهرية' && v.descendant, v.active);
ok('groups in the order written, collected ones after', JSON.stringify(v.heads) === JSON.stringify(['الفواتير', 'إجراءات', 'الصفحات', 'الإدارة']), JSON.stringify(v.heads));
ok('the sidebar links were collected', ['لوحة التحكم', 'الجداول', 'المستخدمون'].every((n) => v.rows.includes(n)), JSON.stringify(v.rows));
const auto = await E(`(() => {
  const d = document.getElementById('pal'), items = [...d.querySelectorAll('.ins-palette-item')];
  const dash = items.find((x) => x.getAttribute('href') === '#dash'), tables = items.find((x) => x.getAttribute('href') === '#tables');
  return { settings: items.filter((x) => x.getAttribute('href') === '#settings').length, js: items.some((x) => /javascript/.test(x.getAttribute('href') || '')),
           icon: !!dash.querySelector('svg use'), keywords: dash.getAttribute('data-keywords'), group: dash.getAttribute('data-ins-palette-group'),
           current: tables.getAttribute('aria-current'), sideClass: dash.classList.contains('ins-shell-link') };
})()`);
ok('a link already listed by hand is not listed twice', auto.settings === 1, auto.settings);
ok('a javascript: link is left out', !auto.js);
ok('a collected link keeps its icon and keywords', auto.icon && auto.keywords === 'dashboard');
ok('and sits under its sidebar heading', auto.group === 'الصفحات', auto.group);
ok('the current page is marked, and no sidebar class came with it', auto.current === 'page' && !auto.sideClass);
await b.key('k', CTRL);
await b.sleep(150);
ok('Ctrl+K again closes it', !(await view()).open);
await b.key('k', CTRL);
await b.sleep(150);
ok('reopening does not collect the links twice', (await E(`document.querySelectorAll('#pal [href="#dash"]').length`)) === 1);

// ------------------------------------------------------------------ ranking
await query('فاتورة');
v = await view();
ok('name start, then word start (after the article too), then keyword',
  JSON.stringify(v.rows) === JSON.stringify(['فاتورة شهرية', 'الفاتورة المرتجعة', 'إضافة فاتورة', 'التقارير']), JSON.stringify(v.rows));
ok('while searching, one list with each group named on its row', v.searching && v.heads.length === 0 &&
  (await E(`getComputedStyle(document.querySelector('#pal .ins-palette-item:not([hidden]) .ins-palette-where')).display`)) !== 'none');
ok('the count is announced', v.status === '4 نتائج', v.status);
await query('اضافه');
ok('no hamza and ه for ة still find إضافة فاتورة', (await view()).rows[0] === 'إضافة فاتورة', JSON.stringify((await view()).rows));
await query('dashboard');
ok('a keyword finds a collected link', JSON.stringify((await view()).rows) === JSON.stringify(['لوحة التحكم']), JSON.stringify((await view()).rows));
await query('فاتورة شهر');
ok('every word has to match', JSON.stringify((await view()).rows) === JSON.stringify(['فاتورة شهرية']), JSON.stringify((await view()).rows));
await query('إجراءات');
ok('a group name finds its items', JSON.stringify((await view()).rows) === JSON.stringify(['إشعار تجريبي', 'فتح حوار']), JSON.stringify((await view()).rows));
await query('zzz');
v = await view();
ok('nothing found: the empty line, no active row', v.empty && v.rows.length === 0 && v.active === null && v.status === 'لا نتائج', JSON.stringify(v));

// ------------------------------------------------------------------ the keyboard
await query('');
v = await view();
ok('an emptied field shows the groups again', !v.searching && v.heads.length === 4 && !v.empty);
await b.key('ArrowDown');
ok('ArrowDown moves to the next row', (await view()).active === 'الفاتورة المرتجعة');
await b.key('ArrowUp');
await b.key('ArrowUp');
v = await view();
ok('ArrowUp from the first wraps to the last', v.active === v.rows[v.rows.length - 1] && v.descendant, v.active);
await E(`window.__evt = null; document.addEventListener('ins:palette', (e) => { window.__evt = e.detail.label + '|' + e.detail.key.split('/').pop(); }, { once: true }); 0`);
await query('مساعدة');
await b.key('Enter');
await b.sleep(150);
ok('Enter follows the link', (await E(`location.hash`)) === '#help');
ok('and closes the palette', !(await view()).open);
ok('ins:palette names what was chosen', (await E(`window.__evt`)) === 'مساعدة|palette.html#help', await E(`window.__evt`));

await b.click('#open');
await b.sleep(150);
ok('the opener button opens it too', (await view()).open);
await b.key('Escape');
await b.sleep(150);
ok('Escape closes it and focus goes back to the opener', !(await view()).open && (await E(`document.activeElement.id`)) === 'open');

// ------------------------------------------------------------------ recents
await b.key('k', CTRL);
await b.sleep(150);
v = await view();
ok('with nothing typed, the recent item comes first', v.heads[0] === 'الأخيرة' && v.rows[0] === 'مساعدة', JSON.stringify({ h: v.heads, r: v.rows.slice(0, 2) }));
ok('and is not listed twice', v.rows.filter((r) => r === 'مساعدة').length === 1);
await query('people');
await b.key('Enter');
await b.sleep(150);
await b.goto(`${BASE}/test/fixtures/palette.html`);
await b.key('k', CTRL);
await b.sleep(150);
v = await view();
ok('recents survive a reload, newest first, collected links included', JSON.stringify(v.rows.slice(0, 2)) === JSON.stringify(['المستخدمون', 'مساعدة']), JSON.stringify(v.rows.slice(0, 3)));

// ------------------------------------------------------------------ items that act
await query('إشعار');
await b.key('Enter');
await b.sleep(250);
ok("a button item does what it did before: the library's toast", (await E(`[...document.querySelectorAll('.ins-toast')].some((t) => t.textContent.includes('تمّ من اللوحة'))`)) && !(await view()).open);
await b.key('k', CTRL);
await b.sleep(150);
await query('حوار');
await b.key('Enter');
await b.sleep(250);
ok('an item opening a dialog: that dialog open, the palette closed', (await E(`document.getElementById('other').open`)) && !(await view()).open);
await close();
await b.sleep(100);

await E(`document.addEventListener('ins:palette', (e) => e.preventDefault(), { once: true }); location.hash = '#start'; 0`);
await b.key('k', CTRL);
await b.sleep(150);
await query('فاتورة شهرية');
await b.key('Enter');
await b.sleep(150);
ok('a cancelled ins:palette stops the item', (await E(`location.hash`)) === '#start' && !(await view()).open, await E(`location.hash`));

// ------------------------------------------------------------------ the pointer
await b.key('k', CTRL);
await b.sleep(150);
await b.hover('#pal [href="#returned"]');
await b.sleep(60);
ok('the pointer moves the one highlight', (await view()).active === 'الفاتورة المرتجعة');
await b.click('#pal [href="#returned"]');
await b.sleep(150);
ok('a click chooses', (await E(`location.hash`)) === '#returned' && !(await view()).open);

// ------------------------------------------------------------------ shortcuts
await E(`document.getElementById('field').focus(); 0`);
await b.key('/');
ok('"/" while typing in a field does nothing', !(await view()).open);
await E(`document.activeElement.blur(); 0`);
await b.key('/');
await b.sleep(150);
ok('"/" elsewhere opens it', (await view()).open);
await b.key('Escape');
await b.sleep(150);
await b.key('/');
await b.sleep(150);
ok('and again after Escape, with nothing to hand focus back to', (await view()).open);
await close();
await raw('ن', 'KeyK', CTRL);
await b.sleep(120);
ok('Ctrl+K on an Arabic layout (the key reads ن)', (await view()).open);
await close();
await raw('ظ', 'Slash');
await b.sleep(120);
ok('"/" on an Arabic layout (the key reads ظ)', (await view()).open);
await close();
await b.key('k', CTRL | SHIFT);
await b.sleep(120);
ok('Ctrl+Shift+K is a different shortcut', !(await view()).open);

await b.click('#open2');
await b.sleep(150);
v = await view('pal2');
ok('a palette without recents shows no Recent heading', v.open && v.heads.length === 0 && JSON.stringify(v.rows) === JSON.stringify(['Alpha', 'Beta']), JSON.stringify(v));
ok('in English where the palette is English', (await E(`document.querySelector('#pal2 .ins-palette-input').placeholder`)) === 'Search pages and commands…');
await query('q', 'pal2');
ok('with English words for nothing found', (await E(`document.querySelector('#pal2 .ins-palette-empty').textContent`)) === 'No results' && (await view('pal2')).empty);
await b.key('Enter');
await b.sleep(100);
ok('Enter with nothing to choose leaves it open', (await view('pal2')).open);
await close();

// ------------------------------------------------------------------ the API
const added = await E(`(() => {
  window.__ran = null;
  const els = Insiyab.palette.add([{ label: 'تبديل الوضع', group: 'مخصّص', icon: '#i-sun', hint: 'T', keywords: ['toggle', 'mode'],
                                     run(e) { window.__ran = this.tagName + ':' + e.type; } }]);
  const el = els[0];
  return { n: els.length, tag: el.tagName, inPal: el.closest('dialog').id, sprite: el.querySelector('svg use').getAttribute('href'),
           kbd: el.querySelector('.ins-kbd').textContent, words: el.getAttribute('data-keywords') };
})()`);
ok('Insiyab.palette.add() makes a button in the first palette', added.n === 1 && added.tag === 'BUTTON' && added.inPal === 'pal', JSON.stringify(added));
ok('with its icon, shortcut and keywords', added.sprite === '#i-sun' && added.kbd === 'T' && added.words === 'toggle mode');
ok('Insiyab.palette("open") opens it', (await E(`Insiyab.palette('open').id`)) === 'pal' && (await view()).open);
ok('the added item heads its own group', (await view()).heads.includes('مخصّص'));
await query('toggle');
await b.key('Enter');
await b.sleep(150);
ok('choosing it runs its function, on the element', (await E(`window.__ran`)) === 'BUTTON:click' && !(await view()).open, await E(`window.__ran`));
await E(`document.querySelector('#pal [data-keywords="toggle mode"]').remove(); Insiyab.palette('#pal', 'open'); 0`);
await b.sleep(120);
ok('an item removed from the page is gone from the palette', !(await view()).rows.includes('تبديل الوضع'));
await close();

await E(`(() => { const d = document.createElement('dialog'); d.id = 'late'; d.setAttribute('data-ins-palette', ''); d.setAttribute('data-ins-palette-keys', 'none');
  d.innerHTML = '<a href="#one">One</a>'; document.body.appendChild(d); Insiyab.init(d); return 0; })()`);
ok('Insiyab.init() builds a palette added later', (await E(`document.querySelectorAll('#late .ins-palette-item').length`)) === 1);

const made = await E(`(() => {
  document.querySelectorAll('[data-ins-palette]').forEach((d) => d.remove());
  Insiyab.palette.add({ label: 'Only', href: '#only' });
  const d = document.querySelector('[data-ins-palette]');
  return d ? { ready: d.hasAttribute('data-ins-palette-ready'), cls: d.className, items: d.querySelectorAll('.ins-palette-item').length } : null;
})()`);
ok('with no palette on the page, add() makes one', made && made.ready && /ins-palette/.test(made.cls) && made.items === 1, JSON.stringify(made));
await b.key('k', CTRL);
await b.sleep(150);
ok('and the shortcut opens it', await E(`document.querySelector('[data-ins-palette]').open`));

// ------------------------------------------------------------------ loaded late
await b.goto(`${BASE}/test/fixtures/components.html`);
const late = await E(`new Promise((done) => {
  const d = document.createElement('dialog'); d.setAttribute('data-ins-palette', ''); d.innerHTML = '<a href="#x">X</a>'; document.body.appendChild(d);
  const s = document.createElement('script'); s.src = '../../dist/plugins/insiyab-palette.js';
  s.onload = () => done(d.hasAttribute('data-ins-palette-ready') && d.querySelectorAll('.ins-palette-item').length === 1);
  s.onerror = () => done(false);
  document.head.appendChild(s);
})`);
ok('a plugin loaded after the page builds the palettes already there', late);

// ------------------------------------------------------------------ the docs site's own search
await b.goto(`${BASE}/demo/tables.html`);
await b.key('k', CTRL);
await b.sleep(200);
v = await view('site-search');
ok("the docs search is the plugin, collecting the sidebar but not the logo", v.open && v.rows.includes('مقدّمة') && !v.rows.some((r) => r.includes('انسياب')), JSON.stringify(v.rows.slice(0, 4)));
await query('جدول', 'site-search');
ok('a singular finds the page named in the plural, through its lead', (await view('site-search')).rows[0] === 'الجداول', JSON.stringify((await view('site-search')).rows));
await query('butt', 'site-search');
ok('an English query finds the page by its English name', (await view('site-search')).rows[0] === 'الأزرار', JSON.stringify((await view('site-search')).rows));

ok('no console errors, warnings or exceptions', b.logs.length === 0, b.logs.join(' | '));

await b.close();
end();
