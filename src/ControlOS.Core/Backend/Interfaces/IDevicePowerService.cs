using Control_OS_Lunix.Backend.Models;
using Control_OS_Lunix.Backend.Results;

namespace Control_OS_Lunix.Backend.Interfaces;

public interface IDevicePowerService
{
    Task<Result<DevicePowerReport>> GetReportAsync(
        DevicePowerConfig device,
        int pingTimeoutSeconds,
        CancellationToken cancellationToken = default);

    Task<Result> SendWakeOnLanAsync(DevicePowerConfig device, CancellationToken cancellationToken = default);

    Task<Result> ShutdownAsync(DevicePowerConfig device, int timeoutSeconds, CancellationToken cancellationToken = default);

    Task<Result> RebootAsync(DevicePowerConfig device, int timeoutSeconds, CancellationToken cancellationToken = default);
}
