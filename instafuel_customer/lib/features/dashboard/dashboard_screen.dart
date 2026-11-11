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
  final _scaffoldKey = GlobalKey<ScaffoldState>();
  final _addressController = TextEditingController();

  FuelType _selectedFuel = FuelType.octane;
  int _selectedLitres = 30;
  String _selectedZone = 'Dhaka Metro · Central Hub';
  String? _deliveryAddress;
  late List<_NotificationItem> _notifications;

  @override
  void initState() {
    super.initState();
    _notifications = [
      _NotificationItem(
        id: '1',
        title: 'Smart Bowser Fleet Active',
        message: 'Primary depot operational. 12 GPS-tracked bowsers ready for fast doorstep refueling in your sector.',
        time: 'Just now',
        icon: Icons.local_shipping_rounded,
        color: const Color(0xFF004D3D),
      ),
      _NotificationItem(
        id: '2',
        title: 'Equipment Verification Audit',
        message: 'SOP §4.3 verification completed for registered vehicles. Instant refueling authorization enabled.',
        time: '25m ago',
        icon: Icons.verified_user_rounded,
        color: const Color(0xFF00E599),
      ),
      _NotificationItem(
        id: '3',
        title: 'Calibrated Fuel Rates Today',
        message: 'Octane ৳125.00/L · Petrol ৳121.00/L · Diesel ৳105.00/L. Digital flow meter certified at primary depot.',
        time: '08:00 AM',
        icon: Icons.speed_rounded,
        color: const Color(0xFF0284C7),
      ),
      _NotificationItem(
        id: '4',
        title: 'Zero Adulteration Guarantee',
        message: 'Every delivery is electronically sealed from depot with density & quality verification certificates.',
        time: 'Yesterday',
        icon: Icons.shield_rounded,
        color: const Color(0xFF6366F1),
      ),
    ];
  }

  @override
  void dispose() {
    _addressController.dispose();
    super.dispose();
  }

  void _showZonePicker() {
    final zones = [
      ('Dhaka Metro · Central Hub', 'Tejgaon Central Fuel Depot (Primary Bowser Hub)', '15-20 min dispatch'),
      ('Gulshan & Banani Zone', 'Diplomatic Enclave & Commercial Offices (Zone A)', '20-25 min dispatch'),
      ('Dhanmondi & Lalmatia Zone', 'Dhanmondi Lake & Residential/Retail (Zone B)', '20-30 min dispatch'),
      ('Uttara & Airport Zone', 'Uttara Sectors 1-18 & Airport Corridor (Zone C)', '25-35 min dispatch'),
      ('Bashundhara & Baridhara Hub', 'Corporate Campuses & Generator Stations (Zone D)', '25-35 min dispatch'),
      ('Mirpur & Pallabi Zone', 'Mirpur DOHS & Industrial Cluster (Zone E)', '25-35 min dispatch'),
      ('Motijheel & Old Dhaka Zone', 'Financial District & Commercial Hub (Zone F)', '20-30 min dispatch'),
    ];

    final orders = ref.read(ordersProvider).value ?? [];
    if (_addressController.text.isEmpty) {
      _addressController.text = _deliveryAddress ?? orders.firstOrNull?.address ?? '';
    }

    showModalBottomSheet(
      context: context,
      isScrollControlled: true,
      backgroundColor: Colors.transparent,
      builder: (ctx) {
        final isDark = Theme.of(ctx).brightness == Brightness.dark;
        final media = MediaQuery.of(ctx);

        return Container(
          constraints: BoxConstraints(
            maxHeight: media.size.height * 0.85,
          ),
          decoration: BoxDecoration(
            color: isDark ? const Color(0xFF0F172A) : Colors.white,
            borderRadius: const BorderRadius.vertical(top: Radius.circular(24)),
          ),
          padding: EdgeInsets.only(
            left: AppTheme.gap16,
            right: AppTheme.gap16,
            top: AppTheme.gap14,
            bottom: media.viewInsets.bottom + AppTheme.gap16,
          ),
          child: Column(
            mainAxisSize: MainAxisSize.min,
            crossAxisAlignment: CrossAxisAlignment.stretch,
            children: [
              // Drag Handle Pill
              Center(
                child: Container(
                  width: 44,
                  height: 4,
                  margin: const EdgeInsets.only(bottom: 12),
                  decoration: BoxDecoration(
                    color: Colors.grey.withValues(alpha: 0.35),
                    borderRadius: BorderRadius.circular(2),
                  ),
                ),
              ),
              // Header
              Row(
                children: [
                  Container(
                    padding: const EdgeInsets.all(8),
                    decoration: BoxDecoration(
                      color: const Color(0xFF004D3D).withValues(alpha: 0.12),
                      borderRadius: BorderRadius.circular(10),
                    ),
                    child: const Icon(Icons.location_on_rounded, color: Color(0xFF004D3D), size: 20),
                  ),
                  const SizedBox(width: 10),
                  const Expanded(
                    child: Column(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        Text('Select Delivery Zone', style: TextStyle(fontWeight: FontWeight.w800, fontSize: 17)),
                        Text('Choose bowser dispatch sector & your street address', style: TextStyle(fontSize: 11, color: Colors.grey)),
                      ],
                    ),
                  ),
                  IconButton(
                    icon: const Icon(Icons.close_rounded),
                    onPressed: () => Navigator.pop(ctx),
                  ),
                ],
              ),
              const SizedBox(height: AppTheme.gap12),

              // Specific Street Address input
              TextFormField(
                controller: _addressController,
                decoration: InputDecoration(
                  labelText: 'Street / Building Address (Optional)',
                  hintText: 'e.g. House 12, Road 5, Block B',
                  prefixIcon: const Icon(Icons.home_work_outlined, size: 20),
                  isDense: true,
                  filled: true,
                  fillColor: isDark ? const Color(0xFF1E293B) : const Color(0xFFF8FAFC),
                  border: OutlineInputBorder(
                    borderRadius: BorderRadius.circular(14),
                    borderSide: BorderSide(color: isDark ? const Color(0xFF334155) : const Color(0xFFE2E8F0)),
                  ),
                  enabledBorder: OutlineInputBorder(
                    borderRadius: BorderRadius.circular(14),
                    borderSide: BorderSide(color: isDark ? const Color(0xFF334155) : const Color(0xFFE2E8F0)),
                  ),
                  focusedBorder: OutlineInputBorder(
                    borderRadius: BorderRadius.circular(14),
                    borderSide: const BorderSide(color: Color(0xFF004D3D), width: 1.5),
                  ),
                ),
                onChanged: (val) {
                  setState(() => _deliveryAddress = val.trim().isEmpty ? null : val.trim());
                },
              ),
              const SizedBox(height: AppTheme.gap12),

              const Text(
                'Available Bowser Hubs',
                style: TextStyle(fontWeight: FontWeight.w700, fontSize: 12.5, letterSpacing: 0.2),
              ),
              const SizedBox(height: AppTheme.gap8),

              // Scrollable List of Delivery Zones
              Flexible(
                child: ListView.separated(
                  shrinkWrap: true,
                  itemCount: zones.length,
                  separatorBuilder: (context, index) => const SizedBox(height: AppTheme.gap8),
                  itemBuilder: (context, i) {
                    final (name, subtitle, eta) = zones[i];
                    final isSelected = _selectedZone == name;

                    return InkWell(
                      borderRadius: BorderRadius.circular(14),
                      onTap: () {
                        setState(() {
                          _selectedZone = name;
                          if (_addressController.text.trim().isNotEmpty) {
                            _deliveryAddress = _addressController.text.trim();
                          }
                        });
                        Navigator.pop(ctx);
                        ScaffoldMessenger.of(context).showSnackBar(
                          SnackBar(
                            content: Text('Delivery zone set to: $name ($eta)'),
                            backgroundColor: const Color(0xFF004D3D),
                            behavior: SnackBarBehavior.floating,
                          ),
                        );
                      },
                      child: Container(
                        padding: const EdgeInsets.symmetric(horizontal: 14, vertical: 12),
                        decoration: BoxDecoration(
                          color: isSelected
                              ? const Color(0xFF004D3D).withValues(alpha: isDark ? 0.25 : 0.08)
                              : (isDark ? const Color(0xFF1E293B) : Colors.white),
                          borderRadius: BorderRadius.circular(14),
                          border: Border.all(
                            color: isSelected
                                ? const Color(0xFF004D3D)
                                : (isDark ? const Color(0xFF334155) : const Color(0xFFE2E8F0)),
                            width: isSelected ? 1.8 : 1,
                          ),
                        ),
                        child: Row(
                          children: [
                            Icon(
                              isSelected ? Icons.check_circle_rounded : Icons.radio_button_unchecked_rounded,
                              color: isSelected ? const Color(0xFF004D3D) : Colors.grey,
                              size: 20,
                            ),
                            const SizedBox(width: 12),
                            Expanded(
                              child: Column(
                                crossAxisAlignment: CrossAxisAlignment.start,
                                children: [
                                  Text(
                                    name,
                                    style: TextStyle(
                                      fontWeight: isSelected ? FontWeight.w800 : FontWeight.w600,
                                      fontSize: 13.5,
                                      color: isSelected ? const Color(0xFF004D3D) : null,
                                    ),
                                  ),
                                  const SizedBox(height: 2),
                                  Text(
                                    subtitle,
                                    style: TextStyle(
                                      fontSize: 11,
                                      color: isDark ? Colors.grey[400] : const Color(0xFF64748B),
                                    ),
                                  ),
                                ],
                              ),
                            ),
                            const SizedBox(width: 8),
                            Container(
                              padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 3),
                              decoration: BoxDecoration(
                                color: const Color(0xFF00E599).withValues(alpha: 0.15),
                                borderRadius: BorderRadius.circular(8),
                              ),
                              child: Text(
                                eta,
                                style: const TextStyle(
                                  fontSize: 10.5,
                                  color: Color(0xFF004D3D),
                                  fontWeight: FontWeight.bold,
                                ),
                              ),
                            ),
                          ],
                        ),
                      ),
                    );
                  },
                ),
              ),
            ],
          ),
        );
      },
    );
  }

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
      key: _scaffoldKey,
      endDrawer: _NotificationSidePanel(
        notifications: _notifications,
        onClearAll: () {
          setState(() => _notifications.clear());
          ScaffoldMessenger.of(context).showSnackBar(
            const SnackBar(
              content: Text('All notifications cleared.'),
              behavior: SnackBarBehavior.floating,
              duration: Duration(seconds: 2),
            ),
          );
        },
        onDismiss: (id) {
          setState(() => _notifications.removeWhere((n) => n.id == id));
        },
      ),
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
                          onTap: _showZonePicker,
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
                                    color: const Color(0xFF004D3D).withValues(alpha: 0.15),
                                    shape: BoxShape.circle,
                                  ),
                                  child: const Icon(Icons.location_on_rounded, color: Color(0xFF004D3D), size: 16),
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
                                              color: Color(0xFF004D3D),
                                              letterSpacing: 0.6,
                                            ),
                                          ),
                                          const SizedBox(width: 4),
                                          Container(
                                            width: 6,
                                            height: 6,
                                            decoration: const BoxDecoration(
                                              shape: BoxShape.circle,
                                              color: Color(0xFF00E599),
                                            ),
                                          ),
                                        ],
                                      ),
                                      const SizedBox(height: 1),
                                      Text(
                                        _selectedZone,
                                        style: const TextStyle(fontSize: 12.5, fontWeight: FontWeight.w800),
                                        maxLines: 1,
                                        overflow: TextOverflow.ellipsis,
                                      ),
                                      Text(
                                        _deliveryAddress ?? orders.firstOrNull?.address ?? 'Tap to set address / zone',
                                        style: TextStyle(
                                          fontSize: 10.5,
                                          color: isDark ? Colors.grey[400] : const Color(0xFF64748B),
                                          fontWeight: FontWeight.w500,
                                        ),
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
                      // Notifications & Side Panel trigger
                      IconButton.filledTonal(
                        tooltip: 'Notifications',
                        style: IconButton.styleFrom(
                          backgroundColor: isDark ? const Color(0xFF1E293B) : Colors.white,
                          shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(16)),
                        ),
                        icon: Stack(
                          clipBehavior: Clip.none,
                          children: [
                            const Icon(Icons.notifications_none_rounded, size: 22),
                            if (_notifications.isNotEmpty)
                              Positioned(
                                top: -1,
                                right: -1,
                                child: Container(
                                  width: 8,
                                  height: 8,
                                  decoration: const BoxDecoration(
                                    color: Color(0xFF00E599),
                                    shape: BoxShape.circle,
                                  ),
                                ),
                              ),
                          ],
                        ),
                        onPressed: () => _scaffoldKey.currentState?.openEndDrawer(),
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
                                      color: Color(0xFF004D3D),
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
                          const SizedBox(height: AppTheme.gap16),
                        ],

                        // 100% Pure, Digital Meter, Safe Delivery (Placed at the very bottom as requested)
                        const _InstaFuelTrustBadges(),
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
                  border: Border.all(color: const Color(0xFF004D3D).withValues(alpha: 0.25), width: 1.5),
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
                  color: const Color(0xFF004D3D).withValues(alpha: 0.2),
                ),
                child: const Icon(Icons.local_shipping_rounded, color: Color(0xFF00E599), size: 16),
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
                          color: const Color(0xFF004D3D).withValues(alpha: 0.2),
                          borderRadius: BorderRadius.circular(20),
                          border: Border.all(color: const Color(0xFF004D3D).withValues(alpha: 0.5)),
                        ),
                        child: const Row(
                          mainAxisSize: MainAxisSize.min,
                          children: [
                            Icon(Icons.radar_rounded, color: Color(0xFF00E599), size: 13),
                            SizedBox(width: 5),
                            Text(
                              'SMART BOWSER RADAR',
                              style: TextStyle(
                                color: Color(0xFF00E599),
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
                          backgroundColor: const Color(0xFF004D3D),
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
            color: const Color(0xFF004D3D),
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
            color: const Color(0xFF004D3D),
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
        return const Color(0xFF004D3D); // Emerald Green
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
                  style: TextStyle(color: Color(0xFF004D3D), fontSize: 9.5, fontWeight: FontWeight.w800),
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
                      selectedColor: const Color(0xFF004D3D),
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
                      color: const Color(0xFF004D3D),
                      letterSpacing: -0.5,
                    ),
                  ),
                ],
              ),
              FilledButton.icon(
                onPressed: onOrderPressed,
                style: FilledButton.styleFrom(
                  backgroundColor: const Color(0xFF004D3D),
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
        border: Border.all(color: const Color(0xFF004D3D).withValues(alpha: 0.5), width: 1.5),
        boxShadow: [
          BoxShadow(
            color: const Color(0xFF004D3D).withValues(alpha: 0.15),
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
                style: const TextStyle(color: Color(0xFF00E599), fontSize: 12, fontWeight: FontWeight.w800, letterSpacing: 0.3),
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
                      child: const Icon(Icons.person_pin_rounded, color: Color(0xFF00E599), size: 24),
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
                        foregroundColor: const Color(0xFF00E599),
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
                            Icon(v.type.icon, size: 20, color: const Color(0xFF004D3D)),
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
                                color: const Color(0xFF004D3D).withValues(alpha: 0.12),
                                borderRadius: BorderRadius.circular(6),
                              ),
                              child: Text(
                                v.fuelType.label,
                                style: const TextStyle(fontSize: 10, fontWeight: FontWeight.bold, color: Color(0xFF004D3D)),
                              ),
                            ),
                            const Spacer(),
                            const Text('Refuel ➔', style: TextStyle(fontSize: 11, fontWeight: FontWeight.w700, color: Color(0xFF004D3D))),
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
    final isDark = Theme.of(context).brightness == Brightness.dark;

    return Container(
      padding: const EdgeInsets.symmetric(horizontal: AppTheme.gap16, vertical: AppTheme.gap16),
      decoration: BoxDecoration(
        color: isDark ? const Color(0xFF1E293B) : const Color(0xFFF8FAFC),
        borderRadius: BorderRadius.circular(18),
        border: Border.all(
          color: isDark ? const Color(0xFF334155) : const Color(0xFFE2E8F0),
        ),
      ),
      child: const Row(
        children: [
          _TrustItem(
            icon: Icons.verified_user_rounded,
            title: '100% Pure',
            subtitle: 'Zero Adulteration',
            iconColor: Color(0xFF00E599),
          ),
          _TrustItem(
            icon: Icons.speed_rounded,
            title: 'Digital Meter',
            subtitle: 'Calibrated Accuracy',
            iconColor: Color(0xFF004D3D),
          ),
          _TrustItem(
            icon: Icons.shield_rounded,
            title: 'Safe Delivery',
            subtitle: 'PESO & Hazmat Compliant',
            iconColor: Color(0xFF10B981),
          ),
        ],
      ),
    );
  }
}

