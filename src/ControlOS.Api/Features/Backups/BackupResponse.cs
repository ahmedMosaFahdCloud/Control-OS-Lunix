namespace ControlOS.Api.Features.Backups;

public sealed record BackupResponse(
    string ArchivePath,
    string Message);
