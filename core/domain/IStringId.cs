using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace lethal.core.domain;
public interface IStringId<TSelf> where TSelf : struct, IStringId<TSelf>
{
    string Value { get; }
    static abstract TSelf Create(string value);
}

public sealed class StringIdJsonConverter<T> : JsonConverter<T>
    where T : struct, IStringId<T>
{
    public override T Read(ref Utf8JsonReader r, Type t, JsonSerializerOptions o)
    {
        var s = r.GetString();
        if (string.IsNullOrWhiteSpace(s)) 
			throw new JsonException($"{typeof(T).Name} was null or blank in serialized data.");
        return T.Create(s);
    }

    public override void Write(Utf8JsonWriter w, T v, JsonSerializerOptions o)
        => w.WriteStringValue(v.Value);

    // Only exercised if the ID is used as a Dictionary key (.NET 8+)
    public override T ReadAsPropertyName(ref Utf8JsonReader r, Type t, JsonSerializerOptions o)
        => Read(ref r, t, o);

    public override void WriteAsPropertyName(Utf8JsonWriter w, T v, JsonSerializerOptions o)
        => w.WritePropertyName(v.Value);
}