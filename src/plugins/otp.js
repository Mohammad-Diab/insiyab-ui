/* ==========================================================================
   Insiyab · One-time code plugin
   ==========================================================================

   The verification-code field, one box per digit. Load it after insiyab.js, with
   its stylesheet:

     <link rel="stylesheet" href="plugins/insiyab-otp.css">
     <script src="insiyab.js"></script>
     <script src="plugins/insiyab-otp.js"></script>

     <input data-ins-otp="6" name="code" aria-label="رمز التحقّق" required>

   **It stays one input.** The boxes are drawn over a single real field, so the
   phone's "code from Messages" suggestion, a paste of the whole code, a password
   manager and a screen reader all meet one ordinary text field — which is what each
   of them is built for, and what six separate inputs would have broken one way or
   another. Without the script it is exactly that field.

     data-ins-otp="6"            how many characters (4 to 10; else maxlength, else 6)
     data-ins-otp-chars="alnum"  letters too, upper-cased; digits only by default
     data-ins-otp-submit         submit the form when the last one is entered

   Typing is always at the end — the next box — and Backspace takes the last one
   off. Arabic-Indic digits are read as Latin ones, and a paste keeps only the code:
   "رمزك هو ١٢٣٤٥٦" fills 123456. A complete code fires `ins:otp` with the value.
   After the server turns a code down, mark the field `aria-invalid="true"`: the
   boxes turn red, and the mark comes off by itself as the code is typed again.
   ========================================================================== */

