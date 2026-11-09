// Production-grade FuelBuddy / CAFU / Booster style on-demand fuel customer dashboard.
// Features: Interactive location radar, live bowser fleet status, quick fuel selector
// with instant litre presets & price calculator, active delivery live tracking card,
// garage vehicle tank cards, value-added service shortcuts, and trust badges.

import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import '../../core/formatters.dart';
import '../../core/theme.dart';
import '../../core/widgets/delivery_progress.dart';
import '../../core/widgets/license_plate.dart';
import '../../core/widgets/responsive_center.dart';
import '../../core/widgets/status_chip.dart';
import '../../models/fuel.dart';
import '../../models/order.dart';
import '../../models/user.dart';
import '../../models/vehicle.dart';
import '../auth/auth_controller.dart';
import '../orders/order_tile.dart';
import '../orders/orders_controller.dart';
import '../vehicles/vehicles_controller.dart';

class DashboardScreen extends ConsumerStatefulWidget {
  const DashboardScreen({super.key});

  @override
  ConsumerState<DashboardScreen> createState() => _DashboardScreenState();
}

class _DashboardScreenState extends ConsumerState<DashboardScreen> {
  FuelType _selectedFuel = FuelType.octane;
  int _selectedLitres = 30;

  @override
  Widget build(BuildContext context) {
    final user = ref.watch(authControllerProvider);
    final orders = ref.watch(ordersProvider).value ?? [];
    final vehicles = ref.watch(vehiclesProvider).value ?? [];
    final prices = ref.watch(fuelProductsProvider).value ?? [];
    final loading = ref.watch(ordersProvider).isLoading && orders.isEmpty;

    final active = orders.where((o) => o.isActive).toList()
      ..sort((a, b) => a.scheduledAt.compareTo(b.scheduledAt));
    final tracked = active.where((o) => o.deliveryStatus != DeliveryStatus.pending).firstOrNull ?? active.firstOrNull;
    final theme = Theme.of(context);
    final isDark = theme.brightness == Brightness.dark;

    return Scaffold(
      body: SafeArea(
        child: RefreshIndicator(
          onRefresh: () {
            ref.invalidate(vehiclesProvider);
            return ref.refresh(ordersProvider.future);
          },
          child: CustomScrollView(
            slivers: [
              // Top Location & Profile Header (FuelBuddy style)
              SliverToBoxAdapter(
                child: Padding(
                  padding: const EdgeInsets.fromLTRB(AppTheme.gap16, AppTheme.gap12, AppTheme.gap16, AppTheme.gap8),
                  child: Row(
                    children: [
                      // Location Selector Chip
                      Expanded(
                        child: InkWell(
                          onTap: () => ScaffoldMessenger.of(context).showSnackBar(
                            const SnackBar(content: Text('Delivery zone: Dhaka Metro (Central & North).')),
                          ),
                          borderRadius: BorderRadius.circular(30),
                          child: Container(
                            padding: const EdgeInsets.symmetric(horizontal: 14, vertical: 8),
                            decoration: BoxDecoration(
                              color: isDark ? const Color(0xFF1E293B) : Colors.white,
                              borderRadius: BorderRadius.circular(30),
                              border: Border.all(
                                color: isDark ? const Color(0xFF334155) : const Color(0xFFE2E8F0),
                              ),
                              boxShadow: [
                                BoxShadow(
                                  color: Colors.black.withValues(alpha: 0.04),
                                  blurRadius: 8,
                                  offset: const Offset(0, 2),
                                ),
                              ],
                            ),
                            child: Row(
                              children: [
                                Container(
                                  width: 28,
                                  height: 28,
                                  decoration: BoxDecoration(
                                    color: const Color(0xFF059669).withValues(alpha: 0.15),
                                    shape: BoxShape.circle,
                                  ),
                                  child: const Icon(Icons.location_on_rounded, color: Color(0xFF059669), size: 16),
                                ),
                                const SizedBox(width: 8),
                                Expanded(
                                  child: Column(
                                    crossAxisAlignment: CrossAxisAlignment.start,
                                    mainAxisSize: MainAxisSize.min,
                                    children: [
                                      Row(
                                        children: [
                                          const Text(
                                            'DELIVER TO',
                                            style: TextStyle(
                                              fontSize: 9,
                                              fontWeight: FontWeight.w800,
                                              color: Color(0xFF059669),
                                              letterSpacing: 0.6,
                                            ),
                                          ),
                                          const SizedBox(width: 4),
                                          Container(
                                            width: 6,
                                            height: 6,
                                            decoration: const BoxDecoration(
                                              shape: BoxShape.circle,
                                              color: Color(0xFF10B981),
                                            ),
                                          ),
                                        ],
                                      ),
                                      const Text(
                                        'Dhanmondi, Road 5 · Dhaka',
                                        style: TextStyle(fontSize: 13, fontWeight: FontWeight.bold),
                                        maxLines: 1,
                                        overflow: TextOverflow.ellipsis,
                                      ),
                                    ],
                                  ),
                                ),
                                const Icon(Icons.keyboard_arrow_down_rounded, size: 18, color: Colors.grey),
                              ],
                            ),
                          ),
                        ),
                      ),
                      const SizedBox(width: AppTheme.gap12),
                      // Notifications & Profile avatar
                      IconButton.filledTonal(
                        tooltip: 'Notifications',
                        style: IconButton.styleFrom(
                          backgroundColor: isDark ? const Color(0xFF1E293B) : Colors.white,
                          shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(16)),
                        ),
                        icon: const Icon(Icons.notifications_none_rounded, size: 22),
                        onPressed: () => ScaffoldMessenger.of(context).showSnackBar(
                          const SnackBar(content: Text('All delivery systems operational.')),
                        ),
                      ),
                    ],
                  ),
                ),
              ),

              // Main Content
              SliverPadding(
                padding: const EdgeInsets.symmetric(horizontal: AppTheme.gap16, vertical: AppTheme.gap8),
                sliver: SliverToBoxAdapter(
                  child: ResponsiveCenter(
                    child: Column(
                      crossAxisAlignment: CrossAxisAlignment.stretch,
                      children: [
                        // User Welcome & Delivery Status
                        Row(
                          mainAxisAlignment: MainAxisAlignment.spaceBetween,
                          crossAxisAlignment: CrossAxisAlignment.end,
                          children: [
                            Expanded(
                              child: Column(
                                crossAxisAlignment: CrossAxisAlignment.start,
                                children: [
                                  Text(
                                    'Hello, ${user?.firstName ?? 'Customer'}',
                                    style: theme.textTheme.headlineSmall?.copyWith(
                                      fontWeight: FontWeight.w900,
                                      letterSpacing: -0.6,
                                    ),
                                  ),
                                  const SizedBox(height: 2),
                                  Text(
                                    'Instant doorstep fuel delivered by smart bowsers',
                                    style: TextStyle(
                                      fontSize: 13,
                                      color: theme.colorScheme.onSurfaceVariant,
                                    ),
                                  ),
                                ],
                              ),
                            ),
                            Container(
                              padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 4),
                              decoration: BoxDecoration(
                                color: const Color(0xFF10B981).withValues(alpha: 0.12),
                                borderRadius: BorderRadius.circular(20),
                              ),
                              child: const Row(
                                mainAxisSize: MainAxisSize.min,
                                children: [
                                  CircleAvatar(radius: 3, backgroundColor: Color(0xFF10B981)),
                                  SizedBox(width: 5),
                                  Text(
                                    'DEPOT OPEN',
                                    style: TextStyle(
                                      color: Color(0xFF059669),
                                      fontSize: 10,
                                      fontWeight: FontWeight.w800,
                                      letterSpacing: 0.5,
                                    ),
                                  ),
                                ],
                              ),
                            ),
                          ],
                        ),
                        const SizedBox(height: AppTheme.gap16),

                        // Loading Indicator
                        if (loading)
                          const Padding(
                            padding: EdgeInsets.all(AppTheme.gap32),
                            child: Center(child: CircularProgressIndicator()),
                          )
                        // Active Live Tracking Card (if order is in progress)
                        else if (tracked != null) ...[
                          _ActiveBowserLiveTracker(order: tracked),
                          const SizedBox(height: AppTheme.gap20),
                        ],

                        // Interactive Simulated Fleet Radar Banner
                        const _SimulatedDepotRadarBanner(),
                        const SizedBox(height: AppTheme.gap20),

                        // Quick Service Matrix (FuelBuddy / CAFU style)
                        _ServicesGrid(hasCorporate: user?.accountType == AccountType.corporate),
                        const SizedBox(height: AppTheme.gap24),

                        // Fuel Selection & Instant Price Estimator (FuelBuddy style)
                        if (prices.isNotEmpty) ...[
                          _FuelSelectorCard(
                            prices: prices,
                            selectedFuel: _selectedFuel,
                            selectedLitres: _selectedLitres,
                            onFuelChanged: (fuel) => setState(() => _selectedFuel = fuel),
                            onLitresChanged: (litres) => setState(() => _selectedLitres = litres),
                            onOrderPressed: () => context.push('/new-order'),
                          ),
                          const SizedBox(height: AppTheme.gap24),
                        ],

                        // My Garage Vehicles Showcase
                        _GarageVehicleShowcase(vehicles: vehicles),
                        const SizedBox(height: AppTheme.gap24),

                        // InstaFuel 4 Safety Pillars (Trust Badges)
                        const _InstaFuelTrustBadges(),
                        const SizedBox(height: AppTheme.gap24),

                        // Recent Refuel Orders
                        if (orders.isNotEmpty) ...[
                          Row(
                            mainAxisAlignment: MainAxisAlignment.spaceBetween,
                            children: [
                              Text(
                                'Recent Refuels',
                                style: theme.textTheme.titleMedium?.copyWith(
                                  fontWeight: FontWeight.w800,
                                  letterSpacing: -0.2,
                                ),
                              ),
                              TextButton.icon(
                                onPressed: () => context.go('/orders'),
                                icon: const Text('View All History', style: TextStyle(fontWeight: FontWeight.bold)),
                                label: const Icon(Icons.arrow_forward_rounded, size: 16),
                              ),
                            ],
                          ),
                          const SizedBox(height: AppTheme.gap8),
                          for (final o in orders.take(3)) ...[
                            OrderTile(order: o),
                            const SizedBox(height: AppTheme.gap12),
                          ],
                        ],
                        const SizedBox(height: AppTheme.gap32),
                      ],
                    ),
                  ),
                ),
              ),
            ],
          ),
        ),
      ),
    );
  }
}

