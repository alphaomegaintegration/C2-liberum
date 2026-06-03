# IIS reference-oracle setup (Windows) for parity diffing

To prove the .NET port is a true 1:1 replica, run the **original Classic ASP** app in IIS on this Windows box and diff its output against the .NET port for an identical seeded dataset.

---

## Verified quick-start — Access `.mdb` backend (no SQL Server)

This is the path that was actually used for `parity/PARITY-REPORT.md` (runs fully offline; the shipped
`db/helpdesk2000.mdb` + the 32-bit Jet 4.0 OLEDB provider that already ships with 64-bit Windows):

```powershell
# 1. Install IIS + Classic ASP
Install-WindowsFeature Web-Server,Web-Asp,Web-Default-Doc,Web-Static-Content,Web-Mgmt-Console,Web-Scripting-Tools

# 2. Deploy the Access DB to the path settings.asp expects (DBType=3, AccessPath)
New-Item -ItemType Directory -Force C:\Inetpub\Databases | Out-Null
Copy-Item .\Old_ClassicASP_Code\LiberumASP\db\helpdesk2000.mdb C:\Inetpub\Databases\ -Force
Set-ItemProperty C:\Inetpub\Databases\helpdesk2000.mdb IsReadOnly $false   # Jet needs read/write
icacls C:\Inetpub\Databases /grant "IIS APPPOOL\LiberumASP:(OI)(CI)M"       # + .ldb lock file

# 3. App pool MUST be 32-bit (Jet 4.0 OLEDB is 32-bit only)
Import-Module WebAdministration
New-WebAppPool LiberumASP
Set-ItemProperty IIS:\AppPools\LiberumASP enable32BitAppOnWin64 $true

# 4. Serve the app from under inetpub (NOT a user profile — IIS_IUSRS can't traverse C:\Users\<admin>)
Copy-Item .\Old_ClassicASP_Code\LiberumASP\www C:\inetpub\LiberumASP -Recurse -Force
New-Website -Name LiberumASP -Port 8080 -PhysicalPath C:\inetpub\LiberumASP -ApplicationPool LiberumASP

# 5. Parent Paths is locked at server scope -> set it on apphost
Set-WebConfigurationProperty -PSPath MACHINE/WEBROOT/APPHOST -Filter system.webServer/asp -Name enableParentPaths -Value $true
```

Then import the languages (the `.mdb` ships with the schema but **no** language strings) and create a login user:

```powershell
# import all 7 languages + overwrite (curl from any shell)
curl -X POST http://localhost:8080/setup.asp -d "updatelang=1&overwrite=on&english=on&norwegian=on&danish=on&dutch=on&german=on&french=on&spanish=on"
# gate admin (pwd 'admin'), then add an admin/admin rep to mirror the .NET sample user
```

Gotchas seen: a read-only `.mdb` or a non-writable folder → Jet "**Operation must use an updateable query**";
serving from a user-profile path → **401.3** (grant `IIS_IUSRS` ReadAndExecute, or copy under `inetpub`).

### Make the `.mdb` match the authoritative `helpdesk.sql` seed, then load fixtures

