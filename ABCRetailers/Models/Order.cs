// Models/Order.cs
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ABCRetailers.Models
{
    public class Order
    {
        [Key]
        [DisplayName("Order ID")]
        public string OrderId { get; set; } = Guid.NewGuid().ToString();

        [Required]
        [DisplayName("Customer")]
        [MaxLength(450)]
        public string CustomerId { get; set; } = string.Empty;

        [DisplayName("Username")]
        [MaxLength(100)]
        public string Username { get; set; } = string.Empty;

        [Required]
        [DisplayName("Product")]
        [MaxLength(450)]
        public string ProductId { get; set; } = string.Empty;

        [DisplayName("Product Name")]
        [MaxLength(200)]
        public string ProductName { get; set; } = string.Empty;

        [Required]
        [DisplayName("Order Date")]
        [DataType(DataType.Date)]
        public DateTime OrderDate { get; set; } = DateTime.Today;

        [Required]
        [DisplayName("Quantity")]
        [Range(1, int.MaxValue, ErrorMessage = "Quantity must be at least 1")]
        public int Quantity { get; set; }

        [DisplayName("Unit Price")]
        [DataType(DataType.Currency)]
        [Column(TypeName = "decimal(18,2)")]
        public decimal UnitPrice { get; set; }

        [DisplayName("Total Price")]
        [DataType(DataType.Currency)]
        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalPrice { get; set; }

        [Required]
        [DisplayName("Status")]
        [MaxLength(50)]
        public string Status { get; set; } = "Submitted";
    }

    public enum OrderStatus
    {
        Submitted,   // When order is first created
        Processing,  // When company opens/reviews the order
        Completed,   // When order is delivered to customer
        Cancelled    // When order is cancelled
    }
}
