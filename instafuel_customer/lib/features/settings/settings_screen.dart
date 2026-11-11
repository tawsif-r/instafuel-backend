// Account & Compliance tab complying with SOP-OPS-001 Section 4, 13 & 14.
// Customer Profile, Corporate Links, HSE Emergency Hotline, Appearance & Sign Out.

import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import '../../core/theme.dart';
import '../../core/widgets/responsive_center.dart';
import '../../models/user.dart';
import '../auth/auth_controller.dart';
import '../corporate/corporate_controller.dart';
import 'theme_controller.dart';

class SettingsScreen extends ConsumerStatefulWidget {
  const SettingsScreen({super.key});

  @override
  ConsumerState<SettingsScreen> createState() => _SettingsScreenState();
}

class _SettingsScreenState extends ConsumerState<SettingsScreen> {
  bool _orderUpdates = true;
  bool _promotions = false;

  Future<void> _logout() async {
    final ok = await showDialog<bool>(
      context: context,
      builder: (context) => AlertDialog(
        title: const Text('Log out of InstaFuel?'),
        content: const Text('You will need to sign in again to schedule refuels and view delivery manifests.'),
        actions: [
          TextButton(onPressed: () => Navigator.pop(context, false), child: const Text('Cancel')),
          FilledButton(
            onPressed: () => Navigator.pop(context, true),
            style: FilledButton.styleFrom(backgroundColor: const Color(0xFFDC2626)),
            child: const Text('Sign Out'),
          ),
        ],
      ),
    );
    if (ok == true) await ref.read(authControllerProvider.notifier).logout();
  }

  void _showHseDialog() {
    showDialog(
      context: context,
      builder: (ctx) => AlertDialog(
        title: const Row(
          children: [
            Icon(Icons.emergency_rounded, color: Color(0xFFDC2626)),
            SizedBox(width: 8),
            Text('HSE Emergency Hotline'),
          ],
        ),
        content: const Text(
          'Under SOP §14 (Health, Safety & Emergency Management), report any fuel spill, vapor leakage, or bowser emergency immediately to our 24/7 Dispatch Operations Centre:\n\n📞 Emergency Hotline: +880 9612-3835-00\n🚒 Fire & Safety Cell: Ext 9',
        ),
        actions: [
          TextButton(onPressed: () => Navigator.pop(ctx), child: const Text('Close')),
        ],
      ),
    );
  }

