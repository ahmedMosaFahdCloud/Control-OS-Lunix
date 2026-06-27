using Control_OS_Lunix.Backend.Models;
using Control_OS_Lunix.Backend.Results;

namespace Control_OS_Lunix.UI.Controllers;

public interface ISettingsController
{
    Task<Result<GlobalSettings?>> ShowAsync(IWin32Window owner, GlobalSettings currentSettings);
}
