using System.Collections.Generic;
using System.Linq;

namespace ABCRetailers.Models.ViewModels
{
    public class CartViewModel
    {
        public List<CartItemViewModel> Items { get; set; } = new List<CartItemViewModel>();

        public decimal Total => Items.Sum(item => item.Subtotal);

        public int ItemCount => Items.Sum(item => item.Quantity);
    }
}
