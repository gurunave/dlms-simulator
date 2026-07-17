using Gurux.DLMS;
using Gurux.DLMS.Enums;
using Gurux.DLMS.Objects;

namespace Gurux.DLMS.Simulator.Net
{
    /// <summary>
    /// Information about a single COSEM attribute read/written by a client.
    /// </summary>
    public sealed class AttributeAccess
    {
        public string LogicalName { get; init; } = "";
        public string ObjectType { get; init; } = "";
        public int Index { get; init; }
        public string? Value { get; init; }
        public string Kind { get; init; } = "read"; // read | write
    }

    /// <summary>
    /// A simulated meter that extends the official Gurux <see cref="GXDLMSMeter"/>
    /// and raises events when a connected client reads or writes an attribute.
    /// The base behaviour (COSEM handling, template load/save) is unchanged.
    /// </summary>
    internal sealed class SimMeter : GXDLMSMeter
    {
        /// <summary>Identifier of the owning meter instance (assigned by MeterManager).</summary>
        public string MeterId { get; set; } = "";

        /// <summary>Raised after a client reads one or more attributes.</summary>
        public Action<string, AttributeAccess>? Accessed { get; set; }

        /// <summary>Raised when a client fails authentication. Args: (meterId, detail).</summary>
        public Action<string, string>? AuthFailed { get; set; }

        public SimMeter(bool logicalNameReferencing, InterfaceType type, bool useUtc2NormalTime, string flagId)
            : base(logicalNameReferencing, type, useUtc2NormalTime, flagId)
        {
        }

        protected override SourceDiagnostic ValidateAuthentication(Authentication authentication, byte[] password)
        {
            var result = base.ValidateAuthentication(authentication, password);
            if (result != SourceDiagnostic.None)
            {
                AuthFailed?.Invoke(MeterId, $"Authentication {authentication} rejected ({result})");
            }
            return result;
        }

        protected override void PostRead(ValueEventArgs[] args)
        {
            base.PostRead(args);
            Raise(args, "read");
        }

        protected override void PostWrite(ValueEventArgs[] args)
        {
            base.PostWrite(args);
            Raise(args, "write");
        }

        private void Raise(ValueEventArgs[] args, string kind)
        {
            var cb = Accessed;
            if (cb == null)
            {
                return;
            }
            foreach (ValueEventArgs it in args)
            {
                if (it.Target == null)
                {
                    continue;
                }
                cb(MeterId, new AttributeAccess
                {
                    LogicalName = it.Target.LogicalName ?? "",
                    ObjectType = it.Target.ObjectType.ToString(),
                    Index = it.Index,
                    Value = ValueFormatter.ToDisplay(it.Value),
                    Kind = kind,
                });
            }
        }
    }

    /// <summary>Helpers to turn COSEM attribute values into display strings.</summary>
    public static class ValueFormatter
    {
        public static string? ToDisplay(object? value)
        {
            switch (value)
            {
                case null:
                    return null;
                case byte[] bytes:
                    return Convert.ToHexString(bytes);
                case System.Collections.IEnumerable en when value is not string:
                    var parts = new List<string>();
                    foreach (var o in en)
                    {
                        parts.Add(ToDisplay(o) ?? "null");
                    }
                    return "[" + string.Join(", ", parts) + "]";
                default:
                    return value.ToString();
            }
        }
    }
}
