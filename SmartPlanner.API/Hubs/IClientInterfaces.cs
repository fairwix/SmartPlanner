// SmartPlanner.API/Hubs/IClientInterfaces.cs
namespace SmartPlanner.API.Hubs
{
    /// <summary>
    /// Интерфейс клиента для NotificationHub
    /// </summary>
    public interface INotificationClient
    {
        // Подключение/отключение
        Task Connected(object connectionInfo);
        Task Disconnected(string message);

        // Уведомления
        Task ReceiveNotification(object notification);
        Task NotificationSent(object confirmation);

        // Файлы
        Task SubscribedToFile(object subscription);
        Task UnsubscribedFromFile(object unsubscription);

        // Общее
        Task Pong(object response);
        Task ConnectionInfoReceived(object info);
        Task ActiveUsersList(object users);
        Task ReceiveBroadcast(object broadcast);
        Task BroadcastSent(object confirmation);
        Task UnreadCountUpdated(object countInfo);

        // Ошибки
        Task Error(object error);
    }

    /// <summary>
    /// Интерфейс клиента для FileHub
    /// </summary>
    public interface IFileClient
    {
        // Подключение/отключение
        Task FileHubConnected(object connectionInfo);

        // Загрузка файлов
        Task UploadStarted(object uploadInfo);
        Task TrackingStarted(object confirmation);
        Task UploadProgressUpdated(object progress);
        Task UploadCompleted(object completion);
        Task UploadProgressRetrieved(object progress);
        Task UploadCancelled(object cancellation);
        Task UploadCancelledConfirmed(object confirmation);

        // События файлов
        Task FileDownloadStarted(object downloadInfo);
        Task FileUploadCompleted(object uploadInfo);
        Task SubscribedToFileEvents(object subscription);

        // Списки
        Task ActiveUploads(object uploads);
        Task ActiveUploadsList(object uploadsList);
        Task DownloadNotificationSent(object confirmation);
        Task UploadCompletedNotificationSent(object confirmation);

        // Ошибки
        Task Error(object error);
    }
}