/// Simulated Depot & Bowser Fleet Radar Banner (CAFU / FuelBuddy signature visual)
class _SimulatedDepotRadarBanner extends StatelessWidget {
  const _SimulatedDepotRadarBanner();

  @override
  Widget build(BuildContext context) {
    return Container(
      decoration: BoxDecoration(
        gradient: const LinearGradient(
          colors: [Color(0xFF0B132B), Color(0xFF1C2541)],
          begin: Alignment.topLeft,
          end: Alignment.bottomRight,
        ),
        borderRadius: BorderRadius.circular(24),
        boxShadow: [
          BoxShadow(
            color: const Color(0xFF0B132B).withValues(alpha: 0.3),
            blurRadius: 18,
            offset: const Offset(0, 8),
          ),
        ],
      ),
      child: ClipRRect(
        borderRadius: BorderRadius.circular(24),
        child: Stack(
          children: [
            // Decorative radar concentric lines
            Positioned(
              right: -40,
              top: -40,
              child: Container(
                width: 180,
                height: 180,
                decoration: BoxDecoration(
                  shape: BoxShape.circle,
                  border: Border.all(color: Colors.white.withValues(alpha: 0.08), width: 1.5),
                ),
              ),
            ),
            Positioned(
              right: -10,
              top: -10,
              child: Container(
                width: 120,
                height: 120,
                decoration: BoxDecoration(
                  shape: BoxShape.circle,
                  border: Border.all(color: const Color(0xFF059669).withValues(alpha: 0.25), width: 1.5),
                ),
              ),
            ),
            Positioned(
              right: 35,
              top: 35,
              child: Container(
                width: 30,
                height: 30,
                decoration: BoxDecoration(
                  shape: BoxShape.circle,
                  color: const Color(0xFF059669).withValues(alpha: 0.2),
                ),
                child: const Icon(Icons.local_shipping_rounded, color: Color(0xFF34D399), size: 16),
              ),
            ),

            Padding(
              padding: const EdgeInsets.all(AppTheme.gap20),
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Row(
                    children: [
                      Container(
                        padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 4),
                        decoration: BoxDecoration(
                          color: const Color(0xFF059669).withValues(alpha: 0.2),
                          borderRadius: BorderRadius.circular(20),
                          border: Border.all(color: const Color(0xFF059669).withValues(alpha: 0.5)),
                        ),
                        child: const Row(
                          mainAxisSize: MainAxisSize.min,
                          children: [
                            Icon(Icons.radar_rounded, color: Color(0xFF34D399), size: 13),
                            SizedBox(width: 5),
                            Text(
                              'SMART BOWSER RADAR',
                              style: TextStyle(
                                color: Color(0xFF34D399),
                                fontSize: 10,
                                fontWeight: FontWeight.w800,
                                letterSpacing: 0.8,
                              ),
                            ),
                          ],
                        ),
                      ),
                      const Spacer(),
                      const Text(
                        '3 Bowsers Nearby',
                        style: TextStyle(color: Color(0xFF10B981), fontSize: 12, fontWeight: FontWeight.bold),
                      ),
                    ],
                  ),
                  const SizedBox(height: AppTheme.gap12),
                  const Text(
                    'Doorstep Fueling\nAt Pump Prices',
                    style: TextStyle(
                      color: Colors.white,
                      fontSize: 22,
                      fontWeight: FontWeight.w900,
                      height: 1.2,
                      letterSpacing: -0.5,
                    ),
                  ),
                  const SizedBox(height: 6),
                  Text(
                    'Digital calibrated flow meters, automated receipt & tamper-proof seals.',
                    style: TextStyle(color: Colors.white.withValues(alpha: 0.75), fontSize: 12.5),
                  ),
                  const SizedBox(height: AppTheme.gap16),
                  Row(
                    children: [
                      ElevatedButton.icon(
                        onPressed: () => context.push('/new-order'),
                        style: ElevatedButton.styleFrom(
                          backgroundColor: const Color(0xFF059669),
                          foregroundColor: Colors.white,
                          elevation: 3,
                          padding: const EdgeInsets.symmetric(horizontal: 20, vertical: 12),
                          shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(14)),
                        ),
                        icon: const Icon(Icons.electric_bolt_rounded, size: 18),
                        label: const Text(
                          'Schedule Refuel',
                          style: TextStyle(fontWeight: FontWeight.w800, fontSize: 13.5),
                        ),
                      ),
                      const SizedBox(width: AppTheme.gap12),
                      OutlinedButton.icon(
                        onPressed: () => context.push('/corporate'),
                        style: OutlinedButton.styleFrom(
                          foregroundColor: Colors.white,
                          side: BorderSide(color: Colors.white.withValues(alpha: 0.3)),
                          padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 12),
                          shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(14)),
                        ),
                        icon: const Icon(Icons.business_center_outlined, size: 16),
                        label: const Text('Fleet Credit', style: TextStyle(fontWeight: FontWeight.w700, fontSize: 13)),
                      ),
                    ],
                  ),
                ],
              ),
            ),
          ],
        ),
      ),
    );
  }
}

