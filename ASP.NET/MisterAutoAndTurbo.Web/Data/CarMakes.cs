using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace MisterAutoAndTurbo.Web.Data
{
    /// <summary>
    /// Vehicle makes offered in the Register / Edit vehicle dropdown
    /// (same list as the Flutter app's car_makes.dart).
    /// </summary>
    public static class CarMakes
    {
        public static readonly IReadOnlyList<string> All = new[]
        {
            "Alfa Romeo",
            "Aston Martin",
            "Audi",
            "BMW",
            "BYD",
            "Bentley",
            "Cadillac",
            "Chery",
            "Chevrolet",
            "Citroën",
            "Fiat",
            "Ferrari",
            "Ford",
            "Genesis",
            "Geely",
            "GMC",
            "Great Wall Motors (GWM)",
            "Haval",
            "Honda",
            "Hyundai",
            "Infiniti",
            "Isuzu",
            "Jaguar",
            "Jeep",
            "Kia",
            "Lamborghini",
            "Land Rover",
            "Lexus",
            "Mahindra",
            "Maserati",
            "Mazda",
            "McLaren",
            "Mercedes-Benz",
            "MG",
            "Mini",
            "Mitsubishi",
            "Nissan",
            "Opel",
            "Peugeot",
            "Polestar",
            "Porsche",
            "Renault",
            "Rolls-Royce",
            "Škoda",
            "SEAT",
            "Subaru",
            "Suzuki",
            "Tesla",
            "Toyota",
            "Volkswagen",
            "Volvo",
        };

        /// <summary>
        /// SelectList items for a Make dropdown. Optionally includes the
        /// current value when it is not already in the list (legacy data).
        /// </summary>
        public static List<SelectListItem> SelectList(string? selected = null)
        {
            var items = All
                .Select(m => new SelectListItem
                {
                    Value = m,
                    Text = m,
                    Selected = !string.IsNullOrEmpty(selected)
                        && string.Equals(m, selected, System.StringComparison.OrdinalIgnoreCase)
                })
                .ToList();

            if (!string.IsNullOrWhiteSpace(selected)
                && !All.Any(m => string.Equals(m, selected, System.StringComparison.OrdinalIgnoreCase)))
            {
                items.Insert(0, new SelectListItem
                {
                    Value = selected.Trim(),
                    Text = selected.Trim(),
                    Selected = true
                });
            }

            return items;
        }
    }
}
