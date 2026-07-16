#!/usr/bin/env bash
# Launcher for the DLMS Simulator (Linux / macOS).
set -e
cd "$(dirname "$0")"

PORT="${PORT:-5000}"
# Listen on all interfaces so other machines on the LAN can connect.
export ASPNETCORE_URLS="http://0.0.0.0:${PORT}"

echo "============================================================"
echo "  DLMS Simulator"
echo "============================================================"
echo
echo "  On this machine:  http://localhost:${PORT}"
# Best-effort: print LAN IPv4 addresses to share with other machines.
if command -v hostname >/dev/null 2>&1; then
  for ip in $(hostname -I 2>/dev/null || ipconfig getifaddr en0 2>/dev/null || true); do
    echo "  On the network:   http://${ip}:${PORT}"
  done
fi
echo
echo "  Close this window (or press Ctrl+C) to stop the simulator."
echo "============================================================"

# Open a browser (best-effort) shortly after the server starts.
( sleep 2
  if command -v xdg-open >/dev/null 2>&1; then xdg-open "http://localhost:${PORT}" >/dev/null 2>&1 || true
  elif command -v open >/dev/null 2>&1; then open "http://localhost:${PORT}" >/dev/null 2>&1 || true
  fi ) &

exec ./DlmsSimulatorGui.Api
