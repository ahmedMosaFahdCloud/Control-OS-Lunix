namespace Control_OS_Lunix.UI.ViewModels;

public sealed class WindowsShutdownDecision
{
    public bool AllowSessionEnd { get; init; } = true;

    public string BlockReason { get; init; } = string.Empty;
}
