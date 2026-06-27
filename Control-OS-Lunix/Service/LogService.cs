using System.Text;
using Control_OS_Lunix.Model;

namespace Control_OS_Lunix.Service;

public sealed class LogService
{
    private readonly string _logFilePath;

    public LogService(string logFilePath)
    {
        _logFilePath = logFilePath;
        Directory.CreateDirectory(Path.GetDirectoryName(_logFilePath)!);
    }

    public async Task WriteAsync(OperationLogEntry entry, CancellationToken cancellationToken = default)
    {
        string line =
            $"[{entry.FinishedAtUtc.ToLocalTime():yyyy-MM-dd HH:mm:ss}] " +
            $"[{entry.TriggeredBy}] [{entry.Status}] [{entry.OperationType}] {entry.DeviceName} - {entry.Summary}";

        if (!string.IsNullOrWhiteSpace(entry.ErrorMessage))
        {
            line += $" Error: {entry.ErrorMessage}";
        }

        await File.AppendAllTextAsync(_logFilePath, line + Environment.NewLine, Encoding.UTF8, cancellationToken);
    }

    public async Task<IReadOnlyList<string>> ReadRecentLinesAsync(int maxLines, CancellationToken cancellationToken = default)
    {
        if (!File.Exists(_logFilePath))
        {
            return [];
        }

        string[] lines = await File.ReadAllLinesAsync(_logFilePath, cancellationToken);
        return lines.TakeLast(maxLines).ToArray();
    }

    public void EnforceRetention(int retentionDays)
    {
        if (!File.Exists(_logFilePath) || retentionDays <= 0)
        {
            return;
        }

        DateTime cutoff = DateTime.Now.AddDays(-retentionDays);
        string[] retainedLines = File.ReadLines(_logFilePath)
            .Where(line => !TryParseTimestamp(line, out DateTime timestamp) || timestamp >= cutoff)
            .ToArray();

        File.WriteAllLines(_logFilePath, retainedLines, Encoding.UTF8);
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
