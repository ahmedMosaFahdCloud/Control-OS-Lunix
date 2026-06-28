using ControlOS.Api.Backend.Models;
using ControlOS.Api.Backend.Results;

namespace ControlOS.Api.Backend.Interfaces;

public interface INetworkScannerService
{
    Result<string> GetSuggestedSubnet();

    Task<Result<IReadOnlyList<NetworkScanResult>>> ScanSubnetAsync(
        string subnetPrefix,
        int startHost,
        int endHost,
        int timeoutMs,
        int maxConcurrency,
        IProgress<int>? progress = null,
        CancellationToken cancellationToken = default);
}
