import 'package:flutter/material.dart';
import '../theme.dart';
import '../widgets/common.dart';

/// Contact Us (147:80). Workshop contact details. Static content, so this is
/// a plain StatelessWidget with no ViewModel.
/// Background uses the workshop "Place" photo.
class ContactView extends StatelessWidget {
  const ContactView({super.key});

  @override
  Widget build(BuildContext context) {
    final l = AppText.plex(18, w: FontWeight.w600, c: Colors.black);
    return Scaffold(
      body: Stack(
        fit: StackFit.expand,
        children: [
          // Workshop photo as full-screen background
          Image.asset(
            'assets/images/place.jpg',
            fit: BoxFit.cover,
            errorBuilder: (_, __, ___) => Container(color: Colors.grey.shade300),
          ),
          // Dark overlay so text stays readable
          Container(color: Colors.black.withOpacity(0.45)),
          SafeArea(
            bottom: false,
            child: Column(
              children: [
                Expanded(
                  child: ListView(
                    padding: const EdgeInsets.fromLTRB(19, 8, 19, 24),
                    children: [
                      const Row(
                        children: [
                          Expanded(child: SizedBox.shrink()),
                          BackArrow(color: Colors.white),
                        ],
                      ),
                      Padding(
                        padding: const EdgeInsets.only(top: 2, bottom: 14),
                        child: Text(
                          'Contact Us.',
                          style: AppText.inter(24, c: Colors.white),
                        ),
                      ),
                      Center(
                        child: Text(
                          'Quality Repairs. Trusted Service.',
                          style: AppText.inter(20, w: FontWeight.w700, c: Colors.white),
                        ),
                      ),
                      const SizedBox(height: 20),
                      const _ContactRow('Cell Workshop', Icons.phone, '073 002 4599'),
                      const _ContactRow(
                          'Email Workshop', Icons.mail, 'tsotetsimotlokoa@yahoo.com'),
                      const _ContactRow(
                        'Location',
                        Icons.location_on,
                        'No. 5 128 Constantia Road\nDagbreek\nWelkom, 9459\nFree State Province',
                      ),
                      InputBox(
                        fill: Colors.white.withOpacity(0.92),
                        child: Column(
                          crossAxisAlignment: CrossAxisAlignment.start,
                          children: [
                            Text('Workshop Opening Hours',
                                style: AppText.plex(20, c: Colors.black)),
                            const SizedBox(height: 6),
                            Text('Mon - Fri 09:00 - 17:00', style: l),
                            Text('Sat- 09:00 - 13:00', style: l),
                          ],
                        ),
                      ),
                    ],
                  ),
                ),
                const BrandFooter(),
              ],
            ),
          ),
        ],
      ),
    );
  }
}

/// A label with an icon + value box underneath.
class _ContactRow extends StatelessWidget {
  final String label;
  final IconData icon;
  final String value;
  const _ContactRow(this.label, this.icon, this.value);

  @override
  Widget build(BuildContext context) => Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Text(label, style: AppText.plex(18, w: FontWeight.w600, c: Colors.white)),
          const SizedBox(height: 8),
          SizedBox(
            width: double.infinity,
            child: InputBox(
              fill: Colors.white.withOpacity(0.92),
              child: Row(children: [
                Icon(icon, size: 30),
                const SizedBox(width: 14),
                Expanded(
                    child: Text(value,
                        style: AppText.inter(16, w: FontWeight.w500, c: Colors.black))),
              ]),
            ),
          ),
          const SizedBox(height: 18),
        ],
      );
}