  void _editProfile(BuildContext context, user) {
    final nameCtrl = TextEditingController(text: user.fullName);
    final phoneCtrl = TextEditingController(text: user.phone);
    final formKey = GlobalKey<FormState>();
    bool saving = false;

    showModalBottomSheet(
      context: context,
      isScrollControlled: true,
      shape: const RoundedRectangleBorder(
        borderRadius: BorderRadius.vertical(top: Radius.circular(24)),
      ),
      builder: (ctx) => StatefulBuilder(
        builder: (ctx, setSheetState) => Padding(
          padding: EdgeInsets.only(
            left: AppTheme.gap20,
            right: AppTheme.gap20,
            top: AppTheme.gap20,
            bottom: MediaQuery.of(ctx).viewInsets.bottom + AppTheme.gap24,
          ),
          child: Form(
            key: formKey,
            child: Column(
              mainAxisSize: MainAxisSize.min,
              crossAxisAlignment: CrossAxisAlignment.stretch,
              children: [
                Row(
                  mainAxisAlignment: MainAxisAlignment.spaceBetween,
                  children: [
                    const Text(
                      'Edit Customer Profile',
                      style: TextStyle(fontWeight: FontWeight.w800, fontSize: 18),
                    ),
                    IconButton(
                      icon: const Icon(Icons.close_rounded),
                      onPressed: () => Navigator.pop(ctx),
                    ),
                  ],
                ),
                const SizedBox(height: AppTheme.gap16),
                TextFormField(
                  controller: nameCtrl,
                  decoration: const InputDecoration(
                    labelText: 'Full Name',
                    prefixIcon: Icon(Icons.person_outline_rounded),
                  ),
                  validator: (v) => v == null || v.trim().isEmpty ? 'Full name is required' : null,
                ),
                const SizedBox(height: AppTheme.gap12),
                TextFormField(
                  controller: phoneCtrl,
                  keyboardType: TextInputType.phone,
                  decoration: const InputDecoration(
                    labelText: 'Phone Number',
                    prefixIcon: Icon(Icons.phone_outlined),
                  ),
                  validator: (v) => v == null || v.trim().isEmpty ? 'Phone number is required' : null,
                ),
                const SizedBox(height: AppTheme.gap20),
                FilledButton(
                  onPressed: saving
                      ? null
                      : () async {
                          if (!formKey.currentState!.validate()) return;
                          setSheetState(() => saving = true);
                          try {
                            await ref.read(authControllerProvider.notifier).updateProfile(
                                  fullName: nameCtrl.text.trim(),
                                  phone: phoneCtrl.text.trim(),
                                );
                            if (ctx.mounted) Navigator.pop(ctx);
                            if (context.mounted) {
                              ScaffoldMessenger.of(context).showSnackBar(
                                const SnackBar(
                                  content: Text('Profile updated successfully!'),
                                  backgroundColor: Color(0xFF004D3D),
                                ),
                              );
                            }
                          } catch (e) {
                            setSheetState(() => saving = false);
                            if (context.mounted) {
                              ScaffoldMessenger.of(context).showSnackBar(
                                SnackBar(
                                  content: Text('Failed to update: $e'),
                                  backgroundColor: const Color(0xFFDC2626),
                                ),
                              );
                            }
                          }
                        },
                  child: saving
                      ? const SizedBox(
                          height: 20,
                          width: 20,
                          child: CircularProgressIndicator(color: Colors.white, strokeWidth: 2),
                        )
                      : const Text('Save Profile'),
                ),
              ],
            ),
          ),
        ),
      ),
    );
  }

