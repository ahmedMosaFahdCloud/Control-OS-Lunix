using Control_OS_Lunix.Backend.Models;
using Control_OS_Lunix.Backend.Results;

namespace Control_OS_Lunix.UI.Controllers;

public interface IDeviceDialogController
{
    Task<Result<DevicePowerConfig?>> ShowCreateAsync(IWin32Window owner, GlobalSettings settings);

    Task<Result<DevicePowerConfig?>> ShowEditAsync(IWin32Window owner, GlobalSettings settings, DevicePowerConfig existingDevice);
}
