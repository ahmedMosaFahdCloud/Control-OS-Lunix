using System.Text.Json.Serialization;
using Control_OS_Lunix.Core.DependencyInjection;
using ControlOS.Tray;

Application.EnableVisualStyles();
Application.SetCompatibleTextRenderingDefault(false);
Application.Run(new TrayApp());
