# Accepted divergences & fidelity-audit outcome

This is the bounded envelope inside which the .NET 10 port is a faithful replica of LiberumASP.
A multi-agent fidelity audit (5 area auditors + synthesizer) compared the port against the original
Classic ASP source. Its verdict: an "unusually faithful replica." Every **Critical** and the reachable
**Important** findings have been **fixed** (see below); the remainder are documented, accepted deviations.

## Fixed after the audit (behavioural parity restored)

| # | Severity | Issue | Fix |
|---|----------|-------|-----|
| A1 | Critical | `ConvertFormattedDate` returned *tomorrow* on a parse failure → a malformed due date was silently stored. | Returns `null` on parse failure; user/rep create + rep update reject it via `DisplayError(1,"DueDate")`. `Services/DateService.cs`. |
| A2 | Critical | `Cfg()`/`Usr()` threw `ArgumentException`/returned null on an absent setting/user → raw HTTP 500. | Both now throw the faithful `DisplayError(3)` red box (`ErrorService.Generic`, no DI cycle): "`<setting>` is an invalid setting." / "User not found." `Services/ConfigService.cs`, `Services/UserService.cs`. |
| A3 | Critical | `admin/test.asp` missing → broken `/Admin/Test` menu link, no Send-Test-Email, SysInfo unreachable. | Added `ConfigController.Test` ([CheckAdmin]) + `Views/Config/Test.cshtml`: System Information link + Send Test Email (`?doit=1` → SMTP to `HDReply`). |
| A4 | Critical | Status list lost the `CloseStatus` `*` marker + "Closed Status. Do not delete" footnote. | `ViewStatus` flags the CloseStatus row; `List.cshtml` renders the marker + footnote. |
| B1 | Important | KB `KBFreeText=1` FREETEXT branch missing. | Branch reproduced; FREETEXT errors on SQLite are caught → faithful `TrapError` box. `Areas/Kb/.../HomeController.cs`. |
| B2 | Important | rep `print.asp` empty-id gave "ProblemID 0 was found…" instead of "No valid problem ID was entered." | Empty-id guard added. `Areas/Rep/.../ProblemController.cs`. |
| B3 | Important | In/Out `update` access check made the original's dead `len(mId)=0` clause live → admin wrongly denied. | Tests coerced `mId.ToString().Length` (always ≥1) — clause stays dead. `Areas/Inout/.../HomeController.cs`. |
| B4 | Important | Rep paging clamped `num`/`start` to >0; original passes `CInt` through. | Clamps removed; window reproduces the original `start ≤ i ≤ start+num-1` counter loop. Rep `ProblemController` + `SearchController`. |
| B5 | Important | In/Out `savefile` `maxSize>0` guard inverted behaviour when `MaxImageSize`=0. | Guard removed → `length>maxSize` rejects, matching `FileSizeIsBad()`. |
| B6 | Important | `viewpri`/`viewstatus` first header used `PriorityNumber`/`StatusNumber`. | Both use the `ID` lang key. |
| B7 | Important (dead) | `confdelete` missing `mtype=7` language-string guard. | `mtype==7 && id>0` → "You must delete variables from english language" reproduced (see deviation note below). |
| B8 | Important | `modify` back-link label hard-coded "Categories". | Label switches per mtype (Categories/Departments/Priorities/Statuses/Languages). |

Regression coverage for the fixes lives in `tests/.../FidelityFixesTests.cs` (and `ParityFixesTests.cs` for
the browser-sweep findings); full suite = **82 green**.

## Full-surface live parity run (see parity/PARITY-REPORT.md)

A later run of `parity/compare.mjs` over the **complete navigable surface (50 pages)** with identical
fixtures seeded into both DBs returned **49 exact MATCH, 1 MINOR (`sysinfo`, intentional host-adaptation),
0 DIFF**. It found and fixed further real port gaps: the two help pages above (C-4/C-5), the `modify` hidden
`mLanguage`/`mLangID` fields, the `confdelete` per-mtype back-link, `user/details`+`user/view` titles, the
`config` bottom Help row, the `adduser` duplicated legend + New-Password `*`, and the **Classic ASP
`<% = expr %>` whitespace-consumption quirk** (whitespace-only literal between adjacent output tags is
dropped → `rep` menu title "Company NameHelp Desk", `rep/print` "Problem1Details", `confdelete`
"ManageCategories"), which the port now reproduces. The only remaining divergence is the intentionally
host-adapted `sysinfo` page.

