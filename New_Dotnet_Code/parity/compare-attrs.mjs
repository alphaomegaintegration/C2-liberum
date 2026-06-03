// Attribute-level parity diff: catches what compare.mjs (visible-text/DOM) cannot see because it strips
// tags — attribute-borne content that should be byte-identical across both apps and is NOT route-dependent:
//   * mailto: hrefs            (e.g. the AssignedTo rep link)
//   * submit/button/reset/image input value="" labels  (button captions live in attributes, not text)
//   * <img> src basenames      (which pin/icon — filename only, since path depth is an accepted divergence)
//   * non-empty text/password input value="" defaults  (prefilled data echoes, e.g. register-edit)
//   * non-empty <option value=""> ... but option *text* is already covered by compare.mjs
//
// Route hrefs (foo.asp vs /Area/Foo) are an ACCEPTED divergence and are intentionally NOT compared here.
//
// Usage:  node parity/compare-attrs.mjs      (both servers up: ASP :8080, .NET :5123)

import http from 'node:http';

const ASP = 'http://localhost:8080';
const NET = 'http://127.0.0.1:5123';

function req(method, urlStr, { jar = {}, body = null } = {}) {
  return new Promise((resolve, reject) => {
    const u = new URL(urlStr);
    const headers = { 'User-Agent': 'parity-attrs' };
    if (body != null) {
      headers['Content-Type'] = 'application/x-www-form-urlencoded';
      headers['Content-Length'] = Buffer.byteLength(body);
    }
    const cookie = Object.entries(jar).map(([k, v]) => `${k}=${v}`).join('; ');
    if (cookie) headers['Cookie'] = cookie;
    const r = http.request({ method, hostname: u.hostname, port: u.port, path: u.pathname + u.search, headers },
      (res) => {
        let data = '';
        res.on('data', (c) => (data += c));
        res.on('end', () => {
          for (const sc of res.headers['set-cookie'] || []) {
            const m = /^([^=]+)=([^;]*)/.exec(sc);
            if (m) jar[m[1]] = m[2];
          }
          resolve({ status: res.statusCode, headers: res.headers, body: data });
        });
      });
    r.on('error', reject);
    if (body != null) r.write(body);
    r.end();
  });
}

