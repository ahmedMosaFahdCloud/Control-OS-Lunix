using System.Text.Json.Serialization;
using Control_OS_Lunix.Core.DependencyInjection;
using ControlOS.Api.Features.Shared;
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
    .AddControlOsCoreServices()
    .AddSingleton<ControlCenterService>();

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

    app.UseDefaultFiles(new DefaultFilesOptions { FileProvider = fileProvider });
    app.UseStaticFiles(new StaticFileOptions { FileProvider = fileProvider });
    app.MapFallbackToFile("index.html", new StaticFileOptions { FileProvider = fileProvider });
}

app.UseCors("control-os-web");
app.MapControllers();

app.Run();
