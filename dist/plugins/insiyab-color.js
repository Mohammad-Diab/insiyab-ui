/*! Insiyab UI v0.6.0 · https://github.com/Mohammad-Diab/insiyab-ui#readme · MIT (fonts: OFL 1.1, see fonts/LICENSE-*.txt) */
/* ==========================================================================
   Insiyab · Colour picker plugin
   ==========================================================================

   A colour field that says whether the colour can carry text. Load it after
   insiyab.js, with its stylesheet:

     <link rel="stylesheet" href="plugins/insiyab-color.css">
     <script src="insiyab.js"></script>
     <script src="plugins/insiyab-color.js"></script>

     <input type="color" data-ins-color name="brand" value="#9b2c5e">

   The field becomes a hex field with a swatch in front of it, in an input group
   (made if there is none). It keeps its name and sends `#rrggbb`, exactly what the
   browser's own colour input sends, so a server needs nothing new. The swatch opens
   the picker: a square for saturation and brightness, a hue slider, preset
   swatches, the eyedropper where the browser has one, and the colour's contrast
   against white and against black — the library's own contrast maths — marked
   against the 4.5:1 that body text needs.

     data-ins-color-swatches="#9b2c5e,#0f766e,…"   the presets; the default is the
                                                   page's brand colour and a
                                                   palette around it
     data-ins-color-swatches="none"                 no presets

   The hex can also be typed — `#abc`, `abc`, `aabbcc` — and is written out in full
   when the field is left. The square is a slider for the keyboard: ←→ saturation,
   ↑↓ brightness, with Shift for steps of ten. `input` fires while the colour
   moves, `change` and `ins:color` when it is set.
   ========================================================================== */