The shipped `helpdesk2000.mdb` is a partially-migrated sample, so two one-off fixes are needed before a
clean full-surface parity run (run each as a throwaway `.asp` via the 32-bit app pool, then delete it —
Jet can't be reached from 64-bit PowerShell):

1. **Schema** — `problems` is missing two 0.98 columns (`setup.asp`'s migration uses T-SQL `ALTER COLUMN`
   that Jet rejects): `ALTER TABLE problems ADD COLUMN due_date DATETIME` and
   `ALTER TABLE problems ADD COLUMN first_response DATETIME`.
2. **Email types** — align `tblConfig_Email` to `helpdesk.sql`: `UPDATE tblConfig_Email SET [Type]='JMail'
   WHERE ID=2` and `INSERT INTO tblConfig_Email (ID,[Type]) VALUES (5,'CDOSYS (Recommended)')`.

Then seed **identical fixtures** into both DBs (so the needs-id pages compare apples-to-apples with fixed
dates): apply `tools/fixtures.sqlite.sql` to the SQLite DB, and the same rows (Access `#yyyy-mm-dd hh:nn:ss#`
date literals) to the `.mdb`. Both DBs must start from the clean seed (`db_keys (1,2,1,1,2)`, admin = sid 1)
so the fixtures get matching ids. See `parity/PARITY-REPORT.md`.

---

## Full setup (SQL Server backend — authoritative `helpdesk.sql`)

## 1. Enable IIS + Classic ASP

```powershell
Enable-WindowsOptionalFeature -Online -FeatureName IIS-WebServerRole, IIS-WebServer, IIS-ASP, IIS-ISAPIExtensions, IIS-ISAPIFilter -All
```

Enable **Parent Paths** (the app uses `../` includes) and turn **debugging off** (parity requires `Application("Debug")=false`):

```powershell
Import-Module WebAdministration
Set-WebConfigurationProperty -Filter '/system.webServer/asp' -PSPath 'IIS:\Sites\Default Web Site' -Name 'enableParentPaths' -Value $true
Set-WebConfigurationProperty -Filter '/system.webServer/asp' -PSPath 'IIS:\Sites\Default Web Site' -Name 'scriptErrorSentToBrowser' -Value $false
```

## 2. Host the original app

Point a site/app at `...\Old_ClassicASP_Code\LiberumASP\www`. Set the app pool to **No Managed Code**, 32-bit if you use the Access/Jet provider.

## 3. Database backend (use SQL Server, NOT the .mdb)

Edit `www/settings.asp` → `DBType = 1` (SQL Server + SQL auth) and the `SQLServer`/`SQLDBase`/`SQLUser`/`SQLPass` values. Create the DB from `db/helpdesk.sql`. **Why SQL Server, not the `.mdb`:** the SQL schema is authoritative, it avoids the 32-bit Jet app-pool requirement, and — critically — it keeps the dead `reps`/mtype=1 path **erroring on both sides** (the `.mdb` reintroduces `reps` and would diverge from the port's faithful error).

In `www/settings.asp` keep `Application("Debug") = false`.

## 4. Seed identically

Browse to `/setup.asp`, **tick all seven languages + Overwrite**, and submit. This imports `tblLangStrings` in the same order the .NET seeder uses (English id=1, then Norwegian, Danish, Dutch, German, French, Spanish = ids 2–7). Set the `tblConfig` values to match `helpdesk.sql`. Add the same sample data (categories, users, a few problems) to both the oracle DB and the .NET SQLite DB.

## 5. Lock the locale

The oracle's `FormatNumber`/`vbLongTime` follow the Windows regional settings. Detect it (`Get-Culture`; Windows Server default `en-US`) and set the .NET app's `Liberum:Culture` to match, so report numbers and long-time renderings line up.

## 6. Diff

Use the harness in `parity/` (DOM-normalized AngleSharp compare as the gate; raw-byte diff advisory). For a fixed route list and identical seeded state, capture original-HTML vs .NET-HTML, normalize (strip session ids, dynamic timestamps, whitespace; treat css/image href depth as non-significant), and diff. Also compare DB row-state after identical scripted actions.

### Accepted / out-of-scope divergences (whitelist)
- Anti-forgery tokens (the port disables them, matching the original's absence).
- Email SMTP headers/MIME (parity is subject + token-substituted body only).
- `Debug=true` raw IIS-500 error pages (un-replicable; both sides run `Debug=false`).
- Accented-character `LIKE` results (SQLite is case-sensitive for non-ASCII vs SQL Server's collation).
- css/image relative-href depth (`../default.css` vs `/css/default.css`).
- The dead `viewrep.asp`/mtype=1 path errors on both sides.
