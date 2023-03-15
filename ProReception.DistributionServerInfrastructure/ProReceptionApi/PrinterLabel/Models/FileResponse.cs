namespace ProReception.DistributionServerInfrastructure.ProReceptionApi.PrinterLabel.Models;

public class FileResponse
{
    public string? Filename { get; set; }

    public required byte[] FileContent { get; set; }
}
