/*! Insiyab UI v0.6.0 · https://github.com/Mohammad-Diab/insiyab-ui#readme · MIT (fonts: OFL 1.1, see fonts/LICENSE-*.txt) */
/* ==========================================================================
   Insiyab · Command palette plugin
   ==========================================================================

   One search box for everywhere a page can go and everything it can do, opened
   from the keyboard. Load it after insiyab.js, with its stylesheet:

     <link rel="stylesheet" href="plugins/insiyab-palette.css">
     <script src="insiyab.js"></script>
     <script src="plugins/insiyab-palette.js"></script>

   and write the items as ordinary links and buttons in a dialog:

     <dialog class="ins-dialog ins-palette" data-ins-palette>
       <div data-ins-palette-group="الصفحات">
         <a href="/invoices" data-keywords="bills فواتير">الفواتير</a>
       </div>
       <div data-ins-palette-group="إجراءات">
         <button data-ins-theme-toggle>تبديل السمة</button>
       </div>
     </dialog>

   Choosing an item is clicking it. A link goes where it points and a button does
   whatever it already did — a `data-ins-*` attribute, a listener of the page's own —
   so an item needs nothing written for the palette. The plugin builds the rest
   around them: the field, the list, the headings, the keyboard.

     data-ins-palette-from=".ins-shell-side"   also offer every link in there, with
                                               its icon and its sidebar heading
                                               (not the logo, nor one marked
                                               data-ins-palette-skip)
     data-ins-palette-keys="mod+k /"            the shortcuts (the default); "none"
     data-ins-palette-recent="5"               how many recent items to show; "0"
     data-ins-palette-placeholder="…"          the field's placeholder

   From code: Insiyab.palette(target?, 'open' | 'close' | 'toggle'), and
   Insiyab.palette.add(items, target?), which makes the dialog too if the page has
   none. Every choice fires `ins:palette`, cancelable, before the item runs.
   ========================================================================== */

