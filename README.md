# Control-OS-Lunix

Control-OS-Lunix is a Windows-first network power control platform for managing Linux devices on a local network through a modern web UI instead of a desktop UI.

The current architecture is split into three runtime layers:

- `ControlOS.Agent`
  A local Windows worker/console host responsible for controller startup and shutdown automation.
- `ControlOS.Api`
  An ASP.NET Core Web API that exposes devices, settings, logs, backups, and network scanning.
- `control-os-web`
  An Angular 21 standalone frontend built with Tailwind and `spartan.ng`.

The original WinForms project is still in the repository as a legacy reference, but it is no longer the primary solution path.

## What The Platform Does

- Start remote devices using Wake-on-LAN.
- Reboot and shut down Linux devices over SSH.
- Scan the local network for reachable hosts and capture IP, host name, and MAC address when available.
- Maintain a managed device list with per-device power settings.
- Automatically start devices when the controller machine boots.
- Automatically shut down devices when the controller process stops.
- Store operation logs and expose them through the API and web UI.
- Create and restore backup archives for configuration and logs.

## Web Areas

- `Dashboard`
  Summary cards, recent activity, device overview, and backup actions.
- `Devices`
  Create, edit, delete, and operate managed devices.
- `Scanner`
  Scan the subnet and convert reachable hosts into managed devices.
- `Logs`
  Review controller and device activity from the browser.
- `Settings`
  Manage timeouts, automation, logging, and restore workflow.

## Solution Structure

```text
Control-OS-Lunix/
├─ src/
│  ├─ ControlOS.Core/        # shared backend library
│  ├─ ControlOS.Api/         # ASP.NET Core Web API
│  ├─ ControlOS.Agent/       # Windows Worker Service (automation)
│  └─ ControlOS.Tray/        # Desktop tray app (recommended launcher)
├─ web/
│  └─ control-os-web/        # Angular 21 SPA frontend
└─ Control-OS-Linux.sln
```

## Desktop App (Recommended)

`ControlOS.Tray` is the single app you should use.
It bundles the API in-process, serves the Angular frontend, and lives in the system tray.

- Auto-opens your browser to the dashboard on launch
- Minimizes to system tray (near the clock) when you close the window
- Right-click tray icon for options: Show Window, Open Browser, Open Config Folder, Auto-start with Windows, Exit

## Project Roles

### ControlOS.Core

Shared backend models, interfaces, services, and result types. This layer contains the reusable power-control logic used by both the API and the agent.

### ControlOS.Api

The HTTP bridge between the browser UI and local controller services.

Main responsibilities:

- dashboard data
- device CRUD
- manual start/reboot/shutdown
- settings management
- network scanning
- logs
- backup and restore

Default local URL:

`http://localhost:5081`

### ControlOS.Agent

The local automation host for Windows.

Main responsibilities:

- register startup with Windows
- run controller startup automation
- run controller shutdown automation when the host stops

### ControlOS.Tray

The recommended desktop launcher. A WinForms system-tray app that:
- Starts the entire ASP.NET Core API in-process (`http://localhost:5081`)
- Serves the Angular SPA from `wwwroot/browser/`
- Shows a status window (minimizes to tray on close)
- Auto-opens the dashboard in your default browser
- Provides tray icon with quick actions (Open Browser, Open Config, Auto-start, Exit)

### control-os-web

The Angular frontend that replaces the desktop UX.

Main frontend characteristics:

- standalone components
- typed API contracts
- Tailwind CSS
- local `spartan.ng`-style primitives and layout
- browser-based control plane

## Technology Stack

- .NET 8
- ASP.NET Core Web API
- .NET Worker Service
- Angular 21
- Tailwind CSS
- `spartan.ng`
- `Microsoft.Extensions.DependencyInjection`
- `SSH.NET`
- Windows DPAPI for password protection

## Remote Power Model

### Start

The platform sends a Wake-on-LAN magic packet to the configured MAC address and broadcast address.

### Shutdown / Reboot

The platform connects over SSH and uses `systemctl` commands through `sudo`.

If the Linux target requires a sudo password and the device has an SSH password configured, the backend attempts a fallback using `sudo -S`.

## Windows-Specific Notes

This platform is intentionally Windows-centric on the controller side.

Reasons:

- DPAPI is used for SSH password protection.
- Windows registry startup registration is used.
- Controller lifecycle automation is designed around a Windows host.

For that reason, the backend runtime projects target `net8.0-windows`.

## Data Storage

Application data is currently stored in:

`%LocalAppData%\Control-OS-Lunix`

Typical files:

- `config.json`
- `activity.log`
- `backups\*.zip`

## Security Notes

- SSH passwords are not stored as plain text in persisted JSON.
- Passwords are protected using Windows DPAPI with `DataProtectionScope.CurrentUser`.
- Protected credentials are tied to the current Windows user profile.
- Restoring backups on another user profile or another machine may require re-entering SSH passwords.

## Build

### Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [Node.js](https://nodejs.org/) (for the Angular frontend)

### Quick Build (Desktop App)

Build everything and produce `ControlOS.exe`:

```powershell
# 1. Build Angular frontend
cd web\control-os-web
npm install
npm run build

# 2. Copy Angular output to Tray project
cd ..\..
Copy-Item -Recurse -Force src\ControlOS.Api\wwwroot\browser src\ControlOS.Tray\wwwroot\

# 3. Build the desktop tray app
dotnet build src\ControlOS.Tray\ControlOS.Tray.csproj
```

The output is at `src\ControlOS.Tray\bin\Debug\net8.0-windows\ControlOS.exe`.

### Run

```powershell
.\src\ControlOS.Tray\bin\Debug\net8.0-windows\ControlOS.exe
```

The app will open your browser to `http://localhost:5081` and add an icon to the system tray.

### Build Individual Components

#### Backend

```powershell
# API only
dotnet build .\src\ControlOS.Api\ControlOS.Api.csproj

# Agent only
dotnet build .\src\ControlOS.Agent\ControlOS.Agent.csproj

# All backend projects
dotnet build .\Control-OS-Linux.sln
```

#### Frontend (standalone dev)

```powershell
cd .\web\control-os-web
npm install
npm run build       # production build
npm run start       # dev server on http://localhost:4200
```

## Notes

- The Angular UI is now the preferred presentation layer.
- The legacy WinForms project remains in the repository only as reference code during migration.
- The solution file now focuses on the new `Core + Api + Agent` backend structure.
