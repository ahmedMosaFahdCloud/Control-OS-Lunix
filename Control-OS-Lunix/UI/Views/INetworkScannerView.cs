using Control_OS_Lunix.UI.ViewModels;

namespace Control_OS_Lunix.UI.Views;

public interface INetworkScannerView
{
    event EventHandler ScanRequested;
    event EventHandler UseSelectionRequested;
    event EventHandler CancelRequested;

    string SubnetPrefix { get; set; }

    int StartHost { get; set; }

    int EndHost { get; set; }

    int TimeoutMs { get; set; }

    int MaxConcurrency { get; set; }

    string SelectedIpAddress { get; }

    void BindResults(IReadOnlyList<NetworkScanRowViewModel> rows);

    void SetProgress(int value);

    void SetBusy(bool isBusy);

    void SetStatus(string message);

    void ShowError(string message, string title);

    DialogResult ShowDialogView(IWin32Window owner);

    void CloseView(DialogResult result);
}
