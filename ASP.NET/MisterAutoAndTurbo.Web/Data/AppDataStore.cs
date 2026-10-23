using Microsoft.EntityFrameworkCore;
using MisterAutoAndTurbo.Web.Models;
using MisterAutoAndTurbo.Web.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using Npgsql;
using MisterAutoAndTurbo.Web.Models;
using MisterAutoAndTurbo.Web.Data;
using Microsoft.AspNetCore.Mvc.Filters;
using MisterAutoAndTurbo.Web.Services;

namespace MisterAutoAndTurbo.Web.Data
{
    /// <summary>
    /// Working copy of the staff app data. It is loaded from the Supabase (PostgreSQL) database when the app starts
    /// and written back to it after every change (see <see cref="PersistChangesFilter"/>).
    /// The sample records below are only inserted the first time, when the database is created.
    /// </summary>
    public static class AppDataStore
    {
        private static readonly object _lock = new();
        private static readonly object _dbLock = new();
        private static int _jobSeq = 3; // after sample job-001..003

        /// <summary>Min time between DB refreshes (avoids hitting Supabase on every click).</summary>
        private static readonly TimeSpan RefreshInterval = TimeSpan.FromSeconds(2.5);
        private static DateTime _lastRefreshUtc = DateTime.MinValue;
        private static DateTime _lastFullLoadUtc = DateTime.MinValue;
        private static readonly TimeSpan FullLoadInterval = TimeSpan.FromSeconds(30);

        // IDs we have already seen in the database, per entity type. A row that is in this set
        // but no longer in memory was deleted locally, so it must NOT be merged back in from the DB.
        private static readonly Dictionary<Type, HashSet<string>> _known = new();

        private static string? IdOf<T>(T e) => (string?)typeof(T).GetProperty("Id")?.GetValue(e);

        private static void RememberKnown<T>(IEnumerable<T> items) where T : class
        {
            if (!_known.TryGetValue(typeof(T), out var set))
                _known[typeof(T)] = set = new HashSet<string>();
            foreach (var e in items)
            {
                var id = IdOf(e);
                if (!string.IsNullOrEmpty(id)) set.Add(id);
            }
        }

        public static List<Vehicle> Vehicles { get; } = new()
        {
            new Vehicle
            {
                Id = "veh-001",
                CustomerId = "cust-001",
                RegistrationNumber = "ABC 123 FS",
                Make = "Toyota",
                Model = "Hilux",
                Year = 2019,
                Mileage = 85400
            },
            new Vehicle
            {
                Id = "veh-002",
                CustomerId = "cust-002",
                RegistrationNumber = "XYZ 456 GP",
                Make = "Volkswagen",
                Model = "Polo",
                Year = 2018,
                Mileage = 67200
            },
            new Vehicle
            {
                Id = "veh-003",
                CustomerId = "cust-003",
                RegistrationNumber = "DEF 789 FS",
                Make = "Ford",
                Model = "Ranger",
                Year = 2021,
                Mileage = 41200
            }
        };

        public static List<Customer> Customers { get; } = new()
        {
            new Customer
            {
                Id = "cust-001",
                Name = "Thabo",
                Surname = "Molefe",
                Phone = "082 111 2233",
                Email = "thabo@example.com",
                Address = "12 Constantia Rd, Welkom"
            },
            new Customer
            {
                Id = "cust-002",
                Name = "Sarah",
                Surname = "Nkosi",
                Phone = "083 444 5566",
                Email = "sarah@example.com",
                Address = "45 Stateway, Welkom"
            },
            new Customer
            {
                Id = "cust-003",
                Name = "Johan",
                Surname = "Botha",
                Phone = "084 777 8899",
                Email = "johan@example.com",
                Address = "8 Dagbreek Ave, Welkom"
            }
        };

