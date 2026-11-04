// All routes in one place.
//
// - Logged-out users only see /login and /register (see `redirect`).
// - The 4 bottom-nav tabs live in a StatefulShellRoute so each tab keeps its
//   own scroll position/stack.
// - Detail/flow screens (new order, payment, vehicle form...) are top-level
//   routes, so they open full screen above the bottom bar.

import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import '../features/auth/auth_controller.dart';
import '../features/auth/login_screen.dart';
import '../features/auth/register_screen.dart';
import '../features/corporate/corporate_screen.dart';
import '../features/dashboard/dashboard_screen.dart';
import '../features/orders/new_order_screen.dart';
import '../features/orders/order_detail_screen.dart';
import '../features/orders/orders_screen.dart';
import '../features/payment/payment_result_screen.dart';
import '../features/payment/payment_screen.dart';
import '../features/settings/settings_screen.dart';
import '../features/vehicles/vehicle_form_screen.dart';
import '../features/vehicles/vehicles_screen.dart';
import '../models/payment.dart';
import 'widgets/app_shell.dart';

final routerProvider = Provider<GoRouter>((ref) {
  // Re-run `redirect` whenever the user logs in or out.
  final authChanges = ValueNotifier(0);
  ref.listen(authControllerProvider, (_, _) => authChanges.value++);
  ref.onDispose(authChanges.dispose);

  return GoRouter(
    initialLocation: '/home',
    refreshListenable: authChanges,
    redirect: (context, state) {
      final loggedIn = ref.read(authControllerProvider) != null;
      final onAuthPage = state.matchedLocation == '/login' || state.matchedLocation == '/register';
      if (!loggedIn && !onAuthPage) return '/login';
      if (loggedIn && onAuthPage) return '/home';
      return null;
    },
    routes: [
      GoRoute(path: '/login', builder: (_, _) => const LoginScreen()),
      GoRoute(path: '/register', builder: (_, _) => const RegisterScreen()),

      StatefulShellRoute.indexedStack(
        builder: (_, _, shell) => AppShell(shell: shell),
        branches: [
          StatefulShellBranch(routes: [GoRoute(path: '/home', builder: (_, _) => const DashboardScreen())]),
          StatefulShellBranch(routes: [GoRoute(path: '/orders', builder: (_, _) => const OrdersScreen())]),
          StatefulShellBranch(routes: [GoRoute(path: '/vehicles', builder: (_, _) => const VehiclesScreen())]),
          StatefulShellBranch(routes: [GoRoute(path: '/account', builder: (_, _) => const SettingsScreen())]),
        ],
      ),

      GoRoute(path: '/new-order', builder: (_, _) => const NewOrderScreen()),
      GoRoute(
        path: '/order/:id',
        builder: (_, state) => OrderDetailScreen(orderId: state.pathParameters['id']!),
      ),
      GoRoute(
        path: '/order/:id/pay',
        builder: (_, state) => PaymentScreen(orderId: state.pathParameters['id']!),
      ),
      GoRoute(
        path: '/order/:id/payment-result',
        builder: (_, state) => PaymentResultScreen(result: state.extra! as PaymentResult),
      ),
      GoRoute(path: '/vehicle/new', builder: (_, _) => const VehicleFormScreen()),
      GoRoute(
        path: '/vehicle/:id',
        builder: (_, state) => VehicleFormScreen(vehicleId: state.pathParameters['id']),
      ),
      GoRoute(path: '/corporate', builder: (_, _) => const CorporateScreen()),
    ],
  );
});