// Decode the entities that ASP (raw bytes) and .NET (HTML-encoded) render identically, THEN collapse any
// genuinely non-ASCII glyph to '?' so the CP1252-vs-UTF8 byte difference doesn't false-positive. Order matters:
// &#39;/&apos; (the apostrophe) and &#x27; decode to a real "'" (char 39) — both apps render the same glyph.
const neutralize = (s) => s
  .replace(/&#0*39;|&apos;|&#x0*27;/gi, "'").replace(/&quot;|&#0*34;|&#x0*22;/gi, '"')
  .replace(/&amp;/gi, '&').replace(/&lt;/gi, '<').replace(/&gt;/gi, '>').replace(/&nbsp;?/gi, ' ')
  .replace(/&#x[0-9a-fA-F]+;?/g, '?').replace(/&#[0-9]+;?/g, '?').replace(/[^\x00-\x7F]/g, '?')
  .replace(/\s+/g, ' ').trim();
const stripNoise = (h) => h.replace(/<!--[\s\S]*?-->/g, ' ').replace(/<script[\s\S]*?<\/script>/gi, ' ').replace(/<style[\s\S]*?<\/style>/gi, ' ');
// Match both quoted (name="v") and unquoted (name=v) attributes — the original markup uses bare `type=button`.
const attr = (tag, name) => {
  const m = new RegExp(`\\b${name}\\s*=\\s*(?:"([^"]*)"|'([^']*)'|([^\\s">]+))`, 'i').exec(tag);
  return m ? (m[1] ?? m[2] ?? m[3]) : undefined;
};

function mailtos(html) {
  return [...stripNoise(html).matchAll(/<a\b[^>]*\bhref\s*=\s*"(mailto:[^"]*)"[^>]*>([\s\S]*?)<\/a>/gi)]
    .map((m) => `${neutralize(m[1])}  ::  ${neutralize(m[2].replace(/<[^>]*>/g, ''))}`).sort();
}
function buttons(html) {
  const out = [];
  for (const m of stripNoise(html).matchAll(/<input\b[^>]*>/gi)) {
    const t = (attr(m[0], 'type') || 'text').toLowerCase();
    if (['submit', 'button', 'reset', 'image'].includes(t)) out.push(`${t}:${neutralize(attr(m[0], 'value') || '')}`);
  }
  // <button>text</button>
  for (const m of stripNoise(html).matchAll(/<button\b[^>]*>([\s\S]*?)<\/button>/gi)) out.push(`button:${neutralize(m[1].replace(/<[^>]*>/g, ''))}`);
  return out.sort();
}
function imgBasenames(html) {
  return [...stripNoise(html).matchAll(/<img\b[^>]*\bsrc\s*=\s*"([^"]*)"/gi)]
    .map((m) => (m[1].split(/[\\/]/).pop() || '').toLowerCase()).sort();
}
function textInputValues(html) {
  const out = [];
  for (const m of stripNoise(html).matchAll(/<input\b[^>]*>/gi)) {
    const t = (attr(m[0], 'type') || 'text').toLowerCase();
    if (!['text', 'password', 'hidden'].includes(t)) continue;
    const v = neutralize(attr(m[0], 'value') || '');
    if (v) out.push(`${attr(m[0], 'name') || '?'}=${v}`);
  }
  return out.sort();
}

const F = 'id=1', K = 'id=2';
const PAGES = [
  ['logon', '/logon.asp', '/Logon', 'pub', 'GET'],
  ['logoff', '/logoff.asp', '/Logoff', 'pub', 'GET'],
  ['register', '/register.asp', '/Register', 'pub', 'GET'],
  ['forgotpass', '/forgotpass.asp', '/ForgotPass', 'pub', 'GET'],
  ['register-edit', '/register.asp?edit=1', '/Register?edit=1', 'auth', 'GET'],
  ['user-default', '/user/default.asp', '/User', 'auth', 'GET'],
  ['user-new', '/user/new.asp', '/User/Problem/New', 'auth', 'GET'],
  ['user-details', '/user/details.asp?' + F, '/User/Problem/Details?' + F, 'auth', 'GET'],
  ['user-view', '/user/view.asp', '/User/Problem/View', 'auth', 'GET'],
  ['user-print', '/user/print.asp?' + F, '/User/Problem/Print?' + F, 'auth', 'GET'],
  ['rep-default', '/rep/default.asp', '/Rep', 'auth', 'GET'],
  ['rep-new', '/rep/new.asp', '/Rep/Problem/New', 'auth', 'GET'],
  ['rep-details', '/rep/details.asp?' + F, '/Rep/Problem/Details?' + F, 'auth', 'GET'],
  ['rep-view', '/rep/view.asp', '/Rep/Problem/View', 'auth', 'GET'],
  ['rep-print', '/rep/print.asp?' + F, '/Rep/Problem/Print?' + F, 'auth', 'GET'],
  ['rep-search', '/rep/search.asp', '/Rep/Search', 'auth', 'GET'],
  ['rep-selectuser', '/rep/selectuser.asp', '/Rep/SelectUser', 'auth', 'GET'],
  ['rep-results', '/rep/results.asp', '/Rep/Results', 'auth', 'POST', 'keywords=password&title=on&description=on&solution=on&order=1&uid=&id=&rep=0&category=0&department=0&status=0&priority=0&s_month=1&s_day=1&s_year=2026&e_month=12&e_day=31&e_year=2026'],
  ['kb', '/kb/default.asp', '/Kb', 'auth', 'GET'],
  ['kb-details', '/kb/details.asp?' + K, '/Kb/Details?' + K, 'auth', 'GET'],
  ['kb-print', '/kb/print.asp?' + K, '/Kb/Print?' + K, 'auth', 'GET'],
  ['inout', '/inout/default.asp', '/Inout', 'auth', 'GET'],
  ['inout-details', '/inout/details.asp?' + F, '/Inout/Details?' + F, 'auth', 'GET'],
  ['inout-status', '/inout/status.asp?' + F, '/Inout/Status?' + F, 'auth', 'GET'],
  ['inout-update', '/inout/update.asp?' + F, '/Inout/Update?' + F, 'auth', 'GET'],
  ['admin-menu', '/admin/default.asp', '/Admin', 'auth', 'GET'],
  ['config', '/admin/config.asp', '/Admin/Config', 'auth', 'GET'],
  ['config-help', '/admin/config_help.asp', '/Admin/ConfigHelp', 'auth', 'GET'],
  ['cfgemail', '/admin/cfgemail.asp', '/Admin/CfgEmail', 'auth', 'GET'],
  ['cfgemail-help', '/admin/cfgemail_help.asp', '/Admin/CfgEmailHelp', 'auth', 'GET'],
  ['adminpass', '/admin/adminpass.asp', '/Admin/AdminPass', 'auth', 'GET'],
  ['viewusers', '/admin/viewusers.asp', '/Admin/ViewUsers', 'auth', 'GET'],
  ['adduser', '/admin/adduser.asp', '/Admin/AddUser', 'auth', 'GET'],
  ['moduser', '/admin/moduser.asp', '/Admin/ModUser', 'auth', 'POST', 'usersid=1'],
  ['viewcat', '/admin/viewcat.asp', '/Admin/ViewCat', 'auth', 'GET'],
  ['modify-cat', '/admin/modify.asp?mtype=2&id=1', '/Admin/Modify?mtype=2&id=1', 'auth', 'GET'],
  ['viewdep', '/admin/viewdep.asp', '/Admin/ViewDep', 'auth', 'GET'],
  ['modify-dep', '/admin/modify.asp?mtype=3&id=1', '/Admin/Modify?mtype=3&id=1', 'auth', 'GET'],
  ['viewpri', '/admin/viewpri.asp', '/Admin/ViewPri', 'auth', 'GET'],
  ['modify-pri', '/admin/modify.asp?mtype=4&id=1', '/Admin/Modify?mtype=4&id=1', 'auth', 'GET'],
  ['viewstatus', '/admin/viewstatus.asp', '/Admin/ViewStatus', 'auth', 'GET'],
  ['modify-status', '/admin/modify.asp?mtype=5&id=1', '/Admin/Modify?mtype=5&id=1', 'auth', 'GET'],
  ['confdelete-cat', '/admin/confdelete.asp?mtype=2&id=1', '/Admin/ConfDelete?mtype=2&id=1', 'auth', 'GET'],
  ['viewlang', '/admin/viewlang.asp', '/Admin/ViewLang', 'auth', 'GET'],
  ['viewlangstring', '/admin/viewlangstring.asp?lang_id=1', '/Admin/ViewLangString?lang_id=1', 'auth', 'GET'],
  ['reports', '/admin/reports.asp', '/Admin/Reports', 'auth', 'GET'],
  ['viewreports', '/admin/viewreports.asp', '/Admin/ViewReports', 'auth', 'POST', 'type=0&s_month=1&s_day=1&s_year=2026&e_month=12&e_day=31&e_year=2026'],
  ['sysinfo', '/admin/sysinfo.asp', '/Admin/SysInfo', 'auth', 'GET'],
  ['test', '/admin/test.asp', '/Admin/Test', 'auth', 'GET'],
];

async function login() {
  const aspAuth = {}, netAuth = {};
  await req('POST', `${ASP}/logon.asp`, { jar: aspAuth, body: 'logon=1&uid=admin&password=admin&URL=default.asp' });
  await req('POST', `${NET}/Logon?URL=default.asp`, { jar: netAuth, body: 'logon=1&uid=admin&password=admin' });
  await req('POST', `${ASP}/admin/default.asp`, { jar: aspAuth, body: 'password=admin' });
  await req('POST', `${NET}/Admin`, { jar: netAuth, body: 'password=admin' });
  return { aspAuth, netAuth };
}

const diff = (a, b) => {
  const A = a.reduce((m, x) => m.set(x, (m.get(x) || 0) + 1), new Map());
  const B = b.reduce((m, x) => m.set(x, (m.get(x) || 0) + 1), new Map());
  const onlyA = [], onlyB = [];
  for (const [x, n] of A) for (let i = 0; i < n - (B.get(x) || 0); i++) onlyA.push(x);
  for (const [x, n] of B) for (let i = 0; i < n - (A.get(x) || 0); i++) onlyB.push(x);
  return { onlyA, onlyB };
};

async function run() {
  const { aspAuth, netAuth } = await login();
  let clean = 0, dirty = 0;
  for (const [name, aspPath, netPath, auth, method, body] of PAGES) {
    const aJar = auth === 'auth' ? aspAuth : {}, nJar = auth === 'auth' ? netAuth : {};
    const a = await req(method === 'POST' ? 'POST' : 'GET', ASP + aspPath, { jar: aJar, body: body ?? null });
    const n = await req(method === 'POST' ? 'POST' : 'GET', NET + netPath, { jar: nJar, body: body ?? null });
    const checks = {
      mailto: diff(mailtos(a.body), mailtos(n.body)),
      button: diff(buttons(a.body), buttons(n.body)),
      img: diff(imgBasenames(a.body), imgBasenames(n.body)),
      inputVal: diff(textInputValues(a.body), textInputValues(n.body)),
    };
    const problems = Object.entries(checks).filter(([, d]) => d.onlyA.length || d.onlyB.length);
    if (problems.length === 0) { clean++; continue; }
    dirty++;
    console.log(`\n[ATTR-DIFF] ${name}  (ASP=${a.status} NET=${n.status})`);
    for (const [k, d] of problems) {
      if (d.onlyA.length) console.log(`   ${k} only in ASP: ${d.onlyA.join('  |  ')}`);
      if (d.onlyB.length) console.log(`   ${k} only in NET: ${d.onlyB.join('  |  ')}`);
    }
  }
  console.log(`\n==== ATTR SUMMARY: ${clean} clean, ${dirty} with attribute diffs, of ${PAGES.length} pages ====`);
}
run().catch((e) => { console.error(e); process.exit(1); });
