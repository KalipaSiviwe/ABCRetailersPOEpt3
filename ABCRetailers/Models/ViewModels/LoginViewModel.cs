// Models/ViewModels/LoginViewModel.cs
using System.ComponentModel.DataAnnotations;

namespace ABCRetailers.Models.ViewModels
{
    public class LoginViewModel
    {
        [Required(ErrorMessage = "Username is required")]
        [Display(Name = "Username")]
        public string Username { get; set; } = string.Empty;

        [Required(ErrorMessage = "Password is required")]
        [DataType(DataType.Password)]
        [Display(Name = "Password")]
        public string Password { get; set; } = string.Empty;

        [Required(ErrorMessage = "Please select your role")]
        [Display(Name = "Login as")]
        public string Role { get; set; } = "Customer"; // "Admin" or "Customer"

        [Display(Name = "Remember me")]
        public bool RememberMe { get; set; }
    }
}

