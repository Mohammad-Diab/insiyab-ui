<div dir="rtl">

# انسياب · Insiyab UI

**مكتبة واجهات بتصميم الزجاج المصنفر، عربية أولًا.**

*انسياب* هو الانتقال السلس بلا مقاومة — وهو ما تحاول المكتبة أن تكون، في شكل
الأسطح وفي تجربة البناء بها معًا.

المكتبة تفترض الاتجاه من اليمين إلى اليسار افتراضًا أصيلًا، لا إضافةً تُفعَّل: لا
ملف تجاوزات لـ `[dir=rtl]`، ولا مرحلة انعكاس. الخطوط العربية واللاتينية
مستضافة محليًا ومقسّمة بـ `unicode-range`، فلا تُحمّل صفحة عربية النصف اللاتيني
من خط عربي.

ملفان فقط، بلا أدوات بناء ولا إطار عمل:

</div>

```html
<link rel="stylesheet" href="insiyab.css">
<script src="insiyab.js"></script>
```

<div dir="rtl">

لا حاجة إلى أي استدعاء بعد ذلك.

</div>

---

> **Status: 0.2.0.** The component vocabulary is in and the demo page shows all
> of it, in Arabic. Still to come: the application shell (sidebar + topbar), and
> then the React and Jinja wrappers. See [IDEA.md](IDEA.md) for the full plan and
> the reasoning behind every decision.
>
> This README is still English-first. The docs, demo and marketing lead in Arabic
> by design — the demo page already does — and the rest is a writing job worth
> doing properly once the vocabulary has fully settled rather than twice.

## Getting started

Download or copy three things next to each other, and reference the first two:

```
insiyab.css
insiyab.js
fonts/            ← 16 .woff2 files + their licences
```

```html
<!doctype html>
<html lang="ar" dir="rtl">
<head>
  <meta charset="utf-8">
  <meta name="viewport" content="width=device-width, initial-scale=1">

  <link rel="stylesheet" href="insiyab.css">
  <script src="insiyab.js"></script>
</head>
<body>
  <div class="ins-glass" style="padding:20px">مرحبًا</div>
</body>
</html>
```

That is the whole setup. Three things are worth knowing about it:

**Put the script in `<head>`, and do not add `defer`.** The theme and the sidebar
state are restored from `localStorage`, and that has to happen *before the first
paint* or every page load flashes the wrong theme. The script stamps `<html>` the
moment it is parsed and waits for `DOMContentLoaded` for everything else. It still
works deferred or at the end of `<body>` — you just get the flash back.

**There is no init call.** Events are delegated from the document, so a
`data-ins-*` element added to the page an hour later works with no help.
`Insiyab.init()` exists only for the few things that build DOM rather than listen
to it.

**The CSS is useful on its own.** Every static component works from the
stylesheet alone; the JavaScript is purely additive, for the parts that move.

### The fonts

`insiyab.css` asks for `fonts/` by relative path, so keep the folder beside the
stylesheet. Drop in only the CSS and you still get a working page — just the
fallback stack instead of the typography.

They are self-hosted rather than loaded from a CDN on purpose: a blocked or slow
font CDN means a page rendered in a fallback serif, and that happens on the
connections this library is published for. All three faces are
[SIL Open Font License 1.1](fonts/) — Tajawal, Inter and Cause — and their
licences ship in `fonts/` because the OFL requires the licence to travel with the
font.

## Theming

### One colour

```html
<html data-ins-primary="#0F766E">
```

```js
Insiyab.brand('#0F766E');
```

Either one derives and applies the rest: the rgb triplet, the gradient end-stop,
the readable text shade and both page washes. The whole system tints from there —
shadows, glows, tints, focus rings, the page ground.

The label colour on a filled surface is **measured, not assumed**. Hardcoding
white is a real failure for a mid-tone brand: white on a mid gold is about 2.4:1.
So `brand()` takes whichever of white and near-black actually measures better,
and then shifts the gradient's second stop *away* from that label — which means
the whole fill carries its own label by construction rather than by luck.

If you are using the CSS without the JavaScript, set the six brand tokens by hand
instead. Nothing else changes.

### Light and dark

The library follows the operating system by default. An explicit choice wins over
it and is remembered:

```html
<button data-ins-theme-toggle>تبديل الوضع</button>
<button data-ins-theme-toggle="system">اتبع النظام</button>
```

```js
Insiyab.theme();          // 'light' | 'dark' — what the user is actually seeing
Insiyab.theme('dark');    // set, and remember
Insiyab.theme('system');  // forget, and follow the OS again
Insiyab.toggleTheme();
```

Dark is a second palette that was designed as one, not an inversion of the first.
Naive inversion of white-alpha glass gives milky grey panels with glaring white
rims; here the rim drops from 85% to 10%, the frost desaturates from 140% to
120%, the shadows go darker *and* stronger, the tints roughly double, and the
brand's readable shade flips direction entirely.

## What is in it

Run `node serve.mjs` and open the demo — every component below is on that one
page, in Arabic, in both themes.

