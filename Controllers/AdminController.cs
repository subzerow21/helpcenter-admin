using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NextHorizon.Models;
using NextHorizon.Models.Admin_Models;
using NextHorizon.Services.AdminServices;
using NextHorizon.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Data.SqlClient;

namespace NextHorizon.Controllers
{
    public class AdminController : Controller
    {
        private readonly DashboardService _dashboardService;
        private readonly AppDbContext _context;

        public AdminController(AppDbContext context)
        {
            _context = context;
            _dashboardService = new DashboardService();
        }

        public IActionResult Dashboard()
        {
            var model = _dashboardService.GetHeroStats();
            return View(model);
        }

        // ─────────────────────────────────────────────
        // HELP CENTER
        // ─────────────────────────────────────────────
        public IActionResult HelpCenter()
        {
            var faqs = _context.FAQs
                .Where(f => f.Status == "active" || f.Status == "Active")
                .OrderByDescending(f => f.DateAdded)
                .Select(f => new FaqItem
                {
                    Id = f.FaqID,
                    Question = f.Question,
                    Answer = f.Answer,
                    Category = f.Category,
                    UserType = f.UserType
                })
                .ToList();

            var categories = _context.FAQs
                .Where(f => f.Status == "active" || f.Status == "Active")
                .Select(f => f.Category)
                .Distinct()
                .OrderBy(c => c)
                .ToList();

            var dbSessions = _context.HelpSessions
                .Where(s => s.Status == "Waiting")
                .OrderBy(s => s.CreatedAt)
                .ToList();

            var sessions = dbSessions.Select((s, i) => new QueueSession
            {
                Id = s.Id,
                SessionNo = "NH-" + s.Id.ToString("D5"),
                CustomerName = "Customer #" + s.Id,
                Role = s.UserType,
                WaitSeconds = (int)(DateTime.Now - s.CreatedAt).TotalSeconds,
                QueuePosition = i + 1,
                Status = "waiting",
                Category = s.Category ?? "General",
                Messages = new List<QueueMessage>()
            }).ToList();

            var activeAgentRows = _context.Agents
                .Where(a => (a.ChatStatus == "Active" || a.ChatStatus == "active")
                         && a.EndTime == null
                         && a.ConversationID != null)
                .ToList();

            var allAgentNames = _context.Agents
                .Select(a => a.AgentName ?? "Unknown")
                .Distinct()
                .ToList();

            var agents = allAgentNames.Select(name =>
            {
                var slots = activeAgentRows
                    .Where(a => a.AgentName == name)
                    .Select(a => new AgentSlot
                    {
                        ConversationId = a.AgentID ?? 0, // now stores SessionId
                        ClientName = a.ClientName,
                        Category = a.Category,
                        SlotNumber = a.ChatSlot ?? 0
                    })
                    .ToList();

                return new AgentStatusViewModel
                {
                    Name = name,
                    Status = "online",
                    ActiveSessions = slots.Count,
                    MaxSessions = 3,
                    Slots = slots
                };
            }).ToList();

            var viewModel = new HelpCenterV2ViewModel
            {
                Sessions = sessions,
                Agents = agents,
                Faqs = faqs,
                Categories = categories
            };

            return View(viewModel);
        }

