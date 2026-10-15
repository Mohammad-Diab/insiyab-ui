// The month plugin's week and day views and their work hours: the work hours are the
// default (6 to 18), an event wholly outside them is counted in a band and opens the
// whole day when pressed, an event across their edge is cut there, the whole day is a
// scroll box as tall as the work hours that starts at the work start, the days stay in
// line with the hours in both directions, PageUp and PageDown turn the week from
// anywhere inside, and the now line follows the shown hours.
import { launch, BASE } from './lib/cdp.mjs';
import { suite } from './lib/check.mjs';

const { ok, end } = suite();
const b = await launch();
const E = (js) => b.evaluate(js);

await b.size(1280, 1300);
await b.goto(`${BASE}/test/fixtures/month.html`);

await E(`(() => {
  window.__ev = [];
  for (const n of ['ins:month-day', 'ins:month-view', 'ins:month-date'])
    document.addEventListener(n, (e) => window.__ev.push(n + ' ' + JSON.stringify({ date: e.detail.date, time: e.detail.time, view: e.detail.view })));
  return 0;
})()`);

const sel = (id, s) => `document.getElementById('${id}').querySelector(${JSON.stringify(s)})`;
const box = (id) => `document.getElementById('${id}')`;
const state = (id) => E(`(() => {
  const box = ${box(id)}, week = box.querySelector('.ins-month-week'), lane = box.querySelector('.ins-month-lane');
  const hp = lane.offsetHeight / box.querySelectorAll('.ins-month-hour').length;
  return { work: box.getAttribute('data-ins-work-hours'), top: week.scrollTop, hp, rows: box.querySelectorAll('.ins-month-hour').length,
           max: week.scrollHeight - week.clientHeight, chips: [...box.querySelectorAll('.ins-month-event.is-timed')].map((c) => c.textContent),
           pressed: (box.querySelector('[data-ins-month-hours]') || {}).ariaPressed || null };
})()`);
const active = () => E(`(() => { const a = document.activeElement; return a ? (a.getAttribute('data-ins-month-hours') !== null ? 'hours' : a.getAttribute('data-ins-month-go') ? 'go:' + a.getAttribute('data-ins-month-go') : a.getAttribute('data-ins-month-view') ? 'view:' + a.getAttribute('data-ins-month-view') : a.tagName) : null; })()`);

// ------------------------------------------------------------------ the work hours
const day = await E(`(() => {
  const box = ${box('m-day')};
  return { hours: [...box.querySelectorAll('.ins-month-hour')].map((h) => h.textContent), pressed: box.querySelector('[data-ins-month-hours]')?.getAttribute('aria-pressed'),
           chips: [...box.querySelectorAll('.ins-month-event.is-timed')].map((c) => c.textContent), all: box.querySelectorAll('.ins-month-week.is-all').length };
})()`);
ok('a day by the hour shows the work hours: 12 rows from 6:00 ص', day.hours.length === 12 && day.hours[0] === '6:00 ص' && day.hours[11] === '5:00 م', JSON.stringify(day.hours));
ok('the hours toggle is pressed, and there is no scroll box', day.pressed === 'true' && day.all === 0, `${day.pressed} ${day.all}`);
ok('the three events wholly outside the hours have no chip', day.chips.length === 2 && !/رحلة الصباح|تجهيز القاعة|عشاء العمل/.test(day.chips.join('|')), JSON.stringify(day.chips));

// ------------------------------------------------------------------ the bands
const bands = await E(`(() => {
  const box = ${box('m-day')}, pill = (k) => box.querySelector('[data-ins-month-band="' + k + '"]');
  const labels = [...box.querySelectorAll('.ins-month-band-label')].map((l) => l.textContent);
  const before = pill('before'), after = pill('after');
  return { b: [before.textContent, before.getAttribute('aria-label'), before.title], a: [after.textContent, after.getAttribute('aria-label'), after.title], labels,
           order: before.getBoundingClientRect().bottom <= box.querySelector('.ins-month-lane').getBoundingClientRect().top + 1 && after.getBoundingClientRect().top >= box.querySelector('.ins-month-lane').getBoundingClientRect().bottom - 1 };
})()`);
ok('the before band counts two events, named and listed', bands.b[0] === '+2' && bands.b[1] === 'موعدان قبل 6:00 ص' && bands.b[2] === '4:00 ص رحلة الصباح، 5:00 ص تجهيز القاعة', JSON.stringify(bands.b));
ok('the after band counts one', bands.a[0] === '+1' && bands.a[1] === 'موعد واحد بعد 6:00 م', JSON.stringify(bands.a));
ok('the band labels say the hour they stop at', bands.labels.join('|') === 'قبل 6:00 ص|بعد 6:00 م', JSON.stringify(bands.labels));
ok('the before band is above the hours and the after band below', bands.order);

