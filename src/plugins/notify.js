// Insiyab · notification centre plugin: a bell whose menu lists notifications, with the unread count on its corner. Load it after insiyab.js, with insiyab-notify.css.
(function (window, document) {
  'use strict';

  var Insiyab = window.Insiyab;
  if (!Insiyab || !Insiyab.define) {
    if (window.console) window.console.warn('[insiyab-notify] load insiyab.js before this plugin.');
    return;
  }

  var BOX = 'details[data-ins-notify]';
  var ITEM = '.ins-notify-list .ins-list-item';

  var TEXT = {
    ar: {
      title: 'الإشعارات', unread: 'غير مقروء', empty: 'لا إشعارات',
      label: function (base, n) {
        if (!n) return base;
        return base + '، ' + (n === 1 ? 'إشعار واحد غير مقروء' : n === 2 ? 'إشعاران غير مقروءين' : n + ' غير مقروءة');
      }
    },
    en: {
      title: 'Notifications', unread: 'Unread', empty: 'No notifications',
      label: function (base, n) { return n ? base + ', ' + n + ' unread' : base; }
    }
  };

  function lang(el) { var h = el.closest('[lang]') || document.documentElement; return h.getAttribute('lang') || 'en'; }
  function text(el) { return lang(el).slice(0, 2).toLowerCase() === 'ar' ? TEXT.ar : TEXT.en; }
  function make(tag, cls, words) {
    var el = document.createElement(tag);
    if (cls) el.className = cls;
    if (words != null) el.textContent = words;
    return el;
  }
  function still() {
    var root = document.documentElement;
    return root.getAttribute('data-ins-fx') === 'off' || !!(window.matchMedia && window.matchMedia('(prefers-reduced-motion: reduce)').matches);
  }
  function child(el, cls) {
    for (var c = el && el.firstElementChild; c; c = c.nextElementSibling) if (c.classList.contains(cls)) return c;
    return null;
  }

  function parts(box) {
    var body = child(box, 'ins-pop-body');
    return { summary: box.querySelector('summary'), body: body, list: body && body.querySelector('.ins-notify-list') };
  }

  function items(box) {
    var list = parts(box).list;
    return list ? Array.prototype.slice.call(list.querySelectorAll('.ins-list-item')) : [];
  }

  // The count, the bell's name, the hidden "unread" words, and the empty state, all from the items as they are now.
  function sync(box) {
    var st = box._insNotify, p = parts(box), t = text(box), all = items(box), n = 0;
    for (var i = 0; i < all.length; i++) {
      var on = all[i].hasAttribute('data-ins-unread'), flag = child(all[i], 'ins-notify-flag');
      if (on) n++;
      if (on && !flag) {
        flag = make('span', 'ins-sr-only ins-notify-flag', t.unread);
        all[i].insertBefore(flag, all[i].firstChild);
      } else if (!on && flag) {
        all[i].removeChild(flag);
      }
    }
    st.count.textContent = n > 99 ? '99+' : String(n);
    st.count.hidden = !n;
    var label = t.label(st.base, n);
    if (p.summary.getAttribute('aria-label') !== label) p.summary.setAttribute('aria-label', label);
    var none = !all.length;
    if (none && !st.empty) {
      st.empty = child(p.body, 'ins-empty');
      if (!st.empty) {
        st.empty = make('div', 'ins-empty ins-notify-empty');
        st.empty.appendChild(make('span', 'ins-empty-title', t.empty));
        var foot = child(p.body, 'ins-notify-foot');
        p.body.insertBefore(st.empty, foot);
      }
    }
    if (st.empty && st.empty.hidden !== !none) st.empty.hidden = !none;
    if (p.list && p.list.hidden !== none) p.list.hidden = none;
    if (st.ready && n > st.n) ring(box);
    st.n = n;
    return n;
  }

  // One short shake of the bell for a notification that has just arrived.
  function ring(box) {
    var summary = parts(box).summary, bell = summary && (summary.querySelector('svg') || summary);
    if (!bell || !bell.animate || still()) return;
    bell.animate([
      { transform: 'rotate(0)' }, { transform: 'rotate(14deg)' }, { transform: 'rotate(-12deg)' },
      { transform: 'rotate(8deg)' }, { transform: 'rotate(-4deg)' }, { transform: 'rotate(0)' }
    ], { duration: 650, easing: 'ease-in-out' });
  }

  function emit(box, detail) {
    var event;
    try { event = new CustomEvent('ins:notify', { detail: detail, bubbles: true }); }
    catch (e) { event = document.createEvent('CustomEvent'); event.initCustomEvent('ins:notify', true, false, detail); }
    box.dispatchEvent(event);
  }

  // Marks one item, or 'all', as read; the event goes out only when something did change, and only for the person's own click.
  function read(box, which, quiet) {
    var list = which === 'all' ? items(box) : [which], changed = false;
    for (var i = 0; i < list.length; i++) {
      if (list[i] && list[i].hasAttribute('data-ins-unread')) { list[i].removeAttribute('data-ins-unread'); changed = true; }
    }
    var n = sync(box);
    if (changed && !quiet) emit(box, { el: box, item: which === 'all' ? null : which, all: which === 'all' });
    return n;
  }

  function build(box) {
    if (box._insNotify) return;
    var p = parts(box);
    if (!p.summary || !p.body) return;
    var t = text(box);
    var st = box._insNotify = { base: p.summary.getAttribute('aria-label') || p.summary.textContent.trim() || t.title, n: 0, ready: false, empty: null };
    st.count = make('span', 'ins-notify-count');
    st.count.setAttribute('aria-hidden', 'true');
    st.count.hidden = true;
    p.summary.appendChild(st.count);
    sync(box);
    st.ready = true;
    if (window.MutationObserver) {
      st.watch = new window.MutationObserver(function () { if (box._insNotify) sync(box); });
      st.watch.observe(p.body, { childList: true, subtree: true, attributes: true, attributeFilter: ['data-ins-unread'] });
    }
    box.setAttribute('data-ins-notify-ready', '');
  }

  function scan(scope) {
    var root = scope || document;
    var boxes = root.querySelectorAll ? root.querySelectorAll(BOX) : [];
    for (var i = 0; i < boxes.length; i++) build(boxes[i]);
    if (root.matches && root.matches(BOX)) build(root);
  }

  document.addEventListener('click', function (e) {
    var t = e.target;
    if (!t.closest) return;
    var all = t.closest('[data-ins-notify-read]'), box = (all || t).closest(BOX);
    if (!box || !box._insNotify) return;
    if (all) { read(box, 'all'); return; }
    var item = t.closest(ITEM);
    if (item && box.contains(item)) read(box, item);
  }, false);

  // Insiyab.notify(box) gives the unread count; Insiyab.notify(box, item) or (box, 'all') marks read, quietly, and gives the count left.
  Insiyab.notify = function (target, which) {
    var box = typeof target === 'string' ? document.querySelector(target) : target;
    if (box && !box.matches(BOX) && box.closest) box = box.closest(BOX);
    if (!box) return null;
    build(box);
    if (!box._insNotify) return null;
    if (which === undefined) return sync(box);
    if (typeof which === 'string' && which !== 'all') which = box.querySelector(which);
    return read(box, which, true);
  };

  Insiyab.define('notify', scan);
  if (document.readyState !== 'loading') scan(document);
})(window, document);
