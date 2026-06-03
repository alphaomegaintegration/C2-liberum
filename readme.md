# C2 Liberum Help Desk Modernization

This repository contains a modernization of **Liberum Help Desk 0.98** from a legacy **Classic ASP / VBScript** application to a modern **ASP.NET Core MVC** application.

The project keeps the original source available for reference while providing a .NET implementation that preserves the behavior, routes, visual output, language strings, and seeded data model of the original application as closely as possible.

## Repository layout

```text
C2-liberum/
├── Old_ClassicASP_Code/
│   └── LiberumASP/
│       ├── www/                  # Original Classic ASP web application
│       ├── db/                   # Original Access database and SQL Server seed script
│       ├── readme.txt            # Original Liberum Help Desk documentation
│       ├── changelog.txt
│       └── ProgrammingStyleGuide.txt
│
└── New_Dotnet_Code/
    ├── src/
    │   └── LiberumHelpDesk.Web/  # ASP.NET Core MVC modernization
    ├── tests/
    │   └── LiberumHelpDesk.Tests/# Unit and integration tests
    ├── parity/                  # Classic ASP vs .NET parity tooling and reports
    ├── deploy/                  # Linux/systemd deployment notes
    ├── tools/                   # Database and fixture helper scripts
    ├── REPLICA-CHECKLIST.md     # Modernization fidelity checklist
    └── LiberumHelpDesk.slnx     # .NET solution file
```

## Modernization summary

The new application is a .NET MVC port of the original Classic ASP help desk system. It includes the major functional areas from the legacy application:

- Public landing, logon, logoff, registration, and forgotten-password flows
- User problem creation, viewing, updating, and printing
- Support representative problem queues, search, assignment, updates, closing/reopening, and printing
- Knowledge base search, details, and print views
- In/Out board status and image upload support
- Administrative configuration, users, lookups, languages, reports, email settings, and system information
- Original styling, language files, status images, and seeded configuration data

The modernization uses ASP.NET Core MVC areas to mirror the structure of the original ASP folders:

```text
Classic ASP folder       .NET MVC area/controller structure
------------------       ----------------------------------
/admin                   Areas/Admin
/user                    Areas/User
/rep                     Areas/Rep
/kb                      Areas/Kb
/inout                   Areas/Inout
root ASP pages           Controllers + Views at the app root
```

## Technology stack

### Original application

- Classic ASP / VBScript
- IIS
- Microsoft Access `.mdb` database and SQL Server seed script
- ASP-era email integration options such as CDONTS, JMail, ASPEmail, ASPMail, and CDOSYS

### Modernized application

- ASP.NET Core MVC targeting `net10.0`
- Razor views
- SQLite database
- Dapper
- Microsoft.Data.Sqlite
- MailKit
- xUnit integration and regression tests
- Node.js parity comparison scripts for Classic ASP vs .NET output comparison

## Prerequisites

To build and run the modernized application:

- .NET 10 SDK
- A terminal or shell environment
- Optional: Node.js, only needed for the parity comparison scripts under `New_Dotnet_Code/parity`
- Optional: IIS with Classic ASP support, only needed if running the original ASP application as a live comparison oracle

## Quick start: run the .NET application

From the repository root:

```bash
cd New_Dotnet_Code

dotnet restore src/LiberumHelpDesk.Web/LiberumHelpDesk.Web.csproj
dotnet build src/LiberumHelpDesk.Web/LiberumHelpDesk.Web.csproj

dotnet run --project src/LiberumHelpDesk.Web/LiberumHelpDesk.Web.csproj
```

By default, the development launch profile uses:

```text
http://localhost:5100
```

On first startup, the .NET application creates and seeds a local SQLite database using the scripts and language files in:

```text
src/LiberumHelpDesk.Web/Data/
```

The seeding process replaces the original `setup.asp` install step from the Classic ASP application.

## Development login notes

The original application has an admin password gate with the default password:

```text
admin
```

In the .NET development configuration, `SeedAdminUser` is enabled so a sample database-authenticated support representative is available for local testing:

```text
Username: admin
Password: admin
```

For any production-like deployment, change the admin password immediately and review the seeded/default configuration through the Admin area.

## Run tests

From `New_Dotnet_Code`:

```bash
dotnet test tests/LiberumHelpDesk.Tests/LiberumHelpDesk.Tests.csproj
```

The test project includes unit and integration coverage for the modernization, including fidelity-related fixes and regression tests.

## Parity verification

