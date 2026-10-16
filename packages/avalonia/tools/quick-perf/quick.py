"""Interleaved A/B of PerfLab scenarios: a base commit (built once, cached) against the working tree. See README.md."""
import csv
import ctypes
import datetime
import hashlib
import io
import json
import os
import platform
import shutil
import statistics
import subprocess
import sys
import tarfile
import tempfile
import time
from ctypes import wintypes
from pathlib import Path

AVALONIA = Path(__file__).resolve().parents[2]
REPO = AVALONIA.parents[1]
HARNESS = AVALONIA / 'tools' / 'PerfLab'
ROOT = Path(tempfile.gettempdir()) / 'insiyab-perf'
VSWHERE_DIR = r'C:\Program Files (x86)\Microsoft Visual Studio\Installer'
KINDS = ['dashboard', 'form', 'table', 'calendar', 'documents', 'lists', 'overlays']
SCROLL_ROWS = {
    'form': [('scroll', 'long form (page)')],
    'table': [('scroll', 'table 1200 rows (inner)')],
    'revisit': [('scroll', 'table 1200 rows back up (revisit)')],
    'sideways': [('scroll', 'table sideways (frozen column)')],
    'calendar': [('scroll', 'calendar week (page)'), ('calendar view', None)],
}
PAGE_METRICS = ['warm shown ms (median)', 'warm ui ms (median)', 'first visit shown ms']
SCROLL_METRICS = ['ui work p50 ms', 'ui work p95 ms', 'gap p95 ms', 'cpu ms/frame', 'shown ms']
BUILD_METRICS = ['total ms', 'attach ms', 'layout ms', 'visuals']
MICRO_METRICS = ['attach us per element']
DETAIL_METRICS = ['create ms', 'posted ms', 'layout us per element']
NOISY = 0.15
SOURCES = ['src', 'gallery', 'tools/PerfLab', 'Directory.Build.props']
USAGE = """usage: python -I quick.py <scenario[,scenario...]> [options]
scenarios (comma separated, mix freely):
  page:<kind>      PerfLab page switch, real window (dashboard form table calendar documents lists overlays, or a gallery slug)
  scroll:<row>     PerfLab scroll row, real window: form table revisit sideways calendar
  group:<name>     a whole PerfLab group (--only <name>), its ms rows
  build:<kind>     headless page build: create + attach + layout + posted work, no render
  micro:<element>  headless attach cost of one element: Border, TextBlock, Border.card, StackPanel, Button...
options:
  --base <commit>  base side (default HEAD); the current side is always the working tree
  --jit | --aot    JIT Release build (default) or Native AOT publish
  --rounds n       interleaved rounds (default 3); borderline rows add rounds up to --max-rounds (default 6)
  --reps n         headless builds per page and process (default 8); --visits n page visits per process (default 4)
  --busy pct       wait while other processes use more than pct of the CPU (default 20), at most --wait s (default 90)
  --detail         also show the headless create and posted-work rows and micro layout
  --rebuild        rebuild both sides even if the cache stamps match"""


def fail(msg):
    print('quick: ' + msg, file=sys.stderr)
    sys.exit(2)


def parse(argv):
    opts = {'base': 'HEAD', 'aot': False, 'detail': False, 'rounds': 3, 'max_rounds': 6, 'reps': 8, 'visits': 4, 'busy': 20.0, 'wait': 90.0, 'rebuild': False}
    scen = []
    i = 0
    while i < len(argv):
        a = argv[i]
        if a in ('-h', '--help'):
            print(USAGE)
            sys.exit(0)
        elif a == '--aot':
            opts['aot'] = True
        elif a == '--jit':
            opts['aot'] = False
        elif a in ('--rebuild', '--detail'):
            opts[a[2:]] = True
        elif a.startswith('--'):
            key = a[2:].replace('-', '_')
            if key not in opts or i + 1 >= len(argv):
                fail('unknown option ' + a + '\n' + USAGE)
            i += 1
            opts[key] = type(opts[key])(argv[i])
        else:
            scen += [s for s in a.split(',') if s]
        i += 1
    if not scen:
        fail('no scenario\n' + USAGE)
    return opts, scen


