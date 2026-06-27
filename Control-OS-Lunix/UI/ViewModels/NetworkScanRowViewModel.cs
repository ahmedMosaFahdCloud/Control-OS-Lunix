namespace Control_OS_Lunix.UI.ViewModels;

public sealed class NetworkScanRowViewModel
{
    public required string IpAddress { get; init; }

    public required string HostName { get; init; }

    public required string MacAddress { get; init; }

    public required string ResponseTimeText { get; init; }

    public required string Summary { get; init; }
}
