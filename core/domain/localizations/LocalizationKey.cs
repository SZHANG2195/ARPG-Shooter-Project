using System;
using System.Diagnostics;
using System.Text.Json.Serialization;

namespace lethal.core.domain.localizations;
[DebuggerDisplay("{Value}")]
[JsonConverter(typeof(StringIdJsonConverter<LocalizationKey>))]
public readonly record struct LocalizationKey : IStringId<LocalizationKey>
{
	public string Value { get; }
    public LocalizationKey(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        Value = value;
    }
    public bool IsDefault => Value is null;
    public override string ToString() => Value ?? string.Empty;
    public static LocalizationKey Create(string value) => new(value);
}
