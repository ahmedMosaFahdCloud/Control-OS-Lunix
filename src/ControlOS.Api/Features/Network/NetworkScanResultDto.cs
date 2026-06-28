namespace ControlOS.Api.Features.Network;

public sealed record NetworkScanResultDto(
    string IpAddress,
    string HostName,
    string MacAddress,
    bool IsOnline,
    long ResponseTimeMs,
    string Summary);
