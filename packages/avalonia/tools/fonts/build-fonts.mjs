// Rebuilds src/Insiyab.Avalonia/Assets/Fonts from pinned upstream files. Usage: node build-fonts.mjs <path to harfbuzzjs@1.6.2 dist/harfbuzz-subset.wasm>
import { createHash } from 'node:crypto';
import { readFileSync, writeFileSync, mkdirSync, readdirSync, rmSync } from 'node:fs';
import { dirname, join, resolve } from 'node:path';
import { fileURLToPath } from 'node:url';
import { inflateRawSync } from 'node:zlib';

const here = dirname(fileURLToPath(import.meta.url));
const repo = resolve(here, '../../../..');
const out = resolve(here, '../../src/Insiyab.Avalonia/Assets/Fonts');

const GF = 'https://raw.githubusercontent.com/google/fonts/20776328c5a501f52fe9553fc5997451b4667589/ofl/tajawal';
const sources = {
  tajawalLight: { url: `${GF}/Tajawal-Light.ttf`, sha256: 'ab26ed3e0c5af2c71b469b5780decff8088d290ab91aa29b07321580136e813a' },
  tajawalRegular: { url: `${GF}/Tajawal-Regular.ttf`, sha256: '6882892da3e03527d5db2bbab3b48bde6ef2e878a43f522d1a4eebda90010a19' },
  tajawalMedium: { url: `${GF}/Tajawal-Medium.ttf`, sha256: '1ff8bd943a261c9dd7906bdb1993cab87b6d925a7fb5f848cdb51ed301531090' },
  tajawalBold: { url: `${GF}/Tajawal-Bold.ttf`, sha256: '0342ab6b74b6bd1b4b5bf7beda45a9734a2b3147a31e8a3a45b6a3417a52d9d7' },
  tajawalOfl: { url: `${GF}/OFL.txt`, sha256: null },
  inter: { url: 'https://github.com/rsms/inter/releases/download/v4.1/Inter-4.1.zip', sha256: '9883fdd4a49d4fb66bd8177ba6625ef9a64aa45899767dde3d36aa425756b11e' },
  cause: { url: 'https://github.com/xconsau/Cause/releases/download/v1.0/Cause-fonts.zip', sha256: '11835a0fd94c7ce75337e48d5c7faba6504fff83af78a867de30c108f4eb53db' },
};

const LATIN_EXT = 'U+0100-02BA,U+02BD-02C5,U+02C7-02CC,U+02CE-02D7,U+02DD-02FF,U+0300-036F,U+1D00-1DBF,U+1E00-1E9F,U+1EF2-1EFF,U+2020,U+20A0-20AB,U+20AD-20C0,U+2113,U+2C60-2C7F,U+A720-A7FF';

function cssRange(file) {
  const css = readFileSync(join(repo, 'src/css/01-fonts.css'), 'utf8');
  const m = css.match(new RegExp(`fonts/${file}\\.woff2'\\)[^;]*;\\s*unicode-range:\\s*([^;]+);`));
  if (!m) throw new Error(`no unicode-range for ${file} in 01-fonts.css`);
  return m[1];
}

function parseRanges(list) {
  return list.split(',').map(s => s.trim().replace(/^U\+/i, '')).filter(Boolean).map(s => {
    const [a, b] = s.split('-');
    return [parseInt(a, 16), parseInt(b ?? a, 16)];
  });
}

async function download(src) {
  const res = await fetch(src.url);
  if (!res.ok) throw new Error(`${src.url}: HTTP ${res.status}`);
  const buf = Buffer.from(await res.arrayBuffer());
  const hash = createHash('sha256').update(buf).digest('hex');
  if (src.sha256 && hash !== src.sha256) throw new Error(`${src.url}: sha256 ${hash}, expected ${src.sha256}`);
  return buf;
}

function unzip(zip, name) {
  let eocd = zip.length - 22;
  while (eocd >= 0 && zip.readUInt32LE(eocd) !== 0x06054b50) eocd--;
  if (eocd < 0) throw new Error('not a zip');
  const count = zip.readUInt16LE(eocd + 10);
  let p = zip.readUInt32LE(eocd + 16);
  for (let i = 0; i < count; i++) {
    const method = zip.readUInt16LE(p + 10);
    const size = zip.readUInt32LE(p + 20);
    const nameLen = zip.readUInt16LE(p + 28);
    const extraLen = zip.readUInt16LE(p + 30);
    const commentLen = zip.readUInt16LE(p + 32);
    const local = zip.readUInt32LE(p + 42);
    const entry = zip.toString('utf8', p + 46, p + 46 + nameLen);
    if (entry === name) {
      const start = local + 30 + zip.readUInt16LE(local + 26) + zip.readUInt16LE(local + 28);
      const data = zip.subarray(start, start + size);
      if (method === 0) return Buffer.from(data);
      if (method === 8) return inflateRawSync(data);
      throw new Error(`${name}: zip method ${method}`);
    }
    p += 46 + nameLen + extraLen + commentLen;
  }
  throw new Error(`${name} not in zip`);
}

