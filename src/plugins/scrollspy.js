/* ==========================================================================
   Insiyab · Scrollspy plugin
   ==========================================================================

   A table of contents that follows the reader. Load it after insiyab.js, with its
   stylesheet if you want the ready-made contents list:

     <link rel="stylesheet" href="plugins/insiyab-scrollspy.css">
     <script src="insiyab.js"></script>
     <script src="plugins/insiyab-scrollspy.js"></script>

     <nav class="ins-toc" data-ins-scrollspy aria-label="في هذه الصفحة">
       <a href="#intro">مقدّمة</a>
       <a href="#setup">الإعداد</a>
     </nav>

   The link whose section is being read gets `is-active` and
   `aria-current="location"`. Any nav works — a navbar, a row of tabs, the list
   above — because that is all it touches.

   "Being read" is decided against whatever scrolls the sections: the page, the
   body when `data-ins-scrollbar="lead"` makes it the scroller, or a box with its
   own scrollbar. The current section is the last one whose top has passed a line
   a third of the way down that view (or `data-ins-scrollspy-offset` pixels from
   its top, for a page with a tall sticky header), and at the very end of the
   scroll it is the last one, however short. A click on a link marks it at once,
   and the marker is not moved again until the scroll it started has stopped.
   ========================================================================== */

