using Control_OS_Lunix.Model;
using Control_OS_Lunix.Service;
using System.Runtime.InteropServices;

namespace Control_OS_Lunix;

public partial class MainDashboardForm : Form
{
    private const uint WmQueryEndSession = 0x0011;
    private const uint WmEndSession = 0x0016;
    private const int ShutdownPriorityLate = 0x3FF;

    private readonly JsonConfigurationStore _configurationStore;
    private readonly LogService _logService;
    private readonly DevicePowerService _devicePowerService;
    private readonly ControllerOrchestrator _controllerOrchestrator;
    private readonly NetworkScannerService _networkScannerService;

    private readonly Label _totalDevicesValue = CreateSummaryValueLabel();
    private readonly Label _onlineDevicesValue = CreateSummaryValueLabel();
    private readonly Label _activeDevicesValue = CreateSummaryValueLabel();
    private readonly Label _lastActionValue = CreateSummaryValueLabel();
    private readonly DataGridView _deviceGrid = new();
    private readonly Button _addButton = CreateActionButton("Add Device");
    private readonly Button _scanButton = CreateActionButton("Scan Network");
    private readonly Button _editButton = CreateActionButton("Edit");
    private readonly Button _deleteButton = CreateActionButton("Delete");
    private readonly Button _startButton = CreateActionButton("Start");
    private readonly Button _rebootButton = CreateActionButton("Reboot");
    private readonly Button _shutdownButton = CreateActionButton("Shutdown");
    private readonly Button _refreshButton = CreateActionButton("Refresh Status");
    private readonly Button _settingsButton = CreateActionButton("Settings");
    private readonly Button _logsButton = CreateActionButton("View Logs");
    private readonly Label _statusBanner = new() { AutoSize = true, ForeColor = Color.FromArgb(74, 85, 104) };

    private AppConfiguration _configuration = new();
    private bool _startupSequenceCompleted;
    private bool _shutdownSequenceInProgress;
    private bool _allowClose;

    public MainDashboardForm()
    {
        InitializeComponent();
        SetProcessShutdownParameters(ShutdownPriorityLate, 0);

        var credentialProtector = new CredentialProtector();
        _configurationStore = new JsonConfigurationStore(ApplicationPaths.ConfigurationFilePath, credentialProtector);
        _logService = new LogService(ApplicationPaths.LogFilePath);
        _devicePowerService = new DevicePowerService();
        _controllerOrchestrator = new ControllerOrchestrator(_devicePowerService, _logService);
        _networkScannerService = new NetworkScannerService();

        BuildLayout();
        WireEvents();
    }

    private void BuildLayout()
    {
        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            RowCount = 5,
            ColumnCount = 1,
            Padding = new Padding(20),
            BackColor = BackColor
        };
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        root.Controls.Add(BuildHeroPanel(), 0, 0);
        root.Controls.Add(BuildSummaryPanel(), 0, 1);
        root.Controls.Add(BuildToolbarPanel(), 0, 2);
        root.Controls.Add(BuildStatusPanel(), 0, 3);
        root.Controls.Add(BuildGridCard(), 0, 4);

