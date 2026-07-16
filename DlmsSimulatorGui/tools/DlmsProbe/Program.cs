using System.Diagnostics;
using Gurux.DLMS;
using Gurux.DLMS.Enums;
using Gurux.DLMS.Objects;
using Gurux.DLMS.Reader;
using Gurux.DLMS.Secure;
using Gurux.Net;

// Minimal DLMS client that connects to a running simulated meter and reads a
// couple of attributes. Used to verify the GUI backend end-to-end (a real
// client read should surface in the live-activity log via SignalR).
//
// Usage: DlmsProbe <host> <port>

string host = args.Length > 0 ? args[0] : "127.0.0.1";
int port = args.Length > 1 ? int.Parse(args[1]) : 4061;

Console.WriteLine($"Connecting to {host}:{port} (WRAPPER, LN, no authentication)…");

var client = new GXDLMSSecureClient(true, 16, 1, Authentication.None, null, InterfaceType.WRAPPER);
var media = new GXNet(NetworkType.Tcp, host, port);
var reader = new GXDLMSReader(client, media, TraceLevel.Off, null);

try
{
    media.Open();
    reader.InitializeConnection();
    Console.WriteLine("Association established.");

    // Read the COSEM logical device name (0.0.42.0.0.255, attribute 2).
    var ldn = new GXDLMSData("0.0.42.0.0.255");
    object v1 = reader.Read(ldn, 2);
    Console.WriteLine($"Logical device name (0.0.42.0.0.255:2) = {Format(v1)}");

    // Read the meter serial (0.0.96.1.0.255, attribute 2).
    var serial = new GXDLMSData("0.0.96.1.0.255");
    object v2 = reader.Read(serial, 2);
    Console.WriteLine($"Serial number (0.0.96.1.0.255:2)       = {Format(v2)}");

    // Read the clock (0.0.1.0.0.255, attribute 2).
    var clock = new GXDLMSClock();
    object v3 = reader.Read(clock, 2);
    Console.WriteLine($"Clock time (0.0.1.0.0.255:2)           = {Format(v3)}");

    Console.WriteLine("READ OK");
}
catch (Exception ex)
{
    Console.WriteLine("PROBE FAILED: " + ex.Message);
    Environment.ExitCode = 1;
}
finally
{
    try { reader.Close(); } catch { }
}

static string Format(object v) => v switch
{
    null => "null",
    byte[] b => Convert.ToHexString(b) + "  (\"" + System.Text.Encoding.ASCII.GetString(b) + "\")",
    _ => v.ToString() ?? "null",
};
