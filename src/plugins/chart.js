/* ==========================================================================
   Insiyab · Chart plugin
   ==========================================================================

   Small charts drawn in SVG, in the library's colours, from a table:

     <figure class="ins-chart" data-ins-chart="bar">
       <table>
         <thead><tr><th></th><th>الطلبات</th><th>المرتجع</th></tr></thead>
         <tbody>
           <tr><th>يناير</th><td>120</td><td>14</td></tr> …
         </tbody>
       </table>
       <figcaption>…</figcaption>
     </figure>

   The first column names the categories, the header row the series. The table
   stays in the page, out of sight, so a screen reader reads the numbers and a
   person who cannot see the picture loses nothing; the drawing itself is hidden
   from assistive technology. With no table, `data-ins-values="3,5,4"` (and
   `data-ins-labels="أ,ب,ج"`) is one series, and `Insiyab.chart(el, data)` draws
   from script: `{ type, labels: [...], series: [{ name, values: [...] }] }`.

   Types: `bar` (grouped; `data-ins-stack` stacks them), `hbar` (bars along the
   line, for long names), `histogram` (bars that touch, for a distribution; several
   series overlap), `line` (`area` fills under it), `donut`, and `spark`, a line
   with no axes small enough to sit in a stat tile. The categories run the way the
   page reads, first at the right on an Arabic page; `dir="ltr"` on the figure turns
   a chart the other way. Figures are in the page's language with Latin digits.
   Pointing at a bar, a point or a slice shows its value. It redraws as its box
   changes size.
   ========================================================================== */

