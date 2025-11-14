// Controllers/ProductController.cs
using Microsoft.AspNetCore.Mvc;
using ABCRetailers.Models;
using ABCRetailers.Services;

namespace ABCRetailers.Controllers
{
    public class ProductController : Controller
    {
        private readonly IAzureStorageService _storageService;
        private readonly ILogger<ProductController> _logger;

        public ProductController(IAzureStorageService storageService, ILogger<ProductController> logger)
        {
            _storageService = storageService;
            _logger = logger;
        }

        public async Task<IActionResult> Index()
        {
            var products = await _storageService.GetAllEntitiesAsync<Product>();
            return View(products);
        }

        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Product product, IFormFile? imageFile)
        {
            if (!ModelState.IsValid)
            {
                return View(product);
            }

            try
            {
                if (imageFile != null && imageFile.Length > 0)
                {
                    var imageUrl = await _storageService.UploadImageAsync(imageFile, "product-images");
                    product.ImageUrl = imageUrl;
                }

                await _storageService.AddEntityAsync(product);
                TempData["Success"] = $"Product '{product.ProductName}' created successfully with price {product.Price:C}";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating product");
                ModelState.AddModelError("", $"Error creating product: {ex.Message}");
                return View(product);
            }
        }

        public async Task<IActionResult> Edit(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return NotFound();
            }

            var product = await _storageService.GetEntityAsync<Product>("Product", id);
            if (product == null)
            {
                return NotFound();
            }

            return View(product);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Product product, IFormFile? imageFile)
        {
            if (!ModelState.IsValid)
            {
                return View(product);
            }

            try
            {
                var original = await _storageService.GetEntityAsync<Product>("Product", product.RowKey);
                if (original == null)
                {
                    return NotFound();
                }

                original.ProductName = product.ProductName;
                original.Description = product.Description;
                original.Price = product.Price;
                original.StockAvailable = product.StockAvailable;

                if (imageFile != null && imageFile.Length > 0)
                {
                    var imageUrl = await _storageService.UploadImageAsync(imageFile, "product-images");
                    original.ImageUrl = imageUrl;
                }

                await _storageService.UpdateEntityAsync(original);
                TempData["Success"] = "Product updated successfully!";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating product");
                ModelState.AddModelError("", $"Error updating product: {ex.Message}");
                return View(product);
            }
        }

        [HttpPost]
        public async Task<IActionResult> Delete(string id)
        {
            try
            {
                await _storageService.DeleteEntityAsync<Product>("Product", id);
                TempData["Success"] = "Product deleted successfully!";
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error deleting product: {ex.Message}";
            }
            return RedirectToAction(nameof(Index));
        }
    }
}