/// Services Grid (Refuel, Garage, Corporate Fleet, Bulk / Generator)
class _ServicesGrid extends StatelessWidget {
  const _ServicesGrid({required this.hasCorporate});

  final bool hasCorporate;

  @override
  Widget build(BuildContext context) {
    return Row(
      children: [
        Expanded(
          child: _ServiceCard(
            title: 'Order Fuel',
            subtitle: 'Cars & Bikes',
            icon: Icons.local_gas_station_rounded,
            color: const Color(0xFF059669),
            onTap: () => context.push('/new-order'),
          ),
        ),
        const SizedBox(width: AppTheme.gap12),
        Expanded(
          child: _ServiceCard(
            title: 'Generator',
            subtitle: 'Commercial Diesel',
            icon: Icons.electric_meter_rounded,
            color: const Color(0xFF0284C7),
            onTap: () => context.push('/new-order'),
          ),
        ),
        const SizedBox(width: AppTheme.gap12),
        Expanded(
          child: _ServiceCard(
            title: 'Corporate',
            subtitle: hasCorporate ? 'Credit Active' : 'Apply Credit',
            icon: Icons.corporate_fare_rounded,
            color: const Color(0xFF059669),
            onTap: () => context.push('/corporate'),
          ),
        ),
      ],
    );
  }
}

