namespace ProReception.DistributionServerInfrastructure.ProReceptionApi.PrinterLabel;

using System.Net;
using System.Net.Mime;
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
                Filename = new ContentDisposition(response.Headers.FirstOrDefault("Content-Disposition")).FileName,
                FileContent = await response.GetBytesAsync()
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
            FileContent = await response.GetBytesAsync()
        };
    }
}
