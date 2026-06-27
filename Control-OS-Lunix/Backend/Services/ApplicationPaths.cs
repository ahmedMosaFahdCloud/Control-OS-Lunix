namespace Control_OS_Lunix.Backend.Services;

public static class ApplicationPaths
{
    public static string BaseDirectory =>
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Control-OS-Lunix");

    public static string ConfigurationFilePath => Path.Combine(BaseDirectory, "config.json");

    public static string LogFilePath => Path.Combine(BaseDirectory, "activity.log");
}
