using ControlOS.Api.Features.Shared.Models;
using ControlOS.Api.Features.Dashboard;
using ControlOS.Api.Features.Devices;
using ControlOS.Api.Features.Settings;
using ControlOS.Api.Features.Logs;
using ControlOS.Api.Features.Network;
using ControlOS.Api.Features.Backups;
using ControlOS.Api.Infrastructure.Services;

namespace ControlOS.Api.Features.Shared;

public sealed class ControlCenterService
{
    private readonly JsonConfigurationStore _configurationStore;
    private readonly DeviceValidatorService _deviceValidator;
    private readonly DevicePowerService _devicePowerService;
    private readonly ControllerOrchestrator _controllerOrchestrator;
    private readonly NetworkScannerService _networkScannerService;
    private readonly LogService _logService;
    private readonly BackupRestoreService _backupRestoreService;
    private readonly SemaphoreSlim _syncLock = new(1, 1);

    public ControlCenterService(
        JsonConfigurationStore configurationStore,
        DeviceValidatorService deviceValidator,
        DevicePowerService devicePowerService,
        ControllerOrchestrator controllerOrchestrator,
        NetworkScannerService networkScannerService,
        LogService logService,
        BackupRestoreService backupRestoreService)
    {
        _configurationStore = configurationStore;
        _deviceValidator = deviceValidator;
        _devicePowerService = devicePowerService;
        _controllerOrchestrator = controllerOrchestrator;
        _networkScannerService = networkScannerService;
        _logService = logService;
        _backupRestoreService = backupRestoreService;
    }

    public async Task<Result<DashboardResponse>> GetDashboardAsync(bool refreshStatuses, int logLines, CancellationToken cancellationToken)
    {
        await _syncLock.WaitAsync(cancellationToken);
        try
        {
            Result<AppConfiguration> loadResult = LoadConfiguration();
            if (!loadResult.IsSuccess || loadResult.Value is null)
            {
                return Result<DashboardResponse>.Failure(loadResult.ErrorCode, loadResult.Message);
            }

            AppConfiguration configuration = loadResult.Value;
            if (refreshStatuses)
            {
                await RefreshStatusesAsync(configuration, cancellationToken);
                Result saveResult = SaveConfiguration(configuration);
                if (!saveResult.IsSuccess)
                {
                    return Result<DashboardResponse>.Failure(saveResult.ErrorCode, saveResult.Message);
                }
            }

            Result<IReadOnlyList<string>> logsResult = await _logService.ReadRecentLinesAsync(logLines, cancellationToken);
            if (!logsResult.IsSuccess || logsResult.Value is null)
            {
                return Result<DashboardResponse>.Failure(logsResult.ErrorCode, logsResult.Message);
            }

            return Result<DashboardResponse>.Success(new DashboardResponse(
                BuildSummary(configuration),
                configuration.Devices
                    .OrderBy(device => device.Name, StringComparer.OrdinalIgnoreCase)
                    .Select(MapDevice)
                    .ToArray(),
                logsResult.Value));
        }
        finally
        {
            _syncLock.Release();
        }
    }

    public Result<IReadOnlyList<DeviceDto>> GetDevices()
    {
        Result<AppConfiguration> loadResult = LoadConfiguration();
        if (!loadResult.IsSuccess || loadResult.Value is null)
        {
            return Result<IReadOnlyList<DeviceDto>>.Failure(loadResult.ErrorCode, loadResult.Message);
        }

        return Result<IReadOnlyList<DeviceDto>>.Success(loadResult.Value.Devices
            .OrderBy(device => device.Name, StringComparer.OrdinalIgnoreCase)
            .Select(MapDevice)
            .ToArray());
    }

