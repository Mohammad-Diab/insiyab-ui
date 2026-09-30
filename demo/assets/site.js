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

  // What the new mode drops leaves first, then what it adds arrives.
  function setMode(next) {
    running.forEach(function (a) { a.finish(); });
    running = [];
    var me = ++turn;
    if (next === root.getAttribute('data-site-mode')) { syncCustomiser(); return; }
    var apply = function () { root.setAttribute('data-site-mode', next); syncCustomiser(); };
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
    t.scrollIntoView();
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
