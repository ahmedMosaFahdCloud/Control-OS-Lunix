using System.Text.Json.Serialization;
using ControlOS.Api.DependencyInjection;
using ControlOS.Tray;

Application.EnableVisualStyles();
Application.SetCompatibleTextRenderingDefault(false);
Application.Run(new TrayApp());
