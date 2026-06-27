using Control_OS_Lunix.Backend.Models;
using Control_OS_Lunix.Backend.Results;

namespace Control_OS_Lunix.UI.Controllers;

public interface INetworkScannerController
{
    Task<Result<DevicePowerConfig?>> ShowAsync(IWin32Window owner, GlobalSettings settings);
}
