using Microsoft.AspNetCore.Mvc;
using ABCRetailers.Attributes;
using ABCRetailers.Models;
using ABCRetailers.Models.ViewModels;
using ABCRetailers.Services;
using System.Text.Json;

namespace ABCRetailers.Controllers
{
    [RequireLogin]
    public class OrderController : Controller
    {
        private readonly ISqlDatabaseService _sqlService;
        private readonly IAzureFunctionsService _functionsService;

        public OrderController(ISqlDatabaseService sqlService, IAzureFunctionsService functionsService)
        {
            _sqlService = sqlService;
            _functionsService = functionsService;
        }

        public async Task<IActionResult> Index()
        {
            var role = HttpContext.Session.GetString("Role");
            var username = HttpContext.Session.GetString("Username");

            if (role == "Admin")
            {
                // Admin sees all orders
                var orders = await _sqlService.GetAllOrdersAsync();
                return View(orders);
            }
            else
            {
                // Customer sees only their orders
                var allOrders = await _sqlService.GetAllOrdersAsync();
                var myOrders = allOrders.Where(o => o.Username == username).ToList();
                return View(myOrders);
            }
        }

        public async Task<IActionResult> Details(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return NotFound();
            }

            var order = await _sqlService.GetOrderByIdAsync(id);
            if (order == null)
            {
                return NotFound();
            }

            // Check if customer is viewing their own order
            var role = HttpContext.Session.GetString("Role");
            var username = HttpContext.Session.GetString("Username");
            if (role == "Customer" && order.Username != username)
            {
                return RedirectToAction("AccessDenied", "Home");
            }

            return View(order);
        }

        [RequireLogin(Roles = "Admin")]
        public async Task<IActionResult> Create()
        {
            var customers = await _sqlService.GetAllCustomersAsync();
            var products = await _sqlService.GetAllProductsAsync();

            var viewModel = new OrderCreateViewModel
            {
                Customers = customers,
                Products = products
            };

            return View(viewModel);
        }

        [RequireLogin(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(OrderCreateViewModel model)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    // Get customer and product details
                    var customer = await _sqlService.GetCustomerByIdAsync(model.CustomerId);
                    var product = await _sqlService.GetProductByIdAsync(model.ProductId);

                    if (customer == null || product == null)
                    {
                        ModelState.AddModelError("", "Invalid customer or product selected.");
                        await PopulateDropdowns(model);
                        return View(model);
                    }

                    // Check stock availability
                    if (product.StockAvailable < model.Quantity)
                    {
                        ModelState.AddModelError("Quantity", $"Insufficient stock. Available: {product.StockAvailable}");
                        await PopulateDropdowns(model);
                        return View(model);
                    }

                    // Create order
                    var order = new Order
                    {
                        CustomerId = model.CustomerId,
                        Username = customer.Username,
                        ProductId = model.ProductId,
                        ProductName = product.ProductName,
                        OrderDate = DateTime.SpecifyKind(model.OrderDate, DateTimeKind.Utc),
                        Quantity = model.Quantity,
                        UnitPrice = product.Price,
                        TotalPrice = product.Price * model.Quantity,
                        Status = "Submitted"
                    };

                    await _sqlService.AddOrderAsync(order);

                    // Update product stock
                    product.StockAvailable -= model.Quantity;
                    await _sqlService.UpdateProductAsync(product);

                    // Note: Queue messages can still be sent using Azure Storage Service if needed
                    // For now, we'll skip this to focus on SQL Database migration

                    TempData["Success"] = "Order created successfully!";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    // Log the full exception for debugging
                    Console.WriteLine($"Error creating order: {ex.Message}");
                    Console.WriteLine($"Stack trace: {ex.StackTrace}");

                    ModelState.AddModelError("", $"Error creating order: {ex.Message}");
                    await PopulateDropdowns(model);
                    return View(model);
                }
            }

