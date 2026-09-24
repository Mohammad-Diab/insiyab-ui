// The file upload plugin: the real input kept and written back, adding with
// `multiple` and replacing without, the accept / size / count limits with their
// messages, drop, paste, removal, the upload hook (progress, done, fail, the ids
// sent in place of files), a required field, reset, the API and the compact look.
import { launch, BASE } from './lib/cdp.mjs';
import { suite } from './lib/check.mjs';

const { ok, end } = suite();
const b = await launch();
const E = (js) => b.evaluate(js);

/* Files made in the page, and the three ways they arrive: the picker (the input's
   own files, then its change event), a drop on the zone, a paste. */
const HELPERS = `(() => {
  window.__mk = (name, size, type) => new File([new Uint8Array(size)], name, { type: type || '', lastModified: 1700000000000 });
  window.__files = (specs) => specs.map((s) => __mk(...s));
  window.__dt = (files) => { const d = new DataTransfer(); files.forEach((f) => d.items.add(f)); return d; };
  window.__pick = (id, specs) => { const i = document.getElementById(id); i.files = __dt(__files(specs)).files; i.dispatchEvent(new Event('change', { bubbles: true })); return 0; };
  window.__drop = (id, specs) => { const zone = document.getElementById(id).parentNode.querySelector('.ins-file-zone'); const d = __dt(__files(specs));
    for (const type of ['dragenter', 'dragover']) zone.dispatchEvent(new DragEvent(type, { dataTransfer: d, bubbles: true, cancelable: true }));
    window.__over = zone.parentNode.classList.contains('is-over');
    zone.dispatchEvent(new DragEvent('drop', { dataTransfer: d, bubbles: true, cancelable: true })); return 0; };
  window.__changes = {}; document.addEventListener('change', (e) => { __changes[e.target.id] = (__changes[e.target.id] || 0) + 1; });
  window.__added = []; document.addEventListener('ins:file', (e) => __added.push(e.detail.input.id + ':' + e.detail.file.name + ':' + e.detail.upload));
  return 0;
})()`;
const view = (id) => E(`(() => {
  const i = document.getElementById(${JSON.stringify(id)}), box = i.parentNode;
  const rows = [...box.querySelectorAll('.ins-file-item')];
  return { files: [...i.files].map((f) => f.name), rows: rows.map((r) => r.querySelector('.ins-file-name').textContent),
           thumbs: rows.map((r) => r.querySelector('img') ? 'img' : r.querySelector('.ins-file-thumb').textContent),
           sizes: rows.map((r) => r.querySelector('.ins-file-size').textContent),
           issues: box.querySelector('.ins-file-issues').hidden ? [] : [...box.querySelector('.ins-file-issues').children].map((x) => x.textContent),
           listHidden: box.querySelector('.ins-file-list').hidden, valid: i.validity.valid, msg: i.validationMessage };
})()`);

await b.size(1280, 900);
await b.goto(`${BASE}/test/fixtures/file.html`);
await E(HELPERS);

// ------------------------------------------------------------------ what it builds
const built = await E(`(() => {
  const i = document.getElementById('f-docs'), box = i.parentNode, zone = box.querySelector('.ins-file-zone'), r = i.getBoundingClientRect();
  return { box: box.className, inField: box.parentNode.classList.contains('ins-field'), name: i.name, tab: i.tabIndex, tiny: r.width <= 1 && r.height <= 1,
           zoneHidden: zone.getAttribute('aria-hidden'), lead: zone.querySelector('.ins-file-lead').textContent, note: (zone.querySelector('.ins-file-note') || {}).textContent,
           one: document.querySelector('#f-one ~ .ins-file-zone .ins-file-lead').textContent,
           own: document.querySelector('#f-req ~ .ins-file-zone .ins-file-note').textContent, listHidden: box.querySelector('.ins-file-list').hidden };
})()`);
ok('the input is kept, wrapped, with its name, out of sight but in the tab order', built.box === 'ins-file' && built.inField && built.name === 'docs' && built.tab === 0 && built.tiny, JSON.stringify(built));
ok('the zone is for the pointer, hidden from assistive tech', built.zoneHidden === 'true');
ok('the zone asks in the plural for a multiple field', built.lead === 'اسحب الملفات إلى هنا أو اختر ملفات', built.lead);
ok('and in the singular for a single one', built.one === 'اسحب الملف إلى هنا أو اختر ملفًا', built.one);
ok('the small print is made from the limits', /1/.test(built.note) && /3 ملفات على الأكثر/.test(built.note), built.note);
ok('or is the page\'s own', built.own === 'صورة الهوية');
ok('no list until there are files', built.listHidden);

