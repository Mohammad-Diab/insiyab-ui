/* ==========================================================================
   Insiyab · Timeline plugin — relative times
   ==========================================================================

   The timeline is CSS (insiyab-timeline.css). This part only writes times the way
   people say them, into any `<time datetime="…" data-ins-time>` on the page, in a
   timeline or not:

     <time datetime="2026-09-24T09:15" data-ins-time></time>   → قبل 5 دقائق · أمس

   In the page's language, from the browser's own Intl.RelativeTimeFormat, in the
   Latin digits the library sets every figure in. Past a week the time is written
   as a date instead — "قبل 23 يومًا" is harder to place than the date itself —
   and `data-ins-time="date"` always writes the date. The full date and time is the
   element's title either way, and the text is brought up to date every minute
   while there is any on the page.
   ========================================================================== */

(function (window, document) {
  'use strict';

  var Insiyab = window.Insiyab;
  if (!Insiyab || !Insiyab.define) {
    if (window.console) window.console.warn('[insiyab-timeline] load insiyab.js before this plugin.');
    return;
  }

  var TIME = 'time[data-ins-time]';
  var WEEK = 7 * 86400000;
  var STEPS = [['year', 31536000000], ['month', 2592000000], ['week', 604800000], ['day', 86400000], ['hour', 3600000], ['minute', 60000]];

  function lang(el) {
    var host = el.closest('[lang]') || document.documentElement;
    return host.getAttribute('lang') || 'en';
  }

  var cache = {};
  function fmt(kind, locale, options) {
    var key = kind + '|' + locale + '|' + JSON.stringify(options);
    if (!cache[key]) {
      /* Latin digits, and the Gregorian calendar named: ar-SA would otherwise write
         the date in Umm al-Qura. A page that spells out its own -u- keeps it. */
      var tag = locale.indexOf('-u-') === -1 ? locale + '-u-ca-gregory-nu-latn' : locale;
      try { cache[key] = kind === 'rel' ? new Intl.RelativeTimeFormat(tag, options) : new Intl.DateTimeFormat(tag, options); }
      catch (e) { cache[key] = kind === 'rel' ? null : new Intl.DateTimeFormat(locale, options); }
    }
    return cache[key];
  }

  /* "2026-09-24" is a day, not midnight in UTC: read as local, like the date field. */
  function parse(value) {
    var m = /^(\d{4})-(\d{2})-(\d{2})$/.exec(value || '');
    if (m) return new Date(+m[1], m[2] - 1, +m[3]);
    var d = new Date(value);
    return isNaN(d) ? null : d;
  }

  function render(el, now) {
    var raw = el.getAttribute('datetime');
    var d = parse(raw);
    if (!d) return;
    var locale = lang(el), dayOnly = /^\d{4}-\d{2}-\d{2}$/.test(raw);
    var full = dayOnly ? { dateStyle: 'full' } : { dateStyle: 'full', timeStyle: 'short' };
    el.title = fmt('date', locale, full).format(d);
    var diff = d - now, rel = fmt('rel', locale, { numeric: 'auto' });
    var text;
    if (el.getAttribute('data-ins-time') === 'date' || Math.abs(diff) >= WEEK || !rel) {
      text = fmt('date', locale, { day: 'numeric', month: 'long', year: d.getFullYear() === now.getFullYear() ? undefined : 'numeric' }).format(d);
    } else if (dayOnly) {
      /* A day is compared to today as a day: yesterday, not "20 hours ago". */
      var today = new Date(now.getFullYear(), now.getMonth(), now.getDate());
      text = rel.format(Math.round((d - today) / 86400000), 'day');
    } else if (Math.abs(diff) < 45000) {
      text = rel.format(0, 'second');
    } else {
      for (var i = 0; i < STEPS.length; i++) {
        if (Math.abs(diff) >= STEPS[i][1] || STEPS[i][0] === 'minute') {
          text = rel.format(Math.round(diff / STEPS[i][1]), STEPS[i][0]);
          break;
        }
      }
    }
    if (el.textContent !== text) el.textContent = text;
  }

  var timer = 0;
  function all(scope) {
    var list = (scope || document).querySelectorAll(TIME), now = new Date();
    for (var i = 0; i < list.length; i++) render(list[i], now);
    if (!timer && document.querySelector(TIME)) timer = setInterval(function () { all(document); }, 60000);
    return list.length;
  }

  /* Insiyab.time(scope?) writes the times again — after times were added to the
     page, say — and returns how many it wrote. */
  Insiyab.time = function (scope) { return all(scope && scope.nodeType ? scope : typeof scope === 'string' ? document.querySelector(scope) : document); };

  Insiyab.define('times', function (scope) { all(scope); });
  if (document.readyState !== 'loading') all(document);
})(window, document);
