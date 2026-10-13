// Runs the real Insiyab.brand() from src/js/insiyab.js in a stub DOM and writes the C# parity fixture.
import { readFileSync, writeFileSync, mkdirSync } from 'node:fs';
import { dirname, resolve } from 'node:path';
import { fileURLToPath } from 'node:url';
import vm from 'node:vm';

const here = dirname(fileURLToPath(import.meta.url));
const source = resolve(here, '../../../src/js/insiyab.js');
const out = resolve(here, '../tests/Insiyab.Avalonia.Tests/Fixtures/brand-parity.json');

const COLOURS = [
  '#4C1D95', '#9B2C5E',
  '#F5E6A8', '#FFFFFF', '#FAFAFA', '#FDE68A', '#A7F3D0', '#FBCFE8',
  '#C9A227', '#F59E0B', '#EA580C', '#10B981', '#0EA5E9',
  '#767676', '#757575', '#777777', '#747474', '#787878', '#808080', '#64748B',
  '#000000', '#111111', '#0A0A0A', '#1A1A1A', '#333333',
  '#FF0000', '#00FF00', '#0000FF', '#FFFF00', '#00FFFF', '#FF00FF',
  '#FFF', '#000', '#F0A', '#abc', '#9b2c5e',
  '#0F766E', '#2563EB', '#7C3AED', '#DC2626', '#16A34A', '#E11D48', '#4F46E5', '#8B5CF6',
];

function anything(overrides = {}) {
  const props = { ...overrides };
  let proxy;
  const target = function () { return proxy; };
  proxy = new Proxy(target, {
    get(_, key) {
      if (key in props) return props[key];
      if (key === Symbol.toPrimitive) return () => 0;
      if (key === 'then') return undefined;
      if (key === 'length') return 0;
      return proxy;
    },
    set(_, key, value) { props[key] = value; return true; },
    has() { return true; },
    apply() { return proxy; },
    construct() { return proxy; },
  });
  return proxy;
}

const attrs = new Map();
let captured = {};
const root = anything({
  getAttribute: (name) => (attrs.has(name) ? attrs.get(name) : null),
  setAttribute: (name, value) => { attrs.set(name, String(value)); },
  removeAttribute: (name) => { attrs.delete(name); },
  hasAttribute: (name) => attrs.has(name),
  toggleAttribute: (name, on) => { if (on) attrs.set(name, ''); else attrs.delete(name); return !!on; },
  style: { setProperty: (name, value) => { captured[name] = value; }, removeProperty: () => '' },
  classList: { add() {}, remove() {}, contains: () => false, toggle: () => false },
});
const document = anything({ documentElement: root, readyState: 'loading', addEventListener() {} });
const window = anything({
  addEventListener() {},
  localStorage: { getItem: () => null, setItem() {}, removeItem() {} },
  sessionStorage: { getItem: () => null, setItem() {}, removeItem() {} },
  matchMedia: () => ({ matches: false, addEventListener() {} }),
  console,
});

const context = vm.createContext({ window, document, console });
vm.runInContext(readFileSync(source, 'utf8'), context, { filename: source });
const Insiyab = window.Insiyab;
if (!Insiyab || typeof Insiyab.brand !== 'function') throw new Error('Insiyab.brand not found');

const cases = [];
for (const hex of COLOURS) {
  for (const theme of ['light', 'dark']) {
    attrs.set('data-ins-theme', theme);
    captured = {};
    if (Insiyab.brand(hex) !== hex) throw new Error('brand() rejected ' + hex);
    const tokens = {};
    for (const [name, value] of Object.entries(captured)) tokens[name.replace(/^--ins-/, '')] = value;
    const direct = Insiyab.color.derive(hex, theme);
    if (JSON.stringify(direct) !== JSON.stringify(tokens)) throw new Error('brand() and color.derive() disagree for ' + hex);
    if (Object.keys(tokens).length !== 11) throw new Error('unexpected token count for ' + hex);
    cases.push({ hex, theme, tokens });
  }
}

const json = JSON.stringify({ source: 'src/js/insiyab.js', cases }, null, 2) + '\n';
if (process.argv.includes('--check')) {
  let current = '';
  try { current = readFileSync(out, 'utf8').replace(/\r\n/g, '\n'); } catch {}
  if (current !== json) {
    console.error('brand-fixture: ' + out + ' is out of date; run node tools/brand-fixture.mjs');
    process.exit(1);
  }
  console.log('brand-fixture: ' + cases.length + ' cases, up to date');
} else {
  mkdirSync(dirname(out), { recursive: true });
  writeFileSync(out, json);
  console.log('wrote ' + cases.length + ' cases to ' + out);
}
