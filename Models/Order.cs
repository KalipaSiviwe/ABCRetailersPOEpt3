using Azure;
using Azure.Data.Tables;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace ABCRetailers.Models
{
    public class Order : ITableEntity
    {
        public string PartitionKey { get; set; } = "Order";
        public string RowKey { get; set; } = Guid.NewGuid().ToString();
        public DateTimeOffset? Timestamp { get; set; }
        public ETag ETag { get; set; }

        [DisplayName("Order ID")]
        public string OrderId => RowKey;

        [Required]
        [DisplayName("Customer")]
        public string CustomerId { get; set; } = string.Empty;

        [DisplayName("Username")]
        public string Username { get; set; } = string.Empty;

        [Required]
        [DisplayName("Product")]
        public string ProductId { get; set; } = string.Empty;

        [DisplayName("Product Name")]
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
        public double UnitPrice { get; set; } // Use double for Azure compatibility

        [DisplayName("Total Price")]
        [DataType(DataType.Currency)]
        public double TotalPrice { get; set; } // Use double for Azure compatibility

        [Required]
        [DisplayName("Status")]
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