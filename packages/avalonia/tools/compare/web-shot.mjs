// Shoots a web docs page in advanced mode (or one element of it) the way tools/Shots shoots the matching gallery page: web-shot.mjs <slug> <out-dir> [--theme] [--dir] [--selector] [--width].
import { spawn } from 'node:child_process';
import { mkdirSync } from 'node:fs';
import { createServer } from 'node:net';
import { dirname, join, resolve } from 'node:path';
import { fileURLToPath } from 'node:url';

const HERE = dirname(fileURLToPath(import.meta.url));
const ROOT = join(HERE, '..', '..', '..', '..');
const args = process.argv.slice(2);
const flag = (name, fallback) => { const i = args.indexOf(`--${name}`); return i < 0 ? fallback : args[i + 1]; };
const [slug, outArg] = args.filter((a, i) => !a.startsWith('--') && !(i > 0 && args[i - 1].startsWith('--')));
if (!slug || !outArg) { console.error('usage: web-shot.mjs <slug> <out-dir> [--theme light|dark|both] [--dir rtl|ltr|both] [--selector css] [--width 1180]'); process.exit(2); }
const out = resolve(outArg);
mkdirSync(out, { recursive: true });
const themes = flag('theme', 'both') === 'both' ? ['light', 'dark'] : [flag('theme')];
const dirs = flag('dir', 'both') === 'both' ? ['rtl', 'ltr'] : [flag('dir')];
const selector = flag('selector', '.site-body');
const width = Number(flag('width', 1180));

const port = await new Promise((r) => { const p = createServer().listen(0, () => { const n = p.address().port; p.close(() => r(n)); }); });
process.env.INS_BASE = `http://localhost:${port}`;
const server = spawn(process.execPath, ['serve.mjs'], { cwd: ROOT, env: { ...process.env, PORT: String(port) }, stdio: 'ignore' });
const { launch } = await import(new URL('file:///' + join(ROOT, 'test', 'lib', 'cdp.mjs').replace(/\\/g, '/')).href);
let up = false;
for (let i = 0; i < 50 && !up; i++) { try { up = (await fetch(`${process.env.INS_BASE}/demo/`)).ok; } catch {} if (!up) await new Promise((r) => setTimeout(r, 100)); }
if (!up) { server.kill(); console.error('web-shot: the dev server did not start'); process.exit(1); }

const b = await launch({ width: width + 260, height: 3200 });
try {
  for (const theme of themes) {
    for (const dir of dirs) {
      await b.size(width + 260, 3200);
      await b.goto(`${process.env.INS_BASE}/demo/${slug}.html?mode=advanced&fx=off&dir=${dir}`);
      await b.evaluate(`Insiyab.theme(${JSON.stringify(theme)}); 0`);
      await b.sleep(700);
      const rect = await b.evaluate(`(() => { const el = document.querySelector(${JSON.stringify(selector)}); if (!el) return null;
        const r = el.getBoundingClientRect(); return { x: r.left + scrollX, y: r.top + scrollY, width: r.width, height: Math.min(r.height, 12000) }; })()`);
      if (!rect) { console.error(`web-shot: no element ${selector} on ${slug}`); process.exitCode = 1; continue; }
      const file = join(out, `web-${slug}-${theme}-${dir}.png`);
      await b.shot(file, rect);
      console.log(file);
    }
  }
} finally {
  await b.close();
  server.kill();
}
