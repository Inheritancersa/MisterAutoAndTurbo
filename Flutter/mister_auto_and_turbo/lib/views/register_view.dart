import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import '../models/customer.dart';
import '../routes/route_manager.dart';
import '../theme.dart';
import '../utils/helpers.dart';
import '../viewmodels/auth_viewmodel.dart';
import '../widgets/common.dart';

/// Register Phone (3:48)
class RegisterView extends StatefulWidget {
  const RegisterView({super.key});
  @override
  State<RegisterView> createState() => _RegisterViewState();
}

class _RegisterViewState extends State<RegisterView> {
  final _form = GlobalKey<FormState>();
  final _name = TextEditingController();
  final _email = TextEditingController();
  final _phone = TextEditingController();
  final _pass = TextEditingController();

  @override
  void dispose() {
    for (final c in [_name, _email, _phone, _pass]) {
      c.dispose();
    }
    super.dispose();
  }

  Future<void> _submit() async {
    if (!_form.currentState!.validate()) return;
    final email = _email.text.trim();
    final error = await context.read<AuthViewModel>().register(
        Customer(
            id: 'c${DateTime.now().millisecondsSinceEpoch}',
            fullName: _name.text.trim(),
            email: email,
            phone: _phone.text.trim()),
        _pass.text);
    if (!mounted) return;
    if (error != null) {
      snack(context, error);
      return;
    }
    // Grab the messenger first - this screen is removed by the navigation below.
    final messenger = ScaffoldMessenger.of(context);
    // pushNamedAndRemoveUntil with (r) => false clears the whole stack, then
    // Log In opens with the new email pre-filled (dynamic route argument).
    Navigator.pushNamedAndRemoveUntil(
      context,
      RouteManager.login,
      (route) => false,
      arguments: email,
    );
    messenger.showSnackBar(
        const SnackBar(content: Text('Account created. Please log in.')));
  }

  @override
  Widget build(BuildContext context) => Scaffold(
        body: SingleChildScrollView(
          child: Form(
            key: _form,
            child: Column(crossAxisAlignment: CrossAxisAlignment.stretch, children: [
              const BrandHeader(),
              Padding(
                padding: const EdgeInsets.fromLTRB(16, 24, 16, 24),
                child: Column(crossAxisAlignment: CrossAxisAlignment.start, children: [
                  GestureDetector(
                    onTap: () => Navigator.pushNamedAndRemoveUntil(
                        context, RouteManager.login, (route) => false),
                    child: Row(mainAxisSize: MainAxisSize.min, children: [
                      const Icon(Icons.arrow_back, size: 22),
                      const SizedBox(width: 6),
                      Text('Back to Log In', style: AppText.plex(16)),
                    ]),
                  ),
                  const SizedBox(height: 16),
                  Text('Create Your Account.', style: AppText.inter(24)),
                  const SizedBox(height: 16),
                  LabeledField(label: 'Full Name', controller: _name, icon: Icons.person, validator: requiredField),
                  LabeledField(
                    label: 'Email Address',
                    controller: _email,
                    icon: Icons.mail,
                    keyboard: TextInputType.emailAddress,
                    validator: emailField,
                  ),
                  LabeledField(
                    label: 'Phone Number',
                    controller: _phone,
                    icon: Icons.phone,
                    keyboard: TextInputType.number,
                    inputFormatters: [digitsOnlyFormatter],
                    validator: phoneField,
                  ),
                  LabeledField(
                    label: 'Password',
                    controller: _pass,
                    icon: Icons.lock,
                    obscure: true,
                    validator: (v) => (v == null || v.length < 6) ? 'At least 6 characters' : null,
                  ),
                  const SizedBox(height: 8),
                  WideButton('Register An Account', onPressed: _submit),
                ]),
              ),
            ]),
          ),
        ),
      );
}
