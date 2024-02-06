namespace ProReception.DistributionServerInfrastructure.ProReceptionApi.NoxConnector;

using Models;

public static class NoxConnectorExtensions
{
    public static async Task<NoxQueueResponse> GetNoxQueue(this IProReceptionApiClient proReceptionApiClient)
        => await proReceptionApiClient.Get<NoxQueueResponse>("nox-connector/queue");

    public static async Task<NoxUserResponse> GetNoxContractor(this IProReceptionApiClient proReceptionApiClient, int contractorId)
        => await proReceptionApiClient.Get<NoxUserResponse>($"nox-connector/contractors/{contractorId}");

    public static async Task<NoxUserResponse> GetNoxVisitorPass(this IProReceptionApiClient proReceptionApiClient, int visitorPassId)
        => await proReceptionApiClient.Get<NoxUserResponse>($"nox-connector/visitor-passes/{visitorPassId}");

    public static async Task SaveNoxDoors(this IProReceptionApiClient proReceptionApiClient, SaveNoxDoorsRequest request)
        => await proReceptionApiClient.Post("nox-connector/doors", request);
}
