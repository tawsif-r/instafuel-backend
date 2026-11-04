import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../core/theme.dart';
import 'corporate_controller.dart';

/// Bottom sheet to request a link with a company using its invite code.
class JoinCompanySheet extends ConsumerStatefulWidget {
  const JoinCompanySheet({super.key});

  static Future<void> show(BuildContext context) => showModalBottomSheet(
        context: context,
        isScrollControlled: true,
        showDragHandle: true,
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
    setState(() {
      _loading = true;
      _error = null;
    });
    try {
      final a = await ref.read(affiliationsProvider.notifier).join(_code.text);
      if (!mounted) return;
      Navigator.pop(context);
      ScaffoldMessenger.of(context).showSnackBar(SnackBar(
        content: Text(a.isActive ? 'Linked to ${a.companyName}' : 'Request sent. Waiting for company approval.'),
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
      // Moves the sheet above the keyboard.
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
          Text('Join a company', style: theme.textTheme.titleLarge),
          const SizedBox(height: AppTheme.gap8),
          Text(
            'Ask your company’s fleet admin for the invite code. Once approved, you can bill orders to the company.',
            style: theme.textTheme.bodyMedium?.copyWith(color: theme.colorScheme.onSurfaceVariant),
          ),
          const SizedBox(height: AppTheme.gap16),
          TextField(
            controller: _code,
            autofocus: true,
            textCapitalization: TextCapitalization.characters,
            decoration: InputDecoration(
              labelText: 'Company code',
              helperText: 'Demo: codes starting with DEMO are approved instantly',
              errorText: _error,
            ),
            onSubmitted: (_) => _join(),
          ),
          const SizedBox(height: AppTheme.gap16),
          FilledButton(
            onPressed: _loading ? null : _join,
            child: _loading
                ? const SizedBox.square(dimension: 22, child: CircularProgressIndicator(strokeWidth: 2.5))
                : const Text('Send request'),
          ),
        ],
      ),
    );
  }
}
