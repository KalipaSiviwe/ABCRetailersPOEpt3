// Attributes/RequireLoginAttribute.cs
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Http;
using System.Linq;

namespace ABCRetailers.Attributes
{
    public class RequireLoginAttribute : ActionFilterAttribute
    {
        public string Roles { get; set; } = string.Empty; // Comma-separated roles, e.g., "Admin,Customer"

        public override void OnActionExecuting(ActionExecutingContext context)
        {
            var userId = context.HttpContext.Session.GetString("UserId");
            var userRole = context.HttpContext.Session.GetString("Role");

            if (string.IsNullOrEmpty(userId))
            {
                // Not logged in, redirect to login page
                context.Result = new RedirectToActionResult("Login", "Login", null);
                return;
            }

            // If specific roles are required, check if user has one of them
            if (!string.IsNullOrEmpty(Roles))
            {
                var requiredRoles = Roles.Split(',').Select(r => r.Trim()).ToList();
                if (!requiredRoles.Contains(userRole))
                {
                    // Logged in but unauthorized role, redirect to access denied
                    context.Result = new RedirectToActionResult("AccessDenied", "Home", null);
                    return;
                }
            }

            base.OnActionExecuting(context);
        }
    }
}

