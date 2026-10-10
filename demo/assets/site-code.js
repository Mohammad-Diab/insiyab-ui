// Docs-only listing highlighter, run by demo/src/site.mjs at build time and by the page for the playground.

var SiteCode = (function () {
  'use strict';

  function esc(s) { return String(s).replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/>/g, '&gt;'); }

  function span(cls, text) { return '<span class="' + cls + '">' + esc(text) + '</span>'; }

  function html(src) {
    var re = /<!--[\s\S]*?-->|<\/?[a-zA-Z][\w-]*(?:\s+[^\s=>/]+(?:\s*=\s*(?:"[^"]*"|'[^']*'|[^\s>]+))?)*\s*\/?>/g;
    var out = '', last = 0, m;
    while ((m = re.exec(src))) {
      out += esc(src.slice(last, m.index));
      var tok = m[0];
      if (tok.indexOf('<!--') === 0) {
        out += span('hl-c', tok);
      } else {
        var head = tok.match(/^<\/?[a-zA-Z][\w-]*/)[0];
        var tail = tok.match(/\s*\/?>$/)[0];
        var body = tok.slice(head.length, tok.length - tail.length);
        out += span('hl-t', head);
        out += body.replace(/([^\s=]+)(\s*=\s*)?("[^"]*"|'[^']*'|[^\s>]+)?|\s+/g, function (all, name, eq, val) {
          if (!name) return all;
          return span('hl-a', name) + (eq ? esc(eq) : '') + (val ? span('hl-s', val) : '');
        });
        out += span('hl-t', tail);
      }
      last = m.index + tok.length;
    }
    return out + esc(src.slice(last));
  }

  var JS_WORDS = /^(?:const|let|var|function|return|if|else|new|true|false|null|undefined|await|async|this|typeof|import|from|export)$/;

  function script(src, css) {
    var re = css
      ? /\/\*[\s\S]*?\*\/|"[^"]*"|'[^']*'|--[\w-]+|[\w-]+(?=\s*:)|#[0-9a-fA-F]{3,8}\b|\b\d+(?:\.\d+)?(?:px|rem|em|s|%)?/g
      : /\/\*[\s\S]*?\*\/|\/\/[^\n]*|`[^`]*`|"(?:[^"\\\n]|\\.)*"|'(?:[^'\\\n]|\\.)*'|\b[A-Za-z_$][\w$]*\b|\b\d+(?:\.\d+)?\b/g;
    var out = '', last = 0, m;
    while ((m = re.exec(src))) {
      var t = m[0], cls = '';
      if (t.indexOf('/*') === 0 || t.indexOf('//') === 0) cls = 'hl-c';
      else if (/^["'`]/.test(t)) cls = 'hl-s';
      else if (/^\d|^#/.test(t)) cls = 'hl-n';
      else if (css) cls = t.indexOf('--') === 0 ? 'hl-a' : 'hl-t';
      else if (JS_WORDS.test(t)) cls = 'hl-k';
      else if (t === 'Insiyab' || t === 'document' || t === 'window') cls = 'hl-t';
      out += esc(src.slice(last, m.index)) + (cls ? span(cls, t) : esc(t));
      last = m.index + t.length;
    }
    return out + esc(src.slice(last));
  }

  function highlight(code, lang) {
    if (lang === 'html') return html(code);
    if (lang === 'js') return script(code, false);
    if (lang === 'css') return script(code, true);
    return esc(code);
  }

  // An icon's <use> points at this site's sprite, which nobody copies, so a listing shows only that an icon goes there.
  function forListing(markup) {
    return markup.replace(/<svg class="ico"[^>]*>[\s\S]*?<\/svg>/g, '<svg>…</svg>');
  }

  // A description or note is plain text in which `backticks` mark code.
  function prose(s) {
    return esc(s).replace(/`([^`]+)`/g, function (all, code) { return '<code>' + code + '</code>'; });
  }

  // A playground's values as its html() sees them: defaults filled in, text escaped, advanced controls at their default in simple mode.
  function state(def, values, simple) {
    var s = {};
    def.controls.forEach(function (c) {
      var v = values && Object.prototype.hasOwnProperty.call(values, c.key) ? values[c.key] : c.value;
      if (simple && c.level === 'advanced') v = c.value;
      if (c.type === 'switch') v = !!v;
      else if (v == null) v = '';
      if (c.type === 'text') v = esc(v).replace(/"/g, '&quot;');
      s[c.key] = v;
    });
    return s;
  }

  // Whether the preview stretches its component to its full width: `wide` is true, or a function of the state.
  function wide(def, s) { return typeof def.wide === 'function' ? !!def.wide(s) : !!def.wide; }

  return { esc: esc, highlight: highlight, forListing: forListing, prose: prose, state: state, wide: wide };
})();
