namespace ControlOS.Api.Backend.Models;

public sealed class NetworkScanResult
{
    public string IpAddress { get; init; } = string.Empty;

    public string HostName { get; init; } = string.Empty;

    public string MacAddress { get; init; } = string.Empty;

    public bool IsOnline { get; init; }

    public long ResponseTimeMs { get; init; }

    public string Summary =>
        IsOnline
            ? $"{(string.IsNullOrWhiteSpace(HostName) ? "Reachable host" : HostName)} responded in {ResponseTimeMs} ms"
            : "No response";
}
