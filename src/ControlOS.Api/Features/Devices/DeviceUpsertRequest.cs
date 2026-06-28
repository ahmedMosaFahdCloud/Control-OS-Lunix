using ControlOS.Api.Backend.Models;

namespace ControlOS.Api.Features.Devices;

public sealed record DeviceUpsertRequest(
    string Name,
    string IpAddress,
    string MacAddress,
    string BroadcastAddress,
    int WolPort,
    string SshHost,
    int SshPort,
    string SshUsername,
    string SshPassword,
    DeviceOperatingSystemType OperatingSystemType,
    bool AutoStartEnabled,
    bool AutoShutdownEnabled,
    bool ManualControlEnabled,
    int TimeoutSeconds,
    int RetryCount,
    string Description,
    bool IsActive);
