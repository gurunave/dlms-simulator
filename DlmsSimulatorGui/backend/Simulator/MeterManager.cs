using System.Collections.Concurrent;
using System.Diagnostics;
using DlmsSimulatorGui.Api.Hubs;
using Gurux.Common;
using Gurux.DLMS;
using Gurux.DLMS.Enums;
using Gurux.DLMS.Objects;
using Gurux.DLMS.Simulator.Net;
using Gurux.Net;
using Microsoft.AspNetCore.SignalR;

namespace DlmsSimulatorGui.Api.Simulator;

/// <summary>
/// Owns the lifecycle of every simulated meter and bridges DLMS server events
/// to the browser over SignalR. Registered as a singleton.
/// </summary>
public sealed class MeterManager : IDisposable
{
    private sealed class Instance
    {
        public required string Id;
        public required CreateMeterRequest Config;
        public SimMeter? Meter;
        public GXNet? Net;
        public MeterStatus Status = MeterStatus.Stopped;
        public int ClientCount;
        public string? Error;
    }

    private readonly ConcurrentDictionary<string, Instance> _meters = new();
    private readonly IHubContext<SimulatorHub> _hub;
    private readonly ILogger<MeterManager> _log;
    private readonly string _templatesDir;

    public MeterManager(IHubContext<SimulatorHub> hub, ILogger<MeterManager> log, IWebHostEnvironment env)
    {
        _hub = hub;
        _log = log;
        _templatesDir = Path.Combine(env.ContentRootPath, "templates");
        Directory.CreateDirectory(_templatesDir);
    }

    public string TemplatesDir => _templatesDir;

    // ---- Templates ---------------------------------------------------------

    public IEnumerable<string> ListTemplates() =>
        Directory.EnumerateFiles(_templatesDir, "*.xml")
                 .Select(Path.GetFileName)
                 .Where(x => x != null)
                 .Select(x => x!)
                 .OrderBy(x => x);

    public string ResolveTemplatePath(string name)
    {
        // Guard against path traversal; only bare file names are allowed.
        var safe = Path.GetFileName(name);
        return Path.Combine(_templatesDir, safe);
    }

    // ---- Meter CRUD --------------------------------------------------------

    public IEnumerable<MeterInfo> List() => _meters.Values.Select(ToInfo).OrderBy(m => m.Port);

    public MeterInfo? Get(string id) => _meters.TryGetValue(id, out var m) ? ToInfo(m) : null;

    public MeterInfo Create(CreateMeterRequest req)
    {
        if (req.Port is < 1 or > 65535)
        {
            throw new ArgumentException("Port must be between 1 and 65535.");
        }
        if (string.IsNullOrWhiteSpace(req.Template) || !File.Exists(ResolveTemplatePath(req.Template)))
        {
            throw new ArgumentException($"Template '{req.Template}' not found.");
        }
        if (_meters.Values.Any(m => m.Config.Port == req.Port))
        {
            throw new ArgumentException($"Port {req.Port} is already assigned to another meter.");
        }
        var id = Guid.NewGuid().ToString("N")[..8];
        if (string.IsNullOrWhiteSpace(req.Name))
        {
            req.Name = $"Meter {req.Serial}";
        }
        var inst = new Instance { Id = id, Config = req };
        _meters[id] = inst;
        _log.LogInformation("Created meter {Id} on port {Port}", id, req.Port);
        return ToInfo(inst);
    }

    public MeterInfo Start(string id)
    {
        var inst = Require(id);
        lock (inst)
        {
            if (inst.Status == MeterStatus.Running)
            {
                return ToInfo(inst);
            }
            try
            {
                var itype = string.Equals(inst.Config.Interface, "HDLC", StringComparison.OrdinalIgnoreCase)
                    ? InterfaceType.HDLC : InterfaceType.WRAPPER;
                var meter = new SimMeter(inst.Config.UseLogicalName, itype, false, "GRX")
                {
                    MeterId = id,
                    Accessed = OnAccessed,
                    AuthFailed = OnAuthFailed,
                };
                var net = new GXNet(NetworkType.Tcp, inst.Config.Port) { Server = true };
                net.OnClientConnected += (_, e) => OnClient(id, e.Info?.ToString() ?? "", true);
                net.OnClientDisconnected += (_, e) => OnClient(id, e.Info?.ToString() ?? "", false);

                var templatePath = ResolveTemplatePath(inst.Config.Template);
                meter.Initialize(net, TraceLevel.Error, templatePath, inst.Config.Serial, false, null);
                ApplySecurity(meter, inst.Config);

                inst.Meter = meter;
                inst.Net = net;
                inst.ClientCount = 0;
                inst.Status = MeterStatus.Running;
                inst.Error = null;
                _log.LogInformation("Started meter {Id} on port {Port}", id, inst.Config.Port);
            }
            catch (Exception ex)
            {
                inst.Status = MeterStatus.Error;
                inst.Error = ex.Message;
                SafeClose(inst);
                _log.LogError(ex, "Failed to start meter {Id}", id);
            }
        }
        BroadcastStatus(inst);
        return ToInfo(inst);
    }

