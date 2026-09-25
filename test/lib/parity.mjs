/* The wrappers' parity check, shared by the React and the Jinja tests. Evaluated in
   the page, it defines `__normalize(root)`: the subtree as a list of lines, one per
   element or run of text, in a form where two builds of the same markup compare
   equal and any real difference does not.

   - Ids are replaced by their position (the first id in the subtree is #1), and so is
     every attribute that points at one — `for`, `aria-controls`, `data-ins-tab`… —
     because the core and React make up different ids for the same element.
   - Classes are sorted, attributes are sorted, and `style` is read back property by
     property, so their order does not matter.
   - Whitespace-only text is dropped, and other text has its whitespace collapsed.
   - `type="button"` on a <button> is dropped: the wrappers write it so a button in
     a form does not submit it, and the docs leave it out.
   - `data-value` is dropped: the React tabs use it to know which tab was chosen.
   - A form control's live value, checked and indeterminate state are listed too,
     since those are properties, not attributes. */
export const NORMALIZE = `window.__normalize = function (root) {
  var ids = new Map(), n = 0;
  function id(v) { if (!ids.has(v)) ids.set(v, '#' + (++n)); return ids.get(v); }
  root.querySelectorAll('[id]').forEach(function (e) { id(e.id); });
  var REFS = ['for', 'aria-controls', 'aria-labelledby', 'aria-describedby', 'aria-activedescendant', 'aria-owns',
    'data-ins-tab', 'data-ins-date-value', 'data-ins-date-end', 'data-ins-date-start', 'data-ins-dialog', 'data-ins-dismiss', 'list', 'href'];
  var out = [];
  function walk(node, depth) {
    var pad = new Array(depth + 1).join('  ');
    node.childNodes.forEach(function (c) {
      if (c.nodeType === 3) {
        var t = c.textContent.replace(/\\s+/g, ' ').trim();
        if (t) out.push(pad + JSON.stringify(t));
        return;
      }
      if (c.nodeType !== 1) return;
      var attrs = [];
      for (var i = 0; i < c.attributes.length; i++) {
        var name = c.attributes[i].name, v = c.attributes[i].value;
        if (name === 'data-value') continue;
        if (name === 'type' && v === 'button' && c.tagName === 'BUTTON') continue;
        if (name === 'class') { v = v.split(/\\s+/).filter(Boolean).sort().join(' '); if (!v) continue; }
        else if (name === 'id') v = id(v);
        else if (name === 'style') {
          var props = [];
          for (var p = 0; p < c.style.length; p++) props.push(c.style[p] + ':' + c.style.getPropertyValue(c.style[p]).trim());
          v = props.sort().join(';');
          if (!v) continue;
        } else if (REFS.indexOf(name) !== -1) {
          if (name === 'href' && v.charAt(0) !== '#') { attrs.push(name + '=' + JSON.stringify(v)); continue; }
          v = v.split(/\\s+/).filter(Boolean).map(function (x) {
            var bare = x.charAt(0) === '#' ? x.slice(1) : x;
            return ids.has(bare) || name !== 'href' ? id(bare) : x;
          }).join(' ');
        }
        attrs.push(name + '=' + JSON.stringify(v));
      }
      attrs.sort();
      var line = pad + '<' + c.tagName.toLowerCase() + (attrs.length ? ' ' + attrs.join(' ') : '') + '>';
      if (c.tagName === 'INPUT' || c.tagName === 'SELECT' || c.tagName === 'TEXTAREA') {
        line += ' value=' + JSON.stringify(c.value) + (c.type === 'checkbox' || c.type === 'radio' ? ' checked=' + c.checked + ' mixed=' + c.indeterminate : '');
      }
      out.push(line);
      walk(c, depth + 1);
    });
  }
  walk(root, 0);
  return out;
}`;
