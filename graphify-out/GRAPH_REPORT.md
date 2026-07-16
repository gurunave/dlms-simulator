# Graph Report - E:\CLAUDEPROJECTS\DLMS Simulator\DlmsSimulatorGui  (2026-07-16)

## Corpus Check
- Corpus is ~7,639 words - fits in a single context window. You may not need a graph.

## Summary
- 110 nodes · 161 edges · 13 communities (9 shown, 4 thin omitted)
- Extraction: 99% EXTRACTED · 1% INFERRED · 0% AMBIGUOUS · INFERRED: 2 edges (avg confidence: 0.85)
- Token cost: 7,548 input · 1,490 output

## Community Hubs (Navigation)
- [[_COMMUNITY_Meter Lifecycle & Management|Meter Lifecycle & Management]]
- [[_COMMUNITY_React UI Components|React UI Components]]
- [[_COMMUNITY_Frontend Build & Dependencies|Frontend Build & Dependencies]]
- [[_COMMUNITY_DLMS Meter Simulation|DLMS Meter Simulation]]
- [[_COMMUNITY_Backend Project & NuGet|Backend Project & NuGet]]
- [[_COMMUNITY_COSEM Objects & DTOs|COSEM Objects & DTOs]]
- [[_COMMUNITY_App Host & Wiring|App Host & Wiring]]
- [[_COMMUNITY_Live Activity Events|Live Activity Events]]
- [[_COMMUNITY_Unix Launcher & Config|Unix Launcher & Config]]
- [[_COMMUNITY_SignalR Bridge|SignalR Bridge]]
- [[_COMMUNITY_Project Overview|Project Overview]]

## God Nodes (most connected - your core abstractions)
1. `MeterManager` - 27 edges
2. `Instance` - 11 edges
3. `SimMeter` - 10 edges
4. `MeterInfo` - 7 edges
5. `SimulatorHub` - 4 edges
6. `scripts` - 4 edges
7. `api` - 4 edges
8. `CreateMeterRequest` - 3 edges
9. `CosemObjectDto` - 3 edges
10. `SetAttributeRequest` - 2 edges

## Surprising Connections (you probably didn't know these)
- `SimMeter` --conceptually_related_to--> `COSEM Object`  [EXTRACTED]
  backend/Simulator/SimMeter.cs → USAGE.md
- `DlmsProbe` --calls--> `SimMeter`  [INFERRED]
  tools/DlmsProbe/Program.cs → backend/Simulator/SimMeter.cs
- `MeterManager` --calls--> `SimulatorHub`  [INFERRED]
  backend/Simulator/MeterManager.cs → backend/Hubs/SimulatorHub.cs
- `React UI (Vite)` --calls--> `SimulatorHub`  [EXTRACTED]
  frontend/src/main.jsx → backend/Hubs/SimulatorHub.cs
- `MeterManager` --calls--> `SimMeter`  [EXTRACTED]
  backend/Simulator/MeterManager.cs → backend/Simulator/SimMeter.cs

## Import Cycles
- None detected.

## Hyperedges (group relationships)
- **Simulator Backend Core** — backend_simmeter, backend_metermanager, backend_simulatorhub [EXTRACTED 0.95]
- **DLMS Communication Flow** — tools_dlmsprobe, backend_simmeter, frontend_react_ui [INFERRED 0.85]

## Communities (13 total, 4 thin omitted)

### Community 0 - "Meter Lifecycle & Management"
Cohesion: 0.16
Nodes (12): ConcurrentDictionary, GXNet, IDisposable, IEnumerable, IHubContext, ILogger, int, MeterStatus (+4 more)

### Community 1 - "React UI Components"
Cohesion: 0.20
Nodes (8): ActivityLog(), KIND_META, CreateMeter(), MeterList(), ObjectTable(), api, App(), connectHub()

### Community 2 - "Frontend Build & Dependencies"
Cohesion: 0.12
Nodes (15): dependencies, @microsoft/signalr, react, react-dom, devDependencies, vite, @vitejs/plugin-react, name (+7 more)

### Community 3 - "DLMS Meter Simulation"
Cohesion: 0.22
Nodes (8): COSEM Object, GXDLMSMeter, GXDLMSMeter, Gurux.DLMS.Simulator.Net, SimMeter, ValueFormatter, DlmsProbe, ValueEventArgs

### Community 4 - "Backend Project & NuGet"
Cohesion: 0.18
Nodes (8): net9.0, Gurux.Net (8.4.2503.1001), Gurux.Serial (8.4.2503.603), Microsoft.NET.Sdk, Microsoft.NET.Sdk.Web, net9.0, Gurux.Net (8.4.2503.1001), Gurux.Serial (8.4.2503.603)

### Community 5 - "COSEM Objects & DTOs"
Cohesion: 0.25
Nodes (5): List, CosemAttributeDto, CosemObjectDto, CreateMeterRequest, SetAttributeRequest

### Community 6 - "App Host & Wiring"
Cohesion: 0.33
Nodes (4): React UI (Vite), Gurux.DLMS.Net, Hub, SimulatorHub

## Knowledge Gaps
- **29 isolated node(s):** `net9.0`, `Gurux.Net (8.4.2503.1001)`, `Gurux.Serial (8.4.2503.603)`, `Microsoft.NET.Sdk.Web`, `CosemAttributeDto` (+24 more)
  These have ≤1 connection - possible missing edges or undocumented components.
- **4 thin communities (<3 nodes) omitted from report** — run `graphify query` to explore isolated nodes.

## Suggested Questions
_Questions this graph is uniquely positioned to answer:_

- **Why does `MeterManager` connect `Meter Lifecycle & Management` to `DLMS Meter Simulation`, `COSEM Objects & DTOs`, `App Host & Wiring`, `Live Activity Events`?**
  _High betweenness centrality (0.189) - this node is a cross-community bridge._
- **Why does `SimMeter` connect `DLMS Meter Simulation` to `Meter Lifecycle & Management`?**
  _High betweenness centrality (0.102) - this node is a cross-community bridge._
- **Why does `SimulatorHub` connect `App Host & Wiring` to `Meter Lifecycle & Management`?**
  _High betweenness centrality (0.046) - this node is a cross-community bridge._
- **What connects `net9.0`, `Gurux.Net (8.4.2503.1001)`, `Gurux.Serial (8.4.2503.603)` to the rest of the system?**
  _29 weakly-connected nodes found - possible documentation gaps or missing edges._
- **Should `Frontend Build & Dependencies` be split into smaller, more focused modules?**
  _Cohesion score 0.125 - nodes in this community are weakly interconnected._