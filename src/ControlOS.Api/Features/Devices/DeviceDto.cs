using ControlOS.Api.Features.Shared.Models;

namespace ControlOS.Api.Features.Devices;

public sealed record DeviceDto(
    Guid DeviceId,
    string Name,
    string IpAddress,
    string MacAddress,
    string BroadcastAddress,
    int WolPort,
    string SshHost,
    int SshPort,
    string SshUsername,
    bool HasSshPassword,
    DeviceOperatingSystemType OperatingSystemType,
    bool AutoStartEnabled,
    bool AutoShutdownEnabled,
    bool ManualControlEnabled,
    int TimeoutSeconds,
    int RetryCount,
    string Description,
    bool IsActive,
    DevicePowerStatus LastKnownStatus,
    string LastOperationSummary,
    DateTime CreatedDateUtc,
    DateTime LastUpdatedDateUtc);
