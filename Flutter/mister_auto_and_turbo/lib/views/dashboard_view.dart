import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import '../models/repair_job.dart';
import '../models/vehicle.dart';
import '../routes/route_manager.dart';
import '../theme.dart';
import '../viewmodels/auth_viewmodel.dart';
import '../viewmodels/workshop_viewmodel.dart';
import '../widgets/common.dart';

/// Profile Phone - the customer home screen.
///
/// A StatelessWidget: the data comes from [WorkshopViewModel] through a
/// [Consumer], and the greeting from [AuthViewModel] through a [Selector].
class DashboardView extends StatelessWidget {
  const DashboardView({super.key});

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      body: SafeArea(
        bottom: false,
        child: Column(
          children: [
            Expanded(
              child: RefreshIndicator(
                onRefresh: () => context.read<WorkshopViewModel>().refresh(),
                child: ListView(
                physics: const AlwaysScrollableScrollPhysics(),
                padding: const EdgeInsets.fromLTRB(19, 8, 19, 24),
                children: [
                  // Greeting on the left; Contact Us (top) / Log Out (below) on the right, same level
                  Row(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      const Expanded(child: GreetingText()),
                      const _MenuButton(),
                    ],
                  ),

                  // Vehicles heading
                  Text('Your Vehicles.', style: AppText.inter(24)),
                  const SizedBox(height: 26),

                  // Rebuilds only when the vehicles / jobs change.
                  const _VehicleList(),

                  // Register another vehicle
                  InputBox(
                    height: 67,
                    onTap: () => Navigator.pushNamed(context, RouteManager.registerVehicle),
                    child: Center(
                      child: Text('+ Register A Vehicle', style: AppText.plex(16)),
                    ),
                  ),

                  const SizedBox(height: 28),

                  // Quick actions
                  Text('Quick Actions...', style: AppText.plex(16, c: AppColors.label)),
                  const SizedBox(height: 16),

                  GridView.count(
                    crossAxisCount: 2,
                    shrinkWrap: true,
                    physics: const NeverScrollableScrollPhysics(),
                    mainAxisSpacing: 28,
                    crossAxisSpacing: 28,
                    childAspectRatio: 144 / 52,
                    children: const [
                      _QuickAction('Book A Service', RouteManager.bookService),
                      _QuickAction('Quotations', RouteManager.quotation),
                      _QuickAction('Repair\nHistory', RouteManager.history),
                      _QuickAction('Notifications', RouteManager.notifications,
                          showBell: true),
                      _QuickAction('Complaints', RouteManager.complaints),
                      _QuickAction('Upload Proof\nPayment.', RouteManager.proofOfPayment),
                    ],
                  ),

                  const SizedBox(height: 8),
                ],
              ),
              ),
            ),
            const BrandFooter(),
          ],
        ),
      ),
    );
  }
}

/// The "Your Vehicles" cards: vehicles with repair jobs first, then the
/// registered vehicles that have no repair job yet.
class _VehicleList extends StatelessWidget {
  const _VehicleList();

  @override
  Widget build(BuildContext context) {
    return Consumer<WorkshopViewModel>(
      builder: (context, workshop, _) {
        final jobs = workshop.jobs;
        final idle = workshop.vehiclesWithoutJob;
        return Column(
          children: [
            if (workshop.vehicles.isEmpty && jobs.isEmpty)
              Padding(
                padding: const EdgeInsets.symmetric(vertical: 24),
                child: Center(
                  child: Text(
                    'No vehicles registered yet.\nRegister your vehicle to get started.',
                    textAlign: TextAlign.center,
                    style: AppText.plex(16, c: AppColors.label),
                  ),
                ),
              ),
            for (final job in jobs)
              _VehicleCard(
                vehicle: job.vehicle,
                job: job,
                subtitle: '${job.vehicle.plate} - ${job.summaryLabel}',
              ),
            for (final vehicle in idle)
              _VehicleCard(
                vehicle: vehicle,
                subtitle: '${vehicle.plate} - No active repair',
              ),
          ],
        );
      },
    );
  }
}

/// One tappable vehicle card. Tapping opens the dynamic Current Status route
/// and passes the vehicle (and its job, if any) as route arguments.
class _VehicleCard extends StatelessWidget {
  final Vehicle vehicle;
  final RepairJob? job;
  final String subtitle;
  const _VehicleCard({required this.vehicle, this.job, required this.subtitle});

