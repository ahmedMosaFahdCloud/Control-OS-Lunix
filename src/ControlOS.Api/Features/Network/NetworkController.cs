using ControlOS.Api.Features.Shared;
using Microsoft.AspNetCore.Mvc;

namespace ControlOS.Api.Features.Network;

[Route("api/network")]
public sealed class NetworkController : ApiControllerBase
{
    private readonly ControlCenterService _service;

    public NetworkController(ControlCenterService service) => _service = service;

    [HttpPost("scan")]
    public async Task<IActionResult> Scan([FromBody] NetworkScanRequest request, CancellationToken cancellationToken)
    {
        var result = await _service.ScanAsync(request, cancellationToken);
        return OkOrProblem(result, "Network scan failed.");
    }
}
