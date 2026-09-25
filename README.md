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

> **Status: 0.6.0.** The core is in place: the application shell, the layout,
> type and utility layer, the components an application form or dashboard needs
> (pickers, steps and confirmation included), and a motion layer across all of
> them. The demo is a documentation site — one page per topic, in Arabic, inside
> the shell, with every example's markup printed under it. Ten plugins: the
> Hijri calendar, the command palette, the one-time code, the phone number, file
> upload, scrollspy, the timeline, the tree, the colour picker and the carousel.
> Still to come: the React and Jinja wrappers. See
> [IDEA.md](IDEA.md) for the plan and the reasoning behind every decision.
>
> This README is still English-first. The docs, demo and marketing lead in Arabic
> by design — the demo site already does — and the rest is a writing job worth
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

Pressing a `data-ins-theme-toggle` switch shows the change: the new palette grows
as a circle out of the switch until it covers the page. It is a View Transition,
so the browser animates two snapshots of the page rather than a transition on
every element. It costs the same whatever is on screen, and it covers gradients,
shadows and masks that a colour transition cannot. Browsers without View
Transitions, reduced motion and `data-ins-fx="off"` all get the instant flip, and
so does `Insiyab.theme()`, which has no switch for the circle to start from.

## What is in it

Run `node serve.mjs` and open the demo — every component below has its own page
there, in Arabic, in both themes, with the markup of each example under it.

| Group | Classes |
|---|---|
| **Surfaces** | `.ins-glass` · `.ins-glass-inner` · `.ins-card` · `.ins-panel` (`-head`, `-title`, `-ico`, `-body`, `-foot`, `-note`, `--alert`, `--open`) |
| **Data** | `.ins-table` (+ `-wrap`; a selected row via `aria-selected="true"`, `aria-current` or `.is-selected`) · `.ins-stat` (6 tones, 3 sizes) · `.ins-pill` · `.ins-badge` · `.ins-money` · `.ins-num` · `.ins-avatar` · `.ins-list` (`-item`, `-text`, `-title`, `-desc`, `-end`; items can be links) |
| **Input** | `.ins-field` · `.ins-label` · `.ins-hint` · `.ins-input` / `.ins-select` (`--sm`, `--lg`) · `.ins-textarea` · `.ins-check` (indeterminate via `data-ins-indeterminate`) · `.ins-switch` (+ `--card`, `-grid`) · `.ins-seg` · `.ins-search` · `.ins-input-group` + `.ins-addon` · `.ins-password-toggle` |
| **Pickers** | `.ins-combo` (`-list`, `-option`, `-empty`) · date and date range via `data-ins-date` (+ `.ins-date-btn`, `.ins-cal`) · `.ins-range` · number stepper `.ins-spin` in an `.ins-input-group` · `.ins-tile` (`-grid`, `-title`, `-desc`) for radio and checkbox cards |
| **Validation** | `aria-invalid="true"` · `:user-invalid` · `.ins-error` · `.ins-input--bad` / `.ins-select--bad` · `.ins-success` · `.ins-input--ok` · `.ins-req` · `form[data-ins-validate]` |
| **Buttons** | `.ins-btn` × `--primary` `--secondary` `--ghost[-danger/-success/-warning/-info]` `--success` `--danger` `--warning` `--info` `--bare`, × `--sm` `--lg` `--icon` `--full` `--lift` · toggle buttons via `data-ins-toggle` (`aria-pressed`) · `.ins-btn-group` (`--sm`; takes a split-button `.ins-pop`) · `.ins-close` (`--sm`, `--bare`) |
| **Feedback** | `.ins-alert` (notched) · `.ins-toast` · `.ins-empty` · `.ins-progress` (native `<progress>`; `--sm`, `--lg`, `--ok`, `--warn`, `--bad`; indeterminate without `value`) · `.ins-ring` (`--lg`) · `.ins-skel` (`--text`, `--title`, `--circle`, `--block`) · `.ins-spinner` (`--sm`) · `.ins-loading` |
| **Overlay** | `.ins-dialog` (native `<dialog>`, `--sm`, `--lg`) · `.ins-drawer` (`--end`, `--bottom`) · `.ins-pop` (native `<details>`, `--start`; `-label`, `-item`, checkable items via `role="menuitemcheckbox|menuitemradio"`) · `.ins-pop-body--card` (+ `.ins-pop-title`, `.ins-pop-text`) · `.ins-tooltip` via `data-ins-tip` · confirmation via `data-ins-confirm` or `Insiyab.confirm()` |
| **Navigation** | `.ins-page-head` (`-text`) · `.ins-page-title` · `.ins-page-sub` · `.ins-page-actions` · `.ins-breadcrumb` · `.ins-tablist` · `.ins-tab` · `.ins-tabpanel` · `.ins-pagination` · `.ins-page` (`--prev`, `--next`) · `.ins-page-gap` · `.ins-navbar` (`--static`; `-brand`, `-toggle`, `-menu`, `-link`, `-end`) · `.ins-accordion` · `.ins-collapse` (`-body`) · `.ins-steps` (`-item`, `-label`) · `.ins-wizard` (`-panel`, `-foot`, `-finish`) |
| **Shell** | `.ins-shell` · `.ins-shell-side` (brand, `-group`, `-link`, `-link-badge`, `-bottom`) · `.ins-topbar` · `.ins-zone` · `.ins-island` (`--icon`, `--title`, `--brand`) |
| **Layout** | `.ins-container` (`--narrow`, `--wide`) · `.ins-stack` · `.ins-row` · `.ins-cols-{2,3,4,6,12}` (+ `--fixed`) · `.ins-span-{2…12,full}` · `.ins-grid` · `.ins-toolbar` · `.ins-searchbar` |
| **Type** | `.ins-display` · `.ins-h1`–`.ins-h4` · `.ins-lead` · `.ins-eyebrow` · `.ins-prose` · `.ins-kbd` · `.ins-divider` (`--start`, `--v`) |
| **Utilities** | spacing `ins-{gap,m,mt,mb,ms,me,mx,my,p,pt,pb,ps,pe,px,py}-{0,1,2,3,4,5,6,8,12}` · flex `ins-flex`, `-wrap`, `ins-grow`, `ins-items-*`, `ins-justify-*` · text `ins-text-{start,center,end,xs…xl,ok,warn,bad,info,brand}`, `ins-fw-*`, `ins-muted`, `ins-truncate`, `ins-clamp-{2,3}` · scroll `ins-scroll-smooth` · visibility `ins-hide-{below,above}-{sm,md,lg}`, `ins-hide-print`, `ins-print-only` |
| **Motion** | `[data-ins-reveal]` · `.ins-hoverable` · `.ins-anim-rise` · `.ins-anim-pop` · `.ins-anim-slide` |
| **Ground** | `.ins-orbs` / `.ins-orb-1..3` — injected for you; opt out with `data-ins-orbs="off"` |

