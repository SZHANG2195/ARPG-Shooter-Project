using System;
using System.Diagnostics;
using System.Text.Json.Serialization;

namespace lethal.core.domain.payloads;

[DebuggerDisplay("{Value}")]
[JsonConverter(typeof(StringIdJsonConverter<PayloadId>))]
public readonly record struct PayloadId : IStringId<PayloadId>, IComparable<PayloadId>
{
    public string Value { get; }

    public PayloadId(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        Value = value;
    }

    // default(PayloadId) skips the constructor, so Value can be null
    public bool IsDefault => Value is null;

    public override string ToString() => Value ?? string.Empty;

    public int CompareTo(PayloadId other) => string.CompareOrdinal(Value, other.Value);

    public static PayloadId Create(string value) => new(value);
}