    public async Task<Result<DeviceDto>> SaveDeviceAsync(Guid? deviceId, DeviceUpsertRequest request, CancellationToken cancellationToken)
    {
        await _syncLock.WaitAsync(cancellationToken);
        try
        {
            Result<AppConfiguration> loadResult = LoadConfiguration();
            if (!loadResult.IsSuccess || loadResult.Value is null)
            {
                return Result<DeviceDto>.Failure(loadResult.ErrorCode, loadResult.Message);
            }

            AppConfiguration configuration = loadResult.Value;
            DevicePowerConfig? existing = deviceId.HasValue
                ? configuration.Devices.FirstOrDefault(device => device.DeviceId == deviceId.Value)
                : null;

            if (deviceId.HasValue && existing is null)
            {
                return Result<DeviceDto>.Failure("device.not_found", "The selected device could not be found.");
            }

            DevicePowerConfig device = BuildDevice(existing, request, configuration.GlobalSettings);
            Result<DevicePowerConfig> validationResult = _deviceValidator.ValidateForSave(device);
            if (!validationResult.IsSuccess || validationResult.Value is null)
            {
                return Result<DeviceDto>.Failure(validationResult.ErrorCode, validationResult.Message);
            }

            if (existing is null)
            {
                configuration.Devices.Add(device);
            }
            else
            {
                int index = configuration.Devices.FindIndex(entry => entry.DeviceId == existing.DeviceId);
                configuration.Devices[index] = device;
            }

            Result saveResult = SaveConfiguration(configuration);
            if (!saveResult.IsSuccess)
            {
                return Result<DeviceDto>.Failure(saveResult.ErrorCode, saveResult.Message);
            }

            return Result<DeviceDto>.Success(MapDevice(device));
        }
        finally
        {
            _syncLock.Release();
        }
    }

    public async Task<Result> DeleteDeviceAsync(Guid deviceId, CancellationToken cancellationToken)
    {
        await _syncLock.WaitAsync(cancellationToken);
        try
        {
            Result<AppConfiguration> loadResult = LoadConfiguration();
            if (!loadResult.IsSuccess || loadResult.Value is null)
            {
                return Result.Failure(loadResult.ErrorCode, loadResult.Message);
            }

            bool removed = loadResult.Value.Devices.RemoveAll(device => device.DeviceId == deviceId) > 0;
            if (!removed)
            {
                return Result.Failure("device.not_found", "The selected device could not be found.");
            }

            return SaveConfiguration(loadResult.Value);
        }
        finally
        {
            _syncLock.Release();
        }
    }

    public async Task<Result<OperationResponse>> ExecuteOperationAsync(Guid deviceId, DevicePowerOperation operation, CancellationToken cancellationToken)
    {
        await _syncLock.WaitAsync(cancellationToken);
        try
        {
            Result<AppConfiguration> loadResult = LoadConfiguration();
            if (!loadResult.IsSuccess || loadResult.Value is null)
            {
                return Result<OperationResponse>.Failure(loadResult.ErrorCode, loadResult.Message);
            }

            AppConfiguration configuration = loadResult.Value;
            DevicePowerConfig? device = configuration.Devices.FirstOrDefault(entry => entry.DeviceId == deviceId);
            if (device is null)
            {
                return Result<OperationResponse>.Failure("device.not_found", "The selected device could not be found.");
            }

            Result<DeviceOperationResult> operationResult = await _controllerOrchestrator.ExecuteOperationAsync(
                device,
                configuration.GlobalSettings,
                operation,
                OperationTrigger.Manual,
                cancellationToken);

            if (!operationResult.IsSuccess || operationResult.Value is null)
            {
                return Result<OperationResponse>.Failure(operationResult.ErrorCode, operationResult.Message);
            }

            Result saveResult = SaveConfiguration(configuration);
            if (!saveResult.IsSuccess)
            {
                return Result<OperationResponse>.Failure(saveResult.ErrorCode, saveResult.Message);
            }

            return Result<OperationResponse>.Success(new OperationResponse(
                operationResult.Value.Device.DeviceId,
                operationResult.Value.Device.Name,
                operationResult.Value.Operation,
                operationResult.Value.IsSuccess,
                operationResult.Value.HasWarning,
                operationResult.Value.Message,
                operationResult.Value.StatusAfterOperation));
        }
        finally
        {
            _syncLock.Release();
        }
    }

    public Result<GlobalSettingsDto> GetSettings()
    {
        Result<AppConfiguration> loadResult = LoadConfiguration();
        if (!loadResult.IsSuccess || loadResult.Value is null)
        {
            return Result<GlobalSettingsDto>.Failure(loadResult.ErrorCode, loadResult.Message);
        }

        return Result<GlobalSettingsDto>.Success(MapSettings(loadResult.Value.GlobalSettings));
    }

