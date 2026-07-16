namespace DlmsSimulatorGui.Api.Simulator;

public enum MeterStatus
{
    Stopped,
    Running,
    Error
}

/// <summary>Request body for creating a meter.</summary>
public sealed class CreateMeterRequest
{
    public string? Name { get; set; }
    public int Port { get; set; }
    public uint Serial { get; set; } = 1;
    public string Template { get; set; } = "";
    public bool UseLogicalName { get; set; } = true;
    /// <summary>HDLC or WRAPPER.</summary>
    public string Interface { get; set; } = "WRAPPER";
}

/// <summary>Summary of a meter for list views.</summary>
public sealed class MeterInfo
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public int Port { get; set; }
    public uint Serial { get; set; }
    public string Status { get; set; } = "Stopped";
    public bool UseLogicalName { get; set; }
    public string Interface { get; set; } = "";
    public string Template { get; set; } = "";
    public int ClientCount { get; set; }
    public int ObjectCount { get; set; }
    public string? Error { get; set; }
}

public sealed class CosemAttributeDto
{
    public int Index { get; set; }
    public string Name { get; set; } = "";
    public string? Value { get; set; }
    public string Type { get; set; } = "";
}

public sealed class CosemObjectDto
{
    public string LogicalName { get; set; } = "";
    public string ObjectType { get; set; } = "";
    public string Description { get; set; } = "";
    public List<CosemAttributeDto> Attributes { get; set; } = new();
}

/// <summary>Request body for editing a single attribute value.</summary>
public sealed class SetAttributeRequest
{
    public int Index { get; set; }
    public string? Value { get; set; }
}

/// <summary>A live connection/read event pushed over SignalR.</summary>
public sealed class ActivityEvent
{
    public string MeterId { get; set; } = "";
    public string Kind { get; set; } = ""; // connected | disconnected | read | write | status
    public string? Detail { get; set; }
    public string? LogicalName { get; set; }
    public int? Index { get; set; }
    public string? Value { get; set; }
    public string Time { get; set; } = DateTime.Now.ToString("HH:mm:ss");
}
