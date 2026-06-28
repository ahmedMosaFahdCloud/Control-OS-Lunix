# Control-OS-Lunix

Control-OS-Lunix is a Windows-first network power control platform for managing Linux devices on a local network through a modern web UI.

The architecture is split into two backend projects and one frontend:

- `ControlOS.Api`
  An ASP.NET Core Web API that exposes devices, settings, logs, backups, and network scanning. Also hosts the Angular SPA in production.
- `ControlOS.Launcher`
  A WinForms system-tray desktop app that starts the API in-process and serves as the recommended launcher.
- `control-os-web`
  An Angular 21 standalone frontend built with Tailwind CSS and `spartan.ng`.

## What The Platform Does

- Start remote devices using Wake-on-LAN.
- Reboot and shut down Linux devices over SSH.
- Scan the local network for reachable hosts and capture IP, hostname, and MAC address.
- Maintain a managed device list with per-device power settings.
- Automatically start devices when the controller machine boots.
- Automatically shut down devices when the controller process stops.
- Store operation logs and expose them through the API and web UI.
- Create and restore backup archives for configuration and logs.

## Web Areas

- `Dashboard` — Summary cards, recent activity, device overview, and backup actions.
- `Devices` — Create, edit, delete, and operate managed devices.
- `Scanner` — Scan the subnet and convert reachable hosts into managed devices.
- `Logs` — Review controller and device activity from the browser.
- `Settings` — Manage timeouts, automation, logging, and restore workflow.

## Solution Structure

```text
Control-OS-Lunix/
├─ src/
│  ├─ ControlOS.Api/         # ASP.NET Core Web API + automation worker
│  ├─ ControlOS.Launcher/    # WinForms system-tray desktop launcher
│  └─ control-os-web/        # Angular 21 SPA frontend
└─ Control-OS-Linux.sln
```

## Desktop App (Recommended)

`ControlOS.Launcher` is the single app you should run.
It starts the API in-process, serves the Angular frontend from `wwwroot/browser/`, and lives in the system tray.

- Auto-opens your browser to the dashboard on launch
- Minimizes to system tray (near the clock) when you close the window
- Right-click tray icon for options: Show Control OS, Open Browser, Open Config Folder, Auto-start with Windows, Exit

## Project Roles

### ControlOS.Api

The HTTP bridge between the browser UI and the local controller services.

Main responsibilities:

- Dashboard data
- Device CRUD
- Manual start / reboot / shutdown
- Settings management
- Network scanning
- Logs
- Backup and restore
- Controller startup and shutdown automation (via a hosted `ControllerAutomationWorker`)

Default local URL (standalone mode): configured via `launchSettings.json` / environment.

### ControlOS.Launcher

The recommended desktop launcher. A WinForms system-tray app that:

- Starts the entire ASP.NET Core API in-process at `http://localhost:58432`
- Serves the Angular SPA from `wwwroot\browser\`
- Shows a status window (minimizes to tray on close)
- Auto-opens the dashboard in your default browser on startup
- Provides a tray icon with quick actions: Open Browser, Config Folder, Auto-start with Windows, Exit
- Assembly output name is `ControlOS.exe`

### control-os-web

The Angular frontend.

Main characteristics:

- Standalone components (Angular 21)
- Typed API contracts
- Tailwind CSS
- Local `spartan.ng`-style UI primitives
- Browser-based control plane

## Technology Stack

- .NET 8 (targets `net8.0-windows`)
- ASP.NET Core Web API
- WinForms
- Angular 21
- Tailwind CSS
- `spartan.ng`
- `SSH.NET`
- Windows DPAPI for SSH password protection
- `Microsoft.Extensions.Hosting`

## Remote Power Model

### Start

Sends a Wake-on-LAN magic packet to the configured MAC address and broadcast address.

### Shutdown / Reboot

Connects over SSH and runs `systemctl` commands through `sudo`.
If the target requires a sudo password and one is configured, the backend falls back to `sudo -S`.

## Windows-Specific Notes

This platform is intentionally Windows-centric on the controller side:

- DPAPI is used for SSH password protection.
- Windows registry startup registration is used for auto-start.
- Controller lifecycle automation is designed around a Windows host.

Backend projects target `net8.0-windows`.

## Data Storage

Application data is stored in:

```
%LocalAppData%\Control-OS-Lunix
```

Typical files:

- `config.json`
- `activity.log`
- `backups\*.zip`

## Security Notes

- SSH passwords are never stored as plain text in persisted JSON.
- Passwords are protected using Windows DPAPI (`DataProtectionScope.CurrentUser`).
- Protected credentials are tied to the current Windows user profile.
- Restoring a backup on a different user profile or machine may require re-entering SSH passwords.

## Build

### Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [Node.js](https://nodejs.org/) (for the Angular frontend)

### Quick Build (Desktop App)

Produce `ControlOS.exe` with the Angular UI bundled:

```powershell
# 1. Build the Angular frontend
cd src\control-os-web
npm install
npm run build

# 2. Copy Angular output into the Launcher project
cd ..\..
Copy-Item -Recurse -Force src\ControlOS.Api\wwwroot\browser src\ControlOS.Launcher\wwwroot\

# 3. Build the desktop launcher
dotnet build src\ControlOS.Launcher\ControlOS.Launcher.csproj
```

Output: `src\ControlOS.Launcher\bin\Debug\net8.0-windows\ControlOS.exe`

### Run

```powershell
.\src\ControlOS.Launcher\bin\Debug\net8.0-windows\ControlOS.exe
```

The app opens your browser to `http://localhost:58432` and adds an icon to the system tray.

### Build Individual Components

#### Backend

```powershell
# API only
dotnet build .\src\ControlOS.Api\ControlOS.Api.csproj

# Launcher only
dotnet build .\src\ControlOS.Launcher\ControlOS.Launcher.csproj

# All backend projects
dotnet build .\Control-OS-Linux.sln
```

#### Frontend (standalone dev)

```powershell
cd .\src\control-os-web
npm install
npm run build       # production build
npm run start       # dev server on http://localhost:4200
```