Three things worth knowing about how these behave:

**Glass inside glass de-nests itself.** The inner surface drops its border,
shadow, sheen and blur automatically. A second frost over an already frosted
ground is what turns this design language to fog, so it is a rule at zero
specificity rather than a class anyone has to remember.

**The interactive components are built on platform elements.** The dialog is a
real `<dialog>` — Escape closes it, focus is trapped, and the top layer puts it
above every stacking context without a single `z-index`. The drawer is the same
element with an edge. The popover is a real `<details>`, so it opens and is
keyboard reachable with no script at all. **It does not close on Escape by
itself.** This README used to say it did, and testing showed a bare `<details>`
ignores Escape. Escape, the arrow keys, closing on choice and flipping at the
viewport edge all come from the script.

**The shell is two ideas.** The top bar is *islands, not a bar*: it is
transparent and holds floating glass pills, and frosts into a solid bar only once
the page scrolls under it. And the sidebar is glass rather than a brand-tinted
column — a tinted column becomes the second most saturated thing on screen after
the primary button, and then competes with it. Below 900px the sidebar becomes an
off-canvas drawer opened by `:target`, so it works with no JavaScript at all.

**Everything is namespaced** — classes `ins-`, tokens `--ins-*`, attributes
`data-ins-*`. Insiyab can be dropped into a page that already has Bootstrap or
Tailwind without a collision. (The dialog being `.ins-dialog` rather than
`.modal` is not politeness: Bootstrap's `.modal { display: none }` outranks the
UA's `dialog[open]`, so a dialog named that way opens, takes the top layer, and
paints nothing.)

## Layout, type and utilities

The plain-CSS layer every page sits on. Four decisions shape it:

**The grids collapse on their own.** A `.ins-cols-*` grid holds its columns on a
desktop, drops to two at 900px (the same line at which the shell's sidebar
becomes a drawer, with spanned cells going full width), and to one on a phone.
So there are no per-breakpoint column classes to learn. `.ins-cols--fixed` opts
out, for grids whose column count *is* the content.

```html
<div class="ins-cols-12">
  <main class="ins-span-8">…</main>
  <aside class="ins-span-4">…</aside>
</div>
```

**The primitives own the gaps.** `.ins-stack`, `.ins-row`, `.ins-grid` and
`.ins-cols-*` space their children with `gap` and clear the children's own outer margins, so a
panel's 1.25rem doesn't stack on top of the gap. A margin utility on a child still
wins. In a stack, `ins-mt-auto` pushes an item to the bottom, and in a row,
`ins-ms-auto` pushes it to the far end.

**Utilities are logical, regular and quiet.** Step N is N × 4px on every family.
`s` and `e` are the inline start and end, so `ins-ms-4` is a right margin in
Arabic and a left one in English, from the same class, and there is no `ml` or
`mr`. They load after every component, so they win without `!important`. The one
exception is `[hidden]`: every component sets a `display`, which would otherwise
beat the attribute, so `<button class="ins-btn" hidden>` would still show.

**Written content is scoped.** `.ins-prose` styles paragraphs, lists, quotes,
code, tables and figures inside it and touches nothing outside it, because rules
on bare `ul` or `p` would restyle the host page's navs and menus. Inside it, Arabic
is never tracked and nothing is set in italic. None of the faces ships an italic,
and a synthesised one leans Arabic the wrong way, so emphasis is colour. Code is an
isolated LTR island in either direction.

**Smooth scrolling is opt-in.** Put `ins-scroll-smooth` on `<html>` and anchor
links glide to their target instead of jumping, or put it on any box that scrolls
by itself, such as a table wrap or a dialog body. It is off by default because a
glide suits a long document and slows down a page whose links jump between
distant screens. Reduced motion and `data-ins-fx="off"` turn it off again. With
or without it, an anchor inside the shell stops below the sticky top bar, not
behind it.

