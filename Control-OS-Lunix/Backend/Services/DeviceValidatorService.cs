using System.Globalization;
using System.Net;
using System.Text.RegularExpressions;
using Control_OS_Lunix.Backend.Interfaces;
using Control_OS_Lunix.Backend.Models;
using Control_OS_Lunix.Backend.Results;

namespace Control_OS_Lunix.Backend.Services;

public sealed partial class DeviceValidatorService : IDeviceValidator
{
    private static readonly Regex MacAddressRegex = CreateMacAddressRegex();

    public Result<DevicePowerConfig> ValidateForSave(DevicePowerConfig device)
    {
        if (string.IsNullOrWhiteSpace(device.Name))
        {
            return Result<DevicePowerConfig>.Failure("device.name.required", "Device name is required.");
        }

        if (!IPAddress.TryParse(device.IpAddress, out _))
        {
            return Result<DevicePowerConfig>.Failure("device.ip.invalid", "A valid IP address is required.");
        }

        if (!string.IsNullOrWhiteSpace(device.MacAddress) && !IsValidMacAddress(device.MacAddress))
        {
            return Result<DevicePowerConfig>.Failure("device.mac.invalid", "MAC address format is invalid.");
        }

        if (!string.IsNullOrWhiteSpace(device.BroadcastAddress) &&
            !IPAddress.TryParse(device.BroadcastAddress, out _))
        {
            return Result<DevicePowerConfig>.Failure("device.broadcast.invalid", "Broadcast address format is invalid.");
        }

        if (device.WolPort is <= 0 or > 65535)
        {
            return Result<DevicePowerConfig>.Failure("device.wolport.invalid", "Wake on LAN port must be between 1 and 65535.");
        }

        if (device.SshPort is <= 0 or > 65535)
        {
            return Result<DevicePowerConfig>.Failure("device.sshport.invalid", "SSH port must be between 1 and 65535.");
        }

        return Result<DevicePowerConfig>.Success(device);
    }

    public Result ValidateForOperation(DevicePowerConfig device, DevicePowerOperation operation)
    {
        if (operation == DevicePowerOperation.Start && string.IsNullOrWhiteSpace(device.MacAddress))
        {
            return Result.Failure("device.mac.required", "Wake on LAN requires a MAC address.");
        }

        if (operation == DevicePowerOperation.Start && !IsValidMacAddress(device.MacAddress))
        {
            return Result.Failure("device.mac.invalid", "Wake on LAN requires a valid MAC address.");
        }

        if (operation == DevicePowerOperation.Start && string.IsNullOrWhiteSpace(device.BroadcastAddress))
        {
            return Result.Failure("device.broadcast.required", "Wake on LAN requires a broadcast address.");
        }

        if ((operation == DevicePowerOperation.Reboot || operation == DevicePowerOperation.Shutdown) &&
            (string.IsNullOrWhiteSpace(device.SshUsername) || string.IsNullOrWhiteSpace(device.SshPassword)))
        {
            return Result.Failure("device.ssh.required", "SSH username and password are required for shutdown and reboot.");
        }

        return Result.Success();
    }

    public string NormalizeMacAddress(string value)
    {
        string clean = new string(value.Where(Uri.IsHexDigit).ToArray()).ToUpperInvariant();

        return string.Join(":",
            Enumerable.Range(0, clean.Length / 2)
                .Select(index => clean.Substring(index * 2, 2).ToUpper(CultureInfo.InvariantCulture)));
    }

    private static bool IsValidMacAddress(string value)
    {
        return MacAddressRegex.IsMatch(value.Trim());
    }

    [GeneratedRegex(@"^([0-9A-Fa-f]{2}[:-]){5}([0-9A-Fa-f]{2})$|^[0-9A-Fa-f]{12}$")]
    private static partial Regex CreateMacAddressRegex();
}
