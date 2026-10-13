# Insiyab.Avalonia

Insiyab for Avalonia 12: the same glass, RTL-first look as the web library, as Avalonia themes. App code stays plain Avalonia: `<Button Classes="primary">`.

```xml
<Application.Styles>
  <ins:InsiyabTheme />   <!-- xmlns:ins="using:Insiyab.Avalonia", in place of FluentTheme -->
</Application.Styles>
```

Set `FlowDirection="RightToLeft"` on the window for an Arabic UI; a Latin field inside it (a username, a password) takes `FlowDirection="LeftToRight"` on its own.

## Usage

| What | How |
|---|---|
| Brand colour | `InsiyabTheme.SetPrimary(Color.Parse("#0F766E"))` re-derives every tinted token, light and dark at once. `InsiyabTheme.Primary` reads it back. |
| Light / dark | `ins:DayNight` is the sky switch for a top bar island; `ins:ThemeToggle.Mode="toggle"` (or `dark`, `light`, `system`) on any Button sets the theme with the circular sweep from it; `ThemeToggle.Apply("dark")` from code, instantly. Underneath it is `Application.RequestedThemeVariant` |
| Effects off | `ins:Motion.Still="True"` on a window is the web's «تخفيف المؤثرات»: hovers and presses keep their colour and lose their movement. The system's reduced motion (Windows) counts too; `Motion.IsStill(visual)` asks from code, and the window carries `:ins-still` meanwhile |
| Text | `TextBlock` classes `display h1 h2 h3 h4 lead prose muted eyebrow num` |
| Surfaces | `Border` classes `card`, `glass`, `glass-inner`, `hoverable` |
| Panel | `Border.panel` around a `DockPanel` of `Border.panel-head` (top), `Border.panel-foot` (bottom) and `Border.panel-body`; `panel alert` for the tinted variant; `panel-title`, `panel-ico`, `panel-actions`, `panel-note` inside the head |
| Button | `primary`, `secondary` (also what a plain `Button` looks like), `ghost`, `bare` (text only); `success danger warning info`; `ghost-success ghost-danger ghost-warning ghost-info`; sizes `sm lg`; `icon`, `lift` |
| Text field | plain `TextBox`; `PasswordChar="•" Classes="revealPasswordButton"` for a password with the eye; `sm lg`; errors through `DataValidationErrors`; `TextBlock.label` and `TextBlock.hint` around it |
| Check, switch | plain `CheckBox` and `ToggleSwitch` |
| Window | every window asks the OS for Mica or acrylic and falls back to the opaque ground on its own; `ins:Frost.Material="False"` keeps one opaque, and `:ins-material` is set while the material is on |
| Title bar | On Windows, `ins:TitleBar.Extends="True"` on the Window draws the title bar in the app: put `<ins:CaptionButtons />` at the window's top-end corner (it follows FlowDirection, so it sits on the left in RTL), mark the empty parts of the top bar `ins:TitleBar.IsDragArea="True"` (controls that take focus stay clickable), and the app icon `ins:TitleBar.IsSystemMenuArea="True"`: a right-click on the icon, or Alt+Space, opens the system menu, and a right-click elsewhere on the bar opens nothing. Snap, Snap Layouts, double-click, resize and the auto-hide taskbar keep working. The buttons hide in full screen, in a single view and on other platforms. |
| Fonts | Tajawal, Inter and Cause are embedded; the body font follows the direction (Tajawal in RTL, Inter in LTR) and LTR headings use Cause |

Avalonia has no CSS, so this package is the one flavour of Insiyab that re-implements the components instead of wrapping the classes. The CSS stays the source of truth: the design tokens are generated from `src/css/02-tokens.css` and `03-tokens-dark.css` by `tools/gen-tokens.mjs`, which fails on any token it cannot map.

## Resource keys

Every token is a resource named after its CSS name: drop `--ins-`, capitalise each dash-separated part, prefix `Ins`.

| CSS | Key |
|---|---|
| `--ins-primary-dark` | `InsPrimaryDark` |
| `--ins-r-md` | `InsRMd` |
| `--ins-sh-glass` | `InsShGlass` |
| `--ins-fs-h1` | `InsFsH1` |
| `--ins-wash-1` | `InsWash1` |

The resource type follows the value:

| Value | Resource |
|---|---|
| colour (`#hex`, `rgba()`) | `{Key}` is a `SolidColorBrush`, `{Key}Color` is a `Color` |
| shadow list | `BoxShadows` |
| radius (`--ins-r-*`) | `CornerRadius` |
| other `px` | `double` |
| `rem` | `double`, in px at 16px per rem |
| seconds | `TimeSpan` |
| `cubic-bezier()` | `SplineEasing` |
| gradient | `LinearGradientBrush` |
| font stack | `FontFamily` (in `Themes/Fonts.axaml`) |

A token whose value differs between light and dark lives in the `Light` and `Dark` theme dictionaries; the rest live at the root. Use `DynamicResource` for every colour, brush and shadow, so theme and brand changes reach it.

Brand tokens (any token that depends on the primary colour, directly or through `var()`) are not in `Tokens.axaml`. They are set from code by `InsiyabTheme.SetPrimary(Color)`, which derives them the way `Insiyab.brand()` does on the web.

## Layout of this folder

| Path | What |
|---|---|
| `src/Insiyab.Avalonia` | the package |
| `src/Insiyab.Avalonia/Generated` | generated tokens, never edited by hand |
| `src/Insiyab.Avalonia/Themes` | one `Styles` file per component |
| `gallery` | every component in light/dark and RTL/LTR |
| `tests` | headless tests |
| `aot-check` | a Native AOT consumer: publishing it must give zero warnings |
| `tools` | generators: `gen-tokens.mjs` (tokens), `brand-fixture.mjs` (the brand parity cases from insiyab.js), `fonts/build-fonts.mjs` (the embedded fonts); the first two take `--check` |

`node test/run.mjs avalonia` from the repository root checks that the generated files are current and runs the headless tests.
