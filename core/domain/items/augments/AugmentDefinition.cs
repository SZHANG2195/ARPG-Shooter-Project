using System.Collections.Immutable;
using System.Text.Json.Serialization;
using lethal.core.domain.payloads;
using lethal.core.domain.tags;

namespace lethal.core.domain.items.augments;
public sealed record AugmentDefinition
{
	[JsonPropertyName("ItemId")]
	public required ItemId Id { get; init; }
	public ImmutableArray<TagId> ExclusivityTagIds { get; init; } = ImmutableArray<TagId>.Empty;
	public ImmutableDictionary<TagId, ImmutableArray<PayloadId>> CategoryPayloadIds { get; init; } = ImmutableDictionary<TagId, ImmutableArray<PayloadId>>.Empty;
}
