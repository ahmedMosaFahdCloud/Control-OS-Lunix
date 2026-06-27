using Control_OS_Lunix.Backend.Results;

namespace Control_OS_Lunix.UI.Controllers;

public interface ILogsController
{
    Task<Result> ShowAsync(IWin32Window owner);
}
