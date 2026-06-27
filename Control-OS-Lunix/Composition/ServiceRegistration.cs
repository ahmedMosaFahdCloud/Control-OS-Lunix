using Control_OS_Lunix.Backend.Interfaces;
using Control_OS_Lunix.Backend.Services;
using Control_OS_Lunix.UI.Controllers;
using Control_OS_Lunix.UI.Views;
using Microsoft.Extensions.DependencyInjection;

namespace Control_OS_Lunix.Composition;

public static class ServiceRegistration
{
    public static ServiceProvider BuildServiceProvider()
    {
        var services = new ServiceCollection();

        services.AddSingleton<ICredentialProtector, CredentialProtector>();
        services.AddSingleton<IDeviceValidator, DeviceValidatorService>();
        services.AddSingleton<IConfigurationStore, JsonConfigurationStore>();
        services.AddSingleton<IBackupRestoreService, BackupRestoreService>();
        services.AddSingleton<IWindowsStartupService, WindowsStartupService>();
        services.AddSingleton<ILogService, LogService>();
        services.AddSingleton<IDevicePowerService, DevicePowerService>();
        services.AddSingleton<IControllerOrchestrator, ControllerOrchestrator>();
        services.AddSingleton<INetworkScannerService, NetworkScannerService>();

        services.AddTransient<IMainDashboardController, MainDashboardController>();
        services.AddTransient<IDeviceDialogController, DeviceDialogController>();
        services.AddTransient<ISettingsController, SettingsController>();
        services.AddTransient<ILogsController, LogsController>();
        services.AddTransient<INetworkScannerController, NetworkScannerController>();

        services.AddTransient<MainDashboardView>();
        services.AddTransient<DeviceDialogView>();
        services.AddTransient<SettingsView>();
        services.AddTransient<LogsView>();
        services.AddTransient<NetworkScannerView>();

        return services.BuildServiceProvider();
    }
}