// ------------------------------------------------------------------ cut edges
const cut = await E(`(() => {
  const box = ${box('m-day')}, lane = box.querySelector('.ins-month-lane').getBoundingClientRect();
  const chip = (t) => [...box.querySelectorAll('.ins-month-event.is-timed')].find((c) => c.textContent.includes(t));
  const a = chip('استقبال الوفد'), z = chip('ورشة عمل'), ar = a.getBoundingClientRect(), zr = z.getBoundingClientRect();
  return { aTop: ar.top - lane.top, aStart: a.classList.contains('is-cut-start'), aEnd: a.classList.contains('is-cut-end'), when: a.querySelector('.ins-month-when').textContent,
           zBottom: lane.bottom - zr.bottom, zStart: z.classList.contains('is-cut-start'), zEnd: z.classList.contains('is-cut-end'), zRadius: getComputedStyle(z).borderEndStartRadius };
})()`);
ok('an event across the start is cut there and sits one pixel under the top', cut.aStart && !cut.aEnd && Math.abs(cut.aTop - 1) <= 1, JSON.stringify(cut));
ok('an event across the end is cut there and ends one pixel above the bottom', cut.zEnd && !cut.zStart && Math.abs(cut.zBottom - 1) <= 1, JSON.stringify(cut));
ok('a cut event keeps its real times', cut.when === '5:30 ص – 7:30 ص', cut.when);
ok('the cut end has square corners', parseFloat(cut.zRadius) === 0, cut.zRadius);

// ------------------------------------------------------------------ pressing a band
await E(`window.__ev.length = 0; 0`);
await b.click('#m-day [data-ins-month-band="before"]');
const opened = await state('m-day');
ok('pressing the before band shows the whole day', opened.work === 'off' && opened.rows === 24, `${opened.work} ${opened.rows}`);
ok('and scrolls half an hour above its first event', Math.abs(opened.top - (4 * opened.hp - opened.hp / 2)) <= 1, `${opened.top} vs ${4 * opened.hp - opened.hp / 2}`);
ok('the hidden event now has its chip', opened.chips.some((t) => t.includes('رحلة الصباح')), JSON.stringify(opened.chips));
ok('the press is not a press on the day', (await E(`window.__ev.filter((e) => e.startsWith('ins:month-day')).length`)) === 0);
ok('with the whole day shown the toggle is not pressed', opened.pressed === 'false', opened.pressed);

// ------------------------------------------------------------------ the toggle, from the keyboard
await E(`${sel('m-day', '[data-ins-month-hours]')}.focus(); 0`);
await b.key(' ');
const back = await state('m-day');
ok('Space on the toggle goes back to the work hours', back.work !== 'off' && back.rows === 12 && back.pressed === 'true', `${back.work} ${back.rows}`);
ok('and the toggle keeps focus through the redraw', (await active()) === 'hours', await active());

await E(`${sel('m-day', '[data-ins-month-band="after"]')}.focus(); 0`);
await b.key('Enter');
const late = await state('m-day');
ok('Enter on the after band shows the whole day at its event, held at the bottom', late.work === 'off' && Math.abs(late.top - late.max) <= 1, `${late.top} vs ${late.max}`);
ok('and moves focus to the hours toggle', (await active()) === 'hours', await active());
await E(`${sel('m-day', '[data-ins-month-hours]')}.click(); 0`);
await b.sleep(80);

// ------------------------------------------------------------------ no hours toggle
await E(`${box('m-day')}.setAttribute('data-ins-hours-switch', 'off'); Insiyab.month('#m-day'); 0`);
const noSwitch = await E(`(() => ({ toggle: !!${sel('m-day', '[data-ins-month-hours]')}, bands: ${box('m-day')}.querySelectorAll('[data-ins-month-band]').length }))()`);
ok('data-ins-hours-switch="off" removes the toggle and leaves the bands', !noSwitch.toggle && noSwitch.bands === 2, JSON.stringify(noSwitch));
await E(`${sel('m-day', '[data-ins-month-band="after"]')}.focus(); 0`);
await b.key('Enter');
ok('a band pressed from the keyboard with no toggle hands focus to Today', (await state('m-day')).work === 'off' && (await E(`document.activeElement.getAttribute('data-ins-month-go')`)) === 'today');
await E(`${box('m-day')}.removeAttribute('data-ins-hours-switch'); ${box('m-day')}.removeAttribute('data-ins-work-hours'); Insiyab.month('#m-day'); 0`);

