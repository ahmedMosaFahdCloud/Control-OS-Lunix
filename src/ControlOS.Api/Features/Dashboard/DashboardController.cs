using ControlOS.Api.Features.Shared;
using Microsoft.AspNetCore.Mvc;

namespace ControlOS.Api.Features.Dashboard;

[Route("api/dashboard")]
public sealed class DashboardController : ApiControllerBase
{
    private readonly ControlCenterService _service;

    public DashboardController(ControlCenterService service) => _service = service;

    [HttpGet]
    public async Task<IActionResult> Get([FromQuery] bool refresh = true, [FromQuery] int logLines = 20, CancellationToken cancellationToken = default)
    {
        var result = await _service.GetDashboardAsync(refresh, Math.Clamp(logLines, 5, 200), cancellationToken);
        return OkOrProblem(result, "Dashboard request failed.");
    }
}
