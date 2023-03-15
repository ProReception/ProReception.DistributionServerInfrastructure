namespace ProReception.DistributionServerInfrastructure.ProReceptionApi.PrintServerConfig;

using Models;

public static class PrintServerConfigExtensions
{
    public static async Task<PrintServerConfigResponse> GetPrintServerConfigAsync(this IProReceptionApiClient proReceptionApiClient) =>
        await proReceptionApiClient.Get<PrintServerConfigResponse>("print-server-config");
}
