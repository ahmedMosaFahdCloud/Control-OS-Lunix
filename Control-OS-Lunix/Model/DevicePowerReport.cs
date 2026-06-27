namespace Control_OS_Lunix.Model;

public sealed class DevicePowerReport
{
    public required Guid DeviceId { get; init; }

    public required string Name { get; init; }

    public required string IpAddress { get; init; }

    public required DevicePowerStatus Status { get; init; }

    public string Message { get; init; } = string.Empty;

    public DateTime CheckedAtUtc { get; init; } = DateTime.UtcNow;
}
