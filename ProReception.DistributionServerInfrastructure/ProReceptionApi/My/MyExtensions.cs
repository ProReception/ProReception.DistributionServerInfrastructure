namespace ProReception.DistributionServerInfrastructure.ProReceptionApi.My;

using JetBrains.Annotations;
using Models;

[PublicAPI]
public static class MyExtensions
{
    public static async Task<List<ClientLevel>> GetMyClientLevels(this IProReceptionApiClient proReceptionApiClient)
        => await proReceptionApiClient.Get<List<ClientLevel>>("my/client-levels");

    public static async Task<List<ClientSite>> GetMyClientSites(this IProReceptionApiClient proReceptionApiClient)
        => await proReceptionApiClient.Get<List<ClientSite>>("my/client-sites");
}
