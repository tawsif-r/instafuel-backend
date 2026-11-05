# Instafuel

**Fuel On-Demand (working title)**: a platform that delivers vehicle and household fuel to the customer's location. It connects customers with local suppliers and our own delivery fleet, with strict safety checks along the way. Examples: a car that ran dry, a fleet that needs refuelling, a generator at home.

This repository holds:

| Folder | What it is |
|---|---|
| [`Instafuel.Api/`](Instafuel.Api) | ASP.NET Core Web API (.NET 10), EF Core + PostgreSQL, JWT auth |
| [`instafuel_customer/`](instafuel_customer) | Flutter customer app (Android + Web) |
| [`docs/`](docs) | [Product plan](docs/plan.md), [database schema](docs/database_schema.md), [architecture + API reference](docs/architecture.md) |
| `docker-compose.yml` | PostgreSQL for local development |

---

## 1. Prerequisites

| Tool | Version | Check with |
|---|---|---|
| Git | any | `git --version` |
| Docker + Docker Compose | Docker 24+ | `docker compose version` |
| .NET SDK | **10.0** | `dotnet --version` |
| EF Core CLI (`dotnet-ef`) | 10.x | `dotnet ef --version` |
| Flutter SDK | 3.44+ (Dart 3.12+) | `flutter --version` |
| Google Chrome or Chromium | any | for the web app |
| Android Studio + an emulator | optional | only to run the app on Android |

### Arch Linux

```bash
sudo pacman -S --needed git docker docker-compose dotnet-sdk aspnet-runtime chromium
sudo systemctl enable --now docker
sudo usermod -aG docker $USER        # then log out and back in so you can run docker without sudo

dotnet tool install --global dotnet-ef
echo 'export PATH="$PATH:$HOME/.dotnet/tools"' >> ~/.bashrc && source ~/.bashrc
```

Flutter: install `flutter-bin` from the AUR (for example `yay -S flutter-bin`), or follow the manual install below.

### Ubuntu / Debian

```bash
sudo apt update && sudo apt install -y git curl unzip xz-utils zip libglu1-mesa
# Docker: https://docs.docker.com/engine/install/ubuntu/
# .NET 10 SDK: https://learn.microsoft.com/dotnet/core/install/linux-ubuntu
dotnet tool install --global dotnet-ef
echo 'export PATH="$PATH:$HOME/.dotnet/tools"' >> ~/.bashrc && source ~/.bashrc
```

### macOS / Windows

- Docker Desktop: https://www.docker.com/products/docker-desktop/
- .NET 10 SDK: https://dotnet.microsoft.com/download/dotnet/10.0
- Then run `dotnet tool install --global dotnet-ef`.

### Flutter (any OS)

Follow https://docs.flutter.dev/get-started/install, or do a manual install on Linux:

```bash
git clone https://github.com/flutter/flutter.git -b stable ~/flutter
echo 'export PATH="$PATH:$HOME/flutter/bin"' >> ~/.bashrc && source ~/.bashrc
flutter doctor          # fix anything it reports for the platforms you need (Chrome / Android)
```

If Flutter can't find Chromium, point it at the browser: `export CHROME_EXECUTABLE=$(which chromium)`.

---

## 2. Get the code

```bash
git clone <repo-url> instafuel
cd instafuel
```

---

## 3. Start the database

PostgreSQL runs in Docker. The user, password and database name are set in `docker-compose.yml` (`tawsif` / `pass` / `instafuel`). The API's connection string in `Instafuel.Api/appsettings.json` uses the same values.

```bash
docker compose up -d db
docker compose ps                    # the db service should be "running" on port 5432
```

> Port 5432 already in use? Stop your local Postgres (`sudo systemctl stop postgresql`) or change the port mapping in `docker-compose.yml` **and** the connection string.

---

## 4. Run the API

```bash
cd Instafuel.Api
dotnet restore                       # download NuGet packages
dotnet ef database update            # create / upgrade the tables
dotnet run --launch-profile http     # http://localhost:5033
```

- Swagger UI: http://localhost:5033/swagger
- Use `dotnet watch --launch-profile http` instead to rebuild on every code change.
- [`Instafuel.Api.http`](Instafuel.Api/Instafuel.Api.http) has sample requests you can run from VS Code / Rider.

### What Development mode gives you

