using Control_OS_Lunix.Core.DependencyInjection;
using ControlOS.Agent;

HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);

builder.Services.AddWindowsService(options =>
{
    options.ServiceName = "ControlOS Agent";
});

builder.Services
    .AddControlOsCoreServices()
    .AddHostedService<ControllerAutomationWorker>();

IHost host = builder.Build();
host.Run();
