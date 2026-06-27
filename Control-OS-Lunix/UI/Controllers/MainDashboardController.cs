using System.Runtime.InteropServices;
using Control_OS_Lunix.Backend.Interfaces;
using Control_OS_Lunix.Backend.Models;
using Control_OS_Lunix.Backend.Results;
using Control_OS_Lunix.UI.ViewModels;
using Control_OS_Lunix.UI.Views;
using Microsoft.Extensions.DependencyInjection;

namespace Control_OS_Lunix.UI.Controllers;

public sealed class MainDashboardController : IMainDashboardController
{
    private const int ShutdownPriorityLate = 0x3FF;

    private readonly IConfigurationStore _configurationStore;
    private readonly ILogService _logService;
    private readonly IDevicePowerService _devicePowerService;
    private readonly IControllerOrchestrator _controllerOrchestrator;
    private readonly IServiceProvider _serviceProvider;

    private IMainDashboardView? _view;
    private AppConfiguration _configuration = new();
    private bool _startupSequenceCompleted;
    private bool _shutdownSequenceInProgress;
    private bool _allowClose;

    public MainDashboardController(
        IConfigurationStore configurationStore,
        ILogService logService,
        IDevicePowerService devicePowerService,
        IControllerOrchestrator controllerOrchestrator,
        IServiceProvider serviceProvider)
    {
        _configurationStore = configurationStore;
        _logService = logService;
        _devicePowerService = devicePowerService;
        _controllerOrchestrator = controllerOrchestrator;
        _serviceProvider = serviceProvider;
        SetProcessShutdownParameters(ShutdownPriorityLate, 0);
    }

    public void AttachView(IMainDashboardView view)
    {
        _view = view;
        _view.ViewLoaded += async (_, _) => await ExecuteAsync(HandleViewLoadedAsync);
        _view.AddRequested += async (_, _) => await ExecuteAsync(HandleAddRequestedAsync);
        _view.ScanRequested += async (_, _) => await ExecuteAsync(HandleScanRequestedAsync);
        _view.EditRequested += async (_, _) => await ExecuteAsync(HandleEditRequestedAsync);
        _view.DeleteRequested += (_, _) => HandleDeleteRequested();
        _view.StartRequested += async (_, _) => await ExecuteAsync(() => HandleOperationRequestedAsync(DevicePowerOperation.Start));
        _view.RebootRequested += async (_, _) => await ExecuteAsync(() => HandleOperationRequestedAsync(DevicePowerOperation.Reboot));
        _view.ShutdownRequested += async (_, _) => await ExecuteAsync(() => HandleOperationRequestedAsync(DevicePowerOperation.Shutdown));
        _view.RefreshRequested += async (_, _) => await ExecuteAsync(RefreshStatusesAsync);
        _view.SettingsRequested += async (_, _) => await ExecuteAsync(HandleSettingsRequestedAsync);
        _view.LogsRequested += async (_, _) => await ExecuteAsync(HandleLogsRequestedAsync);
        _view.ViewClosing += HandleViewClosing;
        _view.WindowsShutdownHandler = HandleWindowsShutdown;
    }

    private async Task HandleViewLoadedAsync()
    {
        Result<AppConfiguration> loadResult = _configurationStore.Load();
        if (!loadResult.IsSuccess || loadResult.Value is null)
        {
            _view!.ShowError(loadResult.Message, "Startup Error");
            return;
        }

        _configuration = loadResult.Value;
        _logService.EnforceRetention(_configuration.GlobalSettings.LogRetentionDays);
        RenderConfiguration();
        await RefreshStatusesAsync();

        if (!_startupSequenceCompleted && _configuration.GlobalSettings.AutoStartDevicesOnControllerBoot)
        {
            _startupSequenceCompleted = true;
            await RunControllerStartupAsync();
        }
    }

    private async Task HandleAddRequestedAsync()
    {
        IDeviceDialogController controller = _serviceProvider.GetRequiredService<IDeviceDialogController>();
        Result<DevicePowerConfig?> dialogResult = await controller.ShowCreateAsync(_view!.OwnerWindow, _configuration.GlobalSettings);
        if (dialogResult.Value is null)
        {
            return;
        }

        _configuration.Devices.Add(dialogResult.Value);
        SaveAndRender();
        _view.SetStatus($"Added device '{dialogResult.Value.Name}'.");
    }

    private async Task HandleScanRequestedAsync()
    {
        INetworkScannerController scannerController = _serviceProvider.GetRequiredService<INetworkScannerController>();
        Result<DevicePowerConfig?> scanResult = await scannerController.ShowAsync(_view!.OwnerWindow, _configuration.GlobalSettings);
        if (scanResult.Value is null)
        {
            return;
        }

        IDeviceDialogController dialogController = _serviceProvider.GetRequiredService<IDeviceDialogController>();
        Result<DevicePowerConfig?> editResult = await dialogController.ShowEditAsync(_view.OwnerWindow, _configuration.GlobalSettings, scanResult.Value);
        if (editResult.Value is null)
        {
            return;
        }

        _configuration.Devices.Add(editResult.Value);
        SaveAndRender();
        _view.SetStatus($"Added scanned device '{editResult.Value.Name}'.");
    }

