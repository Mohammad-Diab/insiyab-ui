/* One reporter for every test file: a PASS or FAIL line per check, a count at the
   end, and a non-zero exit code when anything failed — which is what run.mjs reads. */
export function suite() {
  let passed = 0, failed = 0;
  const ok = (label, cond, detail = '') => {
    if (cond) passed++; else failed++;
    const extra = detail === '' || detail == null ? '' : '  ' + String(detail);
    console.log(`${cond ? 'PASS' : 'FAIL'}  ${label}${extra}`);
  };
  const end = () => {
    console.log(`\n${passed}/${passed + failed} passed`);
    process.exitCode = failed ? 1 : 0;
  };
  return { ok, end };
}
