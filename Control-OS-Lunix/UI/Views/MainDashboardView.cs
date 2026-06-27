using System.Runtime.InteropServices;
using Control_OS_Lunix.UI.ViewModels;

namespace Control_OS_Lunix.UI.Views;

public sealed class MainDashboardView : Form, IMainDashboardView
{
    private const uint WmQueryEndSession = 0x0011;

    private readonly Label _totalDevicesValue = CreateSummaryValueLabel();
    private readonly Label _onlineDevicesValue = CreateSummaryValueLabel();
    private readonly Label _activeDevicesValue = CreateSummaryValueLabel();
    private readonly Label _lastActionValue = CreateSummaryValueLabel();
    private readonly Label _statusBanner = new() { AutoSize = true, ForeColor = Color.FromArgb(74, 85, 104) };
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

    public MainDashboardView()
    {
        Text = "LanPower Manager";
        StartPosition = FormStartPosition.CenterScreen;
        BackColor = Color.FromArgb(243, 246, 249);
        Width = 1420;
        Height = 820;
        MinimumSize = new Size(1160, 720);
        BuildLayout();
        WireEvents();
    }

    public event EventHandler? ViewLoaded;
    public event EventHandler? AddRequested;
    public event EventHandler? ScanRequested;
    public event EventHandler? EditRequested;
    public event EventHandler? DeleteRequested;
    public event EventHandler? StartRequested;
    public event EventHandler? RebootRequested;
    public event EventHandler? ShutdownRequested;
    public event EventHandler? RefreshRequested;
    public event EventHandler? SettingsRequested;
    public event EventHandler? LogsRequested;
    public event FormClosingEventHandler? ViewClosing;

    public Func<WindowsShutdownDecision>? WindowsShutdownHandler { get; set; }
    public Guid? SelectedDeviceId => _deviceGrid.SelectedRows.Count == 0 ? null : (Guid?)_deviceGrid.SelectedRows[0].Tag;
    public IWin32Window OwnerWindow => this;

    public void BindDevices(IReadOnlyList<DeviceGridRowViewModel> devices)
    {
        _deviceGrid.Rows.Clear();
        foreach (DeviceGridRowViewModel device in devices)
        {
            int rowIndex = _deviceGrid.Rows.Add(
                device.Name,
                device.IpAddress,
                device.Status,
                device.AutoStartEnabled,
                device.AutoShutdownEnabled,
                device.LastOperationSummary,
                device.UpdatedText);

            _deviceGrid.Rows[rowIndex].Tag = device.DeviceId;
        }

        UpdateSelectionButtons();
    }

    public void UpdateSummary(DashboardSummaryViewModel summary)
    {
        _totalDevicesValue.Text = summary.TotalDevices;
        _onlineDevicesValue.Text = summary.OnlineDevices;
        _activeDevicesValue.Text = summary.ActiveDevices;
        _lastActionValue.Text = summary.LastAction;
    }

    public void SetStatus(string message) => _statusBanner.Text = message;

