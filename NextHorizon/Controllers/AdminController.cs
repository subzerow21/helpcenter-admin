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

        //tanginamo
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
            return userRole == allowedRole || userRole == "SuperAdmin";
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
        public async Task<IActionResult> Analytics()
        {
            var redirect = RedirectToLoginIfNotAuthenticated();
            if (redirect != null) return redirect;
            var unauthorized = RedirectIfUnauthorized(new[] { "SuperAdmin", "Admin", "Finance Officer" });
            if (unauthorized != null) return unauthorized;

            var topSellers       = new List<SellerMetric>();
            var topProducts      = new List<ProductMetric>();
            var performanceTrends = new List<AnalyticsChartData>();
            var peakEngagement   = new List<HourlyEngagementMetric>();

            int totalConsumers = 0, totalSellers = 0, totalOrders = 0;
            decimal totalRevenue = 0, avgOrderValue = 0;

            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();

                // Total Consumers
                using (var cmd = new SqlCommand("SELECT COUNT(*) FROM Consumers", connection))
                    totalConsumers = (int)await cmd.ExecuteScalarAsync();

                // Total Active Sellers
                using (var cmd = new SqlCommand("SELECT COUNT(*) FROM Sellers WHERE seller_status = 'Active'", connection))
                    totalSellers = (int)await cmd.ExecuteScalarAsync();

                // Total Orders & Revenue & Avg Order Value
                var orderStatsSql = @"
                    SELECT 
                        COUNT(*) AS TotalOrders,
                        ISNULL(SUM(TotalAmount), 0) AS TotalRevenue,
                        ISNULL(AVG(TotalAmount), 0) AS AvgOrderValue
                    FROM Orders";
                using (var cmd = new SqlCommand(orderStatsSql, connection))
                using (var reader = await cmd.ExecuteReaderAsync())
                    if (await reader.ReadAsync())
                    {
                        totalOrders   = reader.GetInt32(reader.GetOrdinal("TotalOrders"));
                        totalRevenue  = reader.GetDecimal(reader.GetOrdinal("TotalRevenue"));
                        avgOrderValue = reader.GetDecimal(reader.GetOrdinal("AvgOrderValue"));
                    }

                // Performance Trends - Revenue per day (last 7 days)
                var trendSql = @"
                    SELECT 
                        CONVERT(VARCHAR, OrderDate, 107) AS DateLabel,
                        ISNULL(SUM(TotalAmount), 0) AS TotalRevenue,
                        COUNT(DISTINCT UserID) AS ChallengeParticipants
                    FROM Orders
                    WHERE OrderDate >= DATEADD(DAY, -7, GETDATE())
                    GROUP BY CAST(OrderDate AS DATE), CONVERT(VARCHAR, OrderDate, 107)
                    ORDER BY CAST(OrderDate AS DATE)";
                using (var cmd = new SqlCommand(trendSql, connection))
                using (var reader = await cmd.ExecuteReaderAsync())
                    while (await reader.ReadAsync())
                        performanceTrends.Add(new AnalyticsChartData
                        {
                            DateLabel           = reader["DateLabel"]?.ToString() ?? "",
                            TotalRevenue        = reader.GetDecimal(reader.GetOrdinal("TotalRevenue")),
                            ChallengeParticipants = reader.GetInt32(reader.GetOrdinal("ChallengeParticipants"))
                        });

                // Top Sellers
                var sellerSql = @"
                    SELECT TOP 5
                        ROW_NUMBER() OVER (ORDER BY SUM(oi.Quantity * oi.UnitPrice) DESC) AS Rank,
                        s.business_name AS ShopName,
                        COUNT(DISTINCT oi.OrderID) AS OrdersFulfilled,
                        SUM(oi.Quantity * oi.UnitPrice) AS RevenueGenerated
                    FROM OrderItems oi
                    INNER JOIN Sellers s ON oi.SellerId = s.seller_id
                    WHERE oi.SellerId IS NOT NULL
                    GROUP BY oi.SellerId, s.business_name
                    ORDER BY RevenueGenerated DESC";
                using (var cmd = new SqlCommand(sellerSql, connection))
                using (var reader = await cmd.ExecuteReaderAsync())
                    while (await reader.ReadAsync())
                        topSellers.Add(new SellerMetric
                        {
                            Rank             = (int)reader.GetInt64(reader.GetOrdinal("Rank")),
                            SellerName       = reader["ShopName"]?.ToString() ?? "",
                            ShopName         = reader["ShopName"]?.ToString() ?? "",
                            OrdersFulfilled  = reader.GetInt32(reader.GetOrdinal("OrdersFulfilled")),
                            RevenueGenerated = reader.GetDecimal(reader.GetOrdinal("RevenueGenerated"))
                        });

                // Top Products
                var productSql = @"
                    SELECT TOP 5
                        p.ProductName,
                        p.Category,
                        SUM(oi.Quantity) AS UnitsSold,
                        SUM(oi.Quantity * oi.UnitPrice) AS Revenue
                    FROM OrderItems oi
                    INNER JOIN Products p ON oi.ProductID = p.ProductId
                    GROUP BY p.ProductId, p.ProductName, p.Category
                    ORDER BY UnitsSold DESC";
                using (var cmd = new SqlCommand(productSql, connection))
                using (var reader = await cmd.ExecuteReaderAsync())
                    while (await reader.ReadAsync())
                        topProducts.Add(new ProductMetric
                        {
                            ProductName = reader["ProductName"]?.ToString() ?? "",
                            Category    = reader["Category"]?.ToString()    ?? "",
                            UnitsSold   = reader.GetInt32(reader.GetOrdinal("UnitsSold")),
                            Revenue     = reader.GetDecimal(reader.GetOrdinal("Revenue"))
                        });

                // Peak Engagement - purchases per hour
                var peakSql = @"
                    SELECT 
                        DATEPART(HOUR, OrderDate) AS Hour,
                        COUNT(*) AS PurchaseCount,
                        COUNT(*) * 10 AS ActivitySyncCount
                    FROM Orders
                    GROUP BY DATEPART(HOUR, OrderDate)
                    ORDER BY Hour";
                using (var cmd = new SqlCommand(peakSql, connection))
                using (var reader = await cmd.ExecuteReaderAsync())
                    while (await reader.ReadAsync())
                        peakEngagement.Add(new HourlyEngagementMetric
                        {
                            Hour              = reader.GetInt32(reader.GetOrdinal("Hour")),
                            PurchaseCount     = reader.GetInt32(reader.GetOrdinal("PurchaseCount")),
                            ActivitySyncCount = reader.GetInt32(reader.GetOrdinal("ActivitySyncCount"))
                        });
            }

            // Fallback if no trend data
            if (!performanceTrends.Any())
                performanceTrends.Add(new AnalyticsChartData { DateLabel = "No Data", TotalRevenue = 0, ChallengeParticipants = 0 });

            var viewModel = new AnalyticsViewModel
            {
                TotalConsumers               = totalConsumers,
                TotalSellers                 = totalSellers,
                TotalRevenue                 = totalRevenue,
                TotalOrders                  = totalOrders,
                AverageOrderValue            = (double)avgOrderValue,
                ChallengeToSaleConversionRate = totalOrders > 0 && totalConsumers > 0
                    ? Math.Round((double)totalOrders / totalConsumers * 100, 1) : 0,
                PerformanceTrends    = performanceTrends,
                TopSellers           = topSellers,
                TopMovingProducts    = topProducts,
                PeakEngagementData   = peakEngagement.Any() ? peakEngagement : new List<HourlyEngagementMetric>
                {
                    new HourlyEngagementMetric { Hour = 0, ActivitySyncCount = 0, PurchaseCount = 0 }
                }
            };

            ViewBag.UserRole = GetCurrentUserRole();
            return View(viewModel);
        }

        [HttpGet]
        public async Task<IActionResult> GetAnalyticsData(int days = 30, string? startDate = null, string? endDate = null)
        {
            try
            {
                DateTime start, end;
                end = DateTime.Now;

                if (!string.IsNullOrEmpty(startDate) && !string.IsNullOrEmpty(endDate))
                {
                    start = DateTime.Parse(startDate);
                    end   = DateTime.Parse(endDate).AddDays(1);
                }
                else
                {
                    start = days switch
                    {
                        7  => DateTime.Now.AddDays(-7),
                        90 => DateTime.Now.AddDays(-90),
                        _  => DateTime.Now.AddDays(-30)
                    };
                }

                var trends     = new List<object>();
                var peakData   = new List<object>();
                int totalOrders = 0;
                decimal totalRevenue = 0, avgOrder = 0;

                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();

                    // Trends
                    var trendSql = @"
                        SELECT 
                            CONVERT(VARCHAR, OrderDate, 107) AS DateLabel,
                            ISNULL(SUM(TotalAmount), 0) AS TotalRevenue,
                            COUNT(DISTINCT UserID) AS ChallengeParticipants
                        FROM Orders
                        WHERE OrderDate >= @Start AND OrderDate < @End
                        GROUP BY CAST(OrderDate AS DATE), CONVERT(VARCHAR, OrderDate, 107)
                        ORDER BY CAST(OrderDate AS DATE)";
                    using (var cmd = new SqlCommand(trendSql, connection))
                    {
                        cmd.Parameters.AddWithValue("@Start", start);
                        cmd.Parameters.AddWithValue("@End",   end);
                        using (var reader = await cmd.ExecuteReaderAsync())
                            while (await reader.ReadAsync())
                                trends.Add(new
                                {
                                    dateLabel             = reader["DateLabel"]?.ToString() ?? "",
                                    totalRevenue          = reader.GetDecimal(reader.GetOrdinal("TotalRevenue")),
                                    challengeParticipants = reader.GetInt32(reader.GetOrdinal("ChallengeParticipants"))
                                });
                    }

                    // Stats
                    var statsSql = @"
                        SELECT COUNT(*) AS TotalOrders,
                            ISNULL(SUM(TotalAmount), 0) AS TotalRevenue,
                            ISNULL(AVG(TotalAmount), 0) AS AvgOrder
                        FROM Orders
                        WHERE OrderDate >= @Start AND OrderDate < @End";
                    using (var cmd = new SqlCommand(statsSql, connection))
                    {
                        cmd.Parameters.AddWithValue("@Start", start);
                        cmd.Parameters.AddWithValue("@End",   end);
                        using (var reader = await cmd.ExecuteReaderAsync())
                            if (await reader.ReadAsync())
                            {
                                totalOrders  = reader.GetInt32(reader.GetOrdinal("TotalOrders"));
                                totalRevenue = reader.GetDecimal(reader.GetOrdinal("TotalRevenue"));
                                avgOrder     = reader.GetDecimal(reader.GetOrdinal("AvgOrder"));
                            }
                    }

                    // Peak hours
                    var peakSql = @"
                        SELECT DATEPART(HOUR, OrderDate) AS Hour,
                            COUNT(*) AS PurchaseCount,
                            COUNT(*) * 10 AS ActivitySyncCount
                        FROM Orders
                        WHERE OrderDate >= @Start AND OrderDate < @End
                        GROUP BY DATEPART(HOUR, OrderDate)
                        ORDER BY Hour";
                    using (var cmd = new SqlCommand(peakSql, connection))
                    {
                        cmd.Parameters.AddWithValue("@Start", start);
                        cmd.Parameters.AddWithValue("@End",   end);
                        using (var reader = await cmd.ExecuteReaderAsync())
                            while (await reader.ReadAsync())
                                peakData.Add(new
                                {
                                    hour              = reader.GetInt32(reader.GetOrdinal("Hour")),
                                    purchaseCount     = reader.GetInt32(reader.GetOrdinal("PurchaseCount")),
                                    activitySyncCount = reader.GetInt32(reader.GetOrdinal("ActivitySyncCount"))
                                });
                    }
                }

                return Json(new
                {
                    trends,
                    peakData,
                    totalOrders,
                    totalRevenue,
                    avgOrderValue = avgOrder
                });
            }
            catch (Exception ex) { return Json(new { error = ex.Message }); }
        }


        // URL: /Admin/SellerPerformance - Accessible by SuperAdmin, Admin, and Finance Officer
        public async Task<IActionResult> SellerPerformance()
        {
            var redirect = RedirectToLoginIfNotAuthenticated();
            if (redirect != null) return redirect;
            var unauthorized = RedirectIfUnauthorized(new[] { "SuperAdmin", "Admin", "Finance Officer" });
            if (unauthorized != null) return unauthorized;

            var topSellers  = new List<SellerMetric>();
            var topProducts = new List<ProductMetric>();
            decimal totalRevenue = 0;
            int totalOrders = 0;

            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();

                // Top Sellers by Revenue
                var sellerSql = @"
                    SELECT 
                        ROW_NUMBER() OVER (ORDER BY SUM(oi.Quantity * oi.UnitPrice) DESC) AS Rank,
                        s.business_name AS ShopName,
                        COUNT(DISTINCT oi.OrderID) AS OrdersFulfilled,
                        SUM(oi.Quantity * oi.UnitPrice) AS RevenueGenerated
                    FROM OrderItems oi
                    INNER JOIN Sellers s ON oi.SellerId = s.seller_id
                    WHERE oi.SellerId IS NOT NULL
                    GROUP BY oi.SellerId, s.business_name
                    ORDER BY RevenueGenerated DESC";

                using (var cmd = new SqlCommand(sellerSql, connection))
                using (var reader = await cmd.ExecuteReaderAsync())
                    while (await reader.ReadAsync())
                        topSellers.Add(new SellerMetric
                        {
                            Rank             = (int)reader.GetInt64(reader.GetOrdinal("Rank")),
                            SellerName       = reader["ShopName"]?.ToString() ?? "",
                            ShopName         = reader["ShopName"]?.ToString() ?? "",
                            OrdersFulfilled  = reader.GetInt32(reader.GetOrdinal("OrdersFulfilled")),
                            RevenueGenerated = reader.GetDecimal(reader.GetOrdinal("RevenueGenerated"))
                        });

                // Top Moving Products
                var productSql = @"
                    SELECT TOP 10
                        p.ProductName,
                        p.Category,
                        SUM(oi.Quantity) AS SalesCount,
                        SUM(oi.Quantity * oi.UnitPrice) AS Revenue
                    FROM OrderItems oi
                    INNER JOIN Products p ON oi.ProductID = p.ProductId
                    GROUP BY p.ProductId, p.ProductName, p.Category
                    ORDER BY SalesCount DESC";

                using (var cmd = new SqlCommand(productSql, connection))
                using (var reader = await cmd.ExecuteReaderAsync())
                    while (await reader.ReadAsync())
                        topProducts.Add(new ProductMetric
                        {
                            ProductName = reader["ProductName"]?.ToString() ?? "",
                            Category    = reader["Category"]?.ToString()    ?? "",
                            SalesCount  = reader.GetInt32(reader.GetOrdinal("SalesCount")),
                            Revenue     = reader.GetDecimal(reader.GetOrdinal("Revenue"))
                        });

                // Platform totals
                var statsSql = "SELECT COUNT(*) AS TotalOrders, ISNULL(SUM(TotalAmount), 0) AS TotalRevenue FROM Orders";
                using (var cmd = new SqlCommand(statsSql, connection))
                using (var reader = await cmd.ExecuteReaderAsync())
                    if (await reader.ReadAsync())
                    {
                        totalOrders  = reader.GetInt32(reader.GetOrdinal("TotalOrders"));
                        totalRevenue = reader.GetDecimal(reader.GetOrdinal("TotalRevenue"));
                    }
            }

            var viewModel = new AnalyticsViewModel
            {
                TotalRevenue = totalRevenue,
                TotalOrders  = totalOrders,
                TotalSellers = topSellers.Count,
                TopSellers   = topSellers,
                TopProducts  = topProducts.OrderByDescending(p => p.Revenue).ToList()
            };

            ViewBag.UserRole = GetCurrentUserRole();
            return View(viewModel);
        }

        [HttpGet]
        public async Task<IActionResult> GetSellerPerformance()
        {
            try
            {
                var topSellers = new List<object>();

                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();

                    var sql = @"
                        SELECT 
                            ROW_NUMBER() OVER (ORDER BY SUM(oi.Quantity * oi.UnitPrice) DESC) AS Rank,
                            s.business_name AS ShopName,
                            COUNT(DISTINCT oi.OrderID) AS OrdersFulfilled,
                            SUM(oi.Quantity * oi.UnitPrice) AS RevenueGenerated
                        FROM OrderItems oi
                        INNER JOIN Sellers s ON oi.SellerId = s.seller_id
                        WHERE oi.SellerId IS NOT NULL
                        GROUP BY oi.SellerId, s.business_name
                        ORDER BY RevenueGenerated DESC";

                    using (var cmd = new SqlCommand(sql, connection))
                    using (var reader = await cmd.ExecuteReaderAsync())
                        while (await reader.ReadAsync())
                            topSellers.Add(new
                            {
                                rank             = (int)reader.GetInt64(reader.GetOrdinal("Rank")),
                                shopName         = reader["ShopName"]?.ToString() ?? "",
                                ordersFulfilled  = reader.GetInt32(reader.GetOrdinal("OrdersFulfilled")),
                                revenueGenerated = reader.GetDecimal(reader.GetOrdinal("RevenueGenerated"))
                            });
                }

                return Json(new { topSellers });
            }
            catch (Exception ex) { return Json(new { error = ex.Message }); }
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

        [HttpGet]
        public async Task<IActionResult> GetSellers(string status = "Pending")
        {
            try
            {
                var sellers = new List<SellerViewModel>();
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    var sql = @"
                                SELECT s.seller_id, s.user_id, s.business_name, s.business_email,
                                    s.business_phone, s.business_type, s.business_address,
                                    s.logo_path, s.document_path, s.seller_status, s.created_at,
                                    ISNULL(c.first_name + ' ' + c.last_name, u.email) AS owner_name,
                                    (SELECT COUNT(*) FROM Products p WHERE p.seller_id = s.seller_id) AS total_products,
                                    ISNULL((SELECT SUM(oi.Quantity * oi.UnitPrice) 
                                            FROM OrderItems oi WHERE oi.SellerId = s.seller_id), 0) AS total_sales
                                FROM Sellers s
                                INNER JOIN Users u ON s.user_id = u.user_id
                                LEFT JOIN Consumers c ON c.user_id = u.user_id
                                WHERE s.seller_status = @Status";
                    using (var cmd = new SqlCommand(sql, connection))
                    {
                        cmd.Parameters.AddWithValue("@Status", status);
                        using (var reader = await cmd.ExecuteReaderAsync())
                            while (await reader.ReadAsync())
                                sellers.Add(new SellerViewModel
                                {
                                    SellerId        = reader.GetInt32(reader.GetOrdinal("seller_id")),
                                    UserId          = reader.GetInt32(reader.GetOrdinal("user_id")),
                                    BusinessName    = reader["business_name"]?.ToString()    ?? "",
                                    BusinessEmail   = reader["business_email"]?.ToString()   ?? "",
                                    BusinessPhone   = reader["business_phone"]?.ToString()   ?? "",
                                    BusinessType    = reader["business_type"]?.ToString()    ?? "",
                                    BusinessAddress = reader["business_address"]?.ToString() ?? "",
                                    LogoPath        = reader["logo_path"]?.ToString(),
                                    DocumentPath    = reader["document_path"]?.ToString(),
                                    SellerStatus    = reader["seller_status"]?.ToString()    ?? "",
                                    OwnerName       = reader["owner_name"]?.ToString()       ?? "",
                                    CreatedAt       = reader["created_at"] as DateTime?,
                                    TotalProducts   = reader.GetInt32(reader.GetOrdinal("total_products")),
                                    TotalSales      = reader.GetDecimal(reader.GetOrdinal("total_sales"))  
                                });
                    }
                }
                return Json(sellers);
            }
            catch (Exception ex) { return Json(new { error = ex.Message }); }
        }

        [HttpPost]
        public async Task<IActionResult> UpdateSellerStatus([FromBody] UpdateSellerStatusRequest request)
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    using (var cmd = new SqlCommand("UPDATE Sellers SET seller_status = @Status WHERE seller_id = @SellerId", connection))
                    {
                        cmd.Parameters.AddWithValue("@Status", request.Status);
                        cmd.Parameters.AddWithValue("@SellerId", request.SellerId);
                        await cmd.ExecuteNonQueryAsync();
                    }
                }
                return Json(new { success = true, message = $"Seller {request.Status} successfully." });
            }
            catch (Exception ex) { return Json(new { success = false, message = ex.Message }); }
        }

        [HttpPost]
        public async Task<IActionResult> UpdateSellerInfo([FromBody] UpdateSellerInfoRequest request)
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    var sql = "UPDATE Sellers SET business_name = @BusinessName, business_email = @BusinessEmail WHERE seller_id = @SellerId";
                    using (var cmd = new SqlCommand(sql, connection))
                    {
                        cmd.Parameters.AddWithValue("@BusinessName",  request.BusinessName);
                        cmd.Parameters.AddWithValue("@BusinessEmail", request.BusinessEmail);
                        cmd.Parameters.AddWithValue("@SellerId",      request.SellerId);
                        await cmd.ExecuteNonQueryAsync();
                    }
                }
                return Json(new { success = true, message = "Seller updated successfully." });
            }
            catch (Exception ex) { return Json(new { success = false, message = ex.Message }); }
        }


        // FinanceRequest - Accessible ONLY by SuperAdmin and Finance Officer
        public async Task<IActionResult> FinanceRequest()
        {
            var redirect = RedirectToLoginIfNotAuthenticated();
            if (redirect != null) return redirect;

            var unauthorized = RedirectIfUnauthorized(new[] { "SuperAdmin", "Finance Officer" });
            if (unauthorized != null) return unauthorized;

            var viewModel = await GetFinanceRequestsAsync();
            
            ViewBag.UserRole = GetCurrentUserRole();
            return View("FinanceRequest", viewModel);
        }

        [HttpGet]
        public async Task<IActionResult> GetPendingPayouts()
        {
            try
            {
                var payouts = new List<PayoutRequest>();
                
                using (var connection = new SqlConnection(_connectionString))
                {
                    using (var command = new SqlCommand("sp_GetPendingPayouts", connection))
                    {
                        command.CommandType = CommandType.StoredProcedure;
                        
                        await connection.OpenAsync();
                        
                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                payouts.Add(new PayoutRequest
                                {
                                    WithdrawalId = reader.GetInt64(reader.GetOrdinal("withdrawal_id")),
                                    SellerId = reader.GetInt32(reader.GetOrdinal("seller_id")),
                                    WalletId = reader.GetInt32(reader.GetOrdinal("wallet_id")),
                                    PayoutAccountId = reader.GetInt32(reader.GetOrdinal("payout_account_id")),
                                    Amount = reader.GetDecimal(reader.GetOrdinal("amount")),
                                    Status = reader.GetString(reader.GetOrdinal("status")),
                                    RequestedAt = reader.GetDateTime(reader.GetOrdinal("requested_at")),
                                    SellerName = reader.GetString(reader.GetOrdinal("seller_name")),
                                    ShopName = reader.GetString(reader.GetOrdinal("shop_name")),
                                    SellerEmail = reader.GetString(reader.GetOrdinal("seller_email")),
                                    BankName = reader.GetString(reader.GetOrdinal("bank_name")),
                                });
                            }
                        }
                    }
                }
                
                return Json(payouts);
            }
            catch (Exception ex)
            {
                return Json(new { error = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetPendingDiscounts()
        {
            try
            {
                var discounts = new List<DiscountRequest>();
                
                using (var connection = new SqlConnection(_connectionString))
                {
                    using (var command = new SqlCommand("sp_GetPendingDiscounts", connection))
                    {
                        command.CommandType = CommandType.StoredProcedure;
                        
                        await connection.OpenAsync();
                        
                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                discounts.Add(new DiscountRequest
                                {
                                    Id = reader.GetInt32(reader.GetOrdinal("id")),
                                    Name = reader.GetString(reader.GetOrdinal("name")),
                                    Type = reader.GetString(reader.GetOrdinal("Type")),
                                    BannerSize = reader.GetString(reader.GetOrdinal("BannerSize")),
                                    TotalDiscountPercent = reader.GetDecimal(reader.GetOrdinal("TotalDiscountPercent")),
                                    TotalDiscountFix = reader.GetDecimal(reader.GetOrdinal("TotalDiscountFix")),
                                    UsageLimit = reader.GetInt32(reader.GetOrdinal("UsageLimit")),
                                    UntilPromotionLast = reader.GetBoolean(reader.GetOrdinal("UntilPromotionLast")),
                                    BuyQuantity = reader.GetInt32(reader.GetOrdinal("BuyQuantity")),
                                    TakeQuantity = reader.GetInt32(reader.GetOrdinal("TakeQuantity")),
                                    FreeItemRequirement = reader.GetString(reader.GetOrdinal("FreeItemRequirement")),
                                    ReturnWindowDays = reader.GetInt32(reader.GetOrdinal("ReturnWindowDays")),
                                    MinimumRequirementType = reader.GetString(reader.GetOrdinal("MinimumRequirementType")),
                                    MinimumPurchaseAmount = reader.GetDecimal(reader.GetOrdinal("MinimumPurchaseAmount")),
                                    Status = reader.GetString(reader.GetOrdinal("Status")),
                                    CreatedAt = reader.GetDateTime(reader.GetOrdinal("CreatedAt")),
                                    UserId = reader.GetInt32(reader.GetOrdinal("user_id")),
                                    ShopName = reader.GetString(reader.GetOrdinal("ShopName")),
                                    ProductName = reader.GetString(reader.GetOrdinal("ProductName")),
                                    OriginalPrice = reader.GetDecimal(reader.GetOrdinal("OriginalPrice")),
                                    StartDate = reader.GetDateTime(reader.GetOrdinal("StartDate")),
                                    EndDate = reader.GetDateTime(reader.GetOrdinal("EndDate")),
                                    ProductImage = reader.IsDBNull(reader.GetOrdinal("ProductImage")) ? null : reader.GetString(reader.GetOrdinal("ProductImage"))
                                });
                            }
                        }
                    }
                }
                
                return Json(discounts);
            }
            catch (Exception ex)
            {
                return Json(new { error = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> ProcessPayout([FromBody] ProcessPayoutRequest request)
        {
            try
            {
                var staffId = HttpContext.Session.GetInt32("StaffId") ?? 0;
                var adminName = HttpContext.Session.GetString("Username") ?? "System";
                
                using (var connection = new SqlConnection(_connectionString))
                {
                    using (var command = new SqlCommand("sp_ProcessPayout", connection))
                    {
                        command.CommandType = CommandType.StoredProcedure;
                        command.Parameters.AddWithValue("@WithdrawalId", request.WithdrawalId);
                        command.Parameters.AddWithValue("@Action", request.Action);
                        command.Parameters.AddWithValue("@Reason", request.Reason ?? (object)DBNull.Value);
                        command.Parameters.AddWithValue("@ProcessedBy", staffId);
                        
                        await connection.OpenAsync();
                        
                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            if (await reader.ReadAsync())
                            {
                                var status = reader["Status"].ToString();
                                var message = reader["Message"].ToString();
                                
                                // Log the action
                                await LogAdminAction(staffId, adminName, 
                                    request.Action == "approve" ? "Approve Payout" : "Reject Payout",
                                    $"Withdrawal #{request.WithdrawalId}", 
                                    status == "Success" ? "Success" : "Failed",
                                    $"Amount: {request.Amount}, Reason: {request.Reason}");
                                
                                return Json(new { success = status == "Success", message = message });
                            }
                        }
                    }
                }
                
                return Json(new { success = false, message = "Processing failed" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> ProcessDiscount([FromBody] ProcessDiscountRequest request)
        {
            try
            {
                var staffId = HttpContext.Session.GetInt32("StaffId") ?? 0;
                var adminName = HttpContext.Session.GetString("Username") ?? "System";
                
                using (var connection = new SqlConnection(_connectionString))
                {
                    using (var command = new SqlCommand("sp_ProcessDiscount", connection))
                    {
                        command.CommandType = CommandType.StoredProcedure;
                        command.Parameters.AddWithValue("@DiscountId", request.DiscountId);
                        command.Parameters.AddWithValue("@Action", request.Action);
                        command.Parameters.AddWithValue("@Reason", request.Reason ?? (object)DBNull.Value);
                        command.Parameters.AddWithValue("@ProcessedBy", staffId);
                        
                        await connection.OpenAsync();
                        
                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            if (await reader.ReadAsync())
                            {
                                var status = reader["Status"].ToString();
                                var message = reader["Message"].ToString();
                                
                                // Log the action
                                await LogAdminAction(staffId, adminName,
                                    request.Action == "approve" ? "Approve Discount" : "Reject Discount",
                                    $"Discount #{request.DiscountId}",
                                    status == "Success" ? "Success" : "Failed",
                                    $"Product: {request.ProductName}, Reason: {request.Reason}");
                                
                                return Json(new { success = status == "Success", message = message });
                            }
                        }
                    }
                }
                
                return Json(new { success = false, message = "Processing failed" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // GET: Get all active global promotions
        [HttpGet]
        public async Task<IActionResult> GetGlobalPromotions()
        {
            try
            {
                var promotions = new List<GlobalPromotion>();
                
                using (var connection = new SqlConnection(_connectionString))
                {
                    using (var command = new SqlCommand("sp_GetGlobalPromotions", connection))
                    {
                        command.CommandType = CommandType.StoredProcedure;
                        
                        await connection.OpenAsync();
                        
                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                var promotion = new GlobalPromotion
                                {
                                    Id = reader.GetInt32(reader.GetOrdinal("id")),
                                    Name = reader.GetString(reader.GetOrdinal("name")),
                                    Description = reader.IsDBNull(reader.GetOrdinal("description")) ? null : reader.GetString(reader.GetOrdinal("description")),
                                    DiscountPercent = reader.GetDecimal(reader.GetOrdinal("discount_percent")),
                                    StartDate = reader.GetDateTime(reader.GetOrdinal("start_date")),
                                    EndDate = reader.IsDBNull(reader.GetOrdinal("end_date")) ? null : reader.GetDateTime(reader.GetOrdinal("end_date")),
                                    IsIndefinite = reader.GetBoolean(reader.GetOrdinal("is_indefinite")),
                                    Status = reader.GetString(reader.GetOrdinal("status")),
                                    CreatedAt = reader.GetDateTime(reader.GetOrdinal("created_at")),
                                    BannerImageName = reader.IsDBNull(reader.GetOrdinal("banner_image_name")) ? null : reader.GetString(reader.GetOrdinal("banner_image_name")),
                                    BannerImageContentType = reader.IsDBNull(reader.GetOrdinal("banner_image_content_type")) ? null : reader.GetString(reader.GetOrdinal("banner_image_content_type"))
                                };
                                
                                // Handle binary image data
                                if (!reader.IsDBNull(reader.GetOrdinal("banner_image")))
                                {
                                    byte[] imageData = (byte[])reader["banner_image"];
                                    promotion.BannerImage = imageData;
                                    
                                    // Convert to Base64 string for display
                                    if (imageData != null && imageData.Length > 0)
                                    {
                                        // Use the stored content type, or default to image/png
                                        string contentType = promotion.BannerImageContentType ?? "image/png";
                                        
                                        // Convert to Base64
                                        string base64String = Convert.ToBase64String(imageData);
                                        
                                        // Create the data URL
                                        promotion.BannerImageBase64 = $"data:{contentType};base64,{base64String}";
                                        
                                        // Debug output
                                        Console.WriteLine($"Loaded image for promotion {promotion.Id}: {imageData.Length} bytes");
                                        Console.WriteLine($"Content Type: {contentType}");
                                        Console.WriteLine($"Base64 length: {base64String.Length}");
                                    }
                                }
                                else
                                {
                                    Console.WriteLine($"No image for promotion {promotion.Id}");
                                    promotion.BannerImageBase64 = null;
                                }
                                
                                promotions.Add(promotion);
                            }
                        }
                    }
                }
                
                // Return just the promotions array
                return Json(promotions);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in GetGlobalPromotions: {ex.Message}");
                return Json(new { error = ex.Message });
            }
        }

        // POST: Save global promotion (create or update)
        [HttpPost]
        public async Task<IActionResult> SaveGlobalPromotion([FromBody] SaveGlobalPromotionRequest request)
        {
            try
            {
                var staffId = HttpContext.Session.GetInt32("StaffId") ?? 0;
                var adminName = HttpContext.Session.GetString("Username") ?? "System";
                
                // Convert Base64 string to byte array if provided
                byte[] bannerImageBytes = null;
                if (!string.IsNullOrEmpty(request.BannerImageBase64))
                {
                    // Remove data URL prefix if present (e.g., "data:image/png;base64,")
                    var base64Data = request.BannerImageBase64;
                    if (base64Data.Contains(","))
                    {
                        base64Data = base64Data.Substring(base64Data.IndexOf(",") + 1);
                    }
                    bannerImageBytes = Convert.FromBase64String(base64Data);
                }
                
                using (var connection = new SqlConnection(_connectionString))
                {
                    using (var command = new SqlCommand("sp_SaveGlobalPromotion", connection))
                    {
                        command.CommandType = CommandType.StoredProcedure;
                        command.Parameters.AddWithValue("@Id", request.Id.HasValue ? request.Id.Value : (object)DBNull.Value);
                        command.Parameters.AddWithValue("@Name", request.Name);
                        command.Parameters.AddWithValue("@Description", request.Description ?? (object)DBNull.Value);
                        command.Parameters.AddWithValue("@BannerImage", bannerImageBytes ?? (object)DBNull.Value);
                        command.Parameters.AddWithValue("@BannerImageName", request.BannerImageName ?? (object)DBNull.Value);
                        command.Parameters.AddWithValue("@BannerImageContentType", request.BannerImageContentType ?? (object)DBNull.Value);
                        command.Parameters.AddWithValue("@DiscountPercent", request.DiscountPercent);
                        command.Parameters.AddWithValue("@StartDate", request.StartDate);
                        command.Parameters.AddWithValue("@EndDate", request.EndDate ?? (object)DBNull.Value);
                        command.Parameters.AddWithValue("@IsIndefinite", request.IsIndefinite);
                        command.Parameters.AddWithValue("@CreatedBy", staffId);
                        
                        await connection.OpenAsync();
                        
                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            if (await reader.ReadAsync())
                            {
                                var status = reader["Status"].ToString();
                                var message = reader["Message"].ToString();
                                var id = reader["Id"] != DBNull.Value ? Convert.ToInt32(reader["Id"]) : (int?)null;
                                
                                // Log the action
                                await LogAdminAction(staffId, adminName, 
                                    request.Id.HasValue ? "Update Global Promotion" : "Create Global Promotion",
                                    $"Promotion: {request.Name}", 
                                    status == "Success" ? "Success" : "Failed");
                                
                                return Json(new { 
                                    success = status == "Success", 
                                    message = message, 
                                    id = id
                                });
                            }
                        }
                    }
                }
                
                return Json(new { success = false, message = "Failed to save promotion" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }
        
        // DELETE: End/Delete a global promotion
        [HttpPost]
        public async Task<IActionResult> DeleteGlobalPromotion([FromBody] DeleteGlobalPromotionRequest request)
        {
            try
            {
                var staffId = HttpContext.Session.GetInt32("StaffId") ?? 0;
                var adminName = HttpContext.Session.GetString("Username") ?? "System";
                
                using (var connection = new SqlConnection(_connectionString))
                {
                    using (var command = new SqlCommand("sp_DeleteGlobalPromotion", connection))
                    {
                        command.CommandType = CommandType.StoredProcedure;
                        command.Parameters.AddWithValue("@Id", request.Id);
                        command.Parameters.AddWithValue("@DeletedBy", staffId);
                        
                        await connection.OpenAsync();
                        await command.ExecuteNonQueryAsync();
                        
                        // Log the action
                        await LogAdminAction(staffId, adminName, "Delete Global Promotion", 
                            $"Promotion ID: {request.Id}", "Success");
                        
                        return Json(new { success = true, message = "Promotion ended successfully" });
                    }
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }



        private async Task<FinanceRequestsViewModel> GetFinanceRequestsAsync()
        {
            var viewModel = new FinanceRequestsViewModel();
            
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                
                // Get pending payouts
                using (var cmd = new SqlCommand("sp_GetPendingPayouts", connection))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            viewModel.PendingPayouts.Add(new PayoutRequest
                            {
                                WithdrawalId = reader.GetInt64(reader.GetOrdinal("withdrawal_id")),
                                SellerId = reader.GetInt32(reader.GetOrdinal("seller_id")),
                                Amount = reader.GetDecimal(reader.GetOrdinal("amount")),
                                RequestedAt = reader.GetDateTime(reader.GetOrdinal("requested_at")),
                                SellerName = reader.GetString(reader.GetOrdinal("seller_name")),
                                ShopName = reader.GetString(reader.GetOrdinal("shop_name")),
                                SellerEmail = reader.GetString(reader.GetOrdinal("seller_email")),
                                BankName = reader.GetString(reader.GetOrdinal("bank_name")),
                            });
                            
                            viewModel.TotalPendingPayoutAmount += viewModel.PendingPayouts.Last().Amount;
                        }
                    }
                }
                
                // Get pending discounts
                using (var cmd = new SqlCommand("sp_GetPendingDiscounts", connection))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            viewModel.PendingDiscounts.Add(new DiscountRequest
                            {
                                Id = reader.GetInt32(reader.GetOrdinal("id")),
                                Name = reader.GetString(reader.GetOrdinal("Name")),
                                TotalDiscountPercent = reader.GetDecimal(reader.GetOrdinal("TotalDiscountPercent")),
                                TotalDiscountFix = reader.GetDecimal(reader.GetOrdinal("TotalDiscountFix")),
                                Status = reader.GetString(reader.GetOrdinal("status")),
                                CreatedAt = reader.GetDateTime(reader.GetOrdinal("CreatedAt")),
                                ShopName = reader.GetString(reader.GetOrdinal("ShopName")),
                                ProductName = reader.GetString(reader.GetOrdinal("ProductName")),
                                OriginalPrice = reader.GetDecimal(reader.GetOrdinal("OriginalPrice")),
                                ProductImage = reader.IsDBNull(reader.GetOrdinal("ProductImage")) ? null : reader.GetString(reader.GetOrdinal("ProductImage"))
                            });
                        }
                    }
                    viewModel.TotalPendingDiscountsCount = viewModel.PendingDiscounts.Count;
                }
            }
            
            return viewModel;
        }

        private async Task LogAdminAction(int staffId, string adminName, string action, string target, string status, string details = null)
        {
            try
            {
                if (string.IsNullOrEmpty(adminName))
                {
                    adminName = HttpContext.Session.GetString("Username") ?? "System";
                }

                using (var connection = new SqlConnection(_connectionString))
                {
                    using (var command = new SqlCommand("sp_InsertAuditLog", connection))
                    {
                        command.CommandType = CommandType.StoredProcedure;
                        command.Parameters.AddWithValue("@StaffId", staffId);
                        command.Parameters.AddWithValue("@AdminName", adminName);
                        command.Parameters.AddWithValue("@Action", action);
                        command.Parameters.AddWithValue("@Target", target);
                        command.Parameters.AddWithValue("@TargetType", "Finance");
                        command.Parameters.AddWithValue("@Status", status);
                        command.Parameters.AddWithValue("@Details", details ?? (object)DBNull.Value);
                        command.Parameters.AddWithValue("@IpAddress", HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown");
                        command.Parameters.AddWithValue("@UserAgent", Request.Headers["User-Agent"].ToString());

                        await connection.OpenAsync();
                        await command.ExecuteNonQueryAsync();
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to log admin action: {ex.Message}");
            }
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

        // Get all logistics partners
        [HttpGet]
        public async Task<IActionResult> GetLogistics(string statusFilter = null, string searchTerm = null)
        {
            try
            {
                var logistics = new List<LogisticsPartner>();
                
                using (var connection = new SqlConnection(_connectionString))
                {
                    using (var command = new SqlCommand("sp_GetLogistics", connection))
                    {
                        command.CommandType = CommandType.StoredProcedure;
                        command.Parameters.AddWithValue("@StatusFilter", statusFilter ?? (object)DBNull.Value);
                        command.Parameters.AddWithValue("@SearchTerm", searchTerm ?? (object)DBNull.Value);
                        command.Parameters.AddWithValue("@IncludeRealTimeStats", 1);
                        
                        await connection.OpenAsync();
                        
                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                var logisticsItem = new LogisticsPartner
                                {
                                    LogisticsId = reader.GetInt32(reader.GetOrdinal("logistics_id")),
                                    CourierName = reader.GetString(reader.GetOrdinal("courier_name")),
                                    ServiceType = reader.GetString(reader.GetOrdinal("service_type")),
                                    LogoUrl = reader.IsDBNull(reader.GetOrdinal("logo_url")) ? null : reader.GetString(reader.GetOrdinal("logo_url")),
                                    Status = reader.GetString(reader.GetOrdinal("status")),
                                    // Fix: Use Convert.ToDecimal to handle both int and decimal
                                    SuccessRate = Convert.ToDecimal(reader["success_rate"]),
                                    AvgDeliveryDays = Convert.ToDecimal(reader["avg_delivery_days"]),
                                    MinDeliveryDays = reader.IsDBNull(reader.GetOrdinal("min_delivery_days")) ? null : Convert.ToInt32(reader["min_delivery_days"]),
                                    MaxDeliveryDays = reader.IsDBNull(reader.GetOrdinal("max_delivery_days")) ? null : Convert.ToInt32(reader["max_delivery_days"]),
                                    ContactPerson = reader.IsDBNull(reader.GetOrdinal("contact_person")) ? null : reader.GetString(reader.GetOrdinal("contact_person")),
                                    ContactEmail = reader.IsDBNull(reader.GetOrdinal("contact_email")) ? null : reader.GetString(reader.GetOrdinal("contact_email")),
                                    ContactPhone = reader.IsDBNull(reader.GetOrdinal("contact_phone")) ? null : reader.GetString(reader.GetOrdinal("contact_phone")),
                                    TrackingUrlTemplate = reader.IsDBNull(reader.GetOrdinal("tracking_url_template")) ? null : reader.GetString(reader.GetOrdinal("tracking_url_template")),
                                    IsPreferred = reader.GetBoolean(reader.GetOrdinal("is_preferred")),
                                    SortOrder = reader.GetInt32(reader.GetOrdinal("sort_order")),
                                    CreatedAt = reader.GetDateTime(reader.GetOrdinal("created_at")),
                                    UpdatedAt = reader.IsDBNull(reader.GetOrdinal("updated_at")) ? null : reader.GetDateTime(reader.GetOrdinal("updated_at")),
                                    DisplayStatus = reader.GetString(reader.GetOrdinal("display_status")),
                                    // Handle nullable ints for performance metrics
                                    TotalOrders = reader.IsDBNull(reader.GetOrdinal("total_orders")) ? (int?)null : Convert.ToInt32(reader["total_orders"]),
                                    DeliveredOrders = reader.IsDBNull(reader.GetOrdinal("delivered_orders")) ? (int?)null : Convert.ToInt32(reader["delivered_orders"]),
                                    Last30DaysOrders = reader.IsDBNull(reader.GetOrdinal("last_30_days_orders")) ? (int?)null : Convert.ToInt32(reader["last_30_days_orders"])
                                };
                                
                                // Handle Base64 image
                                if (!reader.IsDBNull(reader.GetOrdinal("logo_base64")))
                                {
                                    logisticsItem.LogoBase64 = reader.GetString(reader.GetOrdinal("logo_base64"));
                                    logisticsItem.LogoFilename = reader.IsDBNull(reader.GetOrdinal("logo_filename")) ? null : reader.GetString(reader.GetOrdinal("logo_filename"));
                                    logisticsItem.LogoContentType = reader.IsDBNull(reader.GetOrdinal("logo_content_type")) ? null : reader.GetString(reader.GetOrdinal("logo_content_type"));
                                }
                                
                                logistics.Add(logisticsItem);
                            }
                        }
                    }
                }
                
                return Json(logistics);
            }
            catch (Exception ex)
            {
                return Json(new { error = ex.Message });
            }
        }
        // Get logistics statistics
        [HttpGet]
        public async Task<IActionResult> GetLogisticsStats()
        {
            try
            {
                var stats = new LogisticsStats();
                
                using (var connection = new SqlConnection(_connectionString))
                {
                    using (var command = new SqlCommand("sp_GetLogisticsStats", connection))
                    {
                        command.CommandType = CommandType.StoredProcedure;
                        
                        await connection.OpenAsync();
                        
                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            if (await reader.ReadAsync())
                            {
                                stats.TotalCount = reader.GetInt32(reader.GetOrdinal("total_count"));
                                stats.ActiveCount = reader.GetInt32(reader.GetOrdinal("active_count"));
                                stats.InactiveCount = reader.GetInt32(reader.GetOrdinal("inactive_count"));
                                stats.ArchivedCount = reader.GetInt32(reader.GetOrdinal("archived_count"));
                            }
                        }
                    }
                }
                
                return Json(stats);
            }
            catch (Exception ex)
            {
                return Json(new { error = ex.Message });
            }
        }

        // Get detailed logistics performance (for performance modal)
        [HttpGet]
        public async Task<IActionResult> GetLogisticsPerformance(int logisticsId)
        {
            try
            {
                var basicMetrics = new LogisticsPerformanceDetails();
                var monthlyTrend = new List<MonthlyPerformance>();
                
                using (var connection = new SqlConnection(_connectionString))
                {
                    using (var command = new SqlCommand("sp_GetLogisticsPerformance", connection))
                    {
                        command.CommandType = CommandType.StoredProcedure;
                        command.Parameters.AddWithValue("@LogisticsId", logisticsId);
                        
                        await connection.OpenAsync();
                        
                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            // First result set - Basic metrics
                            if (await reader.ReadAsync())
                            {
                                basicMetrics = new LogisticsPerformanceDetails
                                {
                                    LogisticsId = Convert.ToInt32(reader["logistics_id"]),
                                    CourierName = reader["courier_name"].ToString(),
                                    ServiceType = reader.IsDBNull(reader.GetOrdinal("service_type")) ? null : reader["service_type"].ToString(),
                                    TotalOrders = Convert.ToInt32(reader["total_orders"]),
                                    DeliveredOrders = Convert.ToInt32(reader["delivered_orders"]),
                                    CancelledOrders = Convert.ToInt32(reader["cancelled_orders"]),
                                    FailedOrders = Convert.ToInt32(reader["failed_orders"]),
                                    PendingOrders = Convert.ToInt32(reader["pending_orders"]),
                                    InTransitOrders = Convert.ToInt32(reader["in_transit_orders"]),
                                    // Use Convert.ToDecimal for success_rate and avg_delivery_days
                                    SuccessRate = Convert.ToDecimal(reader["success_rate"]),
                                    AvgDeliveryDays = Convert.ToDecimal(reader["avg_delivery_days"]),
                                    MinDeliveryDays = reader.IsDBNull(reader.GetOrdinal("min_delivery_days")) ? (decimal?)null : Convert.ToDecimal(reader["min_delivery_days"]),
                                    MaxDeliveryDays = reader.IsDBNull(reader.GetOrdinal("max_delivery_days")) ? (decimal?)null : Convert.ToDecimal(reader["max_delivery_days"]),
                                    Last7DaysOrders = Convert.ToInt32(reader["last_7_days_orders"]),
                                    Last30DaysOrders = Convert.ToInt32(reader["last_30_days_orders"]),
                                    Last90DaysOrders = Convert.ToInt32(reader["last_90_days_orders"]),
                                    ConfiguredMinDays = reader.IsDBNull(reader.GetOrdinal("configured_min_days")) ? (int?)null : Convert.ToInt32(reader["configured_min_days"]),
                                    ConfiguredMaxDays = reader.IsDBNull(reader.GetOrdinal("configured_max_days")) ? (int?)null : Convert.ToInt32(reader["configured_max_days"])
                                };
                            }
                            
                            // Second result set - Monthly trend
                            await reader.NextResultAsync();
                            while (await reader.ReadAsync())
                            {
                                monthlyTrend.Add(new MonthlyPerformance
                                {
                                    Year = Convert.ToInt32(reader["year"]),
                                    Month = Convert.ToInt32(reader["month"]),
                                    MonthName = reader["month_name"].ToString(),
                                    TotalOrders = Convert.ToInt32(reader["total_orders"]),
                                    DeliveredOrders = Convert.ToInt32(reader["delivered_orders"]),
                                    SuccessRate = Convert.ToDecimal(reader["success_rate"]),
                                    AvgDeliveryDays = Convert.ToDecimal(reader["avg_delivery_days"])
                                });
                            }
                        }
                    }
                }
                
                return Json(new { success = true, basicMetrics = basicMetrics, monthlyTrend = monthlyTrend });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, error = ex.Message });
            }
        }  
                
        // Save logistics partner (UPDATED - removed manual performance fields)
        [HttpPost]
        public async Task<IActionResult> SaveLogistics([FromBody] SaveLogisticsRequest request)
        {
            try
            {
                var staffId = HttpContext.Session.GetInt32("StaffId") ?? 0;
                var adminName = HttpContext.Session.GetString("Username") ?? "System";
                
                using (var connection = new SqlConnection(_connectionString))
                {
                    using (var command = new SqlCommand("sp_SaveLogistics", connection))
                    {
                        command.CommandType = CommandType.StoredProcedure;
                        command.Parameters.AddWithValue("@LogisticsId", request.LogisticsId.HasValue ? request.LogisticsId.Value : (object)DBNull.Value);
                        command.Parameters.AddWithValue("@CourierName", request.CourierName);
                        command.Parameters.AddWithValue("@ServiceType", request.ServiceType);
                        command.Parameters.AddWithValue("@LogoBase64", request.LogoBase64 ?? (object)DBNull.Value);
                        command.Parameters.AddWithValue("@LogoFilename", request.LogoFilename ?? (object)DBNull.Value);
                        command.Parameters.AddWithValue("@LogoContentType", request.LogoContentType ?? (object)DBNull.Value);
                        command.Parameters.AddWithValue("@Status", request.Status ?? "Active");
                        // REMOVED: SuccessRate, AvgDeliveryDays, MinDeliveryDays, MaxDeliveryDays
                        // These are now auto-calculated from orders
                        command.Parameters.AddWithValue("@ContactPerson", request.ContactPerson ?? (object)DBNull.Value);
                        command.Parameters.AddWithValue("@ContactEmail", request.ContactEmail ?? (object)DBNull.Value);
                        command.Parameters.AddWithValue("@ContactPhone", request.ContactPhone ?? (object)DBNull.Value);
                        command.Parameters.AddWithValue("@TrackingUrlTemplate", request.TrackingUrlTemplate ?? (object)DBNull.Value);
                        command.Parameters.AddWithValue("@IsPreferred", request.IsPreferred);
                        command.Parameters.AddWithValue("@SortOrder", request.SortOrder);
                        command.Parameters.AddWithValue("@CreatedBy", staffId);
                        
                        await connection.OpenAsync();
                        
                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            if (await reader.ReadAsync())
                            {
                                var status = reader["Status"].ToString();
                                var message = reader["Message"].ToString();
                                
                                await LogAdminAction(staffId, adminName,
                                    request.LogisticsId.HasValue ? "Update Logistics" : "Add Logistics",
                                    $"Logistics: {request.CourierName}",
                                    status == "Success" ? "Success" : "Failed");
                                
                                return Json(new { success = status == "Success", message = message });
                            }
                        }
                    }
                }
                
                return Json(new { success = false, message = "Failed to save logistics partner" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // Update logistics status
        [HttpPost]
        public async Task<IActionResult> UpdateLogisticsStatus([FromBody] UpdateLogisticsStatusRequest request)
        {
            try
            {
                var staffId = HttpContext.Session.GetInt32("StaffId") ?? 0;
                var adminName = HttpContext.Session.GetString("Username") ?? "System";
                
                using (var connection = new SqlConnection(_connectionString))
                {
                    using (var command = new SqlCommand("sp_UpdateLogisticsStatus", connection))
                    {
                        command.CommandType = CommandType.StoredProcedure;
                        command.Parameters.AddWithValue("@LogisticsId", request.LogisticsId);
                        command.Parameters.AddWithValue("@Status", request.Status);
                        command.Parameters.AddWithValue("@Reason", request.Reason ?? (object)DBNull.Value);
                        command.Parameters.AddWithValue("@UpdatedBy", staffId);
                        
                        await connection.OpenAsync();
                        
                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            if (await reader.ReadAsync())
                            {
                                var status = reader["Status"].ToString();
                                var message = reader["Message"].ToString();
                                
                                await LogAdminAction(staffId, adminName,
                                    $"Update Logistics Status to {request.Status}",
                                    $"Logistics ID: {request.LogisticsId}",
                                    status == "Success" ? "Success" : "Failed",
                                    $"Reason: {request.Reason}");
                                
                                return Json(new { success = status == "Success", message = message });
                            }
                        }
                    }
                }
                
                return Json(new { success = false, message = "Failed to update status" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // Delete/Archive logistics
        [HttpPost]
        public async Task<IActionResult> DeleteLogistics([FromBody] DeleteLogisticsRequest request)
        {
            try
            {
                var staffId = HttpContext.Session.GetInt32("StaffId") ?? 0;
                var adminName = HttpContext.Session.GetString("Username") ?? "System";
                
                using (var connection = new SqlConnection(_connectionString))
                {
                    using (var command = new SqlCommand("sp_DeleteLogistics", connection))
                    {
                        command.CommandType = CommandType.StoredProcedure;
                        command.Parameters.AddWithValue("@LogisticsId", request.LogisticsId);
                        command.Parameters.AddWithValue("@Reason", request.Reason ?? (object)DBNull.Value);
                        command.Parameters.AddWithValue("@DeletedBy", staffId);
                        
                        await connection.OpenAsync();
                        await command.ExecuteNonQueryAsync();
                        
                        await LogAdminAction(staffId, adminName, "Archive Logistics",
                            $"Logistics ID: {request.LogisticsId}", "Success",
                            $"Reason: {request.Reason}");
                        
                        return Json(new { success = true, message = "Logistics partner archived successfully" });
                    }
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // Restore logistics
        [HttpPost]
        public async Task<IActionResult> RestoreLogistics([FromBody] RestoreLogisticsRequest request)
        {
            try
            {
                var staffId = HttpContext.Session.GetInt32("StaffId") ?? 0;
                var adminName = HttpContext.Session.GetString("Username") ?? "System";
                
                using (var connection = new SqlConnection(_connectionString))
                {
                    using (var command = new SqlCommand("sp_RestoreLogistics", connection))
                    {
                        command.CommandType = CommandType.StoredProcedure;
                        command.Parameters.AddWithValue("@LogisticsId", request.LogisticsId);
                        command.Parameters.AddWithValue("@Reason", request.Reason ?? (object)DBNull.Value);
                        command.Parameters.AddWithValue("@RestoredBy", staffId);
                        
                        await connection.OpenAsync();
                        await command.ExecuteNonQueryAsync();
                        
                        await LogAdminAction(staffId, adminName, "Restore Logistics",
                            $"Logistics ID: {request.LogisticsId}", "Success",
                            $"Reason: {request.Reason}");
                        
                        return Json(new { success = true, message = "Logistics partner restored successfully" });
                    }
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
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

            var unauthorized = RedirectIfUnauthorized(new[] { "Support Agent" });
            if (unauthorized != null) return unauthorized;

            ViewBag.UserRole = GetCurrentUserRole();
            return View();
        }

        // Settings - Accessible ONLY by SuperAdmin
        #region Settings

        // GET: Settings page
        public IActionResult Settings()
        {
            var redirect = RedirectToLoginIfNotAuthenticated();
            if (redirect != null) return redirect;

            var unauthorized = RedirectIfUnauthorized(new[] { "SuperAdmin" });
            if (unauthorized != null) return unauthorized;

            // Get from session (already stored during login)
            ViewBag.UserRole = GetCurrentUserRole();
            ViewBag.CurrentAdminName = HttpContext.Session.GetString("FullName");
            ViewBag.CurrentAdminEmail = HttpContext.Session.GetString("Email");
            ViewBag.CurrentAdminUsername = HttpContext.Session.GetString("Username");
            
            return View();
        }

        // GET: Get all active admins
        [HttpGet]
        public async Task<IActionResult> GetActiveAdmins()
        {
            try
            {
                var admins = new List<AdminUserViewModel>();
                
                using (var connection = new SqlConnection(_connectionString))
                {
                    using (var command = new SqlCommand("sp_GetActiveAdmins", connection))
                    {
                        command.CommandType = CommandType.StoredProcedure;
                        
                        await connection.OpenAsync();
                        
                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                admins.Add(new AdminUserViewModel
                                {
                                    StaffId = reader.GetInt32(reader.GetOrdinal("staff_id")),
                                    UserId = reader.GetInt32(reader.GetOrdinal("user_id")),
                                    FirstName = reader.GetString(reader.GetOrdinal("first_name")),
                                    LastName = reader.GetString(reader.GetOrdinal("last_name")),
                                    FullName = reader.GetString(reader.GetOrdinal("full_name")),
                                    Username = reader.GetString(reader.GetOrdinal("username")),
                                    Email = reader.GetString(reader.GetOrdinal("email")),
                                    UserType = reader.GetString(reader.GetOrdinal("user_type")),
                                    Phone = reader.IsDBNull(reader.GetOrdinal("phone")) ? null : reader.GetString(reader.GetOrdinal("phone")),
                                    CreatedAt = reader.GetDateTime(reader.GetOrdinal("created_at")),
                                    LastActive = reader.IsDBNull(reader.GetOrdinal("last_active")) ? null : reader.GetDateTime(reader.GetOrdinal("last_active")),
                                    IsActive = reader.GetBoolean(reader.GetOrdinal("is_active")),
                                    AddedByName = reader.IsDBNull(reader.GetOrdinal("added_by_name")) ? null : reader.GetString(reader.GetOrdinal("added_by_name"))
                                });
                            }
                        }
                    }
                }
                
                return Json(admins);
            }
            catch (Exception ex)
            {
                return Json(new { error = ex.Message });
            }
        }

        // GET: Get revoked admins
        [HttpGet]
        public async Task<IActionResult> GetRevokedAdmins()
        {
            try
            {
                var admins = new List<RevokedAdminViewModel>();
                
                using (var connection = new SqlConnection(_connectionString))
                {
                    using (var command = new SqlCommand("sp_GetRevokedAdmins", connection))
                    {
                        command.CommandType = CommandType.StoredProcedure;
                        
                        await connection.OpenAsync();
                        
                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                admins.Add(new RevokedAdminViewModel
                                {
                                    StaffId = reader.GetInt32(reader.GetOrdinal("staff_id")),
                                    UserId = reader.GetInt32(reader.GetOrdinal("user_id")),
                                    FullName = reader.GetString(reader.GetOrdinal("full_name")),
                                    Username = reader.GetString(reader.GetOrdinal("username")),
                                    Email = reader.GetString(reader.GetOrdinal("email")),
                                    UserType = reader.GetString(reader.GetOrdinal("user_type")),
                                    RevokedAt = reader.GetDateTime(reader.GetOrdinal("revoked_at")),
                                    RevokedBy = reader.GetString(reader.GetOrdinal("revoked_by")),
                                    RevokedReason = reader.IsDBNull(reader.GetOrdinal("revoked_reason")) ? null : reader.GetString(reader.GetOrdinal("revoked_reason"))
                                });
                            }
                        }
                    }
                }
                
                return Json(admins);
            }
            catch (Exception ex)
            {
                return Json(new { error = ex.Message });
            }
        }

        // GET: Get audit logs with pagination
        [HttpGet]
        public async Task<IActionResult> GetAuditLogs(int page = 1, int pageSize = 50, string search = null)
        {
            try
            {
                var logs = new List<AuditLogViewModel>();
                int totalCount = 0;
                
                using (var connection = new SqlConnection(_connectionString))
                {
                    using (var command = new SqlCommand("sp_GetAuditLogs", connection))
                    {
                        command.CommandType = CommandType.StoredProcedure;
                        command.Parameters.AddWithValue("@PageNumber", page);
                        command.Parameters.AddWithValue("@PageSize", pageSize);
                        command.Parameters.AddWithValue("@SearchTerm", search ?? (object)DBNull.Value);
                        
                        await connection.OpenAsync();
                        
                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                logs.Add(new AuditLogViewModel
                                {
                                    LogId = reader.GetInt32(reader.GetOrdinal("log_id")),
                                    Timestamp = reader.GetDateTime(reader.GetOrdinal("timestamp")),
                                    AdminName = reader.GetString(reader.GetOrdinal("admin_name")),
                                    Action = reader.GetString(reader.GetOrdinal("action")),
                                    Target = reader.GetString(reader.GetOrdinal("target")),
                                    TargetType = reader.GetString(reader.GetOrdinal("target_type")),
                                    Status = reader.GetString(reader.GetOrdinal("status")),
                                    Details = reader.IsDBNull(reader.GetOrdinal("details")) ? null : reader.GetString(reader.GetOrdinal("details")),
                                    IpAddress = reader.IsDBNull(reader.GetOrdinal("ip_address")) ? null : reader.GetString(reader.GetOrdinal("ip_address"))
                                });
                            }
                            
                            if (await reader.NextResultAsync() && await reader.ReadAsync())
                            {
                                totalCount = reader.GetInt32(reader.GetOrdinal("total_count"));
                            }
                        }
                    }
                }
                
                return Json(new { success = true, logs = logs, totalCount = totalCount });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, error = ex.Message });
            }
        }

        // POST: Add new admin
        [HttpPost]
        public async Task<IActionResult> AddAdmin([FromBody] AddAdminRequest request)
        {
            try
            {
                var staffId = HttpContext.Session.GetInt32("StaffId") ?? 0;
                var adminName = HttpContext.Session.GetString("Username") ?? "System";
                
                // Generate default username and password if not overridden
                string username = request.OverrideUsername;
                string password = request.OverridePassword;
                bool isPasswordAutoGenerated = false;
                
                if (string.IsNullOrEmpty(username))
                {
                    // Generate username: firstname.lastname
                    username = $"{request.FirstName.ToLower()}.{request.LastName.ToLower()}";
                    isPasswordAutoGenerated = true;
                }
                
                if (string.IsNullOrEmpty(password))
                {
                    // Generate password: Lastname + CurrentYear
                    password = $"{request.LastName}{DateTime.Now.Year}";
                    isPasswordAutoGenerated = true;
                }
                
                // Hash the password using SHA256
                var hashedPassword = HashPassword(password);
                
                using (var connection = new SqlConnection(_connectionString))
                {
                    using (var command = new SqlCommand("sp_AddAdmin", connection))
                    {
                        command.CommandType = CommandType.StoredProcedure;
                        command.Parameters.AddWithValue("@FirstName", request.FirstName);
                        command.Parameters.AddWithValue("@LastName", request.LastName);
                        command.Parameters.AddWithValue("@MiddleName", request.MiddleName ?? (object)DBNull.Value);
                        command.Parameters.AddWithValue("@Email", request.Email);
                        command.Parameters.AddWithValue("@Phone", request.Phone ?? (object)DBNull.Value);
                        command.Parameters.AddWithValue("@Username", username);
                        command.Parameters.AddWithValue("@PasswordHash", hashedPassword);
                        command.Parameters.AddWithValue("@UserType", request.UserType);
                        command.Parameters.AddWithValue("@AddedBy", staffId);
                        
                        await connection.OpenAsync();
                        
                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            if (await reader.ReadAsync())
                            {
                                var status = reader["Status"].ToString();
                                var message = reader["Message"].ToString();
                                var newStaffId = reader["StaffId"] != DBNull.Value ? Convert.ToInt32(reader["StaffId"]) : (int?)null;
                                
                                if (status == "Success")
                                {
                                    // Log the action
                                    await LogAdminAction(staffId, adminName, "Add Admin",
                                        $"{request.FirstName} {request.LastName} ({request.UserType})",
                                        "Success",
                                        $"Username: {username}, Password: {(isPasswordAutoGenerated ? password : "[User Provided]")}");
                                    
                                    return Json(new { 
                                        success = true, 
                                        message = message, 
                                        username = username,
                                        password = isPasswordAutoGenerated ? password : null,
                                        isPasswordAutoGenerated = isPasswordAutoGenerated
                                    });
                                }
                                else
                                {
                                    return Json(new { success = false, message = message });
                                }
                            }
                        }
                    }
                }
                
                return Json(new { success = false, message = "Failed to add admin" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // POST: Update admin
        [HttpPost]
        public async Task<IActionResult> UpdateAdmin([FromBody] UpdateAdminRequest request)
        {
            try
            {
                var staffId = HttpContext.Session.GetInt32("StaffId") ?? 0;
                var adminName = HttpContext.Session.GetString("Username") ?? "System";
                
                string hashedPassword = null;
                bool isPasswordUpdated = false;
                
                if (request.OverrideCredentials && !string.IsNullOrEmpty(request.OverridePassword))
                {
                    hashedPassword = HashPassword(request.OverridePassword);
                    isPasswordUpdated = true;
                }
                
                using (var connection = new SqlConnection(_connectionString))
                {
                    using (var command = new SqlCommand("sp_UpdateAdmin", connection))
                    {
                        command.CommandType = CommandType.StoredProcedure;
                        command.Parameters.AddWithValue("@StaffId", request.StaffId);
                        command.Parameters.AddWithValue("@FirstName", request.FirstName);
                        command.Parameters.AddWithValue("@LastName", request.LastName);
                        command.Parameters.AddWithValue("@MiddleName", request.MiddleName ?? (object)DBNull.Value);
                        command.Parameters.AddWithValue("@Email", request.Email);
                        command.Parameters.AddWithValue("@Phone", request.Phone ?? (object)DBNull.Value);
                        command.Parameters.AddWithValue("@UserType", request.UserType);
                        command.Parameters.AddWithValue("@OverrideUsername", request.OverrideCredentials ? request.OverrideUsername : (object)DBNull.Value);
                        command.Parameters.AddWithValue("@PasswordHash", hashedPassword ?? (object)DBNull.Value);
                        command.Parameters.AddWithValue("@UpdatedBy", staffId);
                        
                        await connection.OpenAsync();
                        
                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            if (await reader.ReadAsync())
                            {
                                var status = reader["Status"].ToString();
                                var message = reader["Message"].ToString();
                                
                                if (status == "Success")
                                {
                                    await LogAdminAction(staffId, adminName, "Update Admin",
                                        $"{request.FirstName} {request.LastName}",
                                        "Success",
                                        isPasswordUpdated ? "Password updated" : "Profile updated");
                                    
                                    return Json(new { success = true, message = message });
                                }
                                else
                                {
                                    return Json(new { success = false, message = message });
                                }
                            }
                        }
                    }
                }
                
                return Json(new { success = false, message = "Failed to update admin" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        private string HashPassword(string password)
        {
            using (var sha256 = System.Security.Cryptography.SHA256.Create())
            {
                var bytes = System.Text.Encoding.UTF8.GetBytes(password);
                var hash = sha256.ComputeHash(bytes);
                return Convert.ToBase64String(hash);
            }
        }
        
        // POST: Revoke admin access
        [HttpPost]
        public async Task<IActionResult> RevokeAdmin([FromBody] RevokeAdminRequest request)
        {
            try
            {
                var staffId = HttpContext.Session.GetInt32("StaffId") ?? 0;
                var adminName = HttpContext.Session.GetString("Username") ?? "System";
                
                using (var connection = new SqlConnection(_connectionString))
                {
                    using (var command = new SqlCommand("sp_RevokeAdmin", connection))
                    {
                        command.CommandType = CommandType.StoredProcedure;
                        command.Parameters.AddWithValue("@StaffId", request.StaffId);
                        command.Parameters.AddWithValue("@Reason", request.Reason ?? (object)DBNull.Value);
                        command.Parameters.AddWithValue("@RevokedBy", staffId);
                        
                        await connection.OpenAsync();
                        
                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            if (await reader.ReadAsync())
                            {
                                var status = reader["Status"].ToString();
                                var message = reader["Message"].ToString();
                                var adminNameRevoked = reader["AdminName"].ToString();
                                
                                if (status == "Success")
                                {
                                    await LogAdminAction(staffId, adminName, "Revoke Admin Access",
                                        adminNameRevoked,
                                        "Success",
                                        $"Reason: {request.Reason}");
                                    
                                    return Json(new { success = true, message = message });
                                }
                                else
                                {
                                    return Json(new { success = false, message = message });
                                }
                            }
                        }
                    }
                }
                
                return Json(new { success = false, message = "Failed to revoke admin" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // POST: Reinstate admin access
        [HttpPost]
        public async Task<IActionResult> ReinstateAdmin([FromBody] ReinstateAdminRequest request)
        {
            try
            {
                var staffId = HttpContext.Session.GetInt32("StaffId") ?? 0;
                var adminName = HttpContext.Session.GetString("Username") ?? "System";
                
                using (var connection = new SqlConnection(_connectionString))
                {
                    using (var command = new SqlCommand("sp_ReinstateAdmin", connection))
                    {
                        command.CommandType = CommandType.StoredProcedure;
                        command.Parameters.AddWithValue("@StaffId", request.StaffId);
                        command.Parameters.AddWithValue("@Reason", request.Reason ?? (object)DBNull.Value);
                        command.Parameters.AddWithValue("@ReinstatedBy", staffId);
                        
                        await connection.OpenAsync();
                        
                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            if (await reader.ReadAsync())
                            {
                                var status = reader["Status"].ToString();
                                var message = reader["Message"].ToString();
                                var adminNameReinstated = reader["AdminName"].ToString();
                                
                                if (status == "Success")
                                {
                                    await LogAdminAction(staffId, adminName, "Reinstate Admin Access",
                                        adminNameReinstated,
                                        "Success",
                                        $"Reason: {request.Reason}");
                                    
                                    return Json(new { success = true, message = message });
                                }
                                else
                                {
                                    return Json(new { success = false, message = message });
                                }
                            }
                        }
                    }
                }
                
                return Json(new { success = false, message = "Failed to reinstate admin" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // POST: Update profile info
        [HttpPost]
        public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileRequest request)
        {
            try
            {
                var staffId = HttpContext.Session.GetInt32("StaffId") ?? 0;
                var adminName = HttpContext.Session.GetString("Username") ?? "System";
                
                // Parse full name to first and last
                var nameParts = request.FullName.Trim().Split(' ', 2);
                var firstName = nameParts[0];
                var lastName = nameParts.Length > 1 ? nameParts[1] : "";
                
                using (var connection = new SqlConnection(_connectionString))
                {
                    using (var command = new SqlCommand("sp_UpdateProfile", connection))
                    {
                        command.CommandType = CommandType.StoredProcedure;
                        command.Parameters.AddWithValue("@StaffId", staffId);
                        command.Parameters.AddWithValue("@FirstName", firstName);
                        command.Parameters.AddWithValue("@LastName", lastName);
                        command.Parameters.AddWithValue("@Email", request.Email);
                        
                        await connection.OpenAsync();
                        
                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            if (await reader.ReadAsync())
                            {
                                var status = reader["Status"].ToString();
                                var message = reader["Message"].ToString();
                                
                                if (status == "Success")
                                {
                                    // Update session
                                    HttpContext.Session.SetString("FullName", request.FullName);
                                    HttpContext.Session.SetString("Username", adminName);
                                    
                                    await LogAdminAction(staffId, adminName, "Update Profile",
                                        adminName,
                                        "Success",
                                        $"Updated profile info");
                                    
                                    return Json(new { success = true, message = message });
                                }
                                else
                                {
                                    return Json(new { success = false, message = message });
                                }
                            }
                        }
                    }
                }
                
                return Json(new { success = false, message = "Failed to update profile" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // POST: Change password
        [HttpPost]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
        {
            try
            {
                var staffId = HttpContext.Session.GetInt32("StaffId") ?? 0;
                var adminName = HttpContext.Session.GetString("Username") ?? "System";
                
                // Verify current password
                using (var connection = new SqlConnection(_connectionString))
                {
                    // First, get the current password hash
                    var getHashCmd = new SqlCommand(@"
                        SELECT u.password_hash 
                        FROM users u
                        INNER JOIN staff_info s ON u.user_id = s.user_id
                        WHERE s.staff_id = @StaffId", connection);
                    getHashCmd.Parameters.AddWithValue("@StaffId", staffId);
                    
                    await connection.OpenAsync();
                    var currentHash = await getHashCmd.ExecuteScalarAsync() as string;
                    
                    // Verify current password
                    var currentHashed = HashPassword(request.CurrentPassword);
                    if (currentHash != currentHashed)
                    {
                        return Json(new { success = false, message = "Current password is incorrect" });
                    }
                    
                    // Update password
                    var newHashed = HashPassword(request.NewPassword);
                    
                    using (var command = new SqlCommand("sp_ChangePassword", connection))
                    {
                        command.CommandType = CommandType.StoredProcedure;
                        command.Parameters.AddWithValue("@StaffId", staffId);
                        command.Parameters.AddWithValue("@NewPasswordHash", newHashed);
                        
                        await command.ExecuteNonQueryAsync();
                        
                        await LogAdminAction(staffId, adminName, "Change Password",
                            adminName,
                            "Success",
                            "Password changed successfully");
                        
                        return Json(new { success = true, message = "Password changed successfully" });
                    }
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        #endregion
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
    public class UpdateSellerStatusRequest   
    { 
        public int SellerId { get; set; }
        public string Status { get; set; } = ""; 
    }
    public class UpdateSellerInfoRequest     
    { 
        public int SellerId { get; set; } 
        public string BusinessName { get; set; } = ""; 
        public string BusinessEmail { get; set; } = ""; 
    }
    public class RestoreConsumerRequest
    {
        public int ConsumerId { get; set; }
    }

    // Request Models
    public class ProcessPayoutRequest
    {
        public long WithdrawalId { get; set; }
        public string Action { get; set; } // approve or reject
        public string Reason { get; set; }
        public decimal Amount { get; set; }
    }

    public class ProcessDiscountRequest
    {
        public int DiscountId { get; set; }
        public string Action { get; set; } // approve or reject
        public string Reason { get; set; }
        public string ProductName { get; set; }
    }
public class DeleteGlobalPromotionRequest
{
    public int Id { get; set; }
}
}