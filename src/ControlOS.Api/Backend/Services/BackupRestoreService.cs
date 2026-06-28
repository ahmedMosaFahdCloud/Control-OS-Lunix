using System.IO.Compression;
using ControlOS.Api.Backend.Interfaces;
using ControlOS.Api.Backend.Results;

namespace ControlOS.Api.Backend.Services;

public sealed class BackupRestoreService : IBackupRestoreService
{
    public Result CreateBackup(string destinationArchivePath)
    {
        if (string.IsNullOrWhiteSpace(destinationArchivePath))
        {
            return Result.Failure("backup.invalid_path", "A valid backup file path is required.");
        }

        try
        {
            ApplicationPaths.EnsureBaseDirectoryExists();

            string? destinationDirectory = Path.GetDirectoryName(destinationArchivePath);
            if (string.IsNullOrWhiteSpace(destinationDirectory))
            {
                return Result.Failure("backup.invalid_directory", "The selected backup folder is invalid.");
            }

            Directory.CreateDirectory(destinationDirectory);

            if (File.Exists(destinationArchivePath))
            {
                File.Delete(destinationArchivePath);
            }

            using ZipArchive archive = ZipFile.Open(destinationArchivePath, ZipArchiveMode.Create);
            foreach (string filePath in Directory.GetFiles(ApplicationPaths.BaseDirectory))
            {
                archive.CreateEntryFromFile(filePath, Path.GetFileName(filePath), CompressionLevel.Optimal);
            }

            return Result.Success("Backup completed successfully.");
        }
        catch (Exception exception)
        {
            return Result.Failure("backup.failed", $"Backup failed. {exception.Message}");
        }
    }

    public Result RestoreBackup(string sourceArchivePath)
    {
        if (string.IsNullOrWhiteSpace(sourceArchivePath) || !File.Exists(sourceArchivePath))
        {
            return Result.Failure("restore.file_not_found", "The selected backup file could not be found.");
        }

        string tempDirectory = Path.Combine(Path.GetTempPath(), "Control-OS-Lunix-Restore", Guid.NewGuid().ToString("N"));

        try
        {
            Directory.CreateDirectory(tempDirectory);
            ZipFile.ExtractToDirectory(sourceArchivePath, tempDirectory);

            string extractedConfigPath = Path.Combine(tempDirectory, Path.GetFileName(ApplicationPaths.ConfigurationFilePath));
            if (!File.Exists(extractedConfigPath))
            {
                return Result.Failure("restore.invalid_backup", "The backup file is missing the application configuration data.");
            }

            ApplicationPaths.EnsureBaseDirectoryExists();

            foreach (string filePath in Directory.GetFiles(tempDirectory))
            {
                string destinationPath = Path.Combine(ApplicationPaths.BaseDirectory, Path.GetFileName(filePath));
                File.Copy(filePath, destinationPath, overwrite: true);
            }

            return Result.Success("Backup restored successfully.");
        }
        catch (InvalidDataException)
        {
            return Result.Failure("restore.invalid_archive", "The selected file is not a valid backup archive.");
        }
        catch (Exception exception)
        {
            return Result.Failure("restore.failed", $"Restore failed. {exception.Message}");
        }
        finally
        {
            if (Directory.Exists(tempDirectory))
            {
                Directory.Delete(tempDirectory, recursive: true);
            }
        }
    }
}
