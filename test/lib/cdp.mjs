/* A minimal DevTools-protocol driver, on a throwaway Chrome profile.

   Why not Puppeteer or Playwright: the library has no dependencies, and neither do
   its tests. This is the part of one that the tests actually use — navigate, press
   a key, click, hover, evaluate, screenshot — driven through real input events, so
   what is tested is what a person does, not a synthetic `el.click()`.

   Always a fresh `--user-data-dir`: a test must never run in somebody's real
   browser profile, and a stored theme or brand from one run must not leak into the
   next. Chrome picks its own debugging port (`--remote-debugging-port=0`) and
   writes it to the profile, so two test files can never collide on one. */
import { spawn } from 'node:child_process';
import { mkdtemp, readFile, rm, writeFile } from 'node:fs/promises';
import { existsSync } from 'node:fs';
import { join } from 'node:path';
import { tmpdir } from 'node:os';

export const BASE = process.env.INS_BASE || 'http://localhost:4173';

const sleep = (ms) => new Promise((r) => setTimeout(r, ms));

function findChrome() {
  if (process.env.CHROME) return process.env.CHROME;
  const candidates = [
    'C:/Program Files/Google/Chrome/Application/chrome.exe',
    'C:/Program Files (x86)/Google/Chrome/Application/chrome.exe',
    process.env.LOCALAPPDATA && join(process.env.LOCALAPPDATA, 'Google/Chrome/Application/chrome.exe'),
    '/Applications/Google Chrome.app/Contents/MacOS/Google Chrome',
    '/usr/bin/google-chrome',
    '/usr/bin/google-chrome-stable',
    '/usr/bin/chromium',
    '/usr/bin/chromium-browser',
  ].filter(Boolean);
  const found = candidates.find((p) => existsSync(p));
  if (!found) throw new Error('Chrome not found. Set CHROME to its path.');
  return found;
}

