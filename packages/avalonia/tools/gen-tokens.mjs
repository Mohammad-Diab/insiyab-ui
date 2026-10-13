// Generates Generated/Tokens.axaml and Generated/BrandTokens.g.cs from the web tokens. Usage: node gen-tokens.mjs [--check]
import fs from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';

const HERE = path.dirname(fileURLToPath(import.meta.url));
const REPO = path.resolve(HERE, '../../..');
const LIGHT_CSS = path.join(REPO, 'src/css/02-tokens.css');
const DARK_CSS = path.join(REPO, 'src/css/03-tokens-dark.css');
const GEN_DIR = path.resolve(HERE, '../src/Insiyab.Avalonia/Generated');
const OUT_AXAML = path.join(GEN_DIR, 'Tokens.axaml');
const OUT_CS = path.join(GEN_DIR, 'BrandTokens.g.cs');

// The brand inputs: each maps to a BrandSet field; hex ones are colours, rgb ones are triplets used inside rgba().
const BRAND_BASE = {
  '--ins-primary': { field: 'Primary', triplet: false },
  '--ins-primary-dark': { field: 'PrimaryDark', triplet: false },
  '--ins-primary-darker': { field: 'PrimaryDarker', triplet: false },
  '--ins-primary-rgb': { field: 'PrimaryRgb', triplet: true },
  '--ins-grad-rgb': { field: 'GradRgb', triplet: true },
  '--ins-accent-rgb': { field: 'AccentRgb', triplet: true },
  '--ins-on-primary': { field: 'OnPrimary', triplet: false },
  '--ins-wash-1': { field: 'Wash1', triplet: false },
  '--ins-wash-2': { field: 'Wash2', triplet: false },
};

const FONT_WHY = 'font stack: a FontFamily, owned by Themes/Fonts.axaml';
const SKIP = {
  '--ins-blur': 'backdrop-filter value: Avalonia has no per-element backdrop blur',
  '--ins-blur-bar': 'backdrop-filter value: Avalonia has no per-element backdrop blur',
  '--ins-sweep-x': 'origin of the web theme-change sweep, set at runtime by insiyab.js',
  '--ins-sweep-y': 'origin of the web theme-change sweep, set at runtime by insiyab.js',
  '--ins-sweep-r': 'reach of the web theme-change sweep, in vmax: no viewport unit in Avalonia',
  '--ins-measure': 'paragraph measure in ch: Avalonia has no ch unit',
  '--ins-font-arabic': FONT_WHY,
  '--ins-font-body-en': FONT_WHY,
  '--ins-font-heading-en': FONT_WHY,
  '--ins-font-mono': FONT_WHY,
  '--ins-font-body': FONT_WHY + ', including its html[dir="ltr"] swap',
};

// Values Avalonia cannot compute, pinned; `css` must still match the source or the pin is reviewed again.
const FIXED = {
  '--ins-fs-display': {
    css: 'clamp(2rem, 1.25rem + 3vw, 3rem)',
    value: '3rem',
    why: '48px, the clamp cap: the web reaches it at any viewport from 934px wide, which every desktop window is',
  },
};

// Non-token properties in the token blocks, each handled on purpose.
const PROPERTIES = {
  'color-scheme': 'native chrome follows RequestedThemeVariant in Avalonia',
};

const errors = [];
const fail = (msg) => errors.push(msg);

