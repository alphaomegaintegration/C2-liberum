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
│       └── readme.txt            # Original Liberum Help Desk documentation
│
└── New_Dotnet_Code/
    ├── src/
    │   └── LiberumHelpDesk.Web/  # ASP.NET Core MVC modernization
    ├── tests/
    │   └── LiberumHelpDesk.Tests/# Unit and integration tests
    ├── parity/                  # Classic ASP vs .NET parity tooling and reports
    ├── deploy/                  # Linux/systemd deployment notes
    ├── tools/                   # Database and fixture helper scripts
    └── REPLICA-CHECKLIST.md     # Modernization fidelity checklist
```

## Quick start

```bash
cd New_Dotnet_Code

dotnet restore src/LiberumHelpDesk.Web/LiberumHelpDesk.Web.csproj
dotnet build src/LiberumHelpDesk.Web/LiberumHelpDesk.Web.csproj
dotnet run --project src/LiberumHelpDesk.Web/LiberumHelpDesk.Web.csproj
```

Open:

```text
http://localhost:5100
```

Run tests:

```bash
dotnet test tests/LiberumHelpDesk.Tests/LiberumHelpDesk.Tests.csproj
```

## Notes

The default admin gate password from the original app is:

```text
admin
```

For local development, the .NET project also seeds a sample database-authenticated user:

```text
Username: admin
Password: admin
```

Change these immediately for any production-like deployment.

See the full README file for modernization details, parity verification, deployment notes, configuration, and reviewer guidance.