(function (window, document) {
  'use strict';

  var Insiyab = window.Insiyab;
  if (!Insiyab || !Insiyab.define) {
    if (window.console) window.console.warn('[insiyab-otp] load insiyab.js before this plugin.');
    return;
  }

  var OTP = 'input[data-ins-otp]';
  var READY = 'input[data-ins-otp-ready]';

  var TEXT = {
    ar: { short: function (n, alnum) { return 'اكتب الرمز كاملًا: ' + n + (alnum ? ' خانات.' : ' أرقام.'); } },
    en: { short: function (n, alnum) { return 'Enter all ' + n + (alnum ? ' characters.' : ' digits.'); } }
  };

  function text(el) {
    var host = el.closest('[lang]') || document.documentElement;
    return (host.getAttribute('lang') || 'en').slice(0, 2).toLowerCase() === 'ar' ? TEXT.ar : TEXT.en;
  }

  function latin(s) {
    return String(s || '')
      .replace(/[٠-٩]/g, function (d) { return String(d.charCodeAt(0) - 0x0660); })
      .replace(/[۰-۹]/g, function (d) { return String(d.charCodeAt(0) - 0x06f0); });
  }

  function size(input) { return +input.getAttribute('data-ins-otp'); }
  function alnum(input) { return input.getAttribute('data-ins-otp-chars') === 'alnum'; }

  /* Only the code: whatever else came with a paste goes, and so does anything past
     the last box. */
  function clean(input, value) {
    var s = latin(value);
    s = alnum(input) ? s.toUpperCase().replace(/[^0-9A-Z]/g, '') : s.replace(/[^0-9]/g, '');
    return s.slice(0, size(input));
  }

  function cells(input) { return input.parentNode.querySelectorAll('.ins-otp-cell'); }

  function render(input) {
    var value = input.value, boxes = cells(input), n = boxes.length;
    var focused = document.activeElement === input && !input.disabled;
    var at = Math.min(value.length, n - 1);
    for (var i = 0; i < n; i++) {
      var ch = value.charAt(i);
      if (boxes[i].textContent !== ch) boxes[i].textContent = ch;
      boxes[i].classList.toggle('is-filled', !!ch);
      boxes[i].classList.toggle('is-active', focused && i === at);
    }
  }

  function check(input) {
    var n = size(input), len = input.value.length;
    input.setCustomValidity(len && len < n ? text(input).short(n, alnum(input)) : '');
  }

  function toEnd(input) {
    var end = input.value.length;
    try { if (input.selectionStart !== end || input.selectionEnd !== end) input.setSelectionRange(end, end); } catch (e) { /* not focusable yet */ }
  }

  function complete(input) {
    var detail = { input: input, value: input.value };
    var event;
    try { event = new CustomEvent('ins:otp', { detail: detail, bubbles: true }); }
    catch (e) { event = document.createEvent('CustomEvent'); event.initCustomEvent('ins:otp', true, false, detail); }
    input.dispatchEvent(event);
    var form = input.form;
    if (form && input.hasAttribute('data-ins-otp-submit')) {
      if (form.requestSubmit) form.requestSubmit();
      else form.submit();
    }
  }

  /* After every change: cleaned, redrawn, checked — and, the moment the last box
     fills, announced. A code already complete that is set again is not announced
     twice. */
  function sync(input, quiet) {
    var was = input.getAttribute('data-ins-otp-last') || '';
    var value = clean(input, input.value);
    if (value !== input.value) input.value = value;
    toEnd(input);
    check(input);
    render(input);
    input.setAttribute('data-ins-otp-last', value);
    if (!quiet && value.length === size(input) && value !== was) complete(input);
  }

  function build(input) {
    if (input.hasAttribute('data-ins-otp-ready')) return;
    var n = parseInt(input.getAttribute('data-ins-otp'), 10) || parseInt(input.getAttribute('maxlength'), 10) || 6;
    n = Math.max(4, Math.min(10, n));
    input.setAttribute('data-ins-otp', String(n));
    input.setAttribute('data-ins-otp-ready', '');
    /* No maxlength: the browser would cut a paste like "123 456" at six characters,
       before the space could be taken out, and lose the last digit. */
    input.removeAttribute('maxlength');
    input.classList.remove('ins-input');
    input.classList.add('ins-otp-input');
    input.type = 'text';
    input.dir = 'ltr';
    input.spellcheck = false;
    input.setAttribute('inputmode', alnum(input) ? 'text' : 'numeric');
    input.setAttribute('autocapitalize', alnum(input) ? 'characters' : 'off');
    if (!input.hasAttribute('autocomplete')) input.setAttribute('autocomplete', 'one-time-code');

    var box = input.parentNode;
    if (!box.classList.contains('ins-otp')) {
      box = document.createElement('div');
      box.className = 'ins-otp';
      input.parentNode.insertBefore(box, input);
      box.appendChild(input);
    }
    box.dir = 'ltr';
    box.style.setProperty('--ins-otp-n', String(n));
    var row = document.createElement('div');
    row.className = 'ins-otp-cells';
    row.setAttribute('aria-hidden', 'true');
    for (var i = 0; i < n; i++) {
      var cell = document.createElement('span');
      cell.className = 'ins-otp-cell';
      row.appendChild(cell);
    }
    box.appendChild(row);
    sync(input, true);
  }

  function ready(el) { return el && el.matches && el.matches(READY); }

  document.addEventListener('input', function (e) {
    if (!ready(e.target)) return;
    /* A code being typed again is no longer the code that was turned down. */
    if (e.target.getAttribute('aria-invalid') === 'true') e.target.removeAttribute('aria-invalid');
    sync(e.target, false);
  }, false);

  /* One place to type: the end. A click on any box, a selection, an arrow key —
     the caret goes back after the last character, where the next one belongs. */
  function settle(e) {
    if (!ready(e.target)) return;
    var input = e.target;
    setTimeout(function () { if (document.activeElement === input) toEnd(input); render(input); }, 0);
    render(input);
  }
  document.addEventListener('focusin', settle, false);
  document.addEventListener('focusout', settle, false);
  document.addEventListener('click', settle, false);
  document.addEventListener('select', settle, true);
  document.addEventListener('keydown', function (e) {
    if (!ready(e.target)) return;
    if (/^(ArrowLeft|ArrowRight|ArrowUp|ArrowDown|Home|End)$/.test(e.key) && !e.shiftKey) e.preventDefault();
  }, false);

  document.addEventListener('reset', function (e) {
    var form = e.target;
    setTimeout(function () {
      var fields = form.querySelectorAll ? form.querySelectorAll(READY) : [];
      for (var i = 0; i < fields.length; i++) sync(fields[i], true);
    }, 0);
  }, true);

  function scan(scope) {
    var found = scope.querySelectorAll ? scope.querySelectorAll(OTP) : [];
    for (var i = 0; i < found.length; i++) build(found[i]);
    if (scope.matches && scope.matches(OTP)) build(scope);
  }

  /* Insiyab.otp(field) reads the code; Insiyab.otp(field, '') clears it — after a
     code was turned down, say — and Insiyab.otp(field, '123456') sets it, quietly. */
  Insiyab.otp = function (target, value) {
    var input = typeof target === 'string' ? document.querySelector(target) : target;
    if (!input || !ready(input)) return null;
    if (value === undefined) return input.value;
    input.value = String(value);
    sync(input, true);
    return input.value;
  };

  Insiyab.define('otp', scan);
  if (document.readyState !== 'loading') scan(document);
})(window, document);
