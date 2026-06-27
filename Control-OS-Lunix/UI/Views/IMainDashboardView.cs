using Control_OS_Lunix.UI.ViewModels;

namespace Control_OS_Lunix.UI.Views;

public interface IMainDashboardView
{
    event EventHandler ViewLoaded;
    event EventHandler AddRequested;
    event EventHandler ScanRequested;
    event EventHandler EditRequested;
    event EventHandler DeleteRequested;
    event EventHandler StartRequested;
    event EventHandler RebootRequested;
    event EventHandler ShutdownRequested;
    event EventHandler RefreshRequested;
    event EventHandler SettingsRequested;
    event EventHandler LogsRequested;
    event FormClosingEventHandler ViewClosing;

    Func<WindowsShutdownDecision>? WindowsShutdownHandler { get; set; }

    Guid? SelectedDeviceId { get; }

    IWin32Window OwnerWindow { get; }

    void BindDevices(IReadOnlyList<DeviceGridRowViewModel> devices);

    void UpdateSummary(DashboardSummaryViewModel summary);

    void SetStatus(string message);

    void SetBusy(bool isBusy);

    void ShowInfo(string message, string title);

    void ShowWarning(string message, string title);

    void ShowError(string message, string title);

    bool Confirm(string message, string title);

    void RequestClose();
}
