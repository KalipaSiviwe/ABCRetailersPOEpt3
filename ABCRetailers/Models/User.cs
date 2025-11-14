// Models/User.cs
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ABCRetailers.Models
{
    public class User
    {
        [Key]
        public int UserId { get; set; }

        [Required]
        [MaxLength(100)]
        [Display(Name = "Username")]
        public string Username { get; set; } = string.Empty;

        [Required]
        [MaxLength(255)]
        [Display(Name = "Password")]
        public string Password { get; set; } = string.Empty;

        [Required]
        [MaxLength(50)]
        [Display(Name = "Role")]
        public string Role { get; set; } = "Customer"; // "Customer" or "Admin"
    }
}