class _ServiceCard extends StatelessWidget {
  const _ServiceCard({
    required this.title,
    required this.subtitle,
    required this.icon,
    required this.color,
    required this.onTap,
  });

  final String title;
  final String subtitle;
  final IconData icon;
  final Color color;
  final VoidCallback onTap;

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final isDark = theme.brightness == Brightness.dark;

    return InkWell(
      onTap: onTap,
      borderRadius: BorderRadius.circular(18),
      child: Container(
        padding: const EdgeInsets.symmetric(horizontal: 12, vertical: 14),
        decoration: BoxDecoration(
          color: isDark ? const Color(0xFF1E293B) : Colors.white,
          borderRadius: BorderRadius.circular(18),
          border: Border.all(
            color: isDark ? const Color(0xFF334155) : const Color(0xFFE2E8F0),
          ),
          boxShadow: [
            BoxShadow(
              color: Colors.black.withValues(alpha: 0.03),
              blurRadius: 6,
              offset: const Offset(0, 2),
            ),
          ],
        ),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Container(
              width: 38,
              height: 38,
              decoration: BoxDecoration(
                color: color.withValues(alpha: 0.12),
                borderRadius: BorderRadius.circular(12),
              ),
              child: Icon(icon, color: color, size: 20),
            ),
            const SizedBox(height: 10),
            Text(
              title,
              style: const TextStyle(fontWeight: FontWeight.w800, fontSize: 13),
              maxLines: 1,
            ),
            const SizedBox(height: 2),
            Text(
              subtitle,
              style: TextStyle(
                fontSize: 10.5,
                color: theme.colorScheme.onSurfaceVariant,
                fontWeight: FontWeight.w500,
              ),
              maxLines: 1,
            ),
          ],
        ),
      ),
    );
  }
}

