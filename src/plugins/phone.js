/* ==========================================================================
   Insiyab · Phone number plugin
   ==========================================================================

   A phone field with its country. Load it after insiyab.js, with its stylesheet:

     <link rel="stylesheet" href="plugins/insiyab-phone.css">
     <script src="insiyab.js"></script>
     <script src="plugins/insiyab-phone.js"></script>

     <div class="ins-input-group">
       <input class="ins-input" type="tel" data-ins-phone name="mobile" value="+966501234567">
     </div>

   The field shows the number the way it is written at home — 050 123 4567 — and a
   button at its start picks the country, from a list that can be searched in
   Arabic, in English, by code or by dial code. **What the form sends is the E.164
   number**, +966501234567: a hidden input takes over the field's `name`, as the
   date field's does, so a server gets one unambiguous string whatever the person
   typed — a leading 0, spaces, Arabic-Indic digits, the country code in front.

   Every country is in the list with its dial code; the names are the browser's own
   (Intl.DisplayNames), in the page's language, so none are shipped. The Arab
   countries and a few others most often met on an Arab form also have their
   number lengths checked and are grouped as they are typed; any other country
   gets the international limit, 15 digits, and no grouping.

     data-ins-phone-country="AE"          the country to start in; otherwise the
                                          number's own, else the page's region
                                          (lang="ar-SA"), else Saudi Arabia
     data-ins-phone-preferred="SA,AE,KW"  listed first, above the rest
     data-ins-phone-only="SA,AE,KW,QA"    the only countries offered

   A number typed or pasted with + or 00 in front picks its own country. The field
   is invalid, with a message, while the number is the wrong length for its
   country. `ins:phone` goes out when the value changes, with the E.164 number, the
   country and whether it is valid.
   ========================================================================== */