(function (window, document) {
  'use strict';

  var Insiyab = window.Insiyab;
  if (!Insiyab || !Insiyab.define) {
    if (window.console) window.console.warn('[insiyab-scrollspy] load insiyab.js before this plugin.');
    return;
  }

  var SPY = '[data-ins-scrollspy]';
  var spies = [];

  function targetOf(link) {
    var href = link.getAttribute('href') || '';
    var at = href.indexOf('#');
    if (at === -1 || at === href.length - 1) return null;
    /* Only this page's sections: "other.html#x" is a link elsewhere. */
    if (at > 0 && link.pathname !== window.location.pathname) return null;
    var id = decodeURIComponent(href.slice(at + 1));
    return document.getElementById(id);
  }

  /* The element that scrolls a section: the nearest ancestor with a scrollbar of
     its own, or the page. */
  function scrollerOf(el) {
    for (var n = el.parentElement; n && n !== document.body && n !== document.documentElement; n = n.parentElement) {
      var o = window.getComputedStyle(n).overflowY;
      if ((o === 'auto' || o === 'scroll' || o === 'overlay') && n.scrollHeight > n.clientHeight) return n;
    }
    var body = document.body;
    if (body && window.getComputedStyle(body).overflowY !== 'visible' && body.scrollHeight > body.clientHeight) return body;
    return null;                                   /* the viewport */
  }

  function view(scroller) {
    if (!scroller) return { top: 0, height: window.innerHeight, end: Math.ceil(window.innerHeight + (window.scrollY || document.documentElement.scrollTop)) >= document.documentElement.scrollHeight - 2, start: (window.scrollY || document.documentElement.scrollTop) <= 0 };
    var r = scroller.getBoundingClientRect();
    return { top: r.top, height: scroller.clientHeight, end: Math.ceil(scroller.scrollTop + scroller.clientHeight) >= scroller.scrollHeight - 2, start: scroller.scrollTop <= 0 };
  }

  function build(nav) {
    if (nav.hasAttribute('data-ins-scrollspy-ready')) return;
    nav.setAttribute('data-ins-scrollspy-ready', '');
    var spy = { nav: nav, links: [], targets: [], scroller: undefined, active: null, held: false, timer: 0 };
    var links = nav.querySelectorAll('a[href*="#"]');
    for (var i = 0; i < links.length; i++) {
      var t = targetOf(links[i]);
      if (!t) continue;
      spy.links.push(links[i]);
      spy.targets.push(t);
    }
    if (!spy.links.length) return;
    spies.push(spy);
    nav.addEventListener('click', function (e) {
      var link = e.target.closest && e.target.closest('a');
      var i = spy.links.indexOf(link);
      if (i === -1) return;
      mark(spy, i);
      hold(spy);
    });
    update(spy);
  }

  /* After a click the page scrolls to the section on its own; the marker stays on
     the clicked link through that scroll instead of stepping through every section
     on the way. */
  function hold(spy) {
    spy.held = true;
    clearTimeout(spy.timer);
    spy.timer = setTimeout(function () { spy.held = false; }, 1200);
  }

  function release(spy) {
    clearTimeout(spy.timer);
    spy.timer = setTimeout(function () { spy.held = false; update(spy); }, 120);
  }

  function current(spy) {
    if (spy.scroller === undefined) spy.scroller = scrollerOf(spy.targets[0]);
    var v = view(spy.scroller);
    var own = parseFloat(spy.nav.getAttribute('data-ins-scrollspy-offset'));
    var line = v.top + (isNaN(own) ? v.height / 3 : own);
    if (v.end && !v.start) return spy.targets.length - 1;
    var at = -1;
    for (var i = 0; i < spy.targets.length; i++) {
      if (spy.targets[i].getBoundingClientRect().top <= line + 1) at = i;
    }
    return at;
  }

  function mark(spy, i) {
    var link = i === -1 ? null : spy.links[i];
    if (link === spy.active) return;
    if (spy.active) { spy.active.classList.remove('is-active'); spy.active.removeAttribute('aria-current'); }
    spy.active = link;
    if (link) {
      link.classList.add('is-active');
      link.setAttribute('aria-current', 'location');
      /* A long list with its own scrollbar keeps the current link in sight — its
         own scroll only, never the page's. */
      var nav = spy.nav;
      if (nav.scrollHeight > nav.clientHeight + 1) {
        var lr = link.getBoundingClientRect(), nr = nav.getBoundingClientRect();
        if (lr.top < nr.top) nav.scrollTop -= nr.top - lr.top + 8;
        else if (lr.bottom > nr.bottom) nav.scrollTop += lr.bottom - nr.bottom + 8;
      }
    }
    var detail = { nav: spy.nav, link: link, section: i === -1 ? null : spy.targets[i] };
    var event;
    try { event = new CustomEvent('ins:scrollspy', { detail: detail, bubbles: true }); }
    catch (e) { event = document.createEvent('CustomEvent'); event.initCustomEvent('ins:scrollspy', true, false, detail); }
    spy.nav.dispatchEvent(event);
  }

  function update(spy) {
    if (spy.held || !spy.nav.isConnected) return;
    mark(spy, current(spy));
  }

  var ticking = false;
  function onScroll() {
    for (var i = 0; i < spies.length; i++) if (spies[i].held) release(spies[i]);
    if (ticking) return;
    ticking = true;
    window.requestAnimationFrame(function () {
      ticking = false;
      for (var j = 0; j < spies.length; j++) update(spies[j]);
    });
  }
  /* Capture, so a box's own scroll is heard too: scroll events do not bubble. */
  window.addEventListener('scroll', onScroll, true);
  window.addEventListener('resize', function () {
    for (var i = 0; i < spies.length; i++) { spies[i].scroller = undefined; update(spies[i]); }
  }, false);

  function scan(scope) {
    var found = scope.querySelectorAll ? scope.querySelectorAll(SPY) : [];
    for (var i = 0; i < found.length; i++) build(found[i]);
    if (scope.matches && scope.matches(SPY)) build(scope);
  }

  /* Insiyab.scrollspy(nav) measures again — after content above the sections grew,
     say — and returns the current link. */
  Insiyab.scrollspy = function (target) {
    var nav = typeof target === 'string' ? document.querySelector(target) : target;
    for (var i = 0; i < spies.length; i++) {
      if (spies[i].nav !== nav) continue;
      spies[i].scroller = undefined;
      spies[i].held = false;
      update(spies[i]);
      return spies[i].active;
    }
    return null;
  };

  Insiyab.define('scrollspy', scan);
  if (document.readyState !== 'loading') scan(document);
})(window, document);
