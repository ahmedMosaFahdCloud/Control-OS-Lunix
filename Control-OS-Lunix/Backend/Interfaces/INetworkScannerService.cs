using Control_OS_Lunix.Backend.Models;
using Control_OS_Lunix.Backend.Results;

namespace Control_OS_Lunix.Backend.Interfaces;

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
