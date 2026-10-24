using System;
using System.Diagnostics;
using System.Text.Json.Serialization;

namespace lethal.core.domain.tags;
[DebuggerDisplay("{Value}")]
[JsonConverter(typeof(StringIdJsonConverter<TagId>))]
public readonly record struct TagId : IStringId<TagId>, IComparable<TagId>
{
    public string Value { get; }

    public TagId(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        Value = value;
    }

    // default(TagId) skips the constructor, so Value can be null
    public bool IsDefault => Value is null;

    public override string ToString() => Value ?? string.Empty;

    public int CompareTo(TagId other) => string.CompareOrdinal(Value, other.Value);

    public static TagId Create(string value) => new(value);
}