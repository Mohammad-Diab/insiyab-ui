// Insiyab · Birth date plugin: day, month and year typed or picked from three columns; the form still sends an ISO date.
(function (window, document) {
  'use strict';

  var Insiyab = window.Insiyab;
  if (!Insiyab || !Insiyab.define) {
    if (window.console) window.console.warn('[insiyab-birthdate] load insiyab.js before this plugin.');
    return;
  }

  var FIELD = 'input[data-ins-birth]';
  var ORDER = ['d', 'm', 'y'], LEN = { d: 2, m: 2, y: 4 }, NEXT = { d: 'm', m: 'y', y: null }, PREV = { d: null, m: 'd', y: 'm' };
  var TODAY = new Date(), TY = TODAY.getFullYear(), TM = TODAY.getMonth() + 1, TD = TODAY.getDate();

  function arYears(n) {
    var r = n % 100;
    return n === 0 ? 'أقل من سنة' : n === 1 ? 'سنة واحدة' : n === 2 ? 'سنتان' : n + (r >= 3 && r <= 10 ? ' سنوات' : ' سنة');
  }
  var TEXT = {
    ar: {
      word: { d: 'يوم', m: 'شهر', y: 'سنة' }, cap: { d: 'اليوم', m: 'الشهر', y: 'السنة' }, group: 'تاريخ الميلاد',
      start: 'اكتب التاريخ أو اسحب الأعمدة', left: function (list) { return 'بقي ' + list.join(' و'); },
      age: function (n) { return 'العمر ' + arYears(n); }, young: function (n) { return 'يجب أن يكون العمر ' + arYears(n) + ' على الأقل'; },
      far: 'هذا التاريخ أبعد من المسموح', incomplete: 'أكمل تاريخ الميلاد', required: 'اكتب تاريخ ميلادك',
      pick: 'اختر من الأعمدة', done: 'تم', cancel: 'إلغاء'
    },
    en: {
      word: { d: 'Day', m: 'Month', y: 'Year' }, cap: { d: 'Day', m: 'Month', y: 'Year' }, group: 'Date of birth',
      start: 'Type the date or drag the columns', left: function (list) { return 'Still to choose: ' + list.join(' and ').toLowerCase(); },
      age: function (n) { return n === 1 ? '1 year old' : n + ' years old'; }, young: function (n) { return 'You must be at least ' + n + ' years old'; },
      far: 'This date is further back than allowed', incomplete: 'Finish the date of birth', required: 'Enter your date of birth',
      pick: 'Pick from the columns', done: 'Done', cancel: 'Cancel'
    }
  };

  function langOf(el) { var h = el.closest('[lang]') || document.documentElement; return h.getAttribute('lang') || 'en'; }
  function isAr(el) { return langOf(el).slice(0, 2).toLowerCase() === 'ar'; }
  function pad(n) { return (n < 10 ? '0' : '') + n; }
  function utc(y, m, d) { var t = new Date(Date.UTC(y, m - 1, d)); if (y < 100) t.setUTCFullYear(y); return t; }
  function real(y, m, d) { var t = utc(y, m, d); return t.getUTCFullYear() === y && t.getUTCMonth() === m - 1 && t.getUTCDate() === d ? t : null; }
  function iso(t) { return t.getUTCFullYear() + '-' + pad(t.getUTCMonth() + 1) + '-' + pad(t.getUTCDate()); }
  function age(t) {
    var a = TY - t.getUTCFullYear();
    if (TM - 1 < t.getUTCMonth() || (TM - 1 === t.getUTCMonth() && TD < t.getUTCDate())) a--;
    return a;
  }
  function latin(s) {
    return String(s || '').replace(/[٠-٩]/g, function (c) { return c.charCodeAt(0) - 0x660; }).replace(/[۰-۹]/g, function (c) { return c.charCodeAt(0) - 0x6f0; });
  }
  function fmt(lang, opts) {
    opts.timeZone = 'UTC'; opts.numberingSystem = 'latn';
    try { return new Intl.DateTimeFormat(lang, opts); } catch (e) { return new Intl.DateTimeFormat('en', opts); }
  }
  function emit(el, name, detail) {
    var ev;
    try { ev = new CustomEvent(name, { detail: detail, bubbles: true }); }
    catch (e) { ev = document.createEvent('CustomEvent'); ev.initCustomEvent(name, true, false, detail); }
    el.dispatchEvent(ev);
  }
  function fire(el, type) {
    var ev;
    try { ev = new Event(type, { bubbles: true }); } catch (e) { ev = document.createEvent('Event'); ev.initEvent(type, true, false); }
    el.dispatchEvent(ev);
  }

  // Pasted text in any common form: digits, Egyptian, Levantine, Maghrebi or English month names, or a Hijri date.
  var hijriParts = null;
  try { hijriParts = new Intl.DateTimeFormat('en-u-ca-islamic-umalqura-nu-latn', { day: 'numeric', month: 'numeric', year: 'numeric', timeZone: 'UTC' }); } catch (e) {}
  function hParts(t) { var p = {}; hijriParts.formatToParts(t).forEach(function (x) { if (x.type !== 'literal') p[x.type] = parseInt(x.value, 10); }); return p; }
  function fromHijri(hy, hm, hd) {
    if (!hijriParts) return null;
    var est = Date.UTC(622, 6, 19) + ((hy - 1) * 354.36707 + (hm - 1) * 29.530588 + hd - 1) * 864e5;
    for (var k = -45; k <= 45; k++) { var t = new Date(est + k * 864e5), p = hParts(t); if (p.year === hy && p.month === hm && p.day === hd) return t; }
    return null;
  }
  function clean(s) {
    return latin(s).toLowerCase().replace(/[ً-ْـ]/g, '').replace(/[أإآ]/g, 'ا').replace(/ة/g, 'ه').replace(/ى/g, 'ي')
      .replace(/[،,.\-\/\\_|]+/g, ' ').replace(/(\d)([^\d\s])/g, '$1 $2').replace(/([^\d\s])(\d)/g, '$1 $2').replace(/\s+/g, ' ').trim();
  }
  var NAMES = [];
  function names(cal, list) { list.forEach(function (words, i) { words.split('|').forEach(function (w) { NAMES.push({ w: clean(w), cal: cal, n: i + 1 }); }); }); }
  names('g', ['يناير|جانفي|كانون الثاني|january|jan', 'فبراير|فيفري|شباط|february|feb', 'مارس|اذار|march|mar', 'ابريل|افريل|نيسان|april|apr',
    'مايو|ماي|ايار|may', 'يونيو|يونيه|جوان|حزيران|june|jun', 'يوليو|يوليه|جويليه|تموز|july|jul', 'اغسطس|اوت|اب|august|aug',
    'سبتمبر|ايلول|september|sept|sep', 'اكتوبر|تشرين الاول|october|oct', 'نوفمبر|تشرين الثاني|november|nov', 'ديسمبر|كانون الاول|december|dec']);
  names('h', ['محرم', 'صفر', 'ربيع الاول|ربيع اول', 'ربيع الثاني|ربيع الاخر|ربيع ثاني', 'جمادى الاولى|جمادى الاول|جمادى اولى',
    'جمادى الثانية|جمادى الاخرة|جمادى الثاني|جمادى الاخر', 'رجب', 'شعبان', 'رمضان', 'شوال', 'ذو القعدة|ذي القعدة', 'ذو الحجة|ذي الحجة']);
  NAMES.sort(function (a, b) { return b.w.length - a.w.length; });
  function understand(raw) {
    var t = ' ' + clean(raw) + ' ', month = 0, cal = '';
    for (var i = 0; i < NAMES.length; i++) {
      var at = t.indexOf(' ' + NAMES[i].w + ' ');
      if (at >= 0) { month = NAMES[i].n; cal = NAMES[i].cal; t = t.slice(0, at) + ' ' + t.slice(at + NAMES[i].w.length + 1); break; }
    }
    if (/ (ه|هجري|هجريه) /.test(t)) cal = 'h';
    var nums = t.match(/\d+/g) || [], d = 0, m = month, ys = '';
    if (month) {
      var big = nums.filter(function (n) { return n.length >= 3 || +n > 31; })[0];
      if (big) { ys = big; d = +(nums.filter(function (n) { return n !== big; })[0] || 0); } else { d = +(nums[0] || 0); ys = nums[1] || ''; }
    } else if (nums.length >= 3) {
      if (nums[0].length >= 3) { ys = nums[0]; m = +nums[1]; d = +nums[2]; } else { d = +nums[0]; m = +nums[1]; ys = nums[2]; }
    }
    if (!d || !m || m > 12 || !(ys.length === 2 || ys.length === 4)) return null;
    var y = +ys, hy = hijriParts ? hParts(TODAY).year : 1448;
    if (ys.length === 2) { if (cal === 'h') { y += 1400; if (y > hy) y -= 100; } else { y += 2000; if (y > TY) y -= 100; } }
    if (!cal) cal = y < 1800 ? 'h' : 'g';
    return cal === 'h' ? fromHijri(y, m, d) : real(y, m, d);
  }

  function state(input) { return input._insBirth; }

  function build(input) {
    if (input.hasAttribute('data-ins-birth-ready')) return;
    input.setAttribute('data-ins-birth-ready', '');
    var lang = langOf(input), ar = isAr(input), t = ar ? TEXT.ar : TEXT.en;
    var monthFmt = fmt(lang, { month: 'long' }), dayFmt = fmt(lang, { weekday: 'long' }), longFmt = fmt(lang, { day: 'numeric', month: 'long', year: 'numeric' });
    var hijriFmt = null;
    if (ar) { try { hijriFmt = new Intl.DateTimeFormat('ar-SA-u-ca-islamic-umalqura-nu-latn', { day: 'numeric', month: 'long', year: 'numeric', timeZone: 'UTC' }); } catch (e) {} }
    var MONTHS = [];
    for (var i = 0; i < 12; i++) MONTHS.push(monthFmt.format(Date.UTC(2000, i, 1)));

    var st = input._insBirth = { input: input, parts: { d: [], m: [], y: [] }, set: { d: false, m: false, y: false }, val: {}, seg: {}, inp: {}, cols: {}, intent: {}, timers: {} };
    st.required = input.hasAttribute('required');
    st.initial = input.value;
    function num(name, dflt) { var v = parseInt(input.getAttribute(name), 10); return isNaN(v) ? dflt : v; }
    function config() {
      st.minAge = Math.max(0, num('data-ins-age-min', 0));
      var maxAge = Math.max(st.minAge + 1, num('data-ins-age-max', 120));
      st.LY = TY - st.minAge; st.FY = TY - maxAge;
      st.limit = Date.UTC(st.LY, TM - 1, TD);
      st.start = { d: 16, m: 6, y: Math.max(st.FY, Math.min(st.LY, TY - num('data-ins-age-start', 31))) };
      st.all = [];
      for (var ms = Date.UTC(st.FY, 0, 1); ms <= st.limit; ms += 864e5) { var x = new Date(ms); st.all.push(pad(x.getUTCDate()) + pad(x.getUTCMonth() + 1) + x.getUTCFullYear()); }
    }
    config();
    st.val = { d: st.start.d, m: st.start.m, y: st.start.y };

    input.removeAttribute('required');
    input.type = 'hidden';
    var inline = input.getAttribute('data-ins-birth') === 'inline';
    if (!inline && typeof document.createElement('div').showPopover !== 'function') inline = true;
    var box = document.createElement('div');
    box.className = 'ins-birth ' + (inline ? 'ins-birth--inline' : 'ins-birth--field');
    box.setAttribute('role', 'group');
    var label = input.id ? document.querySelector('label[for="' + input.id + '"]') : null;
    if (label) {
      if (!label.id) label.id = input.id + '-label';
      box.setAttribute('aria-labelledby', label.id);
      label.addEventListener('click', function (e) { e.preventDefault(); focusPart(firstOpen()); });
    } else box.setAttribute('aria-label', input.getAttribute('aria-label') || t.group);
    input.parentNode.insertBefore(box, input.nextSibling);
    st.box = box;

    var segs = document.createElement('div');
    segs.className = 'ins-birth-segs';
    var control = null, btn = null, pop = null, popLine = null;
    if (inline) box.appendChild(segs);
    else {
      control = document.createElement('div');
      control.className = 'ins-birth-control';
      control.appendChild(segs);
      btn = document.createElement('button');
      btn.type = 'button'; btn.className = 'ins-birth-btn';
      btn.setAttribute('aria-label', t.pick); btn.setAttribute('aria-haspopup', 'dialog'); btn.setAttribute('aria-expanded', 'false');
      btn.innerHTML = '<svg viewBox="0 0 24 24" aria-hidden="true"><rect x="3" y="4" width="18" height="17" rx="3"/><path d="M8 2v4M16 2v4M3 10h18"/></svg>';
      if (input.disabled) btn.disabled = true;
      control.appendChild(btn);
      box.appendChild(control);
    }
    ORDER.forEach(function (k) {
      if (!inline && k !== 'd') { var sep = document.createElement('span'); sep.className = 'ins-birth-sep'; sep.setAttribute('aria-hidden', 'true'); sep.textContent = '/'; segs.appendChild(sep); }
      var seg = document.createElement('label');
      seg.className = 'ins-birth-seg'; seg.setAttribute('data-part', k);
      var field = document.createElement('input');
      field.className = 'ins-birth-in'; field.dir = 'ltr'; field.setAttribute('inputmode', 'decimal');
      field.setAttribute('autocomplete', 'bday-' + { d: 'day', m: 'month', y: 'year' }[k]);
      field.setAttribute('aria-label', t.cap[k]);
      if (input.disabled) field.disabled = true;
      var shown = document.createElement('span'); shown.className = 'ins-birth-box'; shown.setAttribute('aria-hidden', 'true');
      var value = document.createElement('span'); value.className = 'ins-birth-val';
      var dots = document.createElement('span'); dots.className = 'ins-birth-dots';
      for (var j = 0; j < LEN[k]; j++) dots.appendChild(document.createElement('i'));
      shown.appendChild(value); shown.appendChild(dots);
      var cap = document.createElement('span'); cap.className = 'ins-birth-cap'; cap.setAttribute('aria-hidden', 'true'); cap.textContent = t.cap[k];
      seg.appendChild(field); seg.appendChild(shown); seg.appendChild(cap);
      segs.appendChild(seg);
      st.seg[k] = seg; st.inp[k] = field;
    });
    var line = document.createElement('p');
    line.className = 'ins-birth-line'; line.setAttribute('aria-live', 'polite');
    var drum = document.createElement('div');
    drum.className = 'ins-drum ins-birth-drum';
    if (inline) box.appendChild(drum);
    else {
      pop = document.createElement('div');
      pop.className = 'ins-birth-pop'; pop.setAttribute('popover', 'manual'); pop.setAttribute('role', 'dialog');
      pop.setAttribute('aria-label', label ? label.textContent.trim() : (input.getAttribute('aria-label') || t.group));
      popLine = document.createElement('p'); popLine.className = 'ins-birth-line';
      var foot = document.createElement('div'); foot.className = 'ins-birth-foot';
      var ok = document.createElement('button'); ok.type = 'button'; ok.className = 'ins-btn ins-btn--primary ins-btn--sm'; ok.textContent = t.done;
      ok.addEventListener('click', function () { pop.hidePopover(); focusPart(firstOpen()); });
      var no = document.createElement('button'); no.type = 'button'; no.className = 'ins-btn ins-btn--bare ins-btn--sm'; no.textContent = t.cancel;
      no.addEventListener('click', function () { st.cancel(); focusPart(firstOpen()); });
      foot.appendChild(no); foot.appendChild(ok);
      pop.appendChild(popLine); pop.appendChild(drum); pop.appendChild(foot);
      box.appendChild(pop);
      btn.popoverTargetElement = pop; btn.popoverTargetAction = 'toggle';
      var place = function () {
        if (!pop.matches(':popover-open')) return;
        var r = control.getBoundingClientRect(), w = pop.offsetWidth, h = pop.offsetHeight, rtl = getComputedStyle(box).direction === 'rtl';
        var left = rtl ? r.right - w : r.left;
        pop.style.left = Math.max(8, Math.min(window.innerWidth - w - 8, left)) + 'px';
        pop.style.top = (r.bottom + 6 + h > window.innerHeight && r.top - 6 - h > 0 ? r.top - 6 - h : r.bottom + 6) + 'px';
      };
      var isOpen = function () { return pop.matches(':popover-open'); }, stay = false;
      st.open = function (keep) { if (isOpen() || input.disabled) return; stay = keep; pop.showPopover(); };
      pop.addEventListener('toggle', function (e) {
        var open = e.newState === 'open', keep = stay;
        stay = false;
        btn.setAttribute('aria-expanded', String(open));
        if (!open) return;
        st.saved = JSON.stringify({ parts: st.parts, set: st.set, val: st.val });
        place();
        requestAnimationFrame(function () { ORDER.forEach(goValue); place(); if (!keep) st.cols[firstOpen()].focus({ preventScroll: true }); });
      });
      st.cancel = function () {
        var was = JSON.parse(st.saved);
        st.parts = was.parts; st.set = was.set; st.val = was.val;
        fill('d'); ORDER.forEach(goValue); paint();
        pop.hidePopover();
      };
      control.addEventListener('click', function (e) {
        if (btn.contains(e.target)) return;
        if (e.target.closest('.ins-birth-seg') && !e.target.classList.contains('ins-birth-in')) return;
        if (!e.target.closest('.ins-birth-seg')) focusPart(firstOpen());
        if (isOpen()) pop.hidePopover(); else st.open(true);
      });
      document.addEventListener('pointerdown', function (e) { if (isOpen() && !box.contains(e.target)) pop.hidePopover(); }, true);
      box.addEventListener('focusout', function (e) { if (isOpen() && e.relatedTarget && !box.contains(e.relatedTarget)) pop.hidePopover(); });
      box.addEventListener('keydown', function (e) {
        if (e.key !== 'Escape' || !isOpen()) return;
        e.preventDefault(); e.stopPropagation();
        var inside = pop.contains(document.activeElement);
        st.cancel();
        if (inside) focusPart(firstOpen());
      }, true);
      window.addEventListener('scroll', place, true);
      window.addEventListener('resize', place);
    }
    box.appendChild(line);
    st.pop = pop;

    function str(k) { return st.parts[k].map(function (c) { return c.ch; }).join(''); }
    function chars(s, auto) { return s.split('').map(function (c) { return { ch: c, auto: auto }; }); }
    function possible(p) {
      for (var n = 0; n < st.all.length; n++) {
        var s = st.all[n];
        if (s.slice(0, 2).lastIndexOf(p.d, 0) === 0 && s.slice(2, 4).lastIndexOf(p.m, 0) === 0 && s.slice(4).lastIndexOf(p.y, 0) === 0) return true;
      }
      return false;
    }
    function strs(k, s) { var o = { d: str('d'), m: str('m'), y: str('y') }; o[k] = s; return o; }
    function firstOpen() { return !st.set.d ? 'd' : !st.set.m ? 'm' : 'y'; }
    function caretEnd(k) { var f = st.inp[k]; try { f.setSelectionRange(f.value.length, f.value.length); } catch (e) {} }
    function focusPart(k) { st.inp[k].focus({ preventScroll: true }); caretEnd(k); }
    function flash(k) {
      var seg = st.seg[k];
      seg.classList.add('is-no'); setTimeout(function () { seg.classList.remove('is-no'); }, 350);
      if (k === 'y' && st.minAge) line.textContent = t.young(st.minAge);
    }

    function press(k, key) {
      var cur = st.parts[k], s = str(k), tries = [cur.concat(chars(key, false))];
      if (k !== 'y' && !s) tries.push(chars('0', true).concat(chars(key, false)));
      if (k === 'y' && !s) tries.push(chars(key === '0' ? '20' : '19', true).concat(chars(key, false)));
      if (k === 'y' && s.length === 1 && !cur[0].auto) ['20', '19'].forEach(function (c) { tries.push(chars(c, true).concat(cur, chars(key, false))); });
      for (var n = 0; n < tries.length; n++) {
        var ns = tries[n].map(function (c) { return c.ch; }).join('');
        if (ns.length <= LEN[k] && possible(strs(k, ns))) {
          st.parts[k] = tries[n];
          changed();
          if (ns.length === LEN[k] && NEXT[k]) focusPart(NEXT[k]);
          return;
        }
      }
      if (NEXT[k] && s.length === 1 && !cur[0].auto && separate(k)) { press(NEXT[k], key); return; }
      flash(k);
    }
    function separate(k) {
      var s = str(k);
      if (k === 'y' || s.length !== 1 || !possible(strs(k, '0' + s))) return false;
      st.parts[k] = chars('0', true).concat(st.parts[k]);
      changed(); focusPart(NEXT[k]);
      return true;
    }
    function trim(k) { var p = st.parts[k]; while (p.length && p[p.length - 1].auto) p.pop(); }
    function backspace(k) {
      if (!st.parts[k].length) { if (!PREV[k]) return; k = PREV[k]; focusPart(k); }
      trim(k); st.parts[k].pop(); trim(k);
      changed();
    }
    function fillAll(date) {
      st.parts = { d: chars(pad(date.getUTCDate()), false), m: chars(pad(date.getUTCMonth() + 1), false), y: chars(String(date.getUTCFullYear()), false) };
      changed();
    }
    st.setDate = function (date) { if (date) fillAll(date); else clear(); };

    function changed() {
      var refill = false;
      ORDER.forEach(function (p) {
        var s = str(p);
        if (s.length === LEN[p]) {
          var v = +s;
          if (!st.set[p] || st.val[p] !== v) { st.val[p] = v; st.set[p] = true; if (p !== 'd') refill = true; }
        } else st.set[p] = false;
      });
      if (refill) fill('d');
      ORDER.forEach(function (p) { if (st.set[p]) goValue(p); });
      paint();
    }

    function dim() { return st.set.m ? utc(st.set.y ? st.val.y : 2000, st.val.m + 1, 0).getUTCDate() : 31; }
    function items(kind) {
      var a = [];
      if (kind === 'd') for (var n = 1; n <= dim(); n++) a.push({ v: n, t: String(n) });
      if (kind === 'm') MONTHS.forEach(function (name, n) { a.push({ v: n + 1, t: name }); });
      if (kind === 'y') for (var y = st.FY; y <= st.LY; y++) a.push({ v: y, t: String(y) });
      return a;
    }
    function row(kind) { var it = st.cols[kind].firstElementChild; return it ? it.offsetHeight : 36; }
    function index(kind) { return Math.round(st.cols[kind].scrollTop / row(kind)); }
    function fill(kind) {
      var col = st.cols[kind]; col.textContent = '';
      items(kind).forEach(function (it) {
        var el = document.createElement('div');
        el.className = 'ins-drum-item'; el.textContent = it.t; el.setAttribute('data-v', it.v); el.setAttribute('role', 'option');
        col.appendChild(el);
      });
      if (kind === 'd' && !st.set.d && st.val.d > dim()) st.val.d = dim();
    }
    function go(kind, n, smooth) {
      var col = st.cols[kind];
      n = Math.max(0, Math.min(col.children.length - 1, n));
      var top = n * row(kind), still = Math.abs(col.scrollTop - top) < 1;
      col.scrollTo({ top: top, behavior: smooth ? 'smooth' : 'auto' });
      if (!smooth) mark(kind, n);
      else if (still) settle(kind);
    }
    function goValue(kind) { go(kind, kind === 'y' ? st.val.y - st.FY : st.val[kind] - 1, false); }
    function mark(kind, n) {
      [].forEach.call(st.cols[kind].children, function (el, j) { el.classList.toggle('is-on', j === n); el.setAttribute('aria-selected', String(j === n)); });
    }
    function settle(kind) {
      var col = st.cols[kind];
      if (!col.children.length) return;
      if (input.disabled) st.intent[kind] = false;
      var n = Math.max(0, Math.min(col.children.length - 1, index(kind)));
      mark(kind, n);
      if (!st.intent[kind]) return;
      st.intent[kind] = false;
      var v = +col.children[n].getAttribute('data-v');
      st.val[kind] = v; st.set[kind] = true;
      st.parts[kind] = chars(kind === 'y' ? String(v) : pad(v), false);
      if (kind !== 'd') {
        fill('d');
        if (st.set.d) { st.val.d = Math.min(st.val.d, dim()); st.parts.d = chars(pad(st.val.d), false); }
        goValue('d');
      }
      paint();
    }
    function choose(kind) {
      clearTimeout(st.timers[kind]);
      st.intent[kind] = true; settle(kind);
      var next = ORDER.filter(function (k) { return !st.set[k]; })[0];
      if (next) st.cols[next].focus({ preventScroll: true });
      else { st.pop.hidePopover(); focusPart('y'); }
    }
    function makeCol(kind) {
      var col = document.createElement('div');
      col.className = 'ins-drum-col ins-birth-col ins-birth-col--' + kind + ' is-unset'; col.tabIndex = inline ? -1 : 0;
      col.setAttribute('role', 'listbox'); col.setAttribute('aria-label', t.cap[kind]);
      drum.appendChild(col); st.cols[kind] = col;
      ['pointerdown', 'wheel', 'touchstart'].forEach(function (type) { col.addEventListener(type, function () { st.intent[kind] = true; }, { passive: true }); });
      col.addEventListener('scroll', function () {
        if (col.classList.contains('is-drag')) return;
        clearTimeout(st.timers[kind]); st.timers[kind] = setTimeout(function () { settle(kind); }, 90);
      });
      var drag = null;
      col.addEventListener('pointerdown', function (e) {
        if (input.disabled || e.pointerType !== 'mouse' || e.button !== 0) return;
        e.preventDefault();
        drag = { y: e.clientY, top: col.scrollTop, moved: false, lastY: e.clientY, lastT: Date.now(), v: 0 };
        col.setPointerCapture(e.pointerId);
      });
      col.addEventListener('pointermove', function (e) {
        if (!drag) return;
        var dy = e.clientY - drag.y, now = Date.now(), dt = Math.max(1, now - drag.lastT);
        if (!drag.moved && Math.abs(dy) > 3) { drag.moved = true; col.classList.add('is-drag'); }
        if (!drag.moved) return;
        col.scrollTop = drag.top - dy;
        drag.v = drag.v * 0.6 + ((e.clientY - drag.lastY) / dt) * 0.4;
        drag.lastY = e.clientY; drag.lastT = now;
      });
      function drop(e) {
        if (!drag) return;
        var d = drag; drag = null;
        col.classList.remove('is-drag');
        st.intent[kind] = true;
        if (!d.moved) {
          var hit = document.elementFromPoint(e.clientX, e.clientY), item = hit && hit.closest ? hit.closest('.ins-drum-item') : null;
          if (item && col.contains(item)) go(kind, [].indexOf.call(col.children, item), true);
          else st.intent[kind] = false;
          return;
        }
        var fling = Date.now() - d.lastT > 80 || Math.abs(d.v) < 0.6 ? 0 : d.v;
        go(kind, Math.round((col.scrollTop - fling * 180) / row(kind)), true);
      }
      col.addEventListener('pointerup', drop);
      col.addEventListener('pointercancel', drop);
      col.addEventListener('keydown', function (e) {
        if (e.key === 'Enter' && st.pop) { e.preventDefault(); choose(kind); return; }
        var n = index(kind), s = { ArrowDown: 1, ArrowUp: -1, PageDown: 10, PageUp: -10 }[e.key];
        if (e.key === 'Home') s = -n; else if (e.key === 'End') s = col.children.length - 1 - n;
        if (!s) return;
        e.preventDefault(); st.intent[kind] = true; go(kind, n + s, true);
      });
      col.addEventListener('click', function (e) {
        var item = e.target.closest ? e.target.closest('.ins-drum-item') : null;
        if (!item || input.disabled) return;
        st.intent[kind] = true; go(kind, [].indexOf.call(col.children, item), true);
      });
    }
    ORDER.forEach(makeCol); ORDER.forEach(fill);

    function message(date) {
      var any = ORDER.some(function (k) { return st.parts[k].length; });
      if (date && date.getTime() > st.limit) return st.minAge ? t.young(st.minAge) : t.far;
      if (date && date.getTime() < Date.UTC(st.FY, 0, 1)) return t.far;
      if (date) return '';
      return any ? t.incomplete : st.required ? t.required : '';
    }
    function paint() {
      var date = st.set.d && st.set.m && st.set.y ? real(st.val.y, st.val.m, st.val.d) : null;
      ORDER.forEach(function (k) {
        var seg = st.seg[k], p = st.parts[k];
        var val = seg.querySelector('.ins-birth-val'), dots = seg.querySelectorAll('.ins-birth-dots i');
        val.textContent = '';
        if (p.length) p.forEach(function (c) { var s = document.createElement('span'); if (c.auto) s.className = 'is-auto'; s.textContent = c.ch; val.appendChild(s); });
        else { var ph = document.createElement('span'); ph.className = 'ins-birth-ph'; ph.textContent = t.word[k]; val.appendChild(ph); }
        for (var n = 0; n < dots.length; n++) dots[n].classList.toggle('is-on', n < p.length);
        seg.classList.toggle('is-filled', p.length > 0);
        if (st.inp[k].value !== str(k)) st.inp[k].value = str(k);
        st.cols[k].classList.toggle('is-unset', !st.set[k]);
      });
      st.seg.m.querySelector('.ins-birth-cap').textContent = st.set.m ? MONTHS[st.val.m - 1] : t.cap.m;
      [].forEach.call(st.cols.m.children, function (el, n) { el.classList.toggle('is-out', st.set.y && st.val.y === st.LY && n + 1 > TM); });
      var msg = message(date);
      if (msg) date = null;
      var open = ORDER.filter(function (k) { return !st.set[k]; }).map(function (k) { return t.cap[k]; });
      var hard = msg && msg !== t.incomplete && msg !== t.required ? msg : '';
      var status = hard || (open.length === 3 ? t.start : t.left(open));
      [line, popLine].forEach(function (el) {
        if (!el) return;
        el.textContent = '';
        if (date) {
          var b = document.createElement('b'); b.textContent = dayFmt.format(date) + ' ' + longFmt.format(date);
          el.appendChild(b);
          el.appendChild(document.createTextNode(' · ' + t.age(age(date)) + (hijriFmt ? ' · ' + hijriFmt.format(date) : '')));
        } else el.textContent = el === line && !inline ? hard : status;
      });
      if (!inline) line.title = line.textContent;
      var value = date ? iso(date) : '';
      st.inp.d.setCustomValidity(msg);
      var field = box.closest('.ins-field'), err = null;
      if (field) for (var n = 0; n < field.children.length; n++) if (field.children[n].classList.contains('ins-error')) err = field.children[n];
      if (err && err.hasAttribute('data-ins-auto')) err.textContent = msg;
      if (input.value !== value) {
        input.value = value;
        if (!st.quiet) {
          fire(input, 'input'); fire(input, 'change');
          emit(input, 'ins:birth', { input: input, value: value, date: date });
        }
      }
    }

    ORDER.forEach(function (k) {
      var f = st.inp[k];
      function focused() { for (var n = 0; n < ORDER.length; n++) if (st.inp[ORDER[n]] === document.activeElement) return ORDER[n]; return k; }
      f.addEventListener('beforeinput', function (e) {
        if (e.inputType === 'insertText' || e.inputType === 'insertCompositionText') {
          e.preventDefault();
          latin(e.data || '').split('').forEach(function (c) {
            if (/\d/.test(c)) press(focused(), c);
            else if (/[\/\\.,\- ]/.test(c)) separate(focused());
          });
        } else if (e.inputType === 'deleteContentBackward') { e.preventDefault(); backspace(k); }
        else if (e.inputType === 'insertFromPaste' || e.inputType === 'insertFromDrop') e.preventDefault();
      });
      f.addEventListener('keydown', function (e) {
        if (e.key === 'Backspace') { e.preventDefault(); backspace(k); }
        else if (e.key === 'Enter' || (e.key === 'Tab' && !e.shiftKey)) { if (separate(k)) e.preventDefault(); }
        else if (e.key === 'Escape' && str('d') + str('m') + str('y')) { e.preventDefault(); clear(); focusPart('d'); }
        else if (pop && e.key === 'ArrowDown' && e.altKey) { e.preventDefault(); st.open(false); }
      });
      f.addEventListener('input', function (e) {
        e.stopPropagation();
        var v = latin(f.value).replace(/\D/g, '');
        if (v === str(k)) return;
        st.parts[k] = [];
        v.split('').forEach(function (c) { press(k, c); });
        paint();
      });
      f.addEventListener('change', function (e) { e.stopPropagation(); });
      f.addEventListener('paste', function (e) {
        e.preventDefault();
        var raw = (e.clipboardData || window.clipboardData).getData('text'), date = understand(raw);
        if (date) { fillAll(date); focusPart('y'); return; }
        latin(raw).replace(/\D/g, '').split('').forEach(function (c) { press(focused(), c); });
      });
      f.addEventListener('focus', function () { setTimeout(function () { caretEnd(k); }, 0); });
    });

    function clear() {
      st.parts = { d: [], m: [], y: [] }; st.set = { d: false, m: false, y: false };
      st.val = { d: st.start.d, m: st.start.m, y: st.start.y };
      fill('d'); ORDER.forEach(goValue); paint();
    }
    st.clear = clear;
    st.reconfigure = function () { config(); fill('y'); clear(); };

    st.quiet = true;
    var first = st.initial && /^\d{4}-\d{2}-\d{2}$/.test(st.initial) ? real(+st.initial.slice(0, 4), +st.initial.slice(5, 7), +st.initial.slice(8, 10)) : null;
    requestAnimationFrame(function () {
      ORDER.forEach(goValue);
      if (first) fillAll(first); else paint();
      st.quiet = false;
    });
  }

  document.addEventListener('reset', function (e) {
    var form = e.target;
    setTimeout(function () {
      var fields = form.querySelectorAll ? form.querySelectorAll('input[data-ins-birth-ready]') : [];
      for (var i = 0; i < fields.length; i++) {
        var st = state(fields[i]), v = st.initial;
        st.quiet = true;
        st.setDate(/^\d{4}-\d{2}-\d{2}$/.test(v || '') ? real(+v.slice(0, 4), +v.slice(5, 7), +v.slice(8, 10)) : null);
        st.quiet = false;
      }
    }, 0);
  }, true);

  function scan(scope) {
    var found = scope.querySelectorAll ? scope.querySelectorAll(FIELD) : [];
    for (var i = 0; i < found.length; i++) build(found[i]);
    if (scope.matches && scope.matches(FIELD)) build(scope);
  }

  // Insiyab.birth(field) reads the ISO date; Insiyab.birth(field, '1993-01-22') sets it quietly, and '' clears it.
  Insiyab.birth = function (target, value) {
    var input = typeof target === 'string' ? document.querySelector(target) : target;
    var st = input && state(input);
    if (!st) return null;
    if (value === undefined) return input.value;
    st.quiet = true;
    var m = /^(\d{4})-(\d{2})-(\d{2})$/.exec(String(value));
    st.setDate(m ? real(+m[1], +m[2], +m[3]) : null);
    st.quiet = false;
    return input.value;
  };
  Insiyab.birth.reconfigure = function (target) {
    var input = typeof target === 'string' ? document.querySelector(target) : target;
    var st = input && state(input);
    if (st) st.reconfigure();
  };

  Insiyab.define('birthdate', scan);
  if (document.readyState !== 'loading') scan(document);
})(window, document);
