/* ==========================================================================
   Insiyab · Tree plugin
   ==========================================================================

   Nested lists as a tree: folders, categories, an org chart, permissions. Load it
   after insiyab.js, with its stylesheet:

     <link rel="stylesheet" href="plugins/insiyab-tree.css">
     <script src="insiyab.js"></script>
     <script src="plugins/insiyab-tree.js"></script>

     <ul class="ins-tree" data-ins-tree aria-label="الملفّات">
       <li data-open>
         <span>المستندات</span>
         <ul>
           <li><a href="/docs/contract.pdf">العقد.pdf</a></li>
         </ul>
       </li>
     </ul>

   Each item's first element is its label — text in a span, a link, or an
   `.ins-check` label with a checkbox — and a nested list is its branch, closed
   unless the item has `data-open`. Without the script it is the nested list,
   every branch showing, which is a perfectly good fallback.

   The tree is one stop in the tab order and the arrow keys walk it, as a file
   tree does: Up and Down between the rows that are showing, Right to open a branch
   or step into it, Left to close it or step out to its parent, Home and End, `*`
   to open every branch beside this one, and a letter to jump to the next row that
   starts with it. Enter follows a link or selects; Space selects, or ticks the
   box in a tree of checkboxes.

     data-ins-tree-checks   the checkboxes cascade: a branch ticks everything in
                            it, and a branch partly ticked shows as mixed
     data-ins-tree-select="none"   no selection, for a tree that only navigates

   `ins:tree` reports every change, with the item and the action — select, open,
   close or check — and Insiyab.tree(item, action) does the same from code.
   ========================================================================== */

