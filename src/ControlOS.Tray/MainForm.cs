using System.Diagnostics;

namespace ControlOS.Tray;

public sealed class MainForm : Form
{
    private const string AppTitle = "Control OS";
    private const string AppUrl = "http://localhost:5081";
    private bool _allowClose;

    public MainForm()
    {
        Text = AppTitle;
        ClientSize = new Size(420, 200);
        FormBorderStyle = FormBorderStyle.FixedSingle;
        MaximizeBox = false;
        StartPosition = FormStartPosition.CenterScreen;
        Icon = SystemIcons.Application;

        var label = new Label
        {
            Text = $"Control OS is running.\n\nServer: {AppUrl}",
            TextAlign = ContentAlignment.MiddleCenter,
            Dock = DockStyle.Fill,
            Padding = new Padding(20),
            Font = new Font("Segoe UI", 11)
        };

        var openBtn = new Button
        {
            Text = "Open Browser",
            Size = new Size(140, 36),
            Location = new Point(140, 130),
            Font = new Font("Segoe UI", 10)
        };
        openBtn.Click += (_, _) => OpenBrowser();

        Controls.Add(label);
        Controls.Add(openBtn);
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        if (!_allowClose && e.CloseReason == CloseReason.UserClosing)
        {
            e.Cancel = true;
            Hide();
            return;
        }
        base.OnFormClosing(e);
    }

    public void CloseForReal()
    {
        _allowClose = true;
        Close();
    }

    private static void OpenBrowser()
    {
        try
        {
            Process.Start(new ProcessStartInfo(AppUrl) { UseShellExecute = true });
        }
        catch { }
    }
}
