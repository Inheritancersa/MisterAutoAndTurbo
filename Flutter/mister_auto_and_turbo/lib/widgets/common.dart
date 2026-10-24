import 'package:flutter/material.dart';
import 'package:flutter/services.dart';
import 'package:provider/provider.dart';
import '../theme.dart';
import '../viewmodels/auth_viewmodel.dart';

// ============================================================
// SHARED STATELESS WIDGETS
// Small reusable building blocks, extracted so the views stay short.
// ============================================================

/// Blue brand block that sits at the bottom of every inner screen.
class BrandFooter extends StatelessWidget {
  const BrandFooter({super.key});
  @override
  Widget build(BuildContext context) {
    // Hide the footer while the on-screen keyboard is open.
    if (MediaQuery.of(context).viewInsets.bottom > 0) {
      return const SizedBox.shrink();
    }
    return Container(
      width: double.infinity,
      color: AppColors.brand,
      padding: const EdgeInsets.fromLTRB(16, 32, 16, 24),
      child: const _BrandBlock(),
    );
  }
}

/// Splash header used on Login / Register / Forgot password.
class BrandHeader extends StatelessWidget {
  const BrandHeader({super.key});
  @override
  Widget build(BuildContext context) => Container(
        width: double.infinity,
        color: AppColors.brand,
        padding: EdgeInsets.fromLTRB(16, MediaQuery.of(context).padding.top + 24, 16, 28),
        child: const _BrandBlock(),
      );
}

class _BrandBlock extends StatelessWidget {
  const _BrandBlock();
  @override
  Widget build(BuildContext context) => Column(mainAxisSize: MainAxisSize.min, children: [
        const _Logo(),
        const SizedBox(height: 14),
        Text('Mister Auto & Turbo',
            style: AppText.plex(24, c: const Color(0xFFF8FBFF))),
        const SizedBox(height: 4),
        Text(
          'Mister Auto and Turbo Offers  you Trusted  Quality Repairs and Maintainance\non all Models and Make of Cars .',
          textAlign: TextAlign.center,
          style: AppText.inter(12, w: FontWeight.w700, c: const Color(0xFFF6FAFF))
              .copyWith(fontStyle: FontStyle.italic),
        ),
      ]);
}

/// Shows assets/images/logo.png when present; falls back to a text mark so
/// the app still runs before the Figma logo is exported into assets/.
class _Logo extends StatelessWidget {
  const _Logo();
  @override
  Widget build(BuildContext context) => Container(
        width: 156,
        height: 81,
        decoration: BoxDecoration(color: Colors.white, borderRadius: BorderRadius.circular(12)),
        clipBehavior: Clip.antiAlias,
        child: Image.asset('assets/images/logo.png', fit: BoxFit.cover,
            errorBuilder: (_, __, ___) => Center(
                  child: Column(mainAxisSize: MainAxisSize.min, children: [
                    const Icon(Icons.car_repair, color: AppColors.brand, size: 30),
                    Text('Auto&Turbo', style: AppText.inter(16, w: FontWeight.w800, c: AppColors.brand)),
                    Text('Your Car My Passion', style: AppText.inter(9, c: AppColors.link)),
                  ]),
                )),
      );
}

/// Light grey "input-box" card from the Figma file. Tappable when [onTap]
/// is given.
class InputBox extends StatelessWidget {
  final Widget child;
  final VoidCallback? onTap;
  final EdgeInsetsGeometry padding;
  final double? height;
  final Color? fill;
  final Color? borderColor;
  const InputBox({
    super.key,
    required this.child,
    this.onTap,
    this.padding = const EdgeInsets.all(14),
    this.height,
    this.fill,
    this.borderColor,
  });
  @override
  Widget build(BuildContext context) => Material(
        color: fill ?? AppColors.surface,
        shape: RoundedRectangleBorder(
          borderRadius: BorderRadius.circular(8),
          side: BorderSide(color: borderColor ?? AppColors.border),
        ),
        child: InkWell(
          borderRadius: BorderRadius.circular(8),
          onTap: onTap,
          child: Container(height: height, padding: padding, child: child),
        ),
      );
}

