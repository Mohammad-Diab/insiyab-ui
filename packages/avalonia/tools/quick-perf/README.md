# quick-perf

One command for a before/after performance check: a base commit against the working tree, run interleaved in the same session, judged row by row.

```bash
python -I packages/avalonia/tools/quick-perf/quick.py page:dashboard,page:month,scroll:table,build:month,build:tables
python -I packages/avalonia/tools/quick-perf/quick.py build:month,build:tables,build:inputs,micro:Border,micro:TextBlock
python -I packages/avalonia/tools/quick-perf/quick.py scroll:table --aot
```

## Scenarios

| scenario | runs | rows |
|---|---|---|
| `page:<kind>` | PerfLab `--bench --only pages --kinds <kind>`, real window | warm shown, warm ui, first visit shown (ms) |
| `scroll:<row>` | PerfLab `--bench --only scroll --scrolls <row>`; rows `form`, `table`, `revisit`, `sideways`, `calendar` | ui work p50/p95, gap p95, cpu per frame |
| `group:<name>` | PerfLab `--bench --only <name>` (theme, popups, idle...) | every ms row of the group |
| `build:<kind>` | PerfLab `--headless --pages <kind>`: no window, no GPU | total, attach (styling), layout (templates) in ms, visual count; `--detail` adds create and posted work |
| `micro:<element>` | PerfLab `--headless --micro <element>`: 2000 elements into the page host | attach µs per element; `--detail` adds layout |

A kind is one of PerfLab's pages (`dashboard form table calendar documents lists overlays`) or a gallery slug (`month`, `tables`, `inputs`..., the slugs in `GalleryPages.Nav`). An element is `Border`, `TextBlock`, `StackPanel`, `Canvas`, `Panel` or `Button`, with classes after dots (`Border.card`).

## How it works

- **Base side:** `--base <commit>` (default `HEAD`). The commit's `src/`, `gallery/` and `Directory.Build.props` are exported once to `%TEMP%\insiyab-perf\<hash>\tree`, the working tree's `tools/PerfLab` is copied over it (so both sides run the same harness and only the library differs), and it is built once per mode. It is reused while the commit and the harness are the same.
- **Current side:** the working tree, built to `%TEMP%\insiyab-perf\current\<mode>`; skipped when no file under `src/`, `gallery/` or `tools/PerfLab` changed since the last build.
- **Modes:** `--jit` (default) is a Release build, fast to rebuild. `--aot` is the Native AOT publish (minutes per side; the script puts `vswhere` on PATH itself). Confirm anything that touches render or scroll with `--aot` before calling it done.
- **Rounds:** each round runs base and current once per job (the real-window job and the headless job are separate processes), ABBA order across rounds. Default 3 rounds. Borderline rows (see below) get one more round of their job at a time, up to `--max-rounds` (6).
- **Samples:** one per process and row. A page-switch figure is PerfLab's median of the warm visits; a headless figure is the lower quartile of the reps (interference only adds time). The headless timer builds every page once before timing any and runs without background GC (`GCLatencyMode.Batch`): without both, A/A runs showed 20-30% swings.
- **Verdict per row:** delta = current median - base median; spread = the larger of the two sides' max - min, leaving out each side's slowest sample once there are 4 (at least 2% of the base median). `same` when |delta| <= spread; `faster?`/`SLOWER?` when |delta| is 1 to 1.5 spreads (borderline, not proven); `faster`/`SLOWER` beyond. Borderline rows are those with |delta| / spread between 2/3 and 1.5, plus noisy rows at 3 rounds. `(noisy)` marks a spread above 15% of the median: such a row cannot show a smaller change, so judge that work with the headless timer or more `--visits`. A changed visual count shows `CHANGED`.
- **Quiet machine:** before each process the script reads the whole machine's CPU over one second (it sleeps meanwhile, so that is other processes) and waits while it is above `--busy` (20% of all cores) for at most `--wait` (90 s); `--busy 10` is stricter on a quiet machine. Waits and a still-busy start are printed as notes and saved in the JSON.
- **Output:** the table, then `%TEMP%\insiyab-perf\runs\<time>\result.json` (every sample, verdict, note, process time and machine info) and the reports of each process beside it. Each run's base numbers are appended to `%TEMP%\insiyab-perf\baselines.jsonl` with the commit, date and machine, for reference only: verdicts always come from the A/B inside one run.
- `--reps n` sets the headless builds per page and process (default 8, after 2 warm-ups); `--visits n` the page visits per process (default 4: one first visit and three warm); `--rebuild` ignores the cache stamps.

## Limits

- The headless timer (`build:`, `micro:`) never renders: it sees creation, styling, templates and layout, but not composition, drawing or frame pacing. Use it for page-build and styling work; use `page:` and `scroll:` (then `--aot`) for anything that can change what a frame costs.
- `page:` and `scroll:` open a real PerfLab window that does not take focus. Leave it alone while it runs; a locked screen changes frame pacing, so gap rows are only comparable within one unlocked session.
- Gallery pages (`g:` kinds) are JIT only: their playground parses XAML at runtime, which Native AOT cannot do, so under AOT the page is a different tree. Confirm with `--aot` on PerfLab's own pages.
- The base harness is the working tree's PerfLab compiled against the old library, so a base far behind the current API may not build.

## PerfLab options added for this

- `--kinds dashboard,g:month` limits the page switches (and adds gallery pages as `g:<slug>` links); `--rounds n` sets the visits per page.
- `--scrolls form,table,revisit,sideways,calendar` limits the scroll rows.
- `--headless <dir> --pages ... --reps n --micro ...` is the headless timer (PageTimer.cs); it writes `report.csv` like `--bench`.
