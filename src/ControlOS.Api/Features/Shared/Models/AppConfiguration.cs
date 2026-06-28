namespace ControlOS.Api.Features.Shared.Models;

public sealed class AppConfiguration
{
    public GlobalSettings GlobalSettings { get; set; } = new();

    public List<DevicePowerConfig> Devices { get; set; } = [];
}
