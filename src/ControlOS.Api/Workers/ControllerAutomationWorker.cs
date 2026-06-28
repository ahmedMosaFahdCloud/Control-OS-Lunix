using ControlOS.Api.Features.Shared.Models;
using ControlOS.Api.Infrastructure.Services;

namespace ControlOS.Api.Workers;

public sealed class ControllerAutomationWorker : BackgroundService
{
    private readonly JsonConfigurationStore _configurationStore;
    private readonly ControllerOrchestrator _controllerOrchestrator;
    private readonly WindowsStartupService _windowsStartupService;
    private readonly ILogger<ControllerAutomationWorker> _logger;
    private readonly IHostApplicationLifetime _applicationLifetime;

    private AppConfiguration? _configuration;

    public ControllerAutomationWorker(
        JsonConfigurationStore configurationStore,
        ControllerOrchestrator controllerOrchestrator,
        WindowsStartupService windowsStartupService,
        ILogger<ControllerAutomationWorker> logger,
        IHostApplicationLifetime applicationLifetime)
    {
        _configurationStore = configurationStore;
        _controllerOrchestrator = controllerOrchestrator;
        _windowsStartupService = windowsStartupService;
        _logger = logger;
        _applicationLifetime = applicationLifetime;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _applicationLifetime.ApplicationStopping.Register(RunShutdownSequence);

        var registrationResult = _windowsStartupService.EnsureRegistered();
        if (!registrationResult.IsSuccess)
        {
            _logger.LogWarning("Windows auto-start registration failed: {Message}", registrationResult.Message);
        }

        var configurationResult = _configurationStore.Load();
        if (!configurationResult.IsSuccess || configurationResult.Value is null)
        {
            _logger.LogError("Configuration load failed: {Message}", configurationResult.Message);
            return;
        }

        _configuration = configurationResult.Value;

        if (_configuration.GlobalSettings.AutoStartDevicesOnControllerBoot)
        {
            var startupResult = await _controllerOrchestrator.ExecuteControllerStartupAsync(
                _configuration.Devices,
                _configuration.GlobalSettings,
                stoppingToken);

            _logger.LogInformation(
                "Controller startup automation finished. Success={Success}, Count={Count}",
                startupResult.IsSuccess,
                startupResult.Value?.Count ?? 0);
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
        }
    }

    private void RunShutdownSequence()
    {
        if (_configuration is null || !_configuration.GlobalSettings.AutoShutdownDevicesOnControllerShutdown)
        {
            return;
        }

        try
        {
            var shutdownResult = _controllerOrchestrator.ExecuteControllerShutdownAsync(
                _configuration.Devices,
                _configuration.GlobalSettings).GetAwaiter().GetResult();

            _logger.LogInformation(
                "Controller shutdown automation finished. Success={Success}, Count={Count}",
                shutdownResult.IsSuccess,
                shutdownResult.Value?.Count ?? 0);
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Controller shutdown automation failed.");
        }
    }
}
