using Control_OS_Lunix.Backend.Models;
using Control_OS_Lunix.Backend.Results;
using Control_OS_Lunix.UI.ViewModels;
using Control_OS_Lunix.UI.Views;
using Microsoft.Extensions.DependencyInjection;

namespace Control_OS_Lunix.UI.Controllers;

public sealed class SettingsController : ISettingsController
{
    private readonly IServiceProvider _serviceProvider;
    private TaskCompletionSource<Result<GlobalSettings?>>? _completionSource;
    private ISettingsView? _view;

    public SettingsController(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public Task<Result<GlobalSettings?>> ShowAsync(IWin32Window owner, GlobalSettings currentSettings)
    {
        _completionSource = new TaskCompletionSource<Result<GlobalSettings?>>();
        _view = _serviceProvider.GetRequiredService<SettingsView>();
        _view.SaveRequested += HandleSaveRequested;
        _view.CancelRequested += HandleCancelRequested;
        _view.Settings = new SettingsViewModel
        {
            AutoStartDevicesOnControllerBoot = currentSettings.AutoStartDevicesOnControllerBoot,
            AutoShutdownDevicesOnControllerShutdown = currentSettings.AutoShutdownDevicesOnControllerShutdown,
            DelayBetweenCommandsMs = currentSettings.DelayBetweenCommandsMs,
            PingTimeoutSeconds = currentSettings.PingTimeoutSeconds,
            SshTimeoutSeconds = currentSettings.SshTimeoutSeconds,
            RetryCount = currentSettings.RetryCount,
            DefaultWolPort = currentSettings.DefaultWolPort,
            DefaultBroadcastAddress = currentSettings.DefaultBroadcastAddress,
            EnableLogs = currentSettings.EnableLogs,
            LogRetentionDays = currentSettings.LogRetentionDays,
            ConfirmManualShutdown = currentSettings.ConfirmManualShutdown
        };
        _view.ShowDialogView(owner);
        return Task.FromResult(_completionSource.Task.GetAwaiter().GetResult());
    }

    private void HandleSaveRequested(object? sender, EventArgs e)
    {
        SettingsViewModel settings = _view!.Settings;
        _completionSource!.SetResult(Result<GlobalSettings?>.Success(new GlobalSettings
        {
            AutoStartDevicesOnControllerBoot = settings.AutoStartDevicesOnControllerBoot,
            AutoShutdownDevicesOnControllerShutdown = settings.AutoShutdownDevicesOnControllerShutdown,
            DelayBetweenCommandsMs = settings.DelayBetweenCommandsMs,
            PingTimeoutSeconds = settings.PingTimeoutSeconds,
            SshTimeoutSeconds = settings.SshTimeoutSeconds,
            RetryCount = settings.RetryCount,
            DefaultWolPort = settings.DefaultWolPort,
            DefaultBroadcastAddress = settings.DefaultBroadcastAddress,
            EnableLogs = settings.EnableLogs,
            LogRetentionDays = settings.LogRetentionDays,
            ConfirmManualShutdown = settings.ConfirmManualShutdown
        }));
        _view.CloseView(DialogResult.OK);
    }

    private void HandleCancelRequested(object? sender, EventArgs e)
    {
        _completionSource!.SetResult(Result<GlobalSettings?>.Success(null));
        _view!.CloseView(DialogResult.Cancel);
    }
}
