@echo off
title DLMS Simulator
setlocal enabledelayedexpansion
set PORT=5000
set ASPNETCORE_URLS=http://0.0.0.0:%PORT%
echo ============================================================
echo   DLMS Simulator
echo ============================================================
echo.
echo   On this PC:        http://localhost:%PORT%
for /f "tokens=2 delims=:" %%a in ('ipconfig ^| findstr /c:"IPv4"') do (
  set IP=%%a
  set IP=!IP: =!
  echo   On the network:    http://!IP!:%PORT%
)
echo.
echo   Share a "network" address above with other PCs on the same LAN.
echo   (If they cannot connect, allow the app through Windows Firewall.)
echo.
echo   Close this window (or press Ctrl+C) to stop the simulator.
echo ============================================================
start "" http://localhost:%PORT%
"%~dp0DlmsSimulatorGui.Api.exe"