        Controls.Add(root);
    }

    private Control BuildHeroPanel()
    {
        var panel = CreateCardPanel();
        panel.Margin = new Padding(0, 0, 0, 14);
        panel.Padding = new Padding(22);

        var title = new Label
        {
            Text = "LanPower Manager",
            AutoSize = true,
            Font = new Font("Segoe UI Semibold", 20F, FontStyle.Bold),
            ForeColor = Color.FromArgb(22, 36, 57)
        };

        var subtitle = new Label
        {
            Text = "Professional desktop control for Wake on LAN, Linux power commands, device discovery, and controller automation.",
            AutoSize = true,
            MaximumSize = new Size(980, 0),
            ForeColor = Color.FromArgb(91, 102, 122),
            Margin = new Padding(0, 10, 0, 0)
        };

        panel.Controls.Add(title);
        panel.Controls.Add(subtitle);
        return panel;
    }

    private Control BuildSummaryPanel()
    {
        var panel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 4,
            AutoSize = true,
            Margin = new Padding(0, 0, 0, 14)
        };
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25));
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25));
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25));
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25));

        panel.Controls.Add(CreateSummaryCard("Managed Devices", _totalDevicesValue, Color.FromArgb(36, 74, 136)), 0, 0);
        panel.Controls.Add(CreateSummaryCard("Online Now", _onlineDevicesValue, Color.FromArgb(24, 128, 84)), 1, 0);
        panel.Controls.Add(CreateSummaryCard("Active Targets", _activeDevicesValue, Color.FromArgb(180, 98, 21)), 2, 0);
        panel.Controls.Add(CreateSummaryCard("Last Action", _lastActionValue, Color.FromArgb(91, 33, 182)), 3, 0);

        return panel;
    }

    private Control BuildToolbarPanel()
    {
        var panel = CreateCardPanel();
        panel.Margin = new Padding(0, 0, 0, 12);
        panel.Padding = new Padding(18, 14, 18, 14);

        var flow = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            AutoSize = true,
            WrapContents = true,
            Margin = new Padding(0)
        };

        flow.Controls.AddRange([
            _addButton,
            _scanButton,
            _editButton,
            _deleteButton,
            _startButton,
            _rebootButton,
            _shutdownButton,
            _refreshButton,
            _settingsButton,
            _logsButton
        ]);

        panel.Controls.Add(flow);
        return panel;
    }

    private Control BuildStatusPanel()
    {
        var panel = CreateCardPanel();
        panel.Margin = new Padding(0, 0, 0, 12);
        panel.Padding = new Padding(18, 12, 18, 12);
        _statusBanner.Text = "Ready. Select a device or scan the network to begin.";
        panel.Controls.Add(_statusBanner);
        return panel;
    }

    private Control BuildGridCard()
    {
        var panel = CreateCardPanel();
        panel.Padding = new Padding(10);

        _deviceGrid.Dock = DockStyle.Fill;
        _deviceGrid.AllowUserToAddRows = false;
        _deviceGrid.AllowUserToDeleteRows = false;
        _deviceGrid.AllowUserToResizeRows = false;
        _deviceGrid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        _deviceGrid.BackgroundColor = Color.White;
        _deviceGrid.BorderStyle = BorderStyle.None;
        _deviceGrid.MultiSelect = false;
        _deviceGrid.ReadOnly = true;
        _deviceGrid.RowHeadersVisible = false;
        _deviceGrid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        _deviceGrid.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold);
        _deviceGrid.EnableHeadersVisualStyles = false;
        _deviceGrid.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(240, 244, 248);
        _deviceGrid.ColumnHeadersDefaultCellStyle.ForeColor = Color.FromArgb(50, 62, 82);
        _deviceGrid.DefaultCellStyle.SelectionBackColor = Color.FromArgb(224, 236, 255);
        _deviceGrid.DefaultCellStyle.SelectionForeColor = Color.FromArgb(36, 49, 66);
        _deviceGrid.RowsDefaultCellStyle.BackColor = Color.White;
        _deviceGrid.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(249, 250, 251);

        _deviceGrid.Columns.Add(CreateTextColumn("Name", "Name"));
        _deviceGrid.Columns.Add(CreateTextColumn("IpAddress", "IP Address"));
        _deviceGrid.Columns.Add(CreateTextColumn("Status", "Status"));
        _deviceGrid.Columns.Add(CreateCheckBoxColumn("AutoStartEnabled", "Auto Start"));
        _deviceGrid.Columns.Add(CreateCheckBoxColumn("AutoShutdownEnabled", "Auto Shutdown"));
        _deviceGrid.Columns.Add(CreateTextColumn("LastOperationSummary", "Last Operation"));
        _deviceGrid.Columns.Add(CreateTextColumn("Updated", "Updated"));

        panel.Controls.Add(_deviceGrid);
        return panel;
    }

    private void WireEvents()
    {
        Shown += MainDashboardForm_Shown;
        FormClosing += MainDashboardForm_FormClosing;
        _deviceGrid.SelectionChanged += (_, _) => UpdateActionButtons();
        _deviceGrid.CellDoubleClick += (_, _) => EditSelectedDevice();
        _addButton.Click += (_, _) => AddDevice();
        _scanButton.Click += (_, _) => OpenNetworkScanner();
        _editButton.Click += (_, _) => EditSelectedDevice();
        _deleteButton.Click += (_, _) => DeleteSelectedDevice();
        _startButton.Click += async (_, _) => await ExecuteOperationForSelectionAsync(DevicePowerOperation.Start);
        _rebootButton.Click += async (_, _) => await ExecuteOperationForSelectionAsync(DevicePowerOperation.Reboot);
        _shutdownButton.Click += async (_, _) => await ExecuteOperationForSelectionAsync(DevicePowerOperation.Shutdown);
        _refreshButton.Click += async (_, _) => await RefreshStatusesAsync();
        _settingsButton.Click += (_, _) => EditSettings();
        _logsButton.Click += async (_, _) => await ShowLogsAsync();
    }

    private async void MainDashboardForm_Shown(object? sender, EventArgs e)
    {
        try
        {
            _configuration = _configurationStore.Load();
            _logService.EnforceRetention(_configuration.GlobalSettings.LogRetentionDays);
            PopulateGrid();
            UpdateSummary();
            UpdateActionButtons();
            await RefreshStatusesAsync();

            if (!_startupSequenceCompleted && _configuration.GlobalSettings.AutoStartDevicesOnControllerBoot)
            {
                _startupSequenceCompleted = true;
                await RunControllerStartupAsync();
            }
        }
        catch (Exception exception)
        {
            MessageBox.Show(this, exception.Message, "Startup Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private async void MainDashboardForm_FormClosing(object? sender, FormClosingEventArgs e)
    {
        if (e.CloseReason == CloseReason.WindowsShutDown)
        {
            ExecuteWindowsShutdownSequence();
            return;
        }

        if (_allowClose || _shutdownSequenceInProgress || !_configuration.GlobalSettings.AutoShutdownDevicesOnControllerShutdown)
        {
            return;
        }

        _shutdownSequenceInProgress = true;
        e.Cancel = true;
        Enabled = false;

        try
        {
            await RunControllerShutdownAsync();
        }
        finally
        {
            _allowClose = true;
            Enabled = true;
            Close();
        }
    }

    protected override void WndProc(ref Message m)
    {
        if (m.Msg == WmQueryEndSession)
        {
            ExecuteWindowsShutdownSequence();
            m.Result = 1;
            return;
        }

        if (m.Msg == WmEndSession)
        {
            m.Result = 0;
            return;
        }

        base.WndProc(ref m);
    }

    private async Task RunControllerStartupAsync()
    {
        IReadOnlyList<DeviceOperationResult> results = await _controllerOrchestrator.ExecuteControllerStartupAsync(
            _configuration.Devices,
            _configuration.GlobalSettings);

        _configurationStore.Save(_configuration);
        PopulateGrid();
        UpdateSummary();
        SetStatus($"Startup sequence completed for {results.Count} device(s).");
        MessageBox.Show(this, $"Startup sequence completed for {results.Count} device(s).", "Controller Startup", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    private async Task RunControllerShutdownAsync()
    {
        IReadOnlyList<DeviceOperationResult> results = await _controllerOrchestrator.ExecuteControllerShutdownAsync(
            _configuration.Devices,
            _configuration.GlobalSettings);

        _configurationStore.Save(_configuration);
        PopulateGrid();
        UpdateSummary();
        SetStatus($"Shutdown sequence completed for {results.Count} device(s).");
    }

    private void ExecuteWindowsShutdownSequence()
    {
        if (_allowClose || _shutdownSequenceInProgress || !_configuration.GlobalSettings.AutoShutdownDevicesOnControllerShutdown)
        {
            return;
        }

        _shutdownSequenceInProgress = true;

        try
        {
            ShutdownBlockReasonCreate(Handle, "Waiting for remote devices to complete shutdown.");
            ExecuteControllerShutdownForWindowsShutdown();
        }
        finally
        {
            _allowClose = true;
            ShutdownBlockReasonDestroy(Handle);
        }
    }

    private void ExecuteControllerShutdownForWindowsShutdown()
    {
        Task.Run(async () =>
        {
            IReadOnlyList<DeviceOperationResult> results = await _controllerOrchestrator.ExecuteControllerShutdownAsync(
                _configuration.Devices,
                _configuration.GlobalSettings).ConfigureAwait(false);

            _configurationStore.Save(_configuration);

            if (_configuration.GlobalSettings.EnableLogs)
            {
                await _logService.WriteAsync(new OperationLogEntry
                {
                    DeviceName = "Controller",
                    OperationType = "ControllerShutdown",
                    Status = "Success",
                    TriggeredBy = OperationTrigger.ControllerShutdown.ToString(),
                    Summary = $"Windows shutdown sequence completed for {results.Count} device(s)."
                }).ConfigureAwait(false);
            }
        }).GetAwaiter().GetResult();
    }

    private void OpenNetworkScanner()
    {
        using var scanner = new NetworkScannerForm(_networkScannerService, _configuration.GlobalSettings);
        if (scanner.ShowDialog(this) != DialogResult.OK || scanner.Result is null)
        {
            return;
        }

        using var dialog = new DeviceDialogForm(_configuration.GlobalSettings, scanner.Result);
        if (dialog.ShowDialog(this) != DialogResult.OK || dialog.Result is null)
        {
            return;
        }

        _configuration.Devices.Add(dialog.Result);
        SaveAndRefresh();
        SetStatus($"Added scanned device '{dialog.Result.Name}'.");
    }

    private void AddDevice()
    {
        using var dialog = new DeviceDialogForm(_configuration.GlobalSettings);
        if (dialog.ShowDialog(this) != DialogResult.OK || dialog.Result is null)
        {
            return;
        }

        _configuration.Devices.Add(dialog.Result);
        SaveAndRefresh();
        SetStatus($"Added device '{dialog.Result.Name}'.");
    }

    private void EditSelectedDevice()
    {
        DevicePowerConfig? selectedDevice = GetSelectedDevice();
        if (selectedDevice is null)
        {
            return;
        }

        using var dialog = new DeviceDialogForm(_configuration.GlobalSettings, selectedDevice);
        if (dialog.ShowDialog(this) != DialogResult.OK || dialog.Result is null)
        {
            return;
        }

        int index = _configuration.Devices.FindIndex(device => device.DeviceId == selectedDevice.DeviceId);
        if (index >= 0)
        {
            _configuration.Devices[index] = dialog.Result;
            SaveAndRefresh();
            SetStatus($"Updated device '{dialog.Result.Name}'.");
        }
    }

    private void DeleteSelectedDevice()
    {
        DevicePowerConfig? selectedDevice = GetSelectedDevice();
        if (selectedDevice is null)
        {
            return;
        }

        DialogResult result = MessageBox.Show(
            this,
            $"Delete device '{selectedDevice.Name}'?",
            "Delete Device",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Warning);

        if (result != DialogResult.Yes)
        {
            return;
        }

        _configuration.Devices.RemoveAll(device => device.DeviceId == selectedDevice.DeviceId);
        SaveAndRefresh();
        SetStatus($"Deleted device '{selectedDevice.Name}'.");
    }

    private void EditSettings()
    {
        using var dialog = new SettingsDialogForm(_configuration.GlobalSettings);
        if (dialog.ShowDialog(this) != DialogResult.OK || dialog.Result is null)
        {
            return;
        }

        _configuration.GlobalSettings = dialog.Result;
        SaveAndRefresh();
        SetStatus("Global settings saved.");
    }

    private async Task ExecuteOperationForSelectionAsync(DevicePowerOperation operation)
    {
        DevicePowerConfig? selectedDevice = GetSelectedDevice();
        if (selectedDevice is null)
        {
            return;
        }

        if (!selectedDevice.ManualControlEnabled)
        {
            MessageBox.Show(this, "Manual control is disabled for this device.", "Manual Control", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        if (operation == DevicePowerOperation.Shutdown &&
            _configuration.GlobalSettings.ConfirmManualShutdown &&
            MessageBox.Show(this, $"Shutdown '{selectedDevice.Name}'?", "Confirm Shutdown", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
        {
            return;
        }

        ToggleBusyState(true);
        SetStatus($"{operation} operation is running for '{selectedDevice.Name}'...");

        try
        {
            DeviceOperationResult result = await _controllerOrchestrator.ExecuteOperationAsync(
                selectedDevice,
                _configuration.GlobalSettings,
                operation,
                OperationTrigger.Manual);

            _configurationStore.Save(_configuration);
            PopulateGrid();
            UpdateSummary();
            SetStatus(result.Message);

            MessageBoxIcon icon = result.IsSuccess
                ? (result.HasWarning ? MessageBoxIcon.Warning : MessageBoxIcon.Information)
                : MessageBoxIcon.Error;

            MessageBox.Show(this, result.Message, operation.ToString(), MessageBoxButtons.OK, icon);
        }
        finally
        {
            ToggleBusyState(false);
        }
    }

    private async Task RefreshStatusesAsync()
    {
        ToggleBusyState(true);
        SetStatus("Refreshing device reachability...");

        try
        {
            foreach (DevicePowerConfig device in _configuration.Devices)
            {
                DevicePowerReport report = await _devicePowerService.GetReportAsync(
                    device,
                    _configuration.GlobalSettings.PingTimeoutSeconds);

                device.LastKnownStatus = report.Status;
                device.LastUpdatedDateUtc = report.CheckedAtUtc;
            }

            _configurationStore.Save(_configuration);
            PopulateGrid();
            UpdateSummary();
            SetStatus("Device statuses were refreshed.");
        }
        finally
        {
            ToggleBusyState(false);
        }
    }

    private async Task ShowLogsAsync()
    {
        IReadOnlyList<string> lines = await _logService.ReadRecentLinesAsync(500);
        using var dialog = new LogsForm(lines);
        dialog.ShowDialog(this);
    }

    private DevicePowerConfig? GetSelectedDevice()
    {
        if (_deviceGrid.SelectedRows.Count == 0)
        {
            return null;
        }

        Guid deviceId = (Guid)_deviceGrid.SelectedRows[0].Tag!;
        return _configuration.Devices.FirstOrDefault(device => device.DeviceId == deviceId);
    }

    private void PopulateGrid()
    {
        _deviceGrid.Rows.Clear();

        foreach (DevicePowerConfig device in _configuration.Devices.OrderBy(device => device.Name))
        {
            int rowIndex = _deviceGrid.Rows.Add(
                device.Name,
                device.IpAddress,
                device.LastKnownStatus,
                device.AutoStartEnabled,
                device.AutoShutdownEnabled,
                device.LastOperationSummary,
                device.LastUpdatedDateUtc == default
                    ? "-"
                    : device.LastUpdatedDateUtc.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss"));

            _deviceGrid.Rows[rowIndex].Tag = device.DeviceId;
        }
    }

    private void SaveAndRefresh()
    {
        _configurationStore.Save(_configuration);
        PopulateGrid();
        UpdateSummary();
        UpdateActionButtons();
    }

    private void UpdateSummary()
    {
        _totalDevicesValue.Text = _configuration.Devices.Count.ToString();
        _onlineDevicesValue.Text = _configuration.Devices.Count(device => device.LastKnownStatus == DevicePowerStatus.Online).ToString();
        _activeDevicesValue.Text = _configuration.Devices.Count(device => device.IsActive).ToString();
        _lastActionValue.Text = _configuration.Devices
            .OrderByDescending(device => device.LastUpdatedDateUtc)
            .Select(device => device.LastOperationSummary)
            .FirstOrDefault() ?? "No activity";
    }

    private void UpdateActionButtons()
    {
        bool hasSelection = GetSelectedDevice() is not null;
        _editButton.Enabled = hasSelection;
        _deleteButton.Enabled = hasSelection;
        _startButton.Enabled = hasSelection;
        _rebootButton.Enabled = hasSelection;
        _shutdownButton.Enabled = hasSelection;
    }

    private void ToggleBusyState(bool isBusy)
    {
        UseWaitCursor = isBusy;

        foreach (Control control in new Control[]
                 {
                     _addButton, _scanButton, _editButton, _deleteButton, _startButton, _rebootButton,
                     _shutdownButton, _refreshButton, _settingsButton, _logsButton, _deviceGrid
                 })
        {
            control.Enabled = !isBusy;
        }

        if (!isBusy)
        {
            UpdateActionButtons();
        }
    }

    private void SetStatus(string message)
    {
        _statusBanner.Text = message;
    }

    private static Panel CreateCardPanel()
    {
        return new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.White
        };
    }

    private static Panel CreateSummaryCard(string title, Label valueLabel, Color accentColor)
    {
        var card = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.White,
            Margin = new Padding(0, 0, 14, 0),
            Padding = new Padding(18)
        };

        var accent = new Panel
        {
            Dock = DockStyle.Left,
            Width = 4,
            BackColor = accentColor
        };

        var content = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            AutoSize = true,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            Margin = new Padding(12, 0, 0, 0)
        };

        content.Controls.Add(new Label
        {
            Text = title,
            AutoSize = true,
            ForeColor = Color.FromArgb(91, 102, 122)
        });
        content.Controls.Add(valueLabel);

        card.Controls.Add(content);
        card.Controls.Add(accent);
        return card;
    }

    private static Label CreateSummaryValueLabel()
    {
        return new Label
        {
            AutoSize = true,
            Font = new Font("Segoe UI Semibold", 14F, FontStyle.Bold),
            Margin = new Padding(0, 8, 0, 0),
            ForeColor = Color.FromArgb(22, 36, 57)
        };
    }

    private static Button CreateActionButton(string text)
    {
        var button = new Button
        {
            Text = text,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            BackColor = Color.FromArgb(244, 247, 252),
            FlatStyle = FlatStyle.Flat,
            Margin = new Padding(0, 0, 10, 8),
            Padding = new Padding(14, 8, 14, 8)
        };

        button.FlatAppearance.BorderColor = Color.FromArgb(221, 228, 237);
        button.FlatAppearance.MouseDownBackColor = Color.FromArgb(225, 235, 250);
        button.FlatAppearance.MouseOverBackColor = Color.FromArgb(234, 241, 251);

        return button;
    }

    private static DataGridViewTextBoxColumn CreateTextColumn(string name, string headerText)
    {
        return new DataGridViewTextBoxColumn
        {
            Name = name,
            HeaderText = headerText,
            SortMode = DataGridViewColumnSortMode.NotSortable
        };
    }

    private static DataGridViewCheckBoxColumn CreateCheckBoxColumn(string name, string headerText)
    {
        return new DataGridViewCheckBoxColumn
        {
            Name = name,
            HeaderText = headerText,
            SortMode = DataGridViewColumnSortMode.NotSortable
        };
    }

    [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool ShutdownBlockReasonCreate(IntPtr hWnd, string pwszReason);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool ShutdownBlockReasonDestroy(IntPtr hWnd);

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SetProcessShutdownParameters(int dwLevel, int dwFlags);
}
