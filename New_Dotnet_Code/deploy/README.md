# Deploying the Liberum Help Desk .NET port (Linux, no Docker)

This is a plain ASP.NET Core 10 app on Kestrel with a **single-file SQLite database** — there is no database server to install and **no container is required**.

## 1. Publish

```bash
dotnet publish src/LiberumHelpDesk.Web/LiberumHelpDesk.Web.csproj -c Release -o ./publish
```

The `publish/` output contains the app, the `Data/` folder (schema + seed SQL and the 7 language files), and `wwwroot/` (CSS, status images, uploaded profile pictures).

## 2. Run (quick test)

```bash
cd publish
ASPNETCORE_URLS=http://0.0.0.0:5000 dotnet LiberumHelpDesk.Web.dll
```

On first start the `DatabaseSeeder` (the `setup.asp` replica) creates the SQLite schema, seeds the base data, and imports the seven language files. Browse to `http://<host>:5000/`.

## 3. Run as a service (systemd)

```bash
sudo mkdir -p /opt/liberumhelpdesk /var/lib/liberum
sudo cp -r publish/* /opt/liberumhelpdesk/
sudo chown -R www-data:www-data /opt/liberumhelpdesk /var/lib/liberum
sudo cp deploy/liberumhelpdesk.service /etc/systemd/system/
sudo systemctl daemon-reload
sudo systemctl enable --now liberumhelpdesk
sudo systemctl status liberumhelpdesk
```

The unit points the connection string at `/var/lib/liberum/helpdesk.db` so re-publishing never clobbers the data file.

## 4. (Optional) Reverse proxy

Put nginx/Apache in front of Kestrel for TLS and a friendly port. Kestrel itself serves `/css`, `/image`, and the app routes directly.

## Configuration

Everything host-level is in `appsettings.json` under `Liberum` (or override via environment variables, double-underscore syntax):

| Setting | Purpose |
|---|---|
| `ConnectionStrings:HelpDesk` | SQLite connection string |
| `Liberum:Debug` | keep **false** in production / for parity |
| `Liberum:Culture` | locale for date/number formatting (lock to the IIS-oracle box, default `en-US`) |
| `Liberum:SeedOnStartup` | seed the DB on first boot |
| `Liberum:SeedAdminUser` | dev-only sample `admin`/`admin` rep (leave **false** in production) |
| `Liberum:SessionTimeoutMinutes` | session idle timeout (40, matching the original) |

Runtime, admin-editable settings (site name, email type/SMTP, default priority/status, auth type, etc.) live in the `tblConfig` row and are edited at **/Admin → Configure Site** (the admin password gate default is `admin` — change it immediately via **Change Admin Password**).
