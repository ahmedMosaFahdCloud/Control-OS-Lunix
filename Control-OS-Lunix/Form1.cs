using Control_OS_Lunix.Model;
using Control_OS_Lunix.Service;
using System.Runtime.InteropServices;

namespace Control_OS_Lunix;

public partial class Form1 : Form
{
    private const uint WmQueryEndSession = 0x0011;
    private const uint WmEndSession = 0x0016;
    private const int ShutdownPriorityLate = 0x3FF;

    private readonly JsonConfigurationStore _configurationStore;
    private readonly LogService _logService;
    private readonly DevicePowerService _devicePowerService;
    private readonly ControllerOrchestrator _controllerOrchestrator;

    private readonly Label _totalDevicesValue = CreateSummaryValueLabel();
    private readonly Label _onlineDevicesValue = CreateSummaryValueLabel();
    private readonly Label _activeDevicesValue = CreateSummaryValueLabel();
    private readonly Label _lastActionValue = CreateSummaryValueLabel();
    private readonly DataGridView _deviceGrid = new();
    private readonly Button _addButton = new() { Text = "Add Device", AutoSize = true };
    private readonly Button _editButton = new() { Text = "Edit", AutoSize = true };
    private readonly Button _deleteButton = new() { Text = "Delete", AutoSize = true };
    private readonly Button _startButton = new() { Text = "Start", AutoSize = true };
    private readonly Button _rebootButton = new() { Text = "Reboot", AutoSize = true };
    private readonly Button _shutdownButton = new() { Text = "Shutdown", AutoSize = true };
    private readonly Button _refreshButton = new() { Text = "Refresh Status", AutoSize = true };
    private readonly Button _settingsButton = new() { Text = "Settings", AutoSize = true };
    private readonly Button _logsButton = new() { Text = "View Logs", AutoSize = true };

    private AppConfiguration _configuration = new();
    private bool _startupSequenceCompleted;
    private bool _shutdownSequenceInProgress;
    private bool _allowClose;

    public Form1()
    {
        InitializeComponent();
        SetProcessShutdownParameters(ShutdownPriorityLate, 0);

        var credentialProtector = new CredentialProtector();
        _configurationStore = new JsonConfigurationStore(ApplicationPaths.ConfigurationFilePath, credentialProtector);
        _logService = new LogService(ApplicationPaths.LogFilePath);
        _devicePowerService = new DevicePowerService();
        _controllerOrchestrator = new ControllerOrchestrator(_devicePowerService, _logService);

        BuildLayout();
        WireEvents();
    }

    private void BuildLayout()
    {
        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            RowCount = 4,
            ColumnCount = 1,
            Padding = new Padding(16)
        };
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        root.Controls.Add(BuildHeaderPanel(), 0, 0);
        root.Controls.Add(BuildSummaryPanel(), 0, 1);
        root.Controls.Add(BuildToolbarPanel(), 0, 2);
        root.Controls.Add(BuildGridPanel(), 0, 3);

