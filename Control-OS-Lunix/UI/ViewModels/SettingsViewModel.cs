namespace Control_OS_Lunix.UI.ViewModels;

public sealed class SettingsViewModel
{
    public bool AutoStartDevicesOnControllerBoot { get; set; }

    public bool AutoShutdownDevicesOnControllerShutdown { get; set; }

    public int DelayBetweenCommandsMs { get; set; }

    public int PingTimeoutSeconds { get; set; }

    public int SshTimeoutSeconds { get; set; }

    public int RetryCount { get; set; }

    public int DefaultWolPort { get; set; }

    public string DefaultBroadcastAddress { get; set; } = string.Empty;

    public bool EnableLogs { get; set; }

    public int LogRetentionDays { get; set; }

    public bool ConfirmManualShutdown { get; set; }
}