`Instafuel.Api/appsettings.Development.json` is loaded automatically by `dotnet run`. It has:

| Setting | Effect |
|---|---|
| `Jwt:Key` | Local signing key for login tokens. **Never use it in production**: set `Jwt__Key` as an environment variable there instead. |
| `CustomerVehicles:AutoVerifyWhenComplete` | A vehicle with all 3 documents is verified straight away, so you can order without an admin. |
| `Payments:SimulateGateway` | Card / mobile banking payments succeed right away (there's no payment gateway yet). |
| `Seed:DevFuelCatalog` | On startup, adds a "Fuel" category, Octane / Petrol / Diesel products and an "Instafuel Depot" supplier with prices, if they don't exist yet. |

Order rules (24h lead time, delivery fee) are under `Orders` in `appsettings.json`.

### Making an admin user

Register in the app (or with `POST /api/auth/register`), then:

```bash
docker compose exec db psql -U tawsif -d instafuel \
  -c "UPDATE \"Users\" SET \"Role\" = 'admin' WHERE \"Email\" = 'you@example.com';"
```

Log in again so the new role is in your token.

---

## 5. Run the customer app

In a second terminal, with the API still running:

```bash
cd instafuel_customer
flutter pub get                      # download Dart packages
flutter run -d chrome --web-port 8080   # web app on http://localhost:8080, calls the API on :5033
```

Other targets:

```bash
flutter devices                      # list what you can run on
flutter run -d <emulator-id>         # Android emulator, which reaches the API at http://10.0.2.2:5033
```

| `--dart-define` | When to use it |
|---|---|
| `API_BASE_URL=http://192.168.0.10:5033` | A physical phone on the same Wi-Fi. Start the API with `dotnet run --launch-profile http --urls http://0.0.0.0:5033` so the phone can reach it. |
| `MOCK_API=true` | Fully offline: no API or database needed. Log in with the demo account `demo@instafuel.app` / `password123` to see sample data. |

Example: `flutter run -d chrome --dart-define=MOCK_API=true`

More detail on the app's structure: [`instafuel_customer/README.md`](instafuel_customer/README.md).

---

## 6. Try it end to end

1. Open the app and **register** a new account.
2. **Vehicles** → add a vehicle and attach all 3 documents. It's verified straight away in Development.
3. **New order** → pick the vehicle(s), litres, address and a delivery time at least 24 hours away → confirm.
4. **Pay** → choose Card (paid at once) or Cash on delivery (stays pending).
5. The order shows on the dashboard and in **Orders**. You can cancel it until a driver is assigned.

---

## 7. Tests and checks

```bash
# API
cd Instafuel.Api && dotnet build

# App
cd instafuel_customer
flutter analyze
flutter test                         # runs offline, no API needed
```

---

## 8. Common tasks

| Task | Command (run from `Instafuel.Api/`) |
|---|---|
| Add a migration after changing models | `dotnet ef migrations add <Name>`, then review the file in `Migrations/` |
| Apply migrations | `dotnet ef database update` |
| Undo the last migration (if not applied) | `dotnet ef migrations remove` |
| Reset the database completely | `docker compose down -v && docker compose up -d db && dotnet ef database update` (**deletes all data**) |
| See API error logs | `Instafuel.Api/logs/app-error-<date>.log` |

---

## 9. Troubleshooting

| Problem | Fix |
|---|---|
| `dotnet ef: command not found` | `dotnet tool install --global dotnet-ef` and add `~/.dotnet/tools` to `PATH`. |
| API fails to start: `Failed to connect to 127.0.0.1:5432` | The database isn't running: `docker compose up -d db`. |
| `permission denied ... docker.sock` | Add yourself to the `docker` group (see Arch setup) and log in again. |
| App shows "Can't reach the server" | The API isn't running on port 5033, or (on a phone) use `API_BASE_URL` with your PC's IP address. |
| `Delivery must be at least 24 hours from now.` | Expected: pick a later delivery time. |
| Web app can't find Chrome | `export CHROME_EXECUTABLE=$(which chromium)` (or the path to `google-chrome`). |

---

### 10. Default branch
`checkpoint` branch is the new default branch. use `git checkout -b <feature>` or `switch` from this branch to start working on this project. 

**refrain** from `save-point-0` branch push.

## License

See [LICENSE](LICENSE).
