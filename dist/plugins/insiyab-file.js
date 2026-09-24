/*! Insiyab UI v0.5.0 · https://github.com/Mohammad-Diab/insiyab-ui#readme · MIT (fonts: OFL 1.1, see fonts/LICENSE-*.txt) */
/* ==========================================================================
   Insiyab · File upload plugin
   ==========================================================================

   A drop zone and a list of the files chosen. Load it after insiyab.js, with its
   stylesheet:

     <link rel="stylesheet" href="plugins/insiyab-file.css">
     <script src="insiyab.js"></script>
     <script src="plugins/insiyab-file.js"></script>

     <input type="file" name="docs" multiple accept=".pdf,image/*"
            data-ins-file data-ins-file-max="5MB" data-ins-file-count="5">

   **The files stay in the real input.** Each change — a pick, a drop, a paste, a
   removal — is written back to the input's own `files`, so the form sends them as
   it would have without the plugin and the server needs nothing new. The input
   stays in the tab order under its label: the keyboard and a screen reader use the
   browser's own file button, and the zone around it is for the pointer.

     data-ins-file-max="5MB"     largest file (B, KB, MB, GB)
     data-ins-file-count="5"     most files, with `multiple`
     data-ins-file-note="…"      the zone's small print, in place of the one made
                                 from the limits
     data-ins-file-upload        upload each file as it is added — see below
     class="ins-file--compact"   on the input: a button and the list, no zone

   A file the input's `accept` would have refused, one too large, one past the
   count, is not added, and the field says which and why. With `multiple` a new pick
   adds to the list rather than replacing it; without, it replaces.

   Uploading on the spot is the page's to do, with whatever request its server
   wants. With `data-ins-file-upload`, every file added fires `ins:file`; the page
   sends it and reports back through Insiyab.file.progress(file, 0.4),
   Insiyab.file.done(file, id) and Insiyab.file.fail(file, message). The input then
   sends no files of its own: each finished upload leaves a hidden input under the
   field's name holding the id the page gave, and the field is invalid while any
   file is still on its way or has failed.
   ========================================================================== */

