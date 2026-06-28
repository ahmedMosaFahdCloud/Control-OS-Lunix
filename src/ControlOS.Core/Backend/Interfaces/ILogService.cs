using Control_OS_Lunix.Backend.Models;
using Control_OS_Lunix.Backend.Results;

namespace Control_OS_Lunix.Backend.Interfaces;

public interface ILogService
{
    Task<Result> WriteAsync(OperationLogEntry entry, CancellationToken cancellationToken = default);

    Task<Result<IReadOnlyList<string>>> ReadRecentLinesAsync(int maxLines, CancellationToken cancellationToken = default);

    Result EnforceRetention(int retentionDays);
}
