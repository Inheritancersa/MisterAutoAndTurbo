import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import '../routes/route_manager.dart';
import '../theme.dart';
import '../utils/helpers.dart';
import '../viewmodels/auth_viewmodel.dart';
import '../widgets/common.dart';

/// Enter the emailed verification code and choose a new password.
class ResetPasswordView extends StatefulWidget {
  final String email;
  const ResetPasswordView({super.key, required this.email});
  @override
  State<ResetPasswordView> createState() => _ResetPasswordViewState();
}

class _ResetPasswordViewState extends State<ResetPasswordView> {
  final _code = TextEditingController();
  final _password = TextEditingController();
  final _confirm = TextEditingController();

  @override
  void dispose() {
    _code.dispose();
    _password.dispose();
    _confirm.dispose();
    super.dispose();
  }

  Future<void> _submit() async {
    if (_code.text.trim().isEmpty) {
      snack(context, 'Enter the verification code from your email');
      return;
    }
    if (_password.text != _confirm.text) {
      snack(context, 'Passwords do not match');
      return;
    }
    final err = await context.read<AuthViewModel>().resetPasswordWithCode(
          email: widget.email,
          code: _code.text,
          newPassword: _password.text,
        );
    if (!mounted) return;
    if (err != null) {
      snack(context, err);
      return;
    }
    snack(context, 'Password updated. Please log in.');
    Navigator.pushNamedAndRemoveUntil(context, RouteManager.login, (r) => false, arguments: widget.email);
  }

  Future<void> _resend() async {
    final err = await context.read<AuthViewModel>().sendPasswordReset(widget.email);
    if (!mounted) return;
    snack(context, err ?? 'A new code has been sent.');
  }

  @override
  Widget build(BuildContext context) => Scaffold(
        body: SingleChildScrollView(
          child: Column(crossAxisAlignment: CrossAxisAlignment.stretch, children: [
            const BrandHeader(),
            Padding(
              padding: const EdgeInsets.all(16),
              child: Column(crossAxisAlignment: CrossAxisAlignment.start, children: [
                Text('Reset Password', style: AppText.inter(24)),
                const SizedBox(height: 14),
                Text('We emailed a verification code to ${widget.email}.',
                    style: AppText.plex(18, c: AppColors.label)),
                const SizedBox(height: 24),
                LabeledField(
                    label: 'Verification Code',
                    controller: _code,
                    icon: Icons.pin,
                    keyboard: TextInputType.number),
                const SizedBox(height: 16),
                LabeledField(label: 'New Password', controller: _password, icon: Icons.lock, obscure: true),
                const SizedBox(height: 16),
                LabeledField(label: 'Confirm Password', controller: _confirm, icon: Icons.lock, obscure: true),
                const SizedBox(height: 16),
                WideButton('Reset Password', onPressed: _submit),
                const SizedBox(height: 24),
                GestureDetector(
                  onTap: _resend,
                  child: Text('Didn\'t get it? Resend code', style: AppText.plex(16)),
                ),
              ]),
            ),
          ]),
        ),
      );
}
