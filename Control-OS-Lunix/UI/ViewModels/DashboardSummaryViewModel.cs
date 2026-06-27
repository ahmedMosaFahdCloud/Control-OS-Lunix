namespace Control_OS_Lunix.UI.ViewModels;

public sealed class DashboardSummaryViewModel
{
    public string TotalDevices { get; init; } = "0";

    public string OnlineDevices { get; init; } = "0";

    public string ActiveDevices { get; init; } = "0";

    public string LastAction { get; init; } = "No activity";
}