export async function launch({ width = 1440, height = 1000 } = {}) {
  const profile = await mkdtemp(join(tmpdir(), 'ins-test-'));
  const chrome = spawn(findChrome(), [
    '--headless=new', '--disable-gpu', '--hide-scrollbars', '--no-first-run',
    '--no-default-browser-check', `--user-data-dir=${profile}`,
    '--remote-debugging-port=0', 'about:blank',
  ], { stdio: 'ignore' });

  let port = null;
  for (let i = 0; i < 100 && !port; i++) {
    await sleep(100);
    try { port = (await readFile(join(profile, 'DevToolsActivePort'), 'utf8')).split('\n')[0].trim(); } catch {}
  }
  let target;
  for (let i = 0; i < 50 && port && !target; i++) {
    try {
      const list = await (await fetch(`http://127.0.0.1:${port}/json`)).json();
      target = list.find((t) => t.type === 'page');
    } catch {}
    if (!target) await sleep(100);
  }
  if (!target) { chrome.kill(); throw new Error('no Chrome page target'); }

  const ws = new WebSocket(target.webSocketDebuggerUrl);
  await new Promise((r) => ws.addEventListener('open', r));
  let seq = 0;
  const pending = new Map();
  const handlers = {};
  const logs = [];
  ws.addEventListener('message', (e) => {
    const msg = JSON.parse(e.data);
    if (msg.id && pending.has(msg.id)) { pending.get(msg.id)(msg); pending.delete(msg.id); }
    if (msg.method && handlers[msg.method]) handlers[msg.method](msg.params);
    if (msg.method === 'Runtime.exceptionThrown') logs.push('EXCEPTION ' + JSON.stringify(msg.params.exceptionDetails.exception?.description || msg.params.exceptionDetails.text));
    if (msg.method === 'Runtime.consoleAPICalled' && (msg.params.type === 'error' || msg.params.type === 'warning')) {
      logs.push(msg.params.type.toUpperCase() + ' ' + msg.params.args.map((a) => a.value ?? a.description).join(' '));
    }
  });
  const send = (method, params = {}) => new Promise((resolve) => {
    const id = ++seq; pending.set(id, resolve);
    ws.send(JSON.stringify({ id, method, params }));
  });
  /* Waits on a promise the expression returns — so an expression that merely
     *starts* something (`Insiyab.confirm(…).then(…)`) must end in `; 0`, or this
     waits for a dialog nobody will answer. */
  const evaluate = async (expression) => {
    const r = await send('Runtime.evaluate', { expression, returnByValue: true, awaitPromise: true });
    if (r.result?.exceptionDetails) throw new Error(r.result.exceptionDetails.exception?.description || 'eval failed');
    return r.result?.result?.value;
  };

  await send('Page.enable');
  await send('Runtime.enable');

  const KEYS = {
    Escape: { code: 'Escape', keyCode: 27 }, Enter: { code: 'Enter', keyCode: 13 },
    Tab: { code: 'Tab', keyCode: 9 }, ' ': { code: 'Space', keyCode: 32 },
    ArrowDown: { code: 'ArrowDown', keyCode: 40 }, ArrowUp: { code: 'ArrowUp', keyCode: 38 },
    ArrowLeft: { code: 'ArrowLeft', keyCode: 37 }, ArrowRight: { code: 'ArrowRight', keyCode: 39 },
    Home: { code: 'Home', keyCode: 36 }, End: { code: 'End', keyCode: 35 },
    PageUp: { code: 'PageUp', keyCode: 33 }, PageDown: { code: 'PageDown', keyCode: 34 },
  };
  const centre = (selector) => evaluate(`(() => { const el = document.querySelector(${JSON.stringify(selector)});
    if (!el) return null; el.scrollIntoView({ block: 'center', behavior: 'instant' });
    const r = el.getBoundingClientRect(); return { x: r.left + r.width / 2, y: r.top + r.height / 2 }; })()`);

  const api = {
    send, evaluate, logs, sleep,
    on(method, fn) { handlers[method] = fn; },
    async size(w, h = height) {
      await send('Emulation.setDeviceMetricsOverride', { width: w, height: h, deviceScaleFactor: 1, mobile: w < 600 });
    },
    async goto(url) {
      await send('Page.navigate', { url });
      await sleep(1200);
      await evaluate('document.fonts.ready.then(() => true)');
    },
    async key(name, modifiers = 0) {
      const k = KEYS[name] || { code: name, keyCode: name.toUpperCase().charCodeAt(0) };
      const base = { key: name, code: k.code, windowsVirtualKeyCode: k.keyCode, modifiers };
      await send('Input.dispatchKeyEvent', { type: 'keyDown', ...base, text: name === 'Enter' ? '\r' : (name === ' ' ? ' ' : undefined) });
      await send('Input.dispatchKeyEvent', { type: 'keyUp', ...base });
      await sleep(60);
    },
    async type(text) {
      for (const ch of text) await send('Input.insertText', { text: ch });
      await sleep(60);
    },
    /* Scrolls the element to the centre first. A test that must click where the
       element already is (a hand-scrolled sidebar) uses pointAt + mouse instead. */
    async click(selector) {
      const c = await centre(selector);
      if (!c) throw new Error('no element ' + selector);
      await sleep(80);
      await api.mouse(c.x, c.y);
      await sleep(120);
    },
    async mouse(x, y, modifiers = 0) {
      for (const type of ['mouseMoved', 'mousePressed', 'mouseReleased']) {
        await send('Input.dispatchMouseEvent', { type, x, y, button: 'left', clickCount: 1, modifiers });
      }
    },
    async hover(selector) {
      const c = await centre(selector);
      if (!c) throw new Error('no element ' + selector);
      await sleep(60);
      await send('Input.dispatchMouseEvent', { type: 'mouseMoved', x: c.x, y: c.y });
    },
    async mouseTo(x, y) { await send('Input.dispatchMouseEvent', { type: 'mouseMoved', x, y }); },
    async shot(path, clip) {
      const r = await send('Page.captureScreenshot', { format: 'png', captureBeyondViewport: !!clip, ...(clip ? { clip: { ...clip, scale: 1 } } : {}) });
      await writeFile(path, Buffer.from(r.result.data, 'base64'));
    },
    async close() {
      ws.close();
      chrome.kill();
      await sleep(300);
      await rm(profile, { recursive: true, force: true }).catch(() => {});
    },
  };
  return api;
}
