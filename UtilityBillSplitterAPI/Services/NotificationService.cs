using UtilityBillSplitterAPI.Interfaces;
using UtilityBillSplitterAPI.Models;
using UtilityBillSplitterAPI.Data;
using Microsoft.EntityFrameworkCore;

namespace UtilityBillSplitterAPI.Services
{
    public class NotificationService : INotificationService
    {
        private readonly IEmailService _emailService;
        private readonly AppDbContext _context;

        public NotificationService(IEmailService emailService, AppDbContext context)
        {
            _emailService = emailService;
            _context = context;
        }

        public async Task CreateNotificationAsync(int userId, string message, string email)
        {
            var notification = new Notification
            {
                UserId = userId,
                Message = message,
                RecipientEmail = email,
                CreatedAt = DateTime.UtcNow,
                IsRead = false
            };

            // Save to database
            _context.Notifications.Add(notification);
            await _context.SaveChangesAsync();

            // Send email
            bool sent = await _emailService.SendEmailAsync(email, "New Notification", message);

            if (!sent)
            {
                Console.WriteLine($"⚠️ Failed to send email to {email}");
                // Optional: Log this or schedule a retry
            }
        }

        public Task DeleteNotificationAsync(int id)
        {
            throw new NotImplementedException();
        }

        public Task<Notification> GetLatestNotificationAsync(int userId)
        {
            throw new NotImplementedException();
        }

        public Task<int> GetUnreadCountAsync(int userId)
        {
            throw new NotImplementedException();
        }

        public List<Notification> GetUserNotifications(int userId)
        {
            return _context.Notifications
                           .Include(n => n.User)
                           .Where(n => n.UserId == userId)
                           .OrderByDescending(n => n.CreatedAt)
                           .ToList();
        }

        public async Task MarkAsReadAsync(int notificationId)
        {
            var notification = await _context.Notifications.FindAsync(notificationId);
            if (notification != null && !notification.IsRead)
            {
                notification.IsRead = true;
                await _context.SaveChangesAsync();
            }
        }
    }
}


