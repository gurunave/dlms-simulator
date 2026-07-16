# AGENTS.md

This repository's agent/LLM guide lives in **[CLAUDE.md](CLAUDE.md)** — it works
for any coding assistant, not just Claude. Start there.

Deep dives:
- **[docs/ARCHITECTURE.md](docs/ARCHITECTURE.md)** — diagrams and design rationale.
- **[docs/EXTENDING.md](docs/EXTENDING.md)** — step-by-step recipes for adding features.

Quick facts:
- Backend: ASP.NET Core (net9.0) in `DlmsSimulatorGui/backend` — REST + SignalR,
  runs the Gurux DLMS library in-process.
- Frontend: React + Vite in `DlmsSimulatorGui/frontend`.
- `Gurux.DLMS.Net/` is a vendored GPL dependency — **do not edit it**; subclass instead.
- No test suite; verify by running the app and `tools/DlmsProbe`.