class _TrustItem extends StatelessWidget {
  const _TrustItem({
    required this.icon,
    required this.title,
    required this.subtitle,
    required this.iconColor,
  });

  final IconData icon;
  final String title;
  final String subtitle;
  final Color iconColor;

  @override
  Widget build(BuildContext context) {
    final isDark = Theme.of(context).brightness == Brightness.dark;

    return Expanded(
      child: Column(
        children: [
          Container(
            padding: const EdgeInsets.all(8),
            decoration: BoxDecoration(
              color: iconColor.withValues(alpha: isDark ? 0.25 : 0.12),
              shape: BoxShape.circle,
            ),
            child: Icon(icon, color: iconColor, size: 20),
          ),
          const SizedBox(height: 6),
          Text(
            title,
            style: const TextStyle(fontWeight: FontWeight.w800, fontSize: 12),
            textAlign: TextAlign.center,
          ),
          const SizedBox(height: 2),
          Text(
            subtitle,
            style: TextStyle(
              fontSize: 10,
              color: isDark ? Colors.grey[400] : const Color(0xFF64748B),
            ),
            textAlign: TextAlign.center,
          ),
        ],
      ),
    );
  }
}

/// Notification Data Model
class _NotificationItem {
  final String id;
  final String title;
  final String message;
  final String time;
  final IconData icon;
  final Color color;

