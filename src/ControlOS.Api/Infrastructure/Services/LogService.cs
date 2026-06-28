using System.Text;
using ControlOS.Api.Features.Shared;
using ControlOS.Api.Features.Shared.Models;

namespace ControlOS.Api.Infrastructure.Services;

public sealed class LogService
{
    public LogService()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(ApplicationPaths.LogFilePath)!);
    }

    public async Task<Result> WriteAsync(OperationLogEntry entry, CancellationToken cancellationToken = default)
    {
        try
        {
            string line =
                $"[{entry.FinishedAtUtc.ToLocalTime():yyyy-MM-dd HH:mm:ss}] " +
                $"[{entry.TriggeredBy}] [{entry.Status}] [{entry.OperationType}] {entry.DeviceName} - {entry.Summary}";

            if (!string.IsNullOrWhiteSpace(entry.ErrorMessage))
            {
                line += $" Error: {entry.ErrorMessage}";
            }

            await File.AppendAllTextAsync(ApplicationPaths.LogFilePath, line + Environment.NewLine, Encoding.UTF8, cancellationToken);
            return Result.Success();
        }
        catch (Exception exception)
        {
            return Result.Failure("log.write.failed", exception.Message);
        }
    }

    public async Task<Result<IReadOnlyList<string>>> ReadRecentLinesAsync(int maxLines, CancellationToken cancellationToken = default)
    {
        try
        {
            if (!File.Exists(ApplicationPaths.LogFilePath))
            {
                return Result<IReadOnlyList<string>>.Success([]);
            }

            string[] lines = await File.ReadAllLinesAsync(ApplicationPaths.LogFilePath, cancellationToken);
            return Result<IReadOnlyList<string>>.Success(lines.TakeLast(maxLines).ToArray());
        }
        catch (Exception exception)
        {
            return Result<IReadOnlyList<string>>.Failure("log.read.failed", exception.Message);
        }
    }

    public Result EnforceRetention(int retentionDays)
    {
        try
        {
            if (!File.Exists(ApplicationPaths.LogFilePath) || retentionDays <= 0)
            {
                return Result.Success();
            }

            DateTime cutoff = DateTime.Now.AddDays(-retentionDays);
            string[] retainedLines = File.ReadLines(ApplicationPaths.LogFilePath)
                .Where(line => !TryParseTimestamp(line, out DateTime timestamp) || timestamp >= cutoff)
                .ToArray();

            File.WriteAllLines(ApplicationPaths.LogFilePath, retainedLines, Encoding.UTF8);
            return Result.Success();
        }
        catch (Exception exception)
        {
            return Result.Failure("log.retention.failed", exception.Message);
        }
    }

    private static bool TryParseTimestamp(string line, out DateTime timestamp)
    {
        timestamp = default;

        if (line.Length < 21 || line[0] != '[')
        {
            return false;
        }

        return DateTime.TryParseExact(
            line[1..20],
            "yyyy-MM-dd HH:mm:ss",
            provider: null,
            System.Globalization.DateTimeStyles.None,
            out timestamp);
    }
}