(function (window, document) {
  'use strict';

  var Insiyab = window.Insiyab;
  if (!Insiyab || !Insiyab.norm) {
    if (window.console) window.console.warn('[insiyab-palette] load insiyab.js before this plugin.');
    return;
  }
  var norm = Insiyab.norm;

  var PALETTE = '[data-ins-palette]';
  var ITEM = '.ins-palette-item';
  var STORE = 'insiyab-palette-recent';
  var SVG = 'http://www.w3.org/2000/svg';

  var TEXT = {
    ar: {
      label: 'لوحة الأوامر', placeholder: 'ابحث عن صفحة أو أمر…', empty: 'لا نتائج',
      recent: 'الأخيرة', move: 'للتنقل', choose: 'للاختيار', close: 'للإغلاق',
      count: function (n) { return n === 0 ? 'لا نتائج' : n === 1 ? 'نتيجة واحدة' : n === 2 ? 'نتيجتان' : n + (n <= 10 ? ' نتائج' : ' نتيجة'); }
    },
    en: {
      label: 'Command palette', placeholder: 'Search pages and commands…', empty: 'No results',
      recent: 'Recent', move: 'to move', choose: 'to choose', close: 'to close',
      count: function (n) { return n === 0 ? 'No results' : n === 1 ? '1 result' : n + ' results'; }
    }
  };

  function text(el) {
    var host = el.closest('[lang]') || document.documentElement;
    return (host.getAttribute('lang') || 'en').slice(0, 2).toLowerCase() === 'ar' ? TEXT.ar : TEXT.en;
  }

  var seq = 0;
  function uid(prefix) { return prefix + '-' + (++seq); }

  function make(tag, cls, content) {
    var el = document.createElement(tag);
    if (cls) el.className = cls;
    if (content != null) el.textContent = content;
    return el;
  }

  function warn(message) { if (window.console) window.console.warn('[insiyab-palette] ' + message); }

  /* ------------------------------------------------------------- the items */

  /* What an item is called: its `data-label`, or its text without the icon, the
     shortcut, a count badge (a sidebar link's "8") and the group name the palette
     adds to it. */
  function labelOf(el) {
    if (el.hasAttribute('data-label')) return el.getAttribute('data-label');
    var out = '';
    (function walk(node) {
      for (var c = node.firstChild; c; c = c.nextSibling) {
        if (c.nodeType === 3) out += c.nodeValue;
        else if (c.nodeType === 1 && !c.matches('svg, kbd, .ins-kbd, .ins-badge, .ins-shell-link-badge, .ins-palette-where')) walk(c);
      }
    })(el);
    return out.replace(/\s+/g, ' ').trim();
  }

  /* What an item is remembered by, across pages: a key it was given, else where it
     goes, else its name. */
  function keyOf(el) {
    return el.getAttribute('data-ins-palette-key') || (el.tagName === 'A' && el.href) || 'label:' + labelOf(el);
  }

  var order = 0;

  /* Makes a link or button one of the palette's options. The element itself is kept
     — moved, never copied — so whatever listens to it still does. */
  function adopt(dlg, el, group) {
    el.classList.add('ins-palette-item');
    el.setAttribute('role', 'option');
    el.setAttribute('aria-selected', 'false');
    el.setAttribute('tabindex', '-1');
    el.setAttribute('data-ins-palette-order', String(++order));
    if (!el.id) el.id = uid('ins-palette-item');
    if (el.tagName === 'BUTTON' && !el.hasAttribute('type')) el.type = 'button';
    if (group) {
      el.setAttribute('data-ins-palette-group', group);
      if (!el.querySelector('.ins-palette-where')) {
        var where = make('span', 'ins-palette-where', group);
        where.setAttribute('aria-hidden', 'true');
        el.appendChild(where);
      }
    }
    parts(dlg).list.appendChild(el);
    return el;
  }

  function groupOf(el, within) {
    var g = el.closest('[data-ins-palette-group]');
    return g && within.contains(g) ? g.getAttribute('data-ins-palette-group') : '';
  }

  /* The heading a link sits under in the navigation it was collected from: the
     nearest `.ins-shell-group` before it, or a `data-ins-palette-group` around it. */
  function headingOf(link, source) {
    for (var node = link; node && node !== source; node = node.parentElement) {
      if (node !== link && node.hasAttribute('data-ins-palette-group')) return node.getAttribute('data-ins-palette-group');
      for (var s = node.previousElementSibling; s; s = s.previousElementSibling) {
        if (s.classList.contains('ins-shell-group')) return s.textContent.trim();
      }
    }
    return '';
  }

  /* `data-ins-palette-from`: the page's own navigation, read each time the palette
     opens, so a sidebar that changed is never out of date here. A link the palette
     already lists by hand is not listed twice. The shell's logo is left out — it is
     the way home, not a page of its own, and taken first it would stand in for the
     real Home link under a name made of the mark and the tagline — and so is any
     link marked `data-ins-palette-skip`. */
  var SKIP = '.ins-shell-brand, [data-ins-palette-skip]';
  function collect(dlg) {
    var from = dlg.getAttribute('data-ins-palette-from');
    if (!from) return;
    var old = dlg.querySelectorAll('[data-ins-palette-auto]');
    for (var i = 0; i < old.length; i++) old[i].parentNode.removeChild(old[i]);

    var sources;
    try { sources = document.querySelectorAll(from); } catch (e) { warn('not a selector: ' + from); return; }
    var have = {}, items = dlg.querySelectorAll(ITEM);
    for (var j = 0; j < items.length; j++) if (items[j].href) have[items[j].href] = true;

    for (var k = 0; k < sources.length; k++) {
      var links = sources[k].querySelectorAll('a[href]');
      for (var m = 0; m < links.length; m++) {
        var link = links[m], raw = link.getAttribute('href');
        if (link.closest(PALETTE) || link.matches(SKIP) || !raw || /^javascript:/i.test(raw) || have[link.href]) continue;
        have[link.href] = true;
        var a = make('a');
        a.href = raw;
        a.setAttribute('data-ins-palette-auto', '');
        var ico = link.querySelector('svg');
        if (ico) {
          ico = ico.cloneNode(true);
          ico.setAttribute('aria-hidden', 'true');
          a.appendChild(ico);
        }
        a.appendChild(document.createTextNode(link.getAttribute('data-label') || labelOf(link)));
        var words = link.getAttribute('data-keywords') || link.getAttribute('title');
        if (words) a.setAttribute('data-keywords', words);
        if (link.getAttribute('aria-current')) a.setAttribute('aria-current', link.getAttribute('aria-current'));
        adopt(dlg, a, headingOf(link, sources[k]));
      }
    }
  }

  /* ------------------------------------------------------------- recents */

  function recentLimit(dlg) {
    var n = parseInt(dlg.getAttribute('data-ins-palette-recent'), 10);
    return isNaN(n) ? 5 : Math.max(0, n);
  }

  function storeKey(dlg) { return STORE + ':' + dlg.id; }

  function recents(dlg) {
    if (!recentLimit(dlg)) return [];
    try {
      var list = JSON.parse(window.localStorage.getItem(storeKey(dlg)) || '[]');
      return Array.isArray(list) ? list : [];
    } catch (e) { return []; }
  }

  function remember(dlg, item) {
    var limit = recentLimit(dlg);
    if (!limit) return;
    var key = keyOf(item);
    var list = recents(dlg).filter(function (k) { return k !== key; });
    list.unshift(key);
    try { window.localStorage.setItem(storeKey(dlg), JSON.stringify(list.slice(0, limit))); } catch (e) { /* private mode: not fatal */ }
  }

  /* ------------------------------------------------------------- ranking */

  function boundary(ch) { return !ch || /[\s\-_/.,:;()«»"']/.test(ch); }

  /* Where in a name a term starts: a word's start counts for more than its middle,
     and so does a word's start after the Arabic article, so "جدول" is as good a hit
     in "الجدول" as in "جدول". */
  function place(hay, term) {
    var at = hay.indexOf(term);
    if (at === -1) return 0;
    if (hay === term) return 100;
    if (at === 0) return 80;
    for (var i = at; i !== -1; i = hay.indexOf(term, i + 1)) {
      if (boundary(hay.charAt(i - 1))) return 60;
      if (hay.substr(i - 2, 2) === 'ال' && boundary(hay.charAt(i - 3))) return 60;
    }
    return 40;
  }

  /* Every term has to be found somewhere — the name, then the keywords, then the
     group — and each scores by where. Nothing clever beyond that: a result a person
     can predict is worth more than a fuzzy one they cannot. */
  function score(el, terms) {
    var name = norm(labelOf(el)), words = norm(el.getAttribute('data-keywords')), group = norm(el.getAttribute('data-ins-palette-group'));
    var total = 0;
    for (var i = 0; i < terms.length; i++) {
      var s = place(name, terms[i]);
      if (!s) { var w = place(words, terms[i]); s = w ? Math.min(w, 60) / 2 : 0; }
      if (!s && group.indexOf(terms[i]) !== -1) s = 10;
      if (!s) return 0;
      total += s;
    }
    return total;
  }

  /* ------------------------------------------------------------- the dialog */

  function parts(dlg) {
    return {
      input: dlg.querySelector('.ins-palette-input'),
      list: dlg.querySelector('.ins-palette-list'),
      empty: dlg.querySelector('.ins-palette-empty'),
      status: dlg.querySelector('.ins-palette-status')
    };
  }

  function searchIcon() {
    var svg = document.createElementNS(SVG, 'svg');
    svg.setAttribute('class', 'ins-search-ico ins-palette-ico');
    svg.setAttribute('viewBox', '0 0 24 24');
    svg.setAttribute('aria-hidden', 'true');
    var c = document.createElementNS(SVG, 'circle');
    c.setAttribute('cx', '11'); c.setAttribute('cy', '11'); c.setAttribute('r', '7');
    var p = document.createElementNS(SVG, 'path');
    p.setAttribute('d', 'm20 20-3.5-3.5');
    svg.appendChild(c);
    svg.appendChild(p);
    return svg;
  }

  function hint(keys, words) {
    var span = make('span');
    for (var i = 0; i < keys.length; i++) span.appendChild(make('kbd', 'ins-kbd', keys[i]));
    span.appendChild(document.createTextNode(' ' + words));
    return span;
  }

  function build(dlg) {
    if (dlg.hasAttribute('data-ins-palette-ready')) return;
    dlg.setAttribute('data-ins-palette-ready', '');
    if (dlg.tagName !== 'DIALOG') { warn('data-ins-palette belongs on a <dialog>.'); return; }
    dlg.classList.add('ins-dialog', 'ins-palette');
    if (!dlg.id) dlg.id = uid('ins-palette');
    var t = text(dlg);

    /* The page's links and buttons, with the group each was written in, before
       everything around them is replaced. */
    var found = [], all = dlg.querySelectorAll('a[href], button');
    for (var i = 0; i < all.length; i++) {
      var outer = all[i].parentElement && all[i].parentElement.closest('a[href], button');
      if (!outer || !dlg.contains(outer)) found.push({ el: all[i], group: all[i].getAttribute('data-ins-palette-group') || groupOf(all[i], dlg) });
    }
    while (dlg.firstChild) dlg.removeChild(dlg.firstChild);
    if (!dlg.hasAttribute('aria-label') && !dlg.hasAttribute('aria-labelledby')) dlg.setAttribute('aria-label', t.label);

    var top = make('div', 'ins-palette-top');
    var search = make('div', 'ins-search');
    var input = make('input', 'ins-input ins-input--lg ins-palette-input');
    var list = make('div', 'ins-palette-list');
    list.id = dlg.id + '-list';
    list.setAttribute('role', 'listbox');
    list.setAttribute('aria-label', dlg.getAttribute('aria-label') || t.label);
    input.type = 'text';
    input.autocomplete = 'off';
    input.spellcheck = false;
    input.setAttribute('autofocus', '');
    input.setAttribute('role', 'combobox');
    input.setAttribute('aria-autocomplete', 'list');
    input.setAttribute('aria-expanded', 'true');
    input.setAttribute('aria-controls', list.id);
    input.setAttribute('aria-label', dlg.getAttribute('aria-label') || t.label);
    input.placeholder = dlg.getAttribute('data-ins-palette-placeholder') || t.placeholder;
    search.appendChild(searchIcon());
    search.appendChild(input);
    top.appendChild(search);

    var empty = make('div', 'ins-palette-empty', t.empty);
    empty.hidden = true;
    var status = make('div', 'ins-sr-only ins-palette-status');
    status.setAttribute('role', 'status');
    status.setAttribute('aria-live', 'polite');
    var foot = make('div', 'ins-palette-foot');
    foot.setAttribute('aria-hidden', 'true');
    foot.appendChild(hint(['↑', '↓'], t.move));
    foot.appendChild(hint(['Enter'], t.choose));
    foot.appendChild(hint(['Esc'], t.close));

    dlg.appendChild(top);
    dlg.appendChild(list);
    dlg.appendChild(empty);
    dlg.appendChild(status);
    dlg.appendChild(foot);
    for (var j = 0; j < found.length; j++) adopt(dlg, found[j].el, found[j].group);

    input.addEventListener('input', function () { render(dlg, true); });
    input.addEventListener('keydown', function (e) { onKey(dlg, e); });
    /* A press on the list must not take focus from the field. */
    list.addEventListener('mousedown', function (e) { e.preventDefault(); });
    list.addEventListener('pointermove', function (e) {
      var item = e.target.closest && e.target.closest(ITEM);
      if (item && !item.classList.contains('is-active')) activate(dlg, item, false);
    });
    dlg.addEventListener('click', function (e) { onChoose(dlg, e); }, true);
    /* Opened with nothing focused, a dialog has nowhere to hand focus back to, and
       leaves it in its own field — so the next "/" would count as typing there and
       never reopen it. */
    dlg.addEventListener('close', function () {
      if (dlg.contains(document.activeElement)) document.activeElement.blur();
    });

    /* However it opened — a data-ins-dialog button, Insiyab.dialog(), the page's own
       showModal() — it opens fresh: the navigation read again, the field empty, the
       first item ready for Enter. */
    if (window.MutationObserver) {
      new window.MutationObserver(function () { if (dlg.open) opened(dlg); })
        .observe(dlg, { attributes: true, attributeFilter: ['open'] });
    }
    stampOpeners(dlg);
    render(dlg, false);
    if (dlg.open) opened(dlg);
  }

  function opened(dlg) {
    var p = parts(dlg);
    collect(dlg);
    p.input.value = '';
    render(dlg, false);
    p.input.focus();
  }

  /* Lays the list out afresh. With nothing typed: the recent items, then every
     group in the order it was written. With a query: one list, best first, each
     item naming its group at the end. An item that does not match stays in the
     list, hidden, because the list is where the items live. */
  function render(dlg, announce) {
    var p = parts(dlg), t = text(dlg);
    var terms = norm(p.input.value).split(/\s+/).filter(Boolean);
    var items = Array.prototype.slice.call(p.list.querySelectorAll(ITEM));
    items.sort(function (a, b) { return +a.getAttribute('data-ins-palette-order') - +b.getAttribute('data-ins-palette-order'); });
    p.list.textContent = '';
    var shown = [];

    if (terms.length) {
      dlg.classList.add('is-searching');
      var hits = [];
      for (var i = 0; i < items.length; i++) {
        var s = score(items[i], terms);
        if (s) hits.push({ el: items[i], s: s, o: i });
        else items[i].hidden = true;
      }
      hits.sort(function (a, b) { return b.s - a.s || a.o - b.o; });
      for (var h = 0; h < hits.length; h++) shown.push(hits[h].el);
      for (var k = 0; k < shown.length; k++) { shown[k].hidden = false; p.list.appendChild(shown[k]); }
    } else {
      dlg.classList.remove('is-searching');
      var groups = [], byName = {}, used = {};
      var keys = recents(dlg).slice(0, recentLimit(dlg));
      var recent = [];
      for (var r = 0; r < keys.length; r++) {
        for (var q = 0; q < items.length; q++) {
          if (!used[items[q].id] && keyOf(items[q]) === keys[r]) { recent.push(items[q]); used[items[q].id] = true; break; }
        }
      }
      if (recent.length) groups.push({ name: t.recent, items: recent });
      for (var m = 0; m < items.length; m++) {
        items[m].hidden = false;
        if (used[items[m].id]) continue;
        var name = items[m].getAttribute('data-ins-palette-group') || '';
        if (!byName.hasOwnProperty(name)) { byName[name] = { name: name, items: [] }; groups.push(byName[name]); }
        byName[name].items.push(items[m]);
      }
      for (var g = 0; g < groups.length; g++) {
        var box = p.list;
        if (groups[g].name) {
          box = make('div', 'ins-palette-group');
          box.setAttribute('role', 'group');
          var head = make('div', 'ins-palette-head', groups[g].name);
          head.id = uid('ins-palette-head');
          head.setAttribute('role', 'presentation');
          box.setAttribute('aria-labelledby', head.id);
          box.appendChild(head);
          p.list.appendChild(box);
        }
        for (var n = 0; n < groups[g].items.length; n++) { box.appendChild(groups[g].items[n]); shown.push(groups[g].items[n]); }
      }
    }
    /* The ones not shown go last, out of the way. */
    for (var x = 0; x < items.length; x++) if (items[x].hidden) p.list.appendChild(items[x]);

    p.empty.hidden = shown.length > 0;
    p.list.scrollTop = 0;
    activate(dlg, shown[0] || null, false);
    if (announce) p.status.textContent = t.count(shown.length);
  }

  function visible(dlg) {
    return Array.prototype.slice.call(parts(dlg).list.querySelectorAll(ITEM + ':not([hidden])'));
  }

  function activate(dlg, item, scroll) {
    var p = parts(dlg), prev = p.list.querySelector(ITEM + '.is-active');
    if (prev) { prev.classList.remove('is-active'); prev.setAttribute('aria-selected', 'false'); }
    if (item) {
      item.classList.add('is-active');
      item.setAttribute('aria-selected', 'true');
      p.input.setAttribute('aria-activedescendant', item.id);
      if (scroll && item.scrollIntoView) item.scrollIntoView({ block: 'nearest' });
    } else {
      p.input.removeAttribute('aria-activedescendant');
    }
  }

  function onKey(dlg, e) {
    if (e.isComposing) return;
    var items = visible(dlg);
    var at = items.indexOf(parts(dlg).list.querySelector(ITEM + '.is-active'));
    if (e.key === 'ArrowDown' || e.key === 'ArrowUp') {
      e.preventDefault();
      if (!items.length) return;
      var next = e.key === 'ArrowDown' ? (at + 1) % items.length : (at <= 0 ? items.length - 1 : at - 1);
      activate(dlg, items[next], true);
    } else if (e.key === 'Enter') {
      e.preventDefault();
      if (at !== -1) items[at].click();
    }
  }

  /* A choice, by Enter or by a click. The palette closes first, so an item that
     opens a dialog of its own is not opened underneath this one; `ins:palette` goes
     out before the item runs, and a listener that cancels it — a router taking the
     link itself — stops the item altogether. */
  function onChoose(dlg, e) {
    var item = e.target.closest && e.target.closest(ITEM);
    if (!item || !dlg.contains(item)) return;
    var detail = { palette: dlg, item: item, label: labelOf(item), key: keyOf(item) };
    var event;
    try { event = new CustomEvent('ins:palette', { detail: detail, bubbles: true, cancelable: true }); }
    catch (err) { event = document.createEvent('CustomEvent'); event.initCustomEvent('ins:palette', true, true, detail); }
    remember(dlg, item);
    Insiyab.dialog(dlg, 'close');
    if (!dlg.dispatchEvent(event)) { e.preventDefault(); e.stopPropagation(); }
  }

  /* ------------------------------------------------------------ shortcuts */

  var MAC = /Mac|iPhone|iPad/.test((window.navigator && window.navigator.platform) || '');

  function shortcuts(dlg) {
    var raw = dlg.hasAttribute('data-ins-palette-keys') ? dlg.getAttribute('data-ins-palette-keys') : 'mod+k /';
    if (raw.trim() === 'none') return [];
    return raw.trim().split(/\s+/).filter(Boolean).map(function (token) {
      var bits = token.toLowerCase().split('+'), key = bits.pop() || '+';
      var mods = {};
      for (var i = 0; i < bits.length; i++) mods[bits[i]] = true;
      return { key: key, mods: mods };
    });
  }

  function typing(el) {
    return !!el && (/^(INPUT|TEXTAREA|SELECT)$/.test(el.tagName) || el.isContentEditable);
  }

  /* The key is matched by the character, and a letter or the slash also by where it
     sits on the keyboard: with an Arabic layout on, Ctrl+K is Ctrl+ن and "/" is ظ, and
     the shortcut has to work all the same. */
  function pressed(s, e) {
    var m = s.mods, mod = m.mod ? (e.ctrlKey || e.metaKey) : (!!m.ctrl === e.ctrlKey && !!m.meta === e.metaKey);
    if (!mod || !!m.alt !== e.altKey) return false;
    if (m.shift ? !e.shiftKey : (e.shiftKey && s.key.length === 1 && /[a-z]/.test(s.key))) return false;
    var k = (e.key || '').toLowerCase();
    if (k === s.key) return true;
    if (/^[a-z]$/.test(s.key)) return e.code === 'Key' + s.key.toUpperCase();
    if (s.key === '/') return e.code === 'Slash' && !e.shiftKey;
    return false;
  }

  document.addEventListener('keydown', function (e) {
    if (e.defaultPrevented || e.isComposing) return;
    var palettes = document.querySelectorAll(PALETTE + '[data-ins-palette-ready]');
    for (var i = 0; i < palettes.length; i++) {
      var list = shortcuts(palettes[i]);
      for (var j = 0; j < list.length; j++) {
        var s = list[j], bare = !s.mods.mod && !s.mods.ctrl && !s.mods.meta && !s.mods.alt;
        if (bare && typing(e.target)) continue;
        if (!pressed(s, e)) continue;
        e.preventDefault();
        Insiyab.dialog(palettes[i], 'toggle');
        return;
      }
    }
  }, false);

  /* The buttons that open it say which keys do too, for a screen reader. */
  function stampOpeners(dlg) {
    var names = shortcuts(dlg).map(function (s) {
      var out = [];
      if (s.mods.mod) out.push(MAC ? 'Meta' : 'Control');
      if (s.mods.ctrl) out.push('Control');
      if (s.mods.meta) out.push('Meta');
      if (s.mods.alt) out.push('Alt');
      if (s.mods.shift) out.push('Shift');
      out.push(s.key.length === 1 ? s.key.toUpperCase() : s.key.charAt(0).toUpperCase() + s.key.slice(1));
      return out.join('+');
    }).join(' ');
    if (!names) return;
    var openers = document.querySelectorAll('[data-ins-dialog]');
    for (var i = 0; i < openers.length; i++) {
      if (openers[i].hasAttribute('aria-keyshortcuts')) continue;
      var target;
      try { target = document.querySelector(openers[i].getAttribute('data-ins-dialog')); } catch (e) { target = null; }
      if (target === dlg) openers[i].setAttribute('aria-keyshortcuts', names);
    }
  }

  /* ------------------------------------------------------------------ API */

  function find(target, create) {
    var dlg = null;
    if (typeof target === 'string') { try { dlg = document.querySelector(target); } catch (e) { dlg = null; } }
    else if (target && target.nodeType === 1) dlg = target;
    else dlg = document.querySelector(PALETTE);
    if (!dlg && create && !target && document.body) {
      dlg = document.createElement('dialog');
      dlg.setAttribute('data-ins-palette', '');
      document.body.appendChild(dlg);
    }
    if (dlg && dlg.matches(PALETTE)) build(dlg);
    return dlg && dlg.matches(PALETTE) ? dlg : null;
  }

  function palette(target, action) {
    if (target === 'open' || target === 'close' || target === 'toggle') { action = target; target = null; }
    var dlg = find(target, false);
    if (dlg) Insiyab.dialog(dlg, action || 'toggle');
    return dlg;
  }

  /* One item, or a list: { label, href | run, group, icon, keywords, hint, key }.
     `icon` is an element, cloned, or the id of a symbol in the page's sprite
     ("#i-sun"); `hint` is a shortcut shown at the end. Returns the elements, which
     stay the page's to change or remove. */
  palette.add = function (items, target) {
    var dlg = find(target, true);
    if (!dlg) return [];
    var list = Array.isArray(items) ? items : [items], out = [];
    for (var i = 0; i < list.length; i++) {
      var spec = list[i] || {};
      if (!spec.label) { warn('an item needs a label.'); continue; }
      var el = make(spec.href ? 'a' : 'button');
      if (spec.href) el.href = spec.href;
      if (spec.icon) {
        var ico = null;
        if (typeof spec.icon === 'string') {
          ico = document.createElementNS(SVG, 'svg');
          ico.setAttribute('class', 'ins-palette-ico');
          var use = document.createElementNS(SVG, 'use');
          use.setAttribute('href', spec.icon);
          ico.appendChild(use);
        } else if (spec.icon.cloneNode) ico = spec.icon.cloneNode(true);
        if (ico) { ico.setAttribute('aria-hidden', 'true'); el.appendChild(ico); }
      }
      el.appendChild(document.createTextNode(spec.label));
      if (spec.hint) el.appendChild(make('kbd', 'ins-kbd', spec.hint));
      if (spec.keywords) el.setAttribute('data-keywords', Array.isArray(spec.keywords) ? spec.keywords.join(' ') : spec.keywords);
      if (spec.key) el.setAttribute('data-ins-palette-key', spec.key);
      if (typeof spec.run === 'function') {
        (function (fn, node) { node.addEventListener('click', function (e) { fn.call(node, e); }); })(spec.run, el);
      }
      out.push(adopt(dlg, el, spec.group || ''));
    }
    render(dlg, false);
    return out;
  };

  Insiyab.palette = palette;

  function scan(scope) {
    var found = scope.querySelectorAll ? scope.querySelectorAll(PALETTE) : [];
    for (var i = 0; i < found.length; i++) build(found[i]);
    if (scope.matches && scope.matches(PALETTE)) build(scope);
  }

  Insiyab.define('palette', scan);
  /* Loaded after the page was parsed, the library's own scan has already run
     without this one; so it runs now, on its own. */
  if (document.readyState !== 'loading') scan(document);
})(window, document);
