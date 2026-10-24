using System;
using System.Diagnostics;
using System.Text.Json.Serialization;

namespace lethal.core.domain.stats;
[DebuggerDisplay("{Value}")]
[JsonConverter(typeof(StringIdJsonConverter<StatId>))]
public readonly record struct StatId : IStringId<StatId>, IComparable<StatId>
{
	public string Value { get; }

    public StatId(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        Value = value;
    }

    // default(StatId) skips the constructor, so Value can be null
    public bool IsDefault => Value is null;

    public override string ToString() => Value ?? string.Empty;

    public int CompareTo(StatId other) => string.CompareOrdinal(Value, other.Value);

    public static StatId Create(string value) => new(value);
}