  @override
  Widget build(BuildContext context) {
    return Padding(
      padding: const EdgeInsets.only(bottom: 20),
      child: SizedBox(
        width: double.infinity,
        child: InputBox(
          height: 67,
          padding: const EdgeInsets.symmetric(horizontal: 14),
          onTap: () => Navigator.pushNamed(
            context,
            RouteManager.vehicleStatus,
            arguments: VehicleStatusArguments(vehicle: vehicle, job: job),
          ),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            mainAxisAlignment: MainAxisAlignment.center,
            children: [
              Text(
                vehicle.displayName,
                style: AppText.inter(20, w: FontWeight.w800, c: Colors.black),
              ),
              Text(subtitle, style: AppText.plex(16, c: Colors.black)),
            ],
          ),
        ),
      ),
    );
  }
}

/// Dropdown holding Refresh / Contact Us / Edit Profile / Log Out.
class _MenuButton extends StatelessWidget {
  const _MenuButton();

  Future<void> _refresh(BuildContext context) async {
    final workshop = context.read<WorkshopViewModel>();
    if (workshop.isLoading) return;
    final messenger = ScaffoldMessenger.of(context);
    await workshop.refresh();
    messenger.showSnackBar(SnackBar(
      content: Text(workshop.error ?? 'Up to date'),
      duration: const Duration(seconds: 2),
    ));
  }

  void _logout(BuildContext context) {
    context.read<AuthViewModel>().logout(); // fire-and-forget sign-out
    // Clear the whole stack so Back cannot return here.
    Navigator.pushNamedAndRemoveUntil(context, RouteManager.login, (route) => false);
  }

  @override
  Widget build(BuildContext context) {
    final loading = context.select<WorkshopViewModel, bool>((w) => w.isLoading);
    return PopupMenuButton<String>(
      tooltip: 'Menu',
      offset: const Offset(0, 44),
      onSelected: (value) {
        switch (value) {
          case 'refresh':
            _refresh(context);
          case 'contact':
            Navigator.pushNamed(context, RouteManager.contact);
          case 'profile':
            Navigator.pushNamed(context, RouteManager.editProfile);
          case 'logout':
            _logout(context);
        }
      },
      itemBuilder: (_) => [
        PopupMenuItem(
          value: 'refresh',
          enabled: !loading,
          child: Text(loading ? 'Refreshing...' : 'Refresh'),
        ),
        const PopupMenuItem(value: 'contact', child: Text('Contact Us')),
        const PopupMenuItem(value: 'profile', child: Text('Edit Profile')),
        const PopupMenuItem(value: 'logout', child: Text('Log Out')),
      ],
      child: SizedBox(
        width: 110,
        child: InputBox(
          height: 36,
          padding: EdgeInsets.zero,
          child: Row(
            mainAxisAlignment: MainAxisAlignment.center,
            children: [
              Text('Menu', style: AppText.plex(14)),
              const SizedBox(width: 4),
              const Icon(Icons.arrow_drop_down, size: 22),
            ],
          ),
        ),
      ),
    );
  }
}

/// Quick action button that opens the named [route].
class _QuickAction extends StatelessWidget {
  final String label;
  final String route;
  final bool showBell;
  const _QuickAction(this.label, this.route, {this.showBell = false});

  @override
  Widget build(BuildContext context) {
    final unread = showBell
        ? context.select<WorkshopViewModel, int>((w) => w.unreadNotificationCount)
        : 0;
    return InputBox(
      padding: const EdgeInsets.symmetric(horizontal: 6),
      onTap: () => Navigator.pushNamed(context, route),
      child: Stack(
        alignment: Alignment.center,
        children: [
          Center(
            child: Text(label, textAlign: TextAlign.center, style: AppText.plex(16)),
          ),
          if (unread > 0)
            Positioned(
              top: 4,
              right: 4,
              child: Stack(
                clipBehavior: Clip.none,
                children: [
                  const Icon(Icons.notifications_active,
                      size: 26, color: Color(0xFFE53935)),
                  Positioned(
                    right: -4,
                    top: -4,
                    child: Container(
                      padding: const EdgeInsets.all(3),
                      decoration: const BoxDecoration(
                          color: Color(0xFFE53935), shape: BoxShape.circle),
                      constraints: const BoxConstraints(minWidth: 16, minHeight: 16),
                      child: Text(unread > 9 ? '9+' : '$unread',
                          textAlign: TextAlign.center,
                          style: const TextStyle(
                              color: Colors.white,
                              fontSize: 10,
                              fontWeight: FontWeight.w700)),
                    ),
                  ),
                ],
              ),
            ),
        ],
      ),
    );
  }
}
