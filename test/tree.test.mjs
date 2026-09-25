// The tree plugin: roles and levels, one tab stop, the arrow keys in both
// directions of reading, Home/End, type-ahead, `*`, Enter on a link, selection,
// the chevron, the checkbox cascade with its mixed state, a tree without
// selection, the event and the API.
import { launch, BASE } from './lib/cdp.mjs';
import { suite } from './lib/check.mjs';

const { ok, end } = suite();
const b = await launch();
const E = (js) => b.evaluate(js);
const raw = async (key, code, vk, text) => {
  const base = { key, code, windowsVirtualKeyCode: vk, nativeVirtualKeyCode: vk };
  await b.send('Input.dispatchKeyEvent', { type: 'keyDown', ...base, text });
  await b.send('Input.dispatchKeyEvent', { type: 'keyUp', ...base });
  await b.sleep(60);
};
const K = {
  down: () => raw('ArrowDown', 'ArrowDown', 40), up: () => raw('ArrowUp', 'ArrowUp', 38),
  left: () => raw('ArrowLeft', 'ArrowLeft', 37), right: () => raw('ArrowRight', 'ArrowRight', 39),
  home: () => raw('Home', 'Home', 36), end: () => raw('End', 'End', 35),
  enter: () => raw('Enter', 'Enter', 13, '\r'), space: () => raw(' ', 'Space', 32, ' '), star: () => raw('*', 'NumpadMultiply', 106, '*'),
};
const at = () => E(`document.activeElement.id`);
const open = (id) => E(`document.getElementById('${id}').getAttribute('aria-expanded')`);
const checked = (id) => E(`document.getElementById('${id}').getAttribute('aria-checked')`);

await b.size(1280, 900);
await b.goto(`${BASE}/test/fixtures/tree.html`);
await E(`window.__tree = []; document.addEventListener('ins:tree', (e) => __tree.push(e.detail.item.id + ':' + e.detail.action + (e.detail.checked !== undefined ? ':' + e.detail.checked : ''))); 0`);

// ------------------------------------------------------------------ what it builds
const built = await E(`(() => { const t = document.getElementById('files'), d = document.getElementById('docs'), inv = document.getElementById('invoices');
  return { role: t.getAttribute('role'), item: d.getAttribute('role'), level: document.getElementById('inv1').getAttribute('aria-level'),
           set: d.getAttribute('aria-setsize') + '/' + d.getAttribute('aria-posinset'), group: d.querySelector('ul').getAttribute('role'),
           docsOpen: d.getAttribute('aria-expanded'), invOpen: inv.getAttribute('aria-expanded'), invHidden: inv.querySelector('ul').hidden,
           leaf: document.getElementById('notes').hasAttribute('aria-expanded'),
           stops: [...t.querySelectorAll('[tabindex="0"]')].map((x) => x.id), linkTab: document.querySelector('#contract a').tabIndex }; })()`);
ok('a tree of treeitems in groups', built.role === 'tree' && built.item === 'treeitem' && built.group === 'group');
ok('with levels and positions', built.level === '3' && built.set === '3/1', JSON.stringify(built));
ok('data-open opens a branch; the rest start closed', built.docsOpen === 'true' && built.invOpen === 'false' && built.invHidden);
ok('a leaf has no expanded state', !built.leaf);
ok('one tab stop for the whole tree, and links inside are not stops', JSON.stringify(built.stops) === JSON.stringify(['docs']) && built.linkTab === -1, JSON.stringify(built.stops));

// ------------------------------------------------------------------ the keyboard
await E(`document.getElementById('before').focus(); 0`);
await b.key('Tab');
ok('Tab reaches the tree on its first item', (await at()) === 'docs');
await K.down();
ok('Down walks to the next row showing', (await at()) === 'contract');
await K.down(); await K.down();
ok('past a closed branch, not into it', (await at()) === 'photos', await at());
await K.up();
ok('Up walks back', (await at()) === 'invoices');
await K.left();
ok('in Arabic, ← goes in: it opens the branch', (await open('invoices')) === 'true' && (await at()) === 'invoices');
await K.left();
ok('and ← again steps into it', (await at()) === 'inv1');
await K.right();
ok('→ steps out to the parent', (await at()) === 'invoices');
await K.right();
ok('→ on an open branch closes it', (await open('invoices')) === 'false');
await K.end();
ok('End goes to the last row showing', (await at()) === 'notes');
await K.home();
ok('Home to the first', (await at()) === 'docs');
await raw('م', 'KeyL', 76, 'م');
ok('a letter jumps to the next row starting with it, in Arabic', (await at()) === 'notes', await at());
await raw('ا', 'KeyH', 72, 'ا');
ok('and wraps round from the last row to the top', (await at()) === 'docs', await at());
await K.home();
await K.star();
ok('* opens every branch beside this one', (await open('photos')) === 'true');
await K.down(); await K.space();
ok('Space selects', (await E(`document.getElementById('contract').getAttribute('aria-selected')`)) === 'true');
await K.enter();
await b.sleep(80);
ok('Enter on a link follows it', (await E(`location.hash`)) === '#contract-file');
await b.key('Tab');
ok('Tab leaves the tree in one step', (await at()) === 'after', await at());
await b.key('Tab', 8);
ok('and comes back to the row that had focus', (await at()) === 'contract', await at());

