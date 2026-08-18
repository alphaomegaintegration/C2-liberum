# LiberumASP → .NET 10 — "100% replica" verification checklist

Scope note: this checklist is for modernization acceptance only. The separate Playwright project under
`Liberum.PlaywrightTests/` is a recent, independent workstream and is not a modernization gate.

## Build & tests
- [x] `dotnet build` clean (0 warnings, 0 errors)
- [x] `dotnet test tests/LiberumHelpDesk.Tests` all green — **82 passing** (unit + integration)
- [x] `dotnet list` vulnerable scan clean for both Web and modernization tests (no vulnerable packages)
- [x] App boots on Kestrel (no Docker), seeds the SQLite DB + 7 languages on first run, serves `/css/default.css`
      + `/image/*_pin.gif`; DB-auth login (admin/admin) returns 302; `/Admin/ViewLang` lists all 7 languages

## Fidelity audit
- [x] Multi-agent fidelity audit run (5 area auditors + synthesizer) vs the original ASP source
- [x] All Critical findings fixed (A1 due-date, A2 Cfg/Usr error page, A3 admin/test, A4 status `*` marker)
- [x] All reachable Important findings fixed (B1–B8); regression tests in `FidelityFixesTests.cs`
- [x] Residual deviations bounded + documented in `parity/ACCEPTED-DIVERGENCES.md`

## Functional coverage (each ASP page has a port)
- [ ] Root: default (landing redirect), logon (NT/Database/External), logoff, register (create + edit + password change), forgotpass (emails plain password)
- [ ] User: new → postnew, details → update, view (sort/page), print
- [ ] Rep: default (menu + view-for-rep), new (create-on-behalf + select-user popup), details (display + update + close + reopen + change-note audit + 5 email triggers), view, search → results, selectuser, print
- [ ] KB: search (LIKE), details, print (CheckKB matrix honoured)
- [ ] Inout: board (pins/search/sort), details (phone formatting), status, update, savefile (image upload)
- [ ] Admin: gate (lhd_IsAdmin), config, cfgemail, adminpass, viewusers/adduser/moduser (+delete), viewcat/viewdep/viewpri/viewstatus (CRUD), viewlang/viewlangstring, reports/viewreports, sysinfo
- [ ] Faithful-error pages: DisplayError red box; CheckRep/CheckKb/CheckAdmin denials; the dead reps/mtype=1 error

## Golden flows (end-to-end, with email capture)
- [ ] Register → log in → submit problem → email(s) fired → details → add note → repupdate email → appears in list
- [ ] Rep assigns/updates → close ticket → status=CloseStatus, close_date, emailsent=1, first_response set, "OPEN => CLOSED" private note, userclose email
- [ ] Reopen a closed ticket → status=DefaultStatus, close_date NULL, status-change note, usernew email
- [ ] KB article searchable + viewable; In/Out status update shows the right pin
- [ ] Admin: config save persists; lookup add→delete reassigns dependents to 0; user add→login; language string edit clears cache; report totals/% correct

## Parity vs the IIS oracle (see parity/IIS-ORACLE-SETUP.md + parity/PARITY-REPORT.md)
- [x] Original ASP stood up in IIS (Access `helpdesk2000.mdb`, Debug=false, all 7 languages; `.mdb` schema
      patched to 0.98 + `tblConfig_Email` aligned to the authoritative `helpdesk.sql`)
- [x] Identical fixtures seeded byte-for-byte into both DBs (`tools/fixtures.sqlite.sql` + mirrored `.mdb`
      INSERTs): admin rep sid 1, category 1, problem 1 (OPEN) + problem 2 (CLOSED/KB), a note, In/Out board
- [x] DOM-normalized diff (`parity/compare.mjs`) over **50 navigable pages: 49 exact MATCH, 1 MINOR
      (sysinfo, intentional host-adaptation), 0 DIFF** — visible text + form fields + select options +
      headings + title match on every page (incl. all needs-id details/print/view/update pages)
- [x] Real port bugs found by the diff and FIXED: config_help + cfgemail_help pages ported (were missing);
      config Help link restored + cfgemail SyntaxHelp 404 fixed; modify hidden fields; confdelete back-link;
      user details/view titles; config bottom Help row; adduser duplicate legend + password `*`;
      rep menu/print title `<% = %>` whitespace-consumption quirk reproduced
- [x] Browser (Claude-in-Chrome) FULL visual sweep: every navigable page opened logged-in on BOTH apps,
      pixel-identical (one foreground tab holding both per-origin sessions). Found + fixed 3 divergences the
      DOM/attr harnesses can't see: user/print mailto link, viewstatus `100*` marker spacing, viewlangstring
      case-insensitive order (+ systemic `COLLATE NOCASE` on cname/dname/uid lists & dropdowns)
- [x] Attribute-level harness `parity/compare-attrs.mjs` (mailto/button/img/input-value): 49 clean, 0 diffs
- [ ] (optional) SQL Server backend run for byte-level seed parity; DB row-state diff after scripted actions

## Preserved VBScript quirks (spot-check)
- [ ] rep/details title truncates to 50; user/postnew to 255
- [ ] user/view (and user/print) have NO CheckUser guard
- [ ] DisplayDate components are NON zero-padded (e.g. `2026-6-2`)
- [ ] Language lookup is case-insensitive (collation fix) with `@var@`/`!var!` fallbacks
- [ ] Cint uses banker's rounding; GetSid returns 0 when absent
