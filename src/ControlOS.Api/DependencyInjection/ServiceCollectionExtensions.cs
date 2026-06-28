using ControlOS.Api.Features.Shared;
using ControlOS.Api.Infrastructure.Services;
using ControlOS.Api.Workers;

namespace ControlOS.Api.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddControlOsCoreServices(this IServiceCollection services)
    {
        return services
            .AddSingleton<CredentialProtector>()
            .AddSingleton<DeviceValidatorService>()
            .AddSingleton<JsonConfigurationStore>()
            .AddSingleton<DevicePowerService>()
            .AddSingleton<LogService>()
            .AddSingleton<BackupRestoreService>()
            .AddSingleton<ControllerOrchestrator>()
            .AddSingleton<NetworkScannerService>()
            .AddSingleton<ControlCenterService>()
            .AddSingleton<WindowsStartupService>()
            .AddHostedService<ControllerAutomationWorker>();
    }
}
