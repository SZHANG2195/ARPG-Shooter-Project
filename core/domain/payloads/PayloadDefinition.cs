using System.Collections.Immutable;
using System.Text.Json.Serialization;
using lethal.core.domain.localizations;
using lethal.core.domain.payloads.enums;
using lethal.core.domain.tags;

namespace lethal.core.domain.payloads;
[JsonConverter(typeof(PayloadDefinitionConverter))]
public sealed record PayloadDefinition
{
	public required PayloadId Id { get; init; }
	public required LocalizationKey DescriptionLocalizationKey { get; init; }
	public required PayloadBehaviorTypes PayloadBehaviorType { get; init; }
	public required PayloadBehaviorGroup PayloadBehaviorGroup { get; init; }
	public ImmutableArray<TagId> DefaultsForTagIds { get; init; } = ImmutableArray<TagId>.Empty;
	public required IPayload PayloadData { get; init; }
} 