/// Full-width rounded card. Used for the blue info cards (history, bookings,
/// vehicle status), the green "success" cards and the cream note card.
class InfoCard extends StatelessWidget {
  final Widget child;
  final Color? color;
  final Color borderColor;
  final EdgeInsetsGeometry padding;
  final double? minHeight;
  const InfoCard({
    super.key,
    required this.child,
    this.color,
    this.borderColor = AppColors.border,
    this.padding = const EdgeInsets.all(14),
    this.minHeight,
  });
  @override
  Widget build(BuildContext context) => Container(
        width: double.infinity,
        constraints: minHeight == null ? null : BoxConstraints(minHeight: minHeight!),
        padding: padding,
        decoration: BoxDecoration(
          color: color,
          borderRadius: BorderRadius.circular(8),
          border: Border.all(color: borderColor),
        ),
        child: child,
      );
}

/// Red error box shown under a form.
class ErrorBanner extends StatelessWidget {
  final String message;
  const ErrorBanner(this.message, {super.key});
  @override
  Widget build(BuildContext context) => Container(
        width: double.infinity,
        padding: const EdgeInsets.all(12),
        margin: const EdgeInsets.only(bottom: 12),
        decoration: BoxDecoration(
          color: const Color(0xFFFDECEC),
          borderRadius: BorderRadius.circular(8),
          border: Border.all(color: Colors.red),
        ),
        child: Row(children: [
          const Icon(Icons.error_outline, color: Colors.red),
          const SizedBox(width: 8),
          Expanded(
            child: Text(message, style: AppText.inter(14, w: FontWeight.w500, c: Colors.red)),
          ),
        ]),
      );
}

/// Filled button. [green] gives the "Accept" style, [outlined] the light style.
class WideButton extends StatelessWidget {
  final String label;
  final VoidCallback? onPressed;
  final bool green;
  final bool outlined;
  const WideButton(this.label, {super.key, this.onPressed, this.green = false, this.outlined = false});
  @override
  Widget build(BuildContext context) {
    final Color bg = green ? AppColors.successBg : (outlined ? AppColors.surface : AppColors.brand);
    final Color fg = green || outlined ? Colors.black : Colors.white;
    return SizedBox(
      width: double.infinity,
      height: 58,
      child: Material(
        color: bg,
        shape: RoundedRectangleBorder(
          borderRadius: BorderRadius.circular(8),
          side: BorderSide(
              color: green ? AppColors.successBorder : (outlined ? AppColors.border : AppColors.brand)),
        ),
        child: InkWell(
          borderRadius: BorderRadius.circular(8),
          onTap: onPressed,
          child: Center(child: Text(label, style: AppText.inter(18, c: fg))),
        ),
      ),
    );
  }
}

class BackArrow extends StatelessWidget {
  final Color color;
  const BackArrow({super.key, this.color = Colors.black});
  @override
  Widget build(BuildContext context) => IconButton(
        tooltip: 'Back',
        iconSize: 34,
        icon: Icon(Icons.reply, color: color),
        onPressed: () => Navigator.of(context).maybePop(),
      );
}

/// Standard inner screen built on [Scaffold]: "Hello, name" + back arrow, a
/// section title, a scrollable body, and the brand footer pinned at the bottom.
///
/// The greeting comes from [AuthViewModel] through a [Selector], so only the
/// greeting text rebuilds when the customer changes. Pass [greeting] to
/// show a fixed heading instead (e.g. "Proof Of Pay").
class AppScreen extends StatelessWidget {
  final String? greeting;
  final String? title;
  final List<Widget> children;
  final bool showBack;
  final Widget? headerTrailing;
  /// Pull-to-refresh callback (e.g. WorkshopViewModel.refresh).
  final Future<void> Function()? onRefresh;
  const AppScreen({
    super.key,
    this.greeting,
    this.title,
    required this.children,
    this.showBack = true,
    this.headerTrailing,
    this.onRefresh,
  });
  @override
  Widget build(BuildContext context) {
    final list = ListView(
      physics: const AlwaysScrollableScrollPhysics(),
      padding: const EdgeInsets.fromLTRB(19, 8, 19, 24),
      children: [
        Row(children: [
          Expanded(child: GreetingText(fixed: greeting)),
          if (showBack) const BackArrow(),
        ]),
        if (title != null)
          Padding(
            padding: const EdgeInsets.only(top: 2, bottom: 14),
            child: Row(children: [
              Expanded(child: Text(title!, style: AppText.inter(24))),
              if (headerTrailing != null) headerTrailing!,
            ]),
          ),
        ...children,
      ],
    );
    return Scaffold(
      body: SafeArea(
        bottom: false,
        child: Column(children: [
          Expanded(
            child: onRefresh != null
                ? RefreshIndicator(onRefresh: onRefresh!, child: list)
                : list,
          ),
          const BrandFooter(),
        ]),
      ),
    );
  }
}

