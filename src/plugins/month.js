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
   ========================================================================== */

(function (window, document) {
  'use strict';

  var Insiyab = window.Insiyab;
  if (!Insiyab || !Insiyab.define) {
    if (window.console) window.console.warn('[insiyab-month] load insiyab.js before this plugin.');
    return;
  }

  var SHOWN = 3;
  var WORDS = {
    ar: { prev: 'الشهر السابق', next: 'الشهر التالي', today: 'اليوم', more: '+{n}', events: '{n} مواعيد' },
    en: { prev: 'Previous month', next: 'Next month', today: 'Today', more: '+{n}', events: '{n} events' }
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
    var now = new Date();
    return m ? new Date(+m[1], +m[2] - 1, 1) : new Date(now.getFullYear(), now.getMonth(), 1);
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
        tone: li.getAttribute('data-tone'), time: li.getAttribute('data-time')
      });
    }
    return out;
  }

  function sorted(list) {
    return list.slice().sort(function (a, b) { return (a.time || '').localeCompare(b.time || ''); });
  }

  function chip(e, node) {
    var c = el(e.href ? 'a' : 'span', 'ins-month-event' + (e.tone ? ' ins-month-event--' + e.tone : ''));
    if (e.href) c.href = e.href;
    if (e.time) {
      var p = e.time.split(':');
      c.appendChild(el('span', 'ins-month-time', fmt(node, { hour: 'numeric', minute: '2-digit' }).format(new Date(2000, 0, 1, +p[0], +p[1] || 0))));
    }
    c.appendChild(el('span', 'ins-month-title', e.title));
    c.title = e.title;
    return c;
  }

  function draw(box) {
    var month = monthOf(box.getAttribute('data-ins-month'));
    var w = words(box), start = weekStart(box), byDay = events(box);
    var view = box.querySelector('.ins-month-view');
    if (!view) { view = el('div', 'ins-month-view'); box.appendChild(view); }
    view.textContent = '';

    var head = el('div', 'ins-month-head');
    var title = el('h3', 'ins-month-title-main', fmt(box, { month: 'long', year: 'numeric' }).format(month));
    title.setAttribute('aria-live', 'polite');
    var nav = el('div', 'ins-month-nav');
    [['prev', w.prev], ['today', w.today], ['next', w.next]].forEach(function (b) {
      var btn = el('button', 'ins-btn ins-btn--sm ' + (b[0] === 'today' ? 'ins-btn--secondary' : 'ins-btn--bare ins-btn--icon ins-month-' + b[0]), b[0] === 'today' ? b[1] : '');
      btn.type = 'button';
      btn.setAttribute('data-ins-month-go', b[0]);
      if (b[0] !== 'today') {
        btn.setAttribute('aria-label', b[1]);
        /* On a span of its own: a button's ::before is its press effect. */
        btn.appendChild(el('span', 'ins-month-arrow')).setAttribute('aria-hidden', 'true');
      }
      nav.appendChild(btn);
    });
    head.appendChild(title);
    head.appendChild(nav);
    view.appendChild(head);

    var table = el('table', 'ins-month-grid');
    table.setAttribute('aria-labelledby', title.id || (title.id = 'ins-month-' + Math.random().toString(36).slice(2, 8)));
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
    var today = iso(new Date()), full = fmt(box, { day: 'numeric', month: 'long' });
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
        var num = el('span', 'ins-month-num', String(cur.getDate()));
        num.setAttribute('aria-label', full.format(cur) + (list.length ? '، ' + w.events.replace('{n}', list.length) : ''));
        td.appendChild(num);
        if (list.length) {
          var wrap = el('div', 'ins-month-events-day');
          list.slice(0, list.length > SHOWN ? SHOWN - 1 : SHOWN).forEach(function (e) { wrap.appendChild(chip(e, box)); });
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
    view.appendChild(table);
  }

  function go(box, how) {
    var m = monthOf(box.getAttribute('data-ins-month'));
    var now = new Date();
    var next = how === 'today' ? new Date(now.getFullYear(), now.getMonth(), 1)
      : new Date(m.getFullYear(), m.getMonth() + (how === 'next' ? 1 : -1), 1);
    var value = next.getFullYear() + '-' + pad(next.getMonth() + 1);
    box.setAttribute('data-ins-month', value);
    draw(box);
    fire('ins:month', { el: box, month: value });
  }

  function fire(name, detail) {
    var ev;
    try { ev = new CustomEvent(name, { detail: detail, bubbles: true }); }
    catch (e) { ev = document.createEvent('CustomEvent'); ev.initCustomEvent(name, true, false, detail); }
    document.dispatchEvent(ev);
  }

  document.addEventListener('click', function (event) {
    var t = event.target;
    var btn = t.closest ? t.closest('[data-ins-month-go]') : null;
    var box = t.closest ? t.closest('.ins-month') : null;
    if (!box) return;
    if (btn) { go(box, btn.getAttribute('data-ins-month-go')); return; }
    if (t.closest('.ins-month-event, a')) return;
    var day = t.closest('.ins-month-day');
    if (day) fire('ins:month-day', { el: box, date: day.getAttribute('data-date') });
  }, false);

  function scan(scope) {
    var boxes = scope.querySelectorAll('.ins-month');
    for (var i = 0; i < boxes.length; i++) draw(boxes[i]);
  }
  Insiyab.define('month', scan);
  if (document.readyState !== 'loading') scan(document);

  /* Draw again, move to a month ('2026-10'), or hand over the events:
     `{ month, events: [{ date, title, href, tone, time }] }`. */
  Insiyab.month = function (target, value) {
    var box = typeof target === 'string' ? document.querySelector(target) : target;
    if (!box) return null;
    if (typeof value === 'string') box.setAttribute('data-ins-month', value);
    else if (value) {
      if (value.month) box.setAttribute('data-ins-month', value.month);
      if (value.events) {
        var byDay = {};
        value.events.forEach(function (e) { var k = String(e.date).slice(0, 10); (byDay[k] = byDay[k] || []).push(e); });
        box.__insEvents = byDay;
      }
    }
    draw(box);
    return box.getAttribute('data-ins-month');
  };
})(window, document);