        public static List<JobCard> JobCards { get; } = new()
        {
            new JobCard
            {
                Id = "job-001",
                VehichleId = "veh-001",
                CustomerId = "cust-001",
                Status = JobStatus.Quotation,
                Diagnosis = "Turbo lag / boost leak",
                CreatedAt = SaTime.Now.Date.AddDays(-2),
                CreatedById = "staff-01",
                MechanicId = "mech-01"
            },
            new JobCard
            {
                Id = "job-002",
                VehichleId = "veh-002",
                CustomerId = "cust-002",
                Status = JobStatus.Inspection,
                Diagnosis = "Service + brake check",
                CreatedAt = SaTime.Now.Date.AddDays(-1),
                CreatedById = "staff-01"
            },
            new JobCard
            {
                Id = "job-003",
                VehichleId = "veh-003",
                CustomerId = "cust-003",
                Status = JobStatus.ReadyForCollection,
                Diagnosis = "Oil leak under sump",
                CreatedAt = SaTime.Now.Date,
                CreatedById = "staff-01",
                MechanicId = "mech-01"
            }
        };

        public static List<ServiceRequest> ServiceRequests { get; } = new();

        public static List<Quotation> Quotations { get; } = new()
        {
            new Quotation
            {
                Id = "quot-001",
                JobCardId = "job-001",
                CustomerId = "cust-001",
                VehicleId = "veh-001",
                Description = "Diagnose turbo boost leak, replace hose, labour",
                LabourCost = 1200m,
                PartsCost = 850m,
                CustomerSuppliedDifference = 0m,
                TotalCost = 2050m,
                Status = "Pending",
                CreatedAt = SaTime.Now.Date.AddDays(-1)
            },
            new Quotation
            {
                Id = "quot-002",
                JobCardId = "job-002",
                CustomerId = "cust-002",
                VehicleId = "veh-002",
                Description = "Full service + front brake pads",
                LabourCost = 800m,
                PartsCost = 1450m,
                CustomerSuppliedDifference = 200m,
                TotalCost = 2050m,
                Status = "Approved",
                CreatedAt = SaTime.Now.Date
            }
        };



        public static List<Appointment> Appointments { get; } = new()
        {
            new Appointment
            {
                Id = "apt-001",
                CustomerId = "cust-001",
                VehicleId = "veh-001",
                JobCardId = "job-001",
                CreatedById = "staff-01",
                ScheduledAt = SaTime.Now.Date.AddHours(9),
                Status = "Scheduled"
            },
            new Appointment
            {
                Id = "apt-002",
                CustomerId = "cust-002",
                VehicleId = "veh-002",
                CreatedById = "staff-01",
                ScheduledAt = SaTime.Now.Date.AddHours(11),
                Status = "Arrived"
            },
            new Appointment
            {
                Id = "apt-003",
                CustomerId = "cust-003",
                VehicleId = "veh-003",
                CreatedById = "staff-01",
                ScheduledAt = SaTime.Now.Date.AddDays(1).AddHours(10),
                Status = "Scheduled"
            }
        };


        public static List<Part> Parts { get; } = new()
        {
            new Part
            {
                Id = "part-001",
                Name = "Turbo boost hose",
                Category = "Turbo",
                QuantityInStock = 12,
                UnitPrice = 450.00m,
                ReorderLevel = 5
            },
            new Part
            {
                Id = "part-002",
                Name = "Front brake pads (set)",
                Category = "Brakes",
                QuantityInStock = 8,
                UnitPrice = 890.00m,
                ReorderLevel = 4
            },
            new Part
            {
                Id = "part-003",
                Name = "Engine oil 5W-30 5L",
                Category = "Service",
                QuantityInStock = 3,
                UnitPrice = 520.00m,
                ReorderLevel = 6
            },
            new Part
            {
                Id = "part-004",
                Name = "Oil filter standard",
                Category = "Service",
                QuantityInStock = 25,
                UnitPrice = 95.00m,
                ReorderLevel = 10
            },
            new Part
            {
                Id = "part-005",
                Name = "Sump gasket",
                Category = "Engine",
                QuantityInStock = 2,
                UnitPrice = 180.00m,
                ReorderLevel = 3
            }
        };