    public async Task<Result<GlobalSettingsDto>> SaveSettingsAsync(GlobalSettingsDto request, CancellationToken cancellationToken)
    {
        await _syncLock.WaitAsync(cancellationToken);
        try
        {
            Result<AppConfiguration> loadResult = LoadConfiguration();
            if (!loadResult.IsSuccess || loadResult.Value is null)
            {
                return Result<GlobalSettingsDto>.Failure(loadResult.ErrorCode, loadResult.Message);
            }

            loadResult.Value.GlobalSettings = new GlobalSettings
            {
                AutoStartDevicesOnControllerBoot = request.AutoStartDevicesOnControllerBoot,
                AutoShutdownDevicesOnControllerShutdown = request.AutoShutdownDevicesOnControllerShutdown,
                DelayBetweenCommandsMs = request.DelayBetweenCommandsMs,
                PingTimeoutSeconds = request.PingTimeoutSeconds,
                SshTimeoutSeconds = request.SshTimeoutSeconds,
                RetryCount = request.RetryCount,
                DefaultWolPort = request.DefaultWolPort,
                DefaultBroadcastAddress = request.DefaultBroadcastAddress,
                EnableLogs = request.EnableLogs,
                LogRetentionDays = request.LogRetentionDays,
                ConfirmManualShutdown = request.ConfirmManualShutdown
            };

            _logService.EnforceRetention(loadResult.Value.GlobalSettings.LogRetentionDays);
            Result saveResult = SaveConfiguration(loadResult.Value);
            if (!saveResult.IsSuccess)
            {
                return Result<GlobalSettingsDto>.Failure(saveResult.ErrorCode, saveResult.Message);
            }

            return Result<GlobalSettingsDto>.Success(request);
        }
        finally
        {
            _syncLock.Release();
        }
    }

    public async Task<Result<IReadOnlyList<NetworkScanResultDto>>> ScanAsync(NetworkScanRequest request, CancellationToken cancellationToken)
    {
        Result<IReadOnlyList<NetworkScanResult>> result = await _networkScannerService.ScanSubnetAsync(
            request.SubnetPrefix,
            request.StartHost,
            request.EndHost,
            request.TimeoutMs,
            request.MaxConcurrency,
            progress: null,
            cancellationToken);

        if (!result.IsSuccess || result.Value is null)
        {
            return Result<IReadOnlyList<NetworkScanResultDto>>.Failure(result.ErrorCode, result.Message);
        }

        return Result<IReadOnlyList<NetworkScanResultDto>>.Success(result.Value
            .Select(entry => new NetworkScanResultDto(
                entry.IpAddress,
                entry.HostName,
                entry.MacAddress,
                entry.IsOnline,
                entry.ResponseTimeMs,
                entry.Summary))
            .ToArray());
    }

    public async Task<Result<LogsResponse>> GetLogsAsync(int lines, CancellationToken cancellationToken)
    {
        Result<IReadOnlyList<string>> logsResult = await _logService.ReadRecentLinesAsync(lines, cancellationToken);
        return !logsResult.IsSuccess || logsResult.Value is null
            ? Result<LogsResponse>.Failure(logsResult.ErrorCode, logsResult.Message)
            : Result<LogsResponse>.Success(new LogsResponse(logsResult.Value));
    }

    public Result<BackupResponse> CreateBackup()
    {
        string archivePath = ApplicationPaths.CreateDefaultBackupFilePath();
        Result backupResult = _backupRestoreService.CreateBackup(archivePath);
        return !backupResult.IsSuccess
            ? Result<BackupResponse>.Failure(backupResult.ErrorCode, backupResult.Message)
            : Result<BackupResponse>.Success(new BackupResponse(archivePath, backupResult.Message));
    }

    public async Task<Result> RestoreBackupAsync(string archivePath, CancellationToken cancellationToken)
    {
        await _syncLock.WaitAsync(cancellationToken);
        try
        {
            Result restoreResult = _backupRestoreService.RestoreBackup(archivePath);
            if (!restoreResult.IsSuccess)
            {
                return restoreResult;
            }

            Result<AppConfiguration> loadResult = LoadConfiguration();
            if (loadResult.IsSuccess && loadResult.Value is not null)
            {
                _logService.EnforceRetention(loadResult.Value.GlobalSettings.LogRetentionDays);
            }

            return restoreResult;
        }
        finally
        {
            _syncLock.Release();
        }
    }

    private Result<AppConfiguration> LoadConfiguration()
    {
        Result<AppConfiguration> loadResult = _configurationStore.Load();
        if (loadResult.IsSuccess && loadResult.Value is not null)
        {
            _logService.EnforceRetention(loadResult.Value.GlobalSettings.LogRetentionDays);
        }

        return loadResult;
    }

    private Result SaveConfiguration(AppConfiguration configuration)
    {
        return _configurationStore.Save(configuration);
    }

