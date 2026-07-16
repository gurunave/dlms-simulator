# DLMS Simulator GUI

A web-based, fully integrated GUI for the **Gurux DLMS/COSEM meter simulator**.
Unlike the stock command-line simulator, this runs the Gurux DLMS library
**in-process** so you can create, start/stop, inspect, and edit simulated meters
from the browser — and watch live client traffic in real time.

```
┌────────────────────┐   REST + SignalR    ┌───────────────────────────────┐
│  React UI (Vite)   │◄───────────────────►│  ASP.NET Core backend (net9)  │
│  :5173 (dev)       │   /api  /hub        │  references Gurux.DLMS in-proc │
└────────────────────┘                     │  SimMeter : GXDLMSMeter        │
                                           └──────────────┬────────────────┘
                                                          ▼  TCP (WRAPPER/HDLC)
                                            DLMS/COSEM clients, HES, Gurux tools
```

## Features
- Create multiple simulated meters, each on its own TCP port.
- Start / stop / delete meters at runtime.
- Browse every COSEM object and attribute loaded from the template.
- **Edit attribute values live** on a running meter (persisted back to the template).
- **Live activity log**: client connect/disconnect and every attribute read/write,
  pushed to the browser over SignalR.
- Choose from the bundled Gurux templates or upload your own `.xml`.

## Layout
```
DlmsSimulatorGui/
  backend/    ASP.NET Core Web API + SignalR (net9.0)
    Program.cs              REST endpoints, SPA hosting, template seeding
    Simulator/SimMeter.cs   subclass of Gurux GXDLMSMeter that raises read/write events
    Simulator/MeterManager.cs   meter lifecycle + event → SignalR bridge
    Hubs/SimulatorHub.cs    SignalR hub
    wwwroot/                built React app (produced by `npm run build`)
    templates/              meter templates (seeded from the Gurux repo on first run)
  frontend/   React + Vite UI
  tools/DlmsProbe/          tiny DLMS client used to verify a meter end-to-end
```
The backend links `GXDLMSMeter.cs` from `../Gurux.DLMS.Net/Gurux.DLMS.Simulator.Net`
and references `../Gurux.DLMS.Net/Development/Gurux.DLMS.Net.csproj` — the same
library the official simulator uses.

## Prerequisites

### To run a prebuilt release (end users)
**Nothing to install** — the builds on [Releases](https://github.com/gurunave/dlms-simulator/releases)
are self-contained (the .NET runtime is bundled). You only need:
- A supported 64-bit OS: **Windows 10/11**, **Linux** (glibc-based, e.g. Ubuntu 20.04+),
  or **macOS 12+** (Apple Silicon or Intel).
- A modern web browser (Chrome, Edge, Firefox, or Safari).
- For LAN access from other machines: permission to open the port through the
  firewall on the machine running the simulator.

### To build from source (developers)
| Tool | Version | Why |
|------|---------|-----|
| [.NET SDK](https://dotnet.microsoft.com/download) | **9.0.x** | Builds and runs the backend and the DLMS library. |
| [Node.js](https://nodejs.org) + npm | **18+** (built/tested on 20 LTS) | Builds the React UI. |
| [Git](https://git-scm.com) | any recent | Clone the repository. |
| Internet access | — | First build restores NuGet + npm packages. |

Any OS with the .NET 9 SDK works (Windows, Linux, macOS). Check your toolchain:
```bash
dotnet --version   # 9.0.x
node --version     # v18+  (v20 recommended)
```

> Note: the vendored `Gurux.DLMS.Net/Development/Gurux.DLMS.Net.csproj` was
> constrained to `net9.0` (it originally multi-targeted up to net10.0) so it
> builds with the .NET 9 SDK. The original TargetFrameworks list is preserved in
> a comment in that csproj. If you install a newer SDK that supports net10.0, you
> can restore the original list.

## Run — development (hot reload)
Two terminals:
```bash
# 1) backend on http://localhost:5100
cd backend
dotnet run --urls http://localhost:5100

# 2) frontend on http://localhost:5173 (proxies /api and /hub to :5100)
cd frontend
npm install
npm run dev
```
Open http://localhost:5173.

## Run — single app (production-style)
Build the UI into the backend and serve everything from one port:
```bash
cd frontend && npm run build      # emits into ../backend/wwwroot
cd ../backend && dotnet run --urls http://localhost:5100
```
Open http://localhost:5100.

## Build a standalone EXE
Produce a self-contained Windows executable that bundles the .NET runtime, the
web UI, and the meter templates — no .NET install needed on the target machine.

```bash
# 1) Build the UI so it is bundled into the exe
cd frontend && npm run build

# 2) Publish a single-file, self-contained win-x64 build
cd ../backend
dotnet publish DlmsSimulatorGui.Api.csproj -c Release -r win-x64 --self-contained true \
  -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true \
  -p:EnableCompressionInSingleFile=true -o ../dist/win-x64
```

The output in `dist/win-x64/` contains `DlmsSimulatorGui.Api.exe` plus its
`wwwroot/` and `templates/` folders. Run the exe (default
`http://localhost:5000`) or set `ASPNETCORE_URLS` to choose a port:

```bash
set ASPNETCORE_URLS=http://localhost:8080
DlmsSimulatorGui.Api.exe
```

> Swap `-r win-x64` for `linux-x64` or `osx-arm64` to target other platforms.
> Drop `--self-contained true` (and the single-file flags) for a small
> framework-dependent build that needs .NET 9 installed.

The `.csproj` copies the Gurux templates into the build output
(`CopyToOutputDirectory`), so the published exe is fully portable.

## Verify with a real DLMS client
With a meter running (e.g. port 4061):
```bash
cd tools/DlmsProbe
dotnet run 127.0.0.1 4061
```
It associates (WRAPPER, LN, no auth) and reads the logical device name, serial,
and clock. The reads appear live in the UI activity log.

## API
| Method | Route | Purpose |
|--------|-------|---------|
| GET  | `/api/templates` | list templates |
| POST | `/api/templates` | upload a template (`multipart`, field `file`) |
| GET  | `/api/meters` | list meters |
| POST | `/api/meters` | create `{name,port,serial,template,useLogicalName,interface}` |
| POST | `/api/meters/{id}/start` | start |
| POST | `/api/meters/{id}/stop` | stop |
| DELETE | `/api/meters/{id}` | delete |
| GET  | `/api/meters/{id}/objects` | COSEM objects + attribute values |
| PUT  | `/api/meters/{id}/objects/{ln}` | set attribute `{index,value}` |
| WS   | `/hub/simulator` | SignalR: `activity`, `meterStatus` |

## License
Uses the Gurux DLMS library (GPL-2.0-only); this project is therefore GPL-2.0.
