// The notification centre plugin on test/fixtures/notify.html: count, read on click, mark all, live updates, the 99+ cap, empty, name, in a dialog, Escape and outside press.
import { launch, BASE } from './lib/cdp.mjs';
import { suite } from './lib/check.mjs';

const { ok, end } = suite();
const b = await launch({ width: 1280, height: 900 });
const E = (js) => b.evaluate(js);
await b.size(1280, 900);
await b.goto(`${BASE}/test/fixtures/notify.html`);
await E(`window.__ev = []; document.addEventListener('ins:notify', (e) => __ev.push({ el: e.detail.el.id, item: e.detail.item && e.detail.item.id, all: e.detail.all })); document.addEventListener('click', (e) => { if (e.target.closest('a[href]')) e.preventDefault(); }); 0`);

const at = async (sel) => {
  const p = await E(`(() => { const r = document.querySelector(${JSON.stringify(sel)}).getBoundingClientRect(); return { x: r.left + r.width / 2, y: r.top + r.height / 2 }; })()`);
  await b.mouse(p.x, p.y);
  await b.sleep(250);
};
const count = (id) => E(`(() => { const c = document.querySelector('#${id} .ins-notify-count'); return c ? (c.hidden ? '' : c.textContent) : null; })()`);
const name = (id) => E(`document.getElementById('${id}-s').getAttribute('aria-label')`);
const shown = (id) => E(`document.querySelector('#${id} > .ins-pop-body').matches(':popover-open')`);

ok('count: the bell shows the unread items', (await count('nb')) === '3');
ok('count: tone bad, on the bell, hidden from the reader', await E(`(() => { const c = document.querySelector('#nb .ins-notify-count'); return c.parentNode.id === 'nb-s' && c.getAttribute('aria-hidden') === 'true' && getComputedStyle(c).backgroundColor === 'rgb(220, 38, 38)'; })()`));
ok('name: the bell says how many are unread', (await name('nb')) === 'الإشعارات، 3 غير مقروءة', await name('nb'));
ok('name: in English where the page is English', (await name('ne')) === 'Notifications, 1 unread', await name('ne'));
ok('unread items carry hidden words for a screen reader', await E(`document.querySelectorAll('#nb .ins-notify-flag.ins-sr-only').length === 3 && document.querySelector('#n1 .ins-notify-flag').textContent === 'غير مقروء'`));
ok('API: Insiyab.notify(box) gives the count', (await E(`Insiyab.notify('#nb')`)) === 3);

await at('#nb-s');
ok('panel: opens in the top layer', await shown('nb'));
const panel = await E(`(() => { const r = document.querySelector('#nb > .ins-pop-body').getBoundingClientRect(); return { w: Math.round(r.width), inView: r.left >= 0 && r.right <= innerWidth }; })()`);
ok('panel: about 22rem wide, inside the viewport', panel.w === 352 && panel.inView, JSON.stringify(panel));
await at('#n1');
ok('read on click: the item loses its mark and the count drops', (await E(`!document.getElementById('n1').hasAttribute('data-ins-unread') && !document.querySelector('#n1 .ins-notify-flag')`)) && (await count('nb')) === '2');
ok('read on click: ins:notify with the item', JSON.stringify(await E(`__ev[0]`)) === JSON.stringify({ el: 'nb', item: 'n1', all: false }), JSON.stringify(await E(`__ev`)));
await at('#n3');
ok('a read item clicked again fires nothing', (await E(`__ev.length`)) === 1);
ok('name follows the count', (await name('nb')) === 'الإشعارات، إشعاران غير مقروءين', await name('nb'));
await at('#nb-all');
ok('mark all: no unread left, the count is hidden', (await E(`document.querySelectorAll('#nb [data-ins-unread]').length`)) === 0 && (await count('nb')) === '');
ok('mark all: ins:notify with all', JSON.stringify(await E(`__ev[1]`)) === JSON.stringify({ el: 'nb', item: null, all: true }));
ok('name with nothing unread is the plain label', (await name('nb')) === 'الإشعارات');
ok('the panel stays open while marking', await shown('nb'));

await E(`(() => { window.__rang = 0; const svg = document.querySelector('#nb-s svg'), a = svg.animate.bind(svg); svg.animate = function (k, o) { window.__rang++; return a(k, o); };
  document.getElementById('nb-list').insertAdjacentHTML('afterbegin', '<li class="ins-list-item" data-ins-unread id="n5"><span class="ins-list-text"><span class="ins-list-title">جديد</span></span></li>'); return 0; })()`);
