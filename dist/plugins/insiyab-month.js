/*! Insiyab UI v0.6.0 · https://github.com/Mohammad-Diab/insiyab-ui#readme · Apache-2.0 (fonts: OFL 1.1, see fonts/LICENSE-*.txt) */
/* ==========================================================================
   Insiyab · Month plugin — a month calendar with events
   ==========================================================================

   A month on a grid, with what happens on each day:

     <div class="ins-month" data-ins-month="2026-09">
       <ul class="ins-month-events">
         <li data-date="2026-09-03" data-tone="warn"><a href="/orders/1042">تسليم الطلب</a></li>
         <li data-date="2026-09-03" data-time="14:30">اجتماع</li>
         …
       </ul>
     </div>

   The list is the calendar's content and stays the source: each item a day's event
   (`data-date`, and optionally `data-time` and `data-tone`: ok, warn, bad, info),
   its text or link what the day shows. The plugin draws the month from it as a
   table of weeks, the week starting on the day the page's language starts it, the
   month's name and the days' in that language with Latin digits, today marked. A
   day shows three events and a "+2" for the rest. A link stays a link.

   The header moves a month back or on, or to today; each move says so with
   `ins:month` ({ el, month: '2026-10' }), so a page can put that month's events in
   the list and call `Insiyab.month(el)` to draw it again, or hand them straight
   over: `Insiyab.month(el, { month: '2026-10', events: [{ date, title, href, tone,
   time }] })`. A click on a day that is not on an event says `ins:month-day`
   ({ el, date: '2026-09-14' }). Below 34rem a day shows dots for its events
   instead of their names.

   `data-ins-view="week"` or `"day"` draws that view by the hour, for the day in
   `data-ins-date`, each event as tall as its `data-duration` (minutes). Only the
   work hours show, 6 to 18 or `data-ins-day-hours="8-17"`: an event wholly outside
   them is counted in a band above or below, which opens the whole day at it when
   pressed, and `data-ins-work-hours="off"` shows the whole day in a scroll box as
   tall as the work hours. A switch in the header moves between the views, and
   another between the work hours and the whole day. A move says `ins:month-date`,
   a view `ins:month-view`, and a press on the hours `ins:month-day` with its `time`.
   ========================================================================== */

