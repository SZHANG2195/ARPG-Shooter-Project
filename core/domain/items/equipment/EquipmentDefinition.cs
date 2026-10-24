using System.Collections.Immutable;
using System.Text.Json.Serialization;
using lethal.core.domain.affixes;
using lethal.core.domain.payloads;
using lethal.core.domain.tags;

namespace lethal.core.domain.items.equipment;
public sealed record EquipmentDefinition
{
	[JsonPropertyName("ItemId")]
	public required ItemId Id { get; init; }
	public required ImmutableArray<TagId> SlotTagIds { get; init; }
	public ImmutableArray<PayloadId> BaseStatPayloadIds { get; init; } = ImmutableArray<PayloadId>.Empty;
	public ImmutableArray<AffixId> ImplicitAffixIds { get; init; } = ImmutableArray<AffixId>.Empty;
	public ImmutableArray<AffixId> RelicAffixIds { get; init; } = ImmutableArray<AffixId>.Empty;
}
