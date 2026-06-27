using Control_OS_Lunix.Backend.Interfaces;
using Control_OS_Lunix.Backend.Models;
using Control_OS_Lunix.Backend.Results;

namespace Control_OS_Lunix.Backend.Services;

public sealed class ControllerOrchestrator : IControllerOrchestrator
{
    private readonly IDevicePowerService _devicePowerService;
    private readonly IDeviceValidator _deviceValidator;
    private readonly ILogService _logService;

    public ControllerOrchestrator(IDevicePowerService devicePowerService, IDeviceValidator deviceValidator, ILogService logService)
    {
        _devicePowerService = devicePowerService;
        _deviceValidator = deviceValidator;
        _logService = logService;
    }

    public async Task<Result<DeviceOperationResult>> ExecuteOperationAsync(
        DevicePowerConfig device,
        GlobalSettings settings,
        DevicePowerOperation operation,
        OperationTrigger trigger,
        CancellationToken cancellationToken = default)
    {
        Result validation = _deviceValidator.ValidateForOperation(device, operation);
        if (!validation.IsSuccess)
        {
            return Result<DeviceOperationResult>.Success(await CompleteAsync(device, operation, trigger, settings.EnableLogs, false, false, validation.Message, DevicePowerStatus.Error, cancellationToken));
        }

        return operation switch
        {
            DevicePowerOperation.Start => Result<DeviceOperationResult>.Success(await StartAsync(device, settings, trigger, cancellationToken)),
            DevicePowerOperation.Shutdown => Result<DeviceOperationResult>.Success(await ShutdownAsync(device, settings, trigger, cancellationToken)),
            DevicePowerOperation.Reboot => Result<DeviceOperationResult>.Success(await RebootAsync(device, settings, trigger, cancellationToken)),
            _ => Result<DeviceOperationResult>.Failure("device.operation.unsupported", "Unsupported device operation.")
        };
    }

    public async Task<Result<IReadOnlyList<DeviceOperationResult>>> ExecuteControllerStartupAsync(
        IEnumerable<DevicePowerConfig> devices,
        GlobalSettings settings,
        CancellationToken cancellationToken = default)
    {
        List<DeviceOperationResult> results = [];

        foreach (DevicePowerConfig device in devices.Where(device => device.IsActive && device.AutoStartEnabled))
        {
            Result<DeviceOperationResult> operationResult = await ExecuteOperationAsync(device, settings, DevicePowerOperation.Start, OperationTrigger.ControllerStartup, cancellationToken);
            if (operationResult.Value is not null)
            {
                results.Add(operationResult.Value);
            }

            await DelayBetweenDevicesAsync(settings, cancellationToken);
        }

        return Result<IReadOnlyList<DeviceOperationResult>>.Success(results);
    }

    public async Task<Result<IReadOnlyList<DeviceOperationResult>>> ExecuteControllerShutdownAsync(
        IEnumerable<DevicePowerConfig> devices,
        GlobalSettings settings,
        CancellationToken cancellationToken = default)
    {
        List<DeviceOperationResult> results = [];

        foreach (DevicePowerConfig device in devices.Where(device => device.IsActive && device.AutoShutdownEnabled))
        {
            Result<DeviceOperationResult> operationResult = await ExecuteOperationAsync(device, settings, DevicePowerOperation.Shutdown, OperationTrigger.ControllerShutdown, cancellationToken);
            if (operationResult.Value is not null)
            {
                results.Add(operationResult.Value);
            }

            await DelayBetweenDevicesAsync(settings, cancellationToken);
        }

        return Result<IReadOnlyList<DeviceOperationResult>>.Success(results);
    }

    private async Task<DeviceOperationResult> StartAsync(
        DevicePowerConfig device,
        GlobalSettings settings,
        OperationTrigger trigger,
        CancellationToken cancellationToken)
    {
        device.LastKnownStatus = DevicePowerStatus.Starting;

        Result wakeResult = await _devicePowerService.SendWakeOnLanAsync(device, cancellationToken);
        if (!wakeResult.IsSuccess)
        {
            return await CompleteAsync(device, DevicePowerOperation.Start, trigger, settings.EnableLogs, false, false, wakeResult.Message, DevicePowerStatus.Error, cancellationToken);
        }

        await DelayBetweenDevicesAsync(settings, cancellationToken);
        Result<DevicePowerReport> report = await _devicePowerService.GetReportAsync(device, settings.PingTimeoutSeconds, cancellationToken);
        if (!report.IsSuccess)
        {
            return await CompleteAsync(device, DevicePowerOperation.Start, trigger, settings.EnableLogs, true, true, report.Message, DevicePowerStatus.Starting, cancellationToken);
        }

        return report.Value!.Status == DevicePowerStatus.Online
            ? await CompleteAsync(device, DevicePowerOperation.Start, trigger, settings.EnableLogs, true, false, "Wake on LAN packet sent and device is online.", DevicePowerStatus.Online, cancellationToken)
            : await CompleteAsync(device, DevicePowerOperation.Start, trigger, settings.EnableLogs, true, true, "Wake on LAN packet sent, but the device did not respond before timeout.", DevicePowerStatus.Starting, cancellationToken);
    }

