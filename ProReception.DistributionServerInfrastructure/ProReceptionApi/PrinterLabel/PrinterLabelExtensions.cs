namespace ProReception.DistributionServerInfrastructure.ProReceptionApi.PrinterLabel;

using System.Net;
using System.Net.Mime;
using Flurl.Http;
using JetBrains.Annotations;
using Models;

[PublicAPI]
public static class PrinterLabelExtensions
{
    public static async Task<LabelResponse?> GetPrinterLabelAsync(this IProReceptionApiClient proReceptionApiClient)
    {
        try
        {
            var response = await proReceptionApiClient.GetRaw("printer-label");

            return new LabelResponse
            {
                Filename = new ContentDisposition(response.Headers.FirstOrDefault("Content-Disposition")).FileName,
                FileContent = await response.GetBytesAsync(),
                LabelVersion = int.Parse(response.Headers.FirstOrDefault("X-Label-Version") ?? "0")
            };
        }
        catch (FlurlHttpException flurlHttpException) when (flurlHttpException.StatusCode == (int)HttpStatusCode.NotFound)
        {
            return null;
        }
    }

    public static async Task<FileResponse?> GetPrinterLabelLogoAsync(this IProReceptionApiClient proReceptionApiClient)
    {
        var response = await proReceptionApiClient.GetRaw("printer-label/logo-url");
        var labelLogoUrl = await response.GetStringAsync();

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
