// Tabs, menus, tooltips, drawers, dismiss, show-password, validation and the navbar,
// driven with real key and pointer events on test/fixtures/components.html.
import { launch, BASE } from './lib/cdp.mjs';
import { suite } from './lib/check.mjs';

const { ok, end } = suite();
const b = await launch();
const E = (js) => b.evaluate(js);

await b.size(1440, 1000);
await b.goto(`${BASE}/test/fixtures/components.html`);

ok('script flag stamped', await E(`document.documentElement.hasAttribute('data-ins-js')`));

// ------------------------------------------------------------------ tabs
const t = await E(`(() => {
  const list = document.querySelector('.ins-tablist');
  const tabs = [...list.querySelectorAll('[data-ins-tab]')];
  const vis = (id) => getComputedStyle(document.getElementById(id)).display !== 'none';
  return {
    listRole: list.getAttribute('role'),
    roles: tabs.map(x => x.getAttribute('role')).join(','),
    selected: tabs.map(x => x.getAttribute('aria-selected')).join(','),
    tabindex: tabs.map(x => x.getAttribute('tabindex')).join(','),
    panelRole: document.getElementById('tab-general').getAttribute('role'),
    labelled: document.getElementById('tab-general').getAttribute('aria-labelledby') === tabs[0].id,
    visible: ['tab-general','tab-security','tab-notify'].map(vis).join(','),
    navLinkRole: list.querySelector('a.ins-tab').getAttribute('role'),
    brandRole: document.getElementById('brand').getAttribute('role'),
  };
})()`);
ok('tabs: tablist role', t.listRole === 'tablist', t.listRole);
ok('tabs: tab roles', t.roles === 'tab,tab,tab', t.roles);
ok('tabs: first selected', t.selected === 'true,false,false', t.selected);
ok('tabs: roving tabindex', t.tabindex === '0,-1,-1', t.tabindex);
ok('tabs: panel role + labelledby', t.panelRole === 'tabpanel' && t.labelled);
ok('tabs: only first panel shown', t.visible === 'true,false,false', t.visible);
ok('tabs: page link left alone', t.navLinkRole === null && t.brandRole === null, `link=${t.navLinkRole} section=${t.brandRole}`);

await b.click('[data-ins-tab="#tab-notify"]');
ok('tabs: click selects third', await E(`getComputedStyle(document.getElementById('tab-notify')).display !== 'none' && getComputedStyle(document.getElementById('tab-general')).display === 'none'`));

await E(`document.querySelector('[data-ins-tab="#tab-general"]').focus()`);
await b.key('ArrowLeft');   // RTL: left is next
ok('tabs: ArrowLeft is next in RTL', await E(`document.activeElement.getAttribute('data-ins-tab') === '#tab-security' && document.activeElement.getAttribute('aria-selected') === 'true'`),
  await E(`document.activeElement.getAttribute('data-ins-tab')`));
await b.key('End');
ok('tabs: End goes to last', await E(`document.activeElement.getAttribute('data-ins-tab') === '#tab-notify'`));
await b.key('ArrowLeft');
ok('tabs: wraps to first', await E(`document.activeElement.getAttribute('data-ins-tab') === '#tab-general'`));

await b.click('[data-ins-tab="#seg-week"]');
ok('seg as tabs', await E(`getComputedStyle(document.getElementById('seg-week')).display !== 'none' && document.querySelector('[data-ins-tab="#seg-week"]').classList.contains('is-active') && getComputedStyle(document.getElementById('seg-day')).display === 'none'`));

// ------------------------------------------------------------------ menus
await E(`document.querySelector('#menu-actions > summary').focus()`);
await b.key('Enter');
ok('menu: Enter opens', await E(`document.getElementById('menu-actions').open`));
ok('menu: focus on first item', await E(`document.activeElement.textContent.trim().startsWith('تعديل')`), await E(`document.activeElement.textContent.trim()`));
ok('menu: roles stamped', await E(`document.querySelector('#menu-actions .ins-pop-body').getAttribute('role') === 'menu' && document.querySelector('#menu-actions > summary').getAttribute('aria-haspopup') === 'menu'`));
await b.key('ArrowDown');
await b.key('ArrowDown');
ok('menu: arrows skip disabled item', await E(`document.activeElement.textContent.trim() === 'حذف'`), await E(`document.activeElement.textContent.trim()`));
await b.key('ArrowDown');
ok('menu: arrows wrap', await E(`document.activeElement.textContent.trim().startsWith('تعديل')`));
await b.key('Escape');
ok('menu: Escape closes', await E(`!document.getElementById('menu-actions').open`));
ok('menu: focus back on trigger', await E(`document.activeElement === document.querySelector('#menu-actions > summary')`));

