DLMS Simulator - Windows (x64) standalone build
================================================

WHAT THIS IS
  A self-contained build of the DLMS Simulator GUI. The .NET runtime is
  bundled, so nothing needs to be installed - just run it.

HOW TO RUN
  1. Double-click  "Start DLMS Simulator.cmd"
       - it starts the server and opens http://localhost:5000 in your browser.
       - if the page loads before the server is ready, just refresh once.
  2. To stop: close the console window (or press Ctrl+C in it).

  Prefer to run it directly?  Double-click DlmsSimulatorGui.Api.exe and then
  open the address it prints (default http://localhost:5000) in a browser.

  To use a different port, set ASPNETCORE_URLS first, e.g. in a command prompt:
       set ASPNETCORE_URLS=http://0.0.0.0:8080
       DlmsSimulatorGui.Api.exe

NETWORK ACCESS (other machines on the same LAN)
  The server listens on all interfaces, so other PCs can open:
       http://<this-pc-ip>:5000
  The launcher prints the network address(es) on startup. The first time,
  allow the app through Windows Firewall (choose "Private networks" when
  prompted, or add an inbound rule for the port).

FOLDER CONTENTS
  DlmsSimulatorGui.Api.exe   The simulator (server + web UI).
  wwwroot\                   The web UI (served by the exe).
  templates\                 Meter templates loaded on first run.
  tools\DlmsProbe.exe        A small DLMS client to test a running meter:
                                 tools\DlmsProbe.exe 127.0.0.1 4061

NOTES
  - Windows SmartScreen may warn because the exe is unsigned. Choose
    "More info" -> "Run anyway".
  - The firewall may prompt the first time a meter opens a TCP port; allow it
    if you want other machines to connect.

For full usage instructions see USAGE.md in the source repository:
  https://github.com/gurunave/dlms-simulator
