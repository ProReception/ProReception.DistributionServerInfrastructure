namespace ProReception.DistributionServerInfrastructure.ProReceptionApi.PrinterLabel;

using System.Net;
using System.Net.Mime;
using Flurl.Http;
using JetBrains.Annotations;
using Models;

[PublicAPI]
public static class PrinterLabelExtensions
{
    /// <summary>
    /// Retrieves printer label data from the Pro Reception API with optional division filtering.
    /// </summary>
    /// <param name="proReceptionApiClient">The API client instance.</param>
    /// <param name="divisionId">Optional division ID to filter labels. When null, retrieves labels for all divisions.</param>
    /// <returns>A <see cref="LabelResponse"/> containing the label file data and version, or null if no label is found (HTTP 404).</returns>
    public static async Task<LabelResponse?> GetPrinterLabelAsync(this IProReceptionApiClient proReceptionApiClient, int? divisionId = null)
    {
        try
        {
            var url = new Flurl.Url("printer-label");
            if (divisionId.HasValue)
            {
                url.SetQueryParam("divisionId", divisionId.Value);
            }
            var response = await proReceptionApiClient.GetRaw(url.ToString());

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
