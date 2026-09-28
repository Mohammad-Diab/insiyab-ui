# Insiyab · انسياب

**A light UI library with a frosted-glass design language.**

*Insiyab* — انسياب — is Arabic for **flow**: the quality of something moving
smoothly and without resistance. It is what the library is trying to be, twice
over — how the surfaces look, and how it feels to build with.

For projects that should look finished on day one, without adopting a framework,
learning a theme API, or configuring anything.

---

## Why it exists

Most UI libraries are built to please everyone, so they end up neutral: a theme
API, four hundred variables, and a default look nobody would choose on purpose.
You spend the first day configuring it into having an opinion.

Insiyab ships with the opinion already in it. One look, one easing curve, one
radius scale, one brand variable. You pick a color and start building.

The design language isn't new — it already exists across my own projects, in
React, in server-rendered templates, and in a zero-build single HTML file. It has
proven it survives all three without changing its mind about what it looks like.
This is the extraction, so it never gets rebuilt a fourth time. Screenshots of
what that looks like today are kept locally in `reference/` — 12 MB of images
of other projects, so they are not committed.

**Who it's for:** me first — the library I reach for whenever I start anything.
It stays opinionated: if I ever disagree with a decision in it, I change the
library, not the app.

**Who it's published for: Arabic-speaking developers.** That follows from the
product rather than being bolted onto it. The library is RTL-first, so the people
it helps most are the ones every other library helps least — devs building Arabic
interfaces who currently fight Material, Bootstrap or Tailwind for mirroring,
bidi text, numerals and a font stack that doesn't make Arabic look like an
afterthought. There is no well-made Arabic-first component library. That is the
gap.

This is a positioning choice, not a technical limit. **RTL-first is a superset:**
a library built on logical properties works perfectly in LTR, so the English
market stays open — it just isn't the one being designed for. Code, class names
and APIs stay in English, as all code does. Docs, demo and marketing lead in
Arabic.

---

## The design language: frosted glass

Four stacked layers, and nothing else:

1. **Frost** — `backdrop-filter: blur(24px) saturate(140%)`. The saturate is not
   decoration; it is what stops the tinted background going gray.
2. **Fill** — a *diagonal* translucent white gradient at 145°, never a flat rgba.
3. **Rim** — `1px solid rgba(255,255,255,.85)`, top edge brightened to `.95`. A
   near-white hairline, never a gray border.
4. **Bevel + sheen** — `inset 0 1px 0 rgba(255,255,255,.9)` plus a 130° specular
   streak on a pseudo-element.

Shadow is soft, large, and cool-tinted — `0 8px 32px rgba(15,23,42,.09)`. Never
neutral black. **No noise, no grain, no iridescence anywhere** — the liquid
quality is blur + saturate + the one diagonal sheen.

If only five things survive: the 14/22px radii, `blur(24px) saturate(140%)`, the
bevel, the cool shadow, the 145°/135° angles. That is the soul.

### The scales

- **Radii** `7 / 10 / 14 / 22 / 999` — progression ≈ ×1.45, not ×2. 14px as the
  default medium is the single biggest source of the perceived softness.
- **Alpha ladder** — the more "on top" a surface is, the more opaque:
  panel `.58` → hover `.78` → focused field `.90` → popover `.94`.
- **Blur ladder** — panels 24px, bars and pills 16px, modal backdrop only 2px.
- **Semantic triad** — every state is *dark saturated text* + *10–12% tint of the
  same hue* + *18–22% border of the same hue*. This is why badges, alerts and
  glass read as one family instead of three.
- **Motion** — one easing, `cubic-bezier(.22, 1, .36, 1)`. Enter `.24s`, exit
  `.18s`. Deliberately asymmetric. This is the part the name is about.

---

## What makes it different

### RTL-native, not RTL-supported

The differentiator against every mainstream kit. Right-to-left is the default
assumption, not a plugin you enable:

- Logical properties throughout — no `[dir=rtl]` override sheet, no mirroring pass.
- Arabic, Latin and display faces self-hosted and split by `unicode-range`, so an
  Arabic page never downloads the Latin half of an Arabic font.
- Self-hosted on purpose: a blocked font CDN means a page rendered in a fallback
  serif, and that happens on the connections some of this runs on.
- Bidi text, mixed-script numerals and RTL-aware icons handled in the library, not
  left to the app.

If you build anything in Arabic, Hebrew, Farsi or Urdu, every other library makes
this your problem. This one doesn't. The name is part of that promise.

### One brand variable

Everything tints from `--ins-primary` and its rgb triplet. That is the entire
theming story — no theme object, no provider, no build step. Set one color and
the whole system retunes: shadows, glows, tints, table headers, focus rings.

