/* ==========================================================================
   Insiyab · build
   ==========================================================================

   You get a build step; the people using the library do not. This concatenates
   src/css/*.css in filename order into one stylesheet, copies the script and the
   fonts, and writes a minified stylesheet beside them.

   No dependencies, on purpose: `node build.mjs` and nothing else. A release
   pipeline that needs installing is one more reason the two-file promise quietly
   stops being true.

     node build.mjs            build into dist/
     node build.mjs --check    build, verify, write nothing

   Run it from the repository root.
   ========================================================================== */

import { readFile, writeFile, mkdir, readdir, copyFile, rm } from 'node:fs/promises';
import { join, basename } from 'node:path';

const CHECK = process.argv.includes('--check');
const SRC_CSS = 'src/css';
const SRC_JS = 'src/js/insiyab.js';
const FONTS = 'fonts';
const DIST = 'dist';

const problems = [];
const fail = (msg) => problems.push(msg);

/* --------------------------------------------------------------------------
   The dark palette, written once and shipped twice.

   src/css/03-tokens-dark.css carries exactly one rule, keyed on the explicit
   attribute. The library also has to follow the operating system when the user
   has expressed no preference, and CSS has no way to give one declaration block
   two homes — so the block is duplicated here instead of by hand. Duplicating
   per-theme values by hand is how the segmented control broke in the source
   sheet: four copies, two per theme, and one of them got missed.

   The copy is emitted with its comments stripped. They are already above the
   original twenty lines up, and repeating them would nearly double the file for
   no reader's benefit.
   -------------------------------------------------------------------------- */

const DARK_SELECTOR = ':root[data-ins-theme="dark"]';

