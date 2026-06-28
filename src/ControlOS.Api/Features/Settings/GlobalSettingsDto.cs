namespace ControlOS.Api.Features.Settings;

public sealed record GlobalSettingsDto(
    bool AutoStartDevicesOnControllerBoot,
    bool AutoShutdownDevicesOnControllerShutdown,
    int DelayBetweenCommandsMs,
    int PingTimeoutSeconds,
    int SshTimeoutSeconds,
    int RetryCount,
    int DefaultWolPort,
    string DefaultBroadcastAddress,
    bool EnableLogs,
    int LogRetentionDays,
    bool ConfirmManualShutdown);
