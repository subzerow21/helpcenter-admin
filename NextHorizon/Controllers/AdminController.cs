using Microsoft.AspNetCore.Mvc;
using NextHorizon.Models.Admin_Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Data.SqlClient;
using NextHorizon.Services.AdminServices;
using System.Data;
using System.Threading.Tasks;
using System;
using System.Collections.Generic;
using NextHorizon.Models;
using System.Linq;

namespace NextHorizon.Controllers
{
    public class AdminController : Controller
    {
        private readonly DashboardService _dashboardService = new DashboardService();
        private readonly IConfiguration _configuration;
        private readonly string _connectionString;

        // Constructor to inject configuration
        public AdminController(IConfiguration configuration)
        {
            _configuration = configuration;
            _connectionString = _configuration.GetConnectionString("DefaultConnection");
        }

        // Helper method to check if user is logged in
        private bool IsUserLoggedIn()
        {
            return HttpContext.Session.GetInt32("StaffId").HasValue;
        }

        // Helper method to get current user's role
        private string GetCurrentUserRole()
        {
            return HttpContext.Session.GetString("UserType") ?? "Unknown";
        }

        // Helper method to check if user has specific role
        private bool HasRole(string allowedRole)
        {
            var userRole = GetCurrentUserRole();
            return userRole == allowedRole || userRole == "SuperAdmin"; // SuperAdmin can access everything
        }

        // Helper method to check if user has any of the allowed roles
        private bool HasAnyRole(string[] allowedRoles)
        {
            var userRole = GetCurrentUserRole();
            return userRole == "SuperAdmin" || allowedRoles.Contains(userRole);
        }

        // Helper method to redirect to login if not authenticated
        private IActionResult RedirectToLoginIfNotAuthenticated()
        {
            if (!IsUserLoggedIn())
            {
                return RedirectToAction("AdminLogin", "Login");
            }
            return null;
        }

        // Helper method to redirect if unauthorized
        private IActionResult RedirectIfUnauthorized(string[] allowedRoles, string actionName = null)
        {
            if (!HasAnyRole(allowedRoles))
            {
                // Log unauthorized access attempt
                var staffId = HttpContext.Session.GetInt32("StaffId");
                if (staffId.HasValue)
                {
                    var username = HttpContext.Session.GetString("Username") ?? "Unknown";
                    var attemptedAction = actionName ?? ControllerContext.ActionDescriptor.ActionName;
                    
                    // You can log this to audit_logs if you want
                    Console.WriteLine($"UNAUTHORIZED ACCESS ATTEMPT: User {username} (Role: {GetCurrentUserRole()}) tried to access {attemptedAction}");
                }
                
                // Redirect to dashboard with error message
                TempData["ErrorMessage"] = "You don't have permission to access this page.";
                return RedirectToAction("Dashboard");
            }
            return null;
        }

        // URL: /Admin/Dashboard - Accessible by all admin roles
        public IActionResult Dashboard()
        {
            var redirect = RedirectToLoginIfNotAuthenticated();
            if (redirect != null) return redirect;

            var model = _dashboardService.GetHeroStats();
            
            // Pass user role to view for UI customization
            ViewBag.UserRole = GetCurrentUserRole();
            
            return View(model);
        }