await b.sleep(200);
ok('live: a new unread item is counted', (await count('nb')) === '1' && (await E(`!!document.querySelector('#n5 .ins-notify-flag')`)));
ok('live: and the bell rings once', (await E(`__rang`)) === 1, await E(`__rang`));
await E(`document.getElementById('n3').setAttribute('data-ins-unread', ''); 0`); await b.sleep(150);
ok('live: an item marked unread again is counted, and rings', (await count('nb')) === '2' && (await E(`__rang`)) === 2);
await E(`document.getElementById('n5').remove(); 0`); await b.sleep(150);
ok('live: a removed item leaves the count', (await count('nb')) === '1');
await E(`document.documentElement.setAttribute('data-ins-fx', 'off'); document.getElementById('nb-list').insertAdjacentHTML('beforeend', '<li class="ins-list-item" data-ins-unread><span class="ins-list-text"><span class="ins-list-title">x</span></span></li>'); 0`); await b.sleep(150);
ok('live: no ring when motion is off', (await count('nb')) === '2' && (await E(`__rang`)) === 2);
await E(`document.documentElement.removeAttribute('data-ins-fx'); 0`);
ok('API: marking one quietly gives the count left', (await E(`Insiyab.notify('#nb', '#n3')`)) === 1 && (await E(`__ev.length`)) === 2);

await E(`(() => { const l = document.getElementById('nb-list'); let s = ''; for (let i = 0; i < 120; i++) s += '<li class="ins-list-item" data-ins-unread><span class="ins-list-text"><span class="ins-list-title">n' + i + '</span></span></li>'; l.insertAdjacentHTML('beforeend', s); return 0; })()`);
await b.sleep(200);
ok('cap: above 99 the count reads 99+', (await count('nb')) === '99+');
ok('cap: the name keeps the real number', (await name('nb')) === 'الإشعارات، 121 غير مقروءة', await name('nb'));
const scroll = await E(`(() => { const l = document.getElementById('nb-list'), body = l.parentNode, head = body.querySelector('.ins-notify-head'), foot = body.querySelector('.ins-notify-foot'), br = body.getBoundingClientRect();
  return { scrolls: l.scrollHeight > l.clientHeight, headIn: head.getBoundingClientRect().top >= br.top, footIn: foot.getBoundingClientRect().bottom <= br.bottom + 1, fits: br.bottom <= innerHeight }; })()`);
ok('panel: the list scrolls between a fixed head and foot', scroll.scrolls && scroll.headIn && scroll.footIn && scroll.fits, JSON.stringify(scroll));
ok('API: all gives 0', (await E(`Insiyab.notify('#nb', 'all')`)) === 0 && (await count('nb')) === '');

await b.key('Escape'); await b.sleep(250);
ok('Escape closes the panel and focus goes back to the bell', !(await shown('nb')) && (await E(`document.activeElement.id`)) === 'nb-s');
await at('#nb-s');
await at('#nb-s');
ok('a second click on the bell closes it', !(await shown('nb')));
await at('#nb-s');
await b.mouse(60, 600); await b.sleep(250);
ok('an outside press closes it', !(await shown('nb')));

ok('empty: no count, and an empty state in the panel', (await count('n0')) === '' && (await E(`(() => { const e = document.querySelector('#n0 .ins-empty'); return !!e && !e.hidden && e.textContent.trim() === 'لا إشعارات' && document.querySelector('#n0 .ins-notify-list').hidden; })()`)));
await E(`document.querySelector('#n0 .ins-notify-list').insertAdjacentHTML('beforeend', '<li class="ins-list-item" data-ins-unread><span class="ins-list-text"><span class="ins-list-title">x</span></span></li>'); 0`); await b.sleep(150);
ok('empty: the first item hides the empty state again', (await count('n0')) === '1' && (await E(`document.querySelector('#n0 .ins-empty').hidden && !document.querySelector('#n0 .ins-notify-list').hidden`)));

await at('#open-d'); await b.sleep(450);
await at('#nd-s');
ok('in a dialog: the panel opens over it and takes the pointer', (await shown('nd')) && (await E(`(() => { const r = document.querySelector('#nd > .ins-pop-body').getBoundingClientRect(); const h = document.elementFromPoint(r.left + r.width / 2, r.top + 10); return document.querySelector('#nd > .ins-pop-body').contains(h); })()`)));
await at('#nd1');
ok('in a dialog: a click marks the item read', (await count('nd')) === '');
await b.mouse(30, 450); await b.sleep(300);
ok('in a dialog: an outside press closes the panel and not the dialog', !(await shown('nd')) && (await E(`document.getElementById('dn').open`)));
await at('#nd-s');
await b.key('Escape'); await b.sleep(300);
ok('in a dialog: Escape closes the panel only', !(await shown('nd')) && (await E(`document.getElementById('dn').open`)) && (await E(`document.activeElement.id`)) === 'nd-s');
await b.key('Escape'); await b.sleep(400);
ok('then Escape closes the dialog', !(await E(`document.getElementById('dn').open`)));

ok('no console errors or exceptions', b.logs.length === 0, b.logs.join(' | '));
await b.close();
end();
