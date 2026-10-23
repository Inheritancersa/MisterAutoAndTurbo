
using Microsoft.EntityFrameworkCore;
using MisterAutoAndTurbo.Web.Models;

namespace MisterAutoAndTurbo.Web.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(
            DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Customer> Customers => Set<Customer>();
        public DbSet<Vehicle> Vehicles => Set<Vehicle>();
        public DbSet<Appointment> Appointments => Set<Appointment>();
        public DbSet<JobCard> JobCards => Set<JobCard>();
        public DbSet<Quotation> Quotations => Set<Quotation>();
        public DbSet<Payment> Payments => Set<Payment>();
        public DbSet<Inspection> Inspections => Set<Inspection>();
        public DbSet<Repair> Repairs => Set<Repair>();
        public DbSet<Part> Parts => Set<Part>();
        public DbSet<JobPart> JobParts => Set<JobPart>();
        public DbSet<Complaint> Complaints => Set<Complaint>();
        public DbSet<ServiceRequest> ServiceRequests => Set<ServiceRequest>();
        public DbSet<Notification> Notifications => Set<Notification>();
        public DbSet<UserAccount> UserAccounts => Set<UserAccount>();
        public DbSet<Mechanic> Mechanics => Set<Mechanic>();
        public DbSet<Receptionist> Receptionists => Set<Receptionist>();
        public DbSet<VehicleCollection> VehicleCollections => Set<VehicleCollection>();
        public DbSet<BusinessSettings> BusinessSettings => Set<BusinessSettings>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            // Single settings row, id is always 1 (not database-generated).
            modelBuilder.Entity<BusinessSettings>().Property(b => b.Id).ValueGeneratedNever();
        }
    }
}