```html
<html class="ins-scroll-smooth">
<div class="ins-table-wrap ins-scroll-smooth">…</div>
```

Breakpoints, for `ins-hide-below-*` and `ins-hide-above-*`: **sm** 34rem ·
**md** 900px · **lg** 1200px. Each "above" class is the exact negation of its
"below" query, so no width shows both or neither.

## Pickers, steps and confirmation

The components that take a choice from the person. Each one starts from a
platform element and degrades to it.

**The date field is a native date input until the script arrives.** Mark it
`data-ins-date` inside an `.ins-input-group`:

```html
<div class="ins-input-group">
  <input class="ins-input" type="date" data-ins-date name="issued" value="2026-09-23">
</div>
```

The script turns it into a text field showing the date in the page's language,
adds the calendar button, and moves `name` to a hidden input that holds the ISO
string. So the server receives exactly what the native field would have sent. The
week starts where the locale says it does, which for Arabic is Saturday. The
digits are Latin, like every other figure in the library, unless the page asks
for others with `lang="ar-u-nu-arab"`. A typed date is read as ISO or in the
locale's own day/month order, in either set of digits. `min` and `max` still
apply, with messages in the page's language. The arrow keys move through the grid
in the page's direction, so in Arabic the left arrow is the next day.

For a range, point the two fields at each other with `data-ins-date-end` and
`data-ins-date-start`. The end cannot be set before the start. A new start that
falls after the end clears the end and opens its calendar, so moving a range a
week later takes two picks, not three.

**The autocomplete matches Arabic the way it is typed**, not the way it was
stored. Hamza seats, harakat and the tatweel are ignored, ة matches ه, ى matches
ي, and Arabic-Indic digits match Latin ones. «ادلب» finds إدلب and «اللاذقيه» finds
اللاذقيّة. The options are plain markup, and a hidden input receives the chosen
option's `data-value`:

```html
<div class="ins-combo">
  <input class="ins-input" aria-label="المدينة">
  <input type="hidden" name="city">
  <ul class="ins-combo-list">
    <li class="ins-combo-option" data-value="alp">حلب</li>
    <li class="ins-combo-empty">لا توجد مدينة بهذا الاسم</li>
  </ul>
</div>
```

**A wizard is a form in steps.** Next checks only the current step's fields,
with the browser's own validation, before it moves on. So a required field stops
the person on its own step, not at the end. Enter in a field means Next, and
focus moves to the step that arrives. Without the script it is one long form with
the submit button at the bottom, which still works.

**A confirmation is one attribute.** `data-ins-confirm="حذف العملية؟"` on a link,
a submit button or any `data-ins-*` control asks first. On a yes, the original
click is replayed, so the link still navigates, and the button still submits its
form with its own name and value. Focus lands on Cancel, because a confirmation
that defaults to the destructive answer gets pressed through with Enter. A
control already styled as dangerous gets a red confirm button without being told.

## Motion

Every component has an entrance and a hover, and all of it obeys one rule:
**nothing loops.** An entrance, a hover, a press — each pays its cost for its own
duration and stops. The two exceptions are the skeleton and the spinner, and both
mean "waiting".

That is a measurement, not a preference. Three decorative orbs drifting on a 14s
infinite animation cost **47% of a core, forever**, on a page where nothing else
was happening. The obvious diagnosis — re-blurring three blurred layers 60 times
a second — was wrong: with the blur removed and the drift left running the page
still burned 44.7%. What actually costs is the **backdrop-filter surfaces above
them**, because a frosted surface re-blurs whatever is behind it whenever that
changes, so anything moving anywhere underneath re-blurs most of the page every
frame. Stopping the drift took it to 4.8%.

So in a frosted design the choice is never cheap motion versus expensive motion —
it is **motion or frost**, and the frost is the design. `node build.mjs` refuses
to emit a stylesheet containing any `infinite` animation other than those two.

A second rule sits beside it: **nothing that cannot be clicked moves under the
pointer.** A static icon, a status pill, an avatar beside a name, a heading, a
panel: none of them react to hover, because anything that moves under the pointer
reads as a promise of a click. Each of those gets its hover only when it is itself
a link or a button, or when a page opts it in with `.ins-hoverable`.

A few of the gestures, so the vocabulary is legible:

- **Surfaces** rise toward the light — lift, longer shadow, brighter rim. The
  130° sheen deliberately does *not* move: it is the signature, and a signature
  that moves when you point at it is a gimmick.
- **A table row** fills on hover, and that is all. The 3px brand marker on its
  leading edge belongs to a selected row: `aria-selected="true"`, `aria-current`
  or `.is-selected`. It's the same marker the sidebar's selected item carries, with
  the same meaning: *this one is chosen*. It scales from the centre rather than
  sliding, because a bar sliding the length of a wide row draws the eye *along* it.
- **The sidebar's marker** belongs to the selected item only. Hovering an item
  gives it a background, not a marker. When the selection moves, the marker
  travels the way the Windows 10 navigation pane's does: its far end shoots out
  to the new item, the near end catches up, and it settles, in 600ms with WinUI's
  two curves. A click that loads a new page leaves a note in `sessionStorage`,
  and the new page flies the marker from the old item to its own. A same-page
  `#` link, or `Insiyab.sidebarSelect()` in an app that swaps its own content,
  flies it right away. The sidebar also keeps its scroll position from page to
  page. On a fresh visit it brings the selected item into view if it would be
  out of sight. Both happen before the first paint, so the list never shows at
  the top first and then jumps.
