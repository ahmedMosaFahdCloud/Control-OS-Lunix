using Control_OS_Lunix.Model;

namespace Control_OS_Lunix.Service;

public sealed class ControllerOrchestrator
{
    private readonly DevicePowerService _devicePowerService;
    private readonly LogService _logService;

    public ControllerOrchestrator(DevicePowerService devicePowerService, LogService logService)
    {
        _devicePowerService = devicePowerService;
        _logService = logService;
    }

    public async Task<DeviceOperationResult> ExecuteOperationAsync(
        DevicePowerConfig device,
        GlobalSettings settings,
        DevicePowerOperation operation,
        OperationTrigger trigger,
        CancellationToken cancellationToken = default)
    {
        string? validationError = DeviceValidator.ValidateForOperation(device, operation);
        if (validationError is not null)
        {
            return await CompleteAsync(device, operation, trigger, settings.EnableLogs, false, false, validationError, DevicePowerStatus.Error, cancellationToken);
        }

        try
        {
            return operation switch
            {
                DevicePowerOperation.Start => await StartAsync(device, settings, trigger, cancellationToken),
                DevicePowerOperation.Shutdown => await ShutdownAsync(device, settings, trigger, cancellationToken),
                DevicePowerOperation.Reboot => await RebootAsync(device, settings, trigger, cancellationToken),
                _ => throw new ArgumentOutOfRangeException(nameof(operation), operation, null)
            };
        }
        catch (Exception exception)
        {
            return await CompleteAsync(device, operation, trigger, settings.EnableLogs, false, false, exception.Message, DevicePowerStatus.Error, cancellationToken);
        }
    }

    public async Task<IReadOnlyList<DeviceOperationResult>> ExecuteControllerStartupAsync(
        IEnumerable<DevicePowerConfig> devices,
        GlobalSettings settings,
        CancellationToken cancellationToken = default)
    {
        List<DeviceOperationResult> results = [];

        foreach (DevicePowerConfig device in devices.Where(device => device.IsActive && device.AutoStartEnabled))
        {
            results.Add(await ExecuteOperationAsync(device, settings, DevicePowerOperation.Start, OperationTrigger.ControllerStartup, cancellationToken));
            await DelayBetweenDevicesAsync(settings, cancellationToken);
        }

        return results;
    }

    public async Task<IReadOnlyList<DeviceOperationResult>> ExecuteControllerShutdownAsync(
        IEnumerable<DevicePowerConfig> devices,
        GlobalSettings settings,
        CancellationToken cancellationToken = default)
    {
        List<DeviceOperationResult> results = [];

        foreach (DevicePowerConfig device in devices.Where(device => device.IsActive && device.AutoShutdownEnabled))
        {
            results.Add(await ExecuteOperationAsync(device, settings, DevicePowerOperation.Shutdown, OperationTrigger.ControllerShutdown, cancellationToken));
            await DelayBetweenDevicesAsync(settings, cancellationToken);
        }

        return results;
    }

    private async Task<DeviceOperationResult> StartAsync(
        DevicePowerConfig device,
        GlobalSettings settings,
        OperationTrigger trigger,
        CancellationToken cancellationToken)
    {
        device.LastKnownStatus = DevicePowerStatus.Starting;
        await _devicePowerService.SendWakeOnLanAsync(device, cancellationToken);
        await DelayBetweenDevicesAsync(settings, cancellationToken);

        DevicePowerReport report = await _devicePowerService.GetReportAsync(device, settings.PingTimeoutSeconds, cancellationToken);

        if (report.Status == DevicePowerStatus.Online)
        {
            return await CompleteAsync(
                device,
                DevicePowerOperation.Start,
                trigger,
                settings.EnableLogs,
                isSuccess: true,
                hasWarning: false,
                message: "Wake on LAN packet sent and device is online.",
                statusAfterOperation: DevicePowerStatus.Online,
                cancellationToken);
        }

        return await CompleteAsync(
            device,
            DevicePowerOperation.Start,
            trigger,
            settings.EnableLogs,
            isSuccess: true,
            hasWarning: true,
            message: "Wake on LAN packet sent, but the device did not respond before timeout.",
            statusAfterOperation: DevicePowerStatus.Starting,
            cancellationToken);
    }

    private async Task<DeviceOperationResult> ShutdownAsync(
        DevicePowerConfig device,
        GlobalSettings settings,
        OperationTrigger trigger,
        CancellationToken cancellationToken)
    {
        DevicePowerReport report = await _devicePowerService.GetReportAsync(device, settings.PingTimeoutSeconds, cancellationToken);
        if (report.Status != DevicePowerStatus.Online)
        {
            return await CompleteAsync(
                device,
                DevicePowerOperation.Shutdown,
                trigger,
                settings.EnableLogs,
                isSuccess: false,
                hasWarning: false,
                message: "Shutdown skipped because the device is offline.",
                statusAfterOperation: report.Status,
                cancellationToken);
        }

        device.LastKnownStatus = DevicePowerStatus.ShuttingDown;
        await _devicePowerService.ShutdownAsync(device, settings.SshTimeoutSeconds, cancellationToken);
        await DelayBetweenDevicesAsync(settings, cancellationToken);

        DevicePowerReport finalReport = await _devicePowerService.GetReportAsync(device, settings.PingTimeoutSeconds, cancellationToken);
        bool wentOffline = finalReport.Status == DevicePowerStatus.Offline;

        return await CompleteAsync(
            device,
            DevicePowerOperation.Shutdown,
            trigger,
            settings.EnableLogs,
            isSuccess: wentOffline,
            hasWarning: !wentOffline,
            message: wentOffline
                ? "Shutdown command sent and device is offline."
                : "Shutdown command sent, but the device still responds to ping.",
            statusAfterOperation: wentOffline ? DevicePowerStatus.Offline : DevicePowerStatus.ShuttingDown,
            cancellationToken);
    }

    private async Task<DeviceOperationResult> RebootAsync(
        DevicePowerConfig device,
        GlobalSettings settings,
        OperationTrigger trigger,
        CancellationToken cancellationToken)
    {
        DevicePowerReport report = await _devicePowerService.GetReportAsync(device, settings.PingTimeoutSeconds, cancellationToken);
        if (report.Status != DevicePowerStatus.Online)
        {
            return await CompleteAsync(
                device,
                DevicePowerOperation.Reboot,
                trigger,
                settings.EnableLogs,
                isSuccess: false,
                hasWarning: false,
                message: "Reboot skipped because the device is offline.",
                statusAfterOperation: report.Status,
                cancellationToken);
        }

        device.LastKnownStatus = DevicePowerStatus.Rebooting;
        await _devicePowerService.RebootAsync(device, settings.SshTimeoutSeconds, cancellationToken);
        await DelayBetweenDevicesAsync(settings, cancellationToken);
        DevicePowerReport finalReport = await _devicePowerService.GetReportAsync(device, settings.PingTimeoutSeconds, cancellationToken);

        return await CompleteAsync(
            device,
            DevicePowerOperation.Reboot,
            trigger,
            settings.EnableLogs,
            isSuccess: true,
            hasWarning: finalReport.Status != DevicePowerStatus.Online,
            message: finalReport.Status == DevicePowerStatus.Online
                ? "Reboot command sent and device is online."
                : "Reboot command sent, but the device did not return online before timeout.",
            statusAfterOperation: finalReport.Status == DevicePowerStatus.Online
                ? DevicePowerStatus.Online
                : DevicePowerStatus.Rebooting,
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
