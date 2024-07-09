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

    public static async Task<NoxUserResponse> GetNoxVehicle(this IProReceptionApiClient proReceptionApiClient, int vehicleId)
        => await proReceptionApiClient.Get<NoxUserResponse>($"nox-connector/vehicle/{vehicleId}");

    public static async Task SaveNoxDoors(this IProReceptionApiClient proReceptionApiClient, SaveNoxDoorsRequest request)
        => await proReceptionApiClient.Post("nox-connector/doors", request);

    public static async Task SaveNoxAccessRings(this IProReceptionApiClient proReceptionApiClient, SaveNoxAccessRingsRequest request)
        => await proReceptionApiClient.Post("nox-connector/access-rings", request);

    public static async Task SaveNoxUserGroups(this IProReceptionApiClient proReceptionApiClient, SaveNoxUserGroupsRequest request)
        => await proReceptionApiClient.Post("nox-connector/user-groups", request);

    public static async Task<NoxUserResponse> AssignContractorBadgeId(this IProReceptionApiClient proReceptionApiClient, int constructionContractorId)
        => await proReceptionApiClient.Post<NoxUserResponse>($"nox-connector/contractors/{constructionContractorId}/assign-badge-id", new object());

     public static async Task<NoxUserResponse> AssignVisitorPassBadgeId(this IProReceptionApiClient proReceptionApiClient, int constructionVisitorPassId)
         => await proReceptionApiClient.Post<NoxUserResponse>($"nox-connector/visitor-passes/{constructionVisitorPassId}/assign-badge-id", new object());

     public static async Task SaveNoxUserCardNumber(this IProReceptionApiClient proReceptionApiClient, string noxUsername, string cardNumber)
         => await proReceptionApiClient.Post($"nox-connector/nox-user/{noxUsername}/card-number", new { cardNumber });

     public static async Task CheckInNox(this IProReceptionApiClient proReceptionApiClient, string noxUsername, CheckInRequest request)
         => await proReceptionApiClient.Post($"nox-connector/nox-user/{noxUsername}/check-in", request);

     public static async Task CheckOutNox(this IProReceptionApiClient proReceptionApiClient, string noxUsername, CheckOutRequest request)
         => await proReceptionApiClient.Post($"nox-connector/nox-user/{noxUsername}/check-out", request);
}