(function (window, document) {
  'use strict';

  var Insiyab = window.Insiyab;
  if (!Insiyab || !Insiyab.define) {
    if (window.console) window.console.warn('[insiyab-file] load insiyab.js before this plugin.');
    return;
  }

  var FIELD = 'input[type="file"][data-ins-file]';
  var READY = 'input[data-ins-file-ready]';

  var TEXT = {
    ar: {
      drag: 'اسحب الملفات إلى هنا أو', drag1: 'اسحب الملف إلى هنا أو', pick: 'اختر ملفات', pick1: 'اختر ملفًا',
      upTo: 'حتّى {max} للملف', most: '{n} ملفات على الأكثر', remove: 'إزالة {name}',
      type: 'نوع «{name}» غير مقبول.', big: '«{name}» أكبر من {max}.', count: 'لا يُقبل أكثر من {n} ملفات، فلم يُضف «{name}».',
      sending: 'يُرفع…', failed: 'تعذّر الرفع.', wait: 'انتظر حتّى يكتمل رفع الملفات.', fix: 'أزِل الملفات التي تعذّر رفعها أو أعد رفعها.'
    },
    en: {
      drag: 'Drag files here or', drag1: 'Drag a file here or', pick: 'choose files', pick1: 'choose a file',
      upTo: 'up to {max} each', most: '{n} files at most', remove: 'Remove {name}',
      type: '“{name}” is not a type this field accepts.', big: '“{name}” is larger than {max}.', count: 'No more than {n} files, so “{name}” was not added.',
      sending: 'Uploading…', failed: 'Upload failed.', wait: 'Wait for the files to finish uploading.', fix: 'Remove or re-add the files that failed to upload.'
    }
  };

  function lang(el) {
    var host = el.closest('[lang]') || document.documentElement;
    return host.getAttribute('lang') || 'en';
  }
  function text(el) { return lang(el).slice(0, 2).toLowerCase() === 'ar' ? TEXT.ar : TEXT.en; }
  function fill(s, map) { return s.replace(/\{(\w+)\}/g, function (all, k) { return map[k] != null ? map[k] : all; }); }

  function make(tag, cls, content) {
    var el = document.createElement(tag);
    if (cls) el.className = cls;
    if (content != null) el.textContent = content;
    return el;
  }

  /* ------------------------------------------------------------- limits */

  var UNITS = { b: 1, kb: 1024, mb: 1048576, gb: 1073741824 };
  function bytes(spec) {
    var m = /^\s*([\d.]+)\s*([kmg]?b)?\s*$/i.exec(spec || '');
    return m ? Math.round(parseFloat(m[1]) * UNITS[(m[2] || 'b').toLowerCase()]) : 0;
  }

  /* A size the way the page's language writes it, in the Latin digits the library
     sets every figure in. */
  function size(n, el) {
    var unit = n >= 1048576 ? ['megabyte', n / 1048576] : n >= 1024 ? ['kilobyte', n / 1024] : ['byte', n];
    try {
      return new Intl.NumberFormat(lang(el), { style: 'unit', unit: unit[0], unitDisplay: 'short', maximumFractionDigits: unit[1] < 10 ? 1 : 0, numberingSystem: 'latn' }).format(unit[1]);
    } catch (e) {
      return Math.round(unit[1] * 10) / 10 + ' ' + { megabyte: 'MB', kilobyte: 'KB', byte: 'B' }[unit[0]];
    }
  }

  /* `accept` as the picker reads it: extensions, exact types, and type/* families.
     A drop and a paste do not go through the picker, so they are checked here. */
  function accepts(input, file) {
    var list = (input.getAttribute('accept') || '').split(',').map(function (s) { return s.trim().toLowerCase(); }).filter(Boolean);
    if (!list.length) return true;
    var name = file.name.toLowerCase(), type = (file.type || '').toLowerCase();
    for (var i = 0; i < list.length; i++) {
      var a = list[i];
      if (a.charAt(0) === '.' ? name.slice(-a.length) === a : a.slice(-2) === '/*' ? type.indexOf(a.slice(0, -1)) === 0 : type === a) return true;
    }
    return false;
  }

  function same(a, b) { return a.name === b.name && a.size === b.size && a.lastModified === b.lastModified; }

  /* ------------------------------------------------------------- state */

  var records = typeof WeakMap === 'function' ? new WeakMap() : null;   /* file -> { input, item, status, value } */
  function state(input) { return input._insFile; }

  /* The list written back to the input, so the form sends exactly what is shown. */
  function writeBack(input) {
    var st = state(input);
    try {
      var dt = new DataTransfer();
      st.files.forEach(function (f) { dt.items.add(f); });
      input.files = dt.files;
    } catch (e) { /* an engine without a DataTransfer constructor: the list is still right */ }
  }

  function changed(input) {
    var st = state(input);
    st.echo = true;
    input.dispatchEvent(new Event('input', { bubbles: true }));
    input.dispatchEvent(new Event('change', { bubbles: true }));
    st.echo = false;
  }

  /* Adds what came in — from the picker, a drop or a paste — within the limits,
     and says what was left out. */
  function add(input, incoming, fromPicker) {
    var st = state(input), t = text(input), issues = [];
    var multi = input.multiple, max = bytes(input.getAttribute('data-ins-file-max'));
    var count = multi ? parseInt(input.getAttribute('data-ins-file-count'), 10) || 0 : 1;
    var next = multi ? st.files.slice() : [], added = [];
    for (var i = 0; i < incoming.length; i++) {
      var f = incoming[i];
      if (!accepts(input, f)) { issues.push(fill(t.type, { name: f.name })); continue; }
      if (max && f.size > max) { issues.push(fill(t.big, { name: f.name, max: size(max, input) })); continue; }
      if (next.some(function (x) { return same(x, f); })) continue;
      if (!multi) { next = [f]; added = [f]; continue; }
      if (count && next.length >= count) { issues.push(fill(t.count, { n: count, name: f.name })); continue; }
      next.push(f);
      added.push(f);
    }
    var gone = st.files.filter(function (x) { return next.indexOf(x) === -1; });
    st.files = next;
    gone.forEach(function (f) { forget(input, f); });
    writeBack(input);
    render(input);
    report(input, issues);
    added.forEach(function (f) { announce(input, f); });
    /* A pick is read while its own change event is still on the way down, so the
       page's listeners already see the full list in it; anything else needs one. */
    if (!fromPicker) changed(input);
  }

  function remove(input, file) {
    var st = state(input);
    st.files = st.files.filter(function (x) { return x !== file; });
    forget(input, file);
    writeBack(input);
    render(input);
    report(input, []);
    changed(input);
    input.focus();
  }

  function forget(input, file) {
    var rec = records && records.get(file);
    if (rec && rec.url) URL.revokeObjectURL(rec.url);
    if (rec && rec.hidden && rec.hidden.parentNode) rec.hidden.parentNode.removeChild(rec.hidden);
    if (records) records.delete(file);
  }

  /* Each file added fires `ins:file`. With the upload opt-in the file starts out
     on its way, and stays so until the page says otherwise. */
  function announce(input, file) {
    var st = state(input), rec = record(input, file);
    if (st.upload) { rec.status = 'sending'; rec.fraction = null; paint(rec); }
    var detail = { input: input, file: file, item: rec.item, upload: st.upload };
    var event;
    try { event = new CustomEvent('ins:file', { detail: detail, bubbles: true }); }
    catch (e) { event = document.createEvent('CustomEvent'); event.initCustomEvent('ins:file', true, false, detail); }
    input.dispatchEvent(event);
    check(input);
  }

  function record(input, file) {
    var rec = records && records.get(file);
    if (!rec) { rec = { input: input, file: file, item: null, status: 'ready' }; if (records) records.set(file, rec); }
    return rec;
  }

  /* ------------------------------------------------------------- drawing */

  function thumb(rec) {
    var f = rec.file;
    if (/^image\//.test(f.type) && window.URL && URL.createObjectURL) {
      if (!rec.url) rec.url = URL.createObjectURL(f);
      var img = make('img', 'ins-file-thumb');
      img.src = rec.url;
      img.alt = '';
      return img;
    }
    var dot = f.name.lastIndexOf('.');
    var ext = dot > 0 ? f.name.slice(dot + 1, dot + 5).toUpperCase() : '';
    var box = make('span', 'ins-file-thumb ins-file-thumb--type', ext || '·');
    box.setAttribute('aria-hidden', 'true');
    return box;
  }

  function item(input, rec) {
    var t = text(input), li = make('li', 'ins-file-item');
    li.appendChild(thumb(rec));
    var meta = make('div', 'ins-file-meta');
    var name = make('span', 'ins-file-name', rec.file.name);
    name.dir = 'auto';
    name.title = rec.file.name;
    meta.appendChild(name);
    meta.appendChild(make('span', 'ins-file-size', size(rec.file.size, input)));
    li.appendChild(meta);
    var x = make('button', 'ins-close ins-close--sm ins-file-remove');
    x.type = 'button';
    x.setAttribute('aria-label', fill(t.remove, { name: rec.file.name }));
    if (input.disabled) x.disabled = true;
    x.addEventListener('click', function () { remove(input, rec.file); });
    li.appendChild(x);
    rec.item = li;
    paint(rec);
    return li;
  }

  /* An upload's state on its row: a bar while it is on its way (without a value
     until the page reports one), a message if it failed, nothing once it is done. */
  function paint(rec) {
    var li = rec.item;
    if (!li) return;
    var t = text(rec.input), meta = li.querySelector('.ins-file-meta');
    var bar = meta.querySelector('.ins-progress'), note = meta.querySelector('.ins-file-status');
    li.classList.toggle('is-sending', rec.status === 'sending');
    li.classList.toggle('is-done', rec.status === 'done');
    li.classList.toggle('is-failed', rec.status === 'failed');
    if (rec.status === 'sending') {
      if (!bar) { bar = make('progress', 'ins-progress'); meta.appendChild(bar); }
      bar.max = 1;
      if (rec.fraction == null) bar.removeAttribute('value');
      else bar.value = Math.max(0, Math.min(1, rec.fraction));
      bar.setAttribute('aria-label', t.sending + ' ' + rec.file.name);
    } else if (bar) bar.parentNode.removeChild(bar);
    if (rec.status === 'failed') {
      if (!note) { note = make('span', 'ins-file-status'); meta.appendChild(note); }
      note.textContent = rec.message || t.failed;
    } else if (note) note.parentNode.removeChild(note);
  }

  function render(input) {
    var st = state(input);
    st.list.textContent = '';
    for (var i = 0; i < st.files.length; i++) st.list.appendChild(item(input, record(input, st.files[i])));
    st.list.hidden = !st.files.length;
    st.box.classList.toggle('has-files', st.files.length > 0);
    check(input);
  }

  function report(input, issues) {
    var st = state(input);
    st.issues.textContent = '';
    for (var i = 0; i < issues.length; i++) st.issues.appendChild(make('div', '', issues[i]));
    st.issues.hidden = !issues.length;
  }

  /* While an upload is on its way or has failed, the form should not go. */
  function check(input) {
    var st = state(input), t = text(input), msg = '';
    if (st.upload) {
      for (var i = 0; i < st.files.length; i++) {
        var rec = records && records.get(st.files[i]);
        if (rec && rec.status === 'failed') { msg = t.fix; break; }
        if (rec && rec.status === 'sending') msg = t.wait;
      }
    }
    input.setCustomValidity(msg);
  }

  /* ------------------------------------------------------------- building */

  function note(input) {
    var own = input.getAttribute('data-ins-file-note');
    if (own != null) return own;
    var t = text(input), parts = [], max = bytes(input.getAttribute('data-ins-file-max'));
    var count = input.multiple ? parseInt(input.getAttribute('data-ins-file-count'), 10) : 0;
    if (max) parts.push(fill(t.upTo, { max: size(max, input) }));
    if (count) parts.push(fill(t.most, { n: count }));
    return parts.join(lang(input).slice(0, 2) === 'ar' ? '، ' : ', ');
  }

  function build(input) {
    if (input.hasAttribute('data-ins-file-ready')) return;
    input.setAttribute('data-ins-file-ready', '');
    var t = text(input), multi = input.multiple;
    var box = make('div', 'ins-file' + (input.classList.contains('ins-file--compact') ? ' ins-file--compact' : ''));
    input.classList.remove('ins-file--compact');
    input.parentNode.insertBefore(box, input);

    var zone = make('div', 'ins-file-zone');
    zone.setAttribute('aria-hidden', 'true');
    zone.appendChild(make('span', 'ins-file-ico'));
    var lead = make('span', 'ins-file-lead');
    lead.appendChild(make('span', 'ins-file-drag', multi ? t.drag : t.drag1));
    lead.appendChild(document.createTextNode(' '));
    lead.appendChild(make('span', 'ins-file-pick', multi ? t.pick : t.pick1));
    zone.appendChild(lead);
    var small = note(input);
    if (small) zone.appendChild(make('span', 'ins-file-note', small));

    var list = make('ul', 'ins-file-list');
    list.hidden = true;
    var issues = make('div', 'ins-file-issues');
    issues.setAttribute('role', 'alert');
    issues.hidden = true;

    box.appendChild(input);
    box.appendChild(zone);
    box.appendChild(issues);
    box.appendChild(list);

    var upload = input.hasAttribute('data-ins-file-upload');
    input._insFile = { box: box, zone: zone, list: list, issues: issues, files: [], upload: upload, echo: false, name: input.name };
    /* Uploaded files are sent as ids, from hidden inputs; the input sends none. */
    if (upload && input.name) input.removeAttribute('name');

    zone.addEventListener('click', function () { if (!input.disabled) input.click(); });
    var depth = 0;
    box.addEventListener('dragenter', function (e) {
      if (input.disabled || !hasFiles(e)) return;
      e.preventDefault();
      depth++;
      box.classList.add('is-over');
    });
    box.addEventListener('dragover', function (e) {
      if (input.disabled || !hasFiles(e)) return;
      e.preventDefault();
      if (e.dataTransfer) e.dataTransfer.dropEffect = 'copy';
    });
    box.addEventListener('dragleave', function () { if (--depth <= 0) { depth = 0; box.classList.remove('is-over'); } });
    box.addEventListener('drop', function (e) {
      depth = 0;
      box.classList.remove('is-over');
      if (input.disabled || !e.dataTransfer || !e.dataTransfer.files.length) return;
      e.preventDefault();
      add(input, Array.prototype.slice.call(e.dataTransfer.files), false);
    });

    if (input.files && input.files.length) add(input, Array.prototype.slice.call(input.files), true);
  }

  function hasFiles(e) {
    var types = e.dataTransfer && e.dataTransfer.types;
    if (!types) return false;
    for (var i = 0; i < types.length; i++) if (types[i] === 'Files') return true;
    return false;
  }

  function field(el) { return el && el.matches && el.matches(READY) ? el : null; }

  /* The picker's choice arrives as the input's own change: read it into the list,
     unless it is the change this plugin just sent about its own list. */
  document.addEventListener('change', function (e) {
    var input = field(e.target);
    if (!input || state(input).echo) return;
    add(input, Array.prototype.slice.call(input.files || []), true);
  }, true);

  /* A file pasted while the field has focus: a screenshot, most often. */
  document.addEventListener('paste', function (e) {
    var input = field(document.activeElement);
    var files = e.clipboardData && e.clipboardData.files;
    if (!input || input.disabled || !files || !files.length) return;
    e.preventDefault();
    add(input, Array.prototype.slice.call(files), false);
  }, false);

  document.addEventListener('reset', function (e) {
    var form = e.target;
    setTimeout(function () {
      var inputs = form.querySelectorAll ? form.querySelectorAll(READY) : [];
      for (var i = 0; i < inputs.length; i++) {
        var st = state(inputs[i]);
        st.files.forEach(function (f) { forget(inputs[i], f); });
        st.files = [];
        writeBack(inputs[i]);
        render(inputs[i]);
        report(inputs[i], []);
      }
    }, 0);
  }, true);

  function scan(scope) {
    var found = scope.querySelectorAll ? scope.querySelectorAll(FIELD) : [];
    for (var i = 0; i < found.length; i++) build(found[i]);
    if (scope.matches && scope.matches(FIELD)) build(scope);
  }

  /* ------------------------------------------------------------- API */

  function resolve(target) { return field(typeof target === 'string' ? document.querySelector(target) : target); }

  /* Insiyab.file(field) → the files in the list; Insiyab.file(field, []) empties
     it, and a list of Files replaces it, within the limits, quietly. */
  function file(target, files) {
    var input = resolve(target);
    if (!input) return null;
    var st = state(input);
    if (files !== undefined) {
      st.files.forEach(function (f) { forget(input, f); });
      st.files = [];
      if (files && files.length) add(input, Array.prototype.slice.call(files), true);
      else { writeBack(input); render(input); report(input, []); }
    }
    return st.files.slice();
  }

  function update(f, status, extra) {
    var rec = records && records.get(f);
    if (!rec || state(rec.input).files.indexOf(f) === -1) return false;
    rec.status = status;
    for (var k in extra) if (extra.hasOwnProperty(k)) rec[k] = extra[k];
    paint(rec);
    check(rec.input);
    return true;
  }

  file.progress = function (f, fraction) { return update(f, 'sending', { fraction: fraction == null ? null : +fraction }); };
  file.fail = function (f, message) { return update(f, 'failed', { message: message || '' }); };
  file.done = function (f, value) {
    var rec = records && records.get(f);
    if (!rec) return false;
    var st = state(rec.input);
    if (st.name && value != null) {
      if (!rec.hidden) {
        rec.hidden = make('input');
        rec.hidden.type = 'hidden';
        rec.hidden.name = st.name;
        st.box.appendChild(rec.hidden);
      }
      rec.hidden.value = String(value);
    }
    return update(f, 'done', { value: value });
  };

  Insiyab.file = file;
  Insiyab.define('file', scan);
  if (document.readyState !== 'loading') scan(document);
})(window, document);
