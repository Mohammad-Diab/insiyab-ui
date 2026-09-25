/* Builds dist/ from src/ with the TypeScript compiler: ES modules and their types,
   one file per source file, "use client" kept where it is written. */
import { spawnSync } from 'node:child_process';
import { rmSync } from 'node:fs';
import { createRequire } from 'node:module';
import { dirname, join } from 'node:path';
import { fileURLToPath } from 'node:url';

const HERE = dirname(fileURLToPath(import.meta.url));
const require = createRequire(import.meta.url);
rmSync(join(HERE, 'dist'), { recursive: true, force: true });
const tsc = join(dirname(require.resolve('typescript/package.json')), 'bin', 'tsc');
const run = spawnSync(process.execPath, [tsc, '-p', HERE], { stdio: 'inherit' });
if (run.status !== 0) process.exit(run.status ?? 1);
console.log('built packages/react/dist');
