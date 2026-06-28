namespace ControlOS.Api.Backend.Services;

public static class ApplicationPaths
{
    public static string BaseDirectory =>
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Control-OS-Lunix");

    public static string ConfigurationFilePath => Path.Combine(BaseDirectory, "config.json");

    public static string LogFilePath => Path.Combine(BaseDirectory, "activity.log");

    public static string CreateDefaultBackupFilePath()
    {
        string backupDirectory = Path.Combine(BaseDirectory, "backups");
        Directory.CreateDirectory(backupDirectory);
        return Path.Combine(backupDirectory, $"control-os-lunix-backup-{DateTime.Now:yyyyMMdd-HHmmss}.zip");
    }

    public static void EnsureBaseDirectoryExists()
    {
        Directory.CreateDirectory(BaseDirectory);
    }
}
