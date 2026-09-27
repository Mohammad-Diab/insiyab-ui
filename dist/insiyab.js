/*! Insiyab UI v0.6.0 · https://github.com/Mohammad-Diab/insiyab-ui#readme · MIT (fonts: OFL 1.1, see fonts/LICENSE-*.txt) */
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

  /* Stamped by build.mjs from package.json, which is the one place the version is
     written. It used to be typed here as well, and the two could drift; this file
     read on its own, unbuilt, reports 'dev'. */
  var VERSION = '0.6.0';
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
    pressToggles(document);
    emit('ins:theme', { theme: effectiveTheme(), mode: next });
    return effectiveTheme();
  }

  function pressToggles(scope) {
    var dark = effectiveTheme() === 'dark';
    var nodes = scope.querySelectorAll('[data-ins-theme-toggle=""], [data-ins-theme-toggle]:not([data-ins-theme-toggle="dark"]):not([data-ins-theme-toggle="light"]):not([data-ins-theme-toggle="system"])');
    for (var i = 0; i < nodes.length; i++) nodes[i].setAttribute('aria-pressed', String(dark));
  }

  var DAYNIGHT = '<span class="ins-dn-sky" aria-hidden="true">' +
    '<span class="ins-dn-star"></span><span class="ins-dn-star"></span><span class="ins-dn-star"></span>' +
    '<span class="ins-dn-cloud"></span><span class="ins-dn-orb"></span></span>';

  function toggleTheme() {
    return theme(effectiveTheme() === 'dark' ? 'light' : 'dark');
  }

  /* Show the repaint, over the whole page. Carried over from the USSD app, where it
     was designed.

     Flipping `data-ins-theme` is already the cheapest possible theme change — every
     colour is a custom property — but it is also invisible as a *change*: one frame,
     light page, dark page. The usual fix is a blanket `transition: background-color,
     color` on everything, which pays for a transition on every row of a 200-row table
     for the one moment in a session anybody switches, and still cannot animate a
     gradient, a shadow or a mask.

     A View Transition animates the page instead of its properties: the browser
     snapshots before and after, and 18-motion.css reveals the new snapshot through a
     circle growing out of the control that was pressed. Two snapshots and one
     clip-path, whatever is on screen.

     Everything about it is optional. No `startViewTransition`, reduced motion, or
     `data-ins-fx="off"`, and `apply()` is simply called — the instant flip. The theme
     change is inside the callback either way, so a failure here never leaves it
     half-applied. */
  function motionless() {
    if (root.getAttribute('data-ins-fx') === 'off') return true;
    try {
      return !!(window.matchMedia && window.matchMedia('(prefers-reduced-motion: reduce)').matches);
    } catch (e) {
      return false;
    }
  }

  function sweep(from, apply) {
    if (!document.startViewTransition || motionless()) { apply(); return; }

    var box = from && from.getBoundingClientRect ? from.getBoundingClientRect() : null;
    var x = box ? box.left + box.width / 2 : window.innerWidth / 2;
    var y = box ? box.top + box.height / 2 : 0;
    var w = window.innerWidth, h = window.innerHeight;
    /* To the furthest corner, or the old palette survives in one corner of the screen
       for the length of the animation. `sqrt` rather than `Math.hypot`, to stay ES5. */
    var far = Math.max(
      Math.sqrt(x * x + y * y),
      Math.sqrt((w - x) * (w - x) + y * y),
      Math.sqrt(x * x + (h - y) * (h - y)),
      Math.sqrt((w - x) * (w - x) + (h - y) * (h - y)));

    root.style.setProperty('--ins-sweep-x', x + 'px');
    root.style.setProperty('--ins-sweep-y', y + 'px');
    root.style.setProperty('--ins-sweep-r', Math.ceil(far) + 'px');
    root.classList.add('ins-theme-sweep');

    var clean = function () {
      root.classList.remove('ins-theme-sweep');
      root.style.removeProperty('--ins-sweep-x');
      root.style.removeProperty('--ins-sweep-y');
      root.style.removeProperty('--ins-sweep-r');
    };
    var run;
    try {
      run = document.startViewTransition(apply);
    } catch (e) {
      /* Refused to start — another transition mid-flight, a page being unloaded. The
         change still has to happen. */
      clean();
      apply();
      return;
    }
    /* `finished` rejects on a skipped transition, which is not worth having an opinion
       about: either way the class and the three properties come back off. */
    run.finished.then(clean, clean);
  }

  /* -------------------------------------------------------------- sidebar */
  /* The same button means two different things at two widths, and that is a
     property of the layout rather than a shortcut here: above 900px the sidebar is
     a column and the burger *collapses* it, below 900px it is an off-canvas drawer
     and the burger *opens* it. One control, because to the person pressing it there
     is only one sidebar.

     The drawer state is deliberately NOT persisted. A collapsed column is a working
     preference; a drawer that is still open when you come back is a page with its
     content covered. */

  function isDrawer() {
    return !!(window.matchMedia && window.matchMedia('(max-width: 900px)').matches);
  }

  function drawer() { return document.querySelector('.ins-shell-side'); }

  function sidebar(next) {
    var panel = drawer();

    if (isDrawer() && panel) {
      var open = panel.classList.contains('is-open');
      if (next === undefined) return open ? 'open' : 'closed';
      var show = next === 'open' || next === true || (next === 'toggle' && !open);
      panel.classList.toggle('is-open', show);
      /* `:target` opens it with no script at all, which is the point of building it
         that way — but a leftover hash would fight the class on the next toggle. */
      if (!show && location.hash === '#' + panel.id) {
        history.replaceState(null, '', location.pathname + location.search);
      }
      emit('ins:sidebar', { state: show ? 'open' : 'closed', drawer: true });
      return show ? 'open' : 'closed';
    }

    var collapsed = root.classList.contains('ins-sidebar-collapsed');
    if (next === undefined) return collapsed ? 'collapsed' : 'open';
    var want = next === 'collapsed' || next === true ||
               (next === 'toggle' && !collapsed);
    root.classList.toggle('ins-sidebar-collapsed', want);
    if (want) write(KEY_SIDEBAR, 'collapsed'); else drop(KEY_SIDEBAR);
    emit('ins:sidebar', { state: want ? 'collapsed' : 'open', drawer: false });
    return want ? 'collapsed' : 'open';
  }

  /* ---------------------------------------------------- sidebar selection */
  /* The active marker travels to a new item the way the Windows 10 navigation pane's
     does: in two beats, like an inchworm. For the first third its far end shoots out
     to the new item, accelerating, while the near end holds — the bar spans both
     items for an instant. For the rest, the near end catches up, decelerating, and
     the bar is its own size again on the new item. The two curves are the ones
     WinUI's NavigationView uses, and so is the 600ms.

     The marker is a pseudo-element inside each link, clipped by the link's own
     rounded box, so it cannot leave it. The journey is made by a stand-in: one bar,
     absolutely placed in the sidebar (positioned, and the scroller, so the bar
     scrolls and clips with the links), measured from the two real markers, and
     removed when it lands. The real markers are hidden while it flies.

     A sidebar on a server-rendered site is links, and a click loads the next page,
     where the old marker no longer exists. So the click writes down which item was
     selected, and the next page, finding the note, flies the bar from that item to
     its own. The note lasts one page load and five seconds — a stale one from some
     earlier visit must not replay a journey nobody just made. */
  var NAV_KEY = 'ins-nav-from';
  var NAV_EASE_OUT = 'cubic-bezier(.9, .1, 1, .2)';
  var NAV_EASE_IN = 'cubic-bezier(.1, .9, .2, 1)';

  function navActive(side) { return side.querySelector('.ins-shell-link.is-active'); }

  /* The previous page's note, if there is a fresh one: which item was selected
     (`from`, absent when none was) and how far the sidebar was scrolled (`top`).
     Read at parse time to hide the markers and place the sidebar before first
     paint, and again on arrival to fly the marker. */
  function navNote() {
    var note = null;
    try { note = JSON.parse(window.sessionStorage.getItem(NAV_KEY) || 'null'); } catch (e) { return null; }
    return note && Date.now() - note.t < 5000 ? note : null;
  }

  function navArrived() { root.classList.remove('ins-nav-arriving'); }

  /* Where the sidebar is scrolled to on arrival — decided before it is ever painted.
     Scrolling it after the page appeared is what made it flash: painted at the top,
     then jumped to wherever it belonged.

     Clicked from the sidebar, it stays exactly where it was, so the list does not
     move under the pointer between pages — and the marker's flight starts from an
     item that is where the eye left it. Arrived any other way, it is left at the
     top unless the selected item is out of sight, and then that item is brought to
     the middle.

     At parse time the sidebar does not exist yet, so this watches the document
     being built and places the sidebar as its links arrive. Mutation callbacks run
     before the browser is allowed to paint, so no frame shows it anywhere else.
     It stops as soon as the position has been reached — it must never fight the
     person's own scrolling — or when the page is parsed. */
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
      if (side) navPlace(side, note, state);
    };
    var finish = function () { place(); state.done = true; if (mo) mo.disconnect(); };
    var mo = window.MutationObserver ? new MutationObserver(function () {
      place();
      if (state.done || (side && side.nextElementSibling)) finish();
    }) : null;
    if (mo) mo.observe(root, { childList: true, subtree: true });
    document.addEventListener('DOMContentLoaded', finish);
  }

  /* Where a link's marker is, in the sidebar's scrolling coordinates. */
  function navMarker(side, s, link) {
    var r = link.getBoundingClientRect();
    var cs = window.getComputedStyle(link, '::before');
    var top = parseFloat(cs.top) || 0, bottom = parseFloat(cs.bottom) || 0;
    var w = parseFloat(cs.width) || 3;
    var rtl = window.getComputedStyle(link).direction === 'rtl';
    return {
      x: (rtl ? r.right - w : r.left) - s.left - side.clientLeft,
      y: r.top + top - s.top - side.clientTop + side.scrollTop,
      w: w,
      h: r.height - top - bottom
    };
  }

  /* The marker's flight from one item to another, shared by the sidebar and any list
     that marks its current item the same way, with a `::before` bar (the scrollspy
     plugin's contents list). A stand-in bar, `opts.bar`, is laid over the old marker,
     stretches to reach the new one and then lets go of the old end, while `opts.moving`
     on the box hides the real markers. The box has to be positioned: the stand-in is
     placed in its coordinates.

     One flight per box. A move that starts before the last one has landed carries on
     from wherever the bar is, rather than jumping back to an item, which is what a
     contents list does while the page is scrolled past several sections.

     Returns whether it flew; `opts.done` runs once a flight lands or is cut short. */
  function flyMarker(box, from, to, opts) {
    if (!box) return false;
    opts = opts || {};
    var barClass = opts.bar || 'ins-marker-flight', moving = opts.moving || 'ins-marker-moving';
    /* Not a move, or not one anybody should see: whatever is flying keeps flying. */
    if (!from || !to || from === to || !box.contains(from) || !to.animate || motionless()) return false;
    if (window.getComputedStyle(box).position === 'static') return false;
    var s = box.getBoundingClientRect();
    /* A collapsed column or a closed drawer: nobody is looking. */
    if (s.width < 8 || s.right <= 0 || s.left >= window.innerWidth) return false;
    var a = navMarker(box, s, from), b = navMarker(box, s, to);
    if (a.h <= 0 || b.h <= 0) return false;
    /* A real move: it replaces the flight in progress, starting where that bar is. */
    var prev = box.__insFlight;
    if (prev) {
      if (prev.bar.parentNode) {
        var pcs = window.getComputedStyle(prev.bar);
        var py = parseFloat(pcs.top), ph = parseFloat(pcs.height);
        if (!isNaN(py) && ph > 0) { a.y = py; a.h = ph; }
      }
      prev.halt();
    }

    var bar = el('span', barClass);
    bar.setAttribute('aria-hidden', 'true');
    bar.style.left = b.x + 'px';
    bar.style.width = b.w + 'px';
    box.appendChild(bar);
    box.classList.add(moving);

    var down = b.y > a.y;
    var span = down ? { top: a.y, height: b.y + b.h - a.y }
                    : { top: b.y, height: a.y + a.h - b.y };
    var run = null;
    var flight = {
      bar: bar,
      /* Ends this flight: removes its bar, and hands the box back unless a newer
         flight already owns it. Safe to call more than once. */
      land: function () {
        if (bar.parentNode) bar.parentNode.removeChild(bar);
        if (box.__insFlight !== flight) return;
        box.__insFlight = null;
        box.classList.remove(moving);
        if (opts.done) opts.done();
      },
      halt: function () {
        if (run) { run.onfinish = run.oncancel = null; try { run.cancel(); } catch (e) {} }
        flight.land();
      }
    };
    box.__insFlight = flight;
    try {
      run = bar.animate([
        { top: a.y + 'px', height: a.h + 'px', easing: NAV_EASE_OUT },
        { top: span.top + 'px', height: span.height + 'px', offset: 1 / 3, easing: NAV_EASE_IN },
        { top: b.y + 'px', height: b.h + 'px' }
      ], { duration: 600, fill: 'both' });
    } catch (e) {
      flight.land();
      return false;
    }
    run.onfinish = flight.land;
    run.oncancel = flight.land;
    return true;
  }

  /* The sidebar's flight. Once it lands, the markers the arrival hid before first
     paint are shown again; when it does not fly, the caller shows them. */
  function navFly(side, from, to) {
    return flyMarker(side, from, to, { bar: 'ins-shell-indicator', moving: 'ins-shell-moving', done: navArrived });
  }

  /* Select a sidebar item on this page — for a same-page link, or a single-page app
     that swaps the content itself. A server-rendered page marks its own item. */
  function sidebarSelect(target) {
    var link = resolve(target);
    var side = link && link.closest ? link.closest('.ins-shell-side') : null;
    if (!side) return null;
    var old = navActive(side);
    if (old === link) return link;
    if (old) {
      old.classList.remove('is-active');
      if (old.getAttribute('aria-current') === 'page') old.removeAttribute('aria-current');
    }
    link.classList.add('is-active');
    link.setAttribute('aria-current', 'page');
    navFly(side, old, link);
    return link;
  }

  /* Is this click about to load another page, in this tab? */
  function navigates(event, link) {
    if (event.defaultPrevented || event.button !== 0 ||
        event.metaKey || event.ctrlKey || event.shiftKey || event.altKey) return false;
    if (link.hasAttribute('download')) return false;
    var target = link.getAttribute('target');
    if (target && target !== '_self') return false;
    return link.origin === location.origin;
  }

  function samePage(link) {
    return link.pathname === location.pathname && link.search === location.search && !!link.hash;
  }

  /* --------------------------------------------------------------- events */

  /* Returns the event, so a cancelable one can be asked whether anyone said no. */
  function emit(name, detail, cancelable) {
    var event;
    try {
      event = new CustomEvent(name, { detail: detail, bubbles: true, cancelable: !!cancelable });
    } catch (e) {                                  /* very old WebView */
      event = document.createEvent('CustomEvent');
      event.initCustomEvent(name, true, !!cancelable, detail);
    }
    document.dispatchEvent(event);
    return event;
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
    /* "The script is running." The stylesheet keys every script-dependent state
       on this — a tab panel that is hidden until chosen, a navbar menu that is
       folded until opened, a show-password button that does nothing without us —
       so that a page whose script failed shows all of it instead of hiding it for
       ever. Stamped here, before first paint, so the folded states never flash
       open on the way in. */
    root.setAttribute('data-ins-js', '');

    var stored = read(KEY_THEME);
    if (stored === 'dark' || stored === 'light') stampTheme(stored);

    if (read(KEY_SIDEBAR) === 'collapsed') root.classList.add('ins-sidebar-collapsed');

    /* A sidebar marker about to fly in from the previous page. This page's own
       marker must not paint first: the browser paints before `DOMContentLoaded`
       whenever it can, and the flight cannot start until then, so without this the
       marker showed at its destination for a frame, vanished, and then arrived.
       Hidden from the first paint instead, until the flight takes over — and never
       for longer than two seconds, whatever goes wrong after this line. */
    var note = navNote();
    if (note && note.from && !motionless()) {
      root.classList.add('ins-nav-arriving');
      window.setTimeout(navArrived, 2000);
    }
    /* And the sidebar's scroll, placed before its first paint as well. */
    if (document.readyState === 'loading') navWatch(note);

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
        pressToggles(document);
        emit('ins:theme', { theme: effectiveTheme(), mode: 'system' });
      };
      if (mq.addEventListener) mq.addEventListener('change', onChange);
      else if (mq.addListener) mq.addListener(onChange);
    }
  }());

  /* ---------------------------------------------------------------- toast */
  /* The stylesheet hides a toast on its own, with a delayed second animation, so
     this does not own a timer and a toast is correct even if the script dies after
     creating it. All that is left here is inserting it and removing the node once
     its exit has finished. */

  var TONES = { ok: 'ok', bad: 'bad', warn: 'warn', info: 'info' };
  var TONE_GLYPH = { ok: '✓', bad: '✕', warn: '!', info: 'i' };

  function toastLayer() {
    var layer = document.querySelector('.ins-toasts');
    if (!layer) {
      layer = document.createElement('div');
      layer.className = 'ins-toasts';
      /* Announced without stealing focus. `polite` because a toast is never the
         only way to learn what happened. */
      layer.setAttribute('role', 'status');
      layer.setAttribute('aria-live', 'polite');
      document.body.appendChild(layer);
    }
    return layer;
  }

  function toast(message, tone) {
    if (!document.body) return null;
    var kind = TONES[tone] || 'info';

    var el = document.createElement('div');
    el.className = 'ins-toast ins-alert--' + kind;

    var ico = document.createElement('span');
    ico.className = 'ins-toast-ico';
    ico.setAttribute('aria-hidden', 'true');
    ico.textContent = TONE_GLYPH[kind];

    var text = document.createElement('span');
    text.className = 'ins-toast-text';
    /* textContent, not innerHTML: a toast very often carries a server message or a
       user's own input, and this is the obvious place to hand someone an injection. */
    text.textContent = message;

    el.appendChild(ico);
    el.appendChild(text);
    el.addEventListener('click', function () { el.classList.add('is-going'); });
    /* Two animations run on entry (in, then the delayed out), so wait for the one
       that actually ends with the toast gone. */
    el.addEventListener('animationend', function (e) {
      if (e.animationName === 'ins-toast-out' && el.parentNode) el.parentNode.removeChild(el);
    });

    toastLayer().appendChild(el);
    return el;
  }

  /* --------------------------------------------------------------- dialog */

  function dialog(target, action) {
    var el = typeof target === 'string' ? document.querySelector(target) : target;
    if (!el) return null;
    if (typeof el.showModal !== 'function') {
      if (window.console) window.console.warn('[insiyab] <dialog> is not supported here:', target);
      return el;
    }
    var wantOpen = action === 'close' ? false : (action === 'open' ? true : !el.open);
    if (wantOpen && !el.open) el.showModal();
    else if (!wantOpen && el.open) el.close();
    return el;
  }

  var uidCount = 0;
  function uid(prefix) { return prefix + '-' + (++uidCount); }

  function resolve(target) {
    if (typeof target !== 'string') return target || null;
    try { return document.querySelector(target); } catch (e) { return null; }
  }

  /* -------------------------------------------------------------- dismiss */
  /* `data-ins-dismiss` closes whatever the control sits in — the nearest dialog,
     drawer, popover, menu, open navbar, alert or toast — or the one it names:
     `data-ins-dismiss="#notice"`. One attribute, so a close button does not need to
     know what kind of thing it is closing. An alert is hidden rather than removed,
     so a page that wants it back only has to clear `hidden`. */
  function dismiss(from) {
    var el = resolve(from);
    if (!el) return null;
    var named = el.getAttribute && el.getAttribute('data-ins-dismiss');
    var target = named ? resolve(named) : null;
    if (!target && el.closest) {
      target = el.closest('dialog, [popover], details, .ins-navbar.is-open, .ins-alert, .ins-toast');
    }
    if (!target) return null;

    if (target.tagName === 'DIALOG') dialog(target, 'close');
    else if (target.hasAttribute('popover') && target.hidePopover) {
      try { target.hidePopover(); } catch (e) { /* already hidden */ }
    } else if (target.tagName === 'DETAILS') target.open = false;
    else if (target.classList.contains('ins-navbar')) navbar(target, false);
    else if (target.classList.contains('ins-toast')) target.classList.add('is-going');
    else target.hidden = true;

    emit('ins:dismiss', { target: target });
    return target;
  }

  /* ----------------------------------------------------------------- tabs */
  /* A tab is anything carrying `data-ins-tab="#panel"`, or an `.ins-tab` link whose
     href points at an `.ins-tabpanel` on this page. Any other `.ins-tab` — one
     pointing at another page, or at a section of this one — is navigation, not a
     tab, and is left entirely alone. That is what lets one class serve both a panel
     switcher and a row of section links; and the panel's class is what decides,
     because "is a fragment" alone would have made every in-page link in a strip
     into a tab and stamped `role="tabpanel"` on the section it jumps to. */
  var TAB = '[data-ins-tab], .ins-tab';

  function tabPanel(tab) {
    var own = tab.getAttribute('data-ins-tab');
    var ref = own || tab.getAttribute('href') || '';
    if (ref.length < 2 || ref.charAt(0) !== '#') return null;
    var panel = resolve(ref);
    if (panel && !own && !panel.classList.contains('ins-tabpanel')) return null;
    return panel;
  }

  function tabList(tab) {
    return tab.closest('.ins-tablist, [role="tablist"], .ins-seg') || tab.parentNode;
  }

  function tabsIn(list) {
    var all = list.querySelectorAll(TAB), out = [];
    for (var i = 0; i < all.length; i++) {
      if (tabList(all[i]) === list && tabPanel(all[i])) out.push(all[i]);
    }
    return out;
  }

  function selectTab(target, focus, quiet) {
    var tab = resolve(target);
    if (!tab || !tabPanel(tab)) return null;
    var tabs = tabsIn(tabList(tab));
    for (var i = 0; i < tabs.length; i++) {
      var on = tabs[i] === tab;
      var panel = tabPanel(tabs[i]);
      tabs[i].classList.toggle('is-active', on);
      tabs[i].setAttribute('aria-selected', String(on));
      /* The roving tab stop: one tab in the tab order, the arrows for the rest. A
         strip of eight tabs is otherwise eight Tab presses between the page and the
         panel it controls. */
      tabs[i].setAttribute('tabindex', on ? '0' : '-1');
      if (panel) panel.classList.toggle('is-active', on);
    }
    if (focus) tab.focus();
    if (!quiet) emit('ins:tab', { tab: tab, panel: tabPanel(tab) });
    return tab;
  }

  /* The arrows follow the strip, not the keyboard: in RTL the next tab is to the
     *left*, so ArrowLeft is "next". Activation follows focus — the panels are
     already in the page, so there is nothing to wait for. */
  function onTabKey(event) {
    var tab = event.target.closest ? event.target.closest(TAB) : null;
    if (!tab || !tabPanel(tab)) return;
    var list = tabList(tab);
    var tabs = tabsIn(list);
    var i = tabs.indexOf(tab);
    var rtl = window.getComputedStyle(list).direction === 'rtl';
    var next;
    switch (event.key) {
      case 'ArrowRight': next = rtl ? i - 1 : i + 1; break;
      case 'ArrowLeft': next = rtl ? i + 1 : i - 1; break;
      case 'Home': next = 0; break;
      case 'End': next = tabs.length - 1; break;
      default: return;
    }
    event.preventDefault();
    selectTab(tabs[(next + tabs.length) % tabs.length], true);
  }

  /* --------------------------------------------------------------- navbar */
  function navbar(target, open) {
    var bar = resolve(target);
    if (bar && !bar.classList.contains('ins-navbar')) bar = bar.closest('.ins-navbar');
    if (!bar) return null;
    var want = open === undefined ? !bar.classList.contains('is-open') : !!open;
    bar.classList.toggle('is-open', want);
    var toggles = bar.querySelectorAll('[data-ins-navbar]');
    for (var i = 0; i < toggles.length; i++) toggles[i].setAttribute('aria-expanded', String(want));
    emit('ins:navbar', { open: want, el: bar });
    return want;
  }

  /* ------------------------------------------------------------- password */
  /* The input is found by the button's own attribute, or as the password field in
     the same group. It is marked on the first press, because once it is showing its
     `type` is "text" and it would no longer be found as a password field. */
  function togglePassword(btn) {
    var input = resolve(btn.getAttribute('data-ins-password'));
    if (!input) {
      var host = btn.closest('.ins-input-group, .ins-field') || btn.parentNode;
      input = host && host.querySelector('input[type="password"], input[data-ins-secret]');
    }
    if (!input) return null;
    input.setAttribute('data-ins-secret', '');
    var show = input.type === 'password';
    input.type = show ? 'text' : 'password';
    btn.setAttribute('aria-pressed', String(show));
    return show;
  }

  /* -------------------------------------------------------------- tooltip */
  /* One tooltip element for the page, in the top layer, moved to whichever control
     asked. The text is `data-ins-tip`, or the control's own `aria-label` when the
     attribute is empty — which is the common case: an icon button's tip *is* its
     name. When the tip says something the name does not, it is also linked as the
     control's description, so a screen reader hears it too; when it only repeats
     the name it is not, or it would be announced twice.

     Hover waits 400ms, so sweeping the pointer across a toolbar does not strobe a
     tip over every button; once one is showing the next appears at once. Keyboard
     focus shows it immediately. It closes on Escape, on scroll, on a press, and when
     the pointer leaves both the control and the tip — the last so it can be read. */
  var TIP = '[data-ins-tip]';
  var tipEl = null, tipOwner = null, tipTimer = 0, tipWarmUntil = 0;
  var hasPopover = typeof HTMLElement !== 'undefined' &&
                   Object.prototype.hasOwnProperty.call(HTMLElement.prototype, 'popover');

  function tipLayer() {
    if (tipEl) return tipEl;
    tipEl = document.createElement('div');
    tipEl.className = 'ins-tooltip';
    tipEl.id = 'ins-tooltip';
    tipEl.setAttribute('role', 'tooltip');
    if (hasPopover) tipEl.setAttribute('popover', 'manual');
    else tipEl.hidden = true;
    tipEl.addEventListener('pointerenter', function () { clearTimeout(tipTimer); });
    tipEl.addEventListener('pointerleave', hideTipSoon);
    document.body.appendChild(tipEl);
    return tipEl;
  }

  function describedBy(el, add) {
    var ids = (el.getAttribute('aria-describedby') || '').split(/\s+/).filter(Boolean);
    var at = ids.indexOf('ins-tooltip');
    if (add && at === -1) ids.push('ins-tooltip');
    if (!add && at !== -1) ids.splice(at, 1);
    if (ids.length) el.setAttribute('aria-describedby', ids.join(' '));
    else el.removeAttribute('aria-describedby');
  }

  function placeTip(el, tip) {
    var r = el.getBoundingClientRect();
    var gap = 8, pad = 8;
    var vw = root.clientWidth, vh = window.innerHeight;
    tip.style.left = '0px';
    tip.style.top = '0px';
    var w = tip.offsetWidth, h = tip.offsetHeight;

    var side = el.getAttribute('data-ins-tip-side') || 'top';
    var rtl = window.getComputedStyle(el).direction === 'rtl';
    if (side === 'start') side = rtl ? 'right' : 'left';
    if (side === 'end') side = rtl ? 'left' : 'right';
    /* Flip to the opposite side when there is no room, then clamp into the viewport:
       a tip on an icon at the very edge of the page slides along rather than being
       cut off. */
    if (side === 'top' && r.top - gap - h < pad) side = 'bottom';
    else if (side === 'bottom' && r.bottom + gap + h > vh - pad) side = 'top';
    else if (side === 'left' && r.left - gap - w < pad) side = 'right';
    else if (side === 'right' && r.right + gap + w > vw - pad) side = 'left';

    var x, y;
    if (side === 'top' || side === 'bottom') {
      x = r.left + r.width / 2 - w / 2;
      y = side === 'top' ? r.top - gap - h : r.bottom + gap;
    } else {
      x = side === 'left' ? r.left - gap - w : r.right + gap;
      y = r.top + r.height / 2 - h / 2;
    }
    x = Math.max(pad, Math.min(x, vw - w - pad));
    y = Math.max(pad, Math.min(y, vh - h - pad));
    tip.style.left = Math.round(x) + 'px';
    tip.style.top = Math.round(y) + 'px';
    tip.style.setProperty('--ins-tip-dy', side === 'top' ? '4px' : (side === 'bottom' ? '-4px' : '0px'));
    tip.setAttribute('data-side', side);
  }

  function showTip(el) {
    clearTimeout(tipTimer);
    var own = el.getAttribute('data-ins-tip');
    var name = el.getAttribute('aria-label') || '';
    var text = own || name;
    if (!text) return;
    var tip = tipLayer();
    if (tipOwner && tipOwner !== el) describedBy(tipOwner, false);
    tipOwner = el;
    /* textContent, as everywhere else a string reaches the page. */
    tip.textContent = text;
    describedBy(el, !!own && own !== name && own !== (el.textContent || '').trim());
    if (hasPopover) {
      try { if (!tip.matches(':popover-open')) tip.showPopover(); } catch (e) { /* not connected yet */ }
    } else {
      tip.hidden = false;
      tip.classList.add('is-open');
    }
    placeTip(el, tip);
  }

  function hideTip() {
    clearTimeout(tipTimer);
    if (!tipEl || !tipOwner) return;
    describedBy(tipOwner, false);
    tipOwner = null;
    tipWarmUntil = Date.now() + 400;
    if (hasPopover) {
      try { tipEl.hidePopover(); } catch (e) { /* already hidden */ }
    } else {
      tipEl.classList.remove('is-open');
      tipEl.hidden = true;
    }
  }

  function hideTipSoon() {
    clearTimeout(tipTimer);
    tipTimer = setTimeout(hideTip, 120);
  }

  /* ---------------------------------------------------------------- menus */
  /* A `.ins-pop` is a working disclosure without any of this. What the script adds
     is everything a *menu* needs that a <details> does not have: Escape (which the
     platform does not give it — tested, it does nothing), the arrow keys between
     items, opening onto the first item, closing when an item is chosen or focus
     tabs away, one menu open at a time, and turning to open upward or the other way
     when the viewport's edge is in the way. */
  var POP = 'details.ins-pop';

  function popItems(pop) {
    var all = pop.querySelectorAll('.ins-pop-body .ins-pop-item'), out = [];
    for (var i = 0; i < all.length; i++) {
      var it = all[i];
      if (it.disabled || it.getAttribute('aria-disabled') === 'true') continue;
      if (it.closest(POP) === pop) out.push(it);
    }
    return out;
  }

  function focusItem(items, i) {
    if (items.length) items[(i + items.length) % items.length].focus();
  }

  function closePop(pop, refocus) {
    if (!pop.open) return;
    pop.open = false;
    var summary = refocus && pop.querySelector('summary');
    if (summary) summary.focus();
  }

  /* Measured once, on open. The body is placed by the stylesheet, and this only
     decides between the four placements it already has: below or above, and
     toward the start or toward the end. */
  function flipPop(pop) {
    pop.removeAttribute('data-ins-flip');
    var body = pop.querySelector('.ins-pop-body');
    if (!body) return;
    var b = body.getBoundingClientRect();
    var s = pop.getBoundingClientRect();
    var vw = root.clientWidth, vh = window.innerHeight, flips = [];
    if (b.bottom > vh - 8 && s.top - b.height - 6 > 8) flips.push('up');
    if (b.left < 8 || b.right > vw - 8) flips.push('inline');
    if (flips.length) pop.setAttribute('data-ins-flip', flips.join(' '));
  }

  function onPopKey(event) {
    var pop = event.target.closest ? event.target.closest(POP) : null;
    if (!pop) return;
    var summary = pop.querySelector('summary');
    var onSummary = event.target === summary;
    var items = popItems(pop);
    var at = items.indexOf(document.activeElement);

    switch (event.key) {
      case 'Escape':
        if (!pop.open) return;
        /* Prevented, so a menu inside a dialog closes the menu and not the dialog. */
        event.preventDefault();
        closePop(pop, true);
        return;
      case 'ArrowDown':
      case 'ArrowUp':
        if (!items.length) return;
        event.preventDefault();
        if (!pop.open) pop.open = true;
        if (event.key === 'ArrowDown') focusItem(items, onSummary ? 0 : at + 1);
        else focusItem(items, onSummary ? items.length - 1 : at - 1);
        return;
      case 'Home':
      case 'End':
        if (onSummary || !pop.open || !items.length) return;
        event.preventDefault();
        focusItem(items, event.key === 'Home' ? 0 : items.length - 1);
        return;
      case 'Enter':
      case ' ':
        /* Opening from the keyboard lands on the first item, as a menu button does.
           Closing is left to the platform. */
        if (!onSummary || pop.open || !items.length) return;
        event.preventDefault();
        pop.open = true;
        focusItem(items, 0);
        return;
    }
  }

  /* ----------------------------------------------------------- validation */
  /* In a form marked `data-ins-validate`, the browser's own constraint validation
     does the checking and the field's `.ins-error` does the telling. An empty
     message is filled with the browser's `validationMessage` — already in the
     visitor's own language, which is most of the reason to use it — and a message
     the page wrote is left as written. The browser's bubble is suppressed for the
     fields that have a message of their own, and the first of them takes focus,
     which the browser stops doing once its own report is cancelled. */
  function fieldMessage(control) {
    var field = control.closest && control.closest('.ins-field');
    if (!field) return null;
    for (var i = 0; i < field.children.length; i++) {
      if (field.children[i].classList.contains('ins-error')) return field.children[i];
    }
    return null;
  }

  var firstInvalid = null;

  /* An event a page's own listeners will see, as if the person had done it. */
  function fire(el, type) {
    var event;
    try {
      event = new Event(type, { bubbles: true });
    } catch (e) {                                  /* very old WebView */
      event = document.createEvent('Event');
      event.initEvent(type, true, false);
    }
    el.dispatchEvent(event);
  }

  /* The few words the library itself has to say — on a calendar, in a confirmation.
     Arabic and English, chosen by the nearest `lang`; anything else gets English,
     and every one of them can be overridden where it is used. */
  var STRINGS = {
    ar: {
      ok: 'تأكيد', cancel: 'إلغاء', choose: 'اختر تاريخًا', calendar: 'التقويم',
      prevMonth: 'الشهر السابق', nextMonth: 'الشهر التالي', month: 'الشهر', year: 'السنة',
      today: 'اليوم', clear: 'مسح', badDate: 'اكتب تاريخًا صحيحًا.', calendarSystem: 'نظام التقويم',
      early: 'اختر {date} أو ما بعده.', late: 'اختر {date} أو ما قبله.',
      selected: 'المحدّد: {n}', remove: 'إزالة', badTime: 'اكتب وقتًا صحيحًا.',
      words: '{n}/{max} كلمة', wordsFree: '{n} كلمة', tooManyWords: 'لا تتجاوز {max} كلمة.',
      days: '{d} يوم', saving: 'جارٍ الحفظ…', saved: 'حُفظ', saveError: 'تعذّر الحفظ', resize: 'تغيير حجم اللوحين'
    },
    en: {
      ok: 'OK', cancel: 'Cancel', choose: 'Choose a date', calendar: 'Calendar',
      prevMonth: 'Previous month', nextMonth: 'Next month', month: 'Month', year: 'Year',
      today: 'Today', clear: 'Clear', badDate: 'Enter a valid date.', calendarSystem: 'Calendar system',
      early: 'Choose {date} or later.', late: 'Choose {date} or earlier.',
      selected: '{n} selected', remove: 'Remove', badTime: 'Enter a valid time.',
      words: '{n}/{max} words', wordsFree: '{n} words', tooManyWords: 'Use {max} words or fewer.',
      days: '{d}d', saving: 'Saving…', saved: 'Saved', saveError: 'Could not save', resize: 'Resize the panes'
    }
  };

  function langOf(el) {
    var own = el && el.getAttribute && el.getAttribute('data-ins-locale');
    if (own) return own;
    var host = (el && el.closest && el.closest('[lang]')) || root;
    return host.getAttribute('lang') || window.navigator.language || 'en';
  }

  function strings(el) {
    return STRINGS[langOf(el).slice(0, 2).toLowerCase()] || STRINGS.en;
  }

  /* --------------------------------------------------------------- toggle */
  function toggleButton(el) {
    var on = el.getAttribute('aria-pressed') !== 'true';
    el.setAttribute('aria-pressed', String(on));
    emit('ins:toggle', { el: el, pressed: on });
    return on;
  }

  /* ---------------------------------------------------------------- range */
  /* The fill's edge has to sit under the thumb's centre, and the thumb's centre
     does not travel the whole track: it stops half a thumb short of each end. So the
     stop is the percentage plus a correction of up to half a thumb — without it the
     fill runs ahead of the thumb near one end and behind it near the other. */
  var THUMB_REM = 1.25;

  function rangeFill(input) {
    var min = parseFloat(input.min), max = parseFloat(input.max), value = parseFloat(input.value);
    if (isNaN(min)) min = 0;
    if (isNaN(max)) max = 100;
    var pct = max > min ? Math.max(0, Math.min(100, (value - min) / (max - min) * 100)) : 0;
    var nudge = (0.5 - pct / 100) * THUMB_REM;
    input.style.setProperty('--ins-fill', 'calc(' + pct.toFixed(3) + '% + ' + nudge.toFixed(4) + 'rem)');
  }

  /* -------------------------------------------------------- number stepper */
  function spinInput(btn) {
    var group = btn.closest('.ins-input-group') || btn.parentNode;
    return group ? group.querySelector('input') : null;
  }

  function syncSpin(input) {
    var group = input.closest('.ins-input-group') || input.parentNode;
    var value = parseFloat(input.value), min = parseFloat(input.min), max = parseFloat(input.max);
    var btns = group.querySelectorAll('.ins-spin');
    for (var i = 0; i < btns.length; i++) {
      var down = btns[i].getAttribute('data-ins-spin') === 'down';
      btns[i].disabled = input.disabled || (!isNaN(value) && (down ? (!isNaN(min) && value <= min) : (!isNaN(max) && value >= max)));
    }
  }

  function spin(btn) {
    var input = spinInput(btn);
    if (!input || input.disabled || input.readOnly) return null;
    try {
      if (btn.getAttribute('data-ins-spin') === 'down') input.stepDown(); else input.stepUp();
    } catch (e) {
      return null;                                 /* not a number field */
    }
    fire(input, 'input');
    fire(input, 'change');
    syncSpin(input);
    return input.value;
  }

  /* ---------------------------------------------------------- autocomplete */
  /* The matching. Lower case, and then the Arabic that a person types differently
     from how a database stored it made to meet in the middle: canonical
     decomposition splits every hamza off its seat (أ إ آ into ا, ؤ into و, ئ into
     ي) and the combining-mark range then drops it along with every other haraka and
     the tatweel; ة is read as ه and ى as ي; Arabic-Indic and Persian digits are read
     as Latin ones. The same decomposition takes the accents off Latin letters, so
     "Beyrouth" finds "Beyrouth" however it was typed. */
  function norm(text) {
    var s = String(text || '').toLowerCase();
    if (s.normalize) s = s.normalize('NFD');
    return s
      .replace(/[\u0300-\u036f\u064b-\u065f\u0670\u0640]/g, '')
      .replace(/\u0629/g, '\u0647')
      .replace(/\u0649/g, '\u064a')
      .replace(/[\u0660-\u0669]/g, function (d) { return String(d.charCodeAt(0) - 0x0660); })
      .replace(/[\u06f0-\u06f9]/g, function (d) { return String(d.charCodeAt(0) - 0x06f0); })
      .trim();
  }

  var COMBO = '.ins-combo';

  function comboInput(combo) { return combo.querySelector('input:not([type="hidden"])'); }
  function comboList(combo) { return combo.querySelector('.ins-combo-list'); }

  function comboOptions(combo, visible) {
    var all = combo.querySelectorAll('.ins-combo-option'), out = [];
    for (var i = 0; i < all.length; i++) {
      if (visible && (all[i].hidden || all[i].getAttribute('aria-disabled') === 'true')) continue;
      out.push(all[i]);
    }
    return out;
  }

  function comboActive(combo) { return combo.querySelector('.ins-combo-option.is-active'); }

  function comboActivate(combo, opt) {
    var input = comboInput(combo), prev = comboActive(combo);
    if (prev) prev.classList.remove('is-active');
    if (opt) {
      opt.classList.add('is-active');
      input.setAttribute('aria-activedescendant', opt.id);
      if (opt.scrollIntoView) opt.scrollIntoView({ block: 'nearest' });
    } else {
      input.removeAttribute('aria-activedescendant');
    }
  }

  function comboOpen(combo, open) {
    var list = comboList(combo), input = comboInput(combo);
    if (!list || !input) return;
    if (open) {
      list.setAttribute('data-ins-open', '');
      input.setAttribute('aria-expanded', 'true');
      /* The current choice, in view: in a list of forty-eight times, the one set. A
         time list with nothing set opens at the hour it is now. */
      var sel = list.querySelector('.ins-combo-option[aria-selected="true"]:not([hidden])');
      if (sel) comboActivate(combo, sel);
      else if (combo.classList.contains('ins-time')) timeNear(combo);
      combo.removeAttribute('data-ins-flip');
      var r = list.getBoundingClientRect(), s = input.getBoundingClientRect();
      if (r.bottom > window.innerHeight - 8 && s.top - r.height - 6 > 8) combo.setAttribute('data-ins-flip', 'up');
    } else {
      list.removeAttribute('data-ins-open');
      input.setAttribute('aria-expanded', 'false');
      comboActivate(combo, null);
    }
  }

  function comboFilter(combo) {
    var q = norm(comboInput(combo).value), any = false;
    var opts = comboOptions(combo, false);
    /* The field still showing the option already chosen is not a search for it: the
       whole list comes back, so a person reopening it can pick another. */
    var chosen = combo.querySelector('.ins-combo-option[aria-selected="true"]');
    if (chosen && q === norm(chosen.getAttribute('data-label') || chosen.textContent)) q = '';
    for (var i = 0; i < opts.length; i++) {
      var hit = !q || norm(opts[i].textContent).indexOf(q) !== -1 ||
                norm(opts[i].getAttribute('data-value')).indexOf(q) !== -1;
      /* A multi-select's chosen options are chips now, not choices. */
      if (opts[i].hasAttribute('data-ins-chosen')) hit = false;
      opts[i].hidden = !hit;
      if (hit) any = true;
    }
    var empty = combo.querySelector('.ins-combo-empty');
    if (empty) empty.hidden = any;
    return any || !!empty;
  }

  function comboChoose(combo, opt) {
    var input = comboInput(combo);
    var label = opt.getAttribute('data-label') || opt.textContent.trim();
    var value = opt.hasAttribute('data-value') ? opt.getAttribute('data-value') : label;
    /* A multi-select adds a chip and stays open for the next one. */
    if (combo.classList.contains('ins-tags')) {
      tagsAdd(combo, value, label);
      input.value = '';
      comboActivate(combo, null);
      comboOpen(combo, comboFilter(combo));
      return;
    }
    input.value = label;
    var opts = comboOptions(combo, false);
    for (var i = 0; i < opts.length; i++) opts[i].setAttribute('aria-selected', String(opts[i] === opt));
    var hidden = combo.querySelector('input[type="hidden"]');
    if (hidden) hidden.value = value;
    comboOpen(combo, false);
    fire(input, 'change');
    emit('ins:combo', { el: combo, value: value, label: label, option: opt });
  }

  function onComboKey(event) {
    var input = event.target;
    var combo = input.closest ? input.closest(COMBO) : null;
    if (!combo || input !== comboInput(combo)) return;
    var open = comboList(combo) && comboList(combo).hasAttribute('data-ins-open');
    var opts = comboOptions(combo, true);
    var at = opts.indexOf(comboActive(combo));
    switch (event.key) {
      case 'ArrowDown':
      case 'ArrowUp':
        event.preventDefault();
        /* Opening lands on the current choice, when there is one, rather than past it. */
        if (!open) { comboFilter(combo); comboOpen(combo, true); if (event.altKey || comboActive(combo)) return; }
        if (!opts.length) return;
        if (event.key === 'ArrowDown') comboActivate(combo, opts[(at + 1) % opts.length]);
        else comboActivate(combo, opts[at <= 0 ? opts.length - 1 : at - 1]);
        return;
      case 'Enter':
        /* Prevented only when it chooses something: an Enter with nothing highlighted
           is the person submitting the form, and that is theirs to do. */
        if (open && at !== -1) { event.preventDefault(); comboChoose(combo, opts[at]); }
        return;
      case 'Escape':
        if (open) { event.preventDefault(); comboOpen(combo, false); }
        else if (input.value) { event.preventDefault(); input.value = ''; comboFilter(combo); fire(input, 'input'); }
        return;
      case 'Tab':
        if (open) comboOpen(combo, false);
        return;
    }
  }

  /* ----------------------------------------------------------------- date */
  /* A date is a local calendar day, not an instant, so it is kept as one: a Date at
     local midnight, built from its three numbers and moved by adding to the day of
     the month. Nothing here goes near a timezone offset, which is how a date picker
     ends up choosing the day before for everybody west of UTC. */
  function pad2(n) { return (n < 10 ? '0' : '') + n; }
  function isoOf(d) { return d.getFullYear() + '-' + pad2(d.getMonth() + 1) + '-' + pad2(d.getDate()); }
  function day(y, m, d) { return new Date(y, m, d); }
  function today() { var t = new Date(); return day(t.getFullYear(), t.getMonth(), t.getDate()); }
  function sameDay(a, b) { return !!a && !!b && a.getTime() === b.getTime(); }
  function addDays(d, n) { return day(d.getFullYear(), d.getMonth(), d.getDate() + n); }

  function parseIso(s) {
    var m = /^(\d{4})-(\d{2})-(\d{2})$/.exec(s || '');
    if (!m) return null;
    var d = day(+m[1], +m[2] - 1, +m[3]);
    return d.getMonth() === +m[2] - 1 ? d : null;
  }

  /* ----------------------------------------------------- calendar systems */
  /* The picker shows days in a calendar system, and keeps them as Gregorian days
     whatever it shows. A day button's `data-date`, the range, `min` and `max`, and
     what the form sends are all Gregorian ISO in every calendar; only how a date is
     labelled, how the grid is cut into months and how a typed date is read change.

     So a calendar is four small answers, and Gregorian is the one built in:

       intl               the Intl calendar id, for every label — month and day
                          names, the year, the field's text
       parts(d)           { y, m, d } of a local-midnight Date, m from 0
       fromParts(y, m, d) that Date back; m may run past 11 or below 0, and d past
                          the end of the month, the way `new Date()` allows
       monthLength(y, m)
       label              { ar, en } for the switch in the calendar's footer
       parse(text, loc)   optional: a typed date in this calendar — a Date, null for
                          "not a date", or undefined to fall back to the numbers

     Others register with `Insiyab.calendar(name, calendar)`; insiyab-hijri.js adds
     `hijri` (Umm al-Qura) and `hijri-civil`. A field asks for one with
     `data-ins-calendar`, or a whole page does on <html>. And every label passes the
     calendar to Intl explicitly, Gregorian included: `ar-SA` defaults to Umm al-Qura,
     and a Gregorian grid under Hijri month names is what leaving it implicit gave. */
  var GREGORY = {
    intl: 'gregory',
    label: { ar: 'ميلادي', en: 'Gregorian' },
    parts: function (d) { return { y: d.getFullYear(), m: d.getMonth(), d: d.getDate() }; },
    fromParts: function (y, m, d) { return day(y, m, d); },
    monthLength: function (y, m) { return day(y, m + 1, 0).getDate(); }
  };
  var CALENDARS = { gregory: GREGORY };
  var warnedCalendar = {};

  function registerCalendar(name, cal) {
    if (cal === undefined) return CALENDARS[name] || null;
    CALENDARS[name] = cal;
    /* Registered after the page was built — a plugin loaded late, or async. Its
       fields were written in Gregorian meanwhile, so they are written again. */
    var fields = document.querySelectorAll('[data-ins-date-ready]');
    for (var i = 0; i < fields.length; i++) {
      if (homeCalendarName(fields[i]) !== name) continue;
      var d = dateOf(fields[i]);
      if (d) fields[i].value = formatDay(fields[i], d);
    }
    return cal;
  }

  function homeCalendarName(input) {
    var own = input.getAttribute('data-ins-calendar');
    return own || root.getAttribute('data-ins-calendar') || 'gregory';
  }

  /* The field's own calendar. One that is asked for and not registered falls back
     to Gregorian, said once in the console: a Hijri page whose plugin failed to load
     should still be a working date field, not a broken one. */
  function homeCalendar(input) {
    var name = homeCalendarName(input);
    if (CALENDARS[name]) return CALENDARS[name];
    if (!warnedCalendar[name] && window.console) {
      warnedCalendar[name] = true;
      window.console.warn('[insiyab] no calendar called "' + name + '" is registered — showing Gregorian. Load its plugin (insiyab-hijri.js for hijri) after insiyab.js.');
    }
    return GREGORY;
  }

  /* The calendar the field is shown in right now: its own, or Gregorian when the
     person has flipped the footer switch. */
  function viewCalendar(input) {
    return input.getAttribute('data-ins-calendar-view') === 'gregory' ? GREGORY : homeCalendar(input);
  }

  function sysFormat(cal, locale, options) {
    options.calendar = cal.intl;
    return dateFormat(locale, options);
  }

  function sameMonthIn(cal, a, b) {
    var p = cal.parts(a), q = cal.parts(b);
    return p.y === q.y && p.m === q.m;
  }

  function monthStartIn(cal, d) {
    var p = cal.parts(d);
    return cal.fromParts(p.y, p.m, 1);
  }

  /* A month on, or back, in the calendar on screen — the same day of the month where
     it exists, the last day where it does not. */
  function addMonthsIn(cal, d, n) {
    var p = cal.parts(d), y = p.y, m = p.m + n;
    y += Math.floor(m / 12);
    m = ((m % 12) + 12) % 12;
    return cal.fromParts(y, m, Math.min(p.d, cal.monthLength(y, m)));
  }

  /* Latin digits unless the page asked for others. Every figure in this library is
     set in Inter's Latin numerals — the money, the tables, the stat tiles — and a
     calendar in Arabic-Indic digits beside them would be the one thing on the page in
     a different hand. `lang="ar-u-nu-arab"` asks for them explicitly. */
  function dateFormat(locale, options) {
    var opts = {};
    for (var k in options) if (Object.prototype.hasOwnProperty.call(options, k)) opts[k] = options[k];
    var own = null;
    try { own = new Intl.Locale(locale).numberingSystem; } catch (e) { /* no Intl.Locale */ }
    if (!own) opts.numberingSystem = 'latn';
    try { return new Intl.DateTimeFormat(locale, opts); }
    catch (e) { return new Intl.DateTimeFormat('en', opts); }
  }

  /* Where the week starts, as a JavaScript day number (Sunday is 0). The locale
     knows, where the engine will say; otherwise Arabic and Persian start on Saturday,
     the United States on Sunday, and nearly everywhere else on Monday. */
  function weekStart(locale) {
    try {
      var loc = new Intl.Locale(locale);
      var info = loc.getWeekInfo ? loc.getWeekInfo() : loc.weekInfo;
      if (info && info.firstDay) return info.firstDay % 7;
    } catch (e) { /* no Intl.Locale */ }
    var lang = locale.toLowerCase();
    if (/^(ar|fa)\b/.test(lang)) return 6;
    if (/^en-us\b|^he\b|^ja\b|^ko\b/.test(lang)) return 0;
    return 1;
  }

  /* Is day written before month in this locale's numeric dates? Asked of the locale
     rather than assumed, because "3/4" is the third of April or the fourth of March
     depending on who typed it. */
  function monthFirst(locale) {
    try {
      var parts = dateFormat(locale, { day: 'numeric', month: 'numeric', year: 'numeric' }).formatToParts(day(2001, 10, 22));
      for (var i = 0; i < parts.length; i++) {
        if (parts[i].type === 'day') return false;
        if (parts[i].type === 'month') return true;
      }
    } catch (e) { /* no formatToParts */ }
    return false;
  }

  function toLatin(s) {
    return String(s || '')
      .replace(/[\u0660-\u0669]/g, function (d) { return String(d.charCodeAt(0) - 0x0660); })
      .replace(/[\u06f0-\u06f9]/g, function (d) { return String(d.charCodeAt(0) - 0x06f0); });
  }

  /* What a person typed: an ISO date, or three numbers in the locale's own order, in
     either set of digits, read in the calendar on screen. Anything else is not
     guessed at.

     Another calendar reads the text first, ISO-shaped or not: `1448-09-01` typed
     into a Hijri field is the first of Ramadan, not a day in the fifteenth century.
     The Hijri reader passes a year past 1700 back as Gregorian, so `2026-09-24`
     still means what it says there. */
  function parseTyped(text, input) {
    var locale = dateLocale(input), cal = viewCalendar(input);
    var s = toLatin(text).trim();
    var iso = parseIso(s);
    if (iso && cal === GREGORY) return iso;
    if (cal.parse) {
      var own = cal.parse(s, locale);
      if (own !== undefined) return own;
    }
    if (iso) return iso;
    var parts = s.split(/[^0-9]+/).filter(Boolean);
    if (parts.length !== 3) return null;
    var y, m, d;
    if (parts[0].length === 4) { y = +parts[0]; m = +parts[1]; d = +parts[2]; }
    else if (monthFirst(locale)) { m = +parts[0]; d = +parts[1]; y = +parts[2]; }
    else { d = +parts[0]; m = +parts[1]; y = +parts[2]; }
    if (cal === GREGORY && y < 100) y += 2000;
    if (m < 1 || m > 12 || d < 1) return null;
    var out = cal.fromParts(y, m - 1, d), back = out && cal.parts(out);
    return back && back.y === y && back.m === m - 1 && back.d === d ? out : null;
  }

  function dateValue(input) { return resolve(input.getAttribute('data-ins-date-value')); }
  function dateOf(input) { var h = dateValue(input); return h ? parseIso(h.value) : null; }
  function dateLocale(input) { return langOf(input); }
  function formatDay(input, d) {
    return sysFormat(viewCalendar(input), dateLocale(input), { day: 'numeric', month: 'long', year: 'numeric' }).format(d);
  }

  /* The bounds that apply to a field right now: its own `min` and `max`, and for the
     end of a range, its start — the end cannot come before it.

     Only the end is narrowed, never the start. The first version capped the start
     at the end as well, and that made the obvious correction impossible: to move a
     range a week later you pick the new start first, and with the start capped at
     the old end the new start was greyed out. Now any start can be picked, and one
     that falls after the end clears the end and opens it (`calChoose`). */
  function dateBounds(input) {
    var min = parseIso(input.getAttribute('data-ins-min'));
    var max = parseIso(input.getAttribute('data-ins-max'));
    var start = resolve(input.getAttribute('data-ins-date-start'));
    var from = start && dateOf(start);
    if (from && (!min || from > min)) min = from;
    return { min: min, max: max };
  }

  function dateRange(input) {
    var start = resolve(input.getAttribute('data-ins-date-start'));
    var end = resolve(input.getAttribute('data-ins-date-end'));
    if (start) return { from: dateOf(start), to: dateOf(input) };
    if (end) return { from: dateOf(input), to: dateOf(end) };
    return null;
  }

  function checkDate(input, d) {
    var t = strings(input), b = dateBounds(input), msg = '';
    if (d === null) msg = t.badDate;
    else if (d && b.min && d < b.min) msg = t.early.replace('{date}', formatDay(input, b.min));
    else if (d && b.max && d > b.max) msg = t.late.replace('{date}', formatDay(input, b.max));
    input.setCustomValidity(msg);
    return !msg;
  }

  function setDate(input, d, quiet) {
    var hidden = dateValue(input);
    if (!hidden) return;
    var value = d ? isoOf(d) : '';
    var changed = hidden.value !== value;
    hidden.value = value;
    input.value = d ? formatDay(input, d) : '';
    checkDate(input, d || undefined);
    if (!quiet && changed) {
      fire(input, 'input');
      fire(input, 'change');
      emit('ins:date', { input: input, value: value, date: d });
    }
  }

  /* Read what was typed, when the person leaves the field or presses Enter. Text
     that is still exactly the formatted value is the value, untouched. */
  function commitTyped(input) {
    var current = dateOf(input);
    var text = input.value.trim();
    if (current && text === formatDay(input, current)) return;
    if (!text) { setDate(input, null); return; }
    var d = parseTyped(text, input);
    if (d) setDate(input, d);
    else checkDate(input, null);
  }

  /* The calendar, one for the page. */
  var calEl = null, calFor = null, calView = null, calFocus = null;

  function calLayer() {
    if (calEl) return calEl;
    calEl = document.createElement('div');
    calEl.className = 'ins-cal';
    calEl.setAttribute('role', 'dialog');
    if (hasPopover) calEl.setAttribute('popover', 'manual');
    else calEl.hidden = true;
    calEl.addEventListener('keydown', onCalKey);
    calEl.addEventListener('click', onCalClick);
    calEl.addEventListener('change', onCalChange);
    document.body.appendChild(calEl);
    return calEl;
  }

  function calButton(input) {
    var group = input.closest('.ins-input-group') || input.parentNode;
    return group.querySelector('.ins-date-btn');
  }

  function el(tag, cls, text) {
    var node = document.createElement(tag);
    if (cls) node.className = cls;
    if (text !== undefined) node.textContent = text;
    return node;
  }

  function renderCal() {
    var cal = calLayer(), input = calFor, t = strings(input), locale = dateLocale(input);
    var sys = viewCalendar(input), vp = sys.parts(calView);
    var y = vp.y, m = vp.m;
    var selected = dateOf(input), now = today(), bounds = dateBounds(input), range = dateRange(input);
    var fd = weekStart(locale);

    cal.textContent = '';
    cal.setAttribute('aria-label', t.calendar);
    cal.setAttribute('lang', locale);
    cal.dir = window.getComputedStyle(input).direction;

    var head = el('div', 'ins-cal-head');
    var prev = el('button', 'ins-cal-nav ins-cal-nav--prev');
    prev.type = 'button'; prev.setAttribute('aria-label', t.prevMonth); prev.setAttribute('data-ins-cal', 'prev');
    var next = el('button', 'ins-cal-nav ins-cal-nav--next');
    next.type = 'button'; next.setAttribute('aria-label', t.nextMonth); next.setAttribute('data-ins-cal', 'next');

    var title = el('div', 'ins-cal-title');
    var months = el('select', 'ins-select ins-select--sm');
    months.setAttribute('aria-label', t.month); months.setAttribute('data-ins-cal', 'month');
    var monthName = sysFormat(sys, locale, { month: 'long' });
    for (var i = 0; i < 12; i++) {
      var o = el('option', '', monthName.format(sys.fromParts(y, i, 1)));
      o.value = String(i);
      if (i === m) o.selected = true;
      months.appendChild(o);
    }
    var years = el('select', 'ins-select ins-select--sm');
    years.setAttribute('aria-label', t.year); years.setAttribute('data-ins-cal', 'year');
    var yearName = sysFormat(sys, locale, { year: 'numeric' });
    var nowY = sys.parts(now).y;
    var y0 = bounds.min ? sys.parts(bounds.min).y : Math.min(y, nowY) - 100;
    var y1 = bounds.max ? sys.parts(bounds.max).y : Math.max(y, nowY) + 20;
    for (var yy = y1; yy >= y0; yy--) {
      var oy = el('option', '', yearName.format(sys.fromParts(yy, 0, 1)));
      oy.value = String(yy);
      if (yy === y) oy.selected = true;
      years.appendChild(oy);
    }
    title.appendChild(months);
    title.appendChild(years);
    head.appendChild(prev);
    head.appendChild(title);
    head.appendChild(next);
    cal.appendChild(head);

    var grid = el('table', 'ins-cal-grid');
    grid.setAttribute('role', 'grid');
    var thead = el('thead'), hr = el('tr');
    var narrow = dateFormat(locale, { weekday: 'narrow' }), long = dateFormat(locale, { weekday: 'long' });
    for (var c = 0; c < 7; c++) {
      var wd = day(2023, 0, 1 + (fd + c) % 7);        /* 1 Jan 2023 was a Sunday */
      var th = el('th', '', narrow.format(wd));
      th.scope = 'col';
      th.abbr = long.format(wd);
      hr.appendChild(th);
    }
    thead.appendChild(hr);
    grid.appendChild(thead);

    var tbody = el('tbody');
    var first = sys.fromParts(y, m, 1);
    var cell = addDays(first, -((first.getDay() - fd + 7) % 7));
    var dayNum = sysFormat(sys, locale, { day: 'numeric' });
    var full = sysFormat(sys, locale, { weekday: 'long', day: 'numeric', month: 'long', year: 'numeric' });
    for (var r = 0; r < 6; r++) {
      var tr = el('tr');
      for (var k = 0; k < 7; k++) {
        var td = el('td');
        var btn = el('button', 'ins-cal-day', dayNum.format(cell));
        btn.type = 'button';
        btn.setAttribute('data-date', isoOf(cell));
        btn.setAttribute('aria-label', full.format(cell));
        btn.tabIndex = sameDay(cell, calFocus) ? 0 : -1;
        if (!sameMonthIn(sys, cell, first)) btn.classList.add('is-outside');
        if (sameDay(cell, now)) btn.setAttribute('aria-current', 'date');
        if (sameDay(cell, selected)) btn.setAttribute('aria-selected', 'true');
        if ((bounds.min && cell < bounds.min) || (bounds.max && cell > bounds.max)) btn.setAttribute('aria-disabled', 'true');
        if (range && range.from && range.to && cell >= range.from && cell <= range.to) {
          td.classList.add('is-in-range');
          if (sameDay(cell, range.from)) td.classList.add('is-range-start');
          if (sameDay(cell, range.to)) td.classList.add('is-range-end');
        }
        if (range && !sameDay(cell, selected) && (sameDay(cell, range.from) || sameDay(cell, range.to))) {
          btn.classList.add('is-range-edge');
        }
        td.appendChild(btn);
        tr.appendChild(td);
        cell = addDays(cell, 1);
      }
      tbody.appendChild(tr);
    }
    grid.appendChild(tbody);
    cal.appendChild(grid);

    var foot = el('div', 'ins-cal-foot');
    var todayBtn = el('button', 'ins-btn ins-btn--bare ins-btn--sm', t.today);
    todayBtn.type = 'button'; todayBtn.setAttribute('data-ins-cal', 'today');
    foot.appendChild(todayBtn);
    /* A field in another calendar can be flipped to Gregorian and back, for the
       person who thinks in the other one. Only then: a Gregorian field has nothing
       to switch to. The field's text follows the calendar on screen. */
    var home = homeCalendar(input);
    if (home !== GREGORY) {
      var lang = langOf(input).slice(0, 2).toLowerCase();
      var sw = el('div', 'ins-seg ins-cal-switch');
      sw.setAttribute('role', 'group');
      sw.setAttribute('aria-label', t.calendarSystem);
      var pair = [[homeCalendarName(input), home], ['gregory', GREGORY]];
      for (var s = 0; s < 2; s++) {
        var opt = el('button', sys === pair[s][1] ? 'is-active' : '', pair[s][1].label[lang] || pair[s][1].label.en);
        opt.type = 'button';
        opt.setAttribute('data-ins-cal', 'system');
        opt.setAttribute('data-value', pair[s][0]);
        opt.setAttribute('aria-pressed', String(sys === pair[s][1]));
        sw.appendChild(opt);
      }
      foot.appendChild(sw);
    }
    if (!input.required) {
      var clear = el('button', 'ins-btn ins-btn--bare ins-btn--sm', t.clear);
      clear.type = 'button'; clear.setAttribute('data-ins-cal', 'clear');
      foot.appendChild(clear);
    }
    cal.appendChild(foot);
  }

  /* Below the field, aligned to its leading edge — the right edge on an Arabic page
     — and above it when there is no room below. */
  function placeCal() {
    if (!calEl || !calFor) return;
    var anchor = (calFor.closest('.ins-input-group') || calFor).getBoundingClientRect();
    var gap = 6, pad = 8, vw = root.clientWidth, vh = window.innerHeight;
    calEl.style.left = '0px';
    calEl.style.top = '0px';
    var w = calEl.offsetWidth, h = calEl.offsetHeight;
    var y = anchor.bottom + gap;
    if (y + h > vh - pad && anchor.top - gap - h >= pad) y = anchor.top - gap - h;
    var x = calEl.dir === 'rtl' ? anchor.right - w : anchor.left;
    x = Math.max(pad, Math.min(x, vw - w - pad));
    y = Math.max(pad, Math.min(y, vh - h - pad));
    calEl.style.left = Math.round(x) + 'px';
    calEl.style.top = Math.round(y) + 'px';
  }

  function focusCalDay() {
    var btn = calEl && calEl.querySelector('.ins-cal-day[tabindex="0"]');
    if (btn) btn.focus();
  }

  function openCal(input, focusGrid) {
    if (calFor && calFor !== input) closeCal(false);
    calFor = input;
    var start = dateOf(input) || today();
    var b = dateBounds(input);
    if (b.min && start < b.min) start = b.min;
    if (b.max && start > b.max) start = b.max;
    calFocus = start;
    calView = monthStartIn(viewCalendar(input), start);
    renderCal();
    if (hasPopover) {
      try { if (!calEl.matches(':popover-open')) calEl.showPopover(); } catch (e) { /* not connected */ }
    } else {
      calEl.hidden = false;
      calEl.classList.add('is-open');
    }
    placeCal();
    var btn = calButton(input);
    if (btn) btn.setAttribute('aria-expanded', 'true');
    if (focusGrid) focusCalDay();
  }

  function closeCal(refocus) {
    if (!calEl || !calFor) return;
    var input = calFor;
    calFor = null;
    if (hasPopover) {
      try { calEl.hidePopover(); } catch (e) { /* already hidden */ }
    } else {
      calEl.classList.remove('is-open');
      calEl.hidden = true;
    }
    var btn = calButton(input);
    if (btn) btn.setAttribute('aria-expanded', 'false');
    if (refocus) input.focus();
  }

  function calMove(d) {
    calFocus = d;
    var sys = viewCalendar(calFor);
    if (!sameMonthIn(sys, d, calView)) {
      calView = monthStartIn(sys, d);
      renderCal();
    } else {
      var btns = calEl.querySelectorAll('.ins-cal-day');
      for (var i = 0; i < btns.length; i++) btns[i].tabIndex = btns[i].getAttribute('data-date') === isoOf(d) ? 0 : -1;
    }
    focusCalDay();
  }

  function calChoose(d) {
    var input = calFor;
    setDate(input, d);
    closeCal(true);
    /* The start of a range hands straight on to its end, when the end is empty or
       now falls before it. */
    var end = resolve(input.getAttribute('data-ins-date-end'));
    if (d && end) {
      var to = dateOf(end);
      if (!to || to < d) { if (to) setDate(end, null); openCal(end, true); }
    }
  }

  function onCalKey(event) {
    var target = event.target;
    if (event.key === 'Escape') { event.preventDefault(); closeCal(true); return; }
    if (!target.classList.contains('ins-cal-day')) return;
    var d = parseIso(target.getAttribute('data-date'));
    var rtl = calEl.dir === 'rtl';
    var next = null;
    switch (event.key) {
      case 'ArrowRight': next = addDays(d, rtl ? -1 : 1); break;
      case 'ArrowLeft': next = addDays(d, rtl ? 1 : -1); break;
      case 'ArrowDown': next = addDays(d, 7); break;
      case 'ArrowUp': next = addDays(d, -7); break;
      case 'Home': next = addDays(d, -((d.getDay() - weekStart(dateLocale(calFor)) + 7) % 7)); break;
      case 'End': next = addDays(d, 6 - ((d.getDay() - weekStart(dateLocale(calFor)) + 7) % 7)); break;
      case 'PageUp': next = addMonthsIn(viewCalendar(calFor), d, event.shiftKey ? -12 : -1); break;
      case 'PageDown': next = addMonthsIn(viewCalendar(calFor), d, event.shiftKey ? 12 : 1); break;
      default: return;
    }
    event.preventDefault();
    calMove(next);
  }

  function onCalClick(event) {
    var t = event.target.closest ? event.target.closest('button') : null;
    if (!t || !calFor) return;
    if (t.classList.contains('ins-cal-day')) {
      if (t.getAttribute('aria-disabled') === 'true') return;
      calChoose(parseIso(t.getAttribute('data-date')));
      return;
    }
    var act = t.getAttribute('data-ins-cal');
    if (act === 'prev' || act === 'next') {
      var sys = viewCalendar(calFor);
      calFocus = addMonthsIn(sys, calFocus, act === 'prev' ? -1 : 1);
      calView = monthStartIn(sys, calFocus);
      renderCal();
      var again = calEl.querySelector('[data-ins-cal="' + act + '"]');
      if (again) again.focus();
    } else if (act === 'system') {
      /* Stopped here: the switch is drawn as a segmented control, and the page's
         own segmented-control handler has nothing to do with it. */
      event.stopPropagation();
      var input = calFor, want = t.getAttribute('data-value');
      if (want === 'gregory') input.setAttribute('data-ins-calendar-view', 'gregory');
      else input.removeAttribute('data-ins-calendar-view');
      calView = monthStartIn(viewCalendar(input), calFocus);
      renderCal();
      /* The field says the same day in the calendar now on screen, and so do its
         min and max messages. */
      var chosen = dateOf(input);
      if (chosen) { input.value = formatDay(input, chosen); checkDate(input, chosen); }
      emit('ins:calendar', { input: input, calendar: want });
      var back = calEl.querySelector('[data-ins-cal="system"][data-value="' + want + '"]');
      if (back) back.focus();
    } else if (act === 'today') {
      var now = today(), b = dateBounds(calFor);
      if ((!b.min || now >= b.min) && (!b.max || now <= b.max)) calChoose(now);
      else calMove(now);
    } else if (act === 'clear') {
      setDate(calFor, null);
      closeCal(true);
    }
  }

  function onCalChange(event) {
    var sel = event.target, act = sel.getAttribute('data-ins-cal');
    if (act !== 'month' && act !== 'year') return;
    var sys = viewCalendar(calFor), vp = sys.parts(calView);
    var y = act === 'year' ? +sel.value : vp.y;
    var m = act === 'month' ? +sel.value : vp.m;
    calFocus = sys.fromParts(y, m, Math.min(sys.parts(calFocus).d, sys.monthLength(y, m)));
    calView = sys.fromParts(y, m, 1);
    renderCal();
    var again = calEl.querySelector('[data-ins-cal="' + act + '"]');
    if (again) again.focus();
  }

  /* --------------------------------------------------------------- wizard */
  /* Every panel of this wizard, in order, and the ones that are steps right now: a
     panel with `hidden` is skipped, so a flow whose length the server decides (two
     steps for one account, three for another) only has to hide what it does not
     need. */
  function wizardAll(w) {
    var all = w.querySelectorAll('.ins-wizard-panel'), out = [];
    for (var i = 0; i < all.length; i++) if (all[i].closest('.ins-wizard') === w) out.push(all[i]);
    return out;
  }

  function wizardPanels(w) {
    return wizardAll(w).filter(function (p) { return !p.hidden; });
  }

  function wizardIndex(w) {
    var panels = wizardPanels(w);
    for (var i = 0; i < panels.length; i++) if (panels[i].classList.contains('is-active')) return i;
    return 0;
  }

  /* Check the panel being left with the browser's own validation. In a
     `data-ins-validate` form every invalid field is reported, so each shows its own
     message; elsewhere only the first, because the browser's bubble can only point at
     one field at a time. */
  function panelValid(panel) {
    var form = panel.closest('form');
    var inline = !!(form && form.hasAttribute('data-ins-validate'));
    var controls = panel.querySelectorAll('input, select, textarea'), bad = null;
    for (var i = 0; i < controls.length; i++) {
      var c = controls[i];
      if (!c.willValidate || c.validity.valid) continue;
      if (!bad) bad = c;
      if (inline) c.reportValidity();
    }
    if (bad && !inline) bad.reportValidity();
    return !bad;
  }

  /* The optional viewport around the panels, `.ins-wizard-body`. With it the wizard
     changes height smoothly and can slide; without it the panels still animate. */
  function wizardBody(w) {
    var b = w.querySelector('.ins-wizard-body');
    return b && b.closest('.ins-wizard') === w ? b : null;
  }

  /* Where focus goes in a step that has just arrived: its `autofocus` field, so a
     login step can be typed into at once, or else the panel itself, so a screen
     reader starts reading the new step instead of staying on a button that has
     changed underneath it. */
  function wizardFocus(panel) {
    var field = panel.querySelector('[autofocus]');
    (field || panel).focus();
  }

  /* A running move is settled at once before another starts, so two quick clicks
     never leave two panels half-way. */
  function wizardSettle(w) {
    if (w.__insWizMove) w.__insWizMove.finish();
  }

  /* The move from one step to the next. Two ways, chosen on the wizard:

     - Pane (the default, for a form): the new step fades in from 16px away while the
       viewport, if there is one, takes its height at the same time. The old step is
       simply gone.
     - Slide (`data-ins-wizard-motion="slide"`, for a login card): the steps sit side
       by side and the whole row slides a step's width, the old one fading out as the
       new one fades in. The card is never smaller than the step it holds: when the
       new step is taller it grows first and then slides, when it is shorter it slides
       first and then shrinks.

     Either way the next step comes from the end of the line, the left on an Arabic
     page, as the next page of a book does, and going back reverses it. Reduced motion
     and effects-off swap in place. `landed` runs once the move is over, at once when
     nothing moves. */
  function wizardMove(w, from, to, back, before, landed) {
    var body = wizardBody(w);
    var slide = body && w.getAttribute('data-ins-wizard-motion') === 'slide';
    if (!from || !to || from === to || motionless() || !to.animate) { landed(); return; }
    var rtl = window.getComputedStyle(w).direction === 'rtl';
    var side = (rtl ? -1 : 1) * (back ? -1 : 1);
    var anims = [];
    var done = false;
    var move = { finish: function () { end(); } };
    var end = function () {
      if (done) return;
      done = true;
      anims.forEach(function (a) { try { a.cancel(); } catch (e) {} });
      from.removeAttribute('data-ins-leaving');
      to.removeAttribute('data-ins-entering');
      if (body) body.removeAttribute('data-ins-moving');
      if (w.__insWizMove === move) w.__insWizMove = null;
      landed();
    };
    w.__insWizMove = move;

    if (!slide) {
      var enter = to.animate([
        { opacity: 0, transform: 'translateX(' + (16 * side) + 'px)' },
        { opacity: 1, transform: 'none' }
      ], { duration: 250, easing: 'cubic-bezier(.22, 1, .36, 1)' });
      anims.push(enter);
      if (body) {
        var h = body.offsetHeight;
        if (Math.abs(h - before) > 1) {
          body.setAttribute('data-ins-moving', '');
          anims.push(body.animate([{ blockSize: before + 'px' }, { blockSize: h + 'px' }],
            { duration: 250, easing: 'cubic-bezier(.22, 1, .36, 1)' }));
        }
      }
      enter.onfinish = end;
      enter.oncancel = end;
      return;
    }

    /* Slide: the old step stays in the flow, the new one is laid beside it. The states
       are attributes, not classes, so a framework that renders the panels' classes
       (the React wrapper) cannot wipe them half-way through a slide. */
    from.setAttribute('data-ins-leaving', '');
    to.setAttribute('data-ins-entering', '');
    body.setAttribute('data-ins-moving', '');
    var gap = 24;
    var span = to.offsetWidth + gap;
    var after = to.offsetHeight + (body.offsetHeight - from.offsetHeight);
    var ease = 'cubic-bezier(.4, 0, .2, 1)';
    var grow = after > before;
    /* Each stage starts the next only if the move has not been settled meanwhile. */
    var run = function (list, next) {
      if (done) return;
      var last = null;
      list.forEach(function (spec) { last = spec[0].animate(spec[1], spec[2]); anims.push(last); });
      last.onfinish = function () { if (!done) next(); };
    };
    var resize = function (next) {
      if (Math.abs(after - before) <= 1) { next(); return; }
      run([[body, [{ blockSize: before + 'px' }, { blockSize: after + 'px' }], { duration: 320, easing: ease, fill: 'forwards' }]], next);
    };
    var hold = function () {
      anims.push(body.animate([{ blockSize: before + 'px' }, { blockSize: before + 'px' }], { duration: 1, fill: 'forwards' }));
    };
    var shift = function (next) {
      var opts = { duration: 320, easing: ease, fill: 'forwards' };
      run([
        [from, [{ transform: 'none', opacity: 1 }, { opacity: 0, offset: .7 }, { transform: 'translateX(' + (-span * side) + 'px)', opacity: 0 }], opts],
        [to, [{ transform: 'translateX(' + (span * side) + 'px)', opacity: 0 }, { opacity: 1, offset: .7 }, { transform: 'none', opacity: 1 }], opts]
      ], next);
    };
    /* The entering step waits off to the side until its turn. */
    anims.push(to.animate([{ transform: 'translateX(' + (span * side) + 'px)', opacity: 0 }], { duration: 1, fill: 'forwards' }));
    if (grow) resize(function () { shift(end); });
    else { hold(); shift(function () { resize(end); }); }
  }

  function wizardGo(target, index, opts) {
    var w = resolve(target);
    if (w && !w.classList.contains('ins-wizard')) w = w.closest('.ins-wizard');
    if (!w) return null;
    var o = opts || {};
    wizardSettle(w);
    var panels = wizardPanels(w), cur = wizardIndex(w);
    if (!panels.length) return null;
    var to = Math.max(0, Math.min(panels.length - 1, index));
    if (o.validate && to > cur) {
      if (!panelValid(panels[cur])) return cur;
      /* Asked first, for a step that has to reach a server before it is done: a
         listener that cancels this owns the move. The wizard waits, marked busy, until
         the page says `Insiyab.wizard(w, 'next')` or `Insiyab.wizard(w, 'stay')`. */
      var ask = emit('ins:wizard-leave', { el: w, step: cur, to: to, panel: panels[cur] }, true);
      if (ask.defaultPrevented) {
        w.setAttribute('aria-busy', 'true');
        return cur;
      }
    }
    w.removeAttribute('aria-busy');
    var body = wizardBody(w);
    var before = body ? body.offsetHeight : 0;
    var from = panels[cur];
    for (var i = 0; i < panels.length; i++) panels[i].classList.toggle('is-active', i === to);
    /* A step list keeps one item per panel, hidden panels included: an item whose
       panel is hidden goes with it. Each list is counted on its own. */
    var all = wizardAll(w), at = all.indexOf(panels[to]);
    var lists = w.querySelectorAll('.ins-steps');
    for (var l = 0; l < lists.length; l++) {
      if (lists[l].closest('.ins-wizard') !== w) continue;
      var steps = lists[l].querySelectorAll('.ins-steps-item');
      for (var j = 0; j < steps.length; j++) {
        steps[j].hidden = !!(all[j] && all[j].hidden);
        steps[j].classList.toggle('is-done', j < at);
        if (j === at) steps[j].setAttribute('aria-current', 'step');
        else steps[j].removeAttribute('aria-current');
      }
    }
    w.toggleAttribute('data-ins-first', to === 0);
    w.toggleAttribute('data-ins-last', to === panels.length - 1);
    /* An earlier answer said again on a later step: `data-ins-echo="#phone"` shows
       that field's value, so the person sees which number the code went to. */
    var echoes = w.querySelectorAll('[data-ins-echo]');
    for (var k = 0; k < echoes.length; k++) {
      var src = document.querySelector(echoes[k].getAttribute('data-ins-echo'));
      if (src && 'value' in src) echoes[k].textContent = src.value;
    }
    var arrived = panels[to];
    var slides = o.focus && from !== arrived && body && w.getAttribute('data-ins-wizard-motion') === 'slide' && !motionless();
    if (o.focus && !slides) wizardFocus(arrived);
    wizardMove(w, from, arrived, to < cur, before, function () {
      if (slides && arrived.classList.contains('is-active')) wizardFocus(arrived);
    });
    if (!o.quiet) emit('ins:wizard', { el: w, step: to, panel: arrived });
    return to;
  }

  /* ----------------------------------------------------------------- chip */
  /* The value a chip stands for: its hidden input's, else `data-value`, else its
     text. */
  function chipValue(chip) {
    var h = chip.querySelector('input[type="hidden"]');
    if (h) return h.value;
    return chip.hasAttribute('data-value') ? chip.getAttribute('data-value') : chip.textContent.trim();
  }

  /* A chip's × takes it away, unless `ins:chip-remove` is cancelled. Focus moves to
     the next chip's × (or the previous one's, or the field's box), so a keyboard can
     clear several in a row. */
  function chipRemove(chip) {
    if (!chip || !chip.parentNode) return false;
    var value = chipValue(chip);
    var ask = emit('ins:chip-remove', { el: chip, value: value }, true);
    if (ask.defaultPrevented) return false;
    var tags = chip.closest('.ins-tags');
    var near = chip.nextElementSibling, far = chip.previousElementSibling;
    var next = (near && near.querySelector('.ins-chip-x')) || (far && far.querySelector && far.querySelector('.ins-chip-x')) ||
      (tags && tagsInput(tags));
    var hadFocus = chip.contains(document.activeElement);
    chip.parentNode.removeChild(chip);
    if (tags) tagsDropped(tags, value);
    if (hadFocus && next) next.focus();
    return true;
  }

  /* ----------------------------------------------------------------- tags */
  function tagsInput(tags) { return tags.querySelector('.ins-tags-input') || tags.querySelector('input:not([type="hidden"])'); }

  function tagsChips(tags) {
    var out = [];
    for (var i = 0; i < tags.children.length; i++) if (tags.children[i].classList.contains('ins-chip')) out.push(tags.children[i]);
    return out;
  }

  function tagsValues(tags) { return tagsChips(tags).map(chipValue); }

  function tagsOption(tags, value) {
    var opts = tags.querySelectorAll('.ins-combo-option');
    for (var i = 0; i < opts.length; i++) {
      var v = opts[i].hasAttribute('data-value') ? opts[i].getAttribute('data-value') : opts[i].textContent.trim();
      if (v === value) return opts[i];
    }
    return null;
  }

  /* A value as a chip, before the box. Not twice, and not past `data-ins-max`. */
  function tagsAdd(tags, value, label) {
    value = String(value || '').trim();
    if (!value || tagsValues(tags).indexOf(value) !== -1) return false;
    var max = parseInt(tags.getAttribute('data-ins-max'), 10);
    if (max && tagsChips(tags).length >= max) return false;
    label = label || value;
    var chip = el('span', 'ins-chip', label);
    var x = el('button', 'ins-chip-x');
    x.type = 'button';
    x.setAttribute('aria-label', strings(tags).remove + ' ' + label);
    chip.appendChild(x);
    var name = tags.getAttribute('data-ins-name');
    if (name) {
      var h = el('input');
      h.type = 'hidden';
      h.name = name;
      h.value = value;
      chip.appendChild(h);
    } else {
      chip.setAttribute('data-value', value);
    }
    tags.insertBefore(chip, tagsInput(tags));
    var opt = tagsOption(tags, value);
    if (opt) { opt.setAttribute('data-ins-chosen', ''); opt.setAttribute('aria-selected', 'true'); }
    emit('ins:tags', { el: tags, values: tagsValues(tags), added: value });
    return true;
  }

  function tagsDropped(tags, value) {
    var opt = tagsOption(tags, value);
    if (opt) { opt.removeAttribute('data-ins-chosen'); opt.setAttribute('aria-selected', 'false'); }
    emit('ins:tags', { el: tags, values: tagsValues(tags), removed: value });
  }

  /* Whatever is typed becomes a tag when the field has no list to choose from, or
     says `data-ins-free`. */
  function tagsFree(tags) { return tags.hasAttribute('data-ins-free') || !tags.querySelector('.ins-combo-option'); }

  /* The keys a tags box adds to the autocomplete's: Enter or a comma make what was
     typed a tag, where free text is allowed; Backspace in an empty box takes the last
     chip back. Returns whether it used the key. */
  function onTagsKey(event) {
    var input = event.target, tags = input.closest ? input.closest('.ins-tags') : null;
    if (!tags || input !== tagsInput(tags) || event.isComposing) return false;
    var list = tags.querySelector('.ins-combo-list');
    var picking = list && list.hasAttribute('data-ins-open') && tags.querySelector('.ins-combo-option.is-active');
    if ((event.key === 'Enter' || event.key === ',' || event.key === '،') && !picking && tagsFree(tags) && input.value.trim()) {
      event.preventDefault();
      tagsAdd(tags, input.value);
      input.value = '';
      if (list) comboFilter(tags);
      return true;
    }
    if (event.key === 'Backspace' && !input.value && input.selectionStart === 0) {
      var chips = tagsChips(tags);
      if (chips.length) { event.preventDefault(); chipRemove(chips[chips.length - 1]); return true; }
    }
    return false;
  }

  /* ----------------------------------------------------------------- time */
  /* A time as `HH:MM`, from what a person typed: "14:30", "1430", "2:30 م",
     "2 pm", in either set of digits. Anything else: null. */
  function parseTime(text) {
    var t = toLatin(text).toLowerCase().replace(/[‎‏ ]/g, ' ').trim();
    if (!t) return null;
    var pm = /(pm|p\.m|م|مساء)/.test(t), am = /(am|a\.m|ص|صباح)/.test(t);
    var m = t.match(/(\d{1,2})\s*[:.٫]\s*(\d{2})/), h, min;
    if (m) { h = +m[1]; min = +m[2]; }
    else {
      var digits = (t.match(/\d+/) || [''])[0];
      if (!digits || digits.length > 4) return null;
      if (digits.length > 2) { h = +digits.slice(0, -2); min = +digits.slice(-2); }
      else { h = +digits; min = 0; }
    }
    if ((pm || am) && (h < 1 || h > 12)) return null;
    if (pm && h < 12) h += 12;
    if (am && h === 12) h = 0;
    if (h > 23 || min > 59) return null;
    return pad2(h) + ':' + pad2(min);
  }

  function minutesOf(hhmm) { var p = hhmm.split(':'); return +p[0] * 60 + +p[1]; }

  function formatTime(input, hhmm) {
    var p = hhmm.split(':');
    return dateFormat(langOf(input), { hour: 'numeric', minute: '2-digit' }).format(new Date(2000, 0, 1, +p[0], +p[1]));
  }

  function timeHidden(input) { var c = input.closest('.ins-combo'); return c && c.querySelector('input[type="hidden"]'); }

  /* Set a time field: its hidden value, the words in the box, the matching option
     marked. Refuses a time outside `min`–`max`. */
  function setTime(input, hhmm, quiet) {
    var hidden = timeHidden(input), combo = input.closest('.ins-combo');
    if (!hidden) return;
    var lo = input.getAttribute('data-ins-min'), hi = input.getAttribute('data-ins-max');
    var out = hhmm && ((lo && minutesOf(hhmm) < minutesOf(lo)) || (hi && minutesOf(hhmm) > minutesOf(hi)));
    if (out) { input.setCustomValidity(strings(input).badTime); return; }
    input.setCustomValidity('');
    var changed = hidden.value !== (hhmm || '');
    hidden.value = hhmm || '';
    input.value = hhmm ? formatTime(input, hhmm) : '';
    var opts = combo.querySelectorAll('.ins-combo-option');
    for (var i = 0; i < opts.length; i++) opts[i].setAttribute('aria-selected', String(opts[i].getAttribute('data-value') === hhmm));
    if (!quiet && changed) { fire(input, 'change'); emit('ins:time', { input: input, value: hidden.value }); }
  }

  /* What was typed, read as the person leaves the field. The words already showing
     are the value, untouched; text that is not a time marks the field invalid. */
  function commitTime(input) {
    var hidden = timeHidden(input);
    if (!hidden) return;
    var text = input.value.trim();
    if (hidden.value && text === formatTime(input, hidden.value)) return;
    if (!text) { setTime(input, ''); return; }
    var t = parseTime(text);
    if (t) setTime(input, t);
    else input.setCustomValidity(strings(input).badTime);
  }

  /* An empty time list opens scrolled to the hour it is now, without choosing it. */
  function timeNear(combo) {
    var list = combo.querySelector('.ins-combo-list'), now = new Date();
    var at = now.getHours() * 60 + now.getMinutes(), best = null, gap = Infinity;
    var opts = list.querySelectorAll('.ins-combo-option:not([hidden])');
    for (var i = 0; i < opts.length; i++) {
      var d = Math.abs(minutesOf(opts[i].getAttribute('data-value')) - at);
      if (d < gap) { gap = d; best = opts[i]; }
    }
    if (best) list.scrollTop = best.offsetTop - list.clientHeight / 3;
  }

  /* A date and a time sent together, where the group asks: `data-ins-datetime`. */
  function datetimeSync(group) {
    var out = group.querySelector('input[data-ins-datetime-value]');
    if (!out) return;
    var d = group.querySelector('[data-ins-date-ready]'), t = group.querySelector('[data-ins-time-ready]');
    var dv = d && dateValue(d) ? dateValue(d).value : '', tv = t && timeHidden(t) ? timeHidden(t).value : '';
    out.value = dv && tv ? dv + 'T' + tv : '';
  }

  /* -------------------------------------------------------------- counter */
  function countSync(control) {
    var words = control.getAttribute('data-ins-count') === 'words';
    var v = control.value || '';
    var n = words ? (v.trim() ? v.trim().split(/\s+/).length : 0) : v.length;
    var max = words ? parseInt(control.getAttribute('data-ins-max'), 10) : (control.maxLength > 0 ? control.maxLength : parseInt(control.getAttribute('data-ins-max'), 10));
    var out = resolve('#' + control.getAttribute('data-ins-count-el'));
    if (!out) return n;
    var t = strings(control);
    var text = out.getAttribute('data-ins-text') || (words ? (max ? t.words : t.wordsFree) : (max ? '{n}/{max}' : '{n}'));
    out.textContent = text.replace('{n}', n).replace('{max}', max || '');
    out.classList.toggle('is-near', !!max && n >= max * .9 && n <= max);
    out.classList.toggle('is-over', !!max && n > max);
    if (words && max) control.setCustomValidity(n > max ? t.tooManyWords.replace('{max}', max) : '');
    return n;
  }

  /* ------------------------------------------------------------ highlights */
  /* A mark (or a reference marker) and the note it points at, as one group: from a
     mark, its note and every mark sharing it; from a note, the marks pointing at it. */
  function noteGroup(node) {
    if (!node || !node.closest) return null;
    var mark = node.closest('[data-ins-note]'), ref, note;
    if (mark) {
      ref = mark.getAttribute('data-ins-note');
      note = resolve(ref);
    } else {
      note = node.closest('.ins-note[id]');
      if (!note) return null;
      ref = '#' + note.id;
    }
    var marks = document.querySelectorAll('[data-ins-note="' + ref + '"]');
    return marks.length ? { key: ref, marks: marks, note: note } : null;
  }

  var litGroup = null;
  function noteLight(group) {
    if (litGroup && group && litGroup.key === group.key) return;
    var parts = function (g, on) {
      for (var i = 0; i < g.marks.length; i++) g.marks[i].classList.toggle('is-lit', on);
      if (g.note) g.note.classList.toggle('is-lit', on);
    };
    if (litGroup) parts(litGroup, false);
    litGroup = group;
    if (group) parts(group, true);
  }

  /* ------------------------------------------------------------ countdown */
  /* Every running countdown, on one timer. Each keeps the moment it ends rather than
     a count of seconds, so a tab left in the background comes back to the right time
     instead of wherever its throttled ticks had got to. */
  var countdowns = [], countTimer = null;

  function countdownSet(node, value) {
    var v = String(value == null ? '' : value).trim();
    var end = /^\d+(\.\d+)?$/.test(v) ? Date.now() + parseFloat(v) * 1000 : Date.parse(v);
    if (isNaN(end)) return null;
    node.__insEnd = end;
    node.__insWarned = node.__insDone = false;
    node.classList.remove('is-warn', 'is-done');
    if (countdowns.indexOf(node) === -1) countdowns.push(node);
    if (!countTimer) countTimer = setInterval(countdownTick, 1000);
    countdownDraw(node);
    return countdownLeft(node);
  }

  function countdownLeft(node) { return Math.max(0, Math.ceil((node.__insEnd - Date.now()) / 1000)); }

  function countdownDraw(node) {
    var left = countdownLeft(node);
    var d = Math.floor(left / 86400), h = Math.floor(left % 86400 / 3600), m = Math.floor(left % 3600 / 60), sec = left % 60;
    var clock = (d || h ? (d ? pad2(h) : h) + ':' : '') + pad2(m) + ':' + pad2(sec);
    var days = node.querySelector('.ins-countdown-days'), time = node.querySelector('.ins-countdown-clock');
    if (!time) {
      node.textContent = '';
      days = node.appendChild(el('span', 'ins-countdown-days'));
      time = node.appendChild(el('span', 'ins-countdown-clock'));
    }
    days.hidden = !d;
    days.textContent = d ? strings(node).days.replace('{d}', d) : '';
    time.textContent = clock;
    var warn = parseFloat(node.getAttribute('data-ins-warn'));
    if (isNaN(warn)) warn = 60;
    if (!node.__insWarned && left <= warn && left > 0) {
      node.__insWarned = true;
      node.classList.add('is-warn');
      emit('ins:countdown', { el: node, left: left, state: 'warn' });
    }
    if (!node.__insDone && left === 0) {
      node.__insDone = true;
      node.classList.remove('is-warn');
      node.classList.add('is-done');
      emit('ins:countdown', { el: node, left: 0, state: 'end' });
    }
    return left;
  }

  function countdownTick() {
    for (var i = countdowns.length - 1; i >= 0; i--) {
      var node = countdowns[i];
      if (!document.documentElement.contains(node) || countdownDraw(node) === 0) countdowns.splice(i, 1);
    }
    if (!countdowns.length) { clearInterval(countTimer); countTimer = null; }
  }

  /* -------------------------------------------------------- save indicator */
  function saveState(target, state, text) {
    var node = resolve(target);
    if (!node) return null;
    var t = strings(node);
    var said = { saving: t.saving, saved: t.saved, error: t.saveError };
    node.setAttribute('data-ins-state', state);
    node.textContent = state === 'idle' ? '' : (text || node.getAttribute('data-ins-text-' + state) || said[state] || '');
    return state;
  }

  /* A form that keeps itself: a moment after the last change the indicator says
     "saving" and `ins:autosave` goes out with the form's data. A page that saves
     somewhere cancels it and calls `detail.done(true)` or `done(false)` when it
     knows; one that does not cancel it has kept the change itself (a local draft),
     and the indicator says so at once. A later change supersedes an earlier save
     still in flight. */
  function autosave(form) {
    clearTimeout(form.__insSaveTimer);
    var wait = parseInt(form.getAttribute('data-ins-autosave'), 10) || 800;
    form.__insSaveTimer = setTimeout(function () {
      var ind = form.querySelector('.ins-save') || (form.id && document.querySelector('.ins-save[data-ins-save-for="#' + form.id + '"]'));
      var seq = (form.__insSaveSeq || 0) + 1;
      form.__insSaveSeq = seq;
      if (ind) saveState(ind, 'saving');
      var done = function (ok) {
        if (form.__insSaveSeq !== seq) return;
        if (ind) saveState(ind, ok === false ? 'error' : 'saved');
      };
      var data = null;
      try { data = new FormData(form); } catch (e) { /* no FormData */ }
      var ask = emit('ins:autosave', { el: form, data: data, done: done }, true);
      if (!ask.defaultPrevented) done(true);
    }, wait);
  }

  /* ----------------------------------------------------------------- sums */
  /* The total of the chosen values in a region: `data-ins-sum="#matrix"`. */
  function sumSync(out) {
    var region = resolve(out.getAttribute('data-ins-sum'));
    if (!region) return;
    var boxes = region.querySelectorAll('input:checked'), total = 0;
    for (var i = 0; i < boxes.length; i++) { var n = parseFloat(boxes[i].value); if (!isNaN(n)) total += n; }
    out.textContent = String(Math.round(total * 100) / 100);
  }

  function sumsFor(node) {
    var outs = document.querySelectorAll('[data-ins-sum]');
    for (var i = 0; i < outs.length; i++) {
      var region = resolve(outs[i].getAttribute('data-ins-sum'));
      if (region && region.contains(node)) sumSync(outs[i]);
    }
  }

  /* ----------------------------------------------------------------- split */
  function splitHandle(split) {
    for (var i = 0; i < split.children.length; i++) if (split.children[i].classList.contains('ins-split-handle')) return split.children[i];
    return null;
  }

  function splitLimits(split) {
    return { min: parseFloat(split.getAttribute('data-ins-min')) || 20, max: parseFloat(split.getAttribute('data-ins-max')) || 80 };
  }

  /* The first pane's share, in percent, kept within the limits. */
  function splitSet(split, pct, keep) {
    var lim = splitLimits(split);
    pct = Math.max(lim.min, Math.min(lim.max, pct));
    split.style.setProperty('--ins-split', pct + '%');
    var handle = splitHandle(split);
    if (handle) handle.setAttribute('aria-valuenow', String(Math.round(pct)));
    var key = split.getAttribute('data-ins-split');
    if (keep) {
      if (key) write('ins-split:' + key, String(pct));
      emit('ins:split', { el: split, size: pct });
    }
    return pct;
  }

  function splitNow(split) { return parseFloat(split.style.getPropertyValue('--ins-split')) || 50; }

  /* Where the pointer is, as the first pane's share: from the start edge, which is
     the right one on an Arabic page, or from the top for a stacked split. */
  function splitAt(split, event) {
    var r = split.getBoundingClientRect();
    if (split.classList.contains('ins-split--v')) return (event.clientY - r.top) / r.height * 100;
    var rtl = window.getComputedStyle(split).direction === 'rtl';
    return (rtl ? r.right - event.clientX : event.clientX - r.left) / r.width * 100;
  }

  /* ---------------------------------------------------------------- table */
  function tableOf(node) { return node && node.closest ? node.closest('table') : null; }

  /* What a cell sorts and edits by: `data-sort-value` when the text is not the value
     (a date written out in words, say, sorted by its ISO form), else `data-value`,
     else the text. */
  function cellValue(td) {
    if (!td) return '';
    var v = td.getAttribute('data-sort-value');
    if (v === null) v = td.getAttribute('data-value');
    return (v === null ? td.textContent : v).trim();
  }

  /* A figure as a number, in either set of digits, with its thousands separators
     and whatever currency or percent sign stands around it. Not a figure: NaN. */
  function numberOf(text) {
    var t = toLatin(text).replace(/[\s,٬ ]/g, '').replace(/٫/g, '.').replace(/−/g, '-');
    var m = t.match(/^[^\d.-]*(-?\d*\.?\d+)[^\d]*$/);
    return m ? parseFloat(m[1]) : NaN;
  }

  /* Sort a table by one column: 'ascending', 'descending', or the other way from now
     when no direction is given. `ins:sort` is asked first; a page that sorts on the
     server cancels it, and then only the arrow moves. Rows with nothing to sort by go
     last either way. Numbers compare as numbers when every filled cell of the column
     holds one (or the column says `data-ins-sort="number"`); text compares in the
     page's language, with the digits inside it read as numbers. */
  function sortTable(th, dir) {
    var table = tableOf(th), body = table && table.tBodies[0];
    if (!body) return null;
    if (dir !== 'ascending' && dir !== 'descending') {
      dir = th.getAttribute('aria-sort') === 'ascending' ? 'descending' : 'ascending';
    }
    var column = th.cellIndex;
    var heads = th.parentNode.querySelectorAll('th[data-ins-sort]');
    for (var h = 0; h < heads.length; h++) heads[h].setAttribute('aria-sort', heads[h] === th ? dir : 'none');
    var ask = emit('ins:sort', { el: table, th: th, column: column, dir: dir }, true);
    if (ask.defaultPrevented) return dir;

    var rows = Array.prototype.slice.call(body.rows);
    var texts = rows.map(function (r) { return cellValue(r.cells[column]); });
    var kind = th.getAttribute('data-ins-sort');
    var numeric = kind === 'number' || (kind !== 'text' && texts.some(function (t) { return t !== ''; }) &&
      texts.every(function (t) { return t === '' || !isNaN(numberOf(t)); }));
    var keys = numeric ? texts.map(numberOf) : texts;
    var coll = null;
    try { coll = new Intl.Collator(langOf(table), { numeric: true, sensitivity: 'base' }); } catch (e) { /* plain order */ }
    var sign = dir === 'descending' ? -1 : 1;
    var order = rows.map(function (r, i) { return i; });
    order.sort(function (a, b) {
      var x = keys[a], y = keys[b];
      var ex = numeric ? isNaN(x) : x === '', ey = numeric ? isNaN(y) : y === '';
      if (ex || ey) return ex === ey ? a - b : (ex ? 1 : -1);
      var c = numeric ? x - y : coll ? coll.compare(x, y) : (x < y ? -1 : x > y ? 1 : 0);
      return c ? c * sign : a - b;
    });
    for (var i = 0; i < order.length; i++) body.appendChild(rows[order[i]]);
    return dir;
  }

  /* The rows' own checkboxes, not the header's and not a nested table's. */
  function selectBoxes(table) {
    var all = table.querySelectorAll('tbody input[data-ins-select]'), out = [];
    for (var i = 0; i < all.length; i++) if (tableOf(all[i]) === table && all[i].getAttribute('data-ins-select') !== 'all') out.push(all[i]);
    return out;
  }

  /* After any change to the boxes: each row marked as its box says, the header box
     checked, mixed or clear, and every bar tied to this table shown with its count or
     hidden. Returns the selected rows. */
  function selectSync(table, quiet) {
    var boxes = selectBoxes(table), rows = [];
    for (var i = 0; i < boxes.length; i++) {
      var tr = boxes[i].closest('tr');
      if (boxes[i].checked) rows.push(tr);
      if (!tr) continue;
      if (boxes[i].checked) tr.setAttribute('aria-selected', 'true');
      else tr.removeAttribute('aria-selected');
    }
    var head = table.querySelector('thead input[data-ins-select="all"]');
    if (head) {
      head.checked = rows.length > 0 && rows.length === boxes.length;
      head.indeterminate = rows.length > 0 && rows.length < boxes.length;
    }
    var bars = document.querySelectorAll('[data-ins-bulk]');
    for (var j = 0; j < bars.length; j++) {
      if (resolve(bars[j].getAttribute('data-ins-bulk')) !== table) continue;
      bars[j].hidden = rows.length === 0;
      var count = bars[j].querySelector('.ins-bulkbar-count');
      if (count) {
        var text = count.getAttribute('data-ins-text') || strings(count).selected;
        count.textContent = text.replace('{n}', rows.length);
      }
    }
    if (!quiet) emit('ins:select', { el: table, rows: rows, count: rows.length });
    return rows;
  }

  function selectAll(table, on) {
    var boxes = selectBoxes(table);
    for (var i = 0; i < boxes.length; i++) if (!boxes[i].disabled) boxes[i].checked = on;
  }

  /* The heading over a cell, to name the field that replaces it. */
  function columnName(td) {
    var table = tableOf(td), head = table && table.tHead && table.tHead.rows[0];
    var th = head && head.cells[td.cellIndex];
    return th ? th.textContent.trim() : '';
  }

  /* A cell turned into a field. What it held is kept, so Escape, a refused change or
     a failed save can put it back exactly as it was. */
  function editStart(td) {
    if (td.classList.contains('is-editing')) return;
    var kind = td.getAttribute('data-ins-edit') || 'text';
    var old = td.hasAttribute('data-value') ? td.getAttribute('data-value') : td.textContent.trim();
    var field;
    if (kind === 'select') {
      field = el('select', 'ins-select ins-select--sm');
      var opts = (td.getAttribute('data-ins-options') || '').split('|');
      for (var i = 0; i < opts.length; i++) {
        var o = el('option', '', opts[i].trim());
        o.selected = opts[i].trim() === old;
        field.appendChild(o);
      }
    } else {
      field = el('input', 'ins-input ins-input--sm');
      field.type = 'text';
      field.value = old;
      if (kind === 'number') { field.setAttribute('inputmode', 'decimal'); field.dir = 'ltr'; }
    }
    var name = columnName(td);
    if (name) field.setAttribute('aria-label', name);
    td.__insEdit = { old: old, html: td.innerHTML };
    td.classList.add('is-editing');
    td.textContent = '';
    td.appendChild(field);
    field.focus();
    if (field.select) field.select();
  }

  /* Keep or drop the change. `ins:edit` is asked before a change is kept: cancel it
     to refuse the value, or keep it and call `detail.revert()` later if the save it
     starts fails. Returns whether the cell changed. */
  function editEnd(td, keep) {
    var state = td.__insEdit;
    if (!state) return false;
    td.__insEdit = null;
    var field = td.querySelector('input, select');
    var value = field ? field.value.trim() : state.old;
    td.classList.remove('is-editing');
    var changed = !!keep && value !== state.old;
    if (changed) {
      var ask = emit('ins:edit', {
        el: td, row: td.parentNode, value: value, old: state.old,
        revert: function () { td.innerHTML = state.html; if (td.hasAttribute('data-value')) td.setAttribute('data-value', state.old); }
      }, true);
      if (ask.defaultPrevented) changed = false;
    }
    if (changed) {
      td.textContent = value;
      if (td.hasAttribute('data-value')) td.setAttribute('data-value', value);
    } else {
      td.innerHTML = state.html;
    }
    return changed;
  }

  /* -------------------------------------------------------------- confirm */
  /* `Insiyab.confirm('حذف العملية؟')` — a small modal with two buttons and a promise
     of which was pressed. It is a real `<dialog>` with a `method="dialog"` form, so
     Escape, the focus trap and the return value are all the platform's. Focus lands
     on Cancel, the button that does nothing: the confirmation that defaults to the
     destructive answer is the one somebody presses Enter through. */
  function confirmDialog(message, options) {
    var o = options || {};
    var t = strings(document.body);
    if (typeof window.Promise !== 'function' || !document.body) return null;
    return new window.Promise(function (done) {
      var dlg = el('dialog', 'ins-dialog ins-dialog--sm');
      var form = el('form');
      form.method = 'dialog';
      var textId = uid('ins-confirm');
      if (o.title) {
        var head = el('div', 'ins-dialog-head');
        var h = el('h2', 'ins-dialog-title', o.title);
        h.id = textId + '-title';
        head.appendChild(h);
        form.appendChild(head);
        dlg.setAttribute('aria-labelledby', h.id);
      } else {
        dlg.setAttribute('aria-label', t.ok);
      }
      var body = el('div', 'ins-dialog-body');
      var p = el('p', '', String(message));
      p.id = textId;
      p.style.margin = '0';
      body.appendChild(p);
      form.appendChild(body);
      dlg.setAttribute('aria-describedby', textId);
      var foot = el('div', 'ins-dialog-foot');
      var no = el('button', 'ins-btn ins-btn--bare', o.cancel || t.cancel);
      no.value = 'cancel';
      var yes = el('button', 'ins-btn ' + (o.tone === 'danger' ? 'ins-btn--danger' : 'ins-btn--primary'), o.confirm || t.ok);
      yes.value = 'ok';
      yes.style.marginInlineStart = 'auto';
      foot.appendChild(no);
      foot.appendChild(yes);
      form.appendChild(foot);
      dlg.appendChild(form);
      dlg.addEventListener('close', function () {
        done(dlg.returnValue === 'ok');
        setTimeout(function () { if (dlg.parentNode) dlg.parentNode.removeChild(dlg); }, 400);
      });
      document.body.appendChild(dlg);
      dlg.showModal();
      no.focus();
    });
  }

  /* ==========================================================================
     DELEGATION — one listener, so nothing needs wiring up
     ========================================================================== */

  var HANDLES = [
    '[data-ins-theme-toggle]',
    '[data-ins-sidebar]',
    '[data-ins-toast]',
    '[data-ins-dialog]',
    '[data-ins-dialog-close]',
    '[data-ins-fx]',
    '[data-ins-dismiss]',
    '[data-ins-navbar]',
    '[data-ins-password]',
    '[data-ins-toggle]',
    '[data-ins-spin]',
    '[data-ins-wizard="next"]',
    '[data-ins-wizard="prev"]',
    '[data-ins-wizard="restart"]',
    '.ins-date-btn',
    '.ins-sort',
    '.ins-chip-x',
    '[data-ins-select="none"]',
    '.ins-table td[data-ins-edit]',
    TAB,
    '.ins-seg > a',
    '.ins-seg > button'
  ].join(', ');

  document.addEventListener('click', function (event) {
    var el = event.target.closest ? event.target.closest(HANDLES) : null;
    if (!el) return;

    if (el.hasAttribute('data-ins-theme-toggle')) {
      /* An explicit value sets that theme; a bare attribute toggles. */
      var want = el.getAttribute('data-ins-theme-toggle');
      /* The switch itself is where the circle starts, so the page changes from the
         point of contact rather than from an arbitrary corner. */
      sweep(el, function () {
        if (want) theme(want); else toggleTheme();
      });
      if (el.tagName === 'A') event.preventDefault();
      return;
    }

    if (el.hasAttribute('data-ins-sidebar')) {
      sidebar(el.getAttribute('data-ins-sidebar') || 'toggle');
      if (el.tagName === 'A') event.preventDefault();
      return;
    }

    if (el.hasAttribute('data-ins-toast')) {
      toast(el.getAttribute('data-ins-toast'), el.getAttribute('data-ins-tone'));
      if (el.tagName === 'A') event.preventDefault();
      return;
    }

    if (el.hasAttribute('data-ins-dialog')) {
      dialog(el.getAttribute('data-ins-dialog'), 'open');
      if (el.tagName === 'A') event.preventDefault();
      return;
    }

    if (el.hasAttribute('data-ins-dialog-close')) {
      var owner = el.closest('dialog');
      if (owner) dialog(owner, 'close');
      return;
    }

    if (el.hasAttribute('data-ins-fx')) {
      var off = root.getAttribute('data-ins-fx') === 'off';
      if (off) root.removeAttribute('data-ins-fx'); else root.setAttribute('data-ins-fx', 'off');
      el.setAttribute('aria-pressed', String(!off));
      return;
    }

    if (el.hasAttribute('data-ins-dismiss')) {
      dismiss(el);
      if (el.tagName === 'A') event.preventDefault();
      return;
    }

    if (el.hasAttribute('data-ins-navbar')) {
      navbar(el.getAttribute('data-ins-navbar') || el);
      return;
    }

    /* Prevented whatever the element is: a <button> inside a <form> is a submit
       button unless it says otherwise, and showing a password must not send it. */
    if (el.hasAttribute('data-ins-password')) {
      event.preventDefault();
      togglePassword(el);
      return;
    }

    if (el.hasAttribute('data-ins-toggle')) {
      toggleButton(el);
      return;
    }

    /* Prevented for the same reason as the password toggle: a stepper's buttons sit
       inside a form, and a − that submits it is not a −. */
    if (el.hasAttribute('data-ins-spin')) {
      event.preventDefault();
      spin(el);
      return;
    }

    if (el.hasAttribute('data-ins-wizard')) {
      event.preventDefault();
      var w = el.closest('.ins-wizard');
      /* Not while a step is still reaching the server. */
      if (!w || w.getAttribute('aria-busy') === 'true') return;
      var how = el.getAttribute('data-ins-wizard');
      var dest = how === 'restart' ? 0 : wizardIndex(w) + (how === 'next' ? 1 : -1);
      wizardGo(w, dest, { validate: how === 'next', focus: true });
      return;
    }

    if (el.classList.contains('ins-chip-x')) {
      event.preventDefault();
      chipRemove(el.closest('.ins-chip'));
      return;
    }

    if (el.classList.contains('ins-sort')) {
      event.preventDefault();
      sortTable(el.closest('th'));
      return;
    }

    /* "Clear" in a selection's bar: every row of its table let go. */
    if (el.getAttribute('data-ins-select') === 'none') {
      var bar = el.closest('[data-ins-bulk]');
      var owned = bar && resolve(bar.getAttribute('data-ins-bulk'));
      if (owned) { selectAll(owned, false); selectSync(owned); }
      return;
    }

    if (el.hasAttribute('data-ins-edit')) {
      editStart(el);
      return;
    }

    if (el.classList.contains('ins-date-btn')) {
      event.preventDefault();
      var field = (el.closest('.ins-input-group') || el.parentNode).querySelector('[data-ins-date-ready]');
      if (field && calFor === field) closeCal(true);
      else if (field) openCal(field, true);
      return;
    }

    /* A tab with a panel on this page. Checked before the segmented control, because
       a segment can be a tab; `selectTab` moves the segment's selection as well. */
    if (el.matches(TAB) && tabPanel(el)) {
      selectTab(el);
      if (el.tagName === 'A') event.preventDefault();
      return;
    }

    /* A segmented control moves its own selection. Only when the segment is not a
       link to somewhere else: a `.ins-seg > a` with a real href is navigation, and
       marking it active here would flash the wrong segment before the page leaves. */
    if (el.parentNode && el.parentNode.classList.contains('ins-seg')) {
      var href = el.getAttribute('href');
      if (el.tagName === 'A' && href && href.charAt(0) !== '#') return;
      var seg = el.parentNode;
      for (var i = 0; i < seg.children.length; i++) {
        var child = seg.children[i];
        child.classList.toggle('is-active', child === el);
        if (child.hasAttribute('role') || seg.getAttribute('role') === 'tablist') {
          child.setAttribute('aria-selected', String(child === el));
        }
      }
      emit('ins:seg', { value: el.getAttribute('data-ins-value') || el.textContent.trim(), el: el });
      if (el.tagName === 'A') event.preventDefault();
    }
  }, false);

  /* A popover is a <details>, so it opens and is keyboard reachable with no script.
     Clicking elsewhere closes it here; Escape and the arrow keys are `onPopKey`
     above — this comment used to say the platform supplied Escape, and it does not. */
  document.addEventListener('click', function (event) {
    var open = document.querySelectorAll('details.ins-pop[open]');
    for (var i = 0; i < open.length; i++) {
      if (!open[i].contains(event.target)) open[i].removeAttribute('open');
    }
  }, true);

  /* Choosing an item closes its menu and hands focus back to the trigger — except a
     checkbox item, which is a setting being flipped, and whose menu stays open for
     the next one. A radio item moves the check within its group, or within the
     menu when it has none. */
  document.addEventListener('click', function (event) {
    var item = event.target.closest ? event.target.closest('.ins-pop-item') : null;
    var pop = item && item.closest(POP);
    if (!pop) return;
    var role = item.getAttribute('role');
    if (role === 'menuitemcheckbox') {
      var on = item.getAttribute('aria-checked') !== 'true';
      item.setAttribute('aria-checked', String(on));
      emit('ins:menu', { item: item, checked: on });
      return;
    }
    if (role === 'menuitemradio') {
      var group = item.closest('[role="group"]') || item.closest('.ins-pop-body');
      var radios = group.querySelectorAll('[role="menuitemradio"]');
      for (var i = 0; i < radios.length; i++) radios[i].setAttribute('aria-checked', String(radios[i] === item));
      emit('ins:menu', { item: item, checked: true });
    }
    closePop(pop, true);
  }, false);

  /* `toggle` does not bubble, so it is caught on the way down. One menu open at a
     time, however it was opened, and each one measured against the viewport as it
     opens. */
  document.addEventListener('toggle', function (event) {
    var pop = event.target;
    if (!pop.matches || !pop.matches(POP)) return;
    if (!pop.open) { pop.removeAttribute('data-ins-flip'); return; }
    var open = document.querySelectorAll(POP + '[open]');
    for (var i = 0; i < open.length; i++) {
      if (open[i] !== pop && !open[i].contains(pop)) open[i].open = false;
    }
    flipPop(pop);
  }, true);

  /* Tabbing out of an open menu closes it. Only when focus has actually gone
     somewhere: a click on the menu's own padding moves focus to nothing, and
     closing on that would shut the menu under the pointer. */
  document.addEventListener('focusout', function (event) {
    var pop = event.target.closest ? event.target.closest(POP + '[open]') : null;
    var to = event.relatedTarget;
    if (pop && to && !pop.contains(to)) pop.open = false;
  }, false);

  /* A sidebar link: a same-page one moves the selection here and now; one that loads
     another page leaves a note for that page to fly the marker from. */
  document.addEventListener('click', function (event) {
    var link = event.target.closest ? event.target.closest('.ins-shell-side a.ins-shell-link[href]') : null;
    if (!link || !navigates(event, link)) return;
    if (samePage(link)) { sidebarSelect(link); return; }
    var side = link.closest('.ins-shell-side'), old = navActive(side);
    var note = { top: side.scrollTop, t: Date.now() };
    if (old && old !== link && old.href) note.from = old.href;
    try {
      window.sessionStorage.setItem(NAV_KEY, JSON.stringify(note));
    } catch (e) { /* no storage: the page simply arrives with its marker in place */ }
  }, false);

  /* A drawer closes on a click outside it. The click lands on the <dialog> itself
     — that is where a backdrop's clicks go — so the test is whether it fell outside
     the drawer's own box. A modal dialog does not do this, deliberately: it is
     usually a form, and a stray click should not throw one away. */
  document.addEventListener('click', function (event) {
    var d = event.target;
    if (!d || d.tagName !== 'DIALOG' || !d.open || !d.classList.contains('ins-drawer')) return;
    var r = d.getBoundingClientRect();
    if (event.clientX < r.left || event.clientX > r.right ||
        event.clientY < r.top || event.clientY > r.bottom) dialog(d, 'close');
  }, false);

  /* Following a link in an open navbar menu closes it — on a page that does not
     navigate away, a fragment link, the menu would otherwise stay over the content
     it just scrolled to. */
  document.addEventListener('click', function (event) {
    var link = event.target.closest ? event.target.closest('.ins-navbar-link') : null;
    var bar = link && link.closest('.ins-navbar.is-open');
    if (bar) navbar(bar, false);
  }, false);

  /* The tooltip's triggers. Pointer events rather than mouse events, so a pen gets
     tips and a finger does not — a touch has no hover to reveal one, and a tip that
     appears under the finger that just pressed is covering the thing it describes. */
  document.addEventListener('pointerover', function (event) {
    if (event.pointerType === 'touch') return;
    var el = event.target.closest ? event.target.closest(TIP) : null;
    if (!el) return;
    clearTimeout(tipTimer);
    if (el === tipOwner) return;
    var delay = (tipOwner || Date.now() < tipWarmUntil) ? 0 : 400;
    tipTimer = setTimeout(function () { showTip(el); }, delay);
  }, false);

  document.addEventListener('pointerout', function (event) {
    var el = event.target.closest ? event.target.closest(TIP) : null;
    if (!el) return;
    var to = event.relatedTarget;
    if (to && (el.contains(to) || (tipEl && tipEl.contains(to)))) return;
    if (el === tipOwner) hideTipSoon();
    else clearTimeout(tipTimer);
  }, false);

  /* Keyboard focus only: a click also focuses a button, and a tip that appears on
     every click is a tip in the way. */
  document.addEventListener('focusin', function (event) {
    var el = event.target.closest ? event.target.closest(TIP) : null;
    if (!el) return;
    var keyboard = true;
    try { keyboard = el.matches(':focus-visible'); } catch (e) { /* old engine: show it */ }
    if (keyboard) showTip(el);
  }, false);

  document.addEventListener('focusout', function (event) {
    if (tipOwner && tipOwner.contains(event.target)) hideTip();
  }, false);

  document.addEventListener('pointerdown', function () { if (tipOwner) hideTip(); }, true);
  /* Captured, so a scroll inside any element scroller hides it too — the tip is
     fixed to the viewport and would otherwise be left floating over the wrong row. */
  window.addEventListener('scroll', function () { if (tipOwner) hideTip(); }, true);

  /* Validation: see `fieldMessage` above. `invalid` does not bubble either. */
  document.addEventListener('invalid', function (event) {
    var control = event.target;
    /* A field in a wizard step that is not showing cannot be focused or pointed at,
       so the browser's report would say nothing and the submit would simply not
       happen. Go to its step first — whether or not this form reports inline. */
    /* Only ever back, to an earlier step: that is where a field can be left invalid by
       the time the form is submitted from its last step. */
    var panel = control.closest && control.closest('.ins-wizard-panel');
    if (panel && !panel.classList.contains('is-active')) {
      var wiz = panel.closest('.ins-wizard');
      var there = wizardPanels(wiz).indexOf(panel);
      if (there !== -1 && there < wizardIndex(wiz)) wizardGo(wiz, there, { quiet: false });
    }
    if (!control.form || !control.form.hasAttribute('data-ins-validate')) return;
    /* Marked as reported, so the stylesheet can show it as invalid from now on. The
       browser's own `:user-invalid` would do, but it is only set by a submit attempt
       or by the person editing the field — not by `reportValidity()`, which is how a
       wizard checks one step. Tested: after a blocked "next" every field in the step
       was invalid and none of them said so. The mark clears itself: the stylesheet
       reads it together with `:invalid`. */
    control.setAttribute('data-ins-reported', '');
    var msg = fieldMessage(control);
    if (!msg) return;
    event.preventDefault();
    if (msg.hasAttribute('data-ins-auto') || !msg.textContent.trim()) {
      msg.setAttribute('data-ins-auto', '');
      /* The browser's message is in the *browser's* language, which need not be
         the page's: an English Chrome on an Arabic page says "Please fill out this
         field." `dir="auto"` lets the sentence take its own direction, so its full
         stop and its mark land at the right ends rather than mirrored. */
      msg.setAttribute('dir', 'auto');
      msg.textContent = control.validationMessage;
    }
    if (!firstInvalid) {
      firstInvalid = control;
      setTimeout(function () {
        if (firstInvalid) firstInvalid.focus();
        firstInvalid = null;
      }, 0);
    }
  }, true);

  /* Keep a filled-in message current as the person types — "required" becomes
     "not an email address" becomes nothing. */
  document.addEventListener('input', function (event) {
    var control = event.target;
    if (!control.form || !control.form.hasAttribute('data-ins-validate')) return;
    var msg = fieldMessage(control);
    if (msg && msg.hasAttribute('data-ins-auto')) msg.textContent = control.validationMessage;
  }, true);

  /* Enter in a wizard's field means "next". Taken on the key itself, before the
     browser's implicit submission, which would first check the *whole* form: every
     required field in the steps still to come would fail and the step would never
     move. Only the step on screen is checked, and a server step is asked as usual. */
  document.addEventListener('keydown', function (event) {
    if (event.key !== 'Enter' || event.isComposing || event.defaultPrevented) return;
    var t = event.target;
    if (!t || t.tagName !== 'INPUT' || /^(checkbox|radio|submit|button|reset|image|file)$/i.test(t.type)) return;
    var w = t.form && t.form.classList.contains('ins-wizard') ? t.form : null;
    if (!w || w.hasAttribute('data-ins-last') || wizardPanels(w).length < 2) return;
    event.preventDefault();
    if (w.getAttribute('aria-busy') === 'true') return;
    wizardGo(w, wizardIndex(w) + 1, { validate: true, focus: true });
  }, true);

  /* And a submit that arrives before the last step anyway (a submit button in a
     step, say) is taken as "next" too. */
  document.addEventListener('submit', function (event) {
    var w = event.target.classList && event.target.classList.contains('ins-wizard') ? event.target : null;
    if (!w || w.hasAttribute('data-ins-last')) return;
    var panels = wizardPanels(w);
    if (panels.length < 2) return;
    event.preventDefault();
    event.stopImmediatePropagation();
    if (w.getAttribute('aria-busy') === 'true') return;
    wizardGo(w, wizardIndex(w) + 1, { validate: true, focus: true });
  }, true);

  /* A confirmation in front of a click: `data-ins-confirm="حذف هذه العملية؟"`. The
     click is stopped before anything else sees it, the question is asked, and on a
     yes the very same click is replayed — so a link still navigates, a submit button
     still submits its form with its own name and value, and a `data-ins-*` control
     still does what it does, none of them knowing they were asked about. A control
     already styled as dangerous gets a dangerous confirm button. */
  document.addEventListener('click', function (event) {
    var el = event.target.closest ? event.target.closest('[data-ins-confirm]') : null;
    if (!el) return;
    if (el.hasAttribute('data-ins-confirmed')) { el.removeAttribute('data-ins-confirmed'); return; }
    event.preventDefault();
    event.stopImmediatePropagation();
    var danger = /\bins-(btn--danger|btn--ghost-danger|pop-item--bad)\b/.test(el.className);
    var asked = confirmDialog(el.getAttribute('data-ins-confirm'), {
      tone: danger ? 'danger' : el.getAttribute('data-ins-confirm-tone'),
      confirm: el.getAttribute('data-ins-confirm-ok') || undefined
    });
    if (!asked) return;
    asked.then(function (yes) {
      if (!yes) return;
      el.setAttribute('data-ins-confirmed', '');
      el.click();
    });
  }, true);

  /* Everything the new form controls listen for as the person types. */
  document.addEventListener('input', function (event) {
    var t = event.target;
    if (!t.classList) return;
    if (t.classList.contains('ins-range')) rangeFill(t);
    if (t.hasAttribute('data-ins-count')) countSync(t);
    if (t.type === 'number' && t.closest('.ins-input-group') && t.closest('.ins-input-group').querySelector('.ins-spin')) syncSpin(t);
    var combo = t.closest && t.closest(COMBO);
    if (combo && t === comboInput(combo) && event.isTrusted !== false) {
      var any = comboFilter(combo);
      comboActivate(combo, null);
      comboOpen(combo, any);
    }
  }, false);

  /* The keyboard for the autocomplete and the date field. Registered on its own so
     neither has to know about the menus and tabs that own their arrow keys above. */
  document.addEventListener('keydown', function (event) {
    if (onTagsKey(event)) return;
    onComboKey(event);
    var t = event.target;
    if (event.key === 'Enter' && t.hasAttribute && t.hasAttribute('data-ins-time-ready') && !event.defaultPrevented) {
      commitTime(t);
      return;
    }
    if (!t.hasAttribute || !t.hasAttribute('data-ins-date-ready')) return;
    if (event.key === 'ArrowDown') { event.preventDefault(); openCal(t, true); }
    else if (event.key === 'Escape' && calFor === t) { event.preventDefault(); closeCal(false); }
    else if (event.key === 'Enter') { commitTyped(t); if (calFor === t) closeCal(false); }
  }, false);

  /* An autocomplete opens when its field is clicked, and chooses when an option is.
     A date field opens its calendar on a click too — but leaves the caret in the
     field, so a person who would rather type still can. */
  document.addEventListener('click', function (event) {
    var t = event.target;
    var opt = t.closest ? t.closest('.ins-combo-option') : null;
    if (opt) {
      var combo = opt.closest(COMBO);
      if (combo && opt.getAttribute('aria-disabled') !== 'true') { comboChoose(combo, opt); comboInput(combo).focus(); }
      return;
    }
    var tagBox = t.classList && t.classList.contains('ins-tags') ? t : null;
    if (tagBox) { tagsInput(tagBox).focus(); t = tagsInput(tagBox); }
    var box = t.closest && t.closest(COMBO);
    if (box && t === comboInput(box)) {
      var shown = comboList(box) && comboList(box).hasAttribute('data-ins-open');
      if (!shown) comboOpen(box, comboFilter(box));
      return;
    }
    if (t.hasAttribute && t.hasAttribute('data-ins-date-ready') && calFor !== t) openCal(t, false);
  }, false);

  /* A press on the list must not take focus from the field, or the field's blur
     would close the list before the click that was choosing from it arrived. */
  document.addEventListener('mousedown', function (event) {
    if (event.target.closest && event.target.closest('.ins-combo-list')) event.preventDefault();
  }, false);

  document.addEventListener('focusout', function (event) {
    var t = event.target, to = event.relatedTarget;
    var combo = t.closest && t.closest(COMBO);
    if (combo && (!to || !combo.contains(to))) comboOpen(combo, false);
    if (t.hasAttribute && t.hasAttribute('data-ins-time-ready') && !(to && combo && combo.contains(to))) commitTime(t);
    /* A date field reads what was typed as focus leaves it — unless it is leaving
       for its own calendar, which will set the value itself. */
    if (t.hasAttribute && t.hasAttribute('data-ins-date-ready') && !(to && calEl && calEl.contains(to))) commitTyped(t);
    if (calFor && calEl && calEl.contains(t) && to && !calEl.contains(to) && to !== calFor && to !== calButton(calFor)) closeCal(false);
  }, false);

  /* A press anywhere outside the calendar, its field and its button closes it. */
  document.addEventListener('pointerdown', function (event) {
    if (!calFor || !calEl) return;
    var t = event.target;
    if (calEl.contains(t) || t === calFor || calButton(calFor) === t || (calButton(calFor) && calButton(calFor).contains(t))) return;
    closeCal(false);
  }, true);

  /* It is fixed to the viewport, so it follows its field as the page moves. */
  window.addEventListener('scroll', function () { if (calFor) placeCal(); }, true);
  window.addEventListener('resize', function () { if (calFor) placeCal(); }, false);

  /* A form reset puts the native values back, and neither a slider's fill nor a date
     field's text is a native value — so they are redrawn once the reset has run. */
  document.addEventListener('reset', function (event) {
    var form = event.target;
    setTimeout(function () {
      var ranges = form.querySelectorAll('.ins-range');
      for (var i = 0; i < ranges.length; i++) rangeFill(ranges[i]);
      var dates = form.querySelectorAll('[data-ins-date-ready]');
      for (var j = 0; j < dates.length; j++) setDate(dates[j], dateOf(dates[j]), true);
      var counted = form.querySelectorAll('[data-ins-count]');
      for (var k = 0; k < counted.length; k++) countSync(counted[k]);
    }, 0);
  }, true);

  /* A password left showing is put back before the form goes, so the browser's
     password manager sees a password field and offers to save it. */
  document.addEventListener('submit', function (event) {
    var shown = event.target.querySelectorAll ? event.target.querySelectorAll('input[data-ins-secret]') : [];
    for (var i = 0; i < shown.length; i++) shown[i].type = 'password';
    var toggles = event.target.querySelectorAll ? event.target.querySelectorAll('[data-ins-password]') : [];
    for (var j = 0; j < toggles.length; j++) toggles[j].setAttribute('aria-pressed', 'false');
  }, true);

  document.addEventListener('change', function (event) {
    var group = event.target.closest && event.target.closest('[data-ins-datetime]');
    if (group) datetimeSync(group);
  }, false);

  /* Any change in a form that keeps itself. */
  ['input', 'change'].forEach(function (type) {
    document.addEventListener(type, function (event) {
      var form = event.target.closest && event.target.closest('form[data-ins-autosave]');
      if (form) autosave(form);
    }, false);
  });

  /* A choice changes the totals of any region it is in. */
  document.addEventListener('change', function (event) {
    if (event.target.type === 'radio' || event.target.type === 'checkbox') sumsFor(event.target);
  }, false);

  /* The split's handle, dragged. The pointer is captured, so the drag keeps going
     when it runs ahead of the handle, over an iframe or off the window's edge. */
  document.addEventListener('pointerdown', function (event) {
    var handle = event.target.closest ? event.target.closest('.ins-split-handle') : null;
    if (!handle || event.button > 0) return;
    var split = handle.parentNode;
    event.preventDefault();
    handle.focus();
    try { handle.setPointerCapture(event.pointerId); } catch (e) { /* old engine */ }
    handle.classList.add('is-dragging');
    split.classList.add('is-dragging');
    var move = function (e) { splitSet(split, splitAt(split, e)); };
    var up = function () {
      handle.removeEventListener('pointermove', move);
      handle.removeEventListener('pointerup', up);
      handle.removeEventListener('pointercancel', up);
      handle.classList.remove('is-dragging');
      split.classList.remove('is-dragging');
      splitSet(split, splitNow(split), true);
    };
    handle.addEventListener('pointermove', move);
    handle.addEventListener('pointerup', up);
    handle.addEventListener('pointercancel', up);
  }, false);

  document.addEventListener('dblclick', function (event) {
    var handle = event.target.closest ? event.target.closest('.ins-split-handle') : null;
    if (handle) splitSet(handle.parentNode, parseFloat(handle.parentNode.getAttribute('data-ins-split-default')) || 50, true);
  }, false);

  /* The handle from the keyboard: the arrows move the line the way they point, so on
     an Arabic page the left arrow widens the first pane, which is on the right. */
  document.addEventListener('keydown', function (event) {
    var handle = event.target;
    if (!handle.classList || !handle.classList.contains('ins-split-handle')) return;
    var split = handle.parentNode, now = splitNow(split), lim = splitLimits(split), v = split.classList.contains('ins-split--v');
    var rtl = window.getComputedStyle(split).direction === 'rtl';
    var grow = v ? 'ArrowDown' : (rtl ? 'ArrowLeft' : 'ArrowRight');
    var shrink = v ? 'ArrowUp' : (rtl ? 'ArrowRight' : 'ArrowLeft');
    var to = null, step = event.shiftKey ? 10 : 2;
    if (event.key === grow) to = now + step;
    else if (event.key === shrink) to = now - step;
    else if (event.key === 'Home') to = lim.min;
    else if (event.key === 'End') to = lim.max;
    else if (event.key === 'Enter') to = parseFloat(split.getAttribute('data-ins-split-default')) || 50;
    if (to === null) return;
    event.preventDefault();
    splitSet(split, to, true);
  }, false);

  /* Marks and their notes light together under the pointer and on keyboard focus;
     a click on a mark brings its note into view. */
  document.addEventListener('pointerover', function (event) { noteLight(noteGroup(event.target)); }, false);
  document.addEventListener('focusin', function (event) { noteLight(noteGroup(event.target)); }, false);
  document.addEventListener('focusout', function (event) {
    if (!event.relatedTarget || !noteGroup(event.relatedTarget)) noteLight(null);
  }, false);
  document.addEventListener('click', function (event) {
    var mark = event.target.closest ? event.target.closest('.ins-mark[data-ins-note]') : null;
    var note = mark && resolve(mark.getAttribute('data-ins-note'));
    if (note && note.scrollIntoView) note.scrollIntoView({ block: 'nearest', behavior: motionless() ? 'auto' : 'smooth' });
  }, false);

  /* A table's checkboxes. The header box takes every row with it; a row box with
     Shift held takes every row between it and the last one clicked. */
  document.addEventListener('click', function (event) {
    var box = event.target;
    if (!box.matches || !box.matches('.ins-table tbody input[data-ins-select]')) return;
    var table = tableOf(box), boxes = selectBoxes(table);
    var last = table.__insLastBox, from = boxes.indexOf(last), to = boxes.indexOf(box);
    if (event.shiftKey && from !== -1 && to !== -1) {
      for (var i = Math.min(from, to); i <= Math.max(from, to); i++) if (!boxes[i].disabled) boxes[i].checked = box.checked;
    }
    table.__insLastBox = box;
  }, false);

  document.addEventListener('change', function (event) {
    var box = event.target;
    if (!box.matches || !box.matches('input[data-ins-select]')) return;
    var table = tableOf(box);
    if (!table) return;
    if (box.getAttribute('data-ins-select') === 'all') selectAll(table, box.checked);
    selectSync(table);
  }, false);

  /* An editable cell: Enter or F2 opens it from the keyboard; in its field, Enter
     keeps the change and Escape drops it, and focus goes back to the cell either way,
     so the keyboard can carry on down the column. Leaving the field keeps it. */
  document.addEventListener('keydown', function (event) {
    var t = event.target;
    if (!t.closest || event.isComposing) return;
    if (t.matches('.ins-table td[data-ins-edit]') && (event.key === 'Enter' || event.key === 'F2')) {
      event.preventDefault();
      editStart(t);
      return;
    }
    var td = t.closest('.ins-table td.is-editing');
    if (!td || (event.key !== 'Enter' && event.key !== 'Escape')) return;
    event.preventDefault();
    event.stopPropagation();
    editEnd(td, event.key === 'Enter');
    td.focus();
  }, true);

  document.addEventListener('focusout', function (event) {
    var td = event.target.closest && event.target.closest('.ins-table td.is-editing');
    if (td && !(event.relatedTarget && td.contains(event.relatedTarget))) editEnd(td, true);
  }, false);

  /* The drawer's scrim. The CSS shows it as the sidebar's sibling, so it has no
     handler of its own to hang this on. */
  document.addEventListener('click', function (event) {
    if (event.target.classList && event.target.classList.contains('ins-shell-scrim')) {
      sidebar('closed');
    }
  }, false);

  /* The keyboard. Menus and tabs first, because they own their arrow keys; then
     Escape for everything that is not a <dialog> — which gets Escape from the
     platform, as the drawer does, being one. The shell's sidebar drawer, the navbar
     menu and the tooltip are built out of classes and do not, and a panel you cannot
     dismiss from the keyboard is a trap on a narrow screen. */
  document.addEventListener('keydown', function (event) {
    /* An Escape already spent — by a calendar, an autocomplete, a dialog's menu — is
       not also a request to close the drawer behind it. */
    if (event.defaultPrevented) return;
    onPopKey(event);
    if (event.defaultPrevented) return;
    onTabKey(event);
    if (event.defaultPrevented) return;

    if (event.key !== 'Escape' && event.keyCode !== 27) return;
    if (tipOwner) hideTip();
    var panel = drawer();
    if (panel && panel.classList.contains('is-open')) sidebar('closed');
    var bars = document.querySelectorAll('.ins-navbar.is-open');
    for (var i = 0; i < bars.length; i++) {
      navbar(bars[i], false);
      var toggle = bars[i].querySelector('[data-ins-navbar]');
      if (toggle && bars[i].contains(document.activeElement)) toggle.focus();
    }
  }, false);

  /* Crossing the breakpoint with the drawer open would leave a column stuck in its
     open-drawer state, so the class is dropped on the way out. The collapsed
     preference is untouched: it belongs to the wide layout and is still wanted when
     the window grows back. */
  if (window.matchMedia) {
    var wide = window.matchMedia('(min-width: 901px)');
    var onWide = function (e) {
      if (e.matches) {
        var panel = drawer();
        if (panel) panel.classList.remove('is-open');
        /* The navbar's folded menu is the same case: above the line it is a row
           again, and an `is-open` left behind would reopen it on the way back down. */
        var bars = document.querySelectorAll('.ins-navbar.is-open');
        for (var i = 0; i < bars.length; i++) navbar(bars[i], false);
      }
    };
    if (wide.addEventListener) wide.addEventListener('change', onWide);
    else if (wide.addListener) wide.addListener(onWide);
  }

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
    pressToggles(scope);
    var skies = scope.querySelectorAll('.ins-daynight');
    for (var i = 0; i < skies.length; i++) {
      if (skies[i].querySelector('.ins-dn-sky')) continue;
      skies[i].insertAdjacentHTML('beforeend', DAYNIGHT);
    }
  });

  /* The selected face travels like the sidebar marker: the far edge reaches the new segment, then the near edge follows. */
  function segFly(seg, from, to) {
    if (!from || !to || from === to || motionless() || !to.animate || !seg.offsetWidth) return;
    var a = { x: from.offsetLeft, w: from.offsetWidth }, b = { x: to.offsetLeft, w: to.offsetWidth };
    var prev = seg.__insSeg;
    if (prev) {
      if (prev.face.parentNode) {
        var pcs = window.getComputedStyle(prev.face);
        var px = parseFloat(pcs.left), pw = parseFloat(pcs.width);
        if (!isNaN(px) && pw > 0) { a.x = px; a.w = pw; }
      }
      prev.halt();
    }
    var face = el('span', 'ins-seg-flight');
    face.setAttribute('aria-hidden', 'true');
    face.style.top = to.offsetTop + 'px';
    face.style.height = to.offsetHeight + 'px';
    seg.appendChild(face);
    seg.classList.add('ins-seg-moving');
    var left = Math.min(a.x, b.x), right = Math.max(a.x + a.w, b.x + b.w);
    var run = null;
    var flight = {
      face: face,
      land: function () {
        if (face.parentNode) face.parentNode.removeChild(face);
        if (seg.__insSeg !== flight) return;
        seg.__insSeg = null;
        var on = seg.querySelector(':scope > .is-active');
        if (on) on.style.transition = 'none';
        seg.classList.remove('ins-seg-moving');
        if (on) { void on.offsetWidth; on.style.transition = ''; }
      },
      halt: function () {
        if (run) { run.onfinish = run.oncancel = null; try { run.cancel(); } catch (e) {} }
        flight.land();
      }
    };
    seg.__insSeg = flight;
    try {
      run = face.animate([
        { left: a.x + 'px', width: a.w + 'px', easing: NAV_EASE_OUT },
        { left: left + 'px', width: (right - left) + 'px', offset: 1 / 3, easing: NAV_EASE_IN },
        { left: b.x + 'px', width: b.w + 'px' }
      ], { duration: 600, fill: 'both' });
    } catch (e) {
      flight.land();
      return;
    }
    run.onfinish = flight.land;
    run.oncancel = flight.land;
  }

  /* Watches the segments' classes, since several paths change the selection. */
  define('seg-slide', function (scope) {
    if (!window.MutationObserver) return;
    var segs = scope.querySelectorAll('.ins-seg');
    for (var i = 0; i < segs.length; i++) {
      if (segs[i].__insSegWatch) continue;
      segs[i].__insSegWatch = true;
      (function (seg) {
        new MutationObserver(function (list) {
          var from = null, to = null;
          for (var j = 0; j < list.length; j++) {
            var t = list[j].target;
            if (t.parentNode !== seg) continue;
            var was = /(^|\s)is-active(\s|$)/.test(list[j].oldValue || ''), now = t.classList.contains('is-active');
            if (was && !now) from = t;
            if (!was && now) to = t;
          }
          if (from && to) segFly(seg, from, to);
        }).observe(seg, { attributes: true, attributeFilter: ['class'], attributeOldValue: true, subtree: true });
      }(segs[i]));
    }
  });

  /* Tabs: every role and relationship the pattern needs, written once from the
     markup's own `data-ins-tab` references, and the first tab selected when the
     page did not say which. The markup stays two attributes deep. */
  define('tabs', function (scope) {
    var seen = [];
    var nodes = scope.querySelectorAll(TAB);
    for (var i = 0; i < nodes.length; i++) {
      var list = tabList(nodes[i]);
      if (seen.indexOf(list) !== -1) continue;
      seen.push(list);
      var tabs = tabsIn(list);
      if (!tabs.length) continue;
      list.setAttribute('role', 'tablist');
      var active = null;
      for (var j = 0; j < tabs.length; j++) {
        var tab = tabs[j], panel = tabPanel(tabs[j]);
        if (!tab.id) tab.id = uid('ins-tab');
        tab.setAttribute('role', 'tab');
        tab.setAttribute('aria-controls', panel.id);
        panel.setAttribute('role', 'tabpanel');
        panel.setAttribute('aria-labelledby', tab.id);
        /* A panel of plain text has nothing in it to Tab to, so the panel itself is
           the stop after the strip — the pattern's own recommendation. */
        if (!panel.hasAttribute('tabindex')) panel.setAttribute('tabindex', '0');
        if (!active && (tab.classList.contains('is-active') || tab.getAttribute('aria-selected') === 'true')) active = tab;
      }
      selectTab(active || tabs[0], false, true);
    }
  });

  /* Menus: a `.ins-pop` whose body holds items is a menu button, so it is marked as
     one. The items leave the tab order because the arrow keys now reach them; a
     popover holding anything else — a card, a form — is left as the disclosure it
     is. */
  define('menus', function (scope) {
    var pops = scope.querySelectorAll(POP);
    for (var i = 0; i < pops.length; i++) {
      var summary = pops[i].querySelector('summary');
      var body = pops[i].querySelector('.ins-pop-body');
      if (!summary || !body || !body.querySelector('.ins-pop-item')) continue;
      summary.setAttribute('aria-haspopup', 'menu');
      body.setAttribute('role', 'menu');
      var items = body.querySelectorAll('.ins-pop-item');
      for (var j = 0; j < items.length; j++) {
        if (!items[j].hasAttribute('role')) items[j].setAttribute('role', 'menuitem');
        items[j].setAttribute('tabindex', '-1');
      }
      var seps = body.querySelectorAll('.ins-pop-sep');
      for (var k = 0; k < seps.length; k++) seps[k].setAttribute('role', 'separator');
    }
  });

  define('navbar', function (scope) {
    var toggles = scope.querySelectorAll('[data-ins-navbar]');
    for (var i = 0; i < toggles.length; i++) {
      var bar = toggles[i].closest('.ins-navbar');
      var menu = bar && bar.querySelector('.ins-navbar-menu');
      toggles[i].setAttribute('aria-expanded', String(!!(bar && bar.classList.contains('is-open'))));
      if (menu) {
        if (!menu.id) menu.id = uid('ins-navbar-menu');
        toggles[i].setAttribute('aria-controls', menu.id);
      }
    }
  });

  /* A show-password control is a toggle, and a <button> in a form is a submit
     button until it is told otherwise. */
  define('password', function (scope) {
    var toggles = scope.querySelectorAll('[data-ins-password]');
    for (var i = 0; i < toggles.length; i++) {
      if (toggles[i].tagName === 'BUTTON' && !toggles[i].hasAttribute('type')) toggles[i].type = 'button';
      if (!toggles[i].hasAttribute('aria-pressed')) toggles[i].setAttribute('aria-pressed', 'false');
    }
  });

  define('toggles', function (scope) {
    var nodes = scope.querySelectorAll('[data-ins-toggle]');
    for (var i = 0; i < nodes.length; i++) {
      if (!nodes[i].hasAttribute('aria-pressed')) nodes[i].setAttribute('aria-pressed', 'false');
      if (nodes[i].tagName === 'BUTTON' && !nodes[i].hasAttribute('type')) nodes[i].type = 'button';
    }
  });

  /* `indeterminate` is a property with no attribute, so markup cannot say it. */
  define('indeterminate', function (scope) {
    var nodes = scope.querySelectorAll('input[type="checkbox"][data-ins-indeterminate]');
    for (var i = 0; i < nodes.length; i++) nodes[i].indeterminate = true;
  });

  define('ranges', function (scope) {
    var nodes = scope.querySelectorAll('.ins-range');
    for (var i = 0; i < nodes.length; i++) rangeFill(nodes[i]);
  });

  define('steppers', function (scope) {
    var btns = scope.querySelectorAll('.ins-spin');
    for (var i = 0; i < btns.length; i++) {
      if (btns[i].tagName === 'BUTTON' && !btns[i].hasAttribute('type')) btns[i].type = 'button';
      var input = spinInput(btns[i]);
      if (!input) continue;
      if (!input.id) input.id = uid('ins-number');
      btns[i].setAttribute('aria-controls', input.id);
      syncSpin(input);
    }
  });

  /* A time field: the native input turned into a text field with a list of times,
     built as an autocomplete so it shares that one's keys and look. Its hidden
     input takes the `name`, and its default value, so a form reset restores it. */
  define('times', function (scope) {
    var nodes = scope.querySelectorAll('input[data-ins-time]:not([data-ins-time-ready])');
    for (var i = 0; i < nodes.length; i++) {
      var input = nodes[i];
      var value = parseTime(input.value || input.getAttribute('value') || '') || '';
      var step = Math.max(1, Math.round((parseFloat(input.getAttribute('step')) || 1800) / 60));
      var lo = parseTime(input.getAttribute('min') || '') || '00:00';
      var hi = parseTime(input.getAttribute('max') || '') || '23:59';
      var combo = el('div', 'ins-combo ins-time');
      input.parentNode.insertBefore(combo, input);
      combo.appendChild(input);
      var hidden = el('input');
      hidden.type = 'hidden';
      if (input.name) { hidden.name = input.name; input.removeAttribute('name'); }
      hidden.setAttribute('value', value);
      hidden.value = value;
      combo.appendChild(hidden);
      var list = el('ul', 'ins-combo-list');
      for (var m = minutesOf(lo); m <= minutesOf(hi); m += step) {
        var hhmm = pad2(Math.floor(m / 60)) + ':' + pad2(m % 60);
        var opt = el('li', 'ins-combo-option', formatTime(input, hhmm));
        opt.setAttribute('data-value', hhmm);
        list.appendChild(opt);
      }
      combo.appendChild(list);
      if (input.hasAttribute('min')) input.setAttribute('data-ins-min', lo);
      if (input.hasAttribute('max')) input.setAttribute('data-ins-max', hi);
      input.removeAttribute('min');
      input.removeAttribute('max');
      input.removeAttribute('step');
      input.type = 'text';
      input.setAttribute('autocomplete', 'off');
      input.setAttribute('data-ins-time-ready', '');
      setTime(input, value, true);
    }
  });

  /* A tags field with a list is a multi-select, and an autocomplete underneath; the
     chips the server sent mark their options chosen. */
  define('tags', function (scope) {
    var all = scope.querySelectorAll('.ins-tags');
    for (var i = 0; i < all.length; i++) {
      var list = all[i].querySelector('.ins-combo-list');
      if (list) { all[i].classList.add('ins-combo'); list.setAttribute('aria-multiselectable', 'true'); }
      var chips = tagsChips(all[i]);
      for (var j = 0; j < chips.length; j++) {
        var opt = tagsOption(all[i], chipValue(chips[j]));
        if (opt) { opt.setAttribute('data-ins-chosen', ''); opt.setAttribute('aria-selected', 'true'); }
      }
    }
  });

  /* The combobox pattern, stamped from the markup: the field is the combobox, the
     list its listbox, and every option gets an id for `aria-activedescendant` to
     point at. An option already matching the field's value is marked chosen. */
  define('combos', function (scope) {
    var combos = scope.querySelectorAll(COMBO);
    for (var i = 0; i < combos.length; i++) {
      var input = comboInput(combos[i]), list = comboList(combos[i]);
      if (!input || !list) continue;
      if (!list.id) list.id = uid('ins-combo-list');
      input.setAttribute('role', 'combobox');
      input.setAttribute('aria-autocomplete', 'list');
      input.setAttribute('aria-controls', list.id);
      input.setAttribute('aria-expanded', 'false');
      if (!input.hasAttribute('autocomplete')) input.setAttribute('autocomplete', 'off');
      list.setAttribute('role', 'listbox');
      var opts = comboOptions(combos[i], false);
      for (var j = 0; j < opts.length; j++) {
        if (!opts[j].id) opts[j].id = uid('ins-opt');
        opts[j].setAttribute('role', 'option');
        var label = opts[j].getAttribute('data-label') || opts[j].textContent.trim();
        opts[j].setAttribute('aria-selected', String(!!input.value && label === input.value));
      }
      var empty = combos[i].querySelector('.ins-combo-empty');
      if (empty) { empty.setAttribute('role', 'presentation'); empty.hidden = true; }
    }
  });

  /* A counter under every counted field, tied to it for a screen reader. */
  define('counters', function (scope) {
    var nodes = scope.querySelectorAll('[data-ins-count]');
    for (var i = 0; i < nodes.length; i++) {
      var c = nodes[i];
      if (!c.getAttribute('data-ins-count-el')) {
        var out = el('span', 'ins-count');
        out.id = uid('ins-count');
        c.parentNode.insertBefore(out, c.nextSibling);
        c.setAttribute('data-ins-count-el', out.id);
        var ids = (c.getAttribute('aria-describedby') || '').split(/\s+/).filter(Boolean);
        ids.push(out.id);
        c.setAttribute('aria-describedby', ids.join(' '));
      }
      countSync(c);
    }
  });

  /* The date field, which is the one builder here that changes the markup it was
     given. The native date input is turned into a text field showing the date in the
     page's language; a hidden input takes over its `name`, so what reaches the server
     is the same ISO string the native field would have sent; and the calendar button
     is added to the group. The hidden input's *default* value is set too, so that a
     form reset puts the original date back. */
  define('dates', function (scope) {
    var nodes = scope.querySelectorAll('input[data-ins-date]:not([data-ins-date-ready])');
    for (var i = 0; i < nodes.length; i++) {
      var input = nodes[i];
      var value = input.value || input.getAttribute('value') || '';
      var hidden = document.createElement('input');
      hidden.type = 'hidden';
      hidden.id = uid('ins-date-value');
      if (input.name) { hidden.name = input.name; input.removeAttribute('name'); }
      hidden.setAttribute('value', parseIso(value) ? value : '');
      hidden.value = parseIso(value) ? value : '';
      input.parentNode.insertBefore(hidden, input.nextSibling);

      if (input.min) input.setAttribute('data-ins-min', input.min);
      if (input.max) input.setAttribute('data-ins-max', input.max);
      input.removeAttribute('min');
      input.removeAttribute('max');
      input.type = 'text';
      input.setAttribute('autocomplete', 'off');
      input.setAttribute('data-ins-date-value', '#' + hidden.id);
      input.setAttribute('data-ins-date-ready', '');
      setDate(input, parseIso(hidden.value), true);
      input.setCustomValidity('');

      var group = input.closest('.ins-input-group');
      if (group && !group.querySelector('.ins-date-btn')) {
        var btn = document.createElement('button');
        btn.type = 'button';
        btn.className = 'ins-date-btn';
        btn.setAttribute('aria-label', strings(input).choose);
        btn.setAttribute('aria-haspopup', 'dialog');
        btn.setAttribute('aria-expanded', 'false');
        /* Beside its own date, where a time shares the group; else at the end. */
        group.insertBefore(btn, group.querySelector('[data-ins-time]') ? hidden.nextSibling : null);
      }
    }
  });

  define('datetime', function (scope) {
    var groups = scope.querySelectorAll('[data-ins-datetime]');
    for (var i = 0; i < groups.length; i++) {
      if (groups[i].querySelector('input[data-ins-datetime-value]')) continue;
      var h = el('input');
      h.type = 'hidden';
      h.name = groups[i].getAttribute('data-ins-datetime');
      h.setAttribute('data-ins-datetime-value', '');
      groups[i].appendChild(h);
      datetimeSync(groups[i]);
    }
  });

  /* A matrix's radios named for their row and column; totals drawn once. */
  define('matrix', function (scope) {
    var tables = scope.querySelectorAll('.ins-matrix');
    for (var i = 0; i < tables.length; i++) {
      var head = tables[i].tHead && tables[i].tHead.rows[0];
      var body = tables[i].tBodies[0];
      if (!head || !body) continue;
      for (var r = 0; r < body.rows.length; r++) {
        var row = body.rows[r], name = row.cells[0] ? row.cells[0].textContent.trim() : '';
        for (var c = 1; c < row.cells.length; c++) {
          var input = row.cells[c].querySelector('input');
          var col = head.cells[c] ? head.cells[c].textContent.trim() : '';
          if (input && !input.hasAttribute('aria-label') && !input.closest('label.ins-matrix-cell')) input.setAttribute('aria-label', name + ': ' + col);
        }
      }
    }
    var sums = scope.querySelectorAll('[data-ins-sum]');
    for (var k = 0; k < sums.length; k++) sumSync(sums[k]);
  });

  /* A split gets its handle, and the share it was left at. */
  define('split', function (scope) {
    var splits = scope.querySelectorAll('.ins-split');
    for (var i = 0; i < splits.length; i++) {
      var split = splits[i];
      var handle = splitHandle(split);
      var panes = [];
      for (var j = 0; j < split.children.length; j++) if (split.children[j].classList.contains('ins-split-pane')) panes.push(split.children[j]);
      if (!handle && panes.length > 1) {
        handle = el('div', 'ins-split-handle');
        split.insertBefore(handle, panes[1]);
      }
      if (!handle) continue;
      var lim = splitLimits(split);
      handle.setAttribute('role', 'separator');
      handle.setAttribute('tabindex', '0');
      handle.setAttribute('aria-orientation', split.classList.contains('ins-split--v') ? 'horizontal' : 'vertical');
      handle.setAttribute('aria-valuemin', String(lim.min));
      handle.setAttribute('aria-valuemax', String(lim.max));
      if (!handle.hasAttribute('aria-label')) handle.setAttribute('aria-label', strings(split).resize);
      if (panes[0]) { if (!panes[0].id) panes[0].id = uid('ins-pane'); handle.setAttribute('aria-controls', panes[0].id); }
      var key = split.getAttribute('data-ins-split');
      var kept = key ? parseFloat(read('ins-split:' + key)) : NaN;
      splitSet(split, !isNaN(kept) ? kept : (parseFloat(split.getAttribute('data-ins-split-default')) || 50));
    }
  });

  define('countdowns', function (scope) {
    var nodes = scope.querySelectorAll('[data-ins-countdown]');
    for (var i = 0; i < nodes.length; i++) {
      if (nodes[i].__insEnd) continue;
      if (!nodes[i].hasAttribute('role')) nodes[i].setAttribute('role', 'timer');
      countdownSet(nodes[i], nodes[i].getAttribute('data-ins-countdown'));
    }
  });

  /* A save indicator is a status, so a screen reader hears "saved" when it lands. */
  define('save', function (scope) {
    var nodes = scope.querySelectorAll('.ins-save');
    for (var i = 0; i < nodes.length; i++) {
      if (!nodes[i].hasAttribute('role')) nodes[i].setAttribute('role', 'status');
      var st = nodes[i].getAttribute('data-ins-state');
      if (st && !nodes[i].textContent.trim()) saveState(nodes[i], st);
    }
  });

  /* A mark with a note is reachable from the keyboard, and reads its note out. */
  define('marks', function (scope) {
    var marks = scope.querySelectorAll('.ins-mark[data-ins-note]');
    for (var i = 0; i < marks.length; i++) {
      var note = resolve(marks[i].getAttribute('data-ins-note'));
      if (!marks[i].hasAttribute('tabindex')) marks[i].setAttribute('tabindex', '0');
      if (!note) continue;
      if (!note.id) note.id = uid('ins-note');
      var ids = (marks[i].getAttribute('aria-describedby') || '').split(/\s+/).filter(Boolean);
      if (ids.indexOf(note.id) === -1) { ids.push(note.id); marks[i].setAttribute('aria-describedby', ids.join(' ')); }
    }
  });

  /* A sortable heading becomes a button, so the keyboard can reach it, and says how
     its column is sorted — a page the server sent sorted says so with `aria-sort`. */
  define('table-sort', function (scope) {
    var ths = scope.querySelectorAll('.ins-table th[data-ins-sort]');
    for (var i = 0; i < ths.length; i++) {
      var th = ths[i];
      if (!th.hasAttribute('aria-sort')) th.setAttribute('aria-sort', 'none');
      if (th.querySelector('.ins-sort')) continue;
      var btn = el('button', 'ins-sort');
      btn.type = 'button';
      while (th.firstChild) btn.appendChild(th.firstChild);
      th.appendChild(btn);
    }
  });

  /* Rows the server sent checked are marked, and their bar shown, from the start. */
  define('table-select', function (scope) {
    var boxes = scope.querySelectorAll('.ins-table input[data-ins-select]'), seen = [];
    for (var i = 0; i < boxes.length; i++) {
      var table = tableOf(boxes[i]);
      if (!table || seen.indexOf(table) !== -1) continue;
      seen.push(table);
      selectSync(table, true);
    }
  });

  define('table-edit', function (scope) {
    var cells = scope.querySelectorAll('.ins-table td[data-ins-edit]');
    for (var i = 0; i < cells.length; i++) if (!cells[i].hasAttribute('tabindex')) cells[i].setAttribute('tabindex', '0');
  });

  define('wizards', function (scope) {
    var wizards = scope.querySelectorAll('.ins-wizard');
    for (var i = 0; i < wizards.length; i++) {
      var panels = wizardPanels(wizards[i]);
      for (var j = 0; j < panels.length; j++) {
        if (!panels[j].hasAttribute('tabindex')) panels[j].setAttribute('tabindex', '-1');
      }
      wizardGo(wizards[i], wizardIndex(wizards[i]), { quiet: true });
    }
  });

  /* The arrival half of the sidebar's journey: the previous page's note, read once
     and thrown away, and the marker flown from the item it names to this page's. */
  define('nav-arrival', function (scope) {
    if (scope !== document) return;
    var note = navNote();
    try { window.sessionStorage.removeItem(NAV_KEY); } catch (e) { /* read-only storage */ }
    var side = note && document.querySelector('.ins-shell-side');
    var to = side && navActive(side), from = null;
    if (to && note.from && to.href !== note.from) {
      var links = side.querySelectorAll('a.ins-shell-link[href]');
      for (var i = 0; i < links.length; i++) if (links[i].href === note.from) { from = links[i]; break; }
    }
    if (!navFly(side, from, to)) navArrived();
  });

  /* A field's hint and its message describe its control. Without the link a screen
     reader announces a red border, which is to say nothing at all. */
  define('field-messages', function (scope) {
    var msgs = scope.querySelectorAll('.ins-field > .ins-hint, .ins-field > .ins-error, .ins-field > .ins-success');
    for (var i = 0; i < msgs.length; i++) {
      var control = msgs[i].parentNode.querySelector('input, select, textarea');
      if (!control) continue;
      if (!msgs[i].id) msgs[i].id = uid('ins-msg');
      var ids = (control.getAttribute('aria-describedby') || '').split(/\s+/).filter(Boolean);
      if (ids.indexOf(msgs[i].id) === -1) {
        ids.push(msgs[i].id);
        control.setAttribute('aria-describedby', ids.join(' '));
      }
    }
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
    sidebarSelect: sidebarSelect,
    brand: brand,
    toast: toast,
    dialog: dialog,
    dismiss: dismiss,
    tab: function (target) { return selectTab(target, false); },
    navbar: navbar,
    confirm: confirmDialog,
    calendar: registerCalendar,
    /* Text the way the autocomplete compares it, for a plugin that searches, so a
       query finds the same things in both. */
    norm: norm,
    /* The sidebar's marker flight, for a plugin list that marks its current item
       the same way, so the two move alike. */
    flyMarker: flyMarker,
    /* Sort a table by a column: the <th>, or the table and a column number. */
    sort: function (target, column, dir) {
      var node = resolve(target);
      if (node && node.tagName === 'TABLE') {
        var head = node.tHead && node.tHead.rows[0];
        node = head ? head.cells[column] : null;
      } else {
        dir = column;
      }
      return node ? sortTable(node, dir) : null;
    },
    /* Seconds left on a countdown, or start it again: seconds, or the moment it ends. */
    countdown: function (target, value) {
      var node = resolve(target);
      if (!node) return null;
      return value === undefined ? (node.__insEnd ? countdownLeft(node) : null) : countdownSet(node, value);
    },
    /* A split's first pane share in percent, or set it. */
    split: function (target, size) {
      var split = resolve(target);
      if (!split) return null;
      return size === undefined ? splitNow(split) : splitSet(split, size, true);
    },
    /* 'saving', 'saved', 'error' or 'idle', with words of your own if you like. */
    saveState: saveState,
    /* The selected rows of a table; `true` or `false` selects or clears them all. */
    selection: function (target, all) {
      var table = resolve(target);
      if (!table) return null;
      if (all === true || all === false) { selectAll(table, all); return selectSync(table); }
      return selectSync(table, true);
    },
    /* The current step, or go to one with no checks: a number, 'next', 'prev' or
       'restart'. 'stay' ends a wait on the server without moving, after the page
       has shown why the step was refused. */
    wizard: function (target, step) {
      var w = resolve(target);
      if (w && !w.classList.contains('ins-wizard')) w = w.closest('.ins-wizard');
      if (!w) return null;
      if (step === undefined) return wizardIndex(w);
      if (step === 'stay') { w.removeAttribute('aria-busy'); return wizardIndex(w); }
      var cur = wizardIndex(w);
      var dest = step === 'next' ? cur + 1 : step === 'prev' ? cur - 1 : step === 'restart' ? 0 : step;
      return wizardGo(w, dest, { focus: typeof step === 'string' });
    },
    date: function (target, value) {
      var input = resolve(target);
      if (!input || !input.hasAttribute('data-ins-date-ready')) return null;
      if (value === undefined) { var h = dateValue(input); return h ? h.value : null; }
      setDate(input, value ? parseIso(value) : null);
      return value;
    },
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
