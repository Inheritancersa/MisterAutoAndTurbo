import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import '../routes/route_manager.dart';
import '../theme.dart';
import '../utils/helpers.dart';
import '../viewmodels/login_viewmodel.dart';
import '../widgets/common.dart';

/// LogIn Phone (4:259)
///
/// Stateful only to own the text controllers and the form key. The error
/// message lives in [LoginViewModel].
class LoginView extends StatefulWidget {
  final String? initialEmail;
  const LoginView({super.key, this.initialEmail});
  @override
  State<LoginView> createState() => _LoginViewState();
}

class _LoginViewState extends State<LoginView> {
  final _form = GlobalKey<FormState>();
  late final TextEditingController _email =
      TextEditingController(text: widget.initialEmail ?? '');
  final _pass = TextEditingController();

  @override
  void dispose() {
    _email.dispose();
    _pass.dispose();
    super.dispose();
  }

  Future<void> _login() async {
    final vm = context.read<LoginViewModel>();
    vm.clearError();
    if (!_form.currentState!.validate()) return;
    final ok = await vm.login(_email.text, _pass.text);
    if (!mounted) return;
    if (ok) {
      // pushReplacementNamed: Log In is replaced, so Back never returns to it.
      Navigator.pushReplacementNamed(context, RouteManager.dashboard);
    }
  }

  @override
  Widget build(BuildContext context) => Scaffold(
        body: SingleChildScrollView(
          child: Form(
            key: _form,
            child: Column(crossAxisAlignment: CrossAxisAlignment.stretch, children: [
              const BrandHeader(),
              Padding(
                padding: const EdgeInsets.fromLTRB(19, 24, 19, 24),
                child: Column(crossAxisAlignment: CrossAxisAlignment.start, children: [
                  Center(
                    child: Text('Welcome To Mister Auto & Turbo',
                        textAlign: TextAlign.center, style: AppText.inter(24, w: FontWeight.w800)),
                  ),
                  const SizedBox(height: 10),
                  Center(
                    child: Text('Quality Repairs. Trusted Service.',
                        style: AppText.inter(18, w: FontWeight.w800)),
                  ),
                  const SizedBox(height: 24),
                  LabeledField(
                    label: 'Email',
                    labelStyle: AppText.plex(18, c: AppColors.label),
                    controller: _email,
                    icon: Icons.mail,
                    keyboard: TextInputType.emailAddress,
                    validator: emailField,
                  ),
                  LabeledField(
                    label: 'Password',
                    labelStyle: AppText.plex(18, c: AppColors.label),
                    controller: _pass,
                    icon: Icons.lock,
                    obscure: true,
                    validator: requiredField,
                  ),
                  // Only this banner rebuilds when the error changes.
                  Selector<LoginViewModel, String?>(
                    selector: (_, vm) => vm.error,
                    builder: (_, error, __) =>
                        error == null ? const SizedBox.shrink() : ErrorBanner(error),
                  ),
                  GestureDetector(
                    onTap: () => Navigator.pushNamed(context, RouteManager.forgotPassword),
                    child: Text('Forgot Password?', style: AppText.plex(18, c: AppColors.warn)),
                  ),
                  const SizedBox(height: 24),
                  InputBox(
                    onTap: _login,
                    fill: AppColors.loginDisabled,
                    height: 60,
                    child: Center(child: Text('Log In', style: AppText.plex(18))),
                  ),
                  const SizedBox(height: 28),
                  InputBox(
                    height: 60,
                    onTap: () => Navigator.pushNamed(context, RouteManager.register),
                    child: Center(child: Text('Register', style: AppText.plex(18))),
                  ),
                ]),
              ),
            ]),
          ),
        ),
      );
}
