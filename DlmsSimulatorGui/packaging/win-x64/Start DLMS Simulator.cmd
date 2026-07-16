@echo off
title DLMS Simulator
set ASPNETCORE_URLS=http://localhost:5000
echo ============================================================
echo   DLMS Simulator
echo ============================================================
echo.
echo   Starting server on http://localhost:5000
echo   Your browser will open automatically (refresh once if it
echo   loads before the server is ready).
echo.
echo   Close this window (or press Ctrl+C) to stop the simulator.
echo ============================================================
start "" http://localhost:5000
"%~dp0DlmsSimulatorGui.Api.exe"
