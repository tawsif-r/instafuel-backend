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
      setState(() => _error = 'Please accept the fuel safety terms & operating procedures.');
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
      body: SafeArea(
        child: SingleChildScrollView(
          padding: const EdgeInsets.symmetric(horizontal: AppTheme.gap24, vertical: AppTheme.gap16),
          child: ResponsiveCenter(
            maxWidth: 480,
            child: Form(
              key: _formKey,
              child: AutofillGroup(
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.stretch,
                  children: [
                    const SizedBox(height: AppTheme.gap16),
                    const AuthHeader(
                      title: 'Create Account',
                      subtitle: 'Onboard as an individual customer or corporate/institutional fleet.',
                    ),

                    // SOP 4.1 & 4.2 Account Type Selector
                    Container(
                      padding: const EdgeInsets.all(4),
                      decoration: BoxDecoration(
                        color: theme.colorScheme.surfaceContainerHighest.withValues(alpha: 0.5),
                        borderRadius: BorderRadius.circular(16),
                      ),
                      child: Row(
                        children: [
                          Expanded(
                            child: InkWell(
                              onTap: () => setState(() => _accountType = AccountType.individual),
                              borderRadius: BorderRadius.circular(12),
                              child: Container(
                                padding: const EdgeInsets.symmetric(vertical: 10, horizontal: 6),
                                decoration: BoxDecoration(
                                  color: _accountType == AccountType.individual
                                      ? (theme.brightness == Brightness.dark ? const Color(0xFF1E293B) : Colors.white)
                                      : Colors.transparent,
                                  borderRadius: BorderRadius.circular(12),
                                  boxShadow: _accountType == AccountType.individual
                                      ? [BoxShadow(color: Colors.black.withValues(alpha: 0.05), blurRadius: 4)]
                                      : null,
                                ),
                                child: Row(
                                  mainAxisAlignment: MainAxisAlignment.center,
                                  children: [
                                    Icon(
                                      Icons.person_rounded,
                                      size: 18,
                                      color: _accountType == AccountType.individual ? const Color(0xFF059669) : Colors.grey,
                                    ),
                                    const SizedBox(width: 6),
                                    Expanded(
                                      child: Text(
                                        'Private Vehicle',
                                        textAlign: TextAlign.center,
                                        maxLines: 1,
                                        overflow: TextOverflow.ellipsis,
                                        style: TextStyle(
                                          fontWeight: FontWeight.bold,
                                          fontSize: 13,
                                          color: _accountType == AccountType.individual ? const Color(0xFF059669) : Colors.grey,
                                        ),
                                      ),
                                    ),
                                  ],
                                ),
                              ),
                            ),
                          ),
                          Expanded(
                            child: InkWell(
                              onTap: () => setState(() => _accountType = AccountType.corporate),
                              borderRadius: BorderRadius.circular(12),
                              child: Container(
                                padding: const EdgeInsets.symmetric(vertical: 10, horizontal: 6),
                                decoration: BoxDecoration(
                                  color: _accountType == AccountType.corporate
                                      ? (theme.brightness == Brightness.dark ? const Color(0xFF1E293B) : Colors.white)
                                      : Colors.transparent,
                                  borderRadius: BorderRadius.circular(12),
                                  boxShadow: _accountType == AccountType.corporate
                                      ? [BoxShadow(color: Colors.black.withValues(alpha: 0.05), blurRadius: 4)]
                                      : null,
                                ),
                                child: Row(
                                  mainAxisAlignment: MainAxisAlignment.center,
                                  children: [
                                    Icon(
                                      Icons.business_rounded,
                                      size: 18,
                                      color: _accountType == AccountType.corporate ? const Color(0xFF059669) : Colors.grey,
                                    ),
                                    const SizedBox(width: 6),
                                    Expanded(
                                      child: Text(
                                        'Corporate / Fleet',
                                        textAlign: TextAlign.center,
                                        maxLines: 1,
                                        overflow: TextOverflow.ellipsis,
                                        style: TextStyle(
                                          fontWeight: FontWeight.bold,
                                          fontSize: 13,
                                          color: _accountType == AccountType.corporate ? const Color(0xFF059669) : Colors.grey,
                                        ),
                                      ),
                                    ),
                                  ],
                                ),
                              ),
                            ),
                          ),
                        ],
                      ),
                    ),
                    const SizedBox(height: AppTheme.gap20),

                    if (_accountType == AccountType.corporate) ...[
                      TextFormField(
                        controller: _company,
                        textInputAction: TextInputAction.next,
                        textCapitalization: TextCapitalization.words,
                        decoration: const InputDecoration(
                          labelText: 'Company / Institution Name',
                          prefixIcon: Icon(Icons.corporate_fare_rounded),
                          hintText: 'e.g. Northwind Logistics Ltd.',
                        ),
                        validator: (v) => _accountType == AccountType.corporate && (v == null || v.trim().isEmpty)
                            ? 'Enter company name for corporate account'
                            : null,
                      ),
                      gap,
                    ],

                    TextFormField(
                      controller: _name,
                      textInputAction: TextInputAction.next,
                      textCapitalization: TextCapitalization.words,
                      autofillHints: const [AutofillHints.name],
                      decoration: InputDecoration(
                        labelText: _accountType == AccountType.corporate ? 'Authorized Representative' : 'Full Name',
                        prefixIcon: const Icon(Icons.person_outline_rounded),
                        hintText: 'e.g. Ayesha Rahman',
                      ),
                      validator: (v) => v == null || v.trim().isEmpty ? 'Enter your name' : null,
                    ),
                    gap,

                    TextFormField(
                      controller: _phone,
                      keyboardType: TextInputType.phone,
                      textInputAction: TextInputAction.next,
                      autofillHints: const [AutofillHints.telephoneNumber],
                      decoration: const InputDecoration(
                        labelText: 'Mobile Number (Required for Delivery Pin)',
                        prefixIcon: Icon(Icons.phone_outlined),
                        hintText: '01XXXXXXXXX',
                      ),
                      validator: (v) {
                        if (v == null || v.trim().isEmpty) return 'Enter your phone number';
                        if (v.trim().length < 10) return 'Enter a valid phone number';
                        return null;
                      },
                    ),
                    gap,

                    TextFormField(
                      controller: _email,
                      keyboardType: TextInputType.emailAddress,
                      textInputAction: TextInputAction.next,
                      autofillHints: const [AutofillHints.email],
                      decoration: const InputDecoration(
                        labelText: 'Email Address',
                        prefixIcon: Icon(Icons.email_outlined),
                      ),
                      validator: (v) {
                        if (v == null || v.trim().isEmpty) return 'Enter an email';
                        if (!v.contains('@') || !v.contains('.')) return 'Enter a valid email';
                        return null;
                      },
                    ),
                    gap,

                    PasswordField(
                      controller: _password,
                      label: 'Create Password (min. 8 characters)',
                      validator: (v) {
                        if (v == null || v.isEmpty) return 'Enter a password';
                        if (v.length < 8) return 'Password must be at least 8 characters';
                        return null;
                      },
                    ),
                    gap,

                    PasswordField(
                      controller: _confirm,
                      label: 'Confirm Password',
                      textInputAction: TextInputAction.done,
                      onSubmitted: (_) => _submit(),
                      validator: (v) => v != _password.text ? 'Passwords do not match' : null,
                    ),
                    gap,

                    // SOP Safety & Policy Agreement
                    CheckboxListTile(
                      value: _acceptedTerms,
                      contentPadding: EdgeInsets.zero,
                      controlAffinity: ListTileControlAffinity.leading,
                      title: const Text(
                        'I accept the Safety Operating Procedures (SOP-OPS-001), 24h lead-time requirement, and PESO safety standards.',
                        style: TextStyle(fontSize: 12),
                      ),
                      onChanged: (v) => setState(() => _acceptedTerms = v ?? false),
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
                        child: Text(
                          _error!,
                          style: TextStyle(color: theme.colorScheme.error, fontSize: 13, fontWeight: FontWeight.w600),
                        ),
                      ),
                    ],

                    const SizedBox(height: AppTheme.gap20),
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
                          : const Text('Complete Onboarding', style: TextStyle(fontWeight: FontWeight.w800, fontSize: 15)),
                    ),

                    const SizedBox(height: AppTheme.gap16),
                    Row(
                      mainAxisAlignment: MainAxisAlignment.center,
                      children: [
                        Flexible(child: Text('Already registered?', style: theme.textTheme.bodyMedium, overflow: TextOverflow.ellipsis)),
                        TextButton(
                          onPressed: () => context.go('/login'),
                          child: const Text('Sign In', style: TextStyle(fontWeight: FontWeight.bold, color: Color(0xFF059669))),
                        ),
                      ],
                    ),
                    const SizedBox(height: AppTheme.gap24),
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
