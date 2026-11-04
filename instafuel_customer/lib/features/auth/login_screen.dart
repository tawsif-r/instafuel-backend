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
      // The router redirects to /home once the user is set.
    } catch (e) {
      if (mounted) setState(() => _error = e.toString());
    } finally {
      if (mounted) setState(() => _loading = false);
    }
  }

  void _fillDemo() {
    _identifier.text = 'demo@instafuel.app';
    _password.text = 'password123';
  }

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    return Scaffold(
      body: SafeArea(
        child: SingleChildScrollView(
          padding: const EdgeInsets.all(AppTheme.gap24),
          child: ResponsiveCenter(
            maxWidth: 440,
            child: Form(
              key: _formKey,
              child: AutofillGroup(
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.stretch,
                  children: [
                    const SizedBox(height: AppTheme.gap32),
                    const AuthHeader(
                      title: 'Welcome back',
                      subtitle: 'Log in to order fuel and track your deliveries.',
                    ),
                    TextFormField(
                      controller: _identifier,
                      keyboardType: TextInputType.emailAddress,
                      textInputAction: TextInputAction.next,
                      autofillHints: const [AutofillHints.email, AutofillHints.telephoneNumber],
                      decoration: const InputDecoration(
                        labelText: 'Email or phone',
                        prefixIcon: Icon(Icons.person_outline),
                      ),
                      validator: (v) => requiredField(v, 'Email or phone'),
                    ),
                    const SizedBox(height: AppTheme.gap16),
                    PasswordField(
                      controller: _password,
                      textInputAction: TextInputAction.done,
                      onSubmitted: (_) => _submit(),
                      validator: (v) => requiredField(v, 'Password'),
                    ),
                    Align(
                      alignment: Alignment.centerRight,
                      child: TextButton(
                        onPressed: () => ScaffoldMessenger.of(context).showSnackBar(
                          const SnackBar(content: Text('Password reset is coming soon.')),
                        ),
                        child: const Text('Forgot password?'),
                      ),
                    ),
                    if (_error != null) ...[
                      Text(_error!, style: TextStyle(color: theme.colorScheme.error)),
                      const SizedBox(height: AppTheme.gap12),
                    ],
                    FilledButton(
                      onPressed: _loading ? null : _submit,
                      child: _loading
                          ? const SizedBox.square(dimension: 22, child: CircularProgressIndicator(strokeWidth: 2.5))
                          : const Text('Log in'),
                    ),
                    const SizedBox(height: AppTheme.gap16),
                    Row(
                      mainAxisAlignment: MainAxisAlignment.center,
                      children: [
                        const Text("Don't have an account?"),
                        TextButton(onPressed: () => context.go('/register'), child: const Text('Sign up')),
                      ],
                    ),
                    const SizedBox(height: AppTheme.gap24),
                    // Demo helper; remove when real auth is connected.
                    Card(
                      child: ListTile(
                        leading: const Icon(Icons.info_outline),
                        title: const Text('Demo account'),
                        subtitle: const Text('demo@instafuel.app · password123'),
                        trailing: TextButton(onPressed: _fillDemo, child: const Text('Use')),
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