def kind_of(name):
    return name if name in KINDS or name.startswith('g:') else 'g:' + name


def jobs_for(scen, opts):
    """Turns scenarios into PerfLab processes: one real-window job and one headless job, each with its row filter."""
    pages, scrolls, groups, builds, micros = [], [], [], [], []
    for s in scen:
        kind, _, arg = s.partition(':')
        if not arg:
            fail('scenario needs kind:name, got ' + s)
        if kind == 'page':
            pages.append(kind_of(arg))
        elif kind == 'scroll':
            if arg not in SCROLL_ROWS:
                fail('scroll rows: ' + ', '.join(SCROLL_ROWS))
            scrolls.append(arg)
        elif kind == 'group':
            groups.append(arg)
        elif kind == 'build':
            builds.append(kind_of(arg))
        elif kind == 'micro':
            micros.append(arg)
        else:
            fail('unknown scenario kind ' + kind)
    if opts['aot'] and any(k.startswith('g:') for k in pages + builds):
        fail('gallery pages need --jit: their playground uses the runtime XAML loader, which Native AOT cannot run (the page comes out different)')
    jobs = {}
    if pages or scrolls or groups:
        only = (['pages'] if pages else []) + (['scroll'] if scrolls else []) + groups
        args = ['--only', ','.join(only)]
        kinds = list(dict.fromkeys(pages))
        if pages:
            args += ['--kinds', ','.join(kinds), '--rounds', str(opts['visits'])]
        if scrolls:
            args += ['--scrolls', ','.join(dict.fromkeys(scrolls))]
        if not pages and 'pages' in groups:
            args += ['--rounds', str(opts['visits'])]
        want = set()
        for k in kinds:
            want |= {('page switch', k, m) for m in PAGE_METRICS}

        def keep(sec, name, metric, kinds=kinds, scrolls=scrolls, groups=groups):
            if (sec, name, metric) in want:
                return True
            for s in scrolls:
                for rs, rn in SCROLL_ROWS[s]:
                    if sec == rs and (rn is None or rn == name) and metric in SCROLL_METRICS:
                        return True
            return any(sec.startswith(g) or (g == 'pages' and sec == 'page switch') for g in groups) and 'ms' in metric
        jobs['window'] = ('--bench', args, keep)
    if builds or micros:
        args = []
        if builds:
            args += ['--pages', ','.join(dict.fromkeys(builds)), '--reps', str(opts['reps'])]
        if micros:
            args += ['--micro', ','.join(dict.fromkeys(micros))]

        def keep(sec, name, metric, detail=opts['detail']):
            return (sec == 'build' and metric in BUILD_METRICS) or (sec == 'micro' and metric in MICRO_METRICS) or (detail and metric in DETAIL_METRICS)
        jobs['headless'] = ('--headless', args, keep)
    return jobs


def git(*args, text=True):
    return subprocess.run(['git', '-C', str(REPO)] + list(args), capture_output=True, text=text, check=True).stdout


def tree_hash(base, rels):
    h = hashlib.sha1()
    for rel in rels:
        p = base / rel
        files = [p] if p.is_file() else sorted(f for f in p.rglob('*') if f.is_file() and not {'bin', 'obj'} & set(f.relative_to(p).parts))
        for f in files:
            h.update(f.relative_to(base).as_posix().encode())
            h.update(f.read_bytes())
    return h.hexdigest()[:16]


def env_for_build():
    env = dict(os.environ)
    env['PATH'] = VSWHERE_DIR + os.pathsep + env.get('PATH', '')
    env['DOTNET_CLI_TELEMETRY_OPTOUT'] = '1'
    return env


