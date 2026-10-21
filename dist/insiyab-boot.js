/*! Insiyab UI v0.7.0 · https://github.com/Mohammad-Diab/insiyab-ui#readme · Apache-2.0 (fonts: OFL 1.1, see fonts/LICENSE-*.txt) */
(function (window, document) {
  'use strict';
  var VERSION = '0.7.0';
  var root = document.documentElement;
  var KEY_THEME = 'ins-theme';
  var KEY_SIDEBAR = 'ins-sidebar';
  function read(key) {
    try { return window.localStorage.getItem(key); } catch (e) { return null; }
  }
  function write(key, value) {
    try { window.localStorage.setItem(key, value); } catch (e) {  }
  }
  function drop(key) {
    try { window.localStorage.removeItem(key); } catch (e) {  }
  }
  function hexToRgb(hex) {
    if (typeof hex !== 'string') return null;
    var v = hex.trim().replace(/^#/, '');
    if (v.length === 3) v = v[0] + v[0] + v[1] + v[1] + v[2] + v[2];
    if (!/^[0-9a-fA-F]{6}$/.test(v)) return null;
    return [parseInt(v.slice(0, 2), 16), parseInt(v.slice(2, 4), 16), parseInt(v.slice(4, 6), 16)];
  }
  function toHex(rgb) {
    return '#' + rgb.map(function (c) {
      var s = Math.max(0, Math.min(255, c)).toString(16);
      return s.length === 1 ? '0' + s : s;
    }).join('');
  }
  function shift(hex, amount) {
    var rgb = hexToRgb(hex);
    if (!rgb) return hex;
    return toHex(rgb.map(function (c) {
      return amount < 0 ? Math.round(c * (1 + amount)) : Math.round(c + (255 - c) * amount);
    }));
  }
  function luminance(rgb) {
    var ch = rgb.map(function (raw) {
      var c = raw / 255;
      return c <= 0.03928 ? c / 12.92 : Math.pow((c + 0.055) / 1.055, 2.4);
    });
    return 0.2126 * ch[0] + 0.7152 * ch[1] + 0.0722 * ch[2];
  }
  function contrast(a, b) {
    var la = luminance(a), lb = luminance(b);
    return (Math.max(la, lb) + 0.05) / (Math.min(la, lb) + 0.05);
  }
  var WHITE = [255, 255, 255], NEAR_BLACK = [17, 17, 17];
  function labelFor(hex) {
    var rgb = hexToRgb(hex);
    if (!rgb) return '#ffffff';
    return contrast(rgb, WHITE) >= contrast(rgb, NEAR_BLACK) ? '#ffffff' : '#111111';
  }
  function gradientEnd(hex, label) {
    return label === '#ffffff' ? shift(hex, -0.32) : shift(hex, 0.30);
  }
  // The middle shade small brand accents take, as a shift toward white per theme; big fills keep the primary.
  var MID_SHIFT = { light: 0.20, dark: 0.45 };
  function derive(hex, theme) {
    var dark = theme === 'dark';
    var onPrimary = labelFor(hex);
    var primaryDark = gradientEnd(hex, onPrimary);
    var primaryMid = shift(hex, dark ? MID_SHIFT.dark : MID_SHIFT.light);
    return {
      'primary': hex,
      'primary-dark': primaryDark,
      'primary-darker': shift(hex, dark ? 0.55 : -0.55),
      'primary-rgb': hexToRgb(hex).join(', '),
      'grad-rgb': hexToRgb(primaryDark).join(', '),
      'accent-rgb': hexToRgb(primaryDark).join(', '),
      'on-primary': onPrimary,
      'primary-mid': primaryMid,
      'on-primary-mid': labelFor(primaryMid),
      'wash-1': dark ? '#0f1115' : shift(hex, 0.93),
      'wash-2': dark ? '#12141a' : shift(primaryDark, 0.94)
    };
  }
  var brandCurrent = null;   
  function brand(hex) {
    if (hex === undefined) return brandCurrent;
    if (!hexToRgb(hex)) {
      if (window.console) window.console.warn('[insiyab] brand(): not a hex colour:', hex);
      return brandCurrent;
    }
    brandCurrent = hex;
    var tokens = derive(hex, effectiveTheme());
    for (var name in tokens) {
      if (Object.prototype.hasOwnProperty.call(tokens, name)) {
        root.style.setProperty('--ins-' + name, tokens[name]);
      }
    }
    return hex;
  }
  function systemPrefersDark() {
    return !!(window.matchMedia && window.matchMedia('(prefers-color-scheme: dark)').matches);
  }
  function effectiveTheme() {
    var set = root.getAttribute('data-ins-theme');
    if (set === 'dark' || set === 'light') return set;
    return systemPrefersDark() ? 'dark' : 'light';
  }
  function stampTheme(mode) {
    if (mode === 'system') root.removeAttribute('data-ins-theme');
    else root.setAttribute('data-ins-theme', mode);
    if (brandCurrent) brand(brandCurrent);
  }
  function motionless() {
    if (root.getAttribute('data-ins-fx') === 'off') return true;
    try {
      return !!(window.matchMedia && window.matchMedia('(prefers-reduced-motion: reduce)').matches);
    } catch (e) {
      return false;
    }
  }
  var NAV_KEY = 'ins-nav-from';
  var NAV_EASE_OUT = 'cubic-bezier(.9, .1, 1, .2)';
  var NAV_EASE_IN = 'cubic-bezier(.1, .9, .2, 1)';
  function navActive(side) { return side.querySelector('.ins-shell-link.is-active'); }
  function navNote() {
    var note = null;
    try { note = JSON.parse(window.sessionStorage.getItem(NAV_KEY) || 'null'); } catch (e) { return null; }
    return note && Date.now() - note.t < 5000 ? note : null;
  }
  function navArrived() { root.classList.remove('ins-nav-arriving'); }
  function navPlace(side, note, state) {
    if (state.done) return;
    if (note && typeof note.top === 'number') {
      side.scrollTop = note.top;
      if (Math.abs(side.scrollTop - note.top) < 1) state.done = true;
      return;
    }
    var link = navActive(side);
    if (!link) return;
    var bottom = link.offsetTop + link.offsetHeight;
    if (bottom > side.clientHeight) side.scrollTop = link.offsetTop - side.clientHeight / 2 + link.offsetHeight / 2;
    state.done = true;
  }
  function navWatch(note) {
    var state = { done: false }, side = null;
    var place = function () {
      side = side || document.querySelector('.ins-shell-side');
      if (side) { filterBuild(side); navPlace(side, note, state); }
    };
    var finish = function () { place(); state.done = true; if (mo) mo.disconnect(); };
    var mo = window.MutationObserver ? new MutationObserver(function () {
      place();
      if (state.done || (side && side.nextElementSibling)) finish();
    }) : null;
    if (mo) mo.observe(root, { childList: true, subtree: true });
    document.addEventListener('DOMContentLoaded', finish);
  }
  var FILTER_WORDS = { ar: ['بحث', 'لا نتائج'], en: ['Search', 'No results'] };
  function filterBuild(side) {
    if (!side.hasAttribute('data-ins-filter')) return null;
    var box = side.querySelector('.ins-shell-filter');
    if (box) return box;
    var first = side.querySelector('.ins-shell-group, .ins-shell-link');
    if (!first) return null;
    var lang = ((side.closest('[lang]') || root).getAttribute('lang') || 'en').slice(0, 2).toLowerCase();
    var words = FILTER_WORDS[lang] || FILTER_WORDS.en, hint = side.getAttribute('data-ins-filter') || words[0];
    box = document.createElement('div');
    box.className = 'ins-shell-filter ins-search';
    box.innerHTML = '<svg class="ins-search-ico" viewBox="0 0 24 24" aria-hidden="true" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round">' +
      '<circle cx="11" cy="11" r="7"/><path d="m20 20-3.5-3.5"/></svg><input class="ins-input ins-input--sm" type="search" autocomplete="off" spellcheck="false">';
    var input = box.lastChild;
    input.placeholder = hint;
    input.setAttribute('aria-label', hint);
    var empty = document.createElement('p');
    empty.className = 'ins-shell-filter-empty';
    empty.setAttribute('role', 'status');
    empty.hidden = true;
    empty.textContent = side.getAttribute('data-ins-filter-empty') || words[1];
    first.parentNode.insertBefore(box, first);
    first.parentNode.insertBefore(empty, first);
    return box;
  }
  function earlyPaint(booted) {
    root.setAttribute('data-ins-js', '');
    var stored = read(KEY_THEME);
    if (stored === 'dark' || stored === 'light') stampTheme(stored);
    if (read(KEY_SIDEBAR) === 'collapsed') root.classList.add('ins-sidebar-collapsed');
    var note = booted ? null : navNote();
    if (note && note.from && !motionless()) {
      root.classList.add('ins-nav-arriving');
      window.setTimeout(navArrived, 2000);
    }
    if (!booted && document.readyState === 'loading') navWatch(note);
    var declared = root.getAttribute('data-ins-primary');
    if (declared) brand(declared);
  }
  earlyPaint(false);
  window.InsiyabBoot = { version: VERSION };
}(window, document));