| Group | Classes |
|---|---|
| **Surfaces** | `.ins-glass` · `.ins-glass-inner` · `.ins-card` · `.ins-panel` (`-head`, `-title`, `-ico`, `-body`, `-foot`, `-note`, `--alert`, `--open`) |
| **Data** | `.ins-table` (+ `-wrap`) · `.ins-stat` (6 tones, 3 sizes) · `.ins-pill` · `.ins-badge` · `.ins-money` · `.ins-num` · `.ins-avatar` |
| **Input** | `.ins-field` · `.ins-label` · `.ins-hint` · `.ins-input` · `.ins-select` · `.ins-textarea` · `.ins-check` · `.ins-switch` (+ `--card`, `-grid`) · `.ins-seg` · `.ins-search` |
| **Buttons** | `.ins-btn` × `--primary` `--secondary` `--ghost[-danger/-success/-warning/-info]` `--success` `--danger` `--warning` `--info` `--bare`, × `--sm` `--lg` `--icon` `--full` `--lift` |
| **Feedback** | `.ins-alert` (notched) · `.ins-toast` · `.ins-empty` · `.ins-skel` · `.ins-spinner` |
| **Overlay** | `.ins-dialog` (native `<dialog>`) · `.ins-pop` (native `<details>`) |
| **Layout** | `.ins-grid` · `.ins-toolbar` · `.ins-searchbar` |
| **Ground** | `.ins-orbs` / `.ins-orb-1..3` — injected for you; opt out with `data-ins-orbs="off"` |

Three things worth knowing about how these behave:

**Glass inside glass de-nests itself.** The inner surface drops its border,
shadow, sheen and blur automatically. A second frost over an already frosted
ground is what turns this design language to fog, so it is a rule at zero
specificity rather than a class anyone has to remember.

**The interactive components are built on platform elements.** The dialog is a
real `<dialog>` — Escape closes it, focus is trapped, and the top layer puts it
above every stacking context without a single `z-index`. The popover is a real
`<details>` — it opens, closes on Escape and is keyboard reachable with no script
at all. The script only adds click-outside-to-close.

**Everything is namespaced** — classes `ins-`, tokens `--ins-*`, attributes
`data-ins-*`. Insiyab can be dropped into a page that already has Bootstrap or
Tailwind without a collision. (The dialog being `.ins-dialog` rather than
`.modal` is not politeness: Bootstrap's `.modal { display: none }` outranks the
UA's `dialog[open]`, so a dialog named that way opens, takes the top layer, and
paints nothing.)

## Attributes

Everything below works as markup, with no JavaScript in your page:

```html
<button data-ins-theme-toggle>…</button>          <!-- toggle light/dark -->
<button data-ins-theme-toggle="system">…</button> <!-- follow the OS again -->
<button data-ins-fx>…</button>                    <!-- reduce effects -->
<button data-ins-sidebar>…</button>               <!-- collapse the sidebar -->
<button data-ins-dialog="#confirm">…</button>     <!-- open a dialog -->
<button data-ins-dialog-close>…</button>          <!-- close the one it is in -->
<button data-ins-toast="تم الحفظ" data-ins-tone="ok">…</button>
```

On `<html>`: `data-ins-primary="#0F766E"`, `data-ins-theme="dark|light"`,
`data-ins-orbs="off"`, `data-ins-fx="off"`, `data-ins-scrollbar="lead"`.

## JavaScript API

The escape hatch, not the front door — prefer the attributes above.

```js
Insiyab.version                       // '0.1.0'
Insiyab.theme(mode?)                  // 'dark' | 'light' | 'system'
Insiyab.toggleTheme()
Insiyab.brand(hex?)                   // derive and apply a brand colour
Insiyab.toast(message, tone?)         // 'ok' | 'bad' | 'warn' | 'info'
Insiyab.dialog(target, action?)       // 'open' | 'close' | omit to toggle
Insiyab.sidebar(state?)               // 'open' | 'collapsed' | 'toggle'
Insiyab.init(scope?)                  // re-scan DOM you built yourself
Insiyab.define(name, fn)              // add your own builder to that scan
Insiyab.scrollTop()                   // reads whichever element is scrolling
Insiyab.color.derive(hex, theme)      // the full token set, without applying it
Insiyab.color.contrast('#fff','#000') // WCAG ratio
Insiyab.color.labelFor(hex)           // '#ffffff' or '#111111', measured
Insiyab.color.shift(hex, amount)      // toward black (< 0) or white (> 0)
```

Two events on `document`, both with a `detail`:

```js
document.addEventListener('ins:theme', (e) => console.log(e.detail.theme));
document.addEventListener('ins:sidebar', (e) => console.log(e.detail.state));
document.addEventListener('ins:seg', (e) => console.log(e.detail.value));
```

### RTL scrollbars

Chromium pins the *viewport's* scrollbar to the right regardless of `direction`,
so an RTL page gets it on the wrong side. The only fix is to stop scrolling the
viewport, which is a page-wide decision with real costs on mobile — so it is
opt-in:

```html
<html dir="rtl" data-ins-scrollbar="lead">
```

If you use it, read scroll offsets with `Insiyab.scrollTop()`: an element
scroller's `scroll` event never reaches `window`.

## Development

Node 18+ for the build; nothing else, and no dependencies at all.

```sh
node build.mjs           # src/css/*.css + src/js → dist/
node build.mjs --check    # build and verify, write nothing
node serve.mjs            # then open http://localhost:4173
```

**Serve the demo, do not open the file.** Over `file://` the self-hosted faces
count as cross-origin, the browser blocks them, and the typography silently falls
back to a system serif — which hides the most visible part of any change.

The stylesheet is authored in parts under `src/css/` and concatenated in filename
order. One hand-maintained 4,000-line file is unmaintainable by month three; a
build script is not. The build also:

- duplicates the dark palette into a `prefers-color-scheme` variant, so it is
  written once rather than twice;
- fails if any `--dk-*`, `--bs-*` or other pre-rename token survives;
- fails if any `var()` reads a token nothing declares — an undefined custom
  property invalidates its whole declaration silently, which is how three
  shadows in the original sheet were dead for months.

## Licence

MIT for the code — see [LICENSE](LICENSE). The bundled fonts are SIL Open Font
License 1.1, with their own licences in [`fonts/`](fonts/).