function expandDark(css, file) {
  const open = `${DARK_SELECTOR} {`;
  const start = css.indexOf(open);
  if (start === -1) {
    fail(`${file}: expected a rule for ${DARK_SELECTOR} and found none — the dark theme would only work when explicitly set.`);
    return css;
  }
  if (css.indexOf(open, start + 1) !== -1) {
    fail(`${file}: found more than one ${DARK_SELECTOR} rule. The build duplicates exactly one; merge them.`);
    return css;
  }

  /* The block contains no nested braces — only comments, rgba() and
     linear-gradient() — so the first closing brace in column zero ends it. If
     that ever stops being true this assertion is what will say so. */
  const end = css.indexOf('\n}', start);
  if (end === -1) {
    fail(`${file}: the ${DARK_SELECTOR} rule has no closing brace in column zero.`);
    return css;
  }

  const body = css.slice(start + open.length, end);
  if (body.includes('{')) {
    fail(`${file}: the dark rule now contains a nested block, which this duplication cannot handle safely.`);
    return css;
  }

  const declarations = body
    .replace(/\/\*[\s\S]*?\*\//g, '')          /* the comments live above the original */
    .split('\n')
    .map((line) => line.trim())
    .filter(Boolean)
    .map((line) => `    ${line}`)
    .join('\n');

  const media = [
    '',
    '/* The same block, reached by the operating system instead of by an explicit',
    '   choice. Emitted by build.mjs from the rule above — do not edit it here, and',
    '   do not hand-write a third copy. `:not([data-ins-theme="light"])` is what lets',
    "   a user who has chosen light keep it on a machine set to dark: the explicit",
    '   attribute always wins over the preference. */',
    '@media (prefers-color-scheme: dark) {',
    '  :root:not([data-ins-theme="light"]) {',
    declarations,
    '  }',
    '}'
  ].join('\n');

  return `${css.slice(0, end + 2)}\n${media}\n${css.slice(end + 2)}`;
}

/* --------------------------------------------------------------------------
   Minify: comments out, whitespace collapsed, and nothing else.

   Deliberately not a real minifier. Rewriting punctuation — dropping the space
   after a colon, collapsing the last semicolon in a block — is where a
   hand-rolled one breaks a selector like `html[dir="ltr"] :is(h1, h2, h3)`, and
   a stylesheet that is 8% smaller and subtly wrong is a bad trade. Comments are
   most of the weight in these files anyway, and gzip does the rest.
   -------------------------------------------------------------------------- */

/* A single pass that knows where it is. The first version of this tried to find
   strings with a regex and check them for comment markers, and it was wrong in
   both directions: `a date input's own picker` inside a comment looks exactly
   like the start of a quoted string, so prose with an apostrophe in it tripped
   the guard, while a genuine `/*` inside a real string would still have slipped
   through once the comment strip ran. Tracking the state is both simpler and
   actually correct — and it is the only way to collapse whitespace without
   collapsing it inside a font-family name too. */
function minify(css) {
  let out = '';
  let i = 0;
  const n = css.length;

  while (i < n) {
    const c = css[i];

    /* A comment: consume to the terminator, emit nothing. */
    if (c === '/' && css[i + 1] === '*') {
      const end = css.indexOf('*/', i + 2);
      if (end === -1) {
        fail('minify: unterminated comment in the built stylesheet.');
        return css;
      }
      i = end + 2;
      continue;
    }

    /* A string: emit verbatim, escapes included. Whitespace inside it is content
       ("Cascadia Mono" is two words) and comment markers inside it are content
       too, which is exactly the case the old regex could not see. */
    if (c === '"' || c === "'") {
      let j = i + 1;
      while (j < n && css[j] !== c) j += (css[j] === '\\' ? 2 : 1);
      if (j >= n) {
        fail(`minify: unterminated ${c === '"' ? 'double' : 'single'}-quoted string.`);
        return css;
      }
      out += css.slice(i, j + 1);
      i = j + 1;
      continue;
    }

    /* Runs of whitespace become one space. Not removed outright: the space in
       `html[dir="ltr"] :is(h1, h2, h3)` is a descendant combinator, and dropping
       it silently changes which elements the rule matches. */
    if (c === ' ' || c === '\t' || c === '\n' || c === '\r' || c === '\f') {
      let j = i;
      while (j < n && /\s/.test(css[j])) j++;
      if (out && !out.endsWith(' ')) out += ' ';
      i = j;
      continue;
    }

    out += c;
    i++;
  }

  return out.trim();
}

/* --------------------------------------------------------------------------
   Sanity checks on the built stylesheet. Cheap, and each one has cost a render
   at some point.
   -------------------------------------------------------------------------- */

function verify(css) {
  const bare = css.replace(/\/\*[\s\S]*?\*\//g, '');

  const open = (bare.match(/{/g) || []).length;
  const close = (bare.match(/}/g) || []).length;
  if (open !== close) fail(`unbalanced braces in the built stylesheet: ${open} open, ${close} close.`);

  /* The rename is the whole point of the extraction; a survivor means a rule is
     reading a token that no longer exists, which fails silently. */
  const leftovers = [...new Set(bare.match(/--(?:dk|bs|tone|net|sweep|shell)-[a-z0-9-]+/g) || [])];
  if (leftovers.length) fail(`un-renamed tokens still referenced: ${leftovers.join(', ')}`);

  /* Every var() that is read must also be declared somewhere in the bundle. An
     undefined custom property makes the whole declaration invalid at
     computed-value time, so the rule simply does nothing and says nothing — which
     is exactly how three shadows in the source sheet were dead for months. */
  const declared = new Set((bare.match(/--ins-[a-z0-9-]+(?=\s*:)/g) || []));
  const used = new Set((bare.match(/var\(\s*(--ins-[a-z0-9-]+)/g) || [])
    .map((m) => m.replace(/var\(\s*/, '')));
  const undeclared = [...used].filter((name) => !declared.has(name));
  if (undeclared.length) fail(`var() reads tokens nothing declares: ${undeclared.join(', ')}`);

  /* The same failure class one level up: an `animation` naming a keyframe set that
     does not exist is not an error, it simply does nothing — and it does nothing
     silently, which is how a component ends up with no entrance and nobody notices
     for a release. Cheap to check, given the parts are concatenated in one order and
     a keyframe defined in a later file is still in scope for an earlier rule. */
  const frames = new Set((bare.match(/@keyframes\s+([A-Za-z0-9_-]+)/g) || [])
    .map((m) => m.replace(/@keyframes\s+/, '')));
  const animated = new Set();
  for (const decl of bare.match(/animation(?:-name)?\s*:[^;}]+/g) || []) {
    for (const word of decl.split(':')[1].split(/[,\s]+/)) {
      /* Anything that is not a time, a count, a function or a keyword is a name. */
      if (/^ins-[A-Za-z0-9_-]+$/.test(word)) animated.add(word);
    }
  }
  const missing = [...animated].filter((name) => !frames.has(name));
  if (missing.length) fail(`animation names keyframes nothing defines: ${missing.join(', ')}`);

  /* Ambient motion is the one thing this design cannot afford — see the measurement
     in 18-motion.css. Two exceptions, both meaning "waiting". */
  const ALLOWED_INFINITE = ['ins-skel', 'ins-spin'];
  for (const decl of bare.match(/animation[^;}]*infinite[^;}]*/g) || []) {
    if (!ALLOWED_INFINITE.some((name) => decl.includes(name))) {
      fail(`an infinite animation outside the skeleton and the spinner: ${decl.trim()}`);
    }
  }

  return { open, declared: declared.size, used: used.size, frames: frames.size };
}

/* -------------------------------------------------------------------------- */

const files = (await readdir(SRC_CSS)).filter((f) => f.endsWith('.css')).sort();
if (!files.length) fail(`${SRC_CSS}: no stylesheets found.`);

const parts = [];
for (const file of files) {
  let css = await readFile(join(SRC_CSS, file), 'utf8');
  if (file.includes('tokens-dark')) css = expandDark(css, file);
  parts.push(css.trimEnd());
}

const pkg = JSON.parse(await readFile('package.json', 'utf8'));
const banner = `/*! Insiyab UI v${pkg.version} · ${pkg.homepage || 'insiyab'} · MIT (fonts: OFL 1.1, see fonts/LICENSE-*.txt) */\n`;

const css = banner + parts.join('\n\n') + '\n';
const js = banner + (await readFile(SRC_JS, 'utf8'));
const min = banner + minify(css);

const stats = verify(css);

if (problems.length) {
  console.error('\nbuild failed:\n');
  for (const p of problems) console.error(`  · ${p}`);
  console.error('');
  process.exit(1);
}

const kb = (s) => `${(Buffer.byteLength(s) / 1024).toFixed(1)} KB`;

if (CHECK) {
  console.log(`check passed · ${files.length} parts · ${stats.declared} tokens declared, ${stats.used} read`);
  console.log(`               insiyab.css ${kb(css)} · min ${kb(min)} · insiyab.js ${kb(js)}`);
  process.exit(0);
}

await rm(DIST, { recursive: true, force: true });
await mkdir(join(DIST, 'fonts'), { recursive: true });

await writeFile(join(DIST, 'insiyab.css'), css, 'utf8');
await writeFile(join(DIST, 'insiyab.min.css'), min, 'utf8');
await writeFile(join(DIST, 'insiyab.js'), js, 'utf8');

/* The fonts ship with the package because insiyab.css asks for them by relative
   path, and the licences ship because the OFL requires them to travel along. */
for (const font of await readdir(FONTS)) {
  await copyFile(join(FONTS, font), join(DIST, 'fonts', basename(font)));
}

console.log(`built dist/ · ${files.length} parts · ${stats.declared} tokens declared, ${stats.used} read`);
console.log(`  insiyab.css      ${kb(css)}`);
console.log(`  insiyab.min.css  ${kb(min)}`);
console.log(`  insiyab.js       ${kb(js)}`);
console.log(`  fonts/           ${(await readdir(join(DIST, 'fonts'))).length} files`);
console.log('\nnote: insiyab.js is shipped unminified — a hand-rolled JS minifier is not');
console.log('      worth the risk, and a real one would break the no-dependencies rule.');
