namespace ControlOS.Api.Features.Shared.Models;

public enum OperationTrigger
{
    Manual,
    ControllerStartup,
    ControllerShutdown,
    Scheduled
}
