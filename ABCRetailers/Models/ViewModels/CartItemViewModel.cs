// Models/ViewModels/CartItemViewModel.cs
namespace ABCRetailers.Models.ViewModels
{
    public class CartItemViewModel
    {
        public int CartId { get; set; }
        public string ProductId { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
        public string ImageUrl { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public int Quantity { get; set; }
        public int StockAvailable { get; set; }

        public decimal Subtotal => Price * Quantity;
    }
}

