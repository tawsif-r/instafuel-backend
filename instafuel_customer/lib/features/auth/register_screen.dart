import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import '../../core/theme.dart';
import '../../core/widgets/responsive_center.dart';
import '../../models/user.dart';
import 'auth_controller.dart';
import 'auth_widgets.dart';

class RegisterScreen extends ConsumerStatefulWidget {
  const RegisterScreen({super.key});

  @override
  ConsumerState<RegisterScreen> createState() => _RegisterScreenState();
}

class _RegisterScreenState extends ConsumerState<RegisterScreen> {
  final _formKey = GlobalKey<FormState>();
  final _name = TextEditingController();
  final _email = TextEditingController();
  final _phone = TextEditingController();
  final _company = TextEditingController();
  final _password = TextEditingController();
  final _confirm = TextEditingController();
  AccountType _accountType = AccountType.individual;
  bool _acceptedTerms = false;
  bool _loading = false;
  String? _error;

  @override
  void dispose() {
    for (final c in [_name, _email, _phone, _company, _password, _confirm]) {
      c.dispose();
    }
    super.dispose();
  }

  Future<void> _submit() async {
    if (!_formKey.currentState!.validate()) return;
    if (!_acceptedTerms) {
      setState(() => _error = 'Please accept the terms to continue.');
      return;
    }
    setState(() {
      _loading = true;
      _error = null;
    });
    try {
      await ref.read(authControllerProvider.notifier).register(RegisterRequest(
            fullName: _name.text,
            email: _email.text,
            phone: _phone.text.trim(),
            password: _password.text,
            accountType: _accountType,
            companyName: _accountType == AccountType.corporate ? _company.text.trim() : null,
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
    const gap = SizedBox(height: AppTheme.gap16);

    return Scaffold(
      appBar: AppBar(
        leading: IconButton(
          tooltip: 'Back to login',
          icon: const Icon(Icons.arrow_back),
          onPressed: () => context.go('/login'),
        ),
      ),
      body: SafeArea(
        child: SingleChildScrollView(
          padding: const EdgeInsets.symmetric(horizontal: AppTheme.gap24, vertical: AppTheme.gap8),
          child: ResponsiveCenter(
            maxWidth: 440,
            child: Form(
              key: _formKey,
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.stretch,
                children: [
                  const AuthHeader(
                    title: 'Create account',
                    subtitle: 'Fuel delivered to your vehicles, on your schedule.',
                  ),
                  SegmentedButton<AccountType>(
                    segments: const [
                      ButtonSegment(
                        value: AccountType.individual,
                        label: Text('Individual'),
                        icon: Icon(Icons.person_outline),
                      ),
                      ButtonSegment(
                        value: AccountType.corporate,
                        label: Text('Corporate'),
                        icon: Icon(Icons.business_outlined),
                      ),
                    ],
                    selected: {_accountType},
                    onSelectionChanged: (s) => setState(() => _accountType = s.first),
                  ),
                  gap,
                  TextFormField(
                    controller: _name,
                    textCapitalization: TextCapitalization.words,
                    textInputAction: TextInputAction.next,
                    autofillHints: const [AutofillHints.name],
                    decoration: const InputDecoration(labelText: 'Full name', prefixIcon: Icon(Icons.badge_outlined)),
                    validator: (v) => requiredField(v, 'Full name'),
                  ),
                  // Animated so the form doesn't jump when switching account type.
                  AnimatedSize(
                    duration: const Duration(milliseconds: 200),
                    child: _accountType == AccountType.corporate
                        ? Padding(
                            padding: const EdgeInsets.only(top: AppTheme.gap16),
                            child: TextFormField(
                              controller: _company,
                              textCapitalization: TextCapitalization.words,
                              textInputAction: TextInputAction.next,
                              decoration: const InputDecoration(
                                labelText: 'Company name',
                                prefixIcon: Icon(Icons.business_outlined),
                                helperText: 'Your company will need to approve the link.',
                              ),
                              validator: (v) => requiredField(v, 'Company name'),
                            ),
                          )
                        : const SizedBox(width: double.infinity),
                  ),
                  gap,
                  TextFormField(
                    controller: _email,
                    keyboardType: TextInputType.emailAddress,
                    textInputAction: TextInputAction.next,
                    autofillHints: const [AutofillHints.email],
                    decoration: const InputDecoration(labelText: 'Email', prefixIcon: Icon(Icons.mail_outline)),
                    validator: validateEmail,
                  ),
                  gap,
                  TextFormField(
                    controller: _phone,
                    keyboardType: TextInputType.phone,
                    textInputAction: TextInputAction.next,
                    autofillHints: const [AutofillHints.telephoneNumber],
                    decoration: const InputDecoration(
                      labelText: 'Mobile number',
                      hintText: '01XXXXXXXXX',
                      prefixIcon: Icon(Icons.phone_outlined),
                    ),
                    validator: validatePhone,
                  ),
                  gap,
                  PasswordField(controller: _password, validator: validatePassword),
                  gap,
                  PasswordField(
                    controller: _confirm,
                    label: 'Confirm password',
                    textInputAction: TextInputAction.done,
                    validator: (v) => v != _password.text ? 'Passwords do not match' : null,
                  ),
                  const SizedBox(height: AppTheme.gap8),
                  CheckboxListTile(
                    value: _acceptedTerms,
                    onChanged: (v) => setState(() => _acceptedTerms = v ?? false),
                    contentPadding: EdgeInsets.zero,
                    controlAffinity: ListTileControlAffinity.leading,
                    title: const Text('I agree to the Terms of Service and Privacy Policy'),
                  ),
                  if (_error != null) ...[
                    Text(_error!, style: TextStyle(color: theme.colorScheme.error)),
                    const SizedBox(height: AppTheme.gap12),
                  ],
                  FilledButton(
                    onPressed: _loading ? null : _submit,
                    child: _loading
                        ? const SizedBox.square(dimension: 22, child: CircularProgressIndicator(strokeWidth: 2.5))
                        : const Text('Create account'),
                  ),
                  Wrap(
                    alignment: WrapAlignment.center,
                    crossAxisAlignment: WrapCrossAlignment.center,
                    children: [
                      const Text('Already have an account?'),
                      TextButton(onPressed: () => context.go('/login'), child: const Text('Log in')),
                    ],
                  ),
                  const SizedBox(height: AppTheme.gap24),
                ],
              ),
            ),
          ),
        ),
      ),
    );
  }
}
