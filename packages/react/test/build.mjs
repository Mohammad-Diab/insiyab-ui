/* Builds what the React tests load, into test/out/ (not committed):

     harness.js   the cases, the behaviour app and React, for the browser
     parity.html  a page for the parity cases
     spa.html     the app rendered in the browser, from nothing, by spa.js, which
                  imports the core and the plugins as a bundled app would
     ssr.html     the app rendered here with react-dom/server, hydrated on load

   Every page loads the library and all ten plugins from the repository's dist/. */
import { build } from 'esbuild';
import { mkdirSync, writeFileSync } from 'node:fs';
import { dirname, join } from 'node:path';
import { fileURLToPath, pathToFileURL } from 'node:url';

const HERE = dirname(fileURLToPath(import.meta.url));
const OUT = join(HERE, 'out');
mkdirSync(OUT, { recursive: true });

const common = { bundle: true, jsx: 'automatic', logLevel: 'warning', define: { 'process.env.NODE_ENV': '"development"' } };
await build({ ...common, entryPoints: [join(HERE, 'harness.tsx')], outfile: join(OUT, 'harness.js'), format: 'iife', platform: 'browser', target: 'es2020' });
await build({ ...common, entryPoints: [join(HERE, 'spa.tsx')], outfile: join(OUT, 'spa.js'), format: 'esm', platform: 'browser', target: 'es2020' });
await build({ ...common, entryPoints: [join(HERE, 'ssr.tsx')], outfile: join(OUT, 'ssr.mjs'), format: 'esm', platform: 'node', packages: 'external' });

const DIST = '/dist';
const PLUGINS = ['palette', 'otp', 'phone', 'file', 'scrollspy', 'timeline', 'tree', 'color', 'carousel'];
const head = [
  `<link rel="stylesheet" href="${DIST}/insiyab.css">`,
  ...PLUGINS.map((p) => `<link rel="stylesheet" href="${DIST}/plugins/insiyab-${p}.css">`),
  `<script src="${DIST}/insiyab.js"></script>`,
  `<script src="${DIST}/plugins/insiyab-hijri.js"></script>`,
  ...PLUGINS.map((p) => `<script src="${DIST}/plugins/insiyab-${p}.js"></script>`)
].join('\n  ');
const page = (title, body, attrs = '') => `<!doctype html>
<html lang="ar" dir="rtl"${attrs}>
<head>
  <meta charset="utf-8">
  <title>${title}</title>
  ${head}
</head>
<body>
${body}
</body>
</html>
`;

writeFileSync(join(OUT, 'parity.html'), page('parity', `<main id="stage"></main>\n<script src="harness.js"></script>`));
/* No script tags for the core here: spa.js imports it, as a bundled app would. */
const cssOnly = head.split('\n  ').filter((l) => l.startsWith('<link')).join('\n  ');
writeFileSync(join(OUT, 'spa.html'), page('spa', `<div id="root"></div>\n<script type="module" src="spa.js"></script>`).replace(head, cssOnly));

const { renderApp } = await import(pathToFileURL(join(OUT, 'ssr.mjs')).href);
/* Hydrated on load, well after the core's own scan at DOMContentLoaded has been over
   the server's HTML: the order a slow bundle gives, and the one that would show a
   component the core had rebuilt before React arrived. */
writeFileSync(
  join(OUT, 'ssr.html'),
  page('ssr', `<div id="root">${renderApp()}</div>\n<script src="harness.js"></script>\n<script>window.addEventListener('load', function () { __react.hydrate(document.getElementById('root')); window.__hydrated = true; });</script>`)
);
console.log('built packages/react/test/out');