        // ─────────────────────────────────────────────
        // QUEUE ASSIGN (UPDATED 🔥)
        // ─────────────────────────────────────────────
        [HttpPost]
        public IActionResult QueueAssign([FromBody] AssignSessionRequest model)
        {
            if (model == null || model.SessionId <= 0)
                return BadRequest();

            var agentRow = _context.Agents
                .FirstOrDefault(a => a.AgentName == model.AgentName);

            var agentUserId = agentRow?.UserID;

            var usedSlots = _context.Agents
                .Where(a => a.AgentName == model.AgentName
                         && a.EndTime == null)
                .Select(a => (int)(a.ChatSlot ?? 0))
                .ToList();

            int nextSlot = Enumerable.Range(1, 3)
                .FirstOrDefault(s => !usedSlots.Contains(s));

            if (nextSlot == 0)
                return Json(new { success = false });

            var session = _context.HelpSessions.Find(model.SessionId);

            var clientName = "Customer #" + session.Id;
            var category = session.Category ?? "General";
            var previewQ = session.Question ?? "";

            // 🔥 UPDATED INSERT: store SessionId in AgentID (ConversationID equivalent)
            _context.Database.ExecuteSqlRaw(
     @"INSERT INTO dbo.Agents
        (ConversationID, AgentName, ClientName, Category, PreviewQuestion,
         ChatSlot, ChatStatus, StartTime, AgentStatus, UserID)
      VALUES
        (@ConversationID, @AgentName, @ClientName, @Category, @PreviewQuestion,
         @ChatSlot, 'Active', GETDATE(), 'available', @UserID)",
     new SqlParameter("@ConversationID", model.SessionId),
     new SqlParameter("@AgentName", model.AgentName),
     new SqlParameter("@ClientName", clientName),
     new SqlParameter("@Category", category),
     new SqlParameter("@PreviewQuestion", previewQ),
     new SqlParameter("@ChatSlot", Convert.ToByte(nextSlot)),
     new SqlParameter("@UserID", agentUserId.HasValue ? (object)agentUserId.Value : DBNull.Value));

            return Json(new { success = true });
        }

        // ─────────────────────────────────────────────
        // END SESSION (UPDATED 🔥)
        // ─────────────────────────────────────────────
        [HttpPost]
        public IActionResult QueueEndSession([FromBody] QueueSessionActionRequest model)
        {
            if (model == null || model.SessionId <= 0)
                return BadRequest();

            _context.Database.ExecuteSqlRaw(
                "UPDATE dbo.SupportFAQs SET Status='Resolved', EndTime=GETDATE() WHERE Id=@Id",
                new SqlParameter("@Id", model.SessionId));

            _context.Database.ExecuteSqlRaw(
                @"UPDATE dbo.Agents
                  SET ChatStatus='Resolved', EndTime=GETDATE()
                  WHERE AgentID=@SessionId
                    AND ChatStatus='Active'
                    AND EndTime IS NULL",
                new SqlParameter("@SessionId", model.SessionId));

            return Json(new { success = true });
        }

        // ─────────────────────────────────────────────
        // RESOLVE (UPDATED 🔥)
        // ─────────────────────────────────────────────
        [HttpPost]
        public IActionResult QueueResolve([FromBody] QueueSessionActionRequest model)
        {
            if (model == null || model.SessionId <= 0)
                return BadRequest();

            _context.Database.ExecuteSqlRaw(
                "UPDATE dbo.SupportFAQs SET Status='Resolved', EndTime=GETDATE() WHERE Id=@Id",
                new SqlParameter("@Id", model.SessionId));

            _context.Database.ExecuteSqlRaw(
                @"UPDATE dbo.Agents
                  SET ChatStatus='Resolved', EndTime=GETDATE()
                  WHERE AgentID=@SessionId
                    AND ChatStatus='Active'
                    AND EndTime IS NULL",
                new SqlParameter("@SessionId", model.SessionId));

            return Json(new { success = true });
        }

        // ─────────────────────────────────────────────
        // AVAILABLE AGENTS (UPDATED 🔥)
        // ─────────────────────────────────────────────
        [HttpGet]
        public IActionResult GetAvailableAgents()
        {
            var allAgentNames = _context.Agents
                .Select(a => a.AgentName)
                .Distinct()
                .ToList();

            var activeRows = _context.Agents
                .Where(a => (a.ChatStatus == "Active" || a.ChatStatus == "active")
                         && a.EndTime == null
                         && a.AgentID != null)
                .ToList();

            var result = allAgentNames.Select(name =>
            {
                var activeCount = activeRows.Count(a => a.AgentName == name);

                return new
                {
                    name = name,
                    activeSessions = activeCount,
                    maxSessions = 3,
                    hasSlot = activeCount < 3
                };
            });

            return Json(result);
        }
    }
}