def build(project_dir, art, aot, stamp_value, rebuild):
    """Builds PerfLab into art unless its stamp says the same sources are already built; returns the exe and the seconds spent."""
    art.mkdir(parents=True, exist_ok=True)
    stamp = art / 'stamp.txt'
    exe = art / ('publish/PerfLab/release_win-x64/PerfLab.exe' if aot else 'bin/PerfLab/release/PerfLab.exe')
    if not rebuild and exe.exists() and stamp.exists() and stamp.read_text() == stamp_value:
        return exe, 0.0
    t = time.time()
    cmd = ['dotnet', 'publish' if aot else 'build', str(project_dir), '-c', 'Release', '--artifacts-path', str(art), '-nologo', '-v', 'q']
    if aot:
        cmd += ['-r', 'win-x64', '-p:PublishAot=true']
    r = subprocess.run(cmd, capture_output=True, text=True, env=env_for_build())
    if r.returncode != 0 or not exe.exists():
        fail('build failed: ' + ' '.join(cmd) + '\n' + r.stdout[-4000:] + r.stderr[-2000:])
    stamp.write_text(stamp_value)
    return exe, time.time() - t


def base_side(commit, aot, rebuild):
    """Exports the base commit's library and gallery once, overlays the working tree's PerfLab so both sides run the same harness, and builds it."""
    full = git('rev-parse', commit + '^{commit}').strip()
    home = ROOT / full[:12]
    tree = home / 'tree'
    if not (tree / 'packages/avalonia/src').exists():
        tar = git('archive', '--format=tar', full, 'packages/avalonia/src', 'packages/avalonia/gallery', 'packages/avalonia/Directory.Build.props', text=False)
        tarfile.open(fileobj=io.BytesIO(tar)).extractall(tree)
    harness = tree_hash(AVALONIA, ['tools/PerfLab'])
    lab = tree / 'packages/avalonia/tools/PerfLab'
    mark = home / 'harness.txt'
    if not mark.exists() or mark.read_text() != harness:
        for f in [f for f in lab.rglob('*') if f.is_file() and not {'bin', 'obj'} & set(f.relative_to(lab).parts)] if lab.exists() else []:
            f.unlink()
        shutil.copytree(HARNESS, lab, dirs_exist_ok=True, ignore=shutil.ignore_patterns('bin', 'obj'))
        mark.write_text(harness)
    exe, secs = build(lab, home / ('aot' if aot else 'jit'), aot, harness, rebuild)
    return full, exe, secs


def current_side(aot, rebuild):
    exe, secs = build(HARNESS, ROOT / 'current' / ('aot' if aot else 'jit'), aot, tree_hash(AVALONIA, SOURCES), rebuild)
    return exe, secs


def system_times():
    idle, kernel, user = wintypes.FILETIME(), wintypes.FILETIME(), wintypes.FILETIME()
    ctypes.windll.kernel32.GetSystemTimes(ctypes.byref(idle), ctypes.byref(kernel), ctypes.byref(user))
    return [(t.dwHighDateTime << 32) | t.dwLowDateTime for t in (idle, kernel, user)]


def busy_pct(seconds=1.0):
    """The share of all cores busy over the last second; this script sleeps meanwhile, so it is the other processes."""
    a = system_times()
    time.sleep(seconds)
    b = system_times()
    idle, total = b[0] - a[0], (b[1] - a[1]) + (b[2] - a[2])
    return 100.0 * (1 - idle / total) if total else 0.0


def wait_quiet(limit, cap):
    start = time.time()
    busy = busy_pct()
    while busy > limit and time.time() - start < cap:
        busy = busy_pct()
    return busy, time.time() - start


def run_one(exe, flag, args, out, keep):
    if out.exists():
        shutil.rmtree(out)
    cmd = [str(exe), flag, str(out)] + args
    t = time.time()
    try:
        subprocess.run(cmd, timeout=900)
    except subprocess.TimeoutExpired:
        fail('timed out: ' + ' '.join(cmd))
    for bad in ('aborted.txt', 'error.txt'):
        if (out / bad).exists():
            fail(f'{bad} from {" ".join(cmd)}:\n' + (out / bad).read_text(encoding='utf-8')[:3000])
    rows = {}
    with open(out / 'report.csv', encoding='utf-8') as f:
        for r in csv.DictReader(f):
            if keep(r['section'], r['name'], r['metric']):
                rows[' | '.join((r['section'], r['name'], r['metric']))] = float(r['value'])
    if not rows:
        fail('no rows matched from ' + ' '.join(cmd))
    return rows, time.time() - t