            await PopulateDropdowns(model);
            return View(model);
        }

        [RequireLogin(Roles = "Admin")]
        public async Task<IActionResult> Edit(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return NotFound();
            }

            var order = await _sqlService.GetOrderByIdAsync(id);
            if (order == null)
            {
                return NotFound();
            }

            return View(order);
        }

        [RequireLogin(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Order order)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    await _sqlService.UpdateOrderAsync(order);
                    TempData["Success"] = "Order updated successfully!";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", $"Error updating order: {ex.Message}");
                }
            }
            return View(order);
        }

        [RequireLogin(Roles = "Admin")]
        [HttpPost]
        public async Task<IActionResult> Delete(string id)
        {
            try
            {
                await _sqlService.DeleteOrderAsync(id);
                TempData["Success"] = "Order deleted successfully!";
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error deleting order: {ex.Message}";
            }
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<JsonResult> GetProductPrice(string productId)
        {
            try
            {
                var product = await _sqlService.GetProductByIdAsync(productId);
                if (product != null)
                {
                    return Json(new { success = true, price = product.Price, stock = product.StockAvailable, productName = product.ProductName });
                }
                return Json(new { success = false });
            }
            catch
            {
                return Json(new { success = false });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateStatus(string orderId, string newStatus)
        {
            try
            {
                var order = await _sqlService.GetOrderByIdAsync(orderId);
                if (order == null)
                {
                    return NotFound();
                }

                order.Status = newStatus;
                await _sqlService.UpdateOrderAsync(order);

                // Use Azure Functions for advanced order processing
                await _functionsService.ProcessOrderAsync(orderId, newStatus.ToLower());

                TempData["Success"] = $"Order status updated to {newStatus} successfully!";
                return RedirectToAction(nameof(Details), new { id = orderId });
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error updating order status: {ex.Message}";
                return RedirectToAction(nameof(Details), new { id = orderId });
            }
        }

        [HttpPost]
        public async Task<IActionResult> UpdateOrderStatus([FromBody] UpdateOrderStatusRequest request)
        {
            try
            {
                var order = await _sqlService.GetOrderByIdAsync(request.Id);
                if (order == null)
                {
                    return Json(new { success = false, message = "Order not found" });
                }

                order.Status = request.NewStatus;
                await _sqlService.UpdateOrderAsync(order);

                // Use Azure Functions for advanced order processing
                await _functionsService.ProcessOrderAsync(request.Id, request.NewStatus.ToLower());

                return Json(new { success = true, message = "Status updated successfully" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // New action to get order status from Azure Functions
        [HttpGet]
        public async Task<JsonResult> GetOrderStatusFromFunctions(string orderId)
        {
            try
            {
                var statusResponse = await _functionsService.GetOrderStatusAsync(orderId);
                if (statusResponse != null)
                {
                    return Json(new { success = true, status = statusResponse });
                }
                return Json(new { success = false, message = "Order not found" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [RequireLogin(Roles = "Customer")]
        public async Task<IActionResult> MyOrders()
        {
            var username = HttpContext.Session.GetString("Username");
            if (string.IsNullOrEmpty(username))
            {
                TempData["Error"] = "Please login to view your orders.";
                return RedirectToAction("Login", "Login");
            }

            var allOrders = await _sqlService.GetAllOrdersAsync();
            var myOrders = allOrders.Where(o => o.Username == username).ToList();
            return View(myOrders);
        }

        [RequireLogin(Roles = "Admin")]
        public async Task<IActionResult> Manage()
        {
            var orders = await _sqlService.GetAllOrdersAsync();
            return View(orders);
        }

        [RequireLogin(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateOrderStatusToProcessed(string orderId)
        {
            try
            {
                var order = await _sqlService.GetOrderByIdAsync(orderId);
                if (order == null)
                {
                    TempData["Error"] = "Order not found.";
                    return RedirectToAction(nameof(Manage));
                }

                order.Status = "PROCESSED";
                await _sqlService.UpdateOrderAsync(order);

                TempData["Success"] = "Order status updated to PROCESSED successfully!";
                return RedirectToAction(nameof(Manage));
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error updating order status: {ex.Message}";
                return RedirectToAction(nameof(Manage));
            }
        }

        private async Task PopulateDropdowns(OrderCreateViewModel model)
        {
            model.Customers = await _sqlService.GetAllCustomersAsync();
            model.Products = await _sqlService.GetAllProductsAsync();
        }
    }

    public class UpdateOrderStatusRequest
    {
        public string Id { get; set; } = string.Empty;
        public string NewStatus { get; set; } = string.Empty;
    }
}