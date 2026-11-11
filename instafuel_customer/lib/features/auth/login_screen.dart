import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import '../../core/theme.dart';
import '../../core/widgets/responsive_center.dart';
import 'auth_controller.dart';
import 'auth_widgets.dart';

class LoginScreen extends ConsumerStatefulWidget {
  const LoginScreen({super.key});

  @override
  ConsumerState<LoginScreen> createState() => _LoginScreenState();
}

class _LoginScreenState extends ConsumerState<LoginScreen> {
  final _formKey = GlobalKey<FormState>();
  final _identifier = TextEditingController();
  final _password = TextEditingController();
  bool _loading = false;
  String? _error;

  @override
  void dispose() {
    _identifier.dispose();
    _password.dispose();
    super.dispose();
  }

  Future<void> _submit() async {
    if (!_formKey.currentState!.validate()) return;
    setState(() {
      _loading = true;
      _error = null;
    });
    try {
      await ref.read(authControllerProvider.notifier).login(_identifier.text, _password.text);
    } catch (e) {
      if (mounted) setState(() => _error = e.toString());
    } finally {
      if (mounted) setState(() => _loading = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);

    return Scaffold(
      body: SafeArea(
        child: SingleChildScrollView(
          padding: const EdgeInsets.symmetric(horizontal: AppTheme.gap24, vertical: AppTheme.gap16),
          child: ResponsiveCenter(
            maxWidth: 440,
            child: Form(
              key: _formKey,
              child: AutofillGroup(
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.stretch,
                  children: [
                    const SizedBox(height: AppTheme.gap24),
                    const AuthHeader(
                      title: 'Welcome back',
                      subtitle: 'Sign in to order doorstep Octane, Petrol & Diesel for your vehicles and generators.',
                    ),
                    TextFormField(
                      controller: _identifier,
                      keyboardType: TextInputType.emailAddress,
                      textInputAction: TextInputAction.next,
                      autofillHints: const [AutofillHints.email, AutofillHints.telephoneNumber],
                      decoration: const InputDecoration(
                        labelText: 'Email or Mobile Number',
                        prefixIcon: Icon(Icons.person_outline_rounded),
                        hintText: 'e.g. 01700000000 or user@example.com',
                      ),
                      validator: (v) => v == null || v.trim().isEmpty ? 'Enter your email or phone number' : null,
                    ),
                    const SizedBox(height: AppTheme.gap16),
                    PasswordField(
                      controller: _password,
                      onSubmitted: (_) => _submit(),
                      validator: (v) => v == null || v.isEmpty ? 'Enter your password' : null,
                    ),
                    if (_error != null) ...[
                      const SizedBox(height: AppTheme.gap12),
                      Container(
                        padding: const EdgeInsets.all(AppTheme.gap12),
                        decoration: BoxDecoration(
                          color: theme.colorScheme.errorContainer.withValues(alpha: 0.5),
                          borderRadius: BorderRadius.circular(12),
                          border: Border.all(color: theme.colorScheme.error.withValues(alpha: 0.3)),
                        ),
                        child: Row(
                          children: [
                            Icon(Icons.error_outline_rounded, color: theme.colorScheme.error, size: 20),
                            const SizedBox(width: 8),
                            Expanded(
                              child: Text(
                                _error!,
                                style: TextStyle(color: theme.colorScheme.error, fontSize: 13, fontWeight: FontWeight.w600),
                              ),
                            ),
                          ],
                        ),
                      ),
                    ],
                    const SizedBox(height: AppTheme.gap24),
                    FilledButton(
                      onPressed: _loading ? null : _submit,
                      style: FilledButton.styleFrom(
                        backgroundColor: const Color(0xFF059669),
                        foregroundColor: Colors.white,
                        padding: const EdgeInsets.symmetric(vertical: 14),
                        shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(14)),
                      ),
                      child: _loading
                          ? const SizedBox.square(dimension: 20, child: CircularProgressIndicator(strokeWidth: 2, color: Colors.white))
                          : const Text('Sign In', style: TextStyle(fontWeight: FontWeight.w800, fontSize: 15)),
                    ),
                    const SizedBox(height: AppTheme.gap24),
                    Row(
                      mainAxisAlignment: MainAxisAlignment.center,
                      children: [
                        Flexible(child: Text("Don't have an account?", style: theme.textTheme.bodyMedium, overflow: TextOverflow.ellipsis)),
                        TextButton(
                          onPressed: () => context.go('/register'),
                          child: const Text('Register Now', style: TextStyle(fontWeight: FontWeight.bold, color: Color(0xFF059669))),
                        ),
                      ],
                    ),

                    const SizedBox(height: AppTheme.gap32),
                    // SOP Compliance Footer
                    Center(
                      child: Column(
                        children: [
                          const Row(
                            mainAxisAlignment: MainAxisAlignment.center,
                            children: [
                              Icon(Icons.shield_outlined, size: 14, color: Colors.grey),
                              SizedBox(width: 4),
                              Text(
                                'PESO COMPLIANT & LICENSED FUEL OPERATOR',
                                style: TextStyle(fontSize: 10, fontWeight: FontWeight.bold, color: Colors.grey, letterSpacing: 0.5),
                              ),
                            ],
                          ),
                          const SizedBox(height: 2),
                          Text(
                            'SOP-OPS-001 Certified · Minimum 24h Lead Time Required',
                            style: TextStyle(fontSize: 10, color: Colors.grey.withValues(alpha: 0.8)),
                          ),
                        ],
                      ),
                    ),
                  ],
                ),
              ),
            ),
          ),
        ),
      ),
    );
  }
}
