<img src="https://raw.githubusercontent.com/Mohammad-Diab/insiyab-ui/main/brand/mark-256.png" width="88" height="88" alt="Insiyab logo">

# Insiyab.Avalonia

Insiyab for Avalonia 12: the same glass, RTL-first look as the web library, as Avalonia themes. App code stays plain Avalonia: `<Button Classes="primary">`.

```xml
<Application.Styles>
  <ins:InsiyabTheme />   <!-- xmlns:ins="using:Insiyab.Avalonia", in place of FluentTheme -->
</Application.Styles>
```

Set `FlowDirection="RightToLeft"` on the window for an Arabic UI; a Latin field inside it (a username, a password) takes `FlowDirection="LeftToRight"` on its own. Avalonia's own `Calendar`, `CalendarDatePicker`, `DatePicker` and `TimePicker` follow the direction too: Arabic month, day and AM/PM words in RTL, English in LTR, Gregorian dates in Latin digits whatever the machine's culture.

## Usage

| What | How |
|---|---|
| Brand colour | `InsiyabTheme.SetPrimary(Color.Parse("#0F766E"))` re-derives every tinted token, light and dark at once. `InsiyabTheme.Primary` reads it back. Big fills (primary buttons) take the primary; small accents (checks, switches, progress, sliders, markers, dots) take the middle shade `InsPrimaryMid` with its label `InsOnPrimaryMid`, the primary moved toward white by `BrandDerivation.MidShiftLight` (20%) or `MidShiftDark` (45%). |
| Light / dark | `ins:DayNight` is the sky switch for a top bar island; `ins:ThemeToggle.Mode="toggle"` (or `dark`, `light`, `system`) on any Button sets the theme with the circular sweep from it; `ThemeToggle.Apply("dark")` from code, instantly. Underneath it is `Application.RequestedThemeVariant` |
| Effects off | `ins:Motion.Still="True"` on a window is the web's «تخفيف المؤثرات»: hovers and presses keep their colour and lose their movement. The system's reduced motion (Windows) counts too; `Motion.IsStill(visual)` asks from code, and the window carries `:ins-still` meanwhile. Motion nobody can see does not run: out of view, hidden, collapsed or in a minimised window a loop rests and an entrance waits until it is seen |
| Text | `TextBlock` classes `display h1 h2 h3 h4 lead prose muted eyebrow num` |
| Surfaces | `Border` classes `card`, `glass`, `glass-inner`, `hoverable` |
| Panel | `Border.panel` around a `DockPanel` of `Border.panel-head` (top), `Border.panel-foot` (bottom) and `Border.panel-body`; `panel alert` for the tinted variant; `panel-title`, `panel-ico`, `panel-actions`, `panel-note` inside the head |
| Button | `primary`, `secondary` (also what a plain `Button` looks like), `ghost`, `bare` (text only); `success danger warning info`; `ghost-success ghost-danger ghost-warning ghost-info`; sizes `sm lg`; `icon`, `lift` |
| Split and drop-down buttons | plain `SplitButton`, `ToggleSplitButton` and `DropDownButton` with the Button classes (`primary` … `bare`, `sm lg`, `full`); each half of a split button hovers on its own, the chevron turns over while the menu is open, and an unchecked ToggleSplitButton is the neutral button. Give them a `Flyout` of `ins:PopMenu` for the library's menu |
| Text field | plain `TextBox`; `PasswordChar="•" Classes="revealPasswordButton"` for a password with the eye; `sm lg`; errors through `DataValidationErrors`; `TextBlock.label` and `TextBlock.hint` around it |
| Masked field | plain `MaskedTextBox` (`Mask="000 000 0000"`, with `FlowDirection="LeftToRight"` on an Arabic page): the field look in tabular Latin figures, the mask's empty places (`PromptChar`) muted until they fill; `bad`/`ok` and `sm lg` as on a TextBox |
| Document tabs | `ins:DocumentTabs` of `ins:DocumentTab` (or an `ItemsSource`): the tab strip's look with a close button on the selected tab and under the pointer, `IsClosable="False"` for a pinned tab. Closes with the button, Ctrl+W, Ctrl+F4, Delete on a focused tab or a middle click; reorders by drag or Ctrl+Shift+PageUp/PageDown; Ctrl+Tab cycles; the strip scrolls between arrows when the tabs don't fit; `ShowNewButton="True"` adds a «+». Events `TabCloseRequested`, `TabMoveRequested` (set `Handled` to do it yourself or refuse; unhandled, the control edits `Items` or an `IList` ItemsSource) and `NewTabRequested`; `Close(index)` and `Move(from, to)` from code |
| Check, switch | plain `CheckBox` and `ToggleSwitch` |
| Window | every window asks the OS for Mica or acrylic and falls back to the opaque ground on its own; `ins:Frost.Material="False"` keeps one opaque, and `:ins-material` is set while the material is on |
| Title bar | On Windows, `ins:TitleBar.Extends="True"` on the Window draws the title bar in the app: put `<ins:CaptionButtons />` at the end of the top bar, after the last zone: one island of round buttons, centred with the others (it follows FlowDirection, so it sits on the left in RTL), mark the empty parts of the top bar `ins:TitleBar.IsDragArea="True"` (controls that take focus stay clickable), and the app icon `ins:TitleBar.IsSystemMenuArea="True"`: a right-click on the icon, or Alt+Space, opens the system menu, and a right-click elsewhere on the bar opens nothing. Snap, Snap Layouts, double-click, resize and the auto-hide taskbar keep working. The buttons hide in full screen, in a single view and on other platforms. `Preview="Normal"`, `"Maximized"` or `"Inactive"` shows them outside a title bar in that state, as in a docs page: always visible, inert, and not the window's own buttons. |
| Toolbar | `<ins:Toolbar>` holds a row of commands: `ins:ToolbarButton`, `ins:ToolbarToggleButton` and `ins:ToolbarSplitButton`, each with `Icon`, `Label` (in the tip, beside the icon with `ShowLabel="True"`, in the overflow menu, and the screen-reader name) and `Shortcut` (shown in the tip and the menu; unset, `HotKey` is shown), plus `Separator`s and any other control, such as a search `TextBox`. A ToolbarButton with a `Flyout` is a dropdown with a chevron. Glass strip by default, `Classes="plain"` for a flush bar with a hairline under it. What does not fit moves, from the end, into a «⋯» button (`MoreText`) whose `PopMenu` lists the same commands with their labels: a toggle as a check item, a dropdown or split button as a submenu. `ins:Toolbar.Overflow="Never"` keeps a command in the row, `"Always"` puts it only in the menu; controls that are not buttons never move. One Tab stop; Left/Right (mirrored in RTL), Home and End move between the commands. `Click` and `Command` work from the row and from the menu. |
| Status bar | `<ins:StatusBar>`: a thin strip for the foot of a window. Its direct items sit on the start side, `<ins:StatusBar.End>` on the end side (the start side is cut first when it gets narrow). `ins:StatusItem` reports (`Icon`, `IsBusy` for a small spinner, any content), `ins:StatusButton` is clickable (`Icon`, `Click`, `Command`, a `Flyout` opens upward at the window's foot); `Separator`, `ins:SaveStatus` and a short `ProgressBar` fit in too. Dock it at the bottom of a `DockPanel` around the shell's SplitView, so it never covers the content and the drawer opens above it; toasts in the window's corner sit above it. |
| Sidebar | a `SplitView` whose pane is a `DockPanel` of `Border.shell-brand` (top), `Border.shell-bottom` (bottom) and `ListBox.shell-nav`; the head stays pinned and only the links scroll, and `ins:Shell.HeadScrolls="True"` on the `SplitView` scrolls the head away with them, as on the web; `ins:Shell.HasFilter="True"` on the `ListBox` puts a filter field over the links (`ins:Shell.FilterPlaceholder`, `ins:Shell.FilterEmptyText`, and `ins:Shell.Keywords` on a link for more words to find it by), focused by `/` outside a text field |
| Table | `<ins:Table>` with `ins:TableColumn`s in `Columns` (`Header`, `Classes="num"`, `Sort`, `Edit`, `IsFit`, `MinWidth`, the narrowest a drag leaves it) and `ins:TableRow`s, or an `ItemsSource` with each column's `CellText` or `CellTemplate`. `IsSelectable`, `HasSelectAll`, `SelectionMode`, `RowContextMenu`, `Empty`; classes `sticky` (header kept in view) and `pin`. `CanResizeColumns="True"` lets a heading's trailing edge be dragged, and a double-click on that edge fits the column to its content. `FrozenColumns="2"` keeps the leading columns in place while the rows scroll sideways under them, with a shade at their edge. `IsCellNavigation="True"` moves the keyboard from cell to cell as a spreadsheet does (arrows, Home/End, Ctrl+Home/End, Tab, Enter or F2 to edit); `CurrentRow` and `CurrentColumn` say where it is. Events `Sorting`, `CellEditing`, `SelectionChanged` and `RowActivated` (Enter or a double-click). Only the rows in or near view are in the tree (rows just scrolled past wait hidden until the scroll rests), so thousands of rows cost what a screenful does; auto columns size to the widest cell seen so far |
| Find | `<ins:FindBar />` above a page is Ctrl+F's bar: a field, the match count, previous and next, and close. It searches every `TextBlock` inside a container marked `ins:Find.IsSearchable="True"` (or only its `Target`), Arabic-aware (diacritics, tatweel, hamza seats, ة/ه, ى/ي and both sets of digits), highlights the matches and scrolls the current one into view. Enter and Shift+Enter (or F3 and Shift+F3) step, Esc closes and gives the keyboard back. `IsOpen`, `Text`, `MatchCount`, `CurrentMatch`; `Open()`, `Close()`, `Step(1)` from code |
| Month calendar | `<ins:MonthCalendar>` with `ins:MonthEvent`s written inside it (or `Events`): `Date`, `Title`, `Time` (unset, the event takes the whole day), `Duration` (an hour unless set), `Tone`, `Href` for a link. `View="Month"`, `"Week"` or `"Day"`: the weeks of `Month`, or `Date`'s week or day by the hour between `DayStartHour` (8) and `DayEndHour` (20), each hour `HourHeight` tall, events side by side where they overlap and a line at now. `HasViewSwitch="True"` adds the month, week and day switch to the header. Events `MonthChanged`, `DateChanged`, `ViewChanged`, `DayInvoked` (with the half hour pressed in the week and day views) and `EventInvoked`; `Locale` decides the names and the first day of the week |
| Fonts | Tajawal, Inter and Cause are embedded; the body font follows the direction (Tajawal in RTL, Inter in LTR) and LTR headings use Cause; a weight past Bold draws the Bold face, as a browser does, never a synthetic bold |

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
| `tools` | generators: `gen-tokens.mjs` (tokens), `brand-fixture.mjs` (the brand parity cases from insiyab.js), `fonts/build-fonts.mjs` (the embedded fonts); the first two take `--check`; `BrandGen` (the logo files in `/brand`, the gallery and PerfLab icons, and the gallery's `BrandMark.axaml`: `dotnet run --project tools/BrandGen -- ../..`) |

`node test/run.mjs avalonia` from the repository root checks that the generated files are current and runs the headless tests.
