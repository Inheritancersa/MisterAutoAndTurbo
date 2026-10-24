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
                      // Left column: Refresh / Log Out
                      Column(
                        mainAxisSize: MainAxisSize.min,
                        children: [
                          const _RefreshButton(),
                          const SizedBox(height: 8),
                          _SmallButton(
                            label: 'Log Out',
                            onTap: () {
                              context.read<AuthViewModel>().logout(); // fire-and-forget sign-out
                              // Clear the whole stack so Back cannot return here.
                              Navigator.pushNamedAndRemoveUntil(
                                context,
                                RouteManager.login,
                                (route) => false,
                              );
                            },
                          ),
                        ],
                      ),
                      const SizedBox(width: 8),
                      // Right column: Contact Us / Edit Profile
                      Column(
                        mainAxisSize: MainAxisSize.min,
                        children: [
                          _SmallButton(
                            label: 'Contact Us',
                            onTap: () => Navigator.pushNamed(context, RouteManager.contact),
                          ),
                          const SizedBox(height: 8),
                          _SmallButton(
                            label: 'Edit Profile',
                            onTap: () => Navigator.pushNamed(context, RouteManager.editProfile),
                          ),
                        ],
                      ),
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
                      _QuickAction('Notifications', RouteManager.notifications),
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

/// Small bordered button used for Contact Us / Log Out.
class _SmallButton extends StatelessWidget {
  final String label;
  final VoidCallback onTap;
  const _SmallButton({required this.label, required this.onTap});

  @override
  Widget build(BuildContext context) => SizedBox(
        width: 96,
        child: InputBox(
          height: 30,
          padding: EdgeInsets.zero,
          onTap: onTap,
          child: Center(child: Text(label, style: AppText.plex(13))),
        ),
      );
}

/// Reloads everything from the server without logging out.
class _RefreshButton extends StatelessWidget {
  const _RefreshButton();

  @override
  Widget build(BuildContext context) {
    final loading = context.select<WorkshopViewModel, bool>((w) => w.isLoading);
    return _SmallButton(
      label: loading ? 'Refreshing...' : 'Refresh',
      onTap: () async {
        if (loading) return;
        final workshop = context.read<WorkshopViewModel>();
        final messenger = ScaffoldMessenger.of(context);
        await workshop.refresh();
        messenger.showSnackBar(SnackBar(
          content: Text(workshop.error ?? 'Up to date'),
          duration: const Duration(seconds: 2),
        ));
      },
    );
  }
}

/// Quick action button that opens the named [route].
class _QuickAction extends StatelessWidget {
  final String label;
  final String route;
  const _QuickAction(this.label, this.route);

  @override
  Widget build(BuildContext context) => InputBox(
        padding: const EdgeInsets.symmetric(horizontal: 6),
        onTap: () => Navigator.pushNamed(context, route),
        child: Center(
          child: Text(label, textAlign: TextAlign.center, style: AppText.plex(16)),
        ),
      );
}
