// program.cs
using ABCRetailers.Data;
using ABCRetailers.Models;
using ABCRetailers.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Localization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace ABCRetailers
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.
            builder.Services.AddControllersWithViews();

            // Configure SQL Database
            var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
                ?? builder.Configuration.GetConnectionString("AzureSQL");

            // Use local SQL Server for development if no connection string is provided
            if (string.IsNullOrEmpty(connectionString))
            {
                connectionString = "Server=(localdb)\\mssqllocaldb;Database=ABCRetailersDb;Trusted_Connection=True;MultipleActiveResultSets=true";
            }

            // Always register DbContext and SQL Database Service
            builder.Services.AddDbContext<AuthDbContext>(options =>
                options.UseSqlServer(connectionString));

            // Register SQL Database Service
            builder.Services.AddScoped<ISqlDatabaseService, SqlDatabaseService>();

            // Register Azure Storage Service (for blob storage, queues, file shares)
            builder.Services.AddScoped<IAzureStorageService, AzureStorageService>();

            // Add HttpClient services for Azure Functions integration
            builder.Services.AddHttpClient("AzureFunctions", client =>
            {
                client.BaseAddress = new Uri(builder.Configuration["AzureFunctions:BaseUrl"] ?? "http://localhost:7071");
                client.DefaultRequestHeaders.Add("x-functions-key", builder.Configuration["AzureFunctions:FunctionKey"] ?? "");
            });

            // Register Azure Functions services
            builder.Services.AddScoped<IAzureFunctionsService, AzureFunctionsService>();

            // Register Cart Service
            builder.Services.AddScoped<ICartService, CartService>();

            // Add session support for shopping cart
            builder.Services.AddDistributedMemoryCache();
            builder.Services.AddSession(options =>
            {
                options.IdleTimeout = TimeSpan.FromMinutes(30);
                options.Cookie.HttpOnly = true;
                options.Cookie.IsEssential = true;
            });

            // Add authentication services
            builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
                .AddCookie(options =>
                {
                    options.LoginPath = "/Login/Login";
                    options.AccessDeniedPath = "/Home/AccessDenied";
                });

            // Add logging
            builder.Services.AddLogging();

            var app = builder.Build();

            // Set culture for decimal handling (FIXES PRICE ISSUE)
            var culture = new CultureInfo("en-US");
            CultureInfo.DefaultThreadCurrentCulture = culture;
            CultureInfo.DefaultThreadCurrentUICulture = culture;

            var supportedCultures = new[] { culture };
            var localizationOptions = new RequestLocalizationOptions
            {
                DefaultRequestCulture = new RequestCulture(culture),
                SupportedCultures = supportedCultures,
                SupportedUICultures = supportedCultures
            };

            // Configure the HTTP request pipeline.
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Home/Error");
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();
            app.UseRequestLocalization(localizationOptions);
            app.UseRouting();
            app.UseSession(); // Enable session middleware
            app.UseAuthentication(); // Enable authentication middleware (must come before UseAuthorization)
            app.UseAuthorization();

            // Ensure database is created
            using (var scope = app.Services.CreateScope())
            {
                var services = scope.ServiceProvider;
                try
                {
                    var context = services.GetRequiredService<AuthDbContext>();
                    context.Database.EnsureCreated(); // Creates database if it doesn't exist
                }
                catch (Exception ex)
                {
                    var logger = services.GetRequiredService<ILogger<Program>>();
                    logger.LogError(ex, "An error occurred creating the database: {Message}", ex.Message);
                }
            }

            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Index}/{id?}");

            app.Run();
        }
    }
}
