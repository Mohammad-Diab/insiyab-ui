# insiyab for Jinja

Jinja macros for [Insiyab](https://github.com/Mohammad-Diab/insiyab-ui), the
RTL-first UI library, with the library's own stylesheet, script, fonts and plugins
in the package. Each macro writes the markup the library's docs show, and
`insiyab.js` does the behaviour, so a server-rendered page is an Insiyab page like
any other.

```sh
pip install insiyab            # Jinja2 3.0 or later, Python 3.8 or later
pip install "insiyab[flask]"   # and Flask
```

## Setting it up

**Flask.** One call serves the files under `/static/insiyab` and makes the macros
importable:

```python
import insiyab
insiyab.init_app(app)                         # or init_app(app, url_prefix='/assets/insiyab')
```

**Any Jinja2 environment** (FastAPI, Starlette, Django's Jinja backend, a script).
`register()` adds the macros after the environment's own loader, so a template of
yours named `insiyab/…` still wins. Serve the files from `insiyab.static_dir()`:

```python
import insiyab
from fastapi.staticfiles import StaticFiles
from fastapi.templating import Jinja2Templates

templates = Jinja2Templates(directory="templates")
insiyab.register(templates.env)               # static_url='/static/insiyab' by default
app.mount("/static/insiyab", StaticFiles(directory=insiyab.static_dir()), name="insiyab")
```

The macros assume **autoescape is on**. Flask, FastAPI and Django's Jinja backend
already turn it on; a bare `Environment` needs `autoescape=True`. `register()` warns
if it is off. The macro files end in `.html` so that Flask autoescapes them too.

## Using it

```jinja
{% import "insiyab/ui.html" as ins %}
<!doctype html>
<html lang="ar" dir="rtl">
<head>
  <meta charset="utf-8">
  {{ ins.head(plugins=['phone']) }}
</head>
<body>
  {% call ins.panel(title='التسليم') %}
    {% call ins.field('التاريخ', id='delivery') %}
      {{ ins.date(id='delivery', name='delivery', value='2026-09-30') }}
    {% endcall %}
    {% call ins.field('الجوال', id='mobile') %}
      {{ ins.phone(id='mobile', name='mobile', value='+966501234567') }}
    {% endcall %}
    {{ ins.button('حفظ', variant='primary') }}
  {% endcall %}
</body>
</html>
```

`head(plugins=[...])` writes the stylesheet and the script, not deferred, so the
theme is set before the first paint, and then each plugin named. Add `min=true` for
the minified stylesheets, or `base='https://…'` for another host.

The conventions:

- **Keywords are the class's modifiers.** `variant`, `size`, `tone`, `card`, `flush`,
  `icon_only`… each maps to one class from the docs, and `class` adds your own.
- **Any other keyword is an attribute**, with `_` read as `-`: `aria_label='بحث'`,
  `data_ins_tip=true`. `true` writes a bare attribute, and `false` or `none` leaves it
  out. Attribute values are always escaped.
- **Content comes from a `{% call %}` block** wherever a component holds other markup
  (a panel, a field, a menu, a dialog), or from the first argument for plain text.
  Pass a macro's output, or a `{% set %}…{% endset %}` block, to a parameter that
  takes markup: `icon=ins.icon('i-check')`, `footer=buttons`.
- **Fields put the extra keywords on the input** (`name`, `value`, `required`,
  `checked`…) and `class` on the outer element, the one you see. Give `field()` and
  its control the same `id`. The core links the hint and the message with
  `aria-describedby` itself.
- **The docs' attributes are keywords** on `button` and `close_button`: `tip`,
  `confirm` (with `confirm_ok`, `confirm_tone`), `toast` (with `toast_tone`),
  `dismiss`, and `dialog='#id'` to open a dialog or drawer.
- **Text defaults are Arabic**, like the library: «إغلاق», «السابق», «التالي». Every
  one is a parameter.
- **No icons ship.** `ins.icon('i-check')` is the `<svg class="ico"><use href="#i-check"/></svg>`
  the docs use with a sprite; `hidden=true` adds `aria-hidden`.

## Macros

| Group | Macros |
|---|---|
| Layout | `container` `stack` `row` `cols` `grid` `toolbar` `field_row` `form_commit` |
| Type | `display` `heading` `lead` `eyebrow` `prose` `kbd` `num` `money` `divider` |
| Surfaces | `glass` `card` `panel` `panel_head` `panel_body` |
| Data | `table` `stat` `pill` `badge` `avatar` `list` `list_item` |
| Buttons | `button` `button_group` `toggle_button` `close_button` `icon` |
| Forms | `form` `field` `input` `select` `textarea` `input_group` `addon` `check` `radio` `switch` `switch_grid` `tile` `tile_grid` `search` `password` `number` `range` `seg` |
| Pickers | `combo` `date` (a range: `range_end` / `range_start`, the other field's id) |
| Feedback | `alert` `empty` `progress` `ring` `skeleton` `spinner` `loading` |
| Overlay | `dialog` `drawer` `menu` `popover` `menu_item` `menu_checkbox` `menu_radio` `menu_label` `menu_separator` `menu_group` |
| Navigation | `page_head` `breadcrumb` `tablist` `tab` `tabpanel` `pagination` `navbar` `navbar_link` `accordion` `collapse` `steps` `wizard` `wizard_panel` |
| Shell | `shell` `sidebar` `sidebar_brand` `sidebar_group` `sidebar_link` `sidebar_toggle` `topbar` `topbar_title` `island` `theme_toggle` |
| Plugins | `otp` `phone` `file` `color` `tree` `timeline` `timeline_item` `timeline_day` `relative_time` `carousel` `toc` `palette` `palette_group` |
| Page | `head` |

Each group also imports on its own: `{% from "insiyab/forms.html" import field, input %}`.
The Hijri calendar is a keyword: `ins.date(calendar='hijri')`, with
`head(plugins=['hijri'])`. The parameters are documented at the top of each macro, in
`insiyab/templates/insiyab/`.

## Tests

From the repository root:

```sh
node test/run.mjs jinja
```

The package's unit tests (setup, escaping, `head()`, the files, Flask) run first,
one check each. Then every example in `test/fixtures/parity/`, copied from the docs,
is rendered here with the macros and compared in a real browser with the example
itself, once the core has built both: element by element and attribute by
attribute. The React wrapper's test uses the same examples. On their own, the unit
tests run with:

```sh
python -m unittest discover -s packages/jinja/tests -v
```

## Building

The wheel carries a copy of the repository's `dist/`:

```sh
python sync_static.py && pip wheel . --no-deps -w wheelhouse
```

From a checkout, `insiyab.static_dir()` uses the repository's `dist/` directly.
