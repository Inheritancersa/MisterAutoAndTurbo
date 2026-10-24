import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import '../theme.dart';
import '../utils/helpers.dart';
import '../viewmodels/workshop_viewmodel.dart';
import '../widgets/common.dart';
import '../models/booking.dart';

/// My Bookings: the bookings saved in [WorkshopViewModel].
/// Opened by the "My Bookings" buttons on the Book A Service screens.
class MyBookingsView extends StatelessWidget {
  const MyBookingsView({super.key});

  @override
  Widget build(BuildContext context) {
    return Consumer<WorkshopViewModel>(
      builder: (context, workshop, _) => AppScreen(
      onRefresh: () => context.read<WorkshopViewModel>().refresh(),
        title: 'Active Bookings.',
        children: [
          // Show a message if no bookings exist.
          if (workshop.bookings.isEmpty)
            Text('You have no active bookings yet.',
                style: AppText.plex(16, c: AppColors.label)),

          // Display each saved booking.
          for (final booking in workshop.bookings) ...[
            InfoCard(
              color: AppColors.brand,
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  // Service name.
                  Text(booking.service.label,
                      style: AppText.inter(24, c: const Color(0xFFFFFAFA))),
                  const SizedBox(height: 4),
                  // Vehicle and registration plate.
                  Text(
                      '${booking.vehicle.displayName} · ${booking.vehicle.plate}',
                      style: AppText.inter(18, c: const Color(0xFFFFFAFA))),
                  const SizedBox(height: 4),
                  // Appointment date and time.
                  Text('${fmtDate(booking.date)} at ${booking.time}',
                      style: AppText.inter(18,
                          w: FontWeight.w500, c: const Color(0xFFFFFAFA))),
                  // Optional customer notes.
                  if (booking.notes.trim().isNotEmpty) ...[
                    const SizedBox(height: 8),
                    Text('Notes: ${booking.notes}',
                        style: AppText.plex(14, c: const Color(0xFFFFFAFA))),
                  ],
                ],
              ),
            ),
            const SizedBox(height: 20),
          ],
        ],
      ),
    );
  }
}