const wasmPath = process.argv[2];
if (!wasmPath) throw new Error('pass the path to harfbuzz-subset.wasm');
const { instance } = await WebAssembly.instantiate(readFileSync(wasmPath));
const hb = instance.exports;

// Stylistic sets and character variants are opt-in looks the library never asks for, and they are most of Inter's glyphs.
const DROPPED_FEATURES = ['salt', ...Array.from({ length: 20 }, (_, i) => `ss${String(i + 1).padStart(2, '0')}`), ...Array.from({ length: 99 }, (_, i) => `cv${String(i + 1).padStart(2, '0')}`)];
const tagOf = s => ((s.charCodeAt(0) << 24) | (s.charCodeAt(1) << 16) | (s.charCodeAt(2) << 8) | s.charCodeAt(3)) >>> 0;

function subset(font, ranges) {
  const ptr = hb.malloc(font.length);
  new Uint8Array(hb.memory.buffer).set(font, ptr);
  const blob = hb.hb_blob_create(ptr, font.length, 0, 0, 0);
  hb.free(ptr);
  const face = hb.hb_face_create(blob, 0);
  hb.hb_blob_destroy(blob);
  const input = hb.hb_subset_input_create_or_fail();
  const unicodes = hb.hb_subset_input_unicode_set(input);
  for (const [a, b] of ranges) for (let c = a; c <= b; c++) hb.hb_set_add(unicodes, c);
  // Keep every name record, language, layout feature and script, so typographic family names and features like tnum survive.
  for (const which of [4, 5, 6, 7]) {
    const set = hb.hb_subset_input_set(input, which);
    hb.hb_set_clear(set);
    hb.hb_set_invert(set);
  }
  const features = hb.hb_subset_input_set(input, 6);
  for (const tag of DROPPED_FEATURES) hb.hb_set_del(features, tagOf(tag));
  hb.hb_subset_input_set_flags(input, 0x40);
  const result = hb.hb_subset_or_fail(face, input);
  hb.hb_subset_input_destroy(input);
  hb.hb_face_destroy(face);
  if (!result) throw new Error('subset failed');
  const outBlob = hb.hb_face_reference_blob(result);
  const len = hb.hb_blob_get_length(outBlob);
  const data = hb.hb_blob_get_data(outBlob, 0);
  const bytes = Buffer.from(new Uint8Array(hb.memory.buffer, data, len));
  hb.hb_blob_destroy(outBlob);
  hb.hb_face_destroy(result);
  return bytes;
}

const tajawalRanges = parseRanges(`${cssRange('tajawal-arabic-400')},${cssRange('tajawal-latin-400')}`);
const latinRanges = parseRanges(`${cssRange('inter-latin-400')},${LATIN_EXT}`);

const interZip = await download(sources.inter);
const causeZip = await download(sources.cause);

const fonts = [
  ['Tajawal-Light.ttf', await download(sources.tajawalLight), tajawalRanges],
  ['Tajawal-Regular.ttf', await download(sources.tajawalRegular), tajawalRanges],
  ['Tajawal-Medium.ttf', await download(sources.tajawalMedium), tajawalRanges],
  ['Tajawal-Bold.ttf', await download(sources.tajawalBold), tajawalRanges],
  ...['Light', 'Regular', 'Medium', 'SemiBold', 'Bold'].map(w => [`Inter-${w}.ttf`, unzip(interZip, `extras/ttf/Inter-${w}.ttf`), latinRanges]),
  ...['Regular', 'SemiBold', 'Bold'].map(w => [`Cause-${w}.ttf`, unzip(causeZip, `fonts/ttf/Cause-${w}.ttf`), latinRanges]),
];

mkdirSync(out, { recursive: true });
for (const f of readdirSync(out)) rmSync(join(out, f));

let before = 0, after = 0;
for (const [name, full, ranges] of fonts) {
  const small = subset(full, ranges);
  writeFileSync(join(out, name), small);
  before += full.length;
  after += small.length;
  console.log(`${name.padEnd(22)} ${String(full.length).padStart(8)} -> ${String(small.length).padStart(8)}`);
}
console.log(`${'total'.padEnd(22)} ${String(before).padStart(8)} -> ${String(after).padStart(8)}`);

writeFileSync(join(out, 'OFL-Tajawal.txt'), await download(sources.tajawalOfl));
writeFileSync(join(out, 'OFL-Inter.txt'), unzip(interZip, 'LICENSE.txt'));
writeFileSync(join(out, 'OFL-Cause.txt'), unzip(causeZip, 'OFL.txt'));
