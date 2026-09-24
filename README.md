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

> **Status: 0.3.0.** The core is in place: the application shell, the layout,
> type and utility layer, the components an application form or dashboard needs
> (pickers, steps and confirmation included), and a motion layer across all of
> them. The demo is a documentation site — one page per topic, in Arabic, inside
> the shell, with every example's markup printed under it. Still to come:
> the plugins (command palette, file upload, Hijri calendar and the like) and the
> React and Jinja wrappers. See
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
| **Data** | `.ins-table` (+ `-wrap`) · `.ins-stat` (6 tones, 3 sizes) · `.ins-pill` · `.ins-badge` · `.ins-money` · `.ins-num` · `.ins-avatar` · `.ins-list` (`-item`, `-text`, `-title`, `-desc`, `-end`; items can be links) |
| **Input** | `.ins-field` · `.ins-label` · `.ins-hint` · `.ins-input` / `.ins-select` (`--sm`, `--lg`) · `.ins-textarea` · `.ins-check` (indeterminate via `data-ins-indeterminate`) · `.ins-switch` (+ `--card`, `-grid`) · `.ins-seg` · `.ins-search` · `.ins-input-group` + `.ins-addon` · `.ins-password-toggle` |
| **Pickers** | `.ins-combo` (`-list`, `-option`, `-empty`) · date and date range via `data-ins-date` (+ `.ins-date-btn`, `.ins-cal`) · `.ins-range` · number stepper `.ins-spin` in an `.ins-input-group` · `.ins-tile` (`-grid`, `-title`, `-desc`) for radio and checkbox cards |
| **Validation** | `aria-invalid="true"` · `:user-invalid` · `.ins-error` · `.ins-success` · `.ins-input--ok` · `.ins-req` · `form[data-ins-validate]` |
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

**The primitives own the gaps.** `.ins-stack`, `.ins-row` and `.ins-cols-*`
space their children with `gap` and clear the children's own outer margins, so a
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
- **A table row** grows a 3px brand marker on its leading edge — the same marker
  the sidebar's active item uses, so "what is under the pointer" and "where you
  are" speak one vocabulary. It scales from the centre rather than sliding,
  because a bar sliding the length of a wide row draws the eye *along* it.
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
Insiyab.version                       // '0.3.0'
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

## Licence

MIT for the code — see [LICENSE](LICENSE). The bundled fonts are SIL Open Font
License 1.1, with their own licences in [`fonts/`](fonts/).
