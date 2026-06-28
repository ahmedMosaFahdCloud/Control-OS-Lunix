using System.Diagnostics;
using System.Drawing.Drawing2D;

namespace ControlOS.Tray;

public sealed class MainForm : Form
{
    private const string AppTitle = "Control OS";
    private const string AppUrl = "http://localhost:58432";
    private bool _allowClose;

    public MainForm()
    {
        Text = AppTitle;
        ClientSize = new Size(440, 280);
        FormBorderStyle = FormBorderStyle.FixedSingle;
        MaximizeBox = false;
        StartPosition = FormStartPosition.CenterScreen;
        BackColor = Color.FromArgb(248, 250, 252);
        Icon = SystemIcons.Application;

        var titlePanel = CreateTitlePanel();
        var statusPanel = CreateStatusPanel();
        var actionsPanel = CreateActionsPanel();
        var footer = CreateFooter();

        titlePanel.Location = new Point(0, 0);
        titlePanel.Size = new Size(440, 56);
        statusPanel.Location = new Point(24, 72);
        statusPanel.Size = new Size(392, 80);
        actionsPanel.Location = new Point(24, 164);
        actionsPanel.Size = new Size(392, 50);
        footer.Location = new Point(0, 230);
        footer.Size = new Size(440, 50);

        Controls.Add(titlePanel);
        Controls.Add(statusPanel);
        Controls.Add(actionsPanel);
        Controls.Add(footer);
    }

    private Panel CreateTitlePanel()
    {
        var panel = new Panel { BackColor = Color.FromArgb(30, 41, 59) };

        var title = new Label
        {
            Text = AppTitle,
            ForeColor = Color.White,
            Font = new Font("Segoe UI", 16, FontStyle.Bold),
            Location = new Point(20, 14),
            AutoSize = true
        };

        var subtitle = new Label
        {
            Text = "Network Power Controller",
            ForeColor = Color.FromArgb(148, 163, 184),
            Font = new Font("Segoe UI", 9),
            Location = new Point(20, 38),
            AutoSize = true
        };

        panel.Controls.Add(title);
        panel.Controls.Add(subtitle);
        return panel;
    }

    private Panel CreateStatusPanel()
    {
        var panel = new Panel
        {
            BackColor = Color.White,
            BorderStyle = BorderStyle.None
        };

        using var g = panel.CreateGraphics();
        var borderPen = new Pen(Color.FromArgb(226, 232, 240));

        var dot = new Label
        {
            Text = "●",
            ForeColor = Color.FromArgb(34, 197, 94),
            Font = new Font("Segoe UI", 14),
            Location = new Point(16, 14),
            AutoSize = true
        };

        var statusText = new Label
        {
            Text = "Server is running",
            ForeColor = Color.FromArgb(22, 163, 74),
            Font = new Font("Segoe UI", 10, FontStyle.Bold),
            Location = new Point(36, 14),
            AutoSize = true
        };

        var urlLabel = new Label
        {
            Text = AppUrl,
            ForeColor = Color.FromArgb(59, 130, 246),
            Font = new Font("Segoe UI", 9, FontStyle.Underline),
            Location = new Point(36, 34),
            Cursor = Cursors.Hand,
            AutoSize = true
        };
        urlLabel.Click += (_, _) => OpenBrowser();

        var hintLabel = new Label
        {
            Text = "Close window to keep running in system tray",
            ForeColor = Color.FromArgb(148, 163, 184),
            Font = new Font("Segoe UI", 8),
            Location = new Point(16, 56),
            AutoSize = true
        };

        panel.Controls.Add(dot);
        panel.Controls.Add(statusText);
        panel.Controls.Add(urlLabel);
        panel.Controls.Add(hintLabel);
        return panel;
    }

    private Panel CreateActionsPanel()
    {
        var panel = new Panel();
        panel.Paint += (_, e) =>
        {
            using var brush = new SolidBrush(Color.FromArgb(226, 232, 240));
            e.Graphics.FillRectangle(brush, 0, 0, panel.Width, 1);
        };

        var openBtn = new Button
        {
            Text = "  Open Browser",
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.FromArgb(30, 41, 59),
            ForeColor = Color.White,
            Font = new Font("Segoe UI", 9, FontStyle.Bold),
            Size = new Size(150, 34),
            Location = new Point(0, 8),
            FlatAppearance = { BorderSize = 0 }
        };
        openBtn.Paint += (_, e) =>
        {
            var btn = openBtn;
            using var path = GetRoundedRect(btn.ClientRectangle, 6);
            using var brush = new SolidBrush(btn.BackColor);
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            e.Graphics.FillPath(brush, path);
            TextRenderer.DrawText(e.Graphics, btn.Text, btn.Font, btn.ClientRectangle, btn.ForeColor,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
        };
        openBtn.Click += (_, _) => OpenBrowser();

        var configBtn = new Button
        {
            Text = "Config Folder",
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.White,
            ForeColor = Color.FromArgb(30, 41, 59),
            Font = new Font("Segoe UI", 9),
            Size = new Size(120, 34),
            Location = new Point(162, 8),
            FlatAppearance = { BorderColor = Color.FromArgb(226, 232, 240) }
        };
        configBtn.Paint += (_, e) =>
        {
            var btn = configBtn;
            using var path = GetRoundedRect(btn.ClientRectangle, 6);
            using var brush = new SolidBrush(btn.BackColor);
            using var pen = new Pen(Color.FromArgb(226, 232, 240));
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            e.Graphics.FillPath(brush, path);
            e.Graphics.DrawPath(pen, path);
            TextRenderer.DrawText(e.Graphics, btn.Text, btn.Font, btn.ClientRectangle, btn.ForeColor,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
        };
        configBtn.Click += (_, _) => OpenConfig();

        panel.Controls.Add(openBtn);
        panel.Controls.Add(configBtn);
        return panel;
    }

    private static Label CreateFooter()
    {
        return new Label
        {
            Text = "Right-click the tray icon to exit completely",
            ForeColor = Color.FromArgb(148, 163, 184),
            Font = new Font("Segoe UI", 8),
            TextAlign = ContentAlignment.MiddleCenter,
            Dock = DockStyle.Bottom
        };
    }

    private static GraphicsPath GetRoundedRect(Rectangle rect, int radius)
    {
        var path = new GraphicsPath();
        path.AddArc(rect.X, rect.Y, radius * 2, radius * 2, 180, 90);
        path.AddArc(rect.Right - radius * 2, rect.Y, radius * 2, radius * 2, 270, 90);
        path.AddArc(rect.Right - radius * 2, rect.Bottom - radius * 2, radius * 2, radius * 2, 0, 90);
        path.AddArc(rect.X, rect.Bottom - radius * 2, radius * 2, radius * 2, 90, 90);
        path.CloseFigure();
        return path;
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
        try { Process.Start(new ProcessStartInfo(AppUrl) { UseShellExecute = true }); }
        catch { }
    }

    private static void OpenConfig()
    {
        try
        {
            string configPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "Control-OS-Lunix");
            Directory.CreateDirectory(configPath);
            Process.Start("explorer.exe", configPath);
        }
        catch { }
    }

    protected override void OnPaintBackground(PaintEventArgs e)
    {
        base.OnPaintBackground(e);
        using var pen = new Pen(Color.FromArgb(226, 232, 240));
        e.Graphics.DrawRectangle(pen, 0, 0, Width - 1, Height - 1);
    }
}
