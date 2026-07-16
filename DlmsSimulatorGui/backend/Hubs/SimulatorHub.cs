using Microsoft.AspNetCore.SignalR;

namespace DlmsSimulatorGui.Api.Hubs;

/// <summary>
/// Real-time channel to the browser. The backend pushes "activity" (client
/// connections and attribute reads/writes) and "meterStatus" messages.
/// </summary>
public sealed class SimulatorHub : Hub
{
}
