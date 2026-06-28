namespace Control_OS_Lunix.Backend.Models;

public sealed class GlobalSettings
{
    public bool AutoStartDevicesOnControllerBoot { get; set; } = true;

    public bool AutoShutdownDevicesOnControllerShutdown { get; set; } = true;

    public int DelayBetweenCommandsMs { get; set; } = 1000;

    public int PingTimeoutSeconds { get; set; } = 5;

    public int SshTimeoutSeconds { get; set; } = 10;

    public int RetryCount { get; set; } = 1;

    public int DefaultWolPort { get; set; } = 9;

    public string DefaultBroadcastAddress { get; set; } = "255.255.255.255";

    public bool EnableLogs { get; set; } = true;

    public int LogRetentionDays { get; set; } = 30;

    public bool ConfirmManualShutdown { get; set; } = true;
}
