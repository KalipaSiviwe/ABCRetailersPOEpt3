using Microsoft.AspNetCore.Mvc;
using ABCRetailers.Attributes;
using ABCRetailers.Models;
using ABCRetailers.Services;
using Microsoft.AspNetCore.Http;
using System.Linq;

namespace ABCRetailers.Controllers
{
    [RequireLogin]
    public class ProductController : Controller
    {
        private readonly ISqlDatabaseService _sqlService;
        private readonly IAzureFunctionsService _functionsService;
        private readonly IAzureStorageService _storageService;

        public ProductController(ISqlDatabaseService sqlService, IAzureFunctionsService functionsService, IAzureStorageService storageService)
        {
            _sqlService = sqlService;
            _functionsService = functionsService;
            _storageService = storageService;
        }

        public async Task<IActionResult> Index()
        {
            var products = await _sqlService.GetAllProductsAsync();
            return View(products);
        }

        public async Task<IActionResult> Details(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return NotFound();
            }

            var product = await _sqlService.GetProductByIdAsync(id);
            if (product == null)
            {
                return NotFound();
            }

            return View(product);
        }

        [RequireLogin(Roles = "Admin")]
        public IActionResult Create()
        {
            return View();
        }

        [RequireLogin(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Product product, IFormFile? imageFile)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    // Upload image directly to Azure Blob Storage if provided
                    if (imageFile != null && imageFile.Length > 0)
                    {
                        try
                        {
                            var imageUrl = await _storageService.UploadImageAsync(imageFile, "product-images");
                            if (!string.IsNullOrEmpty(imageUrl))
                            {
                                product.ImageUrl = imageUrl;
                            }
                        }
                        catch (Exception ex)
                        {
                            ModelState.AddModelError("", $"Error uploading image: {ex.Message}");
                            return View(product);
                        }
                    }

                    await _sqlService.AddProductAsync(product);
                    TempData["Success"] = "Product created successfully!";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", $"Error creating product: {ex.Message}");
                }
            }
            return View(product);
        }

        [RequireLogin(Roles = "Admin")]
        public async Task<IActionResult> Edit(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return NotFound();
            }

            var product = await _sqlService.GetProductByIdAsync(id);
            if (product == null)
            {
                return NotFound();
            }

            return View(product);
        }

        [RequireLogin(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Product product, IFormFile? imageFile)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    // Get existing product to preserve ImageUrl if not uploading new file
                    var existingProduct = await _sqlService.GetProductByIdAsync(product.ProductId);
                    if (existingProduct == null)
                    {
                        ModelState.AddModelError("", "Product not found.");
                        return View(product);
                    }

                    // Update properties from form
                    existingProduct.ProductName = product.ProductName;
                    existingProduct.Description = product.Description;
                    existingProduct.Price = product.Price;
                    existingProduct.StockAvailable = product.StockAvailable;

                    // Upload new image directly to Azure Blob Storage if provided
                    if (imageFile != null && imageFile.Length > 0)
                    {
                        try
                        {
                            var imageUrl = await _storageService.UploadImageAsync(imageFile, "product-images");
                            if (!string.IsNullOrEmpty(imageUrl))
                            {
                                existingProduct.ImageUrl = imageUrl;
                            }
                        }
                        catch (Exception ex)
                        {
                            ModelState.AddModelError("", $"Error uploading image: {ex.Message}");
                            return View(product);
                        }
                    }

                    await _sqlService.UpdateProductAsync(existingProduct);
                    TempData["Success"] = "Product updated successfully!";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", $"Error updating product: {ex.Message}");
                }
            }
            return View(product);
        }

        [RequireLogin(Roles = "Admin")]
        [HttpPost]
        public async Task<IActionResult> Delete(string id)
        {
            try
            {
                await _sqlService.DeleteProductAsync(id);
                TempData["Success"] = "Product deleted successfully!";
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error deleting product: {ex.Message}";
            }
            return RedirectToAction(nameof(Index));
        }

        // New action to get low stock products using Azure Functions
        [RequireLogin(Roles = "Admin")]
        [HttpGet]
        public async Task<IActionResult> LowStock(int threshold = 10)
        {
            try
            {
                var lowStockProducts = await _functionsService.GetLowStockProductsAsync(threshold);
                ViewBag.Threshold = threshold;
                return View(lowStockProducts);
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error getting low stock products: {ex.Message}";
                return RedirectToAction(nameof(Index));
            }
        }

        // New action to update stock using Azure Functions
        [RequireLogin(Roles = "Admin")]
        [HttpPost]
        public async Task<IActionResult> UpdateStock(string productId, int newStock, string reason)
        {
            try
            {
                var success = await _functionsService.UpdateStockAsync(
                    productId,
                    newStock,
                    User.Identity?.Name ?? "System",
                    reason);

                if (success)
                {
                    TempData["Success"] = "Stock updated successfully!";
                }
                else
                {
                    TempData["Error"] = "Failed to update stock.";
                }
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error updating stock: {ex.Message}";
            }

            return RedirectToAction(nameof(Index));
        }

        // New action to get stock history using Azure Functions
        [HttpGet]
        public async Task<JsonResult> GetStockHistory(string productId)
        {
            try
            {
                var history = await _functionsService.GetStockHistoryAsync(productId);
                return Json(new { success = true, data = history });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }
    }
}