  const _NotificationItem({
    required this.id,
    required this.title,
    required this.message,
    required this.time,
    required this.icon,
    required this.color,
  });
}

/// Notification Sliding Side Panel (Drawer)
class _NotificationSidePanel extends StatelessWidget {
  const _NotificationSidePanel({
    required this.notifications,
    required this.onClearAll,
    required this.onDismiss,
  });

  final List<_NotificationItem> notifications;
  final VoidCallback onClearAll;
  final ValueChanged<String> onDismiss;

  @override
  Widget build(BuildContext context) {
    final isDark = Theme.of(context).brightness == Brightness.dark;

    return Drawer(
      backgroundColor: isDark ? const Color(0xFF0F172A) : Colors.white,
      shape: const RoundedRectangleBorder(
        borderRadius: BorderRadius.horizontal(left: Radius.circular(24)),
      ),
      child: SafeArea(
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.stretch,
          children: [
            // Drawer Header
            Padding(
              padding: const EdgeInsets.fromLTRB(AppTheme.gap20, AppTheme.gap16, AppTheme.gap12, AppTheme.gap12),
              child: Row(
                children: [
                  Container(
                    padding: const EdgeInsets.all(8),
                    decoration: BoxDecoration(
                      color: const Color(0xFF004D3D).withValues(alpha: 0.12),
                      borderRadius: BorderRadius.circular(10),
                    ),
                    child: const Icon(Icons.notifications_active_rounded, color: Color(0xFF004D3D), size: 20),
                  ),
                  const SizedBox(width: 10),
                  Expanded(
                    child: Column(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        const Text(
                          'Notifications',
                          style: TextStyle(fontWeight: FontWeight.w800, fontSize: 17),
                        ),
                        Text(
                          notifications.isEmpty ? 'All caught up' : '${notifications.length} active updates',
                          style: TextStyle(
                            fontSize: 11,
                            color: isDark ? Colors.grey[400] : const Color(0xFF64748B),
                          ),
                        ),
                      ],
                    ),
                  ),
                  if (notifications.isNotEmpty)
                    TextButton.icon(
                      onPressed: onClearAll,
                      style: TextButton.styleFrom(
                        padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 4),
                        minimumSize: Size.zero,
                        tapTargetSize: MaterialTapTargetSize.shrinkWrap,
                        foregroundColor: const Color(0xFFDC2626),
                      ),
                      icon: const Icon(Icons.clear_all_rounded, size: 16),
                      label: const Text('Clear all', style: TextStyle(fontSize: 12, fontWeight: FontWeight.bold)),
                    ),
                  IconButton(
                    icon: const Icon(Icons.close_rounded, size: 20),
                    onPressed: () => Navigator.pop(context),
                  ),
                ],
              ),
            ),
            const Divider(height: 1),

            // Notification List or Empty State
            Expanded(
              child: notifications.isEmpty
                  ? Center(
                      child: Padding(
                        padding: const EdgeInsets.all(AppTheme.gap24),
                        child: Column(
                          mainAxisSize: MainAxisSize.min,
                          children: [
                            Container(
                              padding: const EdgeInsets.all(20),
                              decoration: BoxDecoration(
                                color: isDark ? const Color(0xFF1E293B) : const Color(0xFFF1F5F9),
                                shape: BoxShape.circle,
                              ),
                              child: const Icon(Icons.notifications_off_outlined, size: 40, color: Colors.grey),
                            ),
                            const SizedBox(height: AppTheme.gap16),
                            const Text(
                              'No Notifications',
                              style: TextStyle(fontWeight: FontWeight.bold, fontSize: 16),
                            ),
                            const SizedBox(height: 6),
                            Text(
                              'You are all caught up! New bowser dispatches, tariff updates, and fuel deliveries will show up here.',
                              textAlign: TextAlign.center,
                              style: TextStyle(
                                fontSize: 12,
                                color: isDark ? Colors.grey[400] : const Color(0xFF64748B),
                              ),
                            ),
                          ],
                        ),
                      ),
                    )
                  : ListView.separated(
                      padding: const EdgeInsets.symmetric(horizontal: AppTheme.gap16, vertical: AppTheme.gap12),
                      itemCount: notifications.length,
                      separatorBuilder: (context, index) => const SizedBox(height: AppTheme.gap10),
                      itemBuilder: (context, i) {
                        final n = notifications[i];
                        return Dismissible(
                          key: Key(n.id),
                          direction: DismissDirection.endToStart,
                          background: Container(
                            alignment: Alignment.centerRight,
                            padding: const EdgeInsets.only(right: 16),
                            decoration: BoxDecoration(
                              color: const Color(0xFFDC2626).withValues(alpha: 0.15),
                              borderRadius: BorderRadius.circular(14),
                            ),
                            child: const Icon(Icons.delete_outline_rounded, color: Color(0xFFDC2626)),
                          ),
                          onDismissed: (_) => onDismiss(n.id),
                          child: Container(
                            padding: const EdgeInsets.all(12),
                            decoration: BoxDecoration(
                              color: isDark ? const Color(0xFF1E293B) : const Color(0xFFF8FAFC),
                              borderRadius: BorderRadius.circular(14),
                              border: Border.all(
                                color: isDark ? const Color(0xFF334155) : const Color(0xFFE2E8F0),
                              ),
                            ),
                            child: Row(
                              crossAxisAlignment: CrossAxisAlignment.start,
                              children: [
                                Container(
                                  padding: const EdgeInsets.all(8),
                                  decoration: BoxDecoration(
                                    color: n.color.withValues(alpha: 0.15),
                                    shape: BoxShape.circle,
                                  ),
                                  child: Icon(n.icon, color: n.color, size: 18),
                                ),
                                const SizedBox(width: 10),
                                Expanded(
                                  child: Column(
                                    crossAxisAlignment: CrossAxisAlignment.start,
                                    children: [
                                      Row(
                                        mainAxisAlignment: MainAxisAlignment.spaceBetween,
                                        children: [
                                          Expanded(
                                            child: Text(
                                              n.title,
                                              style: const TextStyle(fontWeight: FontWeight.bold, fontSize: 13),
                                            ),
                                          ),
                                          Text(
                                            n.time,
                                            style: TextStyle(
                                              fontSize: 10,
                                              color: isDark ? Colors.grey[400] : const Color(0xFF94A3B8),
                                            ),
                                          ),
                                        ],
                                      ),
                                      const SizedBox(height: 4),
                                      Text(
                                        n.message,
                                        style: TextStyle(
                                          fontSize: 11.5,
                                          height: 1.35,
                                          color: isDark ? Colors.grey[300] : const Color(0xFF475569),
                                        ),
                                      ),
                                    ],
                                  ),
                                ),
                              ],
                            ),
                          ),
                        );
                      },
                    ),
            ),
          ],
        ),
      ),
    );
  }
}

