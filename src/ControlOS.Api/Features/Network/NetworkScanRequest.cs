namespace ControlOS.Api.Features.Network;

public sealed record NetworkScanRequest(
    string SubnetPrefix,
    int StartHost,
    int EndHost,
    int TimeoutMs,
    int MaxConcurrency);