        // URL: /Admin/Analytics - Accessible by SuperAdmin, Admin, and Finance Officer
        public IActionResult Analytics()
        {
            var redirect = RedirectToLoginIfNotAuthenticated();
            if (redirect != null) return redirect;

            var unauthorized = RedirectIfUnauthorized(new[] { "SuperAdmin", "Admin", "Finance Officer" });
            if (unauthorized != null) return unauthorized;

            // Define raw product data for the "Most Purchased" section
            var productData = new List<ProductMetric>
            {
                new ProductMetric { ProductName = "UltraBoost v2", UnitsSold = 142, Revenue = 852000m, Category = "Footwear", Stock = 25 },
                new ProductMetric { ProductName = "Hydration Pack", UnitsSold = 98, Revenue = 147000m, Category = "Accessories", Stock = 5 },
                new ProductMetric { ProductName = "Swift Shorts", UnitsSold = 76, Revenue = 38000m, Category = "Apparel", Stock = 40 },
                new ProductMetric { ProductName = "Pro GPS Watch", UnitsSold = 185, Revenue = 675000m, Category = "Electronics", Stock = 12 },
                new ProductMetric { ProductName = "Compression Socks", UnitsSold = 210, Revenue = 42000m, Category = "Apparel", Stock = 150 }
            };

            var viewModel = new AnalyticsViewModel
            {
                // Top Header Cards
                TotalConsumers = 12450,
                TotalSellers = 84,
                TotalRevenue = 7954000m,
                AverageOrderValue = 4850.00,
                ChallengeToSaleConversionRate = 32.8,
                UsersPurchased = 1640,
                TotalOrders = 1240,

                // Main Area Chart Data
                PerformanceTrends = new List<AnalyticsChartData>
                {
                    new AnalyticsChartData { DateLabel = "Mar 01", ChallengeParticipants = 120, TotalRevenue = 110000m },
                    new AnalyticsChartData { DateLabel = "Mar 02", ChallengeParticipants = 150, TotalRevenue = 135000m },
                    new AnalyticsChartData { DateLabel = "Mar 03", ChallengeParticipants = 95,  TotalRevenue = 95000m },
                    new AnalyticsChartData { DateLabel = "Mar 04", ChallengeParticipants = 210, TotalRevenue = 160000m },
                    new AnalyticsChartData { DateLabel = "Mar 05", ChallengeParticipants = 300, TotalRevenue = 210000m },
                    new AnalyticsChartData { DateLabel = "Mar 06", ChallengeParticipants = 280, TotalRevenue = 195000m },
                    new AnalyticsChartData { DateLabel = "Mar 07", ChallengeParticipants = 350, TotalRevenue = 240000m }
                },

                // Sidebar: Top Sellers (Take 5 for the small list)
                TopSellers = new List<SellerMetric>
                {
                    new SellerMetric { Rank = 1, SellerName = "Elite Strides Ph", ShopName = "Elite Sports Store", OrdersFulfilled = 452, RevenueGenerated = 1254000.50m },
                    new SellerMetric { Rank = 2, SellerName = "Mountain Peak Gear", ShopName = "Peak Adventure Shop", OrdersFulfilled = 310, RevenueGenerated = 890600.00m },
                    new SellerMetric { Rank = 3, SellerName = "Urban Runner Co.", ShopName = "Urban Runner Flagship", OrdersFulfilled = 285, RevenueGenerated = 412000.75m },
                    new SellerMetric { Rank = 4, SellerName = "Velocity Sports", ShopName = "Velocity Metro", OrdersFulfilled = 198, RevenueGenerated = 356000.00m },
                    new SellerMetric { Rank = 5, SellerName = "HydroFlow Official", ShopName = "HydroFlow PH", OrdersFulfilled = 156, RevenueGenerated = 98000.25m }
                }.OrderByDescending(s => s.RevenueGenerated).ToList(),

                // Bottom: Most Purchased Products
                TopMovingProducts = productData.OrderByDescending(p => p.UnitsSold).Take(5).ToList(),

                // Peak Activity Chart Data
                PeakEngagementData = new List<HourlyEngagementMetric>
                {
                    new HourlyEngagementMetric { Hour = 0, ActivitySyncCount = 20, PurchaseCount = 5 },
                    new HourlyEngagementMetric { Hour = 6, ActivitySyncCount = 420, PurchaseCount = 15 },
                    new HourlyEngagementMetric { Hour = 9, ActivitySyncCount = 150, PurchaseCount = 40 },
                    new HourlyEngagementMetric { Hour = 12, ActivitySyncCount = 110, PurchaseCount = 85 },
                    new HourlyEngagementMetric { Hour = 17, ActivitySyncCount = 580, PurchaseCount = 60 },
                    new HourlyEngagementMetric { Hour = 21, ActivitySyncCount = 310, PurchaseCount = 190 }
                }.OrderBy(h => h.Hour).ToList()
            };

            ViewBag.UserRole = GetCurrentUserRole();
            return View(viewModel);
        }

