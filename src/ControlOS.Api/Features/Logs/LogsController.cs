using ControlOS.Api.Features.Shared;
using Microsoft.AspNetCore.Mvc;

namespace ControlOS.Api.Features.Logs;

[Route("api/logs")]
public sealed class LogsController : ApiControllerBase
{
    private readonly ControlCenterService _service;

    public LogsController(ControlCenterService service) => _service = service;

    [HttpGet]
    public async Task<IActionResult> Get([FromQuery] int lines = 100, CancellationToken cancellationToken = default)
    {
        var result = await _service.GetLogsAsync(Math.Clamp(lines, 10, 1000), cancellationToken);
        return OkOrProblem(result, "Logs request failed.");
    }
}
