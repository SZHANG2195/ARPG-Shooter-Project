using System;
using System.Diagnostics;
using System.Text.Json.Serialization;

namespace lethal.core.domain.affixes;
[DebuggerDisplay("{Value}")]
[JsonConverter(typeof(StringIdJsonConverter<AffixId>))]
public readonly record struct AffixId : IStringId<AffixId>, IComparable<AffixId>
{
    public string Value { get; }

    public AffixId(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        Value = value;
    }

    // default(AffixId) skips the constructor, so Value can be null
    public bool IsDefault => Value is null;

    public override string ToString() => Value ?? string.Empty;

    public int CompareTo(AffixId other) => string.CompareOrdinal(Value, other.Value);

    public static AffixId Create(string value) => new(value);
}
