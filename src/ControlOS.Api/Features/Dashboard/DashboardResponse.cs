using ControlOS.Api.Features.Devices;

namespace ControlOS.Api.Features.Dashboard;

public sealed record DashboardResponse(
    DashboardSummaryDto Summary,
    IReadOnlyList<DeviceDto> Devices,
    IReadOnlyList<string> RecentLogs);
