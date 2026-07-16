# DLMS Simulator

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

## Quick start
Prerequisites: **.NET 9 SDK** and **Node.js 18+**.

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

For hot-reload development (UI on :5173 proxying to the backend), see
[DlmsSimulatorGui/README.md](DlmsSimulatorGui/README.md). For how to operate the
simulator, see [DlmsSimulatorGui/USAGE.md](DlmsSimulatorGui/USAGE.md).

## Credits & license
Built on the [Gurux DLMS library](https://github.com/Gurux/Gurux.DLMS.Net)
(© Gurux Ltd), which is included here under **GPL-2.0-only**. Because this
project links that library, it is likewise licensed under **GPL-2.0-only**.
The vendored Gurux sources keep their original license headers; see
[Gurux.DLMS.Net/LICENSE](Gurux.DLMS.Net/LICENSE).
