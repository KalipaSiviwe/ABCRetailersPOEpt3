using Microsoft.AspNetCore.Mvc;
using ABCRetailers.Models;
using ABCRetailers.Models.ViewModels;
using ABCRetailers.Services;
using System.Text.Json;

namespace ABCRetailers.Controllers
{
    public class OrderController : Controller
    {
        private readonly IAzureStorageService _storageService;

        public OrderController(IAzureStorageService storageService)
        {
            _storageService = storageService;
        }

        public async Task<IActionResult> Index()
        {
            var orders = await _storageService.GetAllEntitiesAsync<Order>();
            return View(orders);
        }

        public async Task<IActionResult> Details(string id)
        {
            var order = await _storageService.GetEntityAsync<Order>("Order", id);
            if (order == null)
            {
                return NotFound();
            }

            return View(order);
        }

        public async Task<IActionResult> Create()
        {
            var model = new OrderCreateViewModel();
            await PopulateDropdowns(model);
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(OrderCreateViewModel model)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    // Get customer and product details
                    var customer = await _storageService.GetEntityAsync<Customer>("Customer", model.CustomerId);
                    var product = await _storageService.GetEntityAsync<Product>("Product", model.ProductId);

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

                    // Create order with double values for Azure compatibility
                    var order = new Order
                    {
                        CustomerId = model.CustomerId,
                        Username = customer.Username,
                        ProductId = model.ProductId,
                        ProductName = product.ProductName,
                        OrderDate = DateTime.SpecifyKind(model.OrderDate, DateTimeKind.Utc),
                        Quantity = model.Quantity,
                        UnitPrice = product.Price, // No cast needed - both are double
                        TotalPrice = product.Price * model.Quantity, // No cast needed
                        Status = "Submitted"
                    };

                    await _storageService.AddEntityAsync(order);

                    // Update product stock
                    product.StockAvailable -= model.Quantity;
                    await _storageService.UpdateEntityAsync(product);

                    TempData["Success"] = "Order created successfully!";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", $"Error creating order: {ex.Message}");
                    await PopulateDropdowns(model);
                    return View(model);
                }
            }

            await PopulateDropdowns(model);
            return View(model);
        }

        public async Task<IActionResult> Edit(string id)
        {
            var order = await _storageService.GetEntityAsync<Order>("Order", id);
            if (order == null)
            {
                return NotFound();
            }

            return View(order);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(string id, Order order)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var existingOrder = await _storageService.GetEntityAsync<Order>("Order", id);
                    if (existingOrder == null)
                    {
                        return NotFound();
                    }

                    // Only update the status and order date
                    existingOrder.Status = order.Status;
                    existingOrder.OrderDate = order.OrderDate;

                    await _storageService.UpdateEntityAsync(existingOrder);

                    TempData["Success"] = "Order updated successfully!";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", $"Error updating order: {ex.Message}");
                    return View(order);
                }
            }

            return View(order);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(string id)
        {
            try
            {
                var order = await _storageService.GetEntityAsync<Order>("Order", id);
                if (order == null)
                {
                    return NotFound();
                }

                // Restore product stock
                var product = await _storageService.GetEntityAsync<Product>("Product", order.ProductId);
                if (product != null)
                {
                    product.StockAvailable += order.Quantity;
                    await _storageService.UpdateEntityAsync(product);
                }

                await _storageService.DeleteEntityAsync<Order>("Order", id);

                TempData["Success"] = "Order deleted successfully!";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error deleting order: {ex.Message}";
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateStatus(string orderId, string newStatus)
        {
            try
            {
                var order = await _storageService.GetEntityAsync<Order>("Order", orderId);
                if (order == null)
                {
                    return NotFound();
                }

                order.Status = newStatus;
                await _storageService.UpdateEntityAsync(order);

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
                var order = await _storageService.GetEntityAsync<Order>("Order", request.Id);
                if (order == null)
                {
                    return Json(new { success = false, message = "Order not found" });
                }

                order.Status = request.NewStatus;
                await _storageService.UpdateEntityAsync(order);

                return Json(new { success = true, message = "Status updated successfully" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> FixExistingOrders()
        {
            try
            {
                var orders = await _storageService.GetAllEntitiesAsync<Order>();
                int fixedCount = 0;

                foreach (var order in orders)
                {
                    var product = await _storageService.GetEntityAsync<Product>("Product", order.ProductId);
                    if (product != null && product.Price > 0)
                    {
                        // Fix existing orders by updating their prices
                        order.UnitPrice = product.Price;
                        order.TotalPrice = product.Price * order.Quantity;

                        await _storageService.UpdateEntityAsync(order);
                        fixedCount++;
                    }
                }

                TempData["Success"] = $"Fixed {fixedCount} existing orders with correct prices!";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error fixing orders: {ex.Message}";
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpGet]
        public async Task<IActionResult> DebugOrders()
        {
            try
            {
                var orders = await _storageService.GetAllEntitiesAsync<Order>();
                var debugInfo = new List<object>();

                foreach (var order in orders)
                {
                    var product = await _storageService.GetEntityAsync<Product>("Product", order.ProductId);
                    debugInfo.Add(new
                    {
                        OrderId = order.OrderId,
                        ProductId = order.ProductId,
                        ProductName = order.ProductName,
                        UnitPrice = order.UnitPrice,
                        TotalPrice = order.TotalPrice,
                        Quantity = order.Quantity,
                        Status = order.Status,
                        ProductPrice = product?.Price ?? 0,
                        ProductStock = product?.StockAvailable ?? 0
                    });
                }

                return Json(new { success = true, orders = debugInfo });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        private async Task PopulateDropdowns(OrderCreateViewModel model)
        {
            model.Customers = await _storageService.GetAllEntitiesAsync<Customer>();
            model.Products = await _storageService.GetAllEntitiesAsync<Product>();
        }
    }

    public class UpdateOrderStatusRequest
    {
        public string Id { get; set; } = string.Empty;
        public string NewStatus { get; set; } = string.Empty;
    }
}