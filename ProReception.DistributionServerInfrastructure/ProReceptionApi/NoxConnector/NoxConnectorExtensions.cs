namespace ProReception.DistributionServerInfrastructure.ProReceptionApi.NoxConnector;

using JetBrains.Annotations;
using Models;

[PublicAPI]
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

    public static async Task<NoxUserResponse> AssignContractorBadgeId(this IProReceptionApiClient proReceptionApiClient, int constructionContractorId)
        => await proReceptionApiClient.Post<NoxUserResponse>($"nox-connector/contractors/{constructionContractorId}/assign-badge-id", new object());

     public static async Task<NoxUserResponse> AssignVisitorPassBadgeId(this IProReceptionApiClient proReceptionApiClient, int constructionVisitorPassId)
         => await proReceptionApiClient.Post<NoxUserResponse>($"nox-connector/visitor-passes/{constructionVisitorPassId}/assign-badge-id", new object());

     public static async Task SaveContractorCardNumber(this IProReceptionApiClient proReceptionApiClient, int constructionContractorId, string cardNumber)
         => await proReceptionApiClient.Post($"nox-connector/contractors/{constructionContractorId}/card-number", new { cardNumber });

     public static async Task SaveVisitorPassCardNumber(this IProReceptionApiClient proReceptionApiClient, int constructionVisitorPassId, string cardNumber)
         => await proReceptionApiClient.Post($"nox-connector/visitor-passes/{constructionVisitorPassId}/card-number", new { cardNumber });
}
