DLMS Simulator - Linux / macOS standalone build
================================================

WHAT THIS IS
  A self-contained build of the DLMS Simulator GUI. The .NET runtime is
  bundled, so nothing needs to be installed - just run it.

HOW TO RUN
  1. Open a terminal in this folder.
  2. Make the launcher executable the first time:
         chmod +x start-dlms-simulator.sh DlmsSimulatorGui.Api
  3. Start it:
         ./start-dlms-simulator.sh
     It serves the UI and (best-effort) opens http://localhost:5000.
  4. To stop: press Ctrl+C.

  Run the binary directly instead:
         ASPNETCORE_URLS=http://0.0.0.0:5000 ./DlmsSimulatorGui.Api

  Use a different port:
         PORT=8080 ./start-dlms-simulator.sh

NETWORK ACCESS (other machines on the same LAN)
  The server listens on all interfaces, so other PCs can open:
         http://<this-machine-ip>:5000
  The launcher prints the network address(es) on startup. Make sure your
  firewall allows inbound connections on the port.

FOLDER CONTENTS
  DlmsSimulatorGui.Api       The simulator (server + web UI).
  wwwroot/                   The web UI (served by the app).
  templates/                 Meter templates loaded on first run.
  tools/DlmsProbe            A small DLMS client to test a running meter:
                                 ./tools/DlmsProbe 127.0.0.1 4061

  macOS note: the binaries are unsigned. If Gatekeeper blocks them, allow them
  in System Settings > Privacy & Security, or run:
         xattr -dr com.apple.quarantine .

For full usage instructions see USAGE.md in the source repository:
  https://github.com/gurunave/dlms-simulator
