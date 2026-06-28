using System.Text.Json.Serialization;

namespace ControlOS.Api.Backend.Models;

public sealed class DevicePowerConfig
{
    public Guid DeviceId { get; set; } = Guid.NewGuid();

    public string Name { get; set; } = string.Empty;

    public string IpAddress { get; set; } = string.Empty;

    public string MacAddress { get; set; } = string.Empty;

    public string BroadcastAddress { get; set; } = "255.255.255.255";

    public int WolPort { get; set; } = 9;

    public string SshHost { get; set; } = string.Empty;

    public int SshPort { get; set; } = 22;

    public string SshUsername { get; set; } = string.Empty;

    public string ProtectedSshPassword { get; set; } = string.Empty;

    [JsonIgnore]
    public string SshPassword { get; set; } = string.Empty;

    public DeviceOperatingSystemType OperatingSystemType { get; set; } = DeviceOperatingSystemType.Linux;

    public bool AutoStartEnabled { get; set; } = true;

    public bool AutoShutdownEnabled { get; set; } = true;

    public bool ManualControlEnabled { get; set; } = true;

    public int TimeoutSeconds { get; set; } = 15;

    public int RetryCount { get; set; } = 1;

    public string Description { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;

    public DateTime CreatedDateUtc { get; set; } = DateTime.UtcNow;

    public DateTime LastUpdatedDateUtc { get; set; } = DateTime.UtcNow;

    public DevicePowerStatus LastKnownStatus { get; set; } = DevicePowerStatus.Unknown;

    public string LastOperationSummary { get; set; } = "No operations yet";

    public DevicePowerConfig Clone()
    {
        return new DevicePowerConfig
        {
            DeviceId = DeviceId,
            Name = Name,
            IpAddress = IpAddress,
            MacAddress = MacAddress,
            BroadcastAddress = BroadcastAddress,
            WolPort = WolPort,
            SshHost = SshHost,
            SshPort = SshPort,
            SshUsername = SshUsername,
            ProtectedSshPassword = ProtectedSshPassword,
            SshPassword = SshPassword,
            OperatingSystemType = OperatingSystemType,
            AutoStartEnabled = AutoStartEnabled,
            AutoShutdownEnabled = AutoShutdownEnabled,
            ManualControlEnabled = ManualControlEnabled,
            TimeoutSeconds = TimeoutSeconds,
            RetryCount = RetryCount,
            Description = Description,
            IsActive = IsActive,
            CreatedDateUtc = CreatedDateUtc,
            LastUpdatedDateUtc = LastUpdatedDateUtc,
            LastKnownStatus = LastKnownStatus,
            LastOperationSummary = LastOperationSummary
        };
    }
}