    private async Task HandleEditRequestedAsync()
    {
        DevicePowerConfig? selected = GetSelectedDevice();
        if (selected is null)
        {
            return;
        }

        IDeviceDialogController controller = _serviceProvider.GetRequiredService<IDeviceDialogController>();
        Result<DevicePowerConfig?> dialogResult = await controller.ShowEditAsync(_view!.OwnerWindow, _configuration.GlobalSettings, selected);
        if (dialogResult.Value is null)
        {
            return;
        }

        int index = _configuration.Devices.FindIndex(device => device.DeviceId == selected.DeviceId);
        if (index >= 0)
        {
            _configuration.Devices[index] = dialogResult.Value;
            SaveAndRender();
            _view.SetStatus($"Updated device '{dialogResult.Value.Name}'.");
        }
    }

    private void HandleDeleteRequested()
    {
        DevicePowerConfig? selected = GetSelectedDevice();
        if (selected is null)
        {
            return;
        }

        if (!_view!.Confirm($"Delete device '{selected.Name}'?", "Delete Device"))
        {
            return;
        }

        _configuration.Devices.RemoveAll(device => device.DeviceId == selected.DeviceId);
        SaveAndRender();
        _view.SetStatus($"Deleted device '{selected.Name}'.");
    }

    private async Task HandleSettingsRequestedAsync()
    {
        ISettingsController controller = _serviceProvider.GetRequiredService<ISettingsController>();
        Result<GlobalSettings?> dialogResult = await controller.ShowAsync(_view!.OwnerWindow, _configuration.GlobalSettings);
        if (dialogResult.Value is null)
        {
            return;
        }

        _configuration.GlobalSettings = dialogResult.Value;
        SaveAndRender();
        _view.SetStatus("Global settings saved.");
    }

    private async Task HandleLogsRequestedAsync()
    {
        ILogsController controller = _serviceProvider.GetRequiredService<ILogsController>();
        Result result = await controller.ShowAsync(_view!.OwnerWindow);
        if (!result.IsSuccess)
        {
            _view.ShowError(result.Message, "Logs");
        }
    }

    private async Task HandleOperationRequestedAsync(DevicePowerOperation operation)
    {
        DevicePowerConfig? selected = GetSelectedDevice();
        if (selected is null)
        {
            return;
        }

        if (!selected.ManualControlEnabled)
        {
            _view!.ShowInfo("Manual control is disabled for this device.", "Manual Control");
            return;
        }

        if (operation == DevicePowerOperation.Shutdown &&
            _configuration.GlobalSettings.ConfirmManualShutdown &&
            !_view!.Confirm($"Shutdown '{selected.Name}'?", "Confirm Shutdown"))
        {
            return;
        }

        _view!.SetBusy(true);
        _view.SetStatus($"{operation} operation is running for '{selected.Name}'...");

        try
        {
            Result<DeviceOperationResult> result = await _controllerOrchestrator.ExecuteOperationAsync(
                selected,
                _configuration.GlobalSettings,
                operation,
                OperationTrigger.Manual);

            if (!result.IsSuccess || result.Value is null)
            {
                _view.ShowError(result.Message, operation.ToString());
                return;
            }

            SaveAndRender();
            _view.SetStatus(result.Value.Message);

            if (result.Value.IsSuccess)
            {
                if (result.Value.HasWarning)
                {
                    _view.ShowWarning(result.Value.Message, operation.ToString());
                }
                else
                {
                    _view.ShowInfo(result.Value.Message, operation.ToString());
                }
            }
            else
            {
                _view.ShowError(result.Value.Message, operation.ToString());
            }
        }
        finally
        {
            _view.SetBusy(false);
        }
    }

    private async Task RefreshStatusesAsync()
    {
        _view!.SetBusy(true);
        _view.SetStatus("Refreshing device reachability...");

        try
        {
            foreach (DevicePowerConfig device in _configuration.Devices)
            {
                Result<DevicePowerReport> report = await _devicePowerService.GetReportAsync(device, _configuration.GlobalSettings.PingTimeoutSeconds);
                device.LastKnownStatus = report.IsSuccess && report.Value is not null
                    ? report.Value.Status
                    : DevicePowerStatus.Unknown;
                device.LastUpdatedDateUtc = report.IsSuccess && report.Value is not null
                    ? report.Value.CheckedAtUtc
                    : DateTime.UtcNow;
            }

            SaveAndRender();
            _view.SetStatus("Device statuses were refreshed.");
        }
        finally
        {
            _view.SetBusy(false);
        }
    }

