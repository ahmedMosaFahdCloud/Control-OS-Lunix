using Control_OS_Lunix.Backend.Models;
using Control_OS_Lunix.UI.ViewModels;

namespace Control_OS_Lunix.UI.Views;

public sealed class DeviceDialogView : Form, IDeviceDialogView
{
    private readonly TextBox _nameTextBox = new() { PlaceholderText = "Ubuntu Server 01" };
    private readonly TextBox _ipAddressTextBox = new() { PlaceholderText = "192.168.1.18" };
    private readonly TextBox _macAddressTextBox = new() { PlaceholderText = "AA:BB:CC:DD:EE:FF" };
    private readonly TextBox _broadcastAddressTextBox = new() { PlaceholderText = "192.168.1.255" };
    private readonly NumericUpDown _wolPortNumeric = new() { Minimum = 1, Maximum = 65535, Value = 9 };
    private readonly TextBox _sshHostTextBox = new() { PlaceholderText = "192.168.1.18" };
    private readonly NumericUpDown _sshPortNumeric = new() { Minimum = 1, Maximum = 65535, Value = 22 };
    private readonly TextBox _sshUsernameTextBox = new();
    private readonly TextBox _sshPasswordTextBox = new() { UseSystemPasswordChar = true };
    private readonly ComboBox _operatingSystemComboBox = new() { DropDownStyle = ComboBoxStyle.DropDownList };
    private readonly CheckBox _autoStartCheckBox = new() { Text = "Auto start enabled" };
    private readonly CheckBox _autoShutdownCheckBox = new() { Text = "Auto shutdown enabled" };
    private readonly CheckBox _manualControlCheckBox = new() { Text = "Manual control enabled" };
    private readonly CheckBox _activeCheckBox = new() { Text = "Active" };
    private readonly NumericUpDown _timeoutSecondsNumeric = new() { Minimum = 1, Maximum = 120, Value = 15 };
    private readonly NumericUpDown _retryCountNumeric = new() { Minimum = 1, Maximum = 10, Value = 1 };
    private readonly TextBox _descriptionTextBox = new() { Multiline = true, Height = 72, ScrollBars = ScrollBars.Vertical };

    public DeviceDialogView()
    {
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        BackColor = ModernUi.AppBackground;
        Font = ModernUi.BodyFont();
        Width = 720;
        Height = 820;
        _operatingSystemComboBox.DataSource = Enum.GetValues<DeviceOperatingSystemType>();
        BuildLayout();
    }

    public event EventHandler? SaveRequested;
    public event EventHandler? CancelRequested;

    public DeviceEditViewModel FormData
    {
        get => new()
        {
            DeviceId = _tagDeviceId,
            Name = _nameTextBox.Text.Trim(),
            IpAddress = _ipAddressTextBox.Text.Trim(),
            MacAddress = _macAddressTextBox.Text.Trim(),
            BroadcastAddress = _broadcastAddressTextBox.Text.Trim(),
            WolPort = (int)_wolPortNumeric.Value,
            SshHost = _sshHostTextBox.Text.Trim(),
            SshPort = (int)_sshPortNumeric.Value,
            SshUsername = _sshUsernameTextBox.Text.Trim(),
            SshPassword = _sshPasswordTextBox.Text,
            OperatingSystemType = (DeviceOperatingSystemType)_operatingSystemComboBox.SelectedItem!,
            AutoStartEnabled = _autoStartCheckBox.Checked,
            AutoShutdownEnabled = _autoShutdownCheckBox.Checked,
            ManualControlEnabled = _manualControlCheckBox.Checked,
            IsActive = _activeCheckBox.Checked,
            TimeoutSeconds = (int)_timeoutSecondsNumeric.Value,
            RetryCount = (int)_retryCountNumeric.Value,
            Description = _descriptionTextBox.Text.Trim(),
            CreatedDateUtc = _createdDateUtc,
            LastUpdatedDateUtc = _lastUpdatedDateUtc,
            LastKnownStatus = _lastKnownStatus,
            LastOperationSummary = _lastOperationSummary
        };
        set
        {
            _tagDeviceId = value.DeviceId;
            _createdDateUtc = value.CreatedDateUtc;
            _lastUpdatedDateUtc = value.LastUpdatedDateUtc;
            _lastKnownStatus = value.LastKnownStatus;
            _lastOperationSummary = value.LastOperationSummary;
            _nameTextBox.Text = value.Name;
            _ipAddressTextBox.Text = value.IpAddress;
            _macAddressTextBox.Text = value.MacAddress;
            _broadcastAddressTextBox.Text = value.BroadcastAddress;
            _wolPortNumeric.Value = value.WolPort;
            _sshHostTextBox.Text = value.SshHost;
            _sshPortNumeric.Value = value.SshPort;
            _sshUsernameTextBox.Text = value.SshUsername;
            _sshPasswordTextBox.Text = value.SshPassword;
            _operatingSystemComboBox.SelectedItem = value.OperatingSystemType;
            _autoStartCheckBox.Checked = value.AutoStartEnabled;
            _autoShutdownCheckBox.Checked = value.AutoShutdownEnabled;
            _manualControlCheckBox.Checked = value.ManualControlEnabled;
            _activeCheckBox.Checked = value.IsActive;
            _timeoutSecondsNumeric.Value = value.TimeoutSeconds;
            _retryCountNumeric.Value = value.RetryCount;
            _descriptionTextBox.Text = value.Description;
        }
    }