Already proven across three very different identities from a single unforked
sheet. **New project = pick a color, done.**

It also works as a *runtime* feature, not just a stylesheet convention: one of the
existing apps serves its brand tokens in the login response and writes them onto
the document on sign-in. Worth shipping as a documented pattern.

### Light and dark, designed rather than inverted

Naive inversion of white-alpha glass gives milky gray blobs with glaring outlines.
Dark is a second palette that was designed as one, not a filter over the first.

### Accessibility already paid for

Not a roadmap item. Contrast failures already found and fixed, focus rings as a
token rather than an afterthought, correct ARIA on the compound controls, and the
interactive parts degrade to working HTML when JavaScript doesn't run.

---

## What's in it

The component vocabulary — named parts instead of `<div class="card p-3">`
written thirty times:

**Shell** — sidebar + topbar app frame, responsive, RTL-aware
**Surfaces** — panel, card, glass, modal, popover, wizard
**Data** — table, stat tile, pill, badge, money, pagination
**Input** — field, select, autocomplete, checkbox, radio tile, segmented control,
switch, date picker, search
**Feedback** — toast, alert, empty state, skeleton, spinner
**Chrome** — avatar, action menu, user menu, day/night switch

Plus the states most kits skip and every real app needs: empty, loading, error,
and offline.

**Not in it** — routing, data fetching, state management, form validation,
charts. Those belong to the app.

---

## How it ships

**Two files. Nothing else.** That is the whole delivery story, and it is the
feature, not a limitation — the audience this is published for is often the one
for whom a mandatory Node toolchain is the reason a library never gets tried.

```html
<link rel="stylesheet" href="insiyab.css">
<script src="insiyab.js"></script>
```

Decided, so it stops being re-litigated per component:

1. **No init call.** The two tags are enough. `Insiyab.init()` exists only to
   re-scan DOM you added after load. Zero commands beats one command: a required
   init is a dead page for everyone who forgets it, and they blame the library.
2. **The script goes in `<head>`, synchronously.** Theme and sidebar state are
   restored from `localStorage`, and that has to land *before first paint* or
   every page load flashes the wrong theme. A deferred script cannot do it. So
   `insiyab.js` stamps `<html>` at parse time and defers the rest of its wiring
   to `DOMContentLoaded` — still one file, one tag, no flash, and no inline
   snippet for anyone to copy.
3. **The primary API is HTML attributes, not JavaScript.**
   `<button data-ins-modal="#confirm">` — you write markup, the library wires
   it. A beginner needs no JS at all. `Insiyab.modal()` is the escape hatch, not
   the front door.
4. **The CSS is useful alone.** Every static component works from the stylesheet
   by itself; the JS is purely additive, for the parts that move. Two rungs: one
   file for everything that doesn't move, add the second for what does.
5. **Classes are prefixed `ins-`,** tokens `--ins-*`, attributes `data-ins-*`.
   One namespace, no collisions — so Insiyab can be dropped into a page that
   already has Bootstrap or Tailwind, which is how it gets adopted at all.
6. **You get a build step; the user doesn't.** The stylesheet is authored in
   parts under `src/css/` and concatenated on release. One hand-maintained
   4,000-line file is unmaintainable by month three; a ~30-line build script is
   not. The consumer's zero-build path is untouched.

`insiyab.css` expects a `fonts/` folder beside it — the faces are self-hosted on
purpose (see above), so "one CSS file" is one CSS file plus its fonts.
Base64-inlining them to force the count to one would add hundreds of KB to the
stylesheet and defeat the `unicode-range` splitting that exists to avoid exactly
that. Drop in only the CSS and you still get a working page, just not the
typography.

### Framework wrappers

| Package | Status |
|---|---|
| `insiyab` | **This is the library.** One CSS file, one JS file, no build. |
| `@insiyab/react` | Built, in `packages/react`. TypeScript, React 18 or later. |
| `insiyab` (PyPI) | Built, in `packages/jinja`: Jinja macros, with the library's files in the package. |

Deferred until the core was done: a wrapper written against an unfinished class
vocabulary has to be rewritten when the vocabulary settles. They live in this
repository, under `packages/`, so a renamed class breaks a wrapper's tests in the
same commit, and they share one parity test: every docs example, built with the
wrapper, has to come out the same as the example.

**The CSS stays the source of truth.** The wrappers are thin
covers over the same classes, never re-implementations — that rule is what stops
three entry points becoming three libraries.

---

## Principles

1. **Opinionated by design.** One look. No theme API beyond the brand color.
2. **Small enough to hold in your head.** A library you have to look up is one
   you'll re-invent instead.
