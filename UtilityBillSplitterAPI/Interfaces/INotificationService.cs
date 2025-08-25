using UtilityBillSplitterAPI.Models;

namespace UtilityBillSplitterAPI.Interfaces
{
    public interface INotificationService
    {
        Task CreateNotificationAsync(int userId, string message, string email);
        Task MarkAsReadAsync(int id);
        List<Notification> GetUserNotifications(int userId);
        Task DeleteNotificationAsync(int id);
        Task<int> GetUnreadCountAsync(int userId);
        Task<Notification> GetLatestNotificationAsync(int userId);
    }
}
