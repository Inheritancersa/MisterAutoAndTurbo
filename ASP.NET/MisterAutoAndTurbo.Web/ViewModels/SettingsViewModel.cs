using System.ComponentModel.DataAnnotations;

namespace MisterAutoAndTurbo.Web.ViewModels
{
    public class SettingsViewModel
    {
        [Required(ErrorMessage = "Enter the business name.")]
        [StringLength(80, ErrorMessage = "Business name can be up to 80 characters.")]
        [Display(Name = "Business Name")]
        public string BusinessName { get; set; } = string.Empty;

        [RegularExpression(@"^\d{10}$", ErrorMessage = "A South African VAT number has 10 digits.")]
        [Display(Name = "VAT Number")]
        public string? VatNumber { get; set; }

        [StringLength(200, ErrorMessage = "Address can be up to 200 characters.")]
        public string? Address { get; set; }

        [Required(ErrorMessage = "Choose a currency.")]
        public string Currency { get; set; } = "ZAR";

        [Required(ErrorMessage = "Enter the VAT rate.")]
        [Range(0, 100, ErrorMessage = "Enter a rate between 0 and 100.")]
        [Display(Name = "VAT Rate (%)")]
        public decimal? VatRate { get; set; }
    }
}