    private async Task RunControllerStartupAsync()
    {
        Result<IReadOnlyList<DeviceOperationResult>> result = await _controllerOrchestrator.ExecuteControllerStartupAsync(
            _configuration.Devices,
            _configuration.GlobalSettings);

        SaveAndRender();
        int count = result.Value?.Count ?? 0;
        _view!.SetStatus($"Startup sequence completed for {count} device(s).");
        _view.ShowInfo($"Startup sequence completed for {count} device(s).", "Controller Startup");
    }

    private async Task RunControllerShutdownAsync()
    {
        Result<IReadOnlyList<DeviceOperationResult>> result = await _controllerOrchestrator.ExecuteControllerShutdownAsync(
            _configuration.Devices,
            _configuration.GlobalSettings);

        SaveAndRender();
        _view!.SetStatus($"Shutdown sequence completed for {result.Value?.Count ?? 0} device(s).");
    }

    private void HandleViewClosing(object? sender, FormClosingEventArgs e)
    {
        if (e.CloseReason == CloseReason.WindowsShutDown)
        {
            return;
        }

        if (_allowClose || _shutdownSequenceInProgress || !_configuration.GlobalSettings.AutoShutdownDevicesOnControllerShutdown)
        {
            return;
        }

        _shutdownSequenceInProgress = true;
        e.Cancel = true;
        _ = ExecuteAsync(async () =>
        {
            _view!.SetBusy(true);
            try
            {
                await RunControllerShutdownAsync();
            }
            finally
            {
                _allowClose = true;
                _view.SetBusy(false);
                _view.RequestClose();
            }
        });
    }

    private WindowsShutdownDecision HandleWindowsShutdown()
    {
        if (_allowClose || _shutdownSequenceInProgress || !_configuration.GlobalSettings.AutoShutdownDevicesOnControllerShutdown)
        {
            return new WindowsShutdownDecision();
        }

        _shutdownSequenceInProgress = true;
        Result<IReadOnlyList<DeviceOperationResult>> result = _controllerOrchestrator.ExecuteControllerShutdownAsync(
            _configuration.Devices,
            _configuration.GlobalSettings).GetAwaiter().GetResult();

        SaveAndRender();

        if (_configuration.GlobalSettings.EnableLogs)
        {
            _logService.WriteAsync(new OperationLogEntry
            {
                DeviceName = "Controller",
                OperationType = "ControllerShutdown",
                Status = result.IsSuccess ? "Success" : "Failed",
                TriggeredBy = OperationTrigger.ControllerShutdown.ToString(),
                Summary = $"Windows shutdown sequence completed for {result.Value?.Count ?? 0} device(s).",
                ErrorMessage = result.IsSuccess ? string.Empty : result.Message
            }).GetAwaiter().GetResult();
        }

        _allowClose = true;
        return new WindowsShutdownDecision
        {
            AllowSessionEnd = true,
            BlockReason = "Waiting for remote devices to complete shutdown."
        };
    }

    private void SaveAndRender()
    {
        Result saveResult = _configurationStore.Save(_configuration);
        if (!saveResult.IsSuccess)
        {
            _view!.ShowError(saveResult.Message, "Configuration");
        }

        RenderConfiguration();
    }

    private void RenderConfiguration()
    {
        _view!.BindDevices(_configuration.Devices
            .OrderBy(device => device.Name)
            .Select(device => new DeviceGridRowViewModel
            {
                DeviceId = device.DeviceId,
                Name = device.Name,
                IpAddress = device.IpAddress,
                Status = device.LastKnownStatus,
                AutoStartEnabled = device.AutoStartEnabled,
                AutoShutdownEnabled = device.AutoShutdownEnabled,
                LastOperationSummary = device.LastOperationSummary,
                UpdatedText = device.LastUpdatedDateUtc == default
                    ? "-"
                    : device.LastUpdatedDateUtc.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss")
            }).ToArray());

        _view.UpdateSummary(new DashboardSummaryViewModel
        {
            TotalDevices = _configuration.Devices.Count.ToString(),
            OnlineDevices = _configuration.Devices.Count(device => device.LastKnownStatus == DevicePowerStatus.Online).ToString(),
            ActiveDevices = _configuration.Devices.Count(device => device.IsActive).ToString(),
            LastAction = _configuration.Devices
                .OrderByDescending(device => device.LastUpdatedDateUtc)
                .Select(device => device.LastOperationSummary)
                .FirstOrDefault() ?? "No activity"
        });
    }

    private DevicePowerConfig? GetSelectedDevice()
    {
        if (!_view!.SelectedDeviceId.HasValue)
        {
            return null;
        }

        return _configuration.Devices.FirstOrDefault(device => device.DeviceId == _view.SelectedDeviceId.Value);
    }

    private async Task ExecuteAsync(Func<Task> action)
    {
        try
        {
            await action();
        }
        catch (Exception exception)
        {
            _view?.ShowError(exception.Message, "Unexpected Error");
        }
    }

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SetProcessShutdownParameters(int dwLevel, int dwFlags);
}
