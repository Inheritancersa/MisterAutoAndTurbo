import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import '../theme.dart';
import '../utils/helpers.dart';
import '../viewmodels/auth_viewmodel.dart';
import '../widgets/common.dart';

/// Edit profile: name, phone (digits only), address.
class EditProfileView extends StatefulWidget {
  const EditProfileView({super.key});

  @override
  State<EditProfileView> createState() => _EditProfileViewState();
}

class _EditProfileViewState extends State<EditProfileView> {
  final _form = GlobalKey<FormState>();
  late final TextEditingController _name;
  late final TextEditingController _phone;
  late final TextEditingController _address;
  late final TextEditingController _email;
  final _password = TextEditingController();
  final _confirm = TextEditingController();

  @override
  void initState() {
    super.initState();
    final c = context.read<AuthViewModel>().customer;
    _name = TextEditingController(text: c?.fullName ?? '');
    _phone = TextEditingController(text: c?.phone ?? '');
    _address = TextEditingController(text: c?.address ?? '');
    _email = TextEditingController(text: c?.email ?? '');
  }

  @override
  void dispose() {
    for (final c in [_name, _phone, _address, _email, _password, _confirm]) {
      c.dispose();
    }
    super.dispose();
  }

  Future<void> _save() async {
    if (!_form.currentState!.validate()) return;
    final err = await context.read<AuthViewModel>().updateProfile(
          fullName: _name.text.trim(),
          phone: _phone.text.trim(),
          address: _address.text.trim(),
          email: _email.text.trim(),
          newPassword: _password.text,
        );
    if (!mounted) return;
    if (err != null) {
      snack(context, err);
      return;
    }
    snack(context, 'Profile updated');
    Navigator.pop(context);
  }

  @override
  Widget build(BuildContext context) {
    final label = AppText.inter(18, w: FontWeight.w700, c: Colors.black);
    final busy = context.watch<AuthViewModel>().isBusy;
    return AppScreen(
      title: 'Edit Profile.',
      children: [
        Form(
          key: _form,
          child: Column(
            children: [
              LabeledField(
                label: 'Full Name',
                labelStyle: label,
                controller: _name,
                icon: Icons.person,
                validator: requiredField,
              ),
              LabeledField(
                label: 'Email',
                labelStyle: label,
                controller: _email,
                icon: Icons.mail,
                keyboard: TextInputType.emailAddress,
                validator: (v) {
                  final t = (v ?? '').trim();
                  if (t.isEmpty) return 'Required';
                  if (!RegExp(r'^[^@\s]+@[^@\s]+\.[^@\s]+$').hasMatch(t)) {
                    return 'Enter a valid email';
                  }
                  return null;
                },
              ),
              LabeledField(
                label: 'Phone Number',
                labelStyle: label,
                controller: _phone,
                icon: Icons.phone,
                keyboard: TextInputType.number,
                inputFormatters: [digitsOnlyFormatter],
                validator: phoneField,
              ),
              LabeledField(
                label: 'Address',
                labelStyle: label,
                controller: _address,
                icon: Icons.home,
                maxLines: 3,
                validator: requiredField,
              ),
              LabeledField(
                label: 'New Password (leave blank to keep)',
                labelStyle: label,
                controller: _password,
                icon: Icons.lock,
                obscure: true,
                validator: (v) => (v == null || v.isEmpty || v.length >= 6)
                    ? null
                    : 'At least 6 characters',
              ),
              LabeledField(
                label: 'Confirm New Password',
                labelStyle: label,
                controller: _confirm,
                icon: Icons.lock_outline,
                obscure: true,
                validator: (v) =>
                    v == _password.text ? null : 'Passwords do not match',
              ),
              const SizedBox(height: 12),
              WideButton(
                busy ? 'Saving…' : 'Save Profile',
                onPressed: busy ? null : _save,
              ),
            ],
          ),
        ),
      ],
    );
  }
}