- **Button icons** nudge toward the trailing edge — the direction the button
  sends you. Icon-only buttons grow instead, having nowhere to travel to.
- **Fields** brighten on hover but do not lift: the lift is what focus means, and
  spending it on hover leaves focus with nothing to say.
- **Overshoot easing** stays reserved for the parts whose whole job is to travel —
  the switch thumb, the checkbox tick, and the icon chips.

Turn it all off with `data-ins-fx="off"` on `<html>`, or a `[data-ins-fx]`
button. `prefers-reduced-motion` is honoured separately and automatically.

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
<button class="ins-close" data-ins-dismiss aria-label="إغلاق"></button>  <!-- close what it is in -->
<button data-ins-dismiss="#notice">…</button>     <!-- …or the one it names -->
<button class="ins-tab" data-ins-tab="#general">…</button>  <!-- switch a tab panel -->
<button aria-label="حذف" data-ins-tip>…</button>  <!-- tooltip from aria-label -->
<span data-ins-tip="نص أطول" data-ins-tip-side="bottom">…</span>  <!-- top|bottom|start|end -->
<button class="ins-navbar-toggle" data-ins-navbar aria-label="القائمة"></button>
<button class="ins-password-toggle" data-ins-password aria-label="إظهار كلمة المرور"></button>
<form data-ins-validate>…</form>                 <!-- browser validation, in-field messages -->
<button class="ins-btn" data-ins-toggle>…</button> <!-- a toggle button: flips aria-pressed -->
<button class="ins-spin" data-ins-spin="down" aria-label="إنقاص"></button>  <!-- or "up"; beside a number input -->
<input type="checkbox" data-ins-indeterminate>    <!-- starts as "some selected" -->
<input type="date" data-ins-date>                 <!-- the date field, in an .ins-input-group -->
<input type="date" data-ins-date data-ins-date-end="#to">      <!-- the start of a range… -->
<input type="date" data-ins-date data-ins-date-start="#from">  <!-- …and its end -->
<input type="date" data-ins-date data-ins-locale="en">         <!-- a language other than the page's -->
<input type="date" data-ins-date data-ins-calendar="hijri">    <!-- Hijri, with the plugin: see Plugins -->
<button data-ins-wizard="next">…</button>         <!-- validate this step, then move; or "prev" -->
<a href="/ops/7/delete" data-ins-confirm="حذف العملية؟">…</a>  <!-- ask first -->
<button data-ins-confirm="أرشفة؟" data-ins-confirm-ok="أرشفة" data-ins-confirm-tone="danger">…</button>
```

The calendar, the confirmation and the date messages speak Arabic or English,
whichever the nearest `lang` says; any other language gets English.
`data-ins-locale` on a date field overrides it for that field.

Only one section open at a time in an accordion is plain HTML: give the
`<details>` the same `name`.

On `<html>`: `data-ins-primary="#0F766E"`, `data-ins-theme="dark|light"`,
`data-ins-orbs="off"`, `data-ins-fx="off"`, `data-ins-scrollbar="lead"`. The
script stamps `data-ins-js` itself before first paint. Every state that needs the
script keys on it: a hidden tab panel, a folded navbar menu, a show-password
button. So a page whose script failed shows all of it rather than hiding it.

## JavaScript API

The escape hatch, not the front door — prefer the attributes above.

```js
Insiyab.version                       // '0.6.0', stamped from package.json by the build
Insiyab.theme(mode?)                  // 'dark' | 'light' | 'system'
Insiyab.toggleTheme()
Insiyab.brand(hex?)                   // derive and apply a brand colour
Insiyab.toast(message, tone?)         // 'ok' | 'bad' | 'warn' | 'info'
Insiyab.dialog(target, action?)       // 'open' | 'close' | omit to toggle — drawers too
Insiyab.dismiss(target)               // close what `target` sits in, as data-ins-dismiss does
Insiyab.tab(tab)                      // select a tab (element or selector)
Insiyab.navbar(target, open?)         // open, close or toggle a navbar's menu
Insiyab.sidebar(state?)               // 'open' | 'collapsed' | 'toggle'
Insiyab.sidebarSelect(link)           // move the selected sidebar item, marker and all
Insiyab.confirm(message, options?)    // Promise<boolean>; options: title, confirm, cancel, tone: 'danger'
Insiyab.wizard(target, step?)         // the current step (0-based), or go to one, with no validation
Insiyab.date(input, iso?)             // read a date field's ISO value, or set it ('' clears)
Insiyab.calendar(name, calendar?)     // register a calendar system for date fields, or look one up
Insiyab.norm(text)                    // text as the autocomplete compares it: no hamza seats or harakat, Latin digits
Insiyab.init(scope?)                  // re-scan DOM you built yourself
Insiyab.define(name, fn)              // add your own builder to that scan
Insiyab.scrollTop()                   // reads whichever element is scrolling
Insiyab.color.derive(hex, theme)      // the full token set, without applying it
Insiyab.color.contrast('#fff','#000') // WCAG ratio
Insiyab.color.labelFor(hex)           // '#ffffff' or '#111111', measured
Insiyab.color.shift(hex, amount)      // toward black (< 0) or white (> 0)
```

Events on `document`, each with a `detail`:

```js
document.addEventListener('ins:theme', (e) => console.log(e.detail.theme));
document.addEventListener('ins:sidebar', (e) => console.log(e.detail.state));
document.addEventListener('ins:seg', (e) => console.log(e.detail.value));
document.addEventListener('ins:tab', (e) => console.log(e.detail.tab, e.detail.panel));
document.addEventListener('ins:menu', (e) => console.log(e.detail.item, e.detail.checked));
document.addEventListener('ins:navbar', (e) => console.log(e.detail.open));
document.addEventListener('ins:dismiss', (e) => console.log(e.detail.target));
document.addEventListener('ins:toggle', (e) => console.log(e.detail.el, e.detail.pressed));
document.addEventListener('ins:combo', (e) => console.log(e.detail.value, e.detail.label));
document.addEventListener('ins:date', (e) => console.log(e.detail.value, e.detail.date));   // '2026-09-23', a local Date
document.addEventListener('ins:wizard', (e) => console.log(e.detail.step, e.detail.panel));
document.addEventListener('ins:calendar', (e) => console.log(e.detail.input, e.detail.calendar)); // after the footer switch
```

The date field, the stepper and the autocomplete also fire an ordinary `change` on
their field, so code that already listens for it needs no changes.

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

## Plugins

Anything that not every page needs ships as a plugin: a third file, loaded after
`insiyab.js`, that registers itself with it. A page that doesn't load one pays
nothing for it. The plugins are built into `dist/plugins/` from `src/plugins/`.

### Hijri calendar

```html
<script src="insiyab.js"></script>
<script src="plugins/insiyab-hijri.js"></script>