        public static List<Payment> Payments { get; } = new()
        {
            new Payment
            {
                Id = "pay-001",
                QuotationId = "quot-002",
                CustomerId = "cust-002",
                RecordedById = "staff-01",
                Amount = 1000.00m,
                Method = "EFT",
                Status = "Confirmed",
                PaidAt = SaTime.Now.Date.AddHours(-3),
                ProofOfPaymentUrl = ""
            }
        };


        public static List<VehicleCollection> VehicleCollections { get; } = new()
        {
            new VehicleCollection
            {
                Id = "col-001",
                JobCardId = "job-003",
                CustomerId = "cust-003",
                VehicleId = "veh-003",
                ManagedById = "staff-01",
                CollectionStatus = "Ready",
                ScheduledAt = SaTime.Now.Date.AddHours(15),
                Notes = "Customer notified — Ford Ranger ready"
            }
        };


        public static List<Complaint> Complaints { get; } = new()
        {
            new Complaint
            {
                Id = "cmp-001",
                CustomerId = "cust-001",
                JobCardId = "job-001",
                VehicleId = "veh-001",
                Subject = "Work took longer than quoted",
                Description = "I was told the turbo hose would be done in one day but the car was still not ready after three days.",
                Status = "Open",
                Source = "Mobile",
                CreatedAt = SaTime.Now.Date.AddHours(-5)
            }
        };

        public static List<Notification> Notifications { get; } = new();

        /// <summary>Job inspections recorded by mechanics (persisted to Supabase).</summary>
        public static List<Inspection> Inspections { get; } = new();
        /// <summary>
        /// Staff accounts — loaded from / saved to Supabase <c>UserAccounts</c>.
        /// Seed entries below are only written on first run when the table is empty.
        /// Passwords are hashed via <see cref="MisterAutoAndTurbo.Web.Services.PasswordService"/> before first save.
        /// Roles: Manager (full access), Receptionist, Mechanic.
        /// </summary>
        public static List<UserAccount> Users { get; } = new()
        {
            new UserAccount
            {
                Id = "user-mgr-001",
                Username = "manager",
                Email = "manager@misterauto.co.za",
                Phone = "082 000 0001",
                PasswordHash = "Manager@123",
                Role = "Manager",
                CreatedAt = DateTime.UtcNow.AddDays(-30)
            },
            new UserAccount
            {
                Id = "user-rec-001",
                Username = "reception",
                Email = "reception@misterauto.co.za",
                Phone = "082 000 0002",
                PasswordHash = "Reception@123",
                Role = "Receptionist",
                CreatedAt = DateTime.UtcNow.AddDays(-20)
            },
            new UserAccount
            {
                Id = "user-mec-001",
                Username = "mechanic",
                Email = "mechanic@misterauto.co.za",
                Phone = "082 000 0003",
                PasswordHash = "Mechanic@123",
                Role = "Mechanic",
                CreatedAt = DateTime.UtcNow.AddDays(-10)
            }
        };

        /// <summary>Business details and payment settings edited on the Settings page.</summary>
        public static BusinessSettings Settings { get; } = new();

        // ------------------------------------------------------------------
        // Database persistence
        // ------------------------------------------------------------------

        /// <summary>Replace the in-memory lists with whatever is saved in the database.</summary>
        /// <summary>Called once at startup: create tables if needed, seed sample data on first run, otherwise load saved data.</summary>
        /// <summary>Called once at startup: create tables if needed, seed sample data on first run, otherwise load saved data.</summary>
        public static void Initialize(ApplicationDbContext db)
        {
            // EnsureCreated() does nothing on Supabase because the database already contains
            // Supabase's own tables, so create this app's tables explicitly.
            var creator = db.GetService<IRelationalDatabaseCreator>();
            try
            {
                creator.CreateTables();
            }
            catch (PostgresException ex) when (ex.SqlState == "42P07")
            {
                // 42P07 = tables already exist (normal on every start after the first).
            }

            if (db.UserAccounts.Any())
            {
                LoadFromDatabase(db);
                // Keep staff accounts durable: never rely on the in-memory seed after first boot.
                HashAnyPlaintextUserPasswords();
            }
            else
            {
                // First run: hash seed passwords, then persist all sample data including users.
                HashAnyPlaintextUserPasswords();
                SaveToDatabase(db);
            }
        }

