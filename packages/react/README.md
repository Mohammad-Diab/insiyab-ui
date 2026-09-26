# @insiyab/react

React components for [Insiyab](https://github.com/Mohammad-Diab/insiyab-ui#readme). They are thin covers over the
library's classes and attributes, not a second implementation. The CSS stays the
source of truth, and the core script (`insiyab.js`) does the behaviour: the date
picker, the tabs' arrow keys, the menus, the dialogs, the plugins. A component
renders the markup the docs show and keeps React's state in step with what the core
does to it.

```sh
npm install insiyab @insiyab/react
```

React 18 or later; the tests run on React 19. Written in TypeScript; the types ship
with it.

## Loading the core

The components need the stylesheet and the script on the page, as any Insiyab page
does. Put them in `<head>`, so the theme is set before the first paint:

```html
<link rel="stylesheet" href="/insiyab/insiyab.css">
<script src="/insiyab/insiyab.js"></script>
<!-- and each plugin you use, after it -->
<link rel="stylesheet" href="/insiyab/plugins/insiyab-phone.css">
<script src="/insiyab/plugins/insiyab-phone.js"></script>
```

With a bundler you can import them instead, before the app renders. The page then
flashes the light theme once on a dark-mode visit, which the `<head>` tag avoids:

```js
import 'insiyab/css';
import 'insiyab';
import 'insiyab/plugins/insiyab-phone.js';
```

The package never imports the core itself. It calls `window.Insiyab` when it needs
it, so a component rendered on a server or before the script arrives just renders
its markup.

**Next.js and other server rendering.** The core stamps `<html>` before the first
paint (`data-ins-js`, the theme, the brand colour), so give the root element
`suppressHydrationWarning`, as any theme script needs:

```jsx
<html lang="ar" dir="rtl" suppressHydrationWarning>
```

The components carry `'use client'` where they need it. The rest is covered by the
design: see "How it works".

## Using it

```jsx
import { Panel, Field, Input, DateField, Button, toast } from '@insiyab/react';

export function Delivery() {
  const [date, setDate] = useState('2026-09-30');
  return (
    <Panel title="التسليم" footer={<Button variant="primary" onClick={() => toast('حُفظ', 'ok')}>حفظ</Button>}>
      <Field label="العنوان" hint="الشارع والمبنى" required>
        <Input name="address" />
      </Field>
      <Field label="التاريخ">
        <DateField name="delivery" value={date} onValueChange={setDate} min="2026-09-23" />
      </Field>
    </Panel>
  );
}
```

The same conventions hold throughout:

- **Props are the class's modifiers.** `variant`, `size`, `tone`, `card`, `flush`,
  `iconOnly`… each maps to one class from the docs. Anything else is passed on as
  an attribute, so `className`, `id`, `aria-*` and `data-*` work everywhere. A
  utility class goes in `className`.
- **Composite fields put `ref` and the input's attributes on the real input**, and
  `className`/`style` on the outermost element, the one you see. `inputClassName`
  reaches the input itself. This covers `Check`, `Switch`, `Tile`, `Search`,
  `PasswordInput`, `NumberInput`, `DateField`, `Combo`, `PhoneField`, `ColorField`,
  `Otp` and `FileField`. Form libraries that register by `ref` (React Hook Form) get
  the input.
- **`Field` wires its control.** The label's `for`, the control's `id`, and
  `aria-describedby` naming the hint and the message are all set for you. `error`
  shows a message and marks the control invalid. Inside `<Form validate>`, each field
  gets the empty message the core fills with the browser's own words, and
  `validationMessage` replaces those words.
- **Values are controlled or not, as React's own inputs are.** `value` with
  `onValueChange`, or `defaultValue`. The same goes for `open`/`onOpenChange`,
  `step`/`onStepChange`, `pressed`/`onPressedChange` and `checked`/`onCheckedChange`.
  A controlled value the page refuses to change is put back.
- **The docs' attributes are props.** On `Button` and `CloseButton`: `tip`,
  `confirm` (with `confirmOk`, `confirmTone`), `toast` (with `toastTone`) and
  `dismiss`. They render exactly the attribute, so the behaviour is the core's.
- **Text defaults are Arabic**, like the library: `CloseButton`'s «إغلاق»,
  `Pagination`'s «السابق»/«التالي». Every one is a prop.
- **No icons ship.** Pass your own element to `icon`. `<Icon name="i-check" />` is
  the `<svg class="ico"><use href="#i-check"/></svg>` the docs use with a sprite.

## Components

| Group | Components |
|---|---|
| Layout | `Container` `Stack` `Row` `Cols` `Grid` `Toolbar` `FieldRow` `FormCommit` |
| Type | `Display` `Heading` `Lead` `Eyebrow` `Prose` `Kbd` `Num` `Money` `Divider` |
| Surfaces | `Glass` `Card` `Panel` (`PanelHead` `PanelBody` `PanelFoot`) |
| Data | `Table` `Stat` `Pill` `Badge` `Avatar` `List` `ListItem` |
| Buttons | `Button` `ButtonGroup` `ToggleButton` `CloseButton` `Icon` |
| Forms | `Form` `Field` `Input` `Select` `Textarea` `InputGroup` `Addon` `Check` `Radio` `Switch` `SwitchGrid` `Tile` `TileGrid` `Search` `PasswordInput` `NumberInput` `Range` `Seg` |
| Pickers | `Combo` `DateField` (a range: `rangeEnd`/`rangeStart` name the other field's id) |
| Feedback | `Alert` `Empty` `Progress` `Ring` `Skeleton` `Spinner` `Loading` |
| Overlay | `Dialog` `Drawer` `Menu` `Popover` `MenuItem` `MenuCheckbox` `MenuRadio` `MenuLabel` `MenuSeparator` `MenuGroup` |
| Navigation | `PageHead` `Breadcrumb` `Tabs` `TabList` `Tab` `TabPanel` `Pagination` `Navbar` `NavbarLink` `Accordion` `Collapse` `Steps` (`dots`) `Wizard` (`motion="pane"` or `"slide"`, `dots`) `WizardPanel` |
| Shell | `Shell` `Sidebar` `SidebarBrand` `SidebarGroup` `SidebarLink` `SidebarToggle` `Topbar` `TopbarTitle` `Island` `ThemeToggle` |
| Plugins | `Otp` `PhoneField` `FileField` `ColorField` `Tree` `Timeline` `TimelineItem` `TimelineDay` `RelativeTime` `Carousel` `Toc` `useScrollspy` `CommandPalette` |

The Hijri calendar is a prop, not a component: `<DateField calendar="hijri" />` once
its plugin is loaded. Each plugin component needs its plugin's script (and
stylesheet) on the page, as on a plain page.

The functions are the core's, safe to call on a server, where they do nothing:
`toast(message, tone)`, `confirm(message, options)` (a `Promise<boolean>`),
`theme(mode)`, `toggleTheme()`, `brand(hex)` and `sidebar(state)`. `core()` returns
`window.Insiyab` itself, typed, for anything else.

Two hooks: `useTheme()` returns the theme showing and a setter, and
`useInsiyabEvent('ins:…', handler)` listens to any of the core's events.

## How it works

The core changes the DOM it is given, and React assumes it owns the DOM it rendered.
The wrapper keeps the two apart in three ways, and the tests check each one.

1. **What only needs attributes is rendered complete.** The tabs' roles and ids, a
   menu's roles, the combobox pattern, a field's `aria-describedby`: the core's
   builders would stamp them, so the components render them already. The server's
   HTML and the built page are then the same, and hydration finds nothing to
   disagree with.
2. **What gets rebuilt is triggered only after mount.** The date field, the phone,
   colour, OTP and file fields, the tree, the carousel, the palette and relative
   times: their trigger attribute (`data-ins-date`, `data-ins-phone`…) is never
   rendered. Each component sets it once mounted, then calls `Insiyab.init`. The
   core's own scan, which can run before React hydrates, finds nothing to rebuild,
   and without script the server's HTML is the plain field.
3. **What the core moves is rendered where the core keeps it.** The OTP box, the
   file box, the carousel's track, the tree's rows and the colour field's group are
   already in the markup, so the plugins adopt them and never move a node React owns.
   Attributes the core takes over after building are read back rather than
   re-asserted: the date field's `type` and `name`, a shown password's `type`. React
   19 writes an input's `type` and `name` on every update. The hidden input the phone
   plugin adds outside the field is removed when the field unmounts.

Some things follow from that:

- A `Tree` whose shape changes (items added or removed) is rebuilt from scratch, and
  so is a `Carousel` whose slide count changes. Changing a label's text is an
  ordinary update.
- `CommandPalette` takes its items as data (`items={[{ label, href | run, group }]}`),
  because the plugin builds the whole dialog. Changing the array replaces them.
- A `SidebarLink`'s `current` is drawn from the first render. After that, a moved
  `current` is handed to `Insiyab.sidebarSelect`, so the marker flies between the
  links.

## Tests

From the repository root, after `npm install` in this folder:

```sh
node test/run.mjs react
```

- Parity: every example in `test/fixtures/parity/`, copied from the docs, is built
  here with these components and compared, once the core has built both, element by
  element and attribute by attribute. Ids are compared by position.
- Hydration: an app of the stateful components, rendered with `react-dom/server`,
  then hydrated after the core's scan has already run over it, in Strict Mode, with
  React's development build. A mismatch or a warning fails the test.
- Behaviour: real clicks and keys on that app, read back from React state.
- Import: the same app bundled with the core and its plugins `import`ed, as a
  bundled app would load them, and mounted from nothing.

Without `node_modules` here, the root test run reports this file as skipped.

## Building

```sh
npm run build    # tsc: dist/*.js and dist/*.d.ts, one per source file
```
