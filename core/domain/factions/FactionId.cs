using System;
using System.Diagnostics;
using System.Text.Json.Serialization;

namespace lethal.core.domain.factions;
[DebuggerDisplay("{Value}")]
[JsonConverter(typeof(StringIdJsonConverter<FactionId>))]
public readonly record struct FactionId : IStringId<FactionId>, IComparable<FactionId>
{
    public string Value { get; }

    public FactionId(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        Value = value;
    }

    //default(FactionId) skips the constructor, so Value can be null
    public bool IsDefault => Value is null;

    public override string ToString() => Value ?? string.Empty;

    public int CompareTo(FactionId other) => string.CompareOrdinal(Value, other.Value);

    public static FactionId Create(string value) => new(value);
}
