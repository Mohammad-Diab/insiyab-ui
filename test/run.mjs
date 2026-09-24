/* ==========================================================================
   Insiyab · tests
   ==========================================================================

     node test/run.mjs                 build, then run every test/*.test.mjs
     node test/run.mjs sidebar dark    only the files whose names contain a word
     node test/run.mjs --no-build      skip the build (dist/ is already current)
     node test/run.mjs --verbose       print every check, not only failures

   Each file drives a real headless Chrome with real key and pointer events, on a
   throwaway profile, against a dev server this script starts on a free port — so
   it never collides with a `node serve.mjs` you already have open. Needs Chrome;
   set CHROME if it is somewhere unusual.

   Component behaviour is tested on test/fixtures/, which exists only for the
   tests, so the docs site can change its examples without breaking them. The
   shell-level behaviour — the theme sweep, the sidebar marker and its scroll,
   OS dark mode across real pages — is tested on the docs site, because it needs
   a real multi-page shell.
   ========================================================================== */
import { spawn, spawnSync } from 'node:child_process';
import { readdir } from 'node:fs/promises';
import { createServer } from 'node:net';
import { dirname, join } from 'node:path';
import { fileURLToPath } from 'node:url';

const HERE = dirname(fileURLToPath(import.meta.url));
const ROOT = join(HERE, '..');
const NODE = process.execPath;
const args = process.argv.slice(2);
const verbose = args.includes('--verbose');
const filters = args.filter((a) => !a.startsWith('--'));

if (!args.includes('--no-build')) {
  const build = spawnSync(NODE, ['build.mjs'], { cwd: ROOT, encoding: 'utf8' });
  if (build.status !== 0) {
    process.stdout.write(build.stdout + build.stderr);
    process.exit(1);
  }
  console.log(build.stdout.split('\n')[0]);
}

const port = await new Promise((resolve) => {
  const probe = createServer().listen(0, () => { const p = probe.address().port; probe.close(() => resolve(p)); });
});
const BASE = `http://localhost:${port}`;
const server = spawn(NODE, ['serve.mjs'], { cwd: ROOT, env: { ...process.env, PORT: String(port) }, stdio: 'ignore' });
let up = false;
for (let i = 0; i < 50 && !up; i++) {
  try { up = (await fetch(`${BASE}/demo/`)).ok; } catch {}
  if (!up) await new Promise((r) => setTimeout(r, 100));
}
if (!up) { server.kill(); console.error('dev server did not start'); process.exit(1); }

const files = (await readdir(HERE))
  .filter((f) => f.endsWith('.test.mjs'))
  .filter((f) => !filters.length || filters.some((w) => f.includes(w)))
  .sort();

let checks = 0, failedChecks = 0, failedFiles = 0;
const started = Date.now();
for (const file of files) {
  const t0 = Date.now();
  const out = await new Promise((resolve) => {
    let text = '';
    const child = spawn(NODE, [join(HERE, file)], { cwd: ROOT, env: { ...process.env, INS_BASE: BASE } });
    child.stdout.on('data', (d) => { text += d; });
    child.stderr.on('data', (d) => { text += d; });
    child.on('close', (code) => resolve({ text, code }));
  });
  const lines = out.text.split('\n');
  const pass = lines.filter((l) => l.startsWith('PASS')).length;
  const fail = lines.filter((l) => l.startsWith('FAIL'));
  checks += pass + fail.length;
  failedChecks += fail.length;
  const bad = out.code !== 0 || fail.length > 0;
  if (bad) failedFiles++;
  const secs = ((Date.now() - t0) / 1000).toFixed(1);
  console.log(`${bad ? 'FAIL' : 'ok  '}  ${file.replace('.test.mjs', '').padEnd(26)} ${String(pass).padStart(3)}/${pass + fail.length}  ${secs}s`);
  if (verbose) for (const l of lines.filter((l) => /^(PASS|FAIL)/.test(l))) console.log(`        ${l}`);
  else for (const l of fail) console.log(`        ${l}`);
  if (out.code !== 0 && !fail.length) console.log(out.text.split('\n').slice(-12).map((l) => `        ${l}`).join('\n'));
}

server.kill();
const mins = ((Date.now() - started) / 60000).toFixed(1);
console.log(`\n${checks - failedChecks}/${checks} checks passed · ${files.length - failedFiles}/${files.length} files passed · ${mins} min`);
process.exit(failedFiles ? 1 : 0);
