using ControlOS.Api.Backend.Models;
using ControlOS.Api.Backend.Results;

namespace ControlOS.Api.Backend.Interfaces;

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