/// Interactive Fuel Selector with Instant Litre Presets & Price Calculator
class _FuelSelectorCard extends StatelessWidget {
  const _FuelSelectorCard({
    required this.prices,
    required this.selectedFuel,
    required this.selectedLitres,
    required this.onFuelChanged,
    required this.onLitresChanged,
    required this.onOrderPressed,
  });

  final List<FuelProduct> prices;
  final FuelType selectedFuel;
  final int selectedLitres;
  final ValueChanged<FuelType> onFuelChanged;
  final ValueChanged<int> onLitresChanged;
  final VoidCallback onOrderPressed;

  double get _currentUnitPrice {
    return prices.where((p) => p.type == selectedFuel).firstOrNull?.pricePerLitre ?? 135.0;
  }

  double get _calculatedTotal => _currentUnitPrice * selectedLitres;

  Color _fuelColor(FuelType type) {
    switch (type) {
      case FuelType.octane:
        return const Color(0xFF0D9488); // Teal (green-family accent)
      case FuelType.petrol:
        return const Color(0xFF059669); // Emerald Green
      case FuelType.diesel:
        return const Color(0xFFD97706); // Warm Amber
    }
  }

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final isDark = theme.brightness == Brightness.dark;

    return Container(
      padding: const EdgeInsets.all(AppTheme.gap20),
      decoration: BoxDecoration(
        color: isDark ? const Color(0xFF1E293B) : Colors.white,
        borderRadius: BorderRadius.circular(22),
        border: Border.all(
          color: isDark ? const Color(0xFF334155) : const Color(0xFFE2E8F0),
        ),
        boxShadow: [
          BoxShadow(
            color: Colors.black.withValues(alpha: 0.04),
            blurRadius: 10,
            offset: const Offset(0, 4),
          ),
        ],
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Row(
            mainAxisAlignment: MainAxisAlignment.spaceBetween,
            children: [
              Text(
                'Live Rates & Fuel Estimator',
                style: theme.textTheme.titleMedium?.copyWith(
                  fontWeight: FontWeight.w800,
                  letterSpacing: -0.3,
                ),
              ),
              Container(
                padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 3),
                decoration: BoxDecoration(
                  color: const Color(0xFF10B981).withValues(alpha: 0.12),
                  borderRadius: BorderRadius.circular(20),
                ),
                child: const Text(
                  'SEALED METER',
                  style: TextStyle(color: Color(0xFF059669), fontSize: 9.5, fontWeight: FontWeight.w800),
                ),
              ),
            ],
          ),
          const SizedBox(height: AppTheme.gap16),

          // Fuel Type Tabs
          Row(
            children: [
              for (final p in prices) ...[
                Expanded(
                  child: InkWell(
                    onTap: () => onFuelChanged(p.type),
                    borderRadius: BorderRadius.circular(14),
                    child: Container(
                      padding: const EdgeInsets.symmetric(vertical: 10, horizontal: 8),
                      decoration: BoxDecoration(
                        color: selectedFuel == p.type
                            ? _fuelColor(p.type).withValues(alpha: isDark ? 0.25 : 0.12)
                            : (isDark ? const Color(0xFF0F172A) : const Color(0xFFF8FAFC)),
                        borderRadius: BorderRadius.circular(14),
                        border: Border.all(
                          color: selectedFuel == p.type ? _fuelColor(p.type) : Colors.transparent,
                          width: 2,
                        ),
                      ),
                      child: Column(
                        children: [
                          Text(
                            p.type.label,
                            style: TextStyle(
                              fontWeight: FontWeight.w800,
                              fontSize: 13,
                              color: selectedFuel == p.type ? _fuelColor(p.type) : theme.colorScheme.onSurface,
                            ),
                          ),
                          const SizedBox(height: 2),
                          Text(
                            formatMoney(p.pricePerLitre),
                            style: TextStyle(
                              fontWeight: FontWeight.w900,
                              fontSize: 14,
                              color: selectedFuel == p.type ? _fuelColor(p.type) : Colors.grey,
                            ),
                          ),
                        ],
                      ),
                    ),
                  ),
                ),
                if (p != prices.last) const SizedBox(width: AppTheme.gap8),
              ],
            ],
          ),
          const SizedBox(height: AppTheme.gap16),

          // Quantity Preset Pills
          const Text('Select Litres', style: TextStyle(fontWeight: FontWeight.bold, fontSize: 13)),
          const SizedBox(height: 8),
          Row(
            mainAxisAlignment: MainAxisAlignment.spaceBetween,
            children: [
              for (final preset in [10, 20, 30, 45, 60]) ...[
                Expanded(
                  child: Padding(
                    padding: const EdgeInsets.symmetric(horizontal: 3),
                    child: ChoiceChip(
                      label: Text('${preset}L', style: const TextStyle(fontWeight: FontWeight.bold, fontSize: 12)),
                      selected: selectedLitres == preset,
                      onSelected: (_) => onLitresChanged(preset),
                      selectedColor: const Color(0xFF059669),
                      labelStyle: TextStyle(
                        color: selectedLitres == preset ? Colors.white : theme.colorScheme.onSurface,
                      ),
                      shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(10)),
                      showCheckmark: false,
                    ),
                  ),
                ),
              ],
            ],
          ),
          const Divider(height: AppTheme.gap24),