<div class="ins-input-group">
  <input class="ins-input" type="date" data-ins-date data-ins-calendar="hijri" name="issued" value="2026-09-24">
</div>
```

The date field, shown and picked in the Hijri calendar: Umm al-Qura with
`hijri`, or the tabular calendar with `hijri-civil`. Put `data-ins-calendar` on
`<html>` to make every date field on the page Hijri, and give one field
`data-ins-calendar="gregory"` to opt it back out. A switch in the calendar's
footer flips the field to Gregorian and back, for the person who thinks in the
other calendar, and the field's text follows it.

**What the form sends does not change.** The hidden input holds the Gregorian
ISO date, exactly what a plain date field sends, so a server and a database need
nothing new. `min`, `max`, ranges and `Insiyab.date()` all stay Gregorian ISO;
their messages are written in whichever calendar is on screen. A typed Hijri date
is read day first, in either set of digits. A four-digit year past 1700 is read
as Gregorian, and a two-digit year as this Hijri century.

No date tables ship with it. Every current browser already knows both calendars
through `Intl`, so the plugin asks it. A browser without them, or a page that
asks for Hijri without loading the plugin, keeps a working Gregorian field and
says why in the console, once.

The plugin is built on a small calendar interface in the core. A calendar gives
its `Intl` id, the parts of a date, the date back from its parts, and the length
of a month; the Gregorian one is built in. Another calendar registers the same
way, with `Insiyab.calendar(name, calendar)`, documented where the interface is
defined in `insiyab.js`.

### Command palette

```html
<link rel="stylesheet" href="plugins/insiyab-palette.css">
<script src="insiyab.js"></script>
<script src="plugins/insiyab-palette.js"></script>

<dialog class="ins-dialog ins-palette" data-ins-palette data-ins-palette-from=".ins-shell-side">
  <div data-ins-palette-group="إجراءات">
    <a href="/invoices/new" data-keywords="new invoice">فاتورة جديدة<kbd class="ins-kbd">N</kbd></a>
    <button data-ins-theme-toggle>تبديل الوضع الداكن</button>
  </div>
</dialog>
```

One search box for every page and every command, opened with Ctrl/⌘+K, or `/`
when the person is not typing in a field. Both also work with an Arabic keyboard
layout on, where those keys type ن and ظ. **The items are the page's own links and
buttons**, so choosing one is clicking it. A link goes where it points, and a
button does whatever it already did: a `data-ins-*` attribute or a listener of
the page's own. The palette closes before the item runs, so an item that opens a
dialog opens it on top of the page.

- **Groups**: `data-ins-palette-group="…"` on an item or around several.
- **The navigation**: `data-ins-palette-from="selector"` also offers every link
  in there, with its icon and under its `.ins-shell-group` heading. It is read
  each time the palette opens. The shell's logo and any link with
  `data-ins-palette-skip` are left out, and a link already listed by hand is not
  listed twice.
- **Matching**: the same Arabic-aware matching as the autocomplete, over the name,
  then `data-keywords`, then the group. Every word typed has to match somewhere.
  Results rank by where the match falls: the start of the name first, then the
  start of a word (after the article ال too), then the middle, then a keyword.
- **Recent**: with nothing typed, the last five items chosen come first, kept in
  `localStorage`. `data-ins-palette-recent="0"` turns them off, and any other
  number changes the count.
- **Shortcuts**: `data-ins-palette-keys="mod+k /"` is the default, and
  `none` turns them off. Buttons that open the palette get `aria-keyshortcuts`.

```js
Insiyab.palette.add([                            // makes the dialog too, if there is none
  { label: 'فاتورة جديدة', group: 'إجراءات', icon: '#i-plus', hint: 'N', keywords: ['new'], run: openForm },
  { label: 'التقارير', href: '/reports' }
]);                                              // returns the elements; .remove() one to drop it
Insiyab.palette('open');                         // 'close', 'toggle'; or ('#pal', 'open')
document.addEventListener('ins:palette', (e) => {
  e.detail.label; e.detail.key; e.detail.item;   // before the item runs; preventDefault() stops it
});
```

For assistive technology, the field is a `combobox` and the results a `listbox` of
labelled groups. Focus stays in the field while the arrow keys move
`aria-activedescendant`, and the number of results is announced as the person
types.

### One-time code

```html
<link rel="stylesheet" href="plugins/insiyab-otp.css">
<script src="plugins/insiyab-otp.js"></script>