    public void SetBusy(bool isBusy)
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
            UpdateSelectionButtons();
        }
    }

    public void ShowInfo(string message, string title) => MessageBox.Show(this, message, title, MessageBoxButtons.OK, MessageBoxIcon.Information);
    public void ShowWarning(string message, string title) => MessageBox.Show(this, message, title, MessageBoxButtons.OK, MessageBoxIcon.Warning);
    public void ShowError(string message, string title) => MessageBox.Show(this, message, title, MessageBoxButtons.OK, MessageBoxIcon.Error);
    public bool Confirm(string message, string title) => MessageBox.Show(this, message, title, MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes;
    public void RequestClose() => Close();

    protected override void WndProc(ref Message m)
    {
        if (m.Msg == WmQueryEndSession && WindowsShutdownHandler is not null)
        {
            WindowsShutdownDecision decision = WindowsShutdownHandler.Invoke();
            if (!string.IsNullOrWhiteSpace(decision.BlockReason))
            {
                ShutdownBlockReasonCreate(Handle, decision.BlockReason);
            }

            m.Result = decision.AllowSessionEnd ? 1 : 0;

            if (!string.IsNullOrWhiteSpace(decision.BlockReason))
            {
                ShutdownBlockReasonDestroy(Handle);
            }
            return;
        }

        base.WndProc(ref m);
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

    private void WireEvents()
    {
        Shown += (_, _) => ViewLoaded?.Invoke(this, EventArgs.Empty);
        FormClosing += (sender, args) => ViewClosing?.Invoke(sender, args);
        _deviceGrid.SelectionChanged += (_, _) => UpdateSelectionButtons();
        _deviceGrid.CellDoubleClick += (_, _) => EditRequested?.Invoke(this, EventArgs.Empty);
        _addButton.Click += (_, _) => AddRequested?.Invoke(this, EventArgs.Empty);
        _scanButton.Click += (_, _) => ScanRequested?.Invoke(this, EventArgs.Empty);
        _editButton.Click += (_, _) => EditRequested?.Invoke(this, EventArgs.Empty);
        _deleteButton.Click += (_, _) => DeleteRequested?.Invoke(this, EventArgs.Empty);
        _startButton.Click += (_, _) => StartRequested?.Invoke(this, EventArgs.Empty);
        _rebootButton.Click += (_, _) => RebootRequested?.Invoke(this, EventArgs.Empty);
        _shutdownButton.Click += (_, _) => ShutdownRequested?.Invoke(this, EventArgs.Empty);
        _refreshButton.Click += (_, _) => RefreshRequested?.Invoke(this, EventArgs.Empty);
        _settingsButton.Click += (_, _) => SettingsRequested?.Invoke(this, EventArgs.Empty);
        _logsButton.Click += (_, _) => LogsRequested?.Invoke(this, EventArgs.Empty);
    }

    private void UpdateSelectionButtons()
    {
        bool hasSelection = SelectedDeviceId.HasValue;
        _editButton.Enabled = hasSelection;
        _deleteButton.Enabled = hasSelection;
        _startButton.Enabled = hasSelection;
        _rebootButton.Enabled = hasSelection;
        _shutdownButton.Enabled = hasSelection;
    }

    private Control BuildHeroPanel()
    {
        var panel = CreateCardPanel();
        panel.Margin = new Padding(0, 0, 0, 14);
        panel.Padding = new Padding(22);
        panel.Controls.Add(new Label
        {
            Text = "LanPower Manager",
            AutoSize = true,
            Font = new Font("Segoe UI Semibold", 20F, FontStyle.Bold),
            ForeColor = Color.FromArgb(22, 36, 57)
        });
        panel.Controls.Add(new Label
        {
            Text = "Professional desktop control for Wake on LAN, Linux power commands, device discovery, and controller automation.",
            AutoSize = true,
            MaximumSize = new Size(980, 0),
            ForeColor = Color.FromArgb(91, 102, 122),
            Margin = new Padding(0, 10, 0, 0)
        });
        return panel;
    }

    private Control BuildSummaryPanel()
    {
        var panel = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 4, AutoSize = true, Margin = new Padding(0, 0, 0, 14) };
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
        var flow = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoSize = true, WrapContents = true, Margin = new Padding(0) };
        flow.Controls.AddRange([_addButton, _scanButton, _editButton, _deleteButton, _startButton, _rebootButton, _shutdownButton, _refreshButton, _settingsButton, _logsButton]);
        panel.Controls.Add(flow);
        return panel;
    }

    private Control BuildStatusPanel()
    {
        var panel = CreateCardPanel();
        panel.Margin = new Padding(0, 0, 0, 12);
        panel.Padding = new Padding(18, 12, 18, 12);
        panel.Controls.Add(_statusBanner);
        _statusBanner.Text = "Ready. Select a device or scan the network to begin.";
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

    private static Panel CreateCardPanel() => new() { Dock = DockStyle.Fill, BackColor = Color.White };

    private static Panel CreateSummaryCard(string title, Label valueLabel, Color accentColor)
    {
        var card = new Panel { Dock = DockStyle.Fill, BackColor = Color.White, Margin = new Padding(0, 0, 14, 0), Padding = new Padding(18) };
        var accent = new Panel { Dock = DockStyle.Left, Width = 4, BackColor = accentColor };
        var content = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoSize = true, FlowDirection = FlowDirection.TopDown, WrapContents = false, Margin = new Padding(12, 0, 0, 0) };
        content.Controls.Add(new Label { Text = title, AutoSize = true, ForeColor = Color.FromArgb(91, 102, 122) });
        content.Controls.Add(valueLabel);
        card.Controls.Add(content);
        card.Controls.Add(accent);
        return card;
    }

    private static Label CreateSummaryValueLabel() => new()
    {
        AutoSize = true,
        Font = new Font("Segoe UI Semibold", 14F, FontStyle.Bold),
        Margin = new Padding(0, 8, 0, 0),
        ForeColor = Color.FromArgb(22, 36, 57)
    };

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

    private static DataGridViewTextBoxColumn CreateTextColumn(string name, string headerText) =>
        new() { Name = name, HeaderText = headerText, SortMode = DataGridViewColumnSortMode.NotSortable };

    private static DataGridViewCheckBoxColumn CreateCheckBoxColumn(string name, string headerText) =>
        new() { Name = name, HeaderText = headerText, SortMode = DataGridViewColumnSortMode.NotSortable };

    [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool ShutdownBlockReasonCreate(IntPtr hWnd, string pwszReason);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool ShutdownBlockReasonDestroy(IntPtr hWnd);
}