## Full browser visual sweep (2026-06-03)

Every navigable page was then opened in a real browser, **logged in on both apps**, and compared
side-by-side; an attribute-level harness (`parity/compare-attrs.mjs`: mailto hrefs, button `value=` labels,
`<img>` basenames, input defaults) was added to cover what the tag-stripping DOM diff cannot see. All pages
were pixel-identical except `sysinfo` (intentional) and **three real divergences found here and fixed** — see
parity/PARITY-REPORT.md: (1) the `user/print` "Assigned To" mailto link (was plain text); (2) the
`viewstatus` CloseStatus `*` marker spacing (`100*` not `100 *`, the `<%= %>` whitespace quirk); (3)
`viewlangstring` row order — Access/SQL Server collate case-INSENSITIVELY, so SQLite now uses
`COLLATE NOCASE` (verified 395 rows position-for-position identical), hardened across all user-facing
`ORDER BY cname/dname/uid` lists + dropdowns. `compare-attrs.mjs` reports 49 clean / 0 attribute diffs; the
suite is **82 green** (`ParityFixesTests.cs`). These three are the class of bug that only a rendered/structural
comparison surfaces: a link vs text, a tag-induced space, and a multiset-equal-but-reordered list.

## Accepted deviations (out of the 100% envelope, by design or platform)

Confirmed by the audit as acceptable; no action:

- **SQLite + dynamic typing** vs SQL Server (e.g. `AllowImageUpload` quoted-string vs int; accented `LIKE`
  is case-sensitive in SQLite).
- **Plain-text passwords** (faithful, user-chosen), **NT-auth Linux stub** (header-based), **`reps`/mtype=1
  dead path** rendered as a faithful error.
- **Raw `<%= %>` / `Html.Raw` output** and **antiforgery globally disabled** (byte/DOM parity).
- **MailKit collapse** of the five `EmailType` providers to one SMTP send (parity scope = subject + token body).
- **32-bit `Vb.CInt`** vs VBScript 16-bit `CInt` overflow; **parameterised SQL** replacing string-built WHERE/
  `SQLDate`; **`GetUnique` transaction-wrapped** (same observable ids).
- **`DisplayDate` long-time locked to en-US** (oracle assumed en-US); never-set `Session("IsRep")/("IsAdmin")`
  chrome quirk preserved.

### Minor cosmetic items deferred (audit class "c")

- **C-1** Note rendering converts a lone `\n` (not just `\r\n`) → a possible extra `<br/>` for notes with bare `\n`.
- **C-2** In/Out `details`/`status` empty-id error runs after `[CheckUser]` → unauthenticated + no id gets a
  logon redirect instead of the "No valid ID given" page (filter-order artefact).
- **C-3** Rep `results` Prev/Next hidden dates echoed as ISO `yyyy-MM-dd HH:mm:ss` vs the original `m/d/yyyy …`
  (re-parses to the same instant; byte-non-identical only).
- **C-4 / C-5** `config_help`/`cfgemail_help` — **now PORTED** (`/Admin/ConfigHelp`, `/Admin/CfgEmailHelp`),
  Config "Help" link + cfgemail "SyntaxHelp" link restored. `default_german` alt menu is an unlinked CVS
  duplicate of `default.asp` and stays unported (no .NET equivalent; the single `/Admin` serves all languages).
- **C-6** `logoff` "Click here to login" points at a literal `…/logon.asp` (faithful to the original's broken-on-
  mismatched-host link).
- **B9** `postnew` reads `department` via `Vb.CInt` vs the original's raw string — identical for the integer
  dropdown; differs only for a non-numeric injected value (clean "Department required" error vs a type mismatch).
- **B7 (remainder)** the `confdelete` *case-7 confirm view* + `delete.asp mtype=7` language-variable delete are
  not built. This path is **unreachable in the original too** (no UI links `confdelete?mtype=7`); only the
  `id>0` guard is reproduced, consistent with the reps/mtype=1 dead-path treatment.
