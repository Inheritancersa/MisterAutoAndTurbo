import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import '../models/booking.dart';
import '../routes/route_manager.dart';
import '../theme.dart';
import '../utils/helpers.dart';
import '../viewmodels/book_service_viewmodel.dart';
import '../viewmodels/workshop_viewmodel.dart';
import '../widgets/common.dart';

// ============================================================
// BOOK A SERVICE
// STEP 1: SELECT A VEHICLE AND SERVICE TYPE
// STEP 2: PICK A TIME
// STEP 3: SHOW BOOKING CONFIRMATION
//
// The step and every choice live in BookServiceViewModel. This view only
// decides which step to draw.
// ============================================================

/// Stateful only to own the notes text controller (it must survive moving
/// between step 1 and step 2) and to open the date picker.
class BookServiceView extends StatefulWidget {
  const BookServiceView({super.key});
  @override
  State<BookServiceView> createState() => _BookServiceViewState();
}

class _BookServiceViewState extends State<BookServiceView> {
  final TextEditingController _notes = TextEditingController();

  @override
  void dispose() {
    _notes.dispose();
    super.dispose();
  }

  /// Opens the date picker. Sundays are unavailable (the workshop is closed)
  /// and appointments can be booked up to 90 days ahead.
  Future<void> _pickDate() async {
    final vm = context.read<BookServiceViewModel>();
    final now = DateTime.now();
    final today = DateTime(now.year, now.month, now.day);

    final selected = await showDatePicker(
      context: context,
      firstDate: today,
      lastDate: now.add(const Duration(days: 90)),
      initialDate: vm.date ?? today,
      selectableDayPredicate: (date) => date.weekday != DateTime.sunday,
    );

    if (selected != null && mounted) vm.selectDate(selected);
  }

  @override
  Widget build(BuildContext context) {
    // Rebuilds only when the step changes.
    return Selector<BookServiceViewModel, BookingStep>(
      selector: (_, vm) => vm.step,
      builder: (context, step, _) => switch (step) {
        BookingStep.chooseService => const _ChooseStep(),
        BookingStep.pickTime => _PickTimeStep(notes: _notes, onPickDate: _pickDate),
        BookingStep.confirmed => const _ConfirmedStep(),
      },
    );
  }
}

/// STEP 1: choose a registered vehicle and a service type.
class _ChooseStep extends StatelessWidget {
  const _ChooseStep();

  @override
  Widget build(BuildContext context) {
    final vm = context.read<BookServiceViewModel>();
    return AppScreen(
      title: 'Book A Service.',
      children: [
        Text('Choose your vehicle', style: AppText.plex(16, c: AppColors.label)),
        const SizedBox(height: 10),

        // All registered vehicles.
        Consumer2<WorkshopViewModel, BookServiceViewModel>(
          builder: (context, workshop, booking, _) => Column(
            children: [
              for (final vehicle in workshop.vehicles)
                _ChoiceCard(
                  text: '${vehicle.displayName} - ${vehicle.plate}',
                  selected: booking.vehicle == vehicle,
                  onTap: () => booking.selectVehicle(vehicle),
                ),
            ],
          ),
        ),

        const SizedBox(height: 14),
        Text('Type of service', style: AppText.plex(16, c: AppColors.label)),
        const SizedBox(height: 10),

        // All available service types.
        Consumer<BookServiceViewModel>(
          builder: (context, booking, _) => Column(
            children: [
              for (final service in ServiceType.values)
                _ChoiceCard(
                  text: service.label,
                  selected: booking.service == service,
                  onTap: () => booking.selectService(service),
                ),
            ],
          ),
        ),

        const SizedBox(height: 14),

        // NEXT: validate the selections and go to Pick A Time.
        WideButton('Next', onPressed: () {
          final error = vm.next();
          if (error != null) snack(context, error);
        }),

        const SizedBox(height: 12),

        // MY BOOKINGS: open the customer's bookings screen.
        WideButton('My Bookings',
            onPressed: () => Navigator.pushNamed(context, RouteManager.myBookings)),
      ],
    );
  }
}

