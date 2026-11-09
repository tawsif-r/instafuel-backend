# Instafuel – Customer app

Flutter app for customers: register / log in, register vehicles, order fuel for one or more vehicles, pay, and track deliveries.

> **Everything talks to the real API** (`Instafuel.Api`, JWT): auth, vehicles, orders, payments
> and corporate accounts. Run with `--dart-define=MOCK_API=true` for a fully offline demo.
> Full setup steps are in the [root README](../README.md).

## Run

Start Postgres and the API first (from the repo root):

```bash
docker compose up -d db
cd Instafuel.Api && dotnet ef database update && dotnet run --launch-profile http   # http://localhost:5033
```

Then the app:

```bash
flutter pub get
flutter run -d chrome          # web, calls http://localhost:5033
flutter run                    # Android emulator, calls http://10.0.2.2:5033 (the host's localhost)
flutter test                   # widget + API client tests (offline)
flutter analyze
```

| `--dart-define` | Use |
|---|---|
| `API_BASE_URL=http://192.168.0.10:5033` | Other API address, e.g. a physical phone on Wi-Fi. Start the API with `dotnet run --urls http://0.0.0.0:5033` so the phone can reach it. |
| `MOCK_API=true` | Offline: in-memory auth, vehicles, orders and payments with the demo account `demo@instafuel.app` / `password123` (shown on the login screen) and sample orders. |

Android debug builds allow plain `http` (`android/app/src/debug/AndroidManifest.xml`); release builds need an `https` API.

## Folder layout

```
lib/
├── main.dart            loads SharedPreferences, starts ProviderScope
├── app.dart             MaterialApp.router + light/dark themes
├── core/                theme, router, formatters, shared widgets
├── models/              plain data classes + status enums (names match the API)
├── data/
│   ├── repositories.dart   interfaces the UI talks to
│   ├── providers.dart      picks the implementation for each repository  <-- swap point
│   ├── session_store.dart  saved login (JWT + user) in flutter_secure_storage
│   ├── api/                ApiClient (http + bearer token + error messages), ApiAuth/Vehicle/Order/Payment/CorporateRepository
│   └── mock/               in-memory implementations + seed data
└── features/            one folder per area: auth, dashboard, orders, payment,
                         vehicles, corporate, settings (screens + Riverpod controllers)
```

- State: [Riverpod](https://riverpod.dev). Each feature has a small controller (`*_controller.dart`) that calls a repository.
- Navigation: `go_router`. Logged-out users are redirected to `/login`. The 4 tabs use a `StatefulShellRoute`; flows (new order, payment, vehicle form) open full screen on top.
- Theme: Material 3 from one seed colour. System / Light / Dark is chosen in Account and saved.

## Auth

- `ApiAuthRepository` calls `POST /api/auth/register` and `POST /api/auth/login`; both return `{ token, expiresAt, user }` (`AuthSession`).
- The token is kept on the shared `ApiClient` and sent as `Authorization: Bearer ...` on every call.
- `AuthController` saves the session with `SessionStore` (Keystore-backed on Android). `main()` reads it before `runApp`; an expired session means logged out.
- API errors (`{ message }` or validation `errors`) and network failures become `AppException`s with a readable message, shown on the form.
- A 401 on any call with a token (expired/invalid) logs the user out (`ApiClient.onUnauthorized`).
- While `MOCK_API=true`, `AuthController` tells the mock database which user is logged in so mock data is scoped per user.

## Connecting more of the API

Done — auth, vehicles, orders, payments and corporate all use the API. New features follow the
same pattern:

1. Write an `Api*Repository implements <Interface>` in `lib/data/api/`, using `ApiClient` (see `ApiOrderRepository` for the pattern).
2. Switch the provider in `lib/data/providers.dart`.

Screens and controllers don't change.

## Placeholders (not wired yet)

- Vehicle documents (registration PDF, fitness PDF, number plate photo): "Attach" only fills a fake file name, which the API stores. Add `file_picker` / `image_picker` + upload when storage is ready.
- Vehicle verification: on the dev API, a vehicle with all 3 documents is verified straight away (`CustomerVehicles:AutoVerifyWhenComplete`). Otherwise it stays pending until an admin reviews it.
- Card / mobile banking: will redirect to a payment gateway. The dev API (`Payments:SimulateGateway`) and the mock mark them paid straight away. Corporate credit is real: it requires an active company membership with available credit (`api/me/corporate`).
- Notifications, call driver, edit profile, saved addresses, password reset: show "coming soon".
- Demo shortcuts: company codes starting with `DEMO` (or `NORTHWIND`) are approved instantly with a 25,000 BDT credit line.