await b.click('#menu-columns > summary');
await b.click('#menu-columns [aria-checked="false"][role="menuitemcheckbox"]');
ok('menu: checkbox item toggles', await E(`[...document.querySelectorAll('#menu-columns [role=menuitemcheckbox]')].map(x=>x.getAttribute('aria-checked')).join(',') === 'true,true,true'`));
ok('menu: stays open for checkbox', await E(`document.getElementById('menu-columns').open`));
await b.click('#menu-columns [role="menuitemradio"][aria-checked="false"]');
ok('menu: radio moves the check', await E(`[...document.querySelectorAll('#menu-columns [role=menuitemradio]')].map(x=>x.getAttribute('aria-checked')).join(',') === 'false,true'`));
ok('menu: radio closes', await E(`!document.getElementById('menu-columns').open`));

// flip: put the trigger at the bottom of the viewport, then open. A spacer above it
// first, because a trigger near the top of the page cannot be scrolled down there.
await E(`(() => { const sp = document.createElement('div'); sp.id = 'flip-spacer'; sp.style.blockSize = '1400px';
  const m = document.querySelector('#menu-actions'); m.parentNode.parentNode.insertBefore(sp, m.parentNode);
  const r = m.getBoundingClientRect(); window.scrollBy(0, r.bottom - innerHeight + 20); })()`);
await b.sleep(150);
await E(`document.getElementById('menu-actions').open = true`);
await b.sleep(150);
ok('menu: flips up at the viewport bottom', await E(`(document.getElementById('menu-actions').getAttribute('data-ins-flip') || '').includes('up')`),
  await E(`document.getElementById('menu-actions').getAttribute('data-ins-flip')`));
ok('menu: flipped body is inside viewport', await E(`(() => { const r = document.querySelector('#menu-actions .ins-pop-body').getBoundingClientRect(); return r.top >= 0 && r.bottom <= innerHeight; })()`));
await E(`document.getElementById('menu-actions').open = false`);
await E(`document.getElementById('flip-spacer').remove(); window.scrollTo(0, 0)`);

// ------------------------------------------------------------------ tooltip
await b.hover('[data-ins-theme-toggle][data-ins-tip]');
await b.sleep(650);
const tip1 = await E(`(() => { const t = document.getElementById('ins-tooltip'); if (!t) return null;
  const btn = document.querySelector('[data-ins-theme-toggle][data-ins-tip]');
  const a = btn.getBoundingClientRect(), r = t.getBoundingClientRect();
  return { open: t.matches(':popover-open'), text: t.textContent, below: r.top >= a.bottom, desc: btn.getAttribute('aria-describedby'), opacity: getComputedStyle(t).opacity }; })()`);
ok('tooltip: shows on hover after delay', tip1 && tip1.open, JSON.stringify(tip1));
ok('tooltip: uses aria-label text', tip1 && tip1.text === 'تبديل الوضع');
ok('tooltip: side=bottom respected', tip1 && tip1.below);
ok('tooltip: not described twice', tip1 && tip1.desc === null, String(tip1 && tip1.desc));
await b.hover('[data-ins-tip^="حذف نهائي"]');
await b.sleep(150);
const tip2 = await E(`(() => { const t = document.getElementById('ins-tooltip'); const btn = document.querySelector('[data-ins-tip^="حذف نهائي"]');
  return { open: t.matches(':popover-open'), text: t.textContent, desc: btn.getAttribute('aria-describedby') }; })()`);
ok('tooltip: warm hand-off is immediate', tip2.open && tip2.text.startsWith('حذف نهائي'), JSON.stringify(tip2));
ok('tooltip: extra text linked as description', tip2.desc === 'ins-tooltip', String(tip2.desc));
await b.key('Escape');
ok('tooltip: Escape hides', await E(`!document.getElementById('ins-tooltip').matches(':popover-open')`));
ok('tooltip: description unlinked on hide', await E(`!document.querySelector('[data-ins-tip^="حذف نهائي"]').hasAttribute('aria-describedby')`));
await b.mouseTo(5, 5);

