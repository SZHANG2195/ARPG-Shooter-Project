using System;
using System.Diagnostics;
using System.Text.Json.Serialization;

namespace lethal.core.domain.context;
[DebuggerDisplay("{Value}")]
[JsonConverter(typeof(StringIdJsonConverter<ContextId>))]
public readonly record struct ContextId : IStringId<ContextId>, IComparable<ContextId>
{
    public string Value { get; }

    public ContextId(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        Value = value;
    }

    //default(ContextId) skips the constructor, so Value can be null
    public bool IsDefault => Value is null;

    public override string ToString() => Value ?? string.Empty;

    public int CompareTo(ContextId other) => string.CompareOrdinal(Value, other.Value);

    public static ContextId Create(string value) => new(value);
}
