using Control_OS_Lunix.UI.ViewModels;

namespace Control_OS_Lunix.UI.Views;

public sealed class NetworkScannerView : Form, INetworkScannerView
{
    private readonly TextBox _subnetTextBox = new();
    private readonly NumericUpDown _startHostNumeric = new() { Minimum = 1, Maximum = 254, Value = 1 };
    private readonly NumericUpDown _endHostNumeric = new() { Minimum = 1, Maximum = 254, Value = 254 };
    private readonly NumericUpDown _timeoutNumeric = new() { Minimum = 100, Maximum = 10000, Increment = 100, Value = 500 };
    private readonly NumericUpDown _concurrencyNumeric = new() { Minimum = 1, Maximum = 128, Value = 32 };
    private readonly Button _scanButton = new() { Text = "Start Scan", AutoSize = true };
    private readonly Button _useResultButton = new() { Text = "Create Device From Selected Host", AutoSize = true };
    private readonly ProgressBar _progressBar = new() { Dock = DockStyle.Fill, Height = 10 };
    private readonly Label _statusLabel = new() { Text = "Ready to scan the local network.", AutoSize = true, ForeColor = Color.DimGray };
    private readonly DataGridView _resultsGrid = new();

    public NetworkScannerView()
    {
        Text = "Network Scanner";
        StartPosition = FormStartPosition.CenterParent;
        Width = 980;
        Height = 680;
        MinimumSize = new Size(860, 580);
        BackColor = Color.FromArgb(245, 247, 250);
        BuildLayout();
    }

    public event EventHandler? ScanRequested;
    public event EventHandler? UseSelectionRequested;
    public event EventHandler? CancelRequested;

    public string SubnetPrefix { get => _subnetTextBox.Text; set => _subnetTextBox.Text = value; }
    public int StartHost { get => (int)_startHostNumeric.Value; set => _startHostNumeric.Value = value; }
    public int EndHost { get => (int)_endHostNumeric.Value; set => _endHostNumeric.Value = value; }
    public int TimeoutMs { get => (int)_timeoutNumeric.Value; set => _timeoutNumeric.Value = value; }
    public int MaxConcurrency { get => (int)_concurrencyNumeric.Value; set => _concurrencyNumeric.Value = value; }
    public string SelectedIpAddress => _resultsGrid.SelectedRows.Count == 0 ? string.Empty : _resultsGrid.SelectedRows[0].Tag?.ToString() ?? string.Empty;

    public void BindResults(IReadOnlyList<NetworkScanRowViewModel> rows)
    {
        _resultsGrid.Rows.Clear();
        foreach (NetworkScanRowViewModel row in rows)
        {
            int rowIndex = _resultsGrid.Rows.Add(row.IpAddress, row.HostName, row.MacAddress, row.ResponseTimeText, row.Summary);
            _resultsGrid.Rows[rowIndex].Tag = row.IpAddress;
        }
        _useResultButton.Enabled = _resultsGrid.SelectedRows.Count > 0;
    }

    public void SetProgress(int value)
    {
        _progressBar.Value = Math.Clamp(value, 0, 100);
    }

    public void SetBusy(bool isBusy)
    {
        _scanButton.Enabled = !isBusy;
        _useResultButton.Enabled = !isBusy && _resultsGrid.SelectedRows.Count > 0;
        _subnetTextBox.Enabled = !isBusy;
        _startHostNumeric.Enabled = !isBusy;
        _endHostNumeric.Enabled = !isBusy;
        _timeoutNumeric.Enabled = !isBusy;
        _concurrencyNumeric.Enabled = !isBusy;
        _resultsGrid.Enabled = !isBusy;
        UseWaitCursor = isBusy;
    }

    public void SetStatus(string message) => _statusLabel.Text = message;

    public void ShowError(string message, string title)
    {
        MessageBox.Show(this, message, title, MessageBoxButtons.OK, MessageBoxIcon.Error);
    }

    public DialogResult ShowDialogView(IWin32Window owner) => ShowDialog(owner);

    public void CloseView(DialogResult result)
    {
        DialogResult = result;
        Close();
    }

