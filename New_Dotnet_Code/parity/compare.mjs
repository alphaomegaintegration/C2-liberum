// Comprehensive parity harness: Classic ASP oracle vs .NET 10 port, page-by-page.
// Fetches each page from both apps, normalizes the DOM (visible text, form fields, SELECT options,
// headings, title), and diffs. Covers public, authenticated (rep), admin, and needs-id pages
// (problem/category/inout fixtures created identically in both DBs by tools/fixtures.*).
//
// Usage:  node parity/compare.mjs
// Both servers must be running:  ASP :8080 (IIS + Access .mdb)   .NET :5123 (Kestrel + SQLite)
// Identical fixtures: admin rep (sid=1), category 1, problem 1 (OPEN), problem 2 (CLOSED kb), one note.
//
// No external dependencies (Node built-ins only).

import http from 'node:http';

const ASP = 'http://localhost:8080';
const NET = 'http://127.0.0.1:5123';

// ---- tiny HTTP client with a cookie jar (no redirect following) -------------
function req(method, urlStr, { jar = {}, body = null } = {}) {
  return new Promise((resolve, reject) => {
    const u = new URL(urlStr);
    const headers = { 'User-Agent': 'parity-harness' };
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

// ---- DOM normalization ------------------------------------------------------
function decode(s) {
  return s
    .replace(/&nbsp;?/gi, ' ').replace(/&amp;/gi, '&').replace(/&lt;/gi, '<')
    .replace(/&gt;/gi, '>').replace(/&quot;/gi, '"').replace(/&copy;/gi, '(c)')
    .replace(/&#39;|&apos;/gi, "'").replace(/&eacute;/gi, 'e');
}
function stripNoise(html) {
  return html
    .replace(/<!--[\s\S]*?-->/g, ' ')
    .replace(/<script[\s\S]*?<\/script>/gi, ' ')
    .replace(/<style[\s\S]*?<\/style>/gi, ' ');
}
// ASP serves CP1252 bytes; .NET serves UTF-8/numeric refs. Both render the same accented glyph in a
// browser (accepted charset divergence) — collapse non-ASCII to '?' to avoid false positives.
function neutralizeAccents(s) {
  return s.replace(/&#x[0-9a-fA-F]+;?/g, '?').replace(/&#[0-9]+;?/g, '?').replace(/[^\x00-\x7F]/g, '?');
}
function visibleText(html) {
  return neutralizeAccents(decode(stripNoise(html).replace(/<[^>]*>/g, ' '))).replace(/\s+/g, ' ').trim();
}
function title(html) {
  const m = /<title>([\s\S]*?)<\/title>/i.exec(html);
  return m ? neutralizeAccents(decode(m[1])).replace(/\s+/g, ' ').trim() : '';
}
function formFields(html) {
  const out = [];
  const clean = stripNoise(html);
  let m;
  const inputRe = /<input\b[^>]*>/gi;
  while ((m = inputRe.exec(clean))) {
    const tag = m[0];
    const name = /\bname\s*=\s*"([^"]*)"/i.exec(tag)?.[1];
    const type = (/\btype\s*=\s*"([^"]*)"/i.exec(tag)?.[1] || 'text').toLowerCase();
    if (name) out.push(`${type}:${name}`);
  }
  for (const re of [/<select\b[^>]*\bname\s*=\s*"([^"]*)"/gi, /<textarea\b[^>]*\bname\s*=\s*"([^"]*)"/gi]) {
    while ((m = re.exec(clean))) out.push(`${re.source.includes('select') ? 'select' : 'textarea'}:${m[1]}`);
  }
  return out.sort();
}
// Extract each <select>'s option texts as "name=[opt | opt | ...]" (catches dropdown-label divergences).
function selectOptions(html) {
  const out = [];
  const clean = stripNoise(html);
  const selRe = /<select\b[^>]*\bname\s*=\s*"([^"]*)"[^>]*>([\s\S]*?)<\/select>/gi;
  let m;
  while ((m = selRe.exec(clean))) {
    const name = m[1];
    const opts = [];
    const optRe = /<option\b[^>]*>([\s\S]*?)<\/option>/gi;
    let o;
    while ((o = optRe.exec(m[2]))) opts.push(neutralizeAccents(decode(o[1].replace(/<[^>]*>/g, ''))).replace(/\s+/g, ' ').trim());
    out.push(`${name}=[${opts.join(' | ')}]`);
  }
  return out.sort();
}
function headings(html) {
  const out = [];
  const re = /<h[1-4][^>]*>([\s\S]*?)<\/h[1-4]>/gi;
  let m;
  while ((m = re.exec(stripNoise(html)))) {
    const t = neutralizeAccents(decode(m[1].replace(/<[^>]*>/g, ' '))).replace(/\s+/g, ' ').trim();
    if (t) out.push(t);
  }
  return out;
}

