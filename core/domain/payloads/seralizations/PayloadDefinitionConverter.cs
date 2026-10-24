using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using lethal.core.domain.localizations;
using lethal.core.domain.payloads.enums;

namespace lethal.core.domain.payloads;

public sealed class PayloadDefinitionConverter : JsonConverter<PayloadDefinition>
{
    public override PayloadDefinition Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        using var doc = JsonDocument.ParseValue(ref reader);
        var row = doc.RootElement;

        string rawId = RequireString(row, "Id", "(unknown)");
        string descriptionKey = RequireString(row, "DescriptionLocalizationKey", rawId);

        try
        {
            var behavior = ReadEnum<PayloadBehaviorTypes>(row, "PayloadBehaviorType", options);
            var group = ReadEnum<PayloadBehaviorGroup>(row, "PayloadBehaviorGroup", options);

            var expectedGroup = PayloadBehaviorMap.GroupFor(behavior);
            if (group != expectedGroup)
                throw new JsonException($"group '{group}' doesn't match '{expectedGroup}', which is what '{behavior}' belongs to.");

            var payloadType = PayloadBehaviorMap.RecordTypeFor(behavior);

            if (!TryGet(row, "PayloadData", out var dataElement))
                throw new JsonException("missing 'PayloadData'.");

            object? data = dataElement.ValueKind switch
            {
                JsonValueKind.Object => dataElement.Deserialize(payloadType, options),
                JsonValueKind.String => JsonSerializer.Deserialize(dataElement.GetString()!, payloadType, options),
                _ => throw new JsonException($"PayloadData must be an object or a JSON string, got {dataElement.ValueKind}.")
            };

            return new PayloadDefinition
            {
                Id = new PayloadId(rawId),
                DescriptionLocalizationKey = new LocalizationKey(descriptionKey),
                PayloadBehaviorType = behavior,
                PayloadBehaviorGroup = group,
                PayloadData = (IPayload)data!
            };
        }
        catch (Exception ex) when (ex is JsonException or ArgumentException)
        {
            throw new JsonException($"Payload '{rawId}': {ex.Message}", ex);
        }
    }

    public override void Write(Utf8JsonWriter writer, PayloadDefinition value, JsonSerializerOptions options)
        => throw new NotSupportedException("PayloadDefinition is read-only data.");

    private static T ReadEnum<T>(JsonElement row, string name, JsonSerializerOptions options) where T : struct, Enum
    {
        if (!TryGet(row, name, out var element))
            throw new JsonException($"missing '{name}'.");
        return element.Deserialize<T>(options);
    }

    private static string RequireString(JsonElement obj, string name, string rowId)
    {
        if (TryGet(obj, name, out var v) && v.ValueKind == JsonValueKind.String
            && !string.IsNullOrWhiteSpace(v.GetString()))
            return v.GetString()!;
        throw new JsonException($"Payload '{rowId}': missing or blank '{name}'.");
    }

    private static bool TryGet(JsonElement obj, string name, out JsonElement value)
    {
        foreach (var p in obj.EnumerateObject())
            if (p.Name.Equals(name, StringComparison.OrdinalIgnoreCase)) { value = p.Value; return true; }
        value = default;
        return false;
    }
}