# Pricer

CLI tool for managing 3D-printing costs. A browser-based web UI is also available — see [web-README.md](web-README.md). Track filament spools (purchase, restock, consumption), printers with hourly overhead and power draw, currencies with exchange rates, and print transactions. Calculates material cost, electricity cost, and printer wear for any print job.

Supports two data backends — a local JSON file (zero setup) and SQL Server — and migrates automatically when you switch between them.

## Prerequisites

- .NET SDK `10.0`

## Run

```pwsh
cd .\Pricer.Cli
dotnet run
```

## Configuration

The CLI loads configuration from (in order):

1. `Pricer.Cli\appsettings.json`
2. `Pricer.Cli\appsettings.Development.json` (optional)
3. Environment variables with prefix `PRICER_`
4. User Secrets (optional)
5. Command-line arguments

### Data access mode

Data access is configured via the `DataAccess` section.

#### File (default)

Stores data in a local JSON file.

`Pricer.Cli\appsettings.json`:

```json
{
  "DataAccess": {
    "Mode": "File"
  }
}
```

When running the CLI from `Pricer.Cli`, the data file is `Pricer.Cli\data.json`.

#### MSSQL

Uses EF Core + SQL Server.

`Pricer.Cli\appsettings.json`:

```json
{
  "DataAccess": {
    "Mode": "Mssql"
  },
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=Pricer;Trusted_Connection=True;TrustServerCertificate=True"
  }
}
```

Notes:

- Connection string key must be `ConnectionStrings:DefaultConnection`.
- When `Mode` is `Mssql`, the CLI applies any pending EF migrations automatically on startup.

##### Using User Secrets (recommended for local dev)

```pwsh
cd .\Pricer.Cli

dotnet user-secrets init

dotnet user-secrets set "DataAccess:Mode" "Mssql"
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=localhost;Database=Pricer;Trusted_Connection=True;TrustServerCertificate=True"
```

### Environment variables

The CLI uses the prefix `PRICER_`. Use `__` to represent `:` in nested keys:

- `PRICER_DataAccess__Mode=Mssql`
- `PRICER_ConnectionStrings__DefaultConnection=...`

## Migrating between data providers

The CLI detects a backend switch on startup and migrates automatically. No manual steps are needed.

### File → SQL Server

1. Switch `DataAccess:Mode` to `Mssql` and set `ConnectionStrings:DefaultConnection`.
2. Start the CLI.

On startup the CLI merges all data from `data.json` into the database (entities already present by ID are skipped, settings are merged field-by-field with the database taking priority), then archives the file as `data.json.bak`.

### SQL Server → File

1. Switch `DataAccess:Mode` to `File` (keep the connection string in config or remove it later).
2. Delete or empty `data.json` if it exists.
3. Start the CLI.

If the file is empty or missing and a `ConnectionStrings:DefaultConnection` value is configured, the CLI pulls all data from the database into `data.json`. If the database cannot be reached the CLI starts normally with an empty file and logs a warning.