// ------------------------------------------------------------------ the whole day: a scroll box, in line
for (const [id, dir] of [['m-week', 'rtl'], ['m-week-en', 'ltr']]) {
  await E(`${box(id)}.scrollIntoView({ block: 'start', behavior: 'instant' }); 0`);
  await b.sleep(150);
  const w = await E(`(() => {
    const box = ${box(id)}, week = box.querySelector('.ins-month-week'), lane = box.querySelector('.ins-month-lane');
    const rows = box.querySelectorAll('.ins-month-hour').length, hp = lane.offsetHeight / rows;
    const edges = (list) => [...list].map((n) => { const r = n.getBoundingClientRect(); return [r.left, r.right]; });
    return { rows, hp, top: week.scrollTop, h: week.getBoundingClientRect().height, topH: box.querySelector('.ins-month-week-top').offsetHeight, all: week.classList.contains('is-all'),
             heads: edges(box.querySelectorAll('.ins-month-wday')), lanes: edges(box.querySelectorAll('.ins-month-lane')), dir: getComputedStyle(box).direction };
  })()`);
  ok(`${dir}: the whole day has 24 hours in a scroll box`, w.rows === 24 && w.all && w.dir === dir, JSON.stringify([w.rows, w.all, w.dir]));
  ok(`${dir}: it starts scrolled to the work start`, Math.abs(w.top - 6 * w.hp) <= 1, `${w.top} vs ${6 * w.hp}`);
  ok(`${dir}: it is as tall as the work hours, with the days above`, Math.abs(w.h - (w.topH + 12 * w.hp + 2)) <= 3 && w.h < 24 * w.hp, `${w.h} vs ${w.topH + 12 * w.hp + 2}`);
  ok(`${dir}: every day heading is in line with its hours column`, w.heads.length === 7 && w.heads.every((h, i) => Math.abs(h[0] - w.lanes[i][0]) <= 1 && Math.abs(h[1] - w.lanes[i][1]) <= 1), JSON.stringify([w.heads, w.lanes]));
}

const en = await E(`(() => {
  const box = ${box('m-week-en')}, tg = box.querySelector('[data-ins-month-hours]');
  return { toggle: tg.textContent, title: tg.title, prev: box.querySelector('[data-ins-month-go="prev"]').getAttribute('aria-label'), views: [...box.querySelectorAll('[data-ins-month-view]')].map((v) => v.textContent).join(), allDay: box.querySelector('.ins-month-allday-label').textContent };
})()`);
ok('in English the words and the toggle tooltip are English', en.toggle === 'Work hours' && en.title === 'Show only the work hours, 6:00 AM to 6:00 PM' && en.prev === 'Previous week' && en.views === 'Month,Week,Day' && en.allDay === 'All day', JSON.stringify(en));

// ------------------------------------------------------------------ PageUp and PageDown
await E(`${box('m-week')}.scrollIntoView({ block: 'start', behavior: 'instant' }); ${sel('m-week', 'a.ins-month-event')}.focus(); 0`);
await b.key('PageDown');
const next = await E(`${box('m-week')}.getAttribute('data-ins-date')`);
ok('PageDown from a focused chip turns the week on', next === '2026-09-28', next);
ok('and focus, which the turn threw away, goes to the next arrow', (await active()) === 'go:next', await active());
await b.key('PageUp');
ok('PageUp from the arrow turns it back', (await E(`${box('m-week')}.getAttribute('data-ins-date')`)) === '2026-09-21');
ok('and the arrow keeps its focus', (await active()) === 'go:next', await active());
await E(`${sel('m-week', 'a.ins-month-event')}.focus(); 0`);
await b.key('PageUp');
ok('PageUp from a chip turns the week back and focus goes to the previous arrow', (await E(`${box('m-week')}.getAttribute('data-ins-date')`)) === '2026-09-14' && (await active()) === 'go:prev', await active());
await b.key('PageDown');
ok('the date moves on again', (await E(`${box('m-week')}.getAttribute('data-ins-date')`)) === '2026-09-21');
ok('every turn says ins:month-date with the new day', (await E(`window.__ev.filter((e) => e.startsWith('ins:month-date')).map((e) => /"date":"([^"]+)"/.exec(e)[1]).join()`)) === '2026-09-28,2026-09-21,2026-09-14,2026-09-21');

