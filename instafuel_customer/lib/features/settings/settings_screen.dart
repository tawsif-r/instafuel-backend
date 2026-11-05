// Account tab: profile, company links, appearance (light/dark), logout.

import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import '../../core/theme.dart';
import '../../core/widgets/responsive_center.dart';
import '../auth/auth_controller.dart';
import '../corporate/corporate_controller.dart';
import 'theme_controller.dart';

class SettingsScreen extends ConsumerStatefulWidget {
  const SettingsScreen({super.key});

  @override
  ConsumerState<SettingsScreen> createState() => _SettingsScreenState();
}

class _SettingsScreenState extends ConsumerState<SettingsScreen> {
  bool _orderUpdates = true; // UI only for now
  bool _promotions = false;

  Future<void> _logout() async {
    final ok = await showDialog<bool>(
      context: context,
      builder: (context) => AlertDialog(
        title: const Text('Log out?'),
        actions: [
          TextButton(onPressed: () => Navigator.pop(context, false), child: const Text('Cancel')),
          FilledButton(onPressed: () => Navigator.pop(context, true), child: const Text('Log out')),
        ],
      ),
    );
    if (ok == true) await ref.read(authControllerProvider.notifier).logout();
  }

  void _comingSoon(String what) =>
      ScaffoldMessenger.of(context).showSnackBar(SnackBar(content: Text('$what is coming soon.')));

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final user = ref.watch(authControllerProvider);
    final themeMode = ref.watch(themeModeProvider);
    final companies = ref.watch(affiliationsProvider).value ?? [];
    if (user == null) return const SizedBox.shrink(); // logging out

    return Scaffold(
      appBar: AppBar(title: const Text('Account')),
      body: ListView(
        padding: const EdgeInsets.symmetric(vertical: AppTheme.gap16),
        children: [
          ResponsiveCenter(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.stretch,
              children: [
                // ----- Profile -----
                Padding(
                  padding: const EdgeInsets.symmetric(horizontal: AppTheme.gap16),
                  child: Card(
                    child: Padding(
                      padding: const EdgeInsets.all(AppTheme.gap16),
                      child: Row(
                        children: [
                          CircleAvatar(
                            radius: 28,
                            backgroundColor: theme.colorScheme.primary,
                            child: Text(
                              user.initials,
                              style: theme.textTheme.titleLarge?.copyWith(color: theme.colorScheme.onPrimary),
                            ),
                          ),
                          const SizedBox(width: AppTheme.gap16),
                          Expanded(
                            child: Column(
                              crossAxisAlignment: CrossAxisAlignment.start,
                              children: [
                                Text(user.fullName, style: theme.textTheme.titleMedium),
                                Text(user.email, style: theme.textTheme.bodySmall),
                                Text(user.phone, style: theme.textTheme.bodySmall),
                              ],
                            ),
                          ),
                          Chip(label: Text(user.accountType.label), visualDensity: VisualDensity.compact),
                        ],
                      ),
                    ),
                  ),
                ),

                const _Header('Account'),
                ListTile(
                  leading: const Icon(Icons.person_outline),
                  title: const Text('Edit profile'),
                  trailing: const Icon(Icons.chevron_right),
                  onTap: () => _comingSoon('Editing your profile'),
                ),
                ListTile(
                  leading: const Icon(Icons.business_outlined),
                  title: const Text('Corporate accounts'),
                  subtitle: Text(companies.isEmpty
                      ? 'Link a company to bill orders to it'
                      : '${companies.length} linked · ${companies.where((c) => c.isActive).length} active'),
                  trailing: const Icon(Icons.chevron_right),
                  onTap: () => context.push('/corporate'),
                ),
                ListTile(
                  leading: const Icon(Icons.location_on_outlined),
                  title: const Text('Saved addresses'),
                  trailing: const Icon(Icons.chevron_right),
                  onTap: () => _comingSoon('Saved addresses'),
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

                const _Header('Notifications'),
                SwitchListTile(
                  secondary: const Icon(Icons.local_shipping_outlined),
                  title: const Text('Order & delivery updates'),
                  value: _orderUpdates,
                  onChanged: (v) => setState(() => _orderUpdates = v),
                ),
                SwitchListTile(
                  secondary: const Icon(Icons.campaign_outlined),
                  title: const Text('Offers and promotions'),
                  value: _promotions,
                  onChanged: (v) => setState(() => _promotions = v),
                ),

                const _Header('Support'),
                ListTile(
                  leading: const Icon(Icons.support_agent_outlined),
                  title: const Text('Help & support'),
                  trailing: const Icon(Icons.chevron_right),
                  onTap: () => _comingSoon('Support'),
                ),
                ListTile(
                  leading: const Icon(Icons.info_outline),
                  title: const Text('About Instafuel'),
                  onTap: () => showAboutDialog(
                    context: context,
                    applicationName: 'Instafuel',
                    applicationVersion: '0.1.0 (preview)',
                    applicationIcon: Icon(Icons.local_gas_station, color: theme.colorScheme.primary),
                  ),
                ),
                const SizedBox(height: AppTheme.gap16),
                Padding(
                  padding: const EdgeInsets.symmetric(horizontal: AppTheme.gap16),
                  child: OutlinedButton.icon(
                    onPressed: _logout,
                    icon: Icon(Icons.logout, color: theme.colorScheme.error),
                    label: Text('Log out', style: TextStyle(color: theme.colorScheme.error)),
                  ),
                ),
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
          text,
          style: Theme.of(context).textTheme.labelLarge?.copyWith(color: Theme.of(context).colorScheme.primary),
        ),
      );
}