(function (window, document) {
  'use strict';

  var Insiyab = window.Insiyab;
  if (!Insiyab || !Insiyab.define) {
    if (window.console) window.console.warn('[insiyab-month] load insiyab.js before this plugin.');
    return;
  }

  var SHOWN = 3;
  var SHORTEST = 22;
  var WORDS = {
    ar: { prev: 'الشهر السابق', next: 'الشهر التالي', today: 'اليوم', more: '+{n}', sep: '، ',
      prevWeek: 'الأسبوع السابق', nextWeek: 'الأسبوع التالي', prevDay: 'اليوم السابق', nextDay: 'اليوم التالي',
      views: ['شهر', 'أسبوع', 'يوم'], viewLabel: 'طريقة العرض', hours: 'ساعات الدوام', allDay: 'طوال اليوم',
      before: 'قبل', after: 'بعد', only: 'اعرض ساعات الدوام وحدها، من {a} إلى {b}',
      events: function (n) { var r = n % 100; return n === 1 ? 'موعد واحد' : n === 2 ? 'موعدان' : n + (r >= 3 && r <= 10 ? ' مواعيد' : r >= 11 ? ' موعدًا' : ' موعد'); } },
    en: { prev: 'Previous month', next: 'Next month', today: 'Today', more: '+{n}', sep: ', ',
      prevWeek: 'Previous week', nextWeek: 'Next week', prevDay: 'Previous day', nextDay: 'Next day',
      views: ['Month', 'Week', 'Day'], viewLabel: 'View', hours: 'Work hours', allDay: 'All day',
      before: 'Before', after: 'After', only: 'Show only the work hours, {a} to {b}',
      events: function (n) { return n === 1 ? '1 event' : n + ' events'; } }
  };

  function el(tag, cls, text) {
    var n = document.createElement(tag);
    if (cls) n.className = cls;
    if (text !== undefined) n.textContent = text;
    return n;
  }

  function lang(node) {
    var host = node.closest('[lang]') || document.documentElement;
    return host.getAttribute('lang') || 'en';
  }

  function words(node) { return WORDS[lang(node).slice(0, 2).toLowerCase()] || WORDS.en; }

  function fmt(node, options) {
    var locale = lang(node);
    var tag = locale.indexOf('-u-') === -1 ? locale + '-u-ca-gregory-nu-latn' : locale;
    try { return new Intl.DateTimeFormat(tag, options); } catch (e) { return new Intl.DateTimeFormat('en', options); }
  }

  function pad(n) { return (n < 10 ? '0' : '') + n; }
  function iso(d) { return d.getFullYear() + '-' + pad(d.getMonth() + 1) + '-' + pad(d.getDate()); }
  function monthValue(d) { return d.getFullYear() + '-' + pad(d.getMonth() + 1); }
  function addDays(d, n) { return new Date(d.getFullYear(), d.getMonth(), d.getDate() + n); }
  function dayOf(d) { return new Date(d.getFullYear(), d.getMonth(), d.getDate()); }

  function parseDay(value) {
    var m = /^(\d{4})-(\d{2})-(\d{2})/.exec(value || '');
    return m ? new Date(+m[1], +m[2] - 1, +m[3]) : null;
  }

  /* The clock the plugin reads, so a test can hold it still. */
  function nowDate() {
    var n = api.now();
    return n instanceof Date ? n : new Date();
  }

  /* The first day of the week in the page's language: the engine's answer when it
     has one, else Saturday for Arabic and Persian, Sunday for the United States,
     Monday elsewhere. As a JavaScript day number. */
  function weekStart(node) {
    var locale = lang(node);
    try {
      var info = new Intl.Locale(locale);
      var w = info.getWeekInfo ? info.getWeekInfo() : info.weekInfo;
      if (w && w.firstDay) return w.firstDay % 7;
    } catch (e) { /* fall through */ }
    if (/^(ar|fa)\b/i.test(locale)) return 6;
    if (/^en-US\b|^en$/i.test(locale)) return 0;
    return 1;
  }

  function monthOf(value) {
    var m = /^(\d{4})-(\d{2})/.exec(value || '');
    var now = nowDate();
    return m ? new Date(+m[1], +m[2] - 1, 1) : new Date(now.getFullYear(), now.getMonth(), 1);
  }

  function viewOf(box) {
    var v = box.getAttribute('data-ins-view');
    return v === 'week' || v === 'day' ? v : 'month';
  }

  /* The day the week and day views show: the one written, else today when it is in the month, else the month's first. */
  function dateOf(box) {
    var d = parseDay(box.getAttribute('data-ins-date'));
    if (d) return d;
    var month = monthOf(box.getAttribute('data-ins-month')), now = nowDate();
    return now.getFullYear() === month.getFullYear() && now.getMonth() === month.getMonth() ? dayOf(now) : month;
  }

  /* The work hours, 6-18 unless written, kept in range: the start within 0 to 23, the end after it up to 24. */
  function hoursOf(box) {
    var m = /^(\d{1,2})\s*-\s*(\d{1,2})$/.exec((box.getAttribute('data-ins-day-hours') || '').trim());
    var s = Math.min(Math.max(m ? +m[1] : 6, 0), 23);
    var e = Math.min(Math.max(m ? +m[2] : 18, s + 1), 24);
    return { start: s, end: e, trims: s > 0 || e < 24 };
  }

  function minutes(time) {
    var m = /^(\d{1,2})(?::(\d{2}))?/.exec(time || '');
    return m ? +m[1] * 60 + (+m[2] || 0) : null;
  }

  function length(e) {
    var d = parseFloat(e.duration);
    return d > 0 ? d : 60;
  }

  /* The events, read from the list (or taken from script), grouped by day. */
  function events(box) {
    if (box.__insEvents) return box.__insEvents;
    var out = {}, items = box.querySelectorAll('.ins-month-events > li[data-date]');
    for (var i = 0; i < items.length; i++) {
      var li = items[i], day = li.getAttribute('data-date').slice(0, 10);
      var link = li.querySelector('a[href]');
      (out[day] = out[day] || []).push({
        title: li.textContent.trim(), href: link ? link.getAttribute('href') : null,
        tone: li.getAttribute('data-tone'), time: li.getAttribute('data-time'), duration: li.getAttribute('data-duration')
      });
    }
    return out;
  }

  function sorted(list) {
    function at(e) { var m = minutes(e.time); return m === null ? -1 : m; }
    return list.slice().sort(function (a, b) { return at(a) - at(b); });
  }

  /* A time of day, in minutes, written in the page's language. */
  function timeFn(node) {
    var f = fmt(node, { hour: 'numeric', minute: '2-digit' });
    return function (mins) { return f.format(new Date(2000, 0, 1, Math.floor(mins / 60), mins % 60)).replace(/[\u202f\u00a0]/g, ' '); };
  }

  function shell(e, extra) {
    var c = el(e.href ? 'a' : 'span', 'ins-month-event' + extra + (e.tone ? ' ins-month-event--' + e.tone : ''));
    if (e.href) c.href = e.href;
    c.title = e.title;
    return c;
  }

  function chip(e, tf) {
    var c = shell(e, '');
    var at = minutes(e.time);
    if (at !== null) c.appendChild(el('span', 'ins-month-time', tf(at)));
    c.appendChild(el('span', 'ins-month-title', e.title));
    return c;
  }

  function timedChip(e, tf) {
    var c = shell(e, ' is-timed');
    var at = minutes(e.time);
    c.appendChild(el('span', 'ins-month-title', e.title));
    c.appendChild(el('span', 'ins-month-when', tf(at) + ' – ' + tf(at + length(e))));
    return c;
  }

  /* Lays overlapping events side by side: each one's lane, and the lanes in its group of overlaps. */
  function lanesOf(spans) {
    var result = spans.map(function () { return { lane: 0, lanes: 1 }; });
    var order = spans.map(function (s, i) { return i; }).sort(function (a, b) { return spans[a][0] - spans[b][0] || spans[b][1] - spans[a][1]; });
    var ends = [], group = [], groupEnd = -Infinity;
    function close() {
      group.forEach(function (g) { result[g].lanes = ends.length; });
      group = [];
      ends = [];
      groupEnd = -Infinity;
    }
    order.forEach(function (i) {
      var s = spans[i][0], e = spans[i][1], lane = -1;
      if (group.length && s >= groupEnd) close();
      for (var k = 0; k < ends.length; k++) if (ends[k] <= s) { lane = k; break; }
      if (lane < 0) { lane = ends.length; ends.push(e); } else ends[lane] = e;
      result[i].lane = lane;
      group.push(i);
      groupEnd = Math.max(groupEnd, e);
    });
    close();
    return result;
  }

  /* An event's minutes as drawn: inside the shown hours, and never shorter than a chip that can hold its title. */
  function visible(e, from, to, shortest) {
    var start = minutes(e.time), end = Math.min(1440, start + length(e));
    start = Math.max(from, Math.min(start, to - shortest));
    end = Math.max(Math.min(end, to), start + shortest);
    return [start, end];
  }

  function num(n) { return String(Math.round(n * 1000) / 1000); }

  /* The header control that holds focus, as a selector, so the redraw can give it back. */
  function holder(box) {
    var a = document.activeElement;
    var c = a && box.contains(a) && a.closest ? a.closest('[data-ins-month-go], [data-ins-month-view], [data-ins-month-hours]') : null;
    if (!c) return null;
    if (c.hasAttribute('data-ins-month-go')) return '[data-ins-month-go="' + c.getAttribute('data-ins-month-go') + '"]';
    if (c.hasAttribute('data-ins-month-view')) return '[data-ins-month-view="' + c.getAttribute('data-ins-month-view') + '"]';
    return '[data-ins-month-hours]';
  }

  function refocus(box, selector) {
    var n = selector ? box.querySelector(selector) : null;
    if (n) n.focus({ preventScroll: true });
  }

  function head(box, view, w, text, id, tf) {
    var hrs = hoursOf(box);
    var node = el('div', 'ins-month-head');
    var title = el('h3', 'ins-month-title-main', text);
    title.id = id;
    title.setAttribute('aria-live', 'polite');
    var nav = el('div', 'ins-month-nav');
    if (view !== 'month' && box.getAttribute('data-ins-hours-switch') !== 'off' && hrs.trims) {
      var on = box.getAttribute('data-ins-work-hours') !== 'off';
      var tg = el('button', 'ins-btn ins-btn--sm ins-btn--secondary ins-month-hours-toggle', w.hours);
      tg.type = 'button';
      tg.setAttribute('data-ins-month-hours', '');
      tg.setAttribute('aria-pressed', String(on));
      tg.title = w.only.replace('{a}', tf(hrs.start * 60)).replace('{b}', tf(hrs.end * 60));
      nav.appendChild(tg);
    }
    if (box.getAttribute('data-ins-view-switch') !== 'off') {
      var seg = el('div', 'ins-seg ins-month-views');
      seg.setAttribute('role', 'group');
      seg.setAttribute('aria-label', w.viewLabel);
      ['month', 'week', 'day'].forEach(function (v, i) {
        var b = el('button', v === view ? 'is-active' : '', w.views[i]);
        b.type = 'button';
        b.setAttribute('data-ins-month-view', v);
        b.setAttribute('aria-pressed', String(v === view));
        seg.appendChild(b);
      });
      nav.appendChild(seg);
    }
    var names = view === 'week' ? [w.prevWeek, w.nextWeek] : view === 'day' ? [w.prevDay, w.nextDay] : [w.prev, w.next];
    var arrows = nav.appendChild(el('div', 'ins-month-arrows'));
    [['prev', names[0]], ['today', w.today], ['next', names[1]]].forEach(function (b) {
      var btn = el('button', 'ins-btn ins-btn--sm ' + (b[0] === 'today' ? 'ins-btn--secondary' : 'ins-btn--bare ins-btn--icon ins-month-' + b[0]), b[0] === 'today' ? b[1] : '');
      btn.type = 'button';
      btn.setAttribute('data-ins-month-go', b[0]);
      if (b[0] !== 'today') {
        btn.setAttribute('aria-label', b[1]);
        /* On a span of its own: a button's ::before is its press effect. */
        btn.appendChild(el('span', 'ins-month-arrow')).setAttribute('aria-hidden', 'true');
      }
      arrows.appendChild(btn);
    });
    node.appendChild(title);
    node.appendChild(nav);
    return node;
  }

  function monthGrid(box, parent, month, w, tf, id, today) {
    var start = weekStart(box), byDay = events(box);
    var table = el('table', 'ins-month-grid');
    table.setAttribute('aria-labelledby', id);
    var thead = table.appendChild(el('thead')).appendChild(el('tr'));
    var dayName = fmt(box, { weekday: 'short' }), dayLong = fmt(box, { weekday: 'long' });
    for (var d = 0; d < 7; d++) {
      var wd = new Date(2026, 1, 1 + ((start + d) % 7));   /* 1 Feb 2026 was a Sunday */
      var th = el('th', '', dayName.format(wd));
      th.setAttribute('abbr', dayLong.format(wd));
      th.scope = 'col';
      thead.appendChild(th);
    }
    var body = table.appendChild(el('tbody'));
    var first = new Date(month.getFullYear(), month.getMonth(), 1);
    var lead = (first.getDay() - start + 7) % 7;
    var cur = new Date(first.getFullYear(), first.getMonth(), 1 - lead);
    var full = fmt(box, { day: 'numeric', month: 'long' });
    for (var week = 0; week < 6; week++) {
      /* Five weeks hold most months; the sixth only when the month reaches it. */
      if (week === 5 && cur.getMonth() !== month.getMonth()) break;
      var tr = body.appendChild(el('tr'));
      for (var k = 0; k < 7; k++) {
        var key = iso(cur), list = sorted(byDay[key] || []);
        var td = el('td', 'ins-month-day');
        td.setAttribute('data-date', key);
        if (cur.getMonth() !== month.getMonth()) td.classList.add('is-other');
        if (key === today) { td.classList.add('is-today'); td.setAttribute('aria-current', 'date'); }
        var number = el('span', 'ins-month-num', String(cur.getDate()));
        number.setAttribute('aria-label', full.format(cur) + (list.length ? '، ' + w.events(list.length) : ''));
        td.appendChild(number);
        if (list.length) {
          var wrap = el('div', 'ins-month-events-day');
          list.slice(0, list.length > SHOWN ? SHOWN - 1 : SHOWN).forEach(function (e) { wrap.appendChild(chip(e, tf)); });
          if (list.length > SHOWN) {
            var more = el('span', 'ins-month-more', w.more.replace('{n}', list.length - SHOWN + 1));
            more.title = list.slice(SHOWN - 1).map(function (e) { return e.title; }).join('، ');
            wrap.appendChild(more);
          }
          var dots = el('span', 'ins-month-dots');
          dots.setAttribute('aria-hidden', 'true');
          list.slice(0, 4).forEach(function (e) { dots.appendChild(el('i', e.tone ? 'ins-month-dot--' + e.tone : '')); });
          td.appendChild(wrap);
          td.appendChild(dots);
        }
        tr.appendChild(td);
        cur = new Date(cur.getFullYear(), cur.getMonth(), cur.getDate() + 1);
      }
    }
    parent.appendChild(table);
  }

  /* The row above or below the hours: a label, and in each day with hidden events a pill counting them. */
  function bandRow(kind, lists, first, hour, w, tf) {
    var at = tf(hour * 60);
    var row = el('div', 'ins-month-band ins-month-band--' + kind);
    row.appendChild(el('div', 'ins-month-band-label', (kind === 'after' ? w.after : w.before) + ' ' + at));
    lists.forEach(function (hidden, d) {
      var cell = el('div', 'ins-month-band-cell');
      if (hidden.length) {
        var btn = el('button', 'ins-month-band-btn', w.more.replace('{n}', hidden.length));
        btn.type = 'button';
        btn.setAttribute('data-ins-month-band', kind);
        btn.setAttribute('data-date', iso(addDays(first, d)));
        btn.setAttribute('aria-label', w.events(hidden.length) + ' ' + (kind === 'after' ? w.after : w.before) + ' ' + at);
        btn.title = hidden.map(function (e) { return tf(minutes(e.time)) + ' ' + e.title; }).join(w.sep);
        btn.__insFirst = Math.min.apply(null, hidden.map(function (e) { return minutes(e.time); }));
        cell.appendChild(btn);
      }
      row.appendChild(cell);
    });
    return row;
  }

  /* The week or the day by the hour: day headings, an all-day row, then the hours with each day's events placed by time and length. */
  function weekGrid(box, parent, view, first, w, tf, id, today) {
    var days = view === 'day' ? 1 : 7, byDay = events(box), hrs = hoursOf(box);
    var all = box.getAttribute('data-ins-work-hours') === 'off';
    var from = all || !hrs.trims ? 0 : hrs.start, to = all || !hrs.trims ? 24 : hrs.end;
    var scrolls = from !== hrs.start || to !== hrs.end;
    var fromM = from * 60, toM = to * 60;
    var wk = el('div', 'ins-month-week' + (scrolls ? ' is-all' : ''));
    wk.style.setProperty('--ins-month-days', String(days));
    wk.setAttribute('role', 'group');
    wk.setAttribute('aria-labelledby', id);
    var top = el('div', 'ins-month-week-top');
    top.appendChild(el('div', 'ins-month-corner'));
    var dayName = fmt(box, { weekday: 'short' }), dayFull = fmt(box, { weekday: 'long', day: 'numeric', month: 'long', year: 'numeric' });
    var cells = [], plan = [], before = [], after = [];
    var hours = el('div', 'ins-month-hours');
    hours.style.setProperty('--ins-month-span', String(to - from));
    hours.__insFrom = fromM;
    hours.__insTo = toM;
    var gutter = hours.appendChild(el('div', 'ins-month-gutter'));
    for (var h = from; h < to; h++) gutter.appendChild(el('span', 'ins-month-hour', tf(h * 60)));

    for (var d = 0; d < days; d++) {
      var day = addDays(first, d), key = iso(day), list = sorted(byDay[key] || []), isToday = key === today;
      var heading = el(view === 'week' ? 'button' : 'div', 'ins-month-wday' + (isToday ? ' is-today' : ''));
      if (view === 'week') {
        heading.type = 'button';
        heading.setAttribute('data-ins-month-open', key);
        heading.setAttribute('aria-label', dayFull.format(day));
      }
      if (isToday) heading.setAttribute('aria-current', 'date');
      heading.appendChild(el('span', '', dayName.format(day)));
      heading.appendChild(el('span', 'ins-month-num', String(day.getDate())));
      top.appendChild(heading);

      var cell = el('div', 'ins-month-allday');
      cell.setAttribute('data-date', key);
      var timed = [], hideBefore = [], hideAfter = [], inside = [];
      list.forEach(function (e) {
        if (minutes(e.time) === null) cell.appendChild(chip(e, tf));
        else timed.push(e);
      });
      cells.push(cell);
      timed.forEach(function (e) {
        var s = minutes(e.time);
        if (s + length(e) <= fromM) hideBefore.push(e);
        else if (s >= toM) hideAfter.push(e);
        else inside.push(e);
      });
      before.push(hideBefore);
      after.push(hideAfter);
      var lane = el('div', 'ins-month-lane' + (isToday ? ' is-today' : ''));
      lane.setAttribute('data-date', key);
      hours.appendChild(lane);
      plan.push({ lane: lane, inside: inside, today: isToday });
    }
    top.appendChild(el('div', 'ins-month-allday-label', w.allDay));
    cells.forEach(function (c) { top.appendChild(c); });
    wk.appendChild(top);
    var anyBefore = before.some(function (l) { return l.length; }), anyAfter = after.some(function (l) { return l.length; });
    if (anyBefore) wk.appendChild(bandRow('before', before, first, from, w, tf));
    wk.appendChild(hours);
    if (anyAfter) wk.appendChild(bandRow('after', after, first, to, w, tf));
    parent.appendChild(wk);

    /* The hour's height is the stylesheet's, so it is measured once the hours are on the page. */
    var hp = plan[0].lane.offsetHeight / (to - from) || 48;
    var line = null, lineLane = null, shortest = SHORTEST / hp * 60;
    plan.forEach(function (p) {
      var spans = p.inside.map(function (e) { return visible(e, fromM, toM, shortest); });
      var lanes = lanesOf(spans);
      p.inside.forEach(function (e, i) {
        var c = timedChip(e, tf);
        if (minutes(e.time) < fromM) c.classList.add('is-cut-start');
        if (minutes(e.time) + length(e) > toM) c.classList.add('is-cut-end');
        c.style.setProperty('--ins-at', num((spans[i][0] - fromM) / 60));
        c.style.setProperty('--ins-len', num((spans[i][1] - spans[i][0]) / 60));
        c.style.setProperty('--ins-lane', String(lanes[i].lane));
        c.style.setProperty('--ins-lanes', String(lanes[i].lanes));
        p.lane.appendChild(c);
      });
      if (p.today) {
        lineLane = p.lane;
        var n = nowDate(), at = n.getHours() * 60 + n.getMinutes();
        if (at >= fromM && at <= toM) {
          line = el('div', 'ins-month-now');
          line.style.setProperty('--ins-at', num((at - fromM) / 60));
          line.setAttribute('aria-hidden', 'true');
          p.lane.appendChild(line);
        }
      }
    });

    if (scrolls) {
      var border = wk.offsetHeight - wk.clientHeight;
      wk.style.setProperty('--ins-month-view-h', (top.offsetHeight + Math.min(Math.max(hrs.end - hrs.start, 4), 24) * hp + border) + 'px');
      var spot = iso(first) + view, saved = box.__insScroll;
      var y = box.__insScrollTo != null ? Math.max(0, box.__insScrollTo / 60 * hp - hp / 2) : saved && saved.key === spot ? saved.y : hrs.start * hp;
      box.__insScrollTo = null;
      box.__insScroll = { key: spot, y: y };
      wk.scrollTo({ top: y, behavior: 'instant' });
      wk.addEventListener('scroll', function () { box.__insScroll = { key: spot, y: wk.scrollTop }; });
    } else {
      box.__insScroll = null;
      box.__insScrollTo = null;
    }

    /* Every half minute the now line moves on; a new day, or the line entering or leaving the shown hours, draws the view again. */
    box.__insClock = setInterval(function () {
      if (!box.isConnected) { clearInterval(box.__insClock); return; }
      var n = nowDate(), at = n.getHours() * 60 + n.getMinutes(), wants = at >= fromM && at <= toM;
      if (iso(n) !== today || (lineLane && !!line !== wants)) { refresh(box); return; }
      if (line) line.style.setProperty('--ins-at', num((at - fromM) / 60));
    }, 30000);
  }

  function draw(box) {
    var view = viewOf(box), w = words(box), start = weekStart(box), tf = timeFn(box);
    var now = nowDate(), today = iso(now), first = null;
    if (box.__insClock) { clearInterval(box.__insClock); box.__insClock = 0; }
    if (view !== 'month') {
      var date = dateOf(box);
      if (box.getAttribute('data-ins-month') !== monthValue(date)) box.setAttribute('data-ins-month', monthValue(date));
      first = view === 'day' ? date : addDays(date, -((date.getDay() - start + 7) % 7));
    }
    var month = monthOf(box.getAttribute('data-ins-month'));
    var old = box.querySelector('.ins-month-view');
    var page = el('div', 'ins-month-view');
    box.appendChild(page);

    var id = 'ins-month-' + Math.random().toString(36).slice(2, 8);
    var text;
    if (view === 'week') {
      var f = fmt(box, { day: 'numeric', month: 'long', year: 'numeric' }), last = addDays(first, 6);
      text = f.formatRange ? f.formatRange(first, last) : f.format(first) + ' – ' + f.format(last);
    } else if (view === 'day') {
      text = fmt(box, { weekday: 'long', day: 'numeric', month: 'long', year: 'numeric' }).format(first);
    } else {
      text = fmt(box, { month: 'long', year: 'numeric' }).format(month);
    }
    page.appendChild(head(box, view, w, text, id, tf));
    if (view === 'month') monthGrid(box, page, month, w, tf, id, today);
    else weekGrid(box, page, view, first, w, tf, id, today);
    if (old) old.parentNode.removeChild(old);
  }

  /* Draws again with focus kept where it was: the header's controls are made new each time. */
  function refresh(box) {
    var held = holder(box);
    draw(box);
    refocus(box, held);
  }

  function go(box, how) {
    var view = viewOf(box), now = nowDate();
    if (view === 'month') {
      var m = monthOf(box.getAttribute('data-ins-month'));
      var next = how === 'today' ? new Date(now.getFullYear(), now.getMonth(), 1)
        : new Date(m.getFullYear(), m.getMonth() + (how === 'next' ? 1 : -1), 1);
      var value = monthValue(next);
      box.setAttribute('data-ins-month', value);
      if (box.hasAttribute('data-ins-date')) box.setAttribute('data-ins-date', iso(how === 'today' ? now : next));
      draw(box);
      fire('ins:month', { el: box, month: value });
      return;
    }
    var day = how === 'today' ? dayOf(now) : addDays(dateOf(box), (how === 'next' ? 1 : -1) * (view === 'week' ? 7 : 1));
    box.setAttribute('data-ins-date', iso(day));
    box.setAttribute('data-ins-month', monthValue(day));
    draw(box);
    fire('ins:month-date', { el: box, date: iso(day) });
  }

  function setView(box, view, day) {
    if (day) {
      box.setAttribute('data-ins-date', iso(day));
      box.setAttribute('data-ins-month', monthValue(day));
    }
    box.setAttribute('data-ins-view', view);
    draw(box);
    fire('ins:month-view', { el: box, view: view });
  }

  /* Work hours only, or the whole day; the band that asks for the whole day says the minute to scroll to. */
  function setHours(box, workOnly, scrollTo) {
    if (workOnly) box.removeAttribute('data-ins-work-hours');
    else box.setAttribute('data-ins-work-hours', 'off');
    box.__insScrollTo = scrollTo == null ? null : scrollTo;
    draw(box);
  }

  function fire(name, detail) {
    var ev;
    try { ev = new CustomEvent(name, { detail: detail, bubbles: true }); }
    catch (e) { ev = document.createEvent('CustomEvent'); ev.initCustomEvent(name, true, false, detail); }
    document.dispatchEvent(ev);
  }

  /* The view switch is drawn as a segmented control, and the page's own handler for those has nothing to do with it: stopped here, before it. */
  document.addEventListener('click', function (event) {
    var t = event.target;
    var btn = t.closest ? t.closest('[data-ins-month-view]') : null;
    var box = btn ? btn.closest('.ins-month') : null;
    if (!box) return;
    event.stopPropagation();
    var held = holder(box), view = btn.getAttribute('data-ins-month-view');
    if (view !== viewOf(box)) setView(box, view);
    refocus(box, held);
  }, true);

  document.addEventListener('click', function (event) {
    var t = event.target;
    var btn = t.closest ? t.closest('[data-ins-month-go]') : null;
    var box = t.closest ? t.closest('.ins-month') : null;
    if (!box) return;
    var held = holder(box);
    if (btn) { go(box, btn.getAttribute('data-ins-month-go')); refocus(box, held); return; }
    var toggle = t.closest('[data-ins-month-hours]');
    if (toggle) { setHours(box, toggle.getAttribute('aria-pressed') !== 'true'); refocus(box, held); return; }
    var band = t.closest('[data-ins-month-band]');
    if (band) {
      var keyboard = event.detail === 0 && document.activeElement === band;
      setHours(box, false, band.__insFirst);
      if (keyboard) refocus(box, box.querySelector('[data-ins-month-hours]') ? '[data-ins-month-hours]' : '[data-ins-month-go="today"]');
      return;
    }
    var open = t.closest('[data-ins-month-open]');
    if (open) { setView(box, 'day', parseDay(open.getAttribute('data-ins-month-open'))); return; }
    if (t.closest('.ins-month-event, a')) return;
    var lane = t.closest('.ins-month-lane');
    if (lane) {
      var hours = lane.parentNode, rect = lane.getBoundingClientRect();
      var hp = lane.offsetHeight / ((hours.__insTo - hours.__insFrom) / 60);
      var at = Math.min(Math.max(hours.__insFrom + Math.floor((event.clientY - rect.top) / hp * 2) * 30, hours.__insFrom), hours.__insTo - 30);
      fire('ins:month-day', { el: box, date: lane.getAttribute('data-date'), time: pad(Math.floor(at / 60)) + ':' + pad(at % 60) });
      return;
    }
    var day = t.closest('.ins-month-day');
    if (day) fire('ins:month-day', { el: box, date: day.getAttribute('data-date') });
  }, false);

  /* PageUp and PageDown anywhere inside turn the month, week or day; the header control that held focus gets it back, and focus on something the turn threw away goes to the matching arrow. */
  document.addEventListener('keydown', function (event) {
    if (event.defaultPrevented || event.altKey || event.ctrlKey || event.metaKey || event.shiftKey) return;
    if (event.key !== 'PageUp' && event.key !== 'PageDown') return;
    var t = event.target;
    var box = t && t.closest ? t.closest('.ins-month') : null;
    if (!box) return;
    event.preventDefault();
    var how = event.key === 'PageUp' ? 'prev' : 'next', held = holder(box), was = document.activeElement;
    go(box, how);
    if (held) refocus(box, held);
    else if (was && !was.isConnected) refocus(box, '[data-ins-month-go="' + how + '"]');
  }, false);

  function scan(scope) {
    var boxes = scope.querySelectorAll('.ins-month');
    for (var i = 0; i < boxes.length; i++) draw(boxes[i]);
  }

  /* Draw again, move to a month ('2026-10'), or hand over { month, view, date, events: [{ date, title, href, tone, time, duration }] }. */
  function api(target, value) {
    var box = typeof target === 'string' ? document.querySelector(target) : target;
    if (!box) return null;
    if (typeof value === 'string') box.setAttribute('data-ins-month', value);
    else if (value) {
      if (value.view) box.setAttribute('data-ins-view', value.view);
      if (value.month) {
        box.setAttribute('data-ins-month', value.month);
        var d = parseDay(box.getAttribute('data-ins-date')), m = monthOf(value.month);
        if (d && !value.date && (d.getFullYear() !== m.getFullYear() || d.getMonth() !== m.getMonth())) box.setAttribute('data-ins-date', iso(m));
      }
      if (value.date) {
        box.setAttribute('data-ins-date', value.date);
        if (!value.month) box.setAttribute('data-ins-month', String(value.date).slice(0, 7));
      }
      if (value.events) {
        var byDay = {};
        value.events.forEach(function (e) { var k = String(e.date).slice(0, 10); (byDay[k] = byDay[k] || []).push(e); });
        box.__insEvents = byDay;
      }
    }
    draw(box);
    return box.getAttribute('data-ins-month');
  }
  api.now = function () { return new Date(); };
  Insiyab.month = api;

  Insiyab.define('month', scan);
  if (document.readyState !== 'loading') scan(document);
})(window, document);
