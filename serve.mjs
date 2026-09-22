/* ==========================================================================
   Insiyab · local server for the demo page
   ==========================================================================

   `node serve.mjs` — then open http://localhost:4173

   This exists because **opening demo/index.html over file:// does not show the
   real design.** The faces are self-hosted and requested by relative path, and
   over file:// they count as cross-origin: the browser blocks them, the page
   falls back to a system serif, and the most visible part of any change to the
   library is exactly the part you cannot see. Same origin over http, and the
   typography is real.

   No dependencies, same rule as build.mjs.
   ========================================================================== */

import { createServer } from 'node:http';
import { readFile } from 'node:fs/promises';
import { extname, normalize, join } from 'node:path';

const PORT = Number(process.env.PORT || 4173);

const TYPES = {
  '.html': 'text/html; charset=utf-8',
  '.css': 'text/css; charset=utf-8',
  '.js': 'text/javascript; charset=utf-8',
  '.json': 'application/json; charset=utf-8',
  '.woff2': 'font/woff2',
  '.woff': 'font/woff',
  '.svg': 'image/svg+xml',
  '.png': 'image/png',
  '.md': 'text/markdown; charset=utf-8'
};

createServer(async (req, res) => {
  const url = decodeURIComponent((req.url || '/').split('?')[0]);
  const rel = url === '/' ? 'demo/index.html' : url.replace(/^\/+/, '');

  /* Serving the repository root means path traversal is worth one line of care,
     even on localhost. */
  const path = normalize(rel);
  if (path.startsWith('..') || path.includes(`..${'/'}`) || path.includes('..\\')) {
    res.writeHead(403).end('forbidden');
    return;
  }

  try {
    const body = await readFile(join(process.cwd(), path));
    res.writeHead(200, {
      'content-type': TYPES[extname(path)] || 'application/octet-stream',
      /* The demo is edited and reloaded constantly; a cached stylesheet is a
         confusing five minutes every single time. */
      'cache-control': 'no-store'
    }).end(body);
  } catch {
    res.writeHead(404, { 'content-type': 'text/plain; charset=utf-8' })
      .end(`not found: /${path}\n\nDid you run \`node build.mjs\` first?`);
  }
}).listen(PORT, () => {
  console.log(`insiyab demo  ->  http://localhost:${PORT}`);
  console.log('serving the repository root; ctrl+c to stop');
});
