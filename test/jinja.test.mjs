// The Jinja package, `insiyab` on PyPI, in two parts:
//
//   python   its own unit tests (setup, escaping, head(), the files, Flask), one
//            check each, run with the first Python found that has Jinja2.
//   parity   every example in test/fixtures/parity/ (copied from the docs) rendered
//            with the macros and compared, once the core has built both, with the
//            example itself, as the React wrapper's test does.
//
// Needs Python 3.8+ with Jinja2 (Flask too, for its checks); without it the file skips.
import { spawnSync } from 'node:child_process';
import { dirname, join } from 'node:path';
import { fileURLToPath } from 'node:url';
import { launch, BASE } from './lib/cdp.mjs';
import { suite } from './lib/check.mjs';
import { NORMALIZE } from './lib/parity.mjs';

const ROOT = join(dirname(fileURLToPath(import.meta.url)), '..');
const PKG = join(ROOT, 'packages', 'jinja');
const PY_ENV = { ...process.env, PYTHONIOENCODING: 'utf-8', PYTHONUTF8: '1' };

const python = [process.env.PYTHON, 'python3', 'python', 'py'].filter(Boolean).find((cmd) => {
  const r = spawnSync(cmd, ['-c', 'import sys, jinja2; sys.exit(0 if sys.version_info >= (3, 8) else 1)'], { encoding: 'utf8', env: PY_ENV });
  return r.status === 0;
});
if (!python) {
  console.log('SKIP  no Python 3.8+ with Jinja2 found (set PYTHON to choose one)');
  process.exit(0);
}

const { ok, end } = suite();

// ------------------------------------------------------------------ python
const unit = spawnSync(python, ['-m', 'unittest', 'discover', '-s', join(PKG, 'tests'), '-v'], { cwd: ROOT, encoding: 'utf8', env: PY_ENV });
const lines = (unit.stderr || '').split(/\r?\n/);
let ran = 0, skipped = 0;
for (const line of lines) {
  const m = line.match(/^(test_\w+) \((?:[\w.]+\.)?(\w+)\) \.\.\. (ok|FAIL|ERROR|skipped.*)$/);
  if (!m) continue;
  ran++;
  /* A skipped test (Flask's, without Flask) is not a check that passed. */
  if (m[3].startsWith('skipped')) { skipped++; continue; }
  ok(`python: ${m[2]}: ${m[1].slice(5).replace(/_/g, ' ')}`, m[3] === 'ok');
}
ok('python: the unit tests ran and passed', unit.status === 0 && ran > 0, unit.status === 0 ? `${ran} tests${skipped ? `, ${skipped} skipped` : ''}` : lines.filter((l) => /Error|assert|FAIL/.test(l)).slice(0, 4).join(' | '));

// ------------------------------------------------------------------ parity
const rendered = spawnSync(python, [join(PKG, 'tests', 'render_parity.py')], { cwd: ROOT, encoding: 'utf8', env: PY_ENV, maxBuffer: 64 * 1024 * 1024 });
if (rendered.status !== 0) {
  ok('parity: the cases render', false, rendered.stderr.trim().split('\n').slice(-3).join(' | '));
  end();
  process.exit(1);
}
const cases = JSON.parse(rendered.stdout);

const b = await launch();
await b.size(1280, 900);
const E = (js) => b.evaluate(js);
await b.goto(`${BASE}/test/fixtures/wrappers.html`);
await E(`${NORMALIZE}; 0`);
const settle = () => E(`new Promise((r) => requestAnimationFrame(() => requestAnimationFrame(() => setTimeout(() => r(0), 120))))`);
const build = async (html) => {
  await E(`(() => { const s = document.getElementById('stage'); s.innerHTML = '<div id="case"></div>'; const c = document.getElementById('case'); c.innerHTML = ${JSON.stringify(html)}; Insiyab.init(c); return 0; })()`);
  await settle();
  const lines = await E(`__normalize(document.getElementById('case'))`);
  await E(`document.getElementById('stage').innerHTML = ''; 0`);
  return lines;
};

const names = Object.keys(cases);
let same = 0;
for (const name of names) {
  const expected = await E(`fetch('/test/fixtures/parity/${name}.html').then((r) => r.ok ? r.text() : null)`);
  if (expected == null) { ok(`parity: ${name} has a fixture`, false); continue; }
  const got = await build(cases[name]);
  const want = await build(expected);
  let at = 0;
  while (at < got.length && at < want.length && got[at] === want[at]) at++;
  const equal = at === got.length && at === want.length;
  if (equal) same++;
  ok(`parity: ${name}`, equal, equal ? '' : `line ${at + 1}: jinja ${(got[at] ?? '(ends)').trim()}  |  docs ${(want[at] ?? '(ends)').trim()}`);
}
ok(`parity: every case compared (${same}/${names.length} the same)`, names.length >= 80, names.length);
ok('parity: no console errors, warnings or exceptions', b.logs.length === 0, b.logs.join(' | '));

await b.close();
end();
