/* ==========================================================================
   Insiyab · Carousel plugin
   ==========================================================================

   Slides in a row, one view at a time. Load it after insiyab.js, with its
   stylesheet:

     <link rel="stylesheet" href="plugins/insiyab-carousel.css">
     <script src="insiyab.js"></script>
     <script src="plugins/insiyab-carousel.js"></script>

     <div class="ins-carousel" data-ins-carousel aria-label="أعمال مختارة">
       <div class="ins-carousel-slide">…</div>
       <div class="ins-carousel-slide">…</div>
     </div>

   The row is the browser's own scroll-snap scroller, so a finger, a trackpad and
   Shift+wheel all move it, and without the script it is still a row that swipes.
   The script adds what a scroller cannot say about itself: previous and next
   buttons (off at the ends), a dot per stop, ← and → in the direction the page
   reads, and "2 من 5" on each slide for a screen reader.

     .ins-carousel--2 / --3      two or three slides at a time where the carousel is
                                 wide enough, one where it is not
     .ins-carousel--peek         the edge of the next slide showing
     data-ins-carousel-dots="off"   no dots

   **It never moves on its own.** Nothing in this library does — a slide that
   leaves while someone is reading it is the reason. `ins:carousel` reports the
   slide in view; Insiyab.carousel(el, index) moves to one.
   ========================================================================== */