    private async Task<DeviceOperationResult> ShutdownAsync(
        DevicePowerConfig device,
        GlobalSettings settings,
        OperationTrigger trigger,
        CancellationToken cancellationToken)
    {
        Result<DevicePowerReport> report = await _devicePowerService.GetReportAsync(device, settings.PingTimeoutSeconds, cancellationToken);
        if (!report.IsSuccess)
        {
            return await CompleteAsync(device, DevicePowerOperation.Shutdown, trigger, settings.EnableLogs, false, false, report.Message, DevicePowerStatus.Error, cancellationToken);
        }

        if (report.Value!.Status != DevicePowerStatus.Online)
        {
            return await CompleteAsync(device, DevicePowerOperation.Shutdown, trigger, settings.EnableLogs, false, false, "Shutdown skipped because the device is offline.", report.Value.Status, cancellationToken);
        }

        device.LastKnownStatus = DevicePowerStatus.ShuttingDown;
        Result shutdownResult = await _devicePowerService.ShutdownAsync(device, settings.SshTimeoutSeconds, cancellationToken);
        if (!shutdownResult.IsSuccess)
        {
            return await CompleteAsync(device, DevicePowerOperation.Shutdown, trigger, settings.EnableLogs, false, false, shutdownResult.Message, DevicePowerStatus.Error, cancellationToken);
        }

        await DelayBetweenDevicesAsync(settings, cancellationToken);
        Result<DevicePowerReport> finalReport = await _devicePowerService.GetReportAsync(device, settings.PingTimeoutSeconds, cancellationToken);
        if (!finalReport.IsSuccess)
        {
            return await CompleteAsync(device, DevicePowerOperation.Shutdown, trigger, settings.EnableLogs, true, true, finalReport.Message, DevicePowerStatus.ShuttingDown, cancellationToken);
        }

        bool wentOffline = finalReport.Value!.Status == DevicePowerStatus.Offline;
        return await CompleteAsync(
            device,
            DevicePowerOperation.Shutdown,
            trigger,
            settings.EnableLogs,
            wentOffline,
            !wentOffline,
            wentOffline
                ? "Shutdown command sent and device is offline."
                : "Shutdown command sent, but the device still responds to ping.",
            wentOffline ? DevicePowerStatus.Offline : DevicePowerStatus.ShuttingDown,
            cancellationToken);
    }

    private async Task<DeviceOperationResult> RebootAsync(
        DevicePowerConfig device,
        GlobalSettings settings,
        OperationTrigger trigger,
        CancellationToken cancellationToken)
    {
        Result<DevicePowerReport> report = await _devicePowerService.GetReportAsync(device, settings.PingTimeoutSeconds, cancellationToken);
        if (!report.IsSuccess)
        {
            return await CompleteAsync(device, DevicePowerOperation.Reboot, trigger, settings.EnableLogs, false, false, report.Message, DevicePowerStatus.Error, cancellationToken);
        }

        if (report.Value!.Status != DevicePowerStatus.Online)
        {
            return await CompleteAsync(device, DevicePowerOperation.Reboot, trigger, settings.EnableLogs, false, false, "Reboot skipped because the device is offline.", report.Value.Status, cancellationToken);
        }

        device.LastKnownStatus = DevicePowerStatus.Rebooting;
        Result rebootResult = await _devicePowerService.RebootAsync(device, settings.SshTimeoutSeconds, cancellationToken);
        if (!rebootResult.IsSuccess)
        {
            return await CompleteAsync(device, DevicePowerOperation.Reboot, trigger, settings.EnableLogs, false, false, rebootResult.Message, DevicePowerStatus.Error, cancellationToken);
        }

        await DelayBetweenDevicesAsync(settings, cancellationToken);
        Result<DevicePowerReport> finalReport = await _devicePowerService.GetReportAsync(device, settings.PingTimeoutSeconds, cancellationToken);
        if (!finalReport.IsSuccess)
        {
            return await CompleteAsync(device, DevicePowerOperation.Reboot, trigger, settings.EnableLogs, true, true, finalReport.Message, DevicePowerStatus.Rebooting, cancellationToken);
        }

        bool isOnline = finalReport.Value!.Status == DevicePowerStatus.Online;
        return await CompleteAsync(
            device,
            DevicePowerOperation.Reboot,
            trigger,
            settings.EnableLogs,
            true,
            !isOnline,
            isOnline
                ? "Reboot command sent and device is online."
                : "Reboot command sent, but the device did not return online before timeout.",
            isOnline ? DevicePowerStatus.Online : DevicePowerStatus.Rebooting,
            cancellationToken);
    }

    private async Task<DeviceOperationResult> CompleteAsync(
        DevicePowerConfig device,
        DevicePowerOperation operation,
        OperationTrigger trigger,
        bool writeLog,
        bool isSuccess,
        bool hasWarning,
        string message,
        DevicePowerStatus statusAfterOperation,
        CancellationToken cancellationToken)
    {
        device.LastKnownStatus = statusAfterOperation;
        device.LastUpdatedDateUtc = DateTime.UtcNow;
        device.LastOperationSummary = $"{operation}: {message}";

        if (writeLog)
        {
            await _logService.WriteAsync(new OperationLogEntry
            {
                DeviceId = device.DeviceId,
                DeviceName = device.Name,
                OperationType = operation.ToString(),
                Status = hasWarning ? "Warning" : isSuccess ? "Success" : "Failed",
                StartedAtUtc = device.LastUpdatedDateUtc,
                FinishedAtUtc = DateTime.UtcNow,
                ErrorMessage = isSuccess ? string.Empty : message,
                TriggeredBy = trigger.ToString(),
                Summary = message
            }, cancellationToken);
        }

        return new DeviceOperationResult
        {
            Device = device,
            Operation = operation,
            IsSuccess = isSuccess,
            HasWarning = hasWarning,
            Message = message,
            StatusAfterOperation = statusAfterOperation
        };
    }

    private static Task DelayBetweenDevicesAsync(GlobalSettings settings, CancellationToken cancellationToken)
    {
        return settings.DelayBetweenCommandsMs <= 0
            ? Task.CompletedTask
            : Task.Delay(settings.DelayBetweenCommandsMs, cancellationToken);
    }
}