// ------------------------------------------------------------------ drawer / dismiss / dialog
await b.click('[data-ins-dialog="#drawer-start"]');
await b.sleep(450);
const dr = await E(`(() => { const d = document.getElementById('drawer-start'); const r = d.getBoundingClientRect(); return { open: d.open, right: Math.round(r.right), vw: innerWidth, left: Math.round(r.left) }; })()`);
ok('drawer: opens', dr.open);
ok('drawer: starts at the right edge in RTL', Math.abs(dr.right - dr.vw) <= 1, JSON.stringify(dr));
await b.mouse(60, 500);
await b.sleep(300);
ok('drawer: backdrop click closes', await E(`!document.getElementById('drawer-start').open`));
await b.click('[data-ins-dialog="#drawer-end"]');
await b.sleep(450);
ok('drawer--end: at the left edge in RTL', await E(`Math.round(document.getElementById('drawer-end').getBoundingClientRect().left) === 0`));
await b.key('Escape');
await b.sleep(300);
ok('drawer: Escape closes', await E(`!document.getElementById('drawer-end').open`));

await b.click('[data-ins-dialog="#demo-confirm"]');
await b.sleep(300);
await b.click('#demo-confirm .ins-dialog-foot [data-ins-dismiss]');
await b.sleep(300);
ok('dismiss: closes its dialog', await E(`!document.getElementById('demo-confirm').open`));
await b.click('#dismiss-me [data-ins-dismiss]');
await b.sleep(500);
ok('dismiss: hides its alert', await E(`document.getElementById('dismiss-me').hidden && getComputedStyle(document.getElementById('dismiss-me')).display === 'none'`));

// ------------------------------------------------------------------ password
await b.click('[data-ins-password]');
ok('password: shows', await E(`document.getElementById('v-pass').type === 'text' && document.querySelector('[data-ins-password]').getAttribute('aria-pressed') === 'true'`));
ok('password: toggle is type=button', await E(`document.querySelector('[data-ins-password]').type === 'button'`));
await b.click('[data-ins-password]');
ok('password: hides again', await E(`document.getElementById('v-pass').type === 'password'`));

// ------------------------------------------------------------------ validation
ok('validation: messages hidden before interaction', await E(`[...document.querySelectorAll('form[data-ins-validate] .ins-error')].every(e => getComputedStyle(e).display === 'none')`));
ok('validation: hint + message described', await E(`(document.getElementById('v-pass').getAttribute('aria-describedby') || '').split(' ').length === 2`),
  await E(`document.getElementById('v-pass').getAttribute('aria-describedby')`));
await b.click('#v-form .ins-btn--primary');
await b.sleep(200);
const v = await E(`(() => { const f = document.getElementById('v-form'); const errs = [...f.querySelectorAll('.ins-error')];
  return { shown: errs.map(e => getComputedStyle(e).display !== 'none').join(','), text0: errs[0].textContent, text1: errs[1].textContent,
           focus: document.activeElement.id, emailBorder: getComputedStyle(document.getElementById('v-email')).borderColor,
           groupBorder: getComputedStyle(f.querySelector('.ins-input-group')).borderColor }; })()`);
ok('validation: both messages shown after submit', v.shown === 'true,true', v.shown);
ok('validation: empty message filled from the browser', v.text0.length > 3, v.text0);
ok('validation: page-written message kept', v.text1.startsWith('كلمة المرور قصيرة'), v.text1);
ok('validation: first invalid field focused', v.focus === 'v-email', v.focus);
ok('validation: invalid input + group turn red', /(239, 68, 68|153, 27, 27)/.test(v.emailBorder) && /239, 68, 68/.test(v.groupBorder), `${v.emailBorder} / ${v.groupBorder}`);
await E(`document.getElementById('v-email').focus()`);
await b.type('a@b.co');
await b.sleep(100);
ok('validation: message clears once valid', await E(`getComputedStyle(document.querySelectorAll('#v-form .ins-error')[0]).display === 'none'`));
const srv = await E(`(() => ({ code: getComputedStyle(document.getElementById('g-code').closest('.ins-input-group')).borderColor,
  innerBg: getComputedStyle(document.getElementById('g-code')).backgroundColor, ok: getComputedStyle(document.getElementById('g-user')).borderColor }))()`);
ok('validation: server error reddens the group, not the inner input', /239, 68, 68/.test(srv.code) && srv.innerBg === 'rgba(0, 0, 0, 0)', JSON.stringify(srv));
ok('validation: success message greens the field', /21, 128, 61|0\.08\d* 0\.50\d* 0\.23\d*/.test(srv.ok), srv.ok);

