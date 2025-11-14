// Models/Customer.cs
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ABCRetailers.Models
{
    public class Customer
    {
        [Key]
        [Display(Name = "Customer ID")]
        public string CustomerId { get; set; } = Guid.NewGuid().ToString();

        [Required]
        [Display(Name = "First Name")]
        [MaxLength(200)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Last Name")]
        [MaxLength(200)]
        public string Surname { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Username")]
        [MaxLength(100)]
        public string Username { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        [Display(Name = "Email")]
        [MaxLength(255)]
        public string Email { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Shipping Address")]
        [MaxLength(500)]
        public string ShippingAddress { get; set; } = string.Empty;
    }
}
