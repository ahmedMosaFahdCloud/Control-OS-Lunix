using ControlOS.Api.Features.Shared;
using Microsoft.AspNetCore.Mvc;

namespace ControlOS.Api.Features.Backups;

[Route("api/backups")]
public sealed class BackupsController : ApiControllerBase
{
    private readonly ControlCenterService _service;

    public BackupsController(ControlCenterService service) => _service = service;

    [HttpPost]
    public IActionResult Create()
    {
        var result = _service.CreateBackup();
        return OkOrProblem(result, "Backup creation failed.");
    }

    [HttpPost("restore")]
    public async Task<IActionResult> Restore([FromQuery] string archivePath, CancellationToken cancellationToken)
    {
        var result = await _service.RestoreBackupAsync(archivePath, cancellationToken);
        return OkOrProblem(result, "Backup restore failed.");
    }
}
