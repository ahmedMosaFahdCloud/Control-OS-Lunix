using Control_OS_Lunix.Backend.Models;
using Control_OS_Lunix.Backend.Results;

namespace Control_OS_Lunix.Backend.Interfaces;

public interface IDeviceValidator
{
    Result<DevicePowerConfig> ValidateForSave(DevicePowerConfig device);

    Result ValidateForOperation(DevicePowerConfig device, DevicePowerOperation operation);

    string NormalizeMacAddress(string value);
}
