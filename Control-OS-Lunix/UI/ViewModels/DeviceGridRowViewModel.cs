using Control_OS_Lunix.Backend.Models;

namespace Control_OS_Lunix.UI.ViewModels;

public sealed class DeviceGridRowViewModel
{
    public required Guid DeviceId { get; init; }

    public required string Name { get; init; }

    public required string IpAddress { get; init; }

    public required DevicePowerStatus Status { get; init; }

    public required bool AutoStartEnabled { get; init; }

    public required bool AutoShutdownEnabled { get; init; }

    public required string LastOperationSummary { get; init; }

    public required string UpdatedText { get; init; }
}
