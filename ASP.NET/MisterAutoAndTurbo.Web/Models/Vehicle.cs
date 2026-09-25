namespace MisterAutoAndTurbo.Web.Models
{
    public class Vehicle
    {
        public string Id { get; set; }
        public string CustomerId { get; set; }
        public string RegistrationNumber { get; set; }
        public string VehicleIdentificationNumber { get; set; }
        public string Make { get; set; }
        public string Model { get; set; }
        public int Year { get; set; }
        public int Mileage { get; set; }

    }
}
