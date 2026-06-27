# Control-OS-Lunix

Control-OS-Lunix is a Windows desktop application for managing the power lifecycle of Linux devices on a local network. It provides Wake-on-LAN startup, SSH-based shutdown and reboot, network discovery, startup/shutdown automation, activity logs, and backup/restore for application data.

The application is built with WinForms on .NET 8 and has been refactored into a layered structure using Dependency Injection, a local Result pattern, and strict MVC for every screen.

## What The App Does

- Start remote devices using Wake-on-LAN.
- Reboot and shut down Linux devices over SSH.
- Scan the local network for reachable hosts and capture IP, host name, and MAC address when available.
- Maintain a managed device list with per-device power settings.
- Automatically start devices when the controller machine boots the app.
- Automatically shut down devices when Windows is shutting down or when the app closes.
- Store operation logs and show them in a dedicated logs screen.
- Create and restore backup archives for configuration and logs.

## Main Screens

- `Main Dashboard`
  Shows device status, summary cards, quick actions, settings, logs, backup, and restore.
- `Device Dialog`
  Add or edit a managed device with Wake-on-LAN and SSH settings.
- `Settings`
  Configure controller behavior, delays, timeouts, retries, logging, and shutdown confirmation.
- `Logs`
  Review recent controller and device operations.
- `Network Scanner`
  Scan a subnet, review online hosts, and create a device draft from scan results.

## Key Features

- Strict MVC separation for each WinForms screen.
- `Result` and `Result<T>` used for expected operation failures instead of exception-driven flow.
- Dependency Injection with `Microsoft.Extensions.DependencyInjection`.
- UI and backend separation in one project:
  - `UI/Views`
  - `UI/Controllers`
  - `UI/ViewModels`
  - `Backend/Models`
  - `Backend/Interfaces`
  - `Backend/Services`
  - `Backend/Results`
  - `Composition`
- Modernized WinForms styling for a cleaner dashboard-focused UX.

## Technology Stack

- .NET 8
- Windows Forms
- C#
- `Microsoft.Extensions.DependencyInjection`
- `SSH.NET`
- Windows DPAPI for password protection

## Project Structure

```text
Control-OS-Lunix/
├─ Control-OS-Lunix/
│  ├─ Backend/
│  │  ├─ Interfaces/
│  │  ├─ Models/
│  │  ├─ Results/
│  │  └─ Services/
│  ├─ Composition/
│  ├─ UI/
│  │  ├─ Controllers/
│  │  ├─ ViewModels/
│  │  └─ Views/
│  ├─ Program.cs
│  └─ Control-OS-Linux.csproj
└─ Control-OS-Linux.sln
```

## How Remote Power Works

### Start

The app sends a Wake-on-LAN magic packet to the device MAC address and broadcast address configured for that device.

### Shutdown / Reboot

The app connects over SSH and runs:

- `sudo -n /usr/bin/systemctl poweroff`
- `sudo -n /usr/bin/systemctl reboot`

If the Linux machine requires a sudo password and the device has an SSH password configured, the app attempts a fallback using `sudo -S`.

## Windows Shutdown Awareness

The main dashboard listens for Windows shutdown/session-end messages. When Windows begins shutting down, the app can block the shutdown briefly, run the controller shutdown sequence, and then allow Windows to continue. This is how remote auto-shutdown is triggered during system shutdown rather than only when the user closes the app manually.

## Data Storage

Application data is currently stored in:

`%LocalAppData%\Control-OS-Lunix`

Typical files:

- `config.json`
- `activity.log`
- `backups\*.zip`

## Security Notes

- SSH passwords are not stored as plain text in the JSON file.
- Passwords are protected using Windows DPAPI with `DataProtectionScope.CurrentUser`.
- This means encrypted secrets are tied to the current Windows user profile.
- If the configuration file is copied to another Windows user or another machine, protected passwords may not decrypt successfully there.
- Backup archives include the stored encrypted configuration and logs, so restoring on a different user profile may require re-entering SSH passwords.

## Backup And Restore

The dashboard includes:

- `Backup Data`
  Creates a `.zip` archive containing the current application data.
- `Restore Data`
  Restores configuration and logs from a selected backup archive.

After restore, the application reloads configuration and refreshes the dashboard.

## Linux Device Requirements

For reliable shutdown and reboot:

- SSH must be enabled on the target Linux machine.
- The configured username and password must be valid.
- `systemctl` must be available.
- The account should be allowed to run shutdown/reboot commands.

For passwordless sudo, you can configure sudoers appropriately. If passwordless sudo is not enabled, the app may still succeed using the stored SSH password through the `sudo -S` fallback.

## Build And Run

### Requirements

- Windows
- .NET 8 SDK
- Network access to target devices

### Build

```powershell
dotnet build .\Control-OS-Linux.sln
```

### Run

```powershell
dotnet run --project .\Control-OS-Lunix\Control-OS-Linux.csproj
```

## Typical Workflow

1. Open the app.
2. Add a device manually or scan the subnet from the Network Scanner screen.
3. Verify MAC address, broadcast address, SSH host, and credentials.
4. Test `Start`, `Reboot`, and `Shutdown`.
5. Configure automation in Settings if you want controller startup/shutdown behavior.
6. Use `Backup Data` after your configuration is ready.

## Current Limitations

- The application targets Windows only because it depends on WinForms and Windows-specific APIs.
- Password protection is user-profile scoped because of DPAPI.
- Wake-on-LAN requires a valid MAC address and a network path that allows broadcast packets.
- MAC address discovery during scanning depends on ARP availability and may be empty for some hosts.

## Notes For Developers

- Keep expected failures in the `Result` flow instead of throwing exceptions for normal validation or connectivity issues.
- Keep business logic inside backend services and controllers, not WinForms views.
- Do not add WinForms dependencies into `Backend`.
- Register new services, controllers, and views through `Composition/ServiceRegistration.cs`.

## License

No license file is currently included in this repository.
