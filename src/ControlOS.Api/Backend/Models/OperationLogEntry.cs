namespace ControlOS.Api.Backend.Models;

public sealed class OperationLogEntry
{
    public Guid OperationId { get; init; } = Guid.NewGuid();

    public Guid? DeviceId { get; init; }

    public string DeviceName { get; init; } = "Controller";

    public string OperationType { get; init; } = string.Empty;

    public string Status { get; init; } = string.Empty;

    public DateTime StartedAtUtc { get; init; } = DateTime.UtcNow;

    public DateTime FinishedAtUtc { get; init; } = DateTime.UtcNow;

    public string ErrorMessage { get; init; } = string.Empty;

    public string TriggeredBy { get; init; } = string.Empty;

    public string Summary { get; init; } = string.Empty;
}