<input data-ins-otp="6" name="code" aria-label="رمز التحقق" required>
```

The verification-code field, drawn as one box per digit. **It stays one input**:
the boxes are drawn over a single real field. The phone's "code from Messages"
suggestion (`autocomplete="one-time-code"`), a paste of the whole code, a password
manager and a screen reader all see one ordinary text field. Without the script,
that plain field is all there is.

Typing always goes at the end, and Backspace takes the last character off.
Arabic-Indic digits are read as Latin ones, and a paste keeps only the code, so
«رمزك هو ١٢٣ ٤٥٦» fills 123456. An incomplete code is invalid, with a message.

- `data-ins-otp="6"`: the number of boxes, 4 to 10. Without a value, the field's
  `maxlength` is used, or 6.
- `data-ins-otp-chars="alnum"`: letters too, upper-cased.
- `data-ins-otp-submit`: submits the form when the last box fills.
- `aria-invalid="true"` after the server turns a code down: the boxes turn red,
  and the mark comes off by itself as the code is typed again.

```js
document.addEventListener('ins:otp', (e) => verify(e.detail.value));   // the code is complete
Insiyab.otp('#code');           // read it
Insiyab.otp('#code', '');       // clear it, quietly
```

### Phone number

```html
<link rel="stylesheet" href="plugins/insiyab-phone.css">
<script src="plugins/insiyab-phone.js"></script>

<div class="ins-input-group">
  <input class="ins-input" data-ins-phone name="mobile" value="+966501234567">
</div>
```

A phone field with its country. A button at the start of the group picks the
country from a list you can search in Arabic, in English, by two-letter code or
by dial code. The field shows the number the way it's written at home
(050 123 4567) and groups it as it's typed, keeping the caret after the same
digit. **What the form sends is E.164**: a hidden input takes over the field's
`name`, as with the date field, and holds +966501234567 whatever was typed (a
leading 0, spaces, Arabic-Indic digits, the country code in front). A number typed
or pasted with + or 00 picks its own country. Shared codes are narrowed by what
follows them, so +1 876 is Jamaica and +7 7 is Kazakhstan.

Every country is in the list, with names from the browser (`Intl.DisplayNames`,
in the page's language), so no name table ships. The Arab countries, plus the US
and Canada, the UK, France, Germany, Turkey, India and Pakistan, have their number
lengths checked and are grouped as they're written there. Any other country gets
the international limit of 15 digits and no grouping. A number of the wrong length
is invalid, with a message naming the country.

- `data-ins-phone-country="AE"`: the country to start in. Without it, the plugin
  uses the number's own country, then the page's region (`lang="ar-EG"`), then
  Saudi Arabia.
- `data-ins-phone-preferred="SA,AE,KW"`: listed first.
- `data-ins-phone-only="SA,AE"`: the only countries offered. A number from
  anywhere else is refused, with a message.
- With no `placeholder` of its own, the field shows an example from its country.

```js
Insiyab.phone('#mobile');                  // { value: '+966501234567', country: 'SA', valid: true }
Insiyab.phone('#mobile', '+97142345678');  // set it, country and all, quietly
document.addEventListener('ins:phone', (e) => console.log(e.detail.value, e.detail.country, e.detail.valid));
```

### File upload

```html
<link rel="stylesheet" href="plugins/insiyab-file.css">
<script src="plugins/insiyab-file.js"></script>

<input type="file" name="docs" multiple accept=".pdf,image/*"
       data-ins-file data-ins-file-max="5MB" data-ins-file-count="5">
```

A drop zone, with the chosen files listed under it: a thumbnail for an image, the
file type for anything else, the name, the size, and the library's close button to
remove it. **The files stay in the real input.** Every change (a pick, a drop, a
paste, a removal) is written back to the input's own `files`, so the form sends
exactly what's listed and the server needs nothing new. The input stays in the tab
order under its label, so the keyboard and screen readers use the browser's own
file button; the zone is for the pointer, and it shows the input's focus.

- `accept`, `data-ins-file-max` and `data-ins-file-count` are checked for drops
  and pastes too, which never pass through the picker. A file outside the limits
  isn't added, and the field says which file and why.
- With `multiple`, a new pick adds to the list; without it, a new pick replaces
  the file.
- `data-ins-file-note="…"` replaces the small print the zone builds from the
  limits.
- `class="ins-file--compact"` on the input gives a button and the list, with no
  zone.

To upload on the spot, add `data-ins-file-upload`. Every file added then fires
`ins:file`, and the page sends it with whatever request its server expects. The
input sends no files of its own: each finished upload leaves a hidden input, under
the field's name, holding the id the page gave. The field is invalid while any file
is still uploading or has failed.

```js
document.addEventListener('ins:file', async (e) => {
  if (!e.detail.upload) return;
  const file = e.detail.file;
  try {
    Insiyab.file.done(file, await send(file, (f) => Insiyab.file.progress(file, f)));
  } catch {
    Insiyab.file.fail(file, 'تعذّر الرفع');
  }
});
Insiyab.file('#docs');       // the files in the list
Insiyab.file('#docs', []);   // empty it
```

### Scrollspy

```html
<link rel="stylesheet" href="plugins/insiyab-scrollspy.css">   <!-- only for .ins-toc -->
<script src="plugins/insiyab-scrollspy.js"></script>

