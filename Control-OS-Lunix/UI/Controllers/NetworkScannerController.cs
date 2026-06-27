using Control_OS_Lunix.Backend.Interfaces;
using Control_OS_Lunix.Backend.Models;
using Control_OS_Lunix.Backend.Results;
using Control_OS_Lunix.UI.ViewModels;
using Control_OS_Lunix.UI.Views;
using Microsoft.Extensions.DependencyInjection;

namespace Control_OS_Lunix.UI.Controllers;

public sealed class NetworkScannerController : INetworkScannerController
{
    private readonly IServiceProvider _serviceProvider;
    private readonly INetworkScannerService _networkScannerService;

    private TaskCompletionSource<Result<DevicePowerConfig?>>? _completionSource;
    private INetworkScannerView? _view;
    private IReadOnlyList<NetworkScanResult> _results = [];
    private GlobalSettings _settings = new();

    public NetworkScannerController(IServiceProvider serviceProvider, INetworkScannerService networkScannerService)
    {
        _serviceProvider = serviceProvider;
        _networkScannerService = networkScannerService;
    }

    public Task<Result<DevicePowerConfig?>> ShowAsync(IWin32Window owner, GlobalSettings settings)
    {
        _settings = settings;
        _completionSource = new TaskCompletionSource<Result<DevicePowerConfig?>>();
        _view = _serviceProvider.GetRequiredService<NetworkScannerView>();
        _view.ScanRequested += async (_, _) => await HandleScanRequestedAsync();
        _view.UseSelectionRequested += HandleUseSelectionRequested;
        _view.CancelRequested += HandleCancelRequested;

        Result<string> subnetResult = _networkScannerService.GetSuggestedSubnet();
        _view.SubnetPrefix = subnetResult.IsSuccess ? subnetResult.Value ?? "192.168.1" : "192.168.1";
        _view.TimeoutMs = Math.Clamp(settings.PingTimeoutSeconds * 1000, 100, 10000);
        _view.ShowDialogView(owner);
        return Task.FromResult(_completionSource.Task.GetAwaiter().GetResult());
    }

    private async Task HandleScanRequestedAsync()
    {
        _view!.SetBusy(true);
        _view.SetProgress(0);
        _view.SetStatus("Scanning network...");

        try
        {
            var progress = new Progress<int>(value =>
            {
                _view.SetProgress(value);
                _view.SetStatus($"Scanning network... {value}%");
            });

            Result<IReadOnlyList<NetworkScanResult>> result = await _networkScannerService.ScanSubnetAsync(
                _view.SubnetPrefix,
                _view.StartHost,
                _view.EndHost,
                _view.TimeoutMs,
                _view.MaxConcurrency,
                progress);

            if (!result.IsSuccess)
            {
                _view.ShowError(result.Message, "Scan Error");
                _view.SetStatus("Scan failed.");
                return;
            }

            _results = result.Value ?? [];
            _view.BindResults(_results.Select(scan => new NetworkScanRowViewModel
            {
                IpAddress = scan.IpAddress,
                HostName = string.IsNullOrWhiteSpace(scan.HostName) ? "-" : scan.HostName,
                MacAddress = string.IsNullOrWhiteSpace(scan.MacAddress) ? "-" : scan.MacAddress,
                ResponseTimeText = $"{scan.ResponseTimeMs} ms",
                Summary = scan.Summary
            }).ToArray());

            _view.SetStatus(_results.Count == 0
                ? "Scan completed. No reachable hosts were found."
                : $"Scan completed. Found {_results.Count} reachable host(s).");
        }
        finally
        {
            _view.SetBusy(false);
        }
    }

    private void HandleUseSelectionRequested(object? sender, EventArgs e)
    {
        NetworkScanResult? selected = _results.FirstOrDefault(result =>
            result.IpAddress.Equals(_view!.SelectedIpAddress, StringComparison.OrdinalIgnoreCase));

        if (selected is null)
        {
            return;
        }

        _completionSource!.SetResult(Result<DevicePowerConfig?>.Success(new DevicePowerConfig
        {
            Name = string.IsNullOrWhiteSpace(selected.HostName) ? $"Device {selected.IpAddress}" : selected.HostName,
            IpAddress = selected.IpAddress,
            MacAddress = selected.MacAddress,
            SshHost = selected.IpAddress,
            BroadcastAddress = $"{_view!.SubnetPrefix}.255",
            WolPort = _settings.DefaultWolPort,
            TimeoutSeconds = _settings.SshTimeoutSeconds,
            RetryCount = _settings.RetryCount,
            AutoStartEnabled = true,
            AutoShutdownEnabled = true,
            ManualControlEnabled = true,
            IsActive = true
        }));
        _view!.CloseView(DialogResult.OK);
    }

    private void HandleCancelRequested(object? sender, EventArgs e)
    {
        _completionSource!.SetResult(Result<DevicePowerConfig?>.Success(null));
        _view!.CloseView(DialogResult.Cancel);
    }
}