// ---- diff helpers -----------------------------------------------------------
const tokens = (s) => s.split(/\s+/).filter(Boolean);
function setDiff(a, b) {
  const A = new Set(a), B = new Set(b);
  return { onlyA: [...A].filter((x) => !B.has(x)), onlyB: [...B].filter((x) => !A.has(x)) };
}
function textDiff(a, b) {
  const count = (arr) => arr.reduce((m, t) => m.set(t, (m.get(t) || 0) + 1), new Map());
  const ca = count(tokens(a)), cb = count(tokens(b));
  const onlyA = [], onlyB = [];
  for (const [t, n] of ca) { const d = n - (cb.get(t) || 0); for (let i = 0; i < d; i++) onlyA.push(t); }
  for (const [t, n] of cb) { const d = n - (ca.get(t) || 0); for (let i = 0; i < d; i++) onlyB.push(t); }
  return { onlyA, onlyB };
}

// ---- page map ---------------------------------------------------------------
// [name, aspPath, netPath, auth('pub'|'auth'), method('GET'|'POST'|'REDIR'), body]
const F = 'id=1', K = 'id=2';
const PAGES = [
  // ---- public ----
  ['logon',         '/logon.asp',                  '/Logon',                    'pub',  'GET'],
  ['logoff',        '/logoff.asp',                 '/Logoff',                   'pub',  'GET'],
  ['register',      '/register.asp',               '/Register',                 'pub',  'GET'],
  ['forgotpass',    '/forgotpass.asp',             '/ForgotPass',               'pub',  'GET'],
  ['default-landing','/default.asp',               '/',                         'auth', 'REDIR'],
  ['register-edit', '/register.asp?edit=1',        '/Register?edit=1',          'auth', 'GET'],
  // ---- user area ----
  ['user-default',  '/user/default.asp',           '/User',                     'auth', 'GET'],
  ['user-new',      '/user/new.asp',               '/User/Problem/New',         'auth', 'GET'],
  ['user-details',  '/user/details.asp?'+F,        '/User/Problem/Details?'+F,  'auth', 'GET'],
  ['user-view',     '/user/view.asp',              '/User/Problem/View',        'auth', 'GET'],
  ['user-print',    '/user/print.asp?'+F,          '/User/Problem/Print?'+F,    'auth', 'GET'],
  // ---- rep area ----
  ['rep-default',   '/rep/default.asp',            '/Rep',                      'auth', 'GET'],
  ['rep-new',       '/rep/new.asp',                '/Rep/Problem/New',          'auth', 'GET'],
  ['rep-details',   '/rep/details.asp?'+F,         '/Rep/Problem/Details?'+F,   'auth', 'GET'],
  ['rep-view',      '/rep/view.asp',               '/Rep/Problem/View',         'auth', 'GET'],
  ['rep-print',     '/rep/print.asp?'+F,           '/Rep/Problem/Print?'+F,     'auth', 'GET'],
  ['rep-search',    '/rep/search.asp',             '/Rep/Search',               'auth', 'GET'],
  ['rep-selectuser','/rep/selectuser.asp',         '/Rep/SelectUser',           'auth', 'GET'],
  ['rep-results',   '/rep/results.asp',            '/Rep/Results',              'auth', 'POST', 'keywords=password&title=on&description=on&solution=on&order=1&uid=&id=&rep=0&category=0&department=0&status=0&priority=0&s_month=1&s_day=1&s_year=2026&e_month=12&e_day=31&e_year=2026'],
  // ---- kb ----
  ['kb',            '/kb/default.asp',             '/Kb',                       'auth', 'GET'],
  ['kb-details',    '/kb/details.asp?'+K,          '/Kb/Details?'+K,            'auth', 'GET'],
  ['kb-print',      '/kb/print.asp?'+K,            '/Kb/Print?'+K,              'auth', 'GET'],
  // ---- inout ----
  ['inout',         '/inout/default.asp',          '/Inout',                    'auth', 'GET'],
  ['inout-details', '/inout/details.asp?'+F,       '/Inout/Details?'+F,         'auth', 'GET'],
  ['inout-status',  '/inout/status.asp?'+F,        '/Inout/Status?'+F,          'auth', 'GET'],
  ['inout-update',  '/inout/update.asp?'+F,        '/Inout/Update?'+F,          'auth', 'GET'],
  // ---- admin ----
  ['admin-menu',    '/admin/default.asp',          '/Admin',                    'auth', 'GET'],
  ['config',        '/admin/config.asp',           '/Admin/Config',             'auth', 'GET'],
  ['config-help',   '/admin/config_help.asp',      '/Admin/ConfigHelp',         'auth', 'GET'],
  ['cfgemail',      '/admin/cfgemail.asp',         '/Admin/CfgEmail',           'auth', 'GET'],
  ['cfgemail-help', '/admin/cfgemail_help.asp',    '/Admin/CfgEmailHelp',       'auth', 'GET'],
  ['adminpass',     '/admin/adminpass.asp',        '/Admin/AdminPass',          'auth', 'GET'],
  ['viewusers',     '/admin/viewusers.asp',        '/Admin/ViewUsers',          'auth', 'GET'],
  ['adduser',       '/admin/adduser.asp',          '/Admin/AddUser',            'auth', 'GET'],
  ['moduser',       '/admin/moduser.asp',          '/Admin/ModUser',            'auth', 'POST', 'usersid=1'],
  ['viewcat',       '/admin/viewcat.asp',          '/Admin/ViewCat',            'auth', 'GET'],
  ['modify-cat',    '/admin/modify.asp?mtype=2&id=1','/Admin/Modify?mtype=2&id=1','auth','GET'],
  ['viewdep',       '/admin/viewdep.asp',          '/Admin/ViewDep',            'auth', 'GET'],
  ['modify-dep',    '/admin/modify.asp?mtype=3&id=1','/Admin/Modify?mtype=3&id=1','auth','GET'],
  ['viewpri',       '/admin/viewpri.asp',          '/Admin/ViewPri',            'auth', 'GET'],
  ['modify-pri',    '/admin/modify.asp?mtype=4&id=1','/Admin/Modify?mtype=4&id=1','auth','GET'],
  ['viewstatus',    '/admin/viewstatus.asp',       '/Admin/ViewStatus',         'auth', 'GET'],
  ['modify-status', '/admin/modify.asp?mtype=5&id=1','/Admin/Modify?mtype=5&id=1','auth','GET'],
  ['confdelete-cat','/admin/confdelete.asp?mtype=2&id=1','/Admin/ConfDelete?mtype=2&id=1','auth','GET'],
  ['viewlang',      '/admin/viewlang.asp',         '/Admin/ViewLang',           'auth', 'GET'],
  ['viewlangstring','/admin/viewlangstring.asp?lang_id=1','/Admin/ViewLangString?lang_id=1','auth','GET'],
  ['reports',       '/admin/reports.asp',          '/Admin/Reports',            'auth', 'GET'],
  ['viewreports',   '/admin/viewreports.asp',      '/Admin/ViewReports',        'auth', 'POST', 'type=0&s_month=1&s_day=1&s_year=2026&e_month=12&e_day=31&e_year=2026'],
  ['sysinfo',       '/admin/sysinfo.asp',          '/Admin/SysInfo',            'auth', 'GET'],
  ['test',          '/admin/test.asp',             '/Admin/Test',               'auth', 'GET'],
];

