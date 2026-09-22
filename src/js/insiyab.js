/* ==========================================================================
   Insiyab · insiyab.js
   ==========================================================================

   One file, one tag, no init call:

     <script src="insiyab.js"></script>

   **Put it in <head>, without `defer`.** The theme and the sidebar state are
   restored from localStorage, and that has to land *before first paint* or every
   page load flashes the wrong theme. So the top half of this file runs the moment
   the tag is parsed — before <body> exists — and stamps <html>. Everything that
   needs actual DOM waits for DOMContentLoaded. Loading it deferred or at the end
   of <body> still works; you just get the flash back.

   Events are **delegated** from the document, not bound per element. That is why
   there is no init call to forget: a `data-ins-*` button added to the page an hour
   later works with no help. `Insiyab.init()` exists only for the few things that
   build DOM rather than listen to it.

   No dependencies, no build, no module system. It defines one global, `Insiyab`.
   ========================================================================== */

(function (window, document) {
  'use strict';

  var VERSION = '0.1.0';
  var root = document.documentElement;

  var KEY_THEME = 'ins-theme';
  var KEY_SIDEBAR = 'ins-sidebar';

  /* ------------------------------------------------------------- storage */
  /* Every access is wrapped, and that is not defensive habit: reading
     localStorage *throws* in a private window with site data blocked, and inside
     a sandboxed iframe with no allow-same-origin. An uncaught throw up here,
     before <body> exists, takes the whole library down on page one. */

  function read(key) {
    try { return window.localStorage.getItem(key); } catch (e) { return null; }
  }
  function write(key, value) {
    try { window.localStorage.setItem(key, value); } catch (e) { /* not fatal */ }
  }
  function drop(key) {
    try { window.localStorage.removeItem(key); } catch (e) { /* not fatal */ }
  }

  /* --------------------------------------------------------------- colour */
  /* The brand derivation, ported from the server that first did it, so one hex in
     gives the whole tinted set out. Contrast is *measured* here, not guessed —
     which is the only reason a pale brand colour cannot configure an unreadable
     button. See the notes on each step. */

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

  /* Toward black when amount < 0, toward white when amount > 0. */
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

  /* The label on a primary-filled button: white, or near-black when the fill is
     pale. Hardcoding white is a real failure for a mid-tone brand — white on a
     mid gold is about 2.4:1, illegible — so take whichever measures better.
     Measured against the primary alone, not the gradient's midpoint: "legible on
     average" is not a standard. Both ends are gradientEnd's problem, below. */
  function labelFor(hex) {
    var rgb = hexToRgb(hex);
    if (!rgb) return '#ffffff';
    return contrast(rgb, WHITE) >= contrast(rgb, NEAR_BLACK) ? '#ffffff' : '#111111';
  }

  /* The gradient's second stop: same hue, shifted *away from the label*.

     Why direction rather than a fixed -32%. A pale brand takes a near-black
     label, and darkening a pale fill walks it straight at that label — 32% darker
     carries near-black at barely 4.7:1, and pulling the shift in far enough to be
     safe leaves a gradient too flat to see. Shifting the other way is free:
     lightening a fill only ever *raises* its contrast with a dark label, so the
     whole fill carries the label by construction rather than by measurement, and
     the gradient keeps its depth.

     The invariant that makes this sound: labelFor takes the better of white and
     near-black, and the worst case for any colour whatsoever is ~4.5:1 (around
     #767676, where the two are equal). So a primary always carries its own label
     at AA, and a stop shifted away from that label can only be better. */
  function gradientEnd(hex, label) {
    return label === '#ffffff' ? shift(hex, -0.32) : shift(hex, 0.30);
  }

  /* One hex in, the whole set out. `theme` decides which way the readable shade
     and the page washes go — on dark they move toward white, not toward black,
     and forgetting that is how a brand-tinted foreground ends up invisible. */
  function derive(hex, theme) {
    var dark = theme === 'dark';
    var onPrimary = labelFor(hex);
    var primaryDark = gradientEnd(hex, onPrimary);
    return {
      'primary': hex,
      'primary-dark': primaryDark,
      'primary-darker': shift(hex, dark ? 0.55 : -0.55),
      'primary-rgb': hexToRgb(hex).join(', '),
      'grad-rgb': hexToRgb(primaryDark).join(', '),
      'accent-rgb': hexToRgb(primaryDark).join(', '),
      'on-primary': onPrimary,
      /* Dark gets its depth from the orbs, not from a tinted ground, so the washes
         are near-black constants rather than a wash of the hue. */
      'wash-1': dark ? '#0f1115' : shift(hex, 0.93),
      'wash-2': dark ? '#12141a' : shift(primaryDark, 0.94)
    };
  }

  var brandCurrent = null;   /* remembered, so a theme change can re-derive */

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

  /* ---------------------------------------------------------------- theme */
  /* Three states, not two. An explicit choice is stored and stamped as
     `data-ins-theme`; "system" stores nothing and stamps nothing, which leaves the
     `prefers-color-scheme` block in the stylesheet in charge. That is why the
     absence of the attribute is meaningful and must not be replaced with
     `data-ins-theme="light"`. */

  function systemPrefersDark() {
    return !!(window.matchMedia && window.matchMedia('(prefers-color-scheme: dark)').matches);
  }

  /* What the user is actually looking at, which is the question callers mean. */
  function effectiveTheme() {
    var set = root.getAttribute('data-ins-theme');
    if (set === 'dark' || set === 'light') return set;
    return systemPrefersDark() ? 'dark' : 'light';
  }

  function stampTheme(mode) {
    if (mode === 'system') root.removeAttribute('data-ins-theme');
    else root.setAttribute('data-ins-theme', mode);
    /* A brand colour set by hand has per-theme values, so it has to be re-derived
       when the theme moves under it — otherwise the readable shade keeps pointing
       the wrong way and the page looks almost right. */
    if (brandCurrent) brand(brandCurrent);
  }

  function theme(next) {
    if (next === undefined) return effectiveTheme();
    if (next !== 'dark' && next !== 'light' && next !== 'system') {
      if (window.console) window.console.warn('[insiyab] theme(): expected dark, light or system:', next);
      return effectiveTheme();
    }
    if (next === 'system') drop(KEY_THEME); else write(KEY_THEME, next);
    stampTheme(next);
    emit('ins:theme', { theme: effectiveTheme(), mode: next });
    return effectiveTheme();
  }

  function toggleTheme() {
    return theme(effectiveTheme() === 'dark' ? 'light' : 'dark');
  }

  /* -------------------------------------------------------------- sidebar */

  function sidebar(next) {
    var collapsed = root.classList.contains('ins-sidebar-collapsed');
    if (next === undefined) return collapsed ? 'collapsed' : 'open';
    var want = next === 'collapsed' || next === true ||
               (next === 'toggle' && !collapsed);
    root.classList.toggle('ins-sidebar-collapsed', want);
    if (want) write(KEY_SIDEBAR, 'collapsed'); else drop(KEY_SIDEBAR);
    emit('ins:sidebar', { state: want ? 'collapsed' : 'open' });
    return want ? 'collapsed' : 'open';
  }

  /* --------------------------------------------------------------- events */

  function emit(name, detail) {
    var event;
    try {
      event = new CustomEvent(name, { detail: detail, bubbles: true });
    } catch (e) {                                  /* very old WebView */
      event = document.createEvent('CustomEvent');
      event.initCustomEvent(name, true, false, detail);
    }
    document.dispatchEvent(event);
  }

  /* --------------------------------------------------------- the scroller */
  /* Which element actually scrolls depends on `data-ins-scrollbar="lead"` (see
     04-reset.css): with it, <body> is the scroll container and its `scroll` event
     never reaches `window`. Anything measuring scroll has to ask this, not
     `window.scrollY`. */

  function scroller() {
    return root.getAttribute('data-ins-scrollbar') === 'lead' ? document.body : window;
  }

  function scrollTop() {
    var el = scroller();
    return el === window ? (window.scrollY || root.scrollTop || 0) : el.scrollTop;
  }

  /* Stamped past 8px so a bar can go from transparent to frosted. Without JS the
     bar simply stays transparent, which is a working page, not a broken one. */
  function watchScroll() {
    var ticking = false;
    function measure() {
      ticking = false;
      root.toggleAttribute('data-ins-scrolled', scrollTop() > 8);
    }
    function onScroll() {
      if (ticking) return;
      ticking = true;
      window.requestAnimationFrame(measure);
    }
    var target = scroller();
    target.addEventListener('scroll', onScroll, { passive: true });
    measure();
  }

  /* ==========================================================================
     PRE-PAINT — runs now, at parse time, with no <body> to speak of
     ========================================================================== */

  (function prePaint() {
    var stored = read(KEY_THEME);
    if (stored === 'dark' || stored === 'light') stampTheme(stored);

    if (read(KEY_SIDEBAR) === 'collapsed') root.classList.add('ins-sidebar-collapsed');

    /* `data-ins-primary` on <html> is the declarative form of brand(): it is read
       here so a custom brand colour is live for the first paint too, rather than
       repainting the whole page a moment after it appears. */
    var declared = root.getAttribute('data-ins-primary');
    if (declared) brand(declared);

    /* Follow the OS while the user has expressed no preference of their own. */
    if (window.matchMedia) {
      var mq = window.matchMedia('(prefers-color-scheme: dark)');
      var onChange = function () {
        if (read(KEY_THEME)) return;              /* an explicit choice wins */
        if (brandCurrent) brand(brandCurrent);    /* re-derive for the new ground */
        emit('ins:theme', { theme: effectiveTheme(), mode: 'system' });
      };
      if (mq.addEventListener) mq.addEventListener('change', onChange);
      else if (mq.addListener) mq.addListener(onChange);
    }
  }());

  /* ==========================================================================
     DELEGATION — one listener, so nothing needs wiring up
     ========================================================================== */

  document.addEventListener('click', function (event) {
    var el = event.target.closest ? event.target.closest('[data-ins-theme-toggle], [data-ins-sidebar]') : null;
    if (!el) return;

    if (el.hasAttribute('data-ins-theme-toggle')) {
      /* An explicit value sets that theme; a bare attribute toggles. */
      var want = el.getAttribute('data-ins-theme-toggle');
      var now = want ? theme(want) : toggleTheme();
      /* Only a real toggle carries pressed state; a set-to-dark button is not a
         two-state control and must not claim to be one. */
      if (!want) el.setAttribute('aria-pressed', String(now === 'dark'));
      if (el.tagName === 'A') event.preventDefault();
      return;
    }

    if (el.hasAttribute('data-ins-sidebar')) {
      sidebar(el.getAttribute('data-ins-sidebar') || 'toggle');
      if (el.tagName === 'A') event.preventDefault();
    }
  }, false);

  /* ==========================================================================
     INIT — only for the things that BUILD DOM rather than listen to it
     ========================================================================== */

  var builders = [];

  function define(name, fn) { builders.push({ name: name, fn: fn }); }

  function init(scope) {
    var where = scope || document;
    for (var i = 0; i < builders.length; i++) {
      try {
        builders[i].fn(where);
      } catch (e) {
        /* One broken builder must not stop the rest, and must not be silent. */
        if (window.console) window.console.error('[insiyab] ' + builders[i].name, e);
      }
    }
    return where;
  }

  /* The orbs. Injected so a two-tag page gets the full ground for free; write the
     markup yourself if you are CSS-only, or opt out with data-ins-orbs="off". */
  define('orbs', function (scope) {
    if (scope !== document) return;                       /* page furniture, once */
    if (root.getAttribute('data-ins-orbs') === 'off') return;
    if (document.querySelector('.ins-orbs')) return;      /* hand-written, respect it */
    var layer = document.createElement('div');
    layer.className = 'ins-orbs';
    layer.setAttribute('aria-hidden', 'true');
    for (var n = 1; n <= 3; n++) {
      var orb = document.createElement('div');
      orb.className = 'ins-orb ins-orb-' + n;
      layer.appendChild(orb);
    }
    document.body.insertBefore(layer, document.body.firstChild);
  });

  /* Give every bare toggle its initial pressed state, so a screen reader is not
     told the page is in light mode when it is in dark. */
  define('theme-toggle-state', function (scope) {
    var dark = effectiveTheme() === 'dark';
    var nodes = scope.querySelectorAll('[data-ins-theme-toggle=""], [data-ins-theme-toggle]:not([data-ins-theme-toggle="dark"]):not([data-ins-theme-toggle="light"]):not([data-ins-theme-toggle="system"])');
    for (var i = 0; i < nodes.length; i++) nodes[i].setAttribute('aria-pressed', String(dark));
  });

  function ready() {
    init(document);
    watchScroll();
  }

  /* Loaded in <head> as recommended: wait. Loaded late, or injected: run now. */
  if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', ready);
  } else {
    ready();
  }

  /* ==========================================================================
     PUBLIC API — the escape hatch, not the front door
     ========================================================================== */

  window.Insiyab = {
    version: VERSION,
    init: init,
    define: define,
    theme: theme,
    toggleTheme: toggleTheme,
    sidebar: sidebar,
    brand: brand,
    /* Exposed because they are genuinely useful on their own, and because the
       contrast maths is the part nobody should have to write twice. */
    color: {
      derive: derive,
      shift: shift,
      contrast: function (a, b) {
        var x = hexToRgb(a), y = hexToRgb(b);
        return (x && y) ? contrast(x, y) : null;
      },
      labelFor: labelFor
    },
    scrollTop: scrollTop
  };
}(window, document));
