// Controllers/LoginController.cs
using ABCRetailers.Data;
using ABCRetailers.Models;
using ABCRetailers.Models.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Security.Claims;

namespace ABCRetailers.Controllers
{
    public class LoginController : Controller
    {
        private readonly AuthDbContext _context;

        public LoginController(AuthDbContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            return View();
        }

        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (ModelState.IsValid)
            {
                var hashedPassword = HashPassword(model.Password);
                var user = await _context.Users
                    .FirstOrDefaultAsync(u => u.Username == model.Username && u.Password == hashedPassword);

                if (user != null)
                {
                    // Validate that the selected role matches the user's role in database
                    if (user.Role != model.Role)
                    {
                        ModelState.AddModelError("Role", $"Invalid role selection. This account is registered as {user.Role}.");
                        return View(model);
                    }

                    // Set session variables
                    HttpContext.Session.SetString("UserId", user.UserId.ToString());
                    HttpContext.Session.SetString("Username", user.Username);
                    HttpContext.Session.SetString("Role", user.Role);

                    // Get associated customer ID if available
                    var customer = await _context.Customers.FirstOrDefaultAsync(c => c.Username == user.Username);
                    if (customer != null)
                    {
                        HttpContext.Session.SetString("CustomerId", customer.CustomerId);
                    }

                    // Create authentication cookie
                    var claims = new List<Claim>
                    {
                        new Claim(ClaimTypes.Name, user.Username),
                        new Claim(ClaimTypes.Role, user.Role),
                        new Claim("UserId", user.UserId.ToString())
                    };

                    var claimsIdentity = new ClaimsIdentity(
                        claims, CookieAuthenticationDefaults.AuthenticationScheme);

                    var authProperties = new AuthenticationProperties
                    {
                        IsPersistent = model.RememberMe
                    };

                    await HttpContext.SignInAsync(
                        CookieAuthenticationDefaults.AuthenticationScheme,
                        new ClaimsPrincipal(claimsIdentity),
                        authProperties);

                    if (user.Role == "Admin")
                    {
                        return RedirectToAction("AdminDashboard", "Home");
                    }
                    else
                    {
                        return RedirectToAction("CustomerDashboard", "Home");
                    }
                }
                else
                {
                    ModelState.AddModelError("", "Invalid username or password");
                }
            }
            return View(model);
        }

        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (ModelState.IsValid)
            {
                // Check if username already exists
                if (await _context.Users.AnyAsync(u => u.Username == model.Username))
                {
                    ModelState.AddModelError("Username", "Username already exists");
                    return View(model);
                }

                // Check if email already exists in Customers
                if (await _context.Customers.AnyAsync(c => c.Email == model.Email))
                {
                    ModelState.AddModelError("Email", "Email already registered");
                    return View(model);
                }

                // Create User with selected role
                var user = new User
                {
                    Username = model.Username,
                    Password = HashPassword(model.Password),
                    Role = model.Role // Use the selected role from the form
                };

                _context.Users.Add(user);
                await _context.SaveChangesAsync();

                // Create Customer (only if role is Customer, or create for both roles for consistency)
                var customer = new Customer
                {
                    CustomerId = Guid.NewGuid().ToString(),
                    Name = model.Name,
                    Surname = model.Surname,
                    Username = model.Username,
                    Email = model.Email,
                    ShippingAddress = model.ShippingAddress
                };

                _context.Customers.Add(customer);
                await _context.SaveChangesAsync();

                TempData["Success"] = "Registration successful! Please login.";
                return RedirectToAction(nameof(Login));
            }
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Logout()
        {
            HttpContext.Session.Clear();
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Index", "Home");
        }

        private string HashPassword(string password)
        {
            using (var sha256 = SHA256.Create())
            {
                var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
                return Convert.ToBase64String(hashedBytes);
            }
        }
    }
}