function stripComments(text, file) {
  const out = text.replace(/\/\*[\s\S]*?\*\//g, ' ');
  if (out.includes('/*')) fail(`${file}: unterminated comment`);
  return out;
}

function matchBrace(text, open, file) {
  let depth = 0;
  for (let i = open; i < text.length; i++) {
    if (text[i] === '{') depth++;
    else if (text[i] === '}') {
      depth--;
      if (depth === 0) return i;
    }
  }
  fail(`${file}: unbalanced braces`);
  return text.length;
}

function parseRules(text, file) {
  const rules = [];
  let i = 0;
  while (i < text.length) {
    if (/\s/.test(text[i])) { i++; continue; }
    const open = text.indexOf('{', i);
    if (open < 0) { fail(`${file}: stray text "${text.slice(i).trim().slice(0, 40)}"`); break; }
    const prelude = text.slice(i, open).trim().replace(/\s+/g, ' ');
    const close = matchBrace(text, open, file);
    const body = text.slice(open + 1, close);
    if (prelude.startsWith('@')) rules.push({ at: prelude, rules: parseRules(body, file) });
    else if (body.includes('{')) fail(`${file}: nested block inside "${prelude}"`);
    else rules.push({ selector: prelude, decls: parseDecls(body, file, prelude) });
    i = close + 1;
  }
  return rules;
}

function splitTop(text, sep) {
  const parts = [];
  let depth = 0, start = 0, quote = null;
  for (let i = 0; i < text.length; i++) {
    const ch = text[i];
    if (quote) { if (ch === quote) quote = null; continue; }
    if (ch === '"' || ch === "'") quote = ch;
    else if (ch === '(') depth++;
    else if (ch === ')') depth--;
    else if (depth === 0 && (sep === ' ' ? /\s/.test(ch) : ch === sep)) {
      parts.push(text.slice(start, i));
      start = i + 1;
    }
  }
  parts.push(text.slice(start));
  return parts.map((p) => p.trim()).filter((p) => p !== '');
}

function parseDecls(body, file, selector) {
  const decls = new Map();
  for (const raw of splitTop(body, ';')) {
    const colon = raw.indexOf(':');
    if (colon < 0) { fail(`${file} ${selector}: cannot read "${raw}"`); continue; }
    const prop = raw.slice(0, colon).trim();
    const value = raw.slice(colon + 1).trim().replace(/\s+/g, ' ');
    if (decls.has(prop)) fail(`${file} ${selector}: ${prop} declared twice`);
    decls.set(prop, value);
  }
  return decls;
}

function readSheets() {
  const lightRules = parseRules(stripComments(fs.readFileSync(LIGHT_CSS, 'utf8'), '02-tokens.css'), '02-tokens.css');
  const darkRules = parseRules(stripComments(fs.readFileSync(DARK_CSS, 'utf8'), '03-tokens-dark.css'), '03-tokens-dark.css');
  let light = null, dark = null, ltr = null, osDark = null;
  for (const r of lightRules) {
    if (r.selector === ':root' && !light) light = r.decls;
    else if (r.selector === 'html[dir="ltr"]' && !ltr) ltr = r.decls;
    else fail(`02-tokens.css: unhandled block "${r.selector ?? r.at}"`);
  }
  for (const r of darkRules) {
    if (r.selector === ':root[data-ins-theme="dark"]' && !dark) dark = r.decls;
    else if (r.at === '@media (prefers-color-scheme: dark)' && r.rules.length === 1 && r.rules[0].selector === ':root:not([data-ins-theme="light"])' && !osDark) osDark = r.rules[0].decls;
    else fail(`03-tokens-dark.css: unhandled block "${r.selector ?? r.at}"`);
  }
  if (!light) fail('02-tokens.css: no :root block');
  if (!dark) fail('03-tokens-dark.css: no :root[data-ins-theme="dark"] block');
  light ??= new Map();
  dark ??= new Map();
  // The OS-dark twin is what build.mjs emits; Avalonia follows the OS through ThemeVariant.Default, so it only has to match.
  if (osDark && JSON.stringify([...osDark]) !== JSON.stringify([...dark])) fail('03-tokens-dark.css: the prefers-color-scheme twin differs from the dark block');
  // The LTR block may only swap tokens Avalonia skips; FlowDirection does the rest.
  for (const prop of (ltr ?? new Map()).keys()) {
    if (!SKIP[prop]) fail(`02-tokens.css html[dir="ltr"]: ${prop} is not on the skip list`);
  }
  for (const [where, decls] of [['02-tokens.css :root', light], ['03-tokens-dark.css dark', dark]]) {
    for (const prop of [...decls.keys()]) {
      if (prop.startsWith('--')) continue;
      if (!PROPERTIES[prop]) fail(`${where}: unhandled property ${prop}`);
      decls.delete(prop);
    }
  }
  for (const prop of dark.keys()) {
    if (!light.has(prop)) fail(`03-tokens-dark.css: ${prop} has no light value`);
    if (!prop.startsWith('--ins-')) fail(`03-tokens-dark.css: ${prop} is not an --ins- token`);
  }
  for (const prop of light.keys()) {
    if (!prop.startsWith('--ins-')) fail(`02-tokens.css: ${prop} is not an --ins- token`);
  }
  for (const prop of Object.keys(SKIP)) {
    if (!light.has(prop)) fail(`skip list: ${prop} no longer exists in the CSS`);
  }
  for (const [prop, pin] of Object.entries(FIXED)) {
    if (!light.has(prop)) fail(`fixed list: ${prop} no longer exists in the CSS`);
    else if (light.get(prop) !== pin.css) fail(`fixed list: ${prop} is now "${light.get(prop)}", review its pinned value`);
    if (dark.has(prop)) fail(`fixed list: ${prop} has a dark value, review its pinned value`);
  }
  return { light, dark };
}

function keyOf(prop) {
  return 'Ins' + prop.slice('--ins-'.length).split('-').map((p) => p.charAt(0).toUpperCase() + p.slice(1)).join('');
}

function makeResolver(light, dark) {
  const envs = { light, dark: new Map([...light, ...dark]) };
  const raw = (prop, theme) => (FIXED[prop] ? FIXED[prop].value : envs[theme].get(prop));
  function resolve(prop, theme, stack = []) {
    if (BRAND_BASE[prop]) return `@${BRAND_BASE[prop].field}`;
    if (stack.includes(prop)) { fail(`${prop}: var() cycle through ${stack.join(' > ')}`); return ''; }
    const value = raw(prop, theme);
    if (value === undefined) { fail(`${stack.at(-1) ?? prop}: var(${prop}) is not defined`); return ''; }
    return substitute(value, theme, [...stack, prop]);
  }
  function substitute(value, theme, stack) {
    let out = '', i = 0;
    while (i < value.length) {
      const at = value.indexOf('var(', i);
      if (at < 0) { out += value.slice(i); break; }
      out += value.slice(i, at);
      let depth = 0, end = at + 3;
      for (; end < value.length; end++) {
        if (value[end] === '(') depth++;
        else if (value[end] === ')' && --depth === 0) break;
      }
      const inner = value.slice(at + 4, end);
      const comma = splitTop(inner, ',');
      const name = comma[0];
      const fallback = inner.includes(',') ? inner.slice(inner.indexOf(',') + 1).trim() : undefined;
      if (raw(name, theme) === undefined && !BRAND_BASE[name] && fallback !== undefined) out += substitute(fallback, theme, stack);
      else out += resolve(name, theme, stack);
      i = end + 1;
    }
    return out;
  }
  return resolve;
}

const NUM = String.raw`-?(?:\d+\.?\d*|\.\d+)`;

function num(n) {
  const r = Math.round(n * 10000) / 10000;
  return Object.is(r, -0) ? '0' : String(r);
}

function parseLength(s) {
  const m = new RegExp(`^(${NUM})(px|rem)?$`).exec(s);
  if (!m) return null;
  const n = parseFloat(m[1]);
  if (!m[2] && n !== 0) return null;
  return m[2] === 'rem' ? n * 16 : n;
}

function parseAlpha(s) {
  const m = new RegExp(`^(${NUM})(%)?$`).exec(s.trim());
  if (!m) return null;
  const a = parseFloat(m[1]) / (m[2] ? 100 : 1);
  return a >= 0 && a <= 1 ? a : null;
}

function alphaByte(a) {
  return Math.round(a * 255);
}

function parseColor(s) {
  s = s.trim();
  if (s === 'transparent') return { r: 0, g: 0, b: 0, a: 0, clear: true };
  const brand = /^@(\w+)$/.exec(s);
  if (brand) return { brand: brand[1], a: null };
  const hex = /^#([0-9a-fA-F]{3,8})$/.exec(s);
  if (hex) {
    let h = hex[1];
    if (h.length === 3 || h.length === 4) h = [...h].map((c) => c + c).join('');
    if (h.length !== 6 && h.length !== 8) return null;
    return { r: parseInt(h.slice(0, 2), 16), g: parseInt(h.slice(2, 4), 16), b: parseInt(h.slice(4, 6), 16), a: h.length === 8 ? parseInt(h.slice(6, 8), 16) : 255 };
  }
  const fn = /^rgba?\((.*)\)$/.exec(s);
  if (!fn) return null;
  let parts = splitTop(fn[1], ',');
  if (parts.length === 1) {
    const [rgb, alpha] = fn[1].split('/');
    parts = splitTop(rgb, ' ');
    if (alpha !== undefined) parts.push(alpha.trim());
  }
  if (parts.length >= 1 && /^@\w+$/.test(parts[0])) {
    if (parts.length > 2) return null;
    const a = parts.length === 2 ? parseAlpha(parts[1]) : 1;
    return a === null ? null : { brand: parts[0].slice(1), a: alphaByte(a), needsTriplet: true };
  }
  if (parts.length !== 3 && parts.length !== 4) return null;
  const ch = parts.slice(0, 3).map((p) => (/^\d+$/.test(p) && +p <= 255 ? +p : null));
  if (ch.includes(null)) return null;
  const a = parts.length === 4 ? parseAlpha(parts[3]) : 1;
  if (a === null) return null;
  return { r: ch[0], g: ch[1], b: ch[2], a: alphaByte(a) };
}

function checkBrandColor(c, prop) {
  if (!c || c.brand === undefined) return c;
  const base = Object.values(BRAND_BASE).find((b) => b.field === c.brand);
  if (!base) { fail(`${prop}: unknown brand input ${c.brand}`); return null; }
  if (c.needsTriplet && !base.triplet) { fail(`${prop}: rgba() over the hex brand token ${c.brand}`); return null; }
  return { brand: c.brand, a: c.a };
}

function parseShadows(s, prop) {
  const list = [];
  for (const one of splitTop(s, ',')) {
    const parts = splitTop(one, ' ');
    let inset = false, color = null;
    const lengths = [];
    for (const p of parts) {
      if (p === 'inset' && !inset) { inset = true; continue; }
      const len = parseLength(p);
      if (len !== null && color === null) { lengths.push(len); continue; }
      const c = parseColor(p);
      if (c && color === null) { color = checkBrandColor(c, prop); continue; }
      return null;
    }
    if (!color || lengths.length < 2 || lengths.length > 4) return null;
    if (lengths[2] !== undefined && lengths[2] < 0) return null;
    list.push({ inset, x: lengths[0], y: lengths[1], blur: lengths[2] ?? 0, spread: lengths[3] ?? 0, color });
  }
  return list.length ? list : null;
}

function angleOf(s) {
  const sides = { 'to top': 0, 'to right': 90, 'to bottom': 180, 'to left': 270 };
  if (s in sides) return sides[s];
  const m = new RegExp(`^(${NUM})(deg|turn|rad|grad)$`).exec(s);
  if (!m) return null;
  const n = parseFloat(m[1]);
  return { deg: n, turn: n * 360, rad: (n * 180) / Math.PI, grad: n * 0.9 }[m[2]];
}

function parseGradient(s, prop) {
  const m = /^linear-gradient\((.*)\)$/.exec(s);
  if (!m) return null;
  const args = splitTop(m[1], ',');
  let angle = 180;
  const first = angleOf(args[0]);
  if (first !== null) { angle = first; args.shift(); }
  const stops = [];
  for (const a of args) {
    const parts = splitTop(a, ' ');
    const c = parseColor(parts[0]);
    if (!c || parts.length > 2) return null;
    if (c.brand !== undefined) { fail(`${prop}: brand colour inside a gradient is not supported`); return null; }
    let offset = null;
    if (parts[1] !== undefined) {
      const pm = new RegExp(`^(${NUM})%$`).exec(parts[1]);
      if (!pm) return null;
      offset = parseFloat(pm[1]) / 100;
    }
    stops.push({ c, offset });
  }
  if (stops.length < 2) return null;
  if (stops[0].offset === null) stops[0].offset = 0;
  if (stops.at(-1).offset === null) stops.at(-1).offset = 1;
  for (let i = 1; i < stops.length - 1; i++) {
    if (stops[i].offset !== null) continue;
    let j = i;
    while (stops[j].offset === null) j++;
    const from = stops[i - 1].offset, step = (stops[j].offset - from) / (j - i + 1);
    for (let k = i; k < j; k++) stops[k].offset = from + step * (k - i + 1);
  }
  // CSS interpolates premultiplied, so a transparent stop fades its neighbour's hue rather than black.
  const out = [];
  stops.forEach((st, i) => {
    if (!st.c.clear) { out.push({ offset: st.offset, c: st.c }); return; }
    const left = stops[i - 1] && !stops[i - 1].c.clear ? stops[i - 1].c : null;
    const right = stops[i + 1] && !stops[i + 1].c.clear ? stops[i + 1].c : null;
    if (left) out.push({ offset: st.offset, c: { ...left, a: 0 } });
    if (right && !(left && left.r === right.r && left.g === right.g && left.b === right.b)) out.push({ offset: st.offset, c: { ...right, a: 0 } });
    if (!left && !right) out.push({ offset: st.offset, c: { r: 0, g: 0, b: 0, a: 0 } });
  });
  const rad = (angle * Math.PI) / 180, sin = Math.sin(rad), cos = Math.cos(rad);
  const half = (Math.abs(sin) + Math.abs(cos)) / 2;
  return {
    start: [0.5 - sin * half, 0.5 + cos * half],
    end: [0.5 + sin * half, 0.5 - cos * half],
    stops: out.map((st) => ({ offset: st.offset, c: { r: st.c.r, g: st.c.g, b: st.c.b, a: st.c.a } })),
  };
}

function mapValue(prop, value) {
  if (prop.startsWith('--ins-r-')) {
    const px = parseLength(value);
    return px === null ? null : { kind: 'radius', px };
  }
  const base = BRAND_BASE[prop];
  if (base) return { kind: 'color', c: { brand: base.field, a: null } };
  const color = parseColor(value);
  if (color) {
    const c = checkBrandColor(color, prop);
    return c && !c.clear ? { kind: 'color', c } : c ? { kind: 'color', c: { r: 0, g: 0, b: 0, a: 0 } } : null;
  }
  if (value.startsWith('linear-gradient(')) {
    const g = parseGradient(value, prop);
    return g ? { kind: 'gradient', ...g } : null;
  }
  const bez = new RegExp(`^cubic-bezier\\((${NUM}),(${NUM}),(${NUM}),(${NUM})\\)$`).exec(value.replace(/\s+/g, ''));
  if (bez) {
    const [x1, y1, x2, y2] = bez.slice(1).map(parseFloat);
    return x1 < 0 || x1 > 1 || x2 < 0 || x2 > 1 ? null : { kind: 'spline', x1, y1, x2, y2 };
  }
  const time = new RegExp(`^(${NUM})(s|ms)$`).exec(value);
  if (time) return { kind: 'time', ms: parseFloat(time[1]) * (time[2] === 's' ? 1000 : 1) };
  const len = parseLength(value);
  if (len !== null) return { kind: 'double', px: len };
  const shadows = parseShadows(value, prop);
  if (shadows) return { kind: 'shadows', list: shadows };
  return null;
}

const isBrand = (c) => c.brand !== undefined;
const usesBrand = (v) => (v.kind === 'color' && isBrand(v.c)) || (v.kind === 'shadows' && v.list.some((s) => isBrand(s.color)));

function hex2(n) {
  return n.toString(16).toUpperCase().padStart(2, '0');
}

function argb(c) {
  return `#${hex2(c.a)}${hex2(c.r)}${hex2(c.g)}${hex2(c.b)}`;
}

function xamlShadows(list) {
  return list.map((s) => `${s.inset ? 'inset ' : ''}${num(s.x)} ${num(s.y)} ${num(s.blur)} ${num(s.spread)} ${argb(s.color)}`).join(', ');
}

function xamlEntries(key, v, pad) {
  switch (v.kind) {
    case 'color':
      return [`${pad}<Color x:Key="${key}Color">${argb(v.c)}</Color>`, `${pad}<SolidColorBrush x:Key="${key}" Color="${argb(v.c)}" />`];
    case 'radius':
      return [`${pad}<CornerRadius x:Key="${key}">${num(v.px)}</CornerRadius>`];
    case 'double':
      return [`${pad}<x:Double x:Key="${key}">${num(v.px)}</x:Double>`];
    case 'time':
      return [`${pad}<sys:TimeSpan x:Key="${key}">0:0:${num(v.ms / 1000)}</sys:TimeSpan>`];
    case 'spline':
      return [`${pad}<SplineEasing x:Key="${key}" X1="${num(v.x1)}" Y1="${num(v.y1)}" X2="${num(v.x2)}" Y2="${num(v.y2)}" />`];
    case 'shadows':
      return [`${pad}<BoxShadows x:Key="${key}">${xamlShadows(v.list)}</BoxShadows>`];
    case 'gradient': {
      const pt = (p) => `${num(p[0] * 100)}%,${num(p[1] * 100)}%`;
      return [
        `${pad}<LinearGradientBrush x:Key="${key}" StartPoint="${pt(v.start)}" EndPoint="${pt(v.end)}">`,
        ...v.stops.map((st) => `${pad}  <GradientStop Offset="${num(st.offset)}" Color="${argb(st.c)}" />`),
        `${pad}</LinearGradientBrush>`,
      ];
    }
  }
  throw new Error(`no XAML for ${v.kind}`);
}

function csColor(c) {
  if (!isBrand(c)) return `Color.FromUInt32(0x${argb(c).slice(1)}u)`;
  return c.a === null ? `brand.${c.brand}` : `WithAlpha(brand.${c.brand}, ${c.a})`;
}

function csShadow(s) {
  return `new BoxShadow { OffsetX = ${num(s.x)}, OffsetY = ${num(s.y)}, Blur = ${num(s.blur)}, Spread = ${num(s.spread)}, Color = ${csColor(s.color)}${s.inset ? ', IsInset = true' : ''} }`;
}

function csLine(key, v, pad, prop) {
  if (v.kind === 'color') return `${pad}SetColor(target, "${key}", ${csColor(v.c)});`;
  if (v.kind === 'shadows') {
    const [first, ...rest] = v.list.map(csShadow);
    const tail = rest.length ? `, new[] { ${rest.join(', ')} }` : '';
    return `${pad}target["${key}"] = new BoxShadows(${first}${tail});`;
  }
  fail(`${prop}: a brand token of kind ${v.kind} has no code path`);
  return '';
}

function generate() {
  const { light, dark } = readSheets();
  const resolve = makeResolver(light, dark);
  const root = [], themed = [], brand = [];
  const keys = new Map();
  const claim = (key, prop) => {
    if (keys.has(key)) fail(`${prop}: resource key ${key} collides with ${keys.get(key)}`);
    keys.set(key, prop);
  };
  for (const prop of light.keys()) {
    if (SKIP[prop]) continue;
    const key = keyOf(prop);
    const values = {};
    for (const theme of ['light', 'dark']) {
      const resolved = resolve(prop, theme);
      const v = mapValue(prop, resolved);
      if (!v) fail(`${prop}: cannot map ${theme} value "${resolved}"; map it or add it to the skip list`);
      values[theme] = v;
    }
    if (!values.light || !values.dark) continue;
    if (values.light.kind !== values.dark.kind) { fail(`${prop}: light is ${values.light.kind}, dark is ${values.dark.kind}`); continue; }
    claim(key, prop);
    if (values.light.kind === 'color') claim(key + 'Color', prop);
    const entry = { prop, key, ...values };
    if (BRAND_BASE[prop] || usesBrand(values.light) || usesBrand(values.dark)) brand.push(entry);
    else if (JSON.stringify(values.light) === JSON.stringify(values.dark)) root.push(entry);
    else themed.push(entry);
  }
  const header = 'Generated by packages/avalonia/tools/gen-tokens.mjs from src/css/02-tokens.css and src/css/03-tokens-dark.css; do not edit.';
  const ax = [
    `<!-- ${header} -->`,
    '<ResourceDictionary xmlns="https://github.com/avaloniaui"',
    '                    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"',
    '                    xmlns:sys="using:System">',
    '  <ResourceDictionary.ThemeDictionaries>',
  ];
  for (const theme of ['light', 'dark']) {
    ax.push(`    <ResourceDictionary x:Key="${theme === 'light' ? 'Light' : 'Dark'}">`);
    for (const e of themed) ax.push(...xamlEntries(e.key, e[theme], '      '));
    ax.push('    </ResourceDictionary>');
  }
  ax.push('  </ResourceDictionary.ThemeDictionaries>');
  for (const e of root) ax.push(...xamlEntries(e.key, e.light, '  '));
  ax.push('</ResourceDictionary>', '');
  const cs = [
    `// <auto-generated>${header}</auto-generated>`,
    'using Avalonia.Controls;',
    'using Avalonia.Media;',
    'using Insiyab.Avalonia.Brand;',
    '',
    'namespace Insiyab.Avalonia.Generated;',
    '',
    'internal static class BrandTokens',
    '{',
    '    public static void Apply(IResourceDictionary target, BrandSet brand, bool dark)',
    '    {',
    '        if (dark)',
    '        {',
    ...brand.map((e) => csLine(e.key, e.dark, '            ', e.prop)),
    '        }',
    '        else',
    '        {',
    ...brand.map((e) => csLine(e.key, e.light, '            ', e.prop)),
    '        }',
    '    }',
    '',
    '    private static Color WithAlpha(Color c, byte alpha) => Color.FromArgb(alpha, c.R, c.G, c.B);',
    '',
    '    private static void SetColor(IResourceDictionary target, string key, Color color)',
    '    {',
    '        target[key] = new SolidColorBrush(color);',
    '        target[key + "Color"] = color;',
    '    }',
    '}',
    '',
  ];
  return { axaml: ax.join('\n'), cs: cs.join('\n'), counts: { root: root.length, themed: themed.length, brand: brand.length, skipped: Object.keys(SKIP).length } };
}

const out = generate();
if (errors.length) {
  for (const e of errors) console.error(`gen-tokens: ${e}`);
  process.exit(1);
}
const files = [[OUT_AXAML, out.axaml], [OUT_CS, out.cs]];
if (process.argv.includes('--check')) {
  const stale = files.filter(([file, text]) => !fs.existsSync(file) || fs.readFileSync(file, 'utf8').replace(/\r\n/g, '\n') !== text);
  for (const [file] of stale) console.error(`gen-tokens: ${path.relative(REPO, file)} is out of date; run node packages/avalonia/tools/gen-tokens.mjs`);
  process.exit(stale.length ? 1 : 0);
}
for (const [file, text] of files) fs.writeFileSync(file, text);
const c = out.counts;
console.log(`gen-tokens: ${c.root} invariant, ${c.themed} per theme, ${c.brand} brand, ${c.skipped} skipped`);