The `New_Dotnet_Code/parity` folder contains tools and reports used to compare the Classic ASP application against the .NET port.

Important files:

```text
parity/PARITY-REPORT.md          # Page-by-page comparison summary
parity/ACCEPTED-DIVERGENCES.md   # Documented intentional differences
parity/IIS-ORACLE-SETUP.md       # Setup notes for running Classic ASP as the oracle
parity/compare.mjs               # DOM/text parity comparison script
parity/compare-attrs.mjs         # Attribute-level parity comparison script
```

The included parity report documents a broad comparison of the original application and the .NET implementation, including public, user, representative, knowledge base, in/out board, and administrative pages.

One intentional difference is the system information page, where the .NET version reports the actual .NET/SQLite host environment instead of pretending to be IIS/Access/Jet.

## Configuration

The modernized app uses `appsettings.json` and standard ASP.NET Core configuration overrides.

Main settings are under:

```json
"ConnectionStrings": {
  "HelpDesk": "Data Source=helpdesk.db;Cache=Shared"
},
"Liberum": {
  "Debug": false,
  "LangFileDirectory": "Data/lang",
  "ImageUploadDirectory": "wwwroot/image",
  "SessionTimeoutMinutes": 40,
  "SeedOnStartup": true,
  "SeedAdminUser": false,
  "Culture": "en-US"
}
```

Common settings:

| Setting | Purpose |
|---|---|
| `ConnectionStrings:HelpDesk` | SQLite database location |
| `Liberum:SeedOnStartup` | Runs the schema/data/language seeding process on first startup |
| `Liberum:SeedAdminUser` | Adds a development-only `admin/admin` user when enabled |
| `Liberum:SessionTimeoutMinutes` | Session idle timeout, matching the legacy behavior |
| `Liberum:Culture` | Controls date and number formatting for parity with the original app |
| `Liberum:LangFileDirectory` | Location of imported language files |
| `Liberum:ImageUploadDirectory` | Location for uploaded user images/status images |

Runtime help desk settings such as site name, email behavior, default status, default priority, authentication mode, and admin password are stored in the database and managed through the Admin area.

## Publish and deploy

A deployment guide is provided in:

```text
New_Dotnet_Code/deploy/README.md
```

Basic publish command:

```bash
cd New_Dotnet_Code
dotnet publish src/LiberumHelpDesk.Web/LiberumHelpDesk.Web.csproj -c Release -o ./publish
```

Run the published app:

```bash
cd publish
ASPNETCORE_URLS=http://0.0.0.0:5000 dotnet LiberumHelpDesk.Web.dll
```

The deployment folder also includes a sample `systemd` service file for running the application on Linux without Docker.

## Original Classic ASP application

The original application is preserved under:

```text
Old_ClassicASP_Code/LiberumASP
```

Its original documentation is available in:

```text
Old_ClassicASP_Code/LiberumASP/readme.txt
```

The legacy application requires IIS with Classic ASP support and uses the original Access or SQL Server database setup flow. It is mainly included here as the source reference and as a parity oracle for validating the .NET modernization.

## Notes for reviewers

This repository is best reviewed as a modernization/reference implementation, not as a brand-new greenfield help desk product.

Recommended review order:

1. Read the original app overview in `Old_ClassicASP_Code/LiberumASP/readme.txt`.
2. Review the .NET app structure under `New_Dotnet_Code/src/LiberumHelpDesk.Web`.
3. Run the .NET app locally and exercise the main user, representative, and admin flows.
4. Run the tests under `New_Dotnet_Code/tests`.
5. Review `New_Dotnet_Code/parity/PARITY-REPORT.md` and `New_Dotnet_Code/REPLICA-CHECKLIST.md` for modernization fidelity details.

## Current modernization status

The .NET port includes a substantial replica of the Classic ASP behavior and page surface, with automated tests and parity documentation. Some legacy behavior is intentionally preserved even when it reflects Classic ASP quirks, because the main goal of this project is modernization fidelity.

Future improvement areas may include:

- Replacing legacy-compatible UI patterns with a modern front end
- Adding stronger authentication and password handling
- Hardening email configuration for production environments
- Adding SQL Server or PostgreSQL support if needed
- Improving deployment automation
- Adding CI/CD pipeline integration
- Removing legacy parity constraints after business acceptance

## License

The original Liberum Help Desk project is distributed under the GNU General Public License according to the legacy `license.html` and original documentation. Review the original license file before redistributing or using this modernization in a client or production context.