        // URL: /Admin/SellerPerformance - Accessible by SuperAdmin, Admin, and Finance Officer
        public IActionResult SellerPerformance()
        {
            var redirect = RedirectToLoginIfNotAuthenticated();
            if (redirect != null) return redirect;

            var unauthorized = RedirectIfUnauthorized(new[] { "SuperAdmin", "Admin", "Finance Officer" });
            if (unauthorized != null) return unauthorized;

            // Detailed Seller List for Table
            var mockSellers = new List<SellerMetric>
            {
                new SellerMetric { Rank = 1, SellerName = "Nike Official", ShopName = "Nike PH Store", OrdersFulfilled = 1450, RevenueGenerated = 850400.00m, GrowthPercentage = 12.5 },
                new SellerMetric { Rank = 2, SellerName = "Adidas Philippines", ShopName = "Adidas PH Store", OrdersFulfilled = 1200, RevenueGenerated = 720300.50m, GrowthPercentage = 8.2 },
                new SellerMetric { Rank = 3, SellerName = "Under Armour", ShopName = "UA Performance Center", OrdersFulfilled = 890, RevenueGenerated = 450000.75m, GrowthPercentage = 15.0 },
                new SellerMetric { Rank = 4, SellerName = "Titan 22", ShopName = "Titan Basketball", OrdersFulfilled = 760, RevenueGenerated = 310200.20m, GrowthPercentage = 5.4 },
                new SellerMetric { Rank = 5, SellerName = "Puma Metro", ShopName = "Puma Metro Hub", OrdersFulfilled = 540, RevenueGenerated = 120500.00m, GrowthPercentage = -2.1 }
            };

            // Detailed Product List for Tab 2
            var mockProducts = new List<ProductMetric>
            {
                new ProductMetric { ProductName = "Vaporfly Next%", Category = "Running", SalesCount = 320, Revenue = 1500000m },
                new ProductMetric { ProductName = "Yoga Mat Pro", Category = "Fitness", SalesCount = 280, Revenue = 56000m },
                new ProductMetric { ProductName = "Dumbbell Set 10kg", Category = "Fitness", SalesCount = 150, Revenue = 75000m },
                new ProductMetric { ProductName = "Stainless Water Bottle", Category = "Accessories", SalesCount = 410, Revenue = 92000m },
                new ProductMetric { ProductName = "Performance Socks", Category = "Apparel", SalesCount = 520, Revenue = 25000m }
             }.OrderByDescending(p => p.Revenue).ToList();

            var viewModel = new AnalyticsViewModel
            {
                TotalSellers = 124,
                TotalConsumers = 1540,
                TotalRevenue = 2451401.45m,
                TotalOrders = 5840,
                TopSellers = mockSellers,
                TopProducts = mockProducts
            };

            ViewBag.UserRole = GetCurrentUserRole();
            return View(viewModel);
        }

