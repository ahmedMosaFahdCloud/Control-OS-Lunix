using ControlOS.Api.Features.Shared;
using Microsoft.AspNetCore.Mvc;

namespace ControlOS.Api.Features.Settings;

[Route("api/settings")]
public sealed class SettingsController : ApiControllerBase
{
    private readonly ControlCenterService _service;

    public SettingsController(ControlCenterService service) => _service = service;

    [HttpGet]
    public IActionResult Get()
    {
        var result = _service.GetSettings();
        return OkOrProblem(result, "Settings request failed.");
    }

    [HttpPut]
    public async Task<IActionResult> Save([FromBody] GlobalSettingsDto request, CancellationToken cancellationToken)
    {
        var result = await _service.SaveSettingsAsync(request, cancellationToken);
        return OkOrProblem(result, "Settings update failed.");
    }
}
