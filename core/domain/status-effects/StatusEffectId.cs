using System;
using System.Diagnostics;
using System.Text.Json.Serialization;

namespace lethal.core.domain.status_effects;
[DebuggerDisplay("{Value}")]
[JsonConverter(typeof(StringIdJsonConverter<StatusEffectId>))]
public readonly record struct StatusEffectId : IStringId<StatusEffectId>, IComparable<StatusEffectId>
{
	public string Value { get; }

    public StatusEffectId(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        Value = value;
    }

    // default(StatusEffectId) skips the constructor, so Value can be null
    public bool IsDefault => Value is null;

    public override string ToString() => Value ?? string.Empty;

    public int CompareTo(StatusEffectId other) => string.CompareOrdinal(Value, other.Value);

    public static StatusEffectId Create(string value) => new(value);
}