        /// <summary>
        /// Upgrade legacy plain-text demo passwords to ASP.NET Identity hashes in memory.
        /// Call before Save so the database never stores plain text for staff accounts.
        /// </summary>
        public static void HashAnyPlaintextUserPasswords()
        {
            foreach (var u in Users)
            {
                if (string.IsNullOrEmpty(u.PasswordHash)) continue;
                if (u.PasswordHash.StartsWith("AQAAAA", StringComparison.Ordinal)) continue;
                u.PasswordHash = MisterAutoAndTurbo.Web.Services.PasswordService.Hash(u.PasswordHash);
            }
        }

        /// <summary>Force-write the current Users list (and other store data) to Supabase.</summary>
        public static void PersistUsers(ApplicationDbContext db)
        {
            HashAnyPlaintextUserPasswords();
            SaveToDatabase(db);
        }

        public static void LoadFromDatabase(ApplicationDbContext db)
        {
            lock (_dbLock)
            {
                LoadFromDatabaseUnlocked(db);
            }
        }

        private static void LoadFromDatabaseUnlocked(ApplicationDbContext db)
        {
            Replace(Vehicles, db.Vehicles);
            Replace(Customers, db.Customers);
            Replace(JobCards, db.JobCards);
            Replace(ServiceRequests, db.ServiceRequests);
            Replace(Quotations, db.Quotations);
            Replace(Appointments, db.Appointments);
            Replace(Parts, db.Parts);
            Replace(Payments, db.Payments);
            Replace(VehicleCollections, db.VehicleCollections);
            Replace(Complaints, db.Complaints);
            Replace(Notifications, db.Notifications);
            Replace(Inspections, db.Inspections);
            Replace(Users, db.UserAccounts);

            var saved = db.BusinessSettings.AsNoTracking().FirstOrDefault();
            if (saved != null)
            {
                Settings.BusinessName = saved.BusinessName;
                Settings.VatNumber = saved.VatNumber;
                Settings.Address = saved.Address;
                Settings.Currency = saved.Currency;
                Settings.VatRate = saved.VatRate;
            }

            // Continue job numbering after the highest saved job-NNN.
            _jobSeq = JobCards
                .Select(j => j.Id.StartsWith("job-") && int.TryParse(j.Id[4..], out var n) ? n : 0)
                .DefaultIfEmpty(0)
                .Max();
        }

        /// <summary>
        /// Pull rows that exist in Supabase but not yet in memory (e.g. created by the Flutter app).
        /// Only adds missing IDs — does not overwrite in-memory edits. Call before Save or when refreshing UI.
        /// </summary>
        public static void MergeExternalChanges(ApplicationDbContext db)
        {
            lock (_dbLock)
            {
                MergeMissing(Customers, db.Customers);
                MergeMissing(Vehicles, db.Vehicles);
                MergeMissing(ServiceRequests, db.ServiceRequests);
                MergeMissing(Appointments, db.Appointments);
                MergeMissing(Complaints, db.Complaints);
                MergeMissing(Quotations, db.Quotations);
                MergeMissing(Payments, db.Payments);
                MergeMissing(VehicleCollections, db.VehicleCollections);
                MergeMissing(JobCards, db.JobCards);
                MergeMissing(Notifications, db.Notifications);
                MergeMissing(Inspections, db.Inspections);
                MergeMissing(Users, db.UserAccounts);

                _jobSeq = Math.Max(_jobSeq, JobCards
                    .Select(j => j.Id.StartsWith("job-") && int.TryParse(j.Id[4..], out var n) ? n : 0)
                    .DefaultIfEmpty(0)
                    .Max());
            }
        }



        /// <summary>Replace in-memory lists from the database (full refresh). Use on list pages so Flutter data appears.</summary>
        public static void ReloadFromDatabase(ApplicationDbContext db)
        {
            LoadFromDatabase(db);
            _lastRefreshUtc = DateTime.UtcNow;
            _lastFullLoadUtc = DateTime.UtcNow;
        }

