// Controllers/CartController.cs
using ABCRetailers.Attributes;
using ABCRetailers.Data;
using ABCRetailers.Models;
using ABCRetailers.Models.ViewModels;
using ABCRetailers.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace ABCRetailers.Controllers
{
    [RequireLogin]
    public class CartController : Controller
    {
        private readonly ICartService _cartService;
        private readonly ISqlDatabaseService _sqlService;
        private readonly AuthDbContext _context;

        public CartController(ICartService cartService, ISqlDatabaseService sqlService, AuthDbContext context)
        {
            _cartService = cartService;
            _sqlService = sqlService;
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var userIdStr = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out int userId))
            {
                TempData["Error"] = "Please login to view your cart.";
                return RedirectToAction("Login", "Login");
            }

            var dbCartItems = await _sqlService.GetUserCartAsync(userId);
            var cartItemsList = new List<CartItemViewModel>();
            foreach (var c in dbCartItems)
            {
                cartItemsList.Add(new CartItemViewModel
                {
                    CartId = c.CartId,
                    ProductId = c.ProductId,
                    ProductName = c.Product?.ProductName ?? "",
                    ImageUrl = c.Product?.ImageUrl ?? "",
                    Price = c.Product?.Price ?? 0,
                    Quantity = c.Quantity,
                    StockAvailable = c.Product?.StockAvailable ?? 0
                });
            }

            var viewModel = new CartViewModel
            {
                Items = cartItemsList
            };
            return View(viewModel);
        }

        [HttpPost]
        public async Task<IActionResult> AddToCart([FromBody] AddToCartRequest? request)
        {
            if (request == null || string.IsNullOrEmpty(request.ProductId))
            {
                return Json(new { success = false, message = "Invalid request" });
            }

            var userIdStr = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out int userId))
            {
                return Json(new { success = false, message = "Please login to add items to cart" });
            }

            try
            {
                var product = await _sqlService.GetProductByIdAsync(request.ProductId);
                if (product == null)
                {
                    return Json(new { success = false, message = "Product not found" });
                }

                var quantity = request.Quantity > 0 ? request.Quantity : 1;

                if (quantity <= 0)
                {
                    return Json(new { success = false, message = "Quantity must be greater than 0" });
                }

                if (product.StockAvailable < quantity)
                {
                    return Json(new { success = false, message = $"Only {product.StockAvailable} items available in stock" });
                }

                var existingCartItem = await _sqlService.GetCartItemAsync(userId, request.ProductId);
                if (existingCartItem != null)
                {
                    existingCartItem.Quantity += quantity;
                    if (existingCartItem.Quantity > product.StockAvailable)
                    {
                        existingCartItem.Quantity = product.StockAvailable;
                    }
                    await _sqlService.UpdateCartItemAsync(existingCartItem);
                }
                else
                {
                    var cart = new Cart
                    {
                        UserId = userId,
                        ProductId = request.ProductId,
                        Quantity = quantity > product.StockAvailable ? product.StockAvailable : quantity,
                        DateAdded = DateTime.UtcNow
                    };
                    await _sqlService.AddCartItemAsync(cart);
                }

                var cartCount = await GetCartCountAsync(userId);
                return Json(new { success = true, message = "Product added to cart", cartCount });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> RemoveFromCart([FromBody] RemoveFromCartRequest request)
        {
            var userIdStr = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out int userId))
            {
                return Json(new { success = false, message = "Please login" });
            }

            try
            {
                var cartItem = await _sqlService.GetCartItemAsync(userId, request.ProductId);
                if (cartItem != null)
                {
                    await _sqlService.DeleteCartItemAsync(cartItem.CartId);
                }
                var cartCount = await GetCartCountAsync(userId);
                return Json(new { success = true, message = "Item removed from cart", cartCount });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> UpdateQuantity([FromBody] UpdateQuantityRequest request)
        {
            var userIdStr = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out int userId))
            {
                return Json(new { success = false, message = "Please login" });
            }

            try
            {
                var cartItem = await _sqlService.GetCartItemAsync(userId, request.ProductId);
                if (cartItem == null)
                {
                    return Json(new { success = false, message = "Cart item not found" });
                }

                var product = await _sqlService.GetProductByIdAsync(request.ProductId);
                if (product == null)
                {
                    return Json(new { success = false, message = "Product not found" });
                }

                if (request.Quantity <= 0)
                {
                    await _sqlService.DeleteCartItemAsync(cartItem.CartId);
                }
                else if (request.Quantity <= product.StockAvailable)
                {
                    cartItem.Quantity = request.Quantity;
                    await _sqlService.UpdateCartItemAsync(cartItem);
                }
                else
                {
                    return Json(new { success = false, message = $"Only {product.StockAvailable} items available" });
                }

                var cartItems = await _sqlService.GetUserCartAsync(userId);
                var updatedItem = cartItems.FirstOrDefault(c => c.ProductId == request.ProductId);
                var total = cartItems.Sum(c => (c.Product?.Price ?? 0) * c.Quantity);
                var cartCount = await GetCartCountAsync(userId);

                if (updatedItem != null)
                {
                    return Json(new
                    {
                        success = true,
                        subtotal = (double)((updatedItem.Product?.Price ?? 0) * updatedItem.Quantity),
                        total = (double)total,
                        cartCount
                    });
                }

                return Json(new { success = true, total = (double)total, cartCount });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetCartCount()
        {
            var userIdStr = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out int userId))
            {
                return Json(new { cartCount = 0 });
            }

            var cartCount = await GetCartCountAsync(userId);
            return Json(new { cartCount });
        }

        public async Task<IActionResult> Checkout()
        {
            var userIdStr = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out int userId))
            {
                TempData["Error"] = "Please login to checkout.";
                return RedirectToAction("Login", "Login");
            }

            var cartItems = await _sqlService.GetUserCartAsync(userId);
            if (!cartItems.Any())
            {
                TempData["Error"] = "Your cart is empty. Add items before checkout.";
                return RedirectToAction(nameof(Index));
            }

            var username = HttpContext.Session.GetString("Username");
            var customer = await _context.Customers.FirstOrDefaultAsync(c => c.Username == username);

            var viewModel = new CheckoutViewModel
            {
                CartItems = cartItems.Select(c => new CartItemViewModel
                {
                    CartId = c.CartId,
                    ProductId = c.ProductId,
                    ProductName = c.Product?.ProductName ?? "",
                    ImageUrl = c.Product?.ImageUrl ?? "",
                    Price = c.Product?.Price ?? 0,
                    Quantity = c.Quantity,
                    StockAvailable = c.Product?.StockAvailable ?? 0
                }).ToList(),
                CustomerId = customer?.CustomerId ?? "",
                Customers = customer != null ? new List<Customer> { customer } : new List<Customer>()
            };

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ProcessCheckout(CheckoutViewModel model)
        {
            var userIdStr = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out int userId))
            {
                TempData["Error"] = "Please login to checkout.";
                return RedirectToAction("Login", "Login");
            }

            var cartItems = await _sqlService.GetUserCartAsync(userId);
            if (!cartItems.Any())
            {
                TempData["Error"] = "Your cart is empty.";
                return RedirectToAction(nameof(Index));
            }

            try
            {
                var username = HttpContext.Session.GetString("Username");
                var customer = await _context.Customers.FirstOrDefaultAsync(c => c.Username == username);
                if (customer == null)
                {
                    TempData["Error"] = "Customer not found.";
                    return RedirectToAction(nameof(Checkout));
                }

                var orders = new List<Order>();
                foreach (var cartItem in cartItems)
                {
                    var product = cartItem.Product;
                    if (product == null)
                    {
                        continue;
                    }

                    if (product.StockAvailable < cartItem.Quantity)
                    {
                        TempData["Error"] = $"Insufficient stock for {product.ProductName}. Available: {product.StockAvailable}";
                        return RedirectToAction(nameof(Index));
                    }

                    var order = new Order
                    {
                        CustomerId = customer.CustomerId,
                        Username = customer.Username,
                        ProductId = product.ProductId,
                        ProductName = product.ProductName,
                        OrderDate = DateTime.UtcNow,
                        Quantity = cartItem.Quantity,
                        UnitPrice = product.Price,
                        TotalPrice = product.Price * cartItem.Quantity,
                        Status = "Submitted"
                    };

                    orders.Add(order);
                    await _sqlService.AddOrderAsync(order);

                    // Update product stock
                    product.StockAvailable -= cartItem.Quantity;
                    await _sqlService.UpdateProductAsync(product);
                }

                // Clear cart after successful checkout
                await _sqlService.ClearUserCartAsync(userId);

                TempData["Success"] = $"Successfully created {orders.Count} order(s)!";
                return RedirectToAction("Confirmation", "Cart");
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error processing checkout: {ex.Message}";
                return RedirectToAction(nameof(Checkout));
            }
        }

        public IActionResult Confirmation()
        {
            return View();
        }

        private async Task<int> GetCartCountAsync(int userId)
        {
            var cartItems = await _sqlService.GetUserCartAsync(userId);
            return cartItems.Sum(c => c.Quantity);
        }
    }

    public class AddToCartRequest
    {
        public string ProductId { get; set; } = string.Empty;
        public int Quantity { get; set; } = 1;
    }

    public class RemoveFromCartRequest
    {
        public string ProductId { get; set; } = string.Empty;
    }

    public class UpdateQuantityRequest
    {
        public string ProductId { get; set; } = string.Empty;
        public int Quantity { get; set; }
    }
}
