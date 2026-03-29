<div align="center">
  <img src="Spooly.WebApp/spooly.webapp.client/src/logo.svg" alt="Spooly" width="80" height="80"/>

  # Spooly

  **Know exactly what every print costs.**

  Spooly tracks your filament inventory, printer overhead, and electricity use so you always have an accurate cost breakdown for every job — no spreadsheets needed.

  [Get started with Docker](#quick-start) · [Developer docs](DEVELOPER.md) · [Web app docs](WEB-DEVELOPER.md)
</div>

---

## What Spooly does

3D printing costs add up across filament, electricity, and printer wear — and they're surprisingly hard to track. Spooly keeps everything in one place:

- **Log a print** in seconds: enter the duration and filament used, get a full cost breakdown instantly.
- **Manage your filament warehouse**: add spools, restock, and watch stock levels update automatically as you print.
- **Account for every expense**: electricity draw, printer hourly overhead, and material cost are calculated together.
- **Multi-currency support**: set a base currency and operating currency with custom exchange rates.
- **See the big picture**: the dashboard shows filament flow and stock levels over the last 30 days with live charts.

---

## Features at a glance

| Area | What you can do |
|------|----------------|
| **Dashboard** | Stats overview, 30-day filament flow chart (candlestick), 30-day stock level chart (line) |
| **Printers** | Add printers with wattage and hourly overhead rate; select the active printer |
| **Materials** | Add spools by filament type, restock, track consumption per print |
| **Transactions** | Record prints (auto-deducts stock), view cost breakdown, revert or delete |
| **Quick Record** | One-click floating button to log a print from anywhere in the app |
| **Currencies** | Define currencies with exchange rates; set base and operating currency |
| **Users & Roles** | Role-based access control; administrator-managed accounts with forced password change on first login |
| **Settings** | Select active printer, operating currency, and other defaults |

---

## Quick start

The easiest way to run Spooly is with Docker Compose. Copy the file below, set a JWT key, and you're up.

**File mode — zero database setup** ([`local-public-compose.yaml`](local-public-compose.yaml))

```sh
# edit Jwt__Key in the file first, then:
docker compose -f local-public-compose.yaml up -d
```

Data is stored in a `./spooly-data/` folder next to the compose file. No database required.

Then open [http://localhost:8080](http://localhost:8080).

**First launch:** the app redirects you to `/onboarding` to create your administrator account. Once created, open registration is permanently disabled — additional users are created by the administrator from the **Users** page.

---

## Setting up your workspace

After logging in, a one-time setup takes about two minutes:

1. **Add a printer** — go to **Printers**, click *Add*, enter the name, average power draw (watts), and hourly overhead rate.
2. **Select it as active** — click *Select* on the printer row.
3. **Add a filament spool** — go to **Materials**, click *Add spool*, choose the type (PLA, PETG, ABS, …), enter the total weight and purchase price.
4. **Set your currency** — go to **Currencies**, add currencies if needed, then go to **Settings** to choose your operating currency.

You're ready to record prints.

---

## Recording a print

Click the **+** button in the bottom-right corner (or go to **Transactions → Record print**).

Enter:
- **Duration** (hours and minutes)
- **Filament used** (grams — or use the estimator to convert from print weight)
- **Material** (which spool to deduct from)

Spooly calculates:
- Material cost (filament price × grams used)
- Electricity cost (wattage × duration × rate)
- Printer wear (hourly overhead × duration)
- **Total cost**

The transaction is saved, filament stock is deducted, and the dashboard charts update.

---

## Deployment options

Three ready-to-use configurations are included. Edit the JWT key (and SQL password where relevant) before deploying.

### File mode — personal / homelab

[`local-public-compose.yaml`](local-public-compose.yaml) — pulls the pre-built image, stores everything in `./spooly-data/`. No database needed.

```sh
docker compose -f local-public-compose.yaml up -d
```

### SQL Server — production

[`public-compose.yaml`](public-compose.yaml) — pulls the pre-built image, spins up an MSSQL container alongside it, and persists data in a named volume. Suitable for always-on installs.

```sh
docker compose -f public-compose.yaml up -d
```

### Kubernetes

[`example-k8s.yaml`](example-k8s.yaml) — a full example manifest: `Secret`, `ConfigMap`, `Deployment`, `Service`, and `Ingress` (nginx). Assumes SQL Server is provisioned separately.

```sh
# 1. Create the image pull secret (if the registry is private)
kubectl create secret docker-registry ghcr-pull-secret \
  --namespace tools \
  --docker-server=ghcr.io \
  --docker-username=<GH_USERNAME> \
  --docker-password=<GH_PAT>

# 2. Fill in connection strings and JWT key in example-k8s.yaml, then apply
kubectl apply -f example-k8s.yaml
```

### Data storage

Spooly supports two backends and migrates automatically when you switch.

| Mode | Best for |
|------|----------|
| **File** (default) | Personal use, zero setup — data lives in `data.json` + `security.db` |
| **SQL Server** | Teams and production installs — set `DataAccess__Mode=Mssql` and a connection string |

---

## Running without Docker

Requires .NET 10 SDK and Node.js 22. See [WEB-DEVELOPER.md](WEB-DEVELOPER.md) for full instructions.

---

## License

AGPL-3.0

See [LICENSE](LICENSE) for the full text.