<nav class="ins-toc" data-ins-scrollspy aria-label="في هذه الصفحة">
  <a href="#intro">مقدّمة</a>
  <a href="#setup">الإعداد</a>
</nav>
```

The link whose section is being read gets `is-active` and `aria-current="location"`,
and that's all it touches, so any nav works: a navbar, a row of tabs, a contents
list. The current section is the last one whose top has passed a line a third of
the way down whatever scrolls it. That can be the page, the body under
`data-ins-scrollbar="lead"`, or a box with its own scrollbar, and it's found
automatically. `data-ins-scrollspy-offset="80"` puts the line that many pixels from
the top instead. At the very end of the scroll the last section is current, however
short it is. A click marks its link at once, and the marker stays put during the
scroll that click starts.

The stylesheet is optional, and only carries `.ins-toc`: a contents list that
sticks under the top bar (`--ins-toc-top`), with `.ins-toc-title`, `.ins-toc-sub`
for a deeper heading, and the sidebar's brand bar on the current entry.
`ins:scrollspy` reports each change, and `Insiyab.scrollspy(nav)` measures again.

### Timeline

```html
<link rel="stylesheet" href="plugins/insiyab-timeline.css">
<script src="plugins/insiyab-timeline.js"></script>   <!-- only for relative times -->

<ol class="ins-timeline">
  <li class="ins-timeline-item ins-timeline-item--ok">
    <span class="ins-timeline-title">تمّ الدفع</span>
    <time class="ins-timeline-time" datetime="2026-09-24T09:15" data-ins-time></time>
    <p class="ins-timeline-body">…</p>
  </li>
  <li class="ins-timeline-item is-current">…</li>
  <li class="ins-timeline-item is-pending">…</li>
</ol>
```

What happened, in order: a list, a rule down its start side, and a dot on the rule
for each event, with the rule ending at the last one. It uses the usual tones
(`--ok`, `--bad`, `--warn`, `--info`), plus `is-current` for the step in progress
(a halo) and `is-pending` for steps still to come (hollow dots on a dashed rule).
Other pieces:

- A `.ins-timeline-ico` as an item's first child replaces the dot with an icon chip
  in the tone.
- A `.ins-timeline-day` item is a date heading.
- `.ins-timeline--compact` is for a narrow column.
- `.ins-timeline--split` puts events on both sides of a centre rule once the
  timeline itself (not the screen) is wide enough.

The timeline is pure CSS. The script only writes relative times into any
`<time datetime data-ins-time>` on the page, in the page's language and from
`Intl` («قبل 5 دقائق», «أمس», «خلال ساعتين»). Past a week it writes the date
instead, and `data-ins-time="date"` always writes the date. The full date and time
go in the title, the text refreshes every minute, and `Insiyab.time()` rewrites it
on demand.

### Tree

```html
<link rel="stylesheet" href="plugins/insiyab-tree.css">
<script src="plugins/insiyab-tree.js"></script>

<ul class="ins-tree" data-ins-tree aria-label="الملفّات">
  <li data-open>
    <span>المستندات</span>
    <ul>
      <li><a href="/docs/contract.pdf">العقد.pdf</a></li>
    </ul>
  </li>
</ul>
```

Nested lists as a tree (folders, categories, an org chart). Each item's first
element is its label: text in a span, a link, or an `.ins-check` label. A nested
list is the item's branch, closed unless the item has `data-open`. Without the
script it's the nested list with every branch showing.

The tree is one tab stop, and the arrow keys walk it like a file tree:

- Up and Down move between the rows that are showing, and Home and End jump to
  the first and last.
- The inward arrow opens a branch, then steps into it. That's ← in Arabic and →
  in English.
- The outward arrow closes a branch or steps out to its parent.
- `*` opens every sibling branch, and typing a letter jumps to the next row that
  starts with it.
- Enter follows a link or selects the row. Space selects it, or ticks the box in
  a tree of checkboxes.

With `data-ins-tree-checks`, the checkboxes cascade. Ticking a branch ticks
everything in it, and a partly ticked branch shows as mixed (`aria-checked="mixed"`,
and not sent with the form). `data-ins-tree-select="none"` gives a tree that only
navigates.

```js
document.addEventListener('ins:tree', (e) => e.detail.action);  // select, open, close, check
Insiyab.tree('#node-42', 'select');   // also opens the way to it
Insiyab.tree('#files', 'open');       // every branch; or 'close'
Insiyab.tree('#files');               // the selected item, or the ticked ones
```

### Colour picker

```html
<link rel="stylesheet" href="plugins/insiyab-color.css">
<script src="plugins/insiyab-color.js"></script>