        Controls.Add(root);
    }

    private Control BuildHeaderPanel()
    {
        var panel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            AutoSize = true,
            Margin = new Padding(0, 0, 0, 12)
        };

        panel.Controls.Add(new Label
        {
            Text = "Network Power Control System",
            Font = new Font(Font.FontFamily, 18, FontStyle.Bold),
            AutoSize = true
        }, 0, 0);

        panel.Controls.Add(new Label
        {
            Text = "Manage Wake on LAN, SSH shutdown and reboot, status reporting, and controller startup or shutdown automation.",
            ForeColor = Color.DimGray,
            AutoSize = true,
            Margin = new Padding(0, 8, 0, 0)
        }, 0, 1);

        return panel;
    }

    private Control BuildSummaryPanel()
    {
        var panel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 4,
            AutoSize = true,
            Margin = new Padding(0, 0, 0, 12)
        };

        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25));
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25));
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25));
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25));

        panel.Controls.Add(CreateSummaryCard("Total Devices", _totalDevicesValue), 0, 0);
        panel.Controls.Add(CreateSummaryCard("Online", _onlineDevicesValue), 1, 0);
        panel.Controls.Add(CreateSummaryCard("Active", _activeDevicesValue), 2, 0);
        panel.Controls.Add(CreateSummaryCard("Last Action", _lastActionValue), 3, 0);

        return panel;
    }

    private Control BuildToolbarPanel()
    {
        var panel = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = true,
            AutoSize = true,
            Margin = new Padding(0, 0, 0, 12)
        };

        panel.Controls.AddRange([
            _addButton,
            _editButton,
            _deleteButton,
            _startButton,
            _rebootButton,
            _shutdownButton,
            _refreshButton,
            _settingsButton,
            _logsButton
        ]);

        return panel;
    }

    private Control BuildGridPanel()
    {
        _deviceGrid.Dock = DockStyle.Fill;
        _deviceGrid.AllowUserToAddRows = false;
        _deviceGrid.AllowUserToDeleteRows = false;
        _deviceGrid.AllowUserToResizeRows = false;
        _deviceGrid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        _deviceGrid.BackgroundColor = SystemColors.Window;
        _deviceGrid.MultiSelect = false;
        _deviceGrid.ReadOnly = true;
        _deviceGrid.RowHeadersVisible = false;
        _deviceGrid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;

        _deviceGrid.Columns.Add(CreateTextColumn("Name", "Name"));
        _deviceGrid.Columns.Add(CreateTextColumn("IpAddress", "IP Address"));
        _deviceGrid.Columns.Add(CreateTextColumn("Status", "Status"));
        _deviceGrid.Columns.Add(CreateCheckBoxColumn("AutoStartEnabled", "Auto Start"));
        _deviceGrid.Columns.Add(CreateCheckBoxColumn("AutoShutdownEnabled", "Auto Shutdown"));
        _deviceGrid.Columns.Add(CreateTextColumn("LastOperationSummary", "Last Operation"));
        _deviceGrid.Columns.Add(CreateTextColumn("Updated", "Updated"));

        return _deviceGrid;
    }

    private void WireEvents()
    {
        Shown += Form1_Shown;
        FormClosing += Form1_FormClosing;
        _deviceGrid.SelectionChanged += (_, _) => UpdateActionButtons();
        _deviceGrid.CellDoubleClick += (_, _) => EditSelectedDevice();
        _addButton.Click += (_, _) => AddDevice();
        _editButton.Click += (_, _) => EditSelectedDevice();
        _deleteButton.Click += (_, _) => DeleteSelectedDevice();
        _startButton.Click += async (_, _) => await ExecuteOperationForSelectionAsync(DevicePowerOperation.Start);
        _rebootButton.Click += async (_, _) => await ExecuteOperationForSelectionAsync(DevicePowerOperation.Reboot);
        _shutdownButton.Click += async (_, _) => await ExecuteOperationForSelectionAsync(DevicePowerOperation.Shutdown);
        _refreshButton.Click += async (_, _) => await RefreshStatusesAsync();
        _settingsButton.Click += (_, _) => EditSettings();
        _logsButton.Click += async (_, _) => await ShowLogsAsync();
    }

    private async void Form1_Shown(object? sender, EventArgs e)
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

    private async void Form1_FormClosing(object? sender, FormClosingEventArgs e)
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

        string summary = $"Startup sequence completed for {results.Count} device(s).";
        _lastActionValue.Text = summary;
        MessageBox.Show(this, summary, "Controller Startup", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    private async Task RunControllerShutdownAsync()
    {
        IReadOnlyList<DeviceOperationResult> results = await _controllerOrchestrator.ExecuteControllerShutdownAsync(
            _configuration.Devices,
            _configuration.GlobalSettings);

        _configurationStore.Save(_configuration);
        PopulateGrid();
        UpdateSummary();

        _lastActionValue.Text = $"Shutdown sequence completed for {results.Count} device(s).";
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

    private void AddDevice()
    {
        using var dialog = new DeviceDialogForm(_configuration.GlobalSettings);
        if (dialog.ShowDialog(this) != DialogResult.OK || dialog.Result is null)
        {
            return;
        }

        _configuration.Devices.Add(dialog.Result);
        SaveAndRefresh();
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
                     _addButton, _editButton, _deleteButton, _startButton, _rebootButton, _shutdownButton,
                     _refreshButton, _settingsButton, _logsButton, _deviceGrid
                 })
        {
            control.Enabled = !isBusy;
        }

        if (!isBusy)
        {
            UpdateActionButtons();
        }
    }

    private static Control CreateSummaryCard(string title, Label valueLabel)
    {
        var card = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            AutoSize = true,
            Padding = new Padding(12),
            Margin = new Padding(0, 0, 12, 0),
            BackColor = Color.WhiteSmoke
        };

        card.Controls.Add(new Label
        {
            Text = title,
            AutoSize = true,
            ForeColor = Color.DimGray
        }, 0, 0);

        card.Controls.Add(valueLabel, 0, 1);
        return card;
    }

    private static Label CreateSummaryValueLabel()
    {
        return new Label
        {
            AutoSize = true,
            Font = new Font(SystemFonts.DefaultFont.FontFamily, 14, FontStyle.Bold),
            Margin = new Padding(0, 8, 0, 0)
        };
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
