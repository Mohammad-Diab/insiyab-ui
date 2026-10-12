// A large field inside an input group makes the group as tall as a large field; a normal group keeps the normal height.
import { launch, BASE } from './lib/cdp.mjs';
import { suite } from './lib/check.mjs';
const { ok, end } = suite();
const b = await launch();
const E = (js) => b.evaluate(js);

await b.size(1440, 1000);
await b.goto(`${BASE}/test/fixtures/components.html`);
await E(`(() => {
  const f = document.createElement('div');
  f.style.cssText = 'position:fixed;inset:20px auto auto 320px;z-index:99999;display:grid;gap:14px;inline-size:420px;padding:16px;background:#fff';
  f.innerHTML = \`
    <input class="ins-input ins-input--lg" id="s-lg">
    <div class="ins-input-group" id="s-lg-group"><input class="ins-input ins-input--lg" id="s-lg-pass" type="password"><button type="button" class="ins-password-toggle" data-ins-password aria-label="إظهار كلمة المرور"></button></div>
    <div class="ins-input-group" id="s-lg-addon"><input class="ins-input ins-input--lg" id="s-lg-price"><span class="ins-addon">ل.س</span></div>
    <input class="ins-input" id="s-md">
    <div class="ins-input-group" id="s-md-group"><input class="ins-input" id="s-md-pass" type="password"><button type="button" class="ins-password-toggle" data-ins-password aria-label="إظهار كلمة المرور"></button></div>\`;
  document.body.appendChild(f); return 0; })()`);
await b.sleep(300);
const h = (id) => E(`Math.round(document.getElementById(${JSON.stringify(id)}).getBoundingClientRect().height)`);

const lg = await h('s-lg');
const md = await h('s-md');
ok('large field: 52px', lg === 52, String(lg));
ok('large password group: as tall as the large field', (await h('s-lg-group')) === lg, String(await h('s-lg-group')));
ok('large group with an addon: as tall as the large field', (await h('s-lg-addon')) === lg, String(await h('s-lg-addon')));
ok('normal field: 46px', md === 46, String(md));
ok('normal group: unchanged, as tall as the normal field', (await h('s-md-group')) === md, String(await h('s-md-group')));

await b.hover('#s-lg-pass'); await b.sleep(300);
ok('large password group: keeps its height under the pointer', (await h('s-lg-group')) === lg, String(await h('s-lg-group')));
await b.click('#s-lg-pass'); await b.sleep(300);
ok('large password group: keeps its height when focused', (await h('s-lg-group')) === lg, String(await h('s-lg-group')));

ok('no console errors or exceptions', b.logs.length === 0, b.logs.join(' | '));
await b.close();
end();
