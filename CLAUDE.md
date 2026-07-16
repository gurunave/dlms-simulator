# CLAUDE.md — agent guide to this repository

This file orients an LLM/coding agent quickly. Read it first, then
[docs/ARCHITECTURE.md](docs/ARCHITECTURE.md) and [docs/EXTENDING.md](docs/EXTENDING.md)
for depth. (Non-Claude tools: see [AGENTS.md](AGENTS.md), which points here.)

> **Knowledge graph available.** `graphify-out/` holds a prebuilt code knowledge
> graph (`graph.json`, human report `GRAPH_REPORT.md`, interactive `graph.html`).
> With the graphify skill you can answer questions from it without re-reading files:
> `graphify query "how does a client read reach the UI?"`. Its god nodes
> (`MeterManager`, `SimMeter`, `SimulatorHub`, `MeterInfo`) are the core abstractions.

## What this project is
A **web GUI for the Gurux DLMS/COSEM meter simulator**. It runs the Gurux DLMS
library **in-process** (not by shelling out to Gurux's console tool), exposes a
REST + SignalR API, and serves a React UI. Users create virtual DLMS meters,
start/stop them, browse and edit their COSEM objects, and watch live client
traffic. Real DLMS clients connect over TCP to each meter's port.

## Repository map
```
CLAUDE.md, AGENTS.md            <- agent guides (this + pointer)
docs/ARCHITECTURE.md            <- diagrams + design rationale
docs/EXTENDING.md               <- how-to recipes for new features
README.md                       <- human overview + download + quick start
DlmsSimulatorGui/
  backend/                      <- ASP.NET Core (net9.0), the whole server
    Program.cs                  <- host, DI, CORS, SPA hosting, REST endpoints,
                                   SignalR map, template seeding, 0.0.0.0 binding
    Simulator/
      SimMeter.cs               <- subclass of Gurux GXDLMSMeter; adds read/write hooks
      MeterManager.cs           <- singleton: meter lifecycle + event->SignalR bridge
      Models.cs                 <- DTOs (requests, MeterInfo, CosemObjectDto, ActivityEvent)
      GXDLMSMeter.cs (linked)   <- the Gurux meter/server, compiled in via <Compile Include>
    Hubs/SimulatorHub.cs        <- SignalR hub (server pushes "activity" + "meterStatus")
    wwwroot/                    <- built React app (generated; git-ignored)
    templates/                  <- meter templates at runtime (seeded; git-ignored)
  frontend/                     <- React + Vite UI
    src/App.jsx                 <- state, hub wiring, 3-pane layout
    src/api.js                  <- REST wrapper      src/signalr.js <- hub client
    src/components/             <- CreateMeter, MeterList, ObjectTable, ActivityLog
    vite.config.js              <- dev proxy (/api,/hub -> :5100); build -> backend/wwwroot
  tools/DlmsProbe/              <- tiny real DLMS client for end-to-end testing
  packaging/                    <- launchers + readmes bundled into release zips
Gurux.DLMS.Net/                 <- VENDORED Gurux library (GPL-2.0). Do not "improve".
  Development/                  <- the DLMS library project (referenced)
  Gurux.DLMS.Simulator.Net/     <- source of the linked GXDLMSMeter.cs + GXDLMSReader.cs + templates
.github/workflows/release.yml   <- multi-platform build + GitHub Release on v* tag
```

## Commands (run from `DlmsSimulatorGui/`)
```bash
# Dev (hot reload): two terminals
cd backend  && dotnet run --urls http://localhost:5100
cd frontend && npm install && npm run dev          # UI :5173, proxies to :5100

# Single app: build UI into backend, then run backend alone
cd frontend && npm run build                         # -> ../backend/wwwroot
cd backend  && dotnet run --urls http://localhost:5100

# End-to-end check: with a meter running on 4061
cd tools/DlmsProbe && dotnet run 127.0.0.1 4061      # associates + reads 3 attributes

# Standalone EXE (self-contained, bundles runtime + UI + templates)
cd backend && dotnet publish DlmsSimulatorGui.Api.csproj -c Release -r win-x64 \
  --self-contained true -p:PublishSingleFile=true \
  -p:IncludeNativeLibrariesForSelfExtract=true -o ../dist/win-x64
```
There is **no automated test suite**; verify by running the app and/or DlmsProbe.

## How it works (30-second version)
- `MeterManager` (singleton) owns a dictionary of meter instances. Each running
  meter is a `SimMeter` bound to a `GXNet` TCP server on its own port.
- **Starting** a meter calls `meter.Initialize(net, trace, templatePath, serial, false, null)`
  — this loads COSEM objects from the template XML and opens the listening socket.
- **Live activity**: `SimMeter` overrides `PostRead`/`PostWrite` and the `GXNet`
  connect/disconnect events feed `MeterManager`, which pushes them to the browser
  via `IHubContext<SimulatorHub>` (`activity` + `meterStatus` messages).
- **Editing** a value: `PUT /api/meters/{id}/objects/{ln}` -> `MeterManager.SetAttribute`
  -> `IGXDLMSBase.SetValue(meter.Settings, ValueEventArgs)` and re-saves the template.
See [docs/ARCHITECTURE.md](docs/ARCHITECTURE.md) for diagrams.

## REST + realtime API
| Method | Route | Purpose |
|---|---|---|
| GET | `/api/templates` | list template XMLs |
| POST | `/api/templates` | upload a template (multipart, field `file`) |
| GET | `/api/meters` / `/api/meters/{id}` | list / get meters |
| POST | `/api/meters` | create `{name,port,serial,template,useLogicalName,interface}` |
| POST | `/api/meters/{id}/start` \| `/stop` | start / stop |
| DELETE | `/api/meters/{id}` | delete |
| GET | `/api/meters/{id}/objects` | COSEM objects + attribute values |
| PUT | `/api/meters/{id}/objects/{ln}` | set attribute `{index,value}` |
| WS | `/hub/simulator` | SignalR: server emits `activity`, `meterStatus` |

## Conventions
- **Backend:** C# minimal APIs in `Program.cs`; business logic in `MeterManager`;
  DTOs in `Models.cs`. Keep endpoints thin. Namespaces `DlmsSimulatorGui.Api.*`.
  `SimMeter` lives in Gurux's `Gurux.DLMS.Simulator.Net` namespace on purpose so it
  can subclass the internal `GXDLMSMeter`.
- **Frontend:** function components + hooks, no state library; all server calls go
  through `src/api.js`; all realtime through `src/signalr.js`. Plain CSS in `styles.css`.
- **Commits:** conventional-ish prefixes (`feat:`, `docs:`, `ci:`, `chore:`);
  end messages with the `Co-Authored-By: Claude ...` trailer.

## Gotchas / non-obvious decisions (READ before editing)
1. **Do not modify `Gurux.DLMS.Net/**`** — it's a vendored GPL dependency. Extend
   behavior by subclassing (as `SimMeter` does), not by editing Gurux source.
2. `GXDLMSMeter.cs` is **linked** into the backend via `<Compile Include>` (see the
   `.csproj`), not copied. `SimMeter : GXDLMSMeter` overrides `PostRead`/`PostWrite`.
3. The vendored `Development/Gurux.DLMS.Net.csproj` was trimmed to **`net9.0`**
   (it originally multi-targeted up to net10.0, which the .NET 9 SDK can't build).
   Original list preserved in a csproj comment.
4. **Editing values requires the meter to be Running**; attribute 1 (logical name)
   is read-only. `MeterManager.ConvertValue` marshals the string to the attribute's
   .NET type (add cases there to support more types).
5. **Template seeding:** on first run the backend copies templates from the vendored
   sim folder if `backend/templates/` is empty; published builds bundle them via a
   `<Content>` glob in the `.csproj`, so the EXE is portable.
6. **Network binding:** defaults to `http://0.0.0.0:5000` (LAN-reachable) unless
   `ASPNETCORE_URLS` or `--urls` is set (see top of `Program.cs`).
7. **CORS** ("dev" policy) only matters for the split dev servers (:5173 -> :5100).
   Single-app mode is same-origin, so LAN access needs no CORS change.
8. **Single-file publish:** `Assembly.Location` is empty; use `AppContext.BaseDirectory`
   if you need the app folder (the IL3000 warnings point at Gurux code, not ours).
9. Each meter needs a **unique TCP port**; `MeterManager.Create` rejects duplicates.

## Releasing
Push a tag `vX.Y.Z`; `.github/workflows/release.yml` builds win-x64, linux-x64,
osx-arm64, osx-x64 (self-contained) and attaches them to a GitHub Release. It also
runs on manual dispatch (artifacts only, no release).
