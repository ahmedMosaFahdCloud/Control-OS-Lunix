using ControlOS.Api.Backend.Results;

namespace ControlOS.Api.Backend.Interfaces;

public interface IWindowsStartupService
{
    Result EnsureRegistered();
}
