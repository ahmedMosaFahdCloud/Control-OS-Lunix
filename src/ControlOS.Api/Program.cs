using System.Text.Json.Serialization;
using ControlOS.Api.Features.Shared;
using ControlOS.Api.Infrastructure.Services;
using ControlOS.Api.Workers;
using Microsoft.Extensions.FileProviders;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

string[] corsOrigins = builder.Configuration.GetSection("CorsOrigins").Get<string[]>() ?? ["http://localhost:4200", "http://localhost:65432"];

builder.Services.AddCors(options =>
{
    options.AddPolicy("control-os-web", policy =>
    {
        policy.WithOrigins(corsOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

builder.Services
    .AddSingleton<CredentialProtector>()
    .AddSingleton<DeviceValidatorService>()
    .AddSingleton<JsonConfigurationStore>()
    .AddSingleton<DevicePowerService>()
    .AddSingleton<LogService>()
    .AddSingleton<BackupRestoreService>()
    .AddSingleton<ControllerOrchestrator>()
    .AddSingleton<NetworkScannerService>()
    .AddSingleton<ControlCenterService>()
    .AddSingleton<WindowsStartupService>()
    .AddHostedService<ControllerAutomationWorker>();

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

WebApplication app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
else
{
    string spaPath = Path.Combine(app.Environment.WebRootPath, "browser");
    var fileProvider = new PhysicalFileProvider(spaPath);

    app.UseWhen(ctx => !ctx.Request.Path.StartsWithSegments("/api"), nonApi =>
    {
        nonApi.UseDefaultFiles(new DefaultFilesOptions { FileProvider = fileProvider });
        nonApi.UseStaticFiles(new StaticFileOptions { FileProvider = fileProvider });
    });
    app.MapFallbackToFile("index.html", new StaticFileOptions { FileProvider = fileProvider });
}

app.UseCors("control-os-web");
app.MapControllers();

// Return 404 JSON for unmatched /api/* routes instead of falling through to the SPA
app.MapFallback("/api/{**path}", async (HttpContext ctx) =>
{
    ctx.Response.StatusCode = StatusCodes.Status404NotFound;
    ctx.Response.ContentType = "application/problem+json";
    await ctx.Response.WriteAsJsonAsync(new
    {
        type = "https://tools.ietf.org/html/rfc9110#section-15.5.5",
        title = "Not Found",
        status = 404,
        detail = $"The requested API endpoint '{ctx.Request.Path}' does not exist."
    });
});

app.Run();
