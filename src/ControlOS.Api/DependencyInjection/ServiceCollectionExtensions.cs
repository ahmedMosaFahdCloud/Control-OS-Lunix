using ControlOS.Api.Backend.Interfaces;
using ControlOS.Api.Backend.Services;
using Microsoft.Extensions.DependencyInjection;
using System.Runtime.Versioning;

namespace ControlOS.Api.DependencyInjection;

[SupportedOSPlatform("windows")]
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddControlOsCoreServices(this IServiceCollection services)
    {
        services.AddSingleton<ICredentialProtector, CredentialProtector>();
        services.AddSingleton<IDeviceValidator, DeviceValidatorService>();
        services.AddSingleton<IConfigurationStore, JsonConfigurationStore>();
        services.AddSingleton<IBackupRestoreService, BackupRestoreService>();
        services.AddSingleton<ILogService, LogService>();
        services.AddSingleton<IDevicePowerService, DevicePowerService>();
        services.AddSingleton<IControllerOrchestrator, ControllerOrchestrator>();
        services.AddSingleton<INetworkScannerService, NetworkScannerService>();
        services.AddSingleton<IWindowsStartupService, WindowsStartupService>();

        return services;
    }
}
