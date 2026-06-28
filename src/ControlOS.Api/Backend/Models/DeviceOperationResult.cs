namespace ControlOS.Api.Backend.Models;

public sealed class DeviceOperationResult
{
    public required DevicePowerConfig Device { get; init; }

    public required DevicePowerOperation Operation { get; init; }

    public required bool IsSuccess { get; init; }

    public bool HasWarning { get; init; }

    public required string Message { get; init; }

    public required DevicePowerStatus StatusAfterOperation { get; init; }
}