(function (window, document) {
  'use strict';

  var Insiyab = window.Insiyab;
  if (!Insiyab || !Insiyab.define) {
    if (window.console) window.console.warn('[insiyab-chart] load insiyab.js before this plugin.');
    return;
  }

  var NS = 'http://www.w3.org/2000/svg';
  var PALETTE = 6;

  function node(tag, attrs, parent) {
    var n = document.createElementNS(NS, tag);
    for (var k in attrs) if (Object.prototype.hasOwnProperty.call(attrs, k)) n.setAttribute(k, attrs[k]);
    if (parent) parent.appendChild(n);
    return n;
  }

  function html(tag, cls, text) {
    var n = document.createElement(tag);
    if (cls) n.className = cls;
    if (text !== undefined) n.textContent = text;
    return n;
  }

  function lang(el) {
    var host = el.closest('[lang]') || document.documentElement;
    return host.getAttribute('lang') || 'en';
  }

  var formats = {};
  function fmt(el, compact) {
    var locale = lang(el), key = locale + (compact ? '|c' : '');
    if (!formats[key]) {
      var tag = locale.indexOf('-u-') === -1 ? locale + '-u-nu-latn' : locale;
      var opts = compact ? { notation: 'compact', maximumFractionDigits: 1 } : { maximumFractionDigits: 2 };
      try { formats[key] = new Intl.NumberFormat(tag, opts); } catch (e) { formats[key] = new Intl.NumberFormat('en', opts); }
    }
    return formats[key];
  }

  /* A figure in a cell, in either set of digits, with its separators and signs. */
  function num(text) {
    var t = String(text == null ? '' : text)
      .replace(/[٠-٩]/g, function (d) { return String(d.charCodeAt(0) - 0x0660); })
      .replace(/[۰-۹]/g, function (d) { return String(d.charCodeAt(0) - 0x06f0); })
      .replace(/[\s,٬]/g, '').replace(/٫/g, '.').replace(/−/g, '-');
    var m = t.match(/-?\d*\.?\d+/);
    return m ? parseFloat(m[0]) : NaN;
  }

  /* ------------------------------------------------------------------ data */
  function fromTable(table) {
    var head = table.tHead && table.tHead.rows[0];
    var body = table.tBodies[0];
    if (!body) return null;
    var series = [];
    var width = head ? head.cells.length : (body.rows[0] ? body.rows[0].cells.length : 0);
    for (var c = 1; c < width; c++) series.push({ name: head ? head.cells[c].textContent.trim() : '', values: [] });
    var labels = [];
    for (var r = 0; r < body.rows.length; r++) {
      var row = body.rows[r];
      labels.push(row.cells[0] ? row.cells[0].textContent.trim() : '');
      for (var s = 0; s < series.length; s++) {
        var cell = row.cells[s + 1];
        series[s].values.push(cell ? num(cell.getAttribute('data-value') || cell.textContent) : NaN);
      }
    }
    return { labels: labels, series: series };
  }

  function fromAttributes(fig) {
    var values = fig.getAttribute('data-ins-values');
    if (!values) return null;
    var list = values.split(',').map(num);
    var labels = (fig.getAttribute('data-ins-labels') || '').split(',').map(function (s) { return s.trim(); });
    return { labels: list.map(function (v, i) { return labels[i] || ''; }), series: [{ name: fig.getAttribute('data-ins-name') || '', values: list }] };
  }

  function dataOf(fig) {
    if (fig.__insChart) return fig.__insChart;
    var table = fig.querySelector('table');
    return (table && fromTable(table)) || fromAttributes(fig) || { labels: [], series: [] };
  }

  /* ----------------------------------------------------------------- scale */
  /* Round steps for an axis: 1, 2 or 5 times a power of ten, about four of them. */
  function ticks(lo, hi, want) {
    if (lo === hi) { hi = lo + 1; }
    var raw = (hi - lo) / (want || 4);
    var mag = Math.pow(10, Math.floor(Math.log(raw) / Math.LN10));
    var step = [1, 2, 5, 10].map(function (m) { return m * mag; }).filter(function (s) { return s >= raw; })[0];
    var a = Math.floor(lo / step) * step, b = Math.ceil(hi / step) * step, out = [];
    for (var v = a; v <= b + step / 2; v += step) out.push(Math.round(v / step) * step);
    return { lo: a, hi: b, list: out };
  }

  function range(series, stacked) {
    var lo = 0, hi = 0;
    if (stacked) {
      for (var i = 0; i < series[0].values.length; i++) {
        var up = 0, down = 0;
        for (var s = 0; s < series.length; s++) { var v = series[s].values[i]; if (v > 0) up += v; else if (v < 0) down += v; }
        hi = Math.max(hi, up); lo = Math.min(lo, down);
      }
    } else {
      series.forEach(function (s) { s.values.forEach(function (v) { if (!isNaN(v)) { hi = Math.max(hi, v); lo = Math.min(lo, v); } }); });
    }
    return { lo: lo, hi: hi };
  }

  function tone(i) { return 'ins-chart-s' + (i % PALETTE + 1); }

  /* ---------------------------------------------------------------- charts */
  /* The frame every axis chart shares: the plot's box, a value-to-position scale
     and a category-to-position one, both turned round for a right-to-left page. */
  function frame(fig, box, data, opts) {
    var f = fmt(fig, true);
    var r = range(data.series, opts.stacked);
    var t = ticks(r.lo, r.hi, box.h < 140 ? 3 : 4);
    var labelW = Math.max.apply(null, t.list.map(function (v) { return f.format(v).length; })) * 7 + 12;
    var pad = opts.horizontal
      ? { start: Math.min(box.w * .35, Math.max.apply(null, data.labels.map(function (l) { return l.length; }).concat([2])) * 7 + 14), end: 12, top: 6, bottom: 22 }
      : { start: labelW, end: 8, top: 10, bottom: 26 };
    var plot = { x0: pad.start, x1: box.w - pad.end, y0: pad.top, y1: box.h - pad.bottom };
    var rtl = box.rtl;
    /* Physical x for a logical offset from the start edge. */
    var X = function (along) { return rtl ? box.w - along : along; };
    return { f: f, t: t, plot: plot, X: X, rtl: rtl };
  }

  function grid(svg, fig, fr, horizontal) {
    var g = node('g', { 'class': 'ins-chart-grid' }, svg);
    var p = fr.plot, span = fr.t.hi - fr.t.lo;
    fr.t.list.forEach(function (v) {
      var at = (v - fr.t.lo) / span;
      if (horizontal) {
        var x = fr.X(p.x0 + at * (p.x1 - p.x0));
        node('line', { x1: x, x2: x, y1: p.y0, y2: p.y1, 'class': v === 0 ? 'is-zero' : '' }, g);
        var tx = node('text', { x: x, y: p.y1 + 16, 'text-anchor': 'middle' }, g);
        tx.textContent = fr.f.format(v);
      } else {
        var y = p.y1 - at * (p.y1 - p.y0);
        node('line', { x1: fr.X(p.x0), x2: fr.X(p.x1), y1: y, y2: y, 'class': v === 0 ? 'is-zero' : '' }, g);
        var ty = node('text', { x: fr.X(p.x0 - 8), y: y + 4, 'text-anchor': fr.rtl ? 'start' : 'end' }, g);
        ty.textContent = fr.f.format(v);
      }
    });
  }

  function mark(el, label, name, value) {
    el.setAttribute('data-label', label);
    el.setAttribute('data-name', name);
    el.setAttribute('data-value', value);
    return el;
  }

  function bars(svg, fig, box, data, type) {
    var horizontal = type === 'hbar', touching = type === 'histogram';
    var stacked = fig.hasAttribute('data-ins-stack');
    var fr = frame(fig, box, data, { horizontal: horizontal, stacked: stacked });
    grid(svg, fig, fr, horizontal);
    var p = fr.plot, n = data.labels.length, span = fr.t.hi - fr.t.lo;
    var along = horizontal ? p.y1 - p.y0 : p.x1 - p.x0;
    var slot = along / Math.max(n, 1);
    var sets = data.series.length;
    var overlap = touching && sets > 1;
    var groupW = touching ? slot - 1 : Math.min(slot * .72, sets * 34);
    var barW = stacked || overlap ? groupW : groupW / sets;
    var zero = (0 - fr.t.lo) / span;
    var g = node('g', { 'class': 'ins-chart-marks' }, svg);
    var labels = node('g', { 'class': 'ins-chart-labels' }, svg);
    data.labels.forEach(function (label, i) {
      var mid = slot * i + slot / 2;
      var up = 0, down = 0;
      data.series.forEach(function (s, k) {
        var v = s.values[i];
        if (isNaN(v)) return;
        var from = zero, to;
        if (stacked) {
          if (v >= 0) { from = (up - fr.t.lo) / span; up += v; to = (up - fr.t.lo) / span; }
          else { from = (down - fr.t.lo) / span; down += v; to = (down - fr.t.lo) / span; }
        } else {
          to = (v - fr.t.lo) / span;
        }
        var offset = stacked || overlap ? -groupW / 2 : -groupW / 2 + barW * k;
        var a = Math.min(from, to), b = Math.max(from, to), rect;
        if (horizontal) {
          var len = p.x1 - p.x0;
          var x0 = p.x0 + a * len, x1 = p.x0 + b * len;
          var y = p.y0 + mid + offset;
          rect = node('rect', { x: Math.min(fr.X(x0), fr.X(x1)), y: y, width: Math.max(1, x1 - x0), height: Math.max(1, barW - 2), rx: Math.min(4, barW / 3) }, g);
        } else {
          var h = p.y1 - p.y0;
          var top = p.y1 - b * h, bottom = p.y1 - a * h;
          var left = p.x0 + mid + offset + (touching ? 0 : 1);
          var w = Math.max(1, barW - (touching ? 1 : 2));
          rect = node('rect', { x: fr.rtl ? box.w - left - w : left, y: top, width: w, height: Math.max(1, bottom - top), rx: touching ? 1 : Math.min(4, w / 3) }, g);
        }
        rect.setAttribute('class', 'ins-chart-bar ' + tone(k) + (overlap ? ' is-overlap' : ''));
        rect.style.animationDelay = Math.min(i * 30, 300) + 'ms';
        mark(rect, label, s.name, v);
      });
      /* The category's name, under its bars, or at the start of its row. Thinned out
         when there are more names than room for them. */
      var every = Math.ceil(n / Math.max(1, Math.floor(along / (horizontal ? 18 : 44))));
      if (i % every) return;
      var text;
      if (horizontal) {
        text = node('text', { x: fr.X(p.x0 - 8), y: p.y0 + mid + 4, 'text-anchor': fr.rtl ? 'start' : 'end' }, labels);
      } else {
        text = node('text', { x: fr.X(p.x0 + mid), y: p.y1 + 17, 'text-anchor': 'middle' }, labels);
      }
      text.textContent = label;
    });
  }

  function lines(svg, fig, box, data, type, bare) {
    var fr = bare ? null : frame(fig, box, data, {});
    var p, span, lo, X;
    if (bare) {
      var r = range(data.series, false);
      lo = Math.min(r.lo, Math.min.apply(null, data.series[0].values.filter(function (v) { return !isNaN(v); })));
      var hi = r.hi;
      span = (hi - lo) || 1;
      p = { x0: 2, x1: box.w - 2, y0: 3, y1: box.h - 3 };
      X = function (a) { return box.rtl ? box.w - a : a; };
    } else {
      grid(svg, fig, fr, false);
      p = fr.plot; lo = fr.t.lo; span = fr.t.hi - fr.t.lo; X = fr.X;
    }
    var n = data.labels.length;
    var step = n > 1 ? (p.x1 - p.x0) / (n - 1) : 0;
    var g = node('g', { 'class': 'ins-chart-marks' }, svg);
    data.series.forEach(function (s, k) {
      var pts = [];
      s.values.forEach(function (v, i) {
        if (isNaN(v)) return;
        pts.push([X(p.x0 + step * i), p.y1 - (v - lo) / span * (p.y1 - p.y0), v, data.labels[i]]);
      });
      if (!pts.length) return;
      var d = pts.map(function (q, i) { return (i ? 'L' : 'M') + q[0].toFixed(1) + ' ' + q[1].toFixed(1); }).join(' ');
      if (type === 'area' || bare) {
        /* Down to the zero line, or to the foot of a spark, which has none. */
        var base = bare ? p.y1 : p.y1 - (0 - lo) / span * (p.y1 - p.y0);
        node('path', { d: d + ' L' + pts[pts.length - 1][0].toFixed(1) + ' ' + base + ' L' + pts[0][0].toFixed(1) + ' ' + base + ' Z', 'class': 'ins-chart-area ' + tone(k) }, g);
      }
      node('path', { d: d, 'class': 'ins-chart-line ' + tone(k), pathLength: '1' }, g);
      if (bare) return;
      pts.forEach(function (q) {
        mark(node('circle', { cx: q[0], cy: q[1], r: 3.5, 'class': 'ins-chart-dot ' + tone(k) }, g), q[3], s.name, q[2]);
      });
    });
    if (bare) return;
    var labels = node('g', { 'class': 'ins-chart-labels' }, svg);
    var every = Math.ceil(n / Math.max(1, Math.floor((p.x1 - p.x0) / 48)));
    data.labels.forEach(function (label, i) {
      if (i % every) return;
      var t = node('text', { x: X(p.x0 + step * i), y: p.y1 + 17, 'text-anchor': 'middle' }, labels);
      t.textContent = label;
    });
  }

  function donut(svg, fig, box, data) {
    var values = data.series[0] ? data.series[0].values : [];
    var total = values.reduce(function (a, v) { return a + (isNaN(v) || v < 0 ? 0 : v); }, 0);
    var size = Math.min(box.w, box.h), r = size / 2 - 6, cx = box.w / 2, cy = box.h / 2;
    var width = Math.max(10, r * .32), ring = r - width / 2;
    var circ = 2 * Math.PI * ring, at = 0;
    var g = node('g', { 'class': 'ins-chart-marks', transform: 'rotate(-90 ' + cx + ' ' + cy + ')' }, svg);
    node('circle', { cx: cx, cy: cy, r: ring, 'class': 'ins-chart-track', 'stroke-width': width }, g);
    values.forEach(function (v, i) {
      if (isNaN(v) || v <= 0 || !total) return;
      var len = v / total * circ;
      var seg = node('circle', {
        cx: cx, cy: cy, r: ring, 'stroke-width': width,
        'stroke-dasharray': Math.max(0, len - 2) + ' ' + circ, 'stroke-dashoffset': -at,
        'class': 'ins-chart-slice ' + tone(i)
      }, g);
      seg.style.animationDelay = Math.min(i * 60, 360) + 'ms';
      mark(seg, data.labels[i], data.series[0].name, v);
      at += len;
    });
    var center = node('text', { x: cx, y: cy + 2, 'text-anchor': 'middle', 'dominant-baseline': 'middle', 'class': 'ins-chart-total' }, svg);
    center.textContent = fig.getAttribute('data-ins-center') || fmt(fig, total >= 10000).format(total);
    var sub = fig.getAttribute('data-ins-center-label');
    if (sub) {
      center.setAttribute('y', cy - 6);
      var t = node('text', { x: cx, y: cy + 14, 'text-anchor': 'middle', 'class': 'ins-chart-total-label' }, svg);
      t.textContent = sub;
    }
  }

  /* ---------------------------------------------------------------- legend */
  function legend(fig, data, type) {
    var old = fig.querySelector('.ins-chart-legend');
    if (old) old.parentNode.removeChild(old);
    var items = type === 'donut' ? data.labels : data.series.length > 1 ? data.series.map(function (s) { return s.name; }) : [];
    if (!items.length || type === 'spark') return;
    var list = html('ul', 'ins-chart-legend');
    list.setAttribute('aria-hidden', 'true');
    items.forEach(function (name, i) {
      var li = html('li', '', name);
      li.insertBefore(html('span', 'ins-chart-key ' + tone(i)), li.firstChild);
      if (type === 'donut') {
        var v = data.series[0].values[i];
        li.appendChild(html('b', 'ins-chart-key-value', isNaN(v) ? '' : fmt(fig).format(v)));
      }
      list.appendChild(li);
    });
    var caption = fig.querySelector('figcaption');
    fig.insertBefore(list, caption && caption.parentNode === fig ? caption : null);
  }

  /* --------------------------------------------------------------- tooltip */
  function tip(fig) {
    var t = fig.querySelector('.ins-chart-tip');
    if (!t) { t = html('div', 'ins-chart-tip'); t.setAttribute('aria-hidden', 'true'); t.hidden = true; fig.appendChild(t); }
    return t;
  }

  function onMove(event) {
    var fig = this, m = event.target.closest ? event.target.closest('[data-value]') : null, t = tip(fig);
    var lit = fig.querySelector('.ins-chart-plot .is-lit');
    if (lit && lit !== m) lit.classList.remove('is-lit');
    if (!m || !fig.contains(m)) { t.hidden = true; return; }
    m.classList.add('is-lit');
    var name = m.getAttribute('data-name'), label = m.getAttribute('data-label');
    t.textContent = '';
    t.appendChild(html('span', 'ins-chart-tip-label', label + (name && name !== label ? ' · ' + name : '')));
    t.appendChild(html('b', '', fmt(fig).format(parseFloat(m.getAttribute('data-value')))));
    t.hidden = false;
    var box = fig.getBoundingClientRect(), r = m.getBoundingClientRect();
    var x = r.left + r.width / 2 - box.left, y = r.top - box.top;
    t.style.left = Math.max(4, Math.min(box.width - t.offsetWidth - 4, x - t.offsetWidth / 2)) + 'px';
    t.style.top = Math.max(0, y - t.offsetHeight - 8) + 'px';
  }

  function onLeave() {
    var t = this.querySelector('.ins-chart-tip');
    if (t) t.hidden = true;
    var lit = this.querySelector('.ins-chart-plot .is-lit');
    if (lit) lit.classList.remove('is-lit');
  }

  /* ------------------------------------------------------------------ draw */
  function draw(fig) {
    var type = fig.getAttribute('data-ins-chart') || 'bar';
    var data = dataOf(fig);
    var plot = fig.querySelector('.ins-chart-plot');
    if (!plot) {
      plot = html('div', 'ins-chart-plot');
      var caption = fig.querySelector('figcaption');
      fig.insertBefore(plot, caption && caption.parentNode === fig ? caption : null);
    }
    plot.textContent = '';
    var table = fig.querySelector('table');
    if (table) table.classList.add('ins-chart-data');
    else if (!fig.hasAttribute('aria-label') && data.series[0]) {
      fig.setAttribute('role', 'img');
      fig.setAttribute('aria-label', data.labels.map(function (l, i) {
        return (l ? l + ': ' : '') + fmt(fig).format(data.series[0].values[i]);
      }).join('، '));
    }
    var box = { w: plot.clientWidth, h: plot.clientHeight, rtl: window.getComputedStyle(fig).direction === 'rtl' };
    if (box.w < 10 || box.h < 10 || !data.series.length) return;
    var svg = node('svg', { width: box.w, height: box.h, viewBox: '0 0 ' + box.w + ' ' + box.h, 'aria-hidden': 'true', focusable: 'false' }, plot);
    if (type === 'line' || type === 'area') lines(svg, fig, box, data, type, false);
    else if (type === 'spark') lines(svg, fig, box, data, type, true);
    else if (type === 'donut') donut(svg, fig, box, data);
    else bars(svg, fig, box, data, type);
    legend(fig, data, type);
    fig.__insWidth = box.w;
  }

  var watcher = window.ResizeObserver ? new ResizeObserver(function (entries) {
    entries.forEach(function (e) {
      var fig = e.target;
      if (Math.abs((fig.__insWidth || 0) - fig.clientWidth) < 2) return;
      window.cancelAnimationFrame(fig.__insFrame);
      fig.__insFrame = window.requestAnimationFrame(function () { draw(fig); });
    });
  }) : null;

  function setup(fig) {
    if (!fig.__insReady) {
      fig.__insReady = true;
      fig.addEventListener('pointermove', onMove);
      fig.addEventListener('pointerleave', onLeave);
      if (watcher) watcher.observe(fig);
    }
    draw(fig);
  }

  function scan(scope) {
    var figs = scope.querySelectorAll('.ins-chart[data-ins-chart]');
    for (var i = 0; i < figs.length; i++) setup(figs[i]);
  }
  Insiyab.define('charts', scan);
  if (document.readyState !== 'loading') scan(document);

  /* Draw from data: `{ type, labels, series: [{ name, values }] }`, or with no data
     draw again from the table (after the page has changed it). A data table is
     written into the figure too, so the numbers are still there for a screen reader. */
  Insiyab.chart = function (target, data) {
    var fig = typeof target === 'string' ? document.querySelector(target) : target;
    if (!fig) return null;
    if (data) {
      if (data.type) fig.setAttribute('data-ins-chart', data.type);
      fig.__insChart = { labels: data.labels || [], series: data.series || [] };
      var table = fig.querySelector('table') || fig.insertBefore(html('table'), fig.firstChild);
      table.textContent = '';
      var head = table.appendChild(html('thead')).appendChild(html('tr'));
      head.appendChild(html('th'));
      fig.__insChart.series.forEach(function (s) { head.appendChild(html('th', '', s.name || '')); });
      var body = table.appendChild(html('tbody'));
      fig.__insChart.labels.forEach(function (l, i) {
        var tr = body.appendChild(html('tr'));
        tr.appendChild(html('th', '', l));
        fig.__insChart.series.forEach(function (s) { tr.appendChild(html('td', '', String(s.values[i]))); });
      });
    } else {
      fig.__insChart = null;
    }
    if (!fig.classList.contains('ins-chart')) fig.classList.add('ins-chart');
    if (!fig.hasAttribute('data-ins-chart')) fig.setAttribute('data-ins-chart', 'bar');
    setup(fig);
    return fig;
  };
})(window, document);