    public MeterInfo Stop(string id)
    {
        var inst = Require(id);
        lock (inst)
        {
            SafeClose(inst);
            inst.Status = MeterStatus.Stopped;
            inst.ClientCount = 0;
            inst.Error = null;
        }
        BroadcastStatus(inst);
        _log.LogInformation("Stopped meter {Id}", id);
        return ToInfo(inst);
    }

    public void Delete(string id)
    {
        if (_meters.TryRemove(id, out var inst))
        {
            lock (inst)
            {
                SafeClose(inst);
            }
        }
    }

    // ---- COSEM objects -----------------------------------------------------

    public List<CosemObjectDto> GetObjects(string id)
    {
        var inst = Require(id);
        GXDLMSObjectCollection items;
        if (inst.Meter != null)
        {
            items = inst.Meter.Items;
        }
        else
        {
            // Meter not running: read the template file directly.
            items = GXDLMSObjectCollection.Load(ResolveTemplatePath(inst.Config.Template));
        }
        var result = new List<CosemObjectDto>();
        foreach (GXDLMSObject obj in items)
        {
            var dto = new CosemObjectDto
            {
                LogicalName = obj.LogicalName ?? "",
                ObjectType = obj.ObjectType.ToString(),
                Description = obj.Description ?? "",
            };
            object[] values;
            try
            {
                values = obj.GetValues();
            }
            catch
            {
                values = Array.Empty<object>();
            }
            for (int i = 0; i < values.Length; i++)
            {
                var v = values[i];
                dto.Attributes.Add(new CosemAttributeDto
                {
                    Index = i + 1,
                    Name = i == 0 ? "Logical Name" : $"Attribute {i + 1}",
                    Value = ValueFormatter.ToDisplay(v),
                    Type = v?.GetType().Name ?? "",
                });
            }
            result.Add(dto);
        }
        return result;
    }

    /// <summary>Edit one attribute of a running meter's COSEM object.</summary>
    public CosemObjectDto SetAttribute(string id, string logicalName, SetAttributeRequest req)
    {
        var inst = Require(id);
        if (inst.Meter == null)
        {
            throw new InvalidOperationException("Meter must be running to edit values.");
        }
        var obj = inst.Meter.Items.FindByLN(ObjectType.None, logicalName)
                  ?? inst.Meter.Items.Cast<GXDLMSObject>().FirstOrDefault(o => o.LogicalName == logicalName);
        if (obj == null)
        {
            throw new ArgumentException($"Object {logicalName} not found.");
        }
        if (obj is not IGXDLMSBase)
        {
            throw new InvalidOperationException("Object does not support editing.");
        }

        // Convert the incoming string to the type the attribute currently holds.
        object[] current = obj.GetValues();
        object? existing = (req.Index - 1) < current.Length ? current[req.Index - 1] : null;
        object? converted = ConvertValue(req.Value, existing);

        var e = new ValueEventArgs(obj, req.Index, 0, null) { Value = converted };
        ((IGXDLMSBase)obj).SetValue(inst.Meter.Settings, e);

        // Persist the change to the template file so it survives a restart.
        try
        {
            var s = new GXXmlWriterSettings();
            inst.Meter.Items.Save(ResolveTemplatePath(inst.Config.Template), s);
        }
        catch (Exception ex)
        {
            _log.LogWarning(ex, "Could not persist template for meter {Id}", id);
        }

        return GetObjects(id).First(o => o.LogicalName == logicalName);
    }

    private static object? ConvertValue(string? raw, object? existing)
    {
        if (raw == null)
        {
            return null;
        }
        try
        {
            switch (existing)
            {
                case null: return raw;
                case byte[]: return Convert.FromHexString(raw);
                case string: return raw;
                case bool: return bool.Parse(raw);
                case sbyte: return sbyte.Parse(raw);
                case byte: return byte.Parse(raw);
                case short: return short.Parse(raw);
                case ushort: return ushort.Parse(raw);
                case int: return int.Parse(raw);
                case uint: return uint.Parse(raw);
                case long: return long.Parse(raw);
                case ulong: return ulong.Parse(raw);
                case float: return float.Parse(raw);
                case double: return double.Parse(raw);
                default:
                    return Convert.ChangeType(raw, existing.GetType());
            }
        }
        catch
        {
            // Fall back to the raw string; the DLMS layer will validate.
            return raw;
        }
    }

