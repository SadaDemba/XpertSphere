using XpertSphere.CommunicationService.Models;

namespace XpertSphere.CommunicationService.Services.Interfaces;

public interface INotificationService
{
    Task<bool> SendNotificationAsync(NotificationMessage notification, CancellationToken cancellationToken = default);
    Task<bool> SendBulkNotificationsAsync(List<NotificationMessage> notifications, CancellationToken cancellationToken = default);
    Task<List<NotificationMessage>> GetUserNotificationsAsync(string userId, bool includeRead = false, CancellationToken cancellationToken = default);
    Task<bool> MarkAsReadAsync(string notificationId, CancellationToken cancellationToken = default);
    Task<bool> MarkAllAsReadAsync(string userId, CancellationToken cancellationToken = default);
    Task<bool> DeleteNotificationAsync(string notificationId, CancellationToken cancellationToken = default);
    Task<int> GetUnreadCountAsync(string userId, CancellationToken cancellationToken = default);
}