// ------------------------------------------------------------------ navbar
ok('navbar wide: toggle hidden, menu shown', await E(`getComputedStyle(document.querySelector('.ins-navbar-toggle')).display === 'none' && getComputedStyle(document.querySelector('.ins-navbar-menu')).display !== 'none'`));
await b.size(800, 1000);
await b.sleep(200);
ok('navbar narrow: menu folded, toggle shown', await E(`getComputedStyle(document.querySelector('.ins-navbar-menu')).display === 'none' && getComputedStyle(document.querySelector('.ins-navbar-toggle')).display !== 'none'`));
await b.click('.ins-navbar-toggle');
ok('navbar narrow: toggle opens', await E(`getComputedStyle(document.querySelector('.ins-navbar-menu')).display !== 'none' && document.querySelector('.ins-navbar-toggle').getAttribute('aria-expanded') === 'true'`));
await b.key('Escape');
ok('navbar: Escape closes', await E(`!document.querySelector('.ins-navbar').classList.contains('is-open')`));
await b.size(1440, 1000);

// ------------------------------------------------------------------ findings
ok('scrolled top bar is frosted, on the layer behind the islands, not on the bar', await E(`(() => { const r = document.documentElement; r.setAttribute('data-ins-scrolled','');
  const bar = document.querySelector('.ins-topbar'); const v = getComputedStyle(bar, '::before').backdropFilter;
  return v !== 'none' && getComputedStyle(bar).backdropFilter === 'none'; })()`));
ok('fx-off: scrolled top bar loses its frost', await E(`(() => { const r = document.documentElement; r.setAttribute('data-ins-fx','off'); r.setAttribute('data-ins-scrolled','');
  const v = getComputedStyle(document.querySelector('.ins-topbar'), '::before').backdropFilter; r.removeAttribute('data-ins-fx'); return v === 'none'; })()`));

// ------------------------------------------------------------------ countdown on demand
const cd = (id) => E(`document.getElementById('${id}').textContent + '|' + Insiyab.countdown('#${id}')`);
ok('countdown: a paused one shows its whole time and waits', (await cd('cd-wait')) === '01:30|null', await cd('cd-wait'));
await E(`document.getElementById('cd-go').scrollIntoView({ block: 'center', behavior: 'instant' }); 0`);
await b.click('#cd-go'); await b.sleep(1300);
ok('countdown: its start button sets it running', /^01:2[89]\|(88|89)$/.test(await cd('cd-wait')), await cd('cd-wait'));
await b.click('#cd-stop'); const held = await cd('cd-wait'); await b.sleep(1300);
ok('countdown: stop holds the time left, on screen and in the API', (await cd('cd-wait')) === held && /\|(88|89)$/.test(held), `${held} → ${await cd('cd-wait')}`);
await b.click('#cd-go'); await b.sleep(100);
ok('countdown: start carries on from where it stopped', (await cd('cd-wait')) === held, `${held} → ${await cd('cd-wait')}`);
await b.click('#cd-again'); await b.sleep(100);
ok('countdown: restart begins again from the full time', (await cd('cd-wait')) === '01:30|90', await cd('cd-wait'));
await E(`Insiyab.countdown('#cd-api', 'start'); 0`);
ok("countdown: Insiyab.countdown(el, 'start') starts a paused one", (await cd('cd-api')) === '00:30|30' && !(await E(`document.getElementById('cd-api').hasAttribute('data-ins-countdown-paused')`)), await cd('cd-api'));
ok("countdown: and 'stop' returns the seconds left", (await E(`Insiyab.countdown('#cd-api', 'stop')`)) === 30);
await E(`window.dispatchEvent(new Event('blur')); 0`);
const away = await cd('cd-away'), stay = await cd('cd-stay');
await b.sleep(1300);
ok('countdown: with pause-away it holds while the window is away', (await cd('cd-away')) === away, `${away} → ${await cd('cd-away')}`);
ok('countdown: without it, it keeps counting', (await cd('cd-stay')) !== stay, `${stay} → ${await cd('cd-stay')}`);
await E(`window.dispatchEvent(new Event('focus')); 0`); await b.sleep(1300);
ok('countdown: and carries on when the window comes back', (await cd('cd-away')) !== away && !(await E(`document.getElementById('cd-away').hasAttribute('data-ins-countdown-paused')`)), `${away} → ${await cd('cd-away')}`);

ok('no console errors or exceptions', b.logs.length === 0, b.logs.join(' | '));
await b.close();
end();
