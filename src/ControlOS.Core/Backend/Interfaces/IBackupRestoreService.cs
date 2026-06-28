using Control_OS_Lunix.Backend.Results;

namespace Control_OS_Lunix.Backend.Interfaces;

public interface IBackupRestoreService
{
    Result CreateBackup(string destinationArchivePath);

    Result RestoreBackup(string sourceArchivePath);
}
