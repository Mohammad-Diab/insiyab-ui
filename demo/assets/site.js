/* ==========================================================================
   Insiyab · documentation site behaviour
   ==========================================================================

   Loaded in <head> before insiyab-boot.js, for the same reason the library
   is: the viewer's chosen brand colour and direction are restored before first
   paint, so moving between pages never flashes the default teal or the wrong
   side for the sidebar.

   None of this is library code. The components on every page need none of it.

   Query parameters, for a screenshot harness that can navigate but not click:
     ?theme=dark|light  ?brand=%230F766E  ?dir=ltr  ?fx=off  ?collapsed=1
     ?open=<dialog id>  ?toast=1  ?mode=simple|advanced
   ========================================================================== */

(function () {
  'use strict';

  var KEY_BRAND = 'insiyab-demo-brand';
  var KEY_DIR = 'insiyab-demo-dir';
  var KEY_MODE = 'insiyab-demo-mode';

  // A page's playground definitions register here as the page parses, before the playgrounds boot.
  var SitePlay = window.SitePlay = { defs: {}, define: function (def) { this.defs[def.id] = def; } };
  var root = document.documentElement;
  var q = new URLSearchParams(location.search);

  function read(key) { try { return localStorage.getItem(key); } catch (e) { return null; } }
  function write(key, value) {
    try { if (value == null) localStorage.removeItem(key); else localStorage.setItem(key, value); } catch (e) { /* not fatal */ }
  }

  function applyDir(dir) {
    root.setAttribute('dir', dir);
    root.setAttribute('lang', dir === 'rtl' ? 'ar' : 'en');
  }

  /* ---------------------------------------------------- before first paint */
  var dir = q.get('dir') || read(KEY_DIR);
  if (dir === 'ltr' || dir === 'rtl') applyDir(dir);
  var brand = q.get('brand') || read(KEY_BRAND);
  if (brand) root.setAttribute('data-ins-primary', brand);
  if (q.get('fx') === 'off') root.setAttribute('data-ins-fx', 'off');
  var mode = q.get('mode') || read(KEY_MODE);
  root.setAttribute('data-site-mode', mode === 'advanced' ? 'advanced' : 'simple');

  /* ----------------------------------------------------------- once parsed */
  function syncCustomiser() {
    var current = (Insiyab.brand() || '').toLowerCase();
    document.querySelectorAll('[data-brand]').forEach(function (b) {
      b.setAttribute('aria-pressed', String(b.getAttribute('data-brand').toLowerCase() === current));
    });
    var d = root.getAttribute('dir');
    document.querySelectorAll('.site-dir [data-dir]').forEach(function (b) {
      var on = b.getAttribute('data-dir') === d;
      b.classList.toggle('is-active', on);
      b.setAttribute('aria-pressed', String(on));
    });
    var m = root.getAttribute('data-site-mode');
    document.querySelectorAll('.site-mode [data-mode]').forEach(function (b) {
      var on = b.getAttribute('data-mode') === m;
      b.classList.toggle('is-active', on);
      b.setAttribute('aria-pressed', String(on));
    });
  }

  function rendered(sel) {
    return Array.prototype.filter.call(document.querySelectorAll(sel), function (el) { return el.getClientRects().length > 0; });
  }

  function seconds(name) { return parseFloat(getComputedStyle(root).getPropertyValue(name)) * 1000 || 250; }

  var running = [], turn = 0;
  function play(el, frames, opts) {
    var a = el.animate(frames, opts);
    running.push(a);
    return a;
  }

  function fold(els, open) {
    var ease = getComputedStyle(root).getPropertyValue('--ins-ease').trim() || 'ease';
    return els.map(function (el) {
      var cs = getComputedStyle(el);
      var full = { height: el.offsetHeight + 'px', paddingBlockStart: cs.paddingBlockStart, paddingBlockEnd: cs.paddingBlockEnd,
        marginBlockStart: cs.marginBlockStart, marginBlockEnd: cs.marginBlockEnd, opacity: 1 };
      var none = { height: '0px', paddingBlockStart: '0px', paddingBlockEnd: '0px', marginBlockStart: '0px', marginBlockEnd: '0px', opacity: 0 };
      el.style.overflow = 'hidden';
      var a = play(el, open ? [none, full] : [full, none], { duration: seconds(open ? '--ins-t-enter' : '--ins-t-exit'), easing: ease });
      var end = function () { el.style.overflow = ''; };
      a.finished.then(end, end);
      return a;
    });
  }

  function sideItems(side) {
    return Array.prototype.filter.call(side.children, function (el) { return el.getClientRects().length > 0; });
  }

  // The sidebar is a stack pulled upward: each item falls into its new place a beat after the one above it, and bounces once.
  function settle(side, items, tops) {
    var view = side.getBoundingClientRect();
    var fall = 'cubic-bezier(.55, 0, 1, .45)', rise = 'cubic-bezier(0, .55, .45, 1)';
    var beat = 0;
    items.forEach(function (el, i) {
      var now = el.getBoundingClientRect(), was = tops[i];
      var seen = (now.bottom > view.top && now.top < view.bottom) || (was != null && was + now.height > view.top && was < view.bottom);
      if (!seen) return;
      if (was == null) {
        play(el, [{ opacity: 0, transform: 'translateY(-10px)' }, { opacity: 1, transform: 'none' }],
          { duration: 260, delay: Math.min(beat++ * 26, 380), easing: rise, fill: 'backwards' });
        return;
      }
      var d = was - now.top;
      if (Math.abs(d) < 1) return;
      var kick = (d > 0 ? 1 : -1) * Math.min(6, Math.abs(d) * .12);
      play(el, [
        { transform: 'translateY(' + d + 'px)', easing: fall },
        { transform: 'none', offset: .7, easing: rise },
        { transform: 'translateY(' + kick + 'px)', offset: .85, easing: fall },
        { transform: 'none' }
      ], { duration: 360, delay: Math.min(beat++ * 26, 380), fill: 'backwards' });
    });
  }

  // The sidebar filter counts only links the mode shows, so a mode change filters again.
  function refilter() {
    var f = document.querySelector('.ins-shell-filter input');
    if (f && f.value) f.dispatchEvent(new Event('input', { bubbles: true }));
  }

  // What the new mode drops leaves first, then what it adds arrives.
  function setMode(next) {
    running.forEach(function (a) { a.finish(); });
    running = [];
    var me = ++turn;
    if (next === root.getAttribute('data-site-mode')) { syncCustomiser(); return; }
    var apply = function () { root.setAttribute('data-site-mode', next); syncCustomiser(); refreshPlays(); refilter(); };
    var still = root.getAttribute('data-ins-fx') === 'off' || !Element.prototype.animate || !window.matchMedia || matchMedia('(prefers-reduced-motion: reduce)').matches;
    if (still) { apply(); return; }
    var drop = next === 'simple' ? '.site-adv' : '.site-simple', add = next === 'simple' ? '.site-simple' : '.site-adv';
    var side = document.querySelector('.ins-shell-side');
    var inSide = function (el) { return !!side && side.contains(el); };
    var oldToc = document.querySelector('.site-toc' + drop), newToc = document.querySelector('.site-toc' + add);
    var paired = oldToc && newToc;
    var own = function (el) { return !inSide(el) && !el.matches('.site-pager') && !(paired && (el === oldToc || el === newToc)); };
    // The two contents lists are one list to the eye, so only the links that differ fold.
    var only = function (a, b) {
      var have = Array.prototype.map.call(b.querySelectorAll('a'), function (x) { return x.getAttribute('href'); });
      return Array.prototype.filter.call(a.querySelectorAll('a'), function (x) { return have.indexOf(x.getAttribute('href')) === -1; });
    };
    var out = fold(rendered(drop).filter(own).concat(paired ? only(oldToc, newToc) : []), false)
      .concat(rendered(drop).filter(inSide).map(function (el) {
        return play(el, [{ opacity: 1, transform: 'none' }, { opacity: 0, transform: 'scale(.94)' }], { duration: seconds('--ins-t-exit'), easing: 'ease-in' });
      }));
    Promise.all(out.map(function (a) { return a.finished.catch(function () {}); })).then(function () {
      if (me !== turn) return;
      var before = side ? sideItems(side) : [];
      var tops = before.map(function (el) { return el.getBoundingClientRect().top; });
      apply();
      window.dispatchEvent(new Event('scroll'));
      if (side) {
        var after = sideItems(side);
        settle(side, after, after.map(function (el) { var i = before.indexOf(el); return i === -1 ? null : tops[i]; }));
      }
      fold(rendered(add).filter(own).concat(paired ? only(newToc, oldToc) : []), true);
      rendered('.site-pager' + add).forEach(function (p) {
        play(p, [{ opacity: 0 }, { opacity: 1 }], { duration: seconds('--ins-t-enter'), easing: 'ease-out' });
      });
    });
  }

  // A link into a section the current mode hides opens this page in the other mode, without changing the saved choice.
  function revealTarget() {
    var id = location.hash.slice(1);
    var t = id && document.getElementById(decodeURIComponent(id));
    if (!t) return;
    var simple = root.getAttribute('data-site-mode') === 'simple';
    if (!t.closest(simple ? '.site-adv' : '.site-simple')) return;
    root.setAttribute('data-site-mode', simple ? 'advanced' : 'simple');
    syncCustomiser();
    refreshPlays();
    t.scrollIntoView();
  }

  /* ------------------------------------------------------------ playgrounds */
  var plays = [];

  function playValues(box) {
    var v = {};
    box.querySelectorAll('.site-play-ctl').forEach(function (ctl) {
      var key = ctl.getAttribute('data-key'), type = ctl.getAttribute('data-type');
      if (type === 'seg') { var on = ctl.querySelector('[aria-pressed="true"]'); v[key] = on ? on.getAttribute('data-value') : ''; }
      else if (type === 'switch') v[key] = ctl.querySelector('input').checked;
      else v[key] = ctl.querySelector('select, input').value;
    });
    return v;
  }

  function setPlayValues(box, v) {
    box.querySelectorAll('.site-play-ctl').forEach(function (ctl) {
      var key = ctl.getAttribute('data-key'), type = ctl.getAttribute('data-type');
      if (!Object.prototype.hasOwnProperty.call(v, key)) return;
      if (type === 'seg') {
        ctl.querySelectorAll('[data-value]').forEach(function (b) {
          var on = b.getAttribute('data-value') === String(v[key]);
          b.classList.toggle('is-active', on);
          b.setAttribute('aria-pressed', String(on));
        });
      } else if (type === 'switch') ctl.querySelector('input').checked = !!v[key];
      else ctl.querySelector('select, input').value = v[key];
    });
  }

  function playDefaults(def) {
    var v = {};
    def.controls.forEach(function (c) { v[c.key] = c.type === 'switch' ? !!c.value : c.value == null ? '' : c.value; });
    return v;
  }

  // Only what differs from the defaults goes in the link, as ?play-<id>=key:value,key:value.
  function savePlay(p) {
    var v = playValues(p.box), d = playDefaults(p.def), parts = [];
    Object.keys(v).forEach(function (k) { if (v[k] !== d[k]) parts.push(k + ':' + encodeURIComponent(v[k] === true ? '1' : v[k] === false ? '0' : v[k])); });
    var url = new URLSearchParams(location.search);
    if (parts.length) url.set('play-' + p.def.id, parts.join(',')); else url.delete('play-' + p.def.id);
    var s = url.toString();
    try { history.replaceState(history.state, '', location.pathname + (s ? '?' + s : '') + location.hash); } catch (e) { /* a file:// page */ }
  }

  function linkedValues(def) {
    var raw = q.get('play-' + def.id), v = {};
    if (!raw) return null;
    raw.split(',').forEach(function (pair) {
      var i = pair.indexOf(':');
      if (i < 1) return;
      var key = pair.slice(0, i), val = decodeURIComponent(pair.slice(i + 1));
      def.controls.forEach(function (c) { if (c.key === key) v[key] = c.type === 'switch' ? val === '1' : val; });
    });
    return v;
  }

  function renderPlay(p) {
    var def = p.def, box = p.box;
    var s = SiteCode.state(def, playValues(box), root.getAttribute('data-site-mode') === 'simple');
    var off = def.disable ? def.disable(s) : [];
    box.querySelectorAll('.site-play-ctl').forEach(function (ctl) {
      var dis = off.indexOf(ctl.getAttribute('data-key')) !== -1;
      ctl.classList.toggle('is-off', dis);
      ctl.querySelectorAll('button, input, select').forEach(function (el) { el.disabled = dis; });
    });
    var markup = def.html(s);
    p.view.classList.toggle('site-play-view--wide', SiteCode.wide(def, s));
    p.view.innerHTML = markup;
    if (window.Insiyab && Insiyab.init) Insiyab.init(p.view);
    if (def.stage) def.stage(p.view, s);
    box.querySelector('.dx-code code').innerHTML = SiteCode.highlight(SiteCode.forListing(markup), 'html');
    p.notes.innerHTML = (def.notes ? def.notes(s) : []).map(function (n) { return '<p class="site-play-note">' + SiteCode.prose(n) + '</p>'; }).join('');
    fitPlay(p);
    fitSoon(p);
  }

  // Room at the preview's foot for whatever reaches past it (an open menu) and for the notes that share its cell on a wide screen.
  function fitPlay(p) {
    var v = p.view;
    v.style.minBlockSize = v.style.paddingBlockEnd = '';
    var box = v.getBoundingClientRect(), low = -Infinity, pad = parseFloat(getComputedStyle(v).paddingBlockEnd) || 0;
    (function walk(el) {
      for (var c = el.firstElementChild; c; c = c.nextElementSibling) {
        var cs = getComputedStyle(c);
        // A modal, a pop-up in the top layer or anything fixed to the screen floats above the page and takes no room in the preview.
        if (cs.position === 'fixed' || c.matches(':modal, :popover-open')) continue;
        var r = c.getBoundingClientRect();
        if (r.height && r.bottom > low) low = r.bottom;
        if (cs.overflowY === 'visible' && cs.overflowX !== 'hidden') walk(c);
      }
    })(v);
    var overlay = p.notes.offsetHeight && getComputedStyle(p.notes).gridRowStart !== 'auto' ? p.notes.offsetHeight + 8 : 0;
    var short = low + overlay + 16 - box.bottom;
    // Height and foot padding grow together, so the centred component keeps its place.
    if (short > 0) { v.style.minBlockSize = Math.ceil(box.height + short) + 'px'; v.style.paddingBlockEnd = Math.ceil(pad + short) + 'px'; }
  }

  // Again two frames later, once the library has placed what just opened (it does so in its own toggle handler).
  function fitSoon(p) { requestAnimationFrame(function () { requestAnimationFrame(function () { fitPlay(p); }); }); }

  function refreshPlays() { plays.forEach(renderPlay); }

  // A Date as the day it stands for, whether made at local or UTC midnight (JSON would often print the day before), a File by its name.
  function eventValue(v) {
    if (v && v.nodeType) return v.tagName.toLowerCase() + (v.classList[0] ? '.' + v.classList[0] : '');
    if (v instanceof Date) {
      var two = function (n) { return (n < 10 ? '0' : '') + n; };
      if (!v.getUTCHours() && !v.getUTCMinutes()) return v.toISOString().slice(0, 10);
      var day = v.getFullYear() + '-' + two(v.getMonth() + 1) + '-' + two(v.getDate());
      return day + (v.getHours() || v.getMinutes() ? ' ' + two(v.getHours()) + ':' + two(v.getMinutes()) : '');
    }
    if (typeof File !== 'undefined' && v instanceof File) return 'File ' + JSON.stringify(v.name);
    return JSON.stringify(v);
  }

  function bootPlays() {
    if (!window.SiteCode) return;
    document.querySelectorAll('.site-play[data-play]').forEach(function (box) {
      var def = SitePlay.defs[box.getAttribute('data-play')];
      if (!def) return;
      var p = { def: def, box: box, view: box.querySelector('.site-play-view'), notes: box.querySelector('.site-play-notes') };
      plays.push(p);
      var change = function () { renderPlay(p); savePlay(p); };
      box.addEventListener('click', function (e) {
        var seg = e.target.closest('.site-play-ctl [data-value]');
        if (seg) {
          seg.parentNode.querySelectorAll('[data-value]').forEach(function (b) { b.setAttribute('aria-pressed', String(b === seg)); });
          change();
          return;
        }
        if (e.target.closest('[data-play-reset]')) { setPlayValues(box, playDefaults(def)); change(); return; }
        if (e.target.closest('.site-play-view a[href]')) e.preventDefault();
      });
      box.addEventListener('change', function (e) { if (e.target.closest('.site-play-controls')) change(); });
      p.view.addEventListener('toggle', function () { fitSoon(p); }, true);
      box.addEventListener('input', function (e) { if (e.target.matches('.site-play-ctl input[type="text"]')) change(); });
      // What the component announces shows under it, so its events can be seen as well as read about.
      (def.events || []).forEach(function (name) {
        document.addEventListener(name, function (e) {
          var d = e.detail || {}, el = d.el || d.target || d.input || e.target;
          if (!el || !el.nodeType || !p.view.contains(el)) return;
          var said = Object.keys(d).filter(function (k) { return k !== 'el'; }).map(function (k) {
            return '`' + k + ': ' + eventValue(d[k]) + '`';
          }).join('، ');
          var line = p.notes.querySelector('.site-play-event') || p.notes.appendChild(document.createElement('p'));
          line.className = 'site-play-note site-play-event';
          line.innerHTML = SiteCode.prose('حدث `' + name + '`' + (said ? '، ومعه ' + said : ''));
          fitPlay(p);
        });
      });
      var linked = linkedValues(def);
      if (linked) setPlayValues(box, linked);
      if (linked || def.stage) renderPlay(p); else fitPlay(p);
    });
    var fitAll = function () { plays.forEach(fitPlay); }, queued = 0;
    window.addEventListener('resize', function () { if (!queued) queued = requestAnimationFrame(function () { queued = 0; fitAll(); }); });
    if (document.fonts && document.fonts.ready) document.fonts.ready.then(fitAll);
  }

  function copy(text) {
    if (navigator.clipboard && window.isSecureContext) return navigator.clipboard.writeText(text);
    return new Promise(function (resolve, reject) {
      var area = document.createElement('textarea');
      area.value = text;
      area.setAttribute('readonly', '');
      area.style.position = 'fixed';
      area.style.opacity = '0';
      document.body.appendChild(area);
      area.select();
      var ok = false;
      try { ok = document.execCommand('copy'); } catch (e) { /* reported below */ }
      area.remove();
      if (ok) resolve(); else reject(new Error('copy failed'));
    });
  }

  /* The sidebar's scroll is the library's job now: insiyab.js keeps it where it was
     between pages and brings the active link into view on a fresh visit, both before
     first paint. This file used to centre the active link on DOMContentLoaded, which
     was after the first paint — the sidebar flashed at the top and then jumped. */

  // The page frosts over while the sides swap, and the new one opens from its start edge like a book.
  function flipDir(dir) {
    if (dir === root.getAttribute('dir')) return;
    var still = root.getAttribute('data-ins-fx') === 'off' || !window.matchMedia || matchMedia('(prefers-reduced-motion: reduce)').matches;
    if (!document.startViewTransition || still) { applyDir(dir); return; }
    root.style.setProperty('--site-flip-edge', dir === 'ltr' ? 'left' : 'right');
    root.style.setProperty('--site-flip-turn', dir === 'ltr' ? '28deg' : '-28deg');
    root.classList.add('site-dir-flip');
    var clean = function () {
      root.classList.remove('site-dir-flip');
      root.style.removeProperty('--site-flip-edge');
      root.style.removeProperty('--site-flip-turn');
    };
    try { document.startViewTransition(function () { applyDir(dir); syncCustomiser(); }).finished.then(clean, clean); }
    catch (e) { clean(); applyDir(dir); }
  }

  function ready() {
    if (q.get('theme')) Insiyab.theme(q.get('theme'));
    if (q.get('collapsed')) Insiyab.sidebar('collapsed');
    syncCustomiser();
    bootPlays();
    revealTarget();
    window.addEventListener('hashchange', revealTarget);

    document.addEventListener('click', function (e) {
      var sw = e.target.closest('[data-brand]');
      if (sw) {
        var hex = sw.getAttribute('data-brand');
        Insiyab.brand(hex);
        write(KEY_BRAND, hex);
        syncCustomiser();
        return;
      }

      var d = e.target.closest('.site-dir [data-dir]');
      if (d) {
        flipDir(d.getAttribute('data-dir'));
        write(KEY_DIR, d.getAttribute('data-dir') === 'rtl' ? null : d.getAttribute('data-dir'));
        syncCustomiser();
        return;
      }

      var md = e.target.closest('.site-mode [data-mode]');
      if (md) {
        write(KEY_MODE, md.getAttribute('data-mode') === 'simple' ? null : 'advanced');
        setMode(md.getAttribute('data-mode'));
        return;
      }

      if (e.target.closest('[data-site-reset]')) {
        write(KEY_BRAND, null);
        write(KEY_DIR, null);
        write(KEY_MODE, null);
        Insiyab.theme('system');
        location.reload();
        return;
      }

      var btn = e.target.closest('.dx-copy');
      if (btn) {
        var code = btn.parentNode.querySelector('pre');
        copy(code.innerText.replace(/\n$/, '')).then(function () {
          btn.textContent = 'نُسخ';
          btn.classList.add('is-done');
          setTimeout(function () { btn.textContent = 'نسخ'; btn.classList.remove('is-done'); }, 1400);
        }, function () {
          Insiyab.toast('تعذر النسخ، فحدد الشيفرة وانسخها بنفسك', 'warn');
        });
      }
    });

    /* Search is the command palette plugin, shortcuts and all: see layout.html. */

    if (q.get('open')) Insiyab.dialog('#' + q.get('open'), 'open');
    if (q.get('toast')) Insiyab.toast('تم الحفظ بنجاح', 'ok');
  }

  if (document.readyState === 'loading') document.addEventListener('DOMContentLoaded', ready);
  else ready();
})();
