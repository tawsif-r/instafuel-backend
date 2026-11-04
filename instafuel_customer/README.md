# Instafuel – Customer app

Flutter app for customers: register / log in, register vehicles, order fuel for one or more vehicles, pay, and track deliveries.

> **Preview build.** All data is mock data kept in memory (resets on restart). No backend calls are made yet.
> Demo login: `demo@instafuel.app` / `password123`

## Run

```bash
flutter pub get
flutter run -d chrome          # web
flutter run                    # Android device / emulator
flutter test                   # widget tests
flutter analyze
```

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
│   └── mock/               in-memory implementations + seed data
└── features/            one folder per area: auth, dashboard, orders, payment,
                         vehicles, corporate, settings (screens + Riverpod controllers)
```

- State: [Riverpod](https://riverpod.dev). Each feature has a small controller (`*_controller.dart`) that calls a repository.
- Navigation: `go_router`. Logged-out users are redirected to `/login`. The 4 tabs use a `StatefulShellRoute`; flows (new order, payment, vehicle form) open full screen on top.
- Theme: Material 3 from one seed colour. System / Light / Dark is chosen in Account and saved.

## Connecting the real API later

1. Add an HTTP client package (e.g. `dio`).
2. Write `ApiOrderRepository implements OrderRepository` (same for the others) in `lib/data/api/`.
3. Change the provider in `lib/data/providers.dart`:
   ```dart
   final orderRepositoryProvider = Provider<OrderRepository>((ref) => ApiOrderRepository(ref.watch(dioProvider)));
   ```
Screens and controllers don't change.

## Placeholders (not wired yet)

- Vehicle documents (registration PDF, fitness PDF, number plate photo): "Attach" only fills a fake file name. Add `file_picker` / `image_picker` + upload when storage is ready.
- Card / mobile banking: will redirect to a payment gateway. The mock returns success.
- Notifications, call driver, edit profile, saved addresses, password reset: show "coming soon".
- Demo shortcuts: vehicles with all 3 documents are verified instantly; company codes starting with `DEMO` are approved instantly.
