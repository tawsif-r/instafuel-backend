// Bottom sheet to link a corporate account conforming to SOP-OPS-001 Section 4.2.

import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../core/theme.dart';
import 'corporate_controller.dart';

class JoinCompanySheet extends ConsumerStatefulWidget {
  const JoinCompanySheet({super.key});

  static Future<void> show(BuildContext context) => showModalBottomSheet(
        context: context,
        isScrollControlled: true,
        showDragHandle: true,
        backgroundColor: Theme.of(context).scaffoldBackgroundColor,
        shape: const RoundedRectangleBorder(
          borderRadius: BorderRadius.vertical(top: Radius.circular(28)),
        ),
        builder: (_) => const JoinCompanySheet(),
      );

  @override
  ConsumerState<JoinCompanySheet> createState() => _JoinCompanySheetState();
}

class _JoinCompanySheetState extends ConsumerState<JoinCompanySheet> {
  final _code = TextEditingController();
  bool _loading = false;
  String? _error;

  @override
  void dispose() {
    _code.dispose();
    super.dispose();
  }

  Future<void> _join() async {
    final text = _code.text.trim();
    if (text.isEmpty) {
      setState(() => _error = 'Please enter your corporate code');
      return;
    }

    setState(() {
      _loading = true;
      _error = null;
    });

    try {
      final a = await ref.read(affiliationsProvider.notifier).join(text);
      if (!mounted) return;
      Navigator.pop(context);
      ScaffoldMessenger.of(context).showSnackBar(SnackBar(
        content: Text(a.isActive
            ? 'Successfully linked to ${a.companyName}! 30-day corporate credit activated.'
            : 'Link request submitted for ${a.companyName}. Waiting for company fleet admin approval.'),
        behavior: SnackBarBehavior.floating,
      ));
    } catch (e) {
      if (mounted) setState(() => _error = e.toString());
    } finally {
      if (mounted) setState(() => _loading = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);

    return Padding(
      padding: EdgeInsets.fromLTRB(
        AppTheme.gap24,
        0,
        AppTheme.gap24,
        MediaQuery.viewInsetsOf(context).bottom + AppTheme.gap24,
      ),
      child: Column(
        mainAxisSize: MainAxisSize.min,
        crossAxisAlignment: CrossAxisAlignment.stretch,
        children: [
          Row(
            children: [
              Container(
                width: 44,
                height: 44,
                decoration: BoxDecoration(
                  color: const Color(0xFF004D3D).withValues(alpha: 0.12),
                  borderRadius: BorderRadius.circular(14),
                ),
                child: const Icon(Icons.business_rounded, color: Color(0xFF004D3D), size: 24),
              ),
              const SizedBox(width: AppTheme.gap12),
              const Expanded(
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Text('Link Corporate Account', style: TextStyle(fontWeight: FontWeight.w900, fontSize: 17)),
                    Text('SOP §4.2 Authorized Fleet Billing', style: TextStyle(color: Color(0xFF004D3D), fontSize: 12, fontWeight: FontWeight.bold)),
                  ],
                ),
              ),
            ],
          ),
          const SizedBox(height: AppTheme.gap16),
          Text(
            'Enter the unique company code provided by your organization’s fleet manager or procurement department.',
            style: theme.textTheme.bodyMedium?.copyWith(
              color: theme.colorScheme.onSurfaceVariant,
              height: 1.4,
            ),
          ),
          const SizedBox(height: AppTheme.gap16),

          // Quick Code Suggestions
          Wrap(
            spacing: 8,
            runSpacing: 8,
            children: [
              _QuickCodeChip(
                code: 'NORTHWIND',
                label: 'Northwind Logistics Ltd.',
                onSelected: () => setState(() => _code.text = 'NORTHWIND'),
              ),
            ],
          ),

          const SizedBox(height: AppTheme.gap14),

          TextField(
            controller: _code,
            autofocus: true,
            textCapitalization: TextCapitalization.characters,
            decoration: InputDecoration(
              labelText: 'Company Access Code',
              hintText: 'e.g. NORTHWIND',
              prefixIcon: const Icon(Icons.tag_rounded),
              errorText: _error,
            ),
            onSubmitted: (_) => _join(),
          ),

          const SizedBox(height: AppTheme.gap20),

          FilledButton(
            onPressed: _loading ? null : _join,
            style: FilledButton.styleFrom(
              backgroundColor: const Color(0xFF004D3D),
              foregroundColor: Colors.white,
              padding: const EdgeInsets.symmetric(vertical: 16),
              shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(16)),
            ),
            child: _loading
                ? const SizedBox.square(dimension: 22, child: CircularProgressIndicator(strokeWidth: 2.5, color: Colors.white))
                : const Text('Verify & Link Account', style: TextStyle(fontWeight: FontWeight.w800, fontSize: 15)),
          ),
        ],
      ),
    );
  }
}

class _QuickCodeChip extends StatelessWidget {
  const _QuickCodeChip({required this.code, required this.label, required this.onSelected});

  final String code;
  final String label;
  final VoidCallback onSelected;

  @override
  Widget build(BuildContext context) {
    return ActionChip(
      avatar: const Icon(Icons.bolt_rounded, size: 16, color: Color(0xFF004D3D)),
      label: Text('$code ($label)', style: const TextStyle(fontSize: 12, fontWeight: FontWeight.bold)),
      onPressed: onSelected,
      backgroundColor: const Color(0xFF004D3D).withValues(alpha: 0.08),
      side: const BorderSide(color: Color(0xFF004D3D), width: 0.8),
      shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(20)),
    );
  }
}