3. **Zero-build must always work.** If a feature can't survive as plain CSS, it
   doesn't go in the core.
4. **Vendored, never forked.** An app that needs different behavior gets a token
   or a modifier upstream — it does not edit the library in place.
5. **Breaking changes are allowed.** Bump the major, migrate when convenient.

---

## The name

**انسياب** / **Insiyab** — Arabic for *flow, fluidity, streamlining*.

It names the aesthetic and the motion system at once. The surfaces are liquid
glass; the motion is one easing curve with a deliberately asymmetric enter and
exit. Both are insiyab. And an Arabic name on a library whose headline feature is
Arabic-first is coherent rather than decorative — it says what the library is for
before anyone reads a line of the README.

### Display name: **Insiyab UI**

The `UI` suffix says what the thing is in one syllable, which is what a library
name should do. It also carries the name safely into English-language contexts —
a package listing, a GitHub topic, a conference slide — without the word having
to explain itself there.

To the audience this is actually published for, **انسياب needs no explanation at
all.** It is an ordinary Arabic word with a refined, literary quality: smooth,
unforced movement. It reads as *elegant* to a native speaker, not as a foreign
coinage — which is precisely the register a design library wants, and exactly
what no English name on the shortlist could have delivered to this market.

The wordmark pairs the two: **انسياب** as the mark, *Insiyab UI* as the Latin
lockup. Arabic letterforms carry craft that Latin sans does not, so the mark does
the premium work and the positioning work at the same time.

### Spelling is fixed: `insiyab`

Transliteration is the one real cost of an Arabic name, so it is decided once
here rather than drifting. **`insiyab`** is canonical everywhere — npm, GitHub,
the CSS prefix, the docs, the domain. Never `insiyaab`, `insiab` or `ensiab`.

Single `a`, not doubled, deliberately: doubled vowels read as a *pronunciation
guide* rather than a name. Compare Salam/Salaam, Kitab/Kitaab — the single-vowel
form looks like a brand, the doubled form looks like a transliteration. Every
Arabic-origin consumer brand that romanizes well does the same: Careem, Talabat,
Anghami, Namshi, Ounass. None of them double a vowel.

### Namespace

| | Status |
|---|---|
| npm `insiyab` | **free** — the bare name, no suffix, no scope workaround |
| npm `@insiyab/react`, `@insiyab/jinja`, `@insiyab/core` | **free** |
| `github.com/insiyab-ui` | **free** — the org |
| `github.com/insiyab` | taken |

The bare GitHub org is gone, which costs nothing: `insiyab-ui` is exactly the
pattern `radix-ui`, `chakra-ui`, `ark-ui` and `base-ui` all use. The npm package
stays bare — `npm i insiyab`, never `insiyab-ui`.

`insiyaab` and `insiab` are also free on npm. Worth parking later as defensive
placeholders pointing at the real package, because people *will* mistype this.

**Folder: `insiyab`** → `D:\Personal\2026 Apps\insiyab`

Token prefix: **`--ins-*`**.

---

## First steps

The base is `ussd-topup-systems/app/static/css/ui.css` — 5,909 lines, 78
`--dk-*` tokens, the only one of the three with dark mode, the full tone system
and the contrast corrections. The screenshots in `reference/` (kept locally, not
committed) show why it,
and not the React original, is the right starting point.

1. **Foundation first** — fonts, tokens light and dark, reset, the glass
   primitives. Everything else is written against these, so they get renamed and
   de-frameworked once, at the start.
2. **Strip the framework coupling.** This is deeper than deleting lines: `ui.css`
   does not merely coexist with Bootstrap, it *retunes* it through 88 `--bs-*`
   variables, and the templates load Bootstrap's CSS and JS from a CDN. Those
   have to be **replaced** with the library's own reset and focus ring, not
   dropped — principle 3 says zero-build must always work.
3. **Cut the app-specific tail.** Roughly the last 1,600 lines are USSD's own:
   customer app, bulk row composer, equation, control plane, ticket thread,
   dialer liveness, `--net-*` carrier colors, impersonation banner. The test for
   keeping a block: *would I want this in the next thing I build?* This makes the
   extraction a curated rewrite, not a copy-and-rename.
4. **Keep the commentary.** The source explains *why* at nearly every section,
   with measured numbers — contrast ratios sampled off renders, the 47%-of-a-core
   orb-drift finding, the scrollbar pixel measurements. That reasoning is the
   expensive part and it does not survive a rewrite twice.
5. **Grow the demo page alongside, from the first component.** A library you
   can't see all of on one page is a library you'll forget you have — and for
   anyone else, that page is the entire pitch.
6. Then the React wrapper, then the Jinja macros.