    private void BuildLayout()
    {
        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            RowCount = 3,
            ColumnCount = 1,
            Padding = new Padding(18),
            BackColor = BackColor
        };
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.Controls.Add(BuildHeaderCard(), 0, 0);
        root.Controls.Add(BuildScanControlsCard(), 0, 1);
        root.Controls.Add(BuildResultsCard(), 0, 2);
        Controls.Add(root);
    }

    private Control BuildHeaderCard()
    {
        FlowLayoutPanel card = CreateFlowCard();
        card.Margin = new Padding(0, 0, 0, 14);
        card.Controls.Add(new Label
        {
            Text = "Discover Devices On The Network",
            Font = new Font(SystemFonts.DefaultFont.FontFamily, 16, FontStyle.Bold),
            AutoSize = true
        });
        card.Controls.Add(new Label
        {
            Text = "Scan your LAN range, review reachable hosts, and create device records without retyping IP addresses.",
            AutoSize = true,
            MaximumSize = new Size(900, 0),
            ForeColor = Color.DimGray,
            Margin = new Padding(0, 8, 0, 0)
        });
        return card;
    }

    private Control BuildScanControlsCard()
    {
        FlowLayoutPanel card = CreateFlowCard();
        card.Margin = new Padding(0, 0, 0, 14);
        var layout = new TableLayoutPanel { Dock = DockStyle.Top, ColumnCount = 4, RowCount = 3, AutoSize = true };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 140));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        AddField(layout, "Subnet Prefix", _subnetTextBox, 0);
        AddField(layout, "Start Host", _startHostNumeric, 1);
        AddField(layout, "End Host", _endHostNumeric, 2);
        AddField(layout, "Timeout (ms)", _timeoutNumeric, 3);
        AddField(layout, "Concurrency", _concurrencyNumeric, 4);

        var actionsPanel = new FlowLayoutPanel { Dock = DockStyle.Top, AutoSize = true, Margin = new Padding(0, 16, 0, 0) };
        var cancelButton = new Button { Text = "Cancel", AutoSize = true };
        _scanButton.Click += (_, _) => ScanRequested?.Invoke(this, EventArgs.Empty);
        _useResultButton.Click += (_, _) => UseSelectionRequested?.Invoke(this, EventArgs.Empty);
        cancelButton.Click += (_, _) => CancelRequested?.Invoke(this, EventArgs.Empty);
        _resultsGrid.SelectionChanged += (_, _) => _useResultButton.Enabled = _resultsGrid.SelectedRows.Count > 0;
        _resultsGrid.CellDoubleClick += (_, _) => UseSelectionRequested?.Invoke(this, EventArgs.Empty);
        actionsPanel.Controls.Add(_scanButton);
        actionsPanel.Controls.Add(_useResultButton);
        actionsPanel.Controls.Add(cancelButton);

        card.Controls.Add(layout);
        card.Controls.Add(actionsPanel);
        card.Controls.Add(_statusLabel);
        card.Controls.Add(_progressBar);
        _statusLabel.Margin = new Padding(0, 12, 0, 0);
        _progressBar.Margin = new Padding(0, 10, 0, 0);
        return card;
    }

    private Control BuildResultsCard()
    {
        var card = new Panel { Dock = DockStyle.Fill, BackColor = Color.White, Padding = new Padding(12) };
        _resultsGrid.Dock = DockStyle.Fill;
        _resultsGrid.AllowUserToAddRows = false;
        _resultsGrid.AllowUserToDeleteRows = false;
        _resultsGrid.AllowUserToResizeRows = false;
        _resultsGrid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        _resultsGrid.BackgroundColor = Color.White;
        _resultsGrid.ReadOnly = true;
        _resultsGrid.MultiSelect = false;
        _resultsGrid.RowHeadersVisible = false;
        _resultsGrid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        _resultsGrid.BorderStyle = BorderStyle.None;
        _resultsGrid.Columns.Add(new DataGridViewTextBoxColumn { Name = "IpAddress", HeaderText = "IP Address", SortMode = DataGridViewColumnSortMode.NotSortable });
        _resultsGrid.Columns.Add(new DataGridViewTextBoxColumn { Name = "HostName", HeaderText = "Host Name", SortMode = DataGridViewColumnSortMode.NotSortable });
        _resultsGrid.Columns.Add(new DataGridViewTextBoxColumn { Name = "MacAddress", HeaderText = "MAC Address", SortMode = DataGridViewColumnSortMode.NotSortable });
        _resultsGrid.Columns.Add(new DataGridViewTextBoxColumn { Name = "ResponseTime", HeaderText = "Ping", SortMode = DataGridViewColumnSortMode.NotSortable });
        _resultsGrid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Summary", HeaderText = "Summary", SortMode = DataGridViewColumnSortMode.NotSortable });
        card.Controls.Add(_resultsGrid);
        return card;
    }

    private static FlowLayoutPanel CreateFlowCard()
    {
        return new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            AutoSize = true,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            Padding = new Padding(18),
            BackColor = Color.White
        };
    }

    private static void AddField(TableLayoutPanel layout, string labelText, Control control, int fieldIndex)
    {
        int rowIndex = fieldIndex / 2;
        int columnOffset = (fieldIndex % 2) * 2;
        while (layout.RowStyles.Count <= rowIndex)
        {
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        }

        layout.Controls.Add(new Label { Text = labelText, AutoSize = true, Margin = new Padding(0, 10, 8, 0) }, columnOffset, rowIndex);
        control.Dock = DockStyle.Top;
        control.Margin = new Padding(0, 6, 0, 0);
        layout.Controls.Add(control, columnOffset + 1, rowIndex);
    }
}