// ------------------------------------------------------------------ picking
await E(`__pick('f-docs', [['عقد.pdf', 500, 'application/pdf'], ['صورة.png', 800, 'image/png']])`);
let v = await view('f-docs');
ok('picked files are listed, in order', JSON.stringify(v.rows) === JSON.stringify(['عقد.pdf', 'صورة.png']), JSON.stringify(v.rows));
ok('and are the input\'s own files', JSON.stringify(v.files) === JSON.stringify(['عقد.pdf', 'صورة.png']));
ok('an image gets a thumbnail, anything else its type', JSON.stringify(v.thumbs) === JSON.stringify(['PDF', 'img']), JSON.stringify(v.thumbs));
ok('sizes in the page\'s language, Latin digits', /^500\s/.test(v.sizes[0]) && /[؀-ۿ]/.test(v.sizes[0]), JSON.stringify(v.sizes));
ok('a pick does not fire a second change of its own', (await E(`__changes['f-docs']`)) === 1, await E(`__changes['f-docs']`));
ok('ins:file for each file added', JSON.stringify(await E(`__added`)) === JSON.stringify(['f-docs:عقد.pdf:false', 'f-docs:صورة.png:false']), JSON.stringify(await E(`__added`)));
await E(`__pick('f-docs', [['ملحق.pdf', 300, 'application/pdf']])`);
ok('with multiple, a new pick adds to the list', JSON.stringify((await view('f-docs')).files) === JSON.stringify(['عقد.pdf', 'صورة.png', 'ملحق.pdf']));
await E(`__pick('f-docs', [['رابع.pdf', 100, 'application/pdf']])`);
v = await view('f-docs');
ok('past the count: not added, and the field says why', v.files.length === 3 && v.issues.length === 1 && /3/.test(v.issues[0]) && /رابع\.pdf/.test(v.issues[0]), JSON.stringify(v.issues));
ok('the reason is announced', (await E(`document.querySelector('#f-docs ~ .ins-file-issues').getAttribute('role')`)) === 'alert');
await E(`__pick('f-docs', [['عقد.pdf', 500, 'application/pdf']])`);
ok('the same file again is not listed twice', (await view('f-docs')).files.length === 3);

// ------------------------------------------------------------------ removing
await b.click('#f-docs ~ .ins-file-list .ins-file-item:nth-child(2) .ins-file-remove');
await b.sleep(80);
v = await view('f-docs');
ok('the remove button takes it off the list and out of the input', JSON.stringify(v.files) === JSON.stringify(['عقد.pdf', 'ملحق.pdf']) && v.rows.length === 2, JSON.stringify(v));
ok('and the old message goes', v.issues.length === 0);
ok('focus goes back to the input, and the page hears a change', (await E(`document.activeElement.id`)) === 'f-docs' && (await E(`__changes['f-docs']`)) >= 2);
ok('the remove button is named for its file', (await E(`document.querySelector('#f-docs ~ .ins-file-list .ins-file-remove').getAttribute('aria-label')`)) === 'إزالة عقد.pdf');

// ------------------------------------------------------------------ dropping
await E(`__drop('f-docs', [['برنامج.exe', 100, 'application/x-msdownload'], ['كبير.pdf', 2048, 'application/pdf'], ['مسح.jpg', 600, 'image/jpeg']])`);
v = await view('f-docs');
ok('a drop marks the zone while held over it', await E(`__over`));
ok('a dropped file the accept list refuses is left out, named', v.issues.some((m) => /برنامج\.exe/.test(m) && /غير مقبول/.test(m)), JSON.stringify(v.issues));
ok('so is one over the size limit', v.issues.some((m) => /كبير\.pdf/.test(m) && /أكبر من/.test(m)), JSON.stringify(v.issues));
ok('and the good one is added', JSON.stringify(v.files) === JSON.stringify(['عقد.pdf', 'ملحق.pdf', 'مسح.jpg']), JSON.stringify(v.files));
ok('what the form sends', JSON.stringify(await E(`new FormData(document.getElementById('ff')).getAll('docs').map((f) => f.name)`)) === JSON.stringify(['عقد.pdf', 'ملحق.pdf', 'مسح.jpg']));

// ------------------------------------------------------------------ one file
await E(`__pick('f-one', [['أ.txt', 10]])`);
await E(`__pick('f-one', [['ب.txt', 20]])`);
ok('without multiple, a new pick replaces', JSON.stringify((await view('f-one')).files) === JSON.stringify(['ب.txt']));
await E(`document.getElementById('f-one').focus(); document.dispatchEvent(new ClipboardEvent('paste', { clipboardData: __dt([__mk('لقطة.png', 50, 'image/png')]), bubbles: true, cancelable: true })); 0`);
ok('a pasted file goes into the focused field', JSON.stringify((await view('f-one')).files) === JSON.stringify(['لقطة.png']));

// ------------------------------------------------------------------ the upload hook
await E(`window.__up = []; document.addEventListener('ins:file', (e) => { if (e.detail.upload) __up.push(e.detail.file); }); 0`);
ok('with data-ins-file-upload the input sends no files of its own', !(await E(`document.getElementById('f-up').hasAttribute('name')`)));
await E(`__pick('f-up', [['one.pdf', 100, 'application/pdf'], ['two.pdf', 100, 'application/pdf']])`);
const rowState = () => E(`[...document.querySelectorAll('#f-up ~ .ins-file-list .ins-file-item')].map((r) => (r.classList.contains('is-sending') ? 'sending' : r.classList.contains('is-done') ? 'done' : r.classList.contains('is-failed') ? 'failed' : 'ready')
  + (r.querySelector('progress') ? ':' + (r.querySelector('progress').hasAttribute('value') ? r.querySelector('progress').value : 'wait') : '') + (r.querySelector('.ins-file-status') ? ':' + r.querySelector('.ins-file-status').textContent : '')).join(' | ')`);
