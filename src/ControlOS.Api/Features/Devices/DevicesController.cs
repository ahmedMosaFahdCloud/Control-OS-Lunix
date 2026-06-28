using ControlOS.Api.Features.Shared;
using ControlOS.Api.Features.Shared.Models;
using Microsoft.AspNetCore.Mvc;

namespace ControlOS.Api.Features.Devices;

[Route("api/devices")]
public sealed class DevicesController : ApiControllerBase
{
    private readonly ControlCenterService _service;

    public DevicesController(ControlCenterService service) => _service = service;

    [HttpGet]
    public IActionResult Get()
    {
        var result = _service.GetDevices();
        return OkOrProblem(result, "Devices request failed.");
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] DeviceUpsertRequest request, CancellationToken cancellationToken)
    {
        var result = await _service.SaveDeviceAsync(null, request, cancellationToken);
        return OkOrProblem(result, "Device creation failed.");
    }

    [HttpPut("{deviceId:guid}")]
    public async Task<IActionResult> Update(Guid deviceId, [FromBody] DeviceUpsertRequest request, CancellationToken cancellationToken)
    {
        var result = await _service.SaveDeviceAsync(deviceId, request, cancellationToken);
        return OkOrProblem(result, "Device update failed.");
    }

    [HttpDelete("{deviceId:guid}")]
    public async Task<IActionResult> Delete(Guid deviceId, CancellationToken cancellationToken)
    {
        var result = await _service.DeleteDeviceAsync(deviceId, cancellationToken);
        return NoContentOrProblem(result, "Device deletion failed.");
    }

    [HttpPost("{deviceId:guid}/start")]
    public Task<IActionResult> Start(Guid deviceId, CancellationToken cancellationToken) =>
        Execute(deviceId, DevicePowerOperation.Start, cancellationToken);

    [HttpPost("{deviceId:guid}/shutdown")]
    public Task<IActionResult> Shutdown(Guid deviceId, CancellationToken cancellationToken) =>
        Execute(deviceId, DevicePowerOperation.Shutdown, cancellationToken);

    [HttpPost("{deviceId:guid}/reboot")]
    public Task<IActionResult> Reboot(Guid deviceId, CancellationToken cancellationToken) =>
        Execute(deviceId, DevicePowerOperation.Reboot, cancellationToken);

    private async Task<IActionResult> Execute(Guid deviceId, DevicePowerOperation operation, CancellationToken cancellationToken)
    {
        var result = await _service.ExecuteOperationAsync(deviceId, operation, cancellationToken);
        return OkOrProblem(result, "Device operation failed.");
    }
}
