// The scrollspy plugin: the current section against a line a third of the way
// down, the last section at the very end, a click held through its own scroll,
// a box with its own scrollbar, the body as the scroller, the offset attribute,
// links to other pages left alone, the event and the API.
import { launch, BASE } from './lib/cdp.mjs';
import { suite } from './lib/check.mjs';

const { ok, end } = suite();
const b = await launch();
const E = (js) => b.evaluate(js);

const active = (nav = 'toc') => E(`(() => { const a = document.querySelector('#${nav} a.is-active'); return a ? a.getAttribute('href') + (a.getAttribute('aria-current') === 'location' ? '' : '!') : null; })()`);
const to = async (js) => { await E(`(${js}); 0`); await b.sleep(180); };
const topOf = (id) => `document.getElementById('${id}').getBoundingClientRect().top + scrollY`;

await b.size(1280, 800);
await b.goto(`${BASE}/test/fixtures/scrollspy.html`);
await E(`document.documentElement.style.scrollBehavior = 'auto'; window.__spy = []; document.addEventListener('ins:scrollspy', (e) => __spy.push(e.detail.nav.id + ':' + (e.detail.link ? e.detail.link.getAttribute('href') : '-'))); 0`);

ok('at the top, the first section is current', (await active()) === '#s1', await active());
ok('marked with is-active and aria-current="location"', !(await active()).endsWith('!'));
await to(`scrollTo(0, ${topOf('s2')} - 200)`);
ok('a section counts once its top passes a third of the way down', (await active()) === '#s2', await active());
await to(`scrollTo(0, ${topOf('s3')} - 300)`);
ok('not before', (await active()) === '#s2', await active());
await to(`scrollTo(0, ${topOf('s3')} - 250)`);
ok('and just after', (await active()) === '#s3', await active());
await to(`scrollTo(0, document.documentElement.scrollHeight)`);
ok('at the very end, the last section, however short', (await active()) === '#s4', await active());
ok('only one link is marked at a time', (await E(`document.querySelectorAll('#toc a.is-active').length`)) === 1);
ok('a link to another page is left alone', !(await E(`document.querySelector('#toc a[href="other.html#s1"]').hasAttribute('aria-current')`)));
ok('ins:scrollspy says each change', JSON.stringify((await E(`__spy`)).filter((x) => x.startsWith('toc'))) === JSON.stringify(['toc:#s2', 'toc:#s3', 'toc:#s4']), JSON.stringify(await E(`__spy`)));

// ------------------------------------------------------------------ a click
await to(`scrollTo(0, 0)`);
await E(`document.documentElement.style.scrollBehavior = 'smooth'; __spy.length = 0; 0`);
await b.click('#toc a[href="#s3"]');
await b.sleep(60);
ok('a click marks its link at once', (await active()) === '#s3');
const flight = await E(`(() => { const nav = document.getElementById('toc'); return { bars: nav.querySelectorAll('.ins-toc-indicator').length, moving: nav.classList.contains('ins-toc-moving') }; })()`);
ok('and its marker flies there the way the sidebar\'s does: one stand-in bar, the real markers hidden', flight.bars === 1 && flight.moving, JSON.stringify(flight));
await b.sleep(700);
const landed = await E(`(() => { const nav = document.getElementById('toc'); return { bars: nav.querySelectorAll('.ins-toc-indicator').length, moving: nav.classList.contains('ins-toc-moving') }; })()`);
ok('then lands, and the stand-in is gone', landed.bars === 0 && !landed.moving, JSON.stringify(landed));
await b.sleep(1500);
ok('and the scroll it starts does not step through the sections on the way', JSON.stringify(__filter(await E(`__spy`))) === JSON.stringify(['toc:#s3']), JSON.stringify(await E(`__spy`)));
function __filter(list) { return list.filter((x) => x.startsWith('toc')); }
await E(`document.documentElement.style.scrollBehavior = 'auto'; 0`);
await to(`scrollTo(0, ${topOf('s1')})`);
await b.sleep(200);
ok('once it has stopped, scrolling moves the marker again', (await active()) === '#s1', await active());
await E(`Insiyab.flyMarker(document.getElementById('toc'), document.querySelector('#toc a[href="#s1"]'), document.querySelector('#toc a[href="#s4"]'), { bar: 'ins-toc-indicator', moving: 'ins-toc-moving' }); 0`);
await b.sleep(150);
await E(`Insiyab.flyMarker(document.getElementById('toc'), document.querySelector('#toc a[href="#s4"]'), document.querySelector('#toc a[href="#s2"]'), { bar: 'ins-toc-indicator', moving: 'ins-toc-moving' }); 0`);
ok('a new move mid-flight replaces the old one: still one stand-in', (await E(`document.querySelectorAll('#toc .ins-toc-indicator').length`)) === 1);
await b.sleep(700);
ok('and it lands cleanly', (await E(`document.querySelectorAll('#toc .ins-toc-indicator').length === 0 && !document.getElementById('toc').classList.contains('ins-toc-moving')`)));

// ------------------------------------------------------------------ a box of its own
ok('a box with its own scrollbar is measured against itself', (await active('box-toc')) === '#b1', await active('box-toc'));
await E(`document.getElementById('box').style.scrollBehavior = 'auto'; 0`);
await to(`document.getElementById('box').scrollTop = 400`);
ok('scrolling the box moves its own list', (await active('box-toc')) === '#b2' && (await active()) === '#s1', `${await active('box-toc')} ${await active()}`);
ok('a list left unpositioned does not fly: no stand-in placed in the wrong box', (await E(`document.querySelectorAll('.ins-toc-indicator').length`)) === 0);
await to(`document.getElementById('box').scrollTop = 99999`);
ok('and its end marks its last section', (await active('box-toc')) === '#b3');

// ------------------------------------------------------------------ offset, the body, the API
await E(`document.getElementById('toc').setAttribute('data-ins-scrollspy-offset', '40'); 0`);
await to(`scrollTo(0, ${topOf('s2')} - 200)`);
ok('data-ins-scrollspy-offset moves the line', (await active()) === '#s1', await active());
await to(`scrollTo(0, ${topOf('s2')} - 30)`);
ok('to that many pixels from the top', (await active()) === '#s2', await active());
await E(`document.getElementById('toc').removeAttribute('data-ins-scrollspy-offset'); 0`);

await to(`scrollTo(0, 0)`);
await E(`document.documentElement.setAttribute('data-ins-scrollbar', 'lead'); 0`);
ok('Insiyab.scrollspy(nav) measures again and returns the link', (await E(`Insiyab.scrollspy('#toc').getAttribute('href')`)) === '#s1');
await to(`document.body.scrollTop = document.getElementById('s3').offsetTop - 150`);
ok('with the body as the scroller, it follows the body', (await active()) === '#s3', `${await active()} body=${await E('document.body.scrollTop')}`);

ok('no console errors, warnings or exceptions', b.logs.length === 0, b.logs.join(' | '));
await b.close();
end();
