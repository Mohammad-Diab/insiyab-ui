/* ==========================================================================
   Insiyab · documentation site behaviour
   ==========================================================================

   Loaded in <head> straight after insiyab.js, for the same reason the library
   is: the viewer's chosen brand colour and direction are restored before first
   paint, so moving between pages never flashes the default teal or the wrong
   side for the sidebar.

   None of this is library code. The components on every page need none of it.

   Query parameters, for a screenshot harness that can navigate but not click:
     ?theme=dark|light  ?brand=%230F766E  ?dir=ltr  ?fx=off  ?collapsed=1
     ?open=<dialog id>  ?toast=1
   ========================================================================== */

(function () {
  'use strict';

  var KEY_BRAND = 'insiyab-demo-brand';
  var KEY_DIR = 'insiyab-demo-dir';
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
  if (brand) Insiyab.brand(brand);
  if (q.get('theme')) Insiyab.theme(q.get('theme'));
  if (q.get('fx') === 'off') root.setAttribute('data-ins-fx', 'off');
  if (q.get('collapsed')) Insiyab.sidebar('collapsed');

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

  function resetSearch() {
    var combo = document.getElementById('site-search-combo');
    if (!combo) return;
    var input = combo.querySelector('input');
    input.value = '';
    combo.querySelectorAll('.ins-combo-option').forEach(function (o) { o.hidden = false; o.classList.remove('is-active'); });
    var empty = combo.querySelector('.ins-combo-empty');
    if (empty) empty.hidden = true;
  }

  function ready() {
    syncCustomiser();
    resetSearch();

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
        applyDir(d.getAttribute('data-dir'));
        write(KEY_DIR, d.getAttribute('data-dir') === 'rtl' ? null : d.getAttribute('data-dir'));
        syncCustomiser();
        return;
      }

      if (e.target.closest('[data-site-reset]')) {
        write(KEY_BRAND, null);
        write(KEY_DIR, null);
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
          Insiyab.toast('تعذّر النسخ — حدّد الشيفرة وانسخها يدويًا', 'warn');
        });
      }
    });

    /* Search: Ctrl+K anywhere, or "/" when not typing. */
    document.addEventListener('keydown', function (e) {
      var typing = /^(INPUT|TEXTAREA|SELECT)$/.test(e.target.tagName) || e.target.isContentEditable;
      if (((e.ctrlKey || e.metaKey) && e.key.toLowerCase() === 'k') || (e.key === '/' && !typing)) {
        e.preventDefault();
        Insiyab.dialog('#site-search', 'open');
      }
    });

    var dlg = document.getElementById('site-search');
    if (dlg) dlg.addEventListener('close', resetSearch);

    document.addEventListener('ins:combo', function (e) {
      if (e.detail.el && e.detail.el.id === 'site-search-combo') location.href = e.detail.value;
    });

    if (q.get('open')) Insiyab.dialog('#' + q.get('open'), 'open');
    if (q.get('toast')) Insiyab.toast('تمّ الحفظ بنجاح', 'ok');
  }

  if (document.readyState === 'loading') document.addEventListener('DOMContentLoaded', ready);
  else ready();
})();
