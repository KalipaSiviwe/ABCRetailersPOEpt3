// Models/ViewModels/CheckoutViewModel.cs
using ABCRetailers.Models;
using ABCRetailers.Models.ViewModels;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace ABCRetailers.Models.ViewModels
{
    public class CheckoutViewModel
    {
        public List<CartItemViewModel> CartItems { get; set; } = new List<CartItemViewModel>();

        public decimal Total => CartItems.Sum(item => item.Subtotal);

        [Required(ErrorMessage = "Please select a customer")]
        [Display(Name = "Customer")]
        public string CustomerId { get; set; } = string.Empty;

        public List<Customer> Customers { get; set; } = new List<Customer>();
    }
}

