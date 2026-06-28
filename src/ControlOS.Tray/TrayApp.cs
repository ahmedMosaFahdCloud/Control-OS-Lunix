using System.Diagnostics;
using System.Linq;
using System.Text.Json.Serialization;
using Control_OS_Lunix.Core.DependencyInjection;
using Microsoft.Win32;

namespace ControlOS.Tray;

public sealed class TrayApp : ApplicationContext
{
    private readonly NotifyIcon _trayIcon;
    private readonly MainForm _mainForm;
    private CancellationTokenSource? _apiCts;
    private Task? _apiTask;

    private const string AppTitle = "Control OS";
    private const string AppUrl = "http://localhost:5081";
    private const string AutoStartKey = @"Software\Microsoft\Windows\CurrentVersion\Run";

    public TrayApp()
    {
        _mainForm = new MainForm();

        _trayIcon = new NotifyIcon
        {
            Text = $"{AppTitle}\n{AppUrl}",
            Visible = true,
            Icon = SystemIcons.Application
        };

        var menu = new ContextMenuStrip();
        menu.Items.Add("Show Control OS", null, (_, _) => ShowWindow());
        menu.Items.Add("Open Browser", null, (_, _) => OpenBrowser());
        menu.Items.Add("Open Config Folder", null, (_, _) => OpenConfig());
        menu.Items.Add("-");
        var autoStartItem = menu.Items.Add("Auto-start with Windows", null, (_, _) => ToggleAutoStart());
        if (autoStartItem is ToolStripMenuItem autoStart)
        {
            autoStart.Checked = IsAutoStartEnabled();
        }
        menu.Items.Add("-");
        menu.Items.Add("Exit", null, (_, _) => ExitApp());

        _trayIcon.ContextMenuStrip = menu;
        _trayIcon.DoubleClick += (_, _) => ShowWindow();

        StartApi();

        _mainForm.Show();
        OpenBrowser();
    }

    private void StartApi()
    {
        _apiCts = new CancellationTokenSource();

        _apiTask = Task.Run(async () =>
        {
            try
            {
                WebApplicationBuilder builder = WebApplication.CreateBuilder([]);

                builder.Services.AddCors(options =>
                {
                    options.AddDefaultPolicy(policy =>
                    {
                        policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod();
                    });
                });

                builder.Services.AddControlOsCoreServices();

                builder.Services.AddControllers()
                    .AddJsonOptions(options =>
                    {
                        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
                    });

                builder.Services.AddEndpointsApiExplorer();
                builder.Services.AddSwaggerGen();

                builder.WebHost.UseUrls(AppUrl);

                WebApplication app = builder.Build();

                string baseDir = AppDomain.CurrentDomain.BaseDirectory;
                string spaPath = Path.Combine(baseDir, "wwwroot", "browser");

                if (Directory.Exists(spaPath))
                {
                    app.UseDefaultFiles(new DefaultFilesOptions
                    {
                        FileProvider = new Microsoft.Extensions.FileProviders.PhysicalFileProvider(spaPath)
                    });
                    app.UseStaticFiles(new StaticFileOptions
                    {
                        FileProvider = new Microsoft.Extensions.FileProviders.PhysicalFileProvider(spaPath)
                    });
                    app.MapFallbackToFile("index.html", new StaticFileOptions
                    {
                        FileProvider = new Microsoft.Extensions.FileProviders.PhysicalFileProvider(spaPath)
                    });
                }

                app.UseCors();
                app.MapControllers();

                await app.RunAsync(_apiCts.Token);
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                MessageBox.Show($"API failed to start: {ex.Message}", AppTitle,
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }, _apiCts.Token);
    }

    private void ShowWindow()
    {
        _mainForm.Show();
        _mainForm.WindowState = FormWindowState.Normal;
        _mainForm.Activate();
    }

    private static void OpenBrowser()
    {
        try
        {
            Process.Start(new ProcessStartInfo(AppUrl) { UseShellExecute = true });
        }
        catch { }
    }

    private static void OpenConfig()
    {
        try
        {
            string configPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "Control-OS-Lunix");

            if (!Directory.Exists(configPath))
            {
                Directory.CreateDirectory(configPath);
            }

            Process.Start("explorer.exe", configPath);
        }
        catch { }
    }

    private static bool IsAutoStartEnabled()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(AutoStartKey);
            return key?.GetValue("ControlOS") is not null;
        }
        catch { return false; }
    }

    private void ToggleAutoStart()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(AutoStartKey, true);
            if (key is null) return;

            if (IsAutoStartEnabled())
            {
                key.DeleteValue("ControlOS", false);
            }
            else
            {
                string exePath = Environment.ProcessPath ?? Application.ExecutablePath;
                key.SetValue("ControlOS", $"\"{exePath}\"");
            }
        }
        catch { }

        foreach (ToolStripMenuItem item in _trayIcon.ContextMenuStrip!.Items.OfType<ToolStripMenuItem>())
        {
            if (item.Text == "Auto-start with Windows")
            {
                item.Checked = IsAutoStartEnabled();
                break;
            }
        }
    }

    private void ExitApp()
    {
        _apiCts?.Cancel();

        try
        {
            _apiTask?.Wait(TimeSpan.FromSeconds(3));
        }
        catch { }

        _mainForm.CloseForReal();

        _trayIcon.Visible = false;
        _trayIcon.Dispose();
        Application.ExitThread();
    }
}
