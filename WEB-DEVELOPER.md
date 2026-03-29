# Pricer — Web App

Browser-based UI for the same 3D-printing cost management features as the CLI: filament warehouse, printers, currencies, settings, and transaction history. Served by an ASP.NET Core 10 backend with JWT authentication and an Angular 20 frontend.

Supports the same two data backends as the CLI (local JSON file or SQL Server). User/auth data is always kept in a separate store: an SQLite file (`security.db`) in File mode, or SQL Server with a `security.` schema in Mssql mode.

## Prerequisites

- .NET SDK `10.0`
- Node.js `22`

## Run (development)

```pwsh
cd .\Pricer.WebApp\pricer.webapp.client
npm install

cd ..\..\
cd .\Pricer.WebApp\Pricer.WebApp.Server
dotnet user-secrets set "Jwt:Key" "your-secret-key-at-least-32-characters-long"
dotnet run
```

The Angular dev server starts automatically as a SPA proxy. Open `https://localhost:5001` (or whichever port is shown in the terminal).

## Configuration

The web server loads configuration from (in order):

1. `Pricer.WebApp/Pricer.WebApp.Server/appsettings.json`
2. `Pricer.WebApp/Pricer.WebApp.Server/appsettings.Development.json` (optional)
3. Environment variables (no prefix; use `__` for nested keys)
4. User Secrets

### JWT key (required)

A signing key of at least 32 characters must be provided. Never commit it to source control.

**User Secrets (recommended for local dev):**

```pwsh
cd .\Pricer.WebApp\Pricer.WebApp.Server
dotnet user-secrets set "Jwt:Key" "your-secret-key-at-least-32-characters-long"
```

**Environment variable (Docker / production):**

```
Jwt__Key=your-secret-key-at-least-32-characters-long
```

Optional JWT settings (defaults shown):

```json
{
  "Jwt": {
    "Issuer": "PL.PrintPrice",
    "Audience": "PL.PrintPrice",
    "ExpirationHours": 24
  }
}
```

### Data access mode

Domain data and security data are configured independently but both follow `DataAccess:Mode`.

#### File (default)

Zero-setup. All data lives in two local files in the working directory.

```json
{
  "DataAccess": {
    "Mode": "File",
    "FilePath": "data.json",
    "SecurityFilePath": "security.db"
  }
}
```

- `FilePath` — domain data (printers, materials, currencies, transactions). Default: `data.json`.
- `SecurityFilePath` — user accounts and roles (SQLite). Default: `security.db`.

#### MSSQL

Uses EF Core + SQL Server for both domain data and user accounts.

```json
{
  "DataAccess": {
    "Mode": "Mssql"
  },
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=Pricer;Trusted_Connection=True;TrustServerCertificate=True",
    "SecurityConnection": "Server=localhost;Database=PricerSecurity;Trusted_Connection=True;TrustServerCertificate=True"
  }
}
```

- `DefaultConnection` is used for domain data.
- `SecurityConnection` is used for user/auth data (tables in the `security.` schema). Falls back to `DefaultConnection` if omitted — both databases can share the same SQL Server instance.
- Both sets of migrations are applied automatically on startup.

### Environment variables

Use `__` to represent `:` in nested keys (no prefix required):

```
DataAccess__Mode=Mssql
ConnectionStrings__DefaultConnection=Server=...
Jwt__Key=...
```

## First run — onboarding

On first launch (no user accounts exist), the app redirects to `/onboarding` and prompts you to create the administrator account. Registration is only possible at this point; once the first account exists, open registration is permanently disabled.

## User management

Administrators can create additional user accounts from the **Users** page (sidebar). Each new account is created with a temporary password chosen by the administrator. On first login the user is forced to set a new password before accessing the app.

Password reset for existing users is also available from the Users page (generates a new temporary password with the same forced-change behaviour).

## Docker

### Build from source

A production image is built from the repo root using the multi-stage `Dockerfile`:

```pwsh
docker build -t spooly -f Spooly.WebApp/Spooly.WebApp.Server/Dockerfile .
```

[`build-compose.yaml`](build-compose.yaml) builds the image and starts it together with a local SQL Server container — useful for testing the production image locally without pushing to a registry:

```sh
docker compose -f build-compose.yaml up --build
```

### Pre-built image (GitHub Container Registry)

Every push to `master` publishes `ghcr.io/p-liubchenko/pl.spooly:latest`. Tagged releases also publish `:<version>` (e.g. `:1.2.3`) and `:<major>.<minor>` (e.g. `:1.2`).

See the [README](README.md#deployment-options) for ready-to-use compose and Kubernetes manifests:

| File | Mode | Notes |
|------|------|-------|
| [`local-public-compose.yaml`](local-public-compose.yaml) | File | Personal / homelab, zero database |
| [`public-compose.yaml`](public-compose.yaml) | MSSQL | Production, includes SQL Server container |
| [`example-k8s.yaml`](example-k8s.yaml) | MSSQL | Kubernetes — Secret, ConfigMap, Deployment, Service, Ingress |
| [`build-compose.yaml`](build-compose.yaml) | MSSQL | Builds image from source + SQL Server (dev/testing) |

## CI

The `webapp.yml` GitHub Actions workflow runs on every push and pull request to `master`:

- **Build & Test** — installs Node 22, builds Angular, then restores/builds/tests the .NET backend.
- **Docker** (push to `master` or tags only) — builds and pushes the image to GHCR with the tags described above.

See [`.github/workflows/webapp.yml`](.github/workflows/webapp.yml).
