using ControlOS.Api.Backend.Results;

namespace ControlOS.Api.Backend.Interfaces;

public interface IBackupRestoreService
{
    Result CreateBackup(string destinationArchivePath);

    Result RestoreBackup(string sourceArchivePath);
}