        // Consumers - Accessible by SuperAdmin, Admin, and Support Agent
        public IActionResult Consumers()
        {
            var redirect = RedirectToLoginIfNotAuthenticated();
            if (redirect != null) return redirect;

            var unauthorized = RedirectIfUnauthorized(new[] { "SuperAdmin", "Admin", "Support Agent" });
            if (unauthorized != null) return unauthorized;

            ViewBag.UserRole = GetCurrentUserRole();
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> GetConsumers(string viewType = "active")
        {
            try
            {
                var consumers = new List<ConsumerViewModel>();
                
                using (var connection = new SqlConnection(_connectionString))
                {
                    using (var command = new SqlCommand("sp_GetConsumers", connection))
                    {
                        command.CommandType = CommandType.StoredProcedure;
                        command.Parameters.AddWithValue("@ViewType", viewType);
                        
                        await connection.OpenAsync();
                        
                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                if (viewType == "active")
                                {
                                    consumers.Add(new ConsumerViewModel
                                    {
                                        ConsumerId = reader.GetInt32(reader.GetOrdinal("consumer_id")),
                                        FullName = reader.GetString(reader.GetOrdinal("full_name")),
                                        PhoneNumber = reader.GetString(reader.GetOrdinal("phone_number")),
                                        Email = reader.GetString(reader.GetOrdinal("email")),
                                        Address = reader.GetString(reader.GetOrdinal("address")),
                                        DateJoined = reader.GetString(reader.GetOrdinal("date_joined"))
                                    });
                                }
                                else
                                {
                                    consumers.Add(new ConsumerViewModel
                                    {
                                        ConsumerId = reader.GetInt32(reader.GetOrdinal("consumer_id")),
                                        FullName = reader.GetString(reader.GetOrdinal("full_name")),
                                        PhoneNumber = reader.GetString(reader.GetOrdinal("phone_number")),
                                        Email = reader.GetString(reader.GetOrdinal("email")),
                                        Address = reader.GetString(reader.GetOrdinal("address"))
                                    });
                                }
                            }
                        }
                    }
                }
                
                return Json(consumers);
            }
            catch (Exception ex)
            {
                return Json(new { error = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> DeleteConsumer([FromBody] DeleteConsumerRequest request)
        {
            try
            {
                var staffId = HttpContext.Session.GetInt32("StaffId") ?? 0;
                var adminName = HttpContext.Session.GetString("Username") ?? "System";
                
                using (var connection = new SqlConnection(_connectionString))
                {
                    using (var command = new SqlCommand("sp_DeleteConsumer", connection))
                    {
                        command.CommandType = CommandType.StoredProcedure;
                        command.Parameters.AddWithValue("@ConsumerId", request.ConsumerId);
                        command.Parameters.AddWithValue("@StaffId", staffId);
                        command.Parameters.AddWithValue("@AdminName", adminName);
                        
                        await connection.OpenAsync();
                        
                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            if (await reader.ReadAsync())
                            {
                                var status = reader["Status"].ToString();
                                var message = reader["Message"].ToString();
                                
                                return Json(new { success = status == "Success", message = message });
                            }
                        }
                    }
                }
                
                return Json(new { success = false, message = "Delete failed" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> RestoreConsumer([FromBody] RestoreConsumerRequest request)
        {
            try
            {
                var staffId = HttpContext.Session.GetInt32("StaffId") ?? 0;
                var adminName = HttpContext.Session.GetString("Username") ?? "System";
                
                using (var connection = new SqlConnection(_connectionString))
                {
                    using (var command = new SqlCommand("sp_RestoreConsumer", connection))
                    {
                        command.CommandType = CommandType.StoredProcedure;
                        command.Parameters.AddWithValue("@ConsumerId", request.ConsumerId);
                        command.Parameters.AddWithValue("@StaffId", staffId);
                        command.Parameters.AddWithValue("@AdminName", adminName);
                        
                        await connection.OpenAsync();
                        
                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            if (await reader.ReadAsync())
                            {
                                var status = reader["Status"].ToString();
                                var message = reader["Message"].ToString();
                                
                                return Json(new { success = status == "Success", message = message });
                            }
                        }
                    }
                }
                
                return Json(new { success = false, message = "Restore failed" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // Sellers - Accessible by SuperAdmin, Admin, and Support Agent
        public IActionResult Sellers()
        {
            var redirect = RedirectToLoginIfNotAuthenticated();
            if (redirect != null) return redirect;

            var unauthorized = RedirectIfUnauthorized(new[] { "SuperAdmin", "Admin", "Support Agent" });
            if (unauthorized != null) return unauthorized;

            ViewBag.UserRole = GetCurrentUserRole();
            return View();
        }

        // FinanceRequest - Accessible ONLY by SuperAdmin and Finance Officer
        public IActionResult FinanceRequest()
        {
            var redirect = RedirectToLoginIfNotAuthenticated();
            if (redirect != null) return redirect;

            var unauthorized = RedirectIfUnauthorized(new[] { "SuperAdmin", "Finance Officer" });
            if (unauthorized != null) return unauthorized;

            var viewModel = new FinanceRequestsViewModel
            {
                PendingPayouts = new List<PayoutRequest>
                {
                    new PayoutRequest { Id = "1001", ShopName = "TechGear Hub", SellerEmail = "owner@techgear.com", Amount = 15500.50m, BankName = "GCash", SellerNote = "Funds Transfer", DateRequested = DateTime.Now.AddDays(-2) },
                    new PayoutRequest { Id = "1002", ShopName = "Luxe Apparel", SellerEmail = "sales@luxe.ph", Amount = 42000.00m, BankName = "BDO", SellerNote = "Withdrawal", DateRequested = DateTime.Now.AddDays(-1) }
                },
                PendingDiscounts = new List<DiscountRequest>
                {
                    new DiscountRequest { Id = "D-55", ShopName = "TechGear Hub", ProductName = "Wireless Earbuds Pro", OriginalPrice = 2500, DiscountPercent = 20, StartDate = DateTime.Now, EndDate = DateTime.Now.AddDays(7) },
                    new DiscountRequest { Id = "D-56", ShopName = "Home Essentials", ProductName = "Ergonomic Chair", OriginalPrice = 8999, DiscountPercent = 50, StartDate = DateTime.Now.AddDays(2), EndDate = DateTime.Now.AddDays(5) }
                }
            };

            ViewBag.UserRole = GetCurrentUserRole();
            return View("FinanceRequest", viewModel);
        }

        // Logistics - Accessible by SuperAdmin, Admin, and Logistics team
        public IActionResult Logistics()
        {
            var redirect = RedirectToLoginIfNotAuthenticated();
            if (redirect != null) return redirect;

            var unauthorized = RedirectIfUnauthorized(new[] { "SuperAdmin", "Admin", "Logistics" });
            if (unauthorized != null) return unauthorized;

            ViewBag.UserRole = GetCurrentUserRole();
            return View();
        }

        // Tasks - Accessible by all admin roles
        public IActionResult Tasks()
        {
            var redirect = RedirectToLoginIfNotAuthenticated();
            if (redirect != null) return redirect;

            ViewBag.UserRole = GetCurrentUserRole();
            return View();
        }

        // ChallengeDetails - Accessible by SuperAdmin, Admin, and Marketing
        public IActionResult ChallengeDetails()
        {
            var redirect = RedirectToLoginIfNotAuthenticated();
            if (redirect != null) return redirect;

            var unauthorized = RedirectIfUnauthorized(new[] { "SuperAdmin", "Admin", "Marketing" });
            if (unauthorized != null) return unauthorized;

            ViewBag.UserRole = GetCurrentUserRole();
            return View();
        }

        // HelpCenter - Accessible by all admin roles
        public IActionResult HelpCenter()
        {
            var redirect = RedirectToLoginIfNotAuthenticated();
            if (redirect != null) return redirect;

            ViewBag.UserRole = GetCurrentUserRole();
            return View();
        }

        // Settings - Accessible ONLY by SuperAdmin
        public IActionResult Settings()
        {
            var redirect = RedirectToLoginIfNotAuthenticated();
            if (redirect != null) return redirect;

            var unauthorized = RedirectIfUnauthorized(new[] { "SuperAdmin" });
            if (unauthorized != null) return unauthorized;

            ViewBag.UserRole = GetCurrentUserRole();
            return View();
        }

        // Notifications - Accessible by all admin roles
        public IActionResult Notifications()
        {
            var redirect = RedirectToLoginIfNotAuthenticated();
            if (redirect != null) return redirect;

            ViewBag.UserRole = GetCurrentUserRole();
            return View();
        }

        // Logout - Accessible by all (clears session)
        public IActionResult Logout()
        {
            var staffId = HttpContext.Session.GetInt32("StaffId");
            var username = HttpContext.Session.GetString("Username");
            
            if (staffId.HasValue)
            {
                Console.WriteLine($"User {username} logged out");
                // You can also log to audit_logs here
            }
            
            // Clear session
            HttpContext.Session.Clear();
            
            // Redirect to login page
            return RedirectToAction("AdminLogin", "Login");
        }
    }

    // Request Models
    public class DeleteConsumerRequest
    {
        public int ConsumerId { get; set; }
    }

    public class RestoreConsumerRequest
    {
        public int ConsumerId { get; set; }
    }
}