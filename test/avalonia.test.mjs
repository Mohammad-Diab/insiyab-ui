// Insiyab.Avalonia: generated tokens and brand fixture are current (node only), then its headless tests if .NET is installed.
import { spawn, spawnSync } from 'node:child_process';
import { readdirSync, readFileSync, statSync } from 'node:fs';
import { availableParallelism } from 'node:os';
import { dirname, join } from 'node:path';
import { fileURLToPath } from 'node:url';
import { suite } from './lib/check.mjs';

const ROOT = join(dirname(fileURLToPath(import.meta.url)), '..');
const PKG = join(ROOT, 'packages', 'avalonia');
const TESTS = join(PKG, 'tests', 'Insiyab.Avalonia.Tests');
const { ok, end } = suite();

for (const [label, script] of [['tokens: the generated files match the CSS', 'gen-tokens.mjs'], ['brand: the parity fixture matches insiyab.js', 'brand-fixture.mjs']]) {
  const r = spawnSync(process.execPath, [join(PKG, 'tools', script), '--check'], { cwd: PKG, encoding: 'utf8' });
  ok(label, r.status === 0, (r.status === 0 ? r.stdout : r.stderr || r.stdout).trim().split('\n').pop());
}

const sdk = spawnSync('dotnet', ['--version'], { encoding: 'utf8' });
if (sdk.status !== 0) {
  console.log('note  dotnet tests skipped: no .NET SDK found');
  end();
} else {
  const build = spawnSync('dotnet', ['build', TESTS], { cwd: PKG, encoding: 'utf8', maxBuffer: 64 * 1024 * 1024 });
  ok('dotnet: the test project builds', build.status === 0, build.status === 0 ? '' : (build.stdout + build.stderr).split(/\r?\n/).filter((l) => / error /.test(l)).slice(0, 3).join(' | '));
  if (build.status === 0) await runTests();
  end();
}

// Each process has its own headless UI thread and the tests mostly wait on real time, so classes run in several processes at once; INS_AVA_ONLY=ClassA,ClassB limits the run.
async function runTests() {
  const only = (process.env.INS_AVA_ONLY || '').split(',').filter(Boolean);
  const classes = readdirSync(TESTS).filter((f) => f.endsWith('.cs'))
    .flatMap((f) => [...readFileSync(join(TESTS, f), 'utf8').matchAll(/public class (\w+Tests)\b/g)].map((m) => ({ name: m[1], size: statSync(join(TESTS, f)).size })))
    .filter((c) => !only.length || only.includes(c.name))
    .sort((a, b) => b.size - a.size);
  const lanes = Array.from({ length: Math.max(1, Math.min(classes.length, Math.floor(availableParallelism() / 2), 8)) }, () => ({ size: 0, names: [] }));
  for (const c of classes) { const lane = lanes.reduce((a, b) => (a.size <= b.size ? a : b)); lane.size += c.size; lane.names.push(c.name); }
  const runs = await Promise.all(lanes.filter((l) => l.names.length).map((l) => new Promise((resolve) => {
    const filter = l.names.map((n) => `FullyQualifiedName~Insiyab.Avalonia.Tests.${n}.`).join('|');
    const child = spawn('dotnet', ['test', TESTS, '--no-build', '--filter', filter], { cwd: PKG });
    let out = '';
    child.stdout.on('data', (d) => { out += d; });
    child.stderr.on('data', (d) => { out += d; });
    child.on('close', (code) => resolve({ code, out }));
  })));
  let passed = 0, failed = 0, broken = false;
  for (const { code, out } of runs) {
    const summary = out.match(/(Passed|Failed)!\s+-\s+Failed:\s+(\d+),\s+Passed:\s+(\d+)/);
    if (!summary) broken = true; else { failed += Number(summary[2]); passed += Number(summary[3]); }
    for (const line of out.split(/\r?\n/).filter((l) => /^\s*Failed Insiyab\./.test(l))) ok(`dotnet: ${line.trim().slice(7).replace(/ \[.*$/, '')}`, false);
    if (code !== 0 && summary && summary[2] === '0') broken = true;
  }
  ok('dotnet: the headless tests ran and passed', !broken && failed === 0 && passed > 0, `${passed} passed, ${failed} failed, ${runs.length} processes`);
}