<input type="color" data-ins-color name="brand" value="#9b2c5e">
```

A colour field that tells you whether the colour can carry text. The field becomes
a hex field with a swatch in front of it, in an input group. It keeps its name and
sends `#rrggbb`, exactly what the browser's own colour input sends. The swatch
opens the picker:

- a square for saturation and brightness, which you can drag, or use with the
  arrow keys (Shift for steps of ten)
- a hue slider
- preset swatches: the page's brand colour and a palette around it, or your own
  list in `data-ins-color-swatches`, or none with `none`
- the eyedropper, where the browser has one
- the colour's **contrast** against white and against black, from the core's
  `Insiyab.color.contrast`, marked against the 4.5:1 that body text needs

A hex can also be typed as `#abc`, `abc` or `aabbcc`, and is written out in full
when the field is left. `input` fires while the colour moves, and `change` and
`ins:color` fire when it's set. `Insiyab.colorField(field, value?)` reads or sets
it; `Insiyab.color` stays the core's colour maths.

### Carousel

```html
<link rel="stylesheet" href="plugins/insiyab-carousel.css">
<script src="plugins/insiyab-carousel.js"></script>

<div class="ins-carousel" data-ins-carousel aria-label="أعمال مختارة">
  <div>…</div>
  <div>…</div>
</div>
```

Slides in a row. The row is the browser's own scroll-snap scroller, so a finger,
a trackpad and Shift+wheel all move it, and without the script it's still a row
that swipes. The script adds what a scroller can't say about itself:

- previous and next buttons, which step aside at the ends
- a dot for each stop
- the arrow keys in the page's reading direction, plus Home and End
- `role="region"` with `aria-roledescription`, and «2 من 5» on each slide for a
  screen reader, with changes announced politely

`.ins-carousel--2` and `--3` show two or three slides at a time when the carousel
itself (not the screen) is wide enough. `--peek` shows the edge of the next slide,
and `data-ins-carousel-dots="off"` drops the dots.

**It never moves on its own.** Nothing in this library does: a slide that leaves
while someone is reading it is the reason. Under `prefers-reduced-motion` it moves
without sliding. `ins:carousel` reports the slide in view, and
`Insiyab.carousel(el, n?)` reads the current stop or moves to one.

## Development

Node 18+ for the build; nothing else, and no dependencies at all.

```sh
node build.mjs           # src/css/*.css + src/js → dist/, and demo/src → demo/
node build.mjs --check    # build and verify, write nothing
node serve.mjs            # then open http://localhost:4173
```

**The demo is generated.** Edit `demo/src/`, never the `demo/*.html` it writes:

```
demo/src/site.mjs         the page list — sidebar order, titles, search terms
demo/src/layout.html      the shell every page is poured into
demo/src/pages/<slug>.html one fragment per page, content only
demo/assets/              the docs site's own CSS and JS — not part of the library
```

In a fragment, `<demo-example>…</demo-example>` renders its markup live and prints
the same markup, highlighted, under it — so an example and its listing cannot
drift apart. `<demo-code lang="js">` is a listing on its own. The build fails on a
page missing from the list, a link to a page that does not exist, an icon the
sprite does not have, or a section heading with no `id`.

**Serve the demo, do not open the file.** Over `file://` the self-hosted faces
count as cross-origin, the browser blocks them, and the typography silently falls
back to a system serif — which hides the most visible part of any change.

The stylesheet is authored in parts under `src/css/` and concatenated in filename
order. One hand-maintained 4,000-line file is unmaintainable by month three; a
build script is not. The build also:

- duplicates the dark palette into a `prefers-color-scheme` variant, so it is
  written once rather than twice, and gives every component rule keyed on
  `:root[data-ins-theme="dark"]` the same twin, in place, so an OS set to dark
  paints exactly what an explicit dark choice does;
- fails if any `--dk-*`, `--bs-*` or other pre-rename token survives;
- fails if any `var()` reads a token nothing declares — an undefined custom
  property invalidates its whole declaration silently, which is how three
  shadows in the original sheet were dead for months.

### Tests

```sh
node test/run.mjs                 # build, then every test/*.test.mjs
node test/run.mjs sidebar dark    # only the files whose names contain a word
node test/run.mjs --verbose       # every check, not only failures
```

About 630 checks in twenty-two files, each driving a real headless Chrome with real
key and pointer events, on a throwaway profile, against a server the runner
starts on a free port. Needs Chrome, found in the usual places or through
`CHROME`, and no npm packages: the driver is a small DevTools-protocol client in
`test/lib/`.
A file that hangs is stopped after three minutes, with its browser, and reported
as failed; `INS_TEST_TIMEOUT` (seconds) changes the limit.

Component behaviour is tested on `test/fixtures/`, which exists only for the
tests, so the docs can change their examples without breaking them. The
shell-level behaviour is tested on the docs site, because it needs a real
multi-page shell: the theme sweep, the sidebar marker and its scroll, and OS dark
mode compared element by element with an explicit dark choice.

## Licence

MIT for the code — see [LICENSE](LICENSE). The bundled fonts are SIL Open Font
License 1.1, with their own licences in [`fonts/`](fonts/).
