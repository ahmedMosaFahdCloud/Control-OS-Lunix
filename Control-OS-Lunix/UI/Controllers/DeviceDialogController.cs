using Control_OS_Lunix.Backend.Interfaces;
using Control_OS_Lunix.Backend.Models;
using Control_OS_Lunix.Backend.Results;
using Control_OS_Lunix.UI.ViewModels;
using Control_OS_Lunix.UI.Views;
using Microsoft.Extensions.DependencyInjection;

namespace Control_OS_Lunix.UI.Controllers;

public sealed class DeviceDialogController : IDeviceDialogController
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IDeviceValidator _deviceValidator;

    private IDeviceDialogView? _view;
    private TaskCompletionSource<Result<DevicePowerConfig?>>? _completionSource;

    public DeviceDialogController(IServiceProvider serviceProvider, IDeviceValidator deviceValidator)
    {
        _serviceProvider = serviceProvider;
        _deviceValidator = deviceValidator;
    }

    public Task<Result<DevicePowerConfig?>> ShowCreateAsync(IWin32Window owner, GlobalSettings settings)
    {
        return ShowInternalAsync(owner, settings, null);
    }

    public Task<Result<DevicePowerConfig?>> ShowEditAsync(IWin32Window owner, GlobalSettings settings, DevicePowerConfig existingDevice)
    {
        return ShowInternalAsync(owner, settings, existingDevice);
    }

    private Task<Result<DevicePowerConfig?>> ShowInternalAsync(IWin32Window owner, GlobalSettings settings, DevicePowerConfig? existingDevice)
    {
        _view = _serviceProvider.GetRequiredService<DeviceDialogView>();
        _completionSource = new TaskCompletionSource<Result<DevicePowerConfig?>>();
        _view.SaveRequested += HandleSaveRequested;
        _view.CancelRequested += HandleCancelRequested;
        _view.ViewTitle = existingDevice is null ? "Add Device" : "Edit Device";
        _view.FormData = Map(existingDevice, settings);
        _view.ShowDialogView(owner);
        return Task.FromResult(_completionSource.Task.GetAwaiter().GetResult());
    }

    private void HandleSaveRequested(object? sender, EventArgs e)
    {
        DevicePowerConfig mappedDevice = Map(_view!.FormData);
        Result<DevicePowerConfig> validationResult = _deviceValidator.ValidateForSave(mappedDevice);
        if (!validationResult.IsSuccess)
        {
            _view.ShowWarning(validationResult.Message, "Validation");
            return;
        }

        _completionSource!.SetResult(Result<DevicePowerConfig?>.Success(mappedDevice));
        _view.CloseView(DialogResult.OK);
    }

    private void HandleCancelRequested(object? sender, EventArgs e)
    {
        _completionSource!.SetResult(Result<DevicePowerConfig?>.Success(null));
        _view!.CloseView(DialogResult.Cancel);
    }

    private static DeviceEditViewModel Map(DevicePowerConfig? device, GlobalSettings settings)
    {
        DevicePowerConfig source = device?.Clone() ?? new DevicePowerConfig
        {
            BroadcastAddress = settings.DefaultBroadcastAddress,
            WolPort = settings.DefaultWolPort
        };

        return new DeviceEditViewModel
        {
            DeviceId = source.DeviceId,
            Name = source.Name,
            IpAddress = source.IpAddress,
            MacAddress = source.MacAddress,
            BroadcastAddress = source.BroadcastAddress,
            WolPort = source.WolPort,
            SshHost = source.SshHost,
            SshPort = source.SshPort,
            SshUsername = source.SshUsername,
            SshPassword = source.SshPassword,
            OperatingSystemType = source.OperatingSystemType,
            AutoStartEnabled = source.AutoStartEnabled,
            AutoShutdownEnabled = source.AutoShutdownEnabled,
            ManualControlEnabled = source.ManualControlEnabled,
            IsActive = source.IsActive,
            TimeoutSeconds = source.TimeoutSeconds,
            RetryCount = source.RetryCount,
            Description = source.Description,
            CreatedDateUtc = source.CreatedDateUtc,
            LastUpdatedDateUtc = source.LastUpdatedDateUtc,
            LastKnownStatus = source.LastKnownStatus,
            LastOperationSummary = source.LastOperationSummary
        };
    }

    private static DevicePowerConfig Map(DeviceEditViewModel model)
    {
        return new DevicePowerConfig
        {
            DeviceId = model.DeviceId,
            Name = model.Name,
            IpAddress = model.IpAddress,
            MacAddress = model.MacAddress,
            BroadcastAddress = model.BroadcastAddress,
            WolPort = model.WolPort,
            SshHost = model.SshHost,
            SshPort = model.SshPort,
            SshUsername = model.SshUsername,
            SshPassword = model.SshPassword,
            OperatingSystemType = model.OperatingSystemType,
            AutoStartEnabled = model.AutoStartEnabled,
            AutoShutdownEnabled = model.AutoShutdownEnabled,
            ManualControlEnabled = model.ManualControlEnabled,
            IsActive = model.IsActive,
            TimeoutSeconds = model.TimeoutSeconds,
            RetryCount = model.RetryCount,
            Description = model.Description,
            CreatedDateUtc = model.CreatedDateUtc,
            LastUpdatedDateUtc = DateTime.UtcNow,
            LastKnownStatus = model.LastKnownStatus,
            LastOperationSummary = model.LastOperationSummary
        };
    }
}
