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

    public static async Task SaveContractorBatchId(this IProReceptionApiClient proReceptionApiClient, int contractorId, string batchId)
        => await proReceptionApiClient.Post($"act-connector/contractors/{contractorId}/batch-id", new { batchId });

    public static async Task<ActUserResponse> GetVisitorPass(this IProReceptionApiClient proReceptionApiClient, int visitorPassId)
        => await proReceptionApiClient.Get<ActUserResponse>($"act-connector/visitor-passes/{visitorPassId}");

    public static async Task SaveVisitorPassBatchId(this IProReceptionApiClient proReceptionApiClient, int visitorPassId, string batchId)
        => await proReceptionApiClient.Post($"act-connector/visitor-passes/{visitorPassId}/batch-id", new { batchId });

    public static async Task CheckIn(this IProReceptionApiClient proReceptionApiClient, string batchId, string location, DateTime timestamp, int doorGroupNumber)
        => await proReceptionApiClient.Post("act-connector/check-in", new { batchId, location, timestamp, doorGroupNumber });

    public static async Task CheckOut(this IProReceptionApiClient proReceptionApiClient, string batchId, string location, DateTime timestamp)
        => await proReceptionApiClient.Post("act-connector/check-out", new { batchId, location, timestamp });

    public static async Task SaveActUserGroups(this IProReceptionApiClient proReceptionApiClient, SaveActUserGroupsRequest request)
        => await proReceptionApiClient.Post("act-connector/user-groups", request);

    public static async Task SaveActDoorGroups(this IProReceptionApiClient proReceptionApiClient, SaveActDoorGroupsRequest request)
        => await proReceptionApiClient.Post("act-connector/door-groups", request);
}
