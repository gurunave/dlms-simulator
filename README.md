# DLMS Simulator

> Web-based GUI for the Gurux DLMS/COSEM meter simulator — create virtual DLMS
> meters, edit their COSEM objects, and watch client traffic live in the browser.

[![Release](https://github.com/gurunave/dlms-simulator/actions/workflows/release.yml/badge.svg)](https://github.com/gurunave/dlms-simulator/actions/workflows/release.yml)
![License](https://img.shields.io/badge/license-GPL--2.0-blue)
![Backend](https://img.shields.io/badge/backend-ASP.NET%20Core%20(.NET%209)-512BD4)
![Frontend](https://img.shields.io/badge/frontend-React%20%2B%20Vite-61DAFB)
![Realtime](https://img.shields.io/badge/realtime-SignalR-ff6f00)
![Platform](https://img.shields.io/badge/platform-Windows%20%7C%20Linux%20%7C%20macOS-informational)

A web-based GUI for the **Gurux DLMS/COSEM meter simulator**. Create virtual
DLMS meters, browse and edit their COSEM objects, and watch live client traffic
in the browser — no physical meter required.

The GUI runs the Gurux DLMS library **in-process** (not by shelling out to the
console tool), so meters can be created, started/stopped, inspected, and edited
at runtime, with reads/writes and connections streamed live over SignalR.

## Repository layout
```
DlmsSimulatorGui/        The GUI (this project)
  backend/               ASP.NET Core Web API + SignalR (net9.0)
  frontend/              React + Vite UI
  tools/DlmsProbe/       Tiny DLMS client used to verify a meter end-to-end
  README.md              Architecture & developer setup
  USAGE.md               How to use the running app
Gurux.DLMS.Net/          Vendored Gurux DLMS library (GPL-2.0) — the GUI builds
  Development/           against this. Only the library + simulator sources are
  Gurux.DLMS.Simulator.Net/   included; unused Gurux example projects are omitted.
```

## Download
**Prerequisites: none** — the prebuilt builds are self-contained (the .NET
runtime is bundled). You just need a supported 64-bit OS (Windows 10/11, Linux,
or macOS 12+) and a web browser.

Prebuilt standalone builds are attached to
[Releases](https://github.com/gurunave/dlms-simulator/releases):

| Platform | Asset | Run |
|----------|-------|-----|
| Windows x64 | `DlmsSimulator-win-x64.zip` | double-click **`Start DLMS Simulator.cmd`** |
| Linux x64 | `DlmsSimulator-linux-x64.tar.gz` | `./start-dlms-simulator.sh` |
| macOS (Apple Silicon) | `DlmsSimulator-osx-arm64.tar.gz` | `./start-dlms-simulator.sh` |
| macOS (Intel) | `DlmsSimulator-osx-x64.tar.gz` | `./start-dlms-simulator.sh` |

The UI listens on all network interfaces, so other machines on the same LAN can
open it at `http://<host-ip>:5000` (allow the port through the firewall).

Releases are produced automatically by GitHub Actions when a version tag is
pushed (`git tag v1.1.0 && git push origin v1.1.0`).

## Quick start (from source)
Prerequisites: **.NET 9 SDK (9.0.x)**, **Node.js 18+** (20 LTS recommended) + npm,
and **Git**. See [DlmsSimulatorGui/README.md](DlmsSimulatorGui/README.md#prerequisites)
for the full list.

```bash
# 1) Build the UI into the backend
cd DlmsSimulatorGui/frontend
npm install
npm run build

# 2) Run the backend (serves the API + the built UI)
cd ../backend
dotnet run --urls http://localhost:5100
```
Open **http://localhost:5100**.

### Prefer a standalone executable?
Build a self-contained Windows EXE (bundles the .NET runtime, UI, and templates —
no install needed to run it):
```bash
cd DlmsSimulatorGui/frontend && npm install && npm run build
cd ../backend && dotnet publish DlmsSimulatorGui.Api.csproj -c Release -r win-x64 \
  --self-contained true -p:PublishSingleFile=true \
  -p:IncludeNativeLibrariesForSelfExtract=true -o ../dist/win-x64
```
Then run `dist/win-x64/DlmsSimulatorGui.Api.exe` (default http://localhost:5000).
Details and other platforms: [DlmsSimulatorGui/README.md](DlmsSimulatorGui/README.md#build-a-standalone-exe).

For hot-reload development (UI on :5173 proxying to the backend), see
[DlmsSimulatorGui/README.md](DlmsSimulatorGui/README.md). For how to operate the
simulator, see [DlmsSimulatorGui/USAGE.md](DlmsSimulatorGui/USAGE.md).

## Credits & license
Built on the [Gurux DLMS library](https://github.com/Gurux/Gurux.DLMS.Net)
(© Gurux Ltd), which is included here under **GPL-2.0-only**. Because this
project links that library, it is likewise licensed under **GPL-2.0-only**.
The vendored Gurux sources keep their original license headers; see
[Gurux.DLMS.Net/LICENSE](Gurux.DLMS.Net/LICENSE).
