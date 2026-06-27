using System.Text.Json;
using Control_OS_Lunix.Model;

namespace Control_OS_Lunix.Service;

public sealed class JsonConfigurationStore
{
    private readonly string _configurationFilePath;
    private readonly CredentialProtector _credentialProtector;
    private readonly JsonSerializerOptions _serializerOptions = new()
    {
        WriteIndented = true
    };

    public JsonConfigurationStore(string configurationFilePath, CredentialProtector credentialProtector)
    {
        _configurationFilePath = configurationFilePath;
        _credentialProtector = credentialProtector;
    }

    public AppConfiguration Load()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(_configurationFilePath)!);

        if (!File.Exists(_configurationFilePath))
        {
            AppConfiguration defaultConfiguration = CreateDefaultConfiguration();
            Save(defaultConfiguration);
            return defaultConfiguration;
        }

        string json = File.ReadAllText(_configurationFilePath);
        AppConfiguration configuration = JsonSerializer.Deserialize<AppConfiguration>(json, _serializerOptions)
            ?? CreateDefaultConfiguration();

        configuration.GlobalSettings ??= new GlobalSettings();
        configuration.Devices ??= [];

        foreach (DevicePowerConfig device in configuration.Devices)
        {
            device.SshPassword = _credentialProtector.Unprotect(device.ProtectedSshPassword);
            device.SshHost = string.IsNullOrWhiteSpace(device.SshHost) ? device.IpAddress : device.SshHost;
            device.BroadcastAddress = string.IsNullOrWhiteSpace(device.BroadcastAddress)
                ? configuration.GlobalSettings.DefaultBroadcastAddress
                : device.BroadcastAddress;
            device.WolPort = device.WolPort <= 0 ? configuration.GlobalSettings.DefaultWolPort : device.WolPort;
        }

        return configuration;
    }

    public void Save(AppConfiguration configuration)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(_configurationFilePath)!);

        AppConfiguration storageModel = new()
        {
            GlobalSettings = configuration.GlobalSettings,
            Devices = configuration.Devices.Select(device => new DevicePowerConfig
            {
                DeviceId = device.DeviceId,
                Name = device.Name.Trim(),
                IpAddress = device.IpAddress.Trim(),
                MacAddress = string.IsNullOrWhiteSpace(device.MacAddress)
                    ? string.Empty
                    : DeviceValidator.NormalizeMacAddress(device.MacAddress),
                BroadcastAddress = device.BroadcastAddress.Trim(),
                WolPort = device.WolPort,
                SshHost = device.SshHost.Trim(),
                SshPort = device.SshPort,
                SshUsername = device.SshUsername.Trim(),
                ProtectedSshPassword = _credentialProtector.Protect(device.SshPassword),
                OperatingSystemType = device.OperatingSystemType,
                AutoStartEnabled = device.AutoStartEnabled,
                AutoShutdownEnabled = device.AutoShutdownEnabled,
                ManualControlEnabled = device.ManualControlEnabled,
                TimeoutSeconds = device.TimeoutSeconds,
                RetryCount = device.RetryCount,
                Description = device.Description.Trim(),
                IsActive = device.IsActive,
                CreatedDateUtc = device.CreatedDateUtc,
                LastUpdatedDateUtc = device.LastUpdatedDateUtc,
                LastKnownStatus = device.LastKnownStatus,
                LastOperationSummary = device.LastOperationSummary
            }).ToList()
        };

        string json = JsonSerializer.Serialize(storageModel, _serializerOptions);
        File.WriteAllText(_configurationFilePath, json);
    }

    private static AppConfiguration CreateDefaultConfiguration()
    {
        return new AppConfiguration
        {
            GlobalSettings = new GlobalSettings(),
            Devices = []
        };
    }
}
