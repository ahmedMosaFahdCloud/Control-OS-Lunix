using System.Text.Json;
using ControlOS.Api.Features.Shared;
using ControlOS.Api.Features.Shared.Models;

namespace ControlOS.Api.Infrastructure.Services;

public sealed class JsonConfigurationStore
{
    private readonly CredentialProtector _credentialProtector;
    private readonly DeviceValidatorService _deviceValidator;
    private readonly JsonSerializerOptions _serializerOptions = new() { WriteIndented = true };

    public JsonConfigurationStore(CredentialProtector credentialProtector, DeviceValidatorService deviceValidator)
    {
        _credentialProtector = credentialProtector;
        _deviceValidator = deviceValidator;
    }

    public Result<AppConfiguration> Load()
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(ApplicationPaths.ConfigurationFilePath)!);

            if (!File.Exists(ApplicationPaths.ConfigurationFilePath))
            {
                AppConfiguration defaultConfiguration = CreateDefaultConfiguration();
                Result saveResult = Save(defaultConfiguration);
                return saveResult.IsSuccess
                    ? Result<AppConfiguration>.Success(defaultConfiguration)
                    : Result<AppConfiguration>.Failure(saveResult.ErrorCode, saveResult.Message);
            }

            string json = File.ReadAllText(ApplicationPaths.ConfigurationFilePath);
            AppConfiguration configuration = JsonSerializer.Deserialize<AppConfiguration>(json, _serializerOptions)
                ?? CreateDefaultConfiguration();

            configuration.GlobalSettings ??= new GlobalSettings();
            configuration.Devices ??= [];

            foreach (DevicePowerConfig device in configuration.Devices)
            {
                device.SshPassword = _credentialProtector.Unprotect(device.ProtectedSshPassword);
                device.SshHost = string.IsNullOrWhiteSpace(device.SshHost) ? device.IpAddress : device.SshHost;
                device.BroadcastAddress = string.IsNullOrWhiteSpace(device.BroadcastAddress)
                    ? configuration.GlobalSettings.DefaultBroadcastAddress
                    : device.BroadcastAddress;
                device.WolPort = device.WolPort <= 0 ? configuration.GlobalSettings.DefaultWolPort : device.WolPort;
            }

            return Result<AppConfiguration>.Success(configuration);
        }
        catch (Exception exception)
        {
            return Result<AppConfiguration>.Failure("config.load.failed", exception.Message);
        }
    }

    public Result Save(AppConfiguration configuration)
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(ApplicationPaths.ConfigurationFilePath)!);

            AppConfiguration storageModel = new()
            {
                GlobalSettings = configuration.GlobalSettings,
                Devices = configuration.Devices.Select(device => new DevicePowerConfig
                {
                    DeviceId = device.DeviceId,
                    Name = device.Name.Trim(),
                    IpAddress = device.IpAddress.Trim(),
                    MacAddress = string.IsNullOrWhiteSpace(device.MacAddress)
                        ? string.Empty
                        : _deviceValidator.NormalizeMacAddress(device.MacAddress),
                    BroadcastAddress = device.BroadcastAddress.Trim(),
                    WolPort = device.WolPort,
                    SshHost = device.SshHost.Trim(),
                    SshPort = device.SshPort,
                    SshUsername = device.SshUsername.Trim(),
                    ProtectedSshPassword = _credentialProtector.Protect(device.SshPassword),
                    OperatingSystemType = device.OperatingSystemType,
                    AutoStartEnabled = device.AutoStartEnabled,
                    AutoShutdownEnabled = device.AutoShutdownEnabled,
                    ManualControlEnabled = device.ManualControlEnabled,
                    TimeoutSeconds = device.TimeoutSeconds,
                    RetryCount = device.RetryCount,
                    Description = device.Description.Trim(),
                    IsActive = device.IsActive,
                    CreatedDateUtc = device.CreatedDateUtc,
                    LastUpdatedDateUtc = device.LastUpdatedDateUtc,
                    LastKnownStatus = device.LastKnownStatus,
                    LastOperationSummary = device.LastOperationSummary
                }).ToList()
            };

            string json = JsonSerializer.Serialize(storageModel, _serializerOptions);
            File.WriteAllText(ApplicationPaths.ConfigurationFilePath, json);
            return Result.Success();
        }
        catch (Exception exception)
        {
            return Result.Failure("config.save.failed", exception.Message);
        }
    }

    private static AppConfiguration CreateDefaultConfiguration()
    {
        return new AppConfiguration
        {
            GlobalSettings = new GlobalSettings(),
            Devices = []
        };
    }
}
