namespace ControlOS.Api.Backend.Models;

public enum OperationTrigger
{
    Manual,
    ControllerStartup,
    ControllerShutdown,
    Scheduled
}
