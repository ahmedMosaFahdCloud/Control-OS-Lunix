using ControlOS.Api.Backend.Models;
using ControlOS.Api.Backend.Results;

namespace ControlOS.Api.Backend.Interfaces;

public interface ILogService
{
    Task<Result> WriteAsync(OperationLogEntry entry, CancellationToken cancellationToken = default);

    Task<Result<IReadOnlyList<string>>> ReadRecentLinesAsync(int maxLines, CancellationToken cancellationToken = default);

    Result EnforceRetention(int retentionDays);
}
