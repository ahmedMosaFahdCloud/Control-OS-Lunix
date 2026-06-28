using Control_OS_Lunix.Backend.Models;

namespace ControlOS.Api.Features.Devices;

public sealed record OperationResponse(
    Guid DeviceId,
    string DeviceName,
    DevicePowerOperation Operation,
    bool IsSuccess,
    bool HasWarning,
    string Message,
    DevicePowerStatus StatusAfterOperation);
