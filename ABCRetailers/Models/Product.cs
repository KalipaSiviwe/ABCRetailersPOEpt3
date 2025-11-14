// Models/Product.cs
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ABCRetailers.Models
{
    public class Product
    {
        [Key]
        [Display(Name = "Product ID")]
        public string ProductId { get; set; } = Guid.NewGuid().ToString();

        [Required]
        [Display(Name = "Product Name")]
        [MaxLength(200)]
        public string ProductName { get; set; } = string.Empty;

        [Display(Name = "Description")]
        [MaxLength(1000)]
        public string Description { get; set; } = string.Empty;

        [Required(ErrorMessage = "Price is required")]
        [Range(0.01, double.MaxValue, ErrorMessage = "Price must be greater than $0.00")]
        [Display(Name = "Price")]
        [DataType(DataType.Currency)]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Price { get; set; }

        [Required]
        [Display(Name = "Stock Available")]
        public int StockAvailable { get; set; }

        [Display(Name = "Image URL")]
        [MaxLength(500)]
        public string ImageUrl { get; set; } = string.Empty;
    }
}