          // Estimated Total & Direct Order Button
          Row(
            mainAxisAlignment: MainAxisAlignment.spaceBetween,
            children: [
              Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  const Text('Estimated Cost', style: TextStyle(fontSize: 11, color: Colors.grey, fontWeight: FontWeight.w600)),
                  Text(
                    formatMoney(_calculatedTotal),
                    style: theme.textTheme.headlineSmall?.copyWith(
                      fontWeight: FontWeight.w900,
                      color: const Color(0xFF059669),
                      letterSpacing: -0.5,
                    ),
                  ),
                ],
              ),
              FilledButton.icon(
                onPressed: onOrderPressed,
                style: FilledButton.styleFrom(
                  backgroundColor: const Color(0xFF059669),
                  padding: const EdgeInsets.symmetric(horizontal: 20, vertical: 12),
                  shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(14)),
                ),
                icon: const Icon(Icons.shopping_bag_outlined, size: 18),
                label: const Text('Proceed with this Fuel', style: TextStyle(fontWeight: FontWeight.bold, fontSize: 13)),
              ),
            ],
          ),
        ],
      ),
    );
  }
}

/// Active Bowser Live Tracker (FuelBuddy Bowser Tracking Card)
class _ActiveBowserLiveTracker extends StatelessWidget {
  const _ActiveBowserLiveTracker({required this.order});

  final Order order;

