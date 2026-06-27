using System.Globalization;
using System.Net;
using System.Text.RegularExpressions;
using Control_OS_Lunix.Model;

namespace Control_OS_Lunix.Service;

public static partial class DeviceValidator
{
    private static readonly Regex MacAddressRegex = CreateMacAddressRegex();

    public static string? ValidateForSave(DevicePowerConfig device)
    {
        if (string.IsNullOrWhiteSpace(device.Name))
        {
            return "Device name is required.";
        }

        if (!IPAddress.TryParse(device.IpAddress, out _))
        {
            return "A valid IP address is required.";
        }

        if (!string.IsNullOrWhiteSpace(device.MacAddress) && !IsValidMacAddress(device.MacAddress))
        {
            return "MAC address format is invalid.";
        }

        if (!string.IsNullOrWhiteSpace(device.BroadcastAddress) &&
            !IPAddress.TryParse(device.BroadcastAddress, out _))
        {
            return "Broadcast address format is invalid.";
        }

        if (device.WolPort is <= 0 or > 65535)
        {
            return "Wake on LAN port must be between 1 and 65535.";
        }

        if (device.SshPort is <= 0 or > 65535)
        {
            return "SSH port must be between 1 and 65535.";
        }

        return null;
    }

    public static string? ValidateForOperation(DevicePowerConfig device, DevicePowerOperation operation)
    {
        return operation switch
        {
            DevicePowerOperation.Start when string.IsNullOrWhiteSpace(device.MacAddress) =>
                "Wake on LAN requires a MAC address.",
            DevicePowerOperation.Start when !IsValidMacAddress(device.MacAddress) =>
                "Wake on LAN requires a valid MAC address.",
            DevicePowerOperation.Start when string.IsNullOrWhiteSpace(device.BroadcastAddress) =>
                "Wake on LAN requires a broadcast address.",
            DevicePowerOperation.Reboot or DevicePowerOperation.Shutdown
                when string.IsNullOrWhiteSpace(device.SshUsername) ||
                     string.IsNullOrWhiteSpace(device.SshPassword) =>
                "SSH username and password are required for shutdown and reboot.",
            _ => null
        };
    }

    public static bool IsValidMacAddress(string value)
    {
        return MacAddressRegex.IsMatch(value.Trim());
    }

    public static string NormalizeMacAddress(string value)
    {
        string clean = new string(value.Where(Uri.IsHexDigit).ToArray()).ToUpperInvariant();

        return string.Join(":",
            Enumerable.Range(0, clean.Length / 2)
                .Select(index => clean.Substring(index * 2, 2).ToUpper(CultureInfo.InvariantCulture)));
    }

    [GeneratedRegex(@"^([0-9A-Fa-f]{2}[:-]){5}([0-9A-Fa-f]{2})$|^[0-9A-Fa-f]{12}$")]
    private static partial Regex CreateMacAddressRegex();
}
