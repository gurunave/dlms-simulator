# Architecture

How the DLMS Simulator is put together, and **why**. Diagrams are Mermaid (render
on GitHub). Pair this with [../CLAUDE.md](../CLAUDE.md) and
[EXTENDING.md](EXTENDING.md).

---

## 1. System context

One process (`DlmsSimulatorGui.Api`) hosts everything: the REST API, the SignalR
hub, the static React UI, and — in-process — the Gurux DLMS meter servers. Two
kinds of external actor connect to it over two different channels.

```mermaid
flowchart LR
  user["Operator's browser"]
  hes["DLMS/COSEM client<br/>(head-end system, Gurux tools, DlmsProbe)"]

  subgraph proc["DlmsSimulatorGui.Api  (single process, net9.0)"]
    ui["React UI<br/>(served from wwwroot)"]
    api["Minimal REST API<br/>/api/*"]
    hub["SignalR hub<br/>/hub/simulator"]
    mm["MeterManager<br/>(singleton)"]
    m1["SimMeter :4061"]
    m2["SimMeter :4062"]
  end

  user -->|"HTTP (REST) + WebSocket (SignalR)"| api
  user -. loads .-> ui
  api --> mm
  hub --> user
  mm --> m1 & m2
  m1 & m2 -->|"read/write & connect events"| mm --> hub
  hes -->|"DLMS over TCP"| m1
  hes -->|"DLMS over TCP"| m2
```

Key idea: the **browser talks HTTP/WebSocket** to control and observe meters,
while **DLMS clients talk raw DLMS/TCP** directly to each meter's port. The
simulator bridges the two — every DLMS read/connect becomes a SignalR event.

---

## 2. Backend modules

```mermaid
flowchart TD
  Program["Program.cs<br/>host · DI · CORS · SPA · endpoints · seeding"]
  Hub["SimulatorHub.cs<br/>(empty; push target)"]
  MM["MeterManager.cs<br/>lifecycle + event bridge"]
  Models["Models.cs<br/>DTOs"]
  Sim["SimMeter.cs<br/>: GXDLMSMeter"]
  GX["GXDLMSMeter.cs (linked, vendored)"]
  Lib["Gurux.DLMS (Development project)"]
  Net["Gurux.Net (GXNet TCP server)"]

  Program --> MM
  Program --> Hub
  Program --> Models
  MM --> Sim
  MM --> Models
  MM -->|IHubContext| Hub
  MM --> Net
  Sim --> GX
  GX --> Lib
  Net --> Lib
```

**The three abstractions that matter:**

| Type | Responsibility | Why it exists |
|------|----------------|---------------|
| `MeterManager` (singleton) | Owns every meter's lifecycle (create/start/stop/delete), reads/edits COSEM objects, and forwards DLMS events to SignalR. | Single source of truth for meter state; keeps `Program.cs` endpoints thin and gives one place to bridge library events to the UI. |
| `SimMeter : GXDLMSMeter` | Overrides `PostRead`/`PostWrite` to raise an `Accessed` callback carrying `{LN, objectType, index, value, kind}`. | Adds live-activity hooks **without editing vendored Gurux source**. The base class already implements the whole DLMS server. |
| `SimulatorHub` (SignalR) | Channel the server pushes `activity` and `meterStatus` messages over. | Real-time UI updates without polling. The hub itself has no methods — it's push-only. |

Each running meter is held in a private `MeterManager.Instance` record:
`{ Id, Config, SimMeter?, GXNet?, MeterStatus, ClientCount, Error }`.

---

## 3. Meter lifecycle + a client read (sequence)

```mermaid
sequenceDiagram
  participant UI as Browser (React)
  participant API as Minimal API
  participant MM as MeterManager
  participant SM as SimMeter (+GXNet)
  participant Hub as SignalR
  participant C as DLMS client

  UI->>API: POST /api/meters {port, template, ...}
  API->>MM: Create(req)
  MM-->>UI: MeterInfo (status=Stopped)

  UI->>API: POST /api/meters/{id}/start
  API->>MM: Start(id)
  MM->>SM: new SimMeter + GXNet(Tcp,port){Server=true}
  MM->>SM: Initialize(net, trace, templatePath, serial, false, null)
  Note over SM: loads COSEM objects from template<br/>and opens the TCP listener
  MM->>Hub: meterStatus (Running)
  Hub-->>UI: meterStatus

  C->>SM: TCP connect
  SM->>MM: GXNet OnClientConnected
  MM->>Hub: activity {kind: connected}
  Hub-->>UI: activity

  C->>SM: DLMS GET (read attribute)
  Note over SM: PostRead override fires
  SM->>MM: Accessed {LN, index, value, kind: read}
  MM->>Hub: activity {kind: read}
  Hub-->>UI: activity (live log updates)
```