  @override
  Widget build(BuildContext context) {
    final moving = order.deliveryStatus == DeliveryStatus.dispatched || order.deliveryStatus == DeliveryStatus.inTransit;

    return Container(
      decoration: BoxDecoration(
        color: const Color(0xFF0F172A),
        borderRadius: BorderRadius.circular(24),
        border: Border.all(color: const Color(0xFF059669).withValues(alpha: 0.5), width: 1.5),
        boxShadow: [
          BoxShadow(
            color: const Color(0xFF059669).withValues(alpha: 0.15),
            blurRadius: 16,
            offset: const Offset(0, 6),
          ),
        ],
      ),
      child: InkWell(
        onTap: () => context.push('/order/${order.id}'),
        borderRadius: BorderRadius.circular(24),
        child: Padding(
          padding: const EdgeInsets.all(AppTheme.gap20),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              // Header Tag
              Row(
                mainAxisAlignment: MainAxisAlignment.spaceBetween,
                children: [
                  Row(
                    children: [
                      Container(
                        width: 8,
                        height: 8,
                        decoration: const BoxDecoration(
                          shape: BoxShape.circle,
                          color: Color(0xFF10B981),
                        ),
                      ),
                      const SizedBox(width: 8),
                      Text(
                        'LIVE BOWSER TRACKING',
                        style: TextStyle(
                          color: Colors.white.withValues(alpha: 0.9),
                          fontSize: 11,
                          fontWeight: FontWeight.w800,
                          letterSpacing: 0.6,
                        ),
                      ),
                    ],
                  ),
                  StatusChip.delivery(order.deliveryStatus),
                ],
              ),
              const SizedBox(height: AppTheme.gap12),

              Text(
                'Tracking ${order.orderNumber}',
                style: const TextStyle(color: Color(0xFF34D399), fontSize: 12, fontWeight: FontWeight.w800, letterSpacing: 0.3),
              ),
              const SizedBox(height: 4),
              Text(
                moving ? 'Bowser is ${formatRelative(order.scheduledAt)}' : 'Refuel Scheduled: ${formatDateTime(order.scheduledAt)}',
                style: const TextStyle(
                  color: Colors.white,
                  fontWeight: FontWeight.w900,
                  fontSize: 18,
                  letterSpacing: -0.4,
                ),
              ),
              const SizedBox(height: 4),
              Text(
                '${formatLitres(order.totalLitres)} · ${order.lines.length} vehicle(s) · ${order.address}',
                style: TextStyle(color: Colors.white.withValues(alpha: 0.65), fontSize: 13),
                maxLines: 1,
                overflow: TextOverflow.ellipsis,
              ),
              const SizedBox(height: AppTheme.gap16),

              // Illuminated Delivery Steps
              DeliveryProgress(status: order.deliveryStatus),

              // Driver & Truck Section
              if (order.driverName != null) ...[
                const Divider(height: AppTheme.gap24, color: Color(0xFF334155)),
                Row(
                  children: [
                    Container(
                      width: 42,
                      height: 42,
                      decoration: const BoxDecoration(
                        shape: BoxShape.circle,
                        color: Color(0xFF1E293B),
                      ),
                      child: const Icon(Icons.person_pin_rounded, color: Color(0xFF34D399), size: 24),
                    ),
                    const SizedBox(width: AppTheme.gap12),
                    Expanded(
                      child: Column(
                        crossAxisAlignment: CrossAxisAlignment.start,
                        children: [
                          Row(
                            children: [
                              Text(
                                order.driverName!,
                                style: const TextStyle(color: Colors.white, fontWeight: FontWeight.bold, fontSize: 14),
                              ),
                              const SizedBox(width: 6),
                              const Icon(Icons.verified_rounded, color: Color(0xFF10B981), size: 14),
                            ],
                          ),
                          const SizedBox(height: 2),
                          if (order.truckRegistration != null) LicensePlate(order.truckRegistration!, compact: true),
                        ],
                      ),
                    ),
                    IconButton.filledTonal(
                      tooltip: 'Call Bowser Driver',
                      style: IconButton.styleFrom(
                        backgroundColor: const Color(0xFF1E293B),
                        foregroundColor: const Color(0xFF34D399),
                      ),
                      icon: const Icon(Icons.phone_in_talk_rounded),
                      onPressed: () => ScaffoldMessenger.of(context).showSnackBar(
                        const SnackBar(content: Text('Contacting Bowser driver...')),
                      ),
                    ),
                  ],
                ),
              ],
            ],
          ),
        ),
      ),
    );
  }
}

/// My Garage Vehicle Showcase
class _GarageVehicleShowcase extends StatelessWidget {
  const _GarageVehicleShowcase({required this.vehicles});