    private async Task RefreshStatusesAsync(AppConfiguration configuration, CancellationToken cancellationToken)
    {
        foreach (DevicePowerConfig device in configuration.Devices)
        {
            Result<DevicePowerReport> report = await _devicePowerService.GetReportAsync(
                device,
                configuration.GlobalSettings.PingTimeoutSeconds,
                cancellationToken);

            device.LastKnownStatus = report.IsSuccess && report.Value is not null
                ? report.Value.Status
                : DevicePowerStatus.Unknown;
            device.LastUpdatedDateUtc = report.IsSuccess && report.Value is not null
                ? report.Value.CheckedAtUtc
                : DateTime.UtcNow;
        }
    }

    private static DevicePowerConfig BuildDevice(DevicePowerConfig? existing, DeviceUpsertRequest request, GlobalSettings settings)
    {
        return new DevicePowerConfig
        {
            DeviceId = existing?.DeviceId ?? Guid.NewGuid(),
            Name = request.Name,
            IpAddress = request.IpAddress,
            MacAddress = request.MacAddress,
            BroadcastAddress = string.IsNullOrWhiteSpace(request.BroadcastAddress)
                ? settings.DefaultBroadcastAddress
                : request.BroadcastAddress,
            WolPort = request.WolPort <= 0 ? settings.DefaultWolPort : request.WolPort,
            SshHost = string.IsNullOrWhiteSpace(request.SshHost) ? request.IpAddress : request.SshHost,
            SshPort = request.SshPort <= 0 ? 22 : request.SshPort,
            SshUsername = request.SshUsername,
            SshPassword = string.IsNullOrWhiteSpace(request.SshPassword)
                ? existing?.SshPassword ?? string.Empty
                : request.SshPassword,
            ProtectedSshPassword = existing?.ProtectedSshPassword ?? string.Empty,
            OperatingSystemType = request.OperatingSystemType,
            AutoStartEnabled = request.AutoStartEnabled,
            AutoShutdownEnabled = request.AutoShutdownEnabled,
            ManualControlEnabled = request.ManualControlEnabled,
            TimeoutSeconds = request.TimeoutSeconds <= 0 ? 15 : request.TimeoutSeconds,
            RetryCount = request.RetryCount <= 0 ? 1 : request.RetryCount,
            Description = request.Description,
            IsActive = request.IsActive,
            CreatedDateUtc = existing?.CreatedDateUtc ?? DateTime.UtcNow,
            LastUpdatedDateUtc = DateTime.UtcNow,
            LastKnownStatus = existing?.LastKnownStatus ?? DevicePowerStatus.Unknown,
            LastOperationSummary = existing?.LastOperationSummary ?? "No operations yet"
        };
    }

    private static DeviceDto MapDevice(DevicePowerConfig device)
    {
        return new DeviceDto(
            device.DeviceId,
            device.Name,
            device.IpAddress,
            device.MacAddress,
            device.BroadcastAddress,
            device.WolPort,
            device.SshHost,
            device.SshPort,
            device.SshUsername,
            !string.IsNullOrWhiteSpace(device.SshPassword) || !string.IsNullOrWhiteSpace(device.ProtectedSshPassword),
            device.OperatingSystemType,
            device.AutoStartEnabled,
            device.AutoShutdownEnabled,
            device.ManualControlEnabled,
            device.TimeoutSeconds,
            device.RetryCount,
            device.Description,
            device.IsActive,
            device.LastKnownStatus,
            device.LastOperationSummary,
            device.CreatedDateUtc,
            device.LastUpdatedDateUtc);
    }

    private static GlobalSettingsDto MapSettings(GlobalSettings settings)
    {
        return new GlobalSettingsDto(
            settings.AutoStartDevicesOnControllerBoot,
            settings.AutoShutdownDevicesOnControllerShutdown,
            settings.DelayBetweenCommandsMs,
            settings.PingTimeoutSeconds,
            settings.SshTimeoutSeconds,
            settings.RetryCount,
            settings.DefaultWolPort,
            settings.DefaultBroadcastAddress,
            settings.EnableLogs,
            settings.LogRetentionDays,
            settings.ConfirmManualShutdown);
    }

    private static DashboardSummaryDto BuildSummary(AppConfiguration configuration)
    {
        return new DashboardSummaryDto(
            configuration.Devices.Count,
            configuration.Devices.Count(device => device.LastKnownStatus == DevicePowerStatus.Online),
            configuration.Devices.Count(device => device.IsActive),
            configuration.Devices
                .OrderByDescending(device => device.LastUpdatedDateUtc)
                .Select(device => device.LastOperationSummary)
                .FirstOrDefault() ?? "No activity");
    }
}
