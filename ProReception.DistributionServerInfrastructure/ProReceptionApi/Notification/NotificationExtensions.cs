using JetBrains.Annotations;

namespace ProReception.DistributionServerInfrastructure.ProReceptionApi.Notification;

[PublicAPI]
public static class NotificationExtensions
{
    public static async Task SendPrinterProblemNotification(this IProReceptionApiClient proReceptionApiClient)
        => await proReceptionApiClient.Post("notification/printer-problem", new object());
}