  final List<Vehicle> vehicles;

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final isDark = theme.brightness == Brightness.dark;

    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Row(
          mainAxisAlignment: MainAxisAlignment.spaceBetween,
          children: [
            Text('My Fuel Garage', style: theme.textTheme.titleMedium?.copyWith(fontWeight: FontWeight.w800)),
            TextButton.icon(
              onPressed: () => context.push('/vehicle/new'),
              icon: const Icon(Icons.add_circle_outline_rounded, size: 18),
              label: const Text('Add Vehicle', style: TextStyle(fontWeight: FontWeight.bold)),
            ),
          ],
        ),
        const SizedBox(height: AppTheme.gap8),
        if (vehicles.isEmpty)
          Container(
            padding: const EdgeInsets.all(AppTheme.gap20),
            decoration: BoxDecoration(
              color: isDark ? const Color(0xFF1E293B) : Colors.white,
              borderRadius: BorderRadius.circular(18),
              border: Border.all(color: isDark ? const Color(0xFF334155) : const Color(0xFFE2E8F0)),
            ),
            child: const Center(
              child: Column(
                children: [
                  Icon(Icons.directions_car_filled_rounded, size: 40, color: Colors.grey),
                  SizedBox(height: 8),
                  Text('Your garage is empty', style: TextStyle(fontWeight: FontWeight.bold, fontSize: 14)),
                  SizedBox(height: 4),
                  Text('Add your car, bike or generator to order doorstep fuel.', style: TextStyle(fontSize: 12, color: Colors.grey)),
                ],
              ),
            ),
          )
        else
          SizedBox(
            height: 110,
            child: ListView.separated(
              scrollDirection: Axis.horizontal,
              itemCount: vehicles.length,
              separatorBuilder: (_, _) => const SizedBox(width: AppTheme.gap12),
              itemBuilder: (context, index) {
                final v = vehicles[index];
                return InkWell(
                  onTap: () => context.push('/new-order'),
                  borderRadius: BorderRadius.circular(18),
                  child: Container(
                    width: 220,
                    padding: const EdgeInsets.all(AppTheme.gap12),
                    decoration: BoxDecoration(
                      color: isDark ? const Color(0xFF1E293B) : Colors.white,
                      borderRadius: BorderRadius.circular(18),
                      border: Border.all(
                        color: v.isVerified ? const Color(0xFF10B981).withValues(alpha: 0.4) : const Color(0xFFF59E0B).withValues(alpha: 0.4),
                        width: 1.5,
                      ),
                      boxShadow: [
                        BoxShadow(
                          color: Colors.black.withValues(alpha: 0.03),
                          blurRadius: 6,
                          offset: const Offset(0, 2),
                        ),
                      ],
                    ),
                    child: Column(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      mainAxisAlignment: MainAxisAlignment.center,
                      children: [
                        Row(
                          children: [
                            Icon(v.type.icon, size: 20, color: const Color(0xFF059669)),
                            const SizedBox(width: 8),
                            Expanded(
                              child: Text(
                                v.nickname,
                                style: const TextStyle(fontWeight: FontWeight.w800, fontSize: 13.5),
                                overflow: TextOverflow.ellipsis,
                              ),
                            ),
                            Icon(
                              v.isVerified ? Icons.verified_rounded : Icons.pending_rounded,
                              size: 16,
                              color: v.isVerified ? const Color(0xFF10B981) : const Color(0xFFF59E0B),
                            ),
                          ],
                        ),
                        const SizedBox(height: 6),
                        Text(
                          v.registrationNumber,
                          style: const TextStyle(fontSize: 11, color: Colors.grey, fontWeight: FontWeight.bold),
                        ),
                        const SizedBox(height: 6),
                        Row(
                          children: [
                            Container(
                              padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 2),
                              decoration: BoxDecoration(
                                color: const Color(0xFF059669).withValues(alpha: 0.12),
                                borderRadius: BorderRadius.circular(6),
                              ),
                              child: Text(
                                v.fuelType.label,
                                style: const TextStyle(fontSize: 10, fontWeight: FontWeight.bold, color: Color(0xFF059669)),
                              ),
                            ),
                            const Spacer(),
                            const Text('Refuel ➔', style: TextStyle(fontSize: 11, fontWeight: FontWeight.w700, color: Color(0xFF059669))),
                          ],
                        ),
                      ],
                    ),
                  ),
                );
              },
            ),
          ),
      ],
    );
  }
}

/// 4 Pillars of InstaFuel Trust & Safety (Certified Flow Meter, Zero Adulteration, SOS)
class _InstaFuelTrustBadges extends StatelessWidget {
  const _InstaFuelTrustBadges();

  @override
  Widget build(BuildContext context) {
    return Container(
      padding: const EdgeInsets.all(AppTheme.gap16),
      decoration: BoxDecoration(
        color: const Color(0xFFF1F5F9),
        borderRadius: BorderRadius.circular(18),
      ),
      child: const Column(
        children: [
          Row(
            children: [
              _TrustItem(icon: Icons.verified_user_rounded, title: '100% Pure', subtitle: 'Zero Adulteration'),
              _TrustItem(icon: Icons.speed_rounded, title: 'Digital Meter', subtitle: 'Calibrated Accuracy'),
              _TrustItem(icon: Icons.shield_rounded, title: 'Safe Delivery', subtitle: 'PESO & Hazmat Compliant'),
            ],
          ),
        ],
      ),
    );
  }
}

class _TrustItem extends StatelessWidget {
  const _TrustItem({required this.icon, required this.title, required this.subtitle});

  final IconData icon;
  final String title;
  final String subtitle;

  @override
  Widget build(BuildContext context) {
    return Expanded(
      child: Column(
        children: [
          Icon(icon, color: const Color(0xFF0F172A), size: 22),
          const SizedBox(height: 6),
          Text(title, style: const TextStyle(fontWeight: FontWeight.bold, fontSize: 11, color: Color(0xFF0F172A))),
          const SizedBox(height: 2),
          Text(subtitle, style: const TextStyle(fontSize: 9.5, color: Color(0xFF64748B)), textAlign: TextAlign.center),
        ],
      ),
    );
  }
}