// ------------------------------------------------------------------ the pointer
await b.click('#invoices > .ins-tree-row .ins-tree-toggle');
ok('the chevron opens a branch', (await open('invoices')) === 'true');
await b.click('#invoices > .ins-tree-row .ins-tree-label');
ok('a click on a branch without a link selects it and toggles it', (await open('invoices')) === 'false' && (await E(`document.getElementById('invoices').getAttribute('aria-selected')`)) === 'true');
ok('selection is single', (await E(`document.querySelectorAll('#files [aria-selected="true"]').length`)) === 1);
await b.click('#docs > .ins-tree-row .ins-tree-toggle');
ok('closing a branch with focus inside moves focus up to it', (await at()) === 'docs');
const chev = await E(`(() => { const open = getComputedStyle(document.querySelector('#p-sales > .ins-tree-row .ins-tree-toggle'), '::before').transform;
  const shut = getComputedStyle(document.querySelector('#docs > .ins-tree-row .ins-tree-toggle'), '::before').transform; return open !== shut; })()`);
ok('an open chevron is drawn differently from a closed one', chev);

// ------------------------------------------------------------------ checkboxes
ok('a branch ticked in the markup ticks what is in it', (await checked('p-hr-view')) === 'true' && (await checked('p-hr')) === 'true');
ok('the others start clear', (await checked('p-sales')) === 'false');
await b.click('#p-view .ins-check');
ok('ticking one child makes the branch mixed', (await checked('p-view')) === 'true' && (await checked('p-sales')) === 'mixed');
ok('the branch box is indeterminate, and not sent', (await E(`document.querySelector('#p-sales input').indeterminate`)) &&
  !(await E(`new FormData(document.getElementById('perm-form')).getAll('p').includes('sales')`)));
await b.click('#p-edit .ins-check');
ok('ticking all of them ticks the branch', (await checked('p-sales')) === 'true');
await b.click('#p-sales > .ins-tree-row .ins-check');
ok('clearing the branch clears everything in it', (await checked('p-view')) === 'false' && (await checked('p-edit')) === 'false' && (await checked('p-sales')) === 'false');
await E(`document.getElementById('p-edit').focus(); 0`);
await K.space();
ok('Space ticks the focused row', (await checked('p-edit')) === 'true' && (await checked('p-sales')) === 'mixed');
ok('a tree of checkboxes selects nothing', (await E(`document.querySelectorAll('#perms [aria-selected]').length`)) === 0);
ok('the form sends what is ticked', JSON.stringify(await E(`new FormData(document.getElementById('perm-form')).getAll('p')`)) === JSON.stringify(['sales.edit', 'hr', 'hr.view']), JSON.stringify(await E(`new FormData(document.getElementById('perm-form')).getAll('p')`)));

// ------------------------------------------------------------------ English, no selection, API
await E(`document.getElementById('en-a').focus(); 0`);
await K.right();
ok('in English, → goes in', (await open('en-a')) === 'true');
await K.space();
ok('data-ins-tree-select="none" selects nothing', (await E(`document.querySelectorAll('#en [aria-selected]').length`)) === 0);
ok('ins:tree reports each change', await E(`['invoices:open', 'invoices:select', 'contract:select', 'p-view:check:true', 'en-a:open'].every((x) => __tree.includes(x))`), JSON.stringify(await E(`__tree`)));
await E(`Insiyab.tree('#inv2', 'select'); 0`);
ok('Insiyab.tree(item, "select") selects it and opens the way to it', (await E(`document.getElementById('inv2').getAttribute('aria-selected')`)) === 'true' && (await open('invoices')) === 'true' && (await open('docs')) === 'true');
ok('Insiyab.tree(tree) returns the selected item', (await E(`Insiyab.tree('#files').id`)) === 'inv2');
ok('or, in a tree of checkboxes, the items ticked', JSON.stringify(await E(`Insiyab.tree('#perms').map((i) => i.id)`)) === JSON.stringify(['p-edit', 'p-hr', 'p-hr-view']), JSON.stringify(await E(`Insiyab.tree('#perms').map((i) => i.id)`)));
await E(`Insiyab.tree('#files', 'close'); 0`);
ok('Insiyab.tree(tree, "close") closes every branch', (await E(`document.querySelectorAll('#files [aria-expanded="true"]').length`)) === 0);
await E(`Insiyab.tree('#p-sales', 'check'); 0`);
ok('Insiyab.tree(item, "check") ticks and cascades', (await checked('p-view')) === 'true' && (await checked('p-sales')) === 'true');
const adopted = await E(`(() => { const a = document.getElementById('b-a'), row = a.firstElementChild;
  return { rows: document.querySelectorAll('#built .ins-tree-row').length, same: row.contains(document.getElementById('b-a-label')),
    nested: document.querySelectorAll('#built .ins-tree-row .ins-tree-row').length, toggle: row.firstElementChild.classList.contains('ins-tree-toggle'),
    label: document.getElementById('b-a1-link').classList.contains('ins-tree-label'), role: a.getAttribute('role'), open: a.getAttribute('aria-expanded') }; })()`);
ok('a row already in the markup is kept, not wrapped in a second one', adopted.rows === 2 && adopted.nested === 0 && adopted.same, JSON.stringify(adopted));
ok('and is set up like one the plugin made', adopted.toggle && adopted.label && adopted.role === 'treeitem' && adopted.open === 'true', JSON.stringify(adopted));

ok('no console errors, warnings or exceptions', b.logs.length === 0, b.logs.join(' | '));
await b.close();
end();