async function login() {
  const aspAuth = {}, netAuth = {};
  // rep login (DB auth, plaintext)
  await req('POST', `${ASP}/logon.asp`,            { jar: aspAuth, body: 'logon=1&uid=admin&password=admin&URL=default.asp' });
  await req('POST', `${NET}/Logon?URL=default.asp`,{ jar: netAuth, body: 'logon=1&uid=admin&password=admin' });
  // admin gate (same jar, so admin pages render with the rep footer too)
  await req('POST', `${ASP}/admin/default.asp`,    { jar: aspAuth, body: 'password=admin' });
  await req('POST', `${NET}/Admin`,                { jar: netAuth, body: 'password=admin' });
  return { aspAuth, netAuth };
}

async function run() {
  const { aspAuth, netAuth } = await login();
  const rows = [];
  for (const [name, aspPath, netPath, auth, method, body] of PAGES) {
    const aJar = auth === 'auth' ? aspAuth : {};
    const nJar = auth === 'auth' ? netAuth : {};
    const verb = method === 'REDIR' ? 'GET' : method;
    const a = await req(verb, ASP + aspPath, { jar: aJar, body: body ?? null });
    const n = await req(verb, NET + netPath, { jar: nJar, body: body ?? null });

    if (method === 'REDIR') {
      const loc = (h) => (h.location || '').replace(/^https?:\/\/[^/]+/i, '').toLowerCase().replace(/\/$/, '');
      const al = loc(a.headers), nl = loc(n.headers);
      const ok = a.status >= 300 && a.status < 400 && n.status >= 300 && n.status < 400 &&
        al.replace(/[^a-z]/g, '').endsWith('rep') && nl.replace(/[^a-z]/g, '').endsWith('rep');
      rows.push({ name, aspStatus: a.status, netStatus: n.status, verdict: ok ? 'MATCH' : 'DIFF',
        titleAsp: '(redirect ' + al + ')', titleNet: '(redirect ' + nl + ')',
        fieldsMatch: ok, headMatch: ok, optMatch: ok, textOnlyA: [], textOnlyB: [], fieldDiff: { onlyA: [], onlyB: [] }, headDiff: { onlyA: [], onlyB: [] }, optDiff: { onlyA: [], onlyB: [] } });
      continue;
    }

    const td = textDiff(visibleText(a.body), visibleText(n.body));
    const fd = setDiff(formFields(a.body), formFields(n.body));
    const hd = setDiff(headings(a.body), headings(n.body));
    const od = setDiff(selectOptions(a.body), selectOptions(n.body));

    const noise = (t) => /^\$|License|license\.?$|^\(c\)|2014|Luxem\.?$/.test(t);
    const textOnlyA = td.onlyA.filter((t) => !noise(t));
    const textOnlyB = td.onlyB.filter((t) => !noise(t));

    const fieldsMatch = fd.onlyA.length === 0 && fd.onlyB.length === 0;
    const headMatch = hd.onlyA.length === 0 && hd.onlyB.length === 0;
    const optMatch = od.onlyA.length === 0 && od.onlyB.length === 0;
    const textMatch = textOnlyA.length === 0 && textOnlyB.length === 0;
    const verdict = fieldsMatch && optMatch && textMatch && headMatch ? 'MATCH'
      : fieldsMatch && optMatch && headMatch ? 'MINOR' : 'DIFF';

    rows.push({ name, aspStatus: a.status, netStatus: n.status, verdict,
      titleAsp: title(a.body), titleNet: title(n.body),
      fieldsMatch, headMatch, optMatch, textOnlyA, textOnlyB, fieldDiff: fd, headDiff: hd, optDiff: od });
  }

  let match = 0, minor = 0, diff = 0;
  for (const r of rows) {
    const tag = r.verdict.padEnd(5);
    if (r.verdict === 'MATCH') match++; else if (r.verdict === 'MINOR') minor++; else diff++;
    console.log(`\n[${tag}] ${r.name.padEnd(15)} ASP=${r.aspStatus} NET=${r.netStatus}  fields=${r.fieldsMatch ? 'ok' : 'DIFF'} opts=${r.optMatch ? 'ok' : 'DIFF'} headings=${r.headMatch ? 'ok' : 'DIFF'}`);
    if (r.titleAsp !== r.titleNet) console.log(`   title:  ASP="${r.titleAsp}"  NET="${r.titleNet}"`);
    if (r.fieldDiff.onlyA.length) console.log(`   fields only in ASP: ${r.fieldDiff.onlyA.join(', ')}`);
    if (r.fieldDiff.onlyB.length) console.log(`   fields only in NET: ${r.fieldDiff.onlyB.join(', ')}`);
    if (r.optDiff?.onlyA.length) console.log(`   options only in ASP: ${r.optDiff.onlyA.join('  ')}`);
    if (r.optDiff?.onlyB.length) console.log(`   options only in NET: ${r.optDiff.onlyB.join('  ')}`);
    if (r.headDiff.onlyA.length) console.log(`   headings only in ASP: ${r.headDiff.onlyA.join(' | ')}`);
    if (r.headDiff.onlyB.length) console.log(`   headings only in NET: ${r.headDiff.onlyB.join(' | ')}`);
    if (r.textOnlyA.length) console.log(`   text only in ASP: ${r.textOnlyA.slice(0, 40).join(' ')}`);
    if (r.textOnlyB.length) console.log(`   text only in NET: ${r.textOnlyB.slice(0, 40).join(' ')}`);
  }
  console.log(`\n==== SUMMARY: ${match} MATCH, ${minor} MINOR, ${diff} DIFF of ${rows.length} pages ====`);
}
run().catch((e) => { console.error(e); process.exit(1); });
