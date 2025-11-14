// Controllers/HomeController.cs
using ABCRetailers.Attributes;
using ABCRetailers.Models;
using ABCRetailers.Models.ViewModels;
using ABCRetailers.Services;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace ABCRetailers.Controllers
{
    public class HomeController : Controller
    {
        private readonly ISqlDatabaseService _sqlService;

        public HomeController(ISqlDatabaseService sqlService)
        {
            _sqlService = sqlService;
        }

        public IActionResult Index()
        {
            var userId = HttpContext.Session.GetString("UserId");

            // If not logged in, redirect to login
            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToAction("Login", "Login");
            }

            // If logged in, redirect to appropriate dashboard
            var role = HttpContext.Session.GetString("Role");
            if (role == "Admin")
            {
                return RedirectToAction("AdminDashboard");
            }
            else
            {
                return RedirectToAction("CustomerDashboard");
            }
        }

        public IActionResult Welcome()
        {
            return View();
        }

        [RequireLogin]
        public IActionResult Privacy()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> InitializeStorage()
        {
            try
            {
                // Database is initialized automatically via migrations
                TempData["Success"] = "Database initialized successfully!";
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Failed to initialize database: {ex.Message}";
            }

            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> AdminDashboard()
        {
            var role = HttpContext.Session.GetString("Role");
            if (role != "Admin")
            {
                return RedirectToAction("AccessDenied", "Home");
            }

            var products = await _sqlService.GetAllProductsAsync();
            var customers = await _sqlService.GetAllCustomersAsync();
            var orders = await _sqlService.GetAllOrdersAsync();

            var viewModel = new HomeViewModel
            {
                FeaturedProducts = products.Take(5).ToList(),
                ProductCount = products.Count,
                CustomerCount = customers.Count,
                OrderCount = orders.Count
            };

            return View(viewModel);
        }

        public async Task<IActionResult> CustomerDashboard()
        {
            var userIdStr = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userIdStr))
            {
                return RedirectToAction("Login", "Login");
            }

            var products = await _sqlService.GetAllProductsAsync();
            var viewModel = new HomeViewModel
            {
                FeaturedProducts = products.Take(5).ToList(),
                ProductCount = products.Count
            };

            return View(viewModel);
        }

        public IActionResult AccessDenied()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}