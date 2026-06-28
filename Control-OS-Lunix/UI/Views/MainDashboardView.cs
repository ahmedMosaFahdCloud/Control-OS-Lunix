using System.Runtime.InteropServices;
using Control_OS_Lunix.UI.ViewModels;
using Microsoft.Win32;

namespace Control_OS_Lunix.UI.Views;

public sealed class MainDashboardView : Form, IMainDashboardView
{
    private const uint WmQueryEndSession = 0x0011;

    private readonly NotifyIcon _trayIcon;
    private readonly ContextMenuStrip _trayMenu;
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
    private readonly Button _backupButton = CreateActionButton("Backup Data");
    private readonly Button _restoreButton = CreateActionButton("Restore Data");
    private bool _exitRequested;
    private bool _firstTrayHideCompleted;

    public MainDashboardView()
    {
        Text = "LanPower Manager";
        StartPosition = FormStartPosition.CenterScreen;
        BackColor = ModernUi.AppBackground;
        Font = ModernUi.BodyFont();
        Width = 1420;
        Height = 820;
        MinimumSize = new Size(1160, 720);
        _trayMenu = BuildTrayMenu();
        _trayIcon = new NotifyIcon
        {
            Text = "LanPower Manager",
            Icon = SystemIcons.Application,
            Visible = true,
            ContextMenuStrip = _trayMenu
        };
        BuildLayout();
        WireEvents();
        SystemEvents.SessionEnding += HandleSessionEnding;
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
    public event EventHandler? BackupRequested;
    public event EventHandler? RestoreRequested;
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
                     _shutdownButton, _refreshButton, _settingsButton, _logsButton, _backupButton,
                     _restoreButton, _deviceGrid
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
    public string? PickBackupPath(string suggestedPath)
    {
        using SaveFileDialog dialog = new()
        {
            Title = "Create Backup",
            Filter = "Backup Archive (*.zip)|*.zip",
            FileName = Path.GetFileName(suggestedPath),
            InitialDirectory = Path.GetDirectoryName(suggestedPath)
        };

        return dialog.ShowDialog(this) == DialogResult.OK ? dialog.FileName : null;
    }

    public string? PickRestorePath()
    {
        using OpenFileDialog dialog = new()
        {
            Title = "Restore Backup",
            Filter = "Backup Archive (*.zip)|*.zip|All Files (*.*)|*.*",
            CheckFileExists = true,
            Multiselect = false
        };

        return dialog.ShowDialog(this) == DialogResult.OK ? dialog.FileName : null;
    }

    public void RequestClose()
    {
        _exitRequested = true;
        Close();
    }

    protected override void WndProc(ref Message m)
    {
        if (m.Msg == WmQueryEndSession && WindowsShutdownHandler is not null)
        {
            WindowsShutdownDecision decision = ExecuteWindowsShutdownDecision();
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

    private void HandleSessionEnding(object? sender, SessionEndingEventArgs args)
    {
        if (WindowsShutdownHandler is null)
        {
            return;
        }

        WindowsShutdownDecision decision = ExecuteWindowsShutdownDecision();
        if (!decision.AllowSessionEnd)
        {
            args.Cancel = true;
        }
    }

    private void BuildLayout()
    {
        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            RowCount = 5,
            ColumnCount = 1,
            Padding = new Padding(24),
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
        Shown += (_, _) => BeginInvoke(HideToTray);
        Resize += (_, _) =>
        {
            if (WindowState == FormWindowState.Minimized)
            {
                HideToTray();
            }
        };
        FormClosing += HandleFormClosing;
        FormClosed += (_, _) =>
        {
            SystemEvents.SessionEnding -= HandleSessionEnding;
            _trayIcon.Visible = false;
            _trayIcon.Dispose();
            _trayMenu.Dispose();
        };
        _trayIcon.DoubleClick += (_, _) => RestoreFromTray();
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
        _backupButton.Click += (_, _) => BackupRequested?.Invoke(this, EventArgs.Empty);
        _restoreButton.Click += (_, _) => RestoreRequested?.Invoke(this, EventArgs.Empty);
    }

    private void HandleFormClosing(object? sender, FormClosingEventArgs args)
    {
        if (args.CloseReason == CloseReason.UserClosing && !_exitRequested)
        {
            args.Cancel = true;
            HideToTray();
            return;
        }

        ViewClosing?.Invoke(sender, args);
    }

    private ContextMenuStrip BuildTrayMenu()
    {
        var menu = new ContextMenuStrip();
        menu.Items.Add("Open Dashboard", null, (_, _) => RestoreFromTray());
        menu.Items.Add("Exit", null, (_, _) => BeginExitFromTray());
        return menu;
    }

    private void HideToTray()
    {
        if (IsDisposed)
        {
            return;
        }

        Hide();
        ShowInTaskbar = false;
        WindowState = FormWindowState.Minimized;

        if (_firstTrayHideCompleted)
        {
            return;
        }

        _trayIcon.ShowBalloonTip(
            2500,
            "LanPower Manager",
            "The app is still running in the system tray next to the clock.",
            ToolTipIcon.Info);
        _firstTrayHideCompleted = true;
    }

    private void RestoreFromTray()
    {
        Show();
        ShowInTaskbar = true;
        WindowState = FormWindowState.Normal;
        Activate();
    }

    private void BeginExitFromTray()
    {
        _exitRequested = true;
        Close();
    }

    private WindowsShutdownDecision ExecuteWindowsShutdownDecision()
    {
        return WindowsShutdownHandler?.Invoke() ?? new WindowsShutdownDecision();
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
        panel.Padding = new Padding(26);

        var ribbon = new Label
        {
            Text = "CONTROL CENTER",
            AutoSize = true,
            Font = new Font("Segoe UI Semibold", 8.5f, FontStyle.Bold),
            ForeColor = Color.White,
            BackColor = ModernUi.Primary,
            Padding = new Padding(10, 5, 10, 5),
            Margin = new Padding(0, 0, 0, 14)
        };

        panel.Controls.Add(ribbon);
        panel.Controls.Add(new Label
        {
            Text = "LanPower Manager",
            AutoSize = true,
            Font = ModernUi.TitleFont(22f),
            ForeColor = ModernUi.TextStrong
        });
        panel.Controls.Add(new Label
        {
            Text = "Professional desktop control for Wake on LAN, Linux power commands, device discovery, and controller automation.",
            AutoSize = true,
            MaximumSize = new Size(1020, 0),
            Font = ModernUi.BodyFont(10f),
            ForeColor = ModernUi.TextMuted,
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
        panel.Padding = new Padding(20, 16, 20, 14);
        var flow = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoSize = true, WrapContents = true, Margin = new Padding(0) };
        flow.Controls.AddRange([_addButton, _scanButton, _editButton, _deleteButton, _startButton, _rebootButton, _shutdownButton, _refreshButton, _settingsButton, _logsButton, _backupButton, _restoreButton]);
        panel.Controls.Add(flow);
        return panel;
    }

    private Control BuildStatusPanel()
    {
        var panel = CreateCardPanel();
        panel.Margin = new Padding(0, 0, 0, 12);
        panel.Padding = new Padding(18, 14, 18, 14);
        panel.BackColor = Color.FromArgb(245, 248, 255);
        panel.Controls.Add(_statusBanner);
        _statusBanner.Text = "Ready. Select a device or scan the network to begin.";
        _statusBanner.Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold);
        return panel;
    }

    private Control BuildGridCard()
    {
        var panel = CreateCardPanel();
        panel.Padding = new Padding(12);
        _deviceGrid.Dock = DockStyle.Fill;
        _deviceGrid.AllowUserToAddRows = false;
        _deviceGrid.AllowUserToDeleteRows = false;
        _deviceGrid.AllowUserToResizeRows = false;
        _deviceGrid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        _deviceGrid.MultiSelect = false;
        _deviceGrid.ReadOnly = true;
        _deviceGrid.RowHeadersVisible = false;
        _deviceGrid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        ModernUi.StyleGrid(_deviceGrid);
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

    private static Panel CreateCardPanel() => ModernUi.CreateCard();

    private static Panel CreateSummaryCard(string title, Label valueLabel, Color accentColor)
    {
        var card = new Panel { Dock = DockStyle.Fill, BackColor = ModernUi.CardBackground, Margin = new Padding(0, 0, 14, 0), Padding = new Padding(18) };
        var accent = new Panel { Dock = DockStyle.Left, Width = 4, BackColor = accentColor };
        var content = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoSize = true, FlowDirection = FlowDirection.TopDown, WrapContents = false, Margin = new Padding(12, 0, 0, 0) };
        content.Controls.Add(new Label { Text = title, AutoSize = true, ForeColor = ModernUi.TextMuted, Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold) });
        content.Controls.Add(valueLabel);
        card.Controls.Add(content);
        card.Controls.Add(accent);
        return card;
    }

    private static Label CreateSummaryValueLabel() => new()
    {
        AutoSize = true,
        Font = ModernUi.TitleFont(14f),
        Margin = new Padding(0, 8, 0, 0),
        ForeColor = ModernUi.TextStrong
    };

    private static Button CreateActionButton(string text) => ModernUi.CreateButton(text, text is "Add Device" or "Scan Network");

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
