using System;
using System.Diagnostics;
using System.Text.Json.Serialization;

namespace lethal.core.domain.items;
[DebuggerDisplay("{Value}")]
[JsonConverter(typeof(StringIdJsonConverter<ItemId>))]
public readonly record struct ItemId : IStringId<ItemId>, IComparable<ItemId>
{
	public string Value { get; }

    public ItemId(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        Value = value;
    }

    //default(ItemId) skips the constructor, so Value can be null
    public bool IsDefault => Value is null;

    public override string ToString() => Value ?? string.Empty;

    public int CompareTo(ItemId other) => string.CompareOrdinal(Value, other.Value);

    public static ItemId Create(string value) => new(value);
}