  @override
  Widget build(BuildContext context) {
    final user = ref.watch(authControllerProvider);
    final themeMode = ref.watch(themeModeProvider);
    final companies = ref.watch(affiliationsProvider).value ?? [];
    if (user == null) return const SizedBox.shrink();

    return Scaffold(
      appBar: AppBar(title: const Text('Account & Compliance')),
      body: ListView(
        padding: const EdgeInsets.symmetric(vertical: AppTheme.gap16),
        children: [
          ResponsiveCenter(
            maxWidth: 640,
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.stretch,
              children: [
                // ----- Profile Card with SOP Verification -----
                Padding(
                  padding: const EdgeInsets.symmetric(horizontal: AppTheme.gap16),
                  child: Container(
                    padding: const EdgeInsets.all(AppTheme.gap20),
                    decoration: BoxDecoration(
                      color: const Color(0xFF0F172A),
                      borderRadius: BorderRadius.circular(20),
                      boxShadow: [
                        BoxShadow(
                          color: Colors.black.withValues(alpha: 0.1),
                          blurRadius: 10,
                          offset: const Offset(0, 4),
                        ),
                      ],
                    ),
                    child: Row(
                      children: [
                        CircleAvatar(
                          radius: 30,
                          backgroundColor: const Color(0xFF004D3D),
                          child: Text(
                            user.initials,
                            style: const TextStyle(fontWeight: FontWeight.w900, color: Colors.white, fontSize: 20),
                          ),
                        ),
                        const SizedBox(width: AppTheme.gap16),
                        Expanded(
                          child: Column(
                            crossAxisAlignment: CrossAxisAlignment.start,
                            children: [
                              Row(
                                children: [
                                  Text(
                                    user.fullName,
                                    style: const TextStyle(color: Colors.white, fontWeight: FontWeight.bold, fontSize: 17),
                                  ),
                                  const SizedBox(width: 6),
                                  const Icon(Icons.verified_rounded, size: 16, color: Color(0xFF10B981)),
                                ],
                              ),
                              const SizedBox(height: 2),
                              if (user.accountType == AccountType.corporate && user.companyName != null && user.companyName!.isNotEmpty) ...[
                                Text(
                                  user.companyName!,
                                  style: const TextStyle(color: Color(0xFF00E599), fontWeight: FontWeight.w700, fontSize: 13),
                                ),
                                const SizedBox(height: 2),
                              ],
                              Text(user.email, style: TextStyle(color: Colors.white.withValues(alpha: 0.7), fontSize: 12.5)),
                              Text(user.phone, style: TextStyle(color: Colors.white.withValues(alpha: 0.7), fontSize: 12.5)),
                              const SizedBox(height: 6),
                              Container(
                                padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 2),
                                decoration: BoxDecoration(
                                  color: const Color(0xFF004D3D).withValues(alpha: 0.25),
                                  borderRadius: BorderRadius.circular(6),
                                ),
                                child: Text(
                                  user.accountType.label.toUpperCase(),
                                  style: const TextStyle(color: Color(0xFF00E599), fontSize: 9.5, fontWeight: FontWeight.w800),
                                ),
                              ),
                            ],
                          ),
                        ),
                        IconButton(
                          tooltip: 'Edit Profile',
                          icon: const Icon(Icons.edit_outlined, color: Colors.white, size: 20),
                          onPressed: () => _editProfile(context, user),
                        ),
                      ],
                    ),
                  ),
                ),

                const _Header('Customer Profile & Fleet (SOP §4)'),
                ListTile(
                  leading: const Icon(Icons.directions_car_rounded, color: Color(0xFF004D3D)),
                  title: const Text('My Fuel Garage & Equipment'),
                  subtitle: const Text('Private vehicles, transport trucks & backup generators'),
                  trailing: const Icon(Icons.chevron_right_rounded),
                  onTap: () => context.push('/vehicles'),
                ),
                if (user.accountType != AccountType.corporate)
                  ListTile(
                    leading: const Icon(Icons.corporate_fare_rounded, color: Color(0xFF004D3D)),
                    title: const Text('Corporate Accounts & Credit Limit'),
                    subtitle: Text(companies.isEmpty
                        ? 'Link company for 30-day billing (SOP §12.3)'
                        : '${companies.length} company linked · ${companies.where((c) => c.isActive).length} active credit account'),
                    trailing: const Icon(Icons.chevron_right_rounded),
                    onTap: () => context.push('/corporate'),
                  ),

                const _Header('Safety & Emergency (SOP §14)'),
                Padding(
                  padding: const EdgeInsets.symmetric(horizontal: AppTheme.gap16),
                  child: Container(
                    decoration: BoxDecoration(
                      color: const Color(0xFFFEF2F2),
                      borderRadius: BorderRadius.circular(14),
                      border: Border.all(color: const Color(0xFFFCA5A5)),
                    ),
                    child: Material(
                      color: Colors.transparent,
                      child: ListTile(
                        leading: const Icon(Icons.emergency_rounded, color: Color(0xFFDC2626)),
                        title: const Text(
                          '24/7 HSE & Spill Emergency Hotline',
                          style: TextStyle(color: Color(0xFF991B1B), fontWeight: FontWeight.bold, fontSize: 13.5),
                        ),
                        subtitle: const Text(
                          'Immediate response for fuel leakage, fire or delivery hazards',
                          style: TextStyle(color: Color(0xFFB91C1C), fontSize: 11),
                        ),
                        trailing: const Icon(Icons.phone_in_talk_rounded, color: Color(0xFFDC2626)),
                        onTap: _showHseDialog,
                      ),
                    ),
                  ),
                ),

                const _Header('Appearance'),
                Padding(
                  padding: const EdgeInsets.symmetric(horizontal: AppTheme.gap16),
                  child: SegmentedButton<ThemeMode>(
                    segments: const [
                      ButtonSegment(value: ThemeMode.system, label: Text('System'), icon: Icon(Icons.brightness_auto)),
                      ButtonSegment(value: ThemeMode.light, label: Text('Light'), icon: Icon(Icons.light_mode_outlined)),
                      ButtonSegment(value: ThemeMode.dark, label: Text('Dark'), icon: Icon(Icons.dark_mode_outlined)),
                    ],
                    selected: {themeMode},
                    onSelectionChanged: (s) => ref.read(themeModeProvider.notifier).setMode(s.first),
                  ),
                ),

                const _Header('Operational Notifications (SOP §13)'),
                SwitchListTile(
                  secondary: const Icon(Icons.local_shipping_outlined),
                  title: const Text('Bowser Dispatch & Slot Updates'),
                  subtitle: const Text('Real-time arrival notification & meter calibration certificate'),
                  value: _orderUpdates,
                  onChanged: (v) => setState(() => _orderUpdates = v),
                ),
                SwitchListTile(
                  secondary: const Icon(Icons.discount_outlined),
                  title: const Text('Fleet Pricing & Regulatory Tariff Notices'),
                  value: _promotions,
                  onChanged: (v) => setState(() => _promotions = v),
                ),

                const _Header('Compliance & Governance (SOP §1, 18, 19)'),
                ListTile(
                  leading: const Icon(Icons.policy_outlined),
                  title: const Text('Standard Operating Procedure (SOP-OPS-001)'),
                  subtitle: const Text('Licensed On-Demand Fuel Supply & Delivery Operations'),
                  trailing: const Icon(Icons.chevron_right_rounded),
                  onTap: () => showDialog(
                    context: context,
                    builder: (ctx) => AlertDialog(
                      title: const Text('SOP-OPS-001 Compliance'),
                      content: const Text(
                        'InstaFuel operates as a licensed petroleum distributor with its own dedicated bowser tanker fleet. All deliveries are governed by 24h advance scheduling, automated meter calibration, and strict HSE safety controls.',
                      ),
                      actions: [
                        TextButton(onPressed: () => Navigator.pop(ctx), child: const Text('OK')),
                      ],
                    ),
                  ),
                ),
                ListTile(
                  leading: const Icon(Icons.info_outline_rounded),
                  title: const Text('About InstaFuel'),
                  subtitle: const Text('v1.0.0 (SOP-OPS-001 Ready)'),
                  onTap: () => showAboutDialog(
                    context: context,
                    applicationName: 'InstaFuel Customer App',
                    applicationVersion: '1.0.0 (SOP Certified)',
                    applicationIcon: const Icon(Icons.local_gas_station_rounded, color: Color(0xFF004D3D), size: 36),
                  ),
                ),

                const SizedBox(height: AppTheme.gap24),
                Padding(
                  padding: const EdgeInsets.symmetric(horizontal: AppTheme.gap16),
                  child: OutlinedButton.icon(
                    onPressed: _logout,
                    style: OutlinedButton.styleFrom(
                      foregroundColor: const Color(0xFFDC2626),
                      side: const BorderSide(color: Color(0xFFDC2626)),
                      padding: const EdgeInsets.symmetric(vertical: 14),
                      shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(14)),
                    ),
                    icon: const Icon(Icons.logout_rounded),
                    label: const Text('Sign Out', style: TextStyle(fontWeight: FontWeight.bold)),
                  ),
                ),
                const SizedBox(height: AppTheme.gap32),
              ],
            ),
          ),
        ],
      ),
    );
  }
}

class _Header extends StatelessWidget {
  const _Header(this.text);

  final String text;

  @override
  Widget build(BuildContext context) => Padding(
        padding: const EdgeInsets.fromLTRB(AppTheme.gap16, AppTheme.gap24, AppTheme.gap16, AppTheme.gap8),
        child: Text(
          text.toUpperCase(),
          style: const TextStyle(
            fontSize: 11,
            fontWeight: FontWeight.w800,
            letterSpacing: 0.8,
            color: Color(0xFF004D3D),
          ),
        ),
      );
}
