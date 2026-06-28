namespace ControlOS.Api.Features.Dashboard;

public sealed record DashboardSummaryDto(
    int TotalDevices,
    int OnlineDevices,
    int ActiveDevices,
    string LastAction);