        /// <summary>
        /// Fast path used by PersistChangesFilter.
        /// Full load at most every 30s; otherwise only re-read tables the Flutter app can change,
        /// and only if the last refresh was more than ~2.5s ago.
        /// </summary>
        public static void RefreshFromDatabase(ApplicationDbContext db, bool force = false)
        {
            var now = DateTime.UtcNow;
            if (!force && now - _lastRefreshUtc < RefreshInterval)
                return;

            lock (_dbLock)
            {
                // Re-check inside the lock so concurrent requests don't all hit Supabase.
                if (!force && DateTime.UtcNow - _lastRefreshUtc < RefreshInterval)
                    return;

                if (force || DateTime.UtcNow - _lastFullLoadUtc >= FullLoadInterval)
                {
                    LoadFromDatabaseUnlocked(db);
                    _lastFullLoadUtc = DateTime.UtcNow;
                }
                else
                {
                    // Selective refresh: only tables the customer app inserts/updates.
                    Replace(Quotations, db.Quotations);
                    Replace(Payments, db.Payments);
                    Replace(Complaints, db.Complaints);
                    Replace(ServiceRequests, db.ServiceRequests);
                    Replace(Appointments, db.Appointments);
                    Replace(Vehicles, db.Vehicles);
                    Replace(Customers, db.Customers);
                    // Notifications: merge new staff/customer rows without wiping in-memory dismissals.
                    MergeMissing(Notifications, db.Notifications);
                    // Staff accounts: pick up users created/edited on another instance without waiting for full load.
                    MergeMissing(Users, db.UserAccounts);
                }

                _lastRefreshUtc = DateTime.UtcNow;
            }
        }

