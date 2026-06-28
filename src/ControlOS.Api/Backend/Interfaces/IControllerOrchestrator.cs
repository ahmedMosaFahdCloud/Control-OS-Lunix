using ControlOS.Api.Backend.Models;
using ControlOS.Api.Backend.Results;

namespace ControlOS.Api.Backend.Interfaces;

public interface IControllerOrchestrator
{
    Task<Result<DeviceOperationResult>> ExecuteOperationAsync(
        DevicePowerConfig device,
        GlobalSettings settings,
        DevicePowerOperation operation,
        OperationTrigger trigger,
        CancellationToken cancellationToken = default);

    Task<Result<IReadOnlyList<DeviceOperationResult>>> ExecuteControllerStartupAsync(
        IEnumerable<DevicePowerConfig> devices,
        GlobalSettings settings,
        CancellationToken cancellationToken = default);

    Task<Result<IReadOnlyList<DeviceOperationResult>>> ExecuteControllerShutdownAsync(
        IEnumerable<DevicePowerConfig> devices,
        GlobalSettings settings,
        CancellationToken cancellationToken = default);
}
