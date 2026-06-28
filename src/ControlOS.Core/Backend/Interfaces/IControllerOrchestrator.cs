using Control_OS_Lunix.Backend.Models;
using Control_OS_Lunix.Backend.Results;

namespace Control_OS_Lunix.Backend.Interfaces;

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
