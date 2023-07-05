namespace ProReception.DistributionServerInfrastructure.Settings;

public class LogService
{
    private readonly ISettingsManagerBase _settingsManager;

    public LogService(ISettingsManagerBase settingsManager)
    {
        _settingsManager = settingsManager;
    }

    public IEnumerable<string> GetLogFileNames()
    {
        var directoryPath = _settingsManager.GetLogFilesPath();
        var filePaths = Directory.EnumerateFiles(directoryPath, "*.txt");

        return filePaths.Select(Path.GetFileName).Where(name => name != null)!;
    }

    public Stream GetLogFileStream(string fileName)
    {
        var directoryPath = _settingsManager.GetLogFilesPath();
        var filePath = Path.Combine(directoryPath, fileName);

        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException($"The log file {fileName} could not be found.");
        }

        return new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
    }
}
