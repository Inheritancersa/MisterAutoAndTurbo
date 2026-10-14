using MisterAutoAndTurbo.Web.Models;

namespace MisterAutoAndTurbo.Web.Data
{
    /// <summary>
    /// Simple in-memory store for the staff app (resets when the app restarts).
    /// </summary>
    public static class AppDataStore
    {
        private static readonly object _lock = new();
        private static int _jobSeq = 3; // after sample job-001..003

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
                CreatedAt = DateTime.Today.AddDays(-2),
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
                CreatedAt = DateTime.Today.AddDays(-1),
                CreatedById = "staff-01"
            },
            new JobCard
            {
                Id = "job-003",
                VehichleId = "veh-003",
                CustomerId = "cust-003",
                Status = JobStatus.InProgress,
                Diagnosis = "Oil leak under sump",
                CreatedAt = DateTime.Today,
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
                CreatedAt = DateTime.Today.AddDays(-1)
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
                CreatedAt = DateTime.Today
            }
        };


        public static List<Notification> Notifications { get; } = new();

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