/// STEP 2: pick a date, a time and optional notes.
class _PickTimeStep extends StatelessWidget {
  final TextEditingController notes;
  final VoidCallback onPickDate;
  const _PickTimeStep({required this.notes, required this.onPickDate});

  @override
  Widget build(BuildContext context) {
    final vm = context.read<BookServiceViewModel>();
    return AppScreen(
      title: 'Pick A Time.',
      children: [
        // Select the appointment date.
        Selector<BookServiceViewModel, DateTime?>(
          selector: (_, vm) => vm.date,
          builder: (context, date, _) => InputBox(
            height: 60,
            onTap: onPickDate,
            child: Row(
              children: [
                const Icon(Icons.calendar_month, color: Colors.black),
                const SizedBox(width: 12),
                Text(date == null ? 'Choose a date' : fmtDate(date),
                    style: AppText.plex(16)),
              ],
            ),
          ),
        ),

        const SizedBox(height: 18),

        // Select an available appointment time.
        Selector<BookServiceViewModel, String?>(
          selector: (_, vm) => vm.time,
          builder: (context, time, _) => Wrap(
            spacing: 10,
            runSpacing: 10,
            children: [
              for (final slot in BookServiceViewModel.slots)
                ChoiceChip(
                  label: Text(slot, style: AppText.plex(15, c: Colors.black)),
                  selected: time == slot,
                  selectedColor: AppColors.successBg,
                  onSelected: (_) => vm.selectTime(slot),
                ),
            ],
          ),
        ),

        const SizedBox(height: 18),

        // Optional notes about the customer's vehicle.
        LabeledField(label: 'Notes (optional)', controller: notes, maxLines: 4),

        const SizedBox(height: 12),

        // Confirm the appointment and show the confirmation.
        WideButton('Confirm Booking', green: true, onPressed: () async {
          final error = await vm.confirm(notes.text);
          if (error != null && context.mounted) snack(context, error);
        }),

        const SizedBox(height: 12),

        // BACK: return to vehicle and service selection.
        WideButton('Back', outlined: true, onPressed: vm.back),
      ],
    );
  }
}

/// STEP 3: booking confirmed.
class _ConfirmedStep extends StatelessWidget {
  const _ConfirmedStep();

  @override
  Widget build(BuildContext context) {
    final booking = context.read<BookServiceViewModel>().confirmed!;
    return AppScreen(
      title: 'Booking Confirmed.',
      children: [
        InfoCard(
          color: AppColors.successBg,
          borderColor: AppColors.successBorder,
          padding: const EdgeInsets.all(20),
          child: Column(
            children: [
              const Icon(Icons.check_circle, size: 48, color: AppColors.done),
              const SizedBox(height: 8),
              Text(booking.service.label, style: AppText.inter(22)),
              Text(booking.vehicle.displayName,
                  style: AppText.plex(16, c: Colors.black)),
              const SizedBox(height: 6),
              Text('${fmtDate(booking.date)} at ${booking.time}',
                  style: AppText.plex(16, c: AppColors.label)),
            ],
          ),
        ),

        const SizedBox(height: 24),

        // Done returns to the previous screen (pop).
        WideButton('Done', onPressed: () => Navigator.pop(context)),

        const SizedBox(height: 12),

        // My Bookings opens the customer's saved bookings (push).
        WideButton('My Bookings',
            onPressed: () => Navigator.pushNamed(context, RouteManager.myBookings)),
      ],
    );
  }
}

/// A selectable card for a vehicle or a service type.
class _ChoiceCard extends StatelessWidget {
  final String text;
  final bool selected;
  final VoidCallback onTap;
  const _ChoiceCard({required this.text, required this.selected, required this.onTap});

  @override
  Widget build(BuildContext context) => Padding(
        padding: const EdgeInsets.only(bottom: 12),
        child: SizedBox(
          width: double.infinity,
          child: InputBox(
            height: 60,
            onTap: onTap,
            fill: selected ? AppColors.successBg : null,
            borderColor: selected ? AppColors.successBorder : null,
            child: Row(
              children: [
                Expanded(child: Text(text, style: AppText.plex(16))),
                if (selected) const Icon(Icons.check_circle, color: AppColors.done),
              ],
            ),
          ),
        ),
      );
}
