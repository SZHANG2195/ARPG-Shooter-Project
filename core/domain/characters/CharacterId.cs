using System;
using System.Diagnostics;
using System.Text.Json.Serialization;

namespace lethal.core.domain.characters;
[DebuggerDisplay("{Value}")]
[JsonConverter(typeof(StringIdJsonConverter<CharacterId>))]
public readonly record struct CharacterId : IStringId<CharacterId>, IComparable<CharacterId>
{
    public string Value { get; }

    public CharacterId(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        Value = value;
    }

    // default(CharacterId) skips the constructor, so Value can be null
    public bool IsDefault => Value is null;

    public override string ToString() => Value ?? string.Empty;

    public int CompareTo(CharacterId other) => string.CompareOrdinal(Value, other.Value);

    public static CharacterId Create(string value) => new(value);
}