Notes:
- `Initialize(...)` does **both** the object load and the socket open (non-exclusive
  mode = one `GXNet` server per meter on its own port).
- `MeterManager` subscribes to the `GXNet` connect/disconnect events *before*
  `Initialize` opens the socket, so no early events are missed.

---

## 4. Live-activity event flow

```mermaid
flowchart LR
  subgraph meter["Running meter"]
    pr["SimMeter.PostRead / PostWrite"]
    ev["GXNet OnClientConnected / OnClientDisconnected"]
  end
  pr -->|Accessed callback| onacc["MeterManager.OnAccessed"]
  ev -->|handler| onc["MeterManager.OnClient"]
  onacc --> push["IHubContext.Clients.All.SendAsync('activity', ...)"]
  onc --> push
  onc --> status["SendAsync('meterStatus', ...)  (client count changed)"]
  push --> browser["Browser: App.jsx onActivity -> ActivityLog"]
  status --> browser2["Browser: App.jsx onStatus -> MeterList/ObjectTable"]
```

Payloads are defined in `Models.cs`: `ActivityEvent { meterId, kind, detail,
logicalName, index, value, time }` and `MeterInfo { id, name, port, serial,
status, clientCount, objectCount, ... }`.

---

## 5. Frontend data flow

```mermaid
flowchart TD
  App["App.jsx<br/>owns: meters[], templates[], selectedId, activity[]"]
  api["api.js (fetch /api/*)"]
  sr["signalr.js (/hub/simulator)"]
  CM["CreateMeter.jsx"]
  ML["MeterList.jsx"]
  OT["ObjectTable.jsx"]
  AL["ActivityLog.jsx"]

  App --> api
  App --> sr
  App --> CM & ML & OT & AL
  api -->|meters, templates, objects| App
  sr -->|activity, meterStatus| App
  CM -->|create| App
  ML -->|start/stop/delete/select| App
  OT -->|edit attribute| App
```

- `App.jsx` holds all state and passes data + callbacks down (no Redux/Context).
- REST responses refresh `meters`/`templates`; SignalR `meterStatus` patches a
  single meter in place; `activity` prepends to a capped (300) list.
- `ObjectTable` fetches `/api/meters/{id}/objects` when the selection changes and
  supports inline editing (running meters only).

---

## 6. Build & packaging pipeline

```mermaid
flowchart LR
  fe["frontend: npm run build"] -->|emits| ww["backend/wwwroot"]
  ww --> pub["dotnet publish -r RID --self-contained /PublishSingleFile"]
  tpl["templates (Content glob in csproj)"] --> pub
  pub --> out["dist/RID: exe + wwwroot + templates + tools"]
  pk["packaging/ launcher + readme"] --> zip["zip / tar.gz"]
  out --> zip
  zip --> rel["GitHub Release (on v* tag)"]
```

The `.csproj` bundles the meter templates into the output (`<Content>` glob), so a
published single-file build is fully portable — no source tree needed at runtime.

---

## Design rationale (the "why")
- **In-process, not process-wrapping.** Referencing the Gurux library directly
  lets the UI read/edit live COSEM object state and hook every read/write. Wrapping
  the console `.exe` could only start/stop it and scrape stdout.
- **Subclass, don't fork.** `SimMeter` adds hooks by overriding virtual methods, so
  the vendored Gurux source stays pristine and upgradeable.
- **One process serves everything.** Simpler to run and distribute (a single exe),
  and the UI is same-origin with the API, so LAN access needs no CORS.
- **Templates are the meter's source of truth.** Loading, editing, and persistence
  all go through the Gurux `GXDLMSObjectCollection` <-> XML template.
