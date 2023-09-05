namespace ProReception.DistributionServerInfrastructure.ProReceptionApi.ActConnector;

using JetBrains.Annotations;
using Models;

[PublicAPI]
public static class ActConnectorExtensions
{
    public static async Task<ActQueueResponse> GetQueue(this IProReceptionApiClient proReceptionApiClient)
        => await proReceptionApiClient.Get<ActQueueResponse>("act-connector/queue");

    public static async Task<ActUserResponse> GetContractor(this IProReceptionApiClient proReceptionApiClient, int contractorId)
        => await proReceptionApiClient.Get<ActUserResponse>($"act-connector/contractors/{contractorId}");

    public static async Task SaveContractorBadgeId(this IProReceptionApiClient proReceptionApiClient, int contractorId, string badgeId)
        => await proReceptionApiClient.Post($"act-connector/contractors/{contractorId}/badge-id", new { badgeId });

    public static async Task<ActUserResponse> GetVisitorPass(this IProReceptionApiClient proReceptionApiClient, int visitorPassId)
        => await proReceptionApiClient.Get<ActUserResponse>($"act-connector/visitor-passes/{visitorPassId}");

    public static async Task SaveVisitorPassBadgeId(this IProReceptionApiClient proReceptionApiClient, int visitorPassId, string badgeId)
        => await proReceptionApiClient.Post($"act-connector/visitor-passes/{visitorPassId}/badge-id", new { badgeId });

    public static async Task DoorPassed(this IProReceptionApiClient proReceptionApiClient, string badgeId, string location, DateTime timestamp, int doorGroupNumber)
        => await proReceptionApiClient.Post("act-connector/door-passed", new { badgeId, location, timestamp, doorGroupNumber });

    public static async Task SaveActUserGroups(this IProReceptionApiClient proReceptionApiClient, SaveActUserGroupsRequest request)
        => await proReceptionApiClient.Post("act-connector/user-groups", request);

    public static async Task SaveActDoorGroups(this IProReceptionApiClient proReceptionApiClient, SaveActDoorGroupsRequest request)
        => await proReceptionApiClient.Post("act-connector/door-groups", request);
}