ok('each file is handed to the page, marked as on its way', (await E(`__up.length`)) === 2 && (await rowState()) === 'sending:wait | sending:wait', await rowState());
v = await view('f-up');
ok('and the form waits for them', !v.valid && v.msg === 'انتظر حتّى يكتمل رفع الملفات.', v.msg);
await E(`Insiyab.file.progress(__up[0], 0.5); 0`);
ok('progress() moves its bar', (await rowState()).startsWith('sending:0.5'), await rowState());
await E(`Insiyab.file.done(__up[0], 'id-1'); Insiyab.file.fail(__up[1], 'الخادم مشغول'); 0`);
ok('done() clears the bar, fail() shows the message', (await rowState()) === 'done | failed:الخادم مشغول', await rowState());
v = await view('f-up');
ok('a failed file keeps the form from going, and says what to do', !v.valid && /أزِل/.test(v.msg), v.msg);
await b.click('#f-up ~ .ins-file-list .ins-file-item:nth-child(2) .ins-file-remove');
await b.sleep(80);
ok('removing it lets the form go', (await view('f-up')).valid);
ok('the form sends the ids the page gave, under the field\'s name, and no files', JSON.stringify(await E(`new FormData(document.getElementById('fu')).getAll('ids')`)) === JSON.stringify(['id-1']));
await b.click('#f-up ~ .ins-file-list .ins-file-remove');
await b.sleep(80);
ok('removing a finished upload takes its id away too', (await E(`new FormData(document.getElementById('fu')).getAll('ids').length`)) === 0);

// ------------------------------------------------------------------ required, keyboard
await b.click('#fr-send');
await b.sleep(250);
ok('an empty required field is not sent, and its zone turns red', !(await E(`window.__sentR`)) &&
  (await E(`getComputedStyle(document.querySelector('#f-req ~ .ins-file-zone')).borderTopColor`)) !== (await E(`getComputedStyle(document.querySelector('#f-docs ~ .ins-file-zone')).borderTopColor`)));
await E(`__pick('f-req', [['هوية.jpg', 100, 'image/jpeg']]); 0`);
await b.click('#fr-send');
await b.sleep(150);
ok('with a file, it goes', await E(`window.__sentR === true`));
await E(`document.getElementById('before').focus(); 0`);
await b.key('Tab');
await b.sleep(250);
ok('Tab reaches the input, and the zone shows the focus', (await E(`document.activeElement.id`)) === 'f-docs' &&
  (await E(`getComputedStyle(document.querySelector('#f-docs ~ .ins-file-zone')).boxShadow`)) !== 'none');

// ------------------------------------------------------------------ reset, API, the rest
await E(`document.getElementById('ff').reset(); 0`);
await b.sleep(80);
v = await view('f-docs');
ok('a form reset empties the list and the input', v.files.length === 0 && v.listHidden, JSON.stringify(v));
ok('Insiyab.file(field, files) sets the list, within the limits', JSON.stringify(await E(`Insiyab.file('#f-docs', __files([['a.pdf', 10, 'application/pdf'], ['b.exe', 10]])).map((f) => f.name)`)) === JSON.stringify(['a.pdf']));
ok('Insiyab.file(field) reads it', JSON.stringify(await E(`Insiyab.file('#f-docs').map((f) => f.name)`)) === JSON.stringify(['a.pdf']));
await E(`Insiyab.file('#f-docs', []); 0`);
ok("Insiyab.file(field, []) empties it", (await view('f-docs')).files.length === 0);
await E(`__drop('f-en', [['big.bin', 3 * 1048576]])`);
ok('an English field explains in English', /“big\.bin” is larger than 2 MB/.test((await view('f-en')).issues[0] || ''), JSON.stringify((await view('f-en')).issues));
ok('with an English zone', (await E(`document.querySelector('#f-en ~ .ins-file-zone .ins-file-lead').textContent`)) === 'Drag files here or choose files');
await E(`__drop('f-off', [['x.txt', 5]])`);
ok('a disabled field takes no drop', (await view('f-off')).files.length === 0);
/* A grid item, so `inline-flex` computes as `flex`; what matters is that it is a
   row sized to its content, not a zone across the field. */
const compact = await E(`(() => { const z = document.querySelector('#f-compact ~ .ins-file-zone');
  return { row: getComputedStyle(z).display === 'flex', narrow: z.offsetWidth < z.parentNode.offsetWidth / 2, drag: getComputedStyle(z.querySelector('.ins-file-drag')).display }; })()`);
ok('the compact look: a button, no drag line', compact.row && compact.narrow && compact.drag === 'none', JSON.stringify(compact));

ok('no console errors, warnings or exceptions', b.logs.length === 0, b.logs.join(' | '));
await b.close();
end();
