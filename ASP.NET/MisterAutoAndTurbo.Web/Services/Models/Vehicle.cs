namespace MisterAutoAndTurbo.Web.Models
{
    /// <summary>
    /// A customer's vehicle: registration, make, model, year and mileage.
    /// </summary>
    public class Vehicle
    {
        public string Id { get; set; } = string.Empty;
        public string CustomerId { get; set; } = string.Empty;
        public string RegistrationNumber { get; set; } = string.Empty;
        public string Make { get; set; } = string.Empty;
        public string Model { get; set; } = string.Empty;
        public int Year { get; set; }
        public int Mileage { get; set; }
    }
}