// ------------------------------------------------------------------ a range out of order
const range = await E(`(() => { const hs = [...${box('m-range')}.querySelectorAll('.ins-month-hour')]; return { n: hs.length, text: hs.map((h) => h.textContent).join(), toggle: !!${sel('m-range', '[data-ins-month-hours]')} }; })()`);
ok('20-18 shows the one hour from 8:00 م', range.n === 1 && range.text === '8:00 م', JSON.stringify(range));

// ------------------------------------------------------------------ the views
await E(`window.__ev.length = 0; window.__seg = 0; document.addEventListener('ins:seg', () => { window.__seg++; }); 0`);
await b.click('#m-week [data-ins-month-view="day"]');
const toDay = await E(`({ view: ${box('m-week')}.getAttribute('data-ins-view'), days: ${box('m-week')}.querySelectorAll('.ins-month-lane').length, pressed: ${box('m-week')}.querySelector('[data-ins-month-view="day"]').getAttribute('aria-pressed') })`);
ok('the view switch moves the week to a day, with its event', toDay.view === 'day' && toDay.days === 1 && toDay.pressed === 'true' && (await E(`window.__ev.join()`)) === 'ins:month-view {"view":"day"}', JSON.stringify(toDay));
ok('and the pressed view button keeps focus', (await active()) === 'view:day', await active());
ok('the view switch does not fire the segmented control event', (await E(`window.__seg`)) === 0);
await b.click('#m-week [data-ins-month-view="week"]');
await E(`window.__ev.length = 0; 0`);
await b.click('#m-week .ins-month-wday:nth-of-type(3)');
const open = await E(`({ view: ${box('m-week')}.getAttribute('data-ins-view'), date: ${box('m-week')}.getAttribute('data-ins-date'), ev: window.__ev.join() })`);
ok('a day heading in the week view opens that day', open.view === 'day' && open.date === '2026-09-21' && open.ev === 'ins:month-view {"view":"day"}', JSON.stringify(open));
await E(`${box('m-week')}.setAttribute('data-ins-view-switch', 'off'); Insiyab.month('#m-week'); 0`);
ok('data-ins-view-switch="off" removes the view switch', (await E(`${box('m-week')}.querySelectorAll('[data-ins-month-view]').length`)) === 0);
await E(`${box('m-week')}.removeAttribute('data-ins-view-switch'); ${box('m-week')}.setAttribute('data-ins-view', 'week'); Insiyab.month('#m-week'); 0`);

// ------------------------------------------------------------------ a press on the hours
await E(`${box('m-week')}.scrollIntoView({ block: 'start', behavior: 'instant' }); 0`);
await b.sleep(150);
const spot = await E(`(() => { const lane = ${sel('m-week', '.ins-month-lane')}, r = lane.getBoundingClientRect(), hp = r.height / 24; return { x: r.left + r.width / 2, y: r.top + 6.6 * hp }; })()`);
await E(`window.__ev.length = 0; 0`);
await b.mouse(spot.x, spot.y);
ok('a press on the hours away from events says the day and the half hour', (await E(`window.__ev.join()`)) === 'ins:month-day {"date":"2026-09-19","time":"06:30"}', await E(`window.__ev.join()`));

// ------------------------------------------------------------------ the now line
await E(`Insiyab.month.now = () => new Date(2026, 8, 15, 3, 0); ${box('m-week')}.setAttribute('data-ins-work-hours', 'on'); ${box('m-week')}.setAttribute('data-ins-date', '2026-09-15'); ${box('m-week')}.setAttribute('data-ins-view', 'week'); Insiyab.month('#m-week'); 0`);
ok('no now line in the work hours at 3:00', (await E(`${box('m-week')}.querySelectorAll('.ins-month-now').length`)) === 0);
await E(`${box('m-week')}.setAttribute('data-ins-work-hours', 'off'); Insiyab.month('#m-week'); 0`);
const now = await E(`(() => { const n = ${box('m-week')}.querySelectorAll('.ins-month-now'); return { n: n.length, at: n[0] && n[0].style.getPropertyValue('--ins-at'), today: n[0] && n[0].parentNode.classList.contains('is-today') }; })()`);
ok('with the whole day there is one, at 3 hours, in today\'s column', now.n === 1 && Number(now.at) === 3 && now.today, JSON.stringify(now));

ok('no console errors, warnings or exceptions', b.logs.length === 0, b.logs.join(' | '));
await b.close();
end();
