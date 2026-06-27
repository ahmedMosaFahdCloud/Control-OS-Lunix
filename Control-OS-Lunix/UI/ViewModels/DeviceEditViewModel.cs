using Control_OS_Lunix.Backend.Models;

namespace Control_OS_Lunix.UI.ViewModels;

public sealed class DeviceEditViewModel
{
    public Guid DeviceId { get; set; } = Guid.NewGuid();

    public string Name { get; set; } = string.Empty;

    public string IpAddress { get; set; } = string.Empty;

    public string MacAddress { get; set; } = string.Empty;

    public string BroadcastAddress { get; set; } = string.Empty;

    public int WolPort { get; set; } = 9;

    public string SshHost { get; set; } = string.Empty;

    public int SshPort { get; set; } = 22;

    public string SshUsername { get; set; } = string.Empty;

    public string SshPassword { get; set; } = string.Empty;

    public DeviceOperatingSystemType OperatingSystemType { get; set; } = DeviceOperatingSystemType.Linux;

    public bool AutoStartEnabled { get; set; } = true;

    public bool AutoShutdownEnabled { get; set; } = true;

    public bool ManualControlEnabled { get; set; } = true;

    public bool IsActive { get; set; } = true;

    public int TimeoutSeconds { get; set; } = 15;

    public int RetryCount { get; set; } = 1;

    public string Description { get; set; } = string.Empty;

    public DateTime CreatedDateUtc { get; set; } = DateTime.UtcNow;

    public DateTime LastUpdatedDateUtc { get; set; } = DateTime.UtcNow;

    public DevicePowerStatus LastKnownStatus { get; set; } = DevicePowerStatus.Unknown;

    public string LastOperationSummary { get; set; } = "No operations yet";
}
