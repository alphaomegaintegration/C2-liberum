# Remediation Plan (Modernization Scope Only)

Scope note applied: the Playwright suite is treated as separate, recent work and not a modernization acceptance gate.

## 1. Acceptance Scope Reset (Day 0)

Objective: freeze what done means for modernization so unrelated recent work does not block release.

1. Define gating suites:
	- Gate on xUnit modernization suite in [New_Dotnet_Code/tests/LiberumHelpDesk.Tests/LiberumHelpDesk.Tests.csproj](New_Dotnet_Code/tests/LiberumHelpDesk.Tests/LiberumHelpDesk.Tests.csproj)
	- Do not gate modernization on [Liberum.PlaywrightTests/Tests/LiberumSmokeTests.cs](Liberum.PlaywrightTests/Tests/LiberumSmokeTests.cs)
2. Update documentation language:
	- Add explicit statement in [modernization_quality_report.md](modernization_quality_report.md)
	- Add scope note to [New_Dotnet_Code/REPLICA-CHECKLIST.md](New_Dotnet_Code/REPLICA-CHECKLIST.md)

Exit criteria:
- Modernization gate definition is written and agreed in repo docs.

## 2. P0 Functional Blocker: Fix Red Regression (Day 0-1)

Objective: return modernization suite to green with deterministic formatting behavior.

Issue:
- One failing test in [New_Dotnet_Code/tests/LiberumHelpDesk.Tests/DateServiceTests.cs](New_Dotnet_Code/tests/LiberumHelpDesk.Tests/DateServiceTests.cs#L24), caused by whitespace variant in time formatting (narrow non-breaking space vs normal space).

Actions:
1. Normalize output in [New_Dotnet_Code/src/LiberumHelpDesk.Web/Services/DateService.cs](New_Dotnet_Code/src/LiberumHelpDesk.Web/Services/DateService.cs) after time formatting:
	- replace Unicode narrow or non-breaking separators with regular ASCII space.
2. Keep parity intent:
	- output should remain legacy-style human time while deterministic across ICU and culture variants.
3. Re-run:
	- dotnet test New_Dotnet_Code/tests/LiberumHelpDesk.Tests/LiberumHelpDesk.Tests.csproj

Exit criteria:
- 82 of 82 modernization tests passing.
- No new failures in fidelity or parity test classes.

## 3. P1 Security Remediation: Dependency CVEs (Day 1-2)

Objective: remove known vulnerable dependencies from modernization build path.

Known from latest run:
- High severity: SQLite native package path.
- Moderate severity: AngleSharp in tests.

Actions:
1. Inventory vulnerable dependency graph:
	- dotnet list New_Dotnet_Code/src/LiberumHelpDesk.Web/LiberumHelpDesk.Web.csproj package --vulnerable --include-transitive
	- dotnet list New_Dotnet_Code/tests/LiberumHelpDesk.Tests/LiberumHelpDesk.Tests.csproj package --vulnerable --include-transitive
2. Upgrade strategy:
	- Pin or upgrade SQLite-related package chain to patched versions in:
	  - [New_Dotnet_Code/src/LiberumHelpDesk.Web/LiberumHelpDesk.Web.csproj](New_Dotnet_Code/src/LiberumHelpDesk.Web/LiberumHelpDesk.Web.csproj)
	  - [New_Dotnet_Code/tests/LiberumHelpDesk.Tests/LiberumHelpDesk.Tests.csproj](New_Dotnet_Code/tests/LiberumHelpDesk.Tests/LiberumHelpDesk.Tests.csproj)
	- Upgrade AngleSharp in test project to a non-vulnerable release.
3. Validate:
	- dotnet restore
	- dotnet test modernization suite
	- re-run vulnerable package scan

Exit criteria:
- No high-severity vulnerabilities in modernization runtime and test dependency graph.
- Modernization tests still green.

## 4. P1 Documentation Truth Sync (Day 2)

Objective: ensure repo claims match current reality.

Actions:
1. Update current status sections in:
	- [New_Dotnet_Code/REPLICA-CHECKLIST.md](New_Dotnet_Code/REPLICA-CHECKLIST.md)
	- [New_Dotnet_Code/parity/ACCEPTED-DIVERGENCES.md](New_Dotnet_Code/parity/ACCEPTED-DIVERGENCES.md)
	- [New_Dotnet_Code/parity/PARITY-REPORT.md](New_Dotnet_Code/parity/PARITY-REPORT.md)
2. If tests are green again, restore all green wording; otherwise reflect exact counts and open items.

Exit criteria:
- No stale all-green claims when suite is not fully green.
- Checklist reflects actual gate decisions (Playwright out-of-scope for modernization).

## 5. P2 Optional Hardening (Not a modernization blocker)

Objective: keep the new Playwright effort useful but independent.

Actions:
1. Either mark non-gating in docs, or maintain as separate CI job.
2. Confirm Playwright smoke tests are all discovered and executing (e.g., ensure each test method has a `[Test]` attribute).
3. Keep a separate UI smoke badge or status from modernization parity gate.

Exit criteria:
- Clear separation between modernization parity and post-modernization UI smoke checks.

## Execution Order (Recommended)

1. Scope reset plus documentation note
2. DateService deterministic fix and green xUnit
3. CVE upgrades and revalidation
4. Documentation truth sync
5. Optional Playwright hardening track

## Success Definition

Modernization is considered remediated when:

1. xUnit modernization suite is fully green.
2. High-severity package vulnerabilities are cleared.
3. Parity and fidelity docs are accurate and current.
4. Playwright status is explicitly non-gating for modernization.
