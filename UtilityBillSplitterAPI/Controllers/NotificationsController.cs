using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UtilityBillSplitterAPI.Data;
using UtilityBillSplitterAPI.Models;

namespace UtilityBillSplitter.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class NotificationsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public NotificationsController(AppDbContext context)
        {
            _context = context;
        }

        // GET: api/notifications
        [HttpGet]
        public IActionResult GetAllNotifications()
        {
            try
            {
                // Get raw notifications data first to avoid EF translation issues
                var rawNotifications = _context.Notifications
                    .Include(n => n.User)
                    .OrderByDescending(n => n.CreatedAt)
                    .ToList();

                // Format the data in memory
                var notifications = rawNotifications.Select(n => new
                {
                    id = n.Id,
                    title = "Bill Notification", // Default title
                    message = n.Message,
                    type = "info", // Default type
                    isRead = n.IsRead,
                    createdAt = n.CreatedAt.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"), // Full ISO format
                    userId = n.UserId,
                    user = new
                    {
                        username = n.User?.Username ?? "",
                        email = n.User?.Email ?? ""
                    },
                    // Additional fields for better display
                    timeAgo = GetTimeAgo(n.CreatedAt),
                    formattedDate = n.CreatedAt.ToString("MMM dd, yyyy 'at' hh:mm tt")
                }).ToList();

                return Ok(notifications);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting notifications: {ex.Message}");
                return StatusCode(500, "Internal server error");
            }
        }

        // GET: api/notifications/user/{userId}
        [HttpGet("user/{userId}")]
        public IActionResult GetUserNotifications(int userId)
        {
            try
            {
                // Get raw notifications data first
                var rawNotifications = _context.Notifications
                    .Include(n => n.User)
                    .Where(n => n.UserId == userId)
                    .OrderByDescending(n => n.CreatedAt)
                    .ToList();

                // Format the data in memory
                var notifications = rawNotifications.Select(n => new
                {
                    id = n.Id,
                    title = "Bill Notification",
                    message = n.Message,
                    type = "info",
                    isRead = n.IsRead,
                    createdAt = n.CreatedAt.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"),
                    userId = n.UserId,
                    user = new
                    {
                        username = n.User?.Username ?? "",
                        email = n.User?.Email ?? ""
                    },
                    timeAgo = GetTimeAgo(n.CreatedAt),
                    formattedDate = n.CreatedAt.ToString("MMM dd, yyyy 'at' hh:mm tt")
                }).ToList();

                return Ok(notifications);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting user notifications: {ex.Message}");
                return StatusCode(500, "Internal server error");
            }
        }

        // POST: api/notifications
        [HttpPost]
        public IActionResult CreateNotification([FromBody] NotificationCreateDto dto)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                // Get user email for the required RecipientEmail field
                var user = _context.Users.Find(dto.UserId);
                if (user == null)
                    return BadRequest("User not found");

                var notification = new Notification
                {
                    Message = dto.Message,
                    UserId = dto.UserId,
                    RecipientEmail = user.Email,
                    IsRead = false,
                    CreatedAt = DateTime.UtcNow
                };

                _context.Notifications.Add(notification);
                _context.SaveChanges();

                var response = new
                {
                    id = notification.Id,
                    title = "Bill Notification",
                    message = notification.Message,
                    type = "info",
                    isRead = notification.IsRead,
                    createdAt = notification.CreatedAt.ToString("yyyy-MM-dd"),
                    userId = notification.UserId
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error creating notification: {ex.Message}");
                return StatusCode(500, "Internal server error");
            }
        }

        // PATCH: api/notifications/{id}/read
        [HttpPatch("{id}/read")]
        public IActionResult MarkNotificationAsRead(int id)
        {
            try
            {
                var notification = _context.Notifications.Find(id);
                if (notification == null)
                    return NotFound();

                notification.IsRead = true;
                _context.SaveChanges();

                return Ok();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error marking notification as read: {ex.Message}");
                return StatusCode(500, "Internal server error");
            }
        }

        // Helper method to calculate time ago
        private static string GetTimeAgo(DateTime dateTime)
        {
            var timeSpan = DateTime.UtcNow - dateTime;

            if (timeSpan.TotalDays >= 365)
                return $"{(int)(timeSpan.TotalDays / 365)} year{((int)(timeSpan.TotalDays / 365) == 1 ? "" : "s")} ago";

            if (timeSpan.TotalDays >= 30)
                return $"{(int)(timeSpan.TotalDays / 30)} month{((int)(timeSpan.TotalDays / 30) == 1 ? "" : "s")} ago";

            if (timeSpan.TotalDays >= 7)
                return $"{(int)(timeSpan.TotalDays / 7)} week{((int)(timeSpan.TotalDays / 7) == 1 ? "" : "s")} ago";

            if (timeSpan.TotalDays >= 1)
                return $"{(int)timeSpan.TotalDays} day{((int)timeSpan.TotalDays == 1 ? "" : "s")} ago";

            if (timeSpan.TotalHours >= 1)
                return $"{(int)timeSpan.TotalHours} hour{((int)timeSpan.TotalHours == 1 ? "" : "s")} ago";

            if (timeSpan.TotalMinutes >= 1)
                return $"{(int)timeSpan.TotalMinutes} minute{((int)timeSpan.TotalMinutes == 1 ? "" : "s")} ago";

            return "Just now";
        }
    }

    public class NotificationCreateDto
    {
        public string Message { get; set; } = string.Empty;
        public int UserId { get; set; }
    }
}

