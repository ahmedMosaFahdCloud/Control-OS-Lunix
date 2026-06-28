using ControlOS.Api.Backend.Models;
using ControlOS.Api.Backend.Results;

namespace ControlOS.Api.Backend.Interfaces;

public interface IDeviceValidator
{
    Result<DevicePowerConfig> ValidateForSave(DevicePowerConfig device);

    Result ValidateForOperation(DevicePowerConfig device, DevicePowerOperation operation);

    string NormalizeMacAddress(string value);
}
