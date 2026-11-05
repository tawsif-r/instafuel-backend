import 'package:flutter/material.dart';

import '../../core/theme.dart';

/// Logo + title block shared by login and register.
class AuthHeader extends StatelessWidget {
  const AuthHeader({super.key, required this.title, required this.subtitle});

  final String title;
  final String subtitle;

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Container(
          padding: const EdgeInsets.all(AppTheme.gap12),
          decoration: BoxDecoration(
            color: theme.colorScheme.primaryContainer,
            borderRadius: BorderRadius.circular(AppTheme.radius),
          ),
          child: Icon(Icons.local_gas_station, size: 32, color: theme.colorScheme.onPrimaryContainer),
        ),
        const SizedBox(height: AppTheme.gap24),
        Text(title, style: theme.textTheme.headlineMedium?.copyWith(fontWeight: FontWeight.w700)),
        const SizedBox(height: AppTheme.gap8),
        Text(subtitle, style: theme.textTheme.bodyLarge?.copyWith(color: theme.colorScheme.onSurfaceVariant)),
        const SizedBox(height: AppTheme.gap32),
      ],
    );
  }
}

/// Password field with a show/hide toggle.
class PasswordField extends StatefulWidget {
  const PasswordField({
    super.key,
    required this.controller,
    this.label = 'Password',
    this.validator,
    this.textInputAction = TextInputAction.next,
    this.onSubmitted,
  });

  final TextEditingController controller;
  final String label;
  final FormFieldValidator<String>? validator;
  final TextInputAction textInputAction;
  final ValueChanged<String>? onSubmitted;

  @override
  State<PasswordField> createState() => _PasswordFieldState();
}

class _PasswordFieldState extends State<PasswordField> {
  bool _hidden = true;

  @override
  Widget build(BuildContext context) {
    return TextFormField(
      controller: widget.controller,
      obscureText: _hidden,
      textInputAction: widget.textInputAction,
      onFieldSubmitted: widget.onSubmitted,
      autofillHints: const [AutofillHints.password],
      decoration: InputDecoration(
        labelText: widget.label,
        prefixIcon: const Icon(Icons.lock_outline),
        suffixIcon: IconButton(
          tooltip: _hidden ? 'Show password' : 'Hide password',
          icon: Icon(_hidden ? Icons.visibility_outlined : Icons.visibility_off_outlined),
          onPressed: () => setState(() => _hidden = !_hidden),
        ),
      ),
      validator: widget.validator,
    );
  }
}

// Simple form validators shared by the auth screens.
String? requiredField(String? v, String name) => (v == null || v.trim().isEmpty) ? '$name is required' : null;

String? validateEmail(String? v) {
  if (v == null || v.trim().isEmpty) return 'Email is required';
  return RegExp(r'^[^@\s]+@[^@\s]+\.[^@\s]+$').hasMatch(v.trim()) ? null : 'Enter a valid email';
}

// Bangladeshi mobile number: 11 digits starting with 01.
String? validatePhone(String? v) {
  if (v == null || v.trim().isEmpty) return 'Phone is required';
  return RegExp(r'^01\d{9}$').hasMatch(v.trim()) ? null : 'Enter an 11-digit number starting with 01';
}

String? validatePassword(String? v) {
  if (v == null || v.isEmpty) return 'Password is required';
  return v.length < 8 ? 'Use at least 8 characters' : null;
}