(function (window, document) {
  'use strict';

  var Insiyab = window.Insiyab;
  if (!Insiyab || !Insiyab.color) {
    if (window.console) window.console.warn('[insiyab-color] load insiyab.js before this plugin.');
    return;
  }

  var FIELD = 'input[data-ins-color]';
  var DEFAULT_SWATCHES = ['#0f766e', '#0369a1', '#4338ca', '#be123c', '#c2410c', '#a16207', '#15803d', '#334155', '#0f172a'];

  var TEXT = {
    ar: {
      pick: 'اختر لونًا', area: 'التشبع والسطوع', hue: 'درجة اللون', swatches: 'ألوان جاهزة', drop: 'التقاط لون من الشاشة',
      value: 'تشبع {s}٪، سطوع {v}٪', contrast: 'التباين', onWhite: 'على الأبيض', onBlack: 'على الأسود',
      pass: 'يصلح للنص', fail: 'لا يكفي للنص', bad: 'اكتب لونًا مثل #9B2C5E.'
    },
    en: {
      pick: 'Choose a color', area: 'Saturation and brightness', hue: 'Hue', swatches: 'Presets', drop: 'Pick a color from the screen',
      value: 'saturation {s}%, brightness {v}%', contrast: 'Contrast', onWhite: 'on white', onBlack: 'on black',
      pass: 'fine for text', fail: 'too low for text', bad: 'Enter a color like #9B2C5E.'
    }
  };

  function lang(el) { var h = el.closest('[lang]') || document.documentElement; return h.getAttribute('lang') || 'en'; }
  function text(el) { return lang(el).slice(0, 2).toLowerCase() === 'ar' ? TEXT.ar : TEXT.en; }
  function fill(s, map) { return s.replace(/\{(\w+)\}/g, function (a, k) { return map[k] != null ? map[k] : a; }); }
  function make(tag, cls, content) { var el = document.createElement(tag); if (cls) el.className = cls; if (content != null) el.textContent = content; return el; }
  function emit(el, name, detail) {
    var ev;
    try { ev = new CustomEvent(name, { detail: detail, bubbles: true }); }
    catch (e) { ev = document.createEvent('CustomEvent'); ev.initCustomEvent(name, true, false, detail); }
    el.dispatchEvent(ev);
  }

  /* ------------------------------------------------------------- colour maths */

  /* "#abc", "abc", "#aabbcc" → "#aabbcc"; anything else → null. */
  function hex(value) {
    var s = String(value || '').trim().replace(/^#/, '').toLowerCase();
    if (/^[0-9a-f]{3}$/.test(s)) s = s.charAt(0) + s.charAt(0) + s.charAt(1) + s.charAt(1) + s.charAt(2) + s.charAt(2);
    return /^[0-9a-f]{6}$/.test(s) ? '#' + s : null;
  }
  function toRgb(h) { var n = parseInt(h.slice(1), 16); return [(n >> 16) & 255, (n >> 8) & 255, n & 255]; }
  function toHex(rgb) { return '#' + rgb.map(function (c) { var x = Math.round(Math.max(0, Math.min(255, c))).toString(16); return x.length < 2 ? '0' + x : x; }).join(''); }

  function toHsv(rgb) {
    var r = rgb[0] / 255, g = rgb[1] / 255, b = rgb[2] / 255;
    var max = Math.max(r, g, b), min = Math.min(r, g, b), d = max - min, h = 0;
    if (d) {
      if (max === r) h = ((g - b) / d) % 6;
      else if (max === g) h = (b - r) / d + 2;
      else h = (r - g) / d + 4;
      h *= 60;
      if (h < 0) h += 360;
    }
    return { h: h, s: max ? d / max : 0, v: max };
  }
  function fromHsv(h, s, v) {
    var c = v * s, x = c * (1 - Math.abs((h / 60) % 2 - 1)), m = v - c, r = 0, g = 0, b = 0;
    if (h < 60) { r = c; g = x; } else if (h < 120) { r = x; g = c; } else if (h < 180) { g = c; b = x; }
    else if (h < 240) { g = x; b = c; } else if (h < 300) { r = x; b = c; } else { r = c; b = x; }
    return [(r + m) * 255, (g + m) * 255, (b + m) * 255];
  }

  function brandHex() {
    var v = window.getComputedStyle(document.documentElement).getPropertyValue('--ins-primary');
    return hex(v);
  }

  /* ------------------------------------------------------------- the field */

  function state(input) { return input._insColor; }

  function paintSwatch(input) {
    var st = state(input);
    st.swatch.style.setProperty('--ins-color-value', st.value || 'transparent');
    st.swatch.classList.toggle('is-empty', !st.value);
  }

  /* The colour set: on the field, the swatch, the picker if it is open. `live` is
     a colour still moving under the pointer — `input` only; otherwise it is set,
     and `change` and `ins:color` follow if it is new. */
  function setValue(input, value, how) {
    var st = state(input), h = hex(value);
    if (!h) return false;
    var was = st.value;
    st.value = h;
    input.value = h;
    input.setCustomValidity('');
    paintSwatch(input);
    if (popFor === input && !applying) syncPicker(true);
    if (how === 'quiet') { st.committed = h; return true; }
    if (h !== was) input.dispatchEvent(new Event('input', { bubbles: true }));
    if (how !== 'live' && h !== st.committed) {
      st.committed = h;
      input.dispatchEvent(new Event('change', { bubbles: true }));
      emit(input, 'ins:color', { input: input, value: h });
    }
    return true;
  }

  function commitTyped(input) {
    var st = state(input), raw = input.value.trim();
    if (!raw) { st.value = ''; paintSwatch(input); input.setCustomValidity(''); return; }
    var h = hex(raw);
    if (h) setValue(input, h, 'set');
    else input.setCustomValidity(text(input).bad);
  }

  function build(input) {
    if (input.hasAttribute('data-ins-color-ready')) return;
    input.setAttribute('data-ins-color-ready', '');
    var initial = hex(input.value || input.getAttribute('value')) || '';
    var group = input.closest('.ins-input-group');
    if (!group) {
      group = make('div', 'ins-input-group');
      input.parentNode.insertBefore(group, input);
      group.appendChild(input);
    }
    group.classList.add('ins-color');
    group.dir = 'ltr';
    input.type = 'text';
    input.classList.add('ins-input');
    input.setAttribute('maxlength', '7');
    input.setAttribute('autocomplete', 'off');
    input.spellcheck = false;
    input.setAttribute('autocapitalize', 'off');
    input.setAttribute('inputmode', 'text');
    if (!input.placeholder) input.placeholder = '#000000';
    var swatch = make('button', 'ins-color-swatch');
    swatch.type = 'button';
    swatch.setAttribute('aria-label', text(input).pick);
    swatch.setAttribute('aria-haspopup', 'dialog');
    swatch.setAttribute('aria-expanded', 'false');
    if (input.disabled) swatch.disabled = true;
    group.insertBefore(swatch, group.firstChild);
    input._insColor = { swatch: swatch, value: initial, committed: initial, initial: initial };
    input.value = initial;
    input.setAttribute('value', initial);
    paintSwatch(input);
  }

  /* ------------------------------------------------------------- the picker */

  var pop = null, popFor = null, hsv = { h: 0, s: 0, v: 0 };
  /* Set while the picker itself sets the colour, so the colour is not read back
     into the picker's position — rounded to a hex on the way, it would nudge the
     thumb a pixel on every move. */
  var applying = false;
  var hasPopover = typeof HTMLElement !== 'undefined' && HTMLElement.prototype.hasOwnProperty('popover');

  function popLayer() {
    if (pop) return pop;
    pop = make('div', 'ins-color-pop');
    pop.setAttribute('role', 'dialog');
    if (hasPopover) pop.setAttribute('popover', 'manual');
    else pop.hidden = true;
    var area = make('div', 'ins-color-area');
    area.setAttribute('role', 'slider');
    area.tabIndex = 0;
    area.setAttribute('aria-valuemin', '0');
    area.setAttribute('aria-valuemax', '100');
    area.appendChild(make('span', 'ins-color-thumb'));
    var hue = make('input', 'ins-color-hue');
    hue.type = 'range';
    hue.min = '0';
    hue.max = '359';
    hue.step = '1';
    var row = make('div', 'ins-color-row');
    var drop = make('button', 'ins-btn ins-btn--icon ins-btn--sm ins-color-drop');
    drop.type = 'button';
    drop.hidden = !('EyeDropper' in window);
    row.appendChild(hue);
    row.appendChild(drop);
    var swatches = make('div', 'ins-color-swatches');
    swatches.setAttribute('role', 'group');
    var readout = make('div', 'ins-color-readout');
    pop.appendChild(area);
    pop.appendChild(row);
    pop.appendChild(swatches);
    pop.appendChild(readout);
    document.body.appendChild(pop);

    /* The square: press and drag, and the keyboard. */
    var dragging = false;
    function fromPoint(e) {
      var r = area.getBoundingClientRect();
      hsv.s = Math.max(0, Math.min(1, (e.clientX - r.left) / r.width));
      hsv.v = Math.max(0, Math.min(1, 1 - (e.clientY - r.top) / r.height));
      apply('live');
    }
    area.addEventListener('pointerdown', function (e) {
      dragging = true;
      try { area.setPointerCapture(e.pointerId); } catch (err) { /* synthetic */ }
      area.focus();
      fromPoint(e);
      e.preventDefault();
    });
    area.addEventListener('pointermove', function (e) { if (dragging) fromPoint(e); });
    function end() { if (dragging) { dragging = false; apply('set'); } }
    area.addEventListener('pointerup', end);
    area.addEventListener('pointercancel', end);
    area.addEventListener('keydown', function (e) {
      var step = e.shiftKey ? 0.1 : 0.01, handled = true;
      if (e.key === 'ArrowRight') hsv.s = Math.min(1, hsv.s + step);
      else if (e.key === 'ArrowLeft') hsv.s = Math.max(0, hsv.s - step);
      else if (e.key === 'ArrowUp') hsv.v = Math.min(1, hsv.v + step);
      else if (e.key === 'ArrowDown') hsv.v = Math.max(0, hsv.v - step);
      else if (e.key === 'PageUp') hsv.v = Math.min(1, hsv.v + 0.1);
      else if (e.key === 'PageDown') hsv.v = Math.max(0, hsv.v - 0.1);
      else handled = false;
      if (handled) { e.preventDefault(); apply('set'); }
    });
    hue.addEventListener('input', function () { hsv.h = +hue.value; apply('live'); });
    hue.addEventListener('change', function () { hsv.h = +hue.value; apply('set'); });
    swatches.addEventListener('click', function (e) {
      var b = e.target.closest && e.target.closest('.ins-color-preset');
      if (b && popFor) setValue(popFor, b.getAttribute('data-value'), 'set');
    });
    drop.addEventListener('click', function () {
      if (!popFor || !('EyeDropper' in window)) return;
      var input = popFor;
      new window.EyeDropper().open().then(function (r) { setValue(input, r.sRGBHex, 'set'); }, function () { /* cancelled */ });
    });
    pop.addEventListener('keydown', function (e) {
      if (e.key === 'Escape') { e.preventDefault(); e.stopPropagation(); closePop(true); }
    });
    return pop;
  }

  /* The picker's own state to the colour, without writing through the field. */
  function apply(how) {
    if (!popFor) return;
    applying = true;
    try { setValue(popFor, toHex(fromHsv(hsv.h, hsv.s, hsv.v)), how === 'live' ? 'live' : 'set'); }
    finally { applying = false; }
    syncPicker(false);
  }

  /* The picker drawn from the colour. `fromValue` reads the colour back into the
     picker's position — not while it is being dragged, or a grey would snap the
     hue back to red. */
  function syncPicker(fromValue) {
    var st = state(popFor), t = text(popFor);
    if (fromValue && st.value) {
      var next = toHsv(toRgb(st.value));
      if (next.s > 0 && next.v > 0) hsv.h = next.h;
      hsv.s = next.s;
      hsv.v = next.v;
    }
    var area = pop.querySelector('.ins-color-area'), thumb = area.firstChild, hue = pop.querySelector('.ins-color-hue');
    area.style.setProperty('--ins-color-hue', toHex(fromHsv(hsv.h, 1, 1)));
    thumb.style.left = (hsv.s * 100) + '%';
    thumb.style.top = ((1 - hsv.v) * 100) + '%';
    thumb.style.setProperty('--ins-color-value', st.value || '#000000');
    hue.value = String(Math.round(hsv.h) % 360);
    area.setAttribute('aria-valuenow', String(Math.round(hsv.s * 100)));
    area.setAttribute('aria-valuetext', fill(t.value, { s: Math.round(hsv.s * 100), v: Math.round(hsv.v * 100) }));
    syncReadout();
  }

  function syncReadout() {
    var st = state(popFor), t = text(popFor), readout = pop.querySelector('.ins-color-readout');
    readout.textContent = '';
    if (!st.value) return;
    var head = make('div', 'ins-color-readout-head');
    head.appendChild(make('span', 'ins-color-hex', st.value.toUpperCase()));
    head.appendChild(make('span', 'ins-color-contrast-label', t.contrast));
    readout.appendChild(head);
    [['#ffffff', t.onWhite], ['#000000', t.onBlack]].forEach(function (pair) {
      var ratio = Insiyab.color.contrast(st.value, pair[0]) || 1, ok = ratio >= 4.5;
      var line = make('div', 'ins-color-ratio ' + (ok ? 'is-pass' : 'is-fail'));
      var sample = make('span', 'ins-color-sample', 'Aa');
      sample.style.background = pair[0];
      sample.style.color = st.value;
      line.appendChild(sample);
      line.appendChild(make('span', 'ins-color-ratio-where', pair[1]));
      var num = make('span', 'ins-color-ratio-num', (Math.round(ratio * 10) / 10).toFixed(1) + ':1');
      num.dir = 'ltr';
      line.appendChild(num);
      line.appendChild(make('span', 'ins-color-ratio-verdict', ok ? t.pass : t.fail));
      readout.appendChild(line);
    });
  }

  function presets(input) {
    var own = input.getAttribute('data-ins-color-swatches');
    if (own === 'none') return [];
    var list = own ? own.split(',').map(hex).filter(Boolean) : [brandHex()].concat(DEFAULT_SWATCHES).filter(Boolean);
    return list.filter(function (h, i) { return list.indexOf(h) === i; });
  }

  function openPop(input) {
    if (popFor && popFor !== input) closePop(false);
    var st = state(input), layer = popLayer(), t = text(input);
    popFor = input;
    layer.setAttribute('lang', lang(input));
    layer.setAttribute('aria-label', t.pick);
    layer.querySelector('.ins-color-area').setAttribute('aria-label', t.area);
    layer.querySelector('.ins-color-hue').setAttribute('aria-label', t.hue);
    var drop = layer.querySelector('.ins-color-drop');
    drop.setAttribute('aria-label', t.drop);
    drop.title = t.drop;
    var box = layer.querySelector('.ins-color-swatches');
    box.setAttribute('aria-label', t.swatches);
    box.textContent = '';
    presets(input).forEach(function (h) {
      var b = make('button', 'ins-color-preset');
      b.type = 'button';
      b.setAttribute('data-value', h);
      b.setAttribute('aria-label', h.toUpperCase());
      b.style.setProperty('--ins-color-value', h);
      box.appendChild(b);
    });
    box.hidden = !box.children.length;
    if (!st.value) { hsv = { h: 0, s: 0, v: 0 }; }
    syncPicker(true);
    if (hasPopover) { try { layer.showPopover(); } catch (e) { /* not connected */ } }
    else { layer.hidden = false; layer.classList.add('is-open'); }
    place();
    st.swatch.setAttribute('aria-expanded', 'true');
    layer.querySelector('.ins-color-area').focus();
  }

  function closePop(refocus) {
    if (!pop || !popFor) return;
    var input = popFor;
    popFor = null;
    if (hasPopover) { try { pop.hidePopover(); } catch (e) { /* already hidden */ } }
    else { pop.classList.remove('is-open'); pop.hidden = true; }
    state(input).swatch.setAttribute('aria-expanded', 'false');
    if (refocus) state(input).swatch.focus();
  }

  function place() {
    if (!pop || !popFor) return;
    var anchor = state(popFor).swatch.parentNode.getBoundingClientRect();
    var gap = 6, pad = 8, vw = document.documentElement.clientWidth, vh = window.innerHeight;
    pop.style.left = '0px';
    pop.style.top = '0px';
    var w = pop.offsetWidth, h = pop.offsetHeight;
    var y = anchor.bottom + gap;
    if (y + h > vh - pad && anchor.top - gap - h >= pad) y = anchor.top - gap - h;
    pop.style.left = Math.round(Math.max(pad, Math.min(anchor.left, vw - w - pad))) + 'px';
    pop.style.top = Math.round(Math.max(pad, Math.min(y, vh - h - pad))) + 'px';
  }

  /* ------------------------------------------------------------- wiring */

  function field(el) { return el && el.matches && el.matches('input[data-ins-color-ready]') ? el : null; }

  document.addEventListener('click', function (e) {
    var sw = e.target.closest && e.target.closest('.ins-color-swatch');
    if (!sw) return;
    var input = field(sw.parentNode.querySelector('input'));
    if (!input) return;
    if (popFor === input) closePop(false);
    else openPop(input);
  }, false);
  /* Typing a hex: the swatch follows as soon as it is a colour. */
  document.addEventListener('input', function (e) {
    var input = field(e.target);
    if (!input || !e.isTrusted) return;
    var h = hex(input.value);
    if (h) { state(input).value = h; paintSwatch(input); input.setCustomValidity(''); if (popFor === input) syncPicker(true); }
  }, false);
  document.addEventListener('focusout', function (e) {
    var input = field(e.target);
    if (input) commitTyped(input);
    if (popFor && pop && pop.contains(e.target) && e.relatedTarget && !pop.contains(e.relatedTarget) && e.relatedTarget !== state(popFor).swatch) closePop(false);
  }, false);
  document.addEventListener('keydown', function (e) {
    var input = field(e.target);
    if (input && e.key === 'Enter') commitTyped(input);
  }, false);
  document.addEventListener('pointerdown', function (e) {
    if (!popFor || !pop) return;
    if (pop.contains(e.target) || state(popFor).swatch.contains(e.target)) return;
    closePop(false);
  }, true);
  window.addEventListener('scroll', function () { if (popFor) place(); }, true);
  window.addEventListener('resize', function () { if (popFor) place(); }, false);
  document.addEventListener('reset', function (e) {
    var form = e.target;
    setTimeout(function () {
      var inputs = form.querySelectorAll ? form.querySelectorAll('input[data-ins-color-ready]') : [];
      for (var i = 0; i < inputs.length; i++) {
        var st = state(inputs[i]);
        st.value = st.initial;
        st.committed = st.initial;
        inputs[i].value = st.initial;
        inputs[i].setCustomValidity('');
        paintSwatch(inputs[i]);
      }
    }, 0);
  }, true);

  function scan(scope) {
    var found = scope.querySelectorAll ? scope.querySelectorAll(FIELD) : [];
    for (var i = 0; i < found.length; i++) build(found[i]);
    if (scope.matches && scope.matches(FIELD)) build(scope);
  }

  /* Insiyab.colorField(field) reads the colour; Insiyab.colorField(field, '#0f766e')
     sets it, quietly. Insiyab.color stays the core's colour maths. */
  Insiyab.colorField = function (target, value) {
    var input = field(typeof target === 'string' ? document.querySelector(target) : target);
    if (!input) return null;
    if (value !== undefined) setValue(input, value, 'quiet');
    return state(input).value;
  };

  Insiyab.define('color-field', scan);
  if (document.readyState !== 'loading') scan(document);
})(window, document);
