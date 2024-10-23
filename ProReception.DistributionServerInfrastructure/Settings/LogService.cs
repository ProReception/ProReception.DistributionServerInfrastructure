namespace ProReception.DistributionServerInfrastructure.Settings;

public class LogService(ISettingsManagerBase settingsManager)
{
    public string GetLogFilesPath()
    {
        return settingsManager.GetLogFilesPath();
    }

    public IEnumerable<string> GetLogFileNames()
    {
        var directoryPath = settingsManager.GetLogFilesPath();
        var filePaths = Directory.EnumerateFiles(directoryPath, "*.txt");

        return filePaths.Select(Path.GetFileName).Where(name => name != null)!;
    }

    public Stream GetLogFileStream(string fileName)
    {
        var directoryPath = settingsManager.GetLogFilesPath();
        var filePath = Path.Combine(directoryPath, fileName);

        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException($"The log file {fileName} could not be found.");
        }

        return new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
    }
}
