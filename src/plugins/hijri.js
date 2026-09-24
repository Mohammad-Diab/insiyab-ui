/* ==========================================================================
   Insiyab · Hijri calendar plugin
   ==========================================================================

   Hijri dates in the date picker. Load it after insiyab.js:

     <script src="insiyab.js"></script>
     <script src="plugins/insiyab-hijri.js"></script>

   and ask for it on a field, or on <html> for every date field on the page:

     <input type="date" data-ins-date data-ins-calendar="hijri">

   Two calendars are registered:

     hijri         Umm al-Qura — Saudi Arabia's official calendar, and what most
                   people mean by "the Hijri date" on a form
     hijri-civil   the tabular (arithmetical) Islamic calendar, for an
                   institution that keeps that one instead

   The field shows and is picked in Hijri; a switch in the calendar's footer flips
   it to Gregorian and back. **What the form sends does not change**: the hidden
   input still holds the Gregorian ISO date, exactly what a plain date field sends,
   so a server and a database need nothing new. Hijri is how the person reads and
   chooses the date, not how it is stored.

   No date tables are shipped. The browser's own Intl already knows both calendars
   — every current engine carries them — so a Hijri date is `formatToParts` away,
   and the one direction Intl does not offer, Hijri to Gregorian, is found by
   estimating from the mean month and correcting against Intl, which converges in
   a step or two. An engine without the calendar is told so once in the console,
   and its fields stay Gregorian and working.
   ========================================================================== */

(function (window) {
  'use strict';

  var Insiyab = window.Insiyab;
  if (!Insiyab || !Insiyab.calendar) {
    if (window.console) window.console.warn('[insiyab-hijri] load insiyab.js before this plugin.');
    return;
  }

  var DAY_MS = 86400000;
  var MEAN_MONTH = 29.530589;             /* days, the mean synodic month */
  var EPOCH = Date.UTC(622, 6, 19);       /* 1 Muharram 1 AH, proleptic Gregorian */

  /* Local-midnight days, as the core keeps them. */
  function day(y, m, d) { return new Date(y, m, d); }
  function addDays(d, n) { return day(d.getFullYear(), d.getMonth(), d.getDate() + n); }
  function daysBetween(a, b) { return Math.round((b - a) / DAY_MS); }

  function supported(intl) {
    try {
      return new Intl.DateTimeFormat('en', { calendar: intl }).resolvedOptions().calendar === intl;
    } catch (e) {
      return false;
    }
  }

  function make(intl, label) {
    /* English, Latin digits: the parts are read as numbers, and the locale of the
       page has nothing to do with which Hijri day it is. */
    var numeric = new Intl.DateTimeFormat('en', {
      calendar: intl, numberingSystem: 'latn', day: 'numeric', month: 'numeric', year: 'numeric'
    });
    var starts = {};                      /* y * 12 + m -> the Date of that month's 1st */

    function parts(d) {
      var ps = numeric.formatToParts(d), out = { y: 0, m: 0, d: 0 };
      for (var i = 0; i < ps.length; i++) {
        if (ps[i].type === 'year') out.y = +ps[i].value;
        else if (ps[i].type === 'month') out.m = +ps[i].value - 1;
        else if (ps[i].type === 'day') out.d = +ps[i].value;
      }
      return out;
    }

    /* The first of a Hijri month. Estimated from the mean month, pulled back to the
       first of whichever month the estimate landed in, then corrected by whole
       months until it is the one asked for — the estimate is rarely out by more
       than a day or two, so this is one step, occasionally two. */
    function monthStart(y, m) {
      var key = y * 12 + m;
      if (starts[key]) return starts[key];
      var est = new Date(EPOCH + ((y - 1) * 12 + m) * MEAN_MONTH * DAY_MS);
      var d = day(est.getUTCFullYear(), est.getUTCMonth(), est.getUTCDate());
      for (var i = 0; i < 12; i++) {
        var p = parts(d);
        d = addDays(d, 1 - p.d);
        var off = (y - p.y) * 12 + (m - p.m);
        if (off === 0) return (starts[key] = d);
        d = addDays(d, Math.round(off * MEAN_MONTH));
      }
      return (starts[key] = d);
    }

    function fromParts(y, m, d) {
      y += Math.floor(m / 12);
      m = ((m % 12) + 12) % 12;
      return addDays(monthStart(y, m), d - 1);
    }

    function monthLength(y, m) {
      var ny = y + (m === 11 ? 1 : 0), nm = (m + 1) % 12;
      return daysBetween(monthStart(y, m), monthStart(ny, nm));
    }

    /* A typed date. Three numbers, day first as Hijri dates are written — unless
       one of them is a four-digit year, which may lead — in either set of digits
       (the core has already made them Latin). A year past 1700 cannot be Hijri for
       centuries yet, so it is read as the Gregorian date the person evidently
       meant; a two-digit year is this Hijri century. */
    function parse(s) {
      var n = s.split(/[^0-9]+/).filter(Boolean);
      if (n.length !== 3) return undefined;
      var y, m, d;
      if (n[0].length === 4) { y = +n[0]; m = +n[1]; d = +n[2]; }
      else { d = +n[0]; m = +n[1]; y = +n[2]; }
      if (m < 1 || m > 12 || d < 1) return null;
      if (y > 1700) {
        var g = day(y, m - 1, d);
        return g.getMonth() === m - 1 && g.getDate() === d ? g : null;
      }
      if (y < 100) y += 1400;
      if (d > monthLength(y, m - 1)) return null;
      return fromParts(y, m - 1, d);
    }

    return { intl: intl, label: label, parts: parts, fromParts: fromParts, monthLength: monthLength, parse: parse };
  }

  var LABEL = { ar: 'هجري', en: 'Hijri' };
  var added = [];
  if (supported('islamic-umalqura')) { Insiyab.calendar('hijri', make('islamic-umalqura', LABEL)); added.push('hijri'); }
  if (supported('islamic-civil')) { Insiyab.calendar('hijri-civil', make('islamic-civil', LABEL)); added.push('hijri-civil'); }
  if (!added.length && window.console) {
    window.console.warn('[insiyab-hijri] this browser has no Islamic calendar in Intl; date fields stay Gregorian.');
  }
})(window);
