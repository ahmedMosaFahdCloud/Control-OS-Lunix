using System.Runtime.Versioning;
using ControlOS.Api.Features.Shared;
using Microsoft.Win32;

namespace ControlOS.Api.Infrastructure.Services;

[SupportedOSPlatform("windows")]
public sealed class WindowsStartupService
{
    private const string RegistryPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string ApplicationName = "Control-OS-Lunix";

    public Result EnsureRegistered()
    {
        try
        {
            using RegistryKey? key = Registry.CurrentUser.OpenSubKey(RegistryPath, writable: true);
            if (key is null)
            {
                return Result.Failure("startup.registry.unavailable", "Unable to access the Windows startup registry key.");
            }

            string executablePath = Environment.ProcessPath ?? string.Empty;
            if (string.IsNullOrWhiteSpace(executablePath))
            {
                return Result.Failure("startup.executable_path.missing", "Unable to determine the application executable path.");
            }

            string expectedValue = $"\"{executablePath}\"";
            string? currentValue = key.GetValue(ApplicationName)?.ToString();

            if (!string.Equals(currentValue, expectedValue, StringComparison.OrdinalIgnoreCase))
            {
                key.SetValue(ApplicationName, expectedValue);
            }

            return Result.Success();
        }
        catch (Exception exception)
        {
            return Result.Failure("startup.registration.failed", exception.Message);
        }
    }
}