(function (window, document) {
  'use strict';

  var Insiyab = window.Insiyab;
  if (!Insiyab || !Insiyab.define) {
    if (window.console) window.console.warn('[insiyab-carousel] load insiyab.js before this plugin.');
    return;
  }

  var CAROUSEL = '[data-ins-carousel]';
  var TEXT = {
    ar: { role: 'عرض شرائح', slide: 'شريحة', of: '{i} من {n}', prev: 'السابق', next: 'التالي', go: 'الشريحة {i}' },
    en: { role: 'carousel', slide: 'slide', of: '{i} of {n}', prev: 'Previous', next: 'Next', go: 'Slide {i}' }
  };

  function lang(el) { var h = el.closest('[lang]') || document.documentElement; return h.getAttribute('lang') || 'en'; }
  function text(el) { return lang(el).slice(0, 2).toLowerCase() === 'ar' ? TEXT.ar : TEXT.en; }
  function fill(s, map) { return s.replace(/\{(\w+)\}/g, function (a, k) { return map[k] != null ? map[k] : a; }); }
  function make(tag, cls) { var el = document.createElement(tag); if (cls) el.className = cls; return el; }
  function rtl(el) { return window.getComputedStyle(el).direction === 'rtl'; }
  function still() {
    var root = document.documentElement;
    return root.getAttribute('data-ins-fx') === 'off' || (window.matchMedia && window.matchMedia('(prefers-reduced-motion: reduce)').matches);
  }

  function state(el) { return el._insCarousel; }

  /* How far along the row a slide starts, from the row's own starting edge. */
  function offset(st, slide) {
    var t = st.track.getBoundingClientRect(), s = slide.getBoundingClientRect();
    return st.rtl ? t.right - s.right : s.left - t.left;
  }

  /* The stops: the slides whose start the row can bring to its edge. Past the
     last of them the row has run out of room, so a wide view has fewer stops than
     slides. */
  function stops(st) {
    var room = st.track.scrollWidth - st.track.clientWidth;
    var along = Math.abs(st.track.scrollLeft), out = [];
    for (var i = 0; i < st.slides.length; i++) {
      var at = offset(st, st.slides[i]) + along;
      if (at <= room + 2 || i === 0) out.push({ index: i, at: Math.min(at, room) });
    }
    return out;
  }

  function current(st) {
    var along = Math.abs(st.track.scrollLeft), list = stops(st), best = 0, gap = Infinity;
    for (var i = 0; i < list.length; i++) {
      var d = Math.abs(list[i].at - along);
      if (d < gap) { gap = d; best = i; }
    }
    return best;
  }

  function go(el, stop) {
    var st = state(el), list = stops(st);
    stop = Math.max(0, Math.min(list.length - 1, stop));
    var delta = list[stop].at - Math.abs(st.track.scrollLeft);
    /* The last scroll's release is due any moment; left to fire, it would let go of
       this target as the new scroll starts. And a move that needs no scroll at all
       still has to let go of it, with no scroll event to say so. */
    clearTimeout(st.settle);
    st.settle = setTimeout(function () { st.target = null; paint(el); }, 600);
    st.track.scrollBy({ left: st.rtl ? -delta : delta, behavior: still() ? 'auto' : 'smooth' });
    st.target = stop;
    paint(el, stop);
    return stop;
  }

  function paint(el, at) {
    var st = state(el), t = text(el), list = stops(st);
    if (at == null) at = current(st);
    st.prev.disabled = at <= 0;
    st.next.disabled = at >= list.length - 1;
    if (st.dots) {
      if (st.dots.children.length !== list.length) {
        st.dots.textContent = '';
        for (var i = 0; i < list.length; i++) {
          var d = make('button', 'ins-carousel-dot');
          d.type = 'button';
          d.setAttribute('aria-label', fill(t.go, { i: i + 1 }));
          d.setAttribute('data-stop', String(i));
          st.dots.appendChild(d);
        }
      }
      for (var j = 0; j < st.dots.children.length; j++) {
        if (j === at) st.dots.children[j].setAttribute('aria-current', 'true');
        else st.dots.children[j].removeAttribute('aria-current');
      }
      st.dots.hidden = list.length < 2;
    }
    st.prev.hidden = st.next.hidden = list.length < 2;
    if (at !== st.at) {
      st.at = at;
      var detail = { carousel: el, index: list[at] ? list[at].index : 0, stop: at };
      var ev;
      try { ev = new CustomEvent('ins:carousel', { detail: detail, bubbles: true }); }
      catch (e) { ev = document.createEvent('CustomEvent'); ev.initCustomEvent('ins:carousel', true, false, detail); }
      if (st.ready) el.dispatchEvent(ev);
    }
  }

  function build(el) {
    if (el.hasAttribute('data-ins-carousel-ready')) return;
    el.setAttribute('data-ins-carousel-ready', '');
    el.classList.add('ins-carousel');
    var t = text(el);
    var track = el.querySelector('.ins-carousel-track');
    if (!track) {
      track = make('div', 'ins-carousel-track');
      while (el.firstChild) track.appendChild(el.firstChild);
      el.appendChild(track);
    }
    var slides = [];
    for (var c = track.firstElementChild; c; c = c.nextElementSibling) {
      c.classList.add('ins-carousel-slide');
      slides.push(c);
    }
    el.setAttribute('role', 'region');
    el.setAttribute('aria-roledescription', t.role);
    /* Nothing turns by itself, so a change is always the person's own: announced
       politely, not as an interruption. */
    track.setAttribute('aria-live', 'polite');
    track.tabIndex = 0;
    for (var i = 0; i < slides.length; i++) {
      slides[i].setAttribute('role', 'group');
      slides[i].setAttribute('aria-roledescription', t.slide);
      if (!slides[i].hasAttribute('aria-label')) slides[i].setAttribute('aria-label', fill(t.of, { i: i + 1, n: slides.length }));
    }
    var prev = make('button', 'ins-carousel-btn ins-carousel-btn--prev');
    prev.type = 'button';
    prev.setAttribute('aria-label', t.prev);
    var next = make('button', 'ins-carousel-btn ins-carousel-btn--next');
    next.type = 'button';
    next.setAttribute('aria-label', t.next);
    el.appendChild(prev);
    el.appendChild(next);
    var dots = null;
    if (el.getAttribute('data-ins-carousel-dots') !== 'off') {
      dots = make('div', 'ins-carousel-dots');
      el.appendChild(dots);
    }
    el._insCarousel = { track: track, slides: slides, prev: prev, next: next, dots: dots, at: -1, rtl: rtl(el), ready: false, target: null };

    prev.addEventListener('click', function () { go(el, (state(el).target != null ? state(el).target : current(state(el))) - 1); });
    next.addEventListener('click', function () { go(el, (state(el).target != null ? state(el).target : current(state(el))) + 1); });
    if (dots) dots.addEventListener('click', function (e) {
      var d = e.target.closest && e.target.closest('.ins-carousel-dot');
      if (d) go(el, +d.getAttribute('data-stop'));
    });
    var ticking = false;
    track.addEventListener('scroll', function () {
      if (ticking) return;
      ticking = true;
      window.requestAnimationFrame(function () {
        ticking = false;
        /* While a button's scroll is on its way, the buttons and dots already show
           where it is going; the scroll catches up to them, not the other way. */
        if (state(el).target == null) paint(el);
      });
    });
    track.addEventListener('scrollend', function () { state(el).target = null; paint(el); });
    /* Without scrollend, the target is let go once the row has been still a while. */
    track.addEventListener('scroll', function () {
      clearTimeout(state(el).settle);
      state(el).settle = setTimeout(function () { state(el).target = null; paint(el); }, 400);
    });
    el.addEventListener('keydown', function (e) {
      if (e.altKey || e.ctrlKey || e.metaKey || /^(INPUT|TEXTAREA|SELECT)$/.test(e.target.tagName) || e.target.isContentEditable) return;
      if (e.key !== 'ArrowLeft' && e.key !== 'ArrowRight' && e.key !== 'Home' && e.key !== 'End') return;
      var st = state(el), at = st.target != null ? st.target : current(st);
      e.preventDefault();
      if (e.key === 'Home') go(el, 0);
      else if (e.key === 'End') go(el, stops(st).length - 1);
      else go(el, at + ((e.key === 'ArrowRight') !== st.rtl ? 1 : -1));
    });
    paint(el);
    state(el).ready = true;
  }

  window.addEventListener('resize', function () {
    var all = document.querySelectorAll(CAROUSEL + '[data-ins-carousel-ready]');
    for (var i = 0; i < all.length; i++) paint(all[i]);
  }, false);

  function scan(scope) {
    var found = scope.querySelectorAll ? scope.querySelectorAll(CAROUSEL) : [];
    for (var i = 0; i < found.length; i++) build(found[i]);
    if (scope.matches && scope.matches(CAROUSEL)) build(scope);
  }

  /* Insiyab.carousel(el) → the stop in view (0-based); Insiyab.carousel(el, n)
     moves to stop n. */
  Insiyab.carousel = function (target, index) {
    var el = typeof target === 'string' ? document.querySelector(target) : target;
    if (!el || !state(el)) return null;
    if (index === undefined) return state(el).target != null ? state(el).target : current(state(el));
    return go(el, +index);
  };

  Insiyab.define('carousel', scan);
  if (document.readyState !== 'loading') scan(document);
})(window, document);