/// Big "Hello, name" heading. Uses [fixed] when given, otherwise listens to
/// the logged-in customer.
class GreetingText extends StatelessWidget {
  final String? fixed;
  const GreetingText({super.key, this.fixed});
  @override
  Widget build(BuildContext context) {
    final fixedText = fixed;
    if (fixedText != null) return Text(fixedText, style: AppText.inter(32));
    return Selector<AuthViewModel, String>(
      selector: (_, auth) => auth.greeting,
      builder: (_, greeting, __) => Text(greeting, style: AppText.inter(32)),
    );
  }
}

/// Label + text field. Stateful only to toggle the password visibility
/// (ephemeral, widget-local state: `setState` is the right tool here).
class LabeledField extends StatefulWidget {
  final String label;
  final TextEditingController controller;
  final IconData? icon;
  final bool obscure;
  final TextInputType? keyboard;
  final int maxLines;
  final String? Function(String?)? validator;
  final TextStyle? labelStyle;
  final List<TextInputFormatter>? inputFormatters;
  final bool readOnly;
  const LabeledField({
    super.key,
    required this.label,
    required this.controller,
    this.icon,
    this.obscure = false,
    this.keyboard,
    this.maxLines = 1,
    this.validator,
    this.labelStyle,
    this.inputFormatters,
    this.readOnly = false,
  });
  @override
  State<LabeledField> createState() => _LabeledFieldState();
}

class _LabeledFieldState extends State<LabeledField> {
  late bool _hidden = widget.obscure;

  @override
  Widget build(BuildContext context) => Padding(
        padding: const EdgeInsets.only(bottom: 14),
        child: Column(crossAxisAlignment: CrossAxisAlignment.start, children: [
          Text(widget.label, style: widget.labelStyle ?? AppText.plex(15, c: AppColors.label)),
          const SizedBox(height: 8),
          TextFormField(
            controller: widget.controller,
            obscureText: _hidden,
            keyboardType: widget.keyboard,
            maxLines: widget.obscure ? 1 : widget.maxLines,
            validator: widget.validator,
            inputFormatters: widget.inputFormatters,
            readOnly: widget.readOnly,
            style: AppText.inter(16, w: FontWeight.w500),
            decoration: fieldDecoration(
              prefixIcon:
                  widget.icon == null ? null : Icon(widget.icon, color: Colors.black, size: 26),
              // Show / hide toggle, only on password fields.
              suffixIcon: widget.obscure
                  ? IconButton(
                      tooltip: _hidden ? 'Show password' : 'Hide password',
                      icon: Icon(_hidden ? Icons.visibility_off : Icons.visibility,
                          color: AppColors.label),
                      onPressed: () => setState(() => _hidden = !_hidden),
                    )
                  : null,
            ),
          ),
        ]),
      );
}

OutlineInputBorder _border(Color c, [double w = 1]) => OutlineInputBorder(
    borderRadius: BorderRadius.circular(8), borderSide: BorderSide(color: c, width: w));

/// The grey rounded input style shared by every text field and dropdown.
InputDecoration fieldDecoration({
  Widget? prefixIcon,
  Widget? suffixIcon,
  String? hintText,
  TextStyle? hintStyle,
}) =>
    InputDecoration(
      prefixIcon: prefixIcon,
      suffixIcon: suffixIcon,
      hintText: hintText,
      hintStyle: hintStyle,
      filled: true,
      fillColor: AppColors.surface,
      contentPadding: const EdgeInsets.all(16),
      enabledBorder: _border(AppColors.border),
      focusedBorder: _border(AppColors.brand, 1.5),
      errorBorder: _border(Colors.red),
      focusedErrorBorder: _border(Colors.red, 1.5),
    );