    public string ViewTitle { set => Text = value; }

    private Guid _tagDeviceId;
    private DateTime _createdDateUtc = DateTime.UtcNow;
    private DateTime _lastUpdatedDateUtc = DateTime.UtcNow;
    private DevicePowerStatus _lastKnownStatus = DevicePowerStatus.Unknown;
    private string _lastOperationSummary = "No operations yet";

    public DialogResult ShowDialogView(IWin32Window owner) => ShowDialog(owner);

    public void CloseView(DialogResult result)
    {
        DialogResult = result;
        Close();
    }

    public void ShowWarning(string message, string title)
    {
        MessageBox.Show(this, message, title, MessageBoxButtons.OK, MessageBoxIcon.Warning);
    }

    private void BuildLayout()
    {
        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(20),
            BackColor = BackColor,
            RowCount = 3,
            ColumnCount = 1
        };
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        var headerCard = ModernUi.CreateCard(22);
        headerCard.Margin = new Padding(0, 0, 0, 14);
        headerCard.Controls.Add(new Label
        {
            Text = "Device Configuration",
            AutoSize = true,
            Font = ModernUi.TitleFont(18f),
            ForeColor = ModernUi.TextStrong
        });
        headerCard.Controls.Add(new Label
        {
            Text = "Configure networking, Wake on LAN, SSH access, and automation flags for this device.",
            AutoSize = true,
            MaximumSize = new Size(620, 0),
            Font = ModernUi.BodyFont(),
            ForeColor = ModernUi.TextMuted,
            Margin = new Padding(0, 10, 0, 0)
        });

        var scrollPanel = new Panel
        {
            Dock = DockStyle.Fill,
            AutoScroll = true,
            BackColor = BackColor
        };

        var content = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            AutoSize = true
        };

        var identityCard = CreateSectionCard("Identity", out TableLayoutPanel identityGrid);
        AddRow(identityGrid, "Device Name", _nameTextBox);
        AddRow(identityGrid, "IP Address", _ipAddressTextBox);
        AddRow(identityGrid, "MAC Address", _macAddressTextBox);
        AddRow(identityGrid, "Description", _descriptionTextBox);

        var networkCard = CreateSectionCard("Networking", out TableLayoutPanel networkGrid);
        AddRow(networkGrid, "Broadcast Address", _broadcastAddressTextBox);
        AddRow(networkGrid, "WOL Port", _wolPortNumeric);
        AddRow(networkGrid, "Operating System", _operatingSystemComboBox);
        AddRow(networkGrid, "Timeout Seconds", _timeoutSecondsNumeric);
        AddRow(networkGrid, "Retry Count", _retryCountNumeric);

        var sshCard = CreateSectionCard("SSH Access", out TableLayoutPanel sshGrid);
        AddRow(sshGrid, "SSH Host", _sshHostTextBox);
        AddRow(sshGrid, "SSH Port", _sshPortNumeric);
        AddRow(sshGrid, "SSH Username", _sshUsernameTextBox);
        AddRow(sshGrid, "SSH Password", _sshPasswordTextBox);

        var behaviorCard = ModernUi.CreateCard(18);
        behaviorCard.Margin = new Padding(0, 0, 0, 14);
        behaviorCard.Controls.Add(ModernUi.CreateSectionLabel("Behavior"));
        foreach (CheckBox checkBox in new[] { _autoStartCheckBox, _autoShutdownCheckBox, _manualControlCheckBox, _activeCheckBox })
        {
            ModernUi.StyleCheckBox(checkBox);
            behaviorCard.Controls.Add(checkBox);
        }

        content.Controls.Add(identityCard);
        content.Controls.Add(networkCard);
        content.Controls.Add(sshCard);
        content.Controls.Add(behaviorCard);
        scrollPanel.Controls.Add(content);

        var buttonsPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Bottom,
            FlowDirection = FlowDirection.RightToLeft,
            Height = 58,
            Padding = new Padding(0, 8, 0, 0),
            BackColor = BackColor
        };

        var saveButton = ModernUi.CreateButton("Save Device", primary: true);
        var cancelButton = ModernUi.CreateButton("Cancel");
        saveButton.Click += (_, _) => SaveRequested?.Invoke(this, EventArgs.Empty);
        cancelButton.Click += (_, _) => CancelRequested?.Invoke(this, EventArgs.Empty);
        buttonsPanel.Controls.Add(saveButton);
        buttonsPanel.Controls.Add(cancelButton);
        AcceptButton = saveButton;
        CancelButton = cancelButton;

        root.Controls.Add(headerCard, 0, 0);
        root.Controls.Add(scrollPanel, 0, 1);
        root.Controls.Add(buttonsPanel, 0, 2);
        Controls.Add(root);
    }

    private static Panel CreateSectionCard(string title, out TableLayoutPanel grid)
    {
        var card = ModernUi.CreateCard(18);
        card.Margin = new Padding(0, 0, 0, 14);
        card.Controls.Add(ModernUi.CreateSectionLabel(title));
        grid = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            ColumnCount = 2
        };
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 180));
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        card.Controls.Add(grid);
        return card;
    }

    private static void AddRow(TableLayoutPanel table, string labelText, Control control)
    {
        table.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        table.Controls.Add(ModernUi.CreateFieldLabel(labelText), 0, table.RowCount);
        ModernUi.StyleInput(control);
        control.Dock = DockStyle.Top;
        table.Controls.Add(control, 1, table.RowCount);
        table.RowCount++;
    }
}