        /// <summary>
        /// Follow-up for actions the customer took in the Flutter app. Flutter can only write its own rows
        /// (it never touches staff notifications or job cards), so the staff app does the rest here.
        /// Idempotent: each effect is created once (keyed on Type + RelatedId). Returns true if anything changed.
        /// </summary>
        public static bool ApplyCustomerAppUpdates()
        {
            var changed = false;
            lock (_dbLock)
            {
                string CustName(string id)
                {
                    var c = Customers.FirstOrDefault(x => x.Id == id);
                    return c == null ? "Customer" : $"{c.Name} {c.Surname}".Trim();
                }

                // Include dismissed stubs (UserAccountId == "__dismissed__") so deleting an alert
                // does not cause ApplyCustomerAppUpdates to recreate it on the next page load.
                bool HasStaffNote(string type, string relatedId) =>
                    Notifications.Any(n => n.Type == type && n.RelatedId == relatedId);

                void StaffNote(string type, string relatedId, string message)
                {
                    Notifications.Add(new Notification
                    {
                        Id = NewId("ntf"),
                        UserAccountId = "staff",
                        Type = type,
                        RelatedId = relatedId,
                        IsRead = false,
                        CreatedAt = DateTime.UtcNow,
                        Message = message
                    });
                    changed = true;
                }

                // Quotation approved / rejected in the app
                foreach (var q in Quotations.Where(q => q.Status == QuotationStatus.Approved || q.Status == QuotationStatus.Rejected).ToList())
                {
                    if (q.RespondedAt == null) { q.RespondedAt = DateTime.UtcNow; changed = true; }

                    foreach (var n in Notifications.Where(n => n.RelatedId == q.Id && n.Type == "Quotation" && !n.IsRead))
                    { n.IsRead = true; changed = true; }

                    if (!HasStaffNote("QuotationResponse", q.Id))
                        StaffNote("QuotationResponse", q.Id,
                            $"Customer {CustName(q.CustomerId)} {q.Status.ToLower()} quotation {q.Id} (R {q.TotalCost:N2}).");

                    if (q.Status == QuotationStatus.Approved && !string.IsNullOrWhiteSpace(q.JobCardId))
                    {
                        var job = JobCards.FirstOrDefault(j => j.Id == q.JobCardId);
                        if (job != null && (job.Status == JobStatus.Quotation || job.Status == JobStatus.Inspection))
                        { job.Status = JobStatus.InProgress; changed = true; }
                    }
                }

                // Proof of payment uploaded in the app
                foreach (var p in Payments.Where(p => !string.IsNullOrWhiteSpace(p.ProofOfPaymentUrl) && p.RecordedById == p.CustomerId).ToList())
                    if (!HasStaffNote("PaymentProof", p.Id))
                        StaffNote("PaymentProof", p.Id,
                            $"Customer {CustName(p.CustomerId)} uploaded proof of payment (R {p.Amount:N2}) for quotation {p.QuotationId}.");

                // Complaints recorded in the app
                foreach (var c in Complaints.Where(c => c.Source == "Mobile").ToList())
                    if (!HasStaffNote("Complaint", c.Id))
                        StaffNote("Complaint", c.Id, $"New complaint from {CustName(c.CustomerId)}: {c.Subject}");

                // Service bookings made in the app (Flutter creates ServiceRequest + Appointment)
                foreach (var r in ServiceRequests.Where(r => r.Status == "Open").ToList())
                    if (!HasStaffNote("ServiceRequest", r.Id))
                        StaffNote("ServiceRequest", r.Id,
                            $"New service booking from {CustName(r.CustomerId)}: {r.RequestedService}"
                            + (string.IsNullOrWhiteSpace(r.Description) ? "" : $" — {r.Description}"));

                // Appointments booked by the customer (CreatedById == CustomerId) as a backup signal
                foreach (var a in Appointments.Where(a =>
                    !string.IsNullOrWhiteSpace(a.CustomerId)
                    && string.Equals(a.CreatedById, a.CustomerId, StringComparison.OrdinalIgnoreCase)
                    && (a.Status == "Scheduled" || a.Status == "Pending" || string.IsNullOrWhiteSpace(a.Status))).ToList())
                {
                    if (!HasStaffNote("AppointmentBooking", a.Id))
                    {
                        var svc = string.IsNullOrWhiteSpace(a.ServiceType) ? "Service" : a.ServiceType;
                        StaffNote("AppointmentBooking", a.Id,
                            $"New appointment from {CustName(a.CustomerId)}: {svc} on {a.ScheduledAt:g}"
                            + (string.IsNullOrWhiteSpace(a.Description) ? "" : $" — {a.Description}"));
                    }
                }
            }
            return changed;
        }

        /// <summary>Write the in-memory lists to the database (insert new, update changed, delete removed).</summary>
        public static void SaveToDatabase(ApplicationDbContext db)
        {
            lock (_dbLock)
            {
                // Only pull in *new* Flutter rows so Sync does not delete them.
                // (Cheaper than a full MergeExternal of every table.)
                MergeMissing(ServiceRequests, db.ServiceRequests);
                MergeMissing(Appointments, db.Appointments);
                MergeMissing(Complaints, db.Complaints);
                MergeMissing(Payments, db.Payments);
                MergeMissing(Quotations, db.Quotations);
                MergeMissing(Vehicles, db.Vehicles);
                MergeMissing(Customers, db.Customers);
                MergeMissing(Notifications, db.Notifications);

                // Detach any leftover tracked entities so Sync starts clean.
                db.ChangeTracker.Clear();

                Sync(db, db.Vehicles, Vehicles);
                Sync(db, db.Customers, Customers);
                Sync(db, db.JobCards, JobCards);
                Sync(db, db.ServiceRequests, ServiceRequests);
                Sync(db, db.Quotations, Quotations);
                Sync(db, db.Appointments, Appointments);
                Sync(db, db.Parts, Parts);
                Sync(db, db.Payments, Payments);
                Sync(db, db.VehicleCollections, VehicleCollections);
                Sync(db, db.Complaints, Complaints);
                Sync(db, db.Notifications, Notifications);
                Sync(db, db.Inspections, Inspections);
                Sync(db, db.UserAccounts, Users);

                Settings.Id = 1;
                var row = db.BusinessSettings.AsNoTracking().FirstOrDefault();
                if (row == null)
                {
                    db.BusinessSettings.Add(new BusinessSettings
                    {
                        Id = 1,
                        BusinessName = Settings.BusinessName,
                        VatNumber = Settings.VatNumber,
                        Address = Settings.Address,
                        Currency = Settings.Currency,
                        VatRate = Settings.VatRate
                    });
                }
                else
                {
                    db.BusinessSettings.Attach(row);
                    db.Entry(row).CurrentValues.SetValues(Settings);
                }

                if (db.ChangeTracker.HasChanges())
                    db.SaveChanges();

                RememberKnown(Vehicles); RememberKnown(Customers); RememberKnown(JobCards);
                RememberKnown(ServiceRequests); RememberKnown(Quotations); RememberKnown(Appointments);
                RememberKnown(Parts); RememberKnown(Payments); RememberKnown(VehicleCollections);
                RememberKnown(Complaints); RememberKnown(Notifications); RememberKnown(Users);

                // Saving implies memory matches DB for known rows.
                _lastRefreshUtc = DateTime.UtcNow;
            }
        }