(function (window, document) {
  'use strict';

  var Insiyab = window.Insiyab;
  if (!Insiyab || !Insiyab.norm) {
    if (window.console) window.console.warn('[insiyab-phone] load insiyab.js before this plugin.');
    return;
  }
  var norm = Insiyab.norm;

  /* ------------------------------------------------------------- the data */

  /* Every country and its dial code. Where several share one, `:` lists what the
     national number starts with for each of them — the area codes of the North
     American plan, Kazakhstan's 6 and 7 inside +7 — and the one without a list is
     the code's default. */
  var DATA =
    'AC247,AD376,AE971,AF93,AG1:268,AI1:264,AL355,AM374,AO244,AR54,AS1:684,AT43,AU61,AW297,AX358:18,AZ994,' +
    'BA387,BB1:246,BD880,BE32,BF226,BG359,BH973,BI257,BJ229,BL590,BM1:441,BN673,BO591,BQ599:3.4.7,BR55,BS1:242,' +
    'BT975,BW267,BY375,BZ501,CA1:204.226.236.249.250.257.263.289.306.343.354.365.367.368.382.403.416.418.428.' +
    '431.437.438.450.460.468.474.506.514.519.548.579.581.584.587.604.613.639.647.672.683.705.709.742.753.778.' +
    '780.782.807.819.825.867.873.879.902.905,CC61:89162,CD243,CF236,CG242,CH41,CI225,CK682,CL56,CM237,CN86,' +
    'CO57,CR506,CU53,CV238,CW599,CX61:89164,CY357,CZ420,DE49,DJ253,DK45,DM1:767,DO1:809.829.849,DZ213,EC593,' +
    'EE372,EG20,EH212:5288.5289,ER291,ES34,ET251,FI358,FJ679,FK500,FM691,FO298,FR33,GA241,GB44,GD1:473,GE995,' +
    'GF594,GG44:1481,GH233,GI350,GL299,GM220,GN224,GP590,GQ240,GR30,GT502,GU1:671,GW245,GY592,HK852,HN504,' +
    'HR385,HT509,HU36,ID62,IE353,IL972,IM44:1624,IN91,IO246,IQ964,IR98,IS354,IT39,JE44:1534,JM1:876.658,JO962,' +
    'JP81,KE254,KG996,KH855,KI686,KM269,KN1:869,KP850,KR82,KW965,KY1:345,KZ7:6.7,LA856,LB961,LC1:758,LI423,' +
    'LK94,LR231,LS266,LT370,LU352,LV371,LY218,MA212,MC377,MD373,ME382,MF590,MG261,MH692,MK389,ML223,MM95,MN976,' +
    'MO853,MP1:670,MQ596,MR222,MS1:664,MT356,MU230,MV960,MW265,MX52,MY60,MZ258,NA264,NC687,NE227,NF672,NG234,' +
    'NI505,NL31,NO47,NP977,NR674,NU683,NZ64,OM968,PA507,PE51,PF689,PG675,PH63,PK92,PL48,PM508,PR1:787.939,PS970,' +
    'PT351,PW680,PY595,QA974,RE262,RO40,RS381,RU7,RW250,SA966,SB677,SC248,SD249,SE46,SG65,SH290,SI386,SJ47:79,' +
    'SK421,SL232,SM378,SN221,SO252,SR597,SS211,ST239,SV503,SX1:721,SY963,SZ268,TC1:649,TD235,TG228,TH66,TJ992,' +
    'TK690,TL670,TM993,TN216,TO676,TR90,TT1:868,TV688,TW886,TZ255,UA380,UG256,US1,UY598,UZ998,VA39:06698,' +
    'VC1:784,VE58,VG1:284,VI1:340,VN84,VU678,WF681,WS685,XK383,YE967,YT262:269.639,ZA27,ZM260,ZW263';

  /* Which country a shared code means when nothing narrows it. */
  var PRIMARY = { 1: 'US', 7: 'RU', 39: 'IT', 44: 'GB', 47: 'NO', 61: 'AU', 212: 'MA', 262: 'RE', 290: 'SH', 358: 'FI', 590: 'GP', 599: 'CW' };

  /* The countries whose numbers are checked and grouped: the lengths a national
     number (the one after the dial code, without the trunk 0) can have, the trunk
     prefix dialled at home, the grouping for each length — X is a digit, anything
     else is written as it is, so the leading 0 is part of how the number looks —
     and an example for the placeholder. */
  var RULES = {
    SA: { lens: [9], trunk: '0', fmt: { 9: '0XX XXX XXXX' }, ex: '501234567' },
    AE: { lens: [8, 9], trunk: '0', fmt: { 8: '0X XXX XXXX', 9: '0XX XXX XXXX' }, ex: '501234567' },
    KW: { lens: [8], trunk: '', fmt: { 8: 'XXXX XXXX' }, ex: '50012345' },
    QA: { lens: [8], trunk: '', fmt: { 8: 'XXXX XXXX' }, ex: '33123456' },
    BH: { lens: [8], trunk: '', fmt: { 8: 'XXXX XXXX' }, ex: '36001234' },
    OM: { lens: [8], trunk: '', fmt: { 8: 'XXXX XXXX' }, ex: '92123456' },
    JO: { lens: [8, 9], trunk: '0', fmt: { 8: '0X XXX XXXX', 9: '0XX XXX XXXX' }, ex: '790123456' },
    LB: { lens: [7, 8], trunk: '0', fmt: { 7: '0X XXX XXX', 8: 'XX XXX XXX' }, ex: '71123456' },
    SY: { lens: [8, 9], trunk: '0', fmt: { 8: '0XX XXX XXX', 9: '0XXX XXX XXX' }, ex: '944567890' },
    IQ: { lens: [8, 9, 10], trunk: '0', fmt: { 8: '0X XXX XXXX', 9: '0XX XXX XXXX', 10: '0XXX XXX XXXX' }, ex: '7912345678' },
    PS: { lens: [8, 9], trunk: '0', fmt: { 8: '0X XXX XXXX', 9: '0XX XXX XXXX' }, ex: '599123456' },
    YE: { lens: [7, 8, 9], trunk: '0', fmt: { 7: '0X XXX XXX', 8: '0X XXX XXXX', 9: 'XXX XXX XXX' }, ex: '712345678' },
    EG: { lens: [8, 9, 10], trunk: '0', fmt: { 8: '0XX XXX XXX', 9: '0XX XXX XXXX', 10: '0XXX XXX XXXX' }, ex: '1001234567' },
    LY: { lens: [8, 9], trunk: '0', fmt: { 8: '0XX XXX XXX', 9: '0XX XXX XXXX' }, ex: '912345678' },
    SD: { lens: [9], trunk: '0', fmt: { 9: '0XX XXX XXXX' }, ex: '911231234' },
    SS: { lens: [9], trunk: '0', fmt: { 9: '0XX XXX XXXX' }, ex: '977123456' },
    TN: { lens: [8], trunk: '', fmt: { 8: 'XX XXX XXX' }, ex: '20123456' },
    DZ: { lens: [8, 9], trunk: '0', fmt: { 8: '0XX XX XX XX', 9: '0XXX XX XX XX' }, ex: '551234567' },
    MA: { lens: [9], trunk: '0', fmt: { 9: '0X XX XX XX XX' }, ex: '612345678' },
    MR: { lens: [8], trunk: '', fmt: { 8: 'XX XX XX XX' }, ex: '22123456' },
    SO: { lens: [7, 8, 9], trunk: '0', fmt: { 7: 'X XXX XXX', 8: 'X XXX XXXX', 9: 'XX XXX XXXX' }, ex: '612345678' },
    DJ: { lens: [8], trunk: '', fmt: { 8: 'XX XX XX XX' }, ex: '77831001' },
    KM: { lens: [7], trunk: '', fmt: { 7: 'XXX XX XX' }, ex: '3212345' },
    US: { lens: [10], trunk: '1', fmt: { 10: '(XXX) XXX-XXXX' }, ex: '2015550123' },
    GB: { lens: [9, 10], trunk: '0', fmt: { 9: '0XXXX XXXXX', 10: '0XXXX XXXXXX' }, ex: '7400123456' },
    FR: { lens: [9], trunk: '0', fmt: { 9: '0X XX XX XX XX' }, ex: '612345678' },
    DE: { lens: [6, 7, 8, 9, 10, 11, 12, 13], trunk: '0', fmt: null, ex: '15123456789' },
    TR: { lens: [10], trunk: '0', fmt: { 10: '0XXX XXX XX XX' }, ex: '5012345678' },
    IN: { lens: [10], trunk: '0', fmt: { 10: 'XXXXX XXXXX' }, ex: '8123456789' },
    PK: { lens: [9, 10], trunk: '0', fmt: { 9: '0XX XXXXXXX', 10: '0XXX XXXXXXX' }, ex: '3012345678' }
  };

  /* A leading 0 is dropped as the trunk prefix almost everywhere, even without a
     rule — except where it is part of the number itself. */
  var KEEP_ZERO = { IT: 1, SM: 1, VA: 1 };

  var COUNTRIES = [], BY_ISO = {}, BY_DIAL = {};
  DATA.split(',').forEach(function (entry) {
    var m = /^([A-Z]{2})(\d+)(?::([\d.]+))?$/.exec(entry);
    if (!m) return;
    var c = { iso: m[1], dial: m[2], lead: m[3] ? m[3].split('.') : null };
    COUNTRIES.push(c);
    BY_ISO[c.iso] = c;
    (BY_DIAL[c.dial] = BY_DIAL[c.dial] || []).push(c);
  });

  function ruleOf(c) { return RULES[c.iso] || (c.dial === '1' ? RULES.US : null); }

  var TEXT = {
    ar: {
      country: 'الدولة', search: 'ابحث عن دولة أو رمز', none: 'لا دولة بهذا الاسم',
      bad: 'اكتب رقم هاتف صحيحًا في {country}.', incomplete: 'اكتب رمز الدولة كاملًا بعد +.',
      elsewhere: 'لا تُقبل هنا أرقام {country}.'
    },
    en: {
      country: 'Country', search: 'Search for a country or code', none: 'No country by that name',
      bad: 'Enter a valid phone number for {country}.', incomplete: 'Enter the full country code after +.',
      elsewhere: 'Numbers from {country} are not accepted here.'
    }
  };

  function lang(el) {
    var own = el.getAttribute('data-ins-locale');
    if (own) return own;
    var host = el.closest('[lang]') || document.documentElement;
    return host.getAttribute('lang') || 'en';
  }
  function text(el) { return lang(el).slice(0, 2).toLowerCase() === 'ar' ? TEXT.ar : TEXT.en; }

  var namers = {};
  function nameOf(iso, locale) {
    var key = locale || 'en';
    if (!namers.hasOwnProperty(key)) {
      try { namers[key] = new Intl.DisplayNames([key], { type: 'region' }); } catch (e) { namers[key] = null; }
    }
    var n = null;
    try { n = namers[key] && namers[key].of(iso); } catch (e) { n = null; }
    return n || iso;
  }

  function latin(s) {
    return String(s || '')
      .replace(/[٠-٩]/g, function (d) { return String(d.charCodeAt(0) - 0x0660); })
      .replace(/[۰-۹]/g, function (d) { return String(d.charCodeAt(0) - 0x06f0); });
  }

  /* ------------------------------------------------------------- the number */

  /* The country an international number belongs to. Dial codes are prefix-free,
     so the first one that matches is the one; a shared code is then narrowed by
     what the national number starts with. */
  function detect(digits) {
    for (var len = 1; len <= 3; len++) {
      var list = BY_DIAL[digits.slice(0, len)];
      if (!list) continue;
      var rest = digits.slice(len), pick = null;
      for (var i = 0; i < list.length && !pick; i++) {
        var lead = list[i].lead;
        for (var j = 0; lead && j < lead.length; j++) if (rest.indexOf(lead[j]) === 0) { pick = list[i]; break; }
      }
      return { country: pick || BY_ISO[PRIMARY[list[0].dial]] || list[0], nsn: rest };
    }
    return null;
  }

  function maxLen(c) { var r = ruleOf(c); return r ? r.lens[r.lens.length - 1] : 15 - c.dial.length; }

  /* What was typed, read as a number in a country: the national number, the
     country (another one, if it began with + or 00), and how many leading digits
     were not part of it — the trunk 0, a dial code typed without the + — which is
     what keeps the caret in place when the field is rewritten. */
  function read(value, country) {
    var t = latin(value).trim();
    var digits = t.replace(/[^0-9]/g, '');
    if (/^(\+|00)/.test(t)) {
      if (t.charAt(0) !== '+') digits = digits.slice(2);
      var d = detect(digits);
      if (!d) return { country: country, nsn: '', intl: true, partial: !!digits || t === '+' };
      return { country: d.country, nsn: d.nsn.slice(0, 15 - d.country.dial.length), intl: true };
    }
    var r = ruleOf(country), cut = 0;
    if (r && r.lens.indexOf(digits.length) === -1 && digits.indexOf(country.dial) === 0 &&
        r.lens.indexOf(digits.length - country.dial.length) !== -1) {
      cut = country.dial.length;
    } else if (r ? (r.trunk && digits.indexOf(r.trunk) === 0 && (r.trunk === '0' || digits.length > maxLen(country)))
                 : (digits.charAt(0) === '0' && !KEEP_ZERO[country.iso])) {
      cut = r ? r.trunk.length : 1;
    }
    return { country: country, nsn: digits.slice(cut).slice(0, maxLen(country)), cut: cut, intl: false, typed: digits };
  }

  /* The pattern for a number of this many digits so far: its own length's, or the
     next one up while it is still being typed. */
  function pattern(c, n) {
    var r = ruleOf(c);
    if (!r || !r.fmt) return null;
    var best = null;
    for (var i = 0; i < r.lens.length; i++) {
      if (!r.fmt[r.lens[i]]) continue;
      best = r.fmt[r.lens[i]];
      if (r.lens[i] >= n) break;
    }
    return best;
  }

  /* The national number written out, and where each of its digits ends up — so the
     caret can be put back after the same digit it was after. */
  function format(nsn, c) {
    var p = pattern(c, nsn.length), r = ruleOf(c);
    var out = '', at = [];
    if (!p) {
      out = r && r.trunk === '0' && nsn ? '0' : '';
      for (var k = 0; k < nsn.length; k++) { out += nsn.charAt(k); at.push(out.length); }
      return { text: out, at: at };
    }
    var di = 0;
    for (var i = 0; i < p.length && di < nsn.length; i++) {
      if (p.charAt(i) === 'X') { out += nsn.charAt(di++); at.push(out.length); }
      else out += p.charAt(i);
    }
    for (; di < nsn.length; di++) { out += nsn.charAt(di); at.push(out.length); }
    return { text: out, at: at };
  }

  function valid(nsn, c) {
    var r = ruleOf(c);
    return r ? r.lens.indexOf(nsn.length) !== -1 : nsn.length >= 4 && nsn.length <= 15 - c.dial.length;
  }

  /* ------------------------------------------------------------- the field */

  function state(input) { return input._insPhone; }

  function allowed(input) {
    var only = (input.getAttribute('data-ins-phone-only') || '').toUpperCase().split(/[\s,]+/).filter(function (x) { return BY_ISO[x]; });
    return only.length ? only.map(function (x) { return BY_ISO[x]; }) : COUNTRIES;
  }

  function isAllowed(input, c) { return allowed(input).indexOf(c) !== -1; }

  function region(input) {
    var parts = lang(input).split(/[-_]/);
    for (var i = 1; i < parts.length; i++) if (/^[A-Za-z]{2}$/.test(parts[i])) return parts[i].toUpperCase();
    return null;
  }

  function startCountry(input, value) {
    var list = allowed(input), c = BY_ISO[(input.getAttribute('data-ins-phone-country') || '').toUpperCase()];
    if (!c) { var d = read(value, list[0]); if (d.intl && !d.partial) c = d.country; }
    if (!c) c = BY_ISO[region(input)];
    if (!c || list.indexOf(c) === -1) c = list.indexOf(BY_ISO.SA) !== -1 ? BY_ISO.SA : list[0];
    return c;
  }

  function e164(st) { return st.nsn ? '+' + st.country.dial + st.nsn : ''; }

  function check(input) {
    var st = state(input), t = text(input), msg = '';
    if (st.elsewhere) msg = t.elsewhere.replace('{country}', nameOf(st.elsewhere.iso, lang(input)));
    else if (st.partial) msg = t.incomplete;
    else if (st.nsn && !valid(st.nsn, st.country)) msg = t.bad.replace('{country}', nameOf(st.country.iso, lang(input)));
    input.setCustomValidity(msg);
    return !msg;
  }

  function paintButton(input) {
    var st = state(input), btn = st.button, c = st.country;
    btn.querySelector('.ins-phone-iso').textContent = c.iso;
    btn.querySelector('.ins-phone-dial').textContent = '+' + c.dial;
    btn.setAttribute('aria-label', text(input).country + ': ' + nameOf(c.iso, lang(input)) + ' +' + c.dial);
    if (st.autoPlaceholder) {
      var r = ruleOf(c);
      if (r && r.ex) input.placeholder = format(r.ex, c).text;
      else input.removeAttribute('placeholder');
    }
  }

  function setCountry(input, c) {
    var st = state(input);
    if (st.country === c) return;
    st.country = c;
    paintButton(input);
  }

  /* After the national number changed: the hidden value, the check, and the
     announcement when the value is not what it was. */
  function commit(input, quiet) {
    var st = state(input), value = e164(st);
    st.hidden.value = value;
    var ok = check(input);
    if (!quiet && value !== st.last) {
      st.last = value;
      var detail = { input: input, value: value, country: st.country.iso, valid: ok && !!value };
      var event;
      try { event = new CustomEvent('ins:phone', { detail: detail, bubbles: true }); }
      catch (e) { event = document.createEvent('CustomEvent'); event.initCustomEvent('ins:phone', true, false, detail); }
      input.dispatchEvent(event);
    }
    if (quiet) st.last = value;
  }

  /* `at`: how many digits of the number the caret was after, or null to leave the
     caret alone. */
  function show(input, at) {
    var st = state(input), f = format(st.nsn, st.country);
    if (input.value !== f.text) input.value = f.text;
    if (at != null && document.activeElement === input) {
      var pos = at <= 0 ? (f.at.length ? f.at[0] - 1 : f.text.length) : f.at[Math.min(at, f.at.length) - 1];
      try { input.setSelectionRange(pos, pos); } catch (e) { /* not a text field */ }
    }
  }

  /* As the person types. A number being written in its own country is regrouped on
     every key, with the caret kept after the digit it was after; one being written
     with + is left as typed until the field is left, since its country is still
     being decided, and the button follows it as soon as it is. */
  function onType(input, e) {
    var st = state(input), raw = input.value;
    var caret = input.selectionStart == null ? raw.length : input.selectionStart;
    var before = latin(raw.slice(0, caret)).replace(/[^0-9]/g, '').length;
    var res = read(raw, st.country);
    st.partial = !!res.partial;
    st.elsewhere = null;
    if (res.intl) {
      if (res.partial || isAllowed(input, res.country)) {
        if (!res.partial) setCountry(input, res.country);
        st.nsn = res.nsn;
      } else {
        st.elsewhere = res.country;
        st.nsn = '';
      }
      commit(input, false);
      return;
    }
    var nsn = res.nsn, at = Math.max(0, before - res.cut);
    /* A Backspace that took out a space or a bracket took nothing out of the number;
       it meant the digit before it. */
    var type = e && e.inputType;
    if (nsn === st.nsn && type === 'deleteContentBackward' && at > 0) { nsn = nsn.slice(0, at - 1) + nsn.slice(at); at--; }
    else if (nsn === st.nsn && type === 'deleteContentForward' && at < nsn.length) nsn = nsn.slice(0, at) + nsn.slice(at + 1);
    st.nsn = nsn;
    /* A lone trunk 0 stays on screen: it is the start of a number, not nothing. */
    if (!nsn && res.typed) { st.hidden.value = ''; input.setCustomValidity(''); return; }
    show(input, at);
    commit(input, false);
  }

  function onLeave(input) {
    var st = state(input);
    var res = read(input.value, st.country);
    st.partial = !!res.partial;
    st.elsewhere = null;
    if (res.intl && !res.partial) {
      if (isAllowed(input, res.country)) { setCountry(input, res.country); st.nsn = res.nsn; }
      else { st.elsewhere = res.country; st.nsn = ''; }
    } else if (!res.intl) st.nsn = res.nsn;
    if (!st.partial && !st.elsewhere) show(input, null);
    commit(input, false);
  }

  function build(input) {
    if (input.hasAttribute('data-ins-phone-ready')) return;
    input.setAttribute('data-ins-phone-ready', '');
    var initial = input.value || input.getAttribute('value') || '';
    var group = input.closest('.ins-input-group');
    if (!group) {
      group = document.createElement('div');
      group.className = 'ins-input-group';
      input.parentNode.insertBefore(group, input);
      group.appendChild(input);
    }
    group.classList.add('ins-phone');
    /* A number reads left to right in any language, with its code in front. */
    group.dir = 'ltr';

    var hidden = document.createElement('input');
    hidden.type = 'hidden';
    if (input.name) { hidden.name = input.name; input.removeAttribute('name'); }
    group.parentNode.insertBefore(hidden, group.nextSibling);

    input.type = 'tel';
    input.setAttribute('inputmode', 'tel');
    if (!input.hasAttribute('autocomplete')) input.setAttribute('autocomplete', 'tel');

    var btn = document.createElement('button');
    btn.type = 'button';
    btn.className = 'ins-phone-cc';
    btn.setAttribute('aria-haspopup', 'listbox');
    btn.setAttribute('aria-expanded', 'false');
    var iso = document.createElement('span');
    iso.className = 'ins-phone-iso';
    var dial = document.createElement('span');
    dial.className = 'ins-phone-dial';
    btn.appendChild(iso);
    btn.appendChild(dial);
    if (input.disabled) btn.disabled = true;
    group.insertBefore(btn, group.firstChild);

    var country = startCountry(input, initial);
    input._insPhone = {
      country: country, nsn: '', hidden: hidden, button: btn, last: '', partial: false, elsewhere: null,
      autoPlaceholder: !input.hasAttribute('placeholder'), start: country
    };
    paintButton(input);
    load(input, initial, country);
    /* What a form reset returns to, kept here: a hidden input has no default value of
       its own — setting its value rewrites its value attribute — so the one it
       started with would be gone after the first key. */
    input._insPhone.initial = e164(input._insPhone);
    input.setAttribute('value', input.value);
  }

  /* A value given to the field — in its markup, from Insiyab.phone(), by a reset. */
  function load(input, value, fallback) {
    var st = state(input);
    var res = read(value || '', fallback || st.country);
    var ok = !res.intl || (!res.partial && isAllowed(input, res.country));
    if (res.intl && ok) setCountry(input, res.country);
    else if (fallback) setCountry(input, fallback);
    st.nsn = ok ? res.nsn : '';
    st.partial = false;
    st.elsewhere = null;
    input.value = format(st.nsn, st.country).text;
    commit(input, true);
  }

  /* ------------------------------------------------------------- the picker */

  var pop = null, popFor = null;
  var hasPopover = typeof HTMLElement !== 'undefined' && HTMLElement.prototype.hasOwnProperty('popover');

  function popLayer() {
    if (pop) return pop;
    pop = document.createElement('div');
    pop.className = 'ins-phone-pop';
    pop.setAttribute('role', 'dialog');
    if (hasPopover) pop.setAttribute('popover', 'manual');
    else pop.hidden = true;
    var find = document.createElement('input');
    find.className = 'ins-input ins-input--sm ins-phone-find';
    find.type = 'text';
    find.autocomplete = 'off';
    find.spellcheck = false;
    find.setAttribute('role', 'combobox');
    find.setAttribute('aria-autocomplete', 'list');
    find.setAttribute('aria-expanded', 'true');
    var list = document.createElement('ul');
    list.className = 'ins-phone-list';
    list.id = 'ins-phone-list';
    list.setAttribute('role', 'listbox');
    find.setAttribute('aria-controls', list.id);
    var none = document.createElement('div');
    none.className = 'ins-phone-none';
    none.hidden = true;
    pop.appendChild(find);
    pop.appendChild(list);
    pop.appendChild(none);
    find.addEventListener('input', filter);
    find.addEventListener('keydown', onPopKey);
    list.addEventListener('mousedown', function (e) { e.preventDefault(); });
    list.addEventListener('click', function (e) {
      var opt = e.target.closest && e.target.closest('.ins-phone-opt');
      if (opt) choose(opt);
    });
    list.addEventListener('pointermove', function (e) {
      var opt = e.target.closest && e.target.closest('.ins-phone-opt');
      if (opt && !opt.classList.contains('is-active')) activate(opt, false);
    });
    document.body.appendChild(pop);
    return pop;
  }

  function option(c, locale, selected) {
    var li = document.createElement('li');
    li.className = 'ins-phone-opt';
    li.id = 'ins-phone-opt-' + c.iso;
    li.setAttribute('role', 'option');
    li.setAttribute('aria-selected', String(selected));
    li.setAttribute('data-iso', c.iso);
    var name = nameOf(c.iso, locale);
    li.setAttribute('data-find', norm(name + ' ' + nameOf(c.iso, 'en') + ' ' + c.iso));
    li.setAttribute('data-dial', c.dial);
    var iso = document.createElement('span');
    iso.className = 'ins-phone-iso';
    iso.textContent = c.iso;
    var n = document.createElement('span');
    n.className = 'ins-phone-opt-name';
    n.textContent = name;
    var d = document.createElement('span');
    d.className = 'ins-phone-dial';
    d.dir = 'ltr';
    d.textContent = '+' + c.dial;
    li.appendChild(iso);
    li.appendChild(n);
    li.appendChild(d);
    return li;
  }

  function openPop(input) {
    if (popFor && popFor !== input) closePop(false);
    var st = state(input), layer = popLayer(), t = text(input), locale = lang(input);
    var find = layer.querySelector('.ins-phone-find'), list = layer.querySelector('.ins-phone-list');
    popFor = input;
    layer.dir = ((st.button.parentNode.parentNode && st.button.parentNode.parentNode.closest('[dir]')) || document.documentElement).getAttribute('dir') || 'ltr';
    layer.setAttribute('lang', locale);
    layer.setAttribute('aria-label', t.country);
    find.placeholder = t.search;
    find.setAttribute('aria-label', t.search);
    layer.querySelector('.ins-phone-none').textContent = t.none;

    var countries = allowed(input).slice();
    var collator = window.Intl && Intl.Collator ? new Intl.Collator(locale) : null;
    countries.sort(function (a, b) {
      var x = nameOf(a.iso, locale), y = nameOf(b.iso, locale);
      return collator ? collator.compare(x, y) : (x < y ? -1 : x > y ? 1 : 0);
    });
    var preferred = (input.getAttribute('data-ins-phone-preferred') || '').toUpperCase().split(/[\s,]+/)
      .map(function (x) { return BY_ISO[x]; })
      .filter(function (c) { return c && countries.indexOf(c) !== -1; });
    list.textContent = '';
    for (var i = 0; i < preferred.length; i++) list.appendChild(option(preferred[i], locale, preferred[i] === st.country));
    if (preferred.length) {
      var sep = document.createElement('li');
      sep.className = 'ins-phone-sep';
      sep.setAttribute('role', 'separator');
      list.appendChild(sep);
    }
    for (var j = 0; j < countries.length; j++) {
      if (preferred.indexOf(countries[j]) !== -1) continue;
      list.appendChild(option(countries[j], locale, countries[j] === st.country));
    }
    find.value = '';
    filter();
    if (hasPopover) { try { layer.showPopover(); } catch (e) { /* not connected */ } }
    else { layer.hidden = false; layer.classList.add('is-open'); }
    place();
    st.button.setAttribute('aria-expanded', 'true');
    activate(list.querySelector('.ins-phone-opt[aria-selected="true"]') || list.querySelector('.ins-phone-opt'), true);
    find.focus();
  }

  function closePop(refocus) {
    if (!pop || !popFor) return;
    var input = popFor;
    popFor = null;
    if (hasPopover) { try { pop.hidePopover(); } catch (e) { /* already hidden */ } }
    else { pop.classList.remove('is-open'); pop.hidden = true; }
    state(input).button.setAttribute('aria-expanded', 'false');
    if (refocus) state(input).button.focus();
  }

  function place() {
    if (!pop || !popFor) return;
    var anchor = state(popFor).button.parentNode.getBoundingClientRect();
    var gap = 6, pad = 8, vw = document.documentElement.clientWidth, vh = window.innerHeight;
    pop.style.left = '0px';
    pop.style.top = '0px';
    var w = pop.offsetWidth, h = pop.offsetHeight;
    var y = anchor.bottom + gap;
    if (y + h > vh - pad && anchor.top - gap - h >= pad) y = anchor.top - gap - h;
    var x = Math.max(pad, Math.min(anchor.left, vw - w - pad));
    pop.style.left = Math.round(x) + 'px';
    pop.style.top = Math.round(Math.max(pad, Math.min(y, vh - h - pad))) + 'px';
  }

  function options(visibleOnly) {
    var all = pop.querySelectorAll('.ins-phone-opt'), out = [];
    for (var i = 0; i < all.length; i++) if (!visibleOnly || !all[i].hidden) out.push(all[i]);
    return out;
  }

  function activate(opt, scroll) {
    var find = pop.querySelector('.ins-phone-find'), prev = pop.querySelector('.ins-phone-opt.is-active');
    if (prev) prev.classList.remove('is-active');
    if (!opt) { find.removeAttribute('aria-activedescendant'); return; }
    opt.classList.add('is-active');
    find.setAttribute('aria-activedescendant', opt.id);
    if (scroll && opt.scrollIntoView) opt.scrollIntoView({ block: 'nearest' });
  }

  /* By name in the page's language or in English, by the two-letter code, or by the
     dial code, with or without its +. */
  function filter() {
    var raw = latin(pop.querySelector('.ins-phone-find').value).trim();
    var q = norm(raw), digits = /^\+?\d+$/.test(raw) ? raw.replace('+', '') : '';
    var all = options(false), first = null, any = false;
    for (var i = 0; i < all.length; i++) {
      var hit = !q || (digits ? all[i].getAttribute('data-dial').indexOf(digits) === 0 : all[i].getAttribute('data-find').indexOf(q) !== -1);
      all[i].hidden = !hit;
      if (hit) { any = true; if (!first) first = all[i]; }
    }
    var seps = pop.querySelectorAll('.ins-phone-sep');
    for (var j = 0; j < seps.length; j++) seps[j].hidden = !!q;
    pop.querySelector('.ins-phone-none').hidden = any;
    if (q) activate(first, true);
  }

  function onPopKey(e) {
    var opts = options(true), at = opts.indexOf(pop.querySelector('.ins-phone-opt.is-active'));
    if (e.key === 'ArrowDown' || e.key === 'ArrowUp') {
      e.preventDefault();
      if (!opts.length) return;
      activate(opts[e.key === 'ArrowDown' ? Math.min(at + 1, opts.length - 1) : Math.max(at - 1, 0)], true);
    } else if (e.key === 'Enter') {
      e.preventDefault();
      if (at !== -1) choose(opts[at]);
    } else if (e.key === 'Escape') {
      e.preventDefault();
      e.stopPropagation();
      closePop(true);
    } else if (e.key === 'Tab') {
      closePop(false);
    }
  }

  function choose(opt) {
    var input = popFor, c = BY_ISO[opt.getAttribute('data-iso')];
    closePop(false);
    if (!input || !c) return;
    setCountry(input, c);
    show(input, null);
    commit(input, false);
    input.dispatchEvent(new Event('change', { bubbles: true }));
    input.focus();
  }

  /* ------------------------------------------------------------- wiring */

  function field(el) { return el && el.matches && el.matches('input[data-ins-phone-ready]') ? el : null; }

  document.addEventListener('input', function (e) { var f = field(e.target); if (f) onType(f, e); }, false);
  document.addEventListener('focusout', function (e) { var f = field(e.target); if (f) onLeave(f); }, false);
  document.addEventListener('keydown', function (e) {
    var f = field(e.target);
    if (f && e.key === 'Enter') onLeave(f);
    var btn = e.target.closest && e.target.closest('.ins-phone-cc');
    if (btn && (e.key === 'ArrowDown' || e.key === 'ArrowUp')) {
      e.preventDefault();
      var input = btn.parentNode.querySelector('input[data-ins-phone-ready]');
      if (input) openPop(input);
    }
  }, false);
  document.addEventListener('click', function (e) {
    var btn = e.target.closest && e.target.closest('.ins-phone-cc');
    if (!btn) return;
    var input = btn.parentNode.querySelector('input[data-ins-phone-ready]');
    if (!input) return;
    if (popFor === input) closePop(false);
    else openPop(input);
  }, false);
  /* A press anywhere else closes the list. */
  document.addEventListener('pointerdown', function (e) {
    if (!popFor || !pop) return;
    var t = e.target;
    if (pop.contains(t) || state(popFor).button.contains(t)) return;
    closePop(false);
  }, true);
  document.addEventListener('focusout', function (e) {
    if (popFor && pop && pop.contains(e.target) && e.relatedTarget && !pop.contains(e.relatedTarget)) closePop(false);
  }, false);
  window.addEventListener('scroll', function () { if (popFor) place(); }, true);
  window.addEventListener('resize', function () { if (popFor) place(); }, false);

  document.addEventListener('reset', function (e) {
    var form = e.target;
    setTimeout(function () {
      var fields = form.querySelectorAll ? form.querySelectorAll('input[data-ins-phone-ready]') : [];
      for (var i = 0; i < fields.length; i++) {
        var st = state(fields[i]);
        load(fields[i], st.initial, st.start);
      }
    }, 0);
  }, true);

  function scan(scope) {
    var found = scope.querySelectorAll ? scope.querySelectorAll('input[data-ins-phone]') : [];
    for (var i = 0; i < found.length; i++) build(found[i]);
    if (scope.matches && scope.matches('input[data-ins-phone]')) build(scope);
  }

  /* Insiyab.phone(field) → { value, country, valid }; Insiyab.phone(field, number)
     sets it — E.164, or national in the field's country — quietly. */
  Insiyab.phone = function (target, value) {
    var input = typeof target === 'string' ? document.querySelector(target) : target;
    if (!input || !field(input)) return null;
    var st = state(input);
    if (value !== undefined) load(input, value == null ? '' : String(value), null);
    return { value: e164(st), country: st.country.iso, valid: !!st.nsn && valid(st.nsn, st.country) };
  };

  Insiyab.define('phone', scan);
  if (document.readyState !== 'loading') scan(document);
})(window, document);