def pct95(v):
    s = sorted(v)
    return s[max(0, min(len(s) - 1, -(-len(s) * 95 // 100) - 1))]


def trimmed_range(v):
    """Max - min, without the slowest sample once there are 4: interference only ever makes a run slower."""
    s = sorted(v)
    s = s[:-1] if len(s) >= 4 else s
    return s[-1] - s[0]


def judge(key, b, c):
    mb, mc = statistics.median(b), statistics.median(c)
    n = min(len(b), len(c))
    spread = max(trimmed_range(b), trimmed_range(c))
    floor = max(spread, 0.02 * abs(mb), 0.01)
    delta = mc - mb
    ratio = abs(delta) / floor
    noisy = spread > NOISY * abs(mb)
    if key.endswith('| visuals'):
        verdict, border = ('same' if mb == mc else 'CHANGED'), False
    else:
        verdict = 'same' if ratio <= 1 else ('faster' if delta < 0 else 'SLOWER') + ('?' if ratio <= 1.5 else '')
        verdict += ' (noisy)' if noisy else ''
        border = 2 / 3 < ratio <= 1.5 or (noisy and n < 4)
    return {'base_median': mb, 'cur_median': mc, 'base_p95': pct95(b), 'cur_p95': pct95(c), 'spread': spread,
            'delta': delta, 'delta_pct': 100 * delta / mb if mb else 0.0, 'ratio': ratio, 'verdict': verdict, 'borderline': border,
            'n': n, 'base': b, 'cur': c}


def machine():
    info = {'os': platform.platform(), 'cores': os.cpu_count()}
    try:
        import winreg
        k = winreg.OpenKey(winreg.HKEY_LOCAL_MACHINE, r'HARDWARE\DESCRIPTION\System\CentralProcessor\0')
        info['cpu'] = winreg.QueryValueEx(k, 'ProcessorNameString')[0].strip()
    except OSError:
        pass

    class MEM(ctypes.Structure):
        _fields_ = [('len', wintypes.DWORD), ('load', wintypes.DWORD)] + [(n, ctypes.c_ulonglong) for n in ('total', 'avail', 'pt', 'pa', 'vt', 'va', 've')]
    m = MEM()
    m.len = ctypes.sizeof(MEM)
    if ctypes.windll.kernel32.GlobalMemoryStatusEx(ctypes.byref(m)):
        info['ram_gb'] = round(m.total / 2 ** 30, 1)
    return info


def fmt(v):
    return f'{v:.0f}' if abs(v) >= 100 else f'{v:.1f}' if abs(v) >= 10 else f'{v:.2f}'


def main():
    opts, scen = parse(sys.argv[1:])
    t0 = time.time()
    aot = opts['aot']
    mode = 'aot' if aot else 'jit'
    jobs = jobs_for(scen, opts)
    print(f'quick: building base {opts["base"]} and the working tree ({mode})...', flush=True)
    base_hash, base_exe, base_secs = base_side(opts['base'], aot, opts['rebuild'])
    cur_exe, cur_secs = current_side(aot, opts['rebuild'])
    dirty = bool(git('status', '--porcelain', '--', 'packages/avalonia/src', 'packages/avalonia/gallery').strip())
    print(f'quick: base {base_hash[:10]} {"built in %.0f s" % base_secs if base_secs else "cached"}; current {"built in %.0f s" % cur_secs if cur_secs else "cached"}', flush=True)
    stamp = datetime.datetime.now().strftime('%Y%m%d-%H%M%S')
    run_dir = ROOT / 'runs' / stamp
    samples = {}
    notes = []
    procs = []

    def round_(i, names):
        for name in names:
            flag, args, keep = jobs[name]
            order = [('base', base_exe), ('cur', cur_exe)] if i % 2 == 0 else [('cur', cur_exe), ('base', base_exe)]
            for side, exe in order:
                busy, waited = wait_quiet(opts['busy'], opts['wait'])
                if waited >= 2 or busy > opts['busy']:
                    notes.append(f'round {i + 1} {name} {side}: waited {waited:.0f} s for a quiet machine, other processes at {busy:.0f}% when it started' + (' (STILL BUSY)' if busy > opts['busy'] else ''))
                rows, secs = run_one(exe, flag, args, run_dir / f'{name}-{side}-{i + 1}', keep)
                procs.append({'round': i + 1, 'job': name, 'side': side, 'seconds': round(secs, 1), 'busy_pct': round(busy, 1)})
                print(f'  round {i + 1} {name:8} {side:4} {secs:5.0f} s  (machine {busy:.0f}% busy)', flush=True)
                for k, v in rows.items():
                    samples.setdefault(k, {'base': [], 'cur': [], 'job': name})[side].append(v)

    for i in range(opts['rounds']):
        round_(i, list(jobs))
    rounds = opts['rounds']
    while rounds < opts['max_rounds']:
        verdicts = {k: judge(k, s['base'], s['cur']) for k, s in samples.items() if s['base'] and s['cur']}
        again = sorted({samples[k]['job'] for k, v in verdicts.items() if v['borderline']})
        if not again:
            break
        print(f'quick: borderline rows, one more round of {", ".join(again)}', flush=True)
        round_(rounds, again)
        rounds += 1
    verdicts = {k: judge(k, s['base'], s['cur']) for k, s in samples.items() if s['base'] and s['cur']}

    width = max(len(k) for k in verdicts)
    print()
    print(f'{"row":{width}}  {"base":>7} {"cur":>7} {"delta":>16} {"spread":>14} {"p95 b/c":>15}  n  verdict')
    for k, v in verdicts.items():
        sp = f'{fmt(v["spread"])} ({100 * v["spread"] / v["base_median"] if v["base_median"] else 0:.0f}%)'
        d = f'{v["delta"]:+.1f} ({v["delta_pct"]:+.1f}%)'
        p = f'{fmt(v["base_p95"])}/{fmt(v["cur_p95"])}'
        tag = v['verdict'] + (' (borderline)' if v['borderline'] else '')
        print(f'{k:{width}}  {fmt(v["base_median"]):>7} {fmt(v["cur_median"]):>7} {d:>16} {sp:>14} {p:>15}  {v["n"]}  {tag}')
    for n in notes:
        print('note: ' + n)
    if not dirty and git('rev-parse', 'HEAD').strip() == base_hash:
        print('note: the working tree matches the base (an A/A run)')
    wall = time.time() - t0
    result = {'date': datetime.datetime.now().isoformat(timespec='seconds'), 'base': base_hash, 'head': git('rev-parse', 'HEAD').strip(),
              'working_tree_dirty': dirty, 'mode': mode, 'scenarios': scen, 'options': opts, 'rounds': rounds,
              'wall_seconds': round(wall), 'build_seconds': {'base': round(base_secs), 'cur': round(cur_secs)},
              'machine': machine(), 'notes': notes, 'processes': procs, 'rows': verdicts}
    run_dir.mkdir(parents=True, exist_ok=True)
    (run_dir / 'result.json').write_text(json.dumps(result, indent=1, ensure_ascii=False), encoding='utf-8')
    with open(ROOT / 'baselines.jsonl', 'a', encoding='utf-8') as f:
        f.write(json.dumps({'date': result['date'], 'commit': base_hash, 'mode': mode, 'machine': result['machine'],
                            'rows': {k: v['base'] for k, v in verdicts.items()}}, ensure_ascii=False) + '\n')
    print(f'\nquick: {rounds} rounds, wall {wall / 60:.1f} min; {run_dir / "result.json"}')


if __name__ == '__main__':
    main()