        private static void Replace<T>(List<T> list, IQueryable<T> query) where T : class
        {
            var items = query.AsNoTracking().ToList();
            list.Clear();
            list.AddRange(items);
            _known[typeof(T)] = new HashSet<string>();
            RememberKnown(items);
        }

        /// <summary>Add DB rows whose primary key is not already present in the in-memory list.</summary>
        private static void MergeMissing<T>(List<T> list, IQueryable<T> query) where T : class
        {
            var keyProp = typeof(T).GetProperty("Id");
            if (keyProp == null) return;

            var existingIds = new HashSet<string>(
                list.Select(e => (string)keyProp.GetValue(e)!)
                    .Where(id => !string.IsNullOrEmpty(id)));

            _known.TryGetValue(typeof(T), out var known);

            foreach (var row in query.AsNoTracking().ToList())
            {
                var id = (string?)keyProp.GetValue(row);
                if (string.IsNullOrEmpty(id) || existingIds.Contains(id)) continue;
                // Seen before but missing from memory => deleted in this app; don't resurrect it.
                if (known != null && known.Contains(id)) continue;
                list.Add(row);
                existingIds.Add(id);
                RememberKnown(new[] { row });
            }
        }

        private static void Sync<T>(ApplicationDbContext db, DbSet<T> set, List<T> items) where T : class
        {
            var key = db.Model.FindEntityType(typeof(T))!.FindPrimaryKey()!.Properties[0].PropertyInfo!;
            // AsNoTracking: do not pay tracking cost while building the id map.
            var saved = set.AsNoTracking().ToList().ToDictionary(e => (string)key.GetValue(e)!);
            var seen = new HashSet<string>();

            foreach (var item in items)
            {
                var id = (string?)key.GetValue(item);
                if (string.IsNullOrEmpty(id) || !seen.Add(id)) continue;

                if (saved.TryGetValue(id, out var row))
                {
                    // Attach the no-tracking snapshot, then copy memory values onto it.
                    set.Attach(row);
                    var entry = db.Entry(row);
                    entry.CurrentValues.SetValues(item);
                    // Avoid pointless UPDATEs when nothing actually changed.
                    if (!entry.Properties.Any(p => p.IsModified))
                        entry.State = EntityState.Unchanged;
                }
                else
                {
                    set.Add(item);
                }
            }

            foreach (var kv in saved)
            {
                if (seen.Contains(kv.Key)) continue;
                // Delete by stub so we do not need a tracked instance from the first load.
                var stub = kv.Value;
                set.Attach(stub);
                set.Remove(stub);
            }
        }

        public static string NewId(string prefix)
        {
            lock (_lock)
            {
                return $"{prefix}-{Guid.NewGuid().ToString("N")[..8]}";
            }
        }

        /// <summary>Sequential job id: job-004, job-005, ...</summary>
        public static string NextJobCardId()
        {
            lock (_lock)
            {
                _jobSeq++;
                return $"job-{_jobSeq:D3}";
            }
        }
    }
}
