// The carousel plugin: the roles and slide labels, the buttons off at the ends,
// the dots, the arrow keys in each reading direction, Home/End, swiping (the
// scroller itself) followed by the dots, three-up by container width, a narrow
// container and a single slide, the event and the API.
import { launch, BASE } from './lib/cdp.mjs';
import { suite } from './lib/check.mjs';

const { ok, end } = suite();
const b = await launch();
const E = (js) => b.evaluate(js);
const raw = async (key, code, vk) => {
  const base = { key, code, windowsVirtualKeyCode: vk, nativeVirtualKeyCode: vk };
  await b.send('Input.dispatchKeyEvent', { type: 'rawKeyDown', ...base });
  await b.send('Input.dispatchKeyEvent', { type: 'keyUp', ...base });
};
const settle = () => b.sleep(700);
const view = (id) => E(`(() => { const c = document.getElementById('${id}'), t = c.querySelector('.ins-carousel-track');
  const dots = [...c.querySelectorAll('.ins-carousel-dot')];
  return { at: Insiyab.carousel(c), dots: dots.length, current: dots.findIndex((d) => d.getAttribute('aria-current') === 'true'),
           prev: c.querySelector('.ins-carousel-btn--prev').disabled, next: c.querySelector('.ins-carousel-btn--next').disabled,
           prevHidden: c.querySelector('.ins-carousel-btn--prev').hidden, scroll: Math.round(t.scrollLeft) }; })()`);

await b.size(1280, 900);
await b.goto(`${BASE}/test/fixtures/carousel.html`);
await E(`window.__car = []; document.addEventListener('ins:carousel', (e) => __car.push(e.detail.carousel.id + ':' + e.detail.index)); 0`);

// ------------------------------------------------------------------ what it builds
const built = await E(`(() => { const c = document.getElementById('c1'), s = document.getElementById('c1-1');
  return { role: c.getAttribute('role'), rd: c.getAttribute('aria-roledescription'), track: !!c.querySelector(':scope > .ins-carousel-track'),
           slide: s.getAttribute('role') + '|' + s.getAttribute('aria-roledescription') + '|' + s.getAttribute('aria-label'),
           last: document.getElementById('c1-5').getAttribute('aria-label'), live: c.querySelector('.ins-carousel-track').getAttribute('aria-live'),
           prev: c.querySelector('.ins-carousel-btn--prev').getAttribute('aria-label'), snap: getComputedStyle(c.querySelector('.ins-carousel-track')).scrollSnapType }; })()`);
ok('a region described as a carousel, in the page\'s language', built.role === 'region' && built.rd === 'عرض شرائح', JSON.stringify(built));
ok('slides written straight in are put in a track', built.track);
ok('each slide is a group that says where it is', built.slide === 'group|شريحة|1 من 5' && built.last === '5 من 5', built.slide);
ok('changes are announced politely, never as an interruption', built.live === 'polite');
ok('the row is a native snap scroller', /x mandatory/.test(built.snap), built.snap);
let v = await view('c1');
ok('it starts on the first slide: previous is off, next is on', v.at === 0 && v.prev && !v.next, JSON.stringify(v));
ok('a dot for each stop, the first current', v.dots === 5 && v.current === 0);

// ------------------------------------------------------------------ moving
await b.click('#c1 .ins-carousel-btn--next');
await settle();
v = await view('c1');
ok('next moves one slide on', v.at === 1 && v.current === 1 && !v.prev, JSON.stringify(v));
ok('in Arabic, onward is to the left: the row scrolls negative', v.scroll < 0, v.scroll);
await b.click('#c1 .ins-carousel-dot:nth-child(4)');
await settle();
ok('a dot goes straight to its slide', (await view('c1')).at === 3);
await E(`document.querySelector('#c1 .ins-carousel-track').focus(); 0`);
await raw('ArrowLeft', 'ArrowLeft', 37);
await settle();
v = await view('c1');
ok('in Arabic, ← is onward: the last slide', v.at === 4 && v.next, JSON.stringify(v));
await raw('ArrowLeft', 'ArrowLeft', 37);
await settle();
ok('and it stops at the end', (await view('c1')).at === 4);
await raw('Home', 'Home', 36);
await settle();
ok('Home goes back to the first', (await view('c1')).at === 0);
await raw('End', 'End', 35);
await settle();
ok('End to the last', (await view('c1')).at === 4);
ok('ins:carousel reports each slide reached', JSON.stringify((await E(`__car`)).filter((x) => x.startsWith('c1'))) === JSON.stringify(['c1:1', 'c1:3', 'c1:4', 'c1:0', 'c1:4']), JSON.stringify(await E(`__car`)));

/* A swipe is the scroller's own: the dots follow it. */
await E(`(() => { const t = document.querySelector('#c1 .ins-carousel-track'); t.style.scrollBehavior = 'auto'; t.style.scrollSnapType = 'none'; t.scrollLeft = -(t.clientWidth + 16) * 2; return 0; })()`);
await settle();
ok('scrolled by hand, the dots and buttons follow', (await view('c1')).current === 2, JSON.stringify(await view('c1')));
await E(`document.querySelector('#c1 .ins-carousel-track').style.scrollSnapType = ''; 0`);

// ------------------------------------------------------------------ several at a time
const three = await E(`(() => { const c = document.getElementById('c3'), s = c.querySelector('.ins-carousel-slide'); return { w: s.getBoundingClientRect().width, box: c.getBoundingClientRect().width, dots: c.querySelectorAll('.ins-carousel-dot').length }; })()`);
ok('--3: three slides at a time in a wide carousel', Math.abs(three.w * 3 + 32 - three.box) < 3, JSON.stringify(three));
ok('with one stop for each place the row can start', three.dots === 3, three.dots);
const narrow = await E(`(() => { const c = document.getElementById('c3n'), s = c.querySelector('.ins-carousel-slide'); return { w: s.getBoundingClientRect().width, box: c.getBoundingClientRect().width, dots: c.querySelectorAll('.ins-carousel-dot').length }; })()`);
ok('and one at a time in a narrow one', Math.abs(narrow.w - narrow.box) < 2, JSON.stringify(narrow));
ok('data-ins-carousel-dots="off" draws no dots', narrow.dots === 0);

// ------------------------------------------------------------------ English, one slide, API
await E(`document.querySelector('#ce .ins-carousel-track').focus(); 0`);
await raw('ArrowRight', 'ArrowRight', 39);
await settle();
v = await view('ce');
ok('in English, → is onward', v.at === 1 && v.scroll > 0, JSON.stringify(v));
ok('with English labels', (await E(`document.querySelector('#ce .ins-carousel-btn--next').getAttribute('aria-label')`)) === 'Next' && (await E(`document.querySelector('#ce .ins-carousel-slide').getAttribute('aria-label')`)) === '1 of 3');
v = await view('c-one');
ok('a single slide has no buttons and no dots', v.prevHidden && (await E(`document.querySelector('#c-one .ins-carousel-dots').hidden`)));
await E(`Insiyab.carousel('#ce', 2); 0`);
await settle();
ok('Insiyab.carousel(el, n) moves to a stop, and Insiyab.carousel(el) reads it', (await E(`Insiyab.carousel('#ce')`)) === 2);
const motion = await E(`(() => { const t = document.querySelector('#ce .ins-carousel-track'); const x = t.scrollLeft; return new Promise((done) => setTimeout(() => done(t.scrollLeft === x), 1500)); })()`);
ok('left alone, it does not move by itself', motion);

ok('no console errors, warnings or exceptions', b.logs.length === 0, b.logs.join(' | '));
await b.close();
end();
