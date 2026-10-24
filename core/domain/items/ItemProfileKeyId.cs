using System;
using System.Diagnostics;
using System.Text.Json.Serialization;

namespace lethal.core.domain.items;
[DebuggerDisplay("{Value}")]
[JsonConverter(typeof(StringIdJsonConverter<ItemProfileKeyId>))]
public readonly record struct ItemProfileKeyId : IStringId<ItemProfileKeyId>, IComparable<ItemProfileKeyId>
{
	public string Value { get; }

    public ItemProfileKeyId(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        Value = value;
    }

    // default(StatId) skips the constructor, so Value can be null
    public bool IsDefault => Value is null;

    public override string ToString() => Value ?? string.Empty;

    public int CompareTo(ItemProfileKeyId other) => string.CompareOrdinal(Value, other.Value);

    public static ItemProfileKeyId Create(string value) => new(value);
}