    // ---- Security ----------------------------------------------------------

    /// <summary>
    /// Applies the configured authentication level and secret to every
    /// association object on a freshly initialized meter. Association secrets
    /// are [XmlIgnore] in Gurux, so they never live in the template file and
    /// must be set in code on each start.
    /// </summary>
    private static void ApplySecurity(SimMeter meter, CreateMeterRequest cfg)
    {
        if (!Enum.TryParse<Authentication>(cfg.AuthenticationLevel, ignoreCase: true, out var level))
        {
            level = Authentication.None;
        }
        var secret = level == Authentication.None ? null : ParseSecret(cfg.Password);

        foreach (GXDLMSObject obj in meter.Items.GetObjects(ObjectType.AssociationLogicalName))
        {
            if (obj is GXDLMSAssociationLogicalName ln)
            {
                ln.AuthenticationMechanismName.MechanismId = level;
                if (secret != null)
                {
                    ln.Secret = secret;
                }
            }
        }
        foreach (GXDLMSObject obj in meter.Items.GetObjects(ObjectType.AssociationShortName))
        {
            if (obj is GXDLMSAssociationShortName sn && secret != null)
            {
                sn.Secret = secret;
            }
        }
    }

    /// <summary>Parses a secret string: "0x"-prefixed = hex, otherwise ASCII.</summary>
    private static byte[]? ParseSecret(string? pwd)
    {
        if (string.IsNullOrEmpty(pwd))
        {
            return null;
        }
        if (pwd.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
        {
            try { return Convert.FromHexString(pwd[2..]); } catch { /* fall through to ASCII */ }
        }
        return System.Text.Encoding.ASCII.GetBytes(pwd);
    }

    // ---- Event bridge ------------------------------------------------------

    private void OnAuthFailed(string meterId, string detail) =>
        Push(new ActivityEvent { MeterId = meterId, Kind = "auth", Detail = detail });

    private void OnAccessed(string meterId, AttributeAccess a)
    {
        Push(new ActivityEvent
        {
            MeterId = meterId,
            Kind = a.Kind,
            LogicalName = a.LogicalName,
            Index = a.Index,
            Value = a.Value,
            Detail = $"{a.ObjectType} {a.LogicalName}:{a.Index}",
        });
    }

    private void OnClient(string meterId, string info, bool connected)
    {
        if (_meters.TryGetValue(meterId, out var inst))
        {
            lock (inst)
            {
                inst.ClientCount = Math.Max(0, inst.ClientCount + (connected ? 1 : -1));
            }
            BroadcastStatus(inst);
        }
        Push(new ActivityEvent
        {
            MeterId = meterId,
            Kind = connected ? "connected" : "disconnected",
            Detail = info,
        });
    }

    private void BroadcastStatus(Instance inst) =>
        _hub.Clients.All.SendAsync("meterStatus", ToInfo(inst));

    private void Push(ActivityEvent ev) =>
        _hub.Clients.All.SendAsync("activity", ev);

    // ---- Helpers -----------------------------------------------------------

    private Instance Require(string id) =>
        _meters.TryGetValue(id, out var m) ? m : throw new KeyNotFoundException($"Meter {id} not found.");

    private static void SafeClose(Instance inst)
    {
        try { inst.Meter?.Close(); } catch { /* ignore */ }
        try { inst.Net?.Close(); } catch { /* ignore */ }
        inst.Meter = null;
        inst.Net = null;
    }

    private static MeterInfo ToInfo(Instance i) => new()
    {
        Id = i.Id,
        Name = i.Config.Name ?? "",
        Port = i.Config.Port,
        Serial = i.Config.Serial,
        Status = i.Status.ToString(),
        UseLogicalName = i.Config.UseLogicalName,
        Interface = i.Config.Interface,
        Template = i.Config.Template,
        ClientCount = i.ClientCount,
        ObjectCount = i.Meter?.Items.Count ?? 0,
        Error = i.Error,
        AuthenticationLevel = i.Config.AuthenticationLevel,
        HasPassword = !string.IsNullOrEmpty(i.Config.Password),
    };

    public void Dispose()
    {
        foreach (var inst in _meters.Values)
        {
            lock (inst) { SafeClose(inst); }
        }
        _meters.Clear();
    }
}