(function (window, document) {
  'use strict';

  var Insiyab = window.Insiyab;
  if (!Insiyab || !Insiyab.define) {
    if (window.console) window.console.warn('[insiyab-tree] load insiyab.js before this plugin.');
    return;
  }
  var norm = Insiyab.norm || function (s) { return String(s || '').toLowerCase().trim(); };

  var TREE = '[data-ins-tree]';
  var ITEM = '[role="treeitem"]';

  function make(tag, cls) { var el = document.createElement(tag); if (cls) el.className = cls; return el; }

  function branchOf(item) { var u = item.lastElementChild; return u && u.tagName === 'UL' && u.getAttribute('role') === 'group' ? u : null; }
  function rowOf(item) { return item.firstElementChild; }
  function labelOf(item) { var r = rowOf(item); return r && r.querySelector('.ins-tree-label'); }
  function treeOf(el) { return el.closest(TREE); }
  function parentItem(item) { var g = item.parentElement; return g && g.getAttribute('role') === 'group' ? g.parentElement : null; }
  function isOpen(item) { return item.getAttribute('aria-expanded') === 'true'; }
  function checksOn(tree) { return tree.hasAttribute('data-ins-tree-checks'); }
  function selects(tree) { return tree.getAttribute('data-ins-tree-select') !== 'none' && !checksOn(tree); }
  function boxOf(item) { var r = rowOf(item); return r && r.querySelector('input[type="checkbox"]'); }

  /* The rows showing, in order: every item whose ancestors are all open. */
  function visible(tree) {
    var all = tree.querySelectorAll(ITEM), out = [];
    for (var i = 0; i < all.length; i++) {
      var shown = true;
      for (var p = parentItem(all[i]); p; p = parentItem(p)) if (!isOpen(p)) { shown = false; break; }
      if (shown) out.push(all[i]);
    }
    return out;
  }

  function emit(tree, item, action, extra) {
    var detail = { tree: tree, item: item, label: labelText(item), action: action };
    for (var k in extra) if (extra.hasOwnProperty(k)) detail[k] = extra[k];
    var event;
    try { event = new CustomEvent('ins:tree', { detail: detail, bubbles: true }); }
    catch (e) { event = document.createEvent('CustomEvent'); event.initCustomEvent('ins:tree', true, false, detail); }
    tree.dispatchEvent(event);
  }

  function labelText(item) { var l = labelOf(item); return l ? l.textContent.replace(/\s+/g, ' ').trim() : ''; }

  /* ------------------------------------------------------------- building */

  function build(tree) {
    if (tree.hasAttribute('data-ins-tree-ready')) return;
    if (tree.tagName !== 'UL' && tree.tagName !== 'OL') return;
    tree.setAttribute('data-ins-tree-ready', '');
    tree.classList.add('ins-tree');
    tree.setAttribute('role', 'tree');
    setup(tree, tree, 1);
    if (checksOn(tree)) {
      /* A branch ticked in the markup ticks what is in it; then every branch is
         worked out again from what is under it. */
      var boxes = tree.querySelectorAll(ITEM);
      for (var i = 0; i < boxes.length; i++) if (boxOf(boxes[i]) && boxOf(boxes[i]).checked) cascade(boxes[i], true);
      settleAll(tree);
    }
    var first = tree.querySelector(ITEM + '[aria-selected="true"]') || tree.querySelector(ITEM);
    if (first) first.tabIndex = 0;
    tree.addEventListener('keydown', onKey);
    tree.addEventListener('click', onClick);
    tree.addEventListener('change', onChange);
    tree.addEventListener('focusin', function (e) {
      var item = e.target.closest && e.target.closest(ITEM);
      if (item && e.target !== item && !e.target.matches('input')) item.focus();
    });
  }

  function setup(list, tree, level) {
    var items = [];
    for (var c = list.firstElementChild; c; c = c.nextElementSibling) if (c.tagName === 'LI') items.push(c);
    for (var i = 0; i < items.length; i++) {
      var li = items[i];
      var sub = null;
      for (var k = li.lastElementChild; k; k = k.previousElementSibling) if (k.tagName === 'UL' || k.tagName === 'OL') { sub = k; break; }
      /* Everything before the branch is the row. A row already in the markup is
         kept, so a framework that rendered it still owns what is in it. */
      var row = li.firstElementChild;
      if (!row || !row.classList.contains('ins-tree-row')) {
        row = make('div', 'ins-tree-row');
        while (li.firstChild && li.firstChild !== sub) row.appendChild(li.firstChild);
        li.insertBefore(row, sub);
      }
      var label = row.querySelector('a, button, label, span') || row;
      if (label !== row) label.classList.add('ins-tree-label');
      var focusables = row.querySelectorAll('a, button');
      for (var f = 0; f < focusables.length; f++) focusables[f].tabIndex = -1;
      var box = row.querySelector('input[type="checkbox"]');
      if (box) box.tabIndex = -1;

      var toggle = make('span', 'ins-tree-toggle');
      toggle.setAttribute('aria-hidden', 'true');
      row.insertBefore(toggle, row.firstChild);

      li.setAttribute('role', 'treeitem');
      li.setAttribute('aria-level', String(level));
      li.setAttribute('aria-setsize', String(items.length));
      li.setAttribute('aria-posinset', String(i + 1));
      li.tabIndex = -1;
      if (sub && sub.children.length) {
        sub.setAttribute('role', 'group');
        li.setAttribute('aria-expanded', String(li.hasAttribute('data-open')));
        sub.hidden = !li.hasAttribute('data-open');
        setup(sub, tree, level + 1);
      } else {
        toggle.classList.add('is-leaf');
      }
      if (li.hasAttribute('data-selected') && selects(tree)) li.setAttribute('aria-selected', 'true');
    }
  }

  /* ------------------------------------------------------------- actions */

  function focusItem(tree, item) {
    var items = tree.querySelectorAll(ITEM);
    for (var i = 0; i < items.length; i++) items[i].tabIndex = items[i] === item ? 0 : -1;
    item.focus();
  }

  function setOpen(item, open, quiet) {
    var branch = branchOf(item);
    if (!branch || isOpen(item) === open) return false;
    item.setAttribute('aria-expanded', String(open));
    branch.hidden = !open;
    var tree = treeOf(item);
    /* Focus inside a branch that closes goes up to the branch. */
    if (!open && item.contains(document.activeElement) && document.activeElement !== item) focusItem(tree, item);
    if (!quiet) emit(tree, item, open ? 'open' : 'close');
    return true;
  }

  function select(item, quiet) {
    var tree = treeOf(item);
    if (!selects(tree)) return false;
    var was = tree.querySelector(ITEM + '[aria-selected="true"]');
    if (was === item) return false;
    if (was) was.removeAttribute('aria-selected');
    item.setAttribute('aria-selected', 'true');
    if (!quiet) emit(tree, item, 'select');
    return true;
  }

  /* Enter, or a click on a row: a link is followed, a branch without one opens or
     closes, and the row is selected. */
  function activate(item, viaKey) {
    var label = labelOf(item), tree = treeOf(item);
    select(item);
    if (label && label.tagName === 'A' && viaKey) { label.click(); return; }
    if ((!label || label.tagName !== 'A') && branchOf(item) && !checksOn(tree)) setOpen(item, !isOpen(item));
  }

  /* ------------------------------------------------------------- checkboxes */

  function cascade(item, checked) {
    var boxes = item.querySelectorAll('input[type="checkbox"]');
    for (var i = 0; i < boxes.length; i++) { boxes[i].checked = checked; boxes[i].indeterminate = false; }
  }

  /* A branch from its children: all ticked, ticked; none, not; else mixed. */
  function settle(item) {
    var box = boxOf(item), branch = branchOf(item);
    if (branch && box) {
      var kids = [], n = 0, on = 0, mixed = 0;
      for (var c = branch.firstElementChild; c; c = c.nextElementSibling) {
        var b = boxOf(c);
        if (!b) continue;
        n++;
        if (b.indeterminate) mixed++;
        else if (b.checked) on++;
        kids.push(c);
      }
      if (n) {
        box.checked = on === n;
        box.indeterminate = !box.checked && (on > 0 || mixed > 0);
      }
    }
    if (box) item.setAttribute('aria-checked', box.indeterminate ? 'mixed' : String(box.checked));
  }

  function settleAll(tree) {
    var items = tree.querySelectorAll(ITEM);
    for (var i = items.length - 1; i >= 0; i--) settle(items[i]);   /* deepest first */
  }

  function settleUp(item) {
    var items = item.querySelectorAll(ITEM);
    for (var i = items.length - 1; i >= 0; i--) settle(items[i]);
    for (var p = item; p; p = parentItem(p)) settle(p);
  }

  /* Space, or Insiyab.tree(item, 'check'): the box is set and a real change sent,
     which the page hears as it would a click, and which does the cascading. */
  function check(item, checked) {
    var box = boxOf(item);
    if (!box || box.disabled) return;
    box.checked = checked;
    box.indeterminate = false;
    box.dispatchEvent(new Event('change', { bubbles: true }));
  }

  function onChange(e) {
    var box = e.target;
    if (!box.matches || !box.matches('input[type="checkbox"]')) return;
    var item = box.closest(ITEM), tree = treeOf(box);
    if (!item || !checksOn(tree) || boxOf(item) !== box) return;
    cascade(item, box.checked);
    settleUp(item);
    emit(tree, item, 'check', { checked: box.checked });
  }

  /* ------------------------------------------------------------- input */

  function onClick(e) {
    var item = e.target.closest && e.target.closest(ITEM);
    if (!item) return;
    var tree = treeOf(item);
    if (e.target.closest('.ins-tree-toggle')) {
      e.preventDefault();
      setOpen(item, !isOpen(item));
      focusItem(tree, item);
      return;
    }
    if (!e.target.closest('.ins-tree-row')) return;
    focusItem(tree, item);
    /* The checkbox and its label do their own thing. */
    if (e.target.closest('label, input')) return;
    activate(item, false);
  }

  function onKey(e) {
    var item = e.target.closest && e.target.closest(ITEM);
    if (!item || e.target !== item || e.altKey || e.ctrlKey || e.metaKey) return;
    var tree = treeOf(item), rows = visible(tree), at = rows.indexOf(item);
    var rtl = window.getComputedStyle(tree).direction === 'rtl';
    var key = e.key;
    /* In → and ← mean into and out of, which is which way depends on the direction
       the tree reads in. */
    if (key === 'ArrowRight' || key === 'ArrowLeft') key = (key === 'ArrowRight') !== rtl ? 'In' : 'Out';
    var go = null;
    switch (key) {
      case 'ArrowDown': go = rows[at + 1]; break;
      case 'ArrowUp': go = rows[at - 1]; break;
      case 'Home': go = rows[0]; break;
      case 'End': go = rows[rows.length - 1]; break;
      case 'In':
        if (branchOf(item)) { if (!isOpen(item)) setOpen(item, true); else go = branchOf(item).firstElementChild; }
        break;
      case 'Out':
        if (branchOf(item) && isOpen(item)) setOpen(item, false);
        else go = parentItem(item);
        break;
      case 'Enter': activate(item, true); break;
      case ' ':
        if (checksOn(tree) && boxOf(item)) check(item, !boxOf(item).checked);
        else select(item);
        break;
      case '*':
        var group = item.parentElement;
        for (var s = group.firstElementChild; s; s = s.nextElementSibling) if (s.matches(ITEM)) setOpen(s, true);
        break;
      default:
        if (key.length === 1 && /\S/.test(key)) {
          var q = norm(key);
          for (var i = 1; i <= rows.length; i++) {
            var r = rows[(at + i) % rows.length];
            if (norm(labelText(r)).indexOf(q) === 0) { go = r; break; }
          }
          if (!go) return;
        } else return;
    }
    e.preventDefault();
    if (go) focusItem(tree, go);
  }

  /* ------------------------------------------------------------- API */

  function scan(scope) {
    var found = scope.querySelectorAll ? scope.querySelectorAll(TREE) : [];
    for (var i = 0; i < found.length; i++) build(found[i]);
    if (scope.matches && scope.matches(TREE)) build(scope);
  }

  /* Insiyab.tree(item, 'open' | 'close' | 'toggle' | 'select' | 'check' | 'uncheck'),
     or Insiyab.tree(tree, 'open' | 'close') for every branch; it returns the item
     or the tree. Insiyab.tree(tree) alone returns the selected item, or in a tree
     of checkboxes the items ticked. */
  Insiyab.tree = function (target, action) {
    var el = typeof target === 'string' ? document.querySelector(target) : target;
    if (!el) return null;
    if (el.matches(TREE)) {
      if (action === 'open' || action === 'close') {
        var all = el.querySelectorAll(ITEM);
        for (var i = 0; i < all.length; i++) setOpen(all[i], action === 'open', true);
        return el;
      }
      if (checksOn(el)) {
        return Array.prototype.filter.call(el.querySelectorAll(ITEM), function (it) { var b = boxOf(it); return b && b.checked; });
      }
      return el.querySelector(ITEM + '[aria-selected="true"]');
    }
    var item = el.matches(ITEM) ? el : el.closest(ITEM);
    if (!item) return null;
    if (action === 'open' || action === 'close') setOpen(item, action === 'open');
    else if (action === 'toggle') setOpen(item, !isOpen(item));
    else if (action === 'select') select(item);
    else if (action === 'check' || action === 'uncheck') check(item, action === 'check');
    /* Opening an item's ancestors too, so the item can be seen. */
    if (action === 'open' || action === 'select') for (var p = parentItem(item); p; p = parentItem(p)) setOpen(p, true);
    return item;
  };

  Insiyab.define('tree', scan);
  if (document.readyState !== 'loading') scan(document);
})(window, document);
