using Control_OS_Lunix.Backend.Results;

namespace Control_OS_Lunix.Backend.Interfaces;

public interface IWindowsStartupService
{
    Result EnsureRegistered();
}
