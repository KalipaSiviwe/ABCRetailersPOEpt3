// Models/ViewModels/RegisterViewModel.cs
using System.ComponentModel.DataAnnotations;

namespace ABCRetailers.Models.ViewModels
{
    public class RegisterViewModel
    {
        [Required(ErrorMessage = "Username is required")]
        [Display(Name = "Username")]
        [MaxLength(100)]
        public string Username { get; set; } = string.Empty;

        [Required(ErrorMessage = "Password is required")]
        [DataType(DataType.Password)]
        [Display(Name = "Password")]
        [MinLength(6, ErrorMessage = "Password must be at least 6 characters")]
        public string Password { get; set; } = string.Empty;

        [Required(ErrorMessage = "Confirm Password is required")]
        [DataType(DataType.Password)]
        [Display(Name = "Confirm Password")]
        [Compare("Password", ErrorMessage = "Passwords do not match")]
        public string ConfirmPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email is required")]
        [EmailAddress]
        [Display(Name = "Email")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "First Name is required")]
        [Display(Name = "First Name")]
        [MaxLength(200)]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "Last Name is required")]
        [Display(Name = "Last Name")]
        [MaxLength(200)]
        public string Surname { get; set; } = string.Empty;

        [Required(ErrorMessage = "Shipping Address is required")]
        [Display(Name = "Shipping Address")]
        [MaxLength(500)]
        public string ShippingAddress { get; set; } = string.Empty;

        [Required(ErrorMessage = "Please select your role")]
        [Display(Name = "Register as")]
        public string Role { get; set; } = "Customer"; // "Admin" or "Customer"
    }
}

