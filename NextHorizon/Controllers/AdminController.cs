    using Microsoft.AspNetCore.Mvc;
    using Microsoft.AspNetCore.Http;  
    using NextHorizon.Models.Admin_Models;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Data.SqlClient;
    using Microsoft.AspNetCore.Identity;
    using NextHorizon.Services.AdminServices;
    using NextHorizon.Services;
    using System.Data;
    using System.Threading.Tasks;
    using System;
    using System.Collections.Generic;
    using NextHorizon.Models;
    using System.Linq;
   using System.Text.Json;

    namespace NextHorizon.Controllers
    {
        public class AdminController : Controller
        {
            private readonly DashboardService _dashboardService = new DashboardService();
            private readonly IConfiguration _configuration;
            private readonly string _connectionString;
            private readonly IPasswordHasher<object> _passwordHasher;
            private readonly IEmailService _emailService;


            //tanginamo
            // Constructor to inject configuration
            public AdminController(IConfiguration configuration, IEmailService emailService)
            {
                _configuration = configuration;
                _connectionString = _configuration.GetConnectionString("DefaultConnection");
                _passwordHasher = new PasswordHasher<object>();;
                _emailService = emailService;

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
        [HttpGet]
  public async Task<IActionResult> Dashboard()
{
    var redirect = RedirectToLoginIfNotAuthenticated();
    if (redirect != null) return redirect;

    var leaderboard   = new List<ConsumerLeaderboardViewModel>();
    var auditLogs     = new List<DashboardAuditLog>();
    var approvalItems = new List<DashboardApprovalItem>();
    decimal revenue   = 0;
    int pendingPayouts = 0, pendingSellers = 0, pendingTickets = 0;

    using (var connection = new SqlConnection(_connectionString))
    {
        using (var cmd = new SqlCommand("sp_GetDashboardStats", connection))
        {
            cmd.CommandType = CommandType.StoredProcedure;
            await connection.OpenAsync();
            using (var reader = await cmd.ExecuteReaderAsync())
            {
                // Result Set 1: Header Stats
                if (await reader.ReadAsync())
                {
                    var rev        = reader["PlatformRevenue"];
                    revenue        = rev != DBNull.Value ? Convert.ToDecimal(rev) : 0;
                    pendingPayouts = reader.GetInt32(reader.GetOrdinal("PendingPayouts"));
                    pendingSellers = reader.GetInt32(reader.GetOrdinal("PendingSellers"));
                }

                    // Result Set 2: Leaderboard
                    await reader.NextResultAsync();
                    while (await reader.ReadAsync())
                    {
                        leaderboard.Add(new ConsumerLeaderboardViewModel
                        {
                            Rank       = reader.GetInt32(reader.GetOrdinal("Rank")),
                            UserName   = reader["AthleteName"]?.ToString() ?? "Unknown User",
                            StravaKM   = Convert.ToDecimal(reader["DistanceKm"] ?? 0),
                            IsVerified = reader["IsVerified"] != DBNull.Value && Convert.ToBoolean(reader["IsVerified"])
                        });
                    }

                // Result Set 3: Audit Logs
                await reader.NextResultAsync();
                while (await reader.ReadAsync())
                    auditLogs.Add(new DashboardAuditLog
                    {
                        Timestamp = reader.GetDateTime(reader.GetOrdinal("timestamp")),
                        AdminName = reader["admin_name"]?.ToString() ?? "",
                        Action    = reader["action"]?.ToString()     ?? "",
                        Target    = reader["target"]?.ToString()     ?? "",
                        Status    = reader["status"]?.ToString()     ?? ""
                    });

                // Result Set 4: Approval Hub
                await reader.NextResultAsync();
                while (await reader.ReadAsync())
                    approvalItems.Add(new DashboardApprovalItem
                    {
                        RequestType = reader["RequestType"]?.ToString() ?? "",
                        EntityName  = reader["EntityName"]?.ToString()  ?? "",
                        Details     = reader["Details"]?.ToString()     ?? "",
                        Status      = reader["Status"]?.ToString()      ?? "",
                        ActionLabel = reader["ActionLabel"]?.ToString() ?? "",
                        RedirectUrl = reader["RedirectUrl"]?.ToString() ?? ""
                    });
            }
        }
    }

    var model = new SuperAdminDashboardViewModel
    {
        PlatformRevenue     = revenue,
        PendingPayouts      = pendingPayouts,
        PendingSellers      = pendingSellers,
        PendingTickets      = pendingTickets,
        ConsumerLeaderboard = leaderboard,
        AuditLogs           = auditLogs,
        ApprovalHub         = approvalItems,
        Stats               = new PlatformStats(),
        TopSellers          = new List<TopSellerViewModel>()
    };

    ViewBag.UserRole = GetCurrentUserRole();
    return View(model);
}

[HttpGet]
public async Task<IActionResult> GetLeaderboard(string category = "All")
{
    var leaderboard = new List<object>();

    using (var connection = new SqlConnection(_connectionString))
    {
        using (var cmd = new SqlCommand("sp_GetLeaderboard", connection))
        {
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Parameters.AddWithValue("@Category", category);
            await connection.OpenAsync();
            using (var reader = await cmd.ExecuteReaderAsync())
                while (await reader.ReadAsync())
                    leaderboard.Add(new
                    {
                        rank       = reader.GetInt32(reader.GetOrdinal("Rank")),
                        userName   = reader["AthleteName"]?.ToString() ?? "",
                        stravaKM   = reader.GetDecimal(reader.GetOrdinal("DistanceKm")),
                        isVerified = reader.GetBoolean(reader.GetOrdinal("IsVerified"))
                    });
        }
    }

    return Json(leaderboard);
}

            // URL: /Admin/Analytics - Accessible by SuperAdmin, Admin, and Finance Officer
public async Task<IActionResult> Analytics()
{
    var redirect = RedirectToLoginIfNotAuthenticated();
    if (redirect != null) return redirect;
    var unauthorized = RedirectIfUnauthorized(new[] { "SuperAdmin", "Admin", "Finance Officer" });
    if (unauthorized != null) return unauthorized;

    var topSellers        = new List<SellerMetric>();
    var topProducts       = new List<ProductMetric>();
    var performanceTrends = new List<AnalyticsChartData>();
    var peakEngagement    = new List<HourlyEngagementMetric>();

    int totalConsumers = 0, totalSellers = 0, totalOrders = 0;
    decimal totalRevenue = 0, avgOrderValue = 0;

    using (var connection = new SqlConnection(_connectionString))
    {
        using (var cmd = new SqlCommand("sp_GetAnalyticsSummary", connection))
        {
            cmd.CommandType = CommandType.StoredProcedure;
            await connection.OpenAsync();
            using (var reader = await cmd.ExecuteReaderAsync())
            {
                // Result Set 1: Header Stats
                if (await reader.ReadAsync())
                {
                    totalConsumers = reader.GetInt32(reader.GetOrdinal("TotalConsumers"));
                    totalSellers   = reader.GetInt32(reader.GetOrdinal("TotalSellers"));
                    totalOrders    = reader.GetInt32(reader.GetOrdinal("TotalOrders"));
                    var avg        = await Task.FromResult(reader["AvgOrderValue"]);
                    avgOrderValue  = avg != DBNull.Value ? Convert.ToDecimal(avg) : 0;
                }

                // Result Set 2: Performance Trends
                await reader.NextResultAsync();
                while (await reader.ReadAsync())
                    performanceTrends.Add(new AnalyticsChartData
                    {
                        DateLabel             = reader["DateLabel"]?.ToString() ?? "",
                        TotalRevenue          = reader.GetDecimal(reader.GetOrdinal("TotalRevenue")),
                        ChallengeParticipants = reader.GetInt32(reader.GetOrdinal("ChallengeParticipants"))
                    });

                // Result Set 3: Top Sellers
                await reader.NextResultAsync();
                while (await reader.ReadAsync())
                    topSellers.Add(new SellerMetric
                    {
                        Rank             = (int)reader.GetInt64(reader.GetOrdinal("Rank")),
                        SellerName       = reader["ShopName"]?.ToString() ?? "",
                        ShopName         = reader["ShopName"]?.ToString() ?? "",
                        OrdersFulfilled  = reader.GetInt32(reader.GetOrdinal("OrdersFulfilled")),
                        RevenueGenerated = reader.GetDecimal(reader.GetOrdinal("RevenueGenerated"))
                    });

                // Result Set 4: Top Products
                await reader.NextResultAsync();
                while (await reader.ReadAsync())
                    topProducts.Add(new ProductMetric
                    {
                        ProductName = reader["ProductName"]?.ToString() ?? "",
                        Category    = reader["Category"]?.ToString()    ?? "",
                        UnitsSold   = reader.GetInt32(reader.GetOrdinal("UnitsSold")),
                        Revenue     = reader.GetDecimal(reader.GetOrdinal("Revenue")),
                        SellerName  = reader["ShopName"]?.ToString() ?? ""
                    });

                // Result Set 5: Peak Engagement
                await reader.NextResultAsync();
                while (await reader.ReadAsync())
                    peakEngagement.Add(new HourlyEngagementMetric
                    {
                        Hour              = reader.GetInt32(reader.GetOrdinal("Hour")),
                        PurchaseCount     = reader.GetInt32(reader.GetOrdinal("PurchaseCount")),
                        ActivitySyncCount = reader.GetInt32(reader.GetOrdinal("ActivitySyncCount"))
                    });
            }
        }
    }

    if (!performanceTrends.Any())
        performanceTrends.Add(new AnalyticsChartData { DateLabel = "No Data", TotalRevenue = 0, ChallengeParticipants = 0 });

    var viewModel = new AnalyticsViewModel
    {
        TotalConsumers                = totalConsumers,
        TotalSellers                  = totalSellers,
        TotalRevenue                  = totalRevenue,
        TotalOrders                   = totalOrders,
        AverageOrderValue             = (double)avgOrderValue,
        ChallengeToSaleConversionRate = totalOrders > 0 && totalConsumers > 0
            ? Math.Round((double)totalOrders / totalConsumers * 100, 1) : 0,
        PerformanceTrends  = performanceTrends,
        TopSellers         = topSellers,
        TopMovingProducts  = topProducts,
        PeakEngagementData = peakEngagement.Any() ? peakEngagement : new List<HourlyEngagementMetric>
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

        if (!string.IsNullOrEmpty(startDate) && !string.IsNullOrEmpty(endDate))
        {
            if (!DateTime.TryParse(startDate, System.Globalization.CultureInfo.InvariantCulture,
                    System.Globalization.DateTimeStyles.None, out start)
             || !DateTime.TryParse(endDate, System.Globalization.CultureInfo.InvariantCulture,
                    System.Globalization.DateTimeStyles.None, out end))
            {
                return Json(new { error = "Invalid date format." });
            }
            end = end.AddDays(1);
        }
        else
        {
            end   = DateTime.Now;
            start = days switch
            {
                7  => DateTime.Now.AddDays(-7),
                90 => DateTime.Now.AddDays(-90),
                _  => DateTime.Now.AddDays(-30)
            };
        }

        var trends      = new List<object>();
        var peakData    = new List<object>();
        var topProducts = new List<object>();
        int totalOrders = 0;
        decimal totalRevenue = 0, avgOrder = 0;

        using (var connection = new SqlConnection(_connectionString))
        {
            using (var cmd = new SqlCommand("sp_GetAnalyticsData", connection))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@Start", start);
                cmd.Parameters.AddWithValue("@End",   end);
                await connection.OpenAsync();

                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    // Result Set 1: Trends
                    while (await reader.ReadAsync())
                        trends.Add(new
                        {
                            dateLabel             = reader["DateLabel"]?.ToString() ?? "",
                            totalRevenue          = reader.GetDecimal(reader.GetOrdinal("TotalRevenue")),
                            challengeParticipants = reader.GetInt32(reader.GetOrdinal("ChallengeParticipants"))
                        });

                    // Result Set 2: Stats
                    await reader.NextResultAsync();
                    if (await reader.ReadAsync())
                    {
                        totalOrders  = reader.GetInt32(reader.GetOrdinal("TotalOrders"));
                        var rev      = reader["TotalRevenue"];
                        totalRevenue = rev != DBNull.Value ? Convert.ToDecimal(rev) : 0;
                        var avg      = reader["AvgOrder"];
                        avgOrder     = avg != DBNull.Value ? Convert.ToDecimal(avg) : 0;
                    }

                    // Result Set 3: Peak Hours
                    await reader.NextResultAsync();
                    while (await reader.ReadAsync())
                        peakData.Add(new
                        {
                            hour              = reader.GetInt32(reader.GetOrdinal("Hour")),
                            purchaseCount     = reader.GetInt32(reader.GetOrdinal("PurchaseCount")),
                            activitySyncCount = reader.GetInt32(reader.GetOrdinal("ActivitySyncCount"))
                        });

                    // Result Set 4: Top Products
                    await reader.NextResultAsync();
                    while (await reader.ReadAsync())
                        topProducts.Add(new
                        {
                            productName = reader["ProductName"]?.ToString() ?? "",
                            unitsSold   = reader.GetInt32(reader.GetOrdinal("UnitsSold")),
                            revenue     = reader.GetDecimal(reader.GetOrdinal("Revenue")),
                            sellerName  = reader["ShopName"]?.ToString() ?? ""
                        });
                }
            }
        }

        return Json(new { trends, peakData, topProducts, totalOrders, totalRevenue, avgOrderValue = avgOrder });
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
        using (var cmd = new SqlCommand("sp_GetSellerPerformance", connection))
        {
            cmd.CommandType = CommandType.StoredProcedure;
            await connection.OpenAsync();
            using (var reader = await cmd.ExecuteReaderAsync())
            {
                // Result set 1 - Top Sellers
                while (await reader.ReadAsync())
                    topSellers.Add(new SellerMetric
                    {
                        Rank             = (int)reader.GetInt64(reader.GetOrdinal("Rank")),
                        SellerName       = reader["ShopName"]?.ToString() ?? "",
                        ShopName         = reader["ShopName"]?.ToString() ?? "",
                        OrdersFulfilled  = reader.GetInt32(reader.GetOrdinal("OrdersFulfilled")),
                        RevenueGenerated = reader.GetDecimal(reader.GetOrdinal("RevenueGenerated"))
                    });

                // Result set 2 - Top Products
                await reader.NextResultAsync();
                while (await reader.ReadAsync())
                    topProducts.Add(new ProductMetric
                    {
                        ProductName = reader["ProductName"]?.ToString() ?? "",
                        Category    = reader["Category"]?.ToString()    ?? "",
                        SalesCount  = reader.GetInt32(reader.GetOrdinal("SalesCount")),
                        Revenue     = reader.GetDecimal(reader.GetOrdinal("Revenue"))
                    });

                // Result set 3 - Platform Totals
                await reader.NextResultAsync();
                if (await reader.ReadAsync())
                {
                    totalOrders  = reader.GetInt32(reader.GetOrdinal("TotalOrders"));
                    totalRevenue = reader.GetDecimal(reader.GetOrdinal("TotalRevenue"));
                }
            }
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
            using (var cmd = new SqlCommand("sp_GetSellerPerformance", connection))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                await connection.OpenAsync();
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
                        await connection.OpenAsync();
                        
                        // Get consumer info
                        string consumerEmail = "";
                        string consumerName = "";
                        
                        using (var cmd = new SqlCommand(@"
                            SELECT u.email, CONCAT(c.first_name, ' ', ISNULL(c.middle_name + ' ', ''), c.last_name) as full_name
                            FROM consumers c
                            LEFT JOIN users u ON c.user_id = u.user_id
                            WHERE c.consumer_id = @ConsumerId", connection))
                        {
                            cmd.Parameters.AddWithValue("@ConsumerId", request.ConsumerId);
                            using (var reader = await cmd.ExecuteReaderAsync())
                            {
                                if (await reader.ReadAsync())
                                {
                                    consumerEmail = reader["email"].ToString();
                                    consumerName = reader["full_name"].ToString();
                                }
                            }
                        }
                        
                        // Perform deletion
                        using (var command = new SqlCommand("sp_DeleteConsumer", connection))
                        {
                            command.CommandType = CommandType.StoredProcedure;
                            command.Parameters.AddWithValue("@ConsumerId", request.ConsumerId);
                            command.Parameters.AddWithValue("@StaffId", staffId);
                            command.Parameters.AddWithValue("@AdminName", adminName);
                            
                            using (var reader = await command.ExecuteReaderAsync())
                            {
                                if (await reader.ReadAsync())
                                {
                                    var status = reader["Status"].ToString();
                                    var message = reader["Message"].ToString();
                                    
                                    if (status == "Success" && !string.IsNullOrEmpty(consumerEmail))
                                    {
                                        // Only send email if deletion was successful
                                        await _emailService.SendAccountDeletionEmailAsync(consumerEmail, consumerName, adminName);
                                        message += " Email notification sent to consumer.";
                                    }
                                    
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
                                    var email = reader["Email"]?.ToString();
                                    var fullName = reader["FullName"]?.ToString();
                                    
                                    string responseMessage = message;
                                    
                                    // Send email notification if restoration was successful
                                    if (status == "Success" && !string.IsNullOrEmpty(email))
                                    {
                                        bool emailSent = await _emailService.SendAccountRestoreEmailAsync(email, fullName, adminName);
                                        
                                        if (emailSent)
                                        {
                                            responseMessage += " Email notification sent to consumer.";
                                        }
                                        else
                                        {
                                            responseMessage += " Warning: Email notification failed to send.";
                                        }
                                    }
                                    
                                    return Json(new { success = status == "Success", message = responseMessage });
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
            using (var cmd = new SqlCommand("sp_GetSellers", connection))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@Status", status);
                await connection.OpenAsync();
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
                            // ADD THIS — reads the has_document flag from SP
                            HasDocument     = reader.GetInt32(reader.GetOrdinal("has_document")) == 1,
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


[HttpGet]
public async Task<IActionResult> GetSellerDocument(int sellerId, string docType = "DTI")
{
    try
    {
        using (var connection = new SqlConnection(_connectionString))
        {
            await connection.OpenAsync();
            var sql = @"
                SELECT 
                    document_path, document_data, document_content_type,
                    dti_data, dti_content_type,
                    bir_data, bir_content_type,
                    permit_data, permit_content_type,
                    additional_doc_data, additional_doc_content_type
                FROM Sellers WHERE seller_id = @SellerId";

            using (var cmd = new SqlCommand(sql, connection))
            {
                cmd.Parameters.AddWithValue("@SellerId", sellerId);
                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    if (await reader.ReadAsync())
                    {
                        // Format 1: New 4-doc binary format
                        byte[] data = null;
                        string contentType = "application/pdf";

                        if (docType == "DTI" && !reader.IsDBNull(reader.GetOrdinal("dti_data")))
                        {
                            data = (byte[])reader["dti_data"];
                            contentType = reader["dti_content_type"]?.ToString() ?? "application/pdf";
                        }
                        else if (docType == "BIR" && !reader.IsDBNull(reader.GetOrdinal("bir_data")))
                        {
                            data = (byte[])reader["bir_data"];
                            contentType = reader["bir_content_type"]?.ToString() ?? "application/pdf";
                        }
                        else if (docType == "Permit" && !reader.IsDBNull(reader.GetOrdinal("permit_data")))
                        {
                            data = (byte[])reader["permit_data"];
                            contentType = reader["permit_content_type"]?.ToString() ?? "application/pdf";
                        }
                        else if (docType == "Additional" && !reader.IsDBNull(reader.GetOrdinal("additional_doc_data")))
                        {
                            data = (byte[])reader["additional_doc_data"];
                            contentType = reader["additional_doc_content_type"]?.ToString() ?? "application/pdf";
                        }
                        // Format 2: Single document_data binary
                        else if (!reader.IsDBNull(reader.GetOrdinal("document_data")))
                        {
                            data = (byte[])reader["document_data"];
                            contentType = reader["document_content_type"]?.ToString() ?? "application/pdf";
                        }

                        if (data != null)
                            return File(data, contentType);

                        // Format 3: Old file path format
                        var docPath = reader["document_path"]?.ToString();
                        if (!string.IsNullOrEmpty(docPath))
                        {
                            var paths = docPath.Split(';').Where(p => !string.IsNullOrEmpty(p.Trim())).ToList();
                            return Json(new { paths, format = "path" });
                        }
                    }
                }
            }
        }
        return NotFound("No documents found.");
    }
    catch (Exception ex) { return BadRequest(ex.Message); }
}

[HttpGet]
public async Task<IActionResult> GetSellerDocumentInfo(int sellerId)
{
    try
    {
        using (var connection = new SqlConnection(_connectionString))
        {
            await connection.OpenAsync();
            var sql = @"
                SELECT 
                    document_path,
                    CASE WHEN document_data IS NOT NULL THEN 1 ELSE 0 END as has_doc_data,
                    CASE WHEN dti_data IS NOT NULL THEN 1 ELSE 0 END as has_dti,
                    CASE WHEN bir_data IS NOT NULL THEN 1 ELSE 0 END as has_bir,
                    CASE WHEN permit_data IS NOT NULL THEN 1 ELSE 0 END as has_permit,
                    CASE WHEN additional_doc_data IS NOT NULL THEN 1 ELSE 0 END as has_additional
                FROM Sellers WHERE seller_id = @SellerId";

            using (var cmd = new SqlCommand(sql, connection))
            {
                cmd.Parameters.AddWithValue("@SellerId", sellerId);
                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    if (await reader.ReadAsync())
                    {
                        var hasDti        = reader.GetInt32(reader.GetOrdinal("has_dti")) == 1;
                        var hasBir        = reader.GetInt32(reader.GetOrdinal("has_bir")) == 1;
                        var hasPermit     = reader.GetInt32(reader.GetOrdinal("has_permit")) == 1;
                        var hasAdditional = reader.GetInt32(reader.GetOrdinal("has_additional")) == 1;
                        var hasDocData    = reader.GetInt32(reader.GetOrdinal("has_doc_data")) == 1;
                        var docPath       = reader["document_path"]?.ToString();

                        // New format
                        if (hasDti || hasBir || hasPermit)
                        {
                            var docs = new List<object>();
                            if (hasDti)        docs.Add(new { label = "DTI Certificate",    type = "DTI" });
                            if (hasBir)        docs.Add(new { label = "BIR Certificate",    type = "BIR" });
                            if (hasPermit)     docs.Add(new { label = "Business Permit",    type = "Permit" });
                            if (hasAdditional) docs.Add(new { label = "Additional Document", type = "Additional" });
                            return Json(new { format = "binary_multi", docs });
                        }

                        // Middle format
                        if (hasDocData)
                            return Json(new { format = "binary_single" });

                        // Old format
                        if (!string.IsNullOrEmpty(docPath))
                        {
                            var paths = docPath.Split(';').Where(p => !string.IsNullOrEmpty(p.Trim())).ToList();
                            return Json(new { format = "path", paths });
                        }
                    }
                }
            }
        }
        return Json(new { format = "none" });
    }
    catch (Exception ex) { return Json(new { error = ex.Message }); }
}


[HttpGet]
public async Task<IActionResult> GetSellerLogo(int sellerId)
{
    try
    {
        using (var connection = new SqlConnection(_connectionString))
        {
            await connection.OpenAsync();
            using (var cmd = new SqlCommand(
                @"SELECT logo_data, logo_content_type, logo_path, 
                         CASE WHEN logo_data IS NOT NULL THEN 'binary' 
                              WHEN logo_path IS NOT NULL THEN 'path'
                              ELSE 'none' END as logo_type
                  FROM Sellers WHERE seller_id = @SellerId", connection))
            {
                cmd.Parameters.AddWithValue("@SellerId", sellerId);
                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    if (await reader.ReadAsync())
                    {
                        // DEBUG: Log what we're getting
                        var logoType = reader["logo_type"]?.ToString() ?? "unknown";
                        Console.WriteLine($"Seller {sellerId}: logo_type={logoType}");

                        // New binary logo
                        if (!reader.IsDBNull(reader.GetOrdinal("logo_data")))
                        {
                            var data = (byte[])reader["logo_data"];
                            var contentType = reader["logo_content_type"]?.ToString() ?? "image/png";
                            Console.WriteLine($"Binary logo found: {data.Length} bytes, type: {contentType}");
                            return File(data, contentType);
                        }
                        
                        // Old file path logo
                        var logoPath = reader["logo_path"]?.ToString();
                        if (!string.IsNullOrEmpty(logoPath))
                        {
                            var fullPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", logoPath.TrimStart('/'));
                            Console.WriteLine($"File path logo: {fullPath}, exists: {System.IO.File.Exists(fullPath)}");
                            
                            if (System.IO.File.Exists(fullPath))
                                return PhysicalFile(fullPath, "image/jpeg");
                        }
                    }
                }
            }
        }
        Console.WriteLine($"No logo found for seller {sellerId}");
        return NotFound();
    }
    catch (Exception ex) 
    { 
        Console.WriteLine($"GetSellerLogo error: {ex.Message}");
        return BadRequest(ex.Message); 
    }
}



    [HttpPost]
    public async Task<IActionResult> UpdateSellerStatus([FromBody] UpdateSellerStatusRequest request)
    {
        try
        {
            var adminName = HttpContext.Session.GetString("Username") ?? "System";
            
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                
                // First, get seller info before update (email, business name, current status)
                string sellerEmail = "";
                string businessName = "";
                string currentStatus = "";
                
                using (var cmd = new SqlCommand(@"
                    SELECT s.seller_status, s.business_name, u.email 
                    FROM Sellers s
                    INNER JOIN users u ON s.user_id = u.user_id
                    WHERE s.seller_id = @SellerId", connection))
                {
                    cmd.Parameters.AddWithValue("@SellerId", request.SellerId);
                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            currentStatus = reader["seller_status"]?.ToString() ?? "";
                            businessName = reader["business_name"]?.ToString() ?? "Seller";
                            sellerEmail = reader["email"]?.ToString() ?? "";
                        }
                    }
                }
                
                using (var transaction = connection.BeginTransaction())
                {
                    try
                    {
                        // Update seller status
                        using (var cmd = new SqlCommand("UPDATE Sellers SET seller_status = @Status WHERE seller_id = @SellerId", connection, transaction))
                        {
                            cmd.Parameters.AddWithValue("@Status", request.Status);
                            cmd.Parameters.AddWithValue("@SellerId", request.SellerId);
                            await cmd.ExecuteNonQueryAsync();
                        }
                        
                        // Insert notification based on status transition
                        string notificationMessage = "";
                        string category = "";
                        string emailStatus = ""; // For email template
                        
                        // Handle Pending → Active (Approval)
                        if (currentStatus == "Pending" && request.Status == "Active")
                        {
                            notificationMessage = $"Your seller application for '{businessName}' has been approved! Welcome aboard!";
                            if (!string.IsNullOrEmpty(request.Note))
                            {
                                notificationMessage += $"\n\nMessage from admin: {request.Note}";
                            }
                            category = "SellerApproval";
                            emailStatus = "Approved";
                        }
                        // Handle Active → Suspended
                        else if (currentStatus == "Active" && request.Status == "Suspended")
                        {
                            notificationMessage = $"Your seller account '{businessName}' has been suspended.";
                            if (!string.IsNullOrEmpty(request.Note))
                            {
                                notificationMessage += $"\n\nReason for suspension: {request.Note}";
                            }
                            else
                            {
                                notificationMessage += " Please contact support for more information.";
                            }
                            category = "SellerSuspension";
                            emailStatus = "Suspended";
                        }
                        // Handle Suspended → Active (Restore)
                        else if (currentStatus == "Suspended" && request.Status == "Active")
                        {
                            notificationMessage = $"Great news! Your seller account '{businessName}' has been reactivated.";
                            if (!string.IsNullOrEmpty(request.Note))
                            {
                                notificationMessage += $"\n\nNote from admin: {request.Note}";
                            }
                            notificationMessage += "\n\nYou can now resume selling on Next Horizon.";
                            category = "SellerRestoration";
                            emailStatus = "Restored";
                        }
                        // Handle Pending → Rejected
                        else if (currentStatus == "Pending" && request.Status == "Rejected")
                        {
                            notificationMessage = $"We regret to inform you that your seller application for '{businessName}' has been rejected.";
                            if (!string.IsNullOrEmpty(request.Note))
                            {
                                notificationMessage += $"\n\nReason: {request.Note}";
                            }
                            else
                            {
                                notificationMessage += " Please contact support for more information.";
                            }
                            category = "SellerRejection";
                            emailStatus = "Rejected";
                        }
                        // Handle any other status changes
                        else
                        {
                            notificationMessage = $"Your seller account '{businessName}' status has been changed to {request.Status}.";
                            if (!string.IsNullOrEmpty(request.Note))
                            {
                                notificationMessage += $"\n\nNote: {request.Note}";
                            }
                            category = "SellerStatusChange";
                            emailStatus = request.Status;
                        }
                        
                        // Insert notification into database
                        using (var cmd = new SqlCommand(@"
                            INSERT INTO Notifications 
                            (RecipientType, RecipientId, OrderId, Message, IsRead, CreatedAt, Category) 
                            VALUES 
                            ('Seller', @RecipientId, NULL, @Message, 0, @CreatedAt, @Category)", 
                            connection, transaction))
                        {
                            cmd.Parameters.AddWithValue("@RecipientId", request.SellerId);
                            cmd.Parameters.AddWithValue("@Message", notificationMessage);
                            cmd.Parameters.AddWithValue("@CreatedAt", DateTime.Now);
                            cmd.Parameters.AddWithValue("@Category", category);
                            await cmd.ExecuteNonQueryAsync();
                        }
                        
                        transaction.Commit();
                        
                        // Send email notification if we have the seller's email
                        bool emailSent = false;
                        if (!string.IsNullOrEmpty(sellerEmail) && !string.IsNullOrEmpty(emailStatus))
                        {
                            emailSent = await _emailService.SendSellerStatusUpdateEmailAsync(
                                sellerEmail, 
                                businessName, 
                                emailStatus, 
                                request.Note ?? "", 
                                adminName
                            );
                        }
                        
                        string successMessage = "";
                        if (currentStatus == "Pending" && request.Status == "Active")
                            successMessage = "Seller approved successfully.";
                        else if (currentStatus == "Active" && request.Status == "Suspended")
                            successMessage = "Seller suspended successfully.";
                        else if (currentStatus == "Suspended" && request.Status == "Active")
                            successMessage = "Seller restored successfully.";
                        else if (currentStatus == "Pending" && request.Status == "Rejected")
                            successMessage = "Seller application rejected successfully.";
                        else
                            successMessage = $"Seller status updated to {request.Status} successfully.";
                        
                        if (emailSent)
                        {
                            successMessage += " Email notification sent to seller.";
                        }
                        else if (!string.IsNullOrEmpty(sellerEmail))
                        {
                            successMessage += " Warning: Email notification failed to send.";
                        }
                        
                        return Json(new { success = true, message = successMessage });
                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback();
                        throw;
                    }
                }
            }
        }
        catch (Exception ex) 
        { 
            return Json(new { success = false, message = ex.Message }); 
        }
    }

[HttpPost]
public async Task<IActionResult> UpdateSellerInfo([FromBody] UpdateSellerInfoRequest request)
{
    try
    {
        using (var connection = new SqlConnection(_connectionString))
        {
            using (var cmd = new SqlCommand("sp_UpdateSellerInfo", connection))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@SellerId",      request.SellerId);
                cmd.Parameters.AddWithValue("@BusinessName",  request.BusinessName);
                cmd.Parameters.AddWithValue("@BusinessEmail", request.BusinessEmail);
                await connection.OpenAsync();
                using (var reader = await cmd.ExecuteReaderAsync())
                    if (await reader.ReadAsync())
                        return Json(new { 
                            success = reader["Status"].ToString() == "Success", 
                            message = reader["Message"].ToString() 
                        });
            }
        }
        return Json(new { success = false, message = "Update failed" });
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
                    // Add validation
                    if (!request.IsIndefinite && string.IsNullOrEmpty(request.EndDate?.ToString()))
                    {
                        return Json(new { success = false, message = "End date is required for non-permanent promotions" });
                    }
                    
                    if (request.DiscountPercent <= 0 || request.DiscountPercent > 100)
                    {
                        return Json(new { success = false, message = "Discount percentage must be between 1 and 100" });
                    }
                    
                    if (request.StartDate > request.EndDate && !request.IsIndefinite && request.EndDate.HasValue)
                    {
                        return Json(new { success = false, message = "End date must be after start date" });
                    }
                    
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

                // 1. Get pending payouts
                using (var cmd = new SqlCommand("sp_GetPendingPayouts", connection))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            var payout = new PayoutRequest
                            {
                                WithdrawalId = reader.GetInt64(reader.GetOrdinal("withdrawal_id")),
                                SellerId = reader.GetInt32(reader.GetOrdinal("seller_id")),
                                Amount = reader.IsDBNull(reader.GetOrdinal("amount")) ? 0 : reader.GetDecimal(reader.GetOrdinal("amount")),
                                RequestedAt = reader.GetDateTime(reader.GetOrdinal("requested_at")),
                                SellerName = reader.IsDBNull(reader.GetOrdinal("seller_name")) ? "" : reader.GetString(reader.GetOrdinal("seller_name")),
                                ShopName = reader.IsDBNull(reader.GetOrdinal("shop_name")) ? "" : reader.GetString(reader.GetOrdinal("shop_name")),
                                SellerEmail = reader.IsDBNull(reader.GetOrdinal("seller_email")) ? "" : reader.GetString(reader.GetOrdinal("seller_email")),
                                BankName = reader.IsDBNull(reader.GetOrdinal("bank_name")) ? "" : reader.GetString(reader.GetOrdinal("bank_name")),
                                SellerNote = reader.IsDBNull(reader.GetOrdinal("seller_note")) ? "" : reader.GetString(reader.GetOrdinal("seller_note"))
                            };

                            viewModel.PendingPayouts.Add(payout);
                            viewModel.TotalPendingPayoutAmount += payout.Amount;
                        }
                    }
                }

                // 2. Get pending discounts - HANDLE ALL DECIMAL FIELDS
                using (var cmd = new SqlCommand("sp_GetPendingDiscounts", connection))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            var discount = new DiscountRequest
                            {
                                // Basic fields
                                Id = reader.GetInt32(reader.GetOrdinal("id")),
                                Name = reader.IsDBNull(reader.GetOrdinal("name")) ? "" : reader.GetString(reader.GetOrdinal("name")),
                                Type = reader.IsDBNull(reader.GetOrdinal("type")) ? "" : reader.GetString(reader.GetOrdinal("type")),
                                BannerSize = reader.IsDBNull(reader.GetOrdinal("BannerSize")) ? "" : reader.GetString(reader.GetOrdinal("BannerSize")),
                                
                                // Decimal fields from stored procedure
                                TotalDiscountPercent = reader.IsDBNull(reader.GetOrdinal("TotalDiscountPercent")) ? 0 : reader.GetDecimal(reader.GetOrdinal("TotalDiscountPercent")),
                                TotalDiscountFix = reader.IsDBNull(reader.GetOrdinal("TotalDiscountFix")) ? 0 : reader.GetDecimal(reader.GetOrdinal("TotalDiscountFix")),
                                DiscountPercent = reader.IsDBNull(reader.GetOrdinal("DiscountPercent")) ? 0 : reader.GetDecimal(reader.GetOrdinal("DiscountPercent")),
                                DiscountedPrice = reader.IsDBNull(reader.GetOrdinal("DiscountedPrice")) ? 0 : reader.GetDecimal(reader.GetOrdinal("DiscountedPrice")),
                                OriginalPrice = reader.IsDBNull(reader.GetOrdinal("OriginalPrice")) ? 0 : reader.GetDecimal(reader.GetOrdinal("OriginalPrice")),
                                MinimumPurchaseAmount = reader.IsDBNull(reader.GetOrdinal("MinimumPurchaseAmount")) ? 0 : reader.GetDecimal(reader.GetOrdinal("MinimumPurchaseAmount")),
                                
                                // Integer fields
                                UsageLimit = reader.IsDBNull(reader.GetOrdinal("UsageLimit")) ? 0 : reader.GetInt32(reader.GetOrdinal("UsageLimit")),
                                BuyQuantity = reader.IsDBNull(reader.GetOrdinal("BuyQuantity")) ? 0 : reader.GetInt32(reader.GetOrdinal("BuyQuantity")),
                                TakeQuantity = reader.IsDBNull(reader.GetOrdinal("TakeQuantity")) ? 0 : reader.GetInt32(reader.GetOrdinal("TakeQuantity")),
                                ReturnWindowDays = reader.IsDBNull(reader.GetOrdinal("ReturnWindowDays")) ? 0 : reader.GetInt32(reader.GetOrdinal("ReturnWindowDays")),
                                UserId = reader.IsDBNull(reader.GetOrdinal("user_id")) ? 0 : reader.GetInt32(reader.GetOrdinal("user_id")),
                                
                                // Boolean field
                                UntilPromotionLast = reader.IsDBNull(reader.GetOrdinal("UntilPromotionLast")) ? false : reader.GetBoolean(reader.GetOrdinal("UntilPromotionLast")),
                                
                                // String fields
                                Status = reader.IsDBNull(reader.GetOrdinal("status")) ? "Pending" : reader.GetString(reader.GetOrdinal("status")),
                                ShopName = reader.IsDBNull(reader.GetOrdinal("ShopName")) ? "" : reader.GetString(reader.GetOrdinal("ShopName")),
                                ProductName = reader.IsDBNull(reader.GetOrdinal("ProductName")) ? "" : reader.GetString(reader.GetOrdinal("ProductName")),
                                MinimumRequirementType = reader.IsDBNull(reader.GetOrdinal("MinimumRequirementType")) ? null : reader.GetString(reader.GetOrdinal("MinimumRequirementType")),
                                FreeItemRequirement = reader.IsDBNull(reader.GetOrdinal("FreeItemRequirement")) ? null : reader.GetString(reader.GetOrdinal("FreeItemRequirement")),
                                ProductImage = reader.IsDBNull(reader.GetOrdinal("ProductImage")) ? null : reader.GetString(reader.GetOrdinal("ProductImage")),
                                
                                // DateTime fields
                                CreatedAt = reader.IsDBNull(reader.GetOrdinal("CreatedAt")) ? DateTime.Now : reader.GetDateTime(reader.GetOrdinal("CreatedAt")),
                                StartDate = reader.IsDBNull(reader.GetOrdinal("StartDate")) ? DateTime.Now : reader.GetDateTime(reader.GetOrdinal("StartDate")),
                                EndDate = reader.IsDBNull(reader.GetOrdinal("EndDate")) ? DateTime.Now.AddDays(7) : reader.GetDateTime(reader.GetOrdinal("EndDate"))
                            };
                            
                            viewModel.PendingDiscounts.Add(discount);
                        }
                    }
                    viewModel.TotalPendingDiscountsCount = viewModel.PendingDiscounts.Count;
                }

                // 3. Get pending products
                using (var cmd = new SqlCommand("sp_GetPendingProducts", connection))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    
                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            // Convert binary image to Base64 string
                            string imageBase64 = null;
                            if (!reader.IsDBNull(reader.GetOrdinal("ImagePath")))
                            {
                                byte[] imageBytes = (byte[])reader["ImagePath"];
                                imageBase64 = Convert.ToBase64String(imageBytes);
                            }
                            
                            viewModel.PendingProducts.Add(new ProductViewModel
                            {
                                ProductId = reader.GetInt32(reader.GetOrdinal("ProductId")),
                                ProductName = reader.GetString(reader.GetOrdinal("ProductName")),
                                SellerName = reader.GetString(reader.GetOrdinal("SellerName")),
                                Category = reader.IsDBNull(reader.GetOrdinal("Category")) ? "Uncategorized" : reader.GetString(reader.GetOrdinal("Category")),
                                Price = reader.IsDBNull(reader.GetOrdinal("Price")) ? 0 : reader.GetDecimal(reader.GetOrdinal("Price")),
                                MainImage = imageBase64 != null ? $"data:image/jpeg;base64,{imageBase64}" : null,
                                Status = reader.GetString(reader.GetOrdinal("Status"))
                            });
                        }
                    }
                    viewModel.TotalPendingProductsCount = viewModel.PendingProducts.Count;
                }
            }

            return viewModel;
        }

        [HttpGet]
        public async Task<IActionResult> GetPendingProducts()
        {
            try
            {
                var products = new List<ProductApprovalRequest>();
                
                using (var connection = new SqlConnection(_connectionString))
                {
                    using (var command = new SqlCommand("sp_GetPendingProducts", connection))
                    {
                        command.CommandType = CommandType.StoredProcedure;
                        
                        await connection.OpenAsync();
                        
                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                products.Add(new ProductApprovalRequest
                                {
                                    ProductId = reader.GetInt32(reader.GetOrdinal("ProductId")),
                                    ProductName = reader.GetString(reader.GetOrdinal("ProductName")),
                                    SellerName = reader.GetString(reader.GetOrdinal("SellerName")),
                                    Category = reader.IsDBNull(reader.GetOrdinal("Category")) ? "Uncategorized" : reader.GetString(reader.GetOrdinal("Category")),
                                    Price = reader.GetDecimal(reader.GetOrdinal("Price")),
                                    MainImage = reader.IsDBNull(reader.GetOrdinal("ImagePath")) ? null : reader.GetString(reader.GetOrdinal("ImagePath")),
                                    ShopName = reader.IsDBNull(reader.GetOrdinal("ShopName")) ? reader.GetString(reader.GetOrdinal("SellerName")) : reader.GetString(reader.GetOrdinal("ShopName"))
                                });
                            }
                        }
                    }
                }
                
                return Json(products);
            }
            catch (Exception ex)
            {
                return Json(new { error = ex.Message });
            }
        }


        [HttpPost]
        public async Task<IActionResult> UpdateProductStatus([FromBody] UpdateProductStatusRequest request)
        {
            try
            {
                var staffId = HttpContext.Session.GetInt32("StaffId") ?? 0;
                var adminName = HttpContext.Session.GetString("Username") ?? "System";
                var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
                var userAgent = Request.Headers["User-Agent"].ToString();
                
                using (var connection = new SqlConnection(_connectionString))
                {
                    using (var command = new SqlCommand("sp_UpdateProductStatus", connection))
                    {
                        command.CommandType = CommandType.StoredProcedure;
                        command.Parameters.AddWithValue("@ProductId", request.ProductId);
                        command.Parameters.AddWithValue("@Status", request.Action);
                        command.Parameters.AddWithValue("@Reason", request.Reason ?? (object)DBNull.Value);
                        command.Parameters.AddWithValue("@ProcessedBy", staffId);
                        command.Parameters.AddWithValue("@IpAddress", ipAddress ?? (object)DBNull.Value);
                        command.Parameters.AddWithValue("@UserAgent", userAgent ?? (object)DBNull.Value);
                        
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
                
                return Json(new { success = false, message = "Failed to update product status" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
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
                                        LogisticsId = reader.IsDBNull(reader.GetOrdinal("logistics_id")) ? 0 : Convert.ToInt32(reader["logistics_id"]),
                                        CourierName = reader.IsDBNull(reader.GetOrdinal("courier_name")) ? null : reader["courier_name"].ToString(),
                                        ServiceType = reader.IsDBNull(reader.GetOrdinal("service_type")) ? null : reader["service_type"].ToString(),
                                        TotalOrders = reader.IsDBNull(reader.GetOrdinal("total_orders")) ? 0 : Convert.ToInt32(reader["total_orders"]),
                                        DeliveredOrders = reader.IsDBNull(reader.GetOrdinal("delivered_orders")) ? 0 : Convert.ToInt32(reader["delivered_orders"]),
                                        CancelledOrders = reader.IsDBNull(reader.GetOrdinal("cancelled_orders")) ? 0 : Convert.ToInt32(reader["cancelled_orders"]),
                                        FailedOrders = reader.IsDBNull(reader.GetOrdinal("failed_orders")) ? 0 : Convert.ToInt32(reader["failed_orders"]),
                                        PendingOrders = reader.IsDBNull(reader.GetOrdinal("pending_orders")) ? 0 : Convert.ToInt32(reader["pending_orders"]),
                                        InTransitOrders = reader.IsDBNull(reader.GetOrdinal("in_transit_orders")) ? 0 : Convert.ToInt32(reader["in_transit_orders"]),
                                        // Handle NULL for success_rate and avg_delivery_days
                                        SuccessRate = reader.IsDBNull(reader.GetOrdinal("success_rate")) ? 0 : Convert.ToDecimal(reader["success_rate"]),
                                        AvgDeliveryDays = reader.IsDBNull(reader.GetOrdinal("avg_delivery_days")) ? 0 : Convert.ToDecimal(reader["avg_delivery_days"]),
                                        MinDeliveryDays = reader.IsDBNull(reader.GetOrdinal("min_delivery_days")) ? (decimal?)null : Convert.ToDecimal(reader["min_delivery_days"]),
                                        MaxDeliveryDays = reader.IsDBNull(reader.GetOrdinal("max_delivery_days")) ? (decimal?)null : Convert.ToDecimal(reader["max_delivery_days"]),
                                        Last7DaysOrders = reader.IsDBNull(reader.GetOrdinal("last_7_days_orders")) ? 0 : Convert.ToInt32(reader["last_7_days_orders"]),
                                        Last30DaysOrders = reader.IsDBNull(reader.GetOrdinal("last_30_days_orders")) ? 0 : Convert.ToInt32(reader["last_30_days_orders"]),
                                        Last90DaysOrders = reader.IsDBNull(reader.GetOrdinal("last_90_days_orders")) ? 0 : Convert.ToInt32(reader["last_90_days_orders"]),
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
                                        Year = reader.IsDBNull(reader.GetOrdinal("year")) ? 0 : Convert.ToInt32(reader["year"]),
                                        Month = reader.IsDBNull(reader.GetOrdinal("month")) ? 0 : Convert.ToInt32(reader["month"]),
                                        MonthName = reader.IsDBNull(reader.GetOrdinal("month_name")) ? "Unknown" : reader["month_name"].ToString(),
                                        TotalOrders = reader.IsDBNull(reader.GetOrdinal("total_orders")) ? 0 : Convert.ToInt32(reader["total_orders"]),
                                        DeliveredOrders = reader.IsDBNull(reader.GetOrdinal("delivered_orders")) ? 0 : Convert.ToInt32(reader["delivered_orders"]),
                                        SuccessRate = reader.IsDBNull(reader.GetOrdinal("success_rate")) ? 0 : Convert.ToDecimal(reader["success_rate"]),
                                        AvgDeliveryDays = reader.IsDBNull(reader.GetOrdinal("avg_delivery_days")) ? 0 : Convert.ToDecimal(reader["avg_delivery_days"])
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



           
        #region Challenges

        // GET: Tasks page (list all challenges)
        public IActionResult Tasks()
        {
            var redirect = RedirectToLoginIfNotAuthenticated();
            if (redirect != null) return redirect;

            ViewBag.UserRole = GetCurrentUserRole();
            return View();
        }

        // GET: Challenge details page
        public async Task<IActionResult> ChallengeDetails(int id)
        {
            var redirect = RedirectToLoginIfNotAuthenticated();
            if (redirect != null) return redirect;

            var unauthorized = RedirectIfUnauthorized(new[] { "SuperAdmin", "Admin" });
            if (unauthorized != null) return unauthorized;

            ViewBag.UserRole = GetCurrentUserRole();
            ViewBag.ChallengeId = id;
            return View();
        }

        // GET: Get all challenges
        [HttpGet]
        public async Task<IActionResult> GetAllChallenges(string status = null)
        {
            try
            {
                var challenges = new List<ChallengeViewModel>();
                
                using (var connection = new SqlConnection(_connectionString))
                {
                    using (var command = new SqlCommand("sp_GetAllChallenges", connection))
                    {
                        command.CommandType = CommandType.StoredProcedure;
                        command.Parameters.AddWithValue("@Status", status ?? (object)DBNull.Value);
                        
                        await connection.OpenAsync();
                        
                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                var challenge = new ChallengeViewModel
                                {
                                    ChallengeId = reader.GetInt32(reader.GetOrdinal("challenge_id")),
                                    Title = reader.GetString(reader.GetOrdinal("title")),
                                    Description = reader.IsDBNull(reader.GetOrdinal("description")) ? null : reader.GetString(reader.GetOrdinal("description")),
                                    Rules = reader.IsDBNull(reader.GetOrdinal("rules")) ? null : reader.GetString(reader.GetOrdinal("rules")),
                                    Prizes = reader.IsDBNull(reader.GetOrdinal("prizes")) ? null : reader.GetString(reader.GetOrdinal("prizes")),
                                    GoalKm = reader.GetDecimal(reader.GetOrdinal("goal_km")),
                                    ActivityType = reader.GetString(reader.GetOrdinal("activity_type")),
                                    StartDate = reader.GetDateTime(reader.GetOrdinal("start_date")),
                                    EndDate = reader.GetDateTime(reader.GetOrdinal("end_date")),
                                    Status = reader.GetString(reader.GetOrdinal("status")),
                                    BannerImageName = reader.IsDBNull(reader.GetOrdinal("banner_image_name")) ? null : reader.GetString(reader.GetOrdinal("banner_image_name")),
                                    BannerImageContentType = reader.IsDBNull(reader.GetOrdinal("banner_image_content_type")) ? null : reader.GetString(reader.GetOrdinal("banner_image_content_type")),
                                    TotalParticipants = reader.GetInt32(reader.GetOrdinal("total_participants")),
                                    TotalCompleted = reader.GetInt32(reader.GetOrdinal("total_completed")),
                                    CompletionRate = reader.GetDecimal(reader.GetOrdinal("completion_rate")),
                                    CreatedAt = reader.GetDateTime(reader.GetOrdinal("created_at")),
                                    UpdatedAt = reader.GetDateTime(reader.GetOrdinal("updated_at"))
                                };
                                
                                // Convert binary image to Base64
                                if (!reader.IsDBNull(reader.GetOrdinal("banner_image")))
                                {
                                    try
                                    {
                                        byte[] imageData = (byte[])reader["banner_image"];
                                        if (imageData != null && imageData.Length > 0)
                                        {
                                            string contentType = challenge.BannerImageContentType ?? "image/jpeg";
                                            string base64String = Convert.ToBase64String(imageData);
                                            challenge.BannerBase64 = $"data:{contentType};base64,{base64String}";
                                        }
                                    }
                                    catch (Exception ex)
                                    {
                                        Console.WriteLine($"Error converting image for challenge {challenge.ChallengeId}: {ex.Message}");
                                        challenge.BannerBase64 = null;
                                    }
                                }
                                
                                challenges.Add(challenge);
                            }
                        }
                    }
                }
                
                return Json(challenges);
            }
            catch (Exception ex)
            {
                return Json(new { error = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetGlobalLeaderboard(int? challengeId = null)
        {
            try
            {
                var leaderboard = new List<GlobalLeaderboardEntry>();
                
                using (var connection = new SqlConnection(_connectionString))
                {
                    using (var command = new SqlCommand("sp_GetGlobalLeaderboard", connection))
                    {
                        command.CommandType = CommandType.StoredProcedure;
                        command.Parameters.AddWithValue("@ChallengeId", challengeId ?? (object)DBNull.Value);
                        
                        await connection.OpenAsync();
                        
                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                // Convert BIGINT to Int32
                                object globalRankObj = reader["global_rank"];
                                int globalRank = globalRankObj != DBNull.Value ? Convert.ToInt32(globalRankObj) : 0;
                                
                                object participantIdObj = reader["participant_id"];
                                int participantId = participantIdObj != DBNull.Value ? Convert.ToInt32(participantIdObj) : 0;
                                
                                object userIdObj = reader["user_id"];
                                int userId = userIdObj != DBNull.Value ? Convert.ToInt32(userIdObj) : 0;
                                
                                object consumerIdObj = reader["consumer_id"];
                                int consumerId = consumerIdObj != DBNull.Value ? Convert.ToInt32(consumerIdObj) : 0;
                                
                                object challengeIdObj = reader["challenge_id"];
                                int challengeIdVal = challengeIdObj != DBNull.Value ? Convert.ToInt32(challengeIdObj) : 0;
                                
                                object totalActivitiesObj = reader["total_activities"];
                                int totalActivities = totalActivitiesObj != DBNull.Value ? Convert.ToInt32(totalActivitiesObj) : 0;
                                
                                object totalTimeSecondsObj = reader["total_time_seconds"];
                                int totalTimeSeconds = totalTimeSecondsObj != DBNull.Value ? Convert.ToInt32(totalTimeSecondsObj) : 0;
                                
                                leaderboard.Add(new GlobalLeaderboardEntry
                                {
                                    GlobalRank = globalRank,
                                    ParticipantId = participantId,
                                    UserId = userId,
                                    ConsumerId = consumerId,
                                    ChallengeId = challengeIdVal,
                                    ChallengeTitle = reader.GetString(reader.GetOrdinal("challenge_title")),
                                    ActivityType = reader.GetString(reader.GetOrdinal("activity_type")),
                                    ChallengeGoalKm = reader.GetDecimal(reader.GetOrdinal("challenge_goal_km")),
                                    AthleteName = reader.GetString(reader.GetOrdinal("athlete_name")),
                                    Username = reader.GetString(reader.GetOrdinal("username")),
                                    PhoneNumber = reader.IsDBNull(reader.GetOrdinal("phone_number")) ? null : reader.GetString(reader.GetOrdinal("phone_number")),
                                    TotalDistanceKm = reader.GetDecimal(reader.GetOrdinal("total_distance_km")),
                                    TotalActivities = totalActivities,
                                    TotalTimeSeconds = totalTimeSeconds,
                                    TotalTimeFormatted = reader.IsDBNull(reader.GetOrdinal("total_time_formatted")) ? "00:00:00" : reader.GetString(reader.GetOrdinal("total_time_formatted")),
                                    AveragePace = reader.GetDecimal(reader.GetOrdinal("average_pace")),
                                    IsCompleted = reader.GetBoolean(reader.GetOrdinal("is_completed")),
                                    LastActivityDate = reader.IsDBNull(reader.GetOrdinal("last_activity_date")) ? null : reader.GetDateTime(reader.GetOrdinal("last_activity_date")),
                                    CompletedAt = reader.IsDBNull(reader.GetOrdinal("completed_at")) ? null : reader.GetDateTime(reader.GetOrdinal("completed_at")),
                                    ChallengeRank = reader.IsDBNull(reader.GetOrdinal("challenge_rank")) ? (int?)null : Convert.ToInt32(reader["challenge_rank"]),
                                    ProgressPercent = reader.GetDecimal(reader.GetOrdinal("progress_percent")),
                                    AvatarUrl = reader.GetString(reader.GetOrdinal("avatar_url"))
                                });
                            }
                        }
                    }
                }
                
                return Json(leaderboard);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in GetGlobalLeaderboard: {ex.Message}");
                return Json(new { error = ex.Message });
            }
        }

        // GET: Get all active challenges for filter dropdown
        [HttpGet]
        public async Task<IActionResult> GetActiveChallenges()
        {
            try
            {
                var challenges = new List<object>();
                
                using (var connection = new SqlConnection(_connectionString))
                {
                    var sql = @"
                        SELECT challenge_id, title, activity_type 
                        FROM challenges 
                        WHERE status = 'Live' 
                        ORDER BY start_date DESC";
                    
                    using (var cmd = new SqlCommand(sql, connection))
                    {
                        await connection.OpenAsync();
                        
                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                challenges.Add(new
                                {
                                    challengeId = reader.GetInt32(reader.GetOrdinal("challenge_id")),
                                    title = reader.GetString(reader.GetOrdinal("title")),
                                    activityType = reader.GetString(reader.GetOrdinal("activity_type"))
                                });
                            }
                        }
                    }
                }
                
                return Json(challenges);
            }
            catch (Exception ex)
            {
                return Json(new { error = ex.Message });
            }
        }
        // GET: Get challenge details
        [HttpGet]
        public async Task<IActionResult> GetChallengeDetails(int id)
        {
            try
            {
                ChallengeViewModel challenge = null;
                var leaderboard = new List<ParticipantLeaderboard>();
                decimal avgDistanceKm = 0;
                decimal avgTimeMinutes = 0;
                
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    
                    using (var command = new SqlCommand("sp_GetChallengeDetails", connection))
                    {
                        command.CommandType = CommandType.StoredProcedure;
                        command.Parameters.AddWithValue("@ChallengeId", id);
                        
                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            // First result set - Challenge details
                            if (await reader.ReadAsync())
                            {
                                challenge = new ChallengeViewModel
                                {
                                    ChallengeId = reader.GetInt32(reader.GetOrdinal("challenge_id")),
                                    Title = reader.GetString(reader.GetOrdinal("title")),
                                    Description = reader.IsDBNull(reader.GetOrdinal("description")) ? null : reader.GetString(reader.GetOrdinal("description")),
                                    Rules = reader.IsDBNull(reader.GetOrdinal("rules")) ? null : reader.GetString(reader.GetOrdinal("rules")),
                                    Prizes = reader.IsDBNull(reader.GetOrdinal("prizes")) ? null : reader.GetString(reader.GetOrdinal("prizes")),
                                    GoalKm = reader.GetDecimal(reader.GetOrdinal("goal_km")),
                                    ActivityType = reader.GetString(reader.GetOrdinal("activity_type")),
                                    StartDate = reader.GetDateTime(reader.GetOrdinal("start_date")),
                                    EndDate = reader.GetDateTime(reader.GetOrdinal("end_date")),
                                    Status = reader.GetString(reader.GetOrdinal("status")),
                                    BannerImageName = reader.IsDBNull(reader.GetOrdinal("banner_image_name")) ? null : reader.GetString(reader.GetOrdinal("banner_image_name")),
                                    BannerImageContentType = reader.IsDBNull(reader.GetOrdinal("banner_image_content_type")) ? null : reader.GetString(reader.GetOrdinal("banner_image_content_type")),
                                    TotalParticipants = reader.GetInt32(reader.GetOrdinal("total_participants")),
                                    TotalCompleted = reader.GetInt32(reader.GetOrdinal("total_completed")),
                                    CreatedAt = reader.GetDateTime(reader.GetOrdinal("created_at"))
                                };
                                
                                avgDistanceKm = reader.GetDecimal(reader.GetOrdinal("avg_distance_km"));
                                avgTimeMinutes = reader.GetDecimal(reader.GetOrdinal("avg_time_minutes"));
                                
                                // Convert binary image to Base64
                                if (!reader.IsDBNull(reader.GetOrdinal("banner_image")))
                                {
                                    try
                                    {
                                        byte[] imageData = (byte[])reader["banner_image"];
                                        if (imageData != null && imageData.Length > 0)
                                        {
                                            string contentType = challenge.BannerImageContentType ?? "image/jpeg";
                                            string base64String = Convert.ToBase64String(imageData);
                                            challenge.BannerBase64 = $"data:{contentType};base64,{base64String}";
                                        }
                                    }
                                    catch (Exception ex)
                                    {
                                        Console.WriteLine($"Error converting image: {ex.Message}");
                                        challenge.BannerBase64 = null;
                                    }
                                }
                            }
                            else
                            {
                                return Json(new { success = false, error = "Challenge not found" });
                            }
                            
                            // Second result set - Leaderboard
                            if (await reader.NextResultAsync())
                            {
                                while (await reader.ReadAsync())
                                {
                                    // Convert BIGINT to Int32 for rank
                                    object rankObj = reader["rank"];
                                    int rank = rankObj != DBNull.Value ? Convert.ToInt32(rankObj) : 0;
                                    
                                    // Convert other BIGINT values
                                    object participantIdObj = reader["participant_id"];
                                    int participantId = participantIdObj != DBNull.Value ? Convert.ToInt32(participantIdObj) : 0;
                                    
                                    object userIdObj = reader["user_id"];
                                    int userId = userIdObj != DBNull.Value ? Convert.ToInt32(userIdObj) : 0;
                                    
                                    object consumerIdObj = reader["consumer_id"];
                                    int consumerId = consumerIdObj != DBNull.Value ? Convert.ToInt32(consumerIdObj) : 0;
                                    
                                    object totalActivitiesObj = reader["total_activities"];
                                    int totalActivities = totalActivitiesObj != DBNull.Value ? Convert.ToInt32(totalActivitiesObj) : 0;
                                    
                                    object totalTimeSecondsObj = reader["total_time_seconds"];
                                    int totalTimeSeconds = totalTimeSecondsObj != DBNull.Value ? Convert.ToInt32(totalTimeSecondsObj) : 0;
                                    
                                    object storedRankObj = reader["stored_rank"];
                                    int? storedRank = storedRankObj != DBNull.Value ? Convert.ToInt32(storedRankObj) : (int?)null;
                                    
                                    leaderboard.Add(new ParticipantLeaderboard
                                    {
                                        Rank = rank,
                                        ParticipantId = participantId,
                                        UserId = userId,
                                        ConsumerId = consumerId,
                                        AthleteName = reader.GetString(reader.GetOrdinal("athlete_name")),
                                        Username = reader.GetString(reader.GetOrdinal("username")),
                                        PhoneNumber = reader.IsDBNull(reader.GetOrdinal("phone_number")) ? null : reader.GetString(reader.GetOrdinal("phone_number")),
                                        TotalDistanceKm = reader.GetDecimal(reader.GetOrdinal("total_distance_km")),
                                        TotalActivities = totalActivities,
                                        TotalTimeSeconds = totalTimeSeconds,
                                        TotalTimeFormatted = reader.IsDBNull(reader.GetOrdinal("total_time_formatted")) ? "00:00:00" : reader.GetString(reader.GetOrdinal("total_time_formatted")),
                                        AveragePace = reader.GetDecimal(reader.GetOrdinal("average_pace")),
                                        IsCompleted = reader.GetBoolean(reader.GetOrdinal("is_completed")),
                                        LastActivityDate = reader.IsDBNull(reader.GetOrdinal("last_activity_date")) ? null : reader.GetDateTime(reader.GetOrdinal("last_activity_date")),
                                        CompletedAt = reader.IsDBNull(reader.GetOrdinal("completed_at")) ? null : reader.GetDateTime(reader.GetOrdinal("completed_at")),
                                        StoredRank = storedRank,
                                        AvatarUrl = reader.GetString(reader.GetOrdinal("avatar_url")),
                                        ProgressPercent = reader.GetDecimal(reader.GetOrdinal("progress_percent")),
                                        ChallengeGoalKm = challenge?.GoalKm ?? 0
                                    });
                                }
                            }
                        }
                    }
                }
                
                return Json(new
                {
                    success = true,
                    challenge = challenge,
                    leaderboard = leaderboard,
                    avgDistanceKm = avgDistanceKm,
                    avgTimeMinutes = avgTimeMinutes
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in GetChallengeDetails: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                return Json(new { success = false, error = ex.Message });
            }
        }        
        
        // GET: Get challenge prizes
        [HttpGet]
        public async Task<IActionResult> GetChallengePrizes(int challengeId)
        {
            try
            {
                var prizes = new List<ChallengePrize>();
                
                using (var connection = new SqlConnection(_connectionString))
                {
                    var sql = @"
                        SELECT cp.*, pt.type_name, pt.type_code
                        FROM challenge_prizes cp
                        INNER JOIN prize_types pt ON cp.prize_type_id = pt.prize_type_id
                        WHERE cp.challenge_id = @ChallengeId AND cp.is_active = 1
                        ORDER BY cp.tier";
                    
                    using (var cmd = new SqlCommand(sql, connection))
                    {
                        cmd.Parameters.AddWithValue("@ChallengeId", challengeId);
                        await connection.OpenAsync();
                        
                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                prizes.Add(new ChallengePrize
                                {
                                    PrizeId = reader.GetInt32(reader.GetOrdinal("prize_id")),
                                    ChallengeId = reader.GetInt32(reader.GetOrdinal("challenge_id")),
                                    PrizeTypeId = reader.GetInt32(reader.GetOrdinal("prize_type_id")),
                                    Tier = reader.GetInt32(reader.GetOrdinal("tier")),
                                    TierName = reader.IsDBNull(reader.GetOrdinal("tier_name")) ? null : reader.GetString(reader.GetOrdinal("tier_name")),
                                    Description = reader.GetString(reader.GetOrdinal("description")),
                                    CashAmount = reader.IsDBNull(reader.GetOrdinal("cash_amount")) ? null : reader.GetDecimal(reader.GetOrdinal("cash_amount")),
                                    VoucherDiscountPercent = reader.IsDBNull(reader.GetOrdinal("voucher_discount_percent")) ? null : reader.GetDecimal(reader.GetOrdinal("voucher_discount_percent")),
                                    VoucherDiscountFixed = reader.IsDBNull(reader.GetOrdinal("voucher_discount_fixed")) ? null : reader.GetDecimal(reader.GetOrdinal("voucher_discount_fixed")),
                                    VoucherMinimumPurchase = reader.IsDBNull(reader.GetOrdinal("voucher_minimum_purchase")) ? null : reader.GetDecimal(reader.GetOrdinal("voucher_minimum_purchase")),
                                    VoucherType = reader.IsDBNull(reader.GetOrdinal("voucher_type")) ? null : reader.GetString(reader.GetOrdinal("voucher_type")),
                                    RewardName = reader.IsDBNull(reader.GetOrdinal("reward_name")) ? null : reader.GetString(reader.GetOrdinal("reward_name")),
                                    RewardValue = reader.IsDBNull(reader.GetOrdinal("reward_value")) ? null : reader.GetDecimal(reader.GetOrdinal("reward_value")),
                                    Quantity = reader.GetInt32(reader.GetOrdinal("quantity")),
                                    IsActive = reader.GetBoolean(reader.GetOrdinal("is_active"))
                                });
                            }
                        }
                    }
                }
                
                return Json(new { success = true, prizes = prizes });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, error = ex.Message });
            }
        }

        // POST: Create challenge prize
        [HttpPost]
        public async Task<IActionResult> CreatePrize([FromBody] CreatePrizeRequest request)
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    using (var command = new SqlCommand("sp_CreateChallengePrize", connection))
                    {
                        command.CommandType = CommandType.StoredProcedure;
                        command.Parameters.AddWithValue("@ChallengeId", request.ChallengeId);
                        command.Parameters.AddWithValue("@PrizeTypeId", request.PrizeTypeId);
                        command.Parameters.AddWithValue("@Tier", request.Tier);
                        command.Parameters.AddWithValue("@TierName", request.TierName ?? (object)DBNull.Value);
                        command.Parameters.AddWithValue("@Description", request.Description);
                        command.Parameters.AddWithValue("@CashAmount", request.CashAmount ?? (object)DBNull.Value);
                        command.Parameters.AddWithValue("@VoucherDiscountPercent", request.VoucherDiscountPercent ?? (object)DBNull.Value);
                        command.Parameters.AddWithValue("@VoucherDiscountFixed", request.VoucherDiscountFixed ?? (object)DBNull.Value);
                        command.Parameters.AddWithValue("@VoucherMinimumPurchase", request.VoucherMinimumPurchase ?? (object)DBNull.Value);
                        command.Parameters.AddWithValue("@VoucherType", request.VoucherType ?? (object)DBNull.Value);
                        command.Parameters.AddWithValue("@RewardName", request.RewardName ?? (object)DBNull.Value);
                        command.Parameters.AddWithValue("@RewardValue", request.RewardValue ?? (object)DBNull.Value);
                        command.Parameters.AddWithValue("@Quantity", request.Quantity);
                        
                        await connection.OpenAsync();
                        var prizeId = Convert.ToInt32(await command.ExecuteScalarAsync());
                        
                        return Json(new { success = true, message = "Prize created successfully", prizeId = prizeId });
                    }
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // POST: Auto-assign prizes when challenge ends
        [HttpPost]
        public async Task<IActionResult> AutoAssignPrizes(int challengeId)
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    using (var cmd = new SqlCommand("sp_AutoAssignChallengePrizes", connection))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@ChallengeId", challengeId);
                        
                        await connection.OpenAsync();
                        await cmd.ExecuteNonQueryAsync();
                        
                        return Json(new { success = true, message = "Prizes assigned successfully" });
                    }
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // GET: Get participant activities
        [HttpGet]
        public async Task<IActionResult> GetParticipantActivities(int participantId)
        {
            try
            {
                var activities = new List<ActivityLogViewModel>();
                
                using (var connection = new SqlConnection(_connectionString))
                {
                    using (var command = new SqlCommand("sp_GetParticipantActivities", connection))
                    {
                        command.CommandType = CommandType.StoredProcedure;
                        command.Parameters.AddWithValue("@ParticipantId", participantId);
                        
                        await connection.OpenAsync();
                        
                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                var activity = new ActivityLogViewModel
                                {
                                    ActivityId = reader.GetInt32(reader.GetOrdinal("activity_id")),
                                    ParticipantId = reader.GetInt32(reader.GetOrdinal("participant_id")),
                                    ChallengeId = reader.GetInt32(reader.GetOrdinal("challenge_id")),
                                    ActivityDate = reader.GetDateTime(reader.GetOrdinal("activity_date")),
                                    DistanceKm = reader.GetDecimal(reader.GetOrdinal("distance_km")),
                                    DurationSeconds = reader.GetInt32(reader.GetOrdinal("duration_seconds")),
                                    AveragePace = reader.GetDecimal(reader.GetOrdinal("average_pace")),
                                    ActivityType = reader.GetString(reader.GetOrdinal("activity_type")),
                                    IsVerified = reader.GetBoolean(reader.GetOrdinal("is_verified")),
                                    VerifiedByName = reader.IsDBNull(reader.GetOrdinal("verified_by_name")) ? null : reader.GetString(reader.GetOrdinal("verified_by_name")),
                                    VerifiedAt = reader.IsDBNull(reader.GetOrdinal("verified_at")) ? null : reader.GetDateTime(reader.GetOrdinal("verified_at")),
                                    Notes = reader.IsDBNull(reader.GetOrdinal("notes")) ? null : reader.GetString(reader.GetOrdinal("notes")),
                                    CreatedAt = reader.GetDateTime(reader.GetOrdinal("created_at")),
                                    ChallengeTitle = reader.IsDBNull(reader.GetOrdinal("challenge_title")) ? null : reader.GetString(reader.GetOrdinal("challenge_title"))
                                };
                                
                                // Handle image proof if exists
                                if (!reader.IsDBNull(reader.GetOrdinal("imageproof")))
                                {
                                    byte[] imageData = (byte[])reader["imageproof"];
                                    if (imageData != null && imageData.Length > 0)
                                    {
                                        activity.ImageProof = imageData;
                                        activity.ImageProofBase64 = Convert.ToBase64String(imageData);
                                    }
                                }
                                
                                activities.Add(activity);
                            }
                        }
                    }
                }
                
                return Json(new { success = true, activities = activities });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, error = ex.Message });
            }
        }

        // POST: Create new challenge
        [HttpPost]
        public async Task<IActionResult> CreateChallenge([FromBody] CreateChallengeRequest request)
        {
            try
            {
                var staffId = HttpContext.Session.GetInt32("StaffId") ?? 0;
                var adminName = HttpContext.Session.GetString("Username") ?? "System";
                
                byte[] bannerImageBytes = null;
                
                // ONLY process if there's actual image data
                if (!string.IsNullOrEmpty(request.BannerBase64) && 
                    request.BannerBase64 != "#" && 
                    request.BannerBase64 != "null" &&
                    request.BannerBase64.Length > 100)
                {
                    try
                    {
                        string base64Data = request.BannerBase64;
                        
                        if (base64Data.Contains(","))
                        {
                            base64Data = base64Data.Substring(base64Data.IndexOf(",") + 1);
                        }
                        
                        base64Data = base64Data.Trim().Replace(" ", "").Replace("\n", "").Replace("\r", "");
                        
                        bannerImageBytes = Convert.FromBase64String(base64Data);
                        Console.WriteLine($"Image converted: {bannerImageBytes.Length} bytes");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Image conversion error: {ex.Message}");
                        bannerImageBytes = null;
                    }
                }
                
                int? newChallengeId = null;
                
                using (var connection = new SqlConnection(_connectionString))
                {
                    using (var command = new SqlCommand("sp_CreateChallenge", connection))
                    {
                        command.CommandType = CommandType.StoredProcedure;
                        
                        command.Parameters.Add("@Title", SqlDbType.NVarChar, 255).Value = request.Title;
                        command.Parameters.Add("@Description", SqlDbType.NVarChar).Value = request.Description ?? (object)DBNull.Value;
                        command.Parameters.Add("@Rules", SqlDbType.NVarChar).Value = request.Rules ?? (object)DBNull.Value;
                        command.Parameters.Add("@Prizes", SqlDbType.NVarChar).Value = request.Prizes ?? (object)DBNull.Value;
                        command.Parameters.Add("@GoalKm", SqlDbType.Decimal).Value = request.GoalKm;
                        command.Parameters.Add("@ActivityType", SqlDbType.NVarChar, 50).Value = request.ActivityType;
                        command.Parameters.Add("@StartDate", SqlDbType.DateTime).Value = request.StartDate;
                        command.Parameters.Add("@EndDate", SqlDbType.DateTime).Value = request.EndDate;
                        
                        var bannerImageParam = new SqlParameter("@BannerImage", SqlDbType.VarBinary, -1);
                        if (bannerImageBytes != null && bannerImageBytes.Length > 0)
                        {
                            bannerImageParam.Value = bannerImageBytes;
                        }
                        else
                        {
                            bannerImageParam.Value = DBNull.Value;
                        }
                        command.Parameters.Add(bannerImageParam);
                        
                        command.Parameters.Add("@BannerImageName", SqlDbType.NVarChar, 255).Value = request.BannerImageName ?? (object)DBNull.Value;
                        command.Parameters.Add("@BannerImageContentType", SqlDbType.NVarChar, 100).Value = request.BannerImageContentType ?? (object)DBNull.Value;
                        command.Parameters.Add("@CreatedBy", SqlDbType.Int).Value = staffId;
                        
                        await connection.OpenAsync();
                        
                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            if (await reader.ReadAsync())
                            {
                                var status = reader["Status"].ToString();
                                var message = reader["Message"].ToString();
                                newChallengeId = reader["ChallengeId"] != DBNull.Value ? Convert.ToInt32(reader["ChallengeId"]) : (int?)null;
                                
                                if (status != "Success")
                                {
                                    return Json(new { success = false, message = message });
                                }
                            }
                        }
                    }
                }
                
                // Create prizes if challenge was created successfully and there are prizes
                if (newChallengeId.HasValue && request.PrizesData != null && request.PrizesData.Any())
                {
                    using (var connection = new SqlConnection(_connectionString))
                    {
                        await connection.OpenAsync();
                        
                        foreach (var prize in request.PrizesData)
                        {
                            using (var cmd = new SqlCommand("sp_CreateChallengePrize", connection))
                            {
                                cmd.CommandType = CommandType.StoredProcedure;
                                cmd.Parameters.AddWithValue("@ChallengeId", newChallengeId.Value);
                                cmd.Parameters.AddWithValue("@PrizeTypeId", prize.PrizeTypeId);
                                cmd.Parameters.AddWithValue("@Tier", prize.Tier);
                                cmd.Parameters.AddWithValue("@TierName", prize.TierName ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@Description", prize.Description);
                                cmd.Parameters.AddWithValue("@CashAmount", prize.CashAmount ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@VoucherDiscountPercent", prize.VoucherDiscountPercent ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@VoucherDiscountFixed", prize.VoucherDiscountFixed ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@VoucherMinimumPurchase", prize.VoucherMinimumPurchase ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@VoucherType", prize.VoucherType ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@RewardName", prize.RewardName ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@RewardValue", prize.RewardValue ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@Quantity", prize.Quantity);
                                
                                await cmd.ExecuteNonQueryAsync();
                            }
                        }
                    }
                    
                    await LogAdminAction(staffId, adminName, "Create Challenge",
                        request.Title, "Success", $"Created with {request.PrizesData.Count} prize tiers");
                }
                else
                {
                    await LogAdminAction(staffId, adminName, "Create Challenge",
                        request.Title, "Success");
                }
                
                return Json(new { success = true, message = "Challenge created successfully", challengeId = newChallengeId });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // POST: Update challenge
        [HttpPost]
        public async Task<IActionResult> UpdateChallenge([FromBody] UpdateChallengeRequest request)
        {
            try
            {
                var staffId = HttpContext.Session.GetInt32("StaffId") ?? 0;
                var adminName = HttpContext.Session.GetString("Username") ?? "System";
                
                byte[] bannerImageBytes = null;
                bool hasNewImage = false;
                
                // Store original status before update to check if it's changing to Completed
                string originalStatus = null;
                using (var connection = new SqlConnection(_connectionString))
                {
                    var getStatusCmd = new SqlCommand("SELECT status FROM challenges WHERE challenge_id = @ChallengeId", connection);
                    getStatusCmd.Parameters.AddWithValue("@ChallengeId", request.ChallengeId);
                    await connection.OpenAsync();
                    originalStatus = (await getStatusCmd.ExecuteScalarAsync())?.ToString();
                }
                
                // Only process if there's a NEW image
                if (!string.IsNullOrEmpty(request.BannerBase64) && 
                    request.BannerBase64 != "#" && 
                    request.BannerBase64 != "null" &&
                    request.BannerBase64.StartsWith("data:"))
                {
                    try
                    {
                        string base64Data = request.BannerBase64;
                        
                        if (base64Data.Contains(","))
                        {
                            base64Data = base64Data.Substring(base64Data.IndexOf(",") + 1);
                        }
                        
                        base64Data = base64Data.Trim().Replace(" ", "").Replace("\n", "").Replace("\r", "");
                        
                        if (!string.IsNullOrEmpty(base64Data))
                        {
                            bannerImageBytes = Convert.FromBase64String(base64Data);
                            hasNewImage = true;
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Image conversion error: {ex.Message}");
                        bannerImageBytes = null;
                        hasNewImage = false;
                    }
                }
                
                bool updateSuccess = false;
                
                using (var connection = new SqlConnection(_connectionString))
                {
                    using (var command = new SqlCommand("sp_UpdateChallenge", connection))
                    {
                        command.CommandType = CommandType.StoredProcedure;
                        command.Parameters.AddWithValue("@ChallengeId", request.ChallengeId);
                        command.Parameters.AddWithValue("@Title", request.Title);
                        command.Parameters.AddWithValue("@Description", request.Description ?? (object)DBNull.Value);
                        command.Parameters.AddWithValue("@Rules", request.Rules ?? (object)DBNull.Value);
                        command.Parameters.AddWithValue("@Prizes", request.Prizes ?? (object)DBNull.Value);
                        command.Parameters.AddWithValue("@GoalKm", request.GoalKm);
                        command.Parameters.AddWithValue("@ActivityType", request.ActivityType);
                        command.Parameters.AddWithValue("@StartDate", request.StartDate);
                        command.Parameters.AddWithValue("@EndDate", request.EndDate);
                        command.Parameters.AddWithValue("@Status", request.Status ?? (object)DBNull.Value);
                        
                        if (hasNewImage && bannerImageBytes != null)
                        {
                            command.Parameters.AddWithValue("@BannerImage", bannerImageBytes);
                            command.Parameters.AddWithValue("@BannerImageName", request.BannerImageName ?? (object)DBNull.Value);
                            command.Parameters.AddWithValue("@BannerImageContentType", request.BannerImageContentType ?? (object)DBNull.Value);
                        }
                        else
                        {
                            command.Parameters.AddWithValue("@BannerImage", DBNull.Value);
                            command.Parameters.AddWithValue("@BannerImageName", DBNull.Value);
                            command.Parameters.AddWithValue("@BannerImageContentType", DBNull.Value);
                        }
                        
                        command.Parameters.AddWithValue("@UpdatedBy", staffId);
                        
                        await connection.OpenAsync();
                        
                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            if (await reader.ReadAsync())
                            {
                                var status = reader["Status"].ToString();
                                var message = reader["Message"].ToString();
                                updateSuccess = status == "Success";
                                
                                if (!updateSuccess)
                                {
                                    return Json(new { success = false, message = message });
                                }
                            }
                        }
                    }
                }
                
                // Update prizes if provided
                if (updateSuccess && request.PrizesData != null && request.PrizesData.Any())
                {
                    using (var connection = new SqlConnection(_connectionString))
                    {
                        await connection.OpenAsync();
                        
                        // First, delete existing prizes for this challenge
                        using (var deleteCmd = new SqlCommand("DELETE FROM challenge_prizes WHERE challenge_id = @ChallengeId", connection))
                        {
                            deleteCmd.Parameters.AddWithValue("@ChallengeId", request.ChallengeId);
                            await deleteCmd.ExecuteNonQueryAsync();
                        }
                        
                        // Then insert new prizes
                        foreach (var prize in request.PrizesData)
                        {
                            using (var cmd = new SqlCommand("sp_CreateChallengePrize", connection))
                            {
                                cmd.CommandType = CommandType.StoredProcedure;
                                cmd.Parameters.AddWithValue("@ChallengeId", request.ChallengeId);
                                cmd.Parameters.AddWithValue("@PrizeTypeId", prize.PrizeTypeId);
                                cmd.Parameters.AddWithValue("@Tier", prize.Tier);
                                cmd.Parameters.AddWithValue("@TierName", prize.TierName ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@Description", prize.Description);
                                cmd.Parameters.AddWithValue("@CashAmount", prize.CashAmount ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@VoucherDiscountPercent", prize.VoucherDiscountPercent ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@VoucherDiscountFixed", prize.VoucherDiscountFixed ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@VoucherMinimumPurchase", prize.VoucherMinimumPurchase ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@VoucherType", prize.VoucherType ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@RewardName", prize.RewardName ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@RewardValue", prize.RewardValue ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@Quantity", prize.Quantity);
                                
                                await cmd.ExecuteNonQueryAsync();
                            }
                        }
                    }
                    
                    await LogAdminAction(staffId, adminName, "Update Challenge",
                        request.Title, "Success", $"Updated with {request.PrizesData.Count} prize tiers");
                }
                else
                {
                    await LogAdminAction(staffId, adminName, "Update Challenge",
                        request.Title, "Success");
                }
                
                // Assign prizes when challenge status changes to "Completed"
                if (updateSuccess && request.Status == "Completed" && originalStatus != "Completed")
                {
                    try
                    {
                        using (var connection = new SqlConnection(_connectionString))
                        {
                            await connection.OpenAsync();
                            
                            // First, check if there are any prizes for this challenge
                            int prizeCount = 0;
                            using (var countCmd = new SqlCommand("SELECT COUNT(*) FROM challenge_prizes WHERE challenge_id = @ChallengeId AND is_active = 1", connection))
                            {
                                countCmd.Parameters.AddWithValue("@ChallengeId", request.ChallengeId);
                                prizeCount = (int)await countCmd.ExecuteScalarAsync();
                            }
                            
                            if (prizeCount > 0)
                            {
                                // Update ranks first
                                var updateRankSql = @"
                                    WITH RankedParticipants AS (
                                        SELECT 
                                            participant_id,
                                            ROW_NUMBER() OVER (ORDER BY total_distance_km DESC) as rank
                                        FROM challenge_participants
                                        WHERE challenge_id = @ChallengeId
                                    )
                                    UPDATE cp
                                    SET rank = rp.rank
                                    FROM challenge_participants cp
                                    INNER JOIN RankedParticipants rp ON cp.participant_id = rp.participant_id
                                    WHERE cp.challenge_id = @ChallengeId";
                                
                                using (var rankCmd = new SqlCommand(updateRankSql, connection))
                                {
                                    rankCmd.Parameters.AddWithValue("@ChallengeId", request.ChallengeId);
                                    await rankCmd.ExecuteNonQueryAsync();
                                }
                                
                                // Call the stored procedure to assign prizes to ranked participants
                                using (var assignCmd = new SqlCommand("sp_AssignPrizesToRankedParticipants", connection))
                                {
                                    assignCmd.CommandType = CommandType.StoredProcedure;
                                    assignCmd.Parameters.AddWithValue("@ChallengeId", request.ChallengeId);
                                    await assignCmd.ExecuteNonQueryAsync();
                                }
                                
                                await LogAdminAction(staffId, adminName, "Assign Prizes",
                                    $"Challenge: {request.Title}", "Success", $"Assigned {prizeCount} prize tiers to participants");
                                
                                Console.WriteLine($"Prizes assigned successfully for challenge {request.ChallengeId}");
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error assigning prizes: {ex.Message}");
                        await LogAdminAction(staffId, adminName, "Assign Prizes Failed",
                            $"Challenge: {request.Title}", "Error", ex.Message);
                    }
                }
                
                return Json(new { success = true, message = "Challenge updated successfully" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // POST: Claim prize
        [HttpPost]
        public async Task<IActionResult> ClaimPrize([FromBody] ClaimPrizeRequest request)
        {
            try
            {
                var userId = HttpContext.Session.GetInt32("UserId") ?? 0;
                
                if (userId == 0)
                {
                    // Try to get from staff session
                    userId = HttpContext.Session.GetInt32("StaffId") ?? 0;
                }
                
                using (var connection = new SqlConnection(_connectionString))
                {
                    using (var command = new SqlCommand("sp_ClaimPrizeWithCode", connection))
                    {
                        command.CommandType = CommandType.StoredProcedure;
                        command.Parameters.AddWithValue("@ClaimCode", request.ClaimCode);
                        command.Parameters.AddWithValue("@UserId", userId);
                        command.Parameters.AddWithValue("@ClaimDetails", request.ClaimDetails ?? (object)DBNull.Value);
                        
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
                
                return Json(new { success = false, message = "Failed to process claim" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // POST: Verify and claim prize with code (alternative method)
        [HttpPost]
        public async Task<IActionResult> VerifyAndClaimPrize([FromBody] VerifyAndClaimRequest request)
        {
            try
            {
                var userId = HttpContext.Session.GetInt32("UserId") ?? 0;
                
                if (userId == 0)
                {
                    userId = HttpContext.Session.GetInt32("StaffId") ?? 0;
                }
                
                using (var connection = new SqlConnection(_connectionString))
                {
                    using (var command = new SqlCommand("sp_ClaimPrizeWithCode", connection))
                    {
                        command.CommandType = CommandType.StoredProcedure;
                        command.Parameters.AddWithValue("@ClaimCode", request.ClaimCode);
                        command.Parameters.AddWithValue("@UserId", userId);
                        command.Parameters.AddWithValue("@ClaimDetails", DBNull.Value);
                        
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
                
                return Json(new { success = false, message = "Invalid claim code" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // GET: Get user prizes for a specific challenge
        [HttpGet]
        public async Task<IActionResult> GetUserPrizes(int challengeId, int userId)
        {
            try
            {
                var prizes = new List<object>();
                bool isAdminView = false;
                
                // Check if the current user is an admin
                var currentStaffId = HttpContext.Session.GetInt32("StaffId");
                var currentUserRole = HttpContext.Session.GetString("UserType");
                
                if (currentStaffId.HasValue && currentStaffId.Value > 0)
                {
                    isAdminView = true;
                }
                
                using (var connection = new SqlConnection(_connectionString))
                {
                    using (var command = new SqlCommand("sp_GetChallengePrizes", connection))
                    {
                        command.CommandType = CommandType.StoredProcedure;
                        command.Parameters.AddWithValue("@ChallengeId", challengeId);
                        command.Parameters.AddWithValue("@UserId", userId);
                        command.Parameters.AddWithValue("@IsAdminView", isAdminView ? 1 : 0);
                        
                        await connection.OpenAsync();
                        
                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                if (isAdminView)
                                {
                                    // Admin view - includes all prizes
                                    var prize = new
                                    {
                                        // Prize configuration
                                        prizeId = reader.GetInt32(reader.GetOrdinal("prize_id")),
                                        tier = reader.GetInt32(reader.GetOrdinal("tier")),
                                        tierName = reader.IsDBNull(reader.GetOrdinal("tier_name")) ? null : reader.GetString(reader.GetOrdinal("tier_name")),
                                        description = reader.GetString(reader.GetOrdinal("description")),
                                        prizeType = reader.GetString(reader.GetOrdinal("prize_type")),
                                        prizeTypeCode = reader.GetString(reader.GetOrdinal("type_code")),
                                        quantity = reader.GetInt32(reader.GetOrdinal("quantity")),
                                        isActive = reader.GetBoolean(reader.GetOrdinal("is_active")),
                                        
                                        // Prize details
                                        cashAmount = reader.IsDBNull(reader.GetOrdinal("cash_amount")) ? 0 : reader.GetDecimal(reader.GetOrdinal("cash_amount")),
                                        voucherDiscountPercent = reader.IsDBNull(reader.GetOrdinal("voucher_discount_percent")) ? (decimal?)null : reader.GetDecimal(reader.GetOrdinal("voucher_discount_percent")),
                                        voucherDiscountFixed = reader.IsDBNull(reader.GetOrdinal("voucher_discount_fixed")) ? (decimal?)null : reader.GetDecimal(reader.GetOrdinal("voucher_discount_fixed")),
                                        voucherMinimumPurchase = reader.IsDBNull(reader.GetOrdinal("voucher_minimum_purchase")) ? (decimal?)null : reader.GetDecimal(reader.GetOrdinal("voucher_minimum_purchase")),
                                        voucherType = reader.IsDBNull(reader.GetOrdinal("voucher_type")) ? null : reader.GetString(reader.GetOrdinal("voucher_type")),
                                        rewardName = reader.IsDBNull(reader.GetOrdinal("reward_name")) ? null : reader.GetString(reader.GetOrdinal("reward_name")),
                                        rewardValue = reader.IsDBNull(reader.GetOrdinal("reward_value")) ? 0 : reader.GetDecimal(reader.GetOrdinal("reward_value")),
                                        
                                        // Assignment info
                                        assignmentStatus = reader.GetString(reader.GetOrdinal("assignment_status")),
                                        winnerId = reader.IsDBNull(reader.GetOrdinal("winner_id")) ? (int?)null : reader.GetInt32(reader.GetOrdinal("winner_id")),
                                        claimStatus = reader.IsDBNull(reader.GetOrdinal("claim_status")) ? null : reader.GetString(reader.GetOrdinal("claim_status")),
                                        claimCode = reader.IsDBNull(reader.GetOrdinal("claim_code")) ? null : reader.GetString(reader.GetOrdinal("claim_code")),
                                        claimDeadline = reader.IsDBNull(reader.GetOrdinal("claim_deadline")) ? (DateTime?)null : reader.GetDateTime(reader.GetOrdinal("claim_deadline")),
                                        claimDate = reader.IsDBNull(reader.GetOrdinal("claim_date")) ? (DateTime?)null : reader.GetDateTime(reader.GetOrdinal("claim_date")),
                                        athleteName = reader.IsDBNull(reader.GetOrdinal("athlete_name")) ? "Not yet assigned" : reader.GetString(reader.GetOrdinal("athlete_name")),
                                        athleteUsername = reader.IsDBNull(reader.GetOrdinal("athlete_username")) ? null : reader.GetString(reader.GetOrdinal("athlete_username")),
                                        athleteTotalDistance = reader.IsDBNull(reader.GetOrdinal("athlete_total_distance")) ? 0 : reader.GetDecimal(reader.GetOrdinal("athlete_total_distance")),
                                        athleteRank = reader.IsDBNull(reader.GetOrdinal("athlete_rank")) ? (int?)null : reader.GetInt32(reader.GetOrdinal("athlete_rank"))
                                    };
                                    prizes.Add(prize);
                                }
                                else
                                {
                                    // User view - only their assigned prizes
                                    var prize = new
                                    {
                                        // Prize winner info
                                        winnerId = reader.GetInt32(reader.GetOrdinal("winner_id")),
                                        rankPosition = reader.GetInt32(reader.GetOrdinal("rank_position")),
                                        claimStatus = reader.GetString(reader.GetOrdinal("claim_status")),
                                        claimCode = reader.GetString(reader.GetOrdinal("claim_code")),
                                        claimDeadline = reader.GetDateTime(reader.GetOrdinal("claim_deadline")),
                                        claimDate = reader.IsDBNull(reader.GetOrdinal("claim_date")) ? (DateTime?)null : reader.GetDateTime(reader.GetOrdinal("claim_date")),
                                        claimNotes = reader.IsDBNull(reader.GetOrdinal("claim_notes")) ? null : reader.GetString(reader.GetOrdinal("claim_notes")),
                                        
                                        // Prize details
                                        cashAmount = reader.IsDBNull(reader.GetOrdinal("cash_amount")) ? 0 : reader.GetDecimal(reader.GetOrdinal("cash_amount")),
                                        voucherDiscountPercent = reader.IsDBNull(reader.GetOrdinal("voucher_discount_percent")) ? (decimal?)null : reader.GetDecimal(reader.GetOrdinal("voucher_discount_percent")),
                                        voucherDiscountFixed = reader.IsDBNull(reader.GetOrdinal("voucher_discount_fixed")) ? (decimal?)null : reader.GetDecimal(reader.GetOrdinal("voucher_discount_fixed")),
                                        voucherMinimumPurchase = reader.IsDBNull(reader.GetOrdinal("voucher_minimum_purchase")) ? (decimal?)null : reader.GetDecimal(reader.GetOrdinal("voucher_minimum_purchase")),
                                        voucherType = reader.IsDBNull(reader.GetOrdinal("voucher_type")) ? null : reader.GetString(reader.GetOrdinal("voucher_type")),
                                        rewardName = reader.IsDBNull(reader.GetOrdinal("reward_name")) ? null : reader.GetString(reader.GetOrdinal("reward_name")),
                                        rewardValue = reader.IsDBNull(reader.GetOrdinal("reward_value")) ? 0 : reader.GetDecimal(reader.GetOrdinal("reward_value")),
                                        tierName = reader.IsDBNull(reader.GetOrdinal("tier_name")) ? null : reader.GetString(reader.GetOrdinal("tier_name")),
                                        description = reader.GetString(reader.GetOrdinal("description")),
                                        prizeType = reader.GetString(reader.GetOrdinal("prize_type")),
                                        prizeTypeCode = reader.GetString(reader.GetOrdinal("type_code")),
                                        
                                        // Voucher specific
                                        voucherCode = reader.IsDBNull(reader.GetOrdinal("voucher_code")) ? null : reader.GetString(reader.GetOrdinal("voucher_code")),
                                        voucherExpiry = reader.IsDBNull(reader.GetOrdinal("voucher_expiry")) ? (DateTime?)null : reader.GetDateTime(reader.GetOrdinal("voucher_expiry")),
                                        voucherSentDate = reader.IsDBNull(reader.GetOrdinal("voucher_sent_date")) ? (DateTime?)null : reader.GetDateTime(reader.GetOrdinal("voucher_sent_date")),
                                        voucherUsed = reader.IsDBNull(reader.GetOrdinal("voucher_used")) ? false : reader.GetBoolean(reader.GetOrdinal("voucher_used")),
                                        
                                        // Physical reward specific
                                        shippingAddress = reader.IsDBNull(reader.GetOrdinal("shipping_address")) ? null : reader.GetString(reader.GetOrdinal("shipping_address")),
                                        trackingNumber = reader.IsDBNull(reader.GetOrdinal("tracking_number")) ? null : reader.GetString(reader.GetOrdinal("tracking_number")),
                                        shippedDate = reader.IsDBNull(reader.GetOrdinal("shipped_date")) ? (DateTime?)null : reader.GetDateTime(reader.GetOrdinal("shipped_date")),
                                        deliveryDate = reader.IsDBNull(reader.GetOrdinal("delivery_date")) ? (DateTime?)null : reader.GetDateTime(reader.GetOrdinal("delivery_date")),
                                        
                                        // Cash specific
                                        bankName = reader.IsDBNull(reader.GetOrdinal("bank_name")) ? null : reader.GetString(reader.GetOrdinal("bank_name")),
                                        accountNumber = reader.IsDBNull(reader.GetOrdinal("account_number")) ? null : reader.GetString(reader.GetOrdinal("account_number")),
                                        accountName = reader.IsDBNull(reader.GetOrdinal("account_name")) ? null : reader.GetString(reader.GetOrdinal("account_name")),
                                        transactionReference = reader.IsDBNull(reader.GetOrdinal("transaction_reference")) ? null : reader.GetString(reader.GetOrdinal("transaction_reference")),
                                        transferDate = reader.IsDBNull(reader.GetOrdinal("transfer_date")) ? (DateTime?)null : reader.GetDateTime(reader.GetOrdinal("transfer_date")),
                                        
                                        // Digital reward specific
                                        digitalCode = reader.IsDBNull(reader.GetOrdinal("digital_code")) ? null : reader.GetString(reader.GetOrdinal("digital_code")),
                                        digitalCodeSentDate = reader.IsDBNull(reader.GetOrdinal("digital_code_sent_date")) ? (DateTime?)null : reader.GetDateTime(reader.GetOrdinal("digital_code_sent_date")),
                                        digitalCodeUsed = reader.IsDBNull(reader.GetOrdinal("digital_code_used")) ? false : reader.GetBoolean(reader.GetOrdinal("digital_code_used")),
                                        
                                        // Admin processing
                                        processedBy = reader.IsDBNull(reader.GetOrdinal("processed_by")) ? (int?)null : reader.GetInt32(reader.GetOrdinal("processed_by")),
                                        processedAt = reader.IsDBNull(reader.GetOrdinal("processed_at")) ? (DateTime?)null : reader.GetDateTime(reader.GetOrdinal("processed_at")),
                                        rejectionReason = reader.IsDBNull(reader.GetOrdinal("rejection_reason")) ? null : reader.GetString(reader.GetOrdinal("rejection_reason")),
                                        adminNotes = reader.IsDBNull(reader.GetOrdinal("admin_notes")) ? null : reader.GetString(reader.GetOrdinal("admin_notes")),
                                        
                                        // Participant stats
                                        totalDistanceKm = reader.GetDecimal(reader.GetOrdinal("total_distance_km")),
                                        totalActivities = reader.GetInt32(reader.GetOrdinal("total_activities")),
                                        totalTimeSeconds = reader.GetInt32(reader.GetOrdinal("total_time_seconds")),
                                        participantRank = reader.IsDBNull(reader.GetOrdinal("rank")) ? (int?)null : reader.GetInt32(reader.GetOrdinal("rank")),
                                        isCompleted = reader.GetBoolean(reader.GetOrdinal("is_completed")),
                                        completedAt = reader.IsDBNull(reader.GetOrdinal("completed_at")) ? (DateTime?)null : reader.GetDateTime(reader.GetOrdinal("completed_at")),
                                        hasPrizes = reader.GetBoolean(reader.GetOrdinal("has_prizes")),
                                        isParticipant = reader.GetBoolean(reader.GetOrdinal("is_participant"))
                                    };
                                    prizes.Add(prize);
                                }
                            }
                        }
                    }
                }
                
                // If admin view and no prizes found, still return empty list
                if (isAdminView && prizes.Count == 0)
                {
                    return Json(new { success = true, prizes = new List<object>(), isAdminView = true, message = "No prizes configured for this challenge" });
                }
                
                // If user view and no prizes found
                if (!isAdminView && prizes.Count == 0)
                {
                    return Json(new { success = true, prizes = new List<object>(), isAdminView = false, message = "No prizes assigned to you for this challenge" });
                }
                
                return Json(new { success = true, prizes = prizes, isAdminView = isAdminView });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in GetUserPrizes: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                return Json(new { success = false, error = ex.Message, prizes = new List<object>() });
            }
        }

        // POST: Verify activity
        [HttpPost]
        public async Task<IActionResult> VerifyActivity([FromBody] VerifyActivityRequest request)
        {
            try
            {
                var staffId = HttpContext.Session.GetInt32("StaffId") ?? 0;
                var adminName = HttpContext.Session.GetString("Username") ?? "System";
                
                using (var connection = new SqlConnection(_connectionString))
                {
                    using (var command = new SqlCommand("sp_VerifyChallengeActivity", connection))
                    {
                        command.CommandType = CommandType.StoredProcedure;
                        command.Parameters.AddWithValue("@ActivityId", request.ActivityId);
                        command.Parameters.AddWithValue("@VerifiedBy", staffId);
                        
                        await connection.OpenAsync();
                        
                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            if (await reader.ReadAsync())
                            {
                                var status = reader["Status"].ToString();
                                var message = reader["Message"].ToString();
                                
                                if (status == "Success")
                                {
                                    await LogAdminAction(staffId, adminName, "Verify Activity",
                                        $"Activity ID: {request.ActivityId}", "Success");
                                }
                                
                                return Json(new { success = status == "Success", message = message });
                            }
                        }
                    }
                }
                
                return Json(new { success = false, message = "Failed to verify activity" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // GET: Get challenge statistics
        [HttpGet]
        public async Task<IActionResult> GetChallengeStatistics()
        {
            try
            {
                ChallengeStatistics stats = null;
                
                using (var connection = new SqlConnection(_connectionString))
                {
                    using (var command = new SqlCommand("sp_GetChallengeStatistics", connection))
                    {
                        command.CommandType = CommandType.StoredProcedure;
                        
                        await connection.OpenAsync();
                        
                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            if (await reader.ReadAsync())
                            {
                                stats = new ChallengeStatistics
                                {
                                    TotalAthletes = reader.GetInt32(reader.GetOrdinal("total_athletes")),
                                    ActiveChallenges = reader.GetInt32(reader.GetOrdinal("active_challenges")),
                                    AvgDistance = reader.GetDecimal(reader.GetOrdinal("avg_distance")),
                                    TotalTimeHours = reader.GetDecimal(reader.GetOrdinal("total_time_hours"))
                                };
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

        // GET: Get user's challenges
        [HttpGet]
        public async Task<IActionResult> GetUserChallenges()
        {
            try
            {
                var userId = HttpContext.Session.GetInt32("UserId") ?? 0;
                
                if (userId == 0)
                {
                    userId = HttpContext.Session.GetInt32("StaffId") ?? 0;
                }
                
                var challenges = new List<object>();
                
                using (var connection = new SqlConnection(_connectionString))
                {
                    using (var command = new SqlCommand("sp_GetUserChallenges", connection))
                    {
                        command.CommandType = CommandType.StoredProcedure;
                        command.Parameters.AddWithValue("@UserId", userId);
                        
                        await connection.OpenAsync();
                        
                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                challenges.Add(new
                                {
                                    challengeId = reader.GetInt32(reader.GetOrdinal("challenge_id")),
                                    title = reader.GetString(reader.GetOrdinal("title")),
                                    description = reader.IsDBNull(reader.GetOrdinal("description")) ? null : reader.GetString(reader.GetOrdinal("description")),
                                    goalKm = reader.GetDecimal(reader.GetOrdinal("goal_km")),
                                    activityType = reader.GetString(reader.GetOrdinal("activity_type")),
                                    startDate = reader.GetDateTime(reader.GetOrdinal("start_date")),
                                    endDate = reader.GetDateTime(reader.GetOrdinal("end_date")),
                                    status = reader.GetString(reader.GetOrdinal("status")),
                                    totalDistanceKm = reader.GetDecimal(reader.GetOrdinal("total_distance_km")),
                                    totalActivities = reader.GetInt32(reader.GetOrdinal("total_activities")),
                                    isCompleted = reader.GetBoolean(reader.GetOrdinal("is_completed")),
                                    joinedAt = reader.GetDateTime(reader.GetOrdinal("joined_at")),
                                    rank = reader.IsDBNull(reader.GetOrdinal("rank")) ? (int?)null : reader.GetInt32(reader.GetOrdinal("rank")),
                                    progressPercent = reader.GetDecimal(reader.GetOrdinal("progress_percent"))
                                });
                            }
                        }
                    }
                }
                
                return Json(challenges);
            }
            catch (Exception ex)
            {
                return Json(new { error = ex.Message });
            }
        }

        #endregion

        // HelpCenter - Accessible by all admin roles
        // ═══════════════════════════════════════════════════════════════════
        // HELP CENTER — page load
        // ═══════════════════════════════════════════════════════════════════
        public async Task<IActionResult> HelpCenter()
        {
            var redirect = RedirectToLoginIfNotAuthenticated();
            if (redirect != null) return redirect;

            var faqs = new List<FaqItem>();
            var categories = new List<string>();
            var sessions = new List<QueueSession>();
            var agents = new List<AgentStatusViewModel>();

            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();

                // Get FAQs
                using (var cmd = new SqlCommand(@"
                    SELECT FaqID, Question, Answer, Category, UserType, DateAdded 
                    FROM FAQs 
                    WHERE Status = 'Active' 
                    ORDER BY DateAdded DESC", connection))
                {
                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            faqs.Add(new FaqItem
                            {
                                Id = reader.GetInt32(reader.GetOrdinal("FaqID")),
                                Question = reader.GetString(reader.GetOrdinal("Question")),
                                Answer = reader.GetString(reader.GetOrdinal("Answer")),
                                Category = reader.IsDBNull(reader.GetOrdinal("Category")) ? "General" : reader.GetString(reader.GetOrdinal("Category")),
                                UserType = reader.IsDBNull(reader.GetOrdinal("UserType")) ? "All" : reader.GetString(reader.GetOrdinal("UserType"))
                            });
                        }
                    }
                }

                // Get categories
                using (var cmd = new SqlCommand(@"
                    SELECT DISTINCT Category 
                    FROM FAQs 
                    WHERE Status = 'Active' AND Category IS NOT NULL AND Category != '' 
                    ORDER BY Category", connection))
                {
                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            categories.Add(reader.GetString(reader.GetOrdinal("Category")));
                        }
                    }
                }

                // Get waiting sessions
                using (var cmd = new SqlCommand(@"
                    SELECT Id, UserType, Category, Question, CreatedAt 
                    FROM SupportFAQs 
                    WHERE Status = 'Waiting' 
                    ORDER BY CreatedAt ASC", connection))
                {
                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        int position = 1;
                        while (await reader.ReadAsync())
                        {
                            var isSeller = (reader["UserType"]?.ToString() ?? "").Equals("Seller", StringComparison.OrdinalIgnoreCase);
                            var createdAt = reader.GetDateTime(reader.GetOrdinal("CreatedAt"));
                            
                            sessions.Add(new QueueSession
                            {
                                Id = reader.GetInt32(reader.GetOrdinal("Id")),
                                SessionNo = "NH-" + reader.GetInt32(reader.GetOrdinal("Id")).ToString("D5"),
                                CustomerName = (isSeller ? "Seller" : "Customer") + " #" + reader.GetInt32(reader.GetOrdinal("Id")),
                                Role = isSeller ? "Seller" : "Consumer",
                                Initials = isSeller ? "SE" : "CU",
                                AvatarColor = "#1a1a1a",
                                WaitSeconds = (int)(DateTime.Now - createdAt).TotalSeconds,
                                QueuePosition = position++,
                                Status = "waiting",
                                Category = reader.IsDBNull(reader.GetOrdinal("Category")) ? "General" : reader.GetString(reader.GetOrdinal("Category")),
                                Messages = new List<QueueMessage>()
                            });
                        }
                    }
                }
            }

            // Get agents (simplified for now)
            agents = await BuildAgentViewModels();

            var resolvedToday = await GetResolvedTodayCount();
            var activeCount = sessions.Count(s => s.Status == "waiting");
            var agentsOnline = agents.Count(a => a.Status == "online" || a.Status == "busy");
            var avgWaitTime = sessions.Any() 
                ? TimeSpan.FromSeconds(sessions.Average(s => s.WaitSeconds)).ToString(@"m\:ss") 
                : "0:00";

            var viewModel = new HelpCenterV2ViewModel
            {
                Stats = new QueueDashboardStatsViewModel
                {
                    InQueue = sessions.Count,
                    ActiveSessions = activeCount,
                    AvgWaitTime = avgWaitTime,
                    ResolvedToday = resolvedToday,
                    AgentsOnline = agentsOnline
                },
                Sessions = sessions,
                Agents = agents,
                Faqs = faqs,
                Categories = categories
            };

            ViewBag.UserRole = GetCurrentUserRole();
            return View(viewModel);
        }

        private async Task<List<AgentStatusViewModel>> GetAgentViewModels()
        {
            var agents = new List<AgentStatusViewModel>();
            
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                
                using (var cmd = new SqlCommand(@"
                    SELECT DISTINCT AgentName, AgentStatus, UserID 
                    FROM Agents 
                    WHERE AgentName IS NOT NULL", connection))
                {
                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            var name = reader.GetString(reader.GetOrdinal("AgentName"));
                            var rawStatus = reader.IsDBNull(reader.GetOrdinal("AgentStatus")) ? "available" : reader.GetString(reader.GetOrdinal("AgentStatus"));
                            var mappedStatus = rawStatus.ToLower() switch
                            {
                                "available" => "online",
                                "busy" => "busy",
                                "away" => "away",
                                "offline" => "offline",
                                _ => "online"
                            };
                            
                            var initials = string.Concat(name.Split(' ', StringSplitOptions.RemoveEmptyEntries)
                                .Take(2)
                                .Select(w => char.ToUpper(w[0]).ToString()));
                            
                            agents.Add(new AgentStatusViewModel
                            {
                                Name = name,
                                Initials = initials,
                                Status = mappedStatus,
                                ActiveSessions = 0,
                                MaxSessions = 3,
                                Slots = new List<AgentSlot>()
                            });
                        }
                    }
                }
            }
            
            return agents;
        }

        private async Task<int> GetResolvedTodayCount()
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                using (var cmd = new SqlCommand(@"
                    SELECT COUNT(*) 
                    FROM SupportFAQs 
                    WHERE Status = 'Resolved' AND CAST(CreatedAt AS DATE) = CAST(GETDATE() AS DATE)", connection))
                {
                    return (int)await cmd.ExecuteScalarAsync();
                }
            }
        }


        // ═══════════════════════════════════════════════════════════════════
        // GET QUEUE SESSIONS
        // ═══════════════════════════════════════════════════════════════════
        [HttpGet]
        public async Task<IActionResult> GetQueueSessions()
        {
            var sessions = new List<object>();
            
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                using (var cmd = new SqlCommand(@"
                    SELECT Id, UserType, Category, CreatedAt 
                    FROM SupportFAQs 
                    WHERE Status = 'Waiting' 
                    ORDER BY CreatedAt ASC", connection))
                {
                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        int position = 1;
                        while (await reader.ReadAsync())
                        {
                            var isSeller = (reader["UserType"]?.ToString() ?? "").Equals("Seller", StringComparison.OrdinalIgnoreCase);
                            var createdAt = reader.GetDateTime(reader.GetOrdinal("CreatedAt"));
                            
                            sessions.Add(new
                            {
                                id = reader.GetInt32(reader.GetOrdinal("Id")),
                                no = "NH-" + reader.GetInt32(reader.GetOrdinal("Id")).ToString("D5"),
                                name = (isSeller ? "Seller" : "Customer") + " #" + reader.GetInt32(reader.GetOrdinal("Id")),
                                role = isSeller ? "Seller" : "Consumer",
                                av = isSeller ? "SE" : "CU",
                                bg = "#1a1a1a",
                                waitSec = (int)(DateTime.Now - createdAt).TotalSeconds,
                                pos = position++,
                                status = "waiting",
                                cat = reader.IsDBNull(reader.GetOrdinal("Category")) ? "General" : reader.GetString(reader.GetOrdinal("Category")),
                                agent = "Unassigned"
                            });
                        }
                    }
                }
            }
            
            return Json(sessions);
        }

        // ── Shared helper — builds agent view models ─────────────────────
        private async Task<List<AgentStatusViewModel>> BuildAgentViewModels()
        {
            var agents = new List<AgentStatusViewModel>();
            
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                
                // Get active agents with their current conversations
                var activeAgents = new Dictionary<string, List<AgentSlot>>();
                
                using (var cmd = new SqlCommand(@"
                    SELECT AgentName, ConversationID, ClientName, Category, ChatSlot, ChatStatus
                    FROM Agents 
                    WHERE (ChatStatus = 'Active' OR ChatStatus = 'active')
                    AND ChatStatus != 'Resolved'
                    AND ConversationID IS NOT NULL", connection))
                {
                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            var agentName = reader.GetString(reader.GetOrdinal("AgentName"));
                            var slot = new AgentSlot
                            {
                                ConversationId = reader.IsDBNull(reader.GetOrdinal("ConversationID")) ? 0 : Convert.ToInt32(reader["ConversationID"]),
                                ClientName = reader.IsDBNull(reader.GetOrdinal("ClientName")) ? "" : reader.GetString(reader.GetOrdinal("ClientName")),
                                Category = reader.IsDBNull(reader.GetOrdinal("Category")) ? "General" : reader.GetString(reader.GetOrdinal("Category")),
                                SlotNumber = reader.IsDBNull(reader.GetOrdinal("ChatSlot")) ? 0 : Convert.ToInt32(reader["ChatSlot"])
                            };
                            
                            if (!activeAgents.ContainsKey(agentName))
                                activeAgents[agentName] = new List<AgentSlot>();
                            
                            activeAgents[agentName].Add(slot);
                        }
                    }
                }
                
                // Get all distinct agent names
                using (var cmd = new SqlCommand(@"
                    SELECT DISTINCT AgentName, AgentStatus 
                    FROM Agents 
                    WHERE AgentName IS NOT NULL", connection))
                {
                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            var name = reader.GetString(reader.GetOrdinal("AgentName"));
                            var rawStatus = reader.IsDBNull(reader.GetOrdinal("AgentStatus")) ? "available" : reader.GetString(reader.GetOrdinal("AgentStatus"));
                            var mappedStatus = rawStatus.ToLower() switch
                            {
                                "available" => "online",
                                "busy" => "busy",
                                "away" => "away",
                                "offline" => "offline",
                                _ => "online"
                            };
                            
                            var initials = string.Concat(name.Split(' ', StringSplitOptions.RemoveEmptyEntries)
                                .Take(2)
                                .Select(w => char.ToUpper(w[0]).ToString()));
                            
                            var slots = activeAgents.ContainsKey(name) ? activeAgents[name] : new List<AgentSlot>();
                            
                            agents.Add(new AgentStatusViewModel
                            {
                                Name = name,
                                Initials = initials,
                                Status = mappedStatus,
                                ActiveSessions = slots.Count,
                                MaxSessions = 3,
                                Slots = slots
                            });
                        }
                    }
                }
            }
            
            return agents;
        }

        // ═══════════════════════════════════════════════════════════════════
        // GET AGENT STATUS
        // ═══════════════════════════════════════════════════════════════════
        [HttpGet]
        public async Task<IActionResult> GetAgentStatus()
        {
            var agents = new List<object>();
            
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                using (var cmd = new SqlCommand(@"
                    SELECT DISTINCT AgentName, AgentStatus 
                    FROM Agents 
                    WHERE AgentName IS NOT NULL", connection))
                {
                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            var name = reader.GetString(reader.GetOrdinal("AgentName"));
                            var rawStatus = reader.IsDBNull(reader.GetOrdinal("AgentStatus")) ? "available" : reader.GetString(reader.GetOrdinal("AgentStatus"));
                            var mappedStatus = rawStatus.ToLower() switch
                            {
                                "available" => "online",
                                "busy" => "busy",
                                "away" => "away",
                                "offline" => "offline",
                                _ => "online"
                            };
                            
                            var initials = string.Concat(name.Split(' ', StringSplitOptions.RemoveEmptyEntries)
                                .Take(2)
                                .Select(w => char.ToUpper(w[0]).ToString()));
                            
                            agents.Add(new
                            {
                                name = name,
                                initials = initials,
                                status = mappedStatus,
                                sessions = 0,
                                max = 3,
                                slots = new List<object>()
                            });
                        }
                    }
                }
            }
            
            return Json(agents);
        }

        // ═══════════════════════════════════════════════════════════════════
        // GET AVAILABLE AGENTS
        // ═══════════════════════════════════════════════════════════════════
        [HttpGet]
        public async Task<IActionResult> GetAvailableAgents()
        {
            var agents = new List<object>();
            
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                using (var cmd = new SqlCommand(@"
                    SELECT DISTINCT AgentName, AgentStatus 
                    FROM Agents 
                    WHERE AgentName IS NOT NULL AND (AgentStatus = 'available' OR AgentStatus = 'online')", connection))
                {
                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            var name = reader.GetString(reader.GetOrdinal("AgentName"));
                            var rawStatus = reader.IsDBNull(reader.GetOrdinal("AgentStatus")) ? "available" : reader.GetString(reader.GetOrdinal("AgentStatus"));
                            
                            agents.Add(new
                            {
                                name = name,
                                activeSessions = 0,
                                maxSessions = 3,
                                status = rawStatus,
                                hasSlot = rawStatus != "offline"
                            });
                        }
                    }
                }
            }
            
            return Json(agents);
        }

        // ═══════════════════════════════════════════════════════════════════
        // QUEUE ASSIGN
        // ═══════════════════════════════════════════════════════════════════
        [HttpPost]
        public async Task<IActionResult> QueueAssign([FromBody] AssignSessionRequest model)
        {
            if (model == null || model.SessionId <= 0 || string.IsNullOrWhiteSpace(model.AgentName))
                return BadRequest(new { success = false, message = "Invalid request." });

            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                
                // Update session status to Active
                using (var cmd = new SqlCommand(@"
                    UPDATE SupportFAQs 
                    SET Status = 'Active', StartTime = GETDATE(), AgentId = @AgentId 
                    WHERE Id = @Id", connection))
                {
                    cmd.Parameters.AddWithValue("@AgentId", model.AgentName);
                    cmd.Parameters.AddWithValue("@Id", model.SessionId);
                    await cmd.ExecuteNonQueryAsync();
                }
                
                // Get session details
                string clientName = "";
                string category = "";
                string previewQ = "";
                
                using (var cmd = new SqlCommand(@"
                    SELECT UserType, Category, Question 
                    FROM SupportFAQs 
                    WHERE Id = @Id", connection))
                {
                    cmd.Parameters.AddWithValue("@Id", model.SessionId);
                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            var isSeller = (reader["UserType"]?.ToString() ?? "").Equals("Seller", StringComparison.OrdinalIgnoreCase);
                            clientName = (isSeller ? "Seller #" : "Customer #") + model.SessionId;
                            category = reader.IsDBNull(reader.GetOrdinal("Category")) ? "General" : reader.GetString(reader.GetOrdinal("Category"));
                            previewQ = reader.IsDBNull(reader.GetOrdinal("Question")) ? "" : reader.GetString(reader.GetOrdinal("Question"));
                        }
                    }
                }
                
                // Insert into Agents
                using (var cmd = new SqlCommand(@"
                    INSERT INTO Agents (ConversationID, AgentName, ClientName, Category, PreviewQuestion, ChatSlot, ChatStatus, AgentStatus)
                    VALUES (@ConversationID, @AgentName, @ClientName, @Category, @PreviewQuestion, @ChatSlot, 'Active', 'available')", connection))
                {
                    cmd.Parameters.AddWithValue("@ConversationID", model.SessionId);
                    cmd.Parameters.AddWithValue("@AgentName", model.AgentName);
                    cmd.Parameters.AddWithValue("@ClientName", clientName);
                    cmd.Parameters.AddWithValue("@Category", category);
                    cmd.Parameters.AddWithValue("@PreviewQuestion", previewQ);
                    cmd.Parameters.AddWithValue("@ChatSlot", 1);
                    await cmd.ExecuteNonQueryAsync();
                }
            }
            
            return Json(new { success = true, sessionId = model.SessionId, agent = model.AgentName, slot = 1 });
        }

        // ═══════════════════════════════════════════════════════════════════
        // END SESSION
        // ═══════════════════════════════════════════════════════════════════
        [HttpPost]
        public async Task<IActionResult> QueueEndSession([FromBody] QueueSessionActionRequest model)
        {
            if (model == null || model.SessionId <= 0)
                return BadRequest(new { success = false });

            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                
                using (var cmd = new SqlCommand(@"
                    UPDATE SupportFAQs SET Status = 'Resolved' WHERE Id = @Id", connection))
                {
                    cmd.Parameters.AddWithValue("@Id", model.SessionId);
                    await cmd.ExecuteNonQueryAsync();
                }
                
                using (var cmd = new SqlCommand(@"
                    UPDATE Agents SET ChatStatus = 'Resolved' WHERE ConversationID = @ConvId", connection))
                {
                    cmd.Parameters.AddWithValue("@ConvId", model.SessionId);
                    await cmd.ExecuteNonQueryAsync();
                }
            }
            
            return Json(new { success = true, sessionId = model.SessionId });
        }

        // ═══════════════════════════════════════════════════════════════════
        // RESOLVE
        // ═══════════════════════════════════════════════════════════════════
        [HttpPost]
        public async Task<IActionResult> QueueResolve([FromBody] QueueSessionActionRequest model)
        {
            if (model == null || model.SessionId <= 0)
                return BadRequest(new { success = false });

            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                
                using (var cmd = new SqlCommand(@"
                    UPDATE SupportFAQs SET Status = 'Resolved' WHERE Id = @Id", connection))
                {
                    cmd.Parameters.AddWithValue("@Id", model.SessionId);
                    await cmd.ExecuteNonQueryAsync();
                }
                
                using (var cmd = new SqlCommand(@"
                    UPDATE Agents SET ChatStatus = 'Resolved' WHERE ConversationID = @ConvId", connection))
                {
                    cmd.Parameters.AddWithValue("@ConvId", model.SessionId);
                    await cmd.ExecuteNonQueryAsync();
                }
            }
            
            return Json(new { success = true, sessionId = model.SessionId });
        }

        [HttpPost]
        public IActionResult QueueSend([FromBody] QueueSendRequest model)
        {
            if (model == null || string.IsNullOrWhiteSpace(model.Message))
                return BadRequest(new { success = false });
            return Json(new { success = true, message = model.Message, isNote = model.IsNote, timestamp = DateTime.Now.ToString("h:mm tt") });
        }

        [HttpPost]
        public IActionResult QueueAutoAssign() => Json(new { success = true }); 

        // ═══════════════════════════════════════════════════════════════════
        // FAQ ENDPOINTS
        // ═══════════════════════════════════════════════════════════════════
        [HttpPost]
        public async Task<IActionResult> FaqAdd([FromBody] AddFaqRequest model)
        {
            if (model == null || string.IsNullOrWhiteSpace(model.Question) || string.IsNullOrWhiteSpace(model.Answer))
                return BadRequest(new { success = false, message = "Question and answer are required." });

            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                using (var cmd = new SqlCommand(@"
                    INSERT INTO FAQs (Question, Answer, Category, UserType, Status, user_id, DateAdded, LastUpdated)
                    VALUES (@Question, @Answer, @Category, @UserType, 'Active', @UserId, GETDATE(), GETDATE());
                    SELECT SCOPE_IDENTITY();", connection))
                {
                    cmd.Parameters.AddWithValue("@Question", model.Question);
                    cmd.Parameters.AddWithValue("@Answer", model.Answer);
                    cmd.Parameters.AddWithValue("@Category", model.Category ?? "");
                    cmd.Parameters.AddWithValue("@UserType", model.UserType ?? "Consumer");
                    cmd.Parameters.AddWithValue("@UserId", 1);
                    
                    var newId = Convert.ToInt32(await cmd.ExecuteScalarAsync());
                    return Json(new { success = true, id = newId });
                }
            }
        }

        [HttpPost]
        public async Task<IActionResult> FaqUpdate([FromBody] UpdateFaqRequest model)
        {
            if (model == null || model.Id <= 0)
                return BadRequest(new { success = false });

            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                using (var cmd = new SqlCommand(@"
                    UPDATE FAQs SET Question=@Question, Answer=@Answer, Category=@Category,
                    UserType=@UserType, LastUpdated=GETDATE() WHERE FaqID=@FaqId", connection))
                {
                    cmd.Parameters.AddWithValue("@Question", model.Question);
                    cmd.Parameters.AddWithValue("@Answer", model.Answer);
                    cmd.Parameters.AddWithValue("@Category", model.Category ?? "");
                    cmd.Parameters.AddWithValue("@UserType", model.UserType ?? "Consumer");
                    cmd.Parameters.AddWithValue("@FaqId", model.Id);
                    
                    int rows = await cmd.ExecuteNonQueryAsync();
                    return rows > 0 ? Json(new { success = true }) : Json(new { success = false, message = "Update failed." });
                }
            }
        }

        [HttpPost]
        public async Task<IActionResult> FaqDelete([FromBody] DeleteFaqRequest model)
        {
            if (model == null || model.Id <= 0)
                return BadRequest(new { success = false });

            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                using (var cmd = new SqlCommand(@"
                    UPDATE FAQs SET Status='Resolved', LastUpdated=GETDATE() WHERE FaqID=@FaqId", connection))
                {
                    cmd.Parameters.AddWithValue("@FaqId", model.Id);
                    int rows = await cmd.ExecuteNonQueryAsync();
                    return rows > 0 ? Json(new { success = true }) : Json(new { success = false, message = "Failed to deactivate." });
                }
            }
        }

        [HttpPost]
        public async Task<IActionResult> FaqCategoryAdd([FromBody] AddCategoryRequest model)
        {
            if (model == null || string.IsNullOrWhiteSpace(model.Name))
                return BadRequest(new { success = false });

            // Just return success since categories are derived from FAQs
            return Json(new { success = true, name = model.Name.Trim() });
        }

        [HttpPost]
        public async Task<IActionResult> FaqCategoryDelete([FromBody] DeleteCategoryRequest model)
        {
            if (model == null || string.IsNullOrWhiteSpace(model.Name))
                return BadRequest(new { success = false });

            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                using (var cmd = new SqlCommand(@"
                    UPDATE FAQs SET Status='Resolved', LastUpdated=GETDATE() WHERE Category=@Category AND Status='Active'", connection))
                {
                    cmd.Parameters.AddWithValue("@Category", model.Name.Trim());
                    int rows = await cmd.ExecuteNonQueryAsync();
                    return Json(new { success = true, deletedCount = rows, message = rows > 0 ? $"{rows} FAQ(s) set to Resolved." : "Category removed." });
                }
            }
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
                                    
                                    // Get total_count from the first row (it will be the same for all rows)
                                    if (totalCount == 0 && logs.Count == 1)
                                    {
                                        totalCount = reader.GetInt32(reader.GetOrdinal("total_count"));
                                    }
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
                        await connection.OpenAsync();
                        
                        // First, check if email already exists
                        using (var checkCmd = new SqlCommand("SELECT COUNT(*) FROM users WHERE email = @Email", connection))
                        {
                            checkCmd.Parameters.AddWithValue("@Email", request.Email);
                            int emailCount = (int)await checkCmd.ExecuteScalarAsync();
                            
                            if (emailCount > 0)
                            {
                                return Json(new { success = false, message = "Email already exists in the system" });
                            }
                        }
                        
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
                            
                            using (var reader = await command.ExecuteReaderAsync())
                            {
                                if (await reader.ReadAsync())
                                {
                                    var status = reader["Status"].ToString();
                                    var message = reader["Message"].ToString();
                                    var newStaffId = reader["StaffId"] != DBNull.Value ? Convert.ToInt32(reader["StaffId"]) : (int?)null;
                                    
                                    if (status == "Success")
                                    {
                                        // Send email notification to the new admin
                                        bool emailSent = await _emailService.SendAdminCredentialsEmailAsync(
                                            request.Email,
                                            request.FirstName,
                                            request.LastName,
                                            username,
                                            password,
                                            request.UserType,
                                            adminName
                                        );
                                        
                                        string emailStatus = emailSent ? " Email notification sent to new admin." : " Warning: Email notification failed to send.";
                                        
                                        // Log the action
                                        await LogAdminAction(staffId, adminName, "Add Admin",
                                            $"{request.FirstName} {request.LastName} ({request.UserType})",
                                            "Success",
                                            $"Username: {username}, Password: {(isPasswordAutoGenerated ? password : "[User Provided]")}, Email sent: {emailSent}");
                                        
                                        return Json(new { 
                                            success = true, 
                                            message = message + emailStatus,
                                            username = username,
                                            password = isPasswordAutoGenerated ? password : null,
                                            isPasswordAutoGenerated = isPasswordAutoGenerated,
                                            emailSent = emailSent
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
                return _passwordHasher.HashPassword(null, password);    
            }

            private bool VerifyPassword(string hashedPassword, string plainTextPassword)
            {
                var result = _passwordHasher.VerifyHashedPassword(null, hashedPassword, plainTextPassword);
                return result == PasswordVerificationResult.Success;
            }
            
            // POST: Revoke admin access
            [HttpPost]
            public async Task<IActionResult> RevokeAdmin([FromBody] RevokeAdminRequest request)
            {
                try
                {
                    var staffId = HttpContext.Session.GetInt32("StaffId") ?? 0;
                    var adminName = HttpContext.Session.GetString("Username") ?? "System";
                    
                    // First, get admin details before revoking
                    string adminEmail = "";
                    string adminFirstName = "";
                    string adminLastName = "";
                    string adminUserType = "";
                    
                    using (var connection = new SqlConnection(_connectionString))
                    {
                        await connection.OpenAsync();
                        
                        // Get admin info for email
                        using (var cmd = new SqlCommand(@"
                            SELECT u.email, s.first_name, s.last_name, u.user_type
                            FROM staff_info s
                            INNER JOIN users u ON s.user_id = u.user_id
                            WHERE s.staff_id = @StaffId", connection))
                        {
                            cmd.Parameters.AddWithValue("@StaffId", request.StaffId);
                            using (var reader = await cmd.ExecuteReaderAsync())
                            {
                                if (await reader.ReadAsync())
                                {
                                    adminEmail = reader["email"]?.ToString() ?? "";
                                    adminFirstName = reader["first_name"]?.ToString() ?? "";
                                    adminLastName = reader["last_name"]?.ToString() ?? "";
                                    adminUserType = reader["user_type"]?.ToString() ?? "";
                                }
                            }
                        }
                        
                        // Revoke admin access
                        using (var command = new SqlCommand("sp_RevokeAdmin", connection))
                        {
                            command.CommandType = CommandType.StoredProcedure;
                            command.Parameters.AddWithValue("@StaffId", request.StaffId);
                            command.Parameters.AddWithValue("@Reason", request.Reason ?? (object)DBNull.Value);
                            command.Parameters.AddWithValue("@RevokedBy", staffId);
                            
                            using (var reader = await command.ExecuteReaderAsync())
                            {
                                if (await reader.ReadAsync())
                                {
                                    var status = reader["Status"].ToString();
                                    var message = reader["Message"].ToString();
                                    var adminNameRevoked = reader["AdminName"].ToString();
                                    
                                    if (status == "Success")
                                    {
                                        // Send email notification to the revoked admin
                                        bool emailSent = false;
                                        if (!string.IsNullOrEmpty(adminEmail))
                                        {
                                            emailSent = await _emailService.SendAdminRevokedEmailAsync(
                                                adminEmail,
                                                adminFirstName,
                                                adminLastName,
                                                adminUserType,
                                                request.Reason ?? "No reason provided",
                                                adminName
                                            );
                                        }
                                        
                                        string emailStatus = emailSent ? " Email notification sent to revoked admin." : " Warning: Email notification failed to send.";
                                        
                                        await LogAdminAction(staffId, adminName, "Revoke Admin Access",
                                            adminNameRevoked,
                                            "Success",
                                            $"Reason: {request.Reason}, Email sent: {emailSent}");
                                        
                                        return Json(new { success = true, message = message + emailStatus });
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
                        var verificationResult = VerifyPassword(currentHash, request.CurrentPassword);

                        if (!verificationResult)
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
            public string Note { get; set; } = "";
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


        public class ClaimPrizeRequest
        {
            public string ClaimCode { get; set; }
            public string ClaimDetails { get; set; }
        }

        public class VerifyAndClaimRequest
        {
            public string ClaimCode { get; set; }
        }

        public class VerifyActivityRequest
        {
            public int ActivityId { get; set; }
        }
    }