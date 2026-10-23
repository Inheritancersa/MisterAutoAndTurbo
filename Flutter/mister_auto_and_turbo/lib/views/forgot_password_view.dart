import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import '../routes/route_manager.dart';
import '../theme.dart';
import '../viewmodels/auth_viewmodel.dart';
import '../utils/helpers.dart';
import '../widgets/common.dart';

/// Forgot Password Mob (3:163)
class ForgotPasswordView extends StatefulWidget {
  const ForgotPasswordView({super.key});
  @override
  State<ForgotPasswordView> createState() => _ForgotPasswordViewState();
}

class _ForgotPasswordViewState extends State<ForgotPasswordView> {
  final _email = TextEditingController();

  @override
  void dispose() {
    _email.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) => Scaffold(
        body: SingleChildScrollView(
          child: Column(crossAxisAlignment: CrossAxisAlignment.stretch, children: [
            const BrandHeader(),
            Padding(
              padding: const EdgeInsets.all(16),
              child: Column(crossAxisAlignment: CrossAxisAlignment.start, children: [
                Text('Forgot Password?', style: AppText.inter(24)),
                const SizedBox(height: 14),
                Text(
                  'Enter your registered email address and we will email you a verification code straight away.',
                  style: AppText.plex(18, c: AppColors.label),
                ),
                const SizedBox(height: 24),
                LabeledField(
                    label: 'Email Address',
                    controller: _email,
                    icon: Icons.mail,
                    keyboard: TextInputType.emailAddress),
                const SizedBox(height: 16),
                WideButton('Send Verification Code', onPressed: () async {
                  final email = _email.text.trim();
                  if (email.isEmpty) {
                    snack(context, 'Enter your email address');
                    return;
                  }
                  final err = await context.read<AuthViewModel>().sendPasswordReset(email);
                  if (!context.mounted) return;
                  if (err != null) {
                    snack(context, err);
                    return;
                  }
                  snack(context, 'Verification code sent. Check your email.');
                  Navigator.pushNamed(context, RouteManager.resetPassword, arguments: email);
                }),
                const SizedBox(height: 40),
                Row(children: [
                  Text('Remember your password? ', style: AppText.plex(16, c: AppColors.label)),
                  GestureDetector(
                    // pop removes this screen and returns to Log In.
                    onTap: () => Navigator.maybePop(context),
                    child: Text('Log In', style: AppText.plex(16)),
                  ),
                ]),
              ]),
            ),
          ]),
        ),
      );
}
