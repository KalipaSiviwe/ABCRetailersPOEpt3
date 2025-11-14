// Models/CartItem.cs
using System.ComponentModel.DataAnnotations;

namespace ABCRetailers.Models
{
    public class CartItem
    {
        public string ProductId { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
        public string ImageUrl { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public int Quantity { get; set; }
        public int StockAvailable { get; set; }

        public decimal Subtotal => Price * Quantity;
    }
}

