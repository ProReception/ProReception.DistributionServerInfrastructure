namespace ProReception.DistributionServerInfrastructure.ProReceptionApi.PrinterLabel;

using System.Net;
using Flurl.Http;
using Models;

public static class PrinterLabelExtensions
{
    public static async Task<FileResponse?> GetPrinterLabelAsync(this IProReceptionApiClient proReceptionApiClient)
    {
        try
        {
            var response = await proReceptionApiClient.GetRaw("printer-label");

            return new FileResponse
            {
                Filename = response.Headers.FirstOrDefault("X-FileName"),
                FileContent = await response.ResponseMessage.Content.ReadAsByteArrayAsync()
            };
        }
        catch (FlurlHttpException flurlHttpException) when (flurlHttpException.StatusCode == (int)HttpStatusCode.NotFound)
        {
            return null;
        }
    }

    public static async Task<FileResponse?> GetPrinterLabelLogoAsync(this IProReceptionApiClient proReceptionApiClient)
    {
        var labelLogoUrl = await proReceptionApiClient.Get<string?>("printer-label/logo-url");

        if (string.IsNullOrWhiteSpace(labelLogoUrl))
        {
            return null;
        }

        return new FileResponse
        {
            Filename = System.IO.Path.GetFileName(labelLogoUrl),
            FileContent = await labelLogoUrl.GetBytesAsync()
        };
    